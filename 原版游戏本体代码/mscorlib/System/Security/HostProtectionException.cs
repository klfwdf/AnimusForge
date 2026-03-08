using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace System.Security
{
	// Token: 0x020001F1 RID: 497
	[ComVisible(true)]
	[Serializable]
	public class HostProtectionException : SystemException
	{
		// Token: 0x06001E06 RID: 7686 RVA: 0x00068CFF File Offset: 0x00066EFF
		public HostProtectionException()
		{
			this.m_protected = HostProtectionResource.None;
			this.m_demanded = HostProtectionResource.None;
		}

		// Token: 0x06001E07 RID: 7687 RVA: 0x00068D15 File Offset: 0x00066F15
		public HostProtectionException(string message)
			: base(message)
		{
			this.m_protected = HostProtectionResource.None;
			this.m_demanded = HostProtectionResource.None;
		}

		// Token: 0x06001E08 RID: 7688 RVA: 0x00068D2C File Offset: 0x00066F2C
		public HostProtectionException(string message, Exception e)
			: base(message, e)
		{
			this.m_protected = HostProtectionResource.None;
			this.m_demanded = HostProtectionResource.None;
		}

		// Token: 0x06001E09 RID: 7689 RVA: 0x00068D44 File Offset: 0x00066F44
		protected HostProtectionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.m_protected = (HostProtectionResource)info.GetValue("ProtectedResources", typeof(HostProtectionResource));
			this.m_demanded = (HostProtectionResource)info.GetValue("DemandedResources", typeof(HostProtectionResource));
		}

		// Token: 0x06001E0A RID: 7690 RVA: 0x00068DA7 File Offset: 0x00066FA7
		public HostProtectionException(string message, HostProtectionResource protectedResources, HostProtectionResource demandedResources)
			: base(message)
		{
			base.SetErrorCode(-2146232768);
			this.m_protected = protectedResources;
			this.m_demanded = demandedResources;
		}

		// Token: 0x06001E0B RID: 7691 RVA: 0x00068DC9 File Offset: 0x00066FC9
		private HostProtectionException(HostProtectionResource protectedResources, HostProtectionResource demandedResources)
			: base(SecurityException.GetResString("HostProtection_HostProtection"))
		{
			base.SetErrorCode(-2146232768);
			this.m_protected = protectedResources;
			this.m_demanded = demandedResources;
		}

		// Token: 0x1700035D RID: 861
		// (get) Token: 0x06001E0C RID: 7692 RVA: 0x00068DF4 File Offset: 0x00066FF4
		public HostProtectionResource ProtectedResources
		{
			get
			{
				return this.m_protected;
			}
		}

		// Token: 0x1700035E RID: 862
		// (get) Token: 0x06001E0D RID: 7693 RVA: 0x00068DFC File Offset: 0x00066FFC
		public HostProtectionResource DemandedResources
		{
			get
			{
				return this.m_demanded;
			}
		}

		// Token: 0x06001E0E RID: 7694 RVA: 0x00068E04 File Offset: 0x00067004
		private string ToStringHelper(string resourceString, object attr)
		{
			if (attr == null)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append(Environment.GetResourceString(resourceString));
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append(attr);
			return stringBuilder.ToString();
		}

		// Token: 0x06001E0F RID: 7695 RVA: 0x00068E60 File Offset: 0x00067060
		public override string ToString()
		{
			string value = this.ToStringHelper("HostProtection_ProtectedResources", this.ProtectedResources);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.ToString());
			stringBuilder.Append(value);
			stringBuilder.Append(this.ToStringHelper("HostProtection_DemandedResources", this.DemandedResources));
			return stringBuilder.ToString();
		}

		// Token: 0x06001E10 RID: 7696 RVA: 0x00068EC4 File Offset: 0x000670C4
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("ProtectedResources", this.ProtectedResources, typeof(HostProtectionResource));
			info.AddValue("DemandedResources", this.DemandedResources, typeof(HostProtectionResource));
		}

		// Token: 0x04000A87 RID: 2695
		private HostProtectionResource m_protected;

		// Token: 0x04000A88 RID: 2696
		private HostProtectionResource m_demanded;

		// Token: 0x04000A89 RID: 2697
		private const string ProtectedResourcesName = "ProtectedResources";

		// Token: 0x04000A8A RID: 2698
		private const string DemandedResourcesName = "DemandedResources";
	}
}
