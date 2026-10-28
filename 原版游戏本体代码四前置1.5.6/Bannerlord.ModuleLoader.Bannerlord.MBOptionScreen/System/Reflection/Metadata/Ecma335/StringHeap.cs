using System.Diagnostics;
using System.Reflection.Internal;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Metadata.Ecma335;

internal struct StringHeap
{
	private static string[] s_virtualValues;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	private VirtualHeap _lazyVirtualHeap = null;

	internal StringHeap(System.Reflection.Internal.MemoryBlock block, System.Reflection.Metadata.MetadataKind metadataKind)
	{
		if (s_virtualValues == null && metadataKind != System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			s_virtualValues = new string[71]
			{
				"System.Runtime.WindowsRuntime", "System.Runtime", "System.ObjectModel", "System.Runtime.WindowsRuntime.UI.Xaml", "System.Runtime.InteropServices.WindowsRuntime", "System.Numerics.Vectors", "Dispose", "AttributeTargets", "AttributeUsageAttribute", "Color",
				"CornerRadius", "DateTimeOffset", "Duration", "DurationType", "EventHandler`1", "EventRegistrationToken", "Exception", "GeneratorPosition", "GridLength", "GridUnitType",
				"ICommand", "IDictionary`2", "IDisposable", "IEnumerable", "IEnumerable`1", "IList", "IList`1", "INotifyCollectionChanged", "INotifyPropertyChanged", "IReadOnlyDictionary`2",
				"IReadOnlyList`1", "KeyTime", "KeyValuePair`2", "Matrix", "Matrix3D", "Matrix3x2", "Matrix4x4", "NotifyCollectionChangedAction", "NotifyCollectionChangedEventArgs", "NotifyCollectionChangedEventHandler",
				"Nullable`1", "Plane", "Point", "PropertyChangedEventArgs", "PropertyChangedEventHandler", "Quaternion", "Rect", "RepeatBehavior", "RepeatBehaviorType", "Size",
				"System", "System.Collections", "System.Collections.Generic", "System.Collections.Specialized", "System.ComponentModel", "System.Numerics", "System.Windows.Input", "Thickness", "TimeSpan", "Type",
				"Uri", "Vector2", "Vector3", "Vector4", "Windows.Foundation", "Windows.UI", "Windows.UI.Xaml", "Windows.UI.Xaml.Controls.Primitives", "Windows.UI.Xaml.Media", "Windows.UI.Xaml.Media.Animation",
				"Windows.UI.Xaml.Media.Media3D"
			};
		}
		Block = TrimEnd(block);
	}

	[Conditional("DEBUG")]
	private static void AssertFilled()
	{
		for (int i = 0; i < s_virtualValues.Length; i++)
		{
		}
	}

	private static System.Reflection.Internal.MemoryBlock TrimEnd(System.Reflection.Internal.MemoryBlock block)
	{
		if (block.Length == 0)
		{
			return block;
		}
		int num = block.Length - 1;
		while (num >= 0 && block.PeekByte(num) == 0)
		{
			num--;
		}
		if (num == block.Length - 1)
		{
			return block;
		}
		return block.GetMemoryBlockAt(0, num + 2);
	}

	internal string GetString(StringHandle handle, MetadataStringDecoder utf8Decoder)
	{
		if (!handle.IsVirtual)
		{
			return GetNonVirtualString(handle, utf8Decoder, null);
		}
		return GetVirtualHandleString(handle, utf8Decoder);
	}

	internal System.Reflection.Internal.MemoryBlock GetMemoryBlock(StringHandle handle)
	{
		if (!handle.IsVirtual)
		{
			return GetNonVirtualStringMemoryBlock(handle);
		}
		return GetVirtualHandleMemoryBlock(handle);
	}

	internal static string GetVirtualString(StringHandle.VirtualIndex index)
	{
		return s_virtualValues[(int)index];
	}

	private string GetNonVirtualString(StringHandle handle, MetadataStringDecoder utf8Decoder, byte[] prefixOpt)
	{
		char terminator = ((handle.StringKind == System.Reflection.Metadata.Ecma335.StringKind.DotTerminated) ? '.' : '\0');
		int numberOfBytesRead;
		return Block.PeekUtf8NullTerminated(handle.GetHeapOffset(), prefixOpt, utf8Decoder, out numberOfBytesRead, terminator);
	}

	private unsafe System.Reflection.Internal.MemoryBlock GetNonVirtualStringMemoryBlock(StringHandle handle)
	{
		char terminator = ((handle.StringKind == System.Reflection.Metadata.Ecma335.StringKind.DotTerminated) ? '.' : '\0');
		int heapOffset = handle.GetHeapOffset();
		int utf8NullTerminatedLength = Block.GetUtf8NullTerminatedLength(heapOffset, out var _, terminator);
		return new System.Reflection.Internal.MemoryBlock(Block.Pointer + heapOffset, utf8NullTerminatedLength);
	}

	private unsafe byte[] GetNonVirtualStringBytes(StringHandle handle, byte[] prefix)
	{
		System.Reflection.Internal.MemoryBlock nonVirtualStringMemoryBlock = GetNonVirtualStringMemoryBlock(handle);
		byte[] array = new byte[prefix.Length + nonVirtualStringMemoryBlock.Length];
		Buffer.BlockCopy(prefix, 0, array, 0, prefix.Length);
		Marshal.Copy((IntPtr)nonVirtualStringMemoryBlock.Pointer, array, prefix.Length, nonVirtualStringMemoryBlock.Length);
		return array;
	}

	private string GetVirtualHandleString(StringHandle handle, MetadataStringDecoder utf8Decoder)
	{
		return handle.StringKind switch
		{
			System.Reflection.Metadata.Ecma335.StringKind.Virtual => GetVirtualString(handle.GetVirtualIndex()), 
			System.Reflection.Metadata.Ecma335.StringKind.WinRTPrefixed => GetNonVirtualString(handle, utf8Decoder, System.Reflection.Metadata.MetadataReader.WinRTPrefix), 
			_ => throw ExceptionUtilities.UnexpectedValue(handle.StringKind), 
		};
	}

	private System.Reflection.Internal.MemoryBlock GetVirtualHandleMemoryBlock(StringHandle handle)
	{
		VirtualHeap orCreateVirtualHeap = VirtualHeap.GetOrCreateVirtualHeap(ref _lazyVirtualHeap);
		lock (orCreateVirtualHeap)
		{
			if (!orCreateVirtualHeap.TryGetMemoryBlock(handle.RawValue, out var block))
			{
				byte[] value = handle.StringKind switch
				{
					System.Reflection.Metadata.Ecma335.StringKind.Virtual => Encoding.UTF8.GetBytes(GetVirtualString(handle.GetVirtualIndex())), 
					System.Reflection.Metadata.Ecma335.StringKind.WinRTPrefixed => GetNonVirtualStringBytes(handle, System.Reflection.Metadata.MetadataReader.WinRTPrefix), 
					_ => throw ExceptionUtilities.UnexpectedValue(handle.StringKind), 
				};
				return orCreateVirtualHeap.AddBlob(handle.RawValue, value);
			}
			return block;
		}
	}

	internal System.Reflection.Metadata.BlobReader GetBlobReader(StringHandle handle)
	{
		return new System.Reflection.Metadata.BlobReader(GetMemoryBlock(handle));
	}

	internal StringHandle GetNextHandle(StringHandle handle)
	{
		if (handle.IsVirtual)
		{
			return default(StringHandle);
		}
		int num = Block.IndexOf(0, handle.GetHeapOffset());
		if (num == -1 || num == Block.Length - 1)
		{
			return default(StringHandle);
		}
		return StringHandle.FromOffset(num + 1);
	}

	internal bool Equals(StringHandle handle, string value, MetadataStringDecoder utf8Decoder, bool ignoreCase)
	{
		if (handle.IsVirtual)
		{
			return string.Equals(GetString(handle, utf8Decoder), value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}
		if (handle.IsNil)
		{
			return value.Length == 0;
		}
		char terminator = ((handle.StringKind == System.Reflection.Metadata.Ecma335.StringKind.DotTerminated) ? '.' : '\0');
		return Block.Utf8NullTerminatedEquals(handle.GetHeapOffset(), value, utf8Decoder, terminator, ignoreCase);
	}

	internal bool StartsWith(StringHandle handle, string value, MetadataStringDecoder utf8Decoder, bool ignoreCase)
	{
		if (handle.IsVirtual)
		{
			return GetString(handle, utf8Decoder).StartsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}
		if (handle.IsNil)
		{
			return value.Length == 0;
		}
		char terminator = ((handle.StringKind == System.Reflection.Metadata.Ecma335.StringKind.DotTerminated) ? '.' : '\0');
		return Block.Utf8NullTerminatedStartsWith(handle.GetHeapOffset(), value, utf8Decoder, terminator, ignoreCase);
	}

	internal bool EqualsRaw(StringHandle rawHandle, string asciiString)
	{
		return Block.CompareUtf8NullTerminatedStringWithAsciiString(rawHandle.GetHeapOffset(), asciiString) == 0;
	}

	internal int IndexOfRaw(int startIndex, char asciiChar)
	{
		return Block.Utf8NullTerminatedOffsetOfAsciiChar(startIndex, asciiChar);
	}

	internal bool StartsWithRaw(StringHandle rawHandle, string asciiPrefix)
	{
		return Block.Utf8NullTerminatedStringStartsWithAsciiPrefix(rawHandle.GetHeapOffset(), asciiPrefix);
	}

	internal int BinarySearchRaw(string[] asciiKeys, StringHandle rawHandle)
	{
		return Block.BinarySearch(asciiKeys, rawHandle.GetHeapOffset());
	}
}
