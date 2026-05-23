using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x0200004E RID: 78
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class WrappedPropertyInfo : PropertyInfo, INotifyPropertyChanged
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600027A RID: 634 RVA: 0x00008E90 File Offset: 0x00007090
		// (remove) Token: 0x0600027B RID: 635 RVA: 0x00008EC8 File Offset: 0x000070C8
		[Nullable(2)]
		[method: NullableContext(2)]
		[Nullable(2)]
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x0600027C RID: 636 RVA: 0x00008EFD File Offset: 0x000070FD
		public WrappedPropertyInfo(PropertyInfo actualPropertyInfo, object instance)
		{
			this._propertyInfoImplementation = actualPropertyInfo;
			this._instance = instance;
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x0600027D RID: 637 RVA: 0x00008F13 File Offset: 0x00007113
		public override PropertyAttributes Attributes
		{
			get
			{
				return this._propertyInfoImplementation.Attributes;
			}
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x0600027E RID: 638 RVA: 0x00008F20 File Offset: 0x00007120
		public override bool CanRead
		{
			get
			{
				return this._propertyInfoImplementation.CanRead;
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x0600027F RID: 639 RVA: 0x00008F2D File Offset: 0x0000712D
		public override bool CanWrite
		{
			get
			{
				return this._propertyInfoImplementation.CanWrite;
			}
		}

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x06000280 RID: 640 RVA: 0x00008F3A File Offset: 0x0000713A
		public override IEnumerable<CustomAttributeData> CustomAttributes
		{
			get
			{
				return this._propertyInfoImplementation.CustomAttributes;
			}
		}

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x06000281 RID: 641 RVA: 0x00008F47 File Offset: 0x00007147
		[Nullable(2)]
		public override Type DeclaringType
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.DeclaringType;
			}
		}

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x06000282 RID: 642 RVA: 0x00008F54 File Offset: 0x00007154
		[Nullable(2)]
		public override MethodInfo GetMethod
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.GetMethod;
			}
		}

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x06000283 RID: 643 RVA: 0x00008F61 File Offset: 0x00007161
		public override MemberTypes MemberType
		{
			get
			{
				return this._propertyInfoImplementation.MemberType;
			}
		}

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x06000284 RID: 644 RVA: 0x00008F6E File Offset: 0x0000716E
		public override int MetadataToken
		{
			get
			{
				return this._propertyInfoImplementation.MetadataToken;
			}
		}

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x06000285 RID: 645 RVA: 0x00008F7B File Offset: 0x0000717B
		public override Module Module
		{
			get
			{
				return this._propertyInfoImplementation.Module;
			}
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x06000286 RID: 646 RVA: 0x00008F88 File Offset: 0x00007188
		public override string Name
		{
			get
			{
				return this._propertyInfoImplementation.Name;
			}
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x06000287 RID: 647 RVA: 0x00008F95 File Offset: 0x00007195
		public override Type PropertyType
		{
			get
			{
				return this._propertyInfoImplementation.PropertyType;
			}
		}

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x06000288 RID: 648 RVA: 0x00008FA2 File Offset: 0x000071A2
		[Nullable(2)]
		public override Type ReflectedType
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.ReflectedType;
			}
		}

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000289 RID: 649 RVA: 0x00008FAF File Offset: 0x000071AF
		[Nullable(2)]
		public override MethodInfo SetMethod
		{
			[NullableContext(2)]
			get
			{
				return this._propertyInfoImplementation.SetMethod;
			}
		}

		// Token: 0x0600028A RID: 650 RVA: 0x00008FBC File Offset: 0x000071BC
		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			return (from m in this._propertyInfoImplementation.GetAccessors(nonPublic)
				select new WrappedMethodInfo(m, this._instance)).Cast<MethodInfo>().ToArray<MethodInfo>();
		}

		// Token: 0x0600028B RID: 651 RVA: 0x00008FE5 File Offset: 0x000071E5
		[NullableContext(2)]
		public override object GetConstantValue()
		{
			return this._propertyInfoImplementation.GetConstantValue();
		}

		// Token: 0x0600028C RID: 652 RVA: 0x00008FF2 File Offset: 0x000071F2
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this._propertyInfoImplementation.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x0600028D RID: 653 RVA: 0x00009001 File Offset: 0x00007201
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this._propertyInfoImplementation.GetCustomAttributes(inherit);
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000900F File Offset: 0x0000720F
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return this._propertyInfoImplementation.GetCustomAttributesData();
		}

		// Token: 0x0600028F RID: 655 RVA: 0x0000901C File Offset: 0x0000721C
		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			MethodInfo getMethod = this._propertyInfoImplementation.GetGetMethod(nonPublic);
			if (getMethod != null)
			{
				return new WrappedMethodInfo(getMethod, this._instance);
			}
			return null;
		}

		// Token: 0x06000290 RID: 656 RVA: 0x00009047 File Offset: 0x00007247
		public override ParameterInfo[] GetIndexParameters()
		{
			return this._propertyInfoImplementation.GetIndexParameters();
		}

		// Token: 0x06000291 RID: 657 RVA: 0x00009054 File Offset: 0x00007254
		public override Type[] GetOptionalCustomModifiers()
		{
			return this._propertyInfoImplementation.GetOptionalCustomModifiers();
		}

		// Token: 0x06000292 RID: 658 RVA: 0x00009061 File Offset: 0x00007261
		[NullableContext(2)]
		public override object GetRawConstantValue()
		{
			return this._propertyInfoImplementation.GetRawConstantValue();
		}

		// Token: 0x06000293 RID: 659 RVA: 0x0000906E File Offset: 0x0000726E
		public override Type[] GetRequiredCustomModifiers()
		{
			return this._propertyInfoImplementation.GetRequiredCustomModifiers();
		}

		// Token: 0x06000294 RID: 660 RVA: 0x0000907C File Offset: 0x0000727C
		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			MethodInfo setMethod = this._propertyInfoImplementation.GetSetMethod(nonPublic);
			if (setMethod != null)
			{
				return new WrappedMethodInfo(setMethod, this._instance);
			}
			return null;
		}

		// Token: 0x06000295 RID: 661 RVA: 0x000090A7 File Offset: 0x000072A7
		[NullableContext(2)]
		public override object GetValue(object obj, object[] index)
		{
			return this._propertyInfoImplementation.GetValue(this._instance, index);
		}

		// Token: 0x06000296 RID: 662 RVA: 0x000090BB File Offset: 0x000072BB
		[NullableContext(2)]
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			return this._propertyInfoImplementation.GetValue(this._instance, invokeAttr, binder, index, culture);
		}

		// Token: 0x06000297 RID: 663 RVA: 0x000090D4 File Offset: 0x000072D4
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this._propertyInfoImplementation.IsDefined(attributeType, inherit);
		}

		// Token: 0x06000298 RID: 664 RVA: 0x000090E3 File Offset: 0x000072E3
		[NullableContext(2)]
		public override void SetValue(object obj, object value, object[] index)
		{
			this._propertyInfoImplementation.SetValue(this._instance, value, index);
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged == null)
			{
				return;
			}
			propertyChanged(this, new PropertyChangedEventArgs(this._propertyInfoImplementation.Name));
		}

		// Token: 0x06000299 RID: 665 RVA: 0x00009119 File Offset: 0x00007319
		[NullableContext(2)]
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			this._propertyInfoImplementation.SetValue(this._instance, value, invokeAttr, binder, index, culture);
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged == null)
			{
				return;
			}
			propertyChanged(this, new PropertyChangedEventArgs(this._propertyInfoImplementation.Name));
		}

		// Token: 0x0600029A RID: 666 RVA: 0x00009155 File Offset: 0x00007355
		[NullableContext(2)]
		public override string ToString()
		{
			return this._propertyInfoImplementation.ToString();
		}

		// Token: 0x0600029B RID: 667 RVA: 0x00009164 File Offset: 0x00007364
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

		// Token: 0x0600029C RID: 668 RVA: 0x000091B8 File Offset: 0x000073B8
		public override int GetHashCode()
		{
			return this._propertyInfoImplementation.GetHashCode();
		}

		// Token: 0x040000F4 RID: 244
		private readonly object _instance;

		// Token: 0x040000F5 RID: 245
		private readonly PropertyInfo _propertyInfoImplementation;
	}
}
