using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib
{
    public class PutOptions
    {
        public bool Details { get; set; }
        public bool Summary
        {
            get; set;
        }
        public bool Sync { get; set; }

        public override string ToString()
        {
            var outString = "";
             if (Details || Summary || Sync)
            {
                outString = "?";
                if (Details)
                {
                    outString += "details";
                }
                else
                if (Summary)
                {

                    outString += "summary";
                }

                if (Sync)
                {
                    if (Summary || Details)
                    {
                        outString += "&";
                    }
                    outString += "sync";
                }
            }
            return outString;
        }
    }
}
