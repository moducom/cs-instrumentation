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
        Node rootNode = new Node("[root]");

        public INode this[string path]
        {
            get
            {
                string[] splitPaths = path.Split('/');

                Node currentNode = rootNode;

                foreach(var name in splitPaths)
                {
                    var node = (Node)currentNode.GetChild(name);

                    if(node == null)
                    {
                        // TODO: have a configuration flag to determine auto add
                        node = new Node(name);
                        currentNode.AddChild(node);
                    }

                    currentNode = node;
                }

                return currentNode;
            }
        }

        public INode RootNode => rootNode;

        public class Node : INode
        {
            LazyLoader<Dictionary<string, Node>> children;
            LazyLoader<Dictionary<string, object>> labels;
            SparseDictionary<string, object> test;
            string name;

            public object Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IEnumerable<INode> Children
            {
                get
                {
                    test.ContainsKey("test");

                    if (children.IsAllocated) return children.Value.Values;

                    return Enumerable.Empty<INode>();
                }
            }

            public string Name => name;

            /// <summary>
            /// TODO: Very likely would prefer a null back if no child, not an exception
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public INode GetChild(string name)
            {
                if (children.IsAllocated)
                {
                    children.Value.TryGetValue(name, out Node value);
                    return value;
                }

                return null;
            }

            public void AddChild(INode node)
            {
                children.Value.Add(node.Name, (Node)node);
            }

            public object GetLabelValue(string label)
            {
                if (!labels.IsAllocated) return null;

                return labels.Value[label];
            }

            public void SetLabels(object labels)
            {
                if (labels is IDictionary<string, object> dictionaryLabels)
                {
                    // Would be tempting to use this directly, but who knows what our
                    // caller later wants to do with labels, so copy it
                    // be aware that the item itself is gonna be a shallow copy
                    this.labels.Value = new Dictionary<string, object>(dictionaryLabels);
                }
                else
                {
                    PropertyInfo[] properties = labels.GetType().GetProperties();

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
}
