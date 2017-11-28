using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public interface IRepository
    {
        INode RootNode { get; }

        INode this[string path] { get; }
    }

    namespace Experimental
    {
        internal class MetricNodeWrapperExperimental<T> : IMetricNode<T>
            where T : IMetricBase
        {
            readonly INode node;

            protected MetricNodeWrapperExperimental(INode node)
            {
                this.node = node;
            }

            public void AddMetric(IMetricBase metric)
            {
                node.AddMetric(metric);
            }

            public TMetric AddMetric<TMetric>(string key = null) where TMetric : IMetricBase
            {
                return node.AddMetric<TMetric>(key);
            }

            public IEnumerable<IMetricBase> GetMetrics(object labels = null)
            {
                return node.GetMetrics(labels);
            }

            /// <summary>
            /// FIX: labels could very well result in multiple matches rather than single one.  Unsure how to
            /// handle this right now
            /// </summary>
            /// <param name="labels"></param>
            /// <returns></returns>
            /// <remarks>
            /// Probably this Labels would act ass the Add metric too, if you request a non-present label
            /// probably we want to create it ad-hoc
            /// </remarks>
            public T Labels(object labels)
            {
                // FIX: Undefined behavior if *NOT* an ICounter, but probably need
                // to define the behavior
                var metrics = node.GetMetrics(labels).OfType<T>();

                // If no metrics match on the labels, then set out to create a new one
                if (!metrics.Any())
                {
                    // FIX: Likely get this from a factory
                    /*
                    ICounter counter = null;

                    node.AddMetric(counter); */
                    T counter = node.AddMetric<T>();

                    counter.SetLabels(labels);

                    return counter;
                }
                else
                    return metrics.Single();
            }
        }

        internal class CounterNodeWrapperExperimental : 
            MetricNodeWrapperExperimental<ICounter>, 
            ICounterNode
        {
            internal CounterNodeWrapperExperimental(INode node) : base(node) { }
        }
    }

    public static class IRepositoryExtensions
    {
        /// <summary>
        /// Get existing or new counter at the specified path with the specified labels
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="path"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        /// <remarks>
        /// FIX: Undefined behavior if labels match multiple, but shouldn't be undefined
        /// </remarks>
        public static ICounter GetCounterExperimental(this IRepository repository, string path, object labels = null)
        {
            try
            {
                INode node = repository[path];
                /*
                // get all counters which match the specified label
                var _counters = node.GetMetrics(labels).ToArray();
                var counters = _counters.OfType<ICounter>();

                // should only ever be one
                if (counters.Any()) return counters.Single();

                var counter = node.AddMetric<ICounter>();

                counter.SetLabels(labels);

                return counter; */

                return node.GetMetricExperimental<ICounter>(labels);
            }
            catch(Exception)
            {
                // FIX: Log instrumentation failure somehow
                return new Instrumentation.Experimental.NullCounter();
            }
        }

        public static Experimental.ICounterNode GetCounterNodeExperimental(this IRepository repository, string path)
        {
            return new Experimental.CounterNodeWrapperExperimental(repository[path]);
        }
    }
}
