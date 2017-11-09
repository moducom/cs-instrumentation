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

            var _value = _node.AddCounter();

            _value.SetLabels(new { instance = 1 });

            _node.GetValuesByLabels(new { instance = 1 }).ToArray();
        }

        [TestMethod]
        public void CounterMetricTest()
        {
            var repo = new DummyRepository();

            INode node = repo["counter/main"];

            var _node = ((DummyRepository.Node)node);

            var _value = _node.AddCounter();

            _value.SetLabels(new { instance = 1 });
            _value.Increment(1);
        }
    }
}
