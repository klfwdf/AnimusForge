using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x02000102 RID: 258
	[NullableContext(2)]
	[Nullable(0)]
	internal class XAttributeWrapper : XObjectWrapper
	{
		// Token: 0x17000263 RID: 611
		// (get) Token: 0x06000D31 RID: 3377 RVA: 0x00033D98 File Offset: 0x00031F98
		[Nullable(1)]
		private XAttribute Attribute
		{
			[NullableContext(1)]
			get
			{
				return (XAttribute)base.WrappedNode;
			}
		}

		// Token: 0x06000D32 RID: 3378 RVA: 0x00033DA5 File Offset: 0x00031FA5
		[NullableContext(1)]
		public XAttributeWrapper(XAttribute attribute)
			: base(attribute)
		{
		}

		// Token: 0x17000264 RID: 612
		// (get) Token: 0x06000D33 RID: 3379 RVA: 0x00033DAE File Offset: 0x00031FAE
		// (set) Token: 0x06000D34 RID: 3380 RVA: 0x00033DBB File Offset: 0x00031FBB
		public override string Value
		{
			get
			{
				return this.Attribute.Value;
			}
			set
			{
				this.Attribute.Value = value ?? string.Empty;
			}
		}

		// Token: 0x17000265 RID: 613
		// (get) Token: 0x06000D35 RID: 3381 RVA: 0x00033DD2 File Offset: 0x00031FD2
		public override string LocalName
		{
			get
			{
				return this.Attribute.Name.LocalName;
			}
		}

		// Token: 0x17000266 RID: 614
		// (get) Token: 0x06000D36 RID: 3382 RVA: 0x00033DE4 File Offset: 0x00031FE4
		public override string NamespaceUri
		{
			get
			{
				return this.Attribute.Name.NamespaceName;
			}
		}

		// Token: 0x17000267 RID: 615
		// (get) Token: 0x06000D37 RID: 3383 RVA: 0x00033DF6 File Offset: 0x00031FF6
		public override IXmlNode ParentNode
		{
			get
			{
				if (this.Attribute.Parent == null)
				{
					return null;
				}
				return XContainerWrapper.WrapNode(this.Attribute.Parent);
			}
		}
	}
}
