using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x0200034B RID: 843
	[ComVisible(true)]
	[Serializable]
	public abstract class CodeGroup
	{
		// Token: 0x060029D5 RID: 10709 RVA: 0x0009A901 File Offset: 0x00098B01
		internal CodeGroup()
		{
		}

		// Token: 0x060029D6 RID: 10710 RVA: 0x0009A90C File Offset: 0x00098B0C
		internal CodeGroup(IMembershipCondition membershipCondition, PermissionSet permSet)
		{
			this.m_membershipCondition = membershipCondition;
			this.m_policy = new PolicyStatement();
			this.m_policy.SetPermissionSetNoCopy(permSet);
			this.m_children = ArrayList.Synchronized(new ArrayList());
			this.m_element = null;
			this.m_parentLevel = null;
		}

		// Token: 0x060029D7 RID: 10711 RVA: 0x0009A95C File Offset: 0x00098B5C
		protected CodeGroup(IMembershipCondition membershipCondition, PolicyStatement policy)
		{
			if (membershipCondition == null)
			{
				throw new ArgumentNullException("membershipCondition");
			}
			if (policy == null)
			{
				this.m_policy = null;
			}
			else
			{
				this.m_policy = policy.Copy();
			}
			this.m_membershipCondition = membershipCondition.Copy();
			this.m_children = ArrayList.Synchronized(new ArrayList());
			this.m_element = null;
			this.m_parentLevel = null;
		}

		// Token: 0x060029D8 RID: 10712 RVA: 0x0009A9C0 File Offset: 0x00098BC0
		[SecuritySafeCritical]
		public void AddChild(CodeGroup group)
		{
			if (group == null)
			{
				throw new ArgumentNullException("group");
			}
			if (this.m_children == null)
			{
				this.ParseChildren();
			}
			lock (this)
			{
				this.m_children.Add(group.Copy());
			}
		}

		// Token: 0x060029D9 RID: 10713 RVA: 0x0009AA24 File Offset: 0x00098C24
		[SecurityCritical]
		internal void AddChildInternal(CodeGroup group)
		{
			if (group == null)
			{
				throw new ArgumentNullException("group");
			}
			if (this.m_children == null)
			{
				this.ParseChildren();
			}
			lock (this)
			{
				this.m_children.Add(group);
			}
		}

		// Token: 0x060029DA RID: 10714 RVA: 0x0009AA84 File Offset: 0x00098C84
		[SecuritySafeCritical]
		public void RemoveChild(CodeGroup group)
		{
			if (group == null)
			{
				return;
			}
			if (this.m_children == null)
			{
				this.ParseChildren();
			}
			lock (this)
			{
				int num = this.m_children.IndexOf(group);
				if (num != -1)
				{
					this.m_children.RemoveAt(num);
				}
			}
		}

		// Token: 0x17000580 RID: 1408
		// (get) Token: 0x060029DB RID: 10715 RVA: 0x0009AAE8 File Offset: 0x00098CE8
		// (set) Token: 0x060029DC RID: 10716 RVA: 0x0009AB70 File Offset: 0x00098D70
		public IList Children
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_children == null)
				{
					this.ParseChildren();
				}
				IList result;
				lock (this)
				{
					IList list = new ArrayList(this.m_children.Count);
					foreach (object obj in this.m_children)
					{
						list.Add(((CodeGroup)obj).Copy());
					}
					result = list;
				}
				return result;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("Children");
				}
				ArrayList arrayList = ArrayList.Synchronized(new ArrayList(value.Count));
				foreach (object obj in value)
				{
					CodeGroup codeGroup = obj as CodeGroup;
					if (codeGroup == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_CodeGroupChildrenMustBeCodeGroups"));
					}
					arrayList.Add(codeGroup.Copy());
				}
				this.m_children = arrayList;
			}
		}

		// Token: 0x060029DD RID: 10717 RVA: 0x0009ABE0 File Offset: 0x00098DE0
		[SecurityCritical]
		internal IList GetChildrenInternal()
		{
			if (this.m_children == null)
			{
				this.ParseChildren();
			}
			return this.m_children;
		}

		// Token: 0x17000581 RID: 1409
		// (get) Token: 0x060029DE RID: 10718 RVA: 0x0009ABF6 File Offset: 0x00098DF6
		// (set) Token: 0x060029DF RID: 10719 RVA: 0x0009AC19 File Offset: 0x00098E19
		public IMembershipCondition MembershipCondition
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_membershipCondition == null && this.m_element != null)
				{
					this.ParseMembershipCondition();
				}
				return this.m_membershipCondition.Copy();
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("MembershipCondition");
				}
				this.m_membershipCondition = value.Copy();
			}
		}

		// Token: 0x17000582 RID: 1410
		// (get) Token: 0x060029E0 RID: 10720 RVA: 0x0009AC35 File Offset: 0x00098E35
		// (set) Token: 0x060029E1 RID: 10721 RVA: 0x0009AC62 File Offset: 0x00098E62
		public PolicyStatement PolicyStatement
		{
			get
			{
				if (this.m_policy == null && this.m_element != null)
				{
					this.ParsePolicy();
				}
				if (this.m_policy != null)
				{
					return this.m_policy.Copy();
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					this.m_policy = value.Copy();
					return;
				}
				this.m_policy = null;
			}
		}

		// Token: 0x17000583 RID: 1411
		// (get) Token: 0x060029E2 RID: 10722 RVA: 0x0009AC7B File Offset: 0x00098E7B
		// (set) Token: 0x060029E3 RID: 10723 RVA: 0x0009AC83 File Offset: 0x00098E83
		public string Name
		{
			get
			{
				return this.m_name;
			}
			set
			{
				this.m_name = value;
			}
		}

		// Token: 0x17000584 RID: 1412
		// (get) Token: 0x060029E4 RID: 10724 RVA: 0x0009AC8C File Offset: 0x00098E8C
		// (set) Token: 0x060029E5 RID: 10725 RVA: 0x0009AC94 File Offset: 0x00098E94
		public string Description
		{
			get
			{
				return this.m_description;
			}
			set
			{
				this.m_description = value;
			}
		}

		// Token: 0x060029E6 RID: 10726
		public abstract PolicyStatement Resolve(Evidence evidence);

		// Token: 0x060029E7 RID: 10727
		public abstract CodeGroup ResolveMatchingCodeGroups(Evidence evidence);

		// Token: 0x060029E8 RID: 10728
		public abstract CodeGroup Copy();

		// Token: 0x17000585 RID: 1413
		// (get) Token: 0x060029E9 RID: 10729 RVA: 0x0009ACA0 File Offset: 0x00098EA0
		public virtual string PermissionSetName
		{
			get
			{
				if (this.m_policy == null && this.m_element != null)
				{
					this.ParsePolicy();
				}
				if (this.m_policy == null)
				{
					return null;
				}
				NamedPermissionSet namedPermissionSet = this.m_policy.GetPermissionSetNoCopy() as NamedPermissionSet;
				if (namedPermissionSet != null)
				{
					return namedPermissionSet.Name;
				}
				return null;
			}
		}

		// Token: 0x17000586 RID: 1414
		// (get) Token: 0x060029EA RID: 10730 RVA: 0x0009ACE9 File Offset: 0x00098EE9
		public virtual string AttributeString
		{
			get
			{
				if (this.m_policy == null && this.m_element != null)
				{
					this.ParsePolicy();
				}
				if (this.m_policy != null)
				{
					return this.m_policy.AttributeString;
				}
				return null;
			}
		}

		// Token: 0x17000587 RID: 1415
		// (get) Token: 0x060029EB RID: 10731
		public abstract string MergeLogic { get; }

		// Token: 0x060029EC RID: 10732 RVA: 0x0009AD16 File Offset: 0x00098F16
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x060029ED RID: 10733 RVA: 0x0009AD1F File Offset: 0x00098F1F
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x060029EE RID: 10734 RVA: 0x0009AD29 File Offset: 0x00098F29
		[SecuritySafeCritical]
		public SecurityElement ToXml(PolicyLevel level)
		{
			return this.ToXml(level, this.GetTypeName());
		}

		// Token: 0x060029EF RID: 10735 RVA: 0x0009AD38 File Offset: 0x00098F38
		internal virtual string GetTypeName()
		{
			return base.GetType().FullName;
		}

		// Token: 0x060029F0 RID: 10736 RVA: 0x0009AD48 File Offset: 0x00098F48
		[SecurityCritical]
		internal SecurityElement ToXml(PolicyLevel level, string policyClassName)
		{
			if (this.m_membershipCondition == null && this.m_element != null)
			{
				this.ParseMembershipCondition();
			}
			if (this.m_children == null)
			{
				this.ParseChildren();
			}
			if (this.m_policy == null && this.m_element != null)
			{
				this.ParsePolicy();
			}
			SecurityElement securityElement = new SecurityElement("CodeGroup");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), policyClassName);
			securityElement.AddAttribute("version", "1");
			securityElement.AddChild(this.m_membershipCondition.ToXml(level));
			if (this.m_policy != null)
			{
				PermissionSet permissionSetNoCopy = this.m_policy.GetPermissionSetNoCopy();
				NamedPermissionSet namedPermissionSet = permissionSetNoCopy as NamedPermissionSet;
				if (namedPermissionSet != null && level != null && level.GetNamedPermissionSetInternal(namedPermissionSet.Name) != null)
				{
					securityElement.AddAttribute("PermissionSetName", namedPermissionSet.Name);
				}
				else if (!permissionSetNoCopy.IsEmpty())
				{
					securityElement.AddChild(permissionSetNoCopy.ToXml());
				}
				if (this.m_policy.Attributes != PolicyStatementAttribute.Nothing)
				{
					securityElement.AddAttribute("Attributes", XMLUtil.BitFieldEnumToString(typeof(PolicyStatementAttribute), this.m_policy.Attributes));
				}
			}
			if (this.m_children.Count > 0)
			{
				lock (this)
				{
					foreach (object obj in this.m_children)
					{
						securityElement.AddChild(((CodeGroup)obj).ToXml(level));
					}
				}
			}
			if (this.m_name != null)
			{
				securityElement.AddAttribute("Name", SecurityElement.Escape(this.m_name));
			}
			if (this.m_description != null)
			{
				securityElement.AddAttribute("Description", SecurityElement.Escape(this.m_description));
			}
			this.CreateXml(securityElement, level);
			return securityElement;
		}

		// Token: 0x060029F1 RID: 10737 RVA: 0x0009AF08 File Offset: 0x00099108
		protected virtual void CreateXml(SecurityElement element, PolicyLevel level)
		{
		}

		// Token: 0x060029F2 RID: 10738 RVA: 0x0009AF0C File Offset: 0x0009910C
		public void FromXml(SecurityElement e, PolicyLevel level)
		{
			if (e == null)
			{
				throw new ArgumentNullException("e");
			}
			lock (this)
			{
				this.m_element = e;
				this.m_parentLevel = level;
				this.m_children = null;
				this.m_membershipCondition = null;
				this.m_policy = null;
				this.m_name = e.Attribute("Name");
				this.m_description = e.Attribute("Description");
				this.ParseXml(e, level);
			}
		}

		// Token: 0x060029F3 RID: 10739 RVA: 0x0009AF9C File Offset: 0x0009919C
		protected virtual void ParseXml(SecurityElement e, PolicyLevel level)
		{
		}

		// Token: 0x060029F4 RID: 10740 RVA: 0x0009AFA0 File Offset: 0x000991A0
		[SecurityCritical]
		private bool ParseMembershipCondition(bool safeLoad)
		{
			bool result;
			lock (this)
			{
				IMembershipCondition membershipCondition = null;
				SecurityElement securityElement = this.m_element.SearchForChildByTag("IMembershipCondition");
				if (securityElement == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidXMLElement"), "IMembershipCondition", base.GetType().FullName));
				}
				try
				{
					membershipCondition = XMLUtil.CreateMembershipCondition(securityElement);
					if (membershipCondition == null)
					{
						return false;
					}
				}
				catch (Exception innerException)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_MembershipConditionElement"), innerException);
				}
				membershipCondition.FromXml(securityElement, this.m_parentLevel);
				this.m_membershipCondition = membershipCondition;
				result = true;
			}
			return result;
		}

		// Token: 0x060029F5 RID: 10741 RVA: 0x0009B064 File Offset: 0x00099264
		[SecurityCritical]
		private void ParseMembershipCondition()
		{
			this.ParseMembershipCondition(false);
		}

		// Token: 0x060029F6 RID: 10742 RVA: 0x0009B070 File Offset: 0x00099270
		[SecurityCritical]
		internal void ParseChildren()
		{
			lock (this)
			{
				ArrayList arrayList = ArrayList.Synchronized(new ArrayList());
				if (this.m_element != null && this.m_element.InternalChildren != null)
				{
					this.m_element.Children = (ArrayList)this.m_element.InternalChildren.Clone();
					ArrayList arrayList2 = ArrayList.Synchronized(new ArrayList());
					Evidence evidence = new Evidence();
					int count = this.m_element.InternalChildren.Count;
					int i = 0;
					while (i < count)
					{
						SecurityElement securityElement = (SecurityElement)this.m_element.Children[i];
						if (securityElement.Tag.Equals("CodeGroup"))
						{
							CodeGroup codeGroup = XMLUtil.CreateCodeGroup(securityElement);
							if (codeGroup != null)
							{
								codeGroup.FromXml(securityElement, this.m_parentLevel);
								if (this.ParseMembershipCondition(true))
								{
									codeGroup.Resolve(evidence);
									codeGroup.MembershipCondition.Check(evidence);
									arrayList.Add(codeGroup);
									i++;
								}
								else
								{
									this.m_element.InternalChildren.RemoveAt(i);
									count = this.m_element.InternalChildren.Count;
									arrayList2.Add(new CodeGroupPositionMarker(i, arrayList.Count, securityElement));
								}
							}
							else
							{
								this.m_element.InternalChildren.RemoveAt(i);
								count = this.m_element.InternalChildren.Count;
								arrayList2.Add(new CodeGroupPositionMarker(i, arrayList.Count, securityElement));
							}
						}
						else
						{
							i++;
						}
					}
					foreach (object obj in arrayList2)
					{
						CodeGroupPositionMarker codeGroupPositionMarker = (CodeGroupPositionMarker)obj;
						CodeGroup codeGroup2 = XMLUtil.CreateCodeGroup(codeGroupPositionMarker.element);
						if (codeGroup2 == null)
						{
							throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_FailedCodeGroup"), codeGroupPositionMarker.element.Attribute("class")));
						}
						codeGroup2.FromXml(codeGroupPositionMarker.element, this.m_parentLevel);
						codeGroup2.Resolve(evidence);
						codeGroup2.MembershipCondition.Check(evidence);
						arrayList.Insert(codeGroupPositionMarker.groupIndex, codeGroup2);
						this.m_element.InternalChildren.Insert(codeGroupPositionMarker.elementIndex, codeGroupPositionMarker.element);
					}
				}
				this.m_children = arrayList;
			}
		}

		// Token: 0x060029F7 RID: 10743 RVA: 0x0009B2F0 File Offset: 0x000994F0
		private void ParsePolicy()
		{
			for (;;)
			{
				PolicyStatement policyStatement = new PolicyStatement();
				bool flag = false;
				SecurityElement securityElement = new SecurityElement("PolicyStatement");
				securityElement.AddAttribute("version", "1");
				SecurityElement element = this.m_element;
				lock (this)
				{
					if (this.m_element != null)
					{
						string text = this.m_element.Attribute("PermissionSetName");
						if (text != null)
						{
							securityElement.AddAttribute("PermissionSetName", text);
							flag = true;
						}
						else
						{
							SecurityElement securityElement2 = this.m_element.SearchForChildByTag("PermissionSet");
							if (securityElement2 != null)
							{
								securityElement.AddChild(securityElement2);
								flag = true;
							}
							else
							{
								securityElement.AddChild(new PermissionSet(false).ToXml());
								flag = true;
							}
						}
						string text2 = this.m_element.Attribute("Attributes");
						if (text2 != null)
						{
							securityElement.AddAttribute("Attributes", text2);
							flag = true;
						}
					}
				}
				if (flag)
				{
					policyStatement.FromXml(securityElement, this.m_parentLevel);
				}
				else
				{
					policyStatement.PermissionSet = null;
				}
				lock (this)
				{
					if (element == this.m_element && this.m_policy == null)
					{
						this.m_policy = policyStatement;
					}
					else if (this.m_policy == null)
					{
						continue;
					}
				}
				break;
			}
			if (this.m_policy != null && this.m_children != null)
			{
				IMembershipCondition membershipCondition = this.m_membershipCondition;
			}
		}

		// Token: 0x060029F8 RID: 10744 RVA: 0x0009B460 File Offset: 0x00099660
		[SecuritySafeCritical]
		public override bool Equals(object o)
		{
			CodeGroup codeGroup = o as CodeGroup;
			if (codeGroup != null && base.GetType().Equals(codeGroup.GetType()) && object.Equals(this.m_name, codeGroup.m_name) && object.Equals(this.m_description, codeGroup.m_description))
			{
				if (this.m_membershipCondition == null && this.m_element != null)
				{
					this.ParseMembershipCondition();
				}
				if (codeGroup.m_membershipCondition == null && codeGroup.m_element != null)
				{
					codeGroup.ParseMembershipCondition();
				}
				if (object.Equals(this.m_membershipCondition, codeGroup.m_membershipCondition))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060029F9 RID: 10745 RVA: 0x0009B4F4 File Offset: 0x000996F4
		[SecuritySafeCritical]
		public bool Equals(CodeGroup cg, bool compareChildren)
		{
			if (!this.Equals(cg))
			{
				return false;
			}
			if (compareChildren)
			{
				if (this.m_children == null)
				{
					this.ParseChildren();
				}
				if (cg.m_children == null)
				{
					cg.ParseChildren();
				}
				ArrayList arrayList = new ArrayList(this.m_children);
				ArrayList arrayList2 = new ArrayList(cg.m_children);
				if (arrayList.Count != arrayList2.Count)
				{
					return false;
				}
				for (int i = 0; i < arrayList.Count; i++)
				{
					if (!((CodeGroup)arrayList[i]).Equals((CodeGroup)arrayList2[i], true))
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x060029FA RID: 10746 RVA: 0x0009B588 File Offset: 0x00099788
		[SecuritySafeCritical]
		public override int GetHashCode()
		{
			if (this.m_membershipCondition == null && this.m_element != null)
			{
				this.ParseMembershipCondition();
			}
			if (this.m_name != null || this.m_membershipCondition != null)
			{
				return ((this.m_name == null) ? 0 : this.m_name.GetHashCode()) + ((this.m_membershipCondition == null) ? 0 : this.m_membershipCondition.GetHashCode());
			}
			return base.GetType().GetHashCode();
		}

		// Token: 0x04001128 RID: 4392
		private IMembershipCondition m_membershipCondition;

		// Token: 0x04001129 RID: 4393
		private IList m_children;

		// Token: 0x0400112A RID: 4394
		private PolicyStatement m_policy;

		// Token: 0x0400112B RID: 4395
		private SecurityElement m_element;

		// Token: 0x0400112C RID: 4396
		private PolicyLevel m_parentLevel;

		// Token: 0x0400112D RID: 4397
		private string m_name;

		// Token: 0x0400112E RID: 4398
		private string m_description;
	}
}
