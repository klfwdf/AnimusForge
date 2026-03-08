using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200002E RID: 46
	[EngineClass("ftdnNative_string")]
	public sealed class NativeString : NativeObject
	{
		// Token: 0x0600012C RID: 300 RVA: 0x00005724 File Offset: 0x00003924
		internal NativeString(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00005733 File Offset: 0x00003933
		public static NativeString Create()
		{
			return LibraryApplicationInterface.INativeString.Create();
		}

		// Token: 0x0600012E RID: 302 RVA: 0x0000573F File Offset: 0x0000393F
		public string GetString()
		{
			return LibraryApplicationInterface.INativeString.GetString(this);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000574C File Offset: 0x0000394C
		public void SetString(string newString)
		{
			LibraryApplicationInterface.INativeString.SetString(this, newString);
		}
	}
}
