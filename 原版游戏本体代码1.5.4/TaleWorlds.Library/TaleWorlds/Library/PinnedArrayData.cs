using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.Library;

public struct PinnedArrayData<T>
{
	private static IntPtr _unmanagedCache;

	private GCHandle _handle;

	public bool Pinned { get; private set; }

	public IntPtr Pointer { get; private set; }

	public T[] Array { get; private set; }

	public T[,] Array2D { get; private set; }

	public GCHandle Handle => _handle;

	static PinnedArrayData()
	{
		_unmanagedCache = Marshal.AllocHGlobal(16384);
	}

	public PinnedArrayData(T[] array, bool manualPinning = false)
	{
		Array = array;
		Array2D = null;
		Pinned = false;
		Pointer = IntPtr.Zero;
		if (array == null)
		{
			return;
		}
		if (!manualPinning)
		{
			try
			{
				_handle = GCHandleFactory.GetHandle();
				_handle.Target = array;
				Pointer = Handle.AddrOfPinnedObject();
				Pinned = true;
			}
			catch (ArgumentException)
			{
				manualPinning = true;
			}
		}
		if (manualPinning)
		{
			Pinned = false;
			int num = Marshal.SizeOf<T>();
			for (int i = 0; i < array.Length; i++)
			{
				Marshal.StructureToPtr(array[i], _unmanagedCache + num * i, fDeleteOld: false);
			}
			Pointer = _unmanagedCache;
		}
	}

	public PinnedArrayData(T[,] array, bool manualPinning = false)
	{
		Array = null;
		Array2D = array;
		Pinned = false;
		Pointer = IntPtr.Zero;
		if (array == null)
		{
			return;
		}
		if (!manualPinning)
		{
			try
			{
				_handle = GCHandleFactory.GetHandle();
				_handle.Target = array;
				Pointer = Handle.AddrOfPinnedObject();
				Pinned = true;
			}
			catch (ArgumentException)
			{
				manualPinning = true;
			}
		}
		if (!manualPinning)
		{
			return;
		}
		Pinned = false;
		int num = Marshal.SizeOf<T>();
		for (int i = 0; i < array.GetLength(0); i++)
		{
			for (int j = 0; j < array.GetLength(1); j++)
			{
				Marshal.StructureToPtr(array[i, j], _unmanagedCache + num * (i * array.GetLength(1) + j), fDeleteOld: false);
			}
		}
		Pointer = _unmanagedCache;
	}

	public static bool CheckIfTypeRequiresManualPinning(Type type)
	{
		bool result = false;
		Array value = System.Array.CreateInstance(type, 10);
		GCHandle gCHandle = default(GCHandle);
		try
		{
			gCHandle = GCHandle.Alloc(value, GCHandleType.Pinned);
			gCHandle.AddrOfPinnedObject();
		}
		catch (ArgumentException)
		{
			result = true;
		}
		if (gCHandle.IsAllocated)
		{
			gCHandle.Free();
		}
		return result;
	}

	public void Dispose()
	{
		if (Pinned)
		{
			if (Array != null)
			{
				_handle.Target = null;
				GCHandleFactory.ReturnHandle(_handle);
				Array = null;
				Pointer = IntPtr.Zero;
			}
			else if (Array2D != null)
			{
				_handle.Target = null;
				GCHandleFactory.ReturnHandle(_handle);
				Array2D = null;
				Pointer = IntPtr.Zero;
			}
		}
	}
}
