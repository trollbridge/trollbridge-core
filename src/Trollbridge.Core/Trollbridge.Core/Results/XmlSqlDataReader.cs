using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Trollbridge.Core.Results
{
    public class XmlSqlDataReader
    {
        public XmlReader XmlReader;
        public int SpReturnValue; // Stored procedure return value (if stored procedure called)

        public XmlSqlDataReader()
        {
        }
    }
}
