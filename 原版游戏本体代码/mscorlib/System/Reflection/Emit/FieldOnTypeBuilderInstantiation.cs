using System;
using System.Globalization;

namespace System.Reflection.Emit
{
	// Token: 0x0200066A RID: 1642
	internal sealed class FieldOnTypeBuilderInstantiation : FieldInfo
	{
		// Token: 0x06004EF4 RID: 20212 RVA: 0x0011BFE4 File Offset: 0x0011A1E4
		internal static FieldInfo GetField(FieldInfo Field, TypeBuilderInstantiation type)
		{
			FieldInfo fieldInfo;
			if (type.m_hashtable.Contains(Field))
			{
				fieldInfo = type.m_hashtable[Field] as FieldInfo;
			}
			else
			{
				fieldInfo = new FieldOnTypeBuilderInstantiation(Field, type);
				type.m_hashtable[Field] = fieldInfo;
			}
			return fieldInfo;
		}

		// Token: 0x06004EF5 RID: 20213 RVA: 0x0011C02B File Offset: 0x0011A22B
		internal FieldOnTypeBuilderInstantiation(FieldInfo field, TypeBuilderInstantiation type)
		{
			this.m_field = field;
			this.m_type = type;
		}

		// Token: 0x17000C88 RID: 3208
		// (get) Token: 0x06004EF6 RID: 20214 RVA: 0x0011C041 File Offset: 0x0011A241
		internal FieldInfo FieldInfo
		{
			get
			{
				return this.m_field;
			}
		}

		// Token: 0x17000C89 RID: 3209
		// (get) Token: 0x06004EF7 RID: 20215 RVA: 0x0011C049 File Offset: 0x0011A249
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Field;
			}
		}

		// Token: 0x17000C8A RID: 3210
		// (get) Token: 0x06004EF8 RID: 20216 RVA: 0x0011C04C File Offset: 0x0011A24C
		public override string Name
		{
			get
			{
				return this.m_field.Name;
			}
		}

		// Token: 0x17000C8B RID: 3211
		// (get) Token: 0x06004EF9 RID: 20217 RVA: 0x0011C059 File Offset: 0x0011A259
		public override Type DeclaringType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x17000C8C RID: 3212
		// (get) Token: 0x06004EFA RID: 20218 RVA: 0x0011C061 File Offset: 0x0011A261
		public override Type ReflectedType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x06004EFB RID: 20219 RVA: 0x0011C069 File Offset: 0x0011A269
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.m_field.GetCustomAttributes(inherit);
		}

		// Token: 0x06004EFC RID: 20220 RVA: 0x0011C077 File Offset: 0x0011A277
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.m_field.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06004EFD RID: 20221 RVA: 0x0011C086 File Offset: 0x0011A286
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.m_field.IsDefined(attributeType, inherit);
		}

		// Token: 0x17000C8D RID: 3213
		// (get) Token: 0x06004EFE RID: 20222 RVA: 0x0011C098 File Offset: 0x0011A298
		internal int MetadataTokenInternal
		{
			get
			{
				FieldBuilder fieldBuilder = this.m_field as FieldBuilder;
				if (fieldBuilder != null)
				{
					return fieldBuilder.MetadataTokenInternal;
				}
				return this.m_field.MetadataToken;
			}
		}

		// Token: 0x17000C8E RID: 3214
		// (get) Token: 0x06004EFF RID: 20223 RVA: 0x0011C0CC File Offset: 0x0011A2CC
		public override Module Module
		{
			get
			{
				return this.m_field.Module;
			}
		}

		// Token: 0x06004F00 RID: 20224 RVA: 0x0011C0D9 File Offset: 0x0011A2D9
		public new Type GetType()
		{
			return base.GetType();
		}

		// Token: 0x06004F01 RID: 20225 RVA: 0x0011C0E1 File Offset: 0x0011A2E1
		public override Type[] GetRequiredCustomModifiers()
		{
			return this.m_field.GetRequiredCustomModifiers();
		}

		// Token: 0x06004F02 RID: 20226 RVA: 0x0011C0EE File Offset: 0x0011A2EE
		public override Type[] GetOptionalCustomModifiers()
		{
			return this.m_field.GetOptionalCustomModifiers();
		}

		// Token: 0x06004F03 RID: 20227 RVA: 0x0011C0FB File Offset: 0x0011A2FB
		public override void SetValueDirect(TypedReference obj, object value)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004F04 RID: 20228 RVA: 0x0011C102 File Offset: 0x0011A302
		public override object GetValueDirect(TypedReference obj)
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000C8F RID: 3215
		// (get) Token: 0x06004F05 RID: 20229 RVA: 0x0011C109 File Offset: 0x0011A309
		public override RuntimeFieldHandle FieldHandle
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000C90 RID: 3216
		// (get) Token: 0x06004F06 RID: 20230 RVA: 0x0011C110 File Offset: 0x0011A310
		public override Type FieldType
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x06004F07 RID: 20231 RVA: 0x0011C117 File Offset: 0x0011A317
		public override object GetValue(object obj)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x06004F08 RID: 20232 RVA: 0x0011C11E File Offset: 0x0011A31E
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x17000C91 RID: 3217
		// (get) Token: 0x06004F09 RID: 20233 RVA: 0x0011C125 File Offset: 0x0011A325
		public override FieldAttributes Attributes
		{
			get
			{
				return this.m_field.Attributes;
			}
		}

		// Token: 0x040021DA RID: 8666
		private FieldInfo m_field;

		// Token: 0x040021DB RID: 8667
		private TypeBuilderInstantiation m_type;
	}
}
