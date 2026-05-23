using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A31 RID: 2609
	[Guid("0000000f-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[__DynamicallyInvokable]
	[ComImport]
	public interface IMoniker
	{
		// Token: 0x06006637 RID: 26167
		[__DynamicallyInvokable]
		void GetClassID(out Guid pClassID);

		// Token: 0x06006638 RID: 26168
		[__DynamicallyInvokable]
		[PreserveSig]
		int IsDirty();

		// Token: 0x06006639 RID: 26169
		[__DynamicallyInvokable]
		void Load(IStream pStm);

		// Token: 0x0600663A RID: 26170
		[__DynamicallyInvokable]
		void Save(IStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);

		// Token: 0x0600663B RID: 26171
		[__DynamicallyInvokable]
		void GetSizeMax(out long pcbSize);

		// Token: 0x0600663C RID: 26172
		[__DynamicallyInvokable]
		void BindToObject(IBindCtx pbc, IMoniker pmkToLeft, [In] ref Guid riidResult, [MarshalAs(UnmanagedType.Interface)] out object ppvResult);

		// Token: 0x0600663D RID: 26173
		[__DynamicallyInvokable]
		void BindToStorage(IBindCtx pbc, IMoniker pmkToLeft, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObj);

		// Token: 0x0600663E RID: 26174
		[__DynamicallyInvokable]
		void Reduce(IBindCtx pbc, int dwReduceHowFar, ref IMoniker ppmkToLeft, out IMoniker ppmkReduced);

		// Token: 0x0600663F RID: 26175
		[__DynamicallyInvokable]
		void ComposeWith(IMoniker pmkRight, [MarshalAs(UnmanagedType.Bool)] bool fOnlyIfNotGeneric, out IMoniker ppmkComposite);

		// Token: 0x06006640 RID: 26176
		[__DynamicallyInvokable]
		void Enum([MarshalAs(UnmanagedType.Bool)] bool fForward, out IEnumMoniker ppenumMoniker);

		// Token: 0x06006641 RID: 26177
		[__DynamicallyInvokable]
		[PreserveSig]
		int IsEqual(IMoniker pmkOtherMoniker);

		// Token: 0x06006642 RID: 26178
		[__DynamicallyInvokable]
		void Hash(out int pdwHash);

		// Token: 0x06006643 RID: 26179
		[__DynamicallyInvokable]
		[PreserveSig]
		int IsRunning(IBindCtx pbc, IMoniker pmkToLeft, IMoniker pmkNewlyRunning);

		// Token: 0x06006644 RID: 26180
		[__DynamicallyInvokable]
		void GetTimeOfLastChange(IBindCtx pbc, IMoniker pmkToLeft, out FILETIME pFileTime);

		// Token: 0x06006645 RID: 26181
		[__DynamicallyInvokable]
		void Inverse(out IMoniker ppmk);

		// Token: 0x06006646 RID: 26182
		[__DynamicallyInvokable]
		void CommonPrefixWith(IMoniker pmkOther, out IMoniker ppmkPrefix);

		// Token: 0x06006647 RID: 26183
		[__DynamicallyInvokable]
		void RelativePathTo(IMoniker pmkOther, out IMoniker ppmkRelPath);

		// Token: 0x06006648 RID: 26184
		[__DynamicallyInvokable]
		void GetDisplayName(IBindCtx pbc, IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] out string ppszDisplayName);

		// Token: 0x06006649 RID: 26185
		[__DynamicallyInvokable]
		void ParseDisplayName(IBindCtx pbc, IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out int pchEaten, out IMoniker ppmkOut);

		// Token: 0x0600664A RID: 26186
		[__DynamicallyInvokable]
		[PreserveSig]
		int IsSystemMoniker(out int pdwMksys);
	}
}
