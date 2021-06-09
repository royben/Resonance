using Resonance.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.ExtensionMethods
{
    /// <summary>
    /// Contains <see cref="MethodInfo"/> extension methods.
    /// </summary>
    internal static class MethodInfoExtensions
    {
        /// <summary>
        /// Invokes the this method using Resonance RPC specifications.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="target">The target.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public static Object InvokeRPC(this MethodInfo methodInfo, Object target, object[] parameters)
        {
            if (methodInfo.GetParameters().Length == 0)
            {
                parameters = null;
            }

            if (methodInfo.ReturnType == typeof(Task))
            {
                var task = (Task)methodInfo.Invoke(target, parameters);
                task.GetAwaiter().GetResult();
                return null;
            }
            else if (methodInfo.ReturnType == typeof(void))
            {
                methodInfo.Invoke(target, parameters);
                return null;
            }
            else if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                var task = (Task)methodInfo.Invoke(target, parameters);
                task.GetAwaiter().GetResult();
                var prop = typeof(Task<>).MakeGenericType(methodInfo.ReturnType.GenericTypeArguments[0]).GetProperty("Result");
                return prop.GetValue(task);
            }
            else
            {
                return methodInfo.Invoke(target, parameters);
            }
        }

        /// <summary>
        /// Invokes the this method using Resonance RPC specifications.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="target">The target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns></returns>
        public static Object InvokeRPC(this MethodInfo methodInfo, Object target, object parameter)
        {
            if (parameter is MethodParamCollection collection)
            {
                return InvokeRPC(methodInfo, target, collection.ToArray());
            }
            else
            {
                return InvokeRPC(methodInfo, target, new object[] { parameter });
            }
        }
    }
}
