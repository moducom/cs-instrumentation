using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="labels">Can be either an anonymous object or an IDictionary of string and object</param>
            void SetLabels(object labels);
        }

        public interface IWithValue
        {
            object Value { get; set; }
        }

        /// <summary>
        /// TODO: move this to a non-instrumentation-specific place (maybe fact.extensions.collections)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface IChildProvider<T>
            where T: INamed
        {
            IEnumerable<T> Children { get; }

            T GetChild(string name);

            void AddChild(T child);
        }

        public interface IWithChildren : IChildProvider<INode> { }

        /// <summary>
        /// TODO: Use Fact.Extensions version of this
        /// </summary>
        public interface INamed
        {
            string Name { get; }
        }


        public interface IMetricsFactory
        {
            T CreateMetric<T>(string key = null) where T : IMetricBase;
        }


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

    public interface INode :
        Experimental.IWithChildren,
        Experimental.IMetricsProvider,
        Experimental.INamed
    {
    }

    public interface IMetricBase :
        Experimental.ILabelsProvider
    {

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
        public static T FindChildByPath<T>(this Experimental.IChildProvider<T> startNode, IEnumerable<string> splitPaths, 
            Func<string, T> nodeFactory)
            where T: class, Experimental.INamed
        {
            Experimental.IChildProvider<T> currentNode = startNode;

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

                currentNode = node as Experimental.IChildProvider<T>;
            }

            return node;
        }
    }
}
