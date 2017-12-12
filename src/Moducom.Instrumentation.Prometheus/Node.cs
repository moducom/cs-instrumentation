using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using PRO = global::Prometheus;
using Moducom.Instrumentation.Abstract.Experimental;
using Prometheus.Contracts;

#if DEBUG
[assembly: InternalsVisibleTo("Moducom.Instrumentation.Test")]
[assembly: InternalsVisibleTo("Moducom.Instrumentation.Prometheus.TestExporter")]
#endif

namespace Moducom.Instrumentation.Prometheus
{
    internal class Node : 
        Experimental.Taxonomy.NodeBase<Node, INode>, 
        INode,
        Abstract.Experimental.IMetricProvider
    {
        internal PRO.Contracts.MetricFamily metricsFamily;
        PRO.Client.Collectors.ICollector collector;
        Repository repository;
        PRO.Client.MetricFactory metricFactory = PRO.Client.Metrics.DefaultFactory;
        static PRO.Client.Collectors.ICollectorRegistry registry = PRO.Client.Collectors.CollectorRegistry.Instance;

        // so that we can get fully-qualified name
        INode parent;

        internal Node(INode parent, string name) : base(name) { this.parent = parent; }


        class Collector<T> : PRO.Client.Collectors.Collector<T>
            where T: PRO.Client.Child, new()
        {
            public Collector(string name, string help, params string[] labelNames) : 
                base(name, help, labelNames)
            {
            }

            protected override MetricType Type => MetricType.COUNTER;
        }

        protected string GetFullName(char delimiter = '/')
        {
            INode node = this.parent;
            string fullname = Name;

            while(node != null)
            {
                fullname = node.Name + delimiter + fullname;
            }

            return fullname;
        }

        PRO.Client.Collectors.ICollector GetOrAdd<T>()
        {
            if (collector == null)
            {
                collector = new Collector<PRO.Client.Counter.ThisChild>(GetFullName('_'), "TBD");
                registry.GetOrAdd(collector);
            }

            return collector;
        }

        public void AddMetric(IMetricBase metric)
        {
            throw new NotImplementedException();

            var _metric = new PRO.Contracts.Metric();

            if (metric is ICounter)
            {
                var _c = new PRO.Contracts.Counter();
                //new CounterMetric(_c);
                // FIX: experimental and untested code
                _metric.counter = _c;
            }

            metricsFamily.metric.Add(_metric);
        }

        public IEnumerable<IMetricBase> GetMetrics(object labels)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<IMetricBase> Metrics => throw new NotImplementedException();


        /// <summary>
        /// Look up or create the metric
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="labels"></param>
        /// <returns></returns>
        public T GetMetric<T>(object labels) where T : ILabelsProvider, IValueGetter
        {
            var labelEnum = Experimental.MemoryRepository.LabelHelper(labels);
            var labelNames = labelEnum.Select(x => x.Key);
            var labelValues = labelEnum.Select(x => x.Value.ToString());

            if (typeof(T) == typeof(ICounter))
            {
                //if (metricsFamily != null)
                {
                    //var nativeCounter = new PRO.Contracts.Counter();
                    //var nativeCounter2 = new PRO.Client.Counter.ThisChild();

                    PRO.Client.Counter counter = metricFactory.CreateCounter(
                        GetFullName(), "TBD", labelNames.ToArray());

                    PRO.Client.Counter.ThisChild nativeCounterChild = counter.Labels(labelValues.ToArray());

                    collector = counter;
                    metricsFamily = counter.Collect();
                    return (T)(ICounter)new CounterMetric(counter);
                }
                //else
                //{
                 //   return default(T);
                //}
            }
            else if (typeof(T) == typeof(IGauge<double>))
            {
                PRO.Client.Gauge gauge = metricFactory.CreateGauge(GetFullName(), "TBD");
            }
            throw new NotImplementedException();
        }
    }

    /*
    public class MetricNode<T> : Abstract.Experimental.IMetricNode<T>
        where T: IMetricBase
    {
        public void AddMetric(IMetricBase metric)
        {
            throw new NotImplementedException();
        }

        public T1 AddMetric<T1>(string key = null) where T1 : IMetricBase
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMetricBase> GetMetrics(object labels = null)
        {
            throw new NotImplementedException();
        }

        public T Labels(object labels)
        {
            throw new NotImplementedException();
        }
    } */
}
