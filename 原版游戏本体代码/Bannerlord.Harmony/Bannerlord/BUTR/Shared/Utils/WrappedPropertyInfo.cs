using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x0200000C RID: 12
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class WrappedPropertyInfo : PropertyInfo, INotifyPropertyChanged
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000071 RID: 113 RVA: 0x00003878 File Offset: 0x00001A78
		// (remove) Token: 0x06000072 RID: 114 RVA: 0x000038B0 File Offset: 0x00001AB0
		[Nullable(2)]
		[method: NullableContext(2)]
		[Nullable(2)]
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x06000073 RID: 115 RVA: 0x000038E5 File Offset: 0x00001AE5
		public WrappedPropertyInfo(PropertyInfo actualPropertyInfo, object instance)
		{
			this._propertyInfoImplementation = actualPropertyInfo;
			this._instance = instance;
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000074 RID: 116 RVA: 0x000038FD File Offset: 0x00001AFD
		public override PropertyAttributes Attributes
		{
			get
			{
				return this._propertyInfoImplementation.Attributes;
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000075 RID: 117 RVA: 0x0000390A File Offset: 0x00001B0A
		public override bool CanRead
		{
			get
			{
				return this._propertyInfoImplementation.CanRead;
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00003917 File Offset: 0x00001B17
		public override bool CanWrite
		{
			get
			{
				return this._propertyInfoImplementation.CanWrite;
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000077 RID: 119 RVA: 0x00003924 File Offset: 0x00001B24
		public override IEnumerable<CustomAttributeData> CustomAttributes
		{
			get
			{
				return this._propertyInfoImplementation.CustomAttributes;
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000078 RID: 120 RVA: 0x00003931 File Offset: 0x00001B31
		[Nullable(2)]
		public override Type DeclaringType
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.DeclaringType;
			}
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000079 RID: 121 RVA: 0x0000393E File Offset: 0x00001B3E
		[Nullable(2)]
		public override MethodInfo GetMethod
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.GetMethod;
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600007A RID: 122 RVA: 0x0000394B File Offset: 0x00001B4B
		public override MemberTypes MemberType
		{
			get
			{
				return this._propertyInfoImplementation.MemberType;
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x0600007B RID: 123 RVA: 0x00003958 File Offset: 0x00001B58
		public override int MetadataToken
		{
			get
			{
				return this._propertyInfoImplementation.MetadataToken;
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x0600007C RID: 124 RVA: 0x00003965 File Offset: 0x00001B65
		public override Module Module
		{
			get
			{
				return this._propertyInfoImplementation.Module;
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x0600007D RID: 125 RVA: 0x00003972 File Offset: 0x00001B72
		public override string Name
		{
			get
			{
				return this._propertyInfoImplementation.Name;
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x0600007E RID: 126 RVA: 0x0000397F File Offset: 0x00001B7F
		public override Type PropertyType
		{
			get
			{
				return this._propertyInfoImplementation.PropertyType;
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600007F RID: 127 RVA: 0x0000398C File Offset: 0x00001B8C
		[Nullable(2)]
		public override Type ReflectedType
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.ReflectedType;
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000080 RID: 128 RVA: 0x00003999 File Offset: 0x00001B99
		[Nullable(2)]
		public override MethodInfo SetMethod
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.SetMethod;
			}
		}

		// Token: 0x06000081 RID: 129 RVA: 0x000039A6 File Offset: 0x00001BA6
		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			return (from m in this._propertyInfoImplementation.GetAccessors(nonPublic)
				select new WrappedMethodInfo(m, this._instance)).Cast<MethodInfo>().ToArray<MethodInfo>();
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000039CF File Offset: 0x00001BCF
		[NullableContext(2)]
		public override object GetConstantValue()
		{
			return this._propertyInfoImplementation.GetConstantValue();
		}

		// Token: 0x06000083 RID: 131 RVA: 0x000039DC File Offset: 0x00001BDC
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this._propertyInfoImplementation.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000039EB File Offset: 0x00001BEB
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this._propertyInfoImplementation.GetCustomAttributes(inherit);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000039F9 File Offset: 0x00001BF9
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return this._propertyInfoImplementation.GetCustomAttributesData();
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00003A08 File Offset: 0x00001C08
		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			MethodInfo getMethod = this._propertyInfoImplementation.GetGetMethod(nonPublic);
			return (getMethod == null) ? null : new WrappedMethodInfo(getMethod, this._instance);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00003A39 File Offset: 0x00001C39
		public override ParameterInfo[] GetIndexParameters()
		{
			return this._propertyInfoImplementation.GetIndexParameters();
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00003A46 File Offset: 0x00001C46
		public override Type[] GetOptionalCustomModifiers()
		{
			return this._propertyInfoImplementation.GetOptionalCustomModifiers();
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00003A53 File Offset: 0x00001C53
		[NullableContext(2)]
		public override object GetRawConstantValue()
		{
			return this._propertyInfoImplementation.GetRawConstantValue();
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00003A60 File Offset: 0x00001C60
		public override Type[] GetRequiredCustomModifiers()
		{
			return this._propertyInfoImplementation.GetRequiredCustomModifiers();
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00003A70 File Offset: 0x00001C70
		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			MethodInfo setMethod = this._propertyInfoImplementation.GetSetMethod(nonPublic);
			return (setMethod == null) ? null : new WrappedMethodInfo(setMethod, this._instance);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00003AA1 File Offset: 0x00001CA1
		[NullableContext(2)]
		public override object GetValue(object obj, object[] index)
		{
			return this._propertyInfoImplementation.GetValue(this._instance, index);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00003AB5 File Offset: 0x00001CB5
		[NullableContext(2)]
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			return this._propertyInfoImplementation.GetValue(this._instance, invokeAttr, binder, index, culture);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00003ACE File Offset: 0x00001CCE
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this._propertyInfoImplementation.IsDefined(attributeType, inherit);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00003ADD File Offset: 0x00001CDD
		[NullableContext(2)]
		public override void SetValue(object obj, object value, object[] index)
		{
			this._propertyInfoImplementation.SetValue(this._instance, value, index);
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(this._propertyInfoImplementation.Name));
			}
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00003B17 File Offset: 0x00001D17
		[NullableContext(2)]
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			this._propertyInfoImplementation.SetValue(this._instance, value, invokeAttr, binder, index, culture);
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(this._propertyInfoImplementation.Name));
			}
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00003B57 File Offset: 0x00001D57
		[NullableContext(2)]
		public override string ToString()
		{
			return this._propertyInfoImplementation.ToString();
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00003B64 File Offset: 0x00001D64
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (!true)
			{
			}
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
			if (!true)
			{
			}
			return result;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00003BC6 File Offset: 0x00001DC6
		public override int GetHashCode()
		{
			return this._propertyInfoImplementation.GetHashCode();
		}

		// Token: 0x04000024 RID: 36
		private readonly object _instance;

		// Token: 0x04000025 RID: 37
		private readonly PropertyInfo _propertyInfoImplementation;
	}
}
