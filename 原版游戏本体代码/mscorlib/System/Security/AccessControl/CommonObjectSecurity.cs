using System;
using System.Security.Permissions;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000228 RID: 552
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	public abstract class CommonObjectSecurity : ObjectSecurity
	{
		// Token: 0x06001FE0 RID: 8160 RVA: 0x0006F067 File Offset: 0x0006D267
		protected CommonObjectSecurity(bool isContainer)
			: base(isContainer, false)
		{
		}

		// Token: 0x06001FE1 RID: 8161 RVA: 0x0006F071 File Offset: 0x0006D271
		internal CommonObjectSecurity(CommonSecurityDescriptor securityDescriptor)
			: base(securityDescriptor)
		{
		}

		// Token: 0x06001FE2 RID: 8162 RVA: 0x0006F07C File Offset: 0x0006D27C
		private AuthorizationRuleCollection GetRules(bool access, bool includeExplicit, bool includeInherited, Type targetType)
		{
			base.ReadLock();
			AuthorizationRuleCollection result;
			try
			{
				AuthorizationRuleCollection authorizationRuleCollection = new AuthorizationRuleCollection();
				if (!SecurityIdentifier.IsValidTargetTypeStatic(targetType))
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_MustBeIdentityReferenceType"), "targetType");
				}
				CommonAcl commonAcl = null;
				if (access)
				{
					if ((this._securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != ControlFlags.None)
					{
						commonAcl = this._securityDescriptor.DiscretionaryAcl;
					}
				}
				else if ((this._securityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != ControlFlags.None)
				{
					commonAcl = this._securityDescriptor.SystemAcl;
				}
				if (commonAcl == null)
				{
					result = authorizationRuleCollection;
				}
				else
				{
					IdentityReferenceCollection identityReferenceCollection = null;
					if (targetType != typeof(SecurityIdentifier))
					{
						IdentityReferenceCollection identityReferenceCollection2 = new IdentityReferenceCollection(commonAcl.Count);
						for (int i = 0; i < commonAcl.Count; i++)
						{
							CommonAce commonAce = commonAcl[i] as CommonAce;
							if (this.AceNeedsTranslation(commonAce, access, includeExplicit, includeInherited))
							{
								identityReferenceCollection2.Add(commonAce.SecurityIdentifier);
							}
						}
						identityReferenceCollection = identityReferenceCollection2.Translate(targetType);
					}
					int num = 0;
					for (int j = 0; j < commonAcl.Count; j++)
					{
						CommonAce commonAce2 = commonAcl[j] as CommonAce;
						if (this.AceNeedsTranslation(commonAce2, access, includeExplicit, includeInherited))
						{
							IdentityReference identityReference = ((targetType == typeof(SecurityIdentifier)) ? commonAce2.SecurityIdentifier : identityReferenceCollection[num++]);
							if (access)
							{
								AccessControlType type;
								if (commonAce2.AceQualifier == AceQualifier.AccessAllowed)
								{
									type = AccessControlType.Allow;
								}
								else
								{
									type = AccessControlType.Deny;
								}
								authorizationRuleCollection.AddRule(this.AccessRuleFactory(identityReference, commonAce2.AccessMask, commonAce2.IsInherited, commonAce2.InheritanceFlags, commonAce2.PropagationFlags, type));
							}
							else
							{
								authorizationRuleCollection.AddRule(this.AuditRuleFactory(identityReference, commonAce2.AccessMask, commonAce2.IsInherited, commonAce2.InheritanceFlags, commonAce2.PropagationFlags, commonAce2.AuditFlags));
							}
						}
					}
					result = authorizationRuleCollection;
				}
			}
			finally
			{
				base.ReadUnlock();
			}
			return result;
		}

		// Token: 0x06001FE3 RID: 8163 RVA: 0x0006F26C File Offset: 0x0006D46C
		private bool AceNeedsTranslation(CommonAce ace, bool isAccessAce, bool includeExplicit, bool includeInherited)
		{
			if (ace == null)
			{
				return false;
			}
			if (isAccessAce)
			{
				if (ace.AceQualifier != AceQualifier.AccessAllowed && ace.AceQualifier != AceQualifier.AccessDenied)
				{
					return false;
				}
			}
			else if (ace.AceQualifier != AceQualifier.SystemAudit)
			{
				return false;
			}
			return (includeExplicit && (ace.AceFlags & AceFlags.Inherited) == AceFlags.None) || (includeInherited && (ace.AceFlags & AceFlags.Inherited) != AceFlags.None);
		}

		// Token: 0x06001FE4 RID: 8164 RVA: 0x0006F2C8 File Offset: 0x0006D4C8
		protected override bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			bool result;
			try
			{
				bool flag = true;
				if (this._securityDescriptor.DiscretionaryAcl == null)
				{
					if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
					{
						modified = false;
						return flag;
					}
					this._securityDescriptor.DiscretionaryAcl = new DiscretionaryAcl(base.IsContainer, base.IsDS, GenericAcl.AclRevision, 1);
					this._securityDescriptor.AddControlFlags(ControlFlags.DiscretionaryAclPresent);
				}
				SecurityIdentifier sid = rule.IdentityReference.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
				if (rule.AccessControlType == AccessControlType.Allow)
				{
					switch (modification)
					{
					case AccessControlModification.Add:
						this._securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.Set:
						this._securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.Reset:
						this._securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
						this._securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.Remove:
						flag = this._securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.RemoveAll:
						flag = this._securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
						if (!flag)
						{
							throw new SystemException();
						}
						break;
					case AccessControlModification.RemoveSpecific:
						this._securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Allow, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					default:
						throw new ArgumentOutOfRangeException("modification", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
					}
				}
				else
				{
					if (rule.AccessControlType != AccessControlType.Deny)
					{
						throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)rule.AccessControlType }), "rule.AccessControlType");
					}
					switch (modification)
					{
					case AccessControlModification.Add:
						this._securityDescriptor.DiscretionaryAcl.AddAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.Set:
						this._securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.Reset:
						this._securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Allow, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
						this._securityDescriptor.DiscretionaryAcl.SetAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.Remove:
						flag = this._securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					case AccessControlModification.RemoveAll:
						flag = this._securityDescriptor.DiscretionaryAcl.RemoveAccess(AccessControlType.Deny, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
						if (!flag)
						{
							throw new SystemException();
						}
						break;
					case AccessControlModification.RemoveSpecific:
						this._securityDescriptor.DiscretionaryAcl.RemoveAccessSpecific(AccessControlType.Deny, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
						break;
					default:
						throw new ArgumentOutOfRangeException("modification", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
					}
				}
				modified = flag;
				base.AccessRulesModified |= modified;
				result = flag;
			}
			finally
			{
				base.WriteUnlock();
			}
			return result;
		}

		// Token: 0x06001FE5 RID: 8165 RVA: 0x0006F648 File Offset: 0x0006D848
		protected override bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			bool result;
			try
			{
				bool flag = true;
				if (this._securityDescriptor.SystemAcl == null)
				{
					if (modification == AccessControlModification.Remove || modification == AccessControlModification.RemoveAll || modification == AccessControlModification.RemoveSpecific)
					{
						modified = false;
						return flag;
					}
					this._securityDescriptor.SystemAcl = new SystemAcl(base.IsContainer, base.IsDS, GenericAcl.AclRevision, 1);
					this._securityDescriptor.AddControlFlags(ControlFlags.SystemAclPresent);
				}
				SecurityIdentifier sid = rule.IdentityReference.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
				switch (modification)
				{
				case AccessControlModification.Add:
					this._securityDescriptor.SystemAcl.AddAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Set:
					this._securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Reset:
					this._securityDescriptor.SystemAcl.SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.Remove:
					flag = this._securityDescriptor.SystemAcl.RemoveAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				case AccessControlModification.RemoveAll:
					flag = this._securityDescriptor.SystemAcl.RemoveAudit(AuditFlags.Success | AuditFlags.Failure, sid, -1, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None);
					if (!flag)
					{
						throw new InvalidProgramException();
					}
					break;
				case AccessControlModification.RemoveSpecific:
					this._securityDescriptor.SystemAcl.RemoveAuditSpecific(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags);
					break;
				default:
					throw new ArgumentOutOfRangeException("modification", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
				}
				modified = flag;
				base.AuditRulesModified |= modified;
				result = flag;
			}
			finally
			{
				base.WriteUnlock();
			}
			return result;
		}

		// Token: 0x06001FE6 RID: 8166 RVA: 0x0006F84C File Offset: 0x0006DA4C
		protected void AddAccessRule(AccessRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				bool flag;
				this.ModifyAccess(AccessControlModification.Add, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FE7 RID: 8167 RVA: 0x0006F894 File Offset: 0x0006DA94
		protected void SetAccessRule(AccessRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				bool flag;
				this.ModifyAccess(AccessControlModification.Set, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FE8 RID: 8168 RVA: 0x0006F8DC File Offset: 0x0006DADC
		protected void ResetAccessRule(AccessRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				bool flag;
				this.ModifyAccess(AccessControlModification.Reset, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FE9 RID: 8169 RVA: 0x0006F924 File Offset: 0x0006DB24
		protected bool RemoveAccessRule(AccessRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			bool result;
			try
			{
				if (this._securityDescriptor == null)
				{
					result = true;
				}
				else
				{
					bool flag;
					result = this.ModifyAccess(AccessControlModification.Remove, rule, out flag);
				}
			}
			finally
			{
				base.WriteUnlock();
			}
			return result;
		}

		// Token: 0x06001FEA RID: 8170 RVA: 0x0006F978 File Offset: 0x0006DB78
		protected void RemoveAccessRuleAll(AccessRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				if (this._securityDescriptor == null)
				{
					return;
				}
				bool flag;
				this.ModifyAccess(AccessControlModification.RemoveAll, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FEB RID: 8171 RVA: 0x0006F9C8 File Offset: 0x0006DBC8
		protected void RemoveAccessRuleSpecific(AccessRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				if (this._securityDescriptor != null)
				{
					bool flag;
					this.ModifyAccess(AccessControlModification.RemoveSpecific, rule, out flag);
				}
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FEC RID: 8172 RVA: 0x0006FA18 File Offset: 0x0006DC18
		protected void AddAuditRule(AuditRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				bool flag;
				this.ModifyAudit(AccessControlModification.Add, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FED RID: 8173 RVA: 0x0006FA60 File Offset: 0x0006DC60
		protected void SetAuditRule(AuditRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				bool flag;
				this.ModifyAudit(AccessControlModification.Set, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FEE RID: 8174 RVA: 0x0006FAA8 File Offset: 0x0006DCA8
		protected bool RemoveAuditRule(AuditRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			bool result;
			try
			{
				bool flag;
				result = this.ModifyAudit(AccessControlModification.Remove, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
			return result;
		}

		// Token: 0x06001FEF RID: 8175 RVA: 0x0006FAF0 File Offset: 0x0006DCF0
		protected void RemoveAuditRuleAll(AuditRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				bool flag;
				this.ModifyAudit(AccessControlModification.RemoveAll, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FF0 RID: 8176 RVA: 0x0006FB38 File Offset: 0x0006DD38
		protected void RemoveAuditRuleSpecific(AuditRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			base.WriteLock();
			try
			{
				bool flag;
				this.ModifyAudit(AccessControlModification.RemoveSpecific, rule, out flag);
			}
			finally
			{
				base.WriteUnlock();
			}
		}

		// Token: 0x06001FF1 RID: 8177 RVA: 0x0006FB80 File Offset: 0x0006DD80
		public AuthorizationRuleCollection GetAccessRules(bool includeExplicit, bool includeInherited, Type targetType)
		{
			return this.GetRules(true, includeExplicit, includeInherited, targetType);
		}

		// Token: 0x06001FF2 RID: 8178 RVA: 0x0006FB8C File Offset: 0x0006DD8C
		public AuthorizationRuleCollection GetAuditRules(bool includeExplicit, bool includeInherited, Type targetType)
		{
			return this.GetRules(false, includeExplicit, includeInherited, targetType);
		}
	}
}
