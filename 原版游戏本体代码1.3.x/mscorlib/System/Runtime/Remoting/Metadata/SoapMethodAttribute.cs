using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D8 RID: 2008
	[AttributeUsage(AttributeTargets.Method)]
	[ComVisible(true)]
	public sealed class SoapMethodAttribute : SoapAttribute
	{
		// Token: 0x17000E53 RID: 3667
		// (get) Token: 0x060056FC RID: 22268 RVA: 0x00134BCB File Offset: 0x00132DCB
		internal bool SoapActionExplicitySet
		{
			get
			{
				return this._bSoapActionExplicitySet;
			}
		}

		// Token: 0x17000E54 RID: 3668
		// (get) Token: 0x060056FD RID: 22269 RVA: 0x00134BD3 File Offset: 0x00132DD3
		// (set) Token: 0x060056FE RID: 22270 RVA: 0x00134C09 File Offset: 0x00132E09
		public string SoapAction
		{
			[SecuritySafeCritical]
			get
			{
				if (this._SoapAction == null)
				{
					this._SoapAction = this.XmlTypeNamespaceOfDeclaringType + "#" + ((MemberInfo)this.ReflectInfo).Name;
				}
				return this._SoapAction;
			}
			set
			{
				this._SoapAction = value;
				this._bSoapActionExplicitySet = true;
			}
		}

		// Token: 0x17000E55 RID: 3669
		// (get) Token: 0x060056FF RID: 22271 RVA: 0x00134C19 File Offset: 0x00132E19
		// (set) Token: 0x06005700 RID: 22272 RVA: 0x00134C1C File Offset: 0x00132E1C
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

		// Token: 0x17000E56 RID: 3670
		// (get) Token: 0x06005701 RID: 22273 RVA: 0x00134C2D File Offset: 0x00132E2D
		// (set) Token: 0x06005702 RID: 22274 RVA: 0x00134C49 File Offset: 0x00132E49
		public override string XmlNamespace
		{
			[SecuritySafeCritical]
			get
			{
				if (this.ProtXmlNamespace == null)
				{
					this.ProtXmlNamespace = this.XmlTypeNamespaceOfDeclaringType;
				}
				return this.ProtXmlNamespace;
			}
			set
			{
				this.ProtXmlNamespace = value;
			}
		}

		// Token: 0x17000E57 RID: 3671
		// (get) Token: 0x06005703 RID: 22275 RVA: 0x00134C52 File Offset: 0x00132E52
		// (set) Token: 0x06005704 RID: 22276 RVA: 0x00134C8A File Offset: 0x00132E8A
		public string ResponseXmlElementName
		{
			get
			{
				if (this._responseXmlElementName == null && this.ReflectInfo != null)
				{
					this._responseXmlElementName = ((MemberInfo)this.ReflectInfo).Name + "Response";
				}
				return this._responseXmlElementName;
			}
			set
			{
				this._responseXmlElementName = value;
			}
		}

		// Token: 0x17000E58 RID: 3672
		// (get) Token: 0x06005705 RID: 22277 RVA: 0x00134C93 File Offset: 0x00132E93
		// (set) Token: 0x06005706 RID: 22278 RVA: 0x00134CAF File Offset: 0x00132EAF
		public string ResponseXmlNamespace
		{
			get
			{
				if (this._responseXmlNamespace == null)
				{
					this._responseXmlNamespace = this.XmlNamespace;
				}
				return this._responseXmlNamespace;
			}
			set
			{
				this._responseXmlNamespace = value;
			}
		}

		// Token: 0x17000E59 RID: 3673
		// (get) Token: 0x06005707 RID: 22279 RVA: 0x00134CB8 File Offset: 0x00132EB8
		// (set) Token: 0x06005708 RID: 22280 RVA: 0x00134CD3 File Offset: 0x00132ED3
		public string ReturnXmlElementName
		{
			get
			{
				if (this._returnXmlElementName == null)
				{
					this._returnXmlElementName = "return";
				}
				return this._returnXmlElementName;
			}
			set
			{
				this._returnXmlElementName = value;
			}
		}

		// Token: 0x17000E5A RID: 3674
		// (get) Token: 0x06005709 RID: 22281 RVA: 0x00134CDC File Offset: 0x00132EDC
		private string XmlTypeNamespaceOfDeclaringType
		{
			[SecurityCritical]
			get
			{
				if (this.ReflectInfo != null)
				{
					Type declaringType = ((MemberInfo)this.ReflectInfo).DeclaringType;
					return XmlNamespaceEncoder.GetXmlNamespaceForType((RuntimeType)declaringType, null);
				}
				return null;
			}
		}

		// Token: 0x040027DC RID: 10204
		private string _SoapAction;

		// Token: 0x040027DD RID: 10205
		private string _responseXmlElementName;

		// Token: 0x040027DE RID: 10206
		private string _responseXmlNamespace;

		// Token: 0x040027DF RID: 10207
		private string _returnXmlElementName;

		// Token: 0x040027E0 RID: 10208
		private bool _bSoapActionExplicitySet;
	}
}
