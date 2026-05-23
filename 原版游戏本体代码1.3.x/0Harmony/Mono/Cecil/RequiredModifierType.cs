using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000276 RID: 630
	internal sealed class RequiredModifierType : TypeSpecification, IModifierType
	{
		// Token: 0x170003AC RID: 940
		// (get) Token: 0x06000EF9 RID: 3833 RVA: 0x0002FCF2 File Offset: 0x0002DEF2
		// (set) Token: 0x06000EFA RID: 3834 RVA: 0x0002FCFA File Offset: 0x0002DEFA
		public TypeReference ModifierType
		{
			get
			{
				return this.modifier_type;
			}
			set
			{
				this.modifier_type = value;
			}
		}

		// Token: 0x170003AD RID: 941
		// (get) Token: 0x06000EFB RID: 3835 RVA: 0x0002FD03 File Offset: 0x0002DF03
		public override string Name
		{
			get
			{
				return base.Name + this.Suffix;
			}
		}

		// Token: 0x170003AE RID: 942
		// (get) Token: 0x06000EFC RID: 3836 RVA: 0x0002FD16 File Offset: 0x0002DF16
		public override string FullName
		{
			get
			{
				return base.FullName + this.Suffix;
			}
		}

		// Token: 0x170003AF RID: 943
		// (get) Token: 0x06000EFD RID: 3837 RVA: 0x0002FD29 File Offset: 0x0002DF29
		private string Suffix
		{
			get
			{
				string str = " modreq(";
				TypeReference typeReference = this.modifier_type;
				return str + ((typeReference != null) ? typeReference.ToString() : null) + ")";
			}
		}

		// Token: 0x170003B0 RID: 944
		// (get) Token: 0x06000EFE RID: 3838 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x06000EFF RID: 3839 RVA: 0x0001C627 File Offset: 0x0001A827
		public override bool IsValueType
		{
			get
			{
				return false;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170003B1 RID: 945
		// (get) Token: 0x06000F00 RID: 3840 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsRequiredModifier
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170003B2 RID: 946
		// (get) Token: 0x06000F01 RID: 3841 RVA: 0x0002FD4C File Offset: 0x0002DF4C
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.modifier_type.ContainsGenericParameter || base.ContainsGenericParameter;
			}
		}

		// Token: 0x06000F02 RID: 3842 RVA: 0x0002FD64 File Offset: 0x0002DF64
		public RequiredModifierType(TypeReference modifierType, TypeReference type)
			: base(type)
		{
			if (modifierType == null)
			{
				throw new ArgumentNullException(Mixin.Argument.modifierType.ToString());
			}
			Mixin.CheckType(type);
			this.modifier_type = modifierType;
			this.etype = Mono.Cecil.Metadata.ElementType.CModReqD;
		}

		// Token: 0x04000496 RID: 1174
		private TypeReference modifier_type;
	}
}
