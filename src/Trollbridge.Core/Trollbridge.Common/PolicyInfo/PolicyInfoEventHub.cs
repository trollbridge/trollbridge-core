using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Trollbridge.Common.PolicyInfo
{
    public class PolicyInfoEventHub
    {
        public int CustomerId { get; set; }
        public int SiteId { get; set; }
        public int PolicyId { get; set; }
        public string EventHubName { get; set; }
        public string PolicyIdentifier { get; set; }
        public int MinToExpire { get; set; }
        public DateTime DateExpires { get; set; }
        public string EventHubConnectionStr { get; set; }
        public Uri EventHubUri { get; set; }
        public bool Send { get; set; }
        public bool Listen { get; set; }
        public bool Manage { get; set; }

        private string _token = string.Empty;
        public string SASToken
        {
            get
            {
                if (_token.Length > 0 && DateExpires.ToUniversalTime() > DateTime.UtcNow.AddMinutes(10)) return _token;

                // EventHubConnectionStr = Endpoint=sb://iotproj.servicebus.windows.net/;SharedAccessKeyName=sendiotevents;SharedAccessKey=S5BMoh8yzJq9YlH4szENcrSGmVDzTwBELvxojPbxRpg=
                string[] connectionInfo = EventHubConnectionStr.Split(';');

                EventHubUri = new Uri(connectionInfo[0].Substring(9));

                string publisher = string.Format("site{0}", SiteId);

                string token = SharedAccessSignatureTokenProvider.GetPublisherSharedAccessSignature(
                                   EventHubUri, EventHubName, publisher, PolicyIdentifier, connectionInfo[2].Substring(16), new TimeSpan(0, MinToExpire, 0));

                // Get the shared access signature to share with users.
                // Note that this call passes in an empty access policy, so that the shared access 
                // signature will use the PolicyIdentifier access policy that's defined on the queue.
                _token = Utilities.CompressString(token);
                return _token;
            }
        }
    }
}
