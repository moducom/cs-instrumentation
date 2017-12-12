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

        /// <summary>
        /// Gets or Adds a metric with label template (as prometheus C# interfaces require)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        PRO.Client.Collectors.ICollector GetOrAdd<T>(string[] labelNames)
        {
            if (collector == null)
            {
                var c = new Collector<PRO.Client.Counter.ThisChild>(GetFullName('_'), "TBD", labelNames);
                var retrieved_collector = registry.GetOrAdd(c);

                if (c == retrieved_collector) { } // indicates we really did add a new one.  
                else
                {
                    // indicates one already existed
                    // TOOD: Issue a warning here
                }

                // in either case, we must map to the one already present in prometheus
                collector = retrieved_collector;
            }

            return collector;
        }


        /// <summary>
        /// GetMetrics won't work until some kind of metric recording has happened
        /// *through* our Node interface
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public IEnumerable<IMetricBase> GetMetrics(object labels)
        {
            if (collector == null) return Enumerable.Empty<IMetricBase>();

            throw new NotImplementedException();
        }

        T Helper<T>(PRO.Contracts.Metric metric)
        {
            //new CounterMetric2(metric.counter, metric.label.Select(x => x.value).ToArray());
            return default(T);
        }


        /// <summary>
        /// Metrics property won't work until some kind of metric recording has happened
        /// *through* our Node interface
        /// </summary>
        public IEnumerable<IMetricBase> Metrics
        {
            get
            {
                if (collector == null) return Enumerable.Empty<IMetricBase>();

                var collected = collector.Collect();

                switch(collected.type)
                {
                    case MetricType.COUNTER:
                        return collected.metric.Select(Helper<IMetricBase>);
                }

                return null;
            }
        }


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
