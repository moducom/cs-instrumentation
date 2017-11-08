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
        Node rootNode = new Node("[root]");

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
