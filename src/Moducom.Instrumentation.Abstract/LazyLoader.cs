using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public struct LazyLoader<T> where T: class, new()
    {
        internal T value;

        public bool IsAllocated => value != default(T);

        public T Value
        {
            get
            {
                if (!IsAllocated) value = new T();

                return value;
            }
            set
            {
                // We can just brute force it too... still counts as a lazy load, 
                // incase we want to construct things differently
                this.value = value;
            }
        }
    }
}
