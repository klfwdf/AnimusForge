using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D7 RID: 2007
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
	[ComVisible(true)]
	public sealed class SoapTypeAttribute : SoapAttribute
	{
		// Token: 0x060056EA RID: 22250 RVA: 0x00134A1E File Offset: 0x00132C1E
		internal bool IsInteropXmlElement()
		{
			return (this._explicitlySet & (SoapTypeAttribute.ExplicitlySet.XmlElementName | SoapTypeAttribute.ExplicitlySet.XmlNamespace)) > SoapTypeAttribute.ExplicitlySet.None;
		}

		// Token: 0x060056EB RID: 22251 RVA: 0x00134A2B File Offset: 0x00132C2B
		internal bool IsInteropXmlType()
		{
			return (this._explicitlySet & (SoapTypeAttribute.ExplicitlySet.XmlTypeName | SoapTypeAttribute.ExplicitlySet.XmlTypeNamespace)) > SoapTypeAttribute.ExplicitlySet.None;
		}

		// Token: 0x17000E4C RID: 3660
		// (get) Token: 0x060056EC RID: 22252 RVA: 0x00134A39 File Offset: 0x00132C39
		// (set) Token: 0x060056ED RID: 22253 RVA: 0x00134A41 File Offset: 0x00132C41
		public SoapOption SoapOptions
		{
			get
			{
				return this._SoapOptions;
			}
			set
			{
				this._SoapOptions = value;
			}
		}

		// Token: 0x17000E4D RID: 3661
		// (get) Token: 0x060056EE RID: 22254 RVA: 0x00134A4A File Offset: 0x00132C4A
		// (set) Token: 0x060056EF RID: 22255 RVA: 0x00134A78 File Offset: 0x00132C78
		public string XmlElementName
		{
			get
			{
				if (this._XmlElementName == null && this.ReflectInfo != null)
				{
					this._XmlElementName = SoapTypeAttribute.GetTypeName((Type)this.ReflectInfo);
				}
				return this._XmlElementName;
			}
			set
			{
				this._XmlElementName = value;
				this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlElementName;
			}
		}

		// Token: 0x17000E4E RID: 3662
		// (get) Token: 0x060056F0 RID: 22256 RVA: 0x00134A8F File Offset: 0x00132C8F
		// (set) Token: 0x060056F1 RID: 22257 RVA: 0x00134AB3 File Offset: 0x00132CB3
		public override string XmlNamespace
		{
			get
			{
				if (this.ProtXmlNamespace == null && this.ReflectInfo != null)
				{
					this.ProtXmlNamespace = this.XmlTypeNamespace;
				}
				return this.ProtXmlNamespace;
			}
			set
			{
				this.ProtXmlNamespace = value;
				this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlNamespace;
			}
		}

		// Token: 0x17000E4F RID: 3663
		// (get) Token: 0x060056F2 RID: 22258 RVA: 0x00134ACA File Offset: 0x00132CCA
		// (set) Token: 0x060056F3 RID: 22259 RVA: 0x00134AF8 File Offset: 0x00132CF8
		public string XmlTypeName
		{
			get
			{
				if (this._XmlTypeName == null && this.ReflectInfo != null)
				{
					this._XmlTypeName = SoapTypeAttribute.GetTypeName((Type)this.ReflectInfo);
				}
				return this._XmlTypeName;
			}
			set
			{
				this._XmlTypeName = value;
				this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlTypeName;
			}
		}

		// Token: 0x17000E50 RID: 3664
		// (get) Token: 0x060056F4 RID: 22260 RVA: 0x00134B0F File Offset: 0x00132D0F
		// (set) Token: 0x060056F5 RID: 22261 RVA: 0x00134B3E File Offset: 0x00132D3E
		public string XmlTypeNamespace
		{
			[SecuritySafeCritical]
			get
			{
				if (this._XmlTypeNamespace == null && this.ReflectInfo != null)
				{
					this._XmlTypeNamespace = XmlNamespaceEncoder.GetXmlNamespaceForTypeNamespace((RuntimeType)this.ReflectInfo, null);
				}
				return this._XmlTypeNamespace;
			}
			set
			{
				this._XmlTypeNamespace = value;
				this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlTypeNamespace;
			}
		}

		// Token: 0x17000E51 RID: 3665
		// (get) Token: 0x060056F6 RID: 22262 RVA: 0x00134B55 File Offset: 0x00132D55
		// (set) Token: 0x060056F7 RID: 22263 RVA: 0x00134B5D File Offset: 0x00132D5D
		public XmlFieldOrderOption XmlFieldOrder
		{
			get
			{
				return this._XmlFieldOrder;
			}
			set
			{
				this._XmlFieldOrder = value;
			}
		}

		// Token: 0x17000E52 RID: 3666
		// (get) Token: 0x060056F8 RID: 22264 RVA: 0x00134B66 File Offset: 0x00132D66
		// (set) Token: 0x060056F9 RID: 22265 RVA: 0x00134B69 File Offset: 0x00132D69
		public override bool UseAttribute
		{
			get
			{
				return false;
			}
			set
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Attribute_UseAttributeNotsettable"));
			}
		}

		// Token: 0x060056FA RID: 22266 RVA: 0x00134B7C File Offset: 0x00132D7C
		private static string GetTypeName(Type t)
		{
			if (!t.IsNested)
			{
				return t.Name;
			}
			string fullName = t.FullName;
			string @namespace = t.Namespace;
			if (@namespace == null || @namespace.Length == 0)
			{
				return fullName;
			}
			return fullName.Substring(@namespace.Length + 1);
		}

		// Token: 0x040027D6 RID: 10198
		private SoapTypeAttribute.ExplicitlySet _explicitlySet;

		// Token: 0x040027D7 RID: 10199
		private SoapOption _SoapOptions;

		// Token: 0x040027D8 RID: 10200
		private string _XmlElementName;

		// Token: 0x040027D9 RID: 10201
		private string _XmlTypeName;

		// Token: 0x040027DA RID: 10202
		private string _XmlTypeNamespace;

		// Token: 0x040027DB RID: 10203
		private XmlFieldOrderOption _XmlFieldOrder;

		// Token: 0x02000C70 RID: 3184
		[Flags]
		[Serializable]
		private enum ExplicitlySet
		{
			// Token: 0x040037F0 RID: 14320
			None = 0,
			// Token: 0x040037F1 RID: 14321
			XmlElementName = 1,
			// Token: 0x040037F2 RID: 14322
			XmlNamespace = 2,
			// Token: 0x040037F3 RID: 14323
			XmlTypeName = 4,
			// Token: 0x040037F4 RID: 14324
			XmlTypeNamespace = 8
		}
	}
}
