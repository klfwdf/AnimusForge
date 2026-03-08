using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F8 RID: 760
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class SecurityPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026B6 RID: 9910 RVA: 0x0008CAED File Offset: 0x0008ACED
		public SecurityPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004E4 RID: 1252
		// (get) Token: 0x060026B7 RID: 9911 RVA: 0x0008CAF6 File Offset: 0x0008ACF6
		// (set) Token: 0x060026B8 RID: 9912 RVA: 0x0008CAFE File Offset: 0x0008ACFE
		public SecurityPermissionFlag Flags
		{
			get
			{
				return this.m_flag;
			}
			set
			{
				this.m_flag = value;
			}
		}

		// Token: 0x170004E5 RID: 1253
		// (get) Token: 0x060026B9 RID: 9913 RVA: 0x0008CB07 File Offset: 0x0008AD07
		// (set) Token: 0x060026BA RID: 9914 RVA: 0x0008CB14 File Offset: 0x0008AD14
		public bool Assertion
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.Assertion) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.Assertion) : (this.m_flag & ~SecurityPermissionFlag.Assertion));
			}
		}

		// Token: 0x170004E6 RID: 1254
		// (get) Token: 0x060026BB RID: 9915 RVA: 0x0008CB32 File Offset: 0x0008AD32
		// (set) Token: 0x060026BC RID: 9916 RVA: 0x0008CB3F File Offset: 0x0008AD3F
		public bool UnmanagedCode
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.UnmanagedCode) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.UnmanagedCode) : (this.m_flag & ~SecurityPermissionFlag.UnmanagedCode));
			}
		}

		// Token: 0x170004E7 RID: 1255
		// (get) Token: 0x060026BD RID: 9917 RVA: 0x0008CB5D File Offset: 0x0008AD5D
		// (set) Token: 0x060026BE RID: 9918 RVA: 0x0008CB6A File Offset: 0x0008AD6A
		public bool SkipVerification
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.SkipVerification) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.SkipVerification) : (this.m_flag & ~SecurityPermissionFlag.SkipVerification));
			}
		}

		// Token: 0x170004E8 RID: 1256
		// (get) Token: 0x060026BF RID: 9919 RVA: 0x0008CB88 File Offset: 0x0008AD88
		// (set) Token: 0x060026C0 RID: 9920 RVA: 0x0008CB95 File Offset: 0x0008AD95
		public bool Execution
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.Execution) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.Execution) : (this.m_flag & ~SecurityPermissionFlag.Execution));
			}
		}

		// Token: 0x170004E9 RID: 1257
		// (get) Token: 0x060026C1 RID: 9921 RVA: 0x0008CBB3 File Offset: 0x0008ADB3
		// (set) Token: 0x060026C2 RID: 9922 RVA: 0x0008CBC1 File Offset: 0x0008ADC1
		public bool ControlThread
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.ControlThread) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.ControlThread) : (this.m_flag & ~SecurityPermissionFlag.ControlThread));
			}
		}

		// Token: 0x170004EA RID: 1258
		// (get) Token: 0x060026C3 RID: 9923 RVA: 0x0008CBE0 File Offset: 0x0008ADE0
		// (set) Token: 0x060026C4 RID: 9924 RVA: 0x0008CBEE File Offset: 0x0008ADEE
		public bool ControlEvidence
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.ControlEvidence) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.ControlEvidence) : (this.m_flag & ~SecurityPermissionFlag.ControlEvidence));
			}
		}

		// Token: 0x170004EB RID: 1259
		// (get) Token: 0x060026C5 RID: 9925 RVA: 0x0008CC0D File Offset: 0x0008AE0D
		// (set) Token: 0x060026C6 RID: 9926 RVA: 0x0008CC1B File Offset: 0x0008AE1B
		public bool ControlPolicy
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.ControlPolicy) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.ControlPolicy) : (this.m_flag & ~SecurityPermissionFlag.ControlPolicy));
			}
		}

		// Token: 0x170004EC RID: 1260
		// (get) Token: 0x060026C7 RID: 9927 RVA: 0x0008CC3A File Offset: 0x0008AE3A
		// (set) Token: 0x060026C8 RID: 9928 RVA: 0x0008CC4B File Offset: 0x0008AE4B
		public bool SerializationFormatter
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.SerializationFormatter) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.SerializationFormatter) : (this.m_flag & ~SecurityPermissionFlag.SerializationFormatter));
			}
		}

		// Token: 0x170004ED RID: 1261
		// (get) Token: 0x060026C9 RID: 9929 RVA: 0x0008CC70 File Offset: 0x0008AE70
		// (set) Token: 0x060026CA RID: 9930 RVA: 0x0008CC81 File Offset: 0x0008AE81
		public bool ControlDomainPolicy
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.ControlDomainPolicy) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.ControlDomainPolicy) : (this.m_flag & ~SecurityPermissionFlag.ControlDomainPolicy));
			}
		}

		// Token: 0x170004EE RID: 1262
		// (get) Token: 0x060026CB RID: 9931 RVA: 0x0008CCA6 File Offset: 0x0008AEA6
		// (set) Token: 0x060026CC RID: 9932 RVA: 0x0008CCB7 File Offset: 0x0008AEB7
		public bool ControlPrincipal
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.ControlPrincipal) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.ControlPrincipal) : (this.m_flag & ~SecurityPermissionFlag.ControlPrincipal));
			}
		}

		// Token: 0x170004EF RID: 1263
		// (get) Token: 0x060026CD RID: 9933 RVA: 0x0008CCDC File Offset: 0x0008AEDC
		// (set) Token: 0x060026CE RID: 9934 RVA: 0x0008CCED File Offset: 0x0008AEED
		public bool ControlAppDomain
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.ControlAppDomain) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.ControlAppDomain) : (this.m_flag & ~SecurityPermissionFlag.ControlAppDomain));
			}
		}

		// Token: 0x170004F0 RID: 1264
		// (get) Token: 0x060026CF RID: 9935 RVA: 0x0008CD12 File Offset: 0x0008AF12
		// (set) Token: 0x060026D0 RID: 9936 RVA: 0x0008CD23 File Offset: 0x0008AF23
		public bool RemotingConfiguration
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.RemotingConfiguration) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.RemotingConfiguration) : (this.m_flag & ~SecurityPermissionFlag.RemotingConfiguration));
			}
		}

		// Token: 0x170004F1 RID: 1265
		// (get) Token: 0x060026D1 RID: 9937 RVA: 0x0008CD48 File Offset: 0x0008AF48
		// (set) Token: 0x060026D2 RID: 9938 RVA: 0x0008CD59 File Offset: 0x0008AF59
		[ComVisible(true)]
		public bool Infrastructure
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.Infrastructure) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.Infrastructure) : (this.m_flag & ~SecurityPermissionFlag.Infrastructure));
			}
		}

		// Token: 0x170004F2 RID: 1266
		// (get) Token: 0x060026D3 RID: 9939 RVA: 0x0008CD7E File Offset: 0x0008AF7E
		// (set) Token: 0x060026D4 RID: 9940 RVA: 0x0008CD8F File Offset: 0x0008AF8F
		public bool BindingRedirects
		{
			get
			{
				return (this.m_flag & SecurityPermissionFlag.BindingRedirects) > SecurityPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | SecurityPermissionFlag.BindingRedirects) : (this.m_flag & ~SecurityPermissionFlag.BindingRedirects));
			}
		}

		// Token: 0x060026D5 RID: 9941 RVA: 0x0008CDB4 File Offset: 0x0008AFB4
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new SecurityPermission(PermissionState.Unrestricted);
			}
			return new SecurityPermission(this.m_flag);
		}

		// Token: 0x04000F03 RID: 3843
		private SecurityPermissionFlag m_flag;
	}
}
