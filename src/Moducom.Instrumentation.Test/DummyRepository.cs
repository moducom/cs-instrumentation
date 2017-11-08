using Moducom.Instrumentation.Abstract;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Moducom.Instrumentation.Test
{
    public class DummyRepository : IRepository
    {
        Node rootNode;

        public INode this[string path] => throw new NotImplementedException();

        public INode RootNode => rootNode;

        public class Node : INode
        {
            LazyLoader<Dictionary<string, Node>> children;
            LazyLoader<Dictionary<string, object>> labels;
            string name;

            public object Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IEnumerable<INode> Children
            {
                get
                {
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
                if (children.IsAllocated) return children.Value[name];

                throw new IndexOutOfRangeException();
            }

            public object GetLabelValue(string label)
            {
                throw new NotImplementedException();
            }

            public void SetLabels(object labels)
            {
                throw new NotImplementedException();
            }
        }
    }
}
