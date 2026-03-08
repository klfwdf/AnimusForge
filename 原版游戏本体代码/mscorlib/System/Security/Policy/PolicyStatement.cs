using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;
using System.Text;

namespace System.Security.Policy
{
	// Token: 0x02000369 RID: 873
	[ComVisible(true)]
	[Serializable]
	public sealed class PolicyStatement : ISecurityPolicyEncodable, ISecurityEncodable
	{
		// Token: 0x06002B19 RID: 11033 RVA: 0x000A0B00 File Offset: 0x0009ED00
		internal PolicyStatement()
		{
			this.m_permSet = null;
			this.m_attributes = PolicyStatementAttribute.Nothing;
		}

		// Token: 0x06002B1A RID: 11034 RVA: 0x000A0B16 File Offset: 0x0009ED16
		public PolicyStatement(PermissionSet permSet)
			: this(permSet, PolicyStatementAttribute.Nothing)
		{
		}

		// Token: 0x06002B1B RID: 11035 RVA: 0x000A0B20 File Offset: 0x0009ED20
		public PolicyStatement(PermissionSet permSet, PolicyStatementAttribute attributes)
		{
			if (permSet == null)
			{
				this.m_permSet = new PermissionSet(false);
			}
			else
			{
				this.m_permSet = permSet.Copy();
			}
			if (PolicyStatement.ValidProperties(attributes))
			{
				this.m_attributes = attributes;
			}
		}

		// Token: 0x06002B1C RID: 11036 RVA: 0x000A0B54 File Offset: 0x0009ED54
		private PolicyStatement(PermissionSet permSet, PolicyStatementAttribute attributes, bool copy)
		{
			if (permSet != null)
			{
				if (copy)
				{
					this.m_permSet = permSet.Copy();
				}
				else
				{
					this.m_permSet = permSet;
				}
			}
			else
			{
				this.m_permSet = new PermissionSet(false);
			}
			this.m_attributes = attributes;
		}

		// Token: 0x170005C2 RID: 1474
		// (get) Token: 0x06002B1D RID: 11037 RVA: 0x000A0B8C File Offset: 0x0009ED8C
		// (set) Token: 0x06002B1E RID: 11038 RVA: 0x000A0BD0 File Offset: 0x0009EDD0
		public PermissionSet PermissionSet
		{
			get
			{
				PermissionSet result;
				lock (this)
				{
					result = this.m_permSet.Copy();
				}
				return result;
			}
			set
			{
				lock (this)
				{
					if (value == null)
					{
						this.m_permSet = new PermissionSet(false);
					}
					else
					{
						this.m_permSet = value.Copy();
					}
				}
			}
		}

		// Token: 0x06002B1F RID: 11039 RVA: 0x000A0C24 File Offset: 0x0009EE24
		internal void SetPermissionSetNoCopy(PermissionSet permSet)
		{
			this.m_permSet = permSet;
		}

		// Token: 0x06002B20 RID: 11040 RVA: 0x000A0C30 File Offset: 0x0009EE30
		internal PermissionSet GetPermissionSetNoCopy()
		{
			PermissionSet permSet;
			lock (this)
			{
				permSet = this.m_permSet;
			}
			return permSet;
		}

		// Token: 0x170005C3 RID: 1475
		// (get) Token: 0x06002B21 RID: 11041 RVA: 0x000A0C70 File Offset: 0x0009EE70
		// (set) Token: 0x06002B22 RID: 11042 RVA: 0x000A0C78 File Offset: 0x0009EE78
		public PolicyStatementAttribute Attributes
		{
			get
			{
				return this.m_attributes;
			}
			set
			{
				if (PolicyStatement.ValidProperties(value))
				{
					this.m_attributes = value;
				}
			}
		}

		// Token: 0x06002B23 RID: 11043 RVA: 0x000A0C8C File Offset: 0x0009EE8C
		public PolicyStatement Copy()
		{
			PolicyStatement policyStatement = new PolicyStatement(this.m_permSet, this.Attributes, true);
			if (this.HasDependentEvidence)
			{
				policyStatement.m_dependentEvidence = new List<IDelayEvaluatedEvidence>(this.m_dependentEvidence);
			}
			return policyStatement;
		}

		// Token: 0x170005C4 RID: 1476
		// (get) Token: 0x06002B24 RID: 11044 RVA: 0x000A0CC8 File Offset: 0x0009EEC8
		public string AttributeString
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				bool flag = true;
				if (this.GetFlag(1))
				{
					stringBuilder.Append("Exclusive");
					flag = false;
				}
				if (this.GetFlag(2))
				{
					if (!flag)
					{
						stringBuilder.Append(" ");
					}
					stringBuilder.Append("LevelFinal");
				}
				return stringBuilder.ToString();
			}
		}

		// Token: 0x06002B25 RID: 11045 RVA: 0x000A0D1E File Offset: 0x0009EF1E
		private static bool ValidProperties(PolicyStatementAttribute attributes)
		{
			if ((attributes & ~(PolicyStatementAttribute.Exclusive | PolicyStatementAttribute.LevelFinal)) == PolicyStatementAttribute.Nothing)
			{
				return true;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"));
		}

		// Token: 0x06002B26 RID: 11046 RVA: 0x000A0D37 File Offset: 0x0009EF37
		private bool GetFlag(int flag)
		{
			return (flag & (int)this.m_attributes) != 0;
		}

		// Token: 0x170005C5 RID: 1477
		// (get) Token: 0x06002B27 RID: 11047 RVA: 0x000A0D44 File Offset: 0x0009EF44
		internal IEnumerable<IDelayEvaluatedEvidence> DependentEvidence
		{
			get
			{
				return this.m_dependentEvidence.AsReadOnly();
			}
		}

		// Token: 0x170005C6 RID: 1478
		// (get) Token: 0x06002B28 RID: 11048 RVA: 0x000A0D51 File Offset: 0x0009EF51
		internal bool HasDependentEvidence
		{
			get
			{
				return this.m_dependentEvidence != null && this.m_dependentEvidence.Count > 0;
			}
		}

		// Token: 0x06002B29 RID: 11049 RVA: 0x000A0D6B File Offset: 0x0009EF6B
		internal void AddDependentEvidence(IDelayEvaluatedEvidence dependentEvidence)
		{
			if (this.m_dependentEvidence == null)
			{
				this.m_dependentEvidence = new List<IDelayEvaluatedEvidence>();
			}
			this.m_dependentEvidence.Add(dependentEvidence);
		}

		// Token: 0x06002B2A RID: 11050 RVA: 0x000A0D8C File Offset: 0x0009EF8C
		internal void InplaceUnion(PolicyStatement childPolicy)
		{
			if ((this.Attributes & childPolicy.Attributes & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive)
			{
				throw new PolicyException(Environment.GetResourceString("Policy_MultipleExclusive"));
			}
			if (childPolicy.HasDependentEvidence)
			{
				bool flag = this.m_permSet.IsSubsetOf(childPolicy.GetPermissionSetNoCopy()) && !childPolicy.GetPermissionSetNoCopy().IsSubsetOf(this.m_permSet);
				if (this.HasDependentEvidence || flag)
				{
					if (this.m_dependentEvidence == null)
					{
						this.m_dependentEvidence = new List<IDelayEvaluatedEvidence>();
					}
					this.m_dependentEvidence.AddRange(childPolicy.DependentEvidence);
				}
			}
			if ((childPolicy.Attributes & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive)
			{
				this.m_permSet = childPolicy.GetPermissionSetNoCopy();
				this.Attributes = childPolicy.Attributes;
				return;
			}
			this.m_permSet.InplaceUnion(childPolicy.GetPermissionSetNoCopy());
			this.Attributes |= childPolicy.Attributes;
		}

		// Token: 0x06002B2B RID: 11051 RVA: 0x000A0E64 File Offset: 0x0009F064
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x06002B2C RID: 11052 RVA: 0x000A0E6D File Offset: 0x0009F06D
		public void FromXml(SecurityElement et)
		{
			this.FromXml(et, null);
		}

		// Token: 0x06002B2D RID: 11053 RVA: 0x000A0E77 File Offset: 0x0009F077
		public SecurityElement ToXml(PolicyLevel level)
		{
			return this.ToXml(level, false);
		}

		// Token: 0x06002B2E RID: 11054 RVA: 0x000A0E84 File Offset: 0x0009F084
		internal SecurityElement ToXml(PolicyLevel level, bool useInternal)
		{
			SecurityElement securityElement = new SecurityElement("PolicyStatement");
			securityElement.AddAttribute("version", "1");
			if (this.m_attributes != PolicyStatementAttribute.Nothing)
			{
				securityElement.AddAttribute("Attributes", XMLUtil.BitFieldEnumToString(typeof(PolicyStatementAttribute), this.m_attributes));
			}
			lock (this)
			{
				if (this.m_permSet != null)
				{
					if (this.m_permSet is NamedPermissionSet)
					{
						NamedPermissionSet namedPermissionSet = (NamedPermissionSet)this.m_permSet;
						if (level != null && level.GetNamedPermissionSet(namedPermissionSet.Name) != null)
						{
							securityElement.AddAttribute("PermissionSetName", namedPermissionSet.Name);
						}
						else if (useInternal)
						{
							securityElement.AddChild(namedPermissionSet.InternalToXml());
						}
						else
						{
							securityElement.AddChild(namedPermissionSet.ToXml());
						}
					}
					else if (useInternal)
					{
						securityElement.AddChild(this.m_permSet.InternalToXml());
					}
					else
					{
						securityElement.AddChild(this.m_permSet.ToXml());
					}
				}
			}
			return securityElement;
		}

		// Token: 0x06002B2F RID: 11055 RVA: 0x000A0F90 File Offset: 0x0009F190
		[SecuritySafeCritical]
		public void FromXml(SecurityElement et, PolicyLevel level)
		{
			this.FromXml(et, level, false);
		}

		// Token: 0x06002B30 RID: 11056 RVA: 0x000A0F9C File Offset: 0x0009F19C
		[SecurityCritical]
		internal void FromXml(SecurityElement et, PolicyLevel level, bool allowInternalOnly)
		{
			if (et == null)
			{
				throw new ArgumentNullException("et");
			}
			if (!et.Tag.Equals("PolicyStatement"))
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidXMLElement"), "PolicyStatement", base.GetType().FullName));
			}
			this.m_attributes = PolicyStatementAttribute.Nothing;
			string text = et.Attribute("Attributes");
			if (text != null)
			{
				this.m_attributes = (PolicyStatementAttribute)Enum.Parse(typeof(PolicyStatementAttribute), text);
			}
			lock (this)
			{
				this.m_permSet = null;
				if (level != null)
				{
					string text2 = et.Attribute("PermissionSetName");
					if (text2 != null)
					{
						this.m_permSet = level.GetNamedPermissionSetInternal(text2);
						if (this.m_permSet == null)
						{
							this.m_permSet = new PermissionSet(PermissionState.None);
						}
					}
				}
				if (this.m_permSet == null)
				{
					SecurityElement securityElement = et.SearchForChildByTag("PermissionSet");
					if (securityElement != null)
					{
						string text3 = securityElement.Attribute("class");
						if (text3 != null && (text3.Equals("NamedPermissionSet") || text3.Equals("System.Security.NamedPermissionSet")))
						{
							this.m_permSet = new NamedPermissionSet("DefaultName", PermissionState.None);
						}
						else
						{
							this.m_permSet = new PermissionSet(PermissionState.None);
						}
						try
						{
							this.m_permSet.FromXml(securityElement, allowInternalOnly, true);
							goto IL_14F;
						}
						catch
						{
							goto IL_14F;
						}
					}
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidXML"));
				}
				IL_14F:
				if (this.m_permSet == null)
				{
					this.m_permSet = new PermissionSet(PermissionState.None);
				}
			}
		}

		// Token: 0x06002B31 RID: 11057 RVA: 0x000A1134 File Offset: 0x0009F334
		[SecurityCritical]
		internal void FromXml(SecurityDocument doc, int position, PolicyLevel level, bool allowInternalOnly)
		{
			if (doc == null)
			{
				throw new ArgumentNullException("doc");
			}
			if (!doc.GetTagForElement(position).Equals("PolicyStatement"))
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidXMLElement"), "PolicyStatement", base.GetType().FullName));
			}
			this.m_attributes = PolicyStatementAttribute.Nothing;
			string attributeForElement = doc.GetAttributeForElement(position, "Attributes");
			if (attributeForElement != null)
			{
				this.m_attributes = (PolicyStatementAttribute)Enum.Parse(typeof(PolicyStatementAttribute), attributeForElement);
			}
			lock (this)
			{
				this.m_permSet = null;
				if (level != null)
				{
					string attributeForElement2 = doc.GetAttributeForElement(position, "PermissionSetName");
					if (attributeForElement2 != null)
					{
						this.m_permSet = level.GetNamedPermissionSetInternal(attributeForElement2);
						if (this.m_permSet == null)
						{
							this.m_permSet = new PermissionSet(PermissionState.None);
						}
					}
				}
				if (this.m_permSet == null)
				{
					ArrayList childrenPositionForElement = doc.GetChildrenPositionForElement(position);
					int num = -1;
					for (int i = 0; i < childrenPositionForElement.Count; i++)
					{
						if (doc.GetTagForElement((int)childrenPositionForElement[i]).Equals("PermissionSet"))
						{
							num = (int)childrenPositionForElement[i];
						}
					}
					if (num == -1)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidXML"));
					}
					string attributeForElement3 = doc.GetAttributeForElement(num, "class");
					if (attributeForElement3 != null && (attributeForElement3.Equals("NamedPermissionSet") || attributeForElement3.Equals("System.Security.NamedPermissionSet")))
					{
						this.m_permSet = new NamedPermissionSet("DefaultName", PermissionState.None);
					}
					else
					{
						this.m_permSet = new PermissionSet(PermissionState.None);
					}
					this.m_permSet.FromXml(doc, num, allowInternalOnly);
				}
				if (this.m_permSet == null)
				{
					this.m_permSet = new PermissionSet(PermissionState.None);
				}
			}
		}

		// Token: 0x06002B32 RID: 11058 RVA: 0x000A1314 File Offset: 0x0009F514
		[ComVisible(false)]
		public override bool Equals(object obj)
		{
			PolicyStatement policyStatement = obj as PolicyStatement;
			return policyStatement != null && this.m_attributes == policyStatement.m_attributes && object.Equals(this.m_permSet, policyStatement.m_permSet);
		}

		// Token: 0x06002B33 RID: 11059 RVA: 0x000A1354 File Offset: 0x0009F554
		[ComVisible(false)]
		public override int GetHashCode()
		{
			int num = (int)this.m_attributes;
			if (this.m_permSet != null)
			{
				num ^= this.m_permSet.GetHashCode();
			}
			return num;
		}

		// Token: 0x04001196 RID: 4502
		internal PermissionSet m_permSet;

		// Token: 0x04001197 RID: 4503
		[NonSerialized]
		private List<IDelayEvaluatedEvidence> m_dependentEvidence;

		// Token: 0x04001198 RID: 4504
		internal PolicyStatementAttribute m_attributes;
	}
}
