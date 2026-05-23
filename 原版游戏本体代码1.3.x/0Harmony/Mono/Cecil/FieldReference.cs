using System;

namespace Mono.Cecil
{
	// Token: 0x0200023A RID: 570
	internal class FieldReference : MemberReference
	{
		// Token: 0x170002C4 RID: 708
		// (get) Token: 0x06000CB2 RID: 3250 RVA: 0x0002B650 File Offset: 0x00029850
		// (set) Token: 0x06000CB3 RID: 3251 RVA: 0x0002B658 File Offset: 0x00029858
		public TypeReference FieldType
		{
			get
			{
				return this.field_type;
			}
			set
			{
				this.field_type = value;
			}
		}

		// Token: 0x170002C5 RID: 709
		// (get) Token: 0x06000CB4 RID: 3252 RVA: 0x0002B661 File Offset: 0x00029861
		public override string FullName
		{
			get
			{
				return this.field_type.FullName + " " + base.MemberFullName();
			}
		}

		// Token: 0x170002C6 RID: 710
		// (get) Token: 0x06000CB5 RID: 3253 RVA: 0x0002B67E File Offset: 0x0002987E
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.field_type.ContainsGenericParameter || base.ContainsGenericParameter;
			}
		}

		// Token: 0x06000CB6 RID: 3254 RVA: 0x0002B695 File Offset: 0x00029895
		internal FieldReference()
		{
			this.token = new MetadataToken(TokenType.MemberRef);
		}

		// Token: 0x06000CB7 RID: 3255 RVA: 0x0002B6AD File Offset: 0x000298AD
		public FieldReference(string name, TypeReference fieldType)
			: base(name)
		{
			Mixin.CheckType(fieldType, Mixin.Argument.fieldType);
			this.field_type = fieldType;
			this.token = new MetadataToken(TokenType.MemberRef);
		}

		// Token: 0x06000CB8 RID: 3256 RVA: 0x0002B6D5 File Offset: 0x000298D5
		public FieldReference(string name, TypeReference fieldType, TypeReference declaringType)
			: this(name, fieldType)
		{
			Mixin.CheckType(declaringType, Mixin.Argument.declaringType);
			this.DeclaringType = declaringType;
		}

		// Token: 0x06000CB9 RID: 3257 RVA: 0x0002B6EE File Offset: 0x000298EE
		protected override IMemberDefinition ResolveDefinition()
		{
			return this.Resolve();
		}

		// Token: 0x06000CBA RID: 3258 RVA: 0x0002B6F6 File Offset: 0x000298F6
		public new virtual FieldDefinition Resolve()
		{
			ModuleDefinition module = this.Module;
			if (module == null)
			{
				throw new NotSupportedException();
			}
			return module.Resolve(this);
		}

		// Token: 0x040003D5 RID: 981
		private TypeReference field_type;
	}
}
