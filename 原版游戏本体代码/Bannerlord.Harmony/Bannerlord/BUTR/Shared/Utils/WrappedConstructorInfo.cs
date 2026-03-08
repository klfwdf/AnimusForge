using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x0200000A RID: 10
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class WrappedConstructorInfo : ConstructorInfo
	{
		// Token: 0x0600003E RID: 62 RVA: 0x00003547 File Offset: 0x00001747
		public WrappedConstructorInfo(ConstructorInfo actualConstructorInfo, object[] args)
		{
			this._constructorInfoImplementation = actualConstructorInfo;
			this._args = args;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x0000355F File Offset: 0x0000175F
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this._constructorInfoImplementation.GetCustomAttributes(inherit);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x0000356D File Offset: 0x0000176D
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this._constructorInfoImplementation.IsDefined(attributeType, inherit);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x0000357C File Offset: 0x0000177C
		public override ParameterInfo[] GetParameters()
		{
			return this._constructorInfoImplementation.GetParameters();
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003589 File Offset: 0x00001789
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this._constructorInfoImplementation.GetMethodImplementationFlags();
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003596 File Offset: 0x00001796
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this._constructorInfoImplementation.Invoke(obj, invokeAttr, binder, parameters, culture);
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000044 RID: 68 RVA: 0x000035AA File Offset: 0x000017AA
		public override string Name
		{
			get
			{
				return this._constructorInfoImplementation.Name;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000045 RID: 69 RVA: 0x000035B7 File Offset: 0x000017B7
		[Nullable(2)]
		public override Type DeclaringType
		{
			[NullableContext(2)]
			get
			{
				return this._constructorInfoImplementation.DeclaringType;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000046 RID: 70 RVA: 0x000035C4 File Offset: 0x000017C4
		[Nullable(2)]
		public override Type ReflectedType
		{
			[NullableContext(2)]
			get
			{
				return this._constructorInfoImplementation.ReflectedType;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000047 RID: 71 RVA: 0x000035D1 File Offset: 0x000017D1
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				return this._constructorInfoImplementation.MethodHandle;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000048 RID: 72 RVA: 0x000035DE File Offset: 0x000017DE
		public override MethodAttributes Attributes
		{
			get
			{
				return this._constructorInfoImplementation.Attributes;
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000035EB File Offset: 0x000017EB
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this._constructorInfoImplementation.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000035FA File Offset: 0x000017FA
		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this._constructorInfoImplementation.Invoke(invokeAttr, binder, this._args, culture);
		}

		// Token: 0x0400001F RID: 31
		private readonly object[] _args;

		// Token: 0x04000020 RID: 32
		private readonly ConstructorInfo _constructorInfoImplementation;
	}
}
