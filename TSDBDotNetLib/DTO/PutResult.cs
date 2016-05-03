using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib 
{
    public class PutResult
    {
        public OTSDBException OtsbException { get; set; }

        public Exception originalException { get; set; }
        public PutDetails details { get; set; }

    }
}
