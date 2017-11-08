using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public struct LazyLoader<T> where T: class, new()
    {
        T value;

        public bool IsAllocated => value != default(T);

        public T Value
        {
            get
            {
                if (!IsAllocated) value = new T();

                return value;
            }
        }
    }
}
