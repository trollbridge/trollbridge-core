using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;

namespace Trollbridge.Common.PolicyInfo
{
    public class PolicyInfoQueue
    {
        public int CustomerId { get; set; }
        public int SiteId { get; set; }
        public int PolicyId { get; set; }
        public string PolicyIdentifier { get; set; }
        public SharedAccessQueuePermissions SAQueuePermissions { get; set; }
        public SharedAccessQueuePolicy AccessPolicy { get; set; }
        public DateTime DateExpires { get; set; }
        public string StorageConnectionString { get; set; }
        public string QueueName { get; set; }

        public Uri QueueUri { get; set; }

        private string _token = string.Empty;
        public string SASToken
        {
            get
            {
                if (_token.Length > 0 && DateExpires.ToUniversalTime() > DateTime.UtcNow.AddMinutes(10)) return _token;

                // Create the storage account with the connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue queue = queueClient.GetQueueReference(QueueName);
                queue.CreateIfNotExists();

                QueuePermissions queuePermissions = queue.GetPermissions();
                ICollection<string> keys = queuePermissions.SharedAccessPolicies.Keys;
                SharedAccessQueuePolicy sharedAccessPolicy = null;

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys.ElementAt(i).Equals(PolicyIdentifier))
                    {
                        sharedAccessPolicy = queuePermissions.SharedAccessPolicies.Values.ElementAt(i);
                        break;
                    }
                }

                if (sharedAccessPolicy == null || sharedAccessPolicy.SharedAccessExpiryTime.GetValueOrDefault(DateTimeOffset.UtcNow) < DateTimeOffset.UtcNow.AddMinutes(2))
                {
                    if (sharedAccessPolicy != null)
                    {
                        queuePermissions.SharedAccessPolicies.Remove(PolicyIdentifier);
                    }

                    // Create blob container permissions, consisting of a shared access policy 
                    // and a public access setting. 
                    sharedAccessPolicy = new SharedAccessQueuePolicy();
                    sharedAccessPolicy.SharedAccessExpiryTime = DateExpires;
                    sharedAccessPolicy.Permissions = SAQueuePermissions;

                    queuePermissions.SharedAccessPolicies.Add(PolicyIdentifier, sharedAccessPolicy);

                    // Set the permission policy on the container.
                    queue.SetPermissions(queuePermissions);
                }

                // Get the shared access signature to share with users.
                // Note that this call passes in an empty access policy, so that the shared access 
                // signature will use the PolicyIdentifier access policy that's defined on the queue.
                _token = Utilities.CompressString(queue.GetSharedAccessSignature(new SharedAccessQueuePolicy(), PolicyIdentifier));
                QueueUri = queue.Uri;
                return _token;
            }
        }
    }
}
