using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000FD RID: 253
	[NullableContext(2)]
	[Nullable(0)]
	internal class XTextWrapper : XObjectWrapper
	{
		// Token: 0x1700024E RID: 590
		// (get) Token: 0x06000D10 RID: 3344 RVA: 0x00033AB1 File Offset: 0x00031CB1
		[Nullable(1)]
		private XText Text
		{
			[NullableContext(1)]
			get
			{
				return (XText)base.WrappedNode;
			}
		}

		// Token: 0x06000D11 RID: 3345 RVA: 0x00033ABE File Offset: 0x00031CBE
		[NullableContext(1)]
		public XTextWrapper(XText text)
			: base(text)
		{
		}

		// Token: 0x1700024F RID: 591
		// (get) Token: 0x06000D12 RID: 3346 RVA: 0x00033AC7 File Offset: 0x00031CC7
		// (set) Token: 0x06000D13 RID: 3347 RVA: 0x00033AD4 File Offset: 0x00031CD4
		public override string Value
		{
			get
			{
				return this.Text.Value;
			}
			set
			{
				this.Text.Value = value ?? string.Empty;
			}
		}

		// Token: 0x17000250 RID: 592
		// (get) Token: 0x06000D14 RID: 3348 RVA: 0x00033AEB File Offset: 0x00031CEB
		public override IXmlNode ParentNode
		{
			get
			{
				if (this.Text.Parent == null)
				{
					return null;
				}
				return XContainerWrapper.WrapNode(this.Text.Parent);
			}
		}
	}
}
