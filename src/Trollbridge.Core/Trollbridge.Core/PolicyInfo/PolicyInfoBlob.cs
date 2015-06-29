using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Trollbridge.Core.PolicyInfo
{
    public class PolicyInfoBlob
    {
        public int CustomerId { get; set; }
        public int SiteId { get; set; }
        public int PolicyId { get; set; }
        public string PolicyIdentifier { get; set; }
        public SharedAccessBlobPermissions BlobPermissions { get; set; }
        public SharedAccessBlobPolicy AccessPolicy { get; set; }
        public DateTime DateExpires { get; set; }
        public string StorageConnectionString { get; set; }
        public string ContainerName { get; set; }

        public Uri ContainerUri { get; set; }

        private string _token = string.Empty;
        public string SASToken
        {
            get
            {
                if (_token.Length > 0 && DateExpires.ToUniversalTime() > DateTime.UtcNow.AddMinutes(10)) return _token;

                // Create the storage account with the connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
                container.CreateIfNotExists();

                BlobContainerPermissions blobPermissions = container.GetPermissions();
                ICollection<string> keys = blobPermissions.SharedAccessPolicies.Keys;
                SharedAccessBlobPolicy sharedAccessPolicy = null;

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys.ElementAt(i).Equals(PolicyIdentifier))
                    {
                        sharedAccessPolicy = blobPermissions.SharedAccessPolicies.Values.ElementAt(i);
                        break;
                    }
                }

                if (sharedAccessPolicy == null || sharedAccessPolicy.SharedAccessExpiryTime.GetValueOrDefault(DateTimeOffset.UtcNow) < DateTimeOffset.UtcNow.AddMinutes(10))
                {
                    if (sharedAccessPolicy != null)
                    {
                        blobPermissions.SharedAccessPolicies.Remove(PolicyIdentifier);
                    }

                    // Since we either don't have a shared access policy, or the old one has expires (or about to)
                    // create a new shared access policy.
                    sharedAccessPolicy = new SharedAccessBlobPolicy();
                    sharedAccessPolicy.SharedAccessExpiryTime = DateExpires;
                    sharedAccessPolicy.Permissions = BlobPermissions;

                    blobPermissions.SharedAccessPolicies.Add(PolicyIdentifier, sharedAccessPolicy);

                    // The public access setting explicitly specifies that 
                    // the container is private, so that it can't be accessed anonymously.
                    blobPermissions.PublicAccess = BlobContainerPublicAccessType.Off;

                    // Set the permission policy on the container.
                    container.SetPermissions(blobPermissions);
                }

                // Get the shared access signature to share with users.
                // Note that this call passes in an empty access policy, so that the shared access 
                // signature will use the PolicyIdentifier access policy that's defined on the container.
                _token = Utilities.CompressString(container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), PolicyIdentifier));
                ContainerUri = container.Uri;
                return _token;
            }
        }
    }
}
