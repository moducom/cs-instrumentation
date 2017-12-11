using Moducom.Instrumentation.Abstract;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Moducom.Instrumentation.Abstract.Experimental;

namespace Moducom.Instrumentation.Experimental
{
    // TODO: Probably getting NETSTANDARD1_6 will be easy, but not important right now
#if NET40 || NET46 || NETSTANDARD2_0
    public class MemoryRepository : IRepository
    {
        readonly Node rootNode = new Node("[root]");

        public INode this[string path]
        {
            get
            {
                string[] splitPaths = path.Split('/');

                return RootNode.FindChildByPath(splitPaths, name => new Node(name));
            }
        }

        public INode RootNode => rootNode;

        public class Node : INode, Abstract.Experimental.IMetricFactory
        {
            LinkedList<IMetricBase> metrics = new LinkedList<IMetricBase>();

            SparseDictionary<string, INode> children;
            readonly string name;

            public IEnumerable<INode> Children => children.Values;

            public string Name => name;

            /// <summary>
            /// TODO: Very likely would prefer a null back if no child, not an exception
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public INode GetChild(string name)
            {
                children.TryGetValue(name, out INode value);
                return value;
            }

            public void AddChild(INode node) => children.Add(node.Name, node);

            /// <summary>
            /// Turn from either anonymous object or dictionary into a key/value label list
            /// </summary>
            /// <param name="labels"></param>
            /// <returns></returns>
            internal static IEnumerable<KeyValuePair<string, object>> LabelHelper(object labels)
            {
                if (labels == null) return Enumerable.Empty<KeyValuePair<string, object>>();

                if (labels is IDictionary<string, object> dictionaryLabels)
                    return dictionaryLabels;
                else
                    return from n in labels.GetType().GetProperties()
                           select new KeyValuePair<string, object>(n.Name, n.GetValue(labels, null));
                            // NOTE: This was working, but doesnt now.  Not sure what circumstances this is OK for
                           //select KeyValuePair.Create(n.Name, n.GetValue(labels, null));
            }

            /// <summary>
            /// Search for all values with the matching provided labels
            /// </summary>
            /// <param name="labels">Either an IDictionary or an anonymous object</param>
            /// <returns></returns>
            public IEnumerable<IMetricBase> GetMetrics(object labels)
            {
                //var _labels = LabelHelper(labels);

                if (labels == null)
                {
                    foreach (var value in metrics) yield return value;

                    yield break;
                }

                foreach(var value in metrics)
                {
                    bool isMatched = true;

                    foreach (var label in LabelHelper(labels))
                    {
                        if (value.GetLabelValue(label.Key, out object targetLabelValue))
                        {
                            // FIX: DbNull represents "wildcard" value and only match on key
                            // this is not super intuitive though, so find a better approach
                            if (label.Value == DBNull.Value) { }
                            else if (label.Value.Equals(targetLabelValue)) { }
                            else isMatched = false;
                        }
                        else isMatched = false;
                    }

                    if (isMatched) yield return value;
                }
            }


            public void AddMetric(IMetricBase metric)
            {
                metrics.AddLast(metric);
            }


            /// <summary>
            /// Interim factory method, to be replaced by IoC/DI
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <returns></returns>
            public T CreateMetric<T>(string key)
                where T : IMetricBase
            {
                if (typeof(T) == typeof(ICounter))
                {
                    var retVal = new Counter();

                    return (T)(IMetricBase)retVal;
                }
                else if (typeof(T) == typeof(IGauge))
                {
                    var retVal = new Gauge();

                    return (T)(IMetricBase)retVal;
                }
                else if (typeof(T) == typeof(IHistogram<double>))
                {
                    var retVal = new Histogram();

                    return (T)(IMetricBase)retVal;
                }
                else
                {
                    var t = typeof(T);
#if NET40
                    var underlyingValueType = t.GetGenericArguments().First();
#else
                    var underlyingValueType = t.GenericTypeArguments.First();
#endif

                    var genericType = t.GetGenericTypeDefinition();

                    if (genericType == typeof(IMetric<>))
                    {
                        var typeToCreate = typeof(Metric<>).MakeGenericType(underlyingValueType);

                        object retVal = Activator.CreateInstance(typeToCreate);

                        return (T)retVal;
                    }

                    return default(T);
                }
            }


            public T AddMetric<T>(string key)
                where T: IMetricBase
            {
                T metric = CreateMetric<T>(key);

                metrics.AddLast(metric);

                return metric;
            }

            /// <summary>
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <param name="labels"></param>
            /// <returns></returns>
            /// <remarks>IMetricFactory version</remarks>
            public T CreateMetric<T>(string key, object labels = null) where T : ILabelsProvider, IValueGetter
            {
                if (typeof(T) == typeof(ICounter))
                {
                    var counter = new Counter();

                    counter.SetLabels(labels);

                    return (T)(object)counter;
                }
                throw new NotImplementedException();
            }

            public Node(string name)
            {
                this.name = name;
            }
        }
    }

    public class MetricBase : IMetricBase
    {
        SparseDictionary<string, object> labels;

        public bool GetLabelValue(string label, out object value) =>
            labels.TryGetValue(label, out value);

        public void SetLabels(object labels)
        {
            this.labels.Clear();

            // this doesnt work because Concat spits out a new enumeration of both
            // which isn't exactly what we're after
            //this.labels.Concat(LabelHelper(labels));

            foreach (var label in MemoryRepository.Node.LabelHelper(labels))
                this.labels.Add(label);
        }

        public IEnumerable<string> Labels => labels.Keys;
    }


    public class Metric<T> : MetricBase, IMetric<T>
    {
        T value;

        // TODO: COnsider making this into PropertyChangedNotification
        public event Action<IMetric<T>> ValueChanged;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                ValueChanged?.Invoke(this);
            }
        }
    }


    internal class Counter : MetricBase, ICounter
    {
        protected double value = 0;

        public double Value => value;

        public event Action<ICounter> Incremented;

        public void Increment(double byAmount)
        {
            value += byAmount;
            Incremented?.Invoke(this);
        }
    }


    internal class Gauge : Counter, IGauge
    {
        public event Action<IGauge> Decremented;

        public void Decrement(double byAmount)
        {
            value -= byAmount;
            Decremented?.Invoke(this);
        }

        public new double Value
        {
            get => this.value;
            set { this.value = value; }
        }
    }


    internal class UptimeGauge : MetricBase, IGauge
    {
        // initialized at first "mention" of UptimeGauge
        static readonly DateTime Start = DateTime.Now;

        public void Increment(double byAmount) => throw new InvalidOperationException();
        public void Decrement(double byAmount) => throw new InvalidOperationException();

        public double Value
        {
            get => DateTime.Now.Subtract(Start).TotalMinutes;
            set => throw new InvalidOperationException();
        }
    }


    /// <summary>
    /// Needs more work binning/bucketing not worked out at all
    /// </summary>
    internal class Histogram : MetricBase, IHistogram<double>
    {
        // adapted from https://github.com/phnx47/Prometheus.Client/blob/master/src/Prometheus.Client/Histogram.cs
        readonly double[] bins;
        static readonly double[] defaultBins = { .005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10 };
        // NOTE: for binning across entire time spectrum only
        readonly ulong[] binCounts;

        internal class Item : IHistogramNode<double>
        {
            internal readonly DateTime timeStamp = DateTime.Now;
            internal double value;

            DateTime IHistogramNode<double>.TimeStamp => timeStamp;

            double IHistogramNode<double>.Value => value;
        }

        internal Histogram(double[] bins = null)
        {
            this.bins = bins ?? defaultBins;
            binCounts = new ulong[this.bins.Length];
        }

        /// <summary>
        /// 
        /// </summary>
        LinkedList<Item> items = new LinkedList<Item>();

        public double Value
        {
            set
            {
                var item = new Item
                {
                    value = value
                };
                items.AddLast(item);

                // FIX: Cheap and nasty auto prune, only hold on to 30 minutes data
                // We need to prune in more places and with more configurability
                while(DateTime.Now.Subtract(items.First.Value.timeStamp).TotalMinutes > 30)
                {
                    items.RemoveFirst();
                }
            }
        }

        public IEnumerable<IHistogramNode<double>> Values => items;
    }

#endif

    public class NullMetric : IMetricBase
    {
        public bool GetLabelValue(string label, out object value)
        {
            value = null;
            return false;
        }

        public void SetLabels(object labels)
        {
        }

        public IEnumerable<string> Labels => Enumerable.Empty<string>();
    }

    public class NullCounter : NullMetric, ICounter
    {
        public void Increment(double byAmount)
        {
        }

        public double Value => 0;
    }


#if !NETSTANDARD1_6
    public static class INodeExtensions
    {
        public static ICounter AddCounter(this INode node)
        {
            // TODO: We need to create these counters from a factory
            var counter = new Counter();
            node.AddMetric(counter);
            return counter;
        }


        /// <summary>
        /// NOTE: Probably won't translate well to prometheus.io
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IGauge AddUptimeGauge(this INode node)
        {
            var gauge = new UptimeGauge();
            node.AddMetric(gauge);
            //if (labels != null) gauge.SetLabels(labels);
            return gauge;
        }


        public static ICounter AddCounter(this INode node, object labels)
        {
            ICounter counter = node.AddCounter();

            counter.SetLabels(labels);

            return counter;
        }
    }
#endif
}
