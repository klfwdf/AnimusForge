using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bannerlord.BUTR.Shared.Utils;

namespace MCM.UI.Utils
{
	// Token: 0x02000015 RID: 21
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class WrappedPropertyInfo : PropertyInfo
	{
		// Token: 0x06000066 RID: 102 RVA: 0x000033AE File Offset: 0x000015AE
		public WrappedPropertyInfo(PropertyInfo actualPropertyInfo, object instance, [Nullable(2)] Action onSet = null)
		{
			this._propertyInfoImplementation = actualPropertyInfo;
			this._instance = instance;
			this._onSet = onSet;
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000067 RID: 103 RVA: 0x000033CB File Offset: 0x000015CB
		public override PropertyAttributes Attributes
		{
			get
			{
				return this._propertyInfoImplementation.Attributes;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000068 RID: 104 RVA: 0x000033D8 File Offset: 0x000015D8
		public override bool CanRead
		{
			get
			{
				return this._propertyInfoImplementation.CanRead;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000069 RID: 105 RVA: 0x000033E5 File Offset: 0x000015E5
		public override bool CanWrite
		{
			get
			{
				return this._propertyInfoImplementation.CanWrite;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600006A RID: 106 RVA: 0x000033F2 File Offset: 0x000015F2
		public override IEnumerable<CustomAttributeData> CustomAttributes
		{
			get
			{
				return this._propertyInfoImplementation.CustomAttributes;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600006B RID: 107 RVA: 0x000033FF File Offset: 0x000015FF
		[Nullable(2)]
		public override Type DeclaringType
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.DeclaringType;
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600006C RID: 108 RVA: 0x0000340C File Offset: 0x0000160C
		[Nullable(2)]
		public override MethodInfo GetMethod
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.GetMethod;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600006D RID: 109 RVA: 0x00003419 File Offset: 0x00001619
		public override MemberTypes MemberType
		{
			get
			{
				return this._propertyInfoImplementation.MemberType;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600006E RID: 110 RVA: 0x00003426 File Offset: 0x00001626
		public override int MetadataToken
		{
			get
			{
				return this._propertyInfoImplementation.MetadataToken;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600006F RID: 111 RVA: 0x00003433 File Offset: 0x00001633
		public override Module Module
		{
			get
			{
				return this._propertyInfoImplementation.Module;
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00003440 File Offset: 0x00001640
		public override string Name
		{
			get
			{
				return this._propertyInfoImplementation.Name;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000071 RID: 113 RVA: 0x0000344D File Offset: 0x0000164D
		public override Type PropertyType
		{
			get
			{
				return this._propertyInfoImplementation.PropertyType;
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000072 RID: 114 RVA: 0x0000345A File Offset: 0x0000165A
		[Nullable(2)]
		public override Type ReflectedType
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.ReflectedType;
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000073 RID: 115 RVA: 0x00003467 File Offset: 0x00001667
		[Nullable(2)]
		public override MethodInfo SetMethod
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.SetMethod;
			}
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00003474 File Offset: 0x00001674
		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			return (from m in this._propertyInfoImplementation.GetAccessors(nonPublic)
				select new WrappedMethodInfo(m, this._instance)).Cast<MethodInfo>().ToArray<MethodInfo>();
		}

		// Token: 0x06000075 RID: 117 RVA: 0x0000349D File Offset: 0x0000169D
		[NullableContext(2)]
		public override object GetConstantValue()
		{
			return this._propertyInfoImplementation.GetConstantValue();
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000034AA File Offset: 0x000016AA
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this._propertyInfoImplementation.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06000077 RID: 119 RVA: 0x000034B9 File Offset: 0x000016B9
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this._propertyInfoImplementation.GetCustomAttributes(inherit);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000034C7 File Offset: 0x000016C7
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return this._propertyInfoImplementation.GetCustomAttributesData();
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000034D4 File Offset: 0x000016D4
		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			MethodInfo getMethod = this._propertyInfoImplementation.GetGetMethod(nonPublic);
			if (getMethod != null)
			{
				return new WrappedMethodInfo(getMethod, this._instance);
			}
			return null;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x000034FF File Offset: 0x000016FF
		public override ParameterInfo[] GetIndexParameters()
		{
			return this._propertyInfoImplementation.GetIndexParameters();
		}

		// Token: 0x0600007B RID: 123 RVA: 0x0000350C File Offset: 0x0000170C
		public override Type[] GetOptionalCustomModifiers()
		{
			return this._propertyInfoImplementation.GetOptionalCustomModifiers();
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00003519 File Offset: 0x00001719
		[NullableContext(2)]
		public override object GetRawConstantValue()
		{
			return this._propertyInfoImplementation.GetRawConstantValue();
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00003526 File Offset: 0x00001726
		public override Type[] GetRequiredCustomModifiers()
		{
			return this._propertyInfoImplementation.GetRequiredCustomModifiers();
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00003534 File Offset: 0x00001734
		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			MethodInfo setMethod = this._propertyInfoImplementation.GetSetMethod(nonPublic);
			if (setMethod != null)
			{
				return new WrappedMethodInfo(setMethod, this._instance);
			}
			return null;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x0000355F File Offset: 0x0000175F
		[NullableContext(2)]
		public override object GetValue(object obj, object[] index)
		{
			return this._propertyInfoImplementation.GetValue(this._instance, index);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00003573 File Offset: 0x00001773
		[NullableContext(2)]
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			return this._propertyInfoImplementation.GetValue(this._instance, invokeAttr, binder, index, culture);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x0000358C File Offset: 0x0000178C
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this._propertyInfoImplementation.IsDefined(attributeType, inherit);
		}

		// Token: 0x06000082 RID: 130 RVA: 0x0000359B File Offset: 0x0000179B
		[NullableContext(2)]
		public override void SetValue(object obj, object value, object[] index)
		{
			this._propertyInfoImplementation.SetValue(this._instance, value, index);
			Action onSet = this._onSet;
			if (onSet == null)
			{
				return;
			}
			onSet();
		}

		// Token: 0x06000083 RID: 131 RVA: 0x000035C0 File Offset: 0x000017C0
		[NullableContext(2)]
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, [Nullable(1)] CultureInfo culture)
		{
			this._propertyInfoImplementation.SetValue(this._instance, value, invokeAttr, binder, index, culture);
			Action onSet = this._onSet;
			if (onSet == null)
			{
				return;
			}
			onSet();
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000035EB File Offset: 0x000017EB
		public override string ToString()
		{
			return this._propertyInfoImplementation.ToString();
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000035F8 File Offset: 0x000017F8
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			WrappedPropertyInfo proxy = obj as WrappedPropertyInfo;
			bool result;
			if (proxy == null)
			{
				PropertyInfo propertyInfo = obj as PropertyInfo;
				if (propertyInfo == null)
				{
					result = this._propertyInfoImplementation.Equals(obj);
				}
				else
				{
					result = this._propertyInfoImplementation.Equals(propertyInfo);
				}
			}
			else
			{
				result = this._propertyInfoImplementation.Equals(proxy._propertyInfoImplementation);
			}
			return result;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x0000364C File Offset: 0x0000184C
		public override int GetHashCode()
		{
			return this._propertyInfoImplementation.GetHashCode();
		}

		// Token: 0x0400001B RID: 27
		private readonly object _instance;

		// Token: 0x0400001C RID: 28
		private readonly PropertyInfo _propertyInfoImplementation;

		// Token: 0x0400001D RID: 29
		[Nullable(2)]
		private readonly Action _onSet;
	}
}
