using Fact.Extensions.Collection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moducom.Instrumentation.Abstract;
using Moducom.Instrumentation.Abstract.Experimental;
using Moducom.Instrumentation.Experimental;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Moducom.Instrumentation.Test
{
    [TestClass]
    public class UnitTest1
    {
        void setup(INode node)
        {
            node.GetCounter(new { instance = 1 });
            node.GetCounter(new { instance = 2 });

            var subNode = node.FindChildByPath(new[] { "subnode" }, (parent, key) => new MemoryRepository.Node(key));

            subNode.GetCounter(new { instance = 3 });

            subNode = node.FindChildByPath(new[] { "subnode2" }, (parent, key) => new MemoryRepository.Node(key));

            subNode.GetCounter(new { instance = 1 }).Increment();
        }

        /// <summary>
        /// This one also adds gauges 
        /// </summary>
        /// <param name="node"></param>
        static void setup2(INode node)
        {
            var gauge = node.GetMetric<IGauge>(new { instance = 3 });

            gauge.Value = 5;
        }

        [TestMethod]
        public void TestMethod1()
        {
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            setup(node);

            Assert.AreEqual("main", node.Name);

            node.Children.ToArray();

            IEnumerable<IMetric> metrics = node.GetMetrics(new { instance = 1 }).ToArray();

            foreach(var metric in metrics)
            {
                var metricWithLabel = (IMetricWithLabels)metric;
                metricWithLabel.GetLabelValue("instance", out object value);

                Assert.AreEqual(1, value);
            }
        }

        [TestMethod]
        public void CounterMetricTest()
        {
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            var value = node.GetCounter(new { instance = 1 });
            value.Increment(1);
        }


        [TestMethod]
        public void CounterLabelsTest()
        {
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            setup(node);

            var _metrics = node.GetMetrics(new { instance = DBNull.Value });
            var metrics = _metrics.Cast<IMetricWithLabels>().ToArray();

            Assert.AreEqual(1, metrics[0].GetLabelValue("instance"));
            Assert.AreEqual(2, metrics[1].GetLabelValue("instance"));

            var metrics2 = node.Metrics.Cast<IMetricWithLabels>().ToArray();

            Assert.AreEqual(1, metrics2[0].GetLabelValue("instance"));
            Assert.AreEqual(2, metrics2[1].GetLabelValue("instance"));

            var labels = ((Abstract.Experimental.ILabelNamesProvider)node).Labels;
        }


        [TestMethod]
        public void FactoryTest()
        {
            var repo = new MemoryRepository();

            INode node = repo["counter/main"];

            setup(node);

            var counter = node.GetCounter(new { test = 1 });

            counter.Increment(1);

            // NOTE: discouraged to add different types of metrics under one node
            // right now the GetMetric code can't handle two different types with the same label
            // so we push in test = 2 for now
            var describer = node.GetGenericMetric<string>(new { test = 2 });

            //describer.SetLabels(new { test = 1 });
            describer.Value = "Test";

            var descriperWithMetrics = (IMetricWithLabels)describer;

            Assert.AreEqual("test", descriperWithMetrics.Labels.First());
            Assert.AreEqual(2, descriperWithMetrics.GetLabelValue("test"));
        }

        [TestMethod]
        public void CounterExperimentalTest()
        {
            var repo = new MemoryRepository();

            ICounter node = repo.GetCounter("counter/main");

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

            repo["uptime"].GetGauge();

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

            ICounter node = repo.GetCounter("counter/main", new { fail = true });

            node.Increment();

        }


        [TestMethod]
        public void LabelHelperTest()
        {
            var labels = Utility.LabelHelper(new { value1 = 1, value2 = "test" }).ToArray();

            Assert.AreEqual("value1", labels[0].Key);
            Assert.AreEqual(1, labels[0].Value);
            Assert.AreEqual("value2", labels[1].Key);
            Assert.AreEqual("test", labels[1].Value);
        }


        [TestMethod]
        public void GaugeTest()
        {
            var repo = new MemoryRepository();

            //var gauge = repo["gauge/main"].GetMetricExperimental<IGauge>();
            var gauge = repo["gauge/main"].GetGauge();

            gauge.Increment(5);

            Assert.AreEqual(5, gauge.Value);
        }


        [TestMethod]
        public void SummaryTest()
        {
            var repo = new MemoryRepository();

            var summary = repo["summaries/test1"].GetSummary();

            summary.Value = 5;
            summary.Value = 3;
            summary.Value = 20;

            var average = summary.Sum(x => x.Value) / summary.Count;

            Assert.AreEqual(28.0 / 3, average);
        }


        [TestMethod]
        public void HistogramTest()
        {
            var repo = new MemoryRepository();

            var histogram = repo["gauge/main"].GetMetric<IHistogram<double>>();

            var testStart = DateTime.Now.AddMilliseconds(-5); // do a little time traveling, since we might finish so fast...

            histogram.Value = 5;
            histogram.Value = 10;

            var testEnd = DateTime.Now.AddMilliseconds(5); // do a little time traveling, since we might finish so fast...

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


        [TestMethod]
        public void GetMetricTest()
        {
            var repo = new MemoryRepository();

            setup(repo.RootNode);

            var counter = repo.RootNode.GetMetric<ICounter>();
        }

        [TestMethod]
        public void GetMetricsTest()
        {
            var repo = new MemoryRepository();

            setup(repo.RootNode);

            try
            {
                var counter2 = repo.RootNode.GetMetrics(null).ToArray();
                Assert.Fail("Should have thrown exception");
            }
            catch (ArgumentNullException)
            {

            }

            var counter = repo.RootNode.GetMetrics(new { instance = 1 }).ToArray();

            var metric = counter[0];
        }


        class FullNameNode : INamed, IChild<FullNameNode>
        {
            internal string name;
            internal FullNameNode parent;

            public string Name => name;

            public FullNameNode Parent => parent;
        }


        [TestMethod]
        public void GetFullNameTest()
        {
            var root = new FullNameNode { name = "root" };
            var child = new FullNameNode { name = "child", parent = root };
            var grandchild = new FullNameNode { name = "child-again", parent = child };

            var fullname = grandchild.GetFullName(':');

            Assert.AreEqual("root:child:child-again", fullname);

            var root2 = new FullNameNode { name = null };
            var child2 = new FullNameNode { name = "child", parent = root2 };

            fullname = child2.GetFullName();

            Assert.AreEqual("child", fullname);
        }
    }
}
