using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000988 RID: 2440
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IMoniker instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("0000000f-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIMoniker
	{
		// Token: 0x060062A4 RID: 25252
		void GetClassID(out Guid pClassID);

		// Token: 0x060062A5 RID: 25253
		[PreserveSig]
		int IsDirty();

		// Token: 0x060062A6 RID: 25254
		void Load(UCOMIStream pStm);

		// Token: 0x060062A7 RID: 25255
		void Save(UCOMIStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);

		// Token: 0x060062A8 RID: 25256
		void GetSizeMax(out long pcbSize);

		// Token: 0x060062A9 RID: 25257
		void BindToObject(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [In] ref Guid riidResult, [MarshalAs(UnmanagedType.Interface)] out object ppvResult);

		// Token: 0x060062AA RID: 25258
		void BindToStorage(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObj);

		// Token: 0x060062AB RID: 25259
		void Reduce(UCOMIBindCtx pbc, int dwReduceHowFar, ref UCOMIMoniker ppmkToLeft, out UCOMIMoniker ppmkReduced);

		// Token: 0x060062AC RID: 25260
		void ComposeWith(UCOMIMoniker pmkRight, [MarshalAs(UnmanagedType.Bool)] bool fOnlyIfNotGeneric, out UCOMIMoniker ppmkComposite);

		// Token: 0x060062AD RID: 25261
		void Enum([MarshalAs(UnmanagedType.Bool)] bool fForward, out UCOMIEnumMoniker ppenumMoniker);

		// Token: 0x060062AE RID: 25262
		void IsEqual(UCOMIMoniker pmkOtherMoniker);

		// Token: 0x060062AF RID: 25263
		void Hash(out int pdwHash);

		// Token: 0x060062B0 RID: 25264
		void IsRunning(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, UCOMIMoniker pmkNewlyRunning);

		// Token: 0x060062B1 RID: 25265
		void GetTimeOfLastChange(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, out FILETIME pFileTime);

		// Token: 0x060062B2 RID: 25266
		void Inverse(out UCOMIMoniker ppmk);

		// Token: 0x060062B3 RID: 25267
		void CommonPrefixWith(UCOMIMoniker pmkOther, out UCOMIMoniker ppmkPrefix);

		// Token: 0x060062B4 RID: 25268
		void RelativePathTo(UCOMIMoniker pmkOther, out UCOMIMoniker ppmkRelPath);

		// Token: 0x060062B5 RID: 25269
		void GetDisplayName(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] out string ppszDisplayName);

		// Token: 0x060062B6 RID: 25270
		void ParseDisplayName(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out int pchEaten, out UCOMIMoniker ppmkOut);

		// Token: 0x060062B7 RID: 25271
		void IsSystemMoniker(out int pdwMksys);
	}
}
