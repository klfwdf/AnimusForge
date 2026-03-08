using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007CC RID: 1996
	internal class ServerIdentity : Identity
	{
		// Token: 0x0600568F RID: 22159 RVA: 0x001332FC File Offset: 0x001314FC
		internal Type GetLastCalledType(string newTypeName)
		{
			ServerIdentity.LastCalledType lastCalledType = this._lastCalledType;
			if (lastCalledType == null)
			{
				return null;
			}
			string typeName = lastCalledType.typeName;
			Type type = lastCalledType.type;
			if (typeName == null || type == null)
			{
				return null;
			}
			if (typeName.Equals(newTypeName))
			{
				return type;
			}
			return null;
		}

		// Token: 0x06005690 RID: 22160 RVA: 0x00133340 File Offset: 0x00131540
		internal void SetLastCalledType(string newTypeName, Type newType)
		{
			this._lastCalledType = new ServerIdentity.LastCalledType
			{
				typeName = newTypeName,
				type = newType
			};
		}

		// Token: 0x06005691 RID: 22161 RVA: 0x00133368 File Offset: 0x00131568
		[SecurityCritical]
		internal void SetHandle()
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(this, ref flag);
				if (!this._srvIdentityHandle.IsAllocated)
				{
					this._srvIdentityHandle = new GCHandle(this, GCHandleType.Normal);
				}
				else
				{
					this._srvIdentityHandle.Target = this;
				}
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}

		// Token: 0x06005692 RID: 22162 RVA: 0x001333C8 File Offset: 0x001315C8
		[SecurityCritical]
		internal void ResetHandle()
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(this, ref flag);
				this._srvIdentityHandle.Target = null;
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}

		// Token: 0x06005693 RID: 22163 RVA: 0x0013340C File Offset: 0x0013160C
		internal GCHandle GetHandle()
		{
			return this._srvIdentityHandle;
		}

		// Token: 0x06005694 RID: 22164 RVA: 0x00133414 File Offset: 0x00131614
		[SecurityCritical]
		internal ServerIdentity(MarshalByRefObject obj, Context serverCtx)
			: base(obj is ContextBoundObject)
		{
			if (obj != null)
			{
				if (!RemotingServices.IsTransparentProxy(obj))
				{
					this._srvType = obj.GetType();
				}
				else
				{
					RealProxy realProxy = RemotingServices.GetRealProxy(obj);
					this._srvType = realProxy.GetProxiedType();
				}
			}
			this._srvCtx = serverCtx;
			this._serverObjectChain = null;
			this._stackBuilderSink = null;
		}

		// Token: 0x06005695 RID: 22165 RVA: 0x00133471 File Offset: 0x00131671
		[SecurityCritical]
		internal ServerIdentity(MarshalByRefObject obj, Context serverCtx, string uri)
			: this(obj, serverCtx)
		{
			base.SetOrCreateURI(uri, true);
		}

		// Token: 0x17000E37 RID: 3639
		// (get) Token: 0x06005696 RID: 22166 RVA: 0x00133483 File Offset: 0x00131683
		internal Context ServerContext
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this._srvCtx;
			}
		}

		// Token: 0x06005697 RID: 22167 RVA: 0x0013348B File Offset: 0x0013168B
		internal void SetSingleCallObjectMode()
		{
			this._flags |= 512;
		}

		// Token: 0x06005698 RID: 22168 RVA: 0x0013349F File Offset: 0x0013169F
		internal void SetSingletonObjectMode()
		{
			this._flags |= 1024;
		}

		// Token: 0x06005699 RID: 22169 RVA: 0x001334B3 File Offset: 0x001316B3
		internal bool IsSingleCall()
		{
			return (this._flags & 512) != 0;
		}

		// Token: 0x0600569A RID: 22170 RVA: 0x001334C4 File Offset: 0x001316C4
		internal bool IsSingleton()
		{
			return (this._flags & 1024) != 0;
		}

		// Token: 0x0600569B RID: 22171 RVA: 0x001334D8 File Offset: 0x001316D8
		[SecurityCritical]
		internal IMessageSink GetServerObjectChain(out MarshalByRefObject obj)
		{
			obj = null;
			if (!this.IsSingleCall())
			{
				if (this._serverObjectChain == null)
				{
					bool flag = false;
					RuntimeHelpers.PrepareConstrainedRegions();
					try
					{
						Monitor.Enter(this, ref flag);
						if (this._serverObjectChain == null)
						{
							MarshalByRefObject tporObject = base.TPOrObject;
							this._serverObjectChain = this._srvCtx.CreateServerObjectChain(tporObject);
						}
					}
					finally
					{
						if (flag)
						{
							Monitor.Exit(this);
						}
					}
				}
				return this._serverObjectChain;
			}
			MarshalByRefObject marshalByRefObject;
			IMessageSink messageSink;
			if (this._tpOrObject != null && this._firstCallDispatched == 0 && Interlocked.CompareExchange(ref this._firstCallDispatched, 1, 0) == 0)
			{
				marshalByRefObject = (MarshalByRefObject)this._tpOrObject;
				messageSink = this._serverObjectChain;
				if (messageSink == null)
				{
					messageSink = this._srvCtx.CreateServerObjectChain(marshalByRefObject);
				}
			}
			else
			{
				marshalByRefObject = (MarshalByRefObject)Activator.CreateInstance(this._srvType, true);
				string objectUri = RemotingServices.GetObjectUri(marshalByRefObject);
				if (objectUri != null)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_WellKnown_CtorCantMarshal"), base.URI));
				}
				if (!RemotingServices.IsTransparentProxy(marshalByRefObject))
				{
					marshalByRefObject.__RaceSetServerIdentity(this);
				}
				else
				{
					RealProxy realProxy = RemotingServices.GetRealProxy(marshalByRefObject);
					realProxy.IdentityObject = this;
				}
				messageSink = this._srvCtx.CreateServerObjectChain(marshalByRefObject);
			}
			obj = marshalByRefObject;
			return messageSink;
		}

		// Token: 0x17000E38 RID: 3640
		// (get) Token: 0x0600569C RID: 22172 RVA: 0x00133608 File Offset: 0x00131808
		// (set) Token: 0x0600569D RID: 22173 RVA: 0x00133610 File Offset: 0x00131810
		internal Type ServerType
		{
			get
			{
				return this._srvType;
			}
			set
			{
				this._srvType = value;
			}
		}

		// Token: 0x17000E39 RID: 3641
		// (get) Token: 0x0600569E RID: 22174 RVA: 0x00133619 File Offset: 0x00131819
		// (set) Token: 0x0600569F RID: 22175 RVA: 0x00133621 File Offset: 0x00131821
		internal bool MarshaledAsSpecificType
		{
			get
			{
				return this._bMarshaledAsSpecificType;
			}
			set
			{
				this._bMarshaledAsSpecificType = value;
			}
		}

		// Token: 0x060056A0 RID: 22176 RVA: 0x0013362C File Offset: 0x0013182C
		[SecurityCritical]
		internal IMessageSink RaceSetServerObjectChain(IMessageSink serverObjectChain)
		{
			if (this._serverObjectChain == null)
			{
				bool flag = false;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					Monitor.Enter(this, ref flag);
					if (this._serverObjectChain == null)
					{
						this._serverObjectChain = serverObjectChain;
					}
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(this);
					}
				}
			}
			return this._serverObjectChain;
		}

		// Token: 0x060056A1 RID: 22177 RVA: 0x00133684 File Offset: 0x00131884
		[SecurityCritical]
		internal bool AddServerSideDynamicProperty(IDynamicProperty prop)
		{
			if (this._dphSrv == null)
			{
				DynamicPropertyHolder dphSrv = new DynamicPropertyHolder();
				bool flag = false;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					Monitor.Enter(this, ref flag);
					if (this._dphSrv == null)
					{
						this._dphSrv = dphSrv;
					}
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(this);
					}
				}
			}
			return this._dphSrv.AddDynamicProperty(prop);
		}

		// Token: 0x060056A2 RID: 22178 RVA: 0x001336E8 File Offset: 0x001318E8
		[SecurityCritical]
		internal bool RemoveServerSideDynamicProperty(string name)
		{
			if (this._dphSrv == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_PropNotFound"));
			}
			return this._dphSrv.RemoveDynamicProperty(name);
		}

		// Token: 0x17000E3A RID: 3642
		// (get) Token: 0x060056A3 RID: 22179 RVA: 0x0013370E File Offset: 0x0013190E
		internal ArrayWithSize ServerSideDynamicSinks
		{
			[SecurityCritical]
			get
			{
				if (this._dphSrv == null)
				{
					return null;
				}
				return this._dphSrv.DynamicSinks;
			}
		}

		// Token: 0x060056A4 RID: 22180 RVA: 0x00133725 File Offset: 0x00131925
		[SecurityCritical]
		internal override void AssertValid()
		{
			if (base.TPOrObject != null)
			{
				RemotingServices.IsTransparentProxy(base.TPOrObject);
			}
		}

		// Token: 0x040027A1 RID: 10145
		internal Context _srvCtx;

		// Token: 0x040027A2 RID: 10146
		internal IMessageSink _serverObjectChain;

		// Token: 0x040027A3 RID: 10147
		internal StackBuilderSink _stackBuilderSink;

		// Token: 0x040027A4 RID: 10148
		internal DynamicPropertyHolder _dphSrv;

		// Token: 0x040027A5 RID: 10149
		internal Type _srvType;

		// Token: 0x040027A6 RID: 10150
		private ServerIdentity.LastCalledType _lastCalledType;

		// Token: 0x040027A7 RID: 10151
		internal bool _bMarshaledAsSpecificType;

		// Token: 0x040027A8 RID: 10152
		internal int _firstCallDispatched;

		// Token: 0x040027A9 RID: 10153
		internal GCHandle _srvIdentityHandle;

		// Token: 0x02000C6B RID: 3179
		private class LastCalledType
		{
			// Token: 0x040037DF RID: 14303
			public string typeName;

			// Token: 0x040037E0 RID: 14304
			public Type type;
		}
	}
}
