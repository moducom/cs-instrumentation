﻿using Moducom.Instrumentation.Abstract;
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
                if (labels is IDictionary<string, object> dictionaryLabels)
                    return dictionaryLabels;
                else
                    return from n in labels.GetType().GetProperties()
                           select KeyValuePair.Create(n.Name, n.GetValue(labels));
            }

            /// <summary>
            /// Search for all values with the matching provided labels
            /// </summary>
            /// <param name="labels">Either an IDictionary or an anonymous object</param>
            /// <returns></returns>
            public IEnumerable<IMetricBase> GetMetrics(object labels)
            {
                //var _labels = LabelHelper(labels);

                foreach(var value in metrics)
                {
                    foreach (var label in LabelHelper(labels))
                    {
                        if(value.GetLabelValue(label.Key, out object targetLabelValue))
                        {
                            // FIX: DbNull represents "wildcard" value and only match on key
                            // this is not super intuitive though, so find a better approach
                            if (label.Value == DBNull.Value)
                                yield return value;
                            else if (label.Value.Equals(targetLabelValue))
                                yield return value;
                        }
                    }
                }
            }


            public void AddMetric(IMetricBase metric)
            {
                metrics.AddLast(metric);
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

            foreach (var label in DummyRepository.Node.LabelHelper(labels))
                this.labels.Add(label);
        }
    }


    public class Metric<T> : MetricBase, IMetric<T>
    {
        public T Value { get; set; }
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
    }

    public class NullCounter : NullMetric, ICounter
    {
        public void Decrement(double byAmount)
        {
        }

        public void Increment(double byAmount)
        {
        }

        public double Value => 0;
    }


    public static class INodeExtensions
    {
        public static ICounter AddCounter(this INode node)
        {
            var counter = new Counter();
            node.AddMetric(counter);
            return counter;
        }


        public static ICounter AddCounter(this INode node, object labels)
        {
            ICounter counter = node.AddCounter();

            counter.SetLabels(labels);

            return counter;
        }
    }
}
