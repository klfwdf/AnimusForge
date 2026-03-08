using System;
using System.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000304 RID: 772
	internal static class PdbGuidMapping
	{
		// Token: 0x0600141C RID: 5148 RVA: 0x00040848 File Offset: 0x0003EA48
		static PdbGuidMapping()
		{
			PdbGuidMapping.AddMapping(DocumentLanguage.C, new Guid("63a08714-fc37-11d2-904c-00c04fa302a1"));
			PdbGuidMapping.AddMapping(DocumentLanguage.Cpp, new Guid("3a12d0b7-c26c-11d0-b442-00a0244a1dd2"));
			PdbGuidMapping.AddMapping(DocumentLanguage.CSharp, new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1"));
			PdbGuidMapping.AddMapping(DocumentLanguage.Basic, new Guid("3a12d0b8-c26c-11d0-b442-00a0244a1dd2"));
			PdbGuidMapping.AddMapping(DocumentLanguage.Java, new Guid("3a12d0b4-c26c-11d0-b442-00a0244a1dd2"));
			PdbGuidMapping.AddMapping(DocumentLanguage.Cobol, new Guid("af046cd1-d0e1-11d2-977c-00a0c9b4d50c"));
			PdbGuidMapping.AddMapping(DocumentLanguage.Pascal, new Guid("af046cd2-d0e1-11d2-977c-00a0c9b4d50c"));
			PdbGuidMapping.AddMapping(DocumentLanguage.Cil, new Guid("af046cd3-d0e1-11d2-977c-00a0c9b4d50c"));
			PdbGuidMapping.AddMapping(DocumentLanguage.JScript, new Guid("3a12d0b6-c26c-11d0-b442-00a0244a1dd2"));
			PdbGuidMapping.AddMapping(DocumentLanguage.Smc, new Guid("0d9b9f7b-6611-11d3-bd2a-0000f80849bd"));
			PdbGuidMapping.AddMapping(DocumentLanguage.MCpp, new Guid("4b35fde8-07c6-11d3-9053-00c04fa302a1"));
			PdbGuidMapping.AddMapping(DocumentLanguage.FSharp, new Guid("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3"));
		}

		// Token: 0x0600141D RID: 5149 RVA: 0x00040978 File Offset: 0x0003EB78
		private static void AddMapping(DocumentLanguage language, Guid guid)
		{
			PdbGuidMapping.guid_language.Add(guid, language);
			PdbGuidMapping.language_guid.Add(language, guid);
		}

		// Token: 0x0600141E RID: 5150 RVA: 0x00040992 File Offset: 0x0003EB92
		public static DocumentType ToType(this Guid guid)
		{
			if (guid == PdbGuidMapping.type_text)
			{
				return DocumentType.Text;
			}
			return DocumentType.Other;
		}

		// Token: 0x0600141F RID: 5151 RVA: 0x000409A4 File Offset: 0x0003EBA4
		public static Guid ToGuid(this DocumentType type)
		{
			if (type == DocumentType.Text)
			{
				return PdbGuidMapping.type_text;
			}
			return default(Guid);
		}

		// Token: 0x06001420 RID: 5152 RVA: 0x000409C4 File Offset: 0x0003EBC4
		public static DocumentHashAlgorithm ToHashAlgorithm(this Guid guid)
		{
			if (guid == PdbGuidMapping.hash_md5)
			{
				return DocumentHashAlgorithm.MD5;
			}
			if (guid == PdbGuidMapping.hash_sha1)
			{
				return DocumentHashAlgorithm.SHA1;
			}
			if (guid == PdbGuidMapping.hash_sha256)
			{
				return DocumentHashAlgorithm.SHA256;
			}
			return DocumentHashAlgorithm.None;
		}

		// Token: 0x06001421 RID: 5153 RVA: 0x000409F4 File Offset: 0x0003EBF4
		public static Guid ToGuid(this DocumentHashAlgorithm hash_algo)
		{
			if (hash_algo == DocumentHashAlgorithm.MD5)
			{
				return PdbGuidMapping.hash_md5;
			}
			if (hash_algo == DocumentHashAlgorithm.SHA1)
			{
				return PdbGuidMapping.hash_sha1;
			}
			if (hash_algo == DocumentHashAlgorithm.SHA256)
			{
				return PdbGuidMapping.hash_sha256;
			}
			return default(Guid);
		}

		// Token: 0x06001422 RID: 5154 RVA: 0x00040A28 File Offset: 0x0003EC28
		public static DocumentLanguage ToLanguage(this Guid guid)
		{
			DocumentLanguage language;
			if (!PdbGuidMapping.guid_language.TryGetValue(guid, out language))
			{
				return DocumentLanguage.Other;
			}
			return language;
		}

		// Token: 0x06001423 RID: 5155 RVA: 0x00040A48 File Offset: 0x0003EC48
		public static Guid ToGuid(this DocumentLanguage language)
		{
			Guid guid;
			if (!PdbGuidMapping.language_guid.TryGetValue(language, out guid))
			{
				return default(Guid);
			}
			return guid;
		}

		// Token: 0x06001424 RID: 5156 RVA: 0x00040A6F File Offset: 0x0003EC6F
		public static DocumentLanguageVendor ToVendor(this Guid guid)
		{
			if (guid == PdbGuidMapping.vendor_ms)
			{
				return DocumentLanguageVendor.Microsoft;
			}
			return DocumentLanguageVendor.Other;
		}

		// Token: 0x06001425 RID: 5157 RVA: 0x00040A84 File Offset: 0x0003EC84
		public static Guid ToGuid(this DocumentLanguageVendor vendor)
		{
			if (vendor == DocumentLanguageVendor.Microsoft)
			{
				return PdbGuidMapping.vendor_ms;
			}
			return default(Guid);
		}

		// Token: 0x040009FA RID: 2554
		private static readonly Dictionary<Guid, DocumentLanguage> guid_language = new Dictionary<Guid, DocumentLanguage>();

		// Token: 0x040009FB RID: 2555
		private static readonly Dictionary<DocumentLanguage, Guid> language_guid = new Dictionary<DocumentLanguage, Guid>();

		// Token: 0x040009FC RID: 2556
		private static readonly Guid type_text = new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd");

		// Token: 0x040009FD RID: 2557
		private static readonly Guid hash_md5 = new Guid("406ea660-64cf-4c82-b6f0-42d48172a799");

		// Token: 0x040009FE RID: 2558
		private static readonly Guid hash_sha1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");

		// Token: 0x040009FF RID: 2559
		private static readonly Guid hash_sha256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");

		// Token: 0x04000A00 RID: 2560
		private static readonly Guid vendor_ms = new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1");
	}
}
