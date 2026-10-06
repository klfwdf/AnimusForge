using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet;

[EngineClass("ftdnNative_array")]
public sealed class NativeArray : NativeObject
{
	private static readonly IntPtr _temporaryData;

	private const int TemporaryDataSize = 16384;

	private static readonly int DataPointerOffset;

	public int DataSize => LibraryApplicationInterface.INativeArray.GetDataSize(base.Pointer);

	static NativeArray()
	{
		_temporaryData = Marshal.AllocHGlobal(16384);
		DataPointerOffset = LibraryApplicationInterface.INativeArray.GetDataPointerOffset();
	}

	internal NativeArray(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public static NativeArray Create()
	{
		return LibraryApplicationInterface.INativeArray.Create();
	}

	private UIntPtr GetDataPointer()
	{
		return LibraryApplicationInterface.INativeArray.GetDataPointer(base.Pointer);
	}

	public int GetLength<T>() where T : struct
	{
		int dataSize = DataSize;
		int num = Marshal.SizeOf<T>();
		return dataSize / num;
	}

	public T GetElementAt<T>(int index) where T : struct
	{
		IntPtr intPtr = Marshal.PtrToStructure<IntPtr>(new IntPtr((long)base.Pointer.ToUInt64() + (long)DataPointerOffset));
		int num = Marshal.SizeOf<T>();
		return Marshal.PtrToStructure<T>(new IntPtr(intPtr.ToInt64() + index * num));
	}

	public IEnumerable<T> GetEnumerator<T>() where T : struct
	{
		int length = GetLength<T>();
		IntPtr ptr = new IntPtr((long)base.Pointer.ToUInt64() + (long)DataPointerOffset);
		IntPtr dataPointer = Marshal.PtrToStructure<IntPtr>(ptr);
		int elementSize = Marshal.SizeOf<T>();
		for (int i = 0; i < length; i++)
		{
			yield return Marshal.PtrToStructure<T>(new IntPtr(dataPointer.ToInt64() + i * elementSize));
		}
	}

	public T[] ToArray<T>() where T : struct
	{
		T[] array = new T[GetLength<T>()];
		IEnumerable<T> enumerator = GetEnumerator<T>();
		int num = 0;
		foreach (T item in enumerator)
		{
			array[num] = item;
			num++;
		}
		return array;
	}

	public void AddElement(int value)
	{
		LibraryApplicationInterface.INativeArray.AddIntegerElement(base.Pointer, value);
	}

	public void AddElement(float value)
	{
		LibraryApplicationInterface.INativeArray.AddFloatElement(base.Pointer, value);
	}

	public void AddElement<T>(T value) where T : struct
	{
		int elementSize = Marshal.SizeOf<T>();
		Marshal.StructureToPtr(value, _temporaryData, fDeleteOld: false);
		LibraryApplicationInterface.INativeArray.AddElement(base.Pointer, _temporaryData, elementSize);
	}

	public void Clear()
	{
		LibraryApplicationInterface.INativeArray.Clear(base.Pointer);
	}
}
