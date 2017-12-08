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
            var c = Metrics.CreateCounter("test", "TEST");
            Metrics.CreateCounter("root_test", "TEST");

            var r = new PRO.Repository();

            var metric = r["test"];

            c.Inc();

            var counter = MOD.INodeExtensions.AddCounterExperimental(metric);

            var metric2 = r["test"].AddMetric<MOD.ICounter>();
        }
    }
}