using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moducom.Instrumentation.Abstract;
using Moducom.Instrumentation.Abstract.Experimental;
using System;
using System.Collections.Generic;
using System.Linq;

using PRO = Moducom.Instrumentation.Prometheus;
using MOD = Moducom.Instrumentation.Abstract;
using Prometheus.Client;
using Prometheus.Client.Collectors;
//using Prometheus.Contracts;

using PROC = Prometheus.Client;
using PROCR = Prometheus.Client.Contracts;

namespace Moducom.Instrumentation.Test
{
    [TestClass]
    public class PrometheusTests
    {
        [TestMethod]
        public void PrometheusBaseTest()
        {
            var cr = CollectorRegistry.Instance;

            var counter = Metrics.CreateCounter("myCounter", "Description of my counter", "allowed_label");

            counter.Labels("5").Inc();

            // this won't work, only 'allowed_label' defined.  
            //counter.Labels("4", "test").Inc();

            // interestingly this does work, so null labels permitted
            counter.Inc();

            // grabs same metric... OK that will get us off the ground for a working codebase
            // Just have to decide if we are gonna maintain our own repo tree or not
            global::Prometheus.Client.Counter counter2 = Metrics.CreateCounter("myCounter", "Description of my counter", "allowed_label");

            // dummy code so far
            var p = new PRO.Provider();
        }

        [TestMethod]
        public void PrometheusProviderTest()
        {
            //var c = Metrics.CreateCounter("test", "TEST");
            var c = Metrics.CreateCounter("root_test", "TEST");
            PROC.Counter c2 = Metrics.CreateCounter("test", "TEST", "instance");

            c2.Labels("1").Inc();
            c2.Labels("2").Inc(5);

            var collected = c2.Collect();

            var r = new PRO.Repository();

            var metric = r["test"];

            c.Inc();

            var counter = metric.GetCounter();
            //var counter = MOD.INodeExtensions.AddCounterExperimental(metric);

            Assert.AreEqual(1, counter.Value);

            c.Inc();

            //var metric2 = r["test"].AddMetric<MOD.ICounter>();
            var metric2 = r["test"].GetCounter();
            //var metric2 = r["test"].GetMetric<MOD.ICounter>();

            Assert.AreEqual(2, metric2.Value);
        }


        [TestMethod]
        public void PrometheusLabelBreakerTest()
        {
            Metrics.CreateCounter("breaker_test", 
                "Hopefully this doesn't break", 
                new[] { "label1", "label2" });

            return;

            // all 3 of these break
            Metrics.CreateCounter("breaker_test",
                "Hopefully this doesn't break");

            Metrics.CreateCounter("breaker_test",
                "Hopefully this doesn't break",
                new[] { "label2" });

            Metrics.CreateCounter("breaker_test",
                "Hopefully this doesn't break",
                new[] { "label3", "label2" });
        }


        class FakeCounter : Collector<PROC.Counter.ThisChild>
        {
            public FakeCounter(string name, string help, params string[] labelNames) : 
                base(name, help, labelNames)
            {
            }

            protected override PROCR.MetricType Type => PROCR.MetricType.Counter;
        }

        [TestMethod]
        public void PrometheusLowLevelTest()
        {
            ICollectorRegistry registry = CollectorRegistry.Instance;

            var fakeCounter = new FakeCounter("fake_counter", "Fake Counter", "label1");

            registry.GetOrAdd(fakeCounter);

            fakeCounter.Labels("1").Inc(7);
            fakeCounter.Labels("2").Inc(14);

            var fakeCounters = fakeCounter.Collect();

            var fakeCounter2 = new FakeCounter("fake_counter", "Fake Counter", "label2", "label3");

            return;

            // label mismatch induces an exception here
            var fakeCounter2_retrieved = registry.GetOrAdd(fakeCounter2);

            Assert.AreSame(fakeCounter, fakeCounter2_retrieved);
            Assert.AreNotSame(fakeCounter2, fakeCounter2_retrieved);
            //new Collector<Counter.ThisChild>();
            //new Counter();

        }


        [TestMethod]
        public void PrometheusInteractionTest()
        {
            // This test specifically tries to INITIALIZE in native prometheus client,
            // then RETRIEVE via our client
            var c = Metrics.CreateCounter("root_counter1", "No help", "label1");
            var _c = c.Labels("5");
            _c.Inc(10);

            var repo = new PRO.Repository();

            var test = repo["counter1"];

            var moducomCounter = test.GetCounter(new { label1 = 5 });

            Assert.AreEqual(moducomCounter.Value, _c.Value);
        }


        [TestMethod]
        public void PrometheusLabelValidatorTest()
        {
            var r = new PRO.Repository();

            var metric = (PRO.Node) r["label_validator_test"];

            metric.Initialize("instance", "test_a", "disposition");

            var counter_i5 = metric.GetMetric<MOD.IGauge>(new { instance = 5 });
            var counter_i3 = metric.GetMetric<MOD.IGauge>(new { disposition = "good", instance = 3 });

            counter_i5.Increment(1);
            counter_i3.Decrement(1);

            // Just a sanity check make sure everything is working as expected
            Assert.AreEqual(1, counter_i5.Value);
            Assert.AreEqual(-1, counter_i3.Value);

            // not supported any more
            //Assert.AreEqual(5, counter_i5.GetLabelValue("instance"));
            //Assert.AreEqual(3, counter_i3.GetLabelValue("instance"));

            try
            {
                var counter_invalid = metric.GetMetric<MOD.IGauge>(new { attitude = "bad", instance = 3 });
                Assert.Fail("Exception should have been thrown");
            }
            catch(IndexOutOfRangeException)
            {

            }

        }

        [TestMethod]
        public void PrometheusHistogramTest()
        {
            var histogram = Metrics.CreateHistogram("myHistogram", "Description of my histogram", "allowed_label");

            histogram.Observe(1);
        }

        [TestMethod]
        public void PrometheusWrapperHistogramTest()
        {
            var r = new PRO.Repository();

            PRO.Node metric = r["histogram_test"];

            metric.Initialize("disposition");

            // FIX: rest of code crashes
            return;

            var histogram1 = metric.GetMetric<MOD.IHistogram>(new { disposition = "good" });

            histogram1.Value = 1;
            histogram1.Value = 2;
        }
    }
}