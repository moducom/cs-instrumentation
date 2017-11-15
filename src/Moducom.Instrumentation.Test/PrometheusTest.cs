using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moducom.Instrumentation.Abstract;
using Moducom.Instrumentation.Abstract.Experimental;
using System;
using System.Collections.Generic;
using System.Linq;

using PRO = Moducom.Instrumentation.Prometheus;
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

            // dummy code so far
            var p = new PRO.Provider();
        }
    }
}