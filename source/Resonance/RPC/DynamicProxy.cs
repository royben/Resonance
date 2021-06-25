using System;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Resonance.RPC
{
    public abstract class DynamicProxy : ResonanceObject, IDisposable
    {
        private static object dummyOut;
        public static MethodInfo TryGetMemberMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TryGetMember(null, null, out dummyOut));
        public static MethodInfo TrySetMemberMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TrySetMemberInternal(null, null, null));
        public static MethodInfo TryInvokeMemberMethodInfo = ExpressionHelper.GetMethodCallExpressionMethodInfo<DynamicProxy>(o => o.TryInvokeMember(null, null, null, out dummyOut));

        protected DynamicProxy()
        {
        }

        protected abstract bool TryInvokeMember(Type interfaceType, MethodInfo methodInfo, object[] args, out object result);

        protected abstract bool TrySetMember(Type interfaceType, string name, object value);

        protected abstract bool TryGetMember(Type interfaceType, string name, out object result);

        protected abstract bool TrySetEvent(Type interfaceType, string name, object value);

        protected bool TrySetMemberInternal(Type interfaceType, string name, object value)
        {
            bool ret;
            if (TypeHelper.HasEvent(interfaceType, name))
            {
                ret = TrySetEvent(interfaceType, name, value);
            }
            else
            {
                ret = TrySetMember(interfaceType, name, value);
            }
            return ret;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DynamicProxy()
        {
            Dispose(false);
        }
    }
}
