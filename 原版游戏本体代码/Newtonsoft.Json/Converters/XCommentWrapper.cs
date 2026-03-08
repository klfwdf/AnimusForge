using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000FE RID: 254
	[NullableContext(2)]
	[Nullable(0)]
	internal class XCommentWrapper : XObjectWrapper
	{
		// Token: 0x17000251 RID: 593
		// (get) Token: 0x06000D15 RID: 3349 RVA: 0x00033B0C File Offset: 0x00031D0C
		[Nullable(1)]
		private XComment Text
		{
			[NullableContext(1)]
			get
			{
				return (XComment)base.WrappedNode;
			}
		}

		// Token: 0x06000D16 RID: 3350 RVA: 0x00033B19 File Offset: 0x00031D19
		[NullableContext(1)]
		public XCommentWrapper(XComment text)
			: base(text)
		{
		}

		// Token: 0x17000252 RID: 594
		// (get) Token: 0x06000D17 RID: 3351 RVA: 0x00033B22 File Offset: 0x00031D22
		// (set) Token: 0x06000D18 RID: 3352 RVA: 0x00033B2F File Offset: 0x00031D2F
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

		// Token: 0x17000253 RID: 595
		// (get) Token: 0x06000D19 RID: 3353 RVA: 0x00033B46 File Offset: 0x00031D46
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
