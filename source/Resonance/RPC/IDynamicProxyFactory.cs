using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Resonance.RPC
{
    internal interface IDynamicProxyFactory
    {
        /// <summary>
        /// Creates an instance of a class which implements the given interface
        /// </summary>
        /// <typeparam name="TInterfaceType">Interface to be implemented</typeparam>
        /// <param name="constructorParameters">ctor parameters for creating the new instance</param>
        /// <returns>The new instance</returns>
        TInterfaceType CreateDynamicProxy<TInterfaceType>(params object[] constructorParameters);

        /// <summary>
        /// Creates an instance of a class which implements the given interface
        /// </summary>
        /// <param name="interfaceType">Interface to be implemented</param>
        /// <param name="constructorParameters">ctor parameters for creating the new instance</param>
        /// <returns>The new instance</returns>
        object CreateDynamicProxy(Type interfaceType, params object[] constructorParameters);
    }
}
