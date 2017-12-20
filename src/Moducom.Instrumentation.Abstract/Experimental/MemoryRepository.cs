// Contracts, as usual, are confusing and seem flaky
//#define ENABLE_CONTRACTS

using Moducom.Instrumentation.Abstract;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Moducom.Instrumentation.Abstract.Experimental;
using Fact.Extensions.Collection;

#if ENABLE_CONTRACTS
using System.Diagnostics.Contracts;
#endif

namespace Moducom.Instrumentation.Experimental
{
    // TODO: Probably getting NETSTANDARD1_6 will be easy, but not important right now
    // TODO: NET40 gets excluded because TaxonomyBase is currently locked up in "Fact.Extensions.Experimental" which has no NET40 target
#if NET46 || NETSTANDARD2_0
    using Fact.Extensions.Experimental;
    /// <summary>
    /// 
    /// </summary>
    public class MemoryRepository : TaxonomyBase<MemoryRepository.Node, INode>, IRepository
    {
        readonly Node rootNode = new Node("[root]");

        class MetricFactory : IMetricFactory
        {
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
                else if (typeof(T) == typeof(IGauge))
                {
                    var retVal = new Gauge();

                    retVal.SetLabels(labels);

                    return (T)(IMetricBase)retVal;
                }
                else if (typeof(T) == typeof(IHistogram<double>))
                {
                    var retVal = new Histogram();

                    retVal.SetLabels(labels);

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

                        // FIX: This is a little bit fragile.  Metric<> itself does implement ILabelsCollection
                        // via MetricBase
                        var retVal = (ILabelsCollection) Activator.CreateInstance(typeToCreate);

                        retVal.SetLabels(labels);

                        return (T)retVal;
                    }

                    return default(T);
                }
                throw new NotImplementedException();
            }
        }

        static readonly MetricFactory metricFactory = new MetricFactory();

        protected override Node CreateNode(Node parent, string name) => new Node(name);

        public override Node RootNode => rootNode;

        /// <summary>
        /// Turn from either anonymous object or dictionary into a key/value label list
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        /// <remarks>TODO:Move this to a better location</remarks>
        public static IEnumerable<KeyValuePair<string, object>> LabelHelper(object labels)
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


        public class Node : 
            NodeBase<INode>, 
            IChildCollection<Node>,
            INode
        {
            LinkedList<IMetricBase> metrics = new LinkedList<IMetricBase>();

            public Node(string name) : base(name) { }

            event Action<object, Node> IChildCollection<Node>.ChildAdded
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            /// Search for all values with the matching provided labels
            /// </summary>
            /// <param name="labels">Either an IDictionary or an anonymous object.  null value not permitted v</param>
            /// <returns></returns>
            public IEnumerable<IMetricBase> GetMetrics(object labels)
            {
#if ENABLE_CONTRACTS
                Contract.Requires<ArgumentNullException>(labels != null, "labels");
#else
                if (labels == null) throw new ArgumentNullException("labels cannot be null");
#endif

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

            public IEnumerable<IMetricBase> Metrics => metrics;

            /// <summary>
            /// FIX: This is fragile, resolve the Node vs INode debacle
            /// </summary>
            IEnumerable<Node> IChildProvider<Node>.Children => base.Children.Cast<Node>();

            public void AddMetric(IMetricBase metric)
            {
                metrics.AddLast(metric);
            }


            public T GetMetric<T>(object labels = null)
                where T: ILabelsProvider, IValueGetter
            {
                IMetricBase foundMetric;

                if (labels == null)
                    // Since null labels into GetMetrics retrieves *ALL* labels, filter further
                    // for a none label
                    foundMetric = metrics.SingleOrDefault(x => !x.Labels.Any());
                else
                    // FIX: One and only design decision one not fully fleshed out
                    foundMetric = GetMetrics(labels).SingleOrDefault();

                // FIX: Chances of a typecast exception seems high
                if (foundMetric != null) return (T)foundMetric;

                var metric = metricFactory.CreateMetric<T>(null, labels);

                // FIX: Eventually IMetricBase will only have providers not collections,
                // making this type cast safer
                AddMetric((IMetricBase)metric);

                return metric;
            }

            void IChildCollection<Node>.AddChild(Node child) => base.AddChild(child);

            Node IChildCollection<Node>.GetChild(string name) => (Node)base.GetChild(name);
        }
    }

    public class MetricBase : 
        IMetricBase,
        ILabelsCollection
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

            foreach (var label in MemoryRepository.LabelHelper(labels))
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

        public IEnumerable<string> Labels => Enumerable.Empty<string>();
    }

    public class NullCounter : NullMetric, ICounter
    {
        public void Increment(double byAmount)
        {
        }

        public double Value => 0;
    }
}
