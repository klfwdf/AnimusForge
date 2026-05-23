using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001AF RID: 431
	[ComVisible(true)]
	public abstract class IsolatedStorage : MarshalByRefObject
	{
		// Token: 0x06001AF9 RID: 6905 RVA: 0x0005B3CA File Offset: 0x000595CA
		internal static bool IsRoaming(IsolatedStorageScope scope)
		{
			return (scope & IsolatedStorageScope.Roaming) > IsolatedStorageScope.None;
		}

		// Token: 0x06001AFA RID: 6906 RVA: 0x0005B3D2 File Offset: 0x000595D2
		internal bool IsRoaming()
		{
			return (this.m_Scope & IsolatedStorageScope.Roaming) > IsolatedStorageScope.None;
		}

		// Token: 0x06001AFB RID: 6907 RVA: 0x0005B3DF File Offset: 0x000595DF
		internal static bool IsDomain(IsolatedStorageScope scope)
		{
			return (scope & IsolatedStorageScope.Domain) > IsolatedStorageScope.None;
		}

		// Token: 0x06001AFC RID: 6908 RVA: 0x0005B3E7 File Offset: 0x000595E7
		internal bool IsDomain()
		{
			return (this.m_Scope & IsolatedStorageScope.Domain) > IsolatedStorageScope.None;
		}

		// Token: 0x06001AFD RID: 6909 RVA: 0x0005B3F4 File Offset: 0x000595F4
		internal static bool IsMachine(IsolatedStorageScope scope)
		{
			return (scope & IsolatedStorageScope.Machine) > IsolatedStorageScope.None;
		}

		// Token: 0x06001AFE RID: 6910 RVA: 0x0005B3FD File Offset: 0x000595FD
		internal bool IsAssembly()
		{
			return (this.m_Scope & IsolatedStorageScope.Assembly) > IsolatedStorageScope.None;
		}

		// Token: 0x06001AFF RID: 6911 RVA: 0x0005B40A File Offset: 0x0005960A
		internal static bool IsApp(IsolatedStorageScope scope)
		{
			return (scope & IsolatedStorageScope.Application) > IsolatedStorageScope.None;
		}

		// Token: 0x06001B00 RID: 6912 RVA: 0x0005B413 File Offset: 0x00059613
		internal bool IsApp()
		{
			return (this.m_Scope & IsolatedStorageScope.Application) > IsolatedStorageScope.None;
		}

		// Token: 0x06001B01 RID: 6913 RVA: 0x0005B424 File Offset: 0x00059624
		private string GetNameFromID(string typeID, string instanceID)
		{
			return typeID + this.SeparatorInternal.ToString() + instanceID;
		}

		// Token: 0x06001B02 RID: 6914 RVA: 0x0005B448 File Offset: 0x00059648
		private static string GetPredefinedTypeName(object o)
		{
			if (o is Publisher)
			{
				return "Publisher";
			}
			if (o is StrongName)
			{
				return "StrongName";
			}
			if (o is Url)
			{
				return "Url";
			}
			if (o is Site)
			{
				return "Site";
			}
			if (o is Zone)
			{
				return "Zone";
			}
			return null;
		}

		// Token: 0x06001B03 RID: 6915 RVA: 0x0005B49C File Offset: 0x0005969C
		internal static string GetHash(Stream s)
		{
			string result;
			using (SHA1 sha = new SHA1CryptoServiceProvider())
			{
				byte[] buff = sha.ComputeHash(s);
				result = Path.ToBase32StringSuitableForDirName(buff);
			}
			return result;
		}

		// Token: 0x06001B04 RID: 6916 RVA: 0x0005B4DC File Offset: 0x000596DC
		private static bool IsValidName(string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (!char.IsLetter(s[i]) && !char.IsDigit(s[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001B05 RID: 6917 RVA: 0x0005B519 File Offset: 0x00059719
		private static SecurityPermission GetControlEvidencePermission()
		{
			if (IsolatedStorage.s_PermControlEvidence == null)
			{
				IsolatedStorage.s_PermControlEvidence = new SecurityPermission(SecurityPermissionFlag.ControlEvidence);
			}
			return IsolatedStorage.s_PermControlEvidence;
		}

		// Token: 0x06001B06 RID: 6918 RVA: 0x0005B539 File Offset: 0x00059739
		private static PermissionSet GetUnrestricted()
		{
			if (IsolatedStorage.s_PermUnrestricted == null)
			{
				IsolatedStorage.s_PermUnrestricted = new PermissionSet(PermissionState.Unrestricted);
			}
			return IsolatedStorage.s_PermUnrestricted;
		}

		// Token: 0x17000301 RID: 769
		// (get) Token: 0x06001B07 RID: 6919 RVA: 0x0005B558 File Offset: 0x00059758
		protected virtual char SeparatorExternal
		{
			get
			{
				return '\\';
			}
		}

		// Token: 0x17000302 RID: 770
		// (get) Token: 0x06001B08 RID: 6920 RVA: 0x0005B55C File Offset: 0x0005975C
		protected virtual char SeparatorInternal
		{
			get
			{
				return '.';
			}
		}

		// Token: 0x17000303 RID: 771
		// (get) Token: 0x06001B09 RID: 6921 RVA: 0x0005B560 File Offset: 0x00059760
		[CLSCompliant(false)]
		[Obsolete("IsolatedStorage.MaximumSize has been deprecated because it is not CLS Compliant.  To get the maximum size use IsolatedStorage.Quota")]
		public virtual ulong MaximumSize
		{
			get
			{
				if (this.m_ValidQuota)
				{
					return this.m_Quota;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_QuotaIsUndefined", new object[] { "MaximumSize" }));
			}
		}

		// Token: 0x17000304 RID: 772
		// (get) Token: 0x06001B0A RID: 6922 RVA: 0x0005B58E File Offset: 0x0005978E
		[CLSCompliant(false)]
		[Obsolete("IsolatedStorage.CurrentSize has been deprecated because it is not CLS Compliant.  To get the current size use IsolatedStorage.UsedSize")]
		public virtual ulong CurrentSize
		{
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_CurrentSizeUndefined", new object[] { "CurrentSize" }));
			}
		}

		// Token: 0x17000305 RID: 773
		// (get) Token: 0x06001B0B RID: 6923 RVA: 0x0005B5AD File Offset: 0x000597AD
		[ComVisible(false)]
		public virtual long UsedSize
		{
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_CurrentSizeUndefined", new object[] { "UsedSize" }));
			}
		}

		// Token: 0x17000306 RID: 774
		// (get) Token: 0x06001B0C RID: 6924 RVA: 0x0005B5CC File Offset: 0x000597CC
		// (set) Token: 0x06001B0D RID: 6925 RVA: 0x0005B5FA File Offset: 0x000597FA
		[ComVisible(false)]
		public virtual long Quota
		{
			get
			{
				if (this.m_ValidQuota)
				{
					return (long)this.m_Quota;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_QuotaIsUndefined", new object[] { "Quota" }));
			}
			internal set
			{
				this.m_Quota = (ulong)value;
				this.m_ValidQuota = true;
			}
		}

		// Token: 0x17000307 RID: 775
		// (get) Token: 0x06001B0E RID: 6926 RVA: 0x0005B60A File Offset: 0x0005980A
		[ComVisible(false)]
		public virtual long AvailableFreeSpace
		{
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_QuotaIsUndefined", new object[] { "AvailableFreeSpace" }));
			}
		}

		// Token: 0x17000308 RID: 776
		// (get) Token: 0x06001B0F RID: 6927 RVA: 0x0005B629 File Offset: 0x00059829
		public object DomainIdentity
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy)]
			get
			{
				if (this.IsDomain())
				{
					return this.m_DomainIdentity;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_DomainUndefined"));
			}
		}

		// Token: 0x17000309 RID: 777
		// (get) Token: 0x06001B10 RID: 6928 RVA: 0x0005B649 File Offset: 0x00059849
		[ComVisible(false)]
		public object ApplicationIdentity
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy)]
			get
			{
				if (this.IsApp())
				{
					return this.m_AppIdentity;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_ApplicationUndefined"));
			}
		}

		// Token: 0x1700030A RID: 778
		// (get) Token: 0x06001B11 RID: 6929 RVA: 0x0005B669 File Offset: 0x00059869
		public object AssemblyIdentity
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy)]
			get
			{
				if (this.IsAssembly())
				{
					return this.m_AssemIdentity;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_AssemblyUndefined"));
			}
		}

		// Token: 0x06001B12 RID: 6930 RVA: 0x0005B689 File Offset: 0x00059889
		[ComVisible(false)]
		public virtual bool IncreaseQuotaTo(long newQuotaSize)
		{
			return false;
		}

		// Token: 0x06001B13 RID: 6931 RVA: 0x0005B68C File Offset: 0x0005988C
		[SecurityCritical]
		internal MemoryStream GetIdentityStream(IsolatedStorageScope scope)
		{
			IsolatedStorage.GetUnrestricted().Assert();
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			MemoryStream memoryStream = new MemoryStream();
			object obj;
			if (IsolatedStorage.IsApp(scope))
			{
				obj = this.m_AppIdentity;
			}
			else if (IsolatedStorage.IsDomain(scope))
			{
				obj = this.m_DomainIdentity;
			}
			else
			{
				obj = this.m_AssemIdentity;
			}
			if (obj != null)
			{
				binaryFormatter.Serialize(memoryStream, obj);
			}
			memoryStream.Position = 0L;
			return memoryStream;
		}

		// Token: 0x1700030B RID: 779
		// (get) Token: 0x06001B14 RID: 6932 RVA: 0x0005B6EC File Offset: 0x000598EC
		public IsolatedStorageScope Scope
		{
			get
			{
				return this.m_Scope;
			}
		}

		// Token: 0x1700030C RID: 780
		// (get) Token: 0x06001B15 RID: 6933 RVA: 0x0005B6F4 File Offset: 0x000598F4
		internal string DomainName
		{
			get
			{
				if (this.IsDomain())
				{
					return this.m_DomainName;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_DomainUndefined"));
			}
		}

		// Token: 0x1700030D RID: 781
		// (get) Token: 0x06001B16 RID: 6934 RVA: 0x0005B714 File Offset: 0x00059914
		internal string AssemName
		{
			get
			{
				if (this.IsAssembly())
				{
					return this.m_AssemName;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_AssemblyUndefined"));
			}
		}

		// Token: 0x1700030E RID: 782
		// (get) Token: 0x06001B17 RID: 6935 RVA: 0x0005B734 File Offset: 0x00059934
		internal string AppName
		{
			get
			{
				if (this.IsApp())
				{
					return this.m_AppName;
				}
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_ApplicationUndefined"));
			}
		}

		// Token: 0x06001B18 RID: 6936 RVA: 0x0005B754 File Offset: 0x00059954
		[SecuritySafeCritical]
		protected void InitStore(IsolatedStorageScope scope, Type domainEvidenceType, Type assemblyEvidenceType)
		{
			PermissionSet permissionSet = null;
			PermissionSet psDenied = null;
			RuntimeAssembly caller = IsolatedStorage.GetCaller();
			IsolatedStorage.GetControlEvidencePermission().Assert();
			if (IsolatedStorage.IsDomain(scope))
			{
				AppDomain domain = Thread.GetDomain();
				if (!IsolatedStorage.IsRoaming(scope))
				{
					permissionSet = domain.PermissionSet;
					if (permissionSet == null)
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DomainGrantSet"));
					}
				}
				this._InitStore(scope, domain.Evidence, domainEvidenceType, caller.Evidence, assemblyEvidenceType, null, null);
			}
			else
			{
				if (!IsolatedStorage.IsRoaming(scope))
				{
					caller.GetGrantSet(out permissionSet, out psDenied);
					if (permissionSet == null)
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_AssemblyGrantSet"));
					}
				}
				this._InitStore(scope, null, null, caller.Evidence, assemblyEvidenceType, null, null);
			}
			this.SetQuota(permissionSet, psDenied);
		}

		// Token: 0x06001B19 RID: 6937 RVA: 0x0005B800 File Offset: 0x00059A00
		[SecuritySafeCritical]
		protected void InitStore(IsolatedStorageScope scope, Type appEvidenceType)
		{
			PermissionSet permissionSet = null;
			PermissionSet psDenied = null;
			Assembly caller = IsolatedStorage.GetCaller();
			IsolatedStorage.GetControlEvidencePermission().Assert();
			if (IsolatedStorage.IsApp(scope))
			{
				AppDomain domain = Thread.GetDomain();
				if (!IsolatedStorage.IsRoaming(scope))
				{
					permissionSet = domain.PermissionSet;
					if (permissionSet == null)
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DomainGrantSet"));
					}
				}
				ActivationContext activationContext = AppDomain.CurrentDomain.ActivationContext;
				if (activationContext == null)
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_ApplicationMissingIdentity"));
				}
				ApplicationSecurityInfo applicationSecurityInfo = new ApplicationSecurityInfo(activationContext);
				this._InitStore(scope, null, null, null, null, applicationSecurityInfo.ApplicationEvidence, appEvidenceType);
			}
			this.SetQuota(permissionSet, psDenied);
		}

		// Token: 0x06001B1A RID: 6938 RVA: 0x0005B898 File Offset: 0x00059A98
		[SecuritySafeCritical]
		internal void InitStore(IsolatedStorageScope scope, object domain, object assem, object app)
		{
			PermissionSet permissionSet = null;
			PermissionSet psDenied = null;
			Evidence evidence = null;
			Evidence evidence2 = null;
			Evidence evidence3 = null;
			if (IsolatedStorage.IsApp(scope))
			{
				EvidenceBase evidenceBase = app as EvidenceBase;
				if (evidenceBase == null)
				{
					evidenceBase = new LegacyEvidenceWrapper(app);
				}
				evidence3 = new Evidence();
				evidence3.AddHostEvidence<EvidenceBase>(evidenceBase);
			}
			else
			{
				EvidenceBase evidenceBase2 = assem as EvidenceBase;
				if (evidenceBase2 == null)
				{
					evidenceBase2 = new LegacyEvidenceWrapper(assem);
				}
				evidence2 = new Evidence();
				evidence2.AddHostEvidence<EvidenceBase>(evidenceBase2);
				if (IsolatedStorage.IsDomain(scope))
				{
					EvidenceBase evidenceBase3 = domain as EvidenceBase;
					if (evidenceBase3 == null)
					{
						evidenceBase3 = new LegacyEvidenceWrapper(domain);
					}
					evidence = new Evidence();
					evidence.AddHostEvidence<EvidenceBase>(evidenceBase3);
				}
			}
			this._InitStore(scope, evidence, null, evidence2, null, evidence3, null);
			if (!IsolatedStorage.IsRoaming(scope))
			{
				RuntimeAssembly caller = IsolatedStorage.GetCaller();
				IsolatedStorage.GetControlEvidencePermission().Assert();
				caller.GetGrantSet(out permissionSet, out psDenied);
				if (permissionSet == null)
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_AssemblyGrantSet"));
				}
			}
			this.SetQuota(permissionSet, psDenied);
		}

		// Token: 0x06001B1B RID: 6939 RVA: 0x0005B97C File Offset: 0x00059B7C
		[SecurityCritical]
		internal void InitStore(IsolatedStorageScope scope, Evidence domainEv, Type domainEvidenceType, Evidence assemEv, Type assemEvidenceType, Evidence appEv, Type appEvidenceType)
		{
			PermissionSet psAllowed = null;
			if (!IsolatedStorage.IsRoaming(scope))
			{
				if (IsolatedStorage.IsApp(scope))
				{
					psAllowed = SecurityManager.GetStandardSandbox(appEv);
				}
				else if (IsolatedStorage.IsDomain(scope))
				{
					psAllowed = SecurityManager.GetStandardSandbox(domainEv);
				}
				else
				{
					psAllowed = SecurityManager.GetStandardSandbox(assemEv);
				}
			}
			this._InitStore(scope, domainEv, domainEvidenceType, assemEv, assemEvidenceType, appEv, appEvidenceType);
			this.SetQuota(psAllowed, null);
		}

		// Token: 0x06001B1C RID: 6940 RVA: 0x0005B9D8 File Offset: 0x00059BD8
		[SecuritySafeCritical]
		internal bool InitStore(IsolatedStorageScope scope, Stream domain, Stream assem, Stream app, string domainName, string assemName, string appName)
		{
			try
			{
				IsolatedStorage.GetUnrestricted().Assert();
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				if (IsolatedStorage.IsApp(scope))
				{
					this.m_AppIdentity = binaryFormatter.Deserialize(app);
					this.m_AppName = appName;
				}
				else
				{
					this.m_AssemIdentity = binaryFormatter.Deserialize(assem);
					this.m_AssemName = assemName;
					if (IsolatedStorage.IsDomain(scope))
					{
						this.m_DomainIdentity = binaryFormatter.Deserialize(domain);
						this.m_DomainName = domainName;
					}
				}
			}
			catch
			{
				return false;
			}
			this.m_Scope = scope;
			return true;
		}

		// Token: 0x06001B1D RID: 6941 RVA: 0x0005BA68 File Offset: 0x00059C68
		[SecurityCritical]
		private void _InitStore(IsolatedStorageScope scope, Evidence domainEv, Type domainEvidenceType, Evidence assemEv, Type assemblyEvidenceType, Evidence appEv, Type appEvidenceType)
		{
			IsolatedStorage.VerifyScope(scope);
			if (IsolatedStorage.IsApp(scope))
			{
				if (appEv == null)
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_ApplicationMissingIdentity"));
				}
			}
			else
			{
				if (assemEv == null)
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_AssemblyMissingIdentity"));
				}
				if (IsolatedStorage.IsDomain(scope) && domainEv == null)
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DomainMissingIdentity"));
				}
			}
			IsolatedStorage.DemandPermission(scope);
			string typeID = null;
			string instanceID = null;
			if (IsolatedStorage.IsApp(scope))
			{
				this.m_AppIdentity = IsolatedStorage.GetAccountingInfo(appEv, appEvidenceType, IsolatedStorageScope.Application, out typeID, out instanceID);
				this.m_AppName = this.GetNameFromID(typeID, instanceID);
			}
			else
			{
				this.m_AssemIdentity = IsolatedStorage.GetAccountingInfo(assemEv, assemblyEvidenceType, IsolatedStorageScope.Assembly, out typeID, out instanceID);
				this.m_AssemName = this.GetNameFromID(typeID, instanceID);
				if (IsolatedStorage.IsDomain(scope))
				{
					this.m_DomainIdentity = IsolatedStorage.GetAccountingInfo(domainEv, domainEvidenceType, IsolatedStorageScope.Domain, out typeID, out instanceID);
					this.m_DomainName = this.GetNameFromID(typeID, instanceID);
				}
			}
			this.m_Scope = scope;
		}

		// Token: 0x06001B1E RID: 6942 RVA: 0x0005BB50 File Offset: 0x00059D50
		[SecurityCritical]
		private static object GetAccountingInfo(Evidence evidence, Type evidenceType, IsolatedStorageScope fAssmDomApp, out string typeName, out string instanceName)
		{
			object obj = null;
			object obj2 = IsolatedStorage._GetAccountingInfo(evidence, evidenceType, fAssmDomApp, out obj);
			typeName = IsolatedStorage.GetPredefinedTypeName(obj2);
			if (typeName == null)
			{
				IsolatedStorage.GetUnrestricted().Assert();
				MemoryStream memoryStream = new MemoryStream();
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				binaryFormatter.Serialize(memoryStream, obj2.GetType());
				memoryStream.Position = 0L;
				typeName = IsolatedStorage.GetHash(memoryStream);
				CodeAccessPermission.RevertAssert();
			}
			instanceName = null;
			if (obj != null)
			{
				if (obj is Stream)
				{
					instanceName = IsolatedStorage.GetHash((Stream)obj);
				}
				else if (obj is string)
				{
					if (IsolatedStorage.IsValidName((string)obj))
					{
						instanceName = (string)obj;
					}
					else
					{
						MemoryStream memoryStream = new MemoryStream();
						BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
						binaryWriter.Write((string)obj);
						memoryStream.Position = 0L;
						instanceName = IsolatedStorage.GetHash(memoryStream);
					}
				}
			}
			else
			{
				obj = obj2;
			}
			if (instanceName == null)
			{
				IsolatedStorage.GetUnrestricted().Assert();
				MemoryStream memoryStream = new MemoryStream();
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				binaryFormatter.Serialize(memoryStream, obj);
				memoryStream.Position = 0L;
				instanceName = IsolatedStorage.GetHash(memoryStream);
				CodeAccessPermission.RevertAssert();
			}
			return obj2;
		}

		// Token: 0x06001B1F RID: 6943 RVA: 0x0005BC5C File Offset: 0x00059E5C
		private static object _GetAccountingInfo(Evidence evidence, Type evidenceType, IsolatedStorageScope fAssmDomApp, out object oNormalized)
		{
			object hostEvidence;
			if (evidenceType == null)
			{
				hostEvidence = evidence.GetHostEvidence<Publisher>();
				if (hostEvidence == null)
				{
					hostEvidence = evidence.GetHostEvidence<StrongName>();
				}
				if (hostEvidence == null)
				{
					hostEvidence = evidence.GetHostEvidence<Url>();
				}
				if (hostEvidence == null)
				{
					hostEvidence = evidence.GetHostEvidence<Site>();
				}
				if (hostEvidence == null)
				{
					hostEvidence = evidence.GetHostEvidence<Zone>();
				}
				if (hostEvidence == null)
				{
					if (fAssmDomApp == IsolatedStorageScope.Domain)
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DomainNoEvidence"));
					}
					if (fAssmDomApp == IsolatedStorageScope.Application)
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_ApplicationNoEvidence"));
					}
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_AssemblyNoEvidence"));
				}
			}
			else
			{
				hostEvidence = evidence.GetHostEvidence(evidenceType);
				if (hostEvidence == null)
				{
					if (fAssmDomApp == IsolatedStorageScope.Domain)
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DomainNoEvidence"));
					}
					if (fAssmDomApp == IsolatedStorageScope.Application)
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_ApplicationNoEvidence"));
					}
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_AssemblyNoEvidence"));
				}
			}
			if (hostEvidence is INormalizeForIsolatedStorage)
			{
				oNormalized = ((INormalizeForIsolatedStorage)hostEvidence).Normalize();
			}
			else if (hostEvidence is Publisher)
			{
				oNormalized = ((Publisher)hostEvidence).Normalize();
			}
			else if (hostEvidence is StrongName)
			{
				oNormalized = ((StrongName)hostEvidence).Normalize();
			}
			else if (hostEvidence is Url)
			{
				oNormalized = ((Url)hostEvidence).Normalize();
			}
			else if (hostEvidence is Site)
			{
				oNormalized = ((Site)hostEvidence).Normalize();
			}
			else if (hostEvidence is Zone)
			{
				oNormalized = ((Zone)hostEvidence).Normalize();
			}
			else
			{
				oNormalized = null;
			}
			return hostEvidence;
		}

		// Token: 0x06001B20 RID: 6944 RVA: 0x0005BDB4 File Offset: 0x00059FB4
		[SecurityCritical]
		private static void DemandPermission(IsolatedStorageScope scope)
		{
			IsolatedStorageFilePermission isolatedStorageFilePermission = null;
			if (scope <= (IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming))
			{
				if (scope <= (IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly))
				{
					if (scope != (IsolatedStorageScope.User | IsolatedStorageScope.Assembly))
					{
						if (scope == (IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly))
						{
							if (IsolatedStorage.s_PermDomain == null)
							{
								IsolatedStorage.s_PermDomain = new IsolatedStorageFilePermission(IsolatedStorageContainment.DomainIsolationByUser, 0L, false);
							}
							isolatedStorageFilePermission = IsolatedStorage.s_PermDomain;
						}
					}
					else
					{
						if (IsolatedStorage.s_PermAssem == null)
						{
							IsolatedStorage.s_PermAssem = new IsolatedStorageFilePermission(IsolatedStorageContainment.AssemblyIsolationByUser, 0L, false);
						}
						isolatedStorageFilePermission = IsolatedStorage.s_PermAssem;
					}
				}
				else if (scope != (IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming))
				{
					if (scope == (IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming))
					{
						if (IsolatedStorage.s_PermDomainRoaming == null)
						{
							IsolatedStorage.s_PermDomainRoaming = new IsolatedStorageFilePermission(IsolatedStorageContainment.DomainIsolationByRoamingUser, 0L, false);
						}
						isolatedStorageFilePermission = IsolatedStorage.s_PermDomainRoaming;
					}
				}
				else
				{
					if (IsolatedStorage.s_PermAssemRoaming == null)
					{
						IsolatedStorage.s_PermAssemRoaming = new IsolatedStorageFilePermission(IsolatedStorageContainment.AssemblyIsolationByRoamingUser, 0L, false);
					}
					isolatedStorageFilePermission = IsolatedStorage.s_PermAssemRoaming;
				}
			}
			else if (scope <= (IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine))
			{
				if (scope != (IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine))
				{
					if (scope == (IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine))
					{
						if (IsolatedStorage.s_PermMachineDomain == null)
						{
							IsolatedStorage.s_PermMachineDomain = new IsolatedStorageFilePermission(IsolatedStorageContainment.DomainIsolationByMachine, 0L, false);
						}
						isolatedStorageFilePermission = IsolatedStorage.s_PermMachineDomain;
					}
				}
				else
				{
					if (IsolatedStorage.s_PermMachineAssem == null)
					{
						IsolatedStorage.s_PermMachineAssem = new IsolatedStorageFilePermission(IsolatedStorageContainment.AssemblyIsolationByMachine, 0L, false);
					}
					isolatedStorageFilePermission = IsolatedStorage.s_PermMachineAssem;
				}
			}
			else if (scope != (IsolatedStorageScope.User | IsolatedStorageScope.Application))
			{
				if (scope != (IsolatedStorageScope.User | IsolatedStorageScope.Roaming | IsolatedStorageScope.Application))
				{
					if (scope == (IsolatedStorageScope.Machine | IsolatedStorageScope.Application))
					{
						if (IsolatedStorage.s_PermAppMachine == null)
						{
							IsolatedStorage.s_PermAppMachine = new IsolatedStorageFilePermission(IsolatedStorageContainment.ApplicationIsolationByMachine, 0L, false);
						}
						isolatedStorageFilePermission = IsolatedStorage.s_PermAppMachine;
					}
				}
				else
				{
					if (IsolatedStorage.s_PermAppUserRoaming == null)
					{
						IsolatedStorage.s_PermAppUserRoaming = new IsolatedStorageFilePermission(IsolatedStorageContainment.ApplicationIsolationByRoamingUser, 0L, false);
					}
					isolatedStorageFilePermission = IsolatedStorage.s_PermAppUserRoaming;
				}
			}
			else
			{
				if (IsolatedStorage.s_PermAppUser == null)
				{
					IsolatedStorage.s_PermAppUser = new IsolatedStorageFilePermission(IsolatedStorageContainment.ApplicationIsolationByUser, 0L, false);
				}
				isolatedStorageFilePermission = IsolatedStorage.s_PermAppUser;
			}
			isolatedStorageFilePermission.Demand();
		}

		// Token: 0x06001B21 RID: 6945 RVA: 0x0005BF7C File Offset: 0x0005A17C
		internal static void VerifyScope(IsolatedStorageScope scope)
		{
			if (scope == (IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly) || scope == (IsolatedStorageScope.User | IsolatedStorageScope.Assembly) || scope == (IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming) || scope == (IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming) || scope == (IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine) || scope == (IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine) || scope == (IsolatedStorageScope.User | IsolatedStorageScope.Application) || scope == (IsolatedStorageScope.Machine | IsolatedStorageScope.Application) || scope == (IsolatedStorageScope.User | IsolatedStorageScope.Roaming | IsolatedStorageScope.Application))
			{
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_Scope_Invalid"));
		}

		// Token: 0x06001B22 RID: 6946 RVA: 0x0005BFBC File Offset: 0x0005A1BC
		[SecurityCritical]
		internal virtual void SetQuota(PermissionSet psAllowed, PermissionSet psDenied)
		{
			IsolatedStoragePermission permission = this.GetPermission(psAllowed);
			this.m_Quota = 0UL;
			if (permission != null)
			{
				if (permission.IsUnrestricted())
				{
					this.m_Quota = 9223372036854775807UL;
				}
				else
				{
					this.m_Quota = (ulong)permission.UserQuota;
				}
			}
			if (psDenied != null)
			{
				IsolatedStoragePermission permission2 = this.GetPermission(psDenied);
				if (permission2 != null)
				{
					if (permission2.IsUnrestricted())
					{
						this.m_Quota = 0UL;
					}
					else
					{
						ulong userQuota = (ulong)permission2.UserQuota;
						if (userQuota > this.m_Quota)
						{
							this.m_Quota = 0UL;
						}
						else
						{
							this.m_Quota -= userQuota;
						}
					}
				}
			}
			this.m_ValidQuota = true;
		}

		// Token: 0x06001B23 RID: 6947
		public abstract void Remove();

		// Token: 0x06001B24 RID: 6948
		protected abstract IsolatedStoragePermission GetPermission(PermissionSet ps);

		// Token: 0x06001B25 RID: 6949 RVA: 0x0005C050 File Offset: 0x0005A250
		[SecuritySafeCritical]
		internal static RuntimeAssembly GetCaller()
		{
			RuntimeAssembly result = null;
			IsolatedStorage.GetCaller(JitHelpers.GetObjectHandleOnStack<RuntimeAssembly>(ref result));
			return result;
		}

		// Token: 0x06001B26 RID: 6950
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetCaller(ObjectHandleOnStack retAssembly);

		// Token: 0x0400094C RID: 2380
		internal const IsolatedStorageScope c_Assembly = IsolatedStorageScope.User | IsolatedStorageScope.Assembly;

		// Token: 0x0400094D RID: 2381
		internal const IsolatedStorageScope c_Domain = IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly;

		// Token: 0x0400094E RID: 2382
		internal const IsolatedStorageScope c_AssemblyRoaming = IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming;

		// Token: 0x0400094F RID: 2383
		internal const IsolatedStorageScope c_DomainRoaming = IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming;

		// Token: 0x04000950 RID: 2384
		internal const IsolatedStorageScope c_MachineAssembly = IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine;

		// Token: 0x04000951 RID: 2385
		internal const IsolatedStorageScope c_MachineDomain = IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine;

		// Token: 0x04000952 RID: 2386
		internal const IsolatedStorageScope c_AppUser = IsolatedStorageScope.User | IsolatedStorageScope.Application;

		// Token: 0x04000953 RID: 2387
		internal const IsolatedStorageScope c_AppMachine = IsolatedStorageScope.Machine | IsolatedStorageScope.Application;

		// Token: 0x04000954 RID: 2388
		internal const IsolatedStorageScope c_AppUserRoaming = IsolatedStorageScope.User | IsolatedStorageScope.Roaming | IsolatedStorageScope.Application;

		// Token: 0x04000955 RID: 2389
		private const string s_Publisher = "Publisher";

		// Token: 0x04000956 RID: 2390
		private const string s_StrongName = "StrongName";

		// Token: 0x04000957 RID: 2391
		private const string s_Site = "Site";

		// Token: 0x04000958 RID: 2392
		private const string s_Url = "Url";

		// Token: 0x04000959 RID: 2393
		private const string s_Zone = "Zone";

		// Token: 0x0400095A RID: 2394
		private ulong m_Quota;

		// Token: 0x0400095B RID: 2395
		private bool m_ValidQuota;

		// Token: 0x0400095C RID: 2396
		private object m_DomainIdentity;

		// Token: 0x0400095D RID: 2397
		private object m_AssemIdentity;

		// Token: 0x0400095E RID: 2398
		private object m_AppIdentity;

		// Token: 0x0400095F RID: 2399
		private string m_DomainName;

		// Token: 0x04000960 RID: 2400
		private string m_AssemName;

		// Token: 0x04000961 RID: 2401
		private string m_AppName;

		// Token: 0x04000962 RID: 2402
		private IsolatedStorageScope m_Scope;

		// Token: 0x04000963 RID: 2403
		private static volatile IsolatedStorageFilePermission s_PermDomain;

		// Token: 0x04000964 RID: 2404
		private static volatile IsolatedStorageFilePermission s_PermMachineDomain;

		// Token: 0x04000965 RID: 2405
		private static volatile IsolatedStorageFilePermission s_PermDomainRoaming;

		// Token: 0x04000966 RID: 2406
		private static volatile IsolatedStorageFilePermission s_PermAssem;

		// Token: 0x04000967 RID: 2407
		private static volatile IsolatedStorageFilePermission s_PermMachineAssem;

		// Token: 0x04000968 RID: 2408
		private static volatile IsolatedStorageFilePermission s_PermAssemRoaming;

		// Token: 0x04000969 RID: 2409
		private static volatile IsolatedStorageFilePermission s_PermAppUser;

		// Token: 0x0400096A RID: 2410
		private static volatile IsolatedStorageFilePermission s_PermAppMachine;

		// Token: 0x0400096B RID: 2411
		private static volatile IsolatedStorageFilePermission s_PermAppUserRoaming;

		// Token: 0x0400096C RID: 2412
		private static volatile SecurityPermission s_PermControlEvidence;

		// Token: 0x0400096D RID: 2413
		private static volatile PermissionSet s_PermUnrestricted;
	}
}
