using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005D5 RID: 1493
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct CustomAttributeNamedArgument
	{
		// Token: 0x06004549 RID: 17737 RVA: 0x000FECCB File Offset: 0x000FCECB
		public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
		{
			return left.Equals(right);
		}

		// Token: 0x0600454A RID: 17738 RVA: 0x000FECE0 File Offset: 0x000FCEE0
		public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
		{
			return !left.Equals(right);
		}

		// Token: 0x0600454B RID: 17739 RVA: 0x000FECF8 File Offset: 0x000FCEF8
		public CustomAttributeNamedArgument(MemberInfo memberInfo, object value)
		{
			if (memberInfo == null)
			{
				throw new ArgumentNullException("memberInfo");
			}
			FieldInfo fieldInfo = memberInfo as FieldInfo;
			PropertyInfo propertyInfo = memberInfo as PropertyInfo;
			Type argumentType;
			if (fieldInfo != null)
			{
				argumentType = fieldInfo.FieldType;
			}
			else
			{
				if (!(propertyInfo != null))
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidMemberForNamedArgument"));
				}
				argumentType = propertyInfo.PropertyType;
			}
			this.m_memberInfo = memberInfo;
			this.m_value = new CustomAttributeTypedArgument(argumentType, value);
		}

		// Token: 0x0600454C RID: 17740 RVA: 0x000FED71 File Offset: 0x000FCF71
		public CustomAttributeNamedArgument(MemberInfo memberInfo, CustomAttributeTypedArgument typedArgument)
		{
			if (memberInfo == null)
			{
				throw new ArgumentNullException("memberInfo");
			}
			this.m_memberInfo = memberInfo;
			this.m_value = typedArgument;
		}

		// Token: 0x0600454D RID: 17741 RVA: 0x000FED98 File Offset: 0x000FCF98
		public override string ToString()
		{
			if (this.m_memberInfo == null)
			{
				return base.ToString();
			}
			return string.Format(CultureInfo.CurrentCulture, "{0} = {1}", this.MemberInfo.Name, this.TypedValue.ToString(this.ArgumentType != typeof(object)));
		}

		// Token: 0x0600454E RID: 17742 RVA: 0x000FEE01 File Offset: 0x000FD001
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x0600454F RID: 17743 RVA: 0x000FEE13 File Offset: 0x000FD013
		public override bool Equals(object obj)
		{
			return obj == this;
		}

		// Token: 0x17000A4C RID: 2636
		// (get) Token: 0x06004550 RID: 17744 RVA: 0x000FEE23 File Offset: 0x000FD023
		internal Type ArgumentType
		{
			get
			{
				if (!(this.m_memberInfo is FieldInfo))
				{
					return ((PropertyInfo)this.m_memberInfo).PropertyType;
				}
				return ((FieldInfo)this.m_memberInfo).FieldType;
			}
		}

		// Token: 0x17000A4D RID: 2637
		// (get) Token: 0x06004551 RID: 17745 RVA: 0x000FEE53 File Offset: 0x000FD053
		public MemberInfo MemberInfo
		{
			get
			{
				return this.m_memberInfo;
			}
		}

		// Token: 0x17000A4E RID: 2638
		// (get) Token: 0x06004552 RID: 17746 RVA: 0x000FEE5B File Offset: 0x000FD05B
		[__DynamicallyInvokable]
		public CustomAttributeTypedArgument TypedValue
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_value;
			}
		}

		// Token: 0x17000A4F RID: 2639
		// (get) Token: 0x06004553 RID: 17747 RVA: 0x000FEE63 File Offset: 0x000FD063
		[__DynamicallyInvokable]
		public string MemberName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.MemberInfo.Name;
			}
		}

		// Token: 0x17000A50 RID: 2640
		// (get) Token: 0x06004554 RID: 17748 RVA: 0x000FEE70 File Offset: 0x000FD070
		[__DynamicallyInvokable]
		public bool IsField
		{
			[__DynamicallyInvokable]
			get
			{
				return this.MemberInfo is FieldInfo;
			}
		}

		// Token: 0x04001C61 RID: 7265
		private MemberInfo m_memberInfo;

		// Token: 0x04001C62 RID: 7266
		private CustomAttributeTypedArgument m_value;
	}
}
