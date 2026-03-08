using System;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace System.Runtime.Hosting
{
	// Token: 0x02000A57 RID: 2647
	[ComVisible(true)]
	[Serializable]
	public sealed class ActivationArguments : EvidenceBase
	{
		// Token: 0x060066C8 RID: 26312 RVA: 0x0015A0E7 File Offset: 0x001582E7
		private ActivationArguments()
		{
		}

		// Token: 0x1700118D RID: 4493
		// (get) Token: 0x060066C9 RID: 26313 RVA: 0x0015A0EF File Offset: 0x001582EF
		internal bool UseFusionActivationContext
		{
			get
			{
				return this.m_useFusionActivationContext;
			}
		}

		// Token: 0x1700118E RID: 4494
		// (get) Token: 0x060066CA RID: 26314 RVA: 0x0015A0F7 File Offset: 0x001582F7
		// (set) Token: 0x060066CB RID: 26315 RVA: 0x0015A0FF File Offset: 0x001582FF
		internal bool ActivateInstance
		{
			get
			{
				return this.m_activateInstance;
			}
			set
			{
				this.m_activateInstance = value;
			}
		}

		// Token: 0x1700118F RID: 4495
		// (get) Token: 0x060066CC RID: 26316 RVA: 0x0015A108 File Offset: 0x00158308
		internal string ApplicationFullName
		{
			get
			{
				return this.m_appFullName;
			}
		}

		// Token: 0x17001190 RID: 4496
		// (get) Token: 0x060066CD RID: 26317 RVA: 0x0015A110 File Offset: 0x00158310
		internal string[] ApplicationManifestPaths
		{
			get
			{
				return this.m_appManifestPaths;
			}
		}

		// Token: 0x060066CE RID: 26318 RVA: 0x0015A118 File Offset: 0x00158318
		public ActivationArguments(ApplicationIdentity applicationIdentity)
			: this(applicationIdentity, null)
		{
		}

		// Token: 0x060066CF RID: 26319 RVA: 0x0015A122 File Offset: 0x00158322
		public ActivationArguments(ApplicationIdentity applicationIdentity, string[] activationData)
		{
			if (applicationIdentity == null)
			{
				throw new ArgumentNullException("applicationIdentity");
			}
			this.m_appFullName = applicationIdentity.FullName;
			this.m_activationData = activationData;
		}

		// Token: 0x060066D0 RID: 26320 RVA: 0x0015A14B File Offset: 0x0015834B
		public ActivationArguments(ActivationContext activationData)
			: this(activationData, null)
		{
		}

		// Token: 0x060066D1 RID: 26321 RVA: 0x0015A158 File Offset: 0x00158358
		public ActivationArguments(ActivationContext activationContext, string[] activationData)
		{
			if (activationContext == null)
			{
				throw new ArgumentNullException("activationContext");
			}
			this.m_appFullName = activationContext.Identity.FullName;
			this.m_appManifestPaths = activationContext.ManifestPaths;
			this.m_activationData = activationData;
			this.m_useFusionActivationContext = true;
		}

		// Token: 0x060066D2 RID: 26322 RVA: 0x0015A1A4 File Offset: 0x001583A4
		internal ActivationArguments(string appFullName, string[] appManifestPaths, string[] activationData)
		{
			if (appFullName == null)
			{
				throw new ArgumentNullException("appFullName");
			}
			this.m_appFullName = appFullName;
			this.m_appManifestPaths = appManifestPaths;
			this.m_activationData = activationData;
			this.m_useFusionActivationContext = true;
		}

		// Token: 0x17001191 RID: 4497
		// (get) Token: 0x060066D3 RID: 26323 RVA: 0x0015A1D6 File Offset: 0x001583D6
		public ApplicationIdentity ApplicationIdentity
		{
			get
			{
				return new ApplicationIdentity(this.m_appFullName);
			}
		}

		// Token: 0x17001192 RID: 4498
		// (get) Token: 0x060066D4 RID: 26324 RVA: 0x0015A1E3 File Offset: 0x001583E3
		public ActivationContext ActivationContext
		{
			get
			{
				if (!this.UseFusionActivationContext)
				{
					return null;
				}
				if (this.m_appManifestPaths == null)
				{
					return new ActivationContext(new ApplicationIdentity(this.m_appFullName));
				}
				return new ActivationContext(new ApplicationIdentity(this.m_appFullName), this.m_appManifestPaths);
			}
		}

		// Token: 0x17001193 RID: 4499
		// (get) Token: 0x060066D5 RID: 26325 RVA: 0x0015A21E File Offset: 0x0015841E
		public string[] ActivationData
		{
			get
			{
				return this.m_activationData;
			}
		}

		// Token: 0x060066D6 RID: 26326 RVA: 0x0015A228 File Offset: 0x00158428
		public override EvidenceBase Clone()
		{
			ActivationArguments activationArguments = new ActivationArguments();
			activationArguments.m_useFusionActivationContext = this.m_useFusionActivationContext;
			activationArguments.m_activateInstance = this.m_activateInstance;
			activationArguments.m_appFullName = this.m_appFullName;
			if (this.m_appManifestPaths != null)
			{
				activationArguments.m_appManifestPaths = new string[this.m_appManifestPaths.Length];
				Array.Copy(this.m_appManifestPaths, activationArguments.m_appManifestPaths, activationArguments.m_appManifestPaths.Length);
			}
			if (this.m_activationData != null)
			{
				activationArguments.m_activationData = new string[this.m_activationData.Length];
				Array.Copy(this.m_activationData, activationArguments.m_activationData, activationArguments.m_activationData.Length);
			}
			activationArguments.m_activateInstance = this.m_activateInstance;
			activationArguments.m_appFullName = this.m_appFullName;
			activationArguments.m_useFusionActivationContext = this.m_useFusionActivationContext;
			return activationArguments;
		}

		// Token: 0x04002E18 RID: 11800
		private bool m_useFusionActivationContext;

		// Token: 0x04002E19 RID: 11801
		private bool m_activateInstance;

		// Token: 0x04002E1A RID: 11802
		private string m_appFullName;

		// Token: 0x04002E1B RID: 11803
		private string[] m_appManifestPaths;

		// Token: 0x04002E1C RID: 11804
		private string[] m_activationData;
	}
}
