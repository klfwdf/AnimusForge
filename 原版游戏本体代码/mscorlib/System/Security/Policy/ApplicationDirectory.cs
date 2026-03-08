using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000341 RID: 833
	[ComVisible(true)]
	[Serializable]
	public sealed class ApplicationDirectory : EvidenceBase
	{
		// Token: 0x0600296D RID: 10605 RVA: 0x000990A8 File Offset: 0x000972A8
		public ApplicationDirectory(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.m_appDirectory = new URLString(name);
		}

		// Token: 0x0600296E RID: 10606 RVA: 0x000990CA File Offset: 0x000972CA
		private ApplicationDirectory(URLString appDirectory)
		{
			this.m_appDirectory = appDirectory;
		}

		// Token: 0x17000568 RID: 1384
		// (get) Token: 0x0600296F RID: 10607 RVA: 0x000990D9 File Offset: 0x000972D9
		public string Directory
		{
			get
			{
				return this.m_appDirectory.ToString();
			}
		}

		// Token: 0x06002970 RID: 10608 RVA: 0x000990E8 File Offset: 0x000972E8
		public override bool Equals(object o)
		{
			ApplicationDirectory applicationDirectory = o as ApplicationDirectory;
			return applicationDirectory != null && this.m_appDirectory.Equals(applicationDirectory.m_appDirectory);
		}

		// Token: 0x06002971 RID: 10609 RVA: 0x00099112 File Offset: 0x00097312
		public override int GetHashCode()
		{
			return this.m_appDirectory.GetHashCode();
		}

		// Token: 0x06002972 RID: 10610 RVA: 0x0009911F File Offset: 0x0009731F
		public override EvidenceBase Clone()
		{
			return new ApplicationDirectory(this.m_appDirectory);
		}

		// Token: 0x06002973 RID: 10611 RVA: 0x0009912C File Offset: 0x0009732C
		public object Copy()
		{
			return this.Clone();
		}

		// Token: 0x06002974 RID: 10612 RVA: 0x00099134 File Offset: 0x00097334
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("System.Security.Policy.ApplicationDirectory");
			securityElement.AddAttribute("version", "1");
			if (this.m_appDirectory != null)
			{
				securityElement.AddChild(new SecurityElement("Directory", this.m_appDirectory.ToString()));
			}
			return securityElement;
		}

		// Token: 0x06002975 RID: 10613 RVA: 0x00099180 File Offset: 0x00097380
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x0400110A RID: 4362
		private URLString m_appDirectory;
	}
}
