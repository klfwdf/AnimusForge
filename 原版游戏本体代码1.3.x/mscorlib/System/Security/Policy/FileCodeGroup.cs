using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000353 RID: 851
	[ComVisible(true)]
	[Serializable]
	public sealed class FileCodeGroup : CodeGroup, IUnionSemanticCodeGroup
	{
		// Token: 0x06002A59 RID: 10841 RVA: 0x0009CD79 File Offset: 0x0009AF79
		internal FileCodeGroup()
		{
		}

		// Token: 0x06002A5A RID: 10842 RVA: 0x0009CD81 File Offset: 0x0009AF81
		public FileCodeGroup(IMembershipCondition membershipCondition, FileIOPermissionAccess access)
			: base(membershipCondition, null)
		{
			this.m_access = access;
		}

		// Token: 0x06002A5B RID: 10843 RVA: 0x0009CD94 File Offset: 0x0009AF94
		[SecuritySafeCritical]
		public override PolicyStatement Resolve(Evidence evidence)
		{
			if (evidence == null)
			{
				throw new ArgumentNullException("evidence");
			}
			object obj = null;
			if (PolicyManager.CheckMembershipCondition(base.MembershipCondition, evidence, out obj))
			{
				PolicyStatement policyStatement = this.CalculateAssemblyPolicy(evidence);
				IDelayEvaluatedEvidence delayEvaluatedEvidence = obj as IDelayEvaluatedEvidence;
				bool flag = delayEvaluatedEvidence != null && !delayEvaluatedEvidence.IsVerified;
				if (flag)
				{
					policyStatement.AddDependentEvidence(delayEvaluatedEvidence);
				}
				bool flag2 = false;
				IEnumerator enumerator = base.Children.GetEnumerator();
				while (enumerator.MoveNext() && !flag2)
				{
					PolicyStatement policyStatement2 = PolicyManager.ResolveCodeGroup(enumerator.Current as CodeGroup, evidence);
					if (policyStatement2 != null)
					{
						policyStatement.InplaceUnion(policyStatement2);
						if ((policyStatement2.Attributes & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive)
						{
							flag2 = true;
						}
					}
				}
				return policyStatement;
			}
			return null;
		}

		// Token: 0x06002A5C RID: 10844 RVA: 0x0009CE3B File Offset: 0x0009B03B
		PolicyStatement IUnionSemanticCodeGroup.InternalResolve(Evidence evidence)
		{
			if (evidence == null)
			{
				throw new ArgumentNullException("evidence");
			}
			if (base.MembershipCondition.Check(evidence))
			{
				return this.CalculateAssemblyPolicy(evidence);
			}
			return null;
		}

		// Token: 0x06002A5D RID: 10845 RVA: 0x0009CE64 File Offset: 0x0009B064
		public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence)
		{
			if (evidence == null)
			{
				throw new ArgumentNullException("evidence");
			}
			if (base.MembershipCondition.Check(evidence))
			{
				CodeGroup codeGroup = this.Copy();
				codeGroup.Children = new ArrayList();
				foreach (object obj in base.Children)
				{
					CodeGroup codeGroup2 = ((CodeGroup)obj).ResolveMatchingCodeGroups(evidence);
					if (codeGroup2 != null)
					{
						codeGroup.AddChild(codeGroup2);
					}
				}
				return codeGroup;
			}
			return null;
		}

		// Token: 0x06002A5E RID: 10846 RVA: 0x0009CED4 File Offset: 0x0009B0D4
		internal PolicyStatement CalculatePolicy(Url url)
		{
			URLString urlstring = url.GetURLString();
			if (string.Compare(urlstring.Scheme, "file", StringComparison.OrdinalIgnoreCase) != 0)
			{
				return null;
			}
			string directoryName = urlstring.GetDirectoryName();
			PermissionSet permissionSet = new PermissionSet(PermissionState.None);
			permissionSet.SetPermission(new FileIOPermission(this.m_access, Path.GetFullPath(directoryName)));
			return new PolicyStatement(permissionSet, PolicyStatementAttribute.Nothing);
		}

		// Token: 0x06002A5F RID: 10847 RVA: 0x0009CF2C File Offset: 0x0009B12C
		private PolicyStatement CalculateAssemblyPolicy(Evidence evidence)
		{
			PolicyStatement policyStatement = null;
			Url hostEvidence = evidence.GetHostEvidence<Url>();
			if (hostEvidence != null)
			{
				policyStatement = this.CalculatePolicy(hostEvidence);
			}
			if (policyStatement == null)
			{
				policyStatement = new PolicyStatement(new PermissionSet(false), PolicyStatementAttribute.Nothing);
			}
			return policyStatement;
		}

		// Token: 0x06002A60 RID: 10848 RVA: 0x0009CF60 File Offset: 0x0009B160
		public override CodeGroup Copy()
		{
			FileCodeGroup fileCodeGroup = new FileCodeGroup(base.MembershipCondition, this.m_access);
			fileCodeGroup.Name = base.Name;
			fileCodeGroup.Description = base.Description;
			foreach (object obj in base.Children)
			{
				fileCodeGroup.AddChild((CodeGroup)obj);
			}
			return fileCodeGroup;
		}

		// Token: 0x1700059D RID: 1437
		// (get) Token: 0x06002A61 RID: 10849 RVA: 0x0009CFBF File Offset: 0x0009B1BF
		public override string MergeLogic
		{
			get
			{
				return Environment.GetResourceString("MergeLogic_Union");
			}
		}

		// Token: 0x1700059E RID: 1438
		// (get) Token: 0x06002A62 RID: 10850 RVA: 0x0009CFCB File Offset: 0x0009B1CB
		public override string PermissionSetName
		{
			get
			{
				return Environment.GetResourceString("FileCodeGroup_PermissionSet", new object[] { XMLUtil.BitFieldEnumToString(typeof(FileIOPermissionAccess), this.m_access) });
			}
		}

		// Token: 0x1700059F RID: 1439
		// (get) Token: 0x06002A63 RID: 10851 RVA: 0x0009CFFA File Offset: 0x0009B1FA
		public override string AttributeString
		{
			get
			{
				return null;
			}
		}

		// Token: 0x06002A64 RID: 10852 RVA: 0x0009CFFD File Offset: 0x0009B1FD
		protected override void CreateXml(SecurityElement element, PolicyLevel level)
		{
			element.AddAttribute("Access", XMLUtil.BitFieldEnumToString(typeof(FileIOPermissionAccess), this.m_access));
		}

		// Token: 0x06002A65 RID: 10853 RVA: 0x0009D024 File Offset: 0x0009B224
		protected override void ParseXml(SecurityElement e, PolicyLevel level)
		{
			string text = e.Attribute("Access");
			if (text != null)
			{
				this.m_access = (FileIOPermissionAccess)Enum.Parse(typeof(FileIOPermissionAccess), text);
				return;
			}
			this.m_access = FileIOPermissionAccess.NoAccess;
		}

		// Token: 0x06002A66 RID: 10854 RVA: 0x0009D064 File Offset: 0x0009B264
		public override bool Equals(object o)
		{
			FileCodeGroup fileCodeGroup = o as FileCodeGroup;
			return fileCodeGroup != null && base.Equals(fileCodeGroup) && this.m_access == fileCodeGroup.m_access;
		}

		// Token: 0x06002A67 RID: 10855 RVA: 0x0009D095 File Offset: 0x0009B295
		public override int GetHashCode()
		{
			return base.GetHashCode() + this.m_access.GetHashCode();
		}

		// Token: 0x06002A68 RID: 10856 RVA: 0x0009D0AF File Offset: 0x0009B2AF
		internal override string GetTypeName()
		{
			return "System.Security.Policy.FileCodeGroup";
		}

		// Token: 0x04001143 RID: 4419
		private FileIOPermissionAccess m_access;
	}
}
