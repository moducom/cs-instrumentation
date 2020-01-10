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
using Prometheus.Client.Collectors.Abstractions;

namespace Moducom.Instrumentation.Prometheus
{
    /// <summary>
    /// Repository oriented specifically towards wrapping a prometheus client ICollectorRegistry
    /// </summary>
    /// <remarks>
    /// </remarks>
    internal class Repository : 
        Moducom.Instrumentation.Experimental.TaxonomyBase<Node, MOD.INode>, MOD.IRepository
    {
        readonly ICollectorRegistry registry;

        const string DEFAULT = "root";

        protected override Node CreateNode(Node parent, string name)
        {
            return new Node(registry, parent, name);
        }

        readonly Node rootNode;

        internal Repository(ICollectorRegistry registry, string rootName = DEFAULT)
        {
            this.registry = registry;
            // TODO: Utilize _CreateNode or change CreateNode signature so that node creation events fire
            rootNode = CreateNode(null, rootName);
        }

        public override Node RootNode => rootNode;
    }
}
