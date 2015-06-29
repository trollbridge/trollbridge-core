using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Trollbridge.Common
{
    [DataContract]
    public class ShareAccessToken
    {
        private string _sasToken = "";
        [DataMember]
        public string SasToken
        {
            get { return _sasToken; }
            set { _sasToken = value; }
        }

        private string _Name = "";
        [DataMember]
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private Uri _uri = null;
        [DataMember]
        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        private DateTime _DateExpires = DateTime.UtcNow;
        [DataMember]
        public DateTime DateExpires
        {
            get { return _DateExpires; }
            set { _DateExpires = value; }
        }
    }
}
