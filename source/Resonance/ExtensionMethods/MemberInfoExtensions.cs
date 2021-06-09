using Resonance.RPC;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Resonance.ExtensionMethods
{
    /// <summary>
    /// Contains <see cref="MemberInfo"/> extension methods.
    /// </summary>
    internal static class MemberInfoExtensions
    {
        /// <summary>
        /// Gets the RPC description of this member (DeclaringType.MemberName).
        /// </summary>
        /// <param name="member">The member.</param>
        public static String ToRpcDescription(this MemberInfo member)
        {
            return $"{member.DeclaringType.Name}.{member.Name}";
        }

        /// <summary>
        /// Gets the RPC attribute for this member or null if not provided.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns></returns>
        public static ResonanceRpcAttribute GetRpcAttribute(this MemberInfo member)
        {
            return member.GetCustomAttribute<ResonanceRpcAttribute>();
        }
    }
}
