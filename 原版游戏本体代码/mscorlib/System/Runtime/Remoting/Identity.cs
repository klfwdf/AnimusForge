using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Security.Cryptography;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B1 RID: 1969
	internal class Identity
	{
		// Token: 0x17000E04 RID: 3588
		// (get) Token: 0x06005546 RID: 21830 RVA: 0x0012F12C File Offset: 0x0012D32C
		internal static string ProcessIDGuid
		{
			get
			{
				return SharedStatics.Remoting_Identity_IDGuid;
			}
		}

		// Token: 0x17000E05 RID: 3589
		// (get) Token: 0x06005547 RID: 21831 RVA: 0x0012F133 File Offset: 0x0012D333
		internal static string AppDomainUniqueId
		{
			get
			{
				if (Identity.s_configuredAppDomainGuid != null)
				{
					return Identity.s_configuredAppDomainGuid;
				}
				return Identity.s_originalAppDomainGuid;
			}
		}

		// Token: 0x17000E06 RID: 3590
		// (get) Token: 0x06005548 RID: 21832 RVA: 0x0012F147 File Offset: 0x0012D347
		internal static string IDGuidString
		{
			get
			{
				return Identity.s_IDGuidString;
			}
		}

		// Token: 0x06005549 RID: 21833 RVA: 0x0012F150 File Offset: 0x0012D350
		internal static string RemoveAppNameOrAppGuidIfNecessary(string uri)
		{
			if (uri == null || uri.Length <= 1 || uri[0] != '/')
			{
				return uri;
			}
			string text;
			if (Identity.s_configuredAppDomainGuidString != null)
			{
				text = Identity.s_configuredAppDomainGuidString;
				if (uri.Length > text.Length && Identity.StringStartsWith(uri, text))
				{
					return uri.Substring(text.Length);
				}
			}
			text = Identity.s_originalAppDomainGuidString;
			if (uri.Length > text.Length && Identity.StringStartsWith(uri, text))
			{
				return uri.Substring(text.Length);
			}
			string applicationName = RemotingConfiguration.ApplicationName;
			if (applicationName != null && uri.Length > applicationName.Length + 2 && string.Compare(uri, 1, applicationName, 0, applicationName.Length, true, CultureInfo.InvariantCulture) == 0 && uri[applicationName.Length + 1] == '/')
			{
				return uri.Substring(applicationName.Length + 2);
			}
			uri = uri.Substring(1);
			return uri;
		}

		// Token: 0x0600554A RID: 21834 RVA: 0x0012F22C File Offset: 0x0012D42C
		private static bool StringStartsWith(string s1, string prefix)
		{
			return s1.Length >= prefix.Length && string.CompareOrdinal(s1, 0, prefix, 0, prefix.Length) == 0;
		}

		// Token: 0x17000E07 RID: 3591
		// (get) Token: 0x0600554B RID: 21835 RVA: 0x0012F250 File Offset: 0x0012D450
		internal static string ProcessGuid
		{
			get
			{
				return Identity.ProcessIDGuid;
			}
		}

		// Token: 0x0600554C RID: 21836 RVA: 0x0012F257 File Offset: 0x0012D457
		private static int GetNextSeqNum()
		{
			return SharedStatics.Remoting_Identity_GetNextSeqNum();
		}

		// Token: 0x0600554D RID: 21837 RVA: 0x0012F260 File Offset: 0x0012D460
		private static byte[] GetRandomBytes()
		{
			byte[] array = new byte[18];
			Identity.s_rng.GetBytes(array);
			return array;
		}

		// Token: 0x0600554E RID: 21838 RVA: 0x0012F281 File Offset: 0x0012D481
		internal Identity(string objURI, string URL)
		{
			if (URL != null)
			{
				this._flags |= 256;
				this._URL = URL;
			}
			this.SetOrCreateURI(objURI, true);
		}

		// Token: 0x0600554F RID: 21839 RVA: 0x0012F2AD File Offset: 0x0012D4AD
		internal Identity(bool bContextBound)
		{
			if (bContextBound)
			{
				this._flags |= 16;
			}
		}

		// Token: 0x17000E08 RID: 3592
		// (get) Token: 0x06005550 RID: 21840 RVA: 0x0012F2C7 File Offset: 0x0012D4C7
		internal bool IsContextBound
		{
			get
			{
				return (this._flags & 16) == 16;
			}
		}

		// Token: 0x17000E09 RID: 3593
		// (get) Token: 0x06005551 RID: 21841 RVA: 0x0012F2D6 File Offset: 0x0012D4D6
		// (set) Token: 0x06005552 RID: 21842 RVA: 0x0012F2E0 File Offset: 0x0012D4E0
		internal bool IsInitializing
		{
			get
			{
				return this._initializing;
			}
			set
			{
				this._initializing = value;
			}
		}

		// Token: 0x06005553 RID: 21843 RVA: 0x0012F2EB File Offset: 0x0012D4EB
		internal bool IsWellKnown()
		{
			return (this._flags & 256) == 256;
		}

		// Token: 0x06005554 RID: 21844 RVA: 0x0012F300 File Offset: 0x0012D500
		internal void SetInIDTable()
		{
			int flags;
			int value;
			do
			{
				flags = this._flags;
				value = this._flags | 4;
			}
			while (flags != Interlocked.CompareExchange(ref this._flags, value, flags));
		}

		// Token: 0x06005555 RID: 21845 RVA: 0x0012F330 File Offset: 0x0012D530
		[SecurityCritical]
		internal void ResetInIDTable(bool bResetURI)
		{
			int flags;
			int value;
			do
			{
				flags = this._flags;
				value = this._flags & -5;
			}
			while (flags != Interlocked.CompareExchange(ref this._flags, value, flags));
			if (bResetURI)
			{
				((ObjRef)this._objRef).URI = null;
				this._ObjURI = null;
			}
		}

		// Token: 0x06005556 RID: 21846 RVA: 0x0012F379 File Offset: 0x0012D579
		internal bool IsInIDTable()
		{
			return (this._flags & 4) == 4;
		}

		// Token: 0x06005557 RID: 21847 RVA: 0x0012F388 File Offset: 0x0012D588
		internal void SetFullyConnected()
		{
			int flags;
			int value;
			do
			{
				flags = this._flags;
				value = this._flags & -4;
			}
			while (flags != Interlocked.CompareExchange(ref this._flags, value, flags));
		}

		// Token: 0x06005558 RID: 21848 RVA: 0x0012F3B6 File Offset: 0x0012D5B6
		internal bool IsFullyDisconnected()
		{
			return (this._flags & 1) == 1;
		}

		// Token: 0x06005559 RID: 21849 RVA: 0x0012F3C3 File Offset: 0x0012D5C3
		internal bool IsRemoteDisconnected()
		{
			return (this._flags & 2) == 2;
		}

		// Token: 0x0600555A RID: 21850 RVA: 0x0012F3D0 File Offset: 0x0012D5D0
		internal bool IsDisconnected()
		{
			return this.IsFullyDisconnected() || this.IsRemoteDisconnected();
		}

		// Token: 0x17000E0A RID: 3594
		// (get) Token: 0x0600555B RID: 21851 RVA: 0x0012F3E2 File Offset: 0x0012D5E2
		internal string URI
		{
			get
			{
				if (this.IsWellKnown())
				{
					return this._URL;
				}
				return this._ObjURI;
			}
		}

		// Token: 0x17000E0B RID: 3595
		// (get) Token: 0x0600555C RID: 21852 RVA: 0x0012F3F9 File Offset: 0x0012D5F9
		internal string ObjURI
		{
			get
			{
				return this._ObjURI;
			}
		}

		// Token: 0x17000E0C RID: 3596
		// (get) Token: 0x0600555D RID: 21853 RVA: 0x0012F401 File Offset: 0x0012D601
		internal MarshalByRefObject TPOrObject
		{
			get
			{
				return (MarshalByRefObject)this._tpOrObject;
			}
		}

		// Token: 0x0600555E RID: 21854 RVA: 0x0012F40E File Offset: 0x0012D60E
		internal object RaceSetTransparentProxy(object tpObj)
		{
			if (this._tpOrObject == null)
			{
				Interlocked.CompareExchange(ref this._tpOrObject, tpObj, null);
			}
			return this._tpOrObject;
		}

		// Token: 0x17000E0D RID: 3597
		// (get) Token: 0x0600555F RID: 21855 RVA: 0x0012F42C File Offset: 0x0012D62C
		internal ObjRef ObjectRef
		{
			[SecurityCritical]
			get
			{
				return (ObjRef)this._objRef;
			}
		}

		// Token: 0x06005560 RID: 21856 RVA: 0x0012F439 File Offset: 0x0012D639
		[SecurityCritical]
		internal ObjRef RaceSetObjRef(ObjRef objRefGiven)
		{
			if (this._objRef == null)
			{
				Interlocked.CompareExchange(ref this._objRef, objRefGiven, null);
			}
			return (ObjRef)this._objRef;
		}

		// Token: 0x17000E0E RID: 3598
		// (get) Token: 0x06005561 RID: 21857 RVA: 0x0012F45C File Offset: 0x0012D65C
		internal IMessageSink ChannelSink
		{
			get
			{
				return (IMessageSink)this._channelSink;
			}
		}

		// Token: 0x06005562 RID: 21858 RVA: 0x0012F469 File Offset: 0x0012D669
		internal IMessageSink RaceSetChannelSink(IMessageSink channelSink)
		{
			if (this._channelSink == null)
			{
				Interlocked.CompareExchange(ref this._channelSink, channelSink, null);
			}
			return (IMessageSink)this._channelSink;
		}

		// Token: 0x17000E0F RID: 3599
		// (get) Token: 0x06005563 RID: 21859 RVA: 0x0012F48C File Offset: 0x0012D68C
		internal IMessageSink EnvoyChain
		{
			get
			{
				return (IMessageSink)this._envoyChain;
			}
		}

		// Token: 0x17000E10 RID: 3600
		// (get) Token: 0x06005564 RID: 21860 RVA: 0x0012F499 File Offset: 0x0012D699
		// (set) Token: 0x06005565 RID: 21861 RVA: 0x0012F4A1 File Offset: 0x0012D6A1
		internal Lease Lease
		{
			get
			{
				return this._lease;
			}
			set
			{
				this._lease = value;
			}
		}

		// Token: 0x06005566 RID: 21862 RVA: 0x0012F4AA File Offset: 0x0012D6AA
		internal IMessageSink RaceSetEnvoyChain(IMessageSink envoyChain)
		{
			if (this._envoyChain == null)
			{
				Interlocked.CompareExchange(ref this._envoyChain, envoyChain, null);
			}
			return (IMessageSink)this._envoyChain;
		}

		// Token: 0x06005567 RID: 21863 RVA: 0x0012F4CD File Offset: 0x0012D6CD
		internal void SetOrCreateURI(string uri)
		{
			this.SetOrCreateURI(uri, false);
		}

		// Token: 0x06005568 RID: 21864 RVA: 0x0012F4D8 File Offset: 0x0012D6D8
		internal void SetOrCreateURI(string uri, bool bIdCtor)
		{
			if (!bIdCtor && this._ObjURI != null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_SetObjectUriForMarshal__UriExists"));
			}
			if (uri == null)
			{
				string text = Convert.ToBase64String(Identity.GetRandomBytes());
				this._ObjURI = string.Concat(new string[]
				{
					Identity.IDGuidString,
					text.Replace('/', '_'),
					"_",
					Identity.GetNextSeqNum().ToString(CultureInfo.InvariantCulture.NumberFormat),
					".rem"
				}).ToLower(CultureInfo.InvariantCulture);
				return;
			}
			if (this is ServerIdentity)
			{
				this._ObjURI = Identity.IDGuidString + uri;
				return;
			}
			this._ObjURI = uri;
		}

		// Token: 0x06005569 RID: 21865 RVA: 0x0012F58C File Offset: 0x0012D78C
		internal static string GetNewLogicalCallID()
		{
			return Identity.IDGuidString + Identity.GetNextSeqNum().ToString();
		}

		// Token: 0x0600556A RID: 21866 RVA: 0x0012F5B0 File Offset: 0x0012D7B0
		[SecurityCritical]
		[Conditional("_DEBUG")]
		internal virtual void AssertValid()
		{
			if (this.URI != null)
			{
				Identity identity = IdentityHolder.ResolveIdentity(this.URI);
			}
		}

		// Token: 0x0600556B RID: 21867 RVA: 0x0012F5D4 File Offset: 0x0012D7D4
		[SecurityCritical]
		internal bool AddProxySideDynamicProperty(IDynamicProperty prop)
		{
			bool result;
			lock (this)
			{
				if (this._dph == null)
				{
					DynamicPropertyHolder dph = new DynamicPropertyHolder();
					lock (this)
					{
						if (this._dph == null)
						{
							this._dph = dph;
						}
					}
				}
				result = this._dph.AddDynamicProperty(prop);
			}
			return result;
		}

		// Token: 0x0600556C RID: 21868 RVA: 0x0012F65C File Offset: 0x0012D85C
		[SecurityCritical]
		internal bool RemoveProxySideDynamicProperty(string name)
		{
			bool result;
			lock (this)
			{
				if (this._dph == null)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Contexts_NoProperty"), name));
				}
				result = this._dph.RemoveDynamicProperty(name);
			}
			return result;
		}

		// Token: 0x17000E11 RID: 3601
		// (get) Token: 0x0600556D RID: 21869 RVA: 0x0012F6C4 File Offset: 0x0012D8C4
		internal ArrayWithSize ProxySideDynamicSinks
		{
			[SecurityCritical]
			get
			{
				if (this._dph == null)
				{
					return null;
				}
				return this._dph.DynamicSinks;
			}
		}

		// Token: 0x04002743 RID: 10051
		private static string s_originalAppDomainGuid = Guid.NewGuid().ToString().Replace('-', '_');

		// Token: 0x04002744 RID: 10052
		private static string s_configuredAppDomainGuid = null;

		// Token: 0x04002745 RID: 10053
		private static string s_originalAppDomainGuidString = "/" + Identity.s_originalAppDomainGuid.ToLower(CultureInfo.InvariantCulture) + "/";

		// Token: 0x04002746 RID: 10054
		private static string s_configuredAppDomainGuidString = null;

		// Token: 0x04002747 RID: 10055
		private static string s_IDGuidString = "/" + Identity.s_originalAppDomainGuid.ToLower(CultureInfo.InvariantCulture) + "/";

		// Token: 0x04002748 RID: 10056
		private static RNGCryptoServiceProvider s_rng = new RNGCryptoServiceProvider();

		// Token: 0x04002749 RID: 10057
		protected const int IDFLG_DISCONNECTED_FULL = 1;

		// Token: 0x0400274A RID: 10058
		protected const int IDFLG_DISCONNECTED_REM = 2;

		// Token: 0x0400274B RID: 10059
		protected const int IDFLG_IN_IDTABLE = 4;

		// Token: 0x0400274C RID: 10060
		protected const int IDFLG_CONTEXT_BOUND = 16;

		// Token: 0x0400274D RID: 10061
		protected const int IDFLG_WELLKNOWN = 256;

		// Token: 0x0400274E RID: 10062
		protected const int IDFLG_SERVER_SINGLECALL = 512;

		// Token: 0x0400274F RID: 10063
		protected const int IDFLG_SERVER_SINGLETON = 1024;

		// Token: 0x04002750 RID: 10064
		internal int _flags;

		// Token: 0x04002751 RID: 10065
		internal object _tpOrObject;

		// Token: 0x04002752 RID: 10066
		protected string _ObjURI;

		// Token: 0x04002753 RID: 10067
		protected string _URL;

		// Token: 0x04002754 RID: 10068
		internal object _objRef;

		// Token: 0x04002755 RID: 10069
		internal object _channelSink;

		// Token: 0x04002756 RID: 10070
		internal object _envoyChain;

		// Token: 0x04002757 RID: 10071
		internal DynamicPropertyHolder _dph;

		// Token: 0x04002758 RID: 10072
		internal Lease _lease;

		// Token: 0x04002759 RID: 10073
		private volatile bool _initializing;
	}
}
