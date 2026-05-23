using System;
using System.Reflection;
using System.Security;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008E0 RID: 2272
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class TypeForwardedToAttribute : Attribute
	{
		// Token: 0x06005DD8 RID: 24024 RVA: 0x00149A4F File Offset: 0x00147C4F
		[__DynamicallyInvokable]
		public TypeForwardedToAttribute(Type destination)
		{
			this._destination = destination;
		}

		// Token: 0x1700101D RID: 4125
		// (get) Token: 0x06005DD9 RID: 24025 RVA: 0x00149A5E File Offset: 0x00147C5E
		[__DynamicallyInvokable]
		public Type Destination
		{
			[__DynamicallyInvokable]
			get
			{
				return this._destination;
			}
		}

		// Token: 0x06005DDA RID: 24026 RVA: 0x00149A68 File Offset: 0x00147C68
		[SecurityCritical]
		internal static TypeForwardedToAttribute[] GetCustomAttribute(RuntimeAssembly assembly)
		{
			Type[] array = null;
			RuntimeAssembly.GetForwardedTypes(assembly.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<Type[]>(ref array));
			TypeForwardedToAttribute[] array2 = new TypeForwardedToAttribute[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = new TypeForwardedToAttribute(array[i]);
			}
			return array2;
		}

		// Token: 0x04002A3A RID: 10810
		private Type _destination;
	}
}
