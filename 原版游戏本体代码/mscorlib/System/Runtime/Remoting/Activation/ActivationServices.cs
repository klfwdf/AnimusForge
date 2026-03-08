using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000891 RID: 2193
	internal static class ActivationServices
	{
		// Token: 0x06005CE7 RID: 23783 RVA: 0x00145AB0 File Offset: 0x00143CB0
		[SecurityCritical]
		private static void Startup()
		{
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			if (!remotingData.ActivationInitialized || remotingData.InitializingActivation)
			{
				object configLock = remotingData.ConfigLock;
				bool flag = false;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					Monitor.Enter(configLock, ref flag);
					remotingData.InitializingActivation = true;
					if (!remotingData.ActivationInitialized)
					{
						remotingData.LocalActivator = new LocalActivator();
						remotingData.ActivationListener = new ActivationListener();
						remotingData.ActivationInitialized = true;
					}
					remotingData.InitializingActivation = false;
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(configLock);
					}
				}
			}
		}

		// Token: 0x06005CE8 RID: 23784 RVA: 0x00145B40 File Offset: 0x00143D40
		[SecurityCritical]
		private static void InitActivationServices()
		{
			if (ActivationServices.activator == null)
			{
				ActivationServices.activator = ActivationServices.GetActivator();
				if (ActivationServices.activator == null)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadInternalState_ActivationFailure"), Array.Empty<object>()));
				}
			}
		}

		// Token: 0x06005CE9 RID: 23785 RVA: 0x00145B80 File Offset: 0x00143D80
		[SecurityCritical]
		private static MarshalByRefObject IsCurrentContextOK(RuntimeType serverType, object[] props, bool bNewObj)
		{
			ActivationServices.InitActivationServices();
			ProxyAttribute proxyAttribute = ActivationServices.GetProxyAttribute(serverType);
			MarshalByRefObject marshalByRefObject;
			if (proxyAttribute == ActivationServices.DefaultProxyAttribute)
			{
				marshalByRefObject = proxyAttribute.CreateInstanceInternal(serverType);
			}
			else
			{
				marshalByRefObject = proxyAttribute.CreateInstance(serverType);
				if (marshalByRefObject != null && !RemotingServices.IsTransparentProxy(marshalByRefObject) && !serverType.IsAssignableFrom(marshalByRefObject.GetType()))
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Activation_BadObject"), serverType));
				}
			}
			return marshalByRefObject;
		}

		// Token: 0x06005CEA RID: 23786 RVA: 0x00145BEC File Offset: 0x00143DEC
		[SecurityCritical]
		private static MarshalByRefObject CreateObjectForCom(RuntimeType serverType, object[] props, bool bNewObj)
		{
			if (ActivationServices.PeekActivationAttributes(serverType) != null)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ActivForCom"));
			}
			ActivationServices.InitActivationServices();
			ProxyAttribute proxyAttribute = ActivationServices.GetProxyAttribute(serverType);
			MarshalByRefObject result;
			if (proxyAttribute is ICustomFactory)
			{
				result = ((ICustomFactory)proxyAttribute).CreateInstance(serverType);
			}
			else
			{
				result = (MarshalByRefObject)Activator.CreateInstance(serverType, true);
			}
			return result;
		}

		// Token: 0x06005CEB RID: 23787 RVA: 0x00145C44 File Offset: 0x00143E44
		[SecurityCritical]
		private static bool IsCurrentContextOK(RuntimeType serverType, object[] props, ref ConstructorCallMessage ctorCallMsg)
		{
			object[] array = ActivationServices.PeekActivationAttributes(serverType);
			if (array != null)
			{
				ActivationServices.PopActivationAttributes(serverType);
			}
			object[] array2 = new object[] { ActivationServices.GetGlobalAttribute() };
			object[] contextAttributesForType = ActivationServices.GetContextAttributesForType(serverType);
			object[] array3 = contextAttributesForType;
			Context currentContext = Thread.CurrentContext;
			ctorCallMsg = new ConstructorCallMessage(array, array2, array3, serverType);
			ctorCallMsg.Activator = new ConstructionLevelActivator();
			bool flag = ActivationServices.QueryAttributesIfContextOK(currentContext, ctorCallMsg, array2);
			if (flag)
			{
				flag = ActivationServices.QueryAttributesIfContextOK(currentContext, ctorCallMsg, array);
				if (flag)
				{
					flag = ActivationServices.QueryAttributesIfContextOK(currentContext, ctorCallMsg, array3);
				}
			}
			return flag;
		}

		// Token: 0x06005CEC RID: 23788 RVA: 0x00145CC4 File Offset: 0x00143EC4
		[SecurityCritical]
		private static void CheckForInfrastructurePermission(RuntimeAssembly asm)
		{
			if (asm != ActivationServices.s_MscorlibAssembly)
			{
				SecurityPermission demand = new SecurityPermission(SecurityPermissionFlag.Infrastructure);
				CodeAccessSecurityEngine.CheckAssembly(asm, demand);
			}
		}

		// Token: 0x06005CED RID: 23789 RVA: 0x00145CF0 File Offset: 0x00143EF0
		[SecurityCritical]
		private static bool QueryAttributesIfContextOK(Context ctx, IConstructionCallMessage ctorMsg, object[] attributes)
		{
			bool flag = true;
			if (attributes != null)
			{
				for (int i = 0; i < attributes.Length; i++)
				{
					IContextAttribute contextAttribute = attributes[i] as IContextAttribute;
					if (contextAttribute == null)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Activation_BadAttribute"));
					}
					RuntimeAssembly asm = (RuntimeAssembly)contextAttribute.GetType().Assembly;
					ActivationServices.CheckForInfrastructurePermission(asm);
					flag = contextAttribute.IsContextOK(ctx, ctorMsg);
					if (!flag)
					{
						break;
					}
				}
			}
			return flag;
		}

		// Token: 0x06005CEE RID: 23790 RVA: 0x00145D54 File Offset: 0x00143F54
		[SecurityCritical]
		internal static void GetPropertiesFromAttributes(IConstructionCallMessage ctorMsg, object[] attributes)
		{
			if (attributes != null)
			{
				for (int i = 0; i < attributes.Length; i++)
				{
					IContextAttribute contextAttribute = attributes[i] as IContextAttribute;
					if (contextAttribute == null)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Activation_BadAttribute"));
					}
					RuntimeAssembly asm = (RuntimeAssembly)contextAttribute.GetType().Assembly;
					ActivationServices.CheckForInfrastructurePermission(asm);
					contextAttribute.GetPropertiesForNewContext(ctorMsg);
				}
			}
		}

		// Token: 0x17000FFB RID: 4091
		// (get) Token: 0x06005CEF RID: 23791 RVA: 0x00145DAE File Offset: 0x00143FAE
		internal static ProxyAttribute DefaultProxyAttribute
		{
			[SecurityCritical]
			get
			{
				return ActivationServices._proxyAttribute;
			}
		}

		// Token: 0x06005CF0 RID: 23792 RVA: 0x00145DB8 File Offset: 0x00143FB8
		[SecurityCritical]
		internal static ProxyAttribute GetProxyAttribute(Type serverType)
		{
			if (!serverType.HasProxyAttribute)
			{
				return ActivationServices.DefaultProxyAttribute;
			}
			ProxyAttribute proxyAttribute = ActivationServices._proxyTable[serverType] as ProxyAttribute;
			if (proxyAttribute == null)
			{
				object[] customAttributes = Attribute.GetCustomAttributes(serverType, ActivationServices.proxyAttributeType, true);
				object[] array = customAttributes;
				if (array != null && array.Length != 0)
				{
					if (!serverType.IsContextful)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Activation_MBR_ProxyAttribute"));
					}
					proxyAttribute = array[0] as ProxyAttribute;
				}
				if (!ActivationServices._proxyTable.Contains(serverType))
				{
					Hashtable proxyTable = ActivationServices._proxyTable;
					lock (proxyTable)
					{
						if (!ActivationServices._proxyTable.Contains(serverType))
						{
							ActivationServices._proxyTable.Add(serverType, proxyAttribute);
						}
					}
				}
			}
			return proxyAttribute;
		}

		// Token: 0x06005CF1 RID: 23793 RVA: 0x00145E74 File Offset: 0x00144074
		[SecurityCritical]
		internal static MarshalByRefObject CreateInstance(RuntimeType serverType)
		{
			ConstructorCallMessage constructorCallMessage = null;
			bool flag = ActivationServices.IsCurrentContextOK(serverType, null, ref constructorCallMessage);
			MarshalByRefObject marshalByRefObject;
			if (flag && !serverType.IsContextful)
			{
				marshalByRefObject = RemotingServices.AllocateUninitializedObject(serverType);
			}
			else
			{
				marshalByRefObject = (MarshalByRefObject)ActivationServices.ConnectIfNecessary(constructorCallMessage);
				RemotingProxy remotingProxy;
				if (marshalByRefObject == null)
				{
					remotingProxy = new RemotingProxy(serverType);
					marshalByRefObject = (MarshalByRefObject)remotingProxy.GetTransparentProxy();
				}
				else
				{
					remotingProxy = (RemotingProxy)RemotingServices.GetRealProxy(marshalByRefObject);
				}
				remotingProxy.ConstructorMessage = constructorCallMessage;
				if (!flag)
				{
					ContextLevelActivator contextLevelActivator = new ContextLevelActivator();
					contextLevelActivator.NextActivator = constructorCallMessage.Activator;
					constructorCallMessage.Activator = contextLevelActivator;
				}
				else
				{
					constructorCallMessage.ActivateInContext = true;
				}
			}
			return marshalByRefObject;
		}

		// Token: 0x06005CF2 RID: 23794 RVA: 0x00145F04 File Offset: 0x00144104
		[SecurityCritical]
		internal static IConstructionReturnMessage Activate(RemotingProxy remProxy, IConstructionCallMessage ctorMsg)
		{
			IConstructionReturnMessage constructionReturnMessage;
			if (((ConstructorCallMessage)ctorMsg).ActivateInContext)
			{
				constructionReturnMessage = ctorMsg.Activator.Activate(ctorMsg);
				if (constructionReturnMessage.Exception != null)
				{
					throw constructionReturnMessage.Exception;
				}
			}
			else
			{
				ActivationServices.GetPropertiesFromAttributes(ctorMsg, ctorMsg.CallSiteActivationAttributes);
				ActivationServices.GetPropertiesFromAttributes(ctorMsg, ((ConstructorCallMessage)ctorMsg).GetWOMAttributes());
				ActivationServices.GetPropertiesFromAttributes(ctorMsg, ((ConstructorCallMessage)ctorMsg).GetTypeAttributes());
				IMessageSink clientContextChain = Thread.CurrentContext.GetClientContextChain();
				IMethodReturnMessage methodReturnMessage = (IMethodReturnMessage)clientContextChain.SyncProcessMessage(ctorMsg);
				constructionReturnMessage = methodReturnMessage as IConstructionReturnMessage;
				if (methodReturnMessage == null)
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_Activation_Failed"));
				}
				if (methodReturnMessage.Exception != null)
				{
					throw methodReturnMessage.Exception;
				}
			}
			return constructionReturnMessage;
		}

		// Token: 0x06005CF3 RID: 23795 RVA: 0x00145FAC File Offset: 0x001441AC
		[SecurityCritical]
		internal static IConstructionReturnMessage DoCrossContextActivation(IConstructionCallMessage reqMsg)
		{
			bool isContextful = reqMsg.ActivationType.IsContextful;
			Context context = null;
			if (isContextful)
			{
				context = new Context();
				ArrayList arrayList = (ArrayList)reqMsg.ContextProperties;
				for (int i = 0; i < arrayList.Count; i++)
				{
					IContextProperty contextProperty = arrayList[i] as IContextProperty;
					if (contextProperty == null)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Activation_BadAttribute"));
					}
					RuntimeAssembly asm = (RuntimeAssembly)contextProperty.GetType().Assembly;
					ActivationServices.CheckForInfrastructurePermission(asm);
					if (context.GetProperty(contextProperty.Name) == null)
					{
						context.SetProperty(contextProperty);
					}
				}
				context.Freeze();
				for (int j = 0; j < arrayList.Count; j++)
				{
					if (!((IContextProperty)arrayList[j]).IsNewContextOK(context))
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Activation_PropertyUnhappy"));
					}
				}
			}
			InternalCrossContextDelegate internalCrossContextDelegate = new InternalCrossContextDelegate(ActivationServices.DoCrossContextActivationCallback);
			object[] args = new object[] { reqMsg };
			IConstructionReturnMessage result;
			if (isContextful)
			{
				result = Thread.CurrentThread.InternalCrossContextCallback(context, internalCrossContextDelegate, args) as IConstructionReturnMessage;
			}
			else
			{
				result = internalCrossContextDelegate(args) as IConstructionReturnMessage;
			}
			return result;
		}

		// Token: 0x06005CF4 RID: 23796 RVA: 0x001460D4 File Offset: 0x001442D4
		[SecurityCritical]
		internal static object DoCrossContextActivationCallback(object[] args)
		{
			IConstructionCallMessage constructionCallMessage = (IConstructionCallMessage)args[0];
			IMethodReturnMessage methodReturnMessage = (IMethodReturnMessage)Thread.CurrentContext.GetServerContextChain().SyncProcessMessage(constructionCallMessage);
			IConstructionReturnMessage constructionReturnMessage = methodReturnMessage as IConstructionReturnMessage;
			if (constructionReturnMessage == null)
			{
				Exception e;
				if (methodReturnMessage != null)
				{
					e = methodReturnMessage.Exception;
				}
				else
				{
					e = new RemotingException(Environment.GetResourceString("Remoting_Activation_Failed"));
				}
				constructionReturnMessage = new ConstructorReturnMessage(e, null);
				((ConstructorReturnMessage)constructionReturnMessage).SetLogicalCallContext((LogicalCallContext)constructionCallMessage.Properties[Message.CallContextKey]);
			}
			return constructionReturnMessage;
		}

		// Token: 0x06005CF5 RID: 23797 RVA: 0x00146154 File Offset: 0x00144354
		[SecurityCritical]
		internal static IConstructionReturnMessage DoServerContextActivation(IConstructionCallMessage reqMsg)
		{
			Exception e = null;
			Type activationType = reqMsg.ActivationType;
			object serverObj = ActivationServices.ActivateWithMessage(activationType, reqMsg, null, out e);
			return ActivationServices.SetupConstructionReply(serverObj, reqMsg, e);
		}

		// Token: 0x06005CF6 RID: 23798 RVA: 0x00146180 File Offset: 0x00144380
		[SecurityCritical]
		internal static IConstructionReturnMessage SetupConstructionReply(object serverObj, IConstructionCallMessage ctorMsg, Exception e)
		{
			IConstructionReturnMessage constructionReturnMessage;
			if (e == null)
			{
				constructionReturnMessage = new ConstructorReturnMessage((MarshalByRefObject)serverObj, null, 0, (LogicalCallContext)ctorMsg.Properties[Message.CallContextKey], ctorMsg);
			}
			else
			{
				constructionReturnMessage = new ConstructorReturnMessage(e, null);
				((ConstructorReturnMessage)constructionReturnMessage).SetLogicalCallContext((LogicalCallContext)ctorMsg.Properties[Message.CallContextKey]);
			}
			return constructionReturnMessage;
		}

		// Token: 0x06005CF7 RID: 23799 RVA: 0x001461E4 File Offset: 0x001443E4
		[SecurityCritical]
		internal static object ActivateWithMessage(Type serverType, IMessage msg, ServerIdentity srvIdToBind, out Exception e)
		{
			e = null;
			object obj = RemotingServices.AllocateUninitializedObject(serverType);
			object obj2;
			if (serverType.IsContextful)
			{
				if (msg is ConstructorCallMessage)
				{
					obj2 = ((ConstructorCallMessage)msg).GetThisPtr();
				}
				else
				{
					obj2 = null;
				}
				obj2 = RemotingServices.Wrap((ContextBoundObject)obj, obj2, false);
			}
			else
			{
				if (Thread.CurrentContext != Context.DefaultContext)
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_Activation_Failed"));
				}
				obj2 = obj;
			}
			IMessageSink messageSink = new StackBuilderSink(obj2);
			IMethodReturnMessage methodReturnMessage = (IMethodReturnMessage)messageSink.SyncProcessMessage(msg);
			if (methodReturnMessage.Exception != null)
			{
				e = methodReturnMessage.Exception;
				return null;
			}
			if (serverType.IsContextful)
			{
				return RemotingServices.Wrap((ContextBoundObject)obj);
			}
			return obj;
		}

		// Token: 0x06005CF8 RID: 23800 RVA: 0x00146288 File Offset: 0x00144488
		[SecurityCritical]
		internal static void StartListeningForRemoteRequests()
		{
			ActivationServices.Startup();
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			if (!remotingData.ActivatorListening)
			{
				object configLock = remotingData.ConfigLock;
				bool flag = false;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					Monitor.Enter(configLock, ref flag);
					if (!remotingData.ActivatorListening)
					{
						RemotingServices.MarshalInternal(Thread.GetDomain().RemotingData.ActivationListener, "RemoteActivationService.rem", typeof(IActivator));
						ServerIdentity serverIdentity = (ServerIdentity)IdentityHolder.ResolveIdentity("RemoteActivationService.rem");
						serverIdentity.SetSingletonObjectMode();
						remotingData.ActivatorListening = true;
					}
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(configLock);
					}
				}
			}
		}

		// Token: 0x06005CF9 RID: 23801 RVA: 0x00146328 File Offset: 0x00144528
		[SecurityCritical]
		internal static IActivator GetActivator()
		{
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			if (remotingData.LocalActivator == null)
			{
				ActivationServices.Startup();
			}
			return remotingData.LocalActivator;
		}

		// Token: 0x06005CFA RID: 23802 RVA: 0x00146353 File Offset: 0x00144553
		[SecurityCritical]
		internal static void Initialize()
		{
			ActivationServices.GetActivator();
		}

		// Token: 0x06005CFB RID: 23803 RVA: 0x0014635C File Offset: 0x0014455C
		[SecurityCritical]
		internal static ContextAttribute GetGlobalAttribute()
		{
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			if (remotingData.LocalActivator == null)
			{
				ActivationServices.Startup();
			}
			return remotingData.LocalActivator;
		}

		// Token: 0x06005CFC RID: 23804 RVA: 0x00146388 File Offset: 0x00144588
		[SecurityCritical]
		internal static IContextAttribute[] GetContextAttributesForType(Type serverType)
		{
			if (!typeof(ContextBoundObject).IsAssignableFrom(serverType) || serverType.IsCOMObject)
			{
				return new ContextAttribute[0];
			}
			int num = 8;
			IContextAttribute[] array = new IContextAttribute[num];
			int num2 = 0;
			object[] customAttributes = serverType.GetCustomAttributes(typeof(IContextAttribute), true);
			foreach (IContextAttribute contextAttribute in customAttributes)
			{
				Type type = contextAttribute.GetType();
				bool flag = false;
				for (int j = 0; j < num2; j++)
				{
					if (type.Equals(array[j].GetType()))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					num2++;
					if (num2 > num - 1)
					{
						IContextAttribute[] array3 = new IContextAttribute[2 * num];
						Array.Copy(array, 0, array3, 0, num);
						array = array3;
						num *= 2;
					}
					array[num2 - 1] = contextAttribute;
				}
			}
			IContextAttribute[] array4 = new IContextAttribute[num2];
			Array.Copy(array, array4, num2);
			return array4;
		}

		// Token: 0x06005CFD RID: 23805 RVA: 0x00146480 File Offset: 0x00144680
		[SecurityCritical]
		internal static object ConnectIfNecessary(IConstructionCallMessage ctorMsg)
		{
			string text = (string)ctorMsg.Properties["Connect"];
			object result = null;
			if (text != null)
			{
				result = RemotingServices.Connect(ctorMsg.ActivationType, text);
			}
			return result;
		}

		// Token: 0x06005CFE RID: 23806 RVA: 0x001464B8 File Offset: 0x001446B8
		[SecurityCritical]
		internal static object CheckIfConnected(RemotingProxy proxy, IConstructionCallMessage ctorMsg)
		{
			string text = (string)ctorMsg.Properties["Connect"];
			object result = null;
			if (text != null)
			{
				result = proxy.GetTransparentProxy();
			}
			return result;
		}

		// Token: 0x06005CFF RID: 23807 RVA: 0x001464E8 File Offset: 0x001446E8
		internal static void PushActivationAttributes(Type serverType, object[] attributes)
		{
			if (ActivationServices._attributeStack == null)
			{
				ActivationServices._attributeStack = new ActivationAttributeStack();
			}
			ActivationServices._attributeStack.Push(serverType, attributes);
		}

		// Token: 0x06005D00 RID: 23808 RVA: 0x00146507 File Offset: 0x00144707
		internal static object[] PeekActivationAttributes(Type serverType)
		{
			if (ActivationServices._attributeStack == null)
			{
				return null;
			}
			return ActivationServices._attributeStack.Peek(serverType);
		}

		// Token: 0x06005D01 RID: 23809 RVA: 0x0014651D File Offset: 0x0014471D
		internal static void PopActivationAttributes(Type serverType)
		{
			ActivationServices._attributeStack.Pop(serverType);
		}

		// Token: 0x040029E5 RID: 10725
		private static volatile IActivator activator = null;

		// Token: 0x040029E6 RID: 10726
		private static Hashtable _proxyTable = new Hashtable();

		// Token: 0x040029E7 RID: 10727
		private static readonly Type proxyAttributeType = typeof(ProxyAttribute);

		// Token: 0x040029E8 RID: 10728
		[SecurityCritical]
		private static ProxyAttribute _proxyAttribute = new ProxyAttribute();

		// Token: 0x040029E9 RID: 10729
		[ThreadStatic]
		internal static ActivationAttributeStack _attributeStack;

		// Token: 0x040029EA RID: 10730
		internal static readonly Assembly s_MscorlibAssembly = typeof(object).Assembly;

		// Token: 0x040029EB RID: 10731
		internal const string ActivationServiceURI = "RemoteActivationService.rem";

		// Token: 0x040029EC RID: 10732
		internal const string RemoteActivateKey = "Remote";

		// Token: 0x040029ED RID: 10733
		internal const string PermissionKey = "Permission";

		// Token: 0x040029EE RID: 10734
		internal const string ConnectKey = "Connect";
	}
}
