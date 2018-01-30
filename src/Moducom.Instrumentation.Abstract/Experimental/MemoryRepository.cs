﻿// Contracts, as usual, are confusing and seem flaky
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
    // Only excluded now due to lack of DBNull, however usage of DBNull is on its way out
#if !NETSTANDARD1_1
    /// <summary>
    /// Represents our own custom instrumentation repository, complete with our own Node structure
    /// Utilize this only for native in-memory instrumentation - does NOT specifically support SNMP, Prometheus, etc.
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
            public T CreateMetric<T>(string key) where T : IValueGetter
            {
                if (typeof(T) == typeof(ICounter))
                {
                    var counter = new Counter();

                    return (T)(object)counter;
                }
                else if (typeof(T) == typeof(IGauge))
                {
                    var retVal = new Gauge();

                    return (T)(IMetricWithLabels)retVal;
                }
                else if (typeof(T) == typeof(IHistogram<double>))
                {
                    var retVal = new Histogram();

                    return (T)(IMetricWithLabels)retVal;
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
                        var retVal = Activator.CreateInstance(typeToCreate);

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

        public class Node : 
            NamedChildCollection<INode>, 
            INamedChildCollection<Node>,
            INode,
            IChild<INode>,
            ILabelNamesProvider
        {
            /// <summary>
            /// Memory repository nodes specifically combine metric and their labels and track them
            /// together in one big list, per node
            /// </summary>
            LinkedList<IMetricWithLabels> metrics = new LinkedList<IMetricWithLabels>();
            readonly Node parent;

            public INode Parent => parent;

            public Node(Node parent, string name) : base(name) { this.parent = parent; }

            /// <summary>
            /// Aggregate all labels together.  In our memory repo, labels are pretty flexible so this can morph and change
            /// at runtime and order is not important
            /// </summary>
            public IEnumerable<string> Labels
            {
                get
                {
                    HashSet<string> labels = new HashSet<string>();

                    foreach(var metric in metrics)
                    {
                        labels.UnionWith(metric.Labels);
                    }

                    return labels;
                }
            }

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


            IEnumerable<IMetric> IMetricsProvider.GetMetrics(object labels)
            {
                return GetMetrics(labels);
            }

            /// <summary>
            /// Search for all values with the matching provided labels
            /// </summary>
            /// <param name="labels">Either an IDictionary or an anonymous object.  null value not permitted v</param>
            /// <returns></returns>
            public IEnumerable<IMetricWithLabels> GetMetrics(object labels)
            {
#if ENABLE_CONTRACTS
                Contract.Requires<ArgumentNullException>(labels != null, "labels");
#else
                if (labels == null) throw new ArgumentNullException("labels cannot be null");
#endif

                foreach(var value in metrics)
                {
                    bool isMatched = true;

                    foreach (var label in Utility.LabelHelper(labels))
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

            public IEnumerable<IMetric> Metrics => metrics;

            public void AddMetric(IMetricWithLabels metric)
            {
                metrics.AddLast(metric);
            }


            public T GetMetric<T>(object labels = null)
                where T: IValueGetter
            {
                IMetricWithLabels foundMetric;

                if (labels == null)
                    // Since null labels into GetMetrics retrieves *ALL* labels, filter further
                    // for a none label
                    foundMetric = metrics.SingleOrDefault(x => !x.Labels.Any());
                else
                    // FIX: One and only design decision one not fully fleshed out
                    foundMetric = GetMetrics(labels).SingleOrDefault();

                // FIX: Chances of a typecast exception seems high
                if (foundMetric != null) return (T)foundMetric;

                var metric = metricFactory.CreateMetric<T>(null);

                // FIX: nasty kludgy typecast AND would prefer to do an is
                var metricWithLabels = (ILabelsCollection)(object)metric;

                metricWithLabels.SetLabels(labels);

                // FIX: Eventually IMetricBase will only have providers not collections,
                // making this type cast safer
                AddMetric((IMetricWithLabels)metric);

                return metric;
            }

            void IChildCollection<Node>.AddChild(Node child) => base.AddChild(child);

            /// <summary>
            /// FIX: This is fragile, resolve the Node vs INode debacle
            /// </summary>
            IEnumerable<Node> IChildProvider<Node>.Children => base.Children.Cast<Node>();

            Node IChildProvider<string, Node>.GetChild(string name) => (Node)base.GetChild(name);
        }
    }

#endif

    public class MetricBase : 
        IMetricWithLabels,
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

            foreach (var label in Utility.LabelHelper(labels))
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

    public class NullMetric : IMetricWithLabels
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
