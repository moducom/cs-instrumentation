using Moducom.Instrumentation.Abstract;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Moducom.Instrumentation.Test
{
    public class DummyRepository : IRepository
    {
        readonly Node rootNode = new Node("[root]");

        public INode this[string path]
        {
            get
            {
                string[] splitPaths = path.Split('/');

                return RootNode.FindNodeByPath(splitPaths, name => new Node(name));
            }
        }

        public INode RootNode => rootNode;

        public class Node : INode
        {
            public class MetricBase : IMetricBase
            {
                SparseDictionary<string, object> labels;

                public IDictionary<string, object> Labels => labels;

                public object GetLabelValue(string label)
                {
                    throw new NotImplementedException();
                }

                public void SetLabels(object labels)
                {
                    this.labels.Clear();

                    // this doesnt work because Concat spits out a new enumeration of both
                    // which isn't exactly what we're after
                    //this.labels.Concat(LabelHelper(labels));

                    foreach (var label in LabelHelper(labels))
                        this.labels.Add(label);
                }
            }

            LinkedList<MetricBase> _values = new LinkedList<MetricBase>();

            LazyLoader<Dictionary<string, object>> labels;
            SparseDictionary<string, Node> children;
            readonly string name;

            public object Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IEnumerable<INode> Children => children.Values;

            public string Name => name;

            /// <summary>
            /// TODO: Very likely would prefer a null back if no child, not an exception
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public INode GetChild(string name)
            {
                children.TryGetValue(name, out Node value);
                return value;
            }

            public void AddChild(INode node)
            {
                children.Add(node.Name, (Node)node);
            }

            public object GetLabelValue(string label)
            {
                if (!labels.IsAllocated) return null;

                object retVal;

                labels.Value.TryGetValue(label, out retVal);

                return retVal;
            }

            /// <summary>
            /// Turn from either anonymous object or dictionary into a key/value label list
            /// </summary>
            /// <param name="labels"></param>
            /// <returns></returns>
            static IEnumerable<KeyValuePair<string, object>> LabelHelper(object labels)
            {
                if (labels is IDictionary<string, object> dictionaryLabels)
                    return dictionaryLabels;
                else
                    return from n in labels.GetType().GetProperties()
                           select KeyValuePair.Create(n.Name, n.GetValue(labels));
            }

            // Search for all values with the matching provided labels
            public IEnumerable<IMetricBase> GetValuesByLabels(object labels)
            {
                //var _labels = LabelHelper(labels);

                foreach(var value in _values)
                {
                    foreach (var label in LabelHelper(labels))
                    {
                        if(value.Labels.ContainsKey(label.Key))
                        {
                            // FIX: DbNull represents "wildcard" value and only match on key
                            // this is not super intuitive though, so find a better approach
                            if (label.Value == DBNull.Value)
                                yield return value;
                            else if (label.Value.Equals(value.Labels[label.Key]))
                                yield return value;
                        }
                    }
                }
            }


            internal class Counter : MetricBase, ICounter
            {
                double value = 0;

                public double Value => value;

                public void Decrement(double byAmount)
                {
                    value -= byAmount;
                }

                public void Increment(double byAmount)
                {
                    value += byAmount;
                }
            }


            internal class Metric<T> : MetricBase, IMetric<T>
            {
                public T Value => throw new NotImplementedException();
            }


            internal T AddMetric<T>()
                where T: MetricBase, new()
            {
                var metric = new T();

                _values.AddLast(metric);

                return metric;
            }

            public ICounter AddCounter()
            {
                return AddMetric<Counter>();
            }

            public void SetLabels(object labels)
            {
                // TODO: Use LabelHelper

                if (labels is IDictionary<string, object> dictionaryLabels)
                {
                    // Would be tempting to use this directly, but who knows what our
                    // caller later wants to do with labels, so copy it
                    // be aware that the item itself is gonna be a shallow copy
                    this.labels.Value = new Dictionary<string, object>(dictionaryLabels);
                }
                else
                {
                    // NOTE: Maybe some of the 'walker' stuff could be useful here
                    PropertyInfo[] properties = labels.GetType().GetProperties();

                    /*
                    this.labels.Value.Concat(properties.Select(
                        x => KeyValuePair.Create(x.Name, x.GetValue(labels)))); */

                    foreach (var property in properties)
                        this.labels.Value.Add(property.Name, property.GetValue(labels));
                }
            }

            public Node(string name)
            {
                this.name = name;
            }
        }
    }

    public class NullCounter : ICounter
    {
        public void Decrement(double byAmount)
        {
        }

        public object GetLabelValue(string label)
        {
            return null;
        }

        public void Increment(double byAmount)
        {
        }

        public void SetLabels(object labels)
        {
        }

        public double Value => 0;
    }
}
