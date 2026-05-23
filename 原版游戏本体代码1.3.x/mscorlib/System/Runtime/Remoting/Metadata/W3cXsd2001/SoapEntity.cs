using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007FC RID: 2044
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapEntity : ISoapXsd
	{
		// Token: 0x17000E9F RID: 3743
		// (get) Token: 0x06005823 RID: 22563 RVA: 0x00136B3D File Offset: 0x00134D3D
		public static string XsdType
		{
			get
			{
				return "ENTITY";
			}
		}

		// Token: 0x06005824 RID: 22564 RVA: 0x00136B44 File Offset: 0x00134D44
		public string GetXsdType()
		{
			return SoapEntity.XsdType;
		}

		// Token: 0x06005825 RID: 22565 RVA: 0x00136B4B File Offset: 0x00134D4B
		public SoapEntity()
		{
		}

		// Token: 0x06005826 RID: 22566 RVA: 0x00136B53 File Offset: 0x00134D53
		public SoapEntity(string value)
		{
			this._value = value;
		}

		// Token: 0x17000EA0 RID: 3744
		// (get) Token: 0x06005827 RID: 22567 RVA: 0x00136B62 File Offset: 0x00134D62
		// (set) Token: 0x06005828 RID: 22568 RVA: 0x00136B6A File Offset: 0x00134D6A
		public string Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = value;
			}
		}

		// Token: 0x06005829 RID: 22569 RVA: 0x00136B73 File Offset: 0x00134D73
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x0600582A RID: 22570 RVA: 0x00136B80 File Offset: 0x00134D80
		public static SoapEntity Parse(string value)
		{
			return new SoapEntity(value);
		}

		// Token: 0x04002830 RID: 10288
		private string _value;
	}
}
