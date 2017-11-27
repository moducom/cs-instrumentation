using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moducom.Instrumentation.Abstract;
using Moducom.Instrumentation.Abstract.Experimental;
using Moducom.Instrumentation.Experimental;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Moducom.Instrumentation.Test
{
    [TestClass]
    public class UnitTest1
    {
        void setup(INode node)
        {
            node.AddCounter(new { instance = 1 });
            node.AddCounter(new { instance = 2 });

            var subNode = node.FindChildByPath(new[] { "subnode" }, key => new MemoryRepository.Node(key));

            subNode.AddCounter(new { instance = 3 });

            subNode = node.FindChildByPath(new[] { "subnode2" }, key => new MemoryRepository.Node(key));

            subNode.AddCounter(new { instance = 1 }).Increment();
        }

        /// <summary>
        /// This one also adds gauges 
        /// </summary>
        /// <param name="node"></param>
        static void setup2(INode node)
        {
            var gauge = node.GetMetricExperimental<IGauge>(new { instance = 3 });

            gauge.Value = 5;
            //node.A
        }

        [TestMethod]
        public void TestMethod1()
        {
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            setup(node);

            Assert.AreEqual("main", node.Name);

            node.Children.ToArray();

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
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            var value = node.AddCounter();

            value.SetLabels(new { instance = 1 });
            value.Increment(1);
        }


        [TestMethod]
        public void CounterLabelsTest()
        {
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            setup(node);

            var metrics = node.GetMetrics(new { instance = DBNull.Value }).ToArray();

            Assert.AreEqual(1, metrics[0].GetLabelValue("instance"));
            Assert.AreEqual(2, metrics[1].GetLabelValue("instance"));

            var metrics2 = node.GetMetrics().ToArray();

            Assert.AreEqual(1, metrics2[0].GetLabelValue("instance"));
            Assert.AreEqual(2, metrics2[1].GetLabelValue("instance"));
        }


        [TestMethod]
        public void FactoryTest()
        {
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            setup(node);

            var counter = node.AddCounterExperimental(new { test = 1 });

            counter.Increment(1);

            // NOTE: discouraged to add different types of metrics under one node
            var describer = node.AddMetricExperimental<string>();

            describer.SetLabels(new { test = 1 });
            describer.Value = "Test";
        }

        [TestMethod]
        public void CounterExperimentalTest()
        {
            var repo = new MemoryRepository();

            ICounter node = repo.GetCounterExperimental("counter/main");

            node.Increment();
        }


        [TestMethod]
        public void CounterNodeExperimentalTest()
        {
            var repo = new MemoryRepository();

            ICounterNode node = repo.GetCounterNodeExperimental("counter/main");

            node.Labels(new { instance = 1 }).Increment();
            node.Labels(new { instance = 2 }).Increment(77);
            node.Labels(new { instance = 1 }).Increment();

            Assert.AreEqual(2, node.Labels(new { instance = 1 }).Value);
            Assert.AreEqual(77, node.Labels(new { instance = 2 }).Value);
        }


        [TestMethod]
        public void TextFileDumpTest()
        {
            var repo = new MemoryRepository();

            setup(repo["counter/main"]);
            setup2(repo["gauge/main"]);

            var d = new TextFileDump(repo);

            var writer = new StringWriter();

            d.Dump(writer);

            writer.Flush();
            var result = writer.ToString();
        }


        [TestMethod]
        public void LabelBreakerTest()
        {
            var repo = new MemoryRepository();

            ICounter node = repo.GetCounterExperimental("counter/main", new { fail = true });

            node.Increment();

        }


        [TestMethod]
        public void GaugeTest()
        {
            var repo = new MemoryRepository();

            var gauge = repo["gauge/main"].GetMetricExperimental<IGauge>();

            gauge.Increment(5);
        }


        [TestMethod]
        public void HistogramTest()
        {
            var repo = new MemoryRepository();

            var histogram = repo["gauge/main"].GetMetricExperimental<IHistogram<double>>();

            var testStart = DateTime.Now;

            histogram.Value = 5;
            histogram.Value = 10;

            var testEnd = DateTime.Now;

            var values = histogram.Values.ToArray();

            Assert.AreEqual(5, values[0].Value);
            Assert.IsTrue(values[0].TimeStamp > testStart);
            Assert.IsTrue(values[0].TimeStamp < testEnd);
            Assert.AreEqual(10, values[1].Value);
            Assert.IsTrue(values[1].TimeStamp > testStart);
            Assert.IsTrue(values[1].TimeStamp < testEnd);

            Assert.AreEqual(2, histogram.GetCount(DateTime.MinValue));
            Assert.AreEqual(15, histogram.GetSum(DateTime.MinValue));
            Assert.AreEqual(7.5, histogram.GetAverage(DateTime.MinValue));
        }
    }
}
