using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002E5 RID: 741
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class HostProtectionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x0600261D RID: 9757 RVA: 0x0008B8DF File Offset: 0x00089ADF
		public HostProtectionAttribute()
			: base(SecurityAction.LinkDemand)
		{
		}

		// Token: 0x0600261E RID: 9758 RVA: 0x0008B8E8 File Offset: 0x00089AE8
		public HostProtectionAttribute(SecurityAction action)
			: base(action)
		{
			if (action != SecurityAction.LinkDemand)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"));
			}
		}

		// Token: 0x170004B1 RID: 1201
		// (get) Token: 0x0600261F RID: 9759 RVA: 0x0008B905 File Offset: 0x00089B05
		// (set) Token: 0x06002620 RID: 9760 RVA: 0x0008B90D File Offset: 0x00089B0D
		public HostProtectionResource Resources
		{
			get
			{
				return this.m_resources;
			}
			set
			{
				this.m_resources = value;
			}
		}

		// Token: 0x170004B2 RID: 1202
		// (get) Token: 0x06002621 RID: 9761 RVA: 0x0008B916 File Offset: 0x00089B16
		// (set) Token: 0x06002622 RID: 9762 RVA: 0x0008B923 File Offset: 0x00089B23
		public bool Synchronization
		{
			get
			{
				return (this.m_resources & HostProtectionResource.Synchronization) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.Synchronization) : (this.m_resources & ~HostProtectionResource.Synchronization));
			}
		}

		// Token: 0x170004B3 RID: 1203
		// (get) Token: 0x06002623 RID: 9763 RVA: 0x0008B941 File Offset: 0x00089B41
		// (set) Token: 0x06002624 RID: 9764 RVA: 0x0008B94E File Offset: 0x00089B4E
		public bool SharedState
		{
			get
			{
				return (this.m_resources & HostProtectionResource.SharedState) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.SharedState) : (this.m_resources & ~HostProtectionResource.SharedState));
			}
		}

		// Token: 0x170004B4 RID: 1204
		// (get) Token: 0x06002625 RID: 9765 RVA: 0x0008B96C File Offset: 0x00089B6C
		// (set) Token: 0x06002626 RID: 9766 RVA: 0x0008B979 File Offset: 0x00089B79
		public bool ExternalProcessMgmt
		{
			get
			{
				return (this.m_resources & HostProtectionResource.ExternalProcessMgmt) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.ExternalProcessMgmt) : (this.m_resources & ~HostProtectionResource.ExternalProcessMgmt));
			}
		}

		// Token: 0x170004B5 RID: 1205
		// (get) Token: 0x06002627 RID: 9767 RVA: 0x0008B997 File Offset: 0x00089B97
		// (set) Token: 0x06002628 RID: 9768 RVA: 0x0008B9A4 File Offset: 0x00089BA4
		public bool SelfAffectingProcessMgmt
		{
			get
			{
				return (this.m_resources & HostProtectionResource.SelfAffectingProcessMgmt) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.SelfAffectingProcessMgmt) : (this.m_resources & ~HostProtectionResource.SelfAffectingProcessMgmt));
			}
		}

		// Token: 0x170004B6 RID: 1206
		// (get) Token: 0x06002629 RID: 9769 RVA: 0x0008B9C2 File Offset: 0x00089BC2
		// (set) Token: 0x0600262A RID: 9770 RVA: 0x0008B9D0 File Offset: 0x00089BD0
		public bool ExternalThreading
		{
			get
			{
				return (this.m_resources & HostProtectionResource.ExternalThreading) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.ExternalThreading) : (this.m_resources & ~HostProtectionResource.ExternalThreading));
			}
		}

		// Token: 0x170004B7 RID: 1207
		// (get) Token: 0x0600262B RID: 9771 RVA: 0x0008B9EF File Offset: 0x00089BEF
		// (set) Token: 0x0600262C RID: 9772 RVA: 0x0008B9FD File Offset: 0x00089BFD
		public bool SelfAffectingThreading
		{
			get
			{
				return (this.m_resources & HostProtectionResource.SelfAffectingThreading) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.SelfAffectingThreading) : (this.m_resources & ~HostProtectionResource.SelfAffectingThreading));
			}
		}

		// Token: 0x170004B8 RID: 1208
		// (get) Token: 0x0600262D RID: 9773 RVA: 0x0008BA1C File Offset: 0x00089C1C
		// (set) Token: 0x0600262E RID: 9774 RVA: 0x0008BA2A File Offset: 0x00089C2A
		[ComVisible(true)]
		public bool SecurityInfrastructure
		{
			get
			{
				return (this.m_resources & HostProtectionResource.SecurityInfrastructure) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.SecurityInfrastructure) : (this.m_resources & ~HostProtectionResource.SecurityInfrastructure));
			}
		}

		// Token: 0x170004B9 RID: 1209
		// (get) Token: 0x0600262F RID: 9775 RVA: 0x0008BA49 File Offset: 0x00089C49
		// (set) Token: 0x06002630 RID: 9776 RVA: 0x0008BA5A File Offset: 0x00089C5A
		public bool UI
		{
			get
			{
				return (this.m_resources & HostProtectionResource.UI) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.UI) : (this.m_resources & ~HostProtectionResource.UI));
			}
		}

		// Token: 0x170004BA RID: 1210
		// (get) Token: 0x06002631 RID: 9777 RVA: 0x0008BA7F File Offset: 0x00089C7F
		// (set) Token: 0x06002632 RID: 9778 RVA: 0x0008BA90 File Offset: 0x00089C90
		public bool MayLeakOnAbort
		{
			get
			{
				return (this.m_resources & HostProtectionResource.MayLeakOnAbort) > HostProtectionResource.None;
			}
			set
			{
				this.m_resources = (value ? (this.m_resources | HostProtectionResource.MayLeakOnAbort) : (this.m_resources & ~HostProtectionResource.MayLeakOnAbort));
			}
		}

		// Token: 0x06002633 RID: 9779 RVA: 0x0008BAB5 File Offset: 0x00089CB5
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new HostProtectionPermission(PermissionState.Unrestricted);
			}
			return new HostProtectionPermission(this.m_resources);
		}

		// Token: 0x04000E9E RID: 3742
		private HostProtectionResource m_resources;
	}
}
