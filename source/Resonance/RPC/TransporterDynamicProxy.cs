using Microsoft.Extensions.Logging;
using Resonance.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.RPC
{
    public class TransporterDynamicProxy : DynamicProxy
    {
        private IResonanceTransporter _transporter;
        private MethodInfo _sendRequestMethod;

        public TransporterDynamicProxy(IResonanceTransporter transporter)
        {
            _transporter = transporter;
        }

        protected override bool TryGetMember(Type interfaceType, string name, out object result)
        {
            ResonanceRequestConfig config = new ResonanceRequestConfig();

            var prop = interfaceType.GetProperty(name);
            var att = prop.GetRpcAttribute();

            if (att != null)
            {
                config = att.ToRequestConfig();
            }

            config.RPCSignature = RPCSignature.FromMemberInfo(prop);

            var response = _transporter.SendRequest(new RpcPropertyGetRequest(), config);
            result = response;
            return true;
        }

        protected override bool TrySetMember(Type interfaceType, string name, object value)
        {
            ResonanceMessageConfig config = new ResonanceMessageConfig();

            var prop = interfaceType.GetProperty(name);
            var att = prop.GetRpcAttribute();

            if (att != null)
            {
                config = att.ToMessageConfig();
            }

            config.RPCSignature = RPCSignature.FromMemberInfo(prop);
            config.RequireACK = true;

            _transporter.Send(value, config);
            return true;
        }

        protected override bool TryInvokeMember(Type interfaceType, MethodInfo methodInfo, object[] args, out object result)
        {
            var attr = methodInfo.GetRpcAttribute();

            if (methodInfo.GetParameters().Length > 1)
            {
                var list = new MethodParamCollection();

                foreach (var arg in args)
                {
                    list.Add(arg);
                }

                args = new object[] { list };
            }

            if (methodInfo.ReturnType == typeof(void) || methodInfo.ReturnType == typeof(Task))
            {
                ResonanceMessageConfig config = new ResonanceMessageConfig();

                if (attr != null)
                {
                    config = attr.ToMessageConfig();
                }
                else
                {
                    config.RequireACK = true;
                }

                config.RPCSignature = RPCSignature.FromMemberInfo(methodInfo);

                if (methodInfo.ReturnType == typeof(Task))
                {
                    result = _transporter.SendAsync(args.FirstOrDefault(), config);
                }
                else
                {
                    _transporter.Send(args.FirstOrDefault(), config);
                    result = new object();
                }
            }
            else
            {
                ResonanceRequestConfig config = new ResonanceRequestConfig();

                if (attr != null)
                {
                    config = attr.ToRequestConfig();
                }

                config.RPCSignature = RPCSignature.FromMemberInfo(methodInfo);

                if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                {
                    var returnType = methodInfo.ReturnType.GetGenericArguments()[0];

                    if (returnType.IsPrimitive)
                    {
                        if (_sendRequestMethod == null)
                        {
                            _sendRequestMethod = typeof(IResonanceTransporter).GetMethods().First(x => x.Name == nameof(IResonanceTransporter.SendRequestAsync) && x.ContainsGenericParameters);
                        }

                        MethodInfo generic = _sendRequestMethod.MakeGenericMethod(typeof(object), returnType);

                        result = generic.Invoke(_transporter, new object[]
                        {
                            args.FirstOrDefault(),
                            config
                        });
                    }
                    else
                    {
                        result = _transporter.SendRequestAsync(args.FirstOrDefault(), config);
                    }
                }
                else
                {
                    result = _transporter.SendRequest(args.FirstOrDefault(), config);
                }
            }

            return true;
        }

        protected override bool TrySetEvent(Type interfaceType, string name, object value)
        {
            if (_transporter.State == ResonanceComponentState.Connected)
            {
                RegisterEvent(interfaceType, name, value);
            }
            else
            {
                _transporter.StateChanged += (x, e) =>
                {
                    if (e.NewState == ResonanceComponentState.Connected)
                    {
                        try
                        {
                            RegisterEvent(interfaceType, name, value);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"Error registering for RPC event '{interfaceType.Name}.{name}'.");
                        }
                    }
                };
            }

            return true;
        }

        private void RegisterEvent(Type interfaceType, string name, object value)
        {
            var ev = interfaceType.GetEvent(name);
            var attr = ev.GetRpcAttribute();

            ResonanceContinuousRequestConfig config = new ResonanceContinuousRequestConfig();

            if (attr != null)
            {
                config = attr.ToContinuousRequestConfig();
                config.Timeout = null;
                config.ContinuousTimeout = null;
            }

            config.RPCSignature = RPCSignature.FromMemberInfo(ev);

            var response = _transporter.SendContinuousRequest<RpcEventRequest, RpcEventResponse>(new RpcEventRequest(), config).Subscribe((r) =>
            {
                try
                {
                    ((MulticastDelegate)value).Method.Invoke(((MulticastDelegate)value).Target, new object[] { _transporter, r.Response });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"The RPC event stream '{config.RPCSignature.ToDescription()}' has terminated with an error.");
                }
            }, (ex) =>
            {
                if (_transporter.State != ResonanceComponentState.Failed)
                {
                    Logger.LogError(ex, $"Error registering for RPC event '{config.RPCSignature.ToDescription()}'.");
                }
            }, () =>
            {
                Logger.LogError($"The RPC event stream '{config.RPCSignature.ToDescription()}' has terminated unexpectedly.");
            });
        }
    }
}
