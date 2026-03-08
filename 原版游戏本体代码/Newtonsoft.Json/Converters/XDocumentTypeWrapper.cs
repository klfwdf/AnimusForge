using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000FB RID: 251
	[NullableContext(2)]
	[Nullable(0)]
	internal class XDocumentTypeWrapper : XObjectWrapper, IXmlDocumentType, IXmlNode
	{
		// Token: 0x06000CF8 RID: 3320 RVA: 0x000338C9 File Offset: 0x00031AC9
		[NullableContext(1)]
		public XDocumentTypeWrapper(XDocumentType documentType)
			: base(documentType)
		{
			this._documentType = documentType;
		}

		// Token: 0x17000245 RID: 581
		// (get) Token: 0x06000CF9 RID: 3321 RVA: 0x000338D9 File Offset: 0x00031AD9
		[Nullable(1)]
		public string Name
		{
			[NullableContext(1)]
			get
			{
				return this._documentType.Name;
			}
		}

		// Token: 0x17000246 RID: 582
		// (get) Token: 0x06000CFA RID: 3322 RVA: 0x000338E6 File Offset: 0x00031AE6
		public string System
		{
			get
			{
				return this._documentType.SystemId;
			}
		}

		// Token: 0x17000247 RID: 583
		// (get) Token: 0x06000CFB RID: 3323 RVA: 0x000338F3 File Offset: 0x00031AF3
		public string Public
		{
			get
			{
				return this._documentType.PublicId;
			}
		}

		// Token: 0x17000248 RID: 584
		// (get) Token: 0x06000CFC RID: 3324 RVA: 0x00033900 File Offset: 0x00031B00
		public string InternalSubset
		{
			get
			{
				return this._documentType.InternalSubset;
			}
		}

		// Token: 0x17000249 RID: 585
		// (get) Token: 0x06000CFD RID: 3325 RVA: 0x0003390D File Offset: 0x00031B0D
		public override string LocalName
		{
			get
			{
				return "DOCTYPE";
			}
		}

		// Token: 0x0400040E RID: 1038
		[Nullable(1)]
		private readonly XDocumentType _documentType;
	}
}
