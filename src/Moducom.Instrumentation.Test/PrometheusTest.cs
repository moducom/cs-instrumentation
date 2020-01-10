﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using Prometheus.Client.Collectors.Abstractions;
using Prometheus.Client.MetricsWriter.Abstractions;
using System.Threading.Tasks;
//using PROCR = Prometheus.Client.Contracts;

namespace Moducom.Instrumentation.Test
{
    [TestClass]
    public class PrometheusTests
    {
        [TestMethod]
        public void PrometheusBaseTest()
        {
            var cr = new CollectorRegistry();
            //var cr = CollectorRegistry.Instance;

            var factory = new MetricFactory(cr);

            var counter = factory.CreateCounter("myCounter", "Description of my counter", "allowed_label");

            counter.Labels("5").Inc();

            // this won't work, only 'allowed_label' defined.  
            //counter.Labels("4", "test").Inc();

            // interestingly this does work, so null labels permitted
            counter.Inc();

            // grabs same metric... OK that will get us off the ground for a working codebase
            // Just have to decide if we are gonna maintain our own repo tree or not
            global::Prometheus.Client.Counter counter2 = factory.CreateCounter("myCounter", "Description of my counter", "allowed_label");

            // dummy code so far
            var p = new PRO.Provider();
        }

        [TestMethod]
        public void PrometheusProviderTest()
        {
            var registry = new CollectorRegistry();
            var factory = new MetricFactory(registry);
            //var c = Metrics.CreateCounter("test", "TEST");
            var c = factory.CreateCounter("root_test", "TEST");
            PROC.Counter c2 = factory.CreateCounter("test", "TEST2", "instance");
            var writer = new SyntheticMetricsWriter();

            c2.WithLabels("1").Inc();
            c2.WithLabels("2").Inc(5);

            //ScrapeHandler.ProcessAsync()
            //c2.Collect(writer);

            // factory adds this already.  I'd consider that somewhat of a side affect            
            //registry.Add(c2);

            var r = new PRO.Repository(registry, "root");

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
            var r = new CollectorRegistry();
            var factory = new MetricFactory(r);

            factory.CreateCounter("breaker_test", 
                "Hopefully this doesn't break", 
                new[] { "label1", "label2" });

            return;

            // all 3 of these break, labels must aligned perfectly
            factory.CreateCounter("breaker_test",
                "Hopefully this doesn't break");

            factory.CreateCounter("breaker_test",
                "Hopefully this doesn't break",
                new[] { "label2" });

            factory.CreateCounter("breaker_test",
                "Hopefully this doesn't break",
                new[] { "label3", "label2" });
        }


        class FakeCounter : PRO.Node.Collector<PROC.Counter.LabelledCounter>
        {
            public FakeCounter(string name, string help, params string[] labelNames) : 
                base(name, help, labelNames)
            {
            }

            protected override MetricType Type => MetricType.Counter;
        }

        [TestMethod]
        public void PrometheusLowLevelTest()
        {
            //ICollectorRegistry registry = CollectorRegistry.Instance;
            ICollectorRegistry registry = new CollectorRegistry();

            var fakeCounter = new FakeCounter("fake_counter", "Fake Counter", "label1");
            CollectorConfiguration config = new CollectorConfiguration("tester");
            IMetricsWriter writer;

            registry.GetOrAdd(config, cfg => fakeCounter);

            fakeCounter.Labels("1").Inc(7);
            fakeCounter.Labels("2").Inc(14);

            //var fakeCounters = fakeCounter.Collect();

            var fakeCounter2 = new FakeCounter("fake_counter", "Fake Counter", "label2", "label3");

            return;
            
            // label mismatch induces an exception here
            var fakeCounter2_retrieved = registry.GetOrAdd(config, cfg => fakeCounter2);

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
            var registry = new CollectorRegistry();
            var factory = new MetricFactory(registry);
            var c = factory.CreateCounter("root_counter1", "No help", "label1");
            var _c = c.Labels("5");
            _c.Inc(10);

            var repo = new PRO.Repository(registry);

            var test = repo["counter1"];

            var moducomCounter = test.GetCounter(new { label1 = 5 });

            Assert.AreEqual(moducomCounter.Value, _c.Value);
        }


        [TestMethod]
        public void PrometheusLabelValidatorTest()
        {
            var _r = new CollectorRegistry();
            var r = new PRO.Repository(_r);

            var metric = (PRO.Node) r["label_validator_test"];

            metric.Initialize("instance", "test_a", "disposition");

            Assert.ThrowsException<IndexOutOfRangeException>(delegate
            {
                metric.GetGauge(new { attitude = "bad", instance = 3 });
            });

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
        public void MultipleCollectorsAtSameNodeTest()
        {
            var r = new PROC.Collectors.CollectorRegistry();
            var factory = new PROC.MetricFactory(r);

            var counter = factory.CreateCounter("test", "test");

            // Cannot create gauge, get a duplicate collector name exception
            Assert.ThrowsException<InvalidOperationException>(() => 
                factory.CreateGauge("test", "test", "label1"));

            /*
            counter.Inc(1);
            gauge.Dec(1);

            var counter2 = factory.CreateCounter("test", "test");

            Assert.AreEqual(counter.Value, counter2.Value); */
        }
        

        [TestMethod]
        public void PrometheusWrapperHistogramTest()
        {
            var _r = new CollectorRegistry();
            var r = new PRO.Repository(_r);

            PRO.Node metric = r["histogram_test"];

            //metric.Initialize("disposition");

            var histogram1 = metric.GetHistogram(new { disposition = "good" });

            histogram1.Value = 1;
            histogram1.Value = 2;
        }
    }
}