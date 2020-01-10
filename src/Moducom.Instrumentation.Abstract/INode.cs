using System;
using System.Collections.Generic;
using Fact.Extensions.Collection;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    // NOTE: Change all "providers" because those lean towards read only
    // perhaps call them "repos"?  That implies more data storage than we're doing though
    namespace Experimental
    {
        public interface ILabelValueProvider
        {
            /// <summary>
            /// Acquire, if we can, the value of a label
            /// </summary>
            /// <param name="label"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            bool GetLabelValue(string label, out object value);
        }

        public interface ILabelNamesProvider
        {
            IEnumerable<string> Labels { get; }
        }

        /// <summary>
        /// TODO: Phase out ILabelValueProvider as a necessary part of the hierarchy, as it is largely an internal API
        /// used by MemoryRepository, Unit tests and TextFileDump
        /// </summary>
        public interface ILabelsProvider : 
            ILabelValueProvider,
            ILabelNamesProvider
        {
        }


        public interface ILabelsCollection : ILabelsProvider
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="labels">Can be either an anonymous object or an IDictionary of string and object</param>
            void SetLabels(object labels);
        }

        public interface IMetricFactory<TKey>
        {
            /// <summary>
            /// Create a metric conforming to interface specified by T
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <param name="options"></param>
            /// <returns></returns>
            /// <remarks>
            /// TODO: Figure out a smooth way to pass in options also (very likely will
            /// have to be an object type
            /// </remarks>
            T CreateMetric<T>(TKey key = default(TKey), object options = null)
                where T : IValueGetter;
        }

        public interface IMetricFactory : IMetricFactory<string> { }

        /// <summary>
        /// Expected to act as a semi-wrapper around IMetricFactory so that metric factory can focus
        /// purely on creating new metrics
        /// </summary>
        public interface IMetricProvider
        {
            /// <summary>
            /// Create or acquire a metric conforming to interface specified by T
            /// If said metric conforming to key and labels has already been created before, 
            /// then pre-existing metric or an alias to it is returned
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="labels">Labels to match on, or null is looking for a metric with no labels</param>
            /// <param name="options">Currently supported are HistogramOptions and SummaryOptions</param>
            /// <returns></returns>
            T GetMetric<T>(object labels = null, object options = null)
                where T : IValueGetter;
        }


        /// <summary>
        /// Can supply and modify metric contents per node
        /// </summary>
        /// <remarks>
        /// Not all metrics tooling supports multiple different metric types per node
        /// </remarks>
        public interface IMetricsProvider
        {
            /// <summary>
            /// Retrieve all metrics associated with this node, filtered by labels
            /// </summary>
            /// <param name="labels">Labels to filter by.  Must not be null</param>
            /// <returns></returns>
            IEnumerable<IMetric> GetMetrics(object labels);

            /// <summary>
            /// All metrics for this node, unfiltered
            /// </summary>
            IEnumerable<IMetric> Metrics { get; }
        }
    }

    /// <summary>
    /// Can have child nodes, can maintain a list of metrics within it
    /// and is named
    /// </summary>
    public interface INode :
        INamedChildProvider<INode>,
        Experimental.IMetricProvider,
        Experimental.IMetricsProvider,
        INamed
    {
    }


    /// <summary>
    /// Mainly amounts to something that can interact directly with labels
    /// </summary>
    public interface IMetricWithLabels :
        IMetric,
        Experimental.ILabelsProvider
    {

    }

    namespace Experimental
    {
        /// <summary>
        /// Fuses node and metric TYPE together since we don't want
        /// different metric types in one node 
        /// </summary>
        public interface IMetricNode<T> : IMetricsProvider
            where T : IMetric
        {
            // Ala https://github.com/phnx47/Prometheus.Client
            T Labels(object labels);
        }

        /// <summary>
        /// Fuses node and metric counter TYPE together (but not counter INSTANCE)
        /// so is a semi-counter factory
        /// </summary>
        public interface ICounterNode : IMetricNode<ICounter> { }

        public interface IGaugeNode : IMetricNode<IGauge<double>> { }
    }


    public static class IMetricsProviderExtensions
    {
        public static IMetric GetMetric(this Experimental.IMetricsProvider metricsProvider, object labels = null)
        {
            return metricsProvider.GetMetrics(labels).Single();
        }
    }


    public static class INodeExtensions
    {
        /// <summary>
        /// Factory version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public static IMetric<T> GetGenericMetric<T>(this INode node, object labels = null)
        {
            var metric = node.GetMetric<IMetric<T>>(labels);

            return metric;
        }
    }
}
