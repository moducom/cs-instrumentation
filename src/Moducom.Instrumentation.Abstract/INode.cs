using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    // NOTE: Change all "providers" because those lean towards read only
    // perhaps call them "repos"?  That implies more data storage than we're doing though
    namespace Experimental
    {
        public interface ILabelsProvider
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="label"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            bool GetLabelValue(string label, out object value);

            IEnumerable<string> Labels { get; }
        }


        public interface ILabelsCollection : ILabelsProvider
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="labels">Can be either an anonymous object or an IDictionary of string and object</param>
            void SetLabels(object labels);
        }

        public interface IWithChildren : Instrumentation.Experimental.IChildCollection<INode> { }

        /// <summary>
        /// TODO: Use Fact.Extensions version of this
        /// </summary>
        public interface INamed
        {
            string Name { get; }
        }


        public interface IMetricFactory
        {
            /// <summary>
            /// Create a metric conforming to interface specified by T
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <param name="labels"></param>
            /// <returns></returns>
            T CreateMetric<T>(string key, object labels = null) 
                where T : ILabelsProvider, IValueGetter;
        }


        /// <summary>
        /// NEW and unused, shall be a semi-wrapper around IMetricFactory so that metric factory can focus
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
            /// <returns></returns>
            T GetMetric<T>(object labels = null)
                where T : ILabelsProvider, IValueGetter;
        }


        /// <summary>
        /// Can supply and modify metric contents
        /// </summary>
        public interface IMetricsProvider
        {
            /// <summary>
            /// Retrieve all metrics associated with this node, filtered by labels
            /// </summary>
            /// <param name="labels">Labels to filter by.  Must not be null</param>
            /// <returns></returns>
            IEnumerable<IMetricBase> GetMetrics(object labels);

            /// <summary>
            /// All metrics for this node, unfiltered
            /// </summary>
            IEnumerable<IMetricBase> Metrics { get; }
        }

        /// <summary>
        /// TODO: Phase this out as an "always available" thing
        /// </summary>
        public interface IMetricsCollection : IMetricsProvider
        {
            void AddMetric(IMetricBase metric);
        }
    }

    /// <summary>
    /// Can have child nodes, can maintain a list of metrics within it
    /// and is named
    /// </summary>
    public interface INode :
        Experimental.IWithChildren,
        Experimental.IMetricProvider,
        Experimental.IMetricsProvider,
        Experimental.INamed
    {
    }


    /// <summary>
    /// Mainly amounts to something that can interact directly with labels
    /// </summary>
    public interface IMetricBase :
        IValueGetter,
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
            where T : IMetricBase
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
        public static IMetricBase GetMetric(this Experimental.IMetricsProvider metricsProvider, object labels = null)
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
        /// <returns></returns>
        public static IMetric<T> GetGenericMetric<T>(this INode node, object labels = null)
        {
            var metric = node.GetMetric<IMetric<T>>(labels);

            return metric;
        }
    }


    public static class IChildProviderExtensions
    {
        /// <summary>
        /// Stock standard tree traversal
        /// </summary>
        /// <param name="startNode">top of tree to search from.  MUST be convertible to type T directly</param>
        /// <param name="splitPaths">broken out path components</param>
        /// <param name="nodeFactory"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T FindChildByPath<T>(this Instrumentation.Experimental.IChildProvider<T> startNode, IEnumerable<string> splitPaths, 
            Func<T, string, T> nodeFactory = null)
            where T: Experimental.INamed
        {
            Instrumentation.Experimental.IChildProvider<T> currentNode = startNode;

            // The ChildProvider must also be a type of T for this to work
            T node = (T)currentNode;

            foreach (var name in splitPaths)
            {
                // We may encounter some nodes which are not child provider nodes
                if (currentNode == null) continue;

                node = currentNode.GetChild(name);

                if (node == null)
                {
                    // If no way to create a new node, then we basically abort (node not found)
                    if (nodeFactory == null) return default(T);

                    if (currentNode is Instrumentation.Experimental.IChildCollection<T> currentWritableNode)
                    {
                        // TODO: have a configuration flag to determine auto add
                        node = nodeFactory(node, name);
                        currentWritableNode.AddChild(node);
                    }
                    else
                        return default(T);
                }

                currentNode = node as Instrumentation.Experimental.IChildProvider<T>;
            }

            return node;
        }
    }
}
