using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting
{
	// Token: 0x020007BC RID: 1980
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	[Serializable]
	public class ObjRef : IObjectReference, ISerializable
	{
		// Token: 0x060055A3 RID: 21923 RVA: 0x001301D2 File Offset: 0x0012E3D2
		internal void SetServerIdentity(GCHandle hndSrvIdentity)
		{
			this.srvIdentity = hndSrvIdentity;
		}

		// Token: 0x060055A4 RID: 21924 RVA: 0x001301DB File Offset: 0x0012E3DB
		internal GCHandle GetServerIdentity()
		{
			return this.srvIdentity;
		}

		// Token: 0x060055A5 RID: 21925 RVA: 0x001301E3 File Offset: 0x0012E3E3
		internal void SetDomainID(int id)
		{
			this.domainID = id;
		}

		// Token: 0x060055A6 RID: 21926 RVA: 0x001301EC File Offset: 0x0012E3EC
		internal int GetDomainID()
		{
			return this.domainID;
		}

		// Token: 0x060055A7 RID: 21927 RVA: 0x001301F4 File Offset: 0x0012E3F4
		[SecurityCritical]
		private ObjRef(ObjRef o)
		{
			this.uri = o.uri;
			this.typeInfo = o.typeInfo;
			this.envoyInfo = o.envoyInfo;
			this.channelInfo = o.channelInfo;
			this.objrefFlags = o.objrefFlags;
			this.SetServerIdentity(o.GetServerIdentity());
			this.SetDomainID(o.GetDomainID());
		}

		// Token: 0x060055A8 RID: 21928 RVA: 0x0013025C File Offset: 0x0012E45C
		[SecurityCritical]
		public ObjRef(MarshalByRefObject o, Type requestedType)
		{
			if (o == null)
			{
				throw new ArgumentNullException("o");
			}
			RuntimeType runtimeType = requestedType as RuntimeType;
			if (requestedType != null && runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			bool flag;
			Identity identity = MarshalByRefObject.GetIdentity(o, out flag);
			this.Init(o, identity, runtimeType);
		}

		// Token: 0x060055A9 RID: 21929 RVA: 0x001302B8 File Offset: 0x0012E4B8
		[SecurityCritical]
		protected ObjRef(SerializationInfo info, StreamingContext context)
		{
			string text = null;
			bool flag = false;
			SerializationInfoEnumerator enumerator = info.GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Name.Equals("uri"))
				{
					this.uri = (string)enumerator.Value;
				}
				else if (enumerator.Name.Equals("typeInfo"))
				{
					this.typeInfo = (IRemotingTypeInfo)enumerator.Value;
				}
				else if (enumerator.Name.Equals("envoyInfo"))
				{
					this.envoyInfo = (IEnvoyInfo)enumerator.Value;
				}
				else if (enumerator.Name.Equals("channelInfo"))
				{
					this.channelInfo = (IChannelInfo)enumerator.Value;
				}
				else if (enumerator.Name.Equals("objrefFlags"))
				{
					object value = enumerator.Value;
					if (value.GetType() == typeof(string))
					{
						this.objrefFlags = ((IConvertible)value).ToInt32(null);
					}
					else
					{
						this.objrefFlags = (int)value;
					}
				}
				else if (enumerator.Name.Equals("fIsMarshalled"))
				{
					object value2 = enumerator.Value;
					int num;
					if (value2.GetType() == typeof(string))
					{
						num = ((IConvertible)value2).ToInt32(null);
					}
					else
					{
						num = (int)value2;
					}
					if (num == 0)
					{
						flag = true;
					}
				}
				else if (enumerator.Name.Equals("url"))
				{
					text = (string)enumerator.Value;
				}
				else if (enumerator.Name.Equals("SrvIdentity"))
				{
					this.SetServerIdentity((GCHandle)enumerator.Value);
				}
				else if (enumerator.Name.Equals("DomainId"))
				{
					this.SetDomainID((int)enumerator.Value);
				}
			}
			if (!flag)
			{
				this.objrefFlags |= 1;
			}
			else
			{
				this.objrefFlags &= -2;
			}
			if (text != null)
			{
				this.uri = text;
				this.objrefFlags |= 4;
			}
		}

		// Token: 0x060055AA RID: 21930 RVA: 0x001304D4 File Offset: 0x0012E6D4
		[SecurityCritical]
		internal bool CanSmuggle()
		{
			if (base.GetType() != typeof(ObjRef) || this.IsObjRefLite())
			{
				return false;
			}
			Type left = null;
			if (this.typeInfo != null)
			{
				left = this.typeInfo.GetType();
			}
			Type left2 = null;
			if (this.channelInfo != null)
			{
				left2 = this.channelInfo.GetType();
			}
			if ((left == null || left == typeof(TypeInfo) || left == typeof(DynamicTypeInfo)) && this.envoyInfo == null && (left2 == null || left2 == typeof(ChannelInfo)))
			{
				if (this.channelInfo != null)
				{
					foreach (object obj in this.channelInfo.ChannelData)
					{
						if (!(obj is CrossAppDomainData))
						{
							return false;
						}
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x060055AB RID: 21931 RVA: 0x001305B3 File Offset: 0x0012E7B3
		[SecurityCritical]
		internal ObjRef CreateSmuggleableCopy()
		{
			return new ObjRef(this);
		}

		// Token: 0x060055AC RID: 21932 RVA: 0x001305BC File Offset: 0x0012E7BC
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.SetType(ObjRef.orType);
			if (!this.IsObjRefLite())
			{
				info.AddValue("uri", this.uri, typeof(string));
				info.AddValue("objrefFlags", this.objrefFlags);
				info.AddValue("typeInfo", this.typeInfo, typeof(IRemotingTypeInfo));
				info.AddValue("envoyInfo", this.envoyInfo, typeof(IEnvoyInfo));
				info.AddValue("channelInfo", this.GetChannelInfoHelper(), typeof(IChannelInfo));
				return;
			}
			info.AddValue("url", this.uri, typeof(string));
		}

		// Token: 0x060055AD RID: 21933 RVA: 0x00130684 File Offset: 0x0012E884
		[SecurityCritical]
		private IChannelInfo GetChannelInfoHelper()
		{
			ChannelInfo channelInfo = this.channelInfo as ChannelInfo;
			if (channelInfo == null)
			{
				return this.channelInfo;
			}
			object[] channelData = channelInfo.ChannelData;
			if (channelData == null)
			{
				return channelInfo;
			}
			string[] array = (string[])CallContext.GetData("__bashChannelUrl");
			if (array == null)
			{
				return channelInfo;
			}
			string value = array[0];
			string text = array[1];
			ChannelInfo channelInfo2 = new ChannelInfo();
			channelInfo2.ChannelData = new object[channelData.Length];
			for (int i = 0; i < channelData.Length; i++)
			{
				channelInfo2.ChannelData[i] = channelData[i];
				ChannelDataStore channelDataStore = channelInfo2.ChannelData[i] as ChannelDataStore;
				if (channelDataStore != null)
				{
					string[] channelUris = channelDataStore.ChannelUris;
					if (channelUris != null && channelUris.Length == 1 && channelUris[0].Equals(value))
					{
						ChannelDataStore channelDataStore2 = channelDataStore.InternalShallowCopy();
						channelDataStore2.ChannelUris = new string[1];
						channelDataStore2.ChannelUris[0] = text;
						channelInfo2.ChannelData[i] = channelDataStore2;
					}
				}
			}
			return channelInfo2;
		}

		// Token: 0x17000E1E RID: 3614
		// (get) Token: 0x060055AE RID: 21934 RVA: 0x0013076B File Offset: 0x0012E96B
		// (set) Token: 0x060055AF RID: 21935 RVA: 0x00130773 File Offset: 0x0012E973
		public virtual string URI
		{
			get
			{
				return this.uri;
			}
			set
			{
				this.uri = value;
			}
		}

		// Token: 0x17000E1F RID: 3615
		// (get) Token: 0x060055B0 RID: 21936 RVA: 0x0013077C File Offset: 0x0012E97C
		// (set) Token: 0x060055B1 RID: 21937 RVA: 0x00130784 File Offset: 0x0012E984
		public virtual IRemotingTypeInfo TypeInfo
		{
			get
			{
				return this.typeInfo;
			}
			set
			{
				this.typeInfo = value;
			}
		}

		// Token: 0x17000E20 RID: 3616
		// (get) Token: 0x060055B2 RID: 21938 RVA: 0x0013078D File Offset: 0x0012E98D
		// (set) Token: 0x060055B3 RID: 21939 RVA: 0x00130795 File Offset: 0x0012E995
		public virtual IEnvoyInfo EnvoyInfo
		{
			get
			{
				return this.envoyInfo;
			}
			set
			{
				this.envoyInfo = value;
			}
		}

		// Token: 0x17000E21 RID: 3617
		// (get) Token: 0x060055B4 RID: 21940 RVA: 0x0013079E File Offset: 0x0012E99E
		// (set) Token: 0x060055B5 RID: 21941 RVA: 0x001307A6 File Offset: 0x0012E9A6
		public virtual IChannelInfo ChannelInfo
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this.channelInfo;
			}
			set
			{
				this.channelInfo = value;
			}
		}

		// Token: 0x060055B6 RID: 21942 RVA: 0x001307AF File Offset: 0x0012E9AF
		[SecurityCritical]
		public virtual object GetRealObject(StreamingContext context)
		{
			return this.GetRealObjectHelper();
		}

		// Token: 0x060055B7 RID: 21943 RVA: 0x001307B8 File Offset: 0x0012E9B8
		[SecurityCritical]
		internal object GetRealObjectHelper()
		{
			if (!this.IsMarshaledObject())
			{
				return this;
			}
			if (this.IsObjRefLite())
			{
				int num = this.uri.IndexOf(RemotingConfiguration.ApplicationId);
				if (num > 0)
				{
					this.uri = this.uri.Substring(num - 1);
				}
			}
			bool fRefine = !(base.GetType() == typeof(ObjRef));
			object ret = RemotingServices.Unmarshal(this, fRefine);
			return this.GetCustomMarshaledCOMObject(ret);
		}

		// Token: 0x060055B8 RID: 21944 RVA: 0x0013082C File Offset: 0x0012EA2C
		[SecurityCritical]
		private object GetCustomMarshaledCOMObject(object ret)
		{
			DynamicTypeInfo dynamicTypeInfo = this.TypeInfo as DynamicTypeInfo;
			if (dynamicTypeInfo != null)
			{
				IntPtr intPtr = IntPtr.Zero;
				if (this.IsFromThisProcess() && !this.IsFromThisAppDomain())
				{
					try
					{
						bool flag;
						intPtr = ((__ComObject)ret).GetIUnknown(out flag);
						if (intPtr != IntPtr.Zero && !flag)
						{
							string typeName = this.TypeInfo.TypeName;
							string name = null;
							string text = null;
							System.Runtime.Remoting.TypeInfo.ParseTypeAndAssembly(typeName, out name, out text);
							Assembly assembly = FormatterServices.LoadAssemblyFromStringNoThrow(text);
							if (assembly == null)
							{
								throw new RemotingException(Environment.GetResourceString("Serialization_AssemblyNotFound", new object[] { text }));
							}
							Type type = assembly.GetType(name, false, false);
							if (type != null && !type.IsVisible)
							{
								type = null;
							}
							object typedObjectForIUnknown = Marshal.GetTypedObjectForIUnknown(intPtr, type);
							if (typedObjectForIUnknown != null)
							{
								ret = typedObjectForIUnknown;
							}
						}
					}
					finally
					{
						if (intPtr != IntPtr.Zero)
						{
							Marshal.Release(intPtr);
						}
					}
				}
			}
			return ret;
		}

		// Token: 0x060055B9 RID: 21945 RVA: 0x00130934 File Offset: 0x0012EB34
		public ObjRef()
		{
			this.objrefFlags = 0;
		}

		// Token: 0x060055BA RID: 21946 RVA: 0x00130943 File Offset: 0x0012EB43
		internal bool IsMarshaledObject()
		{
			return (this.objrefFlags & 1) == 1;
		}

		// Token: 0x060055BB RID: 21947 RVA: 0x00130950 File Offset: 0x0012EB50
		internal void SetMarshaledObject()
		{
			this.objrefFlags |= 1;
		}

		// Token: 0x060055BC RID: 21948 RVA: 0x00130960 File Offset: 0x0012EB60
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal bool IsWellKnown()
		{
			return (this.objrefFlags & 2) == 2;
		}

		// Token: 0x060055BD RID: 21949 RVA: 0x0013096D File Offset: 0x0012EB6D
		internal void SetWellKnown()
		{
			this.objrefFlags |= 2;
		}

		// Token: 0x060055BE RID: 21950 RVA: 0x0013097D File Offset: 0x0012EB7D
		internal bool HasProxyAttribute()
		{
			return (this.objrefFlags & 8) == 8;
		}

		// Token: 0x060055BF RID: 21951 RVA: 0x0013098A File Offset: 0x0012EB8A
		internal void SetHasProxyAttribute()
		{
			this.objrefFlags |= 8;
		}

		// Token: 0x060055C0 RID: 21952 RVA: 0x0013099A File Offset: 0x0012EB9A
		internal bool IsObjRefLite()
		{
			return (this.objrefFlags & 4) == 4;
		}

		// Token: 0x060055C1 RID: 21953 RVA: 0x001309A7 File Offset: 0x0012EBA7
		internal void SetObjRefLite()
		{
			this.objrefFlags |= 4;
		}

		// Token: 0x060055C2 RID: 21954 RVA: 0x001309B8 File Offset: 0x0012EBB8
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private CrossAppDomainData GetAppDomainChannelData()
		{
			for (int i = 0; i < this.ChannelInfo.ChannelData.Length; i++)
			{
				CrossAppDomainData crossAppDomainData = this.ChannelInfo.ChannelData[i] as CrossAppDomainData;
				if (crossAppDomainData != null)
				{
					return crossAppDomainData;
				}
			}
			return null;
		}

		// Token: 0x060055C3 RID: 21955 RVA: 0x001309F8 File Offset: 0x0012EBF8
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public bool IsFromThisProcess()
		{
			if (this.IsWellKnown())
			{
				return false;
			}
			CrossAppDomainData appDomainChannelData = this.GetAppDomainChannelData();
			return appDomainChannelData != null && appDomainChannelData.IsFromThisProcess();
		}

		// Token: 0x060055C4 RID: 21956 RVA: 0x00130A24 File Offset: 0x0012EC24
		[SecurityCritical]
		public bool IsFromThisAppDomain()
		{
			CrossAppDomainData appDomainChannelData = this.GetAppDomainChannelData();
			return appDomainChannelData != null && appDomainChannelData.IsFromThisAppDomain();
		}

		// Token: 0x060055C5 RID: 21957 RVA: 0x00130A44 File Offset: 0x0012EC44
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal int GetServerDomainId()
		{
			if (!this.IsFromThisProcess())
			{
				return 0;
			}
			CrossAppDomainData appDomainChannelData = this.GetAppDomainChannelData();
			return appDomainChannelData.DomainID;
		}

		// Token: 0x060055C6 RID: 21958 RVA: 0x00130A68 File Offset: 0x0012EC68
		[SecurityCritical]
		internal IntPtr GetServerContext(out int domainId)
		{
			IntPtr result = IntPtr.Zero;
			domainId = 0;
			if (this.IsFromThisProcess())
			{
				CrossAppDomainData appDomainChannelData = this.GetAppDomainChannelData();
				domainId = appDomainChannelData.DomainID;
				if (AppDomain.IsDomainIdValid(appDomainChannelData.DomainID))
				{
					result = appDomainChannelData.ContextID;
				}
			}
			return result;
		}

		// Token: 0x060055C7 RID: 21959 RVA: 0x00130AAC File Offset: 0x0012ECAC
		[SecurityCritical]
		internal void Init(object o, Identity idObj, RuntimeType requestedType)
		{
			this.uri = idObj.URI;
			MarshalByRefObject tporObject = idObj.TPOrObject;
			RuntimeType runtimeType;
			if (!RemotingServices.IsTransparentProxy(tporObject))
			{
				runtimeType = (RuntimeType)tporObject.GetType();
			}
			else
			{
				runtimeType = (RuntimeType)RemotingServices.GetRealProxy(tporObject).GetProxiedType();
			}
			RuntimeType runtimeType2 = ((null == requestedType) ? runtimeType : requestedType);
			if (null != requestedType && !requestedType.IsAssignableFrom(runtimeType) && !typeof(IMessageSink).IsAssignableFrom(runtimeType))
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_InvalidRequestedType"), requestedType.ToString()));
			}
			if (runtimeType.IsCOMObject)
			{
				DynamicTypeInfo dynamicTypeInfo = new DynamicTypeInfo(runtimeType2);
				this.TypeInfo = dynamicTypeInfo;
			}
			else
			{
				RemotingTypeCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(runtimeType2);
				this.TypeInfo = reflectionCachedData.TypeInfo;
			}
			if (!idObj.IsWellKnown())
			{
				this.EnvoyInfo = System.Runtime.Remoting.EnvoyInfo.CreateEnvoyInfo(idObj as ServerIdentity);
				IChannelInfo channelInfo = new ChannelInfo();
				if (o is AppDomain)
				{
					object[] channelData = channelInfo.ChannelData;
					int num = channelData.Length;
					object[] array = new object[num];
					Array.Copy(channelData, array, num);
					for (int i = 0; i < num; i++)
					{
						if (!(array[i] is CrossAppDomainData))
						{
							array[i] = null;
						}
					}
					channelInfo.ChannelData = array;
				}
				this.ChannelInfo = channelInfo;
				if (runtimeType.HasProxyAttribute)
				{
					this.SetHasProxyAttribute();
				}
			}
			else
			{
				this.SetWellKnown();
			}
			if (ObjRef.ShouldUseUrlObjRef())
			{
				if (this.IsWellKnown())
				{
					this.SetObjRefLite();
					return;
				}
				string text = ChannelServices.FindFirstHttpUrlForObject(this.URI);
				if (text != null)
				{
					this.URI = text;
					this.SetObjRefLite();
				}
			}
		}

		// Token: 0x060055C8 RID: 21960 RVA: 0x00130C41 File Offset: 0x0012EE41
		internal static bool ShouldUseUrlObjRef()
		{
			return RemotingConfigHandler.UrlObjRefMode;
		}

		// Token: 0x060055C9 RID: 21961 RVA: 0x00130C48 File Offset: 0x0012EE48
		[SecurityCritical]
		internal static bool IsWellFormed(ObjRef objectRef)
		{
			bool result = true;
			if (objectRef == null || objectRef.URI == null || (!objectRef.IsWellKnown() && !objectRef.IsObjRefLite() && !(objectRef.GetType() != ObjRef.orType) && objectRef.ChannelInfo == null))
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0400276B RID: 10091
		internal const int FLG_MARSHALED_OBJECT = 1;

		// Token: 0x0400276C RID: 10092
		internal const int FLG_WELLKNOWN_OBJREF = 2;

		// Token: 0x0400276D RID: 10093
		internal const int FLG_LITE_OBJREF = 4;

		// Token: 0x0400276E RID: 10094
		internal const int FLG_PROXY_ATTRIBUTE = 8;

		// Token: 0x0400276F RID: 10095
		internal string uri;

		// Token: 0x04002770 RID: 10096
		internal IRemotingTypeInfo typeInfo;

		// Token: 0x04002771 RID: 10097
		internal IEnvoyInfo envoyInfo;

		// Token: 0x04002772 RID: 10098
		internal IChannelInfo channelInfo;

		// Token: 0x04002773 RID: 10099
		internal int objrefFlags;

		// Token: 0x04002774 RID: 10100
		internal GCHandle srvIdentity;

		// Token: 0x04002775 RID: 10101
		internal int domainID;

		// Token: 0x04002776 RID: 10102
		private static Type orType = typeof(ObjRef);
	}
}
