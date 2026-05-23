using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x02000103 RID: 259
	[NullableContext(1)]
	[Nullable(0)]
	internal class XElementWrapper : XContainerWrapper, IXmlElement, IXmlNode
	{
		// Token: 0x17000268 RID: 616
		// (get) Token: 0x06000D38 RID: 3384 RVA: 0x00033E17 File Offset: 0x00032017
		private XElement Element
		{
			get
			{
				return (XElement)base.WrappedNode;
			}
		}

		// Token: 0x06000D39 RID: 3385 RVA: 0x00033E24 File Offset: 0x00032024
		public XElementWrapper(XElement element)
			: base(element)
		{
		}

		// Token: 0x06000D3A RID: 3386 RVA: 0x00033E30 File Offset: 0x00032030
		public void SetAttributeNode(IXmlNode attribute)
		{
			XObjectWrapper xobjectWrapper = (XObjectWrapper)attribute;
			this.Element.Add(xobjectWrapper.WrappedNode);
			this._attributes = null;
		}

		// Token: 0x17000269 RID: 617
		// (get) Token: 0x06000D3B RID: 3387 RVA: 0x00033E5C File Offset: 0x0003205C
		public override List<IXmlNode> Attributes
		{
			get
			{
				if (this._attributes == null)
				{
					if (!this.Element.HasAttributes && !this.HasImplicitNamespaceAttribute(this.NamespaceUri))
					{
						this._attributes = XmlNodeConverter.EmptyChildNodes;
					}
					else
					{
						this._attributes = new List<IXmlNode>();
						foreach (XAttribute attribute in this.Element.Attributes())
						{
							this._attributes.Add(new XAttributeWrapper(attribute));
						}
						string namespaceUri = this.NamespaceUri;
						if (this.HasImplicitNamespaceAttribute(namespaceUri))
						{
							this._attributes.Insert(0, new XAttributeWrapper(new XAttribute("xmlns", namespaceUri)));
						}
					}
				}
				return this._attributes;
			}
		}

		// Token: 0x06000D3C RID: 3388 RVA: 0x00033F30 File Offset: 0x00032130
		private bool HasImplicitNamespaceAttribute(string namespaceUri)
		{
			if (!StringUtils.IsNullOrEmpty(namespaceUri))
			{
				IXmlNode parentNode = this.ParentNode;
				if (namespaceUri != ((parentNode != null) ? parentNode.NamespaceUri : null) && StringUtils.IsNullOrEmpty(this.GetPrefixOfNamespace(namespaceUri)))
				{
					bool flag = false;
					if (this.Element.HasAttributes)
					{
						foreach (XAttribute xattribute in this.Element.Attributes())
						{
							if (xattribute.Name.LocalName == "xmlns" && StringUtils.IsNullOrEmpty(xattribute.Name.NamespaceName) && xattribute.Value == namespaceUri)
							{
								flag = true;
							}
						}
					}
					if (!flag)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000D3D RID: 3389 RVA: 0x00034000 File Offset: 0x00032200
		public override IXmlNode AppendChild(IXmlNode newChild)
		{
			IXmlNode result = base.AppendChild(newChild);
			this._attributes = null;
			return result;
		}

		// Token: 0x1700026A RID: 618
		// (get) Token: 0x06000D3E RID: 3390 RVA: 0x00034010 File Offset: 0x00032210
		// (set) Token: 0x06000D3F RID: 3391 RVA: 0x0003401D File Offset: 0x0003221D
		[Nullable(2)]
		public override string Value
		{
			[NullableContext(2)]
			get
			{
				return this.Element.Value;
			}
			[NullableContext(2)]
			set
			{
				this.Element.Value = value ?? string.Empty;
			}
		}

		// Token: 0x1700026B RID: 619
		// (get) Token: 0x06000D40 RID: 3392 RVA: 0x00034034 File Offset: 0x00032234
		[Nullable(2)]
		public override string LocalName
		{
			[NullableContext(2)]
			get
			{
				return this.Element.Name.LocalName;
			}
		}

		// Token: 0x1700026C RID: 620
		// (get) Token: 0x06000D41 RID: 3393 RVA: 0x00034046 File Offset: 0x00032246
		[Nullable(2)]
		public override string NamespaceUri
		{
			[NullableContext(2)]
			get
			{
				return this.Element.Name.NamespaceName;
			}
		}

		// Token: 0x06000D42 RID: 3394 RVA: 0x00034058 File Offset: 0x00032258
		[return: Nullable(2)]
		public string GetPrefixOfNamespace(string namespaceUri)
		{
			return this.Element.GetPrefixOfNamespace(namespaceUri);
		}

		// Token: 0x1700026D RID: 621
		// (get) Token: 0x06000D43 RID: 3395 RVA: 0x0003406B File Offset: 0x0003226B
		public bool IsEmpty
		{
			get
			{
				return this.Element.IsEmpty;
			}
		}

		// Token: 0x04000411 RID: 1041
		[Nullable(new byte[] { 2, 1 })]
		private List<IXmlNode> _attributes;
	}
}
