using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002EC RID: 748
	internal sealed class Document : DebugInformation
	{
		// Token: 0x170004DB RID: 1243
		// (get) Token: 0x06001336 RID: 4918 RVA: 0x0003D4BE File Offset: 0x0003B6BE
		// (set) Token: 0x06001337 RID: 4919 RVA: 0x0003D4C6 File Offset: 0x0003B6C6
		public string Url
		{
			get
			{
				return this.url;
			}
			set
			{
				this.url = value;
			}
		}

		// Token: 0x170004DC RID: 1244
		// (get) Token: 0x06001338 RID: 4920 RVA: 0x0003D4CF File Offset: 0x0003B6CF
		// (set) Token: 0x06001339 RID: 4921 RVA: 0x0003D4DC File Offset: 0x0003B6DC
		public DocumentType Type
		{
			get
			{
				return this.type.ToType();
			}
			set
			{
				this.type = value.ToGuid();
			}
		}

		// Token: 0x170004DD RID: 1245
		// (get) Token: 0x0600133A RID: 4922 RVA: 0x0003D4EA File Offset: 0x0003B6EA
		// (set) Token: 0x0600133B RID: 4923 RVA: 0x0003D4F2 File Offset: 0x0003B6F2
		public Guid TypeGuid
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		// Token: 0x170004DE RID: 1246
		// (get) Token: 0x0600133C RID: 4924 RVA: 0x0003D4FB File Offset: 0x0003B6FB
		// (set) Token: 0x0600133D RID: 4925 RVA: 0x0003D508 File Offset: 0x0003B708
		public DocumentHashAlgorithm HashAlgorithm
		{
			get
			{
				return this.hash_algorithm.ToHashAlgorithm();
			}
			set
			{
				this.hash_algorithm = value.ToGuid();
			}
		}

		// Token: 0x170004DF RID: 1247
		// (get) Token: 0x0600133E RID: 4926 RVA: 0x0003D516 File Offset: 0x0003B716
		// (set) Token: 0x0600133F RID: 4927 RVA: 0x0003D51E File Offset: 0x0003B71E
		public Guid HashAlgorithmGuid
		{
			get
			{
				return this.hash_algorithm;
			}
			set
			{
				this.hash_algorithm = value;
			}
		}

		// Token: 0x170004E0 RID: 1248
		// (get) Token: 0x06001340 RID: 4928 RVA: 0x0003D527 File Offset: 0x0003B727
		// (set) Token: 0x06001341 RID: 4929 RVA: 0x0003D534 File Offset: 0x0003B734
		public DocumentLanguage Language
		{
			get
			{
				return this.language.ToLanguage();
			}
			set
			{
				this.language = value.ToGuid();
			}
		}

		// Token: 0x170004E1 RID: 1249
		// (get) Token: 0x06001342 RID: 4930 RVA: 0x0003D542 File Offset: 0x0003B742
		// (set) Token: 0x06001343 RID: 4931 RVA: 0x0003D54A File Offset: 0x0003B74A
		public Guid LanguageGuid
		{
			get
			{
				return this.language;
			}
			set
			{
				this.language = value;
			}
		}

		// Token: 0x170004E2 RID: 1250
		// (get) Token: 0x06001344 RID: 4932 RVA: 0x0003D553 File Offset: 0x0003B753
		// (set) Token: 0x06001345 RID: 4933 RVA: 0x0003D560 File Offset: 0x0003B760
		public DocumentLanguageVendor LanguageVendor
		{
			get
			{
				return this.language_vendor.ToVendor();
			}
			set
			{
				this.language_vendor = value.ToGuid();
			}
		}

		// Token: 0x170004E3 RID: 1251
		// (get) Token: 0x06001346 RID: 4934 RVA: 0x0003D56E File Offset: 0x0003B76E
		// (set) Token: 0x06001347 RID: 4935 RVA: 0x0003D576 File Offset: 0x0003B776
		public Guid LanguageVendorGuid
		{
			get
			{
				return this.language_vendor;
			}
			set
			{
				this.language_vendor = value;
			}
		}

		// Token: 0x170004E4 RID: 1252
		// (get) Token: 0x06001348 RID: 4936 RVA: 0x0003D57F File Offset: 0x0003B77F
		// (set) Token: 0x06001349 RID: 4937 RVA: 0x0003D587 File Offset: 0x0003B787
		public byte[] Hash
		{
			get
			{
				return this.hash;
			}
			set
			{
				this.hash = value;
			}
		}

		// Token: 0x170004E5 RID: 1253
		// (get) Token: 0x0600134A RID: 4938 RVA: 0x0003D590 File Offset: 0x0003B790
		// (set) Token: 0x0600134B RID: 4939 RVA: 0x0003D598 File Offset: 0x0003B798
		public byte[] EmbeddedSource
		{
			get
			{
				return this.embedded_source;
			}
			set
			{
				this.embedded_source = value;
			}
		}

		// Token: 0x0600134C RID: 4940 RVA: 0x0003D5A1 File Offset: 0x0003B7A1
		public Document(string url)
		{
			this.url = url;
			this.hash = Empty<byte>.Array;
			this.embedded_source = Empty<byte>.Array;
			this.token = new MetadataToken(TokenType.Document);
		}

		// Token: 0x04000896 RID: 2198
		private string url;

		// Token: 0x04000897 RID: 2199
		private Guid type;

		// Token: 0x04000898 RID: 2200
		private Guid hash_algorithm;

		// Token: 0x04000899 RID: 2201
		private Guid language;

		// Token: 0x0400089A RID: 2202
		private Guid language_vendor;

		// Token: 0x0400089B RID: 2203
		private byte[] hash;

		// Token: 0x0400089C RID: 2204
		private byte[] embedded_source;
	}
}
