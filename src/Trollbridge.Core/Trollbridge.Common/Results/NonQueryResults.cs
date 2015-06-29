using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trollbridge.Common.Results
{
    public class NonQueryResults
    {
        public int ExecuteResult { get; set; } // Number of rows affected by the ExecuteNonQuery command
        public int SpReturnValue { get; set; } // Stored procedure return value (if stored procedure called)

        public NonQueryResults()
        {
        }
    }
}
