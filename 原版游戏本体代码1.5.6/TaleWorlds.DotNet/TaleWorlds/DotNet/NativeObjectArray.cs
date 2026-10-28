using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.DotNet;

[EngineClass("ftdnNative_object_array")]
public sealed class NativeObjectArray : NativeObject, IEnumerable<NativeObject>, IEnumerable
{
	public int Count => LibraryApplicationInterface.INativeObjectArray.GetCount(base.Pointer);

	internal NativeObjectArray(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public static NativeObjectArray Create()
	{
		return LibraryApplicationInterface.INativeObjectArray.Create();
	}

	public NativeObject GetElementAt(int index)
	{
		return LibraryApplicationInterface.INativeObjectArray.GetElementAtIndex(base.Pointer, index);
	}

	public void AddElement(NativeObject nativeObject)
	{
		LibraryApplicationInterface.INativeObjectArray.AddElement(base.Pointer, (nativeObject != null) ? nativeObject.Pointer : UIntPtr.Zero);
	}

	public void Clear()
	{
		LibraryApplicationInterface.INativeObjectArray.Clear(base.Pointer);
	}

	IEnumerator<NativeObject> IEnumerable<NativeObject>.GetEnumerator()
	{
		int count = Count;
		for (int i = 0; i < count; i++)
		{
			yield return GetElementAt(i);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		int count = Count;
		for (int i = 0; i < count; i++)
		{
			yield return GetElementAt(i);
		}
	}
}
