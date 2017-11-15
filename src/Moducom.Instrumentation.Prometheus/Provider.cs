using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using global::Prometheus.Client.Collectors;
using global::Prometheus.Client;

namespace Moducom.Instrumentation.Prometheus
{
    public class Provider
    {
        public Provider()
        {
            var instance = CollectorRegistry.Instance;
        }
    }
}
