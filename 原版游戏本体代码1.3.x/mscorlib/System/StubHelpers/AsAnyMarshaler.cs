using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace System.StubHelpers
{
	// Token: 0x020005A1 RID: 1441
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[SecurityCritical]
	internal struct AsAnyMarshaler
	{
		// Token: 0x06004314 RID: 17172 RVA: 0x000F9C21 File Offset: 0x000F7E21
		private static bool IsIn(int dwFlags)
		{
			return (dwFlags & 268435456) != 0;
		}

		// Token: 0x06004315 RID: 17173 RVA: 0x000F9C2D File Offset: 0x000F7E2D
		private static bool IsOut(int dwFlags)
		{
			return (dwFlags & 536870912) != 0;
		}

		// Token: 0x06004316 RID: 17174 RVA: 0x000F9C39 File Offset: 0x000F7E39
		private static bool IsAnsi(int dwFlags)
		{
			return (dwFlags & 16711680) != 0;
		}

		// Token: 0x06004317 RID: 17175 RVA: 0x000F9C45 File Offset: 0x000F7E45
		private static bool IsThrowOn(int dwFlags)
		{
			return (dwFlags & 65280) != 0;
		}

		// Token: 0x06004318 RID: 17176 RVA: 0x000F9C51 File Offset: 0x000F7E51
		private static bool IsBestFit(int dwFlags)
		{
			return (dwFlags & 255) != 0;
		}

		// Token: 0x06004319 RID: 17177 RVA: 0x000F9C5D File Offset: 0x000F7E5D
		internal AsAnyMarshaler(IntPtr pvArrayMarshaler)
		{
			this.pvArrayMarshaler = pvArrayMarshaler;
			this.backPropAction = AsAnyMarshaler.BackPropAction.None;
			this.layoutType = null;
			this.cleanupWorkList = null;
		}

		// Token: 0x0600431A RID: 17178 RVA: 0x000F9C7C File Offset: 0x000F7E7C
		[SecurityCritical]
		private unsafe IntPtr ConvertArrayToNative(object pManagedHome, int dwFlags)
		{
			Type elementType = pManagedHome.GetType().GetElementType();
			VarEnum varEnum;
			switch (Type.GetTypeCode(elementType))
			{
			case TypeCode.Object:
				if (elementType == typeof(IntPtr))
				{
					varEnum = ((IntPtr.Size == 4) ? VarEnum.VT_I4 : VarEnum.VT_I8);
					goto IL_10D;
				}
				if (elementType == typeof(UIntPtr))
				{
					varEnum = ((IntPtr.Size == 4) ? VarEnum.VT_UI4 : VarEnum.VT_UI8);
					goto IL_10D;
				}
				break;
			case TypeCode.Boolean:
				varEnum = (VarEnum)254;
				goto IL_10D;
			case TypeCode.Char:
				varEnum = (AsAnyMarshaler.IsAnsi(dwFlags) ? ((VarEnum)253) : VarEnum.VT_UI2);
				goto IL_10D;
			case TypeCode.SByte:
				varEnum = VarEnum.VT_I1;
				goto IL_10D;
			case TypeCode.Byte:
				varEnum = VarEnum.VT_UI1;
				goto IL_10D;
			case TypeCode.Int16:
				varEnum = VarEnum.VT_I2;
				goto IL_10D;
			case TypeCode.UInt16:
				varEnum = VarEnum.VT_UI2;
				goto IL_10D;
			case TypeCode.Int32:
				varEnum = VarEnum.VT_I4;
				goto IL_10D;
			case TypeCode.UInt32:
				varEnum = VarEnum.VT_UI4;
				goto IL_10D;
			case TypeCode.Int64:
				varEnum = VarEnum.VT_I8;
				goto IL_10D;
			case TypeCode.UInt64:
				varEnum = VarEnum.VT_UI8;
				goto IL_10D;
			case TypeCode.Single:
				varEnum = VarEnum.VT_R4;
				goto IL_10D;
			case TypeCode.Double:
				varEnum = VarEnum.VT_R8;
				goto IL_10D;
			}
			throw new ArgumentException(Environment.GetResourceString("Arg_NDirectBadObject"));
			IL_10D:
			int num = (int)varEnum;
			if (AsAnyMarshaler.IsBestFit(dwFlags))
			{
				num |= 65536;
			}
			if (AsAnyMarshaler.IsThrowOn(dwFlags))
			{
				num |= 16777216;
			}
			MngdNativeArrayMarshaler.CreateMarshaler(this.pvArrayMarshaler, IntPtr.Zero, num);
			IntPtr result;
			IntPtr pNativeHome = new IntPtr((void*)(&result));
			MngdNativeArrayMarshaler.ConvertSpaceToNative(this.pvArrayMarshaler, ref pManagedHome, pNativeHome);
			if (AsAnyMarshaler.IsIn(dwFlags))
			{
				MngdNativeArrayMarshaler.ConvertContentsToNative(this.pvArrayMarshaler, ref pManagedHome, pNativeHome);
			}
			if (AsAnyMarshaler.IsOut(dwFlags))
			{
				this.backPropAction = AsAnyMarshaler.BackPropAction.Array;
			}
			return result;
		}

		// Token: 0x0600431B RID: 17179 RVA: 0x000F9E0C File Offset: 0x000F800C
		[SecurityCritical]
		private static IntPtr ConvertStringToNative(string pManagedHome, int dwFlags)
		{
			IntPtr intPtr;
			if (AsAnyMarshaler.IsAnsi(dwFlags))
			{
				intPtr = CSTRMarshaler.ConvertToNative(dwFlags & 65535, pManagedHome, IntPtr.Zero);
			}
			else
			{
				StubHelpers.CheckStringLength(pManagedHome.Length);
				int num = (pManagedHome.Length + 1) * 2;
				intPtr = Marshal.AllocCoTaskMem(num);
				string.InternalCopy(pManagedHome, intPtr, num);
			}
			return intPtr;
		}

		// Token: 0x0600431C RID: 17180 RVA: 0x000F9E5C File Offset: 0x000F805C
		[SecurityCritical]
		private unsafe IntPtr ConvertStringBuilderToNative(StringBuilder pManagedHome, int dwFlags)
		{
			IntPtr intPtr;
			if (AsAnyMarshaler.IsAnsi(dwFlags))
			{
				StubHelpers.CheckStringLength(pManagedHome.Capacity);
				int num = pManagedHome.Capacity * Marshal.SystemMaxDBCSCharSize + 4;
				intPtr = Marshal.AllocCoTaskMem(num);
				byte* ptr = (byte*)(void*)intPtr;
				*(ptr + num - 3) = 0;
				*(ptr + num - 2) = 0;
				*(ptr + num - 1) = 0;
				if (AsAnyMarshaler.IsIn(dwFlags))
				{
					int num2;
					byte[] src = AnsiCharMarshaler.DoAnsiConversion(pManagedHome.ToString(), AsAnyMarshaler.IsBestFit(dwFlags), AsAnyMarshaler.IsThrowOn(dwFlags), out num2);
					Buffer.Memcpy(ptr, 0, src, 0, num2);
					ptr[num2] = 0;
				}
				if (AsAnyMarshaler.IsOut(dwFlags))
				{
					this.backPropAction = AsAnyMarshaler.BackPropAction.StringBuilderAnsi;
				}
			}
			else
			{
				int num3 = pManagedHome.Capacity * 2 + 4;
				intPtr = Marshal.AllocCoTaskMem(num3);
				byte* ptr2 = (byte*)(void*)intPtr;
				*(ptr2 + num3 - 1) = 0;
				*(ptr2 + num3 - 2) = 0;
				if (AsAnyMarshaler.IsIn(dwFlags))
				{
					int num4 = pManagedHome.Length * 2;
					pManagedHome.InternalCopy(intPtr, num4);
					ptr2[num4] = 0;
					(ptr2 + num4)[1] = 0;
				}
				if (AsAnyMarshaler.IsOut(dwFlags))
				{
					this.backPropAction = AsAnyMarshaler.BackPropAction.StringBuilderUnicode;
				}
			}
			return intPtr;
		}

		// Token: 0x0600431D RID: 17181 RVA: 0x000F9F60 File Offset: 0x000F8160
		[SecurityCritical]
		private unsafe IntPtr ConvertLayoutToNative(object pManagedHome, int dwFlags)
		{
			int cb = Marshal.SizeOfHelper(pManagedHome.GetType(), false);
			IntPtr result = Marshal.AllocCoTaskMem(cb);
			if (AsAnyMarshaler.IsIn(dwFlags))
			{
				StubHelpers.FmtClassUpdateNativeInternal(pManagedHome, (byte*)result.ToPointer(), ref this.cleanupWorkList);
			}
			if (AsAnyMarshaler.IsOut(dwFlags))
			{
				this.backPropAction = AsAnyMarshaler.BackPropAction.Layout;
			}
			this.layoutType = pManagedHome.GetType();
			return result;
		}

		// Token: 0x0600431E RID: 17182 RVA: 0x000F9FB8 File Offset: 0x000F81B8
		[SecurityCritical]
		internal IntPtr ConvertToNative(object pManagedHome, int dwFlags)
		{
			if (pManagedHome == null)
			{
				return IntPtr.Zero;
			}
			if (pManagedHome is ArrayWithOffset)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MarshalAsAnyRestriction"));
			}
			IntPtr result;
			string pManagedHome2;
			StringBuilder pManagedHome3;
			if (pManagedHome.GetType().IsArray)
			{
				result = this.ConvertArrayToNative(pManagedHome, dwFlags);
			}
			else if ((pManagedHome2 = pManagedHome as string) != null)
			{
				result = AsAnyMarshaler.ConvertStringToNative(pManagedHome2, dwFlags);
			}
			else if ((pManagedHome3 = pManagedHome as StringBuilder) != null)
			{
				result = this.ConvertStringBuilderToNative(pManagedHome3, dwFlags);
			}
			else
			{
				if (!pManagedHome.GetType().IsLayoutSequential && !pManagedHome.GetType().IsExplicitLayout)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_NDirectBadObject"));
				}
				result = this.ConvertLayoutToNative(pManagedHome, dwFlags);
			}
			return result;
		}

		// Token: 0x0600431F RID: 17183 RVA: 0x000FA060 File Offset: 0x000F8260
		[SecurityCritical]
		internal unsafe void ConvertToManaged(object pManagedHome, IntPtr pNativeHome)
		{
			switch (this.backPropAction)
			{
			case AsAnyMarshaler.BackPropAction.Array:
				MngdNativeArrayMarshaler.ConvertContentsToManaged(this.pvArrayMarshaler, ref pManagedHome, new IntPtr((void*)(&pNativeHome)));
				return;
			case AsAnyMarshaler.BackPropAction.Layout:
				StubHelpers.FmtClassUpdateCLRInternal(pManagedHome, (byte*)pNativeHome.ToPointer());
				return;
			case AsAnyMarshaler.BackPropAction.StringBuilderAnsi:
			{
				sbyte* newBuffer = (sbyte*)pNativeHome.ToPointer();
				((StringBuilder)pManagedHome).ReplaceBufferAnsiInternal(newBuffer, Win32Native.lstrlenA(pNativeHome));
				return;
			}
			case AsAnyMarshaler.BackPropAction.StringBuilderUnicode:
			{
				char* newBuffer2 = (char*)pNativeHome.ToPointer();
				((StringBuilder)pManagedHome).ReplaceBufferInternal(newBuffer2, Win32Native.lstrlenW(pNativeHome));
				return;
			}
			default:
				return;
			}
		}

		// Token: 0x06004320 RID: 17184 RVA: 0x000FA0E6 File Offset: 0x000F82E6
		[SecurityCritical]
		internal void ClearNative(IntPtr pNativeHome)
		{
			if (pNativeHome != IntPtr.Zero)
			{
				if (this.layoutType != null)
				{
					Marshal.DestroyStructure(pNativeHome, this.layoutType);
				}
				Win32Native.CoTaskMemFree(pNativeHome);
			}
			StubHelpers.DestroyCleanupList(ref this.cleanupWorkList);
		}

		// Token: 0x04001BDA RID: 7130
		private const ushort VTHACK_ANSICHAR = 253;

		// Token: 0x04001BDB RID: 7131
		private const ushort VTHACK_WINBOOL = 254;

		// Token: 0x04001BDC RID: 7132
		private IntPtr pvArrayMarshaler;

		// Token: 0x04001BDD RID: 7133
		private AsAnyMarshaler.BackPropAction backPropAction;

		// Token: 0x04001BDE RID: 7134
		private Type layoutType;

		// Token: 0x04001BDF RID: 7135
		private CleanupWorkList cleanupWorkList;

		// Token: 0x02000C36 RID: 3126
		private enum BackPropAction
		{
			// Token: 0x04003728 RID: 14120
			None,
			// Token: 0x04003729 RID: 14121
			Array,
			// Token: 0x0400372A RID: 14122
			Layout,
			// Token: 0x0400372B RID: 14123
			StringBuilderAnsi,
			// Token: 0x0400372C RID: 14124
			StringBuilderUnicode
		}
	}
}
