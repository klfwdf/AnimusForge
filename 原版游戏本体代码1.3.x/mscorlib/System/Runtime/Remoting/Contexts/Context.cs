using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000809 RID: 2057
	[ComVisible(true)]
	public class Context
	{
		// Token: 0x06005884 RID: 22660 RVA: 0x00138353 File Offset: 0x00136553
		[SecurityCritical]
		public Context()
			: this(0)
		{
		}

		// Token: 0x06005885 RID: 22661 RVA: 0x0013835C File Offset: 0x0013655C
		[SecurityCritical]
		private Context(int flags)
		{
			this._ctxFlags = flags;
			if ((this._ctxFlags & 1) != 0)
			{
				this._ctxID = 0;
			}
			else
			{
				this._ctxID = Interlocked.Increment(ref Context._ctxIDCounter);
			}
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			if (remotingData != null)
			{
				IContextProperty[] appDomainContextProperties = remotingData.AppDomainContextProperties;
				if (appDomainContextProperties != null)
				{
					for (int i = 0; i < appDomainContextProperties.Length; i++)
					{
						this.SetProperty(appDomainContextProperties[i]);
					}
				}
			}
			if ((this._ctxFlags & 1) != 0)
			{
				this.Freeze();
			}
			this.SetupInternalContext((this._ctxFlags & 1) == 1);
		}

		// Token: 0x06005886 RID: 22662
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetupInternalContext(bool bDefault);

		// Token: 0x06005887 RID: 22663 RVA: 0x001383EC File Offset: 0x001365EC
		[SecuritySafeCritical]
		~Context()
		{
			if (this._internalContext != IntPtr.Zero && (this._ctxFlags & 1) == 0)
			{
				this.CleanupInternalContext();
			}
		}

		// Token: 0x06005888 RID: 22664
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void CleanupInternalContext();

		// Token: 0x17000EA9 RID: 3753
		// (get) Token: 0x06005889 RID: 22665 RVA: 0x00138434 File Offset: 0x00136634
		public virtual int ContextID
		{
			[SecurityCritical]
			get
			{
				return this._ctxID;
			}
		}

		// Token: 0x17000EAA RID: 3754
		// (get) Token: 0x0600588A RID: 22666 RVA: 0x0013843C File Offset: 0x0013663C
		internal virtual IntPtr InternalContextID
		{
			get
			{
				return this._internalContext;
			}
		}

		// Token: 0x17000EAB RID: 3755
		// (get) Token: 0x0600588B RID: 22667 RVA: 0x00138444 File Offset: 0x00136644
		internal virtual AppDomain AppDomain
		{
			get
			{
				return this._appDomain;
			}
		}

		// Token: 0x17000EAC RID: 3756
		// (get) Token: 0x0600588C RID: 22668 RVA: 0x0013844C File Offset: 0x0013664C
		internal bool IsDefaultContext
		{
			get
			{
				return this._ctxID == 0;
			}
		}

		// Token: 0x17000EAD RID: 3757
		// (get) Token: 0x0600588D RID: 22669 RVA: 0x00138457 File Offset: 0x00136657
		public static Context DefaultContext
		{
			[SecurityCritical]
			get
			{
				return Thread.GetDomain().GetDefaultContext();
			}
		}

		// Token: 0x0600588E RID: 22670 RVA: 0x00138463 File Offset: 0x00136663
		[SecurityCritical]
		internal static Context CreateDefaultContext()
		{
			return new Context(1);
		}

		// Token: 0x0600588F RID: 22671 RVA: 0x0013846C File Offset: 0x0013666C
		[SecurityCritical]
		public virtual IContextProperty GetProperty(string name)
		{
			if (this._ctxProps == null || name == null)
			{
				return null;
			}
			IContextProperty result = null;
			for (int i = 0; i < this._numCtxProps; i++)
			{
				if (this._ctxProps[i].Name.Equals(name))
				{
					result = this._ctxProps[i];
					break;
				}
			}
			return result;
		}

		// Token: 0x06005890 RID: 22672 RVA: 0x001384BC File Offset: 0x001366BC
		[SecurityCritical]
		public virtual void SetProperty(IContextProperty prop)
		{
			if (prop == null || prop.Name == null)
			{
				throw new ArgumentNullException((prop == null) ? "prop" : "property name");
			}
			if ((this._ctxFlags & 2) != 0)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AddContextFrozen"));
			}
			lock (this)
			{
				Context.CheckPropertyNameClash(prop.Name, this._ctxProps, this._numCtxProps);
				if (this._ctxProps == null || this._numCtxProps == this._ctxProps.Length)
				{
					this._ctxProps = Context.GrowPropertiesArray(this._ctxProps);
				}
				IContextProperty[] ctxProps = this._ctxProps;
				int numCtxProps = this._numCtxProps;
				this._numCtxProps = numCtxProps + 1;
				ctxProps[numCtxProps] = prop;
			}
		}

		// Token: 0x06005891 RID: 22673 RVA: 0x00138584 File Offset: 0x00136784
		[SecurityCritical]
		internal virtual void InternalFreeze()
		{
			this._ctxFlags |= 2;
			for (int i = 0; i < this._numCtxProps; i++)
			{
				this._ctxProps[i].Freeze(this);
			}
		}

		// Token: 0x06005892 RID: 22674 RVA: 0x001385C0 File Offset: 0x001367C0
		[SecurityCritical]
		public virtual void Freeze()
		{
			lock (this)
			{
				if ((this._ctxFlags & 2) != 0)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ContextAlreadyFrozen"));
				}
				this.InternalFreeze();
			}
		}

		// Token: 0x06005893 RID: 22675 RVA: 0x00138618 File Offset: 0x00136818
		internal virtual void SetThreadPoolAware()
		{
			this._ctxFlags |= 4;
		}

		// Token: 0x17000EAE RID: 3758
		// (get) Token: 0x06005894 RID: 22676 RVA: 0x00138628 File Offset: 0x00136828
		internal virtual bool IsThreadPoolAware
		{
			get
			{
				return (this._ctxFlags & 4) == 4;
			}
		}

		// Token: 0x17000EAF RID: 3759
		// (get) Token: 0x06005895 RID: 22677 RVA: 0x00138638 File Offset: 0x00136838
		public virtual IContextProperty[] ContextProperties
		{
			[SecurityCritical]
			get
			{
				if (this._ctxProps == null)
				{
					return null;
				}
				IContextProperty[] result;
				lock (this)
				{
					IContextProperty[] array = new IContextProperty[this._numCtxProps];
					Array.Copy(this._ctxProps, array, this._numCtxProps);
					result = array;
				}
				return result;
			}
		}

		// Token: 0x06005896 RID: 22678 RVA: 0x00138698 File Offset: 0x00136898
		[SecurityCritical]
		internal static void CheckPropertyNameClash(string name, IContextProperty[] props, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (props[i].Name.Equals(name))
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DuplicatePropertyName"));
				}
			}
		}

		// Token: 0x06005897 RID: 22679 RVA: 0x001386D4 File Offset: 0x001368D4
		internal static IContextProperty[] GrowPropertiesArray(IContextProperty[] props)
		{
			int num = ((props != null) ? props.Length : 0) + 8;
			IContextProperty[] array = new IContextProperty[num];
			if (props != null)
			{
				Array.Copy(props, array, props.Length);
			}
			return array;
		}

		// Token: 0x06005898 RID: 22680 RVA: 0x00138704 File Offset: 0x00136904
		[SecurityCritical]
		internal virtual IMessageSink GetServerContextChain()
		{
			if (this._serverContextChain == null)
			{
				IMessageSink messageSink = ServerContextTerminatorSink.MessageSink;
				int numCtxProps = this._numCtxProps;
				while (numCtxProps-- > 0)
				{
					object obj = this._ctxProps[numCtxProps];
					IContributeServerContextSink contributeServerContextSink = obj as IContributeServerContextSink;
					if (contributeServerContextSink != null)
					{
						messageSink = contributeServerContextSink.GetServerContextSink(messageSink);
						if (messageSink == null)
						{
							throw new RemotingException(Environment.GetResourceString("Remoting_Contexts_BadProperty"));
						}
					}
				}
				lock (this)
				{
					if (this._serverContextChain == null)
					{
						this._serverContextChain = messageSink;
					}
				}
			}
			return this._serverContextChain;
		}

		// Token: 0x06005899 RID: 22681 RVA: 0x001387A4 File Offset: 0x001369A4
		[SecurityCritical]
		internal virtual IMessageSink GetClientContextChain()
		{
			if (this._clientContextChain == null)
			{
				IMessageSink messageSink = ClientContextTerminatorSink.MessageSink;
				for (int i = 0; i < this._numCtxProps; i++)
				{
					object obj = this._ctxProps[i];
					IContributeClientContextSink contributeClientContextSink = obj as IContributeClientContextSink;
					if (contributeClientContextSink != null)
					{
						messageSink = contributeClientContextSink.GetClientContextSink(messageSink);
						if (messageSink == null)
						{
							throw new RemotingException(Environment.GetResourceString("Remoting_Contexts_BadProperty"));
						}
					}
				}
				lock (this)
				{
					if (this._clientContextChain == null)
					{
						this._clientContextChain = messageSink;
					}
				}
			}
			return this._clientContextChain;
		}

		// Token: 0x0600589A RID: 22682 RVA: 0x00138844 File Offset: 0x00136A44
		[SecurityCritical]
		internal virtual IMessageSink CreateServerObjectChain(MarshalByRefObject serverObj)
		{
			IMessageSink messageSink = new ServerObjectTerminatorSink(serverObj);
			int numCtxProps = this._numCtxProps;
			while (numCtxProps-- > 0)
			{
				object obj = this._ctxProps[numCtxProps];
				IContributeObjectSink contributeObjectSink = obj as IContributeObjectSink;
				if (contributeObjectSink != null)
				{
					messageSink = contributeObjectSink.GetObjectSink(serverObj, messageSink);
					if (messageSink == null)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Contexts_BadProperty"));
					}
				}
			}
			return messageSink;
		}

		// Token: 0x0600589B RID: 22683 RVA: 0x0013889C File Offset: 0x00136A9C
		[SecurityCritical]
		internal virtual IMessageSink CreateEnvoyChain(MarshalByRefObject objectOrProxy)
		{
			IMessageSink messageSink = EnvoyTerminatorSink.MessageSink;
			for (int i = 0; i < this._numCtxProps; i++)
			{
				object obj = this._ctxProps[i];
				IContributeEnvoySink contributeEnvoySink = obj as IContributeEnvoySink;
				if (contributeEnvoySink != null)
				{
					messageSink = contributeEnvoySink.GetEnvoySink(objectOrProxy, messageSink);
					if (messageSink == null)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Contexts_BadProperty"));
					}
				}
			}
			return messageSink;
		}

		// Token: 0x0600589C RID: 22684 RVA: 0x001388F8 File Offset: 0x00136AF8
		[SecurityCritical]
		internal IMessage NotifyActivatorProperties(IMessage msg, bool bServerSide)
		{
			IMessage message = null;
			try
			{
				int numCtxProps = this._numCtxProps;
				while (numCtxProps-- != 0)
				{
					object obj = this._ctxProps[numCtxProps];
					IContextPropertyActivator contextPropertyActivator = obj as IContextPropertyActivator;
					if (contextPropertyActivator != null)
					{
						IConstructionCallMessage constructionCallMessage = msg as IConstructionCallMessage;
						if (constructionCallMessage != null)
						{
							if (!bServerSide)
							{
								contextPropertyActivator.CollectFromClientContext(constructionCallMessage);
							}
							else
							{
								contextPropertyActivator.DeliverClientContextToServerContext(constructionCallMessage);
							}
						}
						else if (bServerSide)
						{
							contextPropertyActivator.CollectFromServerContext((IConstructionReturnMessage)msg);
						}
						else
						{
							contextPropertyActivator.DeliverServerContextToClientContext((IConstructionReturnMessage)msg);
						}
					}
				}
			}
			catch (Exception e)
			{
				IMethodCallMessage mcm;
				if (msg is IConstructionCallMessage)
				{
					mcm = (IMethodCallMessage)msg;
				}
				else
				{
					mcm = new ErrorMessage();
				}
				message = new ReturnMessage(e, mcm);
				if (msg != null)
				{
					((ReturnMessage)message).SetLogicalCallContext((LogicalCallContext)msg.Properties[Message.CallContextKey]);
				}
			}
			return message;
		}

		// Token: 0x0600589D RID: 22685 RVA: 0x001389D0 File Offset: 0x00136BD0
		public override string ToString()
		{
			return "ContextID: " + this._ctxID.ToString();
		}

		// Token: 0x0600589E RID: 22686 RVA: 0x001389E8 File Offset: 0x00136BE8
		[SecurityCritical]
		public void DoCallBack(CrossContextDelegate deleg)
		{
			if (deleg == null)
			{
				throw new ArgumentNullException("deleg");
			}
			if ((this._ctxFlags & 2) == 0)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Contexts_ContextNotFrozenForCallBack"));
			}
			Context currentContext = Thread.CurrentContext;
			if (currentContext == this)
			{
				deleg();
				return;
			}
			currentContext.DoCallBackGeneric(this.InternalContextID, deleg);
			GC.KeepAlive(this);
		}

		// Token: 0x0600589F RID: 22687 RVA: 0x00138A44 File Offset: 0x00136C44
		[SecurityCritical]
		internal static void DoCallBackFromEE(IntPtr targetCtxID, IntPtr privateData, int targetDomainID)
		{
			if (targetDomainID == 0)
			{
				CallBackHelper @object = new CallBackHelper(privateData, true, targetDomainID);
				CrossContextDelegate deleg = new CrossContextDelegate(@object.Func);
				Thread.CurrentContext.DoCallBackGeneric(targetCtxID, deleg);
				return;
			}
			TransitionCall msg = new TransitionCall(targetCtxID, privateData, targetDomainID);
			Message.PropagateCallContextFromThreadToMessage(msg);
			IMessage message = Thread.CurrentContext.GetClientContextChain().SyncProcessMessage(msg);
			Message.PropagateCallContextFromMessageToThread(message);
			IMethodReturnMessage methodReturnMessage = message as IMethodReturnMessage;
			if (methodReturnMessage != null && methodReturnMessage.Exception != null)
			{
				throw methodReturnMessage.Exception;
			}
		}

		// Token: 0x060058A0 RID: 22688 RVA: 0x00138ABC File Offset: 0x00136CBC
		[SecurityCritical]
		internal void DoCallBackGeneric(IntPtr targetCtxID, CrossContextDelegate deleg)
		{
			TransitionCall msg = new TransitionCall(targetCtxID, deleg);
			Message.PropagateCallContextFromThreadToMessage(msg);
			IMessage message = this.GetClientContextChain().SyncProcessMessage(msg);
			if (message != null)
			{
				Message.PropagateCallContextFromMessageToThread(message);
			}
			IMethodReturnMessage methodReturnMessage = message as IMethodReturnMessage;
			if (methodReturnMessage != null && methodReturnMessage.Exception != null)
			{
				throw methodReturnMessage.Exception;
			}
		}

		// Token: 0x060058A1 RID: 22689
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ExecuteCallBackInEE(IntPtr privateData);

		// Token: 0x17000EB0 RID: 3760
		// (get) Token: 0x060058A2 RID: 22690 RVA: 0x00138B08 File Offset: 0x00136D08
		private LocalDataStore MyLocalStore
		{
			get
			{
				if (this._localDataStore == null)
				{
					LocalDataStoreMgr localDataStoreMgr = Context._localDataStoreMgr;
					lock (localDataStoreMgr)
					{
						if (this._localDataStore == null)
						{
							this._localDataStore = Context._localDataStoreMgr.CreateLocalDataStore();
						}
					}
				}
				return this._localDataStore.Store;
			}
		}

		// Token: 0x060058A3 RID: 22691 RVA: 0x00138B74 File Offset: 0x00136D74
		[SecurityCritical]
		public static LocalDataStoreSlot AllocateDataSlot()
		{
			return Context._localDataStoreMgr.AllocateDataSlot();
		}

		// Token: 0x060058A4 RID: 22692 RVA: 0x00138B80 File Offset: 0x00136D80
		[SecurityCritical]
		public static LocalDataStoreSlot AllocateNamedDataSlot(string name)
		{
			return Context._localDataStoreMgr.AllocateNamedDataSlot(name);
		}

		// Token: 0x060058A5 RID: 22693 RVA: 0x00138B8D File Offset: 0x00136D8D
		[SecurityCritical]
		public static LocalDataStoreSlot GetNamedDataSlot(string name)
		{
			return Context._localDataStoreMgr.GetNamedDataSlot(name);
		}

		// Token: 0x060058A6 RID: 22694 RVA: 0x00138B9A File Offset: 0x00136D9A
		[SecurityCritical]
		public static void FreeNamedDataSlot(string name)
		{
			Context._localDataStoreMgr.FreeNamedDataSlot(name);
		}

		// Token: 0x060058A7 RID: 22695 RVA: 0x00138BA7 File Offset: 0x00136DA7
		[SecurityCritical]
		public static void SetData(LocalDataStoreSlot slot, object data)
		{
			Thread.CurrentContext.MyLocalStore.SetData(slot, data);
		}

		// Token: 0x060058A8 RID: 22696 RVA: 0x00138BBA File Offset: 0x00136DBA
		[SecurityCritical]
		public static object GetData(LocalDataStoreSlot slot)
		{
			return Thread.CurrentContext.MyLocalStore.GetData(slot);
		}

		// Token: 0x060058A9 RID: 22697 RVA: 0x00138BCC File Offset: 0x00136DCC
		private int ReserveSlot()
		{
			if (this._ctxStatics == null)
			{
				this._ctxStatics = new object[8];
				this._ctxStatics[0] = null;
				this._ctxStaticsFreeIndex = 1;
				this._ctxStaticsCurrentBucket = 0;
			}
			if (this._ctxStaticsFreeIndex == 8)
			{
				object[] array = new object[8];
				object[] array2 = this._ctxStatics;
				while (array2[0] != null)
				{
					array2 = (object[])array2[0];
				}
				array2[0] = array;
				this._ctxStaticsFreeIndex = 1;
				this._ctxStaticsCurrentBucket++;
			}
			int ctxStaticsFreeIndex = this._ctxStaticsFreeIndex;
			this._ctxStaticsFreeIndex = ctxStaticsFreeIndex + 1;
			return ctxStaticsFreeIndex | (this._ctxStaticsCurrentBucket << 16);
		}

		// Token: 0x060058AA RID: 22698 RVA: 0x00138C60 File Offset: 0x00136E60
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
		public static bool RegisterDynamicProperty(IDynamicProperty prop, ContextBoundObject obj, Context ctx)
		{
			if (prop == null || prop.Name == null || !(prop is IContributeDynamicSink))
			{
				throw new ArgumentNullException("prop");
			}
			if (obj != null && ctx != null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NonNullObjAndCtx"));
			}
			bool result;
			if (obj != null)
			{
				result = IdentityHolder.AddDynamicProperty(obj, prop);
			}
			else
			{
				result = Context.AddDynamicProperty(ctx, prop);
			}
			return result;
		}

		// Token: 0x060058AB RID: 22699 RVA: 0x00138CBC File Offset: 0x00136EBC
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
		public static bool UnregisterDynamicProperty(string name, ContextBoundObject obj, Context ctx)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (obj != null && ctx != null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NonNullObjAndCtx"));
			}
			bool result;
			if (obj != null)
			{
				result = IdentityHolder.RemoveDynamicProperty(obj, name);
			}
			else
			{
				result = Context.RemoveDynamicProperty(ctx, name);
			}
			return result;
		}

		// Token: 0x060058AC RID: 22700 RVA: 0x00138D05 File Offset: 0x00136F05
		[SecurityCritical]
		internal static bool AddDynamicProperty(Context ctx, IDynamicProperty prop)
		{
			if (ctx != null)
			{
				return ctx.AddPerContextDynamicProperty(prop);
			}
			return Context.AddGlobalDynamicProperty(prop);
		}

		// Token: 0x060058AD RID: 22701 RVA: 0x00138D18 File Offset: 0x00136F18
		[SecurityCritical]
		private bool AddPerContextDynamicProperty(IDynamicProperty prop)
		{
			if (this._dphCtx == null)
			{
				DynamicPropertyHolder dphCtx = new DynamicPropertyHolder();
				lock (this)
				{
					if (this._dphCtx == null)
					{
						this._dphCtx = dphCtx;
					}
				}
			}
			return this._dphCtx.AddDynamicProperty(prop);
		}

		// Token: 0x060058AE RID: 22702 RVA: 0x00138D78 File Offset: 0x00136F78
		[SecurityCritical]
		private static bool AddGlobalDynamicProperty(IDynamicProperty prop)
		{
			return Context._dphGlobal.AddDynamicProperty(prop);
		}

		// Token: 0x060058AF RID: 22703 RVA: 0x00138D85 File Offset: 0x00136F85
		[SecurityCritical]
		internal static bool RemoveDynamicProperty(Context ctx, string name)
		{
			if (ctx != null)
			{
				return ctx.RemovePerContextDynamicProperty(name);
			}
			return Context.RemoveGlobalDynamicProperty(name);
		}

		// Token: 0x060058B0 RID: 22704 RVA: 0x00138D98 File Offset: 0x00136F98
		[SecurityCritical]
		private bool RemovePerContextDynamicProperty(string name)
		{
			if (this._dphCtx == null)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Contexts_NoProperty"), name));
			}
			return this._dphCtx.RemoveDynamicProperty(name);
		}

		// Token: 0x060058B1 RID: 22705 RVA: 0x00138DC9 File Offset: 0x00136FC9
		[SecurityCritical]
		private static bool RemoveGlobalDynamicProperty(string name)
		{
			return Context._dphGlobal.RemoveDynamicProperty(name);
		}

		// Token: 0x17000EB1 RID: 3761
		// (get) Token: 0x060058B2 RID: 22706 RVA: 0x00138DD6 File Offset: 0x00136FD6
		internal virtual IDynamicProperty[] PerContextDynamicProperties
		{
			get
			{
				if (this._dphCtx == null)
				{
					return null;
				}
				return this._dphCtx.DynamicProperties;
			}
		}

		// Token: 0x17000EB2 RID: 3762
		// (get) Token: 0x060058B3 RID: 22707 RVA: 0x00138DED File Offset: 0x00136FED
		internal static ArrayWithSize GlobalDynamicSinks
		{
			[SecurityCritical]
			get
			{
				return Context._dphGlobal.DynamicSinks;
			}
		}

		// Token: 0x17000EB3 RID: 3763
		// (get) Token: 0x060058B4 RID: 22708 RVA: 0x00138DF9 File Offset: 0x00136FF9
		internal virtual ArrayWithSize DynamicSinks
		{
			[SecurityCritical]
			get
			{
				if (this._dphCtx == null)
				{
					return null;
				}
				return this._dphCtx.DynamicSinks;
			}
		}

		// Token: 0x060058B5 RID: 22709 RVA: 0x00138E10 File Offset: 0x00137010
		[SecurityCritical]
		internal virtual bool NotifyDynamicSinks(IMessage msg, bool bCliSide, bool bStart, bool bAsync, bool bNotifyGlobals)
		{
			bool result = false;
			if (bNotifyGlobals && Context._dphGlobal.DynamicProperties != null)
			{
				ArrayWithSize globalDynamicSinks = Context.GlobalDynamicSinks;
				if (globalDynamicSinks != null)
				{
					DynamicPropertyHolder.NotifyDynamicSinks(msg, globalDynamicSinks, bCliSide, bStart, bAsync);
					result = true;
				}
			}
			ArrayWithSize dynamicSinks = this.DynamicSinks;
			if (dynamicSinks != null)
			{
				DynamicPropertyHolder.NotifyDynamicSinks(msg, dynamicSinks, bCliSide, bStart, bAsync);
				result = true;
			}
			return result;
		}

		// Token: 0x0400285A RID: 10330
		internal const int CTX_DEFAULT_CONTEXT = 1;

		// Token: 0x0400285B RID: 10331
		internal const int CTX_FROZEN = 2;

		// Token: 0x0400285C RID: 10332
		internal const int CTX_THREADPOOL_AWARE = 4;

		// Token: 0x0400285D RID: 10333
		private const int GROW_BY = 8;

		// Token: 0x0400285E RID: 10334
		private const int STATICS_BUCKET_SIZE = 8;

		// Token: 0x0400285F RID: 10335
		private IContextProperty[] _ctxProps;

		// Token: 0x04002860 RID: 10336
		private DynamicPropertyHolder _dphCtx;

		// Token: 0x04002861 RID: 10337
		private volatile LocalDataStoreHolder _localDataStore;

		// Token: 0x04002862 RID: 10338
		private IMessageSink _serverContextChain;

		// Token: 0x04002863 RID: 10339
		private IMessageSink _clientContextChain;

		// Token: 0x04002864 RID: 10340
		private AppDomain _appDomain;

		// Token: 0x04002865 RID: 10341
		private object[] _ctxStatics;

		// Token: 0x04002866 RID: 10342
		private IntPtr _internalContext;

		// Token: 0x04002867 RID: 10343
		private int _ctxID;

		// Token: 0x04002868 RID: 10344
		private int _ctxFlags;

		// Token: 0x04002869 RID: 10345
		private int _numCtxProps;

		// Token: 0x0400286A RID: 10346
		private int _ctxStaticsCurrentBucket;

		// Token: 0x0400286B RID: 10347
		private int _ctxStaticsFreeIndex;

		// Token: 0x0400286C RID: 10348
		private static DynamicPropertyHolder _dphGlobal = new DynamicPropertyHolder();

		// Token: 0x0400286D RID: 10349
		private static LocalDataStoreMgr _localDataStoreMgr = new LocalDataStoreMgr();

		// Token: 0x0400286E RID: 10350
		private static int _ctxIDCounter = 0;
	}
}
