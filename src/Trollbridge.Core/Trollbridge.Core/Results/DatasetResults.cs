using System;
using System.Data;
using System.Xml;


namespace Trollbridge.Core.Results
{
    public class DatasetResults
    {
        public DataSet DataSet { get; set; }   // Resulting DataSet from SQL query
        public int SpReturnValue { get; set; } // Stored procedure return value (if stored procedure called)

        public DatasetResults()
        {
        }
    }
}
