using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Resonance.RPC
{
    internal interface IDynamicInterfaceImplementor
    {
        /// <summary>
        /// Create a type which implements the given interface and inherits from the given base type.
        /// Every call can be handled in the base class proper method.
        /// </summary>
        /// <param name="interfaceType">Interface to implement</param>
        /// <param name="dynamicProxyBaseType">Base class, which should inherit from <see cref="DynamicProxy"/></param>
        /// <returns>The new type</returns>
        Type CreateType(Type interfaceType, Type dynamicProxyBaseType);
    }
}
