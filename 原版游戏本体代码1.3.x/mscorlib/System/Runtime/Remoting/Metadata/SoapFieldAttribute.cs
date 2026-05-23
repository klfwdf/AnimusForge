using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D9 RID: 2009
	[AttributeUsage(AttributeTargets.Field)]
	[ComVisible(true)]
	public sealed class SoapFieldAttribute : SoapAttribute
	{
		// Token: 0x0600570B RID: 22283 RVA: 0x00134D18 File Offset: 0x00132F18
		public bool IsInteropXmlElement()
		{
			return (this._explicitlySet & SoapFieldAttribute.ExplicitlySet.XmlElementName) > SoapFieldAttribute.ExplicitlySet.None;
		}

		// Token: 0x17000E5B RID: 3675
		// (get) Token: 0x0600570C RID: 22284 RVA: 0x00134D25 File Offset: 0x00132F25
		// (set) Token: 0x0600570D RID: 22285 RVA: 0x00134D53 File Offset: 0x00132F53
		public string XmlElementName
		{
			get
			{
				if (this._xmlElementName == null && this.ReflectInfo != null)
				{
					this._xmlElementName = ((FieldInfo)this.ReflectInfo).Name;
				}
				return this._xmlElementName;
			}
			set
			{
				this._xmlElementName = value;
				this._explicitlySet |= SoapFieldAttribute.ExplicitlySet.XmlElementName;
			}
		}

		// Token: 0x17000E5C RID: 3676
		// (get) Token: 0x0600570E RID: 22286 RVA: 0x00134D6A File Offset: 0x00132F6A
		// (set) Token: 0x0600570F RID: 22287 RVA: 0x00134D72 File Offset: 0x00132F72
		public int Order
		{
			get
			{
				return this._order;
			}
			set
			{
				this._order = value;
			}
		}

		// Token: 0x040027E1 RID: 10209
		private SoapFieldAttribute.ExplicitlySet _explicitlySet;

		// Token: 0x040027E2 RID: 10210
		private string _xmlElementName;

		// Token: 0x040027E3 RID: 10211
		private int _order;

		// Token: 0x02000C71 RID: 3185
		[Flags]
		[Serializable]
		private enum ExplicitlySet
		{
			// Token: 0x040037F6 RID: 14326
			None = 0,
			// Token: 0x040037F7 RID: 14327
			XmlElementName = 1
		}
	}
}
