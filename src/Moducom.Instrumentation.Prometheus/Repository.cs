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
    internal class Repository : 
        Moducom.Instrumentation.Experimental.TaxonomyBase<Node, MOD.INode>, MOD.IRepository
    {
        readonly ICollectorRegistry registry;

        protected override Node CreateNode(Node parent, string name)
        {
            return new Node(registry, parent, name);
        }

        Node rootNode;

        internal Repository(ICollectorRegistry registry)
        {
            this.registry = registry;
            rootNode = CreateNode(null, "root");
        }


        internal Repository() : this(CollectorRegistry.Instance) { }

        /*
        new // temporarily tagging as new as we pull in base classes
        public MOD.INode this[string path]
        {
            get
            {
                var m = registry.CollectAll();

                // FIX: Combine this with our memory repository to utilize our own tree-tracking code
                // OR decompose the collector results to form ad-hoc tree awareness
                var path_pieces = path.Split('/');
                var name = path_pieces[path_pieces.Length - 1];

                path = path.Replace('/', '_');

                var metricFamily = m.SingleOrDefault(x => x.name == path);

                if(metricFamily == null)
                {
                    var counter = Metrics.CreateCounter("test", "test");

                    var node = new Node(null, name);

                    node.AddMetric(new CounterMetric(counter));

                    return node;
                }
                else
                {
                    var node = new Node(null, name);

                    node.metricsFamily = metricFamily;

                    // FIX: Kludgey.  We might have to ad-hoc create nodes all the time, but we'd rather
                    // keep them persistent - even if it uses more memory, it's more deterministic for the
                    // client
                    foreach(var metric in metricFamily.metric)
                    {
                        //ICounter counter = metric.counter.value;
                        //node.AddMetric(new CounterMetric());
                    }

                    return node;
                }
            }
        }
        */

        public override Node RootNode => rootNode;
    }


    /*
    internal class MetricFactory : IMetricFactory
    {
        PRO.Client.MetricFactory factory = PRO.Client.Metrics.DefaultFactory;

        public T CreateMetric<T>(string key, object labels = null) 
            where T : ILabelsProvider, MOD.IValueGetter
        {
            //new CounterMetric()
        }
    } */

    internal class RepositoryExperimental : MOD.IRepository
    {
        public event Action<object, MOD.INode> NodeCreated;

        internal class Node : MOD.INode
        {
            public event Action<object, MOD.INode> ChildAdded;

            public IEnumerable<MOD.INode> Children => throw new NotImplementedException();

            readonly string name;

            public string Name => name;

            Node parent;

            internal Node(string name)
            {
                this.name = name;
            }

            string GetFullNodeName()
            {
                string name = Name;

                Node current = this;

                while(current.parent != null)
                {
                    name = current.parent.Name + "_" + name;
                }

                return name;
            }

            public void AddChild(MOD.INode child)
            {
                throw new NotImplementedException();
            }

            public void AddMetric(MOD.IMetricBase metric)
            {
                throw new NotImplementedException();
            }

            public MOD.INode GetChild(string name)
            {
                throw new NotImplementedException();
            }


            public T GetMetric<T>(object labels) where T: 
                Abstract.IValueGetter,
                Abstract.Experimental.ILabelsProvider
            {
                var labelEnum = Experimental.MemoryRepository.LabelHelper(labels);

                var factory = PRO.Client.Metrics.DefaultFactory;

                if(typeof(T) == typeof(ICounter))
                {
                    var nativeCounter = factory.CreateCounter(GetFullNodeName(), "TBD", labelEnum.Select(x => x.Key).ToArray());

                    nativeCounter.Labels(labelEnum.Select(x => x.Value.ToString()).ToArray());

                    var wrapped = new CounterMetric(nativeCounter);
                    return (T)(object)wrapped;
                }
                //PRO.Client.Metrics.
                return default(T);
            }

            public IEnumerable<MOD.IMetricBase> GetMetrics(object labels)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<MOD.IMetricBase> Metrics => throw new NotImplementedException();
        }

        internal class CounterWrapper : MOD.ICounter
        {
            readonly Counter.ThisChild child;

            internal CounterWrapper(Counter.ThisChild child) { this.child = child; }

            public double Value => child.Value;

            public bool GetLabelValue(string label, out object value)
            {
                throw new NotImplementedException();
            }

            public void Increment(double byAmount)
            {
                child.Inc(byAmount);
            }

            public void SetLabels(object labels)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> Labels => throw new NotImplementedException();

            public override bool Equals(object obj)
            {
                if(obj is CounterWrapper cw)
                {
                    return child.Equals(cw.child);
                }

                return false;
            }
        }

        public MOD.INode this[string path]
        {
            get
            {
                // FIX: Not sure how we can query existing registry for existence of a particular node path
                path.Split('/');
                path.Replace('/', '_');

                var node = new Node(path);

                return node;
            }
        }

        public MOD.INode RootNode => throw new NotImplementedException();
    }
}
