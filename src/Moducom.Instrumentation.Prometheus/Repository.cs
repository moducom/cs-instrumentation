using Moducom.Instrumentation.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moducom.Instrumentation.Prometheus
{
    internal class Repository : IRepository
    {
        public INode this[string path] => throw new NotImplementedException();

        public INode RootNode => throw new NotImplementedException();
    }
}
