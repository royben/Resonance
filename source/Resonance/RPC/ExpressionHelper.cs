using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Specialized;

namespace Resonance.RPC
{
    internal static class ExpressionHelper
    {
        public static MethodInfo GetMethodCallExpressionMethodInfo<T>(Expression<Action<T>> exp)
        {
            return GetMethodCallExpressionMethodInfo((Expression)exp);
        }

        public static MethodInfo GetMethodCallExpressionMethodInfo<T>(Expression<Func<T, object>> exp)
        {
            return GetMethodCallExpressionMethodInfo((Expression)exp);
        }

        public static MethodInfo GetMethodCallExpressionMethodInfo(Expression exp)
        {
            MethodInfo ret = null;

            if (exp == null)
            {
                throw new ArgumentNullException("exp");
            }

            MethodCallExpression mcexp = exp as MethodCallExpression;
            if (mcexp == null)
            {
                LambdaExpression lex = exp as LambdaExpression;
                if (lex == null)
                {
                    UnaryExpression uex = exp as UnaryExpression;
                    if ((uex == null) || (uex.NodeType != ExpressionType.Convert))
                    {
                        throw new InvalidOperationException("Neither a MethodCallExpression nor a LambdaExpression wrapped MethodCallExpression nor a ConvertExpression wrapped MethodCallExpression!");   //LOCSTR
                    }
                    else
                    {
                        ret = GetMethodCallExpressionMethodInfo(uex.Operand);
                    }
                }
                else
                {
                    ret = GetMethodCallExpressionMethodInfo(lex.Body);
                }
            }
            else
            {
                ret = mcexp.Method;
            }

            return ret;
        }
    }
}
