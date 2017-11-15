using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PRO = global::Prometheus;
using global::Prometheus.Client;
using global::Prometheus.Client.Collectors;

namespace Moducom.Instrumentation.Prometheus
{
    internal class Repository : IRepository
    {
        ICollectorRegistry registry = CollectorRegistry.Instance;

        public INode this[string path]
        {
            get
            {
                registry.CollectAll().Single(x => x.name == path);
                return null;
            }
        }

        public INode RootNode => throw new NotImplementedException();
    }
}
