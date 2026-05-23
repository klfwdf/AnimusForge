using System;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000629 RID: 1577
	internal sealed class InternalAssemblyBuilder : RuntimeAssembly
	{
		// Token: 0x06004905 RID: 18693 RVA: 0x00107ECF File Offset: 0x001060CF
		private InternalAssemblyBuilder()
		{
		}

		// Token: 0x06004906 RID: 18694 RVA: 0x00107ED7 File Offset: 0x001060D7
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is InternalAssemblyBuilder)
			{
				return this == obj;
			}
			return obj.Equals(this);
		}

		// Token: 0x06004907 RID: 18695 RVA: 0x00107EF2 File Offset: 0x001060F2
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06004908 RID: 18696 RVA: 0x00107EFA File Offset: 0x001060FA
		public override string[] GetManifestResourceNames()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
		}

		// Token: 0x06004909 RID: 18697 RVA: 0x00107F0B File Offset: 0x0010610B
		public override FileStream GetFile(string name)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
		}

		// Token: 0x0600490A RID: 18698 RVA: 0x00107F1C File Offset: 0x0010611C
		public override FileStream[] GetFiles(bool getResourceModules)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
		}

		// Token: 0x0600490B RID: 18699 RVA: 0x00107F2D File Offset: 0x0010612D
		public override Stream GetManifestResourceStream(Type type, string name)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
		}

		// Token: 0x0600490C RID: 18700 RVA: 0x00107F3E File Offset: 0x0010613E
		public override Stream GetManifestResourceStream(string name)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
		}

		// Token: 0x0600490D RID: 18701 RVA: 0x00107F4F File Offset: 0x0010614F
		public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
		}

		// Token: 0x17000B6B RID: 2923
		// (get) Token: 0x0600490E RID: 18702 RVA: 0x00107F60 File Offset: 0x00106160
		public override string Location
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
			}
		}

		// Token: 0x17000B6C RID: 2924
		// (get) Token: 0x0600490F RID: 18703 RVA: 0x00107F71 File Offset: 0x00106171
		public override string CodeBase
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
			}
		}

		// Token: 0x06004910 RID: 18704 RVA: 0x00107F82 File Offset: 0x00106182
		public override Type[] GetExportedTypes()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
		}

		// Token: 0x17000B6D RID: 2925
		// (get) Token: 0x06004911 RID: 18705 RVA: 0x00107F93 File Offset: 0x00106193
		public override string ImageRuntimeVersion
		{
			get
			{
				return RuntimeEnvironment.GetSystemVersion();
			}
		}
	}
}
