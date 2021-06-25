using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;

namespace Resonance.RPC
{
    internal class DynamicInterfaceImplementor : IDynamicInterfaceImplementor
    {
        public DynamicInterfaceImplementor()
        {
            InitTypeBuilder();
        }

        private ModuleBuilder moduleBuilder = null;

        private void InitTypeBuilder()
        {
            tryGetMemberMethodInfo = DynamicProxy.TryGetMemberMethodInfo;
            trySetMemberMethodInfo = DynamicProxy.TrySetMemberMethodInfo;
            tryInvokeMemberInfo = DynamicProxy.TryInvokeMemberMethodInfo;

            Type ownClass = typeof(DynamicInterfaceImplementor);
            string guid = Guid.NewGuid().ToString();
            AssemblyName assemblyName = new AssemblyName(string.Concat(ownClass.Namespace, ".", ownClass.Name, "_", guid));

            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = ab.DefineDynamicModule(assemblyName.Name);

            listAddMethodInfo = TypeHelper.GetMethodInfo(typeof(List<object>), listAddMethodName, new Type[] { typeof(object) });
            listToArrayMethodInfo = TypeHelper.GetMethodInfo(typeof(List<object>), listToArrayMethodName, Type.EmptyTypes);
            
            getTypeFromHandleMethodInfo = TypeHelper.GetMethodInfo(typeof(Type), getTypeFromHandleMethodName, new Type[] { typeof(RuntimeTypeHandle) });
            delegateCombineMethodInfo = TypeHelper.GetMethodInfo(typeof(Delegate), delegateCombineMethodName, new Type[] { typeof(Delegate), typeof(Delegate) });
            delegateRemoveMethodInfo = TypeHelper.GetMethodInfo(typeof(Delegate), delegateRemoveMethodName, new Type[] { typeof(Delegate), typeof(Delegate) });
            activatorCreateInstanceMethodInfo = TypeHelper.GetMethodInfo(typeof(Activator), createInstanceMethodName, new Type[] { typeof(Type) });
        }

        private Dictionary<string, Type> dynamicTypes = new Dictionary<string, Type>();
        private SpinLock dynamicTypeEmitSyncRoot = new SpinLock();

        private string ownClassName = typeof(DynamicInterfaceImplementor).Name;
        private string getTypeFromHandleMethodName = ExpressionHelper.GetMethodCallExpressionMethodInfo<RuntimeTypeHandle>(t => Type.GetTypeFromHandle(t)).Name;
        private string listAddMethodName = ExpressionHelper.GetMethodCallExpressionMethodInfo<List<object>>(l => l.Add(new object())).Name;
        private string listToArrayMethodName = ExpressionHelper.GetMethodCallExpressionMethodInfo<List<object>>(l => l.ToArray()).Name;
        private string createInstanceMethodName = ExpressionHelper.GetMethodCallExpressionMethodInfo<object>(a => Activator.CreateInstance(a.GetType())).Name;
        private string delegateCombineMethodName = ExpressionHelper.GetMethodCallExpressionMethodInfo<Delegate>(d => Delegate.Combine(d, d)).Name;
        private string delegateRemoveMethodName = ExpressionHelper.GetMethodCallExpressionMethodInfo<Delegate>(d => Delegate.Remove(d, d)).Name;

        private MethodInfo listAddMethodInfo = null;
        private MethodInfo listToArrayMethodInfo = null;
        private MethodInfo getTypeFromHandleMethodInfo = null;
        private MethodInfo getMethodMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
        private MethodInfo delegateCombineMethodInfo = null;
        private MethodInfo delegateRemoveMethodInfo = null;
        private MethodInfo activatorCreateInstanceMethodInfo = null;

        private MethodInfo tryGetMemberMethodInfo = null;
        private MethodInfo trySetMemberMethodInfo = null;
        private MethodInfo tryInvokeMemberInfo = null;

        public virtual Type CreateType(Type interfaceType, Type dynamicProxyBaseType)
        {
            Type ret;

            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }
            if (dynamicProxyBaseType == null)
            {
                throw new ArgumentNullException("dynamicProxyBaseType");
            }
            if (!TypeHelper.IsSubclassOf(dynamicProxyBaseType, typeof(DynamicProxy)))
            {
                throw new ArgumentException("dynamicProxyType must be a child of DynamicProxy"); //LOCSTR
            }

            string typeName = string.Concat(ownClassName, "+", interfaceType.FullName);
            bool gotLock = false;
            try
            {
                dynamicTypeEmitSyncRoot.Enter(ref gotLock);
                dynamicTypes.TryGetValue(typeName, out ret);

                if (ret == null)
                {
                    TypeBuilder tb = moduleBuilder.DefineType(typeName, TypeAttributes.Public);
                    tb.SetParent(dynamicProxyBaseType);
                    tb.AddInterfaceImplementation(interfaceType);

                    CreateConstructorBaseCalls(dynamicProxyBaseType, tb);

                    DynamicImplementInterface(new List<Type> { interfaceType }, new List<string>(), interfaceType, tb);
                    ret = tb.CreateTypeInfo();

                    dynamicTypes.Add(typeName, ret);
                }
            }
            finally
            {
                if (gotLock)
                {
                    dynamicTypeEmitSyncRoot.Exit();
                }
            }

            return ret;
        }
       
        private void CreateConstructorBaseCalls(Type baseClass, TypeBuilder tb)
        {
            foreach (var baseConstructor in baseClass.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var parameters = baseConstructor.GetParameters();
                if (parameters.Length > 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false))
                {
                    throw new InvalidOperationException("Variadic constructors are not supported"); //LOCSTR
                }

                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

                var ctor = tb.DefineConstructor(MethodAttributes.Public, baseConstructor.CallingConvention, parameterTypes);
                for (var i = 0; i < parameters.Length; ++i)
                {
                    var parameter = parameters[i];
                    var pb = ctor.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
                    if (((int)parameter.Attributes & (int)ParameterAttributes.HasDefault) != 0)
                    {
                        pb.SetConstant(parameter.RawDefaultValue);
                    }
                }

                var getIL = ctor.GetILGenerator();
                getIL.Emit(OpCodes.Nop);

                getIL.Emit(OpCodes.Ldarg_0);
                for (var i = 1; i <= parameters.Length; ++i)
                {
                    getIL.Emit(OpCodes.Ldarg, i);
                }
                getIL.Emit(OpCodes.Call, baseConstructor);

                getIL.Emit(OpCodes.Ret);
            }
        }

        private void DynamicImplementInterface(List<Type> implementedInterfaceList, List<string> usedNames, Type interfaceType, TypeBuilder tb)
        {
            if (interfaceType != typeof(IDisposable))
            {
                List<MethodInfo> propAccessorList = new List<MethodInfo>();

                GenerateProperties(usedNames, interfaceType, tb, propAccessorList);

                GenerateEvents(usedNames, interfaceType, tb, propAccessorList);

                GenerateMethods(usedNames, interfaceType, tb, propAccessorList);

                foreach (Type i in interfaceType.GetInterfaces())
                {
                    if (!implementedInterfaceList.Contains(i))
                    {
                        DynamicImplementInterface(implementedInterfaceList, usedNames, i, tb);
                        implementedInterfaceList.Add(i);
                    }
                }
            }
        }

        private void EmitAndStoreGetTypeFromHandle(ILGenerator ilGenerator, Type type, OpCode storeCode)
        {
            //C#: Type.GetTypeFromHandle(interfaceType)
            ilGenerator.Emit(OpCodes.Ldtoken, type);
            ilGenerator.EmitCall(OpCodes.Call, getTypeFromHandleMethodInfo, null);
            ilGenerator.Emit(storeCode);
        }

        private void EmitInvokeMethod(MethodInfo mi, MethodBuilder mb)
        {
            ILGenerator ilGenerator = mb.GetILGenerator();
            
            string methodName = mb.Name;
            LocalBuilder typeLb = ilGenerator.DeclareLocal(typeof(Type), true);
            LocalBuilder paramsLb = ilGenerator.DeclareLocal(typeof(List<object>), true);
            LocalBuilder resultLb = ilGenerator.DeclareLocal(typeof(object), true);
            LocalBuilder retLb = ilGenerator.DeclareLocal(typeof(bool), true);

            //C#: Type.GetTypeFromHandle(interfaceType)
            EmitAndStoreGetTypeFromHandle(ilGenerator, mi.DeclaringType, OpCodes.Stloc_0);

            //C#: params = new List<object>()
            ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Stloc_1);

            int i = 0;
            foreach (ParameterInfo pi in mi.GetParameters())
            {
                //C#: params.Add(param[i])
                i++;
                ilGenerator.Emit(OpCodes.Ldloc_1);
                ilGenerator.Emit(OpCodes.Ldarg, i);
                if (pi.ParameterType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, pi.ParameterType);
                }
                ilGenerator.EmitCall(OpCodes.Callvirt, listAddMethodInfo, null);
            }
            //C#: ret = DynamicProxy.TryInvokeMember(interfaceType, propertyName, params, out result)
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);

            ilGenerator.Emit(OpCodes.Ldtoken, mi);
            ilGenerator.EmitCall(OpCodes.Call, getMethodMethod, null);

            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.EmitCall(OpCodes.Callvirt, listToArrayMethodInfo, null);
            ilGenerator.Emit(OpCodes.Ldloca_S, 2);
            ilGenerator.EmitCall(OpCodes.Callvirt, tryInvokeMemberInfo, null);
            ilGenerator.Emit(OpCodes.Stloc_3);

            if (mi.ReturnType != typeof(void))
            {
                ilGenerator.Emit(OpCodes.Ldloc_2);
                //C#: if(ret == ValueType && ret == null){
                //    ret = Activator.CreateInstance(returnType) }
                if (mi.ReturnType.IsValueType)
                {
                    Label retisnull = ilGenerator.DefineLabel();
                    Label endofif = ilGenerator.DefineLabel();

                    ilGenerator.Emit(OpCodes.Ldnull);
                    ilGenerator.Emit(OpCodes.Ceq);
                    ilGenerator.Emit(OpCodes.Brtrue_S, retisnull);
                    ilGenerator.Emit(OpCodes.Ldloc_2);
                    ilGenerator.Emit(OpCodes.Unbox_Any, mi.ReturnType);
                    ilGenerator.Emit(OpCodes.Br_S, endofif);
                    ilGenerator.MarkLabel(retisnull);
                    ilGenerator.Emit(OpCodes.Ldtoken, mi.ReturnType);
                    ilGenerator.EmitCall(OpCodes.Call, getTypeFromHandleMethodInfo, null);
                    ilGenerator.EmitCall(OpCodes.Call, activatorCreateInstanceMethodInfo, null);
                    ilGenerator.Emit(OpCodes.Unbox_Any, mi.ReturnType);
                    ilGenerator.MarkLabel(endofif);
                }
            }
            //C#: return ret
            ilGenerator.Emit(OpCodes.Ret);
        }

        private void GenerateMethods(List<string> usedNames, Type interfaceType, TypeBuilder tb, List<MethodInfo> propAccessors)
        {
            foreach (MethodInfo mi in interfaceType.GetMethods())
            {
                var parameterInfoArray = mi.GetParameters();
                var genericArgumentArray = mi.GetGenericArguments();

                string paramNames = string.Join(", ", parameterInfoArray.Select(pi => pi.ParameterType));
                string nameWithParams = string.Concat(mi.Name, "(", paramNames, ")");
                if (usedNames.Contains(nameWithParams))
                {
                    throw new NotSupportedException(string.Format("Error in interface {1}! Method '{0}' already used in other child interface!", nameWithParams, interfaceType.Name)); //LOCSTR
                }
                else
                {
                    usedNames.Add(nameWithParams);
                }

                if (!propAccessors.Contains(mi))
                {
                    MethodBuilder mb = tb.DefineMethod(mi.Name, MethodAttributes.Public | MethodAttributes.Virtual, mi.ReturnType, parameterInfoArray.Select(pi => pi.ParameterType).ToArray());
                    if (genericArgumentArray.Any())
                    {
                        mb.DefineGenericParameters(genericArgumentArray.Select(s => s.Name).ToArray());
                    }

                    EmitInvokeMethod(mi, mb);

                    tb.DefineMethodOverride(mb, mi);
                }
            }
        }

        private void EmitEventAdd(ILGenerator ilGenerator, EventInfo eventInfo, FieldBuilder eventField)
        {
            string eventName = eventInfo.Name;
            Type ehType = eventInfo.EventHandlerType;
            LocalBuilder typeLb = ilGenerator.DeclareLocal(typeof(Type), true);
            LocalBuilder objectLb = ilGenerator.DeclareLocal(typeof(object), true);
            LocalBuilder retLb = ilGenerator.DeclareLocal(typeof(bool), true);

            //C#: Type.GetTypeFromHandle(interfaceType)
            EmitAndStoreGetTypeFromHandle(ilGenerator, eventInfo.DeclaringType, OpCodes.Stloc_0);

            //C#: Delegate.Combine(eventHandler, value)
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, eventField);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.EmitCall(OpCodes.Call, delegateCombineMethodInfo, null);
            ilGenerator.Emit(OpCodes.Castclass, ehType);
            ilGenerator.Emit(OpCodes.Stfld, eventField);

            //C#: DynamicProxy.TrySetMember(interfaceType, propertyName, eventHandler)
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldstr, eventName);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, eventField);
            ilGenerator.EmitCall(OpCodes.Callvirt, trySetMemberMethodInfo, null);
            ilGenerator.Emit(OpCodes.Stloc_2);

            //C#: return
            ilGenerator.Emit(OpCodes.Ret);

        }

        private void EmitEventRemove(ILGenerator ilGenerator, EventInfo eventInfo, FieldBuilder eventField)
        {
            string eventName = eventInfo.Name;
            Type ehType = eventInfo.EventHandlerType;
            LocalBuilder typeLb = ilGenerator.DeclareLocal(typeof(Type), true);
            LocalBuilder objectLb = ilGenerator.DeclareLocal(typeof(object), true);
            LocalBuilder retLb = ilGenerator.DeclareLocal(typeof(bool), true);

            //C#: Type.GetTypeFromHandle(interfaceType)
            EmitAndStoreGetTypeFromHandle(ilGenerator, eventInfo.DeclaringType, OpCodes.Stloc_0);

            //C#: Delegate.Remove(eventHandler, value)
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, eventField);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.EmitCall(OpCodes.Call, delegateRemoveMethodInfo, null);
            ilGenerator.Emit(OpCodes.Castclass, ehType);
            ilGenerator.Emit(OpCodes.Stfld, eventField);

            //C#: DynamicProxy.TrySetMember(interfaceType, propertyName, eventHandler)
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldstr, eventName);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, eventField);
            ilGenerator.EmitCall(OpCodes.Callvirt, trySetMemberMethodInfo, null);
            ilGenerator.Emit(OpCodes.Stloc_2);

            //C#: return
            ilGenerator.Emit(OpCodes.Ret);

        }

        private void GenerateEvents(List<string> usedNames, Type interfaceType, TypeBuilder tb, List<MethodInfo> propAccessors)
        {
            foreach (EventInfo eventInfo in interfaceType.GetEvents(BindingFlags.Instance | BindingFlags.Public))
            {
                if (usedNames.Contains(eventInfo.Name))
                {
                    throw new NotSupportedException(string.Format("Error in interface {1}! Event name '{0}' already used in other child interface!", eventInfo.Name, interfaceType.Name)); //LOCSTR
                }
                else
                {
                    usedNames.Add(eventInfo.Name);
                }

                EventBuilder eb = tb.DefineEvent(eventInfo.Name, eventInfo.Attributes, eventInfo.EventHandlerType);
                FieldBuilder ef = tb.DefineField(string.Concat("_", eventInfo.Name), eventInfo.EventHandlerType, FieldAttributes.Private);

                //add
                {
                    MethodInfo addMethodInfo = eventInfo.GetAddMethod();
                    propAccessors.Add(addMethodInfo);
                    MethodBuilder getMb = tb.DefineMethod(addMethodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new Type[] { eventInfo.EventHandlerType });
                    ILGenerator ilGenerator = getMb.GetILGenerator();

                    EmitEventAdd(ilGenerator, eventInfo, ef);

                    tb.DefineMethodOverride(getMb, addMethodInfo);
                }
                //remove
                {
                    MethodInfo removeMethodInfo = eventInfo.GetRemoveMethod();
                    propAccessors.Add(removeMethodInfo);
                    MethodBuilder getMb = tb.DefineMethod(removeMethodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new Type[] { eventInfo.EventHandlerType });
                    ILGenerator ilGenerator = getMb.GetILGenerator();

                    EmitEventRemove(ilGenerator, eventInfo, ef);

                    tb.DefineMethodOverride(getMb, removeMethodInfo);

                }
            }
        }

        private void EmitPropertyGet(ILGenerator ilGenerator, PropertyInfo propertyInfo)
        {
            string propertyName = propertyInfo.Name;
            LocalBuilder typeLb = ilGenerator.DeclareLocal(typeof(Type), true);
            LocalBuilder outObjectLb = ilGenerator.DeclareLocal(typeof(object), true);
            LocalBuilder retLb = ilGenerator.DeclareLocal(typeof(bool), true);

            //C#: Type.GetTypeFromHandle(interfaceType)
            EmitAndStoreGetTypeFromHandle(ilGenerator, propertyInfo.DeclaringType, OpCodes.Stloc_0);

            //C#: ret = DynamicProxy.TryGetMember(interfaceType, propertyName, out outObject)
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldstr, propertyName);
            ilGenerator.Emit(OpCodes.Ldloca_S, 1);
            ilGenerator.EmitCall(OpCodes.Callvirt, tryGetMemberMethodInfo, null);
            ilGenerator.Emit(OpCodes.Stloc_2);

            //C#: if(ret == ValueType && ret == null){
            //    ret = Activator.CreateInstance(returnType) }
            ilGenerator.Emit(OpCodes.Ldloc_1);
            if (propertyInfo.PropertyType.IsValueType)
            {
                Label retisnull = ilGenerator.DefineLabel();
                Label endofif = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Brtrue_S, retisnull);
                ilGenerator.Emit(OpCodes.Ldloc_1);
                ilGenerator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
                ilGenerator.Emit(OpCodes.Br_S, endofif);
                ilGenerator.MarkLabel(retisnull);
                ilGenerator.Emit(OpCodes.Ldtoken, propertyInfo.PropertyType);
                ilGenerator.EmitCall(OpCodes.Call, getTypeFromHandleMethodInfo, null);
                ilGenerator.EmitCall(OpCodes.Call, activatorCreateInstanceMethodInfo, null);
                ilGenerator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
                ilGenerator.MarkLabel(endofif);
            }
            //C#: return ret
            ilGenerator.Emit(OpCodes.Ret);

        }

        private void EmitPropertySet(ILGenerator ilGenerator, PropertyInfo propertyInfo)
        {
            string propertyName = propertyInfo.Name;
            LocalBuilder typeLb = ilGenerator.DeclareLocal(typeof(Type), true);
            LocalBuilder objectLb = ilGenerator.DeclareLocal(propertyInfo.PropertyType, true);
            LocalBuilder retLb = ilGenerator.DeclareLocal(typeof(bool), true);

            //C#: Type.GetTypeFromHandle(interfaceType)
            EmitAndStoreGetTypeFromHandle(ilGenerator, propertyInfo.DeclaringType, OpCodes.Stloc_0);

            //C#: object = value
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stloc_1);

            //C#: DynamicProxy.TrySetMember(interfaceType, propertyName, object)
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldstr, propertyName);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            if (propertyInfo.PropertyType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }
            ilGenerator.EmitCall(OpCodes.Callvirt, trySetMemberMethodInfo, null);
            ilGenerator.Emit(OpCodes.Stloc_2);

            //C#: return
            ilGenerator.Emit(OpCodes.Ret);
        }

        private void GenerateProperties(List<string> usedNames, Type interfaceType, TypeBuilder tb, List<MethodInfo> propAccessors)
        {
            foreach (PropertyInfo propertyInfo in interfaceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (usedNames.Contains(propertyInfo.Name))
                {
                    throw new NotSupportedException(string.Format("Error in interface {1}! Property name '{0}' already used in other child interface!", propertyInfo.Name, interfaceType.Name)); //LOCSTR
                }
                else
                {
                    usedNames.Add(propertyInfo.Name);
                }

                PropertyBuilder pb = tb.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, null);
                if (propertyInfo.CanRead)
                {
                    MethodInfo getMethodInfo = propertyInfo.GetGetMethod();
                    propAccessors.Add(getMethodInfo);

                    MethodBuilder getMb = tb.DefineMethod(getMethodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, propertyInfo.PropertyType, Type.EmptyTypes);
                    ILGenerator ilGenerator = getMb.GetILGenerator();

                    EmitPropertyGet(ilGenerator, propertyInfo);

                    pb.SetGetMethod(getMb);
                    tb.DefineMethodOverride(getMb, getMethodInfo);
                }

                if (propertyInfo.CanWrite)
                {
                    MethodInfo setMethodInfo = propertyInfo.GetSetMethod();
                    propAccessors.Add(setMethodInfo);
                    MethodBuilder setMb = tb.DefineMethod(setMethodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new Type[] { propertyInfo.PropertyType });
                    ILGenerator ilGenerator = setMb.GetILGenerator();

                    EmitPropertySet(ilGenerator, propertyInfo);

                    pb.SetSetMethod(setMb);
                    tb.DefineMethodOverride(setMb, setMethodInfo);
                }
            }
        }
    }
}
