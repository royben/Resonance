using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Resonance.RPC
{
    internal class DynamicProxyFactory<TDynamicProxyType> : IDynamicProxyFactory where TDynamicProxyType : DynamicProxy
    {
        private IDynamicInterfaceImplementor interfaceImplementor = null;
        public DynamicProxyFactory(IDynamicInterfaceImplementor interfaceImplementor)
        {
            this.interfaceImplementor = interfaceImplementor;
        }

        public virtual TInterfaceType CreateDynamicProxy<TInterfaceType>(params object[] constructorParameters)
        {
            TInterfaceType ret;

            ret = (TInterfaceType)CreateDynamicProxy(typeof(TInterfaceType), constructorParameters);

            return ret;
        }

        public virtual object CreateDynamicProxy(Type interfaceType, params object[] constructorParameters)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }

            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("interfaceType must be an interface!"); //LOCSTR
            }

            object ret = null;

            Type t = interfaceImplementor.CreateType(interfaceType, typeof(TDynamicProxyType));
            ret = Activator.CreateInstance(t, constructorParameters);

            return ret;
        }

    }
}
