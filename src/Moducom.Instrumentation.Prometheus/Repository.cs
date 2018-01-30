using MOD = Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PRO = global::Prometheus;
using global::Prometheus.Client;
using global::Prometheus.Client.Collectors;
using Moducom.Instrumentation.Abstract.Experimental;

namespace Moducom.Instrumentation.Prometheus
{
    /// <summary>
    /// Repository oriented specifically towards wrapping a prometheus client ICollectorRegistry
    /// </summary>
    internal class Repository : 
        Moducom.Instrumentation.Experimental.TaxonomyBase<Node, MOD.INode>, MOD.IRepository
    {
        readonly ICollectorRegistry registry;

        protected override Node CreateNode(Node parent, string name)
        {
            return new Node(registry, parent, name);
        }

        readonly Node rootNode;

        internal Repository(ICollectorRegistry registry)
        {
            this.registry = registry;
            rootNode = CreateNode(null, "root");
        }


        internal Repository() : this(CollectorRegistry.Instance) { }



        public override Node RootNode => rootNode;
    }
}
