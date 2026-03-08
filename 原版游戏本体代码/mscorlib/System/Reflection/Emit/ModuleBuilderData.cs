using System;
using System.IO;
using System.Security;

namespace System.Reflection.Emit
{
	// Token: 0x02000652 RID: 1618
	[Serializable]
	internal class ModuleBuilderData
	{
		// Token: 0x06004C9E RID: 19614 RVA: 0x00115AD7 File Offset: 0x00113CD7
		[SecurityCritical]
		internal ModuleBuilderData(ModuleBuilder module, string strModuleName, string strFileName, int tkFile)
		{
			this.m_globalTypeBuilder = new TypeBuilder(module);
			this.m_module = module;
			this.m_tkFile = tkFile;
			this.InitNames(strModuleName, strFileName);
		}

		// Token: 0x06004C9F RID: 19615 RVA: 0x00115B04 File Offset: 0x00113D04
		[SecurityCritical]
		private void InitNames(string strModuleName, string strFileName)
		{
			this.m_strModuleName = strModuleName;
			if (strFileName == null)
			{
				this.m_strFileName = strModuleName;
				return;
			}
			string extension = Path.GetExtension(strFileName);
			if (extension == null || extension == string.Empty)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NoModuleFileExtension", new object[] { strFileName }));
			}
			this.m_strFileName = strFileName;
		}

		// Token: 0x06004CA0 RID: 19616 RVA: 0x00115B5B File Offset: 0x00113D5B
		[SecurityCritical]
		internal virtual void ModifyModuleName(string strModuleName)
		{
			this.InitNames(strModuleName, null);
		}

		// Token: 0x17000BF9 RID: 3065
		// (get) Token: 0x06004CA1 RID: 19617 RVA: 0x00115B65 File Offset: 0x00113D65
		// (set) Token: 0x06004CA2 RID: 19618 RVA: 0x00115B6D File Offset: 0x00113D6D
		internal int FileToken
		{
			get
			{
				return this.m_tkFile;
			}
			set
			{
				this.m_tkFile = value;
			}
		}

		// Token: 0x04001F5C RID: 8028
		internal string m_strModuleName;

		// Token: 0x04001F5D RID: 8029
		internal string m_strFileName;

		// Token: 0x04001F5E RID: 8030
		internal bool m_fGlobalBeenCreated;

		// Token: 0x04001F5F RID: 8031
		internal bool m_fHasGlobal;

		// Token: 0x04001F60 RID: 8032
		[NonSerialized]
		internal TypeBuilder m_globalTypeBuilder;

		// Token: 0x04001F61 RID: 8033
		[NonSerialized]
		internal ModuleBuilder m_module;

		// Token: 0x04001F62 RID: 8034
		private int m_tkFile;

		// Token: 0x04001F63 RID: 8035
		internal bool m_isSaved;

		// Token: 0x04001F64 RID: 8036
		[NonSerialized]
		internal ResWriterData m_embeddedRes;

		// Token: 0x04001F65 RID: 8037
		internal const string MULTI_BYTE_VALUE_CLASS = "$ArrayType$";

		// Token: 0x04001F66 RID: 8038
		internal string m_strResourceFileName;

		// Token: 0x04001F67 RID: 8039
		internal byte[] m_resourceBytes;
	}
}
