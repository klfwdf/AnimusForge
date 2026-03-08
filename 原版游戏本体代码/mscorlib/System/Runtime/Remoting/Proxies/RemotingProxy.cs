using System;
using System.Reflection;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Proxies
{
	// Token: 0x02000802 RID: 2050
	[SecurityCritical]
	internal class RemotingProxy : RealProxy, IRemotingTypeInfo
	{
		// Token: 0x06005858 RID: 22616 RVA: 0x0013779A File Offset: 0x0013599A
		public RemotingProxy(Type serverType)
			: base(serverType)
		{
		}

		// Token: 0x06005859 RID: 22617 RVA: 0x001377A3 File Offset: 0x001359A3
		private RemotingProxy()
		{
		}

		// Token: 0x17000EA4 RID: 3748
		// (get) Token: 0x0600585A RID: 22618 RVA: 0x001377AB File Offset: 0x001359AB
		// (set) Token: 0x0600585B RID: 22619 RVA: 0x001377B3 File Offset: 0x001359B3
		internal int CtorThread
		{
			get
			{
				return this._ctorThread;
			}
			set
			{
				this._ctorThread = value;
			}
		}

		// Token: 0x0600585C RID: 22620 RVA: 0x001377BC File Offset: 0x001359BC
		internal static IMessage CallProcessMessage(IMessageSink ms, IMessage reqMsg, ArrayWithSize proxySinks, Thread currentThread, Context currentContext, bool bSkippingContextChain)
		{
			if (proxySinks != null)
			{
				DynamicPropertyHolder.NotifyDynamicSinks(reqMsg, proxySinks, true, true, false);
			}
			bool flag = false;
			if (bSkippingContextChain)
			{
				flag = currentContext.NotifyDynamicSinks(reqMsg, true, true, false, true);
				ChannelServices.NotifyProfiler(reqMsg, RemotingProfilerEvent.ClientSend);
			}
			if (ms == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Proxy_NoChannelSink"));
			}
			IMessage message = ms.SyncProcessMessage(reqMsg);
			if (bSkippingContextChain)
			{
				ChannelServices.NotifyProfiler(message, RemotingProfilerEvent.ClientReceive);
				if (flag)
				{
					currentContext.NotifyDynamicSinks(message, true, false, false, true);
				}
			}
			IMethodReturnMessage methodReturnMessage = message as IMethodReturnMessage;
			if (message == null || methodReturnMessage == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadType"));
			}
			if (proxySinks != null)
			{
				DynamicPropertyHolder.NotifyDynamicSinks(message, proxySinks, true, false, false);
			}
			return message;
		}

		// Token: 0x0600585D RID: 22621 RVA: 0x00137854 File Offset: 0x00135A54
		[SecurityCritical]
		public override IMessage Invoke(IMessage reqMsg)
		{
			IConstructionCallMessage constructionCallMessage = reqMsg as IConstructionCallMessage;
			if (constructionCallMessage != null)
			{
				return this.InternalActivate(constructionCallMessage);
			}
			if (!base.Initialized)
			{
				if (this.CtorThread != Thread.CurrentThread.GetHashCode())
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_Proxy_InvalidCall"));
				}
				ServerIdentity serverIdentity = this.IdentityObject as ServerIdentity;
				RemotingServices.Wrap((ContextBoundObject)base.UnwrappedServerObject);
			}
			int callType = 0;
			Message message = reqMsg as Message;
			if (message != null)
			{
				callType = message.GetCallType();
			}
			return this.InternalInvoke((IMethodCallMessage)reqMsg, false, callType);
		}

		// Token: 0x0600585E RID: 22622 RVA: 0x001378E0 File Offset: 0x00135AE0
		internal virtual IMessage InternalInvoke(IMethodCallMessage reqMcmMsg, bool useDispatchMessage, int callType)
		{
			Message message = reqMcmMsg as Message;
			if (message == null && callType != 0)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Proxy_InvalidCallType"));
			}
			IMessage result = null;
			Thread currentThread = Thread.CurrentThread;
			LogicalCallContext logicalCallContext = currentThread.GetMutableExecutionContext().LogicalCallContext;
			Identity identityObject = this.IdentityObject;
			ServerIdentity serverIdentity = identityObject as ServerIdentity;
			if (serverIdentity != null && identityObject.IsFullyDisconnected())
			{
				throw new ArgumentException(Environment.GetResourceString("Remoting_ServerObjectNotFound", new object[] { reqMcmMsg.Uri }));
			}
			MethodBase methodBase = reqMcmMsg.MethodBase;
			if (RemotingProxy._getTypeMethod == methodBase)
			{
				Type proxiedType = base.GetProxiedType();
				return new ReturnMessage(proxiedType, null, 0, logicalCallContext, reqMcmMsg);
			}
			if (RemotingProxy._getHashCodeMethod == methodBase)
			{
				int hashCode = identityObject.GetHashCode();
				return new ReturnMessage(hashCode, null, 0, logicalCallContext, reqMcmMsg);
			}
			if (identityObject.ChannelSink == null)
			{
				IMessageSink chnlSink = null;
				IMessageSink envoySink = null;
				if (!identityObject.ObjectRef.IsObjRefLite())
				{
					RemotingServices.CreateEnvoyAndChannelSinks(null, identityObject.ObjectRef, out chnlSink, out envoySink);
				}
				else
				{
					RemotingServices.CreateEnvoyAndChannelSinks(identityObject.ObjURI, null, out chnlSink, out envoySink);
				}
				RemotingServices.SetEnvoyAndChannelSinks(identityObject, chnlSink, envoySink);
				if (identityObject.ChannelSink == null)
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_Proxy_NoChannelSink"));
				}
			}
			IInternalMessage internalMessage = (IInternalMessage)reqMcmMsg;
			internalMessage.IdentityObject = identityObject;
			if (serverIdentity != null)
			{
				internalMessage.ServerIdentityObject = serverIdentity;
			}
			else
			{
				internalMessage.SetURI(identityObject.URI);
			}
			switch (callType)
			{
			case 0:
			{
				bool bSkippingContextChain = false;
				Context currentContextInternal = currentThread.GetCurrentContextInternal();
				IMessageSink messageSink = identityObject.EnvoyChain;
				if (currentContextInternal.IsDefaultContext && messageSink is EnvoyTerminatorSink)
				{
					bSkippingContextChain = true;
					messageSink = identityObject.ChannelSink;
				}
				result = RemotingProxy.CallProcessMessage(messageSink, reqMcmMsg, identityObject.ProxySideDynamicSinks, currentThread, currentContextInternal, bSkippingContextChain);
				break;
			}
			case 1:
			case 9:
			{
				logicalCallContext = (LogicalCallContext)logicalCallContext.Clone();
				internalMessage.SetCallContext(logicalCallContext);
				AsyncResult asyncResult = new AsyncResult(message);
				this.InternalInvokeAsync(asyncResult, message, useDispatchMessage, callType);
				result = new ReturnMessage(asyncResult, null, 0, null, message);
				break;
			}
			case 2:
				result = RealProxy.EndInvokeHelper(message, true);
				break;
			case 8:
				logicalCallContext = (LogicalCallContext)logicalCallContext.Clone();
				internalMessage.SetCallContext(logicalCallContext);
				this.InternalInvokeAsync(null, message, useDispatchMessage, callType);
				result = new ReturnMessage(null, null, 0, null, reqMcmMsg);
				break;
			case 10:
				result = new ReturnMessage(null, null, 0, null, reqMcmMsg);
				break;
			}
			return result;
		}

		// Token: 0x0600585F RID: 22623 RVA: 0x00137B3C File Offset: 0x00135D3C
		internal void InternalInvokeAsync(IMessageSink ar, Message reqMsg, bool useDispatchMessage, int callType)
		{
			Identity identityObject = this.IdentityObject;
			ServerIdentity serverIdentity = identityObject as ServerIdentity;
			MethodCall methodCall = new MethodCall(reqMsg);
			IInternalMessage internalMessage = methodCall;
			internalMessage.IdentityObject = identityObject;
			if (serverIdentity != null)
			{
				internalMessage.ServerIdentityObject = serverIdentity;
			}
			if (useDispatchMessage)
			{
				IMessageCtrl messageCtrl = ChannelServices.AsyncDispatchMessage(methodCall, ((callType & 8) != 0) ? null : ar);
			}
			else
			{
				if (identityObject.EnvoyChain == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Remoting_Proxy_InvalidState"));
				}
				IMessageCtrl messageCtrl = identityObject.EnvoyChain.AsyncProcessMessage(methodCall, ((callType & 8) != 0) ? null : ar);
			}
			if ((callType & 1) != 0 && (callType & 8) != 0)
			{
				ar.SyncProcessMessage(null);
			}
		}

		// Token: 0x06005860 RID: 22624 RVA: 0x00137BD4 File Offset: 0x00135DD4
		private IConstructionReturnMessage InternalActivate(IConstructionCallMessage ctorMsg)
		{
			this.CtorThread = Thread.CurrentThread.GetHashCode();
			IConstructionReturnMessage result = ActivationServices.Activate(this, ctorMsg);
			base.Initialized = true;
			return result;
		}

		// Token: 0x06005861 RID: 22625 RVA: 0x00137C04 File Offset: 0x00135E04
		private static void Invoke(object NotUsed, ref MessageData msgData)
		{
			Message message = new Message();
			message.InitFields(msgData);
			object thisPtr = message.GetThisPtr();
			Delegate @delegate;
			if ((@delegate = thisPtr as Delegate) == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
			}
			RemotingProxy remotingProxy = (RemotingProxy)RemotingServices.GetRealProxy(@delegate.Target);
			if (remotingProxy != null)
			{
				remotingProxy.InternalInvoke(message, true, message.GetCallType());
				return;
			}
			int callType = message.GetCallType();
			if (callType <= 2)
			{
				if (callType != 1)
				{
					if (callType != 2)
					{
						return;
					}
					RealProxy.EndInvokeHelper(message, false);
					return;
				}
			}
			else if (callType != 9)
			{
				return;
			}
			message.Properties[Message.CallContextKey] = Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext.Clone();
			AsyncResult asyncResult = new AsyncResult(message);
			AgileAsyncWorkerItem state = new AgileAsyncWorkerItem(message, ((callType & 8) != 0) ? null : asyncResult, @delegate.Target);
			ThreadPool.QueueUserWorkItem(new WaitCallback(AgileAsyncWorkerItem.ThreadPoolCallBack), state);
			if ((callType & 8) != 0)
			{
				asyncResult.SyncProcessMessage(null);
			}
			message.PropagateOutParameters(null, asyncResult);
		}

		// Token: 0x17000EA5 RID: 3749
		// (get) Token: 0x06005862 RID: 22626 RVA: 0x00137D0D File Offset: 0x00135F0D
		// (set) Token: 0x06005863 RID: 22627 RVA: 0x00137D15 File Offset: 0x00135F15
		internal ConstructorCallMessage ConstructorMessage
		{
			get
			{
				return this._ccm;
			}
			set
			{
				this._ccm = value;
			}
		}

		// Token: 0x17000EA6 RID: 3750
		// (get) Token: 0x06005864 RID: 22628 RVA: 0x00137D1E File Offset: 0x00135F1E
		// (set) Token: 0x06005865 RID: 22629 RVA: 0x00137D2B File Offset: 0x00135F2B
		public string TypeName
		{
			[SecurityCritical]
			get
			{
				return base.GetProxiedType().FullName;
			}
			[SecurityCritical]
			set
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06005866 RID: 22630 RVA: 0x00137D34 File Offset: 0x00135F34
		[SecurityCritical]
		public override IntPtr GetCOMIUnknown(bool fIsBeingMarshalled)
		{
			IntPtr result = IntPtr.Zero;
			object transparentProxy = this.GetTransparentProxy();
			bool flag = RemotingServices.IsObjectOutOfProcess(transparentProxy);
			if (flag)
			{
				if (fIsBeingMarshalled)
				{
					result = MarshalByRefObject.GetComIUnknown((MarshalByRefObject)transparentProxy);
				}
				else
				{
					result = MarshalByRefObject.GetComIUnknown((MarshalByRefObject)transparentProxy);
				}
			}
			else
			{
				bool flag2 = RemotingServices.IsObjectOutOfAppDomain(transparentProxy);
				if (flag2)
				{
					result = ((MarshalByRefObject)transparentProxy).GetComIUnknown(fIsBeingMarshalled);
				}
				else
				{
					result = MarshalByRefObject.GetComIUnknown((MarshalByRefObject)transparentProxy);
				}
			}
			return result;
		}

		// Token: 0x06005867 RID: 22631 RVA: 0x00137D9D File Offset: 0x00135F9D
		[SecurityCritical]
		public override void SetCOMIUnknown(IntPtr i)
		{
		}

		// Token: 0x06005868 RID: 22632 RVA: 0x00137DA0 File Offset: 0x00135FA0
		[SecurityCritical]
		public bool CanCastTo(Type castType, object o)
		{
			if (castType == null)
			{
				throw new ArgumentNullException("castType");
			}
			RuntimeType runtimeType = castType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			bool flag = false;
			if (runtimeType == RemotingProxy.s_typeofObject || runtimeType == RemotingProxy.s_typeofMarshalByRefObject)
			{
				return true;
			}
			ObjRef objectRef = this.IdentityObject.ObjectRef;
			if (objectRef != null)
			{
				object transparentProxy = this.GetTransparentProxy();
				IRemotingTypeInfo typeInfo = objectRef.TypeInfo;
				if (typeInfo != null)
				{
					flag = typeInfo.CanCastTo(runtimeType, transparentProxy);
					if (!flag && typeInfo.GetType() == typeof(TypeInfo) && objectRef.IsWellKnown())
					{
						flag = this.CanCastToWK(runtimeType);
					}
				}
				else if (objectRef.IsObjRefLite())
				{
					flag = MarshalByRefObject.CanCastToXmlTypeHelper(runtimeType, (MarshalByRefObject)o);
				}
			}
			else
			{
				flag = this.CanCastToWK(runtimeType);
			}
			return flag;
		}

		// Token: 0x06005869 RID: 22633 RVA: 0x00137E78 File Offset: 0x00136078
		private bool CanCastToWK(Type castType)
		{
			bool result = false;
			if (castType.IsClass)
			{
				result = base.GetProxiedType().IsAssignableFrom(castType);
			}
			else if (!(this.IdentityObject is ServerIdentity))
			{
				result = true;
			}
			return result;
		}

		// Token: 0x04002849 RID: 10313
		private static MethodInfo _getTypeMethod = typeof(object).GetMethod("GetType");

		// Token: 0x0400284A RID: 10314
		private static MethodInfo _getHashCodeMethod = typeof(object).GetMethod("GetHashCode");

		// Token: 0x0400284B RID: 10315
		private static RuntimeType s_typeofObject = (RuntimeType)typeof(object);

		// Token: 0x0400284C RID: 10316
		private static RuntimeType s_typeofMarshalByRefObject = (RuntimeType)typeof(MarshalByRefObject);

		// Token: 0x0400284D RID: 10317
		private ConstructorCallMessage _ccm;

		// Token: 0x0400284E RID: 10318
		private int _ctorThread;
	}
}
