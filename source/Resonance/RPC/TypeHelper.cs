using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Resonance.RPC
{
    internal static class TypeHelper
    {
        private static Dictionary<string, object> _localCacheGetMethodInfoCustomAttributes = new Dictionary<string,object>();
        private static Dictionary<string, object> localCacheGetMethodInfoCustomAttributes
        {
            get
            {
                return _localCacheGetMethodInfoCustomAttributes;
            }
        }

        private static Dictionary<string, object> _localCacheIsSubclassOf = new Dictionary<string, object>();
        private static Dictionary<string, object> localCacheIsSubclassOf
        {
            get
            {
                return _localCacheIsSubclassOf;
            }
        }

        private static Dictionary<string, object> _localCacheIsImplementingInterface = new Dictionary<string, object>();
        private static Dictionary<string, object> localCacheIsImplementingInterface
        {
            get
            {
                return _localCacheIsImplementingInterface;
            }
        }

        private static Dictionary<string, object> _localCacheGetInterfaces = new Dictionary<string, object>();
        private static Dictionary<string, object> localCacheGetInterfaces
        {
            get
            {
                return _localCacheGetInterfaces;
            }
        }

        private static Dictionary<string, Tuple<bool, EventInfo>> _localCacheGetEventInfo = new Dictionary<string, Tuple<bool, EventInfo>>();
        private static Dictionary<string, Tuple<bool, EventInfo>> localCacheGetEventInfo
        {
            get
            {
                return _localCacheGetEventInfo;
            }
        }

        private static Dictionary<string, Tuple<bool, EventInfo>> _localCacheHasEvent = new Dictionary<string, Tuple<bool, EventInfo>>();
        private static Dictionary<string, Tuple<bool, EventInfo>> localCacheHasEvent
        {
            get
            {
                return _localCacheHasEvent;
            }
        }

        private static Dictionary<string, Tuple<bool, MethodInfo>> _localCacheGetMethodInfo = new Dictionary<string, Tuple<bool, MethodInfo>>();
        private static Dictionary<string, Tuple<bool, MethodInfo>> localCacheGetMethodInfo
        {
            get
            {
                return _localCacheGetMethodInfo;
            }
        }
        private static Dictionary<string, Tuple<bool, PropertyInfo>> _localCacheGetPropertyInfo = new Dictionary<string, Tuple<bool, PropertyInfo>>();
        private static Dictionary<string, Tuple<bool, PropertyInfo>> localCacheGetPropertyInfo
        {
            get
            {
                return _localCacheGetPropertyInfo;
            }
        }
        

        public static bool IsImplementingInterface(Type toCheck, Type interfaceType)
        {
            if (toCheck == null)
            {
                throw new ArgumentNullException("toCheck");
            }
            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }

            bool ret = false;

            string cacheKey = String.Concat(toCheck.FullName, "+", interfaceType.FullName);
            object c = null;
            localCacheIsImplementingInterface.TryGetValue(cacheKey, out c);
            if (c == null)
            {
                if (toCheck == interfaceType)
                {
                    ret = true;
                }
                else
                {
                    if (interfaceType.IsGenericTypeDefinition)
                    {
                        if (toCheck.Assembly.ReflectionOnly || interfaceType.Assembly.ReflectionOnly)
                        {
                            ret = toCheck.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition().AssemblyQualifiedName == interfaceType.AssemblyQualifiedName);
                        }
                        else
                        {
                            ret = toCheck.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType);
                        }
                    }
                    else
                    {
                        if (toCheck.Assembly.ReflectionOnly || interfaceType.Assembly.ReflectionOnly)
                        {
                            ret = toCheck.GetInterfaces().Any(x => x.AssemblyQualifiedName == interfaceType.AssemblyQualifiedName);
                        }
                        else
                        {
                            ret = toCheck.GetInterfaces().Any(x => x == interfaceType);
                        }
                    }
                }

                localCacheIsImplementingInterface.Add(cacheKey, ret);
            }
            else
            {
                ret = (bool)c;
            }

            return ret;
        }

        public static bool IsSubclassOf(Type toCheck, Type baseType)
        {
            if (toCheck == null)
            {
                throw new ArgumentNullException("toCheck");
            }
            if (baseType == null)
            {
                throw new ArgumentNullException("baseType");
            }

            bool ret = false;

            string cacheKey = String.Concat(toCheck.FullName, "+", baseType.FullName);
            object c = null;
            localCacheIsSubclassOf.TryGetValue(cacheKey, out c);
            if (c == null)
            {
                if (toCheck.IsSubclassOf(baseType))
                {
                    ret = true;
                }
                else
                {
                    if (toCheck.Assembly.ReflectionOnly || baseType.Assembly.ReflectionOnly)
                    {
                        while (toCheck.AssemblyQualifiedName != typeof(object).AssemblyQualifiedName && toCheck != null)
                        {
                            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                            if (baseType.AssemblyQualifiedName == cur.AssemblyQualifiedName)
                            {
                                ret = true;
                                break;
                            }
                            toCheck = toCheck.BaseType;
                        }
                    }
                    else
                    {
                        while (toCheck != typeof(object) && toCheck != null)
                        {
                            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                            if (baseType == cur)
                            {
                                ret = true;
                                break;
                            }
                            toCheck = toCheck.BaseType;
                        }
                    }
                }

                localCacheIsSubclassOf.Add(cacheKey, ret);
            }
            else
            {
                ret = (bool)c;
            }

            return ret;
        }

        public static EventInfo GetEventInfo(Type type, string eventName)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (String.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            EventInfo ret = null;

            string cacheKey = String.Concat(type.FullName, "+", eventName);
            Tuple<bool, EventInfo> item = null;
            localCacheGetEventInfo.TryGetValue(cacheKey, out item);

            if (item == null)
            {
                ret = GetEventInfoInInterfaces(type, eventName);
                item = new Tuple<bool, EventInfo>((ret != null), ret);
                localCacheGetEventInfo.Add(cacheKey, item);
            }
            else
            {
                ret = item.Item2;
            }

            return ret;
        }

        private static void GetEventInfoInInterfaces(Type type, List<EventInfo> events)
        {
            var localEvents = type.GetEvents();
            foreach (var item in localEvents)
            {
                if (!events.Contains(item))
                {
                    events.Add(item);
                }
            }

            foreach (var t in type.GetInterfaces())
            {
                GetEventInfoInInterfaces(t, events);
            }
        }



        private static EventInfo GetEventInfoInInterfaces(Type type, string eventName)
        {
            EventInfo ei = type.GetEvent(eventName);
            if (ei != null)
            {
                return ei;
            }

            foreach (var t in type.GetInterfaces())
            {
                EventInfo iei = GetEventInfoInInterfaces(t, eventName);
                if (iei != null)
                {
                    return iei;
                }

            }
            return null;
        }


        public static bool AreMatchingTypes(Type typeToCheck, Type baseTypeOrImplementedInterface)
        {
            bool ret = true;

            if (!TypeHelper.IsSubclassOf(typeToCheck, baseTypeOrImplementedInterface) &&
                !TypeHelper.IsImplementingInterface(typeToCheck, baseTypeOrImplementedInterface))
            {
                ret = false;
            }

            return ret;
        }



        public static bool HasEvent(Type type, string eventName)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (String.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            bool ret;
            string cacheKey = String.Concat(type.FullName, "+", eventName);
            Tuple<bool, EventInfo> item = null;
            localCacheHasEvent.TryGetValue(cacheKey, out item);

            if (item == null)
            {
                EventInfo ei = GetEventInfoInInterfaces(type, eventName);
                ret = ei != null;
                item = new Tuple<bool, EventInfo>(ret, ei);
                localCacheHasEvent.Add(cacheKey, item);
            }
            else
            {
                ret = item.Item1;
            }
            return ret;
        }


        public static MethodInfo GetMethodInfo(Type declaringType, string methodName, object[] arguments, BindingFlags bindingFlags)
        {
            if (declaringType == null)
            {
                throw new ArgumentNullException("declaringType");
            }
            if (String.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException("methodName");
            }

            return GetMethodInfoInternal(declaringType, methodName, arguments, bindingFlags);
        }

        public static MethodInfo GetMethodInfo(Type declaringType, string methodName, object[] arguments)
        {
            if (declaringType == null)
            {
                throw new ArgumentNullException("declaringType");
            }
            if (String.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException("methodName");
            }

            return GetMethodInfo(declaringType, methodName, arguments, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        }

        public static MethodInfo GetMethodInfo(Type declaringType, string methodName, Type[] argumentTypes)
        {
            if (declaringType == null)
            {
                throw new ArgumentNullException("declaringType");
            }
            if (String.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException("methodName");
            }

            return GetMethodInfoInternal(declaringType, methodName, argumentTypes, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        }

        private static MethodInfo GetMethodInfoInternal(Type declaringType, string methodName, object[] arguments, BindingFlags bindingFlags)
        {
            MethodInfo ret = null;

            var parameterTypes = arguments.Select(i => (i != null) ? i.GetType() : null).ToArray();
            ret = GetMethodInfoInternal(declaringType, methodName, parameterTypes, bindingFlags);

            return ret;
        }

        private static MethodInfo GetMethodInfoInternal(Type declaringType, string methodName, Type[] parameterTypes, BindingFlags bindingFlags)
        {
            MethodInfo ret = null;

            var parameterTypesKeyPart = (parameterTypes == null) ? new string[] { String.Empty } : parameterTypes.Select(p => (p != null) ? p.ToString() : "null").ToArray();
            string cacheKey = String.Concat(declaringType.FullName, "+", methodName, String.Join(":", parameterTypesKeyPart));

            Tuple<bool, MethodInfo> item = null;
            localCacheGetMethodInfo.TryGetValue(cacheKey, out item);

            if (item == null)
            {
                foreach (var mi in declaringType.GetMethods(bindingFlags).Where(m => m.Name == methodName))
                {
                    bool isMatchingMethodInfo = true;

                    if (parameterTypes != null)
                    {
                        var declaredParameters = mi.GetParameters();
                        if (declaredParameters.Length == parameterTypes.Length)
                        {
                            for (int i = 0; i < parameterTypes.Length; i++)
                            {
                                var methodParameterType = parameterTypes[i];
                                if (methodParameterType != null)
                                {

                                    var declaredParameterType = declaredParameters[i].ParameterType;
                                    if (declaredParameterType.IsGenericParameter)
                                    {
                                        if ((declaredParameterType.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask) != GenericParameterAttributes.None)
                                        {
                                            // TODO: implement class, new(), etc. constraints
                                            isMatchingMethodInfo = false;
                                        }

                                        foreach (var constraintType in declaredParameterType.GetGenericParameterConstraints())
                                        {
                                            if (!AreMatchingTypes(methodParameterType, constraintType))
                                            {
                                                isMatchingMethodInfo = false;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (declaredParameterType.IsGenericType)
                                        {
                                            declaredParameterType = declaredParameterType.GetGenericTypeDefinition();
                                        }

                                        if (!AreMatchingTypes(methodParameterType, declaredParameterType))
                                        {
                                            isMatchingMethodInfo = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            isMatchingMethodInfo = false;
                        }
                    }

                    if (isMatchingMethodInfo)
                    {
                        if (ret == null)
                        {
                            ret = mi;
                        }
                        else
                        {
                            throw new AmbiguousMatchException(String.Format("At least between {0} and {1}", ret, mi)); //LOCSTR
                        }
                    }

                }

                item = new Tuple<bool, MethodInfo>((ret != null), ret);
                localCacheGetMethodInfo.Add(cacheKey, item);
            }
            else
            {
                ret = item.Item2;
            }

            if (ret == null)
            {
                // fallback to type's interfaces
                foreach (var iface in declaringType.GetInterfaces())
                {
                    ret = GetMethodInfoInternal(iface, methodName, parameterTypes, bindingFlags);
                    if (ret != null)
                    {
                        break;
                    }
                }
            }

            return ret;
        }

        public static PropertyInfo GetPropertyInfo(object o, Type explicitInterfaceType, string propertyName)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            PropertyInfo ret = null;

            ret = GetPropertyInfo(o, explicitInterfaceType, propertyName, null);

            return ret;
        }

        public static PropertyInfo GetPropertyInfo(object o, Type explicitInterfaceType, string propertyName, Type propertyType)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            PropertyInfo ret = null;

            Type type = o.GetType();

            ret = GetPropertyInfo(type, explicitInterfaceType, propertyName, propertyType);

            return ret;
        }

        public static PropertyInfo GetPropertyInfo(Type declaringType, Type explicitInterfaceType, string propertyName)
        {
            PropertyInfo ret = GetPropertyInfo(declaringType, explicitInterfaceType, propertyName, null);
            return ret;
        }

        public static PropertyInfo GetPropertyInfo(Type declaringType, Type explicitInterfaceType, string propertyName, Type propertyType)
        {
            PropertyInfo ret = GetPropertyInfo(declaringType, explicitInterfaceType, propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy, propertyType);
            return ret;
        }

        public static PropertyInfo GetPropertyInfo(Type declaringType, Type explicitInterfaceType, string propertyName, BindingFlags bindingFlags)
        {
            PropertyInfo ret = GetPropertyInfo(declaringType, explicitInterfaceType, propertyName, bindingFlags, null);
            return ret;
        }

        public static PropertyInfo GetPropertyInfo(Type declaringType, Type explicitInterfaceType, string propertyName, BindingFlags bindingFlags, Type propertyType)
        {
            if (declaringType == null)
            {
                throw new ArgumentNullException("declaringType");
            }
            if (String.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException("propertyName");
            }

            PropertyInfo ret = null;

            string cacheKey = String.Concat(declaringType.FullName, explicitInterfaceType != null ? String.Concat("_", explicitInterfaceType.FullName) : String.Empty, "+", propertyName);

            Tuple<bool, PropertyInfo> item;
            localCacheGetPropertyInfo.TryGetValue(cacheKey, out item);

            if (item == null)
            {
                if (explicitInterfaceType != null)
                {
                    string interfaceTypeName = explicitInterfaceType.Name;
                    Type interfaceType = declaringType.GetInterface(interfaceTypeName);
                    if (interfaceType != null)
                    {
                        if (propertyType == null)
                        {
                            ret = interfaceType.GetProperty(propertyName, bindingFlags);
                        }
                        else
                        {
                            ret = interfaceType.GetProperty(propertyName, bindingFlags, null, propertyType, Type.EmptyTypes, null);
                        }
                    }
                    else
                    {
                        throw new ArgumentException(String.Format("Type '{0}' does not implement interface {1} - searched for by name: '{2}'!", declaringType, interfaceType, interfaceTypeName)); //LOCSTR
                    }

                }
                else
                {
                    if (propertyType == null)
                    {
                        ret = declaringType.GetProperty(propertyName, bindingFlags);
                    }
                    else
                    {
                        ret = declaringType.GetProperty(propertyName, bindingFlags, null, propertyType, Type.EmptyTypes, null);
                    }
                }
                item = new Tuple<bool, PropertyInfo>((ret != null), ret);
                localCacheGetPropertyInfo.Add(cacheKey, item);
            }
            else
            {
                ret = item.Item2;
            }

            return ret;
        }


    }
}
