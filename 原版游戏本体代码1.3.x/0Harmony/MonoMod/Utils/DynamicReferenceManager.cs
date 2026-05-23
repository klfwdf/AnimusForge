using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace MonoMod.Utils
{
	// Token: 0x020008A1 RID: 2209
	[NullableContext(1)]
	[Nullable(0)]
	internal static class DynamicReferenceManager
	{
		// Token: 0x06002D5A RID: 11610 RVA: 0x00098748 File Offset: 0x00096948
		[NullableContext(0)]
		private static DataScope<DynamicReferenceCell> AllocReferenceCore([Nullable(1)] DynamicReferenceManager.Cell cell, out DynamicReferenceCell cellRef)
		{
			cellRef = default(DynamicReferenceCell);
			bool lockTaken = false;
			try
			{
				DynamicReferenceManager.writeLock.Enter(ref lockTaken);
				DynamicReferenceManager.Cell[] array = DynamicReferenceManager.cells;
				int emptyCell = DynamicReferenceManager.firstEmptyCell;
				if (emptyCell >= array.Length)
				{
					DynamicReferenceManager.Cell[] newAlloc = new DynamicReferenceManager.Cell[array.Length * 2];
					Array.Copy(array, newAlloc, array.Length);
					array = (DynamicReferenceManager.cells = newAlloc);
				}
				int idx = emptyCell++;
				while (emptyCell < array.Length && array[emptyCell] != null)
				{
					emptyCell++;
				}
				DynamicReferenceManager.firstEmptyCell = emptyCell;
				Volatile.Write<DynamicReferenceManager.Cell>(ref array[idx], cell);
				cellRef = new DynamicReferenceCell(idx, cell.GetHashCode());
			}
			finally
			{
				if (lockTaken)
				{
					DynamicReferenceManager.writeLock.Exit();
				}
			}
			return new DataScope<DynamicReferenceCell>(DynamicReferenceManager.ScopeHandler.Instance, cellRef);
		}

		// Token: 0x06002D5B RID: 11611 RVA: 0x00098810 File Offset: 0x00096A10
		[NullableContext(0)]
		private static DataScope<DynamicReferenceCell> AllocReferenceClass([Nullable(2)] object value, out DynamicReferenceCell cellRef)
		{
			return DynamicReferenceManager.AllocReferenceCore(new DynamicReferenceManager.RefCell
			{
				Value = value
			}, out cellRef);
		}

		// Token: 0x06002D5C RID: 11612 RVA: 0x00098824 File Offset: 0x00096A24
		[NullableContext(0)]
		private static DataScope<DynamicReferenceCell> AllocReferenceStruct<[Nullable(2)] T>([Nullable(1)] in T value, out DynamicReferenceCell cellRef)
		{
			return DynamicReferenceManager.AllocReferenceCore(new DynamicReferenceManager.ValueCell<T>
			{
				Value = value
			}, out cellRef);
		}

		// Token: 0x06002D5D RID: 11613 RVA: 0x0009883D File Offset: 0x00096A3D
		[NullableContext(2)]
		[MethodImpl((MethodImplOptions)512)]
		[return: Nullable(0)]
		public unsafe static DataScope<DynamicReferenceCell> AllocReference<T>(in T value, out DynamicReferenceCell cellRef)
		{
			if (!typeof(T).IsValueType)
			{
				return DynamicReferenceManager.AllocReferenceClass(*Unsafe.As<T, object>(Unsafe.AsRef<T>(value)), out cellRef);
			}
			return DynamicReferenceManager.AllocReferenceStruct<T>(value, out cellRef);
		}

		// Token: 0x06002D5E RID: 11614 RVA: 0x0009886C File Offset: 0x00096A6C
		private static DynamicReferenceManager.Cell GetCell(DynamicReferenceCell cellRef)
		{
			DynamicReferenceManager.Cell cell = Volatile.Read<DynamicReferenceManager.Cell>(ref DynamicReferenceManager.cells[cellRef.Index]);
			if (cell == null || cell.GetHashCode() != cellRef.Hash)
			{
				throw new ArgumentException("Referenced cell no longer exists", "cellRef");
			}
			return cell;
		}

		// Token: 0x06002D5F RID: 11615 RVA: 0x000988B8 File Offset: 0x00096AB8
		[NullableContext(2)]
		public static object GetValue(DynamicReferenceCell cellRef)
		{
			DynamicReferenceManager.Cell cell = DynamicReferenceManager.GetCell(cellRef);
			ulong num = (ulong)cell.Type;
			if (num == 0UL)
			{
				return Unsafe.As<DynamicReferenceManager.RefCell>(cell).Value;
			}
			if (num != 1UL)
			{
				throw new InvalidOperationException("Cell is not of valid type");
			}
			return Unsafe.As<DynamicReferenceManager.ValueCellBase>(cell).BoxValue();
		}

		// Token: 0x06002D60 RID: 11616 RVA: 0x00098900 File Offset: 0x00096B00
		[NullableContext(2)]
		[MethodImpl((MethodImplOptions)512)]
		private static ref T GetValueRef<T>(DynamicReferenceCell cellRef)
		{
			DynamicReferenceManager.Cell cell = DynamicReferenceManager.GetCell(cellRef);
			ulong num = (ulong)cell.Type;
			if (num == 0UL)
			{
				Helpers.Assert(!typeof(T).IsValueType, null, "!typeof(T).IsValueType");
				DynamicReferenceManager.RefCell refCell = Unsafe.As<DynamicReferenceManager.RefCell>(cell);
				object value = refCell.Value;
				bool value2 = value == null || value is T;
				Helpers.Assert(value2, null, "c.Value is null or T");
				return Unsafe.As<object, T>(ref refCell.Value);
			}
			if (num != 1UL)
			{
				throw new InvalidOperationException("Cell is not of valid type");
			}
			Helpers.Assert(typeof(T).IsValueType, null, "typeof(T).IsValueType");
			return ref ((DynamicReferenceManager.ValueCell<T>)cell).Value;
		}

		// Token: 0x06002D61 RID: 11617 RVA: 0x000989A8 File Offset: 0x00096BA8
		[NullableContext(2)]
		[MethodImpl((MethodImplOptions)512)]
		private static ref T GetValueRefUnsafe<T>(DynamicReferenceCell cellRef)
		{
			DynamicReferenceManager.Cell cell = DynamicReferenceManager.GetCell(cellRef);
			if (default(T) == null)
			{
				return Unsafe.As<object, T>(ref Unsafe.As<DynamicReferenceManager.RefCell>(cell).Value);
			}
			return ref Unsafe.As<DynamicReferenceManager.ValueCell<T>>(cell).Value;
		}

		// Token: 0x06002D62 RID: 11618 RVA: 0x000989E8 File Offset: 0x00096BE8
		[NullableContext(2)]
		public unsafe static T GetValue<T>(DynamicReferenceCell cellRef)
		{
			return *DynamicReferenceManager.GetValueRef<T>(cellRef);
		}

		// Token: 0x06002D63 RID: 11619 RVA: 0x000989F5 File Offset: 0x00096BF5
		[NullableContext(2)]
		internal static object GetValue(int index, int hash)
		{
			return DynamicReferenceManager.GetValue(new DynamicReferenceCell(index, hash));
		}

		// Token: 0x06002D64 RID: 11620 RVA: 0x00098A03 File Offset: 0x00096C03
		[NullableContext(2)]
		internal static T GetValueT<T>(int index, int hash)
		{
			return DynamicReferenceManager.GetValue<T>(new DynamicReferenceCell(index, hash));
		}

		// Token: 0x06002D65 RID: 11621 RVA: 0x00098A11 File Offset: 0x00096C11
		[NullableContext(2)]
		internal unsafe static T GetValueTUnsafe<T>(int index, int hash)
		{
			return *DynamicReferenceManager.GetValueRefUnsafe<T>(new DynamicReferenceCell(index, hash));
		}

		// Token: 0x06002D66 RID: 11622 RVA: 0x00098A24 File Offset: 0x00096C24
		[NullableContext(2)]
		public unsafe static void SetValue<T>(DynamicReferenceCell cellRef, in T value)
		{
			*DynamicReferenceManager.GetValueRef<T>(cellRef) = value;
		}

		// Token: 0x06002D67 RID: 11623 RVA: 0x00098A38 File Offset: 0x00096C38
		public static void EmitLoadReference(this ILProcessor il, DynamicReferenceCell cellRef)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(Mono.Cecil.Cil.OpCodes.Call, il.Body.Method.Module.ImportReference(DynamicReferenceManager.Self_GetValue_ii));
		}

		// Token: 0x06002D68 RID: 11624 RVA: 0x00098A9C File Offset: 0x00096C9C
		public static void EmitLoadReference(this ILCursor il, DynamicReferenceCell cellRef)
		{
			Helpers.ThrowIfArgumentNull<ILCursor>(il, "il");
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(Mono.Cecil.Cil.OpCodes.Call, il.Body.Method.Module.ImportReference(DynamicReferenceManager.Self_GetValue_ii));
		}

		// Token: 0x06002D69 RID: 11625 RVA: 0x00098B00 File Offset: 0x00096D00
		public static void EmitLoadReference(this ILGenerator il, DynamicReferenceCell cellRef)
		{
			Helpers.ThrowIfArgumentNull<ILGenerator>(il, "il");
			il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(System.Reflection.Emit.OpCodes.Call, DynamicReferenceManager.Self_GetValue_ii);
		}

		// Token: 0x06002D6A RID: 11626 RVA: 0x00098B4C File Offset: 0x00096D4C
		public static void EmitLoadTypedReference(this ILProcessor il, DynamicReferenceCell cellRef, Type type)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(Mono.Cecil.Cil.OpCodes.Call, il.Body.Method.Module.ImportReference(DynamicReferenceManager.Self_GetValueT_ii.MakeGenericMethod(new Type[] { type })));
		}

		// Token: 0x06002D6B RID: 11627 RVA: 0x00098BBC File Offset: 0x00096DBC
		public static void EmitLoadTypedReference(this ILCursor il, DynamicReferenceCell cellRef, Type type)
		{
			Helpers.ThrowIfArgumentNull<ILCursor>(il, "il");
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(Mono.Cecil.Cil.OpCodes.Call, il.Body.Method.Module.ImportReference(DynamicReferenceManager.Self_GetValueT_ii.MakeGenericMethod(new Type[] { type })));
		}

		// Token: 0x06002D6C RID: 11628 RVA: 0x00098C30 File Offset: 0x00096E30
		public static void EmitLoadTypedReference(this ILGenerator il, DynamicReferenceCell cellRef, Type type)
		{
			Helpers.ThrowIfArgumentNull<ILGenerator>(il, "il");
			il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(System.Reflection.Emit.OpCodes.Call, DynamicReferenceManager.Self_GetValueT_ii.MakeGenericMethod(new Type[] { type }));
		}

		// Token: 0x06002D6D RID: 11629 RVA: 0x00098C8C File Offset: 0x00096E8C
		internal static void EmitLoadTypedReferenceUnsafe(this ILProcessor il, DynamicReferenceCell cellRef, Type type)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(Mono.Cecil.Cil.OpCodes.Call, il.Body.Method.Module.ImportReference(DynamicReferenceManager.Self_GetValueTUnsafe_ii.MakeGenericMethod(new Type[] { type })));
		}

		// Token: 0x06002D6E RID: 11630 RVA: 0x00098CFC File Offset: 0x00096EFC
		internal static void EmitLoadTypedReferenceUnsafe(this ILCursor il, DynamicReferenceCell cellRef, Type type)
		{
			Helpers.ThrowIfArgumentNull<ILCursor>(il, "il");
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(Mono.Cecil.Cil.OpCodes.Call, il.Body.Method.Module.ImportReference(DynamicReferenceManager.Self_GetValueTUnsafe_ii.MakeGenericMethod(new Type[] { type })));
		}

		// Token: 0x06002D6F RID: 11631 RVA: 0x00098D70 File Offset: 0x00096F70
		internal static void EmitLoadTypedReferenceUnsafe(this ILGenerator il, DynamicReferenceCell cellRef, Type type)
		{
			Helpers.ThrowIfArgumentNull<ILGenerator>(il, "il");
			il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, cellRef.Index);
			il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, cellRef.Hash);
			il.Emit(System.Reflection.Emit.OpCodes.Call, DynamicReferenceManager.Self_GetValueTUnsafe_ii.MakeGenericMethod(new Type[] { type }));
		}

		// Token: 0x06002D70 RID: 11632 RVA: 0x00098DCB File Offset: 0x00096FCB
		[NullableContext(0)]
		public static DataScope<DynamicReferenceCell> EmitNewReference([Nullable(1)] this ILProcessor il, [Nullable(2)] object value, out DynamicReferenceCell cellRef)
		{
			DataScope<DynamicReferenceCell> result = DynamicReferenceManager.AllocReference<object>(value, out cellRef);
			il.EmitLoadReference(cellRef);
			return result;
		}

		// Token: 0x06002D71 RID: 11633 RVA: 0x00098DE1 File Offset: 0x00096FE1
		[NullableContext(0)]
		public static DataScope<DynamicReferenceCell> EmitNewReference([Nullable(1)] this ILCursor il, [Nullable(2)] object value, out DynamicReferenceCell cellRef)
		{
			DataScope<DynamicReferenceCell> result = DynamicReferenceManager.AllocReference<object>(value, out cellRef);
			il.EmitLoadReference(cellRef);
			return result;
		}

		// Token: 0x06002D72 RID: 11634 RVA: 0x00098DF7 File Offset: 0x00096FF7
		[NullableContext(0)]
		public static DataScope<DynamicReferenceCell> EmitNewReference([Nullable(1)] this ILGenerator il, [Nullable(2)] object value, out DynamicReferenceCell cellRef)
		{
			DataScope<DynamicReferenceCell> result = DynamicReferenceManager.AllocReference<object>(value, out cellRef);
			il.EmitLoadReference(cellRef);
			return result;
		}

		// Token: 0x06002D73 RID: 11635 RVA: 0x00098E0D File Offset: 0x0009700D
		[NullableContext(2)]
		[return: Nullable(0)]
		public static DataScope<DynamicReferenceCell> EmitNewTypedReference<T>([Nullable(1)] this ILProcessor il, T value, out DynamicReferenceCell cellRef)
		{
			DataScope<DynamicReferenceCell> result = DynamicReferenceManager.AllocReference<T>(value, out cellRef);
			il.EmitLoadTypedReferenceUnsafe(cellRef, typeof(T));
			return result;
		}

		// Token: 0x06002D74 RID: 11636 RVA: 0x00098E2D File Offset: 0x0009702D
		[NullableContext(2)]
		[return: Nullable(0)]
		public static DataScope<DynamicReferenceCell> EmitNewTypedReference<T>([Nullable(1)] this ILCursor il, T value, out DynamicReferenceCell cellRef)
		{
			DataScope<DynamicReferenceCell> result = DynamicReferenceManager.AllocReference<T>(value, out cellRef);
			il.EmitLoadTypedReferenceUnsafe(cellRef, typeof(T));
			return result;
		}

		// Token: 0x06002D75 RID: 11637 RVA: 0x00098E4D File Offset: 0x0009704D
		[NullableContext(2)]
		[return: Nullable(0)]
		public static DataScope<DynamicReferenceCell> EmitNewTypedReference<T>([Nullable(1)] this ILGenerator il, T value, out DynamicReferenceCell cellRef)
		{
			DataScope<DynamicReferenceCell> result = DynamicReferenceManager.AllocReference<T>(value, out cellRef);
			il.EmitLoadTypedReferenceUnsafe(cellRef, typeof(T));
			return result;
		}

		// Token: 0x06002D76 RID: 11638 RVA: 0x00098E70 File Offset: 0x00097070
		// Note: this type is marked as 'beforefieldinit'.
		static DynamicReferenceManager()
		{
			MethodInfo method = typeof(DynamicReferenceManager).GetMethod("GetValue", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[]
			{
				typeof(int),
				typeof(int)
			}, null);
			if (method == null)
			{
				throw new InvalidOperationException("GetValue doesn't exist?!?!?!?");
			}
			DynamicReferenceManager.Self_GetValue_ii = method;
			MethodInfo method2 = typeof(DynamicReferenceManager).GetMethod("GetValueT", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[]
			{
				typeof(int),
				typeof(int)
			}, null);
			if (method2 == null)
			{
				throw new InvalidOperationException("GetValueT doesn't exist?!?!?!?");
			}
			DynamicReferenceManager.Self_GetValueT_ii = method2;
			MethodInfo method3 = typeof(DynamicReferenceManager).GetMethod("GetValueTUnsafe", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[]
			{
				typeof(int),
				typeof(int)
			}, null);
			if (method3 == null)
			{
				throw new InvalidOperationException("GetValueTUnsafe doesn't exist?!?!?!?");
			}
			DynamicReferenceManager.Self_GetValueTUnsafe_ii = method3;
		}

		// Token: 0x04003ACD RID: 15053
		[NativeInteger]
		private const UIntPtr RefValueCell = 0;

		// Token: 0x04003ACE RID: 15054
		[NativeInteger]
		private const UIntPtr ValueTypeCell = 1;

		// Token: 0x04003ACF RID: 15055
		private static SpinLock writeLock = new SpinLock(false);

		// Token: 0x04003AD0 RID: 15056
		[Nullable(new byte[] { 1, 2 })]
		private static volatile DynamicReferenceManager.Cell[] cells = new DynamicReferenceManager.Cell[16];

		// Token: 0x04003AD1 RID: 15057
		private static volatile int firstEmptyCell;

		// Token: 0x04003AD2 RID: 15058
		private static readonly MethodInfo Self_GetValue_ii;

		// Token: 0x04003AD3 RID: 15059
		private static readonly MethodInfo Self_GetValueT_ii;

		// Token: 0x04003AD4 RID: 15060
		private static readonly MethodInfo Self_GetValueTUnsafe_ii;

		// Token: 0x020008A2 RID: 2210
		[NullableContext(0)]
		private abstract class Cell
		{
			// Token: 0x06002D77 RID: 11639 RVA: 0x00098F7A File Offset: 0x0009717A
			protected Cell([NativeInteger] UIntPtr type)
			{
				this.Type = type;
			}

			// Token: 0x04003AD5 RID: 15061
			[NativeInteger]
			public readonly UIntPtr Type;
		}

		// Token: 0x020008A3 RID: 2211
		[NullableContext(0)]
		private class RefCell : DynamicReferenceManager.Cell
		{
			// Token: 0x06002D78 RID: 11640 RVA: 0x00098F89 File Offset: 0x00097189
			public RefCell()
				: base((UIntPtr)((IntPtr)0))
			{
			}

			// Token: 0x04003AD6 RID: 15062
			[Nullable(2)]
			public object Value;
		}

		// Token: 0x020008A4 RID: 2212
		[NullableContext(0)]
		private abstract class ValueCellBase : DynamicReferenceManager.Cell
		{
			// Token: 0x06002D79 RID: 11641 RVA: 0x00098F93 File Offset: 0x00097193
			public ValueCellBase()
				: base((UIntPtr)((IntPtr)1))
			{
			}

			// Token: 0x06002D7A RID: 11642
			[NullableContext(2)]
			public abstract object BoxValue();
		}

		// Token: 0x020008A5 RID: 2213
		[NullableContext(2)]
		[Nullable(0)]
		private class ValueCell<T> : DynamicReferenceManager.ValueCellBase
		{
			// Token: 0x06002D7B RID: 11643 RVA: 0x00098F9D File Offset: 0x0009719D
			public override object BoxValue()
			{
				return this.Value;
			}

			// Token: 0x04003AD7 RID: 15063
			public T Value;
		}

		// Token: 0x020008A6 RID: 2214
		[NullableContext(0)]
		private sealed class ScopeHandler : ScopeHandlerBase<DynamicReferenceCell>
		{
			// Token: 0x06002D7D RID: 11645 RVA: 0x00098FB4 File Offset: 0x000971B4
			public override void EndScope(DynamicReferenceCell data)
			{
				bool lockTaken = false;
				try
				{
					DynamicReferenceManager.writeLock.Enter(ref lockTaken);
					DynamicReferenceManager.Cell[] array = DynamicReferenceManager.cells;
					DynamicReferenceManager.Cell cell = Volatile.Read<DynamicReferenceManager.Cell>(ref array[data.Index]);
					if (cell != null && cell.GetHashCode() == data.Hash)
					{
						Volatile.Write<DynamicReferenceManager.Cell>(ref array[data.Index], null);
						DynamicReferenceManager.firstEmptyCell = Math.Min(DynamicReferenceManager.firstEmptyCell, data.Index);
					}
				}
				finally
				{
					if (lockTaken)
					{
						DynamicReferenceManager.writeLock.Exit();
					}
				}
			}

			// Token: 0x04003AD8 RID: 15064
			[Nullable(1)]
			public static readonly DynamicReferenceManager.ScopeHandler Instance = new DynamicReferenceManager.ScopeHandler();
		}
	}
}
