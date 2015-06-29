using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trollbridge.Common.Results
{
    public class ScalarResults
    {
        public object ExecuteScalarReturnValue { get; set; }
        public int SpReturnValue { get; set; } // Stored procedure return value (if stored procedure called)

        public ScalarResults()
        {
        }
    }
}
