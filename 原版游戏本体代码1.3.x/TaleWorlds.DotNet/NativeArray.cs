using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000029 RID: 41
	[EngineClass("ftdnNative_array")]
	public sealed class NativeArray : NativeObject
	{
		// Token: 0x06000104 RID: 260 RVA: 0x00004FC3 File Offset: 0x000031C3
		internal NativeArray(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00004FD2 File Offset: 0x000031D2
		public static NativeArray Create()
		{
			return LibraryApplicationInterface.INativeArray.Create();
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000106 RID: 262 RVA: 0x00004FDE File Offset: 0x000031DE
		public int DataSize
		{
			get
			{
				return LibraryApplicationInterface.INativeArray.GetDataSize(base.Pointer);
			}
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00004FF0 File Offset: 0x000031F0
		private UIntPtr GetDataPointer()
		{
			return LibraryApplicationInterface.INativeArray.GetDataPointer(base.Pointer);
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00005004 File Offset: 0x00003204
		public int GetLength<T>() where T : struct
		{
			int dataSize = this.DataSize;
			int num = Marshal.SizeOf<T>();
			return dataSize / num;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00005020 File Offset: 0x00003220
		public T GetElementAt<T>(int index) where T : struct
		{
			IntPtr intPtr = Marshal.PtrToStructure<IntPtr>(new IntPtr((long)(base.Pointer.ToUInt64() + (ulong)((long)NativeArray.DataPointerOffset))));
			int num = Marshal.SizeOf<T>();
			return Marshal.PtrToStructure<T>(new IntPtr(intPtr.ToInt64() + (long)(index * num)));
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00005069 File Offset: 0x00003269
		public IEnumerable<T> GetEnumerator<T>() where T : struct
		{
			int length = this.GetLength<T>();
			IntPtr ptr = new IntPtr((long)(base.Pointer.ToUInt64() + (ulong)((long)NativeArray.DataPointerOffset)));
			IntPtr dataPointer = Marshal.PtrToStructure<IntPtr>(ptr);
			int elementSize = Marshal.SizeOf<T>();
			int num;
			for (int i = 0; i < length; i = num + 1)
			{
				T t = Marshal.PtrToStructure<T>(new IntPtr(dataPointer.ToInt64() + (long)(i * elementSize)));
				yield return t;
				num = i;
			}
			yield break;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000507C File Offset: 0x0000327C
		public T[] ToArray<T>() where T : struct
		{
			T[] array = new T[this.GetLength<T>()];
			IEnumerable<T> enumerator = this.GetEnumerator<T>();
			int num = 0;
			foreach (T t in enumerator)
			{
				array[num] = t;
				num++;
			}
			return array;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x000050E0 File Offset: 0x000032E0
		public void AddElement(int value)
		{
			LibraryApplicationInterface.INativeArray.AddIntegerElement(base.Pointer, value);
		}

		// Token: 0x0600010D RID: 269 RVA: 0x000050F3 File Offset: 0x000032F3
		public void AddElement(float value)
		{
			LibraryApplicationInterface.INativeArray.AddFloatElement(base.Pointer, value);
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00005108 File Offset: 0x00003308
		public void AddElement<T>(T value) where T : struct
		{
			int elementSize = Marshal.SizeOf<T>();
			Marshal.StructureToPtr<T>(value, NativeArray._temporaryData, false);
			LibraryApplicationInterface.INativeArray.AddElement(base.Pointer, NativeArray._temporaryData, elementSize);
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000513D File Offset: 0x0000333D
		public void Clear()
		{
			LibraryApplicationInterface.INativeArray.Clear(base.Pointer);
		}

		// Token: 0x0400006B RID: 107
		private static readonly IntPtr _temporaryData = Marshal.AllocHGlobal(16384);

		// Token: 0x0400006C RID: 108
		private const int TemporaryDataSize = 16384;

		// Token: 0x0400006D RID: 109
		private static readonly int DataPointerOffset = LibraryApplicationInterface.INativeArray.GetDataPointerOffset();
	}
}
