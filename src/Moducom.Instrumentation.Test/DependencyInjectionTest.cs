using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Test
{
    [TestClass]
    public class DependencyInjectionTest
    {
        [TestMethod]
        public void SetupDITest()
        {
            var sc = new ServiceCollection();

            sc.AddPrometheus("unit test");

            var sp = sc.BuildServiceProvider();

            var repo = sp.GetService<IRepository>();

            Assert.IsInstanceOfType(repo, typeof(Prometheus.Repository));

            var r = (Prometheus.Repository)repo;

            Assert.AreEqual("unit test", repo.RootNode.Name);
        }
    }
}
