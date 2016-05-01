using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSDBDotNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBDotNetLib.Tests
{
    [TestClass()]
    public class PutOptionsTests
    {
        [TestMethod()]
        public void ToStringTest()
        {
            PutOptions putoptions = new PutOptions();

            Assert.IsTrue(putoptions.ToString() == "", "empty");

            putoptions.Details = true;

            Assert.IsTrue(putoptions.ToString() == "?details", "only details");

            putoptions.Summary = true;

            Assert.IsTrue(putoptions.ToString() == "?details"," summary+details, only details");

            putoptions.Details = false;

            Assert.IsTrue(putoptions.ToString() == "?summary","summary only");


            putoptions.Sync = true;


            Assert.IsTrue(putoptions.ToString() == "?summary&sync", "summary & sync");

            putoptions.Summary = false;


            Assert.IsTrue(putoptions.ToString() == "?sync", "only sync");


            putoptions.Details = true;
            Assert.IsTrue(putoptions.ToString() == "?details&sync", "details+ sync");
        }
    }
}