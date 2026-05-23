using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Text
{
	// Token: 0x02000A5B RID: 2651
	[Serializable]
	internal abstract class BaseCodePageEncoding : EncodingNLS, ISerializable
	{
		// Token: 0x06006750 RID: 26448 RVA: 0x0015D011 File Offset: 0x0015B211
		[SecurityCritical]
		internal BaseCodePageEncoding(int codepage)
			: this(codepage, codepage)
		{
		}

		// Token: 0x06006751 RID: 26449 RVA: 0x0015D01B File Offset: 0x0015B21B
		[SecurityCritical]
		internal BaseCodePageEncoding(int codepage, int dataCodePage)
		{
			this.bFlagDataTable = true;
			this.pCodePage = null;
			base..ctor((codepage == 0) ? Win32Native.GetACP() : codepage);
			this.dataTableCodePage = dataCodePage;
			this.LoadCodePageTables();
		}

		// Token: 0x06006752 RID: 26450 RVA: 0x0015D04A File Offset: 0x0015B24A
		[SecurityCritical]
		internal BaseCodePageEncoding(SerializationInfo info, StreamingContext context)
		{
			this.bFlagDataTable = true;
			this.pCodePage = null;
			base..ctor(0);
			throw new ArgumentNullException("this");
		}

		// Token: 0x06006753 RID: 26451 RVA: 0x0015D06C File Offset: 0x0015B26C
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.SerializeEncoding(info, context);
			info.AddValue(this.m_bUseMlangTypeForSerialization ? "m_maxByteSize" : "maxCharSize", this.IsSingleByte ? 1 : 2);
			info.SetType(this.m_bUseMlangTypeForSerialization ? typeof(MLangCodePageEncoding) : typeof(CodePageEncoding));
		}

		// Token: 0x06006754 RID: 26452 RVA: 0x0015D0CC File Offset: 0x0015B2CC
		[SecurityCritical]
		private unsafe void LoadCodePageTables()
		{
			BaseCodePageEncoding.CodePageHeader* ptr = BaseCodePageEncoding.FindCodePage(this.dataTableCodePage);
			if (ptr == null)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoCodepageData", new object[] { this.CodePage }));
			}
			this.pCodePage = ptr;
			this.LoadManagedCodePage();
		}

		// Token: 0x06006755 RID: 26453 RVA: 0x0015D11C File Offset: 0x0015B31C
		[SecurityCritical]
		private unsafe static BaseCodePageEncoding.CodePageHeader* FindCodePage(int codePage)
		{
			for (int i = 0; i < (int)BaseCodePageEncoding.m_pCodePageFileHeader->CodePageCount; i++)
			{
				BaseCodePageEncoding.CodePageIndex* ptr = &BaseCodePageEncoding.m_pCodePageFileHeader->CodePages + i;
				if ((int)ptr->CodePage == codePage)
				{
					return (BaseCodePageEncoding.CodePageHeader*)(BaseCodePageEncoding.m_pCodePageFileHeader + ptr->Offset / sizeof(BaseCodePageEncoding.CodePageDataFileHeader));
				}
			}
			return null;
		}

		// Token: 0x06006756 RID: 26454 RVA: 0x0015D170 File Offset: 0x0015B370
		[SecurityCritical]
		internal unsafe static int GetCodePageByteSize(int codePage)
		{
			BaseCodePageEncoding.CodePageHeader* ptr = BaseCodePageEncoding.FindCodePage(codePage);
			if (ptr == null)
			{
				return 0;
			}
			return (int)ptr->ByteCount;
		}

		// Token: 0x06006757 RID: 26455
		[SecurityCritical]
		protected abstract void LoadManagedCodePage();

		// Token: 0x06006758 RID: 26456 RVA: 0x0015D194 File Offset: 0x0015B394
		[SecurityCritical]
		protected unsafe byte* GetSharedMemory(int iSize)
		{
			string memorySectionName = this.GetMemorySectionName();
			IntPtr intPtr;
			byte* ptr = EncodingTable.nativeCreateOpenFileMapping(memorySectionName, iSize, out intPtr);
			if (ptr == null)
			{
				throw new OutOfMemoryException(Environment.GetResourceString("Arg_OutOfMemoryException"));
			}
			if (intPtr != IntPtr.Zero)
			{
				this.safeMemorySectionHandle = new SafeViewOfFileHandle((IntPtr)((void*)ptr), true);
				this.safeFileMappingHandle = new SafeFileMappingHandle(intPtr, true);
			}
			return ptr;
		}

		// Token: 0x06006759 RID: 26457 RVA: 0x0015D1F4 File Offset: 0x0015B3F4
		[SecurityCritical]
		protected unsafe virtual string GetMemorySectionName()
		{
			int num = (this.bFlagDataTable ? this.dataTableCodePage : this.CodePage);
			return string.Format(CultureInfo.InvariantCulture, "NLS_CodePage_{0}_{1}_{2}_{3}_{4}", new object[]
			{
				num,
				this.pCodePage->VersionMajor,
				this.pCodePage->VersionMinor,
				this.pCodePage->VersionRevision,
				this.pCodePage->VersionBuild
			});
		}

		// Token: 0x0600675A RID: 26458
		[SecurityCritical]
		protected abstract void ReadBestFitTable();

		// Token: 0x0600675B RID: 26459 RVA: 0x0015D284 File Offset: 0x0015B484
		[SecuritySafeCritical]
		internal override char[] GetBestFitUnicodeToBytesData()
		{
			if (this.arrayUnicodeBestFit == null)
			{
				this.ReadBestFitTable();
			}
			return this.arrayUnicodeBestFit;
		}

		// Token: 0x0600675C RID: 26460 RVA: 0x0015D29A File Offset: 0x0015B49A
		[SecuritySafeCritical]
		internal override char[] GetBestFitBytesToUnicodeData()
		{
			if (this.arrayBytesBestFit == null)
			{
				this.ReadBestFitTable();
			}
			return this.arrayBytesBestFit;
		}

		// Token: 0x0600675D RID: 26461 RVA: 0x0015D2B0 File Offset: 0x0015B4B0
		[SecurityCritical]
		internal void CheckMemorySection()
		{
			if (this.safeMemorySectionHandle != null && this.safeMemorySectionHandle.DangerousGetHandle() == IntPtr.Zero)
			{
				this.LoadManagedCodePage();
			}
		}

		// Token: 0x04002E2A RID: 11818
		internal const string CODE_PAGE_DATA_FILE_NAME = "codepages.nlp";

		// Token: 0x04002E2B RID: 11819
		[NonSerialized]
		protected int dataTableCodePage;

		// Token: 0x04002E2C RID: 11820
		[NonSerialized]
		protected bool bFlagDataTable;

		// Token: 0x04002E2D RID: 11821
		[NonSerialized]
		protected int iExtraBytes;

		// Token: 0x04002E2E RID: 11822
		[NonSerialized]
		protected char[] arrayUnicodeBestFit;

		// Token: 0x04002E2F RID: 11823
		[NonSerialized]
		protected char[] arrayBytesBestFit;

		// Token: 0x04002E30 RID: 11824
		[NonSerialized]
		protected bool m_bUseMlangTypeForSerialization;

		// Token: 0x04002E31 RID: 11825
		[SecurityCritical]
		private unsafe static BaseCodePageEncoding.CodePageDataFileHeader* m_pCodePageFileHeader = (BaseCodePageEncoding.CodePageDataFileHeader*)GlobalizationAssembly.GetGlobalizationResourceBytePtr(typeof(CharUnicodeInfo).Assembly, "codepages.nlp");

		// Token: 0x04002E32 RID: 11826
		[SecurityCritical]
		[NonSerialized]
		protected unsafe BaseCodePageEncoding.CodePageHeader* pCodePage;

		// Token: 0x04002E33 RID: 11827
		[SecurityCritical]
		[NonSerialized]
		protected SafeViewOfFileHandle safeMemorySectionHandle;

		// Token: 0x04002E34 RID: 11828
		[SecurityCritical]
		[NonSerialized]
		protected SafeFileMappingHandle safeFileMappingHandle;

		// Token: 0x02000CAC RID: 3244
		[StructLayout(LayoutKind.Explicit)]
		internal struct CodePageDataFileHeader
		{
			// Token: 0x04003891 RID: 14481
			[FieldOffset(0)]
			internal char TableName;

			// Token: 0x04003892 RID: 14482
			[FieldOffset(32)]
			internal ushort Version;

			// Token: 0x04003893 RID: 14483
			[FieldOffset(40)]
			internal short CodePageCount;

			// Token: 0x04003894 RID: 14484
			[FieldOffset(42)]
			internal short unused1;

			// Token: 0x04003895 RID: 14485
			[FieldOffset(44)]
			internal BaseCodePageEncoding.CodePageIndex CodePages;
		}

		// Token: 0x02000CAD RID: 3245
		[StructLayout(LayoutKind.Explicit, Pack = 2)]
		internal struct CodePageIndex
		{
			// Token: 0x04003896 RID: 14486
			[FieldOffset(0)]
			internal char CodePageName;

			// Token: 0x04003897 RID: 14487
			[FieldOffset(32)]
			internal short CodePage;

			// Token: 0x04003898 RID: 14488
			[FieldOffset(34)]
			internal short ByteCount;

			// Token: 0x04003899 RID: 14489
			[FieldOffset(36)]
			internal int Offset;
		}

		// Token: 0x02000CAE RID: 3246
		[StructLayout(LayoutKind.Explicit)]
		internal struct CodePageHeader
		{
			// Token: 0x0400389A RID: 14490
			[FieldOffset(0)]
			internal char CodePageName;

			// Token: 0x0400389B RID: 14491
			[FieldOffset(32)]
			internal ushort VersionMajor;

			// Token: 0x0400389C RID: 14492
			[FieldOffset(34)]
			internal ushort VersionMinor;

			// Token: 0x0400389D RID: 14493
			[FieldOffset(36)]
			internal ushort VersionRevision;

			// Token: 0x0400389E RID: 14494
			[FieldOffset(38)]
			internal ushort VersionBuild;

			// Token: 0x0400389F RID: 14495
			[FieldOffset(40)]
			internal short CodePage;

			// Token: 0x040038A0 RID: 14496
			[FieldOffset(42)]
			internal short ByteCount;

			// Token: 0x040038A1 RID: 14497
			[FieldOffset(44)]
			internal char UnicodeReplace;

			// Token: 0x040038A2 RID: 14498
			[FieldOffset(46)]
			internal ushort ByteReplace;

			// Token: 0x040038A3 RID: 14499
			[FieldOffset(48)]
			internal short FirstDataWord;
		}
	}
}
