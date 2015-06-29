using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Trollbridge.Common
{
    [DataContract]
    public class TokenHolder
    {
        private DateTime _DateExpires = DateTime.Now;
        [DataMember]
        public DateTime EarliestTokenExpires
        {
            get { return _DateExpires; }
            set { _DateExpires = value; }
        }

        private List<ShareAccessToken> _azureStorage = null;
        [DataMember]
        public List<ShareAccessToken> AzureStorage
        {
            get { return _azureStorage; }
            set { _azureStorage = value; }
        }

        private List<ShareAccessToken> _azureQueues = null;
        [DataMember]
        public List<ShareAccessToken> AzureQueues
        {
            get { return _azureQueues; }
            set { _azureQueues = value; }
        }

        private List<ShareAccessToken> _azureEventHubs = null;
        [DataMember]
        public List<ShareAccessToken> AzureEventHubs
        {
            get { return _azureEventHubs; }
            set { _azureEventHubs = value; }
        }
    }
}
