using System;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000F0 RID: 240
	[NullableContext(1)]
	[Nullable(0)]
	internal class XmlDocumentWrapper : XmlNodeWrapper, IXmlDocument, IXmlNode
	{
		// Token: 0x06000CA1 RID: 3233 RVA: 0x00033386 File Offset: 0x00031586
		public XmlDocumentWrapper(XmlDocument document)
			: base(document)
		{
			this._document = document;
		}

		// Token: 0x06000CA2 RID: 3234 RVA: 0x00033396 File Offset: 0x00031596
		public IXmlNode CreateComment([Nullable(2)] string data)
		{
			return new XmlNodeWrapper(this._document.CreateComment(data));
		}

		// Token: 0x06000CA3 RID: 3235 RVA: 0x000333A9 File Offset: 0x000315A9
		public IXmlNode CreateTextNode([Nullable(2)] string text)
		{
			return new XmlNodeWrapper(this._document.CreateTextNode(text));
		}

		// Token: 0x06000CA4 RID: 3236 RVA: 0x000333BC File Offset: 0x000315BC
		public IXmlNode CreateCDataSection([Nullable(2)] string data)
		{
			return new XmlNodeWrapper(this._document.CreateCDataSection(data));
		}

		// Token: 0x06000CA5 RID: 3237 RVA: 0x000333CF File Offset: 0x000315CF
		public IXmlNode CreateWhitespace([Nullable(2)] string text)
		{
			return new XmlNodeWrapper(this._document.CreateWhitespace(text));
		}

		// Token: 0x06000CA6 RID: 3238 RVA: 0x000333E2 File Offset: 0x000315E2
		public IXmlNode CreateSignificantWhitespace([Nullable(2)] string text)
		{
			return new XmlNodeWrapper(this._document.CreateSignificantWhitespace(text));
		}

		// Token: 0x06000CA7 RID: 3239 RVA: 0x000333F5 File Offset: 0x000315F5
		public IXmlNode CreateXmlDeclaration(string version, [Nullable(2)] string encoding, [Nullable(2)] string standalone)
		{
			return new XmlDeclarationWrapper(this._document.CreateXmlDeclaration(version, encoding, standalone));
		}

		// Token: 0x06000CA8 RID: 3240 RVA: 0x0003340A File Offset: 0x0003160A
		[NullableContext(2)]
		[return: Nullable(1)]
		public IXmlNode CreateXmlDocumentType([Nullable(1)] string name, string publicId, string systemId, string internalSubset)
		{
			return new XmlDocumentTypeWrapper(this._document.CreateDocumentType(name, publicId, systemId, null));
		}

		// Token: 0x06000CA9 RID: 3241 RVA: 0x00033420 File Offset: 0x00031620
		public IXmlNode CreateProcessingInstruction(string target, string data)
		{
			return new XmlNodeWrapper(this._document.CreateProcessingInstruction(target, data));
		}

		// Token: 0x06000CAA RID: 3242 RVA: 0x00033434 File Offset: 0x00031634
		public IXmlElement CreateElement(string elementName)
		{
			return new XmlElementWrapper(this._document.CreateElement(elementName));
		}

		// Token: 0x06000CAB RID: 3243 RVA: 0x00033447 File Offset: 0x00031647
		public IXmlElement CreateElement(string qualifiedName, string namespaceUri)
		{
			return new XmlElementWrapper(this._document.CreateElement(qualifiedName, namespaceUri));
		}

		// Token: 0x06000CAC RID: 3244 RVA: 0x0003345B File Offset: 0x0003165B
		public IXmlNode CreateAttribute(string name, [Nullable(2)] string value)
		{
			return new XmlNodeWrapper(this._document.CreateAttribute(name))
			{
				Value = value
			};
		}

		// Token: 0x06000CAD RID: 3245 RVA: 0x00033475 File Offset: 0x00031675
		public IXmlNode CreateAttribute(string qualifiedName, [Nullable(2)] string namespaceUri, [Nullable(2)] string value)
		{
			return new XmlNodeWrapper(this._document.CreateAttribute(qualifiedName, namespaceUri))
			{
				Value = value
			};
		}

		// Token: 0x1700021B RID: 539
		// (get) Token: 0x06000CAE RID: 3246 RVA: 0x00033490 File Offset: 0x00031690
		[Nullable(2)]
		public IXmlElement DocumentElement
		{
			[NullableContext(2)]
			get
			{
				if (this._document.DocumentElement == null)
				{
					return null;
				}
				return new XmlElementWrapper(this._document.DocumentElement);
			}
		}

		// Token: 0x04000406 RID: 1030
		private readonly XmlDocument _document;
	}
}
