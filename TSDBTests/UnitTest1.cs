using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSDBDotNetLib;

namespace TSDBTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            DataPoint datapoint = new DataPoint("sys.meter.uno",   12, DateTime.Now);
            datapoint.Tags.Add("tag1", "value");
         
            TSDBDotNetLib.Connector connector = new Connector(@"http://localhost:4242");

          


        }
    }
}
