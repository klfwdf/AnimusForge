using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000275 RID: 629
	internal sealed class OptionalModifierType : TypeSpecification, IModifierType
	{
		// Token: 0x170003A5 RID: 933
		// (get) Token: 0x06000EEF RID: 3823 RVA: 0x0002FC3D File Offset: 0x0002DE3D
		// (set) Token: 0x06000EF0 RID: 3824 RVA: 0x0002FC45 File Offset: 0x0002DE45
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

		// Token: 0x170003A6 RID: 934
		// (get) Token: 0x06000EF1 RID: 3825 RVA: 0x0002FC4E File Offset: 0x0002DE4E
		public override string Name
		{
			get
			{
				return base.Name + this.Suffix;
			}
		}

		// Token: 0x170003A7 RID: 935
		// (get) Token: 0x06000EF2 RID: 3826 RVA: 0x0002FC61 File Offset: 0x0002DE61
		public override string FullName
		{
			get
			{
				return base.FullName + this.Suffix;
			}
		}

		// Token: 0x170003A8 RID: 936
		// (get) Token: 0x06000EF3 RID: 3827 RVA: 0x0002FC74 File Offset: 0x0002DE74
		private string Suffix
		{
			get
			{
				string str = " modopt(";
				TypeReference typeReference = this.modifier_type;
				return str + ((typeReference != null) ? typeReference.ToString() : null) + ")";
			}
		}

		// Token: 0x170003A9 RID: 937
		// (get) Token: 0x06000EF4 RID: 3828 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x06000EF5 RID: 3829 RVA: 0x0001C627 File Offset: 0x0001A827
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

		// Token: 0x170003AA RID: 938
		// (get) Token: 0x06000EF6 RID: 3830 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsOptionalModifier
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170003AB RID: 939
		// (get) Token: 0x06000EF7 RID: 3831 RVA: 0x0002FC97 File Offset: 0x0002DE97
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.modifier_type.ContainsGenericParameter || base.ContainsGenericParameter;
			}
		}

		// Token: 0x06000EF8 RID: 3832 RVA: 0x0002FCB0 File Offset: 0x0002DEB0
		public OptionalModifierType(TypeReference modifierType, TypeReference type)
			: base(type)
		{
			if (modifierType == null)
			{
				throw new ArgumentNullException(Mixin.Argument.modifierType.ToString());
			}
			Mixin.CheckType(type);
			this.modifier_type = modifierType;
			this.etype = Mono.Cecil.Metadata.ElementType.CModOpt;
		}

		// Token: 0x04000495 RID: 1173
		private TypeReference modifier_type;
	}
}
