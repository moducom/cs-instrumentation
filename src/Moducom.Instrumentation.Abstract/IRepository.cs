using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public interface IRepository
    {
        INode RootNode { get; }

        INode this[string path] { get; }
    }

    public static class IRepositoryExtensions
    {
    }
}
