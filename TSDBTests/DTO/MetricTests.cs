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
    public class MetricTests
    {
        [TestMethod()]
        public void MetricTest()
        {
            Metric metric = new Metric();
            metric.Key = "system.key.test";
            Assert.IsNotNull(metric.Tags);
            metric.Tags.Add("test", "value");
            Assert.IsTrue(metric.Tags.Count == 1);
            
        }

        [TestMethod()]
        public void MetricTestInit()
        {
            Metric metric = new Metric("system.key.test");
            Assert.IsTrue(metric.Key == "system.key.test");
            Assert.IsNotNull(metric.Tags);
            metric.Tags.Add("test", "value");
            Assert.IsTrue(metric.Tags.Count == 1);
        }

        [TestMethod()]
        public void MetricTestInit2()
        {
            Metric metric = new Metric("system.key.test");
        
            metric.Tags.Add("test", "value");
 
            Metric metric2 = new Metric(metric, "item");
            Assert.IsNotNull(metric2.Tags, "tags initialized");

            Assert.IsTrue(metric2.Key == metric.Key + ".item", "well formed composed key");
            metric2.Tags.Add("test2", "value2");

            Assert.IsTrue(metric2.Tags.Count == 2,"added tags");   
        }

        [TestMethod()]
        public void MetricTestInit3()
        {
            Metric metric = new Metric("system.key.test");

            metric.Tags.Add("test", "value");

            Metric metric2 = new Metric(metric);
            Assert.IsNotNull(metric2.Tags,"tags initialized");

            Assert.IsTrue(metric2.Key == metric.Key,"same key");
            metric2.Tags.Add("test2", "value2");

            Assert.IsTrue(metric2.Tags.Count == 2,"added tag");
        }
    }
}