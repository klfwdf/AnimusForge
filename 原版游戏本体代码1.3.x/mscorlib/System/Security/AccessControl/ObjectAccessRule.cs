using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000233 RID: 563
	public abstract class ObjectAccessRule : AccessRule
	{
		// Token: 0x0600203F RID: 8255 RVA: 0x00071474 File Offset: 0x0006F674
		protected ObjectAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AccessControlType type)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
		{
			if (!objectType.Equals(Guid.Empty) && (accessMask & ObjectAce.AccessMaskWithObjectType) != 0)
			{
				this._objectType = objectType;
				this._objectFlags |= ObjectAceFlags.ObjectAceTypePresent;
			}
			else
			{
				this._objectType = Guid.Empty;
			}
			if (!inheritedObjectType.Equals(Guid.Empty) && (inheritanceFlags & InheritanceFlags.ContainerInherit) != InheritanceFlags.None)
			{
				this._inheritedObjectType = inheritedObjectType;
				this._objectFlags |= ObjectAceFlags.InheritedObjectAceTypePresent;
				return;
			}
			this._inheritedObjectType = Guid.Empty;
		}

		// Token: 0x170003C3 RID: 963
		// (get) Token: 0x06002040 RID: 8256 RVA: 0x00071500 File Offset: 0x0006F700
		public Guid ObjectType
		{
			get
			{
				return this._objectType;
			}
		}

		// Token: 0x170003C4 RID: 964
		// (get) Token: 0x06002041 RID: 8257 RVA: 0x00071508 File Offset: 0x0006F708
		public Guid InheritedObjectType
		{
			get
			{
				return this._inheritedObjectType;
			}
		}

		// Token: 0x170003C5 RID: 965
		// (get) Token: 0x06002042 RID: 8258 RVA: 0x00071510 File Offset: 0x0006F710
		public ObjectAceFlags ObjectFlags
		{
			get
			{
				return this._objectFlags;
			}
		}

		// Token: 0x04000BA9 RID: 2985
		private readonly Guid _objectType;

		// Token: 0x04000BAA RID: 2986
		private readonly Guid _inheritedObjectType;

		// Token: 0x04000BAB RID: 2987
		private readonly ObjectAceFlags _objectFlags;
	}
}
