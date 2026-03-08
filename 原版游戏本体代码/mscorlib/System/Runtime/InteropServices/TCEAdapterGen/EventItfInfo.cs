using System;
using System.Reflection;

namespace System.Runtime.InteropServices.TCEAdapterGen
{
	// Token: 0x020009C3 RID: 2499
	internal class EventItfInfo
	{
		// Token: 0x060063B2 RID: 25522 RVA: 0x001545BB File Offset: 0x001527BB
		public EventItfInfo(string strEventItfName, string strSrcItfName, string strEventProviderName, RuntimeAssembly asmImport, RuntimeAssembly asmSrcItf)
		{
			this.m_strEventItfName = strEventItfName;
			this.m_strSrcItfName = strSrcItfName;
			this.m_strEventProviderName = strEventProviderName;
			this.m_asmImport = asmImport;
			this.m_asmSrcItf = asmSrcItf;
		}

		// Token: 0x060063B3 RID: 25523 RVA: 0x001545E8 File Offset: 0x001527E8
		public Type GetEventItfType()
		{
			Type type = this.m_asmImport.GetType(this.m_strEventItfName, true, false);
			if (type != null && !type.IsVisible)
			{
				type = null;
			}
			return type;
		}

		// Token: 0x060063B4 RID: 25524 RVA: 0x00154620 File Offset: 0x00152820
		public Type GetSrcItfType()
		{
			Type type = this.m_asmSrcItf.GetType(this.m_strSrcItfName, true, false);
			if (type != null && !type.IsVisible)
			{
				type = null;
			}
			return type;
		}

		// Token: 0x060063B5 RID: 25525 RVA: 0x00154655 File Offset: 0x00152855
		public string GetEventProviderName()
		{
			return this.m_strEventProviderName;
		}

		// Token: 0x04002CD1 RID: 11473
		private string m_strEventItfName;

		// Token: 0x04002CD2 RID: 11474
		private string m_strSrcItfName;

		// Token: 0x04002CD3 RID: 11475
		private string m_strEventProviderName;

		// Token: 0x04002CD4 RID: 11476
		private RuntimeAssembly m_asmImport;

		// Token: 0x04002CD5 RID: 11477
		private RuntimeAssembly m_asmSrcItf;
	}
}
