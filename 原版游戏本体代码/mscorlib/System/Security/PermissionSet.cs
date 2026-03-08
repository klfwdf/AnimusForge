using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.Policy;
using System.Security.Util;
using System.Text;
using System.Threading;

namespace System.Security
{
	// Token: 0x020001DE RID: 478
	[ComVisible(true)]
	[StrongNameIdentityPermission(SecurityAction.InheritanceDemand, Name = "mscorlib", PublicKey = "0x00000000000000000400000000000000")]
	[Serializable]
	public class PermissionSet : ISecurityEncodable, ICollection, IEnumerable, IStackWalk, IDeserializationCallback
	{
		// Token: 0x06001CC4 RID: 7364 RVA: 0x000625D8 File Offset: 0x000607D8
		[Conditional("_DEBUG")]
		private static void DEBUG_WRITE(string str)
		{
		}

		// Token: 0x06001CC5 RID: 7365 RVA: 0x000625DA File Offset: 0x000607DA
		[Conditional("_DEBUG")]
		private static void DEBUG_COND_WRITE(bool exp, string str)
		{
		}

		// Token: 0x06001CC6 RID: 7366 RVA: 0x000625DC File Offset: 0x000607DC
		[Conditional("_DEBUG")]
		private static void DEBUG_PRINTSTACK(Exception e)
		{
		}

		// Token: 0x06001CC7 RID: 7367 RVA: 0x000625DE File Offset: 0x000607DE
		[OnDeserializing]
		private void OnDeserializing(StreamingContext ctx)
		{
			this.Reset();
		}

		// Token: 0x06001CC8 RID: 7368 RVA: 0x000625E8 File Offset: 0x000607E8
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			if (this.m_serializedPermissionSet != null)
			{
				this.FromXml(SecurityElement.FromString(this.m_serializedPermissionSet));
			}
			else if (this.m_normalPermSet != null)
			{
				this.m_permSet = this.m_normalPermSet.SpecialUnion(this.m_unrestrictedPermSet);
			}
			else if (this.m_unrestrictedPermSet != null)
			{
				this.m_permSet = this.m_unrestrictedPermSet.SpecialUnion(this.m_normalPermSet);
			}
			this.m_serializedPermissionSet = null;
			this.m_normalPermSet = null;
			this.m_unrestrictedPermSet = null;
		}

		// Token: 0x06001CC9 RID: 7369 RVA: 0x00062668 File Offset: 0x00060868
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != (StreamingContextStates)0)
			{
				this.m_serializedPermissionSet = this.ToString();
				if (this.m_permSet != null)
				{
					this.m_permSet.SpecialSplit(ref this.m_unrestrictedPermSet, ref this.m_normalPermSet, this.m_ignoreTypeLoadFailures);
				}
				this.m_permSetSaved = this.m_permSet;
				this.m_permSet = null;
			}
		}

		// Token: 0x06001CCA RID: 7370 RVA: 0x000626C8 File Offset: 0x000608C8
		[OnSerialized]
		private void OnSerialized(StreamingContext context)
		{
			if ((context.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != (StreamingContextStates)0)
			{
				this.m_serializedPermissionSet = null;
				this.m_permSet = this.m_permSetSaved;
				this.m_permSetSaved = null;
				this.m_unrestrictedPermSet = null;
				this.m_normalPermSet = null;
			}
		}

		// Token: 0x06001CCB RID: 7371 RVA: 0x00062701 File Offset: 0x00060901
		internal PermissionSet()
		{
			this.Reset();
			this.m_Unrestricted = true;
		}

		// Token: 0x06001CCC RID: 7372 RVA: 0x00062716 File Offset: 0x00060916
		internal PermissionSet(bool fUnrestricted)
			: this()
		{
			this.SetUnrestricted(fUnrestricted);
		}

		// Token: 0x06001CCD RID: 7373 RVA: 0x00062725 File Offset: 0x00060925
		public PermissionSet(PermissionState state)
			: this()
		{
			if (state == PermissionState.Unrestricted)
			{
				this.SetUnrestricted(true);
				return;
			}
			if (state == PermissionState.None)
			{
				this.SetUnrestricted(false);
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
		}

		// Token: 0x06001CCE RID: 7374 RVA: 0x00062754 File Offset: 0x00060954
		public PermissionSet(PermissionSet permSet)
			: this()
		{
			if (permSet == null)
			{
				this.Reset();
				return;
			}
			this.m_Unrestricted = permSet.m_Unrestricted;
			this.m_CheckedForNonCas = permSet.m_CheckedForNonCas;
			this.m_ContainsCas = permSet.m_ContainsCas;
			this.m_ContainsNonCas = permSet.m_ContainsNonCas;
			this.m_ignoreTypeLoadFailures = permSet.m_ignoreTypeLoadFailures;
			if (permSet.m_permSet != null)
			{
				this.m_permSet = new TokenBasedSet(permSet.m_permSet);
				for (int i = this.m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); i++)
				{
					object item = this.m_permSet.GetItem(i);
					IPermission permission = item as IPermission;
					ISecurityElementFactory securityElementFactory = item as ISecurityElementFactory;
					if (permission != null)
					{
						this.m_permSet.SetItem(i, permission.Copy());
					}
					else if (securityElementFactory != null)
					{
						this.m_permSet.SetItem(i, securityElementFactory.Copy());
					}
				}
			}
		}

		// Token: 0x06001CCF RID: 7375 RVA: 0x00062830 File Offset: 0x00060A30
		public virtual void CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(this);
			while (permissionSetEnumeratorInternal.MoveNext())
			{
				object value = permissionSetEnumeratorInternal.Current;
				array.SetValue(value, index++);
			}
		}

		// Token: 0x06001CD0 RID: 7376 RVA: 0x00062871 File Offset: 0x00060A71
		private PermissionSet(object trash, object junk)
		{
			this.m_Unrestricted = false;
		}

		// Token: 0x1700033E RID: 830
		// (get) Token: 0x06001CD1 RID: 7377 RVA: 0x00062880 File Offset: 0x00060A80
		public virtual object SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x1700033F RID: 831
		// (get) Token: 0x06001CD2 RID: 7378 RVA: 0x00062883 File Offset: 0x00060A83
		public virtual bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000340 RID: 832
		// (get) Token: 0x06001CD3 RID: 7379 RVA: 0x00062886 File Offset: 0x00060A86
		public virtual bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06001CD4 RID: 7380 RVA: 0x00062889 File Offset: 0x00060A89
		internal void Reset()
		{
			this.m_Unrestricted = false;
			this.m_allPermissionsDecoded = true;
			this.m_permSet = null;
			this.m_ignoreTypeLoadFailures = false;
			this.m_CheckedForNonCas = false;
			this.m_ContainsCas = false;
			this.m_ContainsNonCas = false;
			this.m_permSetSaved = null;
		}

		// Token: 0x06001CD5 RID: 7381 RVA: 0x000628C3 File Offset: 0x00060AC3
		internal void CheckSet()
		{
			if (this.m_permSet == null)
			{
				this.m_permSet = new TokenBasedSet();
			}
		}

		// Token: 0x06001CD6 RID: 7382 RVA: 0x000628D8 File Offset: 0x00060AD8
		public bool IsEmpty()
		{
			if (this.m_Unrestricted)
			{
				return false;
			}
			if (this.m_permSet == null || this.m_permSet.FastIsEmpty())
			{
				return true;
			}
			PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(this);
			while (permissionSetEnumeratorInternal.MoveNext())
			{
				object obj = permissionSetEnumeratorInternal.Current;
				IPermission permission = (IPermission)obj;
				if (!permission.IsSubsetOf(null))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001CD7 RID: 7383 RVA: 0x00062932 File Offset: 0x00060B32
		internal bool FastIsEmpty()
		{
			return !this.m_Unrestricted && (this.m_permSet == null || this.m_permSet.FastIsEmpty());
		}

		// Token: 0x17000341 RID: 833
		// (get) Token: 0x06001CD8 RID: 7384 RVA: 0x00062958 File Offset: 0x00060B58
		public virtual int Count
		{
			get
			{
				int num = 0;
				if (this.m_permSet != null)
				{
					num += this.m_permSet.GetCount();
				}
				return num;
			}
		}

		// Token: 0x06001CD9 RID: 7385 RVA: 0x00062980 File Offset: 0x00060B80
		internal IPermission GetPermission(int index)
		{
			if (this.m_permSet == null)
			{
				return null;
			}
			object item = this.m_permSet.GetItem(index);
			if (item == null)
			{
				return null;
			}
			IPermission permission = item as IPermission;
			if (permission != null)
			{
				return permission;
			}
			permission = this.CreatePermission(item, index);
			if (permission == null)
			{
				return null;
			}
			return permission;
		}

		// Token: 0x06001CDA RID: 7386 RVA: 0x000629C4 File Offset: 0x00060BC4
		internal IPermission GetPermission(PermissionToken permToken)
		{
			if (permToken == null)
			{
				return null;
			}
			return this.GetPermission(permToken.m_index);
		}

		// Token: 0x06001CDB RID: 7387 RVA: 0x000629D7 File Offset: 0x00060BD7
		internal IPermission GetPermission(IPermission perm)
		{
			if (perm == null)
			{
				return null;
			}
			return this.GetPermission(PermissionToken.GetToken(perm));
		}

		// Token: 0x06001CDC RID: 7388 RVA: 0x000629EA File Offset: 0x00060BEA
		public IPermission GetPermission(Type permClass)
		{
			return this.GetPermissionImpl(permClass);
		}

		// Token: 0x06001CDD RID: 7389 RVA: 0x000629F3 File Offset: 0x00060BF3
		protected virtual IPermission GetPermissionImpl(Type permClass)
		{
			if (permClass == null)
			{
				return null;
			}
			return this.GetPermission(PermissionToken.FindToken(permClass));
		}

		// Token: 0x06001CDE RID: 7390 RVA: 0x00062A0C File Offset: 0x00060C0C
		public IPermission SetPermission(IPermission perm)
		{
			return this.SetPermissionImpl(perm);
		}

		// Token: 0x06001CDF RID: 7391 RVA: 0x00062A18 File Offset: 0x00060C18
		protected virtual IPermission SetPermissionImpl(IPermission perm)
		{
			if (perm == null)
			{
				return null;
			}
			PermissionToken token = PermissionToken.GetToken(perm);
			if ((token.m_type & PermissionTokenType.IUnrestricted) != (PermissionTokenType)0)
			{
				this.m_Unrestricted = false;
			}
			this.CheckSet();
			IPermission permission = this.GetPermission(token.m_index);
			this.m_CheckedForNonCas = false;
			this.m_permSet.SetItem(token.m_index, perm);
			return perm;
		}

		// Token: 0x06001CE0 RID: 7392 RVA: 0x00062A71 File Offset: 0x00060C71
		public IPermission AddPermission(IPermission perm)
		{
			return this.AddPermissionImpl(perm);
		}

		// Token: 0x06001CE1 RID: 7393 RVA: 0x00062A7C File Offset: 0x00060C7C
		protected virtual IPermission AddPermissionImpl(IPermission perm)
		{
			if (perm == null)
			{
				return null;
			}
			this.m_CheckedForNonCas = false;
			PermissionToken token = PermissionToken.GetToken(perm);
			if (this.IsUnrestricted() && (token.m_type & PermissionTokenType.IUnrestricted) != (PermissionTokenType)0)
			{
				Type type = perm.GetType();
				return (IPermission)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, new object[] { PermissionState.Unrestricted }, null);
			}
			this.CheckSet();
			IPermission permission = this.GetPermission(token.m_index);
			if (permission != null)
			{
				IPermission permission2 = permission.Union(perm);
				this.m_permSet.SetItem(token.m_index, permission2);
				return permission2;
			}
			this.m_permSet.SetItem(token.m_index, perm);
			return perm;
		}

		// Token: 0x06001CE2 RID: 7394 RVA: 0x00062B20 File Offset: 0x00060D20
		private IPermission RemovePermission(int index)
		{
			if (this.GetPermission(index) == null)
			{
				return null;
			}
			return (IPermission)this.m_permSet.RemoveItem(index);
		}

		// Token: 0x06001CE3 RID: 7395 RVA: 0x00062B4B File Offset: 0x00060D4B
		public IPermission RemovePermission(Type permClass)
		{
			return this.RemovePermissionImpl(permClass);
		}

		// Token: 0x06001CE4 RID: 7396 RVA: 0x00062B54 File Offset: 0x00060D54
		protected virtual IPermission RemovePermissionImpl(Type permClass)
		{
			if (permClass == null)
			{
				return null;
			}
			PermissionToken permissionToken = PermissionToken.FindToken(permClass);
			if (permissionToken == null)
			{
				return null;
			}
			return this.RemovePermission(permissionToken.m_index);
		}

		// Token: 0x06001CE5 RID: 7397 RVA: 0x00062B84 File Offset: 0x00060D84
		internal void SetUnrestricted(bool unrestricted)
		{
			this.m_Unrestricted = unrestricted;
			if (unrestricted)
			{
				this.m_permSet = null;
			}
		}

		// Token: 0x06001CE6 RID: 7398 RVA: 0x00062B97 File Offset: 0x00060D97
		public bool IsUnrestricted()
		{
			return this.m_Unrestricted;
		}

		// Token: 0x06001CE7 RID: 7399 RVA: 0x00062BA0 File Offset: 0x00060DA0
		internal bool IsSubsetOfHelper(PermissionSet target, PermissionSet.IsSubsetOfType type, out IPermission firstPermThatFailed, bool ignoreNonCas)
		{
			firstPermThatFailed = null;
			if (target == null || target.FastIsEmpty())
			{
				if (this.IsEmpty())
				{
					return true;
				}
				firstPermThatFailed = this.GetFirstPerm();
				return false;
			}
			else
			{
				if (this.IsUnrestricted() && !target.IsUnrestricted())
				{
					return false;
				}
				if (this.m_permSet == null)
				{
					return true;
				}
				target.CheckSet();
				for (int i = this.m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); i++)
				{
					IPermission permission = this.GetPermission(i);
					if (permission != null && !permission.IsSubsetOf(null))
					{
						IPermission permission2 = target.GetPermission(i);
						if (!target.m_Unrestricted)
						{
							CodeAccessPermission codeAccessPermission = permission as CodeAccessPermission;
							if (codeAccessPermission == null)
							{
								if (!ignoreNonCas && !permission.IsSubsetOf(permission2))
								{
									firstPermThatFailed = permission;
									return false;
								}
							}
							else
							{
								firstPermThatFailed = permission;
								switch (type)
								{
								case PermissionSet.IsSubsetOfType.Normal:
									if (!permission.IsSubsetOf(permission2))
									{
										return false;
									}
									break;
								case PermissionSet.IsSubsetOfType.CheckDemand:
									if (!codeAccessPermission.CheckDemand((CodeAccessPermission)permission2))
									{
										return false;
									}
									break;
								case PermissionSet.IsSubsetOfType.CheckPermitOnly:
									if (!codeAccessPermission.CheckPermitOnly((CodeAccessPermission)permission2))
									{
										return false;
									}
									break;
								case PermissionSet.IsSubsetOfType.CheckAssertion:
									if (!codeAccessPermission.CheckAssert((CodeAccessPermission)permission2))
									{
										return false;
									}
									break;
								}
								firstPermThatFailed = null;
							}
						}
					}
				}
				return true;
			}
		}

		// Token: 0x06001CE8 RID: 7400 RVA: 0x00062CB8 File Offset: 0x00060EB8
		public bool IsSubsetOf(PermissionSet target)
		{
			IPermission permission;
			return this.IsSubsetOfHelper(target, PermissionSet.IsSubsetOfType.Normal, out permission, false);
		}

		// Token: 0x06001CE9 RID: 7401 RVA: 0x00062CD0 File Offset: 0x00060ED0
		internal bool CheckDemand(PermissionSet target, out IPermission firstPermThatFailed)
		{
			return this.IsSubsetOfHelper(target, PermissionSet.IsSubsetOfType.CheckDemand, out firstPermThatFailed, true);
		}

		// Token: 0x06001CEA RID: 7402 RVA: 0x00062CDC File Offset: 0x00060EDC
		internal bool CheckPermitOnly(PermissionSet target, out IPermission firstPermThatFailed)
		{
			return this.IsSubsetOfHelper(target, PermissionSet.IsSubsetOfType.CheckPermitOnly, out firstPermThatFailed, true);
		}

		// Token: 0x06001CEB RID: 7403 RVA: 0x00062CE8 File Offset: 0x00060EE8
		internal bool CheckAssertion(PermissionSet target)
		{
			IPermission permission;
			return this.IsSubsetOfHelper(target, PermissionSet.IsSubsetOfType.CheckAssertion, out permission, true);
		}

		// Token: 0x06001CEC RID: 7404 RVA: 0x00062D00 File Offset: 0x00060F00
		internal bool CheckDeny(PermissionSet deniedSet, out IPermission firstPermThatFailed)
		{
			firstPermThatFailed = null;
			if (deniedSet == null || deniedSet.FastIsEmpty() || this.FastIsEmpty())
			{
				return true;
			}
			if (this.m_Unrestricted && deniedSet.m_Unrestricted)
			{
				return false;
			}
			PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(this);
			while (permissionSetEnumeratorInternal.MoveNext())
			{
				object obj = permissionSetEnumeratorInternal.Current;
				CodeAccessPermission codeAccessPermission = obj as CodeAccessPermission;
				if (codeAccessPermission != null && !codeAccessPermission.IsSubsetOf(null))
				{
					if (deniedSet.m_Unrestricted)
					{
						firstPermThatFailed = codeAccessPermission;
						return false;
					}
					CodeAccessPermission denied = (CodeAccessPermission)deniedSet.GetPermission(permissionSetEnumeratorInternal.GetCurrentIndex());
					if (!codeAccessPermission.CheckDeny(denied))
					{
						firstPermThatFailed = codeAccessPermission;
						return false;
					}
				}
			}
			if (this.m_Unrestricted)
			{
				PermissionSetEnumeratorInternal permissionSetEnumeratorInternal2 = new PermissionSetEnumeratorInternal(deniedSet);
				while (permissionSetEnumeratorInternal2.MoveNext())
				{
					if (permissionSetEnumeratorInternal2.Current is IPermission)
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06001CED RID: 7405 RVA: 0x00062DBD File Offset: 0x00060FBD
		internal void CheckDecoded(CodeAccessPermission demandedPerm, PermissionToken tokenDemandedPerm)
		{
			if (this.m_allPermissionsDecoded || this.m_permSet == null)
			{
				return;
			}
			if (tokenDemandedPerm == null)
			{
				tokenDemandedPerm = PermissionToken.GetToken(demandedPerm);
			}
			this.CheckDecoded(tokenDemandedPerm.m_index);
		}

		// Token: 0x06001CEE RID: 7406 RVA: 0x00062DE7 File Offset: 0x00060FE7
		internal void CheckDecoded(int index)
		{
			if (this.m_allPermissionsDecoded || this.m_permSet == null)
			{
				return;
			}
			this.GetPermission(index);
		}

		// Token: 0x06001CEF RID: 7407 RVA: 0x00062E04 File Offset: 0x00061004
		internal void CheckDecoded(PermissionSet demandedSet)
		{
			if (this.m_allPermissionsDecoded || this.m_permSet == null)
			{
				return;
			}
			PermissionSetEnumeratorInternal enumeratorInternal = demandedSet.GetEnumeratorInternal();
			while (enumeratorInternal.MoveNext())
			{
				this.CheckDecoded(enumeratorInternal.GetCurrentIndex());
			}
		}

		// Token: 0x06001CF0 RID: 7408 RVA: 0x00062E44 File Offset: 0x00061044
		internal static void SafeChildAdd(SecurityElement parent, ISecurityElementFactory child, bool copy)
		{
			if (child == parent)
			{
				return;
			}
			if (child.GetTag().Equals("IPermission") || child.GetTag().Equals("Permission"))
			{
				parent.AddChild(child);
				return;
			}
			if (parent.Tag.Equals(child.GetTag()))
			{
				SecurityElement securityElement = (SecurityElement)child;
				for (int i = 0; i < securityElement.InternalChildren.Count; i++)
				{
					ISecurityElementFactory child2 = (ISecurityElementFactory)securityElement.InternalChildren[i];
					parent.AddChildNoDuplicates(child2);
				}
				return;
			}
			parent.AddChild((ISecurityElementFactory)(copy ? child.Copy() : child));
		}

		// Token: 0x06001CF1 RID: 7409 RVA: 0x00062EE4 File Offset: 0x000610E4
		internal void InplaceIntersect(PermissionSet other)
		{
			Exception ex = null;
			this.m_CheckedForNonCas = false;
			if (this == other)
			{
				return;
			}
			if (other == null || other.FastIsEmpty())
			{
				this.Reset();
				return;
			}
			if (this.FastIsEmpty())
			{
				return;
			}
			int num = ((this.m_permSet == null) ? (-1) : this.m_permSet.GetMaxUsedIndex());
			int num2 = ((other.m_permSet == null) ? (-1) : other.m_permSet.GetMaxUsedIndex());
			if (this.IsUnrestricted() && num < num2)
			{
				num = num2;
				this.CheckSet();
			}
			if (other.IsUnrestricted())
			{
				other.CheckSet();
			}
			for (int i = 0; i <= num; i++)
			{
				object item = this.m_permSet.GetItem(i);
				IPermission permission = item as IPermission;
				ISecurityElementFactory securityElementFactory = item as ISecurityElementFactory;
				object item2 = other.m_permSet.GetItem(i);
				IPermission permission2 = item2 as IPermission;
				ISecurityElementFactory securityElementFactory2 = item2 as ISecurityElementFactory;
				if (item != null || item2 != null)
				{
					if (securityElementFactory != null && securityElementFactory2 != null)
					{
						if (securityElementFactory.GetTag().Equals("PermissionIntersection") || securityElementFactory.GetTag().Equals("PermissionUnrestrictedIntersection"))
						{
							PermissionSet.SafeChildAdd((SecurityElement)securityElementFactory, securityElementFactory2, true);
						}
						else
						{
							bool copy = true;
							if (this.IsUnrestricted())
							{
								SecurityElement securityElement = new SecurityElement("PermissionUnrestrictedUnion");
								securityElement.AddAttribute("class", securityElementFactory.Attribute("class"));
								PermissionSet.SafeChildAdd(securityElement, securityElementFactory, false);
								securityElementFactory = securityElement;
							}
							if (other.IsUnrestricted())
							{
								SecurityElement securityElement2 = new SecurityElement("PermissionUnrestrictedUnion");
								securityElement2.AddAttribute("class", securityElementFactory2.Attribute("class"));
								PermissionSet.SafeChildAdd(securityElement2, securityElementFactory2, true);
								securityElementFactory2 = securityElement2;
								copy = false;
							}
							SecurityElement securityElement3 = new SecurityElement("PermissionIntersection");
							securityElement3.AddAttribute("class", securityElementFactory.Attribute("class"));
							PermissionSet.SafeChildAdd(securityElement3, securityElementFactory, false);
							PermissionSet.SafeChildAdd(securityElement3, securityElementFactory2, copy);
							this.m_permSet.SetItem(i, securityElement3);
						}
					}
					else if (item == null)
					{
						if (this.IsUnrestricted())
						{
							if (securityElementFactory2 != null)
							{
								SecurityElement securityElement4 = new SecurityElement("PermissionUnrestrictedIntersection");
								securityElement4.AddAttribute("class", securityElementFactory2.Attribute("class"));
								PermissionSet.SafeChildAdd(securityElement4, securityElementFactory2, true);
								this.m_permSet.SetItem(i, securityElement4);
							}
							else
							{
								PermissionToken permissionToken = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
								if ((permissionToken.m_type & PermissionTokenType.IUnrestricted) != (PermissionTokenType)0)
								{
									this.m_permSet.SetItem(i, permission2.Copy());
								}
							}
						}
					}
					else if (item2 == null)
					{
						if (other.IsUnrestricted())
						{
							if (securityElementFactory != null)
							{
								SecurityElement securityElement5 = new SecurityElement("PermissionUnrestrictedIntersection");
								securityElement5.AddAttribute("class", securityElementFactory.Attribute("class"));
								PermissionSet.SafeChildAdd(securityElement5, securityElementFactory, false);
								this.m_permSet.SetItem(i, securityElement5);
							}
							else
							{
								PermissionToken permissionToken2 = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
								if ((permissionToken2.m_type & PermissionTokenType.IUnrestricted) == (PermissionTokenType)0)
								{
									this.m_permSet.SetItem(i, null);
								}
							}
						}
						else
						{
							this.m_permSet.SetItem(i, null);
						}
					}
					else
					{
						if (securityElementFactory != null)
						{
							permission = this.CreatePermission(securityElementFactory, i);
						}
						if (securityElementFactory2 != null)
						{
							permission2 = other.CreatePermission(securityElementFactory2, i);
						}
						try
						{
							IPermission item3;
							if (permission == null)
							{
								item3 = permission2;
							}
							else if (permission2 == null)
							{
								item3 = permission;
							}
							else
							{
								item3 = permission.Intersect(permission2);
							}
							this.m_permSet.SetItem(i, item3);
						}
						catch (Exception ex2)
						{
							if (ex == null)
							{
								ex = ex2;
							}
						}
					}
				}
			}
			this.m_Unrestricted = this.m_Unrestricted && other.m_Unrestricted;
			if (ex != null)
			{
				throw ex;
			}
		}

		// Token: 0x06001CF2 RID: 7410 RVA: 0x0006327C File Offset: 0x0006147C
		public PermissionSet Intersect(PermissionSet other)
		{
			if (other == null || other.FastIsEmpty() || this.FastIsEmpty())
			{
				return null;
			}
			int num = ((this.m_permSet == null) ? (-1) : this.m_permSet.GetMaxUsedIndex());
			int num2 = ((other.m_permSet == null) ? (-1) : other.m_permSet.GetMaxUsedIndex());
			int num3 = ((num < num2) ? num : num2);
			if (this.IsUnrestricted() && num3 < num2)
			{
				num3 = num2;
				this.CheckSet();
			}
			if (other.IsUnrestricted() && num3 < num)
			{
				num3 = num;
				other.CheckSet();
			}
			PermissionSet permissionSet = new PermissionSet(false);
			if (num3 > -1)
			{
				permissionSet.m_permSet = new TokenBasedSet();
			}
			for (int i = 0; i <= num3; i++)
			{
				object item = this.m_permSet.GetItem(i);
				IPermission permission = item as IPermission;
				ISecurityElementFactory securityElementFactory = item as ISecurityElementFactory;
				object item2 = other.m_permSet.GetItem(i);
				IPermission permission2 = item2 as IPermission;
				ISecurityElementFactory securityElementFactory2 = item2 as ISecurityElementFactory;
				if (item != null || item2 != null)
				{
					if (securityElementFactory != null && securityElementFactory2 != null)
					{
						bool copy = true;
						bool copy2 = true;
						SecurityElement securityElement = new SecurityElement("PermissionIntersection");
						securityElement.AddAttribute("class", securityElementFactory2.Attribute("class"));
						if (this.IsUnrestricted())
						{
							SecurityElement securityElement2 = new SecurityElement("PermissionUnrestrictedUnion");
							securityElement2.AddAttribute("class", securityElementFactory.Attribute("class"));
							PermissionSet.SafeChildAdd(securityElement2, securityElementFactory, true);
							copy2 = false;
							securityElementFactory = securityElement2;
						}
						if (other.IsUnrestricted())
						{
							SecurityElement securityElement3 = new SecurityElement("PermissionUnrestrictedUnion");
							securityElement3.AddAttribute("class", securityElementFactory2.Attribute("class"));
							PermissionSet.SafeChildAdd(securityElement3, securityElementFactory2, true);
							copy = false;
							securityElementFactory2 = securityElement3;
						}
						PermissionSet.SafeChildAdd(securityElement, securityElementFactory2, copy);
						PermissionSet.SafeChildAdd(securityElement, securityElementFactory, copy2);
						permissionSet.m_permSet.SetItem(i, securityElement);
					}
					else if (item == null)
					{
						if (this.m_Unrestricted)
						{
							if (securityElementFactory2 != null)
							{
								SecurityElement securityElement4 = new SecurityElement("PermissionUnrestrictedIntersection");
								securityElement4.AddAttribute("class", securityElementFactory2.Attribute("class"));
								PermissionSet.SafeChildAdd(securityElement4, securityElementFactory2, true);
								permissionSet.m_permSet.SetItem(i, securityElement4);
							}
							else if (permission2 != null)
							{
								PermissionToken permissionToken = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
								if ((permissionToken.m_type & PermissionTokenType.IUnrestricted) != (PermissionTokenType)0)
								{
									permissionSet.m_permSet.SetItem(i, permission2.Copy());
								}
							}
						}
					}
					else if (item2 == null)
					{
						if (other.m_Unrestricted)
						{
							if (securityElementFactory != null)
							{
								SecurityElement securityElement5 = new SecurityElement("PermissionUnrestrictedIntersection");
								securityElement5.AddAttribute("class", securityElementFactory.Attribute("class"));
								PermissionSet.SafeChildAdd(securityElement5, securityElementFactory, true);
								permissionSet.m_permSet.SetItem(i, securityElement5);
							}
							else if (permission != null)
							{
								PermissionToken permissionToken2 = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
								if ((permissionToken2.m_type & PermissionTokenType.IUnrestricted) != (PermissionTokenType)0)
								{
									permissionSet.m_permSet.SetItem(i, permission.Copy());
								}
							}
						}
					}
					else
					{
						if (securityElementFactory != null)
						{
							permission = this.CreatePermission(securityElementFactory, i);
						}
						if (securityElementFactory2 != null)
						{
							permission2 = other.CreatePermission(securityElementFactory2, i);
						}
						IPermission item3;
						if (permission == null)
						{
							item3 = permission2;
						}
						else if (permission2 == null)
						{
							item3 = permission;
						}
						else
						{
							item3 = permission.Intersect(permission2);
						}
						permissionSet.m_permSet.SetItem(i, item3);
					}
				}
			}
			permissionSet.m_Unrestricted = this.m_Unrestricted && other.m_Unrestricted;
			if (permissionSet.FastIsEmpty())
			{
				return null;
			}
			return permissionSet;
		}

		// Token: 0x06001CF3 RID: 7411 RVA: 0x000635F4 File Offset: 0x000617F4
		internal void InplaceUnion(PermissionSet other)
		{
			if (this == other)
			{
				return;
			}
			if (other == null || other.FastIsEmpty())
			{
				return;
			}
			this.m_CheckedForNonCas = false;
			this.m_Unrestricted = this.m_Unrestricted || other.m_Unrestricted;
			if (this.m_Unrestricted)
			{
				this.m_permSet = null;
				return;
			}
			int num = -1;
			if (other.m_permSet != null)
			{
				num = other.m_permSet.GetMaxUsedIndex();
				this.CheckSet();
			}
			Exception ex = null;
			for (int i = 0; i <= num; i++)
			{
				object item = this.m_permSet.GetItem(i);
				IPermission permission = item as IPermission;
				ISecurityElementFactory securityElementFactory = item as ISecurityElementFactory;
				object item2 = other.m_permSet.GetItem(i);
				IPermission permission2 = item2 as IPermission;
				ISecurityElementFactory securityElementFactory2 = item2 as ISecurityElementFactory;
				if (item != null || item2 != null)
				{
					if (securityElementFactory != null && securityElementFactory2 != null)
					{
						if (securityElementFactory.GetTag().Equals("PermissionUnion") || securityElementFactory.GetTag().Equals("PermissionUnrestrictedUnion"))
						{
							PermissionSet.SafeChildAdd((SecurityElement)securityElementFactory, securityElementFactory2, true);
						}
						else
						{
							SecurityElement securityElement;
							if (this.IsUnrestricted() || other.IsUnrestricted())
							{
								securityElement = new SecurityElement("PermissionUnrestrictedUnion");
							}
							else
							{
								securityElement = new SecurityElement("PermissionUnion");
							}
							securityElement.AddAttribute("class", securityElementFactory.Attribute("class"));
							PermissionSet.SafeChildAdd(securityElement, securityElementFactory, false);
							PermissionSet.SafeChildAdd(securityElement, securityElementFactory2, true);
							this.m_permSet.SetItem(i, securityElement);
						}
					}
					else if (item == null)
					{
						if (securityElementFactory2 != null)
						{
							this.m_permSet.SetItem(i, securityElementFactory2.Copy());
						}
						else if (permission2 != null)
						{
							PermissionToken permissionToken = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
							if ((permissionToken.m_type & PermissionTokenType.IUnrestricted) == (PermissionTokenType)0 || !this.m_Unrestricted)
							{
								this.m_permSet.SetItem(i, permission2.Copy());
							}
						}
					}
					else if (item2 != null)
					{
						if (securityElementFactory != null)
						{
							permission = this.CreatePermission(securityElementFactory, i);
						}
						if (securityElementFactory2 != null)
						{
							permission2 = other.CreatePermission(securityElementFactory2, i);
						}
						try
						{
							IPermission item3;
							if (permission == null)
							{
								item3 = permission2;
							}
							else if (permission2 == null)
							{
								item3 = permission;
							}
							else
							{
								item3 = permission.Union(permission2);
							}
							this.m_permSet.SetItem(i, item3);
						}
						catch (Exception ex2)
						{
							if (ex == null)
							{
								ex = ex2;
							}
						}
					}
				}
			}
			if (ex != null)
			{
				throw ex;
			}
		}

		// Token: 0x06001CF4 RID: 7412 RVA: 0x0006383C File Offset: 0x00061A3C
		public PermissionSet Union(PermissionSet other)
		{
			if (other == null || other.FastIsEmpty())
			{
				return this.Copy();
			}
			if (this.FastIsEmpty())
			{
				return other.Copy();
			}
			PermissionSet permissionSet = new PermissionSet();
			permissionSet.m_Unrestricted = this.m_Unrestricted || other.m_Unrestricted;
			if (permissionSet.m_Unrestricted)
			{
				return permissionSet;
			}
			this.CheckSet();
			other.CheckSet();
			int num = ((this.m_permSet.GetMaxUsedIndex() > other.m_permSet.GetMaxUsedIndex()) ? this.m_permSet.GetMaxUsedIndex() : other.m_permSet.GetMaxUsedIndex());
			permissionSet.m_permSet = new TokenBasedSet();
			for (int i = 0; i <= num; i++)
			{
				object item = this.m_permSet.GetItem(i);
				IPermission permission = item as IPermission;
				ISecurityElementFactory securityElementFactory = item as ISecurityElementFactory;
				object item2 = other.m_permSet.GetItem(i);
				IPermission permission2 = item2 as IPermission;
				ISecurityElementFactory securityElementFactory2 = item2 as ISecurityElementFactory;
				if (item != null || item2 != null)
				{
					if (securityElementFactory != null && securityElementFactory2 != null)
					{
						SecurityElement securityElement;
						if (this.IsUnrestricted() || other.IsUnrestricted())
						{
							securityElement = new SecurityElement("PermissionUnrestrictedUnion");
						}
						else
						{
							securityElement = new SecurityElement("PermissionUnion");
						}
						securityElement.AddAttribute("class", securityElementFactory.Attribute("class"));
						PermissionSet.SafeChildAdd(securityElement, securityElementFactory, true);
						PermissionSet.SafeChildAdd(securityElement, securityElementFactory2, true);
						permissionSet.m_permSet.SetItem(i, securityElement);
					}
					else if (item == null)
					{
						if (securityElementFactory2 != null)
						{
							permissionSet.m_permSet.SetItem(i, securityElementFactory2.Copy());
						}
						else if (permission2 != null)
						{
							PermissionToken permissionToken = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
							if ((permissionToken.m_type & PermissionTokenType.IUnrestricted) == (PermissionTokenType)0 || !permissionSet.m_Unrestricted)
							{
								permissionSet.m_permSet.SetItem(i, permission2.Copy());
							}
						}
					}
					else if (item2 == null)
					{
						if (securityElementFactory != null)
						{
							permissionSet.m_permSet.SetItem(i, securityElementFactory.Copy());
						}
						else if (permission != null)
						{
							PermissionToken permissionToken2 = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
							if ((permissionToken2.m_type & PermissionTokenType.IUnrestricted) == (PermissionTokenType)0 || !permissionSet.m_Unrestricted)
							{
								permissionSet.m_permSet.SetItem(i, permission.Copy());
							}
						}
					}
					else
					{
						if (securityElementFactory != null)
						{
							permission = this.CreatePermission(securityElementFactory, i);
						}
						if (securityElementFactory2 != null)
						{
							permission2 = other.CreatePermission(securityElementFactory2, i);
						}
						IPermission item3;
						if (permission == null)
						{
							item3 = permission2;
						}
						else if (permission2 == null)
						{
							item3 = permission;
						}
						else
						{
							item3 = permission.Union(permission2);
						}
						permissionSet.m_permSet.SetItem(i, item3);
					}
				}
			}
			return permissionSet;
		}

		// Token: 0x06001CF5 RID: 7413 RVA: 0x00063ABC File Offset: 0x00061CBC
		internal void MergeDeniedSet(PermissionSet denied)
		{
			if (denied == null || denied.FastIsEmpty() || this.FastIsEmpty())
			{
				return;
			}
			this.m_CheckedForNonCas = false;
			if (this.m_permSet == null || denied.m_permSet == null)
			{
				return;
			}
			int num = ((denied.m_permSet.GetMaxUsedIndex() > this.m_permSet.GetMaxUsedIndex()) ? this.m_permSet.GetMaxUsedIndex() : denied.m_permSet.GetMaxUsedIndex());
			for (int i = 0; i <= num; i++)
			{
				IPermission permission = denied.m_permSet.GetItem(i) as IPermission;
				if (permission != null)
				{
					IPermission permission2 = this.m_permSet.GetItem(i) as IPermission;
					if (permission2 == null && !this.m_Unrestricted)
					{
						denied.m_permSet.SetItem(i, null);
					}
					else if (permission2 != null && permission != null && permission2.IsSubsetOf(permission))
					{
						this.m_permSet.SetItem(i, null);
						denied.m_permSet.SetItem(i, null);
					}
				}
			}
		}

		// Token: 0x06001CF6 RID: 7414 RVA: 0x00063B9C File Offset: 0x00061D9C
		internal bool Contains(IPermission perm)
		{
			if (perm == null)
			{
				return true;
			}
			if (this.m_Unrestricted)
			{
				return true;
			}
			if (this.FastIsEmpty())
			{
				return false;
			}
			PermissionToken token = PermissionToken.GetToken(perm);
			if (this.m_permSet.GetItem(token.m_index) == null)
			{
				return perm.IsSubsetOf(null);
			}
			IPermission permission = this.GetPermission(token.m_index);
			if (permission != null)
			{
				return perm.IsSubsetOf(permission);
			}
			return perm.IsSubsetOf(null);
		}

		// Token: 0x06001CF7 RID: 7415 RVA: 0x00063C08 File Offset: 0x00061E08
		[ComVisible(false)]
		public override bool Equals(object obj)
		{
			PermissionSet permissionSet = obj as PermissionSet;
			if (permissionSet == null)
			{
				return false;
			}
			if (this.m_Unrestricted != permissionSet.m_Unrestricted)
			{
				return false;
			}
			this.CheckSet();
			permissionSet.CheckSet();
			this.DecodeAllPermissions();
			permissionSet.DecodeAllPermissions();
			int num = Math.Max(this.m_permSet.GetMaxUsedIndex(), permissionSet.m_permSet.GetMaxUsedIndex());
			for (int i = 0; i <= num; i++)
			{
				IPermission permission = (IPermission)this.m_permSet.GetItem(i);
				IPermission permission2 = (IPermission)permissionSet.m_permSet.GetItem(i);
				if (permission != null || permission2 != null)
				{
					if (permission == null)
					{
						if (!permission2.IsSubsetOf(null))
						{
							return false;
						}
					}
					else if (permission2 == null)
					{
						if (!permission.IsSubsetOf(null))
						{
							return false;
						}
					}
					else if (!permission.Equals(permission2))
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06001CF8 RID: 7416 RVA: 0x00063CC8 File Offset: 0x00061EC8
		[ComVisible(false)]
		public override int GetHashCode()
		{
			int num = (this.m_Unrestricted ? (-1) : 0);
			if (this.m_permSet != null)
			{
				this.DecodeAllPermissions();
				int maxUsedIndex = this.m_permSet.GetMaxUsedIndex();
				for (int i = this.m_permSet.GetStartingIndex(); i <= maxUsedIndex; i++)
				{
					IPermission permission = (IPermission)this.m_permSet.GetItem(i);
					if (permission != null)
					{
						num ^= permission.GetHashCode();
					}
				}
			}
			return num;
		}

		// Token: 0x06001CF9 RID: 7417 RVA: 0x00063D34 File Offset: 0x00061F34
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Demand()
		{
			if (this.FastIsEmpty())
			{
				return;
			}
			this.ContainsNonCodeAccessPermissions();
			if (this.m_ContainsCas)
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCallersCaller;
				CodeAccessSecurityEngine.Check(this.GetCasOnlySet(), ref stackCrawlMark);
			}
			if (this.m_ContainsNonCas)
			{
				this.DemandNonCAS();
			}
		}

		// Token: 0x06001CFA RID: 7418 RVA: 0x00063D78 File Offset: 0x00061F78
		[SecurityCritical]
		internal void DemandNonCAS()
		{
			this.ContainsNonCodeAccessPermissions();
			if (this.m_ContainsNonCas && this.m_permSet != null)
			{
				this.CheckSet();
				for (int i = this.m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); i++)
				{
					IPermission permission = this.GetPermission(i);
					if (permission != null && !(permission is CodeAccessPermission))
					{
						permission.Demand();
					}
				}
			}
		}

		// Token: 0x06001CFB RID: 7419 RVA: 0x00063DDC File Offset: 0x00061FDC
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Assert()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.Assert(this, ref stackCrawlMark);
		}

		// Token: 0x06001CFC RID: 7420 RVA: 0x00063DF4 File Offset: 0x00061FF4
		[SecuritySafeCritical]
		[Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Deny()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.Deny(this, ref stackCrawlMark);
		}

		// Token: 0x06001CFD RID: 7421 RVA: 0x00063E0C File Offset: 0x0006200C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void PermitOnly()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.PermitOnly(this, ref stackCrawlMark);
		}

		// Token: 0x06001CFE RID: 7422 RVA: 0x00063E24 File Offset: 0x00062024
		internal IPermission GetFirstPerm()
		{
			IEnumerator enumerator = this.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				return null;
			}
			return enumerator.Current as IPermission;
		}

		// Token: 0x06001CFF RID: 7423 RVA: 0x00063E4D File Offset: 0x0006204D
		public virtual PermissionSet Copy()
		{
			return new PermissionSet(this);
		}

		// Token: 0x06001D00 RID: 7424 RVA: 0x00063E58 File Offset: 0x00062058
		internal PermissionSet CopyWithNoIdentityPermissions()
		{
			PermissionSet permissionSet = new PermissionSet(this);
			permissionSet.RemovePermission(typeof(GacIdentityPermission));
			permissionSet.RemovePermission(typeof(PublisherIdentityPermission));
			permissionSet.RemovePermission(typeof(StrongNameIdentityPermission));
			permissionSet.RemovePermission(typeof(UrlIdentityPermission));
			permissionSet.RemovePermission(typeof(ZoneIdentityPermission));
			return permissionSet;
		}

		// Token: 0x06001D01 RID: 7425 RVA: 0x00063EC2 File Offset: 0x000620C2
		public IEnumerator GetEnumerator()
		{
			return this.GetEnumeratorImpl();
		}

		// Token: 0x06001D02 RID: 7426 RVA: 0x00063ECA File Offset: 0x000620CA
		protected virtual IEnumerator GetEnumeratorImpl()
		{
			return new PermissionSetEnumerator(this);
		}

		// Token: 0x06001D03 RID: 7427 RVA: 0x00063ED2 File Offset: 0x000620D2
		internal PermissionSetEnumeratorInternal GetEnumeratorInternal()
		{
			return new PermissionSetEnumeratorInternal(this);
		}

		// Token: 0x06001D04 RID: 7428 RVA: 0x00063EDA File Offset: 0x000620DA
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06001D05 RID: 7429 RVA: 0x00063EE8 File Offset: 0x000620E8
		private void NormalizePermissionSet()
		{
			PermissionSet permissionSet = new PermissionSet(false);
			permissionSet.m_Unrestricted = this.m_Unrestricted;
			if (this.m_permSet != null)
			{
				for (int i = this.m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); i++)
				{
					object item = this.m_permSet.GetItem(i);
					IPermission permission = item as IPermission;
					ISecurityElementFactory securityElementFactory = item as ISecurityElementFactory;
					if (securityElementFactory != null)
					{
						permission = this.CreatePerm(securityElementFactory);
					}
					if (permission != null)
					{
						permissionSet.SetPermission(permission);
					}
				}
			}
			this.m_permSet = permissionSet.m_permSet;
		}

		// Token: 0x06001D06 RID: 7430 RVA: 0x00063F70 File Offset: 0x00062170
		private bool DecodeXml(byte[] data, HostProtectionResource fullTrustOnlyResources, HostProtectionResource inaccessibleResources)
		{
			if (data != null && data.Length != 0)
			{
				this.FromXml(new Parser(data, Tokenizer.ByteTokenEncoding.UnicodeTokens).GetTopElement());
			}
			this.FilterHostProtectionPermissions(fullTrustOnlyResources, inaccessibleResources);
			this.DecodeAllPermissions();
			return true;
		}

		// Token: 0x06001D07 RID: 7431 RVA: 0x00063F9C File Offset: 0x0006219C
		private void DecodeAllPermissions()
		{
			if (this.m_permSet == null)
			{
				this.m_allPermissionsDecoded = true;
				return;
			}
			int maxUsedIndex = this.m_permSet.GetMaxUsedIndex();
			for (int i = 0; i <= maxUsedIndex; i++)
			{
				this.GetPermission(i);
			}
			this.m_allPermissionsDecoded = true;
		}

		// Token: 0x06001D08 RID: 7432 RVA: 0x00063FE0 File Offset: 0x000621E0
		internal void FilterHostProtectionPermissions(HostProtectionResource fullTrustOnly, HostProtectionResource inaccessible)
		{
			HostProtectionPermission.protectedResources = fullTrustOnly;
			HostProtectionPermission hostProtectionPermission = (HostProtectionPermission)this.GetPermission(HostProtectionPermission.GetTokenIndex());
			if (hostProtectionPermission == null)
			{
				return;
			}
			HostProtectionPermission hostProtectionPermission2 = (HostProtectionPermission)hostProtectionPermission.Intersect(new HostProtectionPermission(fullTrustOnly));
			if (hostProtectionPermission2 == null)
			{
				this.RemovePermission(typeof(HostProtectionPermission));
				return;
			}
			if (hostProtectionPermission2.Resources != hostProtectionPermission.Resources)
			{
				this.SetPermission(hostProtectionPermission2);
			}
		}

		// Token: 0x06001D09 RID: 7433 RVA: 0x00064047 File Offset: 0x00062247
		public virtual void FromXml(SecurityElement et)
		{
			this.FromXml(et, false, false);
		}

		// Token: 0x06001D0A RID: 7434 RVA: 0x00064054 File Offset: 0x00062254
		internal static bool IsPermissionTag(string tag, bool allowInternalOnly)
		{
			return tag.Equals("Permission") || tag.Equals("IPermission") || (allowInternalOnly && (tag.Equals("PermissionUnion") || tag.Equals("PermissionIntersection") || tag.Equals("PermissionUnrestrictedIntersection") || tag.Equals("PermissionUnrestrictedUnion")));
		}

		// Token: 0x06001D0B RID: 7435 RVA: 0x000640B8 File Offset: 0x000622B8
		internal virtual void FromXml(SecurityElement et, bool allowInternalOnly, bool ignoreTypeLoadFailures)
		{
			if (et == null)
			{
				throw new ArgumentNullException("et");
			}
			if (!et.Tag.Equals("PermissionSet"))
			{
				throw new ArgumentException(string.Format(null, Environment.GetResourceString("Argument_InvalidXMLElement"), "PermissionSet", base.GetType().FullName));
			}
			this.Reset();
			this.m_ignoreTypeLoadFailures = ignoreTypeLoadFailures;
			this.m_allPermissionsDecoded = false;
			this.m_Unrestricted = XMLUtil.IsUnrestricted(et);
			if (et.InternalChildren != null)
			{
				int count = et.InternalChildren.Count;
				for (int i = 0; i < count; i++)
				{
					SecurityElement securityElement = (SecurityElement)et.Children[i];
					if (PermissionSet.IsPermissionTag(securityElement.Tag, allowInternalOnly))
					{
						string text = securityElement.Attribute("class");
						PermissionToken permissionToken;
						object obj;
						if (text != null)
						{
							permissionToken = PermissionToken.GetToken(text);
							if (permissionToken == null)
							{
								obj = this.CreatePerm(securityElement);
								if (obj != null)
								{
									permissionToken = PermissionToken.GetToken((IPermission)obj);
								}
							}
							else
							{
								obj = securityElement;
							}
						}
						else
						{
							IPermission permission = this.CreatePerm(securityElement);
							if (permission == null)
							{
								permissionToken = null;
								obj = null;
							}
							else
							{
								permissionToken = PermissionToken.GetToken(permission);
								obj = permission;
							}
						}
						if (permissionToken != null && obj != null)
						{
							if (this.m_permSet == null)
							{
								this.m_permSet = new TokenBasedSet();
							}
							if (this.m_permSet.GetItem(permissionToken.m_index) != null)
							{
								IPermission target;
								if (this.m_permSet.GetItem(permissionToken.m_index) is IPermission)
								{
									target = (IPermission)this.m_permSet.GetItem(permissionToken.m_index);
								}
								else
								{
									target = this.CreatePerm((SecurityElement)this.m_permSet.GetItem(permissionToken.m_index));
								}
								if (obj is IPermission)
								{
									obj = ((IPermission)obj).Union(target);
								}
								else
								{
									obj = this.CreatePerm((SecurityElement)obj).Union(target);
								}
							}
							if (this.m_Unrestricted && obj is IPermission)
							{
								obj = null;
							}
							this.m_permSet.SetItem(permissionToken.m_index, obj);
						}
					}
				}
			}
		}

		// Token: 0x06001D0C RID: 7436 RVA: 0x000642BC File Offset: 0x000624BC
		internal virtual void FromXml(SecurityDocument doc, int position, bool allowInternalOnly)
		{
			if (doc == null)
			{
				throw new ArgumentNullException("doc");
			}
			if (!doc.GetTagForElement(position).Equals("PermissionSet"))
			{
				throw new ArgumentException(string.Format(null, Environment.GetResourceString("Argument_InvalidXMLElement"), "PermissionSet", base.GetType().FullName));
			}
			this.Reset();
			this.m_allPermissionsDecoded = false;
			Exception ex = null;
			string attributeForElement = doc.GetAttributeForElement(position, "Unrestricted");
			if (attributeForElement != null)
			{
				this.m_Unrestricted = attributeForElement.Equals("True") || attributeForElement.Equals("true") || attributeForElement.Equals("TRUE");
			}
			else
			{
				this.m_Unrestricted = false;
			}
			ArrayList childrenPositionForElement = doc.GetChildrenPositionForElement(position);
			int count = childrenPositionForElement.Count;
			for (int i = 0; i < count; i++)
			{
				int position2 = (int)childrenPositionForElement[i];
				if (PermissionSet.IsPermissionTag(doc.GetTagForElement(position2), allowInternalOnly))
				{
					try
					{
						string attributeForElement2 = doc.GetAttributeForElement(position2, "class");
						PermissionToken permissionToken;
						object obj;
						if (attributeForElement2 != null)
						{
							permissionToken = PermissionToken.GetToken(attributeForElement2);
							if (permissionToken == null)
							{
								obj = this.CreatePerm(doc.GetElement(position2, true));
								if (obj != null)
								{
									permissionToken = PermissionToken.GetToken((IPermission)obj);
								}
							}
							else
							{
								obj = ((ISecurityElementFactory)new SecurityDocumentElement(doc, position2)).CreateSecurityElement();
							}
						}
						else
						{
							IPermission permission = this.CreatePerm(doc.GetElement(position2, true));
							if (permission == null)
							{
								permissionToken = null;
								obj = null;
							}
							else
							{
								permissionToken = PermissionToken.GetToken(permission);
								obj = permission;
							}
						}
						if (permissionToken != null && obj != null)
						{
							if (this.m_permSet == null)
							{
								this.m_permSet = new TokenBasedSet();
							}
							IPermission permission2 = null;
							if (this.m_permSet.GetItem(permissionToken.m_index) != null)
							{
								if (this.m_permSet.GetItem(permissionToken.m_index) is IPermission)
								{
									permission2 = (IPermission)this.m_permSet.GetItem(permissionToken.m_index);
								}
								else
								{
									permission2 = this.CreatePerm(this.m_permSet.GetItem(permissionToken.m_index));
								}
							}
							if (permission2 != null)
							{
								if (obj is IPermission)
								{
									obj = permission2.Union((IPermission)obj);
								}
								else
								{
									obj = permission2.Union(this.CreatePerm(obj));
								}
							}
							if (this.m_Unrestricted && obj is IPermission)
							{
								obj = null;
							}
							this.m_permSet.SetItem(permissionToken.m_index, obj);
						}
					}
					catch (Exception ex2)
					{
						if (ex == null)
						{
							ex = ex2;
						}
					}
				}
			}
			if (ex != null)
			{
				throw ex;
			}
		}

		// Token: 0x06001D0D RID: 7437 RVA: 0x00064538 File Offset: 0x00062738
		private IPermission CreatePerm(object obj)
		{
			return PermissionSet.CreatePerm(obj, this.m_ignoreTypeLoadFailures);
		}

		// Token: 0x06001D0E RID: 7438 RVA: 0x00064548 File Offset: 0x00062748
		internal static IPermission CreatePerm(object obj, bool ignoreTypeLoadFailures)
		{
			SecurityElement securityElement = obj as SecurityElement;
			ISecurityElementFactory securityElementFactory = obj as ISecurityElementFactory;
			if (securityElement == null && securityElementFactory != null)
			{
				securityElement = securityElementFactory.CreateSecurityElement();
			}
			IPermission permission = null;
			string tag = securityElement.Tag;
			if (!(tag == "PermissionUnion"))
			{
				if (!(tag == "PermissionIntersection"))
				{
					if (!(tag == "PermissionUnrestrictedUnion"))
					{
						if (!(tag == "PermissionUnrestrictedIntersection"))
						{
							if (tag == "IPermission" || tag == "Permission")
							{
								permission = securityElement.ToPermission(ignoreTypeLoadFailures);
							}
						}
						else
						{
							foreach (object obj2 in securityElement.Children)
							{
								IPermission permission2 = PermissionSet.CreatePerm((SecurityElement)obj2, ignoreTypeLoadFailures);
								if (permission2 == null)
								{
									return null;
								}
								PermissionToken token = PermissionToken.GetToken(permission2);
								if ((token.m_type & PermissionTokenType.IUnrestricted) != (PermissionTokenType)0)
								{
									if (permission != null)
									{
										permission = permission2.Intersect(permission);
									}
									else
									{
										permission = permission2;
									}
								}
								else
								{
									permission = null;
								}
								if (permission == null)
								{
									return null;
								}
							}
						}
					}
					else
					{
						IEnumerator enumerator = securityElement.Children.GetEnumerator();
						bool flag = true;
						while (enumerator.MoveNext())
						{
							object obj3 = enumerator.Current;
							IPermission permission3 = PermissionSet.CreatePerm((SecurityElement)obj3, ignoreTypeLoadFailures);
							if (permission3 != null)
							{
								PermissionToken token2 = PermissionToken.GetToken(permission3);
								if ((token2.m_type & PermissionTokenType.IUnrestricted) != (PermissionTokenType)0)
								{
									permission = XMLUtil.CreatePermission(PermissionSet.GetPermissionElement((SecurityElement)enumerator.Current), PermissionState.Unrestricted, ignoreTypeLoadFailures);
									break;
								}
								if (flag)
								{
									permission = permission3;
								}
								else
								{
									permission = permission3.Union(permission);
								}
								flag = false;
							}
						}
					}
				}
				else
				{
					foreach (object obj4 in securityElement.Children)
					{
						IPermission permission4 = PermissionSet.CreatePerm((SecurityElement)obj4, ignoreTypeLoadFailures);
						if (permission != null)
						{
							permission = permission.Intersect(permission4);
						}
						else
						{
							permission = permission4;
						}
						if (permission == null)
						{
							return null;
						}
					}
				}
			}
			else
			{
				foreach (object obj5 in securityElement.Children)
				{
					IPermission permission5 = PermissionSet.CreatePerm((SecurityElement)obj5, ignoreTypeLoadFailures);
					if (permission != null)
					{
						permission = permission.Union(permission5);
					}
					else
					{
						permission = permission5;
					}
				}
			}
			return permission;
		}

		// Token: 0x06001D0F RID: 7439 RVA: 0x0006474C File Offset: 0x0006294C
		internal IPermission CreatePermission(object obj, int index)
		{
			IPermission permission = this.CreatePerm(obj);
			if (permission == null)
			{
				return null;
			}
			if (this.m_Unrestricted)
			{
				permission = null;
			}
			this.CheckSet();
			this.m_permSet.SetItem(index, permission);
			if (permission != null)
			{
				PermissionToken token = PermissionToken.GetToken(permission);
				if (token != null && token.m_index != index)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_UnableToGeneratePermissionSet"));
				}
			}
			return permission;
		}

		// Token: 0x06001D10 RID: 7440 RVA: 0x000647AC File Offset: 0x000629AC
		private static SecurityElement GetPermissionElement(SecurityElement el)
		{
			string tag = el.Tag;
			if (tag == "IPermission" || tag == "Permission")
			{
				return el;
			}
			IEnumerator enumerator = el.Children.GetEnumerator();
			if (enumerator.MoveNext())
			{
				return PermissionSet.GetPermissionElement((SecurityElement)enumerator.Current);
			}
			return null;
		}

		// Token: 0x06001D11 RID: 7441 RVA: 0x00064804 File Offset: 0x00062A04
		internal static SecurityElement CreateEmptyPermissionSetXml()
		{
			SecurityElement securityElement = new SecurityElement("PermissionSet");
			securityElement.AddAttribute("class", "System.Security.PermissionSet");
			securityElement.AddAttribute("version", "1");
			return securityElement;
		}

		// Token: 0x06001D12 RID: 7442 RVA: 0x00064840 File Offset: 0x00062A40
		internal SecurityElement ToXml(string permName)
		{
			SecurityElement securityElement = new SecurityElement("PermissionSet");
			securityElement.AddAttribute("class", permName);
			securityElement.AddAttribute("version", "1");
			PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(this);
			if (this.m_Unrestricted)
			{
				securityElement.AddAttribute("Unrestricted", "true");
			}
			while (permissionSetEnumeratorInternal.MoveNext())
			{
				object obj = permissionSetEnumeratorInternal.Current;
				IPermission permission = (IPermission)obj;
				if (!this.m_Unrestricted)
				{
					securityElement.AddChild(permission.ToXml());
				}
			}
			return securityElement;
		}

		// Token: 0x06001D13 RID: 7443 RVA: 0x000648C4 File Offset: 0x00062AC4
		internal SecurityElement InternalToXml()
		{
			SecurityElement securityElement = new SecurityElement("PermissionSet");
			securityElement.AddAttribute("class", base.GetType().FullName);
			securityElement.AddAttribute("version", "1");
			if (this.m_Unrestricted)
			{
				securityElement.AddAttribute("Unrestricted", "true");
			}
			if (this.m_permSet != null)
			{
				int maxUsedIndex = this.m_permSet.GetMaxUsedIndex();
				for (int i = this.m_permSet.GetStartingIndex(); i <= maxUsedIndex; i++)
				{
					object item = this.m_permSet.GetItem(i);
					if (item != null)
					{
						if (item is IPermission)
						{
							if (!this.m_Unrestricted)
							{
								securityElement.AddChild(((IPermission)item).ToXml());
							}
						}
						else
						{
							securityElement.AddChild((SecurityElement)item);
						}
					}
				}
			}
			return securityElement;
		}

		// Token: 0x06001D14 RID: 7444 RVA: 0x00064984 File Offset: 0x00062B84
		public virtual SecurityElement ToXml()
		{
			return this.ToXml("System.Security.PermissionSet");
		}

		// Token: 0x06001D15 RID: 7445 RVA: 0x00064994 File Offset: 0x00062B94
		internal byte[] EncodeXml()
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.Unicode);
			binaryWriter.Write(this.ToXml().ToString());
			binaryWriter.Flush();
			memoryStream.Position = 2L;
			int num = (int)memoryStream.Length - 2;
			byte[] array = new byte[num];
			memoryStream.Read(array, 0, array.Length);
			return array;
		}

		// Token: 0x06001D16 RID: 7446 RVA: 0x000649F0 File Offset: 0x00062BF0
		[Obsolete("This method is obsolete and shoud no longer be used.")]
		public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001D17 RID: 7447 RVA: 0x000649F8 File Offset: 0x00062BF8
		public bool ContainsNonCodeAccessPermissions()
		{
			if (this.m_CheckedForNonCas)
			{
				return this.m_ContainsNonCas;
			}
			lock (this)
			{
				if (this.m_CheckedForNonCas)
				{
					return this.m_ContainsNonCas;
				}
				this.m_ContainsCas = false;
				this.m_ContainsNonCas = false;
				if (this.IsUnrestricted())
				{
					this.m_ContainsCas = true;
				}
				if (this.m_permSet != null)
				{
					PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(this);
					while (permissionSetEnumeratorInternal.MoveNext() && (!this.m_ContainsCas || !this.m_ContainsNonCas))
					{
						IPermission permission = permissionSetEnumeratorInternal.Current as IPermission;
						if (permission != null)
						{
							if (permission is CodeAccessPermission)
							{
								this.m_ContainsCas = true;
							}
							else
							{
								this.m_ContainsNonCas = true;
							}
						}
					}
				}
				this.m_CheckedForNonCas = true;
			}
			return this.m_ContainsNonCas;
		}

		// Token: 0x06001D18 RID: 7448 RVA: 0x00064AD4 File Offset: 0x00062CD4
		private PermissionSet GetCasOnlySet()
		{
			if (!this.m_ContainsNonCas)
			{
				return this;
			}
			if (this.IsUnrestricted())
			{
				return this;
			}
			PermissionSet permissionSet = new PermissionSet(false);
			PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(this);
			while (permissionSetEnumeratorInternal.MoveNext())
			{
				object obj = permissionSetEnumeratorInternal.Current;
				IPermission permission = (IPermission)obj;
				if (permission is CodeAccessPermission)
				{
					permissionSet.AddPermission(permission);
				}
			}
			permissionSet.m_CheckedForNonCas = true;
			permissionSet.m_ContainsCas = !permissionSet.IsEmpty();
			permissionSet.m_ContainsNonCas = false;
			return permissionSet;
		}

		// Token: 0x06001D19 RID: 7449 RVA: 0x00064B4C File Offset: 0x00062D4C
		[SecurityCritical]
		private static void SetupSecurity()
		{
			PolicyLevel policyLevel = PolicyLevel.CreateAppDomainLevel();
			CodeGroup codeGroup = new UnionCodeGroup(new AllMembershipCondition(), policyLevel.GetNamedPermissionSet("Execution"));
			StrongNamePublicKeyBlob blob = new StrongNamePublicKeyBlob("002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293");
			CodeGroup group = new UnionCodeGroup(new StrongNameMembershipCondition(blob, null, null), policyLevel.GetNamedPermissionSet("FullTrust"));
			StrongNamePublicKeyBlob blob2 = new StrongNamePublicKeyBlob("00000000000000000400000000000000");
			CodeGroup group2 = new UnionCodeGroup(new StrongNameMembershipCondition(blob2, null, null), policyLevel.GetNamedPermissionSet("FullTrust"));
			CodeGroup group3 = new UnionCodeGroup(new GacMembershipCondition(), policyLevel.GetNamedPermissionSet("FullTrust"));
			codeGroup.AddChild(group);
			codeGroup.AddChild(group2);
			codeGroup.AddChild(group3);
			policyLevel.RootCodeGroup = codeGroup;
			try
			{
				AppDomain.CurrentDomain.SetAppDomainPolicy(policyLevel);
			}
			catch (PolicyException)
			{
			}
		}

		// Token: 0x06001D1A RID: 7450 RVA: 0x00064C18 File Offset: 0x00062E18
		private static void MergePermission(IPermission perm, bool separateCasFromNonCas, ref PermissionSet casPset, ref PermissionSet nonCasPset)
		{
			if (perm == null)
			{
				return;
			}
			if (!separateCasFromNonCas || perm is CodeAccessPermission)
			{
				if (casPset == null)
				{
					casPset = new PermissionSet(false);
				}
				IPermission permission = casPset.GetPermission(perm);
				IPermission target = casPset.AddPermission(perm);
				if (permission != null && !permission.IsSubsetOf(target))
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_DeclarativeUnion"));
				}
			}
			else
			{
				if (nonCasPset == null)
				{
					nonCasPset = new PermissionSet(false);
				}
				IPermission permission2 = nonCasPset.GetPermission(perm);
				IPermission target2 = nonCasPset.AddPermission(perm);
				if (permission2 != null && !permission2.IsSubsetOf(target2))
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_DeclarativeUnion"));
				}
			}
		}

		// Token: 0x06001D1B RID: 7451 RVA: 0x00064CA8 File Offset: 0x00062EA8
		private static byte[] CreateSerialized(object[] attrs, bool serialize, ref byte[] nonCasBlob, out PermissionSet casPset, HostProtectionResource fullTrustOnlyResources, bool allowEmptyPermissionSets)
		{
			casPset = null;
			PermissionSet permissionSet = null;
			for (int i = 0; i < attrs.Length; i++)
			{
				if (attrs[i] is PermissionSetAttribute)
				{
					PermissionSet permissionSet2 = ((PermissionSetAttribute)attrs[i]).CreatePermissionSet();
					if (permissionSet2 == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_UnableToGeneratePermissionSet"));
					}
					PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(permissionSet2);
					while (permissionSetEnumeratorInternal.MoveNext())
					{
						object obj = permissionSetEnumeratorInternal.Current;
						IPermission perm = (IPermission)obj;
						PermissionSet.MergePermission(perm, serialize, ref casPset, ref permissionSet);
					}
					if (casPset == null)
					{
						casPset = new PermissionSet(false);
					}
					if (permissionSet2.IsUnrestricted())
					{
						casPset.SetUnrestricted(true);
					}
				}
				else
				{
					IPermission perm2 = ((SecurityAttribute)attrs[i]).CreatePermission();
					PermissionSet.MergePermission(perm2, serialize, ref casPset, ref permissionSet);
				}
			}
			if (casPset != null)
			{
				casPset.FilterHostProtectionPermissions(fullTrustOnlyResources, HostProtectionResource.None);
				casPset.ContainsNonCodeAccessPermissions();
				if (allowEmptyPermissionSets && casPset.IsEmpty())
				{
					casPset = null;
				}
			}
			if (permissionSet != null)
			{
				permissionSet.FilterHostProtectionPermissions(fullTrustOnlyResources, HostProtectionResource.None);
				permissionSet.ContainsNonCodeAccessPermissions();
				if (allowEmptyPermissionSets && permissionSet.IsEmpty())
				{
					permissionSet = null;
				}
			}
			byte[] result = null;
			nonCasBlob = null;
			if (serialize)
			{
				if (casPset != null)
				{
					result = casPset.EncodeXml();
				}
				if (permissionSet != null)
				{
					nonCasBlob = permissionSet.EncodeXml();
				}
			}
			return result;
		}

		// Token: 0x06001D1C RID: 7452 RVA: 0x00064DC4 File Offset: 0x00062FC4
		void IDeserializationCallback.OnDeserialization(object sender)
		{
			this.NormalizePermissionSet();
			this.m_CheckedForNonCas = false;
		}

		// Token: 0x06001D1D RID: 7453 RVA: 0x00064DD4 File Offset: 0x00062FD4
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void RevertAssert()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.RevertAssert(ref stackCrawlMark);
		}

		// Token: 0x06001D1E RID: 7454 RVA: 0x00064DEC File Offset: 0x00062FEC
		internal static PermissionSet RemoveRefusedPermissionSet(PermissionSet assertSet, PermissionSet refusedSet, out bool bFailedToCompress)
		{
			PermissionSet permissionSet = null;
			bFailedToCompress = false;
			if (assertSet == null)
			{
				return null;
			}
			if (refusedSet != null)
			{
				if (refusedSet.IsUnrestricted())
				{
					return null;
				}
				PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(refusedSet);
				while (permissionSetEnumeratorInternal.MoveNext())
				{
					object obj = permissionSetEnumeratorInternal.Current;
					CodeAccessPermission codeAccessPermission = (CodeAccessPermission)obj;
					int currentIndex = permissionSetEnumeratorInternal.GetCurrentIndex();
					if (codeAccessPermission != null)
					{
						CodeAccessPermission codeAccessPermission2 = (CodeAccessPermission)assertSet.GetPermission(currentIndex);
						try
						{
							if (codeAccessPermission.Intersect(codeAccessPermission2) != null)
							{
								if (!codeAccessPermission.Equals(codeAccessPermission2))
								{
									bFailedToCompress = true;
									return assertSet;
								}
								if (permissionSet == null)
								{
									permissionSet = assertSet.Copy();
								}
								permissionSet.RemovePermission(currentIndex);
							}
						}
						catch (ArgumentException)
						{
							if (permissionSet == null)
							{
								permissionSet = assertSet.Copy();
							}
							permissionSet.RemovePermission(currentIndex);
						}
						continue;
					}
				}
			}
			if (permissionSet != null)
			{
				return permissionSet;
			}
			return assertSet;
		}

		// Token: 0x06001D1F RID: 7455 RVA: 0x00064EAC File Offset: 0x000630AC
		internal static void RemoveAssertedPermissionSet(PermissionSet demandSet, PermissionSet assertSet, out PermissionSet alteredDemandSet)
		{
			alteredDemandSet = null;
			PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(demandSet);
			while (permissionSetEnumeratorInternal.MoveNext())
			{
				object obj = permissionSetEnumeratorInternal.Current;
				CodeAccessPermission codeAccessPermission = (CodeAccessPermission)obj;
				int currentIndex = permissionSetEnumeratorInternal.GetCurrentIndex();
				if (codeAccessPermission != null)
				{
					CodeAccessPermission asserted = (CodeAccessPermission)assertSet.GetPermission(currentIndex);
					try
					{
						if (codeAccessPermission.CheckAssert(asserted))
						{
							if (alteredDemandSet == null)
							{
								alteredDemandSet = demandSet.Copy();
							}
							alteredDemandSet.RemovePermission(currentIndex);
						}
					}
					catch (ArgumentException)
					{
					}
				}
			}
		}

		// Token: 0x06001D20 RID: 7456 RVA: 0x00064F28 File Offset: 0x00063128
		internal static bool IsIntersectingAssertedPermissions(PermissionSet assertSet1, PermissionSet assertSet2)
		{
			bool result = false;
			if (assertSet1 != null && assertSet2 != null)
			{
				PermissionSetEnumeratorInternal permissionSetEnumeratorInternal = new PermissionSetEnumeratorInternal(assertSet2);
				while (permissionSetEnumeratorInternal.MoveNext())
				{
					object obj = permissionSetEnumeratorInternal.Current;
					CodeAccessPermission codeAccessPermission = (CodeAccessPermission)obj;
					int currentIndex = permissionSetEnumeratorInternal.GetCurrentIndex();
					if (codeAccessPermission != null)
					{
						CodeAccessPermission codeAccessPermission2 = (CodeAccessPermission)assertSet1.GetPermission(currentIndex);
						try
						{
							if (codeAccessPermission2 != null && !codeAccessPermission2.Equals(codeAccessPermission))
							{
								result = true;
							}
						}
						catch (ArgumentException)
						{
							result = true;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x17000342 RID: 834
		// (set) Token: 0x06001D21 RID: 7457 RVA: 0x00064FA0 File Offset: 0x000631A0
		internal bool IgnoreTypeLoadFailures
		{
			set
			{
				this.m_ignoreTypeLoadFailures = value;
			}
		}

		// Token: 0x04000A11 RID: 2577
		private bool m_Unrestricted;

		// Token: 0x04000A12 RID: 2578
		[OptionalField(VersionAdded = 2)]
		private bool m_allPermissionsDecoded;

		// Token: 0x04000A13 RID: 2579
		[OptionalField(VersionAdded = 2)]
		internal TokenBasedSet m_permSet;

		// Token: 0x04000A14 RID: 2580
		[OptionalField(VersionAdded = 2)]
		private bool m_ignoreTypeLoadFailures;

		// Token: 0x04000A15 RID: 2581
		[OptionalField(VersionAdded = 2)]
		private string m_serializedPermissionSet;

		// Token: 0x04000A16 RID: 2582
		[NonSerialized]
		private bool m_CheckedForNonCas;

		// Token: 0x04000A17 RID: 2583
		[NonSerialized]
		private bool m_ContainsCas;

		// Token: 0x04000A18 RID: 2584
		[NonSerialized]
		private bool m_ContainsNonCas;

		// Token: 0x04000A19 RID: 2585
		[NonSerialized]
		private TokenBasedSet m_permSetSaved;

		// Token: 0x04000A1A RID: 2586
		private bool readableonly;

		// Token: 0x04000A1B RID: 2587
		private TokenBasedSet m_unrestrictedPermSet;

		// Token: 0x04000A1C RID: 2588
		private TokenBasedSet m_normalPermSet;

		// Token: 0x04000A1D RID: 2589
		[OptionalField(VersionAdded = 2)]
		private bool m_canUnrestrictedOverride;

		// Token: 0x04000A1E RID: 2590
		internal static readonly PermissionSet s_fullTrust = new PermissionSet(true);

		// Token: 0x04000A1F RID: 2591
		private const string s_str_PermissionSet = "PermissionSet";

		// Token: 0x04000A20 RID: 2592
		private const string s_str_Permission = "Permission";

		// Token: 0x04000A21 RID: 2593
		private const string s_str_IPermission = "IPermission";

		// Token: 0x04000A22 RID: 2594
		private const string s_str_Unrestricted = "Unrestricted";

		// Token: 0x04000A23 RID: 2595
		private const string s_str_PermissionUnion = "PermissionUnion";

		// Token: 0x04000A24 RID: 2596
		private const string s_str_PermissionIntersection = "PermissionIntersection";

		// Token: 0x04000A25 RID: 2597
		private const string s_str_PermissionUnrestrictedUnion = "PermissionUnrestrictedUnion";

		// Token: 0x04000A26 RID: 2598
		private const string s_str_PermissionUnrestrictedIntersection = "PermissionUnrestrictedIntersection";

		// Token: 0x02000B2C RID: 2860
		internal enum IsSubsetOfType
		{
			// Token: 0x0400333C RID: 13116
			Normal,
			// Token: 0x0400333D RID: 13117
			CheckDemand,
			// Token: 0x0400333E RID: 13118
			CheckPermitOnly,
			// Token: 0x0400333F RID: 13119
			CheckAssertion
		}
	}
}
