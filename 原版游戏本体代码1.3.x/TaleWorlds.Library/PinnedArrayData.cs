using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.Library
{
	// Token: 0x0200007C RID: 124
	public struct PinnedArrayData<T>
	{
		// Token: 0x1700006F RID: 111
		// (get) Token: 0x0600045D RID: 1117 RVA: 0x0000F72B File Offset: 0x0000D92B
		// (set) Token: 0x0600045E RID: 1118 RVA: 0x0000F733 File Offset: 0x0000D933
		public bool Pinned { get; private set; }

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x0600045F RID: 1119 RVA: 0x0000F73C File Offset: 0x0000D93C
		// (set) Token: 0x06000460 RID: 1120 RVA: 0x0000F744 File Offset: 0x0000D944
		public IntPtr Pointer { get; private set; }

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x06000461 RID: 1121 RVA: 0x0000F74D File Offset: 0x0000D94D
		// (set) Token: 0x06000462 RID: 1122 RVA: 0x0000F755 File Offset: 0x0000D955
		public T[] Array { get; private set; }

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x06000463 RID: 1123 RVA: 0x0000F75E File Offset: 0x0000D95E
		// (set) Token: 0x06000464 RID: 1124 RVA: 0x0000F766 File Offset: 0x0000D966
		public T[,] Array2D { get; private set; }

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x06000465 RID: 1125 RVA: 0x0000F76F File Offset: 0x0000D96F
		public GCHandle Handle
		{
			get
			{
				return this._handle;
			}
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x0000F778 File Offset: 0x0000D978
		public PinnedArrayData(T[] array, bool manualPinning = false)
		{
			this.Array = array;
			this.Array2D = null;
			this.Pinned = false;
			this.Pointer = IntPtr.Zero;
			if (array != null)
			{
				if (!manualPinning)
				{
					try
					{
						this._handle = GCHandleFactory.GetHandle();
						this._handle.Target = array;
						this.Pointer = this.Handle.AddrOfPinnedObject();
						this.Pinned = true;
					}
					catch (ArgumentException)
					{
						manualPinning = true;
					}
				}
				if (manualPinning)
				{
					this.Pinned = false;
					int num = Marshal.SizeOf<T>();
					for (int i = 0; i < array.Length; i++)
					{
						Marshal.StructureToPtr<T>(array[i], PinnedArrayData<T>._unmanagedCache + num * i, false);
					}
					this.Pointer = PinnedArrayData<T>._unmanagedCache;
				}
			}
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x0000F83C File Offset: 0x0000DA3C
		public PinnedArrayData(T[,] array, bool manualPinning = false)
		{
			this.Array = null;
			this.Array2D = array;
			this.Pinned = false;
			this.Pointer = IntPtr.Zero;
			if (array != null)
			{
				if (!manualPinning)
				{
					try
					{
						this._handle = GCHandleFactory.GetHandle();
						this._handle.Target = array;
						this.Pointer = this.Handle.AddrOfPinnedObject();
						this.Pinned = true;
					}
					catch (ArgumentException)
					{
						manualPinning = true;
					}
				}
				if (manualPinning)
				{
					this.Pinned = false;
					int num = Marshal.SizeOf<T>();
					for (int i = 0; i < array.GetLength(0); i++)
					{
						for (int j = 0; j < array.GetLength(1); j++)
						{
							Marshal.StructureToPtr<T>(array[i, j], PinnedArrayData<T>._unmanagedCache + num * (i * array.GetLength(1) + j), false);
						}
					}
					this.Pointer = PinnedArrayData<T>._unmanagedCache;
				}
			}
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x0000F920 File Offset: 0x0000DB20
		public static bool CheckIfTypeRequiresManualPinning(Type type)
		{
			bool result = false;
			Array value = System.Array.CreateInstance(type, 10);
			GCHandle gchandle;
			try
			{
				gchandle = GCHandle.Alloc(value, GCHandleType.Pinned);
				gchandle.AddrOfPinnedObject();
			}
			catch (ArgumentException)
			{
				result = true;
			}
			if (gchandle.IsAllocated)
			{
				gchandle.Free();
			}
			return result;
		}

		// Token: 0x06000469 RID: 1129 RVA: 0x0000F970 File Offset: 0x0000DB70
		public void Dispose()
		{
			if (this.Pinned)
			{
				if (this.Array != null)
				{
					this._handle.Target = null;
					GCHandleFactory.ReturnHandle(this._handle);
					this.Array = null;
					this.Pointer = IntPtr.Zero;
					return;
				}
				if (this.Array2D != null)
				{
					this._handle.Target = null;
					GCHandleFactory.ReturnHandle(this._handle);
					this.Array2D = null;
					this.Pointer = IntPtr.Zero;
				}
			}
		}

		// Token: 0x0400015C RID: 348
		private static IntPtr _unmanagedCache = Marshal.AllocHGlobal(16384);

		// Token: 0x04000161 RID: 353
		private GCHandle _handle;
	}
}
