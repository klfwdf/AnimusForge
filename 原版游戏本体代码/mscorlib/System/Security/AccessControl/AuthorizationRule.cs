using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000231 RID: 561
	public abstract class AuthorizationRule
	{
		// Token: 0x06002037 RID: 8247 RVA: 0x0007128C File Offset: 0x0006F48C
		protected internal AuthorizationRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			if (accessMask == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ArgumentZero"), "accessMask");
			}
			if ((inheritanceFlags < InheritanceFlags.None) || inheritanceFlags > (InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit))
			{
				throw new ArgumentOutOfRangeException("inheritanceFlags", Environment.GetResourceString("Argument_InvalidEnumValue", new object[] { inheritanceFlags, "InheritanceFlags" }));
			}
			if ((propagationFlags < PropagationFlags.None) || propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly))
			{
				throw new ArgumentOutOfRangeException("propagationFlags", Environment.GetResourceString("Argument_InvalidEnumValue", new object[] { inheritanceFlags, "PropagationFlags" }));
			}
			if (!identity.IsValidTargetType(typeof(SecurityIdentifier)))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeIdentityReferenceType"), "identity");
			}
			this._identity = identity;
			this._accessMask = accessMask;
			this._isInherited = isInherited;
			this._inheritanceFlags = inheritanceFlags;
			if (inheritanceFlags != InheritanceFlags.None)
			{
				this._propagationFlags = propagationFlags;
				return;
			}
			this._propagationFlags = PropagationFlags.None;
		}

		// Token: 0x170003BD RID: 957
		// (get) Token: 0x06002038 RID: 8248 RVA: 0x00071391 File Offset: 0x0006F591
		public IdentityReference IdentityReference
		{
			get
			{
				return this._identity;
			}
		}

		// Token: 0x170003BE RID: 958
		// (get) Token: 0x06002039 RID: 8249 RVA: 0x00071399 File Offset: 0x0006F599
		protected internal int AccessMask
		{
			get
			{
				return this._accessMask;
			}
		}

		// Token: 0x170003BF RID: 959
		// (get) Token: 0x0600203A RID: 8250 RVA: 0x000713A1 File Offset: 0x0006F5A1
		public bool IsInherited
		{
			get
			{
				return this._isInherited;
			}
		}

		// Token: 0x170003C0 RID: 960
		// (get) Token: 0x0600203B RID: 8251 RVA: 0x000713A9 File Offset: 0x0006F5A9
		public InheritanceFlags InheritanceFlags
		{
			get
			{
				return this._inheritanceFlags;
			}
		}

		// Token: 0x170003C1 RID: 961
		// (get) Token: 0x0600203C RID: 8252 RVA: 0x000713B1 File Offset: 0x0006F5B1
		public PropagationFlags PropagationFlags
		{
			get
			{
				return this._propagationFlags;
			}
		}

		// Token: 0x04000BA3 RID: 2979
		private readonly IdentityReference _identity;

		// Token: 0x04000BA4 RID: 2980
		private readonly int _accessMask;

		// Token: 0x04000BA5 RID: 2981
		private readonly bool _isInherited;

		// Token: 0x04000BA6 RID: 2982
		private readonly InheritanceFlags _inheritanceFlags;

		// Token: 0x04000BA7 RID: 2983
		private readonly PropagationFlags _propagationFlags;
	}
}
