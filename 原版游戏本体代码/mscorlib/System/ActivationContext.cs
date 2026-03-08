using System;
using System.Collections;
using System.Deployment.Internal.Isolation;
using System.Deployment.Internal.Isolation.Manifest;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x020000A3 RID: 163
	[ComVisible(false)]
	[Serializable]
	public sealed class ActivationContext : IDisposable, ISerializable
	{
		// Token: 0x06000969 RID: 2409 RVA: 0x0001E9B9 File Offset: 0x0001CBB9
		private ActivationContext()
		{
		}

		// Token: 0x0600096A RID: 2410 RVA: 0x0001E9C4 File Offset: 0x0001CBC4
		[SecurityCritical]
		private ActivationContext(SerializationInfo info, StreamingContext context)
		{
			string applicationIdentityFullName = (string)info.GetValue("FullName", typeof(string));
			string[] array = (string[])info.GetValue("ManifestPaths", typeof(string[]));
			if (array == null)
			{
				this.CreateFromName(new ApplicationIdentity(applicationIdentityFullName));
				return;
			}
			this.CreateFromNameAndManifests(new ApplicationIdentity(applicationIdentityFullName), array);
		}

		// Token: 0x0600096B RID: 2411 RVA: 0x0001EA2A File Offset: 0x0001CC2A
		internal ActivationContext(ApplicationIdentity applicationIdentity)
		{
			this.CreateFromName(applicationIdentity);
		}

		// Token: 0x0600096C RID: 2412 RVA: 0x0001EA39 File Offset: 0x0001CC39
		internal ActivationContext(ApplicationIdentity applicationIdentity, string[] manifestPaths)
		{
			this.CreateFromNameAndManifests(applicationIdentity, manifestPaths);
		}

		// Token: 0x0600096D RID: 2413 RVA: 0x0001EA4C File Offset: 0x0001CC4C
		[SecuritySafeCritical]
		private void CreateFromName(ApplicationIdentity applicationIdentity)
		{
			if (applicationIdentity == null)
			{
				throw new ArgumentNullException("applicationIdentity");
			}
			this._applicationIdentity = applicationIdentity;
			IEnumDefinitionIdentity enumDefinitionIdentity = this._applicationIdentity.Identity.EnumAppPath();
			this._definitionIdentities = new ArrayList(2);
			IDefinitionIdentity[] array = new IDefinitionIdentity[1];
			while (enumDefinitionIdentity.Next(1U, array) == 1U)
			{
				this._definitionIdentities.Add(array[0]);
			}
			this._definitionIdentities.TrimToSize();
			if (this._definitionIdentities.Count <= 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidAppId"));
			}
			this._manifestPaths = null;
			this._manifests = null;
			this._actContext = IsolationInterop.CreateActContext(this._applicationIdentity.Identity);
			this._form = ActivationContext.ContextForm.StoreBounded;
			this._appRunState = ActivationContext.ApplicationStateDisposition.Undefined;
		}

		// Token: 0x0600096E RID: 2414 RVA: 0x0001EB0C File Offset: 0x0001CD0C
		[SecuritySafeCritical]
		private void CreateFromNameAndManifests(ApplicationIdentity applicationIdentity, string[] manifestPaths)
		{
			if (applicationIdentity == null)
			{
				throw new ArgumentNullException("applicationIdentity");
			}
			if (manifestPaths == null)
			{
				throw new ArgumentNullException("manifestPaths");
			}
			this._applicationIdentity = applicationIdentity;
			IEnumDefinitionIdentity enumDefinitionIdentity = this._applicationIdentity.Identity.EnumAppPath();
			this._manifests = new ArrayList(2);
			this._manifestPaths = new string[manifestPaths.Length];
			IDefinitionIdentity[] array = new IDefinitionIdentity[1];
			int num = 0;
			while (enumDefinitionIdentity.Next(1U, array) == 1U)
			{
				ICMS icms = (ICMS)IsolationInterop.ParseManifest(manifestPaths[num], null, ref IsolationInterop.IID_ICMS);
				if (!IsolationInterop.IdentityAuthority.AreDefinitionsEqual(0U, icms.Identity, array[0]))
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_IllegalAppIdMismatch"));
				}
				this._manifests.Add(icms);
				this._manifestPaths[num] = manifestPaths[num];
				num++;
			}
			if (num != manifestPaths.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IllegalAppId"));
			}
			this._manifests.TrimToSize();
			if (this._manifests.Count <= 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidAppId"));
			}
			this._definitionIdentities = null;
			this._actContext = null;
			this._form = ActivationContext.ContextForm.Loose;
			this._appRunState = ActivationContext.ApplicationStateDisposition.Undefined;
		}

		// Token: 0x0600096F RID: 2415 RVA: 0x0001EC30 File Offset: 0x0001CE30
		~ActivationContext()
		{
			this.Dispose(false);
		}

		// Token: 0x06000970 RID: 2416 RVA: 0x0001EC60 File Offset: 0x0001CE60
		public static ActivationContext CreatePartialActivationContext(ApplicationIdentity identity)
		{
			return new ActivationContext(identity);
		}

		// Token: 0x06000971 RID: 2417 RVA: 0x0001EC68 File Offset: 0x0001CE68
		public static ActivationContext CreatePartialActivationContext(ApplicationIdentity identity, string[] manifestPaths)
		{
			return new ActivationContext(identity, manifestPaths);
		}

		// Token: 0x17000130 RID: 304
		// (get) Token: 0x06000972 RID: 2418 RVA: 0x0001EC71 File Offset: 0x0001CE71
		public ApplicationIdentity Identity
		{
			get
			{
				return this._applicationIdentity;
			}
		}

		// Token: 0x17000131 RID: 305
		// (get) Token: 0x06000973 RID: 2419 RVA: 0x0001EC79 File Offset: 0x0001CE79
		public ActivationContext.ContextForm Form
		{
			get
			{
				return this._form;
			}
		}

		// Token: 0x17000132 RID: 306
		// (get) Token: 0x06000974 RID: 2420 RVA: 0x0001EC81 File Offset: 0x0001CE81
		public byte[] ApplicationManifestBytes
		{
			get
			{
				return this.GetApplicationManifestBytes();
			}
		}

		// Token: 0x17000133 RID: 307
		// (get) Token: 0x06000975 RID: 2421 RVA: 0x0001EC89 File Offset: 0x0001CE89
		public byte[] DeploymentManifestBytes
		{
			get
			{
				return this.GetDeploymentManifestBytes();
			}
		}

		// Token: 0x17000134 RID: 308
		// (get) Token: 0x06000976 RID: 2422 RVA: 0x0001EC91 File Offset: 0x0001CE91
		internal string[] ManifestPaths
		{
			get
			{
				return this._manifestPaths;
			}
		}

		// Token: 0x06000977 RID: 2423 RVA: 0x0001EC99 File Offset: 0x0001CE99
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x17000135 RID: 309
		// (get) Token: 0x06000978 RID: 2424 RVA: 0x0001ECA8 File Offset: 0x0001CEA8
		internal string ApplicationDirectory
		{
			[SecurityCritical]
			get
			{
				if (this._form == ActivationContext.ContextForm.Loose)
				{
					return Path.GetDirectoryName(this._manifestPaths[this._manifestPaths.Length - 1]);
				}
				string result;
				this._actContext.ApplicationBasePath(0U, out result);
				return result;
			}
		}

		// Token: 0x17000136 RID: 310
		// (get) Token: 0x06000979 RID: 2425 RVA: 0x0001ECE4 File Offset: 0x0001CEE4
		internal string DataDirectory
		{
			[SecurityCritical]
			get
			{
				if (this._form == ActivationContext.ContextForm.Loose)
				{
					return null;
				}
				string result;
				this._actContext.GetApplicationStateFilesystemLocation(1U, UIntPtr.Zero, IntPtr.Zero, out result);
				return result;
			}
		}

		// Token: 0x17000137 RID: 311
		// (get) Token: 0x0600097A RID: 2426 RVA: 0x0001ED14 File Offset: 0x0001CF14
		internal ICMS ActivationContextData
		{
			[SecurityCritical]
			get
			{
				return this.ApplicationComponentManifest;
			}
		}

		// Token: 0x17000138 RID: 312
		// (get) Token: 0x0600097B RID: 2427 RVA: 0x0001ED1C File Offset: 0x0001CF1C
		internal ICMS DeploymentComponentManifest
		{
			[SecurityCritical]
			get
			{
				if (this._form == ActivationContext.ContextForm.Loose)
				{
					return (ICMS)this._manifests[0];
				}
				return this.GetComponentManifest((IDefinitionIdentity)this._definitionIdentities[0]);
			}
		}

		// Token: 0x17000139 RID: 313
		// (get) Token: 0x0600097C RID: 2428 RVA: 0x0001ED50 File Offset: 0x0001CF50
		internal ICMS ApplicationComponentManifest
		{
			[SecurityCritical]
			get
			{
				if (this._form == ActivationContext.ContextForm.Loose)
				{
					return (ICMS)this._manifests[this._manifests.Count - 1];
				}
				return this.GetComponentManifest((IDefinitionIdentity)this._definitionIdentities[this._definitionIdentities.Count - 1]);
			}
		}

		// Token: 0x1700013A RID: 314
		// (get) Token: 0x0600097D RID: 2429 RVA: 0x0001EDA6 File Offset: 0x0001CFA6
		internal ActivationContext.ApplicationStateDisposition LastApplicationStateResult
		{
			get
			{
				return this._appRunState;
			}
		}

		// Token: 0x0600097E RID: 2430 RVA: 0x0001EDB0 File Offset: 0x0001CFB0
		[SecurityCritical]
		internal ICMS GetComponentManifest(IDefinitionIdentity component)
		{
			object obj;
			this._actContext.GetComponentManifest(0U, component, ref IsolationInterop.IID_ICMS, out obj);
			return obj as ICMS;
		}

		// Token: 0x0600097F RID: 2431 RVA: 0x0001EDD8 File Offset: 0x0001CFD8
		[SecuritySafeCritical]
		internal byte[] GetDeploymentManifestBytes()
		{
			string manifestPath;
			if (this._form == ActivationContext.ContextForm.Loose)
			{
				manifestPath = this._manifestPaths[0];
			}
			else
			{
				object obj;
				this._actContext.GetComponentManifest(0U, (IDefinitionIdentity)this._definitionIdentities[0], ref IsolationInterop.IID_IManifestInformation, out obj);
				((IManifestInformation)obj).get_FullPath(out manifestPath);
				Marshal.ReleaseComObject(obj);
			}
			return ActivationContext.ReadBytesFromFile(manifestPath);
		}

		// Token: 0x06000980 RID: 2432 RVA: 0x0001EE38 File Offset: 0x0001D038
		[SecuritySafeCritical]
		internal byte[] GetApplicationManifestBytes()
		{
			string manifestPath;
			if (this._form == ActivationContext.ContextForm.Loose)
			{
				manifestPath = this._manifestPaths[this._manifests.Count - 1];
			}
			else
			{
				object obj;
				this._actContext.GetComponentManifest(0U, (IDefinitionIdentity)this._definitionIdentities[1], ref IsolationInterop.IID_IManifestInformation, out obj);
				((IManifestInformation)obj).get_FullPath(out manifestPath);
				Marshal.ReleaseComObject(obj);
			}
			return ActivationContext.ReadBytesFromFile(manifestPath);
		}

		// Token: 0x06000981 RID: 2433 RVA: 0x0001EEA2 File Offset: 0x0001D0A2
		[SecuritySafeCritical]
		internal void PrepareForExecution()
		{
			if (this._form == ActivationContext.ContextForm.Loose)
			{
				return;
			}
			this._actContext.PrepareForExecution(IntPtr.Zero, IntPtr.Zero);
		}

		// Token: 0x06000982 RID: 2434 RVA: 0x0001EEC4 File Offset: 0x0001D0C4
		[SecuritySafeCritical]
		internal ActivationContext.ApplicationStateDisposition SetApplicationState(ActivationContext.ApplicationState s)
		{
			if (this._form == ActivationContext.ContextForm.Loose)
			{
				return ActivationContext.ApplicationStateDisposition.Undefined;
			}
			uint appRunState;
			this._actContext.SetApplicationRunningState(0U, (uint)s, out appRunState);
			this._appRunState = (ActivationContext.ApplicationStateDisposition)appRunState;
			return this._appRunState;
		}

		// Token: 0x06000983 RID: 2435 RVA: 0x0001EEF7 File Offset: 0x0001D0F7
		[SecuritySafeCritical]
		private void Dispose(bool fDisposing)
		{
			this._applicationIdentity = null;
			this._definitionIdentities = null;
			this._manifests = null;
			this._manifestPaths = null;
			if (this._actContext != null)
			{
				Marshal.ReleaseComObject(this._actContext);
			}
		}

		// Token: 0x06000984 RID: 2436 RVA: 0x0001EF2C File Offset: 0x0001D12C
		private static byte[] ReadBytesFromFile(string manifestPath)
		{
			byte[] array = null;
			using (FileStream fileStream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read))
			{
				int num = (int)fileStream.Length;
				array = new byte[num];
				if (fileStream.CanSeek)
				{
					fileStream.Seek(0L, SeekOrigin.Begin);
				}
				fileStream.Read(array, 0, num);
			}
			return array;
		}

		// Token: 0x06000985 RID: 2437 RVA: 0x0001EF8C File Offset: 0x0001D18C
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (this._applicationIdentity != null)
			{
				info.AddValue("FullName", this._applicationIdentity.FullName, typeof(string));
			}
			if (this._manifestPaths != null)
			{
				info.AddValue("ManifestPaths", this._manifestPaths, typeof(string[]));
			}
		}

		// Token: 0x040003BF RID: 959
		private ApplicationIdentity _applicationIdentity;

		// Token: 0x040003C0 RID: 960
		private ArrayList _definitionIdentities;

		// Token: 0x040003C1 RID: 961
		private ArrayList _manifests;

		// Token: 0x040003C2 RID: 962
		private string[] _manifestPaths;

		// Token: 0x040003C3 RID: 963
		private ActivationContext.ContextForm _form;

		// Token: 0x040003C4 RID: 964
		private ActivationContext.ApplicationStateDisposition _appRunState;

		// Token: 0x040003C5 RID: 965
		private IActContext _actContext;

		// Token: 0x040003C6 RID: 966
		private const int DefaultComponentCount = 2;

		// Token: 0x02000AD1 RID: 2769
		public enum ContextForm
		{
			// Token: 0x0400310C RID: 12556
			Loose,
			// Token: 0x0400310D RID: 12557
			StoreBounded
		}

		// Token: 0x02000AD2 RID: 2770
		internal enum ApplicationState
		{
			// Token: 0x0400310F RID: 12559
			Undefined,
			// Token: 0x04003110 RID: 12560
			Starting,
			// Token: 0x04003111 RID: 12561
			Running
		}

		// Token: 0x02000AD3 RID: 2771
		internal enum ApplicationStateDisposition
		{
			// Token: 0x04003113 RID: 12563
			Undefined,
			// Token: 0x04003114 RID: 12564
			Starting,
			// Token: 0x04003115 RID: 12565
			StartingMigrated = 65537,
			// Token: 0x04003116 RID: 12566
			Running = 2,
			// Token: 0x04003117 RID: 12567
			RunningFirstTime = 131074
		}
	}
}
