﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Fact.Extensions.Collection;

namespace Moducom.Instrumentation.Experimental
{
#if !NET40
    using Fact.Extensions.Experimental;

    public abstract class TaxonomyBase<TNode, TINode> : TaxonomyBase<TNode>, ITaxonomy<TINode>
        where TNode: INamedChildProvider<TNode>, TINode
        where TINode: INamedChildProvider<TINode>, INamed
    {
        TINode IAccessor<string, TINode>.this[string key] => base[key];

        TINode ITaxonomy<TINode>.RootNode => RootNode;
    }
#endif
}
