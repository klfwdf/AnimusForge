using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x020005A6 RID: 1446
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class SystemTypeMarshaler
	{
		// Token: 0x06004326 RID: 17190 RVA: 0x000FA1A4 File Offset: 0x000F83A4
		[SecurityCritical]
		internal unsafe static void ConvertToNative(Type managedType, TypeNameNative* pNativeType)
		{
			if (!Environment.IsWinRTSupported)
			{
				throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
			}
			string text2;
			if (managedType != null)
			{
				if (managedType.GetType() != typeof(RuntimeType))
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_WinRTSystemRuntimeType", new object[] { managedType.GetType().ToString() }));
				}
				bool flag;
				string text = WinRTTypeNameConverter.ConvertToWinRTTypeName(managedType, out flag);
				if (text != null)
				{
					text2 = text;
					if (flag)
					{
						pNativeType->typeKind = TypeKind.Primitive;
					}
					else
					{
						pNativeType->typeKind = TypeKind.Metadata;
					}
				}
				else
				{
					text2 = managedType.AssemblyQualifiedName;
					pNativeType->typeKind = TypeKind.Projection;
				}
			}
			else
			{
				text2 = "";
				pNativeType->typeKind = TypeKind.Projection;
			}
			int errorCode = UnsafeNativeMethods.WindowsCreateString(text2, text2.Length, &pNativeType->typeName);
			Marshal.ThrowExceptionForHR(errorCode, new IntPtr(-1));
		}

		// Token: 0x06004327 RID: 17191 RVA: 0x000FA26C File Offset: 0x000F846C
		[SecurityCritical]
		internal unsafe static void ConvertToManaged(TypeNameNative* pNativeType, ref Type managedType)
		{
			if (!Environment.IsWinRTSupported)
			{
				throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
			}
			string text = WindowsRuntimeMarshal.HStringToString(pNativeType->typeName);
			if (string.IsNullOrEmpty(text))
			{
				managedType = null;
				return;
			}
			if (pNativeType->typeKind == TypeKind.Projection)
			{
				managedType = Type.GetType(text, true);
				return;
			}
			bool flag;
			managedType = WinRTTypeNameConverter.GetTypeFromWinRTTypeName(text, out flag);
			if (flag != (pNativeType->typeKind == TypeKind.Primitive))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_Unexpected_TypeSource"));
			}
		}

		// Token: 0x06004328 RID: 17192 RVA: 0x000FA2E1 File Offset: 0x000F84E1
		[SecurityCritical]
		internal unsafe static void ClearNative(TypeNameNative* pNativeType)
		{
			TypeNameNative typeNameNative = *pNativeType;
			UnsafeNativeMethods.WindowsDeleteString(pNativeType->typeName);
		}
	}
}
