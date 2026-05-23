using System;
using System.Reflection;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000935 RID: 2357
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class StructLayoutAttribute : Attribute
	{
		// Token: 0x0600603F RID: 24639 RVA: 0x0014BDD0 File Offset: 0x00149FD0
		[SecurityCritical]
		internal static Attribute GetCustomAttribute(RuntimeType type)
		{
			if (!StructLayoutAttribute.IsDefined(type))
			{
				return null;
			}
			int num = 0;
			int size = 0;
			LayoutKind layoutKind = LayoutKind.Auto;
			TypeAttributes typeAttributes = type.Attributes & TypeAttributes.LayoutMask;
			if (typeAttributes != TypeAttributes.NotPublic)
			{
				if (typeAttributes != TypeAttributes.SequentialLayout)
				{
					if (typeAttributes == TypeAttributes.ExplicitLayout)
					{
						layoutKind = LayoutKind.Explicit;
					}
				}
				else
				{
					layoutKind = LayoutKind.Sequential;
				}
			}
			else
			{
				layoutKind = LayoutKind.Auto;
			}
			CharSet charSet = CharSet.None;
			TypeAttributes typeAttributes2 = type.Attributes & TypeAttributes.StringFormatMask;
			if (typeAttributes2 != TypeAttributes.NotPublic)
			{
				if (typeAttributes2 != TypeAttributes.UnicodeClass)
				{
					if (typeAttributes2 == TypeAttributes.AutoClass)
					{
						charSet = CharSet.Auto;
					}
				}
				else
				{
					charSet = CharSet.Unicode;
				}
			}
			else
			{
				charSet = CharSet.Ansi;
			}
			type.GetRuntimeModule().MetadataImport.GetClassLayout(type.MetadataToken, out num, out size);
			if (num == 0)
			{
				num = 8;
			}
			return new StructLayoutAttribute(layoutKind, num, size, charSet);
		}

		// Token: 0x06006040 RID: 24640 RVA: 0x0014BE6F File Offset: 0x0014A06F
		internal static bool IsDefined(RuntimeType type)
		{
			return !type.IsInterface && !type.HasElementType && !type.IsGenericParameter;
		}

		// Token: 0x06006041 RID: 24641 RVA: 0x0014BE8C File Offset: 0x0014A08C
		internal StructLayoutAttribute(LayoutKind layoutKind, int pack, int size, CharSet charSet)
		{
			this._val = layoutKind;
			this.Pack = pack;
			this.Size = size;
			this.CharSet = charSet;
		}

		// Token: 0x06006042 RID: 24642 RVA: 0x0014BEB1 File Offset: 0x0014A0B1
		[__DynamicallyInvokable]
		public StructLayoutAttribute(LayoutKind layoutKind)
		{
			this._val = layoutKind;
		}

		// Token: 0x06006043 RID: 24643 RVA: 0x0014BEC0 File Offset: 0x0014A0C0
		public StructLayoutAttribute(short layoutKind)
		{
			this._val = (LayoutKind)layoutKind;
		}

		// Token: 0x170010E3 RID: 4323
		// (get) Token: 0x06006044 RID: 24644 RVA: 0x0014BECF File Offset: 0x0014A0CF
		[__DynamicallyInvokable]
		public LayoutKind Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002B1C RID: 11036
		private const int DEFAULT_PACKING_SIZE = 8;

		// Token: 0x04002B1D RID: 11037
		internal LayoutKind _val;

		// Token: 0x04002B1E RID: 11038
		[__DynamicallyInvokable]
		public int Pack;

		// Token: 0x04002B1F RID: 11039
		[__DynamicallyInvokable]
		public int Size;

		// Token: 0x04002B20 RID: 11040
		[__DynamicallyInvokable]
		public CharSet CharSet;
	}
}
