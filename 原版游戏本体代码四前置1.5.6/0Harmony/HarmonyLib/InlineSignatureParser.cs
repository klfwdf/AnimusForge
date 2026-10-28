using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Cecil;

namespace HarmonyLib;

internal static class InlineSignatureParser
{
	internal static InlineSignature ImportCallSite(Module moduleFrom, byte[] data)
	{
		InlineSignature inlineSignature = new InlineSignature();
		BinaryReader reader;
		using (MemoryStream input = new MemoryStream(data, writable: false))
		{
			reader = new BinaryReader(input);
			try
			{
				ReadMethodSignature(inlineSignature);
				return inlineSignature;
			}
			finally
			{
				if (reader != null)
				{
					((IDisposable)reader).Dispose();
				}
			}
		}
		Type GetTypeDefOrRef()
		{
			uint num = ReadCompressedUInt();
			uint num2 = num >> 2;
			return moduleFrom.ResolveType((num & 3) switch
			{
				0u => (int)(0x2000000 | num2), 
				1u => (int)(0x1000000 | num2), 
				2u => (int)(0x1B000000 | num2), 
				_ => 0, 
			});
		}
		int ReadCompressedInt()
		{
			byte b = reader.ReadByte();
			reader.BaseStream.Seek(-1L, SeekOrigin.Current);
			int num = (int)ReadCompressedUInt();
			int num2 = num >> 1;
			if ((num & 1) == 0)
			{
				return num2;
			}
			switch (b & 0xC0)
			{
			case 0:
			case 64:
				return num2 - 64;
			case 128:
				return num2 - 8192;
			default:
				return num2 - 268435456;
			}
		}
		uint ReadCompressedUInt()
		{
			byte b = reader.ReadByte();
			if ((b & 0x80) == 0)
			{
				return b;
			}
			if ((b & 0x40) == 0)
			{
				return (uint)(((b & -129) << 8) | reader.ReadByte());
			}
			return (uint)(((b & -193) << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte());
		}
		void ReadMethodSignature(InlineSignature method)
		{
			byte b = reader.ReadByte();
			if ((b & 0x20) != 0)
			{
				method.HasThis = true;
				b = (byte)(b & -33);
			}
			if ((b & 0x40) != 0)
			{
				method.ExplicitThis = true;
				b = (byte)(b & -65);
			}
			method.CallingConvention = (CallingConvention)(b + 1);
			if ((b & 0x10) != 0)
			{
				ReadCompressedUInt();
			}
			uint num = ReadCompressedUInt();
			method.ReturnType = ReadTypeSignature();
			for (int i = 0; i < num; i++)
			{
				method.Parameters.Add(ReadTypeSignature());
			}
		}
		object ReadTypeSignature()
		{
			MetadataType metadataType = (MetadataType)reader.ReadByte();
			switch (metadataType)
			{
			case MetadataType.ValueType:
			case MetadataType.Class:
				return GetTypeDefOrRef();
			case MetadataType.Pointer:
				return ((Type)ReadTypeSignature()).MakePointerType();
			case MetadataType.FunctionPointer:
			{
				InlineSignature inlineSignature2 = new InlineSignature();
				ReadMethodSignature(inlineSignature2);
				return inlineSignature2;
			}
			case MetadataType.ByReference:
				return ((Type)ReadTypeSignature()).MakePointerType();
			case (MetadataType)29:
				return ((Type)ReadTypeSignature()).MakeArrayType();
			case MetadataType.Array:
			{
				Type type2 = (Type)ReadTypeSignature();
				uint rank = ReadCompressedUInt();
				uint num = ReadCompressedUInt();
				for (int num2 = 0; num2 < num; num2++)
				{
					ReadCompressedUInt();
				}
				uint num3 = ReadCompressedUInt();
				for (int num4 = 0; num4 < num3; num4++)
				{
					ReadCompressedInt();
				}
				return type2.MakeArrayType((int)rank);
			}
			case MetadataType.OptionalModifier:
				return new InlineSignature.ModifierType
				{
					IsOptional = true,
					Modifier = GetTypeDefOrRef(),
					Type = ReadTypeSignature()
				};
			case MetadataType.RequiredModifier:
				return new InlineSignature.ModifierType
				{
					IsOptional = false,
					Modifier = GetTypeDefOrRef(),
					Type = ReadTypeSignature()
				};
			case MetadataType.Var:
			case MetadataType.MVar:
				throw new NotSupportedException($"Unsupported generic callsite element: {metadataType}");
			case MetadataType.GenericInstance:
			{
				reader.ReadByte();
				Type type = GetTypeDefOrRef();
				int count = (int)ReadCompressedUInt();
				return type.MakeGenericType((from _ in Enumerable.Range(0, count)
					select (Type)ReadTypeSignature()).ToArray());
			}
			case MetadataType.Object:
				return typeof(object);
			case MetadataType.Void:
				return typeof(void);
			case MetadataType.TypedByReference:
				return typeof(TypedReference);
			case MetadataType.IntPtr:
				return typeof(IntPtr);
			case MetadataType.UIntPtr:
				return typeof(UIntPtr);
			case MetadataType.Boolean:
				return typeof(bool);
			case MetadataType.Char:
				return typeof(char);
			case MetadataType.SByte:
				return typeof(sbyte);
			case MetadataType.Byte:
				return typeof(byte);
			case MetadataType.Int16:
				return typeof(short);
			case MetadataType.UInt16:
				return typeof(ushort);
			case MetadataType.Int32:
				return typeof(int);
			case MetadataType.UInt32:
				return typeof(uint);
			case MetadataType.Int64:
				return typeof(long);
			case MetadataType.UInt64:
				return typeof(ulong);
			case MetadataType.Single:
				return typeof(float);
			case MetadataType.Double:
				return typeof(double);
			case MetadataType.String:
				return typeof(string);
			default:
				throw new NotSupportedException($"Unsupported callsite element: {metadataType}");
			}
		}
	}
}
