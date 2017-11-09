using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moducom.Instrumentation.Abstract;
using System.Collections.Generic;
using System.Linq;

namespace Moducom.Instrumentation.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var repo = new DummyRepository();

            INode node = repo["counter/main"];

            Assert.AreEqual("main", node.Name);

            node.Children.ToArray();

            node.AddCounter(new { instance = 1 });
            node.AddCounter(new { instance = 2 });

            IEnumerable<IMetricBase> metrics = node.GetMetrics(new { instance = 1 }).ToArray();

            foreach(var metric in metrics)
            {
                metric.GetLabelValue("instance", out object value);

                Assert.AreEqual(1, value);
            }
        }

        [TestMethod]
        public void CounterMetricTest()
        {
            var repo = new DummyRepository();

            INode node = repo["counter/main"];

            var value = node.AddCounter();

            value.SetLabels(new { instance = 1 });
            value.Increment(1);
        }
    }
}
