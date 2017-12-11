using MOD = Moducom.Instrumentation.Abstract;
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
    internal class Repository : 
        Moducom.Instrumentation.Experimental.Taxonomy<Node, MOD.INode>, MOD.IRepository
    {
        static ICollectorRegistry registry = CollectorRegistry.Instance;

        protected override Node CreateNode(string name)
        {
            return new Node(name);
        }

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

                    var node = new Node(name);

                    node.AddMetric(new CounterMetric(counter));

                    return node;
                }
                else
                {
                    var node = new Node(name);

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

        public override MOD.INode RootNode => throw new NotImplementedException();
    }

    internal class RepositoryExperimental : MOD.IRepository
    {
        internal class Node : MOD.INode
        {
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

            public T AddMetric<T>(string key = null) where T : MOD.IMetricBase
            {
                if(typeof(T) == typeof(MOD.ICounter))
                {
                    // FIX: convert key from either anonymous type or dictionary to the 
                    // label names
                    var nativeCounter = Metrics.CreateCounter(GetFullNodeName(), null, null);

                    // this gets the particular instance associated with label values
                    // FIX: convert key from either anonymous type or dictionary to the
                    // label values
                    var child = nativeCounter.Labels(null);

                    // FIX: Do we really need to carry labels into the metric too?  I guess we do, in our architecture
                    return (T)(object)(new CounterWrapper(child));

                }
                throw new NotImplementedException();
            }

            public MOD.INode GetChild(string name)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<MOD.IMetricBase> GetMetrics(object labels = null)
            {
                throw new NotImplementedException();
            }
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
