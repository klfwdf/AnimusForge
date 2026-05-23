using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Threading;

namespace System.Security.Policy
{
	// Token: 0x0200034D RID: 845
	[ComVisible(true)]
	[Serializable]
	public sealed class Evidence : ICollection, IEnumerable
	{
		// Token: 0x060029FC RID: 10748 RVA: 0x0009B611 File Offset: 0x00099811
		public Evidence()
		{
			this.m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
			this.m_evidenceLock = new ReaderWriterLock();
		}

		// Token: 0x060029FD RID: 10749 RVA: 0x0009B630 File Offset: 0x00099830
		public Evidence(Evidence evidence)
		{
			this.m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
			if (evidence != null)
			{
				using (new Evidence.EvidenceLockHolder(evidence, Evidence.EvidenceLockHolder.LockType.Reader))
				{
					foreach (KeyValuePair<Type, EvidenceTypeDescriptor> keyValuePair in evidence.m_evidence)
					{
						EvidenceTypeDescriptor evidenceTypeDescriptor = keyValuePair.Value;
						if (evidenceTypeDescriptor != null)
						{
							evidenceTypeDescriptor = evidenceTypeDescriptor.Clone();
						}
						this.m_evidence[keyValuePair.Key] = evidenceTypeDescriptor;
					}
					this.m_target = evidence.m_target;
					this.m_locked = evidence.m_locked;
					this.m_deserializedTargetEvidence = evidence.m_deserializedTargetEvidence;
					if (evidence.Target != null)
					{
						this.m_cloneOrigin = new WeakReference(evidence);
					}
				}
			}
			this.m_evidenceLock = new ReaderWriterLock();
		}

		// Token: 0x060029FE RID: 10750 RVA: 0x0009B71C File Offset: 0x0009991C
		[Obsolete("This constructor is obsolete. Please use the constructor which takes arrays of EvidenceBase instead.")]
		public Evidence(object[] hostEvidence, object[] assemblyEvidence)
		{
			this.m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
			if (hostEvidence != null)
			{
				foreach (object id in hostEvidence)
				{
					this.AddHost(id);
				}
			}
			if (assemblyEvidence != null)
			{
				foreach (object id2 in assemblyEvidence)
				{
					this.AddAssembly(id2);
				}
			}
			this.m_evidenceLock = new ReaderWriterLock();
		}

		// Token: 0x060029FF RID: 10751 RVA: 0x0009B788 File Offset: 0x00099988
		public Evidence(EvidenceBase[] hostEvidence, EvidenceBase[] assemblyEvidence)
		{
			this.m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
			if (hostEvidence != null)
			{
				foreach (EvidenceBase evidence in hostEvidence)
				{
					this.AddHostEvidence(evidence, Evidence.GetEvidenceIndexType(evidence), Evidence.DuplicateEvidenceAction.Throw);
				}
			}
			if (assemblyEvidence != null)
			{
				foreach (EvidenceBase evidence2 in assemblyEvidence)
				{
					this.AddAssemblyEvidence(evidence2, Evidence.GetEvidenceIndexType(evidence2), Evidence.DuplicateEvidenceAction.Throw);
				}
			}
			this.m_evidenceLock = new ReaderWriterLock();
		}

		// Token: 0x06002A00 RID: 10752 RVA: 0x0009B804 File Offset: 0x00099A04
		[SecuritySafeCritical]
		internal Evidence(IRuntimeEvidenceFactory target)
		{
			this.m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
			this.m_target = target;
			foreach (Type key in Evidence.RuntimeEvidenceTypes)
			{
				this.m_evidence[key] = null;
			}
			this.QueryHostForPossibleEvidenceTypes();
			this.m_evidenceLock = new ReaderWriterLock();
		}

		// Token: 0x17000588 RID: 1416
		// (get) Token: 0x06002A01 RID: 10753 RVA: 0x0009B860 File Offset: 0x00099A60
		internal static Type[] RuntimeEvidenceTypes
		{
			get
			{
				if (Evidence.s_runtimeEvidenceTypes == null)
				{
					Type[] array = new Type[]
					{
						typeof(ActivationArguments),
						typeof(ApplicationDirectory),
						typeof(ApplicationTrust),
						typeof(GacInstalled),
						typeof(Hash),
						typeof(Publisher),
						typeof(Site),
						typeof(StrongName),
						typeof(Url),
						typeof(Zone)
					};
					if (AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
					{
						int num = array.Length;
						Array.Resize<Type>(ref array, num + 1);
						array[num] = typeof(PermissionRequestEvidence);
					}
					Evidence.s_runtimeEvidenceTypes = array;
				}
				return Evidence.s_runtimeEvidenceTypes;
			}
		}

		// Token: 0x17000589 RID: 1417
		// (get) Token: 0x06002A02 RID: 10754 RVA: 0x0009B93A File Offset: 0x00099B3A
		private bool IsReaderLockHeld
		{
			get
			{
				return this.m_evidenceLock == null || this.m_evidenceLock.IsReaderLockHeld;
			}
		}

		// Token: 0x1700058A RID: 1418
		// (get) Token: 0x06002A03 RID: 10755 RVA: 0x0009B951 File Offset: 0x00099B51
		private bool IsWriterLockHeld
		{
			get
			{
				return this.m_evidenceLock == null || this.m_evidenceLock.IsWriterLockHeld;
			}
		}

		// Token: 0x06002A04 RID: 10756 RVA: 0x0009B968 File Offset: 0x00099B68
		private void AcquireReaderLock()
		{
			if (this.m_evidenceLock != null)
			{
				this.m_evidenceLock.AcquireReaderLock(5000);
			}
		}

		// Token: 0x06002A05 RID: 10757 RVA: 0x0009B982 File Offset: 0x00099B82
		private void AcquireWriterlock()
		{
			if (this.m_evidenceLock != null)
			{
				this.m_evidenceLock.AcquireWriterLock(5000);
			}
		}

		// Token: 0x06002A06 RID: 10758 RVA: 0x0009B99C File Offset: 0x00099B9C
		private void DowngradeFromWriterLock(ref LockCookie lockCookie)
		{
			if (this.m_evidenceLock != null)
			{
				this.m_evidenceLock.DowngradeFromWriterLock(ref lockCookie);
			}
		}

		// Token: 0x06002A07 RID: 10759 RVA: 0x0009B9B4 File Offset: 0x00099BB4
		private LockCookie UpgradeToWriterLock()
		{
			if (this.m_evidenceLock == null)
			{
				return default(LockCookie);
			}
			return this.m_evidenceLock.UpgradeToWriterLock(5000);
		}

		// Token: 0x06002A08 RID: 10760 RVA: 0x0009B9E3 File Offset: 0x00099BE3
		private void ReleaseReaderLock()
		{
			if (this.m_evidenceLock != null)
			{
				this.m_evidenceLock.ReleaseReaderLock();
			}
		}

		// Token: 0x06002A09 RID: 10761 RVA: 0x0009B9F8 File Offset: 0x00099BF8
		private void ReleaseWriterLock()
		{
			if (this.m_evidenceLock != null)
			{
				this.m_evidenceLock.ReleaseWriterLock();
			}
		}

		// Token: 0x06002A0A RID: 10762 RVA: 0x0009BA10 File Offset: 0x00099C10
		[Obsolete("This method is obsolete. Please use AddHostEvidence instead.")]
		[SecuritySafeCritical]
		public void AddHost(object id)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}
			if (!id.GetType().IsSerializable)
			{
				throw new ArgumentException(Environment.GetResourceString("Policy_EvidenceMustBeSerializable"), "id");
			}
			if (this.m_locked)
			{
				new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
			}
			EvidenceBase evidence = Evidence.WrapLegacyEvidence(id);
			Type evidenceIndexType = Evidence.GetEvidenceIndexType(evidence);
			this.AddHostEvidence(evidence, evidenceIndexType, Evidence.DuplicateEvidenceAction.Merge);
		}

		// Token: 0x06002A0B RID: 10763 RVA: 0x0009BA78 File Offset: 0x00099C78
		[Obsolete("This method is obsolete. Please use AddAssemblyEvidence instead.")]
		public void AddAssembly(object id)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}
			if (!id.GetType().IsSerializable)
			{
				throw new ArgumentException(Environment.GetResourceString("Policy_EvidenceMustBeSerializable"), "id");
			}
			EvidenceBase evidence = Evidence.WrapLegacyEvidence(id);
			Type evidenceIndexType = Evidence.GetEvidenceIndexType(evidence);
			this.AddAssemblyEvidence(evidence, evidenceIndexType, Evidence.DuplicateEvidenceAction.Merge);
		}

		// Token: 0x06002A0C RID: 10764 RVA: 0x0009BACC File Offset: 0x00099CCC
		[ComVisible(false)]
		public void AddAssemblyEvidence<T>(T evidence) where T : EvidenceBase
		{
			if (evidence == null)
			{
				throw new ArgumentNullException("evidence");
			}
			Type evidenceType = typeof(T);
			if (typeof(T) == typeof(EvidenceBase) || evidence is ILegacyEvidenceAdapter)
			{
				evidenceType = Evidence.GetEvidenceIndexType(evidence);
			}
			this.AddAssemblyEvidence(evidence, evidenceType, Evidence.DuplicateEvidenceAction.Throw);
		}

		// Token: 0x06002A0D RID: 10765 RVA: 0x0009BB3C File Offset: 0x00099D3C
		private void AddAssemblyEvidence(EvidenceBase evidence, Type evidenceType, Evidence.DuplicateEvidenceAction duplicateAction)
		{
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Writer))
			{
				this.AddAssemblyEvidenceNoLock(evidence, evidenceType, duplicateAction);
			}
		}

		// Token: 0x06002A0E RID: 10766 RVA: 0x0009BB78 File Offset: 0x00099D78
		private void AddAssemblyEvidenceNoLock(EvidenceBase evidence, Type evidenceType, Evidence.DuplicateEvidenceAction duplicateAction)
		{
			this.DeserializeTargetEvidence();
			EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(evidenceType, true);
			this.m_version += 1U;
			if (evidenceTypeDescriptor.AssemblyEvidence == null)
			{
				evidenceTypeDescriptor.AssemblyEvidence = evidence;
				return;
			}
			evidenceTypeDescriptor.AssemblyEvidence = Evidence.HandleDuplicateEvidence(evidenceTypeDescriptor.AssemblyEvidence, evidence, duplicateAction);
		}

		// Token: 0x06002A0F RID: 10767 RVA: 0x0009BBC8 File Offset: 0x00099DC8
		[ComVisible(false)]
		public void AddHostEvidence<T>(T evidence) where T : EvidenceBase
		{
			if (evidence == null)
			{
				throw new ArgumentNullException("evidence");
			}
			Type evidenceType = typeof(T);
			if (typeof(T) == typeof(EvidenceBase) || evidence is ILegacyEvidenceAdapter)
			{
				evidenceType = Evidence.GetEvidenceIndexType(evidence);
			}
			this.AddHostEvidence(evidence, evidenceType, Evidence.DuplicateEvidenceAction.Throw);
		}

		// Token: 0x06002A10 RID: 10768 RVA: 0x0009BC38 File Offset: 0x00099E38
		[SecuritySafeCritical]
		private void AddHostEvidence(EvidenceBase evidence, Type evidenceType, Evidence.DuplicateEvidenceAction duplicateAction)
		{
			if (this.Locked)
			{
				new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
			}
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Writer))
			{
				this.AddHostEvidenceNoLock(evidence, evidenceType, duplicateAction);
			}
		}

		// Token: 0x06002A11 RID: 10769 RVA: 0x0009BC88 File Offset: 0x00099E88
		private void AddHostEvidenceNoLock(EvidenceBase evidence, Type evidenceType, Evidence.DuplicateEvidenceAction duplicateAction)
		{
			EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(evidenceType, true);
			this.m_version += 1U;
			if (evidenceTypeDescriptor.HostEvidence == null)
			{
				evidenceTypeDescriptor.HostEvidence = evidence;
				return;
			}
			evidenceTypeDescriptor.HostEvidence = Evidence.HandleDuplicateEvidence(evidenceTypeDescriptor.HostEvidence, evidence, duplicateAction);
		}

		// Token: 0x06002A12 RID: 10770 RVA: 0x0009BCD0 File Offset: 0x00099ED0
		[SecurityCritical]
		private void QueryHostForPossibleEvidenceTypes()
		{
			if (AppDomain.CurrentDomain.DomainManager != null)
			{
				HostSecurityManager hostSecurityManager = AppDomain.CurrentDomain.DomainManager.HostSecurityManager;
				if (hostSecurityManager != null)
				{
					Type[] array = null;
					AppDomain appDomain = this.m_target.Target as AppDomain;
					Assembly assembly = this.m_target.Target as Assembly;
					if (assembly != null && (hostSecurityManager.Flags & HostSecurityManagerOptions.HostAssemblyEvidence) == HostSecurityManagerOptions.HostAssemblyEvidence)
					{
						array = hostSecurityManager.GetHostSuppliedAssemblyEvidenceTypes(assembly);
					}
					else if (appDomain != null && (hostSecurityManager.Flags & HostSecurityManagerOptions.HostAppDomainEvidence) == HostSecurityManagerOptions.HostAppDomainEvidence)
					{
						array = hostSecurityManager.GetHostSuppliedAppDomainEvidenceTypes();
					}
					if (array != null)
					{
						foreach (Type evidenceType in array)
						{
							EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(evidenceType, true);
							evidenceTypeDescriptor.HostCanGenerate = true;
						}
					}
				}
			}
		}

		// Token: 0x1700058B RID: 1419
		// (get) Token: 0x06002A13 RID: 10771 RVA: 0x0009BD8C File Offset: 0x00099F8C
		internal bool IsUnmodified
		{
			get
			{
				return this.m_version == 0U;
			}
		}

		// Token: 0x1700058C RID: 1420
		// (get) Token: 0x06002A14 RID: 10772 RVA: 0x0009BD97 File Offset: 0x00099F97
		// (set) Token: 0x06002A15 RID: 10773 RVA: 0x0009BD9F File Offset: 0x00099F9F
		public bool Locked
		{
			get
			{
				return this.m_locked;
			}
			[SecuritySafeCritical]
			set
			{
				if (!value)
				{
					new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
					this.m_locked = false;
					return;
				}
				this.m_locked = true;
			}
		}

		// Token: 0x1700058D RID: 1421
		// (get) Token: 0x06002A16 RID: 10774 RVA: 0x0009BDBF File Offset: 0x00099FBF
		// (set) Token: 0x06002A17 RID: 10775 RVA: 0x0009BDC8 File Offset: 0x00099FC8
		internal IRuntimeEvidenceFactory Target
		{
			get
			{
				return this.m_target;
			}
			[SecurityCritical]
			set
			{
				using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Writer))
				{
					this.m_target = value;
					this.QueryHostForPossibleEvidenceTypes();
				}
			}
		}

		// Token: 0x06002A18 RID: 10776 RVA: 0x0009BE08 File Offset: 0x0009A008
		private static Type GetEvidenceIndexType(EvidenceBase evidence)
		{
			ILegacyEvidenceAdapter legacyEvidenceAdapter = evidence as ILegacyEvidenceAdapter;
			if (legacyEvidenceAdapter != null)
			{
				return legacyEvidenceAdapter.EvidenceType;
			}
			return evidence.GetType();
		}

		// Token: 0x06002A19 RID: 10777 RVA: 0x0009BE2C File Offset: 0x0009A02C
		internal EvidenceTypeDescriptor GetEvidenceTypeDescriptor(Type evidenceType)
		{
			return this.GetEvidenceTypeDescriptor(evidenceType, false);
		}

		// Token: 0x06002A1A RID: 10778 RVA: 0x0009BE38 File Offset: 0x0009A038
		private EvidenceTypeDescriptor GetEvidenceTypeDescriptor(Type evidenceType, bool addIfNotExist)
		{
			EvidenceTypeDescriptor evidenceTypeDescriptor = null;
			if (!this.m_evidence.TryGetValue(evidenceType, out evidenceTypeDescriptor) && !addIfNotExist)
			{
				return null;
			}
			if (evidenceTypeDescriptor == null)
			{
				evidenceTypeDescriptor = new EvidenceTypeDescriptor();
				bool flag = false;
				LockCookie lockCookie = default(LockCookie);
				try
				{
					if (!this.IsWriterLockHeld)
					{
						lockCookie = this.UpgradeToWriterLock();
						flag = true;
					}
					this.m_evidence[evidenceType] = evidenceTypeDescriptor;
				}
				finally
				{
					if (flag)
					{
						this.DowngradeFromWriterLock(ref lockCookie);
					}
				}
			}
			return evidenceTypeDescriptor;
		}

		// Token: 0x06002A1B RID: 10779 RVA: 0x0009BEAC File Offset: 0x0009A0AC
		private static EvidenceBase HandleDuplicateEvidence(EvidenceBase original, EvidenceBase duplicate, Evidence.DuplicateEvidenceAction action)
		{
			switch (action)
			{
			case Evidence.DuplicateEvidenceAction.Throw:
				throw new InvalidOperationException(Environment.GetResourceString("Policy_DuplicateEvidence", new object[] { duplicate.GetType().FullName }));
			case Evidence.DuplicateEvidenceAction.Merge:
			{
				LegacyEvidenceList legacyEvidenceList = original as LegacyEvidenceList;
				if (legacyEvidenceList == null)
				{
					legacyEvidenceList = new LegacyEvidenceList();
					legacyEvidenceList.Add(original);
				}
				legacyEvidenceList.Add(duplicate);
				return legacyEvidenceList;
			}
			case Evidence.DuplicateEvidenceAction.SelectNewObject:
				return duplicate;
			default:
				return null;
			}
		}

		// Token: 0x06002A1C RID: 10780 RVA: 0x0009BF14 File Offset: 0x0009A114
		private static EvidenceBase WrapLegacyEvidence(object evidence)
		{
			EvidenceBase evidenceBase = evidence as EvidenceBase;
			if (evidenceBase == null)
			{
				evidenceBase = new LegacyEvidenceWrapper(evidence);
			}
			return evidenceBase;
		}

		// Token: 0x06002A1D RID: 10781 RVA: 0x0009BF34 File Offset: 0x0009A134
		private static object UnwrapEvidence(EvidenceBase evidence)
		{
			ILegacyEvidenceAdapter legacyEvidenceAdapter = evidence as ILegacyEvidenceAdapter;
			if (legacyEvidenceAdapter != null)
			{
				return legacyEvidenceAdapter.EvidenceObject;
			}
			return evidence;
		}

		// Token: 0x06002A1E RID: 10782 RVA: 0x0009BF54 File Offset: 0x0009A154
		[SecuritySafeCritical]
		public void Merge(Evidence evidence)
		{
			if (evidence == null)
			{
				return;
			}
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Writer))
			{
				bool flag = false;
				IEnumerator hostEnumerator = evidence.GetHostEnumerator();
				while (hostEnumerator.MoveNext())
				{
					if (this.Locked && !flag)
					{
						new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
						flag = true;
					}
					Type type = hostEnumerator.Current.GetType();
					if (this.m_evidence.ContainsKey(type))
					{
						this.GetHostEvidenceNoLock(type);
					}
					EvidenceBase evidence2 = Evidence.WrapLegacyEvidence(hostEnumerator.Current);
					this.AddHostEvidenceNoLock(evidence2, Evidence.GetEvidenceIndexType(evidence2), Evidence.DuplicateEvidenceAction.Merge);
				}
				IEnumerator assemblyEnumerator = evidence.GetAssemblyEnumerator();
				while (assemblyEnumerator.MoveNext())
				{
					object evidence3 = assemblyEnumerator.Current;
					EvidenceBase evidence4 = Evidence.WrapLegacyEvidence(evidence3);
					this.AddAssemblyEvidenceNoLock(evidence4, Evidence.GetEvidenceIndexType(evidence4), Evidence.DuplicateEvidenceAction.Merge);
				}
			}
		}

		// Token: 0x06002A1F RID: 10783 RVA: 0x0009C028 File Offset: 0x0009A228
		internal void MergeWithNoDuplicates(Evidence evidence)
		{
			if (evidence == null)
			{
				return;
			}
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Writer))
			{
				IEnumerator hostEnumerator = evidence.GetHostEnumerator();
				while (hostEnumerator.MoveNext())
				{
					object evidence2 = hostEnumerator.Current;
					EvidenceBase evidence3 = Evidence.WrapLegacyEvidence(evidence2);
					this.AddHostEvidenceNoLock(evidence3, Evidence.GetEvidenceIndexType(evidence3), Evidence.DuplicateEvidenceAction.SelectNewObject);
				}
				IEnumerator assemblyEnumerator = evidence.GetAssemblyEnumerator();
				while (assemblyEnumerator.MoveNext())
				{
					object evidence4 = assemblyEnumerator.Current;
					EvidenceBase evidence5 = Evidence.WrapLegacyEvidence(evidence4);
					this.AddAssemblyEvidenceNoLock(evidence5, Evidence.GetEvidenceIndexType(evidence5), Evidence.DuplicateEvidenceAction.SelectNewObject);
				}
			}
		}

		// Token: 0x06002A20 RID: 10784 RVA: 0x0009C0B8 File Offset: 0x0009A2B8
		[ComVisible(false)]
		[OnSerializing]
		[SecurityCritical]
		[PermissionSet(SecurityAction.Assert, Unrestricted = true)]
		private void OnSerializing(StreamingContext context)
		{
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				foreach (Type type in new List<Type>(this.m_evidence.Keys))
				{
					this.GetHostEvidenceNoLock(type);
				}
				this.DeserializeTargetEvidence();
			}
			ArrayList arrayList = new ArrayList();
			IEnumerator hostEnumerator = this.GetHostEnumerator();
			while (hostEnumerator.MoveNext())
			{
				object value = hostEnumerator.Current;
				arrayList.Add(value);
			}
			this.m_hostList = arrayList;
			ArrayList arrayList2 = new ArrayList();
			IEnumerator assemblyEnumerator = this.GetAssemblyEnumerator();
			while (assemblyEnumerator.MoveNext())
			{
				object value2 = assemblyEnumerator.Current;
				arrayList2.Add(value2);
			}
			this.m_assemblyList = arrayList2;
		}

		// Token: 0x06002A21 RID: 10785 RVA: 0x0009C19C File Offset: 0x0009A39C
		[ComVisible(false)]
		[OnDeserialized]
		[SecurityCritical]
		private void OnDeserialized(StreamingContext context)
		{
			if (this.m_evidence == null)
			{
				this.m_evidence = new Dictionary<Type, EvidenceTypeDescriptor>();
				if (this.m_hostList != null)
				{
					foreach (object obj in this.m_hostList)
					{
						if (obj != null)
						{
							this.AddHost(obj);
						}
					}
					this.m_hostList = null;
				}
				if (this.m_assemblyList != null)
				{
					foreach (object obj2 in this.m_assemblyList)
					{
						if (obj2 != null)
						{
							this.AddAssembly(obj2);
						}
					}
					this.m_assemblyList = null;
				}
			}
			this.m_evidenceLock = new ReaderWriterLock();
		}

		// Token: 0x06002A22 RID: 10786 RVA: 0x0009C288 File Offset: 0x0009A488
		private void DeserializeTargetEvidence()
		{
			if (this.m_target != null && !this.m_deserializedTargetEvidence)
			{
				bool flag = false;
				LockCookie lockCookie = default(LockCookie);
				try
				{
					if (!this.IsWriterLockHeld)
					{
						lockCookie = this.UpgradeToWriterLock();
						flag = true;
					}
					this.m_deserializedTargetEvidence = true;
					foreach (EvidenceBase evidence in this.m_target.GetFactorySuppliedEvidence())
					{
						this.AddAssemblyEvidenceNoLock(evidence, Evidence.GetEvidenceIndexType(evidence), Evidence.DuplicateEvidenceAction.Throw);
					}
				}
				finally
				{
					if (flag)
					{
						this.DowngradeFromWriterLock(ref lockCookie);
					}
				}
			}
		}

		// Token: 0x06002A23 RID: 10787 RVA: 0x0009C32C File Offset: 0x0009A52C
		[SecurityCritical]
		internal byte[] RawSerialize()
		{
			byte[] result;
			try
			{
				using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
				{
					Dictionary<Type, EvidenceBase> dictionary = new Dictionary<Type, EvidenceBase>();
					foreach (KeyValuePair<Type, EvidenceTypeDescriptor> keyValuePair in this.m_evidence)
					{
						if (keyValuePair.Value != null && keyValuePair.Value.HostEvidence != null)
						{
							dictionary[keyValuePair.Key] = keyValuePair.Value.HostEvidence;
						}
					}
					using (MemoryStream memoryStream = new MemoryStream())
					{
						BinaryFormatter binaryFormatter = new BinaryFormatter();
						binaryFormatter.Serialize(memoryStream, dictionary);
						result = memoryStream.ToArray();
					}
				}
			}
			catch (SecurityException)
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06002A24 RID: 10788 RVA: 0x0009C41C File Offset: 0x0009A61C
		[Obsolete("Evidence should not be treated as an ICollection. Please use the GetHostEnumerator and GetAssemblyEnumerator methods rather than using CopyTo.")]
		public void CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0 || index > array.Length - this.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			int num = index;
			IEnumerator hostEnumerator = this.GetHostEnumerator();
			while (hostEnumerator.MoveNext())
			{
				object value = hostEnumerator.Current;
				array.SetValue(value, num);
				num++;
			}
			IEnumerator assemblyEnumerator = this.GetAssemblyEnumerator();
			while (assemblyEnumerator.MoveNext())
			{
				object value2 = assemblyEnumerator.Current;
				array.SetValue(value2, num);
				num++;
			}
		}

		// Token: 0x06002A25 RID: 10789 RVA: 0x0009C49C File Offset: 0x0009A69C
		public IEnumerator GetHostEnumerator()
		{
			IEnumerator result;
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				result = new Evidence.EvidenceEnumerator(this, Evidence.EvidenceEnumerator.Category.Host);
			}
			return result;
		}

		// Token: 0x06002A26 RID: 10790 RVA: 0x0009C4D8 File Offset: 0x0009A6D8
		public IEnumerator GetAssemblyEnumerator()
		{
			IEnumerator result;
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				this.DeserializeTargetEvidence();
				result = new Evidence.EvidenceEnumerator(this, Evidence.EvidenceEnumerator.Category.Assembly);
			}
			return result;
		}

		// Token: 0x06002A27 RID: 10791 RVA: 0x0009C518 File Offset: 0x0009A718
		internal Evidence.RawEvidenceEnumerator GetRawAssemblyEvidenceEnumerator()
		{
			this.DeserializeTargetEvidence();
			return new Evidence.RawEvidenceEnumerator(this, new List<Type>(this.m_evidence.Keys), false);
		}

		// Token: 0x06002A28 RID: 10792 RVA: 0x0009C537 File Offset: 0x0009A737
		internal Evidence.RawEvidenceEnumerator GetRawHostEvidenceEnumerator()
		{
			return new Evidence.RawEvidenceEnumerator(this, new List<Type>(this.m_evidence.Keys), true);
		}

		// Token: 0x06002A29 RID: 10793 RVA: 0x0009C550 File Offset: 0x0009A750
		[Obsolete("GetEnumerator is obsolete. Please use GetAssemblyEnumerator and GetHostEnumerator instead.")]
		public IEnumerator GetEnumerator()
		{
			IEnumerator result;
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				result = new Evidence.EvidenceEnumerator(this, Evidence.EvidenceEnumerator.Category.Host | Evidence.EvidenceEnumerator.Category.Assembly);
			}
			return result;
		}

		// Token: 0x06002A2A RID: 10794 RVA: 0x0009C58C File Offset: 0x0009A78C
		[ComVisible(false)]
		public T GetAssemblyEvidence<T>() where T : EvidenceBase
		{
			return Evidence.UnwrapEvidence(this.GetAssemblyEvidence(typeof(T))) as T;
		}

		// Token: 0x06002A2B RID: 10795 RVA: 0x0009C5B0 File Offset: 0x0009A7B0
		internal EvidenceBase GetAssemblyEvidence(Type type)
		{
			EvidenceBase assemblyEvidenceNoLock;
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				assemblyEvidenceNoLock = this.GetAssemblyEvidenceNoLock(type);
			}
			return assemblyEvidenceNoLock;
		}

		// Token: 0x06002A2C RID: 10796 RVA: 0x0009C5EC File Offset: 0x0009A7EC
		private EvidenceBase GetAssemblyEvidenceNoLock(Type type)
		{
			this.DeserializeTargetEvidence();
			EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(type);
			if (evidenceTypeDescriptor != null)
			{
				return evidenceTypeDescriptor.AssemblyEvidence;
			}
			return null;
		}

		// Token: 0x06002A2D RID: 10797 RVA: 0x0009C612 File Offset: 0x0009A812
		[ComVisible(false)]
		public T GetHostEvidence<T>() where T : EvidenceBase
		{
			return Evidence.UnwrapEvidence(this.GetHostEvidence(typeof(T))) as T;
		}

		// Token: 0x06002A2E RID: 10798 RVA: 0x0009C633 File Offset: 0x0009A833
		internal T GetDelayEvaluatedHostEvidence<T>() where T : EvidenceBase, IDelayEvaluatedEvidence
		{
			return Evidence.UnwrapEvidence(this.GetHostEvidence(typeof(T), false)) as T;
		}

		// Token: 0x06002A2F RID: 10799 RVA: 0x0009C655 File Offset: 0x0009A855
		internal EvidenceBase GetHostEvidence(Type type)
		{
			return this.GetHostEvidence(type, true);
		}

		// Token: 0x06002A30 RID: 10800 RVA: 0x0009C660 File Offset: 0x0009A860
		[SecuritySafeCritical]
		private EvidenceBase GetHostEvidence(Type type, bool markDelayEvaluatedEvidenceUsed)
		{
			EvidenceBase result;
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				EvidenceBase hostEvidenceNoLock = this.GetHostEvidenceNoLock(type);
				if (markDelayEvaluatedEvidenceUsed)
				{
					IDelayEvaluatedEvidence delayEvaluatedEvidence = hostEvidenceNoLock as IDelayEvaluatedEvidence;
					if (delayEvaluatedEvidence != null)
					{
						delayEvaluatedEvidence.MarkUsed();
					}
				}
				result = hostEvidenceNoLock;
			}
			return result;
		}

		// Token: 0x06002A31 RID: 10801 RVA: 0x0009C6B0 File Offset: 0x0009A8B0
		[SecurityCritical]
		private EvidenceBase GetHostEvidenceNoLock(Type type)
		{
			EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(type);
			if (evidenceTypeDescriptor == null)
			{
				return null;
			}
			if (evidenceTypeDescriptor.HostEvidence != null)
			{
				return evidenceTypeDescriptor.HostEvidence;
			}
			if (this.m_target != null && !evidenceTypeDescriptor.Generated)
			{
				using (new Evidence.EvidenceUpgradeLockHolder(this))
				{
					evidenceTypeDescriptor.Generated = true;
					EvidenceBase evidenceBase = this.GenerateHostEvidence(type, evidenceTypeDescriptor.HostCanGenerate);
					if (evidenceBase != null)
					{
						evidenceTypeDescriptor.HostEvidence = evidenceBase;
						Evidence evidence = ((this.m_cloneOrigin != null) ? (this.m_cloneOrigin.Target as Evidence) : null);
						if (evidence != null)
						{
							using (new Evidence.EvidenceLockHolder(evidence, Evidence.EvidenceLockHolder.LockType.Writer))
							{
								EvidenceTypeDescriptor evidenceTypeDescriptor2 = evidence.GetEvidenceTypeDescriptor(type);
								if (evidenceTypeDescriptor2 != null && evidenceTypeDescriptor2.HostEvidence == null)
								{
									evidenceTypeDescriptor2.HostEvidence = evidenceBase.Clone();
								}
							}
						}
					}
					return evidenceBase;
				}
			}
			return null;
		}

		// Token: 0x06002A32 RID: 10802 RVA: 0x0009C7A0 File Offset: 0x0009A9A0
		[SecurityCritical]
		private EvidenceBase GenerateHostEvidence(Type type, bool hostCanGenerate)
		{
			if (hostCanGenerate)
			{
				AppDomain appDomain = this.m_target.Target as AppDomain;
				Assembly assembly = this.m_target.Target as Assembly;
				EvidenceBase evidenceBase = null;
				if (appDomain != null)
				{
					evidenceBase = AppDomain.CurrentDomain.HostSecurityManager.GenerateAppDomainEvidence(type);
				}
				else if (assembly != null)
				{
					evidenceBase = AppDomain.CurrentDomain.HostSecurityManager.GenerateAssemblyEvidence(type, assembly);
				}
				if (evidenceBase != null)
				{
					if (!type.IsAssignableFrom(evidenceBase.GetType()))
					{
						string fullName = AppDomain.CurrentDomain.HostSecurityManager.GetType().FullName;
						string fullName2 = evidenceBase.GetType().FullName;
						string fullName3 = type.FullName;
						throw new InvalidOperationException(Environment.GetResourceString("Policy_IncorrectHostEvidence", new object[] { fullName, fullName2, fullName3 }));
					}
					return evidenceBase;
				}
			}
			return this.m_target.GenerateEvidence(type);
		}

		// Token: 0x1700058E RID: 1422
		// (get) Token: 0x06002A33 RID: 10803 RVA: 0x0009C878 File Offset: 0x0009AA78
		[Obsolete("Evidence should not be treated as an ICollection. Please use GetHostEnumerator and GetAssemblyEnumerator to iterate over the evidence to collect a count.")]
		public int Count
		{
			get
			{
				int num = 0;
				IEnumerator hostEnumerator = this.GetHostEnumerator();
				while (hostEnumerator.MoveNext())
				{
					num++;
				}
				IEnumerator assemblyEnumerator = this.GetAssemblyEnumerator();
				while (assemblyEnumerator.MoveNext())
				{
					num++;
				}
				return num;
			}
		}

		// Token: 0x1700058F RID: 1423
		// (get) Token: 0x06002A34 RID: 10804 RVA: 0x0009C8B4 File Offset: 0x0009AAB4
		[ComVisible(false)]
		internal int RawCount
		{
			get
			{
				int num = 0;
				using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
				{
					foreach (Type evidenceType in new List<Type>(this.m_evidence.Keys))
					{
						EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(evidenceType);
						if (evidenceTypeDescriptor != null)
						{
							if (evidenceTypeDescriptor.AssemblyEvidence != null)
							{
								num++;
							}
							if (evidenceTypeDescriptor.HostEvidence != null)
							{
								num++;
							}
						}
					}
				}
				return num;
			}
		}

		// Token: 0x17000590 RID: 1424
		// (get) Token: 0x06002A35 RID: 10805 RVA: 0x0009C954 File Offset: 0x0009AB54
		public object SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x17000591 RID: 1425
		// (get) Token: 0x06002A36 RID: 10806 RVA: 0x0009C957 File Offset: 0x0009AB57
		public bool IsSynchronized
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000592 RID: 1426
		// (get) Token: 0x06002A37 RID: 10807 RVA: 0x0009C95A File Offset: 0x0009AB5A
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06002A38 RID: 10808 RVA: 0x0009C95D File Offset: 0x0009AB5D
		[ComVisible(false)]
		public Evidence Clone()
		{
			return new Evidence(this);
		}

		// Token: 0x06002A39 RID: 10809 RVA: 0x0009C968 File Offset: 0x0009AB68
		[ComVisible(false)]
		[SecuritySafeCritical]
		public void Clear()
		{
			if (this.Locked)
			{
				new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
			}
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Writer))
			{
				this.m_version += 1U;
				this.m_evidence.Clear();
			}
		}

		// Token: 0x06002A3A RID: 10810 RVA: 0x0009C9C8 File Offset: 0x0009ABC8
		[ComVisible(false)]
		[SecuritySafeCritical]
		public void RemoveType(Type t)
		{
			if (t == null)
			{
				throw new ArgumentNullException("t");
			}
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Writer))
			{
				EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(t);
				if (evidenceTypeDescriptor != null)
				{
					this.m_version += 1U;
					if (this.Locked && (evidenceTypeDescriptor.HostEvidence != null || evidenceTypeDescriptor.HostCanGenerate))
					{
						new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
					}
					this.m_evidence.Remove(t);
				}
			}
		}

		// Token: 0x06002A3B RID: 10811 RVA: 0x0009CA58 File Offset: 0x0009AC58
		internal void MarkAllEvidenceAsUsed()
		{
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				foreach (KeyValuePair<Type, EvidenceTypeDescriptor> keyValuePair in this.m_evidence)
				{
					if (keyValuePair.Value != null)
					{
						IDelayEvaluatedEvidence delayEvaluatedEvidence = keyValuePair.Value.HostEvidence as IDelayEvaluatedEvidence;
						if (delayEvaluatedEvidence != null)
						{
							delayEvaluatedEvidence.MarkUsed();
						}
						IDelayEvaluatedEvidence delayEvaluatedEvidence2 = keyValuePair.Value.AssemblyEvidence as IDelayEvaluatedEvidence;
						if (delayEvaluatedEvidence2 != null)
						{
							delayEvaluatedEvidence2.MarkUsed();
						}
					}
				}
			}
		}

		// Token: 0x06002A3C RID: 10812 RVA: 0x0009CB04 File Offset: 0x0009AD04
		private bool WasStrongNameEvidenceUsed()
		{
			bool result;
			using (new Evidence.EvidenceLockHolder(this, Evidence.EvidenceLockHolder.LockType.Reader))
			{
				EvidenceTypeDescriptor evidenceTypeDescriptor = this.GetEvidenceTypeDescriptor(typeof(StrongName));
				if (evidenceTypeDescriptor != null)
				{
					IDelayEvaluatedEvidence delayEvaluatedEvidence = evidenceTypeDescriptor.HostEvidence as IDelayEvaluatedEvidence;
					result = delayEvaluatedEvidence != null && delayEvaluatedEvidence.WasUsed;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x04001132 RID: 4402
		[OptionalField(VersionAdded = 4)]
		private Dictionary<Type, EvidenceTypeDescriptor> m_evidence;

		// Token: 0x04001133 RID: 4403
		[OptionalField(VersionAdded = 4)]
		private bool m_deserializedTargetEvidence;

		// Token: 0x04001134 RID: 4404
		private volatile ArrayList m_hostList;

		// Token: 0x04001135 RID: 4405
		private volatile ArrayList m_assemblyList;

		// Token: 0x04001136 RID: 4406
		[NonSerialized]
		private ReaderWriterLock m_evidenceLock;

		// Token: 0x04001137 RID: 4407
		[NonSerialized]
		private uint m_version;

		// Token: 0x04001138 RID: 4408
		[NonSerialized]
		private IRuntimeEvidenceFactory m_target;

		// Token: 0x04001139 RID: 4409
		private bool m_locked;

		// Token: 0x0400113A RID: 4410
		[NonSerialized]
		private WeakReference m_cloneOrigin;

		// Token: 0x0400113B RID: 4411
		private static volatile Type[] s_runtimeEvidenceTypes;

		// Token: 0x0400113C RID: 4412
		private const int LockTimeout = 5000;

		// Token: 0x02000B5A RID: 2906
		private enum DuplicateEvidenceAction
		{
			// Token: 0x0400341D RID: 13341
			Throw,
			// Token: 0x0400341E RID: 13342
			Merge,
			// Token: 0x0400341F RID: 13343
			SelectNewObject
		}

		// Token: 0x02000B5B RID: 2907
		private class EvidenceLockHolder : IDisposable
		{
			// Token: 0x06006BE7 RID: 27623 RVA: 0x001756E7 File Offset: 0x001738E7
			public EvidenceLockHolder(Evidence target, Evidence.EvidenceLockHolder.LockType lockType)
			{
				this.m_target = target;
				this.m_lockType = lockType;
				if (this.m_lockType == Evidence.EvidenceLockHolder.LockType.Reader)
				{
					this.m_target.AcquireReaderLock();
					return;
				}
				this.m_target.AcquireWriterlock();
			}

			// Token: 0x06006BE8 RID: 27624 RVA: 0x0017571C File Offset: 0x0017391C
			public void Dispose()
			{
				if (this.m_lockType == Evidence.EvidenceLockHolder.LockType.Reader && this.m_target.IsReaderLockHeld)
				{
					this.m_target.ReleaseReaderLock();
					return;
				}
				if (this.m_lockType == Evidence.EvidenceLockHolder.LockType.Writer && this.m_target.IsWriterLockHeld)
				{
					this.m_target.ReleaseWriterLock();
				}
			}

			// Token: 0x04003420 RID: 13344
			private Evidence m_target;

			// Token: 0x04003421 RID: 13345
			private Evidence.EvidenceLockHolder.LockType m_lockType;

			// Token: 0x02000D02 RID: 3330
			public enum LockType
			{
				// Token: 0x0400392F RID: 14639
				Reader,
				// Token: 0x04003930 RID: 14640
				Writer
			}
		}

		// Token: 0x02000B5C RID: 2908
		private class EvidenceUpgradeLockHolder : IDisposable
		{
			// Token: 0x06006BE9 RID: 27625 RVA: 0x0017576B File Offset: 0x0017396B
			public EvidenceUpgradeLockHolder(Evidence target)
			{
				this.m_target = target;
				this.m_cookie = this.m_target.UpgradeToWriterLock();
			}

			// Token: 0x06006BEA RID: 27626 RVA: 0x0017578B File Offset: 0x0017398B
			public void Dispose()
			{
				if (this.m_target.IsWriterLockHeld)
				{
					this.m_target.DowngradeFromWriterLock(ref this.m_cookie);
				}
			}

			// Token: 0x04003422 RID: 13346
			private Evidence m_target;

			// Token: 0x04003423 RID: 13347
			private LockCookie m_cookie;
		}

		// Token: 0x02000B5D RID: 2909
		internal sealed class RawEvidenceEnumerator : IEnumerator<EvidenceBase>, IDisposable, IEnumerator
		{
			// Token: 0x06006BEB RID: 27627 RVA: 0x001757AB File Offset: 0x001739AB
			public RawEvidenceEnumerator(Evidence evidence, IEnumerable<Type> evidenceTypes, bool hostEnumerator)
			{
				this.m_evidence = evidence;
				this.m_hostEnumerator = hostEnumerator;
				this.m_evidenceTypes = Evidence.RawEvidenceEnumerator.GenerateEvidenceTypes(evidence, evidenceTypes, hostEnumerator);
				this.m_evidenceVersion = evidence.m_version;
				this.Reset();
			}

			// Token: 0x17001235 RID: 4661
			// (get) Token: 0x06006BEC RID: 27628 RVA: 0x001757E1 File Offset: 0x001739E1
			public EvidenceBase Current
			{
				get
				{
					if (this.m_evidence.m_version != this.m_evidenceVersion)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					return this.m_currentEvidence;
				}
			}

			// Token: 0x17001236 RID: 4662
			// (get) Token: 0x06006BED RID: 27629 RVA: 0x0017580C File Offset: 0x00173A0C
			object IEnumerator.Current
			{
				get
				{
					if (this.m_evidence.m_version != this.m_evidenceVersion)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					return this.m_currentEvidence;
				}
			}

			// Token: 0x17001237 RID: 4663
			// (get) Token: 0x06006BEE RID: 27630 RVA: 0x00175838 File Offset: 0x00173A38
			private static List<Type> ExpensiveEvidence
			{
				get
				{
					if (Evidence.RawEvidenceEnumerator.s_expensiveEvidence == null)
					{
						Evidence.RawEvidenceEnumerator.s_expensiveEvidence = new List<Type>
						{
							typeof(Hash),
							typeof(Publisher)
						};
					}
					return Evidence.RawEvidenceEnumerator.s_expensiveEvidence;
				}
			}

			// Token: 0x06006BEF RID: 27631 RVA: 0x00175883 File Offset: 0x00173A83
			public void Dispose()
			{
			}

			// Token: 0x06006BF0 RID: 27632 RVA: 0x00175888 File Offset: 0x00173A88
			private static Type[] GenerateEvidenceTypes(Evidence evidence, IEnumerable<Type> evidenceTypes, bool hostEvidence)
			{
				List<Type> list = new List<Type>();
				List<Type> list2 = new List<Type>();
				List<Type> list3 = new List<Type>(Evidence.RawEvidenceEnumerator.ExpensiveEvidence.Count);
				foreach (Type type in evidenceTypes)
				{
					EvidenceTypeDescriptor evidenceTypeDescriptor = evidence.GetEvidenceTypeDescriptor(type);
					bool flag = (hostEvidence && evidenceTypeDescriptor.HostEvidence != null) || (!hostEvidence && evidenceTypeDescriptor.AssemblyEvidence != null);
					if (flag)
					{
						list.Add(type);
					}
					else if (Evidence.RawEvidenceEnumerator.ExpensiveEvidence.Contains(type))
					{
						list3.Add(type);
					}
					else
					{
						list2.Add(type);
					}
				}
				Type[] array = new Type[list.Count + list2.Count + list3.Count];
				list.CopyTo(array, 0);
				list2.CopyTo(array, list.Count);
				list3.CopyTo(array, list.Count + list2.Count);
				return array;
			}

			// Token: 0x06006BF1 RID: 27633 RVA: 0x00175988 File Offset: 0x00173B88
			[SecuritySafeCritical]
			public bool MoveNext()
			{
				using (new Evidence.EvidenceLockHolder(this.m_evidence, Evidence.EvidenceLockHolder.LockType.Reader))
				{
					if (this.m_evidence.m_version != this.m_evidenceVersion)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
					}
					this.m_currentEvidence = null;
					do
					{
						this.m_typeIndex++;
						if (this.m_typeIndex < this.m_evidenceTypes.Length)
						{
							if (this.m_hostEnumerator)
							{
								this.m_currentEvidence = this.m_evidence.GetHostEvidenceNoLock(this.m_evidenceTypes[this.m_typeIndex]);
							}
							else
							{
								this.m_currentEvidence = this.m_evidence.GetAssemblyEvidenceNoLock(this.m_evidenceTypes[this.m_typeIndex]);
							}
						}
					}
					while (this.m_typeIndex < this.m_evidenceTypes.Length && this.m_currentEvidence == null);
				}
				return this.m_currentEvidence != null;
			}

			// Token: 0x06006BF2 RID: 27634 RVA: 0x00175A70 File Offset: 0x00173C70
			public void Reset()
			{
				if (this.m_evidence.m_version != this.m_evidenceVersion)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				this.m_typeIndex = -1;
				this.m_currentEvidence = null;
			}

			// Token: 0x04003424 RID: 13348
			private Evidence m_evidence;

			// Token: 0x04003425 RID: 13349
			private bool m_hostEnumerator;

			// Token: 0x04003426 RID: 13350
			private uint m_evidenceVersion;

			// Token: 0x04003427 RID: 13351
			private Type[] m_evidenceTypes;

			// Token: 0x04003428 RID: 13352
			private int m_typeIndex;

			// Token: 0x04003429 RID: 13353
			private EvidenceBase m_currentEvidence;

			// Token: 0x0400342A RID: 13354
			private static volatile List<Type> s_expensiveEvidence;
		}

		// Token: 0x02000B5E RID: 2910
		private sealed class EvidenceEnumerator : IEnumerator
		{
			// Token: 0x06006BF3 RID: 27635 RVA: 0x00175AA3 File Offset: 0x00173CA3
			internal EvidenceEnumerator(Evidence evidence, Evidence.EvidenceEnumerator.Category category)
			{
				this.m_evidence = evidence;
				this.m_category = category;
				this.ResetNoLock();
			}

			// Token: 0x06006BF4 RID: 27636 RVA: 0x00175AC0 File Offset: 0x00173CC0
			public bool MoveNext()
			{
				IEnumerator currentEnumerator = this.CurrentEnumerator;
				if (currentEnumerator == null)
				{
					this.m_currentEvidence = null;
					return false;
				}
				if (currentEnumerator.MoveNext())
				{
					LegacyEvidenceWrapper legacyEvidenceWrapper = currentEnumerator.Current as LegacyEvidenceWrapper;
					LegacyEvidenceList legacyEvidenceList = currentEnumerator.Current as LegacyEvidenceList;
					if (legacyEvidenceWrapper != null)
					{
						this.m_currentEvidence = legacyEvidenceWrapper.EvidenceObject;
					}
					else if (legacyEvidenceList != null)
					{
						IEnumerator enumerator = legacyEvidenceList.GetEnumerator();
						this.m_enumerators.Push(enumerator);
						this.MoveNext();
					}
					else
					{
						this.m_currentEvidence = currentEnumerator.Current;
					}
					return true;
				}
				this.m_enumerators.Pop();
				return this.MoveNext();
			}

			// Token: 0x17001238 RID: 4664
			// (get) Token: 0x06006BF5 RID: 27637 RVA: 0x00175B50 File Offset: 0x00173D50
			public object Current
			{
				get
				{
					return this.m_currentEvidence;
				}
			}

			// Token: 0x17001239 RID: 4665
			// (get) Token: 0x06006BF6 RID: 27638 RVA: 0x00175B58 File Offset: 0x00173D58
			private IEnumerator CurrentEnumerator
			{
				get
				{
					if (this.m_enumerators.Count <= 0)
					{
						return null;
					}
					return this.m_enumerators.Peek() as IEnumerator;
				}
			}

			// Token: 0x06006BF7 RID: 27639 RVA: 0x00175B7C File Offset: 0x00173D7C
			public void Reset()
			{
				using (new Evidence.EvidenceLockHolder(this.m_evidence, Evidence.EvidenceLockHolder.LockType.Reader))
				{
					this.ResetNoLock();
				}
			}

			// Token: 0x06006BF8 RID: 27640 RVA: 0x00175BB8 File Offset: 0x00173DB8
			private void ResetNoLock()
			{
				this.m_currentEvidence = null;
				this.m_enumerators = new Stack();
				if ((this.m_category & Evidence.EvidenceEnumerator.Category.Host) == Evidence.EvidenceEnumerator.Category.Host)
				{
					this.m_enumerators.Push(this.m_evidence.GetRawHostEvidenceEnumerator());
				}
				if ((this.m_category & Evidence.EvidenceEnumerator.Category.Assembly) == Evidence.EvidenceEnumerator.Category.Assembly)
				{
					this.m_enumerators.Push(this.m_evidence.GetRawAssemblyEvidenceEnumerator());
				}
			}

			// Token: 0x0400342B RID: 13355
			private Evidence m_evidence;

			// Token: 0x0400342C RID: 13356
			private Evidence.EvidenceEnumerator.Category m_category;

			// Token: 0x0400342D RID: 13357
			private Stack m_enumerators;

			// Token: 0x0400342E RID: 13358
			private object m_currentEvidence;

			// Token: 0x02000D03 RID: 3331
			[Flags]
			internal enum Category
			{
				// Token: 0x04003932 RID: 14642
				Host = 1,
				// Token: 0x04003933 RID: 14643
				Assembly = 2
			}
		}
	}
}
