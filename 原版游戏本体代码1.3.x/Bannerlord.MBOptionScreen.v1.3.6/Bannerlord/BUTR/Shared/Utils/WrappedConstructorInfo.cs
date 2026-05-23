using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x0200004C RID: 76
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class WrappedConstructorInfo : ConstructorInfo
	{
		// Token: 0x06000247 RID: 583 RVA: 0x00008B70 File Offset: 0x00006D70
		public WrappedConstructorInfo(ConstructorInfo actualConstructorInfo, object[] args)
		{
			this._constructorInfoImplementation = actualConstructorInfo;
			this._args = args;
		}

		// Token: 0x06000248 RID: 584 RVA: 0x00008B86 File Offset: 0x00006D86
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this._constructorInfoImplementation.GetCustomAttributes(inherit);
		}

		// Token: 0x06000249 RID: 585 RVA: 0x00008B94 File Offset: 0x00006D94
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this._constructorInfoImplementation.IsDefined(attributeType, inherit);
		}

		// Token: 0x0600024A RID: 586 RVA: 0x00008BA3 File Offset: 0x00006DA3
		public override ParameterInfo[] GetParameters()
		{
			return this._constructorInfoImplementation.GetParameters();
		}

		// Token: 0x0600024B RID: 587 RVA: 0x00008BB0 File Offset: 0x00006DB0
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this._constructorInfoImplementation.GetMethodImplementationFlags();
		}

		// Token: 0x0600024C RID: 588 RVA: 0x00008BBD File Offset: 0x00006DBD
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this._constructorInfoImplementation.Invoke(obj, invokeAttr, binder, parameters, culture);
		}

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x0600024D RID: 589 RVA: 0x00008BD1 File Offset: 0x00006DD1
		public override string Name
		{
			get
			{
				return this._constructorInfoImplementation.Name;
			}
		}

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x0600024E RID: 590 RVA: 0x00008BDE File Offset: 0x00006DDE
		[Nullable(2)]
		public override Type DeclaringType
		{
			[NullableContext(2)]
			get
			{
				return this._constructorInfoImplementation.DeclaringType;
			}
		}

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x0600024F RID: 591 RVA: 0x00008BEB File Offset: 0x00006DEB
		[Nullable(2)]
		public override Type ReflectedType
		{
			[NullableContext(2)]
			get
			{
				return this._constructorInfoImplementation.ReflectedType;
			}
		}

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x06000250 RID: 592 RVA: 0x00008BF8 File Offset: 0x00006DF8
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				return this._constructorInfoImplementation.MethodHandle;
			}
		}

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x06000251 RID: 593 RVA: 0x00008C05 File Offset: 0x00006E05
		public override MethodAttributes Attributes
		{
			get
			{
				return this._constructorInfoImplementation.Attributes;
			}
		}

		// Token: 0x06000252 RID: 594 RVA: 0x00008C12 File Offset: 0x00006E12
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this._constructorInfoImplementation.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06000253 RID: 595 RVA: 0x00008C21 File Offset: 0x00006E21
		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this._constructorInfoImplementation.Invoke(invokeAttr, binder, this._args, culture);
		}

		// Token: 0x040000EF RID: 239
		private readonly object[] _args;

		// Token: 0x040000F0 RID: 240
		private readonly ConstructorInfo _constructorInfoImplementation;
	}
}
