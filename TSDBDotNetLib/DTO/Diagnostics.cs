using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib
{
    public class Diagnostics
    {
        public DateTime StartDate { get; set; }
        public DateTime LastSuccessfulPut { get; set; }
        public ulong SentDatapoints { get; set; }
        public ulong SuccessfulSentDatapoints { get; set; }
        public ulong FailedSentDatapoints { get; set; }
        public long PendingPutRequests { get; set; }
        public long LastQueryElapsedMs { get; set; }
        public long LastQueryResponseSize { get; set; }
        public double Rate { get
            {
                var millisecs= ((DateTime.Now - StartDate).TotalMilliseconds);
                return SentDatapoints / millisecs;
                       
            }
        }

        public double SuccessfulRate
        {
            get
            {
                var millisecs = ((DateTime.Now - StartDate).TotalMilliseconds);
                return SuccessfulSentDatapoints / millisecs;

            }
        }
        public void Reset()
        {
            StartDate = LastSuccessfulPut= DateTime.MinValue;
            SentDatapoints = SuccessfulSentDatapoints = FailedSentDatapoints = 0;
            PendingPutRequests = LastQueryElapsedMs = LastQueryResponseSize = 0;

        }
        override public  string  ToString()
        {
            var outMessage = String.Format("Start Date: {0}\r\nLast Successful Put:{1}\r\nSent data points: {2}\t Successful: {3}\t sent-successful {7}  Failed: {4}\t Pending put rqs:{5}\r\nRate: {6} put/ms\r\nLast query elapsed: {8}ms\tLast successful query response size:{9} chars\r\n",
                this.StartDate,
                this.LastSuccessfulPut,
                this.SentDatapoints,
                this.SuccessfulSentDatapoints,
                this.FailedSentDatapoints,
                PendingPutRequests,
                this.Rate, 
                SentDatapoints-SuccessfulSentDatapoints,
                LastQueryElapsedMs,
                LastQueryResponseSize);
            return outMessage;
        }
    }
}
