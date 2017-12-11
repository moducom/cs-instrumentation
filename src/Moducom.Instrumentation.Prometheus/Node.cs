using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using PRO = global::Prometheus;
using Moducom.Instrumentation.Abstract.Experimental;

#if DEBUG
[assembly: InternalsVisibleTo("Moducom.Instrumentation.Test")]
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

        // so that we can get fully-qualified name
        INode parent;

        readonly string name;

        internal Node(string name) : base(name) { }

        protected string GetFullName()
        {
            INode node = this.parent;
            string fullname = name;

            while(node != null)
            {
                fullname = node.Name + "/" + fullname;
            }

            return fullname;
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

        public T AddMetric<T>(string key = null) where T : IMetricBase
        {
            return GetMetric<T>(null);
        }

        public IEnumerable<IMetricBase> GetMetrics(object labels = null)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Look up or create the metric
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="labels"></param>
        /// <returns></returns>
        public T GetMetric<T>(object labels = null) where T : ILabelsProvider, IValueGetter
        {
            if (typeof(T) == typeof(ICounter))
            {
                if (metricsFamily != null)
                {
                    //Experimental.MemoryRepository.Node.LabelHelper(labels);

                    PRO.Client.Counter counter = metricFactory.CreateCounter(GetFullName(), "TBD");
                    collector = counter;
                    metricsFamily = counter.Collect();
                    return (T)(ICounter)new CounterMetric(counter);
                }
                else
                {
                    return default(T);
                }
            }
            else if (typeof(T) == typeof(IGauge<double>))
            {
                PRO.Client.Gauge gauge = metricFactory.CreateGauge(GetFullName(), "TBD");
            }
            throw new NotImplementedException();
        }
    }


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
    }
}
