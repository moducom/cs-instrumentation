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

        /// <summary>
        /// TODO: move this to a non-instrumentation-specific place (maybe fact.extensions.collections)
        /// TODO: removed INamed requirement (AddChild will need to take name as part of parameter)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface IChildProvider<T>
            where T: INamed
        {
            IEnumerable<T> Children { get; }

            T GetChild(string name);
        }


        public interface IChildCollection<T> : IChildProvider<T>
            where T: INamed
        {
            void AddChild(T child);
        }

        public interface IWithChildren : IChildCollection<INode> { }

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
            /// If said metric conforming to key and labels has already been created before, 
            /// then pre-existing metric or an alias to it is returned
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <param name="labels"></param>
            /// <returns></returns>
            T CreateMetric<T>(string key, object labels = null) 
                where T : ILabelsProvider, IValueGetter;
        }


        /// <summary>
        /// Can supply and modify metric contents
        /// </summary>
        public interface IMetricsProvider
        {
            void AddMetric(IMetricBase metric);

            /// <summary>
            /// Experimental factory version
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            T AddMetric<T>(string key = null) where T : IMetricBase;

            /// <summary>
            /// Retrieve all metrics associated with this node, filtered by labels
            /// </summary>
            /// <param name="labels"></param>
            /// <returns></returns>
            IEnumerable<IMetricBase> GetMetrics(object labels = null);
        }
    }

    /// <summary>
    /// Can have child nodes, can maintain a list of metrics within it
    /// and is named
    /// </summary>
    public interface INode :
        Experimental.IWithChildren,
        Experimental.IMetricsProvider,
        Experimental.INamed
    {
    }


    /// <summary>
    /// Mainly amounts to something that can interact directly with labels
    /// </summary>
    public interface IMetricBase :
        Experimental.ILabelsProvider,
        Experimental.ILabelsCollection
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
        public static IMetric<T> AddMetricExperimental<T>(this INode node, object labels = null)
        {
            var metric = node.AddMetric<IMetric<T>>();

            if (labels != null) metric.SetLabels(labels);

            return metric;
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public static T GetMetricExperimental<T>(this INode node, object labels = null)
            where T: IMetricBase
        {
            var _metrics = node.GetMetrics(labels).ToArray();
            var metrics = _metrics.OfType<T>();

            // should only ever be one
            if (metrics.Any()) return metrics.Single();

            var metric = node.AddMetric<T>();

            metric.SetLabels(labels);

            return metric;
        }

        /// <summary>
        /// Factory version
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static ICounter AddCounterExperimental(this INode node, object labels = null)
        {
            var metric = node.AddMetric<ICounter>();

            if (labels != null) metric.SetLabels(labels);

            return metric;
        }


        public static ICounter CreateCounterExperimental(this Experimental.IMetricFactory factory)
        {
            return factory.CreateMetric<ICounter>(null);
        }
    }


    public static class IMetricNodeExtensions
    {
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
        public static T FindChildByPath<T>(this Experimental.IChildCollection<T> startNode, IEnumerable<string> splitPaths, 
            Func<string, T> nodeFactory)
            where T: class, Experimental.INamed
        {
            Experimental.IChildCollection<T> currentNode = startNode;

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
                    if (nodeFactory == null) return null;

                    // TODO: have a configuration flag to determine auto add
                    node = nodeFactory(name);
                    currentNode.AddChild(node);
                }

                currentNode = node as Experimental.IChildCollection<T>;
            }

            return node;
        }
    }
}
