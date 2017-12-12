﻿using Moducom.Instrumentation.Abstract;
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
        PRO.Client.Collectors.Collector<TNativeMetric> GetOrAdd<TNativeMetric>(string[] labelNames)
            where TNativeMetric : PRO.Client.Child, new()
        {
            if (collector == null)
            {
                var fullName = GetFullName('_');
                var c = new Collector<TNativeMetric>(fullName, "TBD", labelNames);
                var retrieved_collector = registry.GetOrAdd(c);

                if (c == retrieved_collector)
                {
                    // indicates we really did add a new one.  
                }
                else
                {
                    // indicates one already existed
                    // TODO: issue warning that we weren't the ones who registered the metric
                    //throw new IndexOutOfRangeException($"Key already added with a different type: {fullName}");
                }

                // in either case, we must map to the one already present in prometheus
                collector = retrieved_collector;
            }

            return (PRO.Client.Collectors.Collector<TNativeMetric>) collector;
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

        IMetricBase CounterHelper(PRO.Contracts.Metric metric)
        {
            return null;
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
                    {
                        var counterCollector = (PRO.Client.Collectors.Collector<PRO.Client.Counter.ThisChild>)collector;

                        return collected.metric.Select(Helper<IMetricBase>);
                    }
                }

                return null;
            }
        }


        /// <summary>
        /// </summary>
        /// <typeparam name="TNativeMetricChild"></typeparam>
        /// <param name="labelNames"></param>
        /// <returns></returns>
        TNativeMetricChild GetMetricHelper<TNativeMetricChild>(
            IEnumerable<string> labelNames,
            IEnumerable<string> labelValues)
            where TNativeMetricChild: PRO.Client.Child, new()
        {
            var c = GetOrAdd<TNativeMetricChild>(labelNames.ToArray());

            // FIX: Moducom layer allows omission of labels, but Prometheus
            // layer does not, so this is going to break without additional
            // support logic
            var nativeMetricChild = c.Labels(labelValues.ToArray());

            return nativeMetricChild;
        }

        TNativeMetricChild GetMetricHelper<TNativeMetricChild>(object labels)
            where TNativeMetricChild : PRO.Client.Child, new()
        {
            var labelEnum = Experimental.MemoryRepository.LabelHelper(labels);
            var labelNames = labelEnum.Select(x => x.Key);
            var labelValues = labelEnum.Select(x => x.Value.ToString());

            return GetMetricHelper<TNativeMetricChild>(labelNames, labelValues);
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
                var nativeCounter = GetMetricHelper<PRO.Client.Counter.ThisChild>(labelNames, labelValues);

                var moducomCounter = new CounterMetric(nativeCounter);

                return (T)(object)moducomCounter;
            }
            else if (typeof(T) == typeof(IGauge<double>))
            {
                var nativeGauge = GetMetricHelper<PRO.Client.Gauge.ThisChild>(labels);
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
