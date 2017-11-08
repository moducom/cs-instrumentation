using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moducom.Instrumentation.Abstract;
using System.Linq;

namespace Moducom.Instrumentation.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var repo = new DummyRepository();

            INode node = repo["counter/main"];

            node.Children.ToArray();

            var _node = ((DummyRepository.Node)node);

            var _value = _node.AddValueInternal();

            _value.SetLabels(new { instance = 1 });

            _node.GetValuesByLabels(new { instance = 1 }).ToArray();
        }
    }
}
