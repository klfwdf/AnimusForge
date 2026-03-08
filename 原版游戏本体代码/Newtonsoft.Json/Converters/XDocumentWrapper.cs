using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000FC RID: 252
	[NullableContext(1)]
	[Nullable(0)]
	internal class XDocumentWrapper : XContainerWrapper, IXmlDocument, IXmlNode
	{
		// Token: 0x1700024A RID: 586
		// (get) Token: 0x06000CFE RID: 3326 RVA: 0x00033914 File Offset: 0x00031B14
		private XDocument Document
		{
			get
			{
				return (XDocument)base.WrappedNode;
			}
		}

		// Token: 0x06000CFF RID: 3327 RVA: 0x00033921 File Offset: 0x00031B21
		public XDocumentWrapper(XDocument document)
			: base(document)
		{
		}

		// Token: 0x1700024B RID: 587
		// (get) Token: 0x06000D00 RID: 3328 RVA: 0x0003392C File Offset: 0x00031B2C
		public override List<IXmlNode> ChildNodes
		{
			get
			{
				List<IXmlNode> childNodes = base.ChildNodes;
				if (this.Document.Declaration != null && (childNodes.Count == 0 || childNodes[0].NodeType != XmlNodeType.XmlDeclaration))
				{
					childNodes.Insert(0, new XDeclarationWrapper(this.Document.Declaration));
				}
				return childNodes;
			}
		}

		// Token: 0x1700024C RID: 588
		// (get) Token: 0x06000D01 RID: 3329 RVA: 0x0003397D File Offset: 0x00031B7D
		protected override bool HasChildNodes
		{
			get
			{
				return base.HasChildNodes || this.Document.Declaration != null;
			}
		}

		// Token: 0x06000D02 RID: 3330 RVA: 0x00033997 File Offset: 0x00031B97
		public IXmlNode CreateComment([Nullable(2)] string text)
		{
			return new XObjectWrapper(new XComment(text));
		}

		// Token: 0x06000D03 RID: 3331 RVA: 0x000339A4 File Offset: 0x00031BA4
		public IXmlNode CreateTextNode([Nullable(2)] string text)
		{
			return new XObjectWrapper(new XText(text));
		}

		// Token: 0x06000D04 RID: 3332 RVA: 0x000339B1 File Offset: 0x00031BB1
		public IXmlNode CreateCDataSection([Nullable(2)] string data)
		{
			return new XObjectWrapper(new XCData(data));
		}

		// Token: 0x06000D05 RID: 3333 RVA: 0x000339BE File Offset: 0x00031BBE
		public IXmlNode CreateWhitespace([Nullable(2)] string text)
		{
			return new XObjectWrapper(new XText(text));
		}

		// Token: 0x06000D06 RID: 3334 RVA: 0x000339CB File Offset: 0x00031BCB
		public IXmlNode CreateSignificantWhitespace([Nullable(2)] string text)
		{
			return new XObjectWrapper(new XText(text));
		}

		// Token: 0x06000D07 RID: 3335 RVA: 0x000339D8 File Offset: 0x00031BD8
		public IXmlNode CreateXmlDeclaration(string version, [Nullable(2)] string encoding, [Nullable(2)] string standalone)
		{
			return new XDeclarationWrapper(new XDeclaration(version, encoding, standalone));
		}

		// Token: 0x06000D08 RID: 3336 RVA: 0x000339E7 File Offset: 0x00031BE7
		[NullableContext(2)]
		[return: Nullable(1)]
		public IXmlNode CreateXmlDocumentType([Nullable(1)] string name, string publicId, string systemId, string internalSubset)
		{
			return new XDocumentTypeWrapper(new XDocumentType(name, publicId, systemId, internalSubset));
		}

		// Token: 0x06000D09 RID: 3337 RVA: 0x000339F8 File Offset: 0x00031BF8
		public IXmlNode CreateProcessingInstruction(string target, string data)
		{
			return new XProcessingInstructionWrapper(new XProcessingInstruction(target, data));
		}

		// Token: 0x06000D0A RID: 3338 RVA: 0x00033A06 File Offset: 0x00031C06
		public IXmlElement CreateElement(string elementName)
		{
			return new XElementWrapper(new XElement(elementName));
		}

		// Token: 0x06000D0B RID: 3339 RVA: 0x00033A18 File Offset: 0x00031C18
		public IXmlElement CreateElement(string qualifiedName, string namespaceUri)
		{
			return new XElementWrapper(new XElement(XName.Get(MiscellaneousUtils.GetLocalName(qualifiedName), namespaceUri)));
		}

		// Token: 0x06000D0C RID: 3340 RVA: 0x00033A30 File Offset: 0x00031C30
		public IXmlNode CreateAttribute(string name, string value)
		{
			return new XAttributeWrapper(new XAttribute(name, value));
		}

		// Token: 0x06000D0D RID: 3341 RVA: 0x00033A43 File Offset: 0x00031C43
		public IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value)
		{
			return new XAttributeWrapper(new XAttribute(XName.Get(MiscellaneousUtils.GetLocalName(qualifiedName), namespaceUri), value));
		}

		// Token: 0x1700024D RID: 589
		// (get) Token: 0x06000D0E RID: 3342 RVA: 0x00033A5C File Offset: 0x00031C5C
		[Nullable(2)]
		public IXmlElement DocumentElement
		{
			[NullableContext(2)]
			get
			{
				if (this.Document.Root == null)
				{
					return null;
				}
				return new XElementWrapper(this.Document.Root);
			}
		}

		// Token: 0x06000D0F RID: 3343 RVA: 0x00033A80 File Offset: 0x00031C80
		public override IXmlNode AppendChild(IXmlNode newChild)
		{
			XDeclarationWrapper xdeclarationWrapper = newChild as XDeclarationWrapper;
			if (xdeclarationWrapper != null)
			{
				this.Document.Declaration = xdeclarationWrapper.Declaration;
				return xdeclarationWrapper;
			}
			return base.AppendChild(newChild);
		}
	}
}
