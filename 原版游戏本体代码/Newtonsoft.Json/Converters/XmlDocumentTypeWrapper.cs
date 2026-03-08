using System;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000F3 RID: 243
	[NullableContext(2)]
	[Nullable(0)]
	internal class XmlDocumentTypeWrapper : XmlNodeWrapper, IXmlDocumentType, IXmlNode
	{
		// Token: 0x06000CB9 RID: 3257 RVA: 0x0003355D File Offset: 0x0003175D
		[NullableContext(1)]
		public XmlDocumentTypeWrapper(XmlDocumentType documentType)
			: base(documentType)
		{
			this._documentType = documentType;
		}

		// Token: 0x17000220 RID: 544
		// (get) Token: 0x06000CBA RID: 3258 RVA: 0x0003356D File Offset: 0x0003176D
		[Nullable(1)]
		public string Name
		{
			[NullableContext(1)]
			get
			{
				return this._documentType.Name;
			}
		}

		// Token: 0x17000221 RID: 545
		// (get) Token: 0x06000CBB RID: 3259 RVA: 0x0003357A File Offset: 0x0003177A
		public string System
		{
			get
			{
				return this._documentType.SystemId;
			}
		}

		// Token: 0x17000222 RID: 546
		// (get) Token: 0x06000CBC RID: 3260 RVA: 0x00033587 File Offset: 0x00031787
		public string Public
		{
			get
			{
				return this._documentType.PublicId;
			}
		}

		// Token: 0x17000223 RID: 547
		// (get) Token: 0x06000CBD RID: 3261 RVA: 0x00033594 File Offset: 0x00031794
		public string InternalSubset
		{
			get
			{
				return this._documentType.InternalSubset;
			}
		}

		// Token: 0x17000224 RID: 548
		// (get) Token: 0x06000CBE RID: 3262 RVA: 0x000335A1 File Offset: 0x000317A1
		public override string LocalName
		{
			get
			{
				return "DOCTYPE";
			}
		}

		// Token: 0x04000409 RID: 1033
		[Nullable(1)]
		private readonly XmlDocumentType _documentType;
	}
}
