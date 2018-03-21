using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using PRO = global::Prometheus;
using Moducom.Instrumentation.Abstract.Experimental;
using Moducom.Instrumentation.Experimental;
using Prometheus.Client.Contracts;
using Fact.Extensions.Collection;

#if DEBUG
[assembly: InternalsVisibleTo("Moducom.Instrumentation.Test")]
[assembly: InternalsVisibleTo("Moducom.Instrumentation.Prometheus.TestExporter")]
#endif

namespace Moducom.Instrumentation.Prometheus
{
    internal class Node : 
        NamedChildCollection<Node>,
        INode,
        IChild<Node>,
        ILabelNamesProvider
    {
        //internal PRO.Client.Contracts.MetricFamily metricsFamily;
        PRO.Client.Collectors.ICollector collector;
        //PRO.Client.MetricFactory metricFactory = PRO.Client.Metrics.DefaultFactory;
        readonly PRO.Client.Collectors.ICollectorRegistry registry;

        // so that we can get fully-qualified name
        readonly INode parent;

        public Node Parent => (Node)parent;

        internal Node(PRO.Client.Collectors.ICollectorRegistry registry, INode parent, string name) : base(name)
        {
            this.registry = registry;
            this.parent = parent;
        }

        /// <summary>
        /// Description to feed directly into prometheus collector desc
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Our custom collector which for now is hardcoded to Counter type
        /// but eventually should be a shim around any type.  Pretty much 1:1
        /// with native collector, but we control creation of it
        /// Mostly useful to replace LabelNameOnlyCollector when we encounter it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        class Collector<T> : PRO.Client.Collectors.Collector<T>
            where T: PRO.Client.Child, new()
        {
            public Collector(string name, string help, params string[] labelNames) : 
                base(name, help, labelNames)
            {
            }

            protected override MetricType Type => MetricType.Counter;
        }


        /// <summary>
        /// Use this to pre-initialize labelnames so that subsequent partial-label lookups
        /// don't incorrectly initialize Prometheus
        /// </summary>
        class LabelNameOnlyCollector : PRO.Client.Collectors.ICollector
        {
            public string Name => "label_name_only";

            readonly string[] labelNames;

            internal LabelNameOnlyCollector(string[] labelNames)
            {
                // FIX: watch this, we want to copy the array not just the
                // array reference
                this.labelNames = labelNames;
            }

            public string[] LabelNames => labelNames;

            public MetricFamily Collect()
            {
                // Since this is a shim collector only to hold on to label names,
                // retrieving actual metrics is an undefined/unsupported operation
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets or Adds a metric with label template (as prometheus C# interfaces require)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        PRO.Client.Collectors.Collector<TNativeMetric> GetOrAdd<TNativeMetric>(string[] labelNames)
            where TNativeMetric : PRO.Client.Child, new()
        {
            // check to see if we've templated label collector names
            if (collector is LabelNameOnlyCollector labelCollector)
            {
                // FIX: this isn't quite in the right spot.  By the time we get here labelNames
                // have been homogonized by our LabelHelper, so we need to do it out there
                //var foreignLabels = labelCollector.LabelNames.Except(labelNames);

                // if any labels are leftover from labelNames REMOVING valid labelCollector.LabelNames
                // then we know invalid labels are present
                var foreignLabels = labelNames.Except(labelCollector.LabelNames);

                if (foreignLabels.Any())
                    throw new IndexOutOfRangeException(
                        $"Invalid label specified: {Fact.Extensions.Collection.StringEnumerationExtensions.ToString(foreignLabels, ",")}");

                labelNames = labelCollector.LabelNames;

                // LabelNameOnlyCollector did its job and cached LabelNames, so remove it
                collector = null;
            }

            // If no real data collector for this Node has been instantiated, instantiate one now
            if (collector == null)
            {
                // ascertain name in context of taxonomy
                var fullName = this.GetFullName('_');
                
                // create Prometheus-native-compatible collector
                // Will be used DEFINITELY for name lookup in native registry, and POSSIBLY as actual
                // collector itself if one does not already exist
                var c = new Collector<TNativeMetric>(fullName, Description, labelNames);

                PRO.Client.Collectors.ICollector retrieved_collector;

                // try/catch no longer specifically needed, just keeping around for debug 
                // convenience
                try
                {
                    retrieved_collector = registry.GetOrAdd(c);
                }
                catch(Exception e)
                {
                    throw;
                }

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
        /// Retrieve all metrics associated with this node, filtered by label
        /// 
        /// Won't work until some kind of metric recording has happened
        /// *through* our Node interface
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public IEnumerable<IMetric> GetMetrics(object labels)
        {
            // FIX: For now, just returning *ALL* metrics
            // We will want to filter this for real quite soon
            return Metrics;
        }

        T Helper<T>(PRO.Client.Contracts.Metric metric)
        {
            //new CounterMetric2(metric.counter, metric.label.Select(x => x.value).ToArray());
            return default(T);
        }

        public void Initialize(params string[] labelNames)
        {
            collector = new LabelNameOnlyCollector(labelNames);
        }


        /// <summary>
        /// Retrieve labels associated with this node and ALL metrics in this node
        /// NOTE: Some kind of setup of label names must have happened prior to this call
        /// </summary>
        public IEnumerable<string> Labels
        {
            get
            {
                if (collector == null) return Enumerable.Empty<string>();

                return collector.LabelNames;
            }
        }

        /// <summary>
        /// FIX: Does not return usable results at this time
        /// Metrics property won't work until some kind of metric recording has happened
        /// *through* our Node interface
        /// Reason for this is that labels need to be initialized before we can start querying these metrics
        /// </summary>
        public IEnumerable<IMetric> Metrics
        {
            get
            {
                if (collector == null) return Enumerable.Empty<IMetric>();

                var collected = collector.Collect();

                switch(collected.Type)
                {
                    case MetricType.Counter:
                    {
                        var counterCollector = (PRO.Client.Collectors.Collector<PRO.Client.Counter.ThisChild>)collector;
                        
                        // FIX: This compiles but yields basically a sparse enumeration,
                        // since Helper doesn't do anything
                        return collected.Metrics.Select(Helper<IMetric>);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Get or Add a metric with the provided label template & values,
        /// in native Prometheus.Client format
        /// </summary>
        /// <typeparam name="TNativeMetricChild"></typeparam>
        /// <param name="labelNames"></param>
        /// <returns></returns>
        TNativeMetricChild GetMetricNative<TNativeMetricChild>(
            IEnumerable<string> labelNames,
            IEnumerable<string> labelValues)
            where TNativeMetricChild: PRO.Client.Child, new()
        {
            var c = GetOrAdd<TNativeMetricChild>(labelNames.ToArray());

            // FIX: Moducom layer allows omission of labels, but Prometheus
            // layer does not, so this is going to break without additional
            // support logic.  Namely we have to un-sprase the labelValues
            // and stuff in blanks where Prometheus expects them
            // FIX: For some reason, Histogram breaks this
            var nativeMetricChild = c.Labels(labelValues.ToArray());

            return nativeMetricChild;
        }

        /// <summary>
        /// Pads incoming anon/dictionary label with spaces if prometheus labelling is a superset
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, object>> LabelHelper(object labels)
        {
            var labelEnum = Utility.LabelHelper(labels).ToArray();
            var labelNames = labelEnum.Select(x => x.Key);

            if (collector != null)
            {
                // any labelNames which DON'T appear in canonical collector are foreign labels
                var foreignLabels = labelNames.Except(collector.LabelNames);

                if (foreignLabels.Any())
                    throw new IndexOutOfRangeException(
                        $"Invalid label specified: {foreignLabels.ToString(",")}");
            }

            foreach (var labelName in collector.LabelNames)
            {
                var hasLabel = labelEnum.SingleOrDefault(x => x.Key == labelName);

                if (hasLabel.Key != null) // since KeyValuePair can't be compared to null
                    yield return hasLabel;
                else
                    yield return new KeyValuePair<string, object>(labelName, null);

            }
        }


        /// <summary>
        /// Retrieves metric in native Prometheus.Client format
        /// </summary>
        /// <typeparam name="TNativeMetricChild"></typeparam>
        /// <param name="labels"></param>
        /// <returns></returns>
        TNativeMetricChild GetMetricNative<TNativeMetricChild>(object labels)
            where TNativeMetricChild : PRO.Client.Child, new()
        {
            IEnumerable<KeyValuePair<string, object>> labelEnum;

            if (collector == null)
                labelEnum = Utility.LabelHelper(labels);
            else
                labelEnum = LabelHelper(labels).ToArray();

            var labelNames = labelEnum.Select(x => x.Key);
            var labelValues = labelEnum.Select(x => x.Value?.ToString());

            return GetMetricNative<TNativeMetricChild>(labelNames, labelValues);
        }

        /// <summary>
        /// Look up or create the metric in Moducom format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="labels"></param>
        /// <returns></returns>
        /// <remarks>Would be nice to liberate this to MetricFactory somehow</remarks>
        public T GetMetric<T>(object labels, object options = null) where T : 
            IValueGetter
        {
            if (typeof(T) == typeof(ICounter))
            {
                var nativeCounter = GetMetricNative<PRO.Client.Counter.ThisChild>(labels);

                var moducomCounter = new Counter(nativeCounter);

                return (T)(object)moducomCounter;
            }
            //else if (typeof(T).IsAssignableFrom(typeof(IGauge<double>)))
            else if (typeof(IGauge<double>).IsAssignableFrom(typeof(T)))
            {
                var nativeGauge = GetMetricNative<PRO.Client.Gauge.ThisChild>(labels);

                var moducomGauge = new Gauge(nativeGauge);

                return (T)(object)moducomGauge;
            }
            else if (typeof(IHistogram<double>).IsAssignableFrom(typeof(T)))
            {
                // FIX: May be something wrong here, might have to use PRO.Client.Histogram
                // itself
                var nativeHistogram = GetMetricNative<PRO.Client.Histogram.ThisChild>(labels);

                var moducomHistogram = new Histogram(nativeHistogram);

                return (T)(object)moducomHistogram;
            }
            throw new NotImplementedException();
        }

        INode IChildProvider<string, INode>.GetChild(string key) => base.GetChild(key);

        IEnumerable<INode> IChildProvider<INode>.Children => base.Children;
    }
}
