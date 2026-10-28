using System.Globalization;
using System.Numerics.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Numerics;

[Intrinsic]
internal struct Vector<T> : IEquatable<Vector<T>>, IFormattable where T : struct
{
	private struct VectorSizeHelper
	{
		internal Vector<T> _placeholder;

		internal byte _byte;
	}

	private Register register;

	private static readonly int s_count = InitializeCount();

	private static readonly Vector<T> s_zero = default(Vector<T>);

	private static readonly Vector<T> s_one = new Vector<T>(GetOneValue());

	private static readonly Vector<T> s_allOnes = new Vector<T>(GetAllBitsSetValue());

	public static int Count
	{
		[Intrinsic]
		get
		{
			return s_count;
		}
	}

	public static Vector<T> Zero
	{
		[Intrinsic]
		get
		{
			return s_zero;
		}
	}

	public static Vector<T> One
	{
		[Intrinsic]
		get
		{
			return s_one;
		}
	}

	internal static Vector<T> AllOnes => s_allOnes;

	public unsafe T this[int index]
	{
		[Intrinsic]
		get
		{
			if (index >= Count || index < 0)
			{
				throw new IndexOutOfRangeException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Format(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_ArgumentOutOfRangeException, index));
			}
			if (typeof(T) == typeof(byte))
			{
				fixed (byte* byte_ = &register.byte_0)
				{
					return (T)(object)byte_[index];
				}
			}
			if (typeof(T) == typeof(sbyte))
			{
				fixed (sbyte* sbyte_ = &register.sbyte_0)
				{
					return (T)(object)sbyte_[index];
				}
			}
			if (typeof(T) == typeof(ushort))
			{
				fixed (ushort* uint16_ = &register.uint16_0)
				{
					return (T)(object)uint16_[index];
				}
			}
			if (typeof(T) == typeof(short))
			{
				fixed (short* int16_ = &register.int16_0)
				{
					return (T)(object)int16_[index];
				}
			}
			if (typeof(T) == typeof(uint))
			{
				fixed (uint* uint32_ = &register.uint32_0)
				{
					return (T)(object)uint32_[index];
				}
			}
			if (typeof(T) == typeof(int))
			{
				fixed (int* int32_ = &register.int32_0)
				{
					return (T)(object)int32_[index];
				}
			}
			if (typeof(T) == typeof(ulong))
			{
				fixed (ulong* uint64_ = &register.uint64_0)
				{
					return (T)(object)uint64_[index];
				}
			}
			if (typeof(T) == typeof(long))
			{
				fixed (long* int64_ = &register.int64_0)
				{
					return (T)(object)int64_[index];
				}
			}
			if (typeof(T) == typeof(float))
			{
				fixed (float* single_ = &register.single_0)
				{
					return (T)(object)single_[index];
				}
			}
			if (typeof(T) == typeof(double))
			{
				fixed (double* double_ = &register.double_0)
				{
					return (T)(object)double_[index];
				}
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
	}

	private unsafe static int InitializeCount()
	{
		VectorSizeHelper vectorSizeHelper = default(VectorSizeHelper);
		byte* ptr = &vectorSizeHelper._placeholder.register.byte_0;
		byte* ptr2 = &vectorSizeHelper._byte;
		int num = (int)(ptr2 - ptr);
		int num2 = -1;
		if (typeof(T) == typeof(byte))
		{
			num2 = 1;
		}
		else if (typeof(T) == typeof(sbyte))
		{
			num2 = 1;
		}
		else if (typeof(T) == typeof(ushort))
		{
			num2 = 2;
		}
		else if (typeof(T) == typeof(short))
		{
			num2 = 2;
		}
		else if (typeof(T) == typeof(uint))
		{
			num2 = 4;
		}
		else if (typeof(T) == typeof(int))
		{
			num2 = 4;
		}
		else if (typeof(T) == typeof(ulong))
		{
			num2 = 8;
		}
		else if (typeof(T) == typeof(long))
		{
			num2 = 8;
		}
		else if (typeof(T) == typeof(float))
		{
			num2 = 4;
		}
		else
		{
			if (!(typeof(T) == typeof(double)))
			{
				throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
			}
			num2 = 8;
		}
		return num / num2;
	}

	[Intrinsic]
	public unsafe Vector(T value)
	{
		this = default(Vector<T>);
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				fixed (byte* byte_ = &register.byte_0)
				{
					for (int i = 0; i < Count; i++)
					{
						byte_[i] = (byte)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(sbyte))
			{
				fixed (sbyte* sbyte_ = &register.sbyte_0)
				{
					for (int j = 0; j < Count; j++)
					{
						sbyte_[j] = (sbyte)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(ushort))
			{
				fixed (ushort* uint16_ = &register.uint16_0)
				{
					for (int k = 0; k < Count; k++)
					{
						uint16_[k] = (ushort)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(short))
			{
				fixed (short* int16_ = &register.int16_0)
				{
					for (int l = 0; l < Count; l++)
					{
						int16_[l] = (short)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(uint))
			{
				fixed (uint* uint32_ = &register.uint32_0)
				{
					for (int m = 0; m < Count; m++)
					{
						uint32_[m] = (uint)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(int))
			{
				fixed (int* int32_ = &register.int32_0)
				{
					for (int n = 0; n < Count; n++)
					{
						int32_[n] = (int)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(ulong))
			{
				fixed (ulong* uint64_ = &register.uint64_0)
				{
					for (int num = 0; num < Count; num++)
					{
						uint64_[num] = (ulong)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(long))
			{
				fixed (long* int64_ = &register.int64_0)
				{
					for (int num2 = 0; num2 < Count; num2++)
					{
						int64_[num2] = (long)(object)value;
					}
				}
			}
			else if (typeof(T) == typeof(float))
			{
				fixed (float* single_ = &register.single_0)
				{
					for (int num3 = 0; num3 < Count; num3++)
					{
						single_[num3] = (float)(object)value;
					}
				}
			}
			else
			{
				if (!(typeof(T) == typeof(double)))
				{
					return;
				}
				fixed (double* double_ = &register.double_0)
				{
					for (int num4 = 0; num4 < Count; num4++)
					{
						double_[num4] = (double)(object)value;
					}
				}
			}
		}
		else if (typeof(T) == typeof(byte))
		{
			register.byte_0 = (byte)(object)value;
			register.byte_1 = (byte)(object)value;
			register.byte_2 = (byte)(object)value;
			register.byte_3 = (byte)(object)value;
			register.byte_4 = (byte)(object)value;
			register.byte_5 = (byte)(object)value;
			register.byte_6 = (byte)(object)value;
			register.byte_7 = (byte)(object)value;
			register.byte_8 = (byte)(object)value;
			register.byte_9 = (byte)(object)value;
			register.byte_10 = (byte)(object)value;
			register.byte_11 = (byte)(object)value;
			register.byte_12 = (byte)(object)value;
			register.byte_13 = (byte)(object)value;
			register.byte_14 = (byte)(object)value;
			register.byte_15 = (byte)(object)value;
		}
		else if (typeof(T) == typeof(sbyte))
		{
			register.sbyte_0 = (sbyte)(object)value;
			register.sbyte_1 = (sbyte)(object)value;
			register.sbyte_2 = (sbyte)(object)value;
			register.sbyte_3 = (sbyte)(object)value;
			register.sbyte_4 = (sbyte)(object)value;
			register.sbyte_5 = (sbyte)(object)value;
			register.sbyte_6 = (sbyte)(object)value;
			register.sbyte_7 = (sbyte)(object)value;
			register.sbyte_8 = (sbyte)(object)value;
			register.sbyte_9 = (sbyte)(object)value;
			register.sbyte_10 = (sbyte)(object)value;
			register.sbyte_11 = (sbyte)(object)value;
			register.sbyte_12 = (sbyte)(object)value;
			register.sbyte_13 = (sbyte)(object)value;
			register.sbyte_14 = (sbyte)(object)value;
			register.sbyte_15 = (sbyte)(object)value;
		}
		else if (typeof(T) == typeof(ushort))
		{
			register.uint16_0 = (ushort)(object)value;
			register.uint16_1 = (ushort)(object)value;
			register.uint16_2 = (ushort)(object)value;
			register.uint16_3 = (ushort)(object)value;
			register.uint16_4 = (ushort)(object)value;
			register.uint16_5 = (ushort)(object)value;
			register.uint16_6 = (ushort)(object)value;
			register.uint16_7 = (ushort)(object)value;
		}
		else if (typeof(T) == typeof(short))
		{
			register.int16_0 = (short)(object)value;
			register.int16_1 = (short)(object)value;
			register.int16_2 = (short)(object)value;
			register.int16_3 = (short)(object)value;
			register.int16_4 = (short)(object)value;
			register.int16_5 = (short)(object)value;
			register.int16_6 = (short)(object)value;
			register.int16_7 = (short)(object)value;
		}
		else if (typeof(T) == typeof(uint))
		{
			register.uint32_0 = (uint)(object)value;
			register.uint32_1 = (uint)(object)value;
			register.uint32_2 = (uint)(object)value;
			register.uint32_3 = (uint)(object)value;
		}
		else if (typeof(T) == typeof(int))
		{
			register.int32_0 = (int)(object)value;
			register.int32_1 = (int)(object)value;
			register.int32_2 = (int)(object)value;
			register.int32_3 = (int)(object)value;
		}
		else if (typeof(T) == typeof(ulong))
		{
			register.uint64_0 = (ulong)(object)value;
			register.uint64_1 = (ulong)(object)value;
		}
		else if (typeof(T) == typeof(long))
		{
			register.int64_0 = (long)(object)value;
			register.int64_1 = (long)(object)value;
		}
		else if (typeof(T) == typeof(float))
		{
			register.single_0 = (float)(object)value;
			register.single_1 = (float)(object)value;
			register.single_2 = (float)(object)value;
			register.single_3 = (float)(object)value;
		}
		else if (typeof(T) == typeof(double))
		{
			register.double_0 = (double)(object)value;
			register.double_1 = (double)(object)value;
		}
	}

	[Intrinsic]
	public Vector(T[] values)
		: this(values, 0)
	{
	}

	public unsafe Vector(T[] values, int index)
	{
		this = default(Vector<T>);
		if (values == null)
		{
			throw new NullReferenceException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_NullArgumentNullRef);
		}
		if (index < 0 || values.Length - index < Count)
		{
			throw new IndexOutOfRangeException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Format(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_InsufficientNumberOfElements, Count, "values"));
		}
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				fixed (byte* byte_ = &register.byte_0)
				{
					for (int i = 0; i < Count; i++)
					{
						byte_[i] = (byte)(object)values[i + index];
					}
				}
			}
			else if (typeof(T) == typeof(sbyte))
			{
				fixed (sbyte* sbyte_ = &register.sbyte_0)
				{
					for (int j = 0; j < Count; j++)
					{
						sbyte_[j] = (sbyte)(object)values[j + index];
					}
				}
			}
			else if (typeof(T) == typeof(ushort))
			{
				fixed (ushort* uint16_ = &register.uint16_0)
				{
					for (int k = 0; k < Count; k++)
					{
						uint16_[k] = (ushort)(object)values[k + index];
					}
				}
			}
			else if (typeof(T) == typeof(short))
			{
				fixed (short* int16_ = &register.int16_0)
				{
					for (int l = 0; l < Count; l++)
					{
						int16_[l] = (short)(object)values[l + index];
					}
				}
			}
			else if (typeof(T) == typeof(uint))
			{
				fixed (uint* uint32_ = &register.uint32_0)
				{
					for (int m = 0; m < Count; m++)
					{
						uint32_[m] = (uint)(object)values[m + index];
					}
				}
			}
			else if (typeof(T) == typeof(int))
			{
				fixed (int* int32_ = &register.int32_0)
				{
					for (int n = 0; n < Count; n++)
					{
						int32_[n] = (int)(object)values[n + index];
					}
				}
			}
			else if (typeof(T) == typeof(ulong))
			{
				fixed (ulong* uint64_ = &register.uint64_0)
				{
					for (int num = 0; num < Count; num++)
					{
						uint64_[num] = (ulong)(object)values[num + index];
					}
				}
			}
			else if (typeof(T) == typeof(long))
			{
				fixed (long* int64_ = &register.int64_0)
				{
					for (int num2 = 0; num2 < Count; num2++)
					{
						int64_[num2] = (long)(object)values[num2 + index];
					}
				}
			}
			else if (typeof(T) == typeof(float))
			{
				fixed (float* single_ = &register.single_0)
				{
					for (int num3 = 0; num3 < Count; num3++)
					{
						single_[num3] = (float)(object)values[num3 + index];
					}
				}
			}
			else
			{
				if (!(typeof(T) == typeof(double)))
				{
					return;
				}
				fixed (double* double_ = &register.double_0)
				{
					for (int num4 = 0; num4 < Count; num4++)
					{
						double_[num4] = (double)(object)values[num4 + index];
					}
				}
			}
		}
		else if (typeof(T) == typeof(byte))
		{
			fixed (byte* byte_2 = &register.byte_0)
			{
				*byte_2 = (byte)(object)values[index];
				byte_2[1] = (byte)(object)values[1 + index];
				byte_2[2] = (byte)(object)values[2 + index];
				byte_2[3] = (byte)(object)values[3 + index];
				byte_2[4] = (byte)(object)values[4 + index];
				byte_2[5] = (byte)(object)values[5 + index];
				byte_2[6] = (byte)(object)values[6 + index];
				byte_2[7] = (byte)(object)values[7 + index];
				byte_2[8] = (byte)(object)values[8 + index];
				byte_2[9] = (byte)(object)values[9 + index];
				byte_2[10] = (byte)(object)values[10 + index];
				byte_2[11] = (byte)(object)values[11 + index];
				byte_2[12] = (byte)(object)values[12 + index];
				byte_2[13] = (byte)(object)values[13 + index];
				byte_2[14] = (byte)(object)values[14 + index];
				byte_2[15] = (byte)(object)values[15 + index];
			}
		}
		else if (typeof(T) == typeof(sbyte))
		{
			fixed (sbyte* sbyte_2 = &register.sbyte_0)
			{
				*sbyte_2 = (sbyte)(object)values[index];
				sbyte_2[1] = (sbyte)(object)values[1 + index];
				sbyte_2[2] = (sbyte)(object)values[2 + index];
				sbyte_2[3] = (sbyte)(object)values[3 + index];
				sbyte_2[4] = (sbyte)(object)values[4 + index];
				sbyte_2[5] = (sbyte)(object)values[5 + index];
				sbyte_2[6] = (sbyte)(object)values[6 + index];
				sbyte_2[7] = (sbyte)(object)values[7 + index];
				sbyte_2[8] = (sbyte)(object)values[8 + index];
				sbyte_2[9] = (sbyte)(object)values[9 + index];
				sbyte_2[10] = (sbyte)(object)values[10 + index];
				sbyte_2[11] = (sbyte)(object)values[11 + index];
				sbyte_2[12] = (sbyte)(object)values[12 + index];
				sbyte_2[13] = (sbyte)(object)values[13 + index];
				sbyte_2[14] = (sbyte)(object)values[14 + index];
				sbyte_2[15] = (sbyte)(object)values[15 + index];
			}
		}
		else if (typeof(T) == typeof(ushort))
		{
			fixed (ushort* uint16_2 = &register.uint16_0)
			{
				*uint16_2 = (ushort)(object)values[index];
				uint16_2[1] = (ushort)(object)values[1 + index];
				uint16_2[2] = (ushort)(object)values[2 + index];
				uint16_2[3] = (ushort)(object)values[3 + index];
				uint16_2[4] = (ushort)(object)values[4 + index];
				uint16_2[5] = (ushort)(object)values[5 + index];
				uint16_2[6] = (ushort)(object)values[6 + index];
				uint16_2[7] = (ushort)(object)values[7 + index];
			}
		}
		else if (typeof(T) == typeof(short))
		{
			fixed (short* int16_2 = &register.int16_0)
			{
				*int16_2 = (short)(object)values[index];
				int16_2[1] = (short)(object)values[1 + index];
				int16_2[2] = (short)(object)values[2 + index];
				int16_2[3] = (short)(object)values[3 + index];
				int16_2[4] = (short)(object)values[4 + index];
				int16_2[5] = (short)(object)values[5 + index];
				int16_2[6] = (short)(object)values[6 + index];
				int16_2[7] = (short)(object)values[7 + index];
			}
		}
		else if (typeof(T) == typeof(uint))
		{
			fixed (uint* uint32_2 = &register.uint32_0)
			{
				*uint32_2 = (uint)(object)values[index];
				uint32_2[1] = (uint)(object)values[1 + index];
				uint32_2[2] = (uint)(object)values[2 + index];
				uint32_2[3] = (uint)(object)values[3 + index];
			}
		}
		else if (typeof(T) == typeof(int))
		{
			fixed (int* int32_2 = &register.int32_0)
			{
				*int32_2 = (int)(object)values[index];
				int32_2[1] = (int)(object)values[1 + index];
				int32_2[2] = (int)(object)values[2 + index];
				int32_2[3] = (int)(object)values[3 + index];
			}
		}
		else if (typeof(T) == typeof(ulong))
		{
			fixed (ulong* uint64_2 = &register.uint64_0)
			{
				*uint64_2 = (ulong)(object)values[index];
				uint64_2[1] = (ulong)(object)values[1 + index];
			}
		}
		else if (typeof(T) == typeof(long))
		{
			fixed (long* int64_2 = &register.int64_0)
			{
				*int64_2 = (long)(object)values[index];
				int64_2[1] = (long)(object)values[1 + index];
			}
		}
		else if (typeof(T) == typeof(float))
		{
			fixed (float* single_2 = &register.single_0)
			{
				*single_2 = (float)(object)values[index];
				single_2[1] = (float)(object)values[1 + index];
				single_2[2] = (float)(object)values[2 + index];
				single_2[3] = (float)(object)values[3 + index];
			}
		}
		else if (typeof(T) == typeof(double))
		{
			fixed (double* double_2 = &register.double_0)
			{
				*double_2 = (double)(object)values[index];
				double_2[1] = (double)(object)values[1 + index];
			}
		}
	}

	internal unsafe Vector(void* dataPointer)
		: this(dataPointer, 0)
	{
	}

	internal unsafe Vector(void* dataPointer, int offset)
	{
		this = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			byte* ptr = (byte*)dataPointer;
			ptr += offset;
			fixed (byte* byte_ = &register.byte_0)
			{
				for (int i = 0; i < Count; i++)
				{
					byte_[i] = ptr[i];
				}
			}
			return;
		}
		if (typeof(T) == typeof(sbyte))
		{
			sbyte* ptr2 = (sbyte*)dataPointer;
			ptr2 += offset;
			fixed (sbyte* sbyte_ = &register.sbyte_0)
			{
				for (int j = 0; j < Count; j++)
				{
					sbyte_[j] = ptr2[j];
				}
			}
			return;
		}
		if (typeof(T) == typeof(ushort))
		{
			ushort* ptr3 = (ushort*)dataPointer;
			ptr3 += offset;
			fixed (ushort* uint16_ = &register.uint16_0)
			{
				for (int k = 0; k < Count; k++)
				{
					uint16_[k] = ptr3[k];
				}
			}
			return;
		}
		if (typeof(T) == typeof(short))
		{
			short* ptr4 = (short*)dataPointer;
			ptr4 += offset;
			fixed (short* int16_ = &register.int16_0)
			{
				for (int l = 0; l < Count; l++)
				{
					int16_[l] = ptr4[l];
				}
			}
			return;
		}
		if (typeof(T) == typeof(uint))
		{
			uint* ptr5 = (uint*)dataPointer;
			ptr5 += offset;
			fixed (uint* uint32_ = &register.uint32_0)
			{
				for (int m = 0; m < Count; m++)
				{
					uint32_[m] = ptr5[m];
				}
			}
			return;
		}
		if (typeof(T) == typeof(int))
		{
			int* ptr6 = (int*)dataPointer;
			ptr6 += offset;
			fixed (int* int32_ = &register.int32_0)
			{
				for (int n = 0; n < Count; n++)
				{
					int32_[n] = ptr6[n];
				}
			}
			return;
		}
		if (typeof(T) == typeof(ulong))
		{
			ulong* ptr7 = (ulong*)dataPointer;
			ptr7 += offset;
			fixed (ulong* uint64_ = &register.uint64_0)
			{
				for (int num = 0; num < Count; num++)
				{
					uint64_[num] = ptr7[num];
				}
			}
			return;
		}
		if (typeof(T) == typeof(long))
		{
			long* ptr8 = (long*)dataPointer;
			ptr8 += offset;
			fixed (long* int64_ = &register.int64_0)
			{
				for (int num2 = 0; num2 < Count; num2++)
				{
					int64_[num2] = ptr8[num2];
				}
			}
			return;
		}
		if (typeof(T) == typeof(float))
		{
			float* ptr9 = (float*)dataPointer;
			ptr9 += offset;
			fixed (float* single_ = &register.single_0)
			{
				for (int num3 = 0; num3 < Count; num3++)
				{
					single_[num3] = ptr9[num3];
				}
			}
			return;
		}
		if (typeof(T) == typeof(double))
		{
			double* ptr10 = (double*)dataPointer;
			ptr10 += offset;
			fixed (double* double_ = &register.double_0)
			{
				for (int num4 = 0; num4 < Count; num4++)
				{
					double_[num4] = ptr10[num4];
				}
			}
			return;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	private Vector(ref Register existingRegister)
	{
		register = existingRegister;
	}

	[Intrinsic]
	public void CopyTo(T[] destination)
	{
		CopyTo(destination, 0);
	}

	[Intrinsic]
	public unsafe void CopyTo(T[] destination, int startIndex)
	{
		if (destination == null)
		{
			throw new NullReferenceException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_NullArgumentNullRef);
		}
		if (startIndex < 0 || startIndex >= destination.Length)
		{
			throw new ArgumentOutOfRangeException("startIndex", _003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Format(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_ArgumentOutOfRangeException, startIndex));
		}
		if (destination.Length - startIndex < Count)
		{
			throw new ArgumentException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Format(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_ElementsInSourceIsGreaterThanDestination, startIndex));
		}
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				fixed (byte* ptr = (byte[])(object)destination)
				{
					for (int i = 0; i < Count; i++)
					{
						ptr[startIndex + i] = (byte)(object)this[i];
					}
				}
			}
			else if (typeof(T) == typeof(sbyte))
			{
				fixed (sbyte* ptr2 = (sbyte[])(object)destination)
				{
					for (int j = 0; j < Count; j++)
					{
						ptr2[startIndex + j] = (sbyte)(object)this[j];
					}
				}
			}
			else if (typeof(T) == typeof(ushort))
			{
				fixed (ushort* ptr3 = (ushort[])(object)destination)
				{
					for (int k = 0; k < Count; k++)
					{
						ptr3[startIndex + k] = (ushort)(object)this[k];
					}
				}
			}
			else if (typeof(T) == typeof(short))
			{
				fixed (short* ptr4 = (short[])(object)destination)
				{
					for (int l = 0; l < Count; l++)
					{
						ptr4[startIndex + l] = (short)(object)this[l];
					}
				}
			}
			else if (typeof(T) == typeof(uint))
			{
				fixed (uint* ptr5 = (uint[])(object)destination)
				{
					for (int m = 0; m < Count; m++)
					{
						ptr5[startIndex + m] = (uint)(object)this[m];
					}
				}
			}
			else if (typeof(T) == typeof(int))
			{
				fixed (int* ptr6 = (int[])(object)destination)
				{
					for (int n = 0; n < Count; n++)
					{
						ptr6[startIndex + n] = (int)(object)this[n];
					}
				}
			}
			else if (typeof(T) == typeof(ulong))
			{
				fixed (ulong* ptr7 = (ulong[])(object)destination)
				{
					for (int num = 0; num < Count; num++)
					{
						ptr7[startIndex + num] = (ulong)(object)this[num];
					}
				}
			}
			else if (typeof(T) == typeof(long))
			{
				fixed (long* ptr8 = (long[])(object)destination)
				{
					for (int num2 = 0; num2 < Count; num2++)
					{
						ptr8[startIndex + num2] = (long)(object)this[num2];
					}
				}
			}
			else if (typeof(T) == typeof(float))
			{
				fixed (float* ptr9 = (float[])(object)destination)
				{
					for (int num3 = 0; num3 < Count; num3++)
					{
						ptr9[startIndex + num3] = (float)(object)this[num3];
					}
				}
			}
			else
			{
				if (!(typeof(T) == typeof(double)))
				{
					return;
				}
				fixed (double* ptr10 = (double[])(object)destination)
				{
					for (int num4 = 0; num4 < Count; num4++)
					{
						ptr10[startIndex + num4] = (double)(object)this[num4];
					}
				}
			}
		}
		else if (typeof(T) == typeof(byte))
		{
			fixed (byte* ptr11 = (byte[])(object)destination)
			{
				ptr11[startIndex] = register.byte_0;
				ptr11[startIndex + 1] = register.byte_1;
				ptr11[startIndex + 2] = register.byte_2;
				ptr11[startIndex + 3] = register.byte_3;
				ptr11[startIndex + 4] = register.byte_4;
				ptr11[startIndex + 5] = register.byte_5;
				ptr11[startIndex + 6] = register.byte_6;
				ptr11[startIndex + 7] = register.byte_7;
				ptr11[startIndex + 8] = register.byte_8;
				ptr11[startIndex + 9] = register.byte_9;
				ptr11[startIndex + 10] = register.byte_10;
				ptr11[startIndex + 11] = register.byte_11;
				ptr11[startIndex + 12] = register.byte_12;
				ptr11[startIndex + 13] = register.byte_13;
				ptr11[startIndex + 14] = register.byte_14;
				ptr11[startIndex + 15] = register.byte_15;
			}
		}
		else if (typeof(T) == typeof(sbyte))
		{
			fixed (sbyte* ptr12 = (sbyte[])(object)destination)
			{
				ptr12[startIndex] = register.sbyte_0;
				ptr12[startIndex + 1] = register.sbyte_1;
				ptr12[startIndex + 2] = register.sbyte_2;
				ptr12[startIndex + 3] = register.sbyte_3;
				ptr12[startIndex + 4] = register.sbyte_4;
				ptr12[startIndex + 5] = register.sbyte_5;
				ptr12[startIndex + 6] = register.sbyte_6;
				ptr12[startIndex + 7] = register.sbyte_7;
				ptr12[startIndex + 8] = register.sbyte_8;
				ptr12[startIndex + 9] = register.sbyte_9;
				ptr12[startIndex + 10] = register.sbyte_10;
				ptr12[startIndex + 11] = register.sbyte_11;
				ptr12[startIndex + 12] = register.sbyte_12;
				ptr12[startIndex + 13] = register.sbyte_13;
				ptr12[startIndex + 14] = register.sbyte_14;
				ptr12[startIndex + 15] = register.sbyte_15;
			}
		}
		else if (typeof(T) == typeof(ushort))
		{
			fixed (ushort* ptr13 = (ushort[])(object)destination)
			{
				ptr13[startIndex] = register.uint16_0;
				ptr13[startIndex + 1] = register.uint16_1;
				ptr13[startIndex + 2] = register.uint16_2;
				ptr13[startIndex + 3] = register.uint16_3;
				ptr13[startIndex + 4] = register.uint16_4;
				ptr13[startIndex + 5] = register.uint16_5;
				ptr13[startIndex + 6] = register.uint16_6;
				ptr13[startIndex + 7] = register.uint16_7;
			}
		}
		else if (typeof(T) == typeof(short))
		{
			fixed (short* ptr14 = (short[])(object)destination)
			{
				ptr14[startIndex] = register.int16_0;
				ptr14[startIndex + 1] = register.int16_1;
				ptr14[startIndex + 2] = register.int16_2;
				ptr14[startIndex + 3] = register.int16_3;
				ptr14[startIndex + 4] = register.int16_4;
				ptr14[startIndex + 5] = register.int16_5;
				ptr14[startIndex + 6] = register.int16_6;
				ptr14[startIndex + 7] = register.int16_7;
			}
		}
		else if (typeof(T) == typeof(uint))
		{
			fixed (uint* ptr15 = (uint[])(object)destination)
			{
				ptr15[startIndex] = register.uint32_0;
				ptr15[startIndex + 1] = register.uint32_1;
				ptr15[startIndex + 2] = register.uint32_2;
				ptr15[startIndex + 3] = register.uint32_3;
			}
		}
		else if (typeof(T) == typeof(int))
		{
			fixed (int* ptr16 = (int[])(object)destination)
			{
				ptr16[startIndex] = register.int32_0;
				ptr16[startIndex + 1] = register.int32_1;
				ptr16[startIndex + 2] = register.int32_2;
				ptr16[startIndex + 3] = register.int32_3;
			}
		}
		else if (typeof(T) == typeof(ulong))
		{
			fixed (ulong* ptr17 = (ulong[])(object)destination)
			{
				ptr17[startIndex] = register.uint64_0;
				ptr17[startIndex + 1] = register.uint64_1;
			}
		}
		else if (typeof(T) == typeof(long))
		{
			fixed (long* ptr18 = (long[])(object)destination)
			{
				ptr18[startIndex] = register.int64_0;
				ptr18[startIndex + 1] = register.int64_1;
			}
		}
		else if (typeof(T) == typeof(float))
		{
			fixed (float* ptr19 = (float[])(object)destination)
			{
				ptr19[startIndex] = register.single_0;
				ptr19[startIndex + 1] = register.single_1;
				ptr19[startIndex + 2] = register.single_2;
				ptr19[startIndex + 3] = register.single_3;
			}
		}
		else if (typeof(T) == typeof(double))
		{
			fixed (double* ptr20 = (double[])(object)destination)
			{
				ptr20[startIndex] = register.double_0;
				ptr20[startIndex + 1] = register.double_1;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object obj)
	{
		if (!(obj is Vector<T>))
		{
			return false;
		}
		return Equals((Vector<T>)obj);
	}

	[Intrinsic]
	public bool Equals(Vector<T> other)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			for (int i = 0; i < Count; i++)
			{
				if (!ScalarEquals(this[i], other[i]))
				{
					return false;
				}
			}
			return true;
		}
		if (typeof(T) == typeof(byte))
		{
			if (register.byte_0 == other.register.byte_0 && register.byte_1 == other.register.byte_1 && register.byte_2 == other.register.byte_2 && register.byte_3 == other.register.byte_3 && register.byte_4 == other.register.byte_4 && register.byte_5 == other.register.byte_5 && register.byte_6 == other.register.byte_6 && register.byte_7 == other.register.byte_7 && register.byte_8 == other.register.byte_8 && register.byte_9 == other.register.byte_9 && register.byte_10 == other.register.byte_10 && register.byte_11 == other.register.byte_11 && register.byte_12 == other.register.byte_12 && register.byte_13 == other.register.byte_13 && register.byte_14 == other.register.byte_14)
			{
				return register.byte_15 == other.register.byte_15;
			}
			return false;
		}
		if (typeof(T) == typeof(sbyte))
		{
			if (register.sbyte_0 == other.register.sbyte_0 && register.sbyte_1 == other.register.sbyte_1 && register.sbyte_2 == other.register.sbyte_2 && register.sbyte_3 == other.register.sbyte_3 && register.sbyte_4 == other.register.sbyte_4 && register.sbyte_5 == other.register.sbyte_5 && register.sbyte_6 == other.register.sbyte_6 && register.sbyte_7 == other.register.sbyte_7 && register.sbyte_8 == other.register.sbyte_8 && register.sbyte_9 == other.register.sbyte_9 && register.sbyte_10 == other.register.sbyte_10 && register.sbyte_11 == other.register.sbyte_11 && register.sbyte_12 == other.register.sbyte_12 && register.sbyte_13 == other.register.sbyte_13 && register.sbyte_14 == other.register.sbyte_14)
			{
				return register.sbyte_15 == other.register.sbyte_15;
			}
			return false;
		}
		if (typeof(T) == typeof(ushort))
		{
			if (register.uint16_0 == other.register.uint16_0 && register.uint16_1 == other.register.uint16_1 && register.uint16_2 == other.register.uint16_2 && register.uint16_3 == other.register.uint16_3 && register.uint16_4 == other.register.uint16_4 && register.uint16_5 == other.register.uint16_5 && register.uint16_6 == other.register.uint16_6)
			{
				return register.uint16_7 == other.register.uint16_7;
			}
			return false;
		}
		if (typeof(T) == typeof(short))
		{
			if (register.int16_0 == other.register.int16_0 && register.int16_1 == other.register.int16_1 && register.int16_2 == other.register.int16_2 && register.int16_3 == other.register.int16_3 && register.int16_4 == other.register.int16_4 && register.int16_5 == other.register.int16_5 && register.int16_6 == other.register.int16_6)
			{
				return register.int16_7 == other.register.int16_7;
			}
			return false;
		}
		if (typeof(T) == typeof(uint))
		{
			if (register.uint32_0 == other.register.uint32_0 && register.uint32_1 == other.register.uint32_1 && register.uint32_2 == other.register.uint32_2)
			{
				return register.uint32_3 == other.register.uint32_3;
			}
			return false;
		}
		if (typeof(T) == typeof(int))
		{
			if (register.int32_0 == other.register.int32_0 && register.int32_1 == other.register.int32_1 && register.int32_2 == other.register.int32_2)
			{
				return register.int32_3 == other.register.int32_3;
			}
			return false;
		}
		if (typeof(T) == typeof(ulong))
		{
			if (register.uint64_0 == other.register.uint64_0)
			{
				return register.uint64_1 == other.register.uint64_1;
			}
			return false;
		}
		if (typeof(T) == typeof(long))
		{
			if (register.int64_0 == other.register.int64_0)
			{
				return register.int64_1 == other.register.int64_1;
			}
			return false;
		}
		if (typeof(T) == typeof(float))
		{
			if (register.single_0 == other.register.single_0 && register.single_1 == other.register.single_1 && register.single_2 == other.register.single_2)
			{
				return register.single_3 == other.register.single_3;
			}
			return false;
		}
		if (typeof(T) == typeof(double))
		{
			if (register.double_0 == other.register.double_0)
			{
				return register.double_1 == other.register.double_1;
			}
			return false;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	public override int GetHashCode()
	{
		int num = 0;
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				for (int i = 0; i < Count; i++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((byte)(object)this[i]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(sbyte))
			{
				for (int j = 0; j < Count; j++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((sbyte)(object)this[j]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(ushort))
			{
				for (int k = 0; k < Count; k++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((ushort)(object)this[k]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(short))
			{
				for (int l = 0; l < Count; l++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((short)(object)this[l]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(uint))
			{
				for (int m = 0; m < Count; m++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((uint)(object)this[m]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(int))
			{
				for (int n = 0; n < Count; n++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((int)(object)this[n]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(ulong))
			{
				for (int num2 = 0; num2 < Count; num2++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((ulong)(object)this[num2]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(long))
			{
				for (int num3 = 0; num3 < Count; num3++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((long)(object)this[num3]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(float))
			{
				for (int num4 = 0; num4 < Count; num4++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((float)(object)this[num4]).GetHashCode());
				}
				return num;
			}
			if (typeof(T) == typeof(double))
			{
				for (int num5 = 0; num5 < Count; num5++)
				{
					num = System.Numerics.Hashing.HashHelpers.Combine(num, ((double)(object)this[num5]).GetHashCode());
				}
				return num;
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		if (typeof(T) == typeof(byte))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_0.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_1.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_2.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_3.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_4.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_5.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_6.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_7.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_8.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_9.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_10.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_11.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_12.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_13.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_14.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.byte_15.GetHashCode());
		}
		if (typeof(T) == typeof(sbyte))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_0.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_1.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_2.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_3.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_4.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_5.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_6.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_7.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_8.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_9.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_10.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_11.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_12.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_13.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_14.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.sbyte_15.GetHashCode());
		}
		if (typeof(T) == typeof(ushort))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_0.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_1.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_2.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_3.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_4.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_5.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_6.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.uint16_7.GetHashCode());
		}
		if (typeof(T) == typeof(short))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_0.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_1.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_2.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_3.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_4.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_5.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_6.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.int16_7.GetHashCode());
		}
		if (typeof(T) == typeof(uint))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint32_0.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint32_1.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint32_2.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.uint32_3.GetHashCode());
		}
		if (typeof(T) == typeof(int))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int32_0.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int32_1.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int32_2.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.int32_3.GetHashCode());
		}
		if (typeof(T) == typeof(ulong))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.uint64_0.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.uint64_1.GetHashCode());
		}
		if (typeof(T) == typeof(long))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.int64_0.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.int64_1.GetHashCode());
		}
		if (typeof(T) == typeof(float))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.single_0.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.single_1.GetHashCode());
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.single_2.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.single_3.GetHashCode());
		}
		if (typeof(T) == typeof(double))
		{
			num = System.Numerics.Hashing.HashHelpers.Combine(num, register.double_0.GetHashCode());
			return System.Numerics.Hashing.HashHelpers.Combine(num, register.double_1.GetHashCode());
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	public override string ToString()
	{
		return ToString("G", CultureInfo.CurrentCulture);
	}

	public string ToString(string format)
	{
		return ToString(format, CultureInfo.CurrentCulture);
	}

	public string ToString(string format, IFormatProvider formatProvider)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		stringBuilder.Append('<');
		for (int i = 0; i < Count - 1; i++)
		{
			stringBuilder.Append(((IFormattable)(object)this[i]).ToString(format, formatProvider));
			stringBuilder.Append(numberGroupSeparator);
			stringBuilder.Append(' ');
		}
		stringBuilder.Append(((IFormattable)(object)this[Count - 1]).ToString(format, formatProvider));
		stringBuilder.Append('>');
		return stringBuilder.ToString();
	}

	public unsafe static Vector<T> operator +(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)(object)ScalarAdd(left[i], right[i]);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)(object)ScalarAdd(left[j], right[j]);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)(object)ScalarAdd(left[k], right[k]);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)(object)ScalarAdd(left[l], right[l]);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (uint)(object)ScalarAdd(left[m], right[m]);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (int)(object)ScalarAdd(left[n], right[n]);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ulong)(object)ScalarAdd(left[num], right[num]);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (long)(object)ScalarAdd(left[num2], right[num2]);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (float)(object)ScalarAdd(left[num3], right[num3]);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (double)(object)ScalarAdd(left[num4], right[num4]);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = (byte)(left.register.byte_0 + right.register.byte_0);
			result.register.byte_1 = (byte)(left.register.byte_1 + right.register.byte_1);
			result.register.byte_2 = (byte)(left.register.byte_2 + right.register.byte_2);
			result.register.byte_3 = (byte)(left.register.byte_3 + right.register.byte_3);
			result.register.byte_4 = (byte)(left.register.byte_4 + right.register.byte_4);
			result.register.byte_5 = (byte)(left.register.byte_5 + right.register.byte_5);
			result.register.byte_6 = (byte)(left.register.byte_6 + right.register.byte_6);
			result.register.byte_7 = (byte)(left.register.byte_7 + right.register.byte_7);
			result.register.byte_8 = (byte)(left.register.byte_8 + right.register.byte_8);
			result.register.byte_9 = (byte)(left.register.byte_9 + right.register.byte_9);
			result.register.byte_10 = (byte)(left.register.byte_10 + right.register.byte_10);
			result.register.byte_11 = (byte)(left.register.byte_11 + right.register.byte_11);
			result.register.byte_12 = (byte)(left.register.byte_12 + right.register.byte_12);
			result.register.byte_13 = (byte)(left.register.byte_13 + right.register.byte_13);
			result.register.byte_14 = (byte)(left.register.byte_14 + right.register.byte_14);
			result.register.byte_15 = (byte)(left.register.byte_15 + right.register.byte_15);
		}
		else if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = (sbyte)(left.register.sbyte_0 + right.register.sbyte_0);
			result.register.sbyte_1 = (sbyte)(left.register.sbyte_1 + right.register.sbyte_1);
			result.register.sbyte_2 = (sbyte)(left.register.sbyte_2 + right.register.sbyte_2);
			result.register.sbyte_3 = (sbyte)(left.register.sbyte_3 + right.register.sbyte_3);
			result.register.sbyte_4 = (sbyte)(left.register.sbyte_4 + right.register.sbyte_4);
			result.register.sbyte_5 = (sbyte)(left.register.sbyte_5 + right.register.sbyte_5);
			result.register.sbyte_6 = (sbyte)(left.register.sbyte_6 + right.register.sbyte_6);
			result.register.sbyte_7 = (sbyte)(left.register.sbyte_7 + right.register.sbyte_7);
			result.register.sbyte_8 = (sbyte)(left.register.sbyte_8 + right.register.sbyte_8);
			result.register.sbyte_9 = (sbyte)(left.register.sbyte_9 + right.register.sbyte_9);
			result.register.sbyte_10 = (sbyte)(left.register.sbyte_10 + right.register.sbyte_10);
			result.register.sbyte_11 = (sbyte)(left.register.sbyte_11 + right.register.sbyte_11);
			result.register.sbyte_12 = (sbyte)(left.register.sbyte_12 + right.register.sbyte_12);
			result.register.sbyte_13 = (sbyte)(left.register.sbyte_13 + right.register.sbyte_13);
			result.register.sbyte_14 = (sbyte)(left.register.sbyte_14 + right.register.sbyte_14);
			result.register.sbyte_15 = (sbyte)(left.register.sbyte_15 + right.register.sbyte_15);
		}
		else if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = (ushort)(left.register.uint16_0 + right.register.uint16_0);
			result.register.uint16_1 = (ushort)(left.register.uint16_1 + right.register.uint16_1);
			result.register.uint16_2 = (ushort)(left.register.uint16_2 + right.register.uint16_2);
			result.register.uint16_3 = (ushort)(left.register.uint16_3 + right.register.uint16_3);
			result.register.uint16_4 = (ushort)(left.register.uint16_4 + right.register.uint16_4);
			result.register.uint16_5 = (ushort)(left.register.uint16_5 + right.register.uint16_5);
			result.register.uint16_6 = (ushort)(left.register.uint16_6 + right.register.uint16_6);
			result.register.uint16_7 = (ushort)(left.register.uint16_7 + right.register.uint16_7);
		}
		else if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = (short)(left.register.int16_0 + right.register.int16_0);
			result.register.int16_1 = (short)(left.register.int16_1 + right.register.int16_1);
			result.register.int16_2 = (short)(left.register.int16_2 + right.register.int16_2);
			result.register.int16_3 = (short)(left.register.int16_3 + right.register.int16_3);
			result.register.int16_4 = (short)(left.register.int16_4 + right.register.int16_4);
			result.register.int16_5 = (short)(left.register.int16_5 + right.register.int16_5);
			result.register.int16_6 = (short)(left.register.int16_6 + right.register.int16_6);
			result.register.int16_7 = (short)(left.register.int16_7 + right.register.int16_7);
		}
		else if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = left.register.uint32_0 + right.register.uint32_0;
			result.register.uint32_1 = left.register.uint32_1 + right.register.uint32_1;
			result.register.uint32_2 = left.register.uint32_2 + right.register.uint32_2;
			result.register.uint32_3 = left.register.uint32_3 + right.register.uint32_3;
		}
		else if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = left.register.int32_0 + right.register.int32_0;
			result.register.int32_1 = left.register.int32_1 + right.register.int32_1;
			result.register.int32_2 = left.register.int32_2 + right.register.int32_2;
			result.register.int32_3 = left.register.int32_3 + right.register.int32_3;
		}
		else if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = left.register.uint64_0 + right.register.uint64_0;
			result.register.uint64_1 = left.register.uint64_1 + right.register.uint64_1;
		}
		else if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = left.register.int64_0 + right.register.int64_0;
			result.register.int64_1 = left.register.int64_1 + right.register.int64_1;
		}
		else if (typeof(T) == typeof(float))
		{
			result.register.single_0 = left.register.single_0 + right.register.single_0;
			result.register.single_1 = left.register.single_1 + right.register.single_1;
			result.register.single_2 = left.register.single_2 + right.register.single_2;
			result.register.single_3 = left.register.single_3 + right.register.single_3;
		}
		else if (typeof(T) == typeof(double))
		{
			result.register.double_0 = left.register.double_0 + right.register.double_0;
			result.register.double_1 = left.register.double_1 + right.register.double_1;
		}
		return result;
	}

	public unsafe static Vector<T> operator -(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)(object)ScalarSubtract(left[i], right[i]);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)(object)ScalarSubtract(left[j], right[j]);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)(object)ScalarSubtract(left[k], right[k]);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)(object)ScalarSubtract(left[l], right[l]);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (uint)(object)ScalarSubtract(left[m], right[m]);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (int)(object)ScalarSubtract(left[n], right[n]);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ulong)(object)ScalarSubtract(left[num], right[num]);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (long)(object)ScalarSubtract(left[num2], right[num2]);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (float)(object)ScalarSubtract(left[num3], right[num3]);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (double)(object)ScalarSubtract(left[num4], right[num4]);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = (byte)(left.register.byte_0 - right.register.byte_0);
			result.register.byte_1 = (byte)(left.register.byte_1 - right.register.byte_1);
			result.register.byte_2 = (byte)(left.register.byte_2 - right.register.byte_2);
			result.register.byte_3 = (byte)(left.register.byte_3 - right.register.byte_3);
			result.register.byte_4 = (byte)(left.register.byte_4 - right.register.byte_4);
			result.register.byte_5 = (byte)(left.register.byte_5 - right.register.byte_5);
			result.register.byte_6 = (byte)(left.register.byte_6 - right.register.byte_6);
			result.register.byte_7 = (byte)(left.register.byte_7 - right.register.byte_7);
			result.register.byte_8 = (byte)(left.register.byte_8 - right.register.byte_8);
			result.register.byte_9 = (byte)(left.register.byte_9 - right.register.byte_9);
			result.register.byte_10 = (byte)(left.register.byte_10 - right.register.byte_10);
			result.register.byte_11 = (byte)(left.register.byte_11 - right.register.byte_11);
			result.register.byte_12 = (byte)(left.register.byte_12 - right.register.byte_12);
			result.register.byte_13 = (byte)(left.register.byte_13 - right.register.byte_13);
			result.register.byte_14 = (byte)(left.register.byte_14 - right.register.byte_14);
			result.register.byte_15 = (byte)(left.register.byte_15 - right.register.byte_15);
		}
		else if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = (sbyte)(left.register.sbyte_0 - right.register.sbyte_0);
			result.register.sbyte_1 = (sbyte)(left.register.sbyte_1 - right.register.sbyte_1);
			result.register.sbyte_2 = (sbyte)(left.register.sbyte_2 - right.register.sbyte_2);
			result.register.sbyte_3 = (sbyte)(left.register.sbyte_3 - right.register.sbyte_3);
			result.register.sbyte_4 = (sbyte)(left.register.sbyte_4 - right.register.sbyte_4);
			result.register.sbyte_5 = (sbyte)(left.register.sbyte_5 - right.register.sbyte_5);
			result.register.sbyte_6 = (sbyte)(left.register.sbyte_6 - right.register.sbyte_6);
			result.register.sbyte_7 = (sbyte)(left.register.sbyte_7 - right.register.sbyte_7);
			result.register.sbyte_8 = (sbyte)(left.register.sbyte_8 - right.register.sbyte_8);
			result.register.sbyte_9 = (sbyte)(left.register.sbyte_9 - right.register.sbyte_9);
			result.register.sbyte_10 = (sbyte)(left.register.sbyte_10 - right.register.sbyte_10);
			result.register.sbyte_11 = (sbyte)(left.register.sbyte_11 - right.register.sbyte_11);
			result.register.sbyte_12 = (sbyte)(left.register.sbyte_12 - right.register.sbyte_12);
			result.register.sbyte_13 = (sbyte)(left.register.sbyte_13 - right.register.sbyte_13);
			result.register.sbyte_14 = (sbyte)(left.register.sbyte_14 - right.register.sbyte_14);
			result.register.sbyte_15 = (sbyte)(left.register.sbyte_15 - right.register.sbyte_15);
		}
		else if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = (ushort)(left.register.uint16_0 - right.register.uint16_0);
			result.register.uint16_1 = (ushort)(left.register.uint16_1 - right.register.uint16_1);
			result.register.uint16_2 = (ushort)(left.register.uint16_2 - right.register.uint16_2);
			result.register.uint16_3 = (ushort)(left.register.uint16_3 - right.register.uint16_3);
			result.register.uint16_4 = (ushort)(left.register.uint16_4 - right.register.uint16_4);
			result.register.uint16_5 = (ushort)(left.register.uint16_5 - right.register.uint16_5);
			result.register.uint16_6 = (ushort)(left.register.uint16_6 - right.register.uint16_6);
			result.register.uint16_7 = (ushort)(left.register.uint16_7 - right.register.uint16_7);
		}
		else if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = (short)(left.register.int16_0 - right.register.int16_0);
			result.register.int16_1 = (short)(left.register.int16_1 - right.register.int16_1);
			result.register.int16_2 = (short)(left.register.int16_2 - right.register.int16_2);
			result.register.int16_3 = (short)(left.register.int16_3 - right.register.int16_3);
			result.register.int16_4 = (short)(left.register.int16_4 - right.register.int16_4);
			result.register.int16_5 = (short)(left.register.int16_5 - right.register.int16_5);
			result.register.int16_6 = (short)(left.register.int16_6 - right.register.int16_6);
			result.register.int16_7 = (short)(left.register.int16_7 - right.register.int16_7);
		}
		else if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = left.register.uint32_0 - right.register.uint32_0;
			result.register.uint32_1 = left.register.uint32_1 - right.register.uint32_1;
			result.register.uint32_2 = left.register.uint32_2 - right.register.uint32_2;
			result.register.uint32_3 = left.register.uint32_3 - right.register.uint32_3;
		}
		else if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = left.register.int32_0 - right.register.int32_0;
			result.register.int32_1 = left.register.int32_1 - right.register.int32_1;
			result.register.int32_2 = left.register.int32_2 - right.register.int32_2;
			result.register.int32_3 = left.register.int32_3 - right.register.int32_3;
		}
		else if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = left.register.uint64_0 - right.register.uint64_0;
			result.register.uint64_1 = left.register.uint64_1 - right.register.uint64_1;
		}
		else if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = left.register.int64_0 - right.register.int64_0;
			result.register.int64_1 = left.register.int64_1 - right.register.int64_1;
		}
		else if (typeof(T) == typeof(float))
		{
			result.register.single_0 = left.register.single_0 - right.register.single_0;
			result.register.single_1 = left.register.single_1 - right.register.single_1;
			result.register.single_2 = left.register.single_2 - right.register.single_2;
			result.register.single_3 = left.register.single_3 - right.register.single_3;
		}
		else if (typeof(T) == typeof(double))
		{
			result.register.double_0 = left.register.double_0 - right.register.double_0;
			result.register.double_1 = left.register.double_1 - right.register.double_1;
		}
		return result;
	}

	public unsafe static Vector<T> operator *(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)(object)ScalarMultiply(left[i], right[i]);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)(object)ScalarMultiply(left[j], right[j]);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)(object)ScalarMultiply(left[k], right[k]);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)(object)ScalarMultiply(left[l], right[l]);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (uint)(object)ScalarMultiply(left[m], right[m]);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (int)(object)ScalarMultiply(left[n], right[n]);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ulong)(object)ScalarMultiply(left[num], right[num]);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (long)(object)ScalarMultiply(left[num2], right[num2]);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (float)(object)ScalarMultiply(left[num3], right[num3]);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (double)(object)ScalarMultiply(left[num4], right[num4]);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = (byte)(left.register.byte_0 * right.register.byte_0);
			result.register.byte_1 = (byte)(left.register.byte_1 * right.register.byte_1);
			result.register.byte_2 = (byte)(left.register.byte_2 * right.register.byte_2);
			result.register.byte_3 = (byte)(left.register.byte_3 * right.register.byte_3);
			result.register.byte_4 = (byte)(left.register.byte_4 * right.register.byte_4);
			result.register.byte_5 = (byte)(left.register.byte_5 * right.register.byte_5);
			result.register.byte_6 = (byte)(left.register.byte_6 * right.register.byte_6);
			result.register.byte_7 = (byte)(left.register.byte_7 * right.register.byte_7);
			result.register.byte_8 = (byte)(left.register.byte_8 * right.register.byte_8);
			result.register.byte_9 = (byte)(left.register.byte_9 * right.register.byte_9);
			result.register.byte_10 = (byte)(left.register.byte_10 * right.register.byte_10);
			result.register.byte_11 = (byte)(left.register.byte_11 * right.register.byte_11);
			result.register.byte_12 = (byte)(left.register.byte_12 * right.register.byte_12);
			result.register.byte_13 = (byte)(left.register.byte_13 * right.register.byte_13);
			result.register.byte_14 = (byte)(left.register.byte_14 * right.register.byte_14);
			result.register.byte_15 = (byte)(left.register.byte_15 * right.register.byte_15);
		}
		else if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = (sbyte)(left.register.sbyte_0 * right.register.sbyte_0);
			result.register.sbyte_1 = (sbyte)(left.register.sbyte_1 * right.register.sbyte_1);
			result.register.sbyte_2 = (sbyte)(left.register.sbyte_2 * right.register.sbyte_2);
			result.register.sbyte_3 = (sbyte)(left.register.sbyte_3 * right.register.sbyte_3);
			result.register.sbyte_4 = (sbyte)(left.register.sbyte_4 * right.register.sbyte_4);
			result.register.sbyte_5 = (sbyte)(left.register.sbyte_5 * right.register.sbyte_5);
			result.register.sbyte_6 = (sbyte)(left.register.sbyte_6 * right.register.sbyte_6);
			result.register.sbyte_7 = (sbyte)(left.register.sbyte_7 * right.register.sbyte_7);
			result.register.sbyte_8 = (sbyte)(left.register.sbyte_8 * right.register.sbyte_8);
			result.register.sbyte_9 = (sbyte)(left.register.sbyte_9 * right.register.sbyte_9);
			result.register.sbyte_10 = (sbyte)(left.register.sbyte_10 * right.register.sbyte_10);
			result.register.sbyte_11 = (sbyte)(left.register.sbyte_11 * right.register.sbyte_11);
			result.register.sbyte_12 = (sbyte)(left.register.sbyte_12 * right.register.sbyte_12);
			result.register.sbyte_13 = (sbyte)(left.register.sbyte_13 * right.register.sbyte_13);
			result.register.sbyte_14 = (sbyte)(left.register.sbyte_14 * right.register.sbyte_14);
			result.register.sbyte_15 = (sbyte)(left.register.sbyte_15 * right.register.sbyte_15);
		}
		else if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = (ushort)(left.register.uint16_0 * right.register.uint16_0);
			result.register.uint16_1 = (ushort)(left.register.uint16_1 * right.register.uint16_1);
			result.register.uint16_2 = (ushort)(left.register.uint16_2 * right.register.uint16_2);
			result.register.uint16_3 = (ushort)(left.register.uint16_3 * right.register.uint16_3);
			result.register.uint16_4 = (ushort)(left.register.uint16_4 * right.register.uint16_4);
			result.register.uint16_5 = (ushort)(left.register.uint16_5 * right.register.uint16_5);
			result.register.uint16_6 = (ushort)(left.register.uint16_6 * right.register.uint16_6);
			result.register.uint16_7 = (ushort)(left.register.uint16_7 * right.register.uint16_7);
		}
		else if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = (short)(left.register.int16_0 * right.register.int16_0);
			result.register.int16_1 = (short)(left.register.int16_1 * right.register.int16_1);
			result.register.int16_2 = (short)(left.register.int16_2 * right.register.int16_2);
			result.register.int16_3 = (short)(left.register.int16_3 * right.register.int16_3);
			result.register.int16_4 = (short)(left.register.int16_4 * right.register.int16_4);
			result.register.int16_5 = (short)(left.register.int16_5 * right.register.int16_5);
			result.register.int16_6 = (short)(left.register.int16_6 * right.register.int16_6);
			result.register.int16_7 = (short)(left.register.int16_7 * right.register.int16_7);
		}
		else if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = left.register.uint32_0 * right.register.uint32_0;
			result.register.uint32_1 = left.register.uint32_1 * right.register.uint32_1;
			result.register.uint32_2 = left.register.uint32_2 * right.register.uint32_2;
			result.register.uint32_3 = left.register.uint32_3 * right.register.uint32_3;
		}
		else if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = left.register.int32_0 * right.register.int32_0;
			result.register.int32_1 = left.register.int32_1 * right.register.int32_1;
			result.register.int32_2 = left.register.int32_2 * right.register.int32_2;
			result.register.int32_3 = left.register.int32_3 * right.register.int32_3;
		}
		else if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = left.register.uint64_0 * right.register.uint64_0;
			result.register.uint64_1 = left.register.uint64_1 * right.register.uint64_1;
		}
		else if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = left.register.int64_0 * right.register.int64_0;
			result.register.int64_1 = left.register.int64_1 * right.register.int64_1;
		}
		else if (typeof(T) == typeof(float))
		{
			result.register.single_0 = left.register.single_0 * right.register.single_0;
			result.register.single_1 = left.register.single_1 * right.register.single_1;
			result.register.single_2 = left.register.single_2 * right.register.single_2;
			result.register.single_3 = left.register.single_3 * right.register.single_3;
		}
		else if (typeof(T) == typeof(double))
		{
			result.register.double_0 = left.register.double_0 * right.register.double_0;
			result.register.double_1 = left.register.double_1 * right.register.double_1;
		}
		return result;
	}

	public static Vector<T> operator *(Vector<T> value, T factor)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			return new Vector<T>(factor) * value;
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = (byte)(value.register.byte_0 * (byte)(object)factor);
			result.register.byte_1 = (byte)(value.register.byte_1 * (byte)(object)factor);
			result.register.byte_2 = (byte)(value.register.byte_2 * (byte)(object)factor);
			result.register.byte_3 = (byte)(value.register.byte_3 * (byte)(object)factor);
			result.register.byte_4 = (byte)(value.register.byte_4 * (byte)(object)factor);
			result.register.byte_5 = (byte)(value.register.byte_5 * (byte)(object)factor);
			result.register.byte_6 = (byte)(value.register.byte_6 * (byte)(object)factor);
			result.register.byte_7 = (byte)(value.register.byte_7 * (byte)(object)factor);
			result.register.byte_8 = (byte)(value.register.byte_8 * (byte)(object)factor);
			result.register.byte_9 = (byte)(value.register.byte_9 * (byte)(object)factor);
			result.register.byte_10 = (byte)(value.register.byte_10 * (byte)(object)factor);
			result.register.byte_11 = (byte)(value.register.byte_11 * (byte)(object)factor);
			result.register.byte_12 = (byte)(value.register.byte_12 * (byte)(object)factor);
			result.register.byte_13 = (byte)(value.register.byte_13 * (byte)(object)factor);
			result.register.byte_14 = (byte)(value.register.byte_14 * (byte)(object)factor);
			result.register.byte_15 = (byte)(value.register.byte_15 * (byte)(object)factor);
		}
		else if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = (sbyte)(value.register.sbyte_0 * (sbyte)(object)factor);
			result.register.sbyte_1 = (sbyte)(value.register.sbyte_1 * (sbyte)(object)factor);
			result.register.sbyte_2 = (sbyte)(value.register.sbyte_2 * (sbyte)(object)factor);
			result.register.sbyte_3 = (sbyte)(value.register.sbyte_3 * (sbyte)(object)factor);
			result.register.sbyte_4 = (sbyte)(value.register.sbyte_4 * (sbyte)(object)factor);
			result.register.sbyte_5 = (sbyte)(value.register.sbyte_5 * (sbyte)(object)factor);
			result.register.sbyte_6 = (sbyte)(value.register.sbyte_6 * (sbyte)(object)factor);
			result.register.sbyte_7 = (sbyte)(value.register.sbyte_7 * (sbyte)(object)factor);
			result.register.sbyte_8 = (sbyte)(value.register.sbyte_8 * (sbyte)(object)factor);
			result.register.sbyte_9 = (sbyte)(value.register.sbyte_9 * (sbyte)(object)factor);
			result.register.sbyte_10 = (sbyte)(value.register.sbyte_10 * (sbyte)(object)factor);
			result.register.sbyte_11 = (sbyte)(value.register.sbyte_11 * (sbyte)(object)factor);
			result.register.sbyte_12 = (sbyte)(value.register.sbyte_12 * (sbyte)(object)factor);
			result.register.sbyte_13 = (sbyte)(value.register.sbyte_13 * (sbyte)(object)factor);
			result.register.sbyte_14 = (sbyte)(value.register.sbyte_14 * (sbyte)(object)factor);
			result.register.sbyte_15 = (sbyte)(value.register.sbyte_15 * (sbyte)(object)factor);
		}
		else if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = (ushort)(value.register.uint16_0 * (ushort)(object)factor);
			result.register.uint16_1 = (ushort)(value.register.uint16_1 * (ushort)(object)factor);
			result.register.uint16_2 = (ushort)(value.register.uint16_2 * (ushort)(object)factor);
			result.register.uint16_3 = (ushort)(value.register.uint16_3 * (ushort)(object)factor);
			result.register.uint16_4 = (ushort)(value.register.uint16_4 * (ushort)(object)factor);
			result.register.uint16_5 = (ushort)(value.register.uint16_5 * (ushort)(object)factor);
			result.register.uint16_6 = (ushort)(value.register.uint16_6 * (ushort)(object)factor);
			result.register.uint16_7 = (ushort)(value.register.uint16_7 * (ushort)(object)factor);
		}
		else if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = (short)(value.register.int16_0 * (short)(object)factor);
			result.register.int16_1 = (short)(value.register.int16_1 * (short)(object)factor);
			result.register.int16_2 = (short)(value.register.int16_2 * (short)(object)factor);
			result.register.int16_3 = (short)(value.register.int16_3 * (short)(object)factor);
			result.register.int16_4 = (short)(value.register.int16_4 * (short)(object)factor);
			result.register.int16_5 = (short)(value.register.int16_5 * (short)(object)factor);
			result.register.int16_6 = (short)(value.register.int16_6 * (short)(object)factor);
			result.register.int16_7 = (short)(value.register.int16_7 * (short)(object)factor);
		}
		else if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = value.register.uint32_0 * (uint)(object)factor;
			result.register.uint32_1 = value.register.uint32_1 * (uint)(object)factor;
			result.register.uint32_2 = value.register.uint32_2 * (uint)(object)factor;
			result.register.uint32_3 = value.register.uint32_3 * (uint)(object)factor;
		}
		else if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = value.register.int32_0 * (int)(object)factor;
			result.register.int32_1 = value.register.int32_1 * (int)(object)factor;
			result.register.int32_2 = value.register.int32_2 * (int)(object)factor;
			result.register.int32_3 = value.register.int32_3 * (int)(object)factor;
		}
		else if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = value.register.uint64_0 * (ulong)(object)factor;
			result.register.uint64_1 = value.register.uint64_1 * (ulong)(object)factor;
		}
		else if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = value.register.int64_0 * (long)(object)factor;
			result.register.int64_1 = value.register.int64_1 * (long)(object)factor;
		}
		else if (typeof(T) == typeof(float))
		{
			result.register.single_0 = value.register.single_0 * (float)(object)factor;
			result.register.single_1 = value.register.single_1 * (float)(object)factor;
			result.register.single_2 = value.register.single_2 * (float)(object)factor;
			result.register.single_3 = value.register.single_3 * (float)(object)factor;
		}
		else if (typeof(T) == typeof(double))
		{
			result.register.double_0 = value.register.double_0 * (double)(object)factor;
			result.register.double_1 = value.register.double_1 * (double)(object)factor;
		}
		return result;
	}

	public static Vector<T> operator *(T factor, Vector<T> value)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			return new Vector<T>(factor) * value;
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = (byte)(value.register.byte_0 * (byte)(object)factor);
			result.register.byte_1 = (byte)(value.register.byte_1 * (byte)(object)factor);
			result.register.byte_2 = (byte)(value.register.byte_2 * (byte)(object)factor);
			result.register.byte_3 = (byte)(value.register.byte_3 * (byte)(object)factor);
			result.register.byte_4 = (byte)(value.register.byte_4 * (byte)(object)factor);
			result.register.byte_5 = (byte)(value.register.byte_5 * (byte)(object)factor);
			result.register.byte_6 = (byte)(value.register.byte_6 * (byte)(object)factor);
			result.register.byte_7 = (byte)(value.register.byte_7 * (byte)(object)factor);
			result.register.byte_8 = (byte)(value.register.byte_8 * (byte)(object)factor);
			result.register.byte_9 = (byte)(value.register.byte_9 * (byte)(object)factor);
			result.register.byte_10 = (byte)(value.register.byte_10 * (byte)(object)factor);
			result.register.byte_11 = (byte)(value.register.byte_11 * (byte)(object)factor);
			result.register.byte_12 = (byte)(value.register.byte_12 * (byte)(object)factor);
			result.register.byte_13 = (byte)(value.register.byte_13 * (byte)(object)factor);
			result.register.byte_14 = (byte)(value.register.byte_14 * (byte)(object)factor);
			result.register.byte_15 = (byte)(value.register.byte_15 * (byte)(object)factor);
		}
		else if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = (sbyte)(value.register.sbyte_0 * (sbyte)(object)factor);
			result.register.sbyte_1 = (sbyte)(value.register.sbyte_1 * (sbyte)(object)factor);
			result.register.sbyte_2 = (sbyte)(value.register.sbyte_2 * (sbyte)(object)factor);
			result.register.sbyte_3 = (sbyte)(value.register.sbyte_3 * (sbyte)(object)factor);
			result.register.sbyte_4 = (sbyte)(value.register.sbyte_4 * (sbyte)(object)factor);
			result.register.sbyte_5 = (sbyte)(value.register.sbyte_5 * (sbyte)(object)factor);
			result.register.sbyte_6 = (sbyte)(value.register.sbyte_6 * (sbyte)(object)factor);
			result.register.sbyte_7 = (sbyte)(value.register.sbyte_7 * (sbyte)(object)factor);
			result.register.sbyte_8 = (sbyte)(value.register.sbyte_8 * (sbyte)(object)factor);
			result.register.sbyte_9 = (sbyte)(value.register.sbyte_9 * (sbyte)(object)factor);
			result.register.sbyte_10 = (sbyte)(value.register.sbyte_10 * (sbyte)(object)factor);
			result.register.sbyte_11 = (sbyte)(value.register.sbyte_11 * (sbyte)(object)factor);
			result.register.sbyte_12 = (sbyte)(value.register.sbyte_12 * (sbyte)(object)factor);
			result.register.sbyte_13 = (sbyte)(value.register.sbyte_13 * (sbyte)(object)factor);
			result.register.sbyte_14 = (sbyte)(value.register.sbyte_14 * (sbyte)(object)factor);
			result.register.sbyte_15 = (sbyte)(value.register.sbyte_15 * (sbyte)(object)factor);
		}
		else if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = (ushort)(value.register.uint16_0 * (ushort)(object)factor);
			result.register.uint16_1 = (ushort)(value.register.uint16_1 * (ushort)(object)factor);
			result.register.uint16_2 = (ushort)(value.register.uint16_2 * (ushort)(object)factor);
			result.register.uint16_3 = (ushort)(value.register.uint16_3 * (ushort)(object)factor);
			result.register.uint16_4 = (ushort)(value.register.uint16_4 * (ushort)(object)factor);
			result.register.uint16_5 = (ushort)(value.register.uint16_5 * (ushort)(object)factor);
			result.register.uint16_6 = (ushort)(value.register.uint16_6 * (ushort)(object)factor);
			result.register.uint16_7 = (ushort)(value.register.uint16_7 * (ushort)(object)factor);
		}
		else if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = (short)(value.register.int16_0 * (short)(object)factor);
			result.register.int16_1 = (short)(value.register.int16_1 * (short)(object)factor);
			result.register.int16_2 = (short)(value.register.int16_2 * (short)(object)factor);
			result.register.int16_3 = (short)(value.register.int16_3 * (short)(object)factor);
			result.register.int16_4 = (short)(value.register.int16_4 * (short)(object)factor);
			result.register.int16_5 = (short)(value.register.int16_5 * (short)(object)factor);
			result.register.int16_6 = (short)(value.register.int16_6 * (short)(object)factor);
			result.register.int16_7 = (short)(value.register.int16_7 * (short)(object)factor);
		}
		else if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = value.register.uint32_0 * (uint)(object)factor;
			result.register.uint32_1 = value.register.uint32_1 * (uint)(object)factor;
			result.register.uint32_2 = value.register.uint32_2 * (uint)(object)factor;
			result.register.uint32_3 = value.register.uint32_3 * (uint)(object)factor;
		}
		else if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = value.register.int32_0 * (int)(object)factor;
			result.register.int32_1 = value.register.int32_1 * (int)(object)factor;
			result.register.int32_2 = value.register.int32_2 * (int)(object)factor;
			result.register.int32_3 = value.register.int32_3 * (int)(object)factor;
		}
		else if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = value.register.uint64_0 * (ulong)(object)factor;
			result.register.uint64_1 = value.register.uint64_1 * (ulong)(object)factor;
		}
		else if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = value.register.int64_0 * (long)(object)factor;
			result.register.int64_1 = value.register.int64_1 * (long)(object)factor;
		}
		else if (typeof(T) == typeof(float))
		{
			result.register.single_0 = value.register.single_0 * (float)(object)factor;
			result.register.single_1 = value.register.single_1 * (float)(object)factor;
			result.register.single_2 = value.register.single_2 * (float)(object)factor;
			result.register.single_3 = value.register.single_3 * (float)(object)factor;
		}
		else if (typeof(T) == typeof(double))
		{
			result.register.double_0 = value.register.double_0 * (double)(object)factor;
			result.register.double_1 = value.register.double_1 * (double)(object)factor;
		}
		return result;
	}

	public unsafe static Vector<T> operator /(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)(object)ScalarDivide(left[i], right[i]);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)(object)ScalarDivide(left[j], right[j]);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)(object)ScalarDivide(left[k], right[k]);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)(object)ScalarDivide(left[l], right[l]);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (uint)(object)ScalarDivide(left[m], right[m]);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (int)(object)ScalarDivide(left[n], right[n]);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ulong)(object)ScalarDivide(left[num], right[num]);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (long)(object)ScalarDivide(left[num2], right[num2]);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (float)(object)ScalarDivide(left[num3], right[num3]);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (double)(object)ScalarDivide(left[num4], right[num4]);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = (byte)(left.register.byte_0 / right.register.byte_0);
			result.register.byte_1 = (byte)(left.register.byte_1 / right.register.byte_1);
			result.register.byte_2 = (byte)(left.register.byte_2 / right.register.byte_2);
			result.register.byte_3 = (byte)(left.register.byte_3 / right.register.byte_3);
			result.register.byte_4 = (byte)(left.register.byte_4 / right.register.byte_4);
			result.register.byte_5 = (byte)(left.register.byte_5 / right.register.byte_5);
			result.register.byte_6 = (byte)(left.register.byte_6 / right.register.byte_6);
			result.register.byte_7 = (byte)(left.register.byte_7 / right.register.byte_7);
			result.register.byte_8 = (byte)(left.register.byte_8 / right.register.byte_8);
			result.register.byte_9 = (byte)(left.register.byte_9 / right.register.byte_9);
			result.register.byte_10 = (byte)(left.register.byte_10 / right.register.byte_10);
			result.register.byte_11 = (byte)(left.register.byte_11 / right.register.byte_11);
			result.register.byte_12 = (byte)(left.register.byte_12 / right.register.byte_12);
			result.register.byte_13 = (byte)(left.register.byte_13 / right.register.byte_13);
			result.register.byte_14 = (byte)(left.register.byte_14 / right.register.byte_14);
			result.register.byte_15 = (byte)(left.register.byte_15 / right.register.byte_15);
		}
		else if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = (sbyte)(left.register.sbyte_0 / right.register.sbyte_0);
			result.register.sbyte_1 = (sbyte)(left.register.sbyte_1 / right.register.sbyte_1);
			result.register.sbyte_2 = (sbyte)(left.register.sbyte_2 / right.register.sbyte_2);
			result.register.sbyte_3 = (sbyte)(left.register.sbyte_3 / right.register.sbyte_3);
			result.register.sbyte_4 = (sbyte)(left.register.sbyte_4 / right.register.sbyte_4);
			result.register.sbyte_5 = (sbyte)(left.register.sbyte_5 / right.register.sbyte_5);
			result.register.sbyte_6 = (sbyte)(left.register.sbyte_6 / right.register.sbyte_6);
			result.register.sbyte_7 = (sbyte)(left.register.sbyte_7 / right.register.sbyte_7);
			result.register.sbyte_8 = (sbyte)(left.register.sbyte_8 / right.register.sbyte_8);
			result.register.sbyte_9 = (sbyte)(left.register.sbyte_9 / right.register.sbyte_9);
			result.register.sbyte_10 = (sbyte)(left.register.sbyte_10 / right.register.sbyte_10);
			result.register.sbyte_11 = (sbyte)(left.register.sbyte_11 / right.register.sbyte_11);
			result.register.sbyte_12 = (sbyte)(left.register.sbyte_12 / right.register.sbyte_12);
			result.register.sbyte_13 = (sbyte)(left.register.sbyte_13 / right.register.sbyte_13);
			result.register.sbyte_14 = (sbyte)(left.register.sbyte_14 / right.register.sbyte_14);
			result.register.sbyte_15 = (sbyte)(left.register.sbyte_15 / right.register.sbyte_15);
		}
		else if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = (ushort)(left.register.uint16_0 / right.register.uint16_0);
			result.register.uint16_1 = (ushort)(left.register.uint16_1 / right.register.uint16_1);
			result.register.uint16_2 = (ushort)(left.register.uint16_2 / right.register.uint16_2);
			result.register.uint16_3 = (ushort)(left.register.uint16_3 / right.register.uint16_3);
			result.register.uint16_4 = (ushort)(left.register.uint16_4 / right.register.uint16_4);
			result.register.uint16_5 = (ushort)(left.register.uint16_5 / right.register.uint16_5);
			result.register.uint16_6 = (ushort)(left.register.uint16_6 / right.register.uint16_6);
			result.register.uint16_7 = (ushort)(left.register.uint16_7 / right.register.uint16_7);
		}
		else if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = (short)(left.register.int16_0 / right.register.int16_0);
			result.register.int16_1 = (short)(left.register.int16_1 / right.register.int16_1);
			result.register.int16_2 = (short)(left.register.int16_2 / right.register.int16_2);
			result.register.int16_3 = (short)(left.register.int16_3 / right.register.int16_3);
			result.register.int16_4 = (short)(left.register.int16_4 / right.register.int16_4);
			result.register.int16_5 = (short)(left.register.int16_5 / right.register.int16_5);
			result.register.int16_6 = (short)(left.register.int16_6 / right.register.int16_6);
			result.register.int16_7 = (short)(left.register.int16_7 / right.register.int16_7);
		}
		else if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = left.register.uint32_0 / right.register.uint32_0;
			result.register.uint32_1 = left.register.uint32_1 / right.register.uint32_1;
			result.register.uint32_2 = left.register.uint32_2 / right.register.uint32_2;
			result.register.uint32_3 = left.register.uint32_3 / right.register.uint32_3;
		}
		else if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = left.register.int32_0 / right.register.int32_0;
			result.register.int32_1 = left.register.int32_1 / right.register.int32_1;
			result.register.int32_2 = left.register.int32_2 / right.register.int32_2;
			result.register.int32_3 = left.register.int32_3 / right.register.int32_3;
		}
		else if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = left.register.uint64_0 / right.register.uint64_0;
			result.register.uint64_1 = left.register.uint64_1 / right.register.uint64_1;
		}
		else if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = left.register.int64_0 / right.register.int64_0;
			result.register.int64_1 = left.register.int64_1 / right.register.int64_1;
		}
		else if (typeof(T) == typeof(float))
		{
			result.register.single_0 = left.register.single_0 / right.register.single_0;
			result.register.single_1 = left.register.single_1 / right.register.single_1;
			result.register.single_2 = left.register.single_2 / right.register.single_2;
			result.register.single_3 = left.register.single_3 / right.register.single_3;
		}
		else if (typeof(T) == typeof(double))
		{
			result.register.double_0 = left.register.double_0 / right.register.double_0;
			result.register.double_1 = left.register.double_1 / right.register.double_1;
		}
		return result;
	}

	public static Vector<T> operator -(Vector<T> value)
	{
		return Zero - value;
	}

	[Intrinsic]
	public unsafe static Vector<T> operator &(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			long* ptr = &result.register.int64_0;
			long* ptr2 = &left.register.int64_0;
			long* ptr3 = &right.register.int64_0;
			for (int i = 0; i < Vector<long>.Count; i++)
			{
				ptr[i] = ptr2[i] & ptr3[i];
			}
		}
		else
		{
			result.register.int64_0 = left.register.int64_0 & right.register.int64_0;
			result.register.int64_1 = left.register.int64_1 & right.register.int64_1;
		}
		return result;
	}

	[Intrinsic]
	public unsafe static Vector<T> operator |(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			long* ptr = &result.register.int64_0;
			long* ptr2 = &left.register.int64_0;
			long* ptr3 = &right.register.int64_0;
			for (int i = 0; i < Vector<long>.Count; i++)
			{
				ptr[i] = ptr2[i] | ptr3[i];
			}
		}
		else
		{
			result.register.int64_0 = left.register.int64_0 | right.register.int64_0;
			result.register.int64_1 = left.register.int64_1 | right.register.int64_1;
		}
		return result;
	}

	[Intrinsic]
	public unsafe static Vector<T> operator ^(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			long* ptr = &result.register.int64_0;
			long* ptr2 = &left.register.int64_0;
			long* ptr3 = &right.register.int64_0;
			for (int i = 0; i < Vector<long>.Count; i++)
			{
				ptr[i] = ptr2[i] ^ ptr3[i];
			}
		}
		else
		{
			result.register.int64_0 = left.register.int64_0 ^ right.register.int64_0;
			result.register.int64_1 = left.register.int64_1 ^ right.register.int64_1;
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> operator ~(Vector<T> value)
	{
		return s_allOnes ^ value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Vector<T> left, Vector<T> right)
	{
		return left.Equals(right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Vector<T> left, Vector<T> right)
	{
		return !(left == right);
	}

	[Intrinsic]
	public static explicit operator Vector<byte>(Vector<T> value)
	{
		return new Vector<byte>(ref value.register);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<sbyte>(Vector<T> value)
	{
		return new Vector<sbyte>(ref value.register);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<ushort>(Vector<T> value)
	{
		return new Vector<ushort>(ref value.register);
	}

	[Intrinsic]
	public static explicit operator Vector<short>(Vector<T> value)
	{
		return new Vector<short>(ref value.register);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<uint>(Vector<T> value)
	{
		return new Vector<uint>(ref value.register);
	}

	[Intrinsic]
	public static explicit operator Vector<int>(Vector<T> value)
	{
		return new Vector<int>(ref value.register);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<ulong>(Vector<T> value)
	{
		return new Vector<ulong>(ref value.register);
	}

	[Intrinsic]
	public static explicit operator Vector<long>(Vector<T> value)
	{
		return new Vector<long>(ref value.register);
	}

	[Intrinsic]
	public static explicit operator Vector<float>(Vector<T> value)
	{
		return new Vector<float>(ref value.register);
	}

	[Intrinsic]
	public static explicit operator Vector<double>(Vector<T> value)
	{
		return new Vector<double>(ref value.register);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal unsafe static Vector<T> Equals(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)(ScalarEquals(left[i], right[i]) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)(ScalarEquals(left[j], right[j]) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)(ScalarEquals(left[k], right[k]) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)(ScalarEquals(left[l], right[l]) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (ScalarEquals(left[m], right[m]) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (ScalarEquals(left[n], right[n]) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ScalarEquals(left[num], right[num]) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (ScalarEquals(left[num2], right[num2]) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (ScalarEquals(left[num3], right[num3]) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (ScalarEquals(left[num4], right[num4]) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Register existingRegister = default(Register);
		if (typeof(T) == typeof(byte))
		{
			existingRegister.byte_0 = (byte)((left.register.byte_0 == right.register.byte_0) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_1 = (byte)((left.register.byte_1 == right.register.byte_1) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_2 = (byte)((left.register.byte_2 == right.register.byte_2) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_3 = (byte)((left.register.byte_3 == right.register.byte_3) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_4 = (byte)((left.register.byte_4 == right.register.byte_4) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_5 = (byte)((left.register.byte_5 == right.register.byte_5) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_6 = (byte)((left.register.byte_6 == right.register.byte_6) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_7 = (byte)((left.register.byte_7 == right.register.byte_7) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_8 = (byte)((left.register.byte_8 == right.register.byte_8) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_9 = (byte)((left.register.byte_9 == right.register.byte_9) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_10 = (byte)((left.register.byte_10 == right.register.byte_10) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_11 = (byte)((left.register.byte_11 == right.register.byte_11) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_12 = (byte)((left.register.byte_12 == right.register.byte_12) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_13 = (byte)((left.register.byte_13 == right.register.byte_13) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_14 = (byte)((left.register.byte_14 == right.register.byte_14) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_15 = (byte)((left.register.byte_15 == right.register.byte_15) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(sbyte))
		{
			existingRegister.sbyte_0 = (sbyte)((left.register.sbyte_0 == right.register.sbyte_0) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_1 = (sbyte)((left.register.sbyte_1 == right.register.sbyte_1) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_2 = (sbyte)((left.register.sbyte_2 == right.register.sbyte_2) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_3 = (sbyte)((left.register.sbyte_3 == right.register.sbyte_3) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_4 = (sbyte)((left.register.sbyte_4 == right.register.sbyte_4) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_5 = (sbyte)((left.register.sbyte_5 == right.register.sbyte_5) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_6 = (sbyte)((left.register.sbyte_6 == right.register.sbyte_6) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_7 = (sbyte)((left.register.sbyte_7 == right.register.sbyte_7) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_8 = (sbyte)((left.register.sbyte_8 == right.register.sbyte_8) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_9 = (sbyte)((left.register.sbyte_9 == right.register.sbyte_9) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_10 = (sbyte)((left.register.sbyte_10 == right.register.sbyte_10) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_11 = (sbyte)((left.register.sbyte_11 == right.register.sbyte_11) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_12 = (sbyte)((left.register.sbyte_12 == right.register.sbyte_12) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_13 = (sbyte)((left.register.sbyte_13 == right.register.sbyte_13) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_14 = (sbyte)((left.register.sbyte_14 == right.register.sbyte_14) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_15 = (sbyte)((left.register.sbyte_15 == right.register.sbyte_15) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(ushort))
		{
			existingRegister.uint16_0 = (ushort)((left.register.uint16_0 == right.register.uint16_0) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_1 = (ushort)((left.register.uint16_1 == right.register.uint16_1) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_2 = (ushort)((left.register.uint16_2 == right.register.uint16_2) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_3 = (ushort)((left.register.uint16_3 == right.register.uint16_3) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_4 = (ushort)((left.register.uint16_4 == right.register.uint16_4) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_5 = (ushort)((left.register.uint16_5 == right.register.uint16_5) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_6 = (ushort)((left.register.uint16_6 == right.register.uint16_6) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_7 = (ushort)((left.register.uint16_7 == right.register.uint16_7) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(short))
		{
			existingRegister.int16_0 = (short)((left.register.int16_0 == right.register.int16_0) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_1 = (short)((left.register.int16_1 == right.register.int16_1) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_2 = (short)((left.register.int16_2 == right.register.int16_2) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_3 = (short)((left.register.int16_3 == right.register.int16_3) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_4 = (short)((left.register.int16_4 == right.register.int16_4) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_5 = (short)((left.register.int16_5 == right.register.int16_5) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_6 = (short)((left.register.int16_6 == right.register.int16_6) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_7 = (short)((left.register.int16_7 == right.register.int16_7) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(uint))
		{
			existingRegister.uint32_0 = ((left.register.uint32_0 == right.register.uint32_0) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_1 = ((left.register.uint32_1 == right.register.uint32_1) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_2 = ((left.register.uint32_2 == right.register.uint32_2) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_3 = ((left.register.uint32_3 == right.register.uint32_3) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(int))
		{
			existingRegister.int32_0 = ((left.register.int32_0 == right.register.int32_0) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_1 = ((left.register.int32_1 == right.register.int32_1) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_2 = ((left.register.int32_2 == right.register.int32_2) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_3 = ((left.register.int32_3 == right.register.int32_3) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(ulong))
		{
			existingRegister.uint64_0 = ((left.register.uint64_0 == right.register.uint64_0) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
			existingRegister.uint64_1 = ((left.register.uint64_1 == right.register.uint64_1) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(long))
		{
			existingRegister.int64_0 = ((left.register.int64_0 == right.register.int64_0) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
			existingRegister.int64_1 = ((left.register.int64_1 == right.register.int64_1) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(float))
		{
			existingRegister.single_0 = ((left.register.single_0 == right.register.single_0) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_1 = ((left.register.single_1 == right.register.single_1) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_2 = ((left.register.single_2 == right.register.single_2) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_3 = ((left.register.single_3 == right.register.single_3) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(double))
		{
			existingRegister.double_0 = ((left.register.double_0 == right.register.double_0) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
			existingRegister.double_1 = ((left.register.double_1 == right.register.double_1) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
			return new Vector<T>(ref existingRegister);
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal unsafe static Vector<T> LessThan(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)(ScalarLessThan(left[i], right[i]) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)(ScalarLessThan(left[j], right[j]) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)(ScalarLessThan(left[k], right[k]) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)(ScalarLessThan(left[l], right[l]) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (ScalarLessThan(left[m], right[m]) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (ScalarLessThan(left[n], right[n]) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ScalarLessThan(left[num], right[num]) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (ScalarLessThan(left[num2], right[num2]) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (ScalarLessThan(left[num3], right[num3]) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (ScalarLessThan(left[num4], right[num4]) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Register existingRegister = default(Register);
		if (typeof(T) == typeof(byte))
		{
			existingRegister.byte_0 = (byte)((left.register.byte_0 < right.register.byte_0) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_1 = (byte)((left.register.byte_1 < right.register.byte_1) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_2 = (byte)((left.register.byte_2 < right.register.byte_2) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_3 = (byte)((left.register.byte_3 < right.register.byte_3) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_4 = (byte)((left.register.byte_4 < right.register.byte_4) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_5 = (byte)((left.register.byte_5 < right.register.byte_5) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_6 = (byte)((left.register.byte_6 < right.register.byte_6) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_7 = (byte)((left.register.byte_7 < right.register.byte_7) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_8 = (byte)((left.register.byte_8 < right.register.byte_8) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_9 = (byte)((left.register.byte_9 < right.register.byte_9) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_10 = (byte)((left.register.byte_10 < right.register.byte_10) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_11 = (byte)((left.register.byte_11 < right.register.byte_11) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_12 = (byte)((left.register.byte_12 < right.register.byte_12) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_13 = (byte)((left.register.byte_13 < right.register.byte_13) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_14 = (byte)((left.register.byte_14 < right.register.byte_14) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_15 = (byte)((left.register.byte_15 < right.register.byte_15) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(sbyte))
		{
			existingRegister.sbyte_0 = (sbyte)((left.register.sbyte_0 < right.register.sbyte_0) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_1 = (sbyte)((left.register.sbyte_1 < right.register.sbyte_1) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_2 = (sbyte)((left.register.sbyte_2 < right.register.sbyte_2) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_3 = (sbyte)((left.register.sbyte_3 < right.register.sbyte_3) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_4 = (sbyte)((left.register.sbyte_4 < right.register.sbyte_4) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_5 = (sbyte)((left.register.sbyte_5 < right.register.sbyte_5) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_6 = (sbyte)((left.register.sbyte_6 < right.register.sbyte_6) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_7 = (sbyte)((left.register.sbyte_7 < right.register.sbyte_7) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_8 = (sbyte)((left.register.sbyte_8 < right.register.sbyte_8) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_9 = (sbyte)((left.register.sbyte_9 < right.register.sbyte_9) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_10 = (sbyte)((left.register.sbyte_10 < right.register.sbyte_10) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_11 = (sbyte)((left.register.sbyte_11 < right.register.sbyte_11) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_12 = (sbyte)((left.register.sbyte_12 < right.register.sbyte_12) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_13 = (sbyte)((left.register.sbyte_13 < right.register.sbyte_13) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_14 = (sbyte)((left.register.sbyte_14 < right.register.sbyte_14) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_15 = (sbyte)((left.register.sbyte_15 < right.register.sbyte_15) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(ushort))
		{
			existingRegister.uint16_0 = (ushort)((left.register.uint16_0 < right.register.uint16_0) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_1 = (ushort)((left.register.uint16_1 < right.register.uint16_1) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_2 = (ushort)((left.register.uint16_2 < right.register.uint16_2) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_3 = (ushort)((left.register.uint16_3 < right.register.uint16_3) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_4 = (ushort)((left.register.uint16_4 < right.register.uint16_4) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_5 = (ushort)((left.register.uint16_5 < right.register.uint16_5) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_6 = (ushort)((left.register.uint16_6 < right.register.uint16_6) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_7 = (ushort)((left.register.uint16_7 < right.register.uint16_7) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(short))
		{
			existingRegister.int16_0 = (short)((left.register.int16_0 < right.register.int16_0) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_1 = (short)((left.register.int16_1 < right.register.int16_1) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_2 = (short)((left.register.int16_2 < right.register.int16_2) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_3 = (short)((left.register.int16_3 < right.register.int16_3) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_4 = (short)((left.register.int16_4 < right.register.int16_4) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_5 = (short)((left.register.int16_5 < right.register.int16_5) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_6 = (short)((left.register.int16_6 < right.register.int16_6) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_7 = (short)((left.register.int16_7 < right.register.int16_7) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(uint))
		{
			existingRegister.uint32_0 = ((left.register.uint32_0 < right.register.uint32_0) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_1 = ((left.register.uint32_1 < right.register.uint32_1) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_2 = ((left.register.uint32_2 < right.register.uint32_2) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_3 = ((left.register.uint32_3 < right.register.uint32_3) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(int))
		{
			existingRegister.int32_0 = ((left.register.int32_0 < right.register.int32_0) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_1 = ((left.register.int32_1 < right.register.int32_1) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_2 = ((left.register.int32_2 < right.register.int32_2) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_3 = ((left.register.int32_3 < right.register.int32_3) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(ulong))
		{
			existingRegister.uint64_0 = ((left.register.uint64_0 < right.register.uint64_0) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
			existingRegister.uint64_1 = ((left.register.uint64_1 < right.register.uint64_1) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(long))
		{
			existingRegister.int64_0 = ((left.register.int64_0 < right.register.int64_0) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
			existingRegister.int64_1 = ((left.register.int64_1 < right.register.int64_1) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(float))
		{
			existingRegister.single_0 = ((left.register.single_0 < right.register.single_0) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_1 = ((left.register.single_1 < right.register.single_1) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_2 = ((left.register.single_2 < right.register.single_2) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_3 = ((left.register.single_3 < right.register.single_3) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(double))
		{
			existingRegister.double_0 = ((left.register.double_0 < right.register.double_0) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
			existingRegister.double_1 = ((left.register.double_1 < right.register.double_1) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
			return new Vector<T>(ref existingRegister);
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal unsafe static Vector<T> GreaterThan(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)(ScalarGreaterThan(left[i], right[i]) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)(ScalarGreaterThan(left[j], right[j]) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)(ScalarGreaterThan(left[k], right[k]) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)(ScalarGreaterThan(left[l], right[l]) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (ScalarGreaterThan(left[m], right[m]) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (ScalarGreaterThan(left[n], right[n]) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ScalarGreaterThan(left[num], right[num]) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (ScalarGreaterThan(left[num2], right[num2]) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (ScalarGreaterThan(left[num3], right[num3]) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (ScalarGreaterThan(left[num4], right[num4]) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Register existingRegister = default(Register);
		if (typeof(T) == typeof(byte))
		{
			existingRegister.byte_0 = (byte)((left.register.byte_0 > right.register.byte_0) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_1 = (byte)((left.register.byte_1 > right.register.byte_1) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_2 = (byte)((left.register.byte_2 > right.register.byte_2) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_3 = (byte)((left.register.byte_3 > right.register.byte_3) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_4 = (byte)((left.register.byte_4 > right.register.byte_4) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_5 = (byte)((left.register.byte_5 > right.register.byte_5) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_6 = (byte)((left.register.byte_6 > right.register.byte_6) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_7 = (byte)((left.register.byte_7 > right.register.byte_7) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_8 = (byte)((left.register.byte_8 > right.register.byte_8) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_9 = (byte)((left.register.byte_9 > right.register.byte_9) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_10 = (byte)((left.register.byte_10 > right.register.byte_10) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_11 = (byte)((left.register.byte_11 > right.register.byte_11) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_12 = (byte)((left.register.byte_12 > right.register.byte_12) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_13 = (byte)((left.register.byte_13 > right.register.byte_13) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_14 = (byte)((left.register.byte_14 > right.register.byte_14) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			existingRegister.byte_15 = (byte)((left.register.byte_15 > right.register.byte_15) ? ConstantHelper.GetByteWithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(sbyte))
		{
			existingRegister.sbyte_0 = (sbyte)((left.register.sbyte_0 > right.register.sbyte_0) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_1 = (sbyte)((left.register.sbyte_1 > right.register.sbyte_1) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_2 = (sbyte)((left.register.sbyte_2 > right.register.sbyte_2) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_3 = (sbyte)((left.register.sbyte_3 > right.register.sbyte_3) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_4 = (sbyte)((left.register.sbyte_4 > right.register.sbyte_4) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_5 = (sbyte)((left.register.sbyte_5 > right.register.sbyte_5) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_6 = (sbyte)((left.register.sbyte_6 > right.register.sbyte_6) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_7 = (sbyte)((left.register.sbyte_7 > right.register.sbyte_7) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_8 = (sbyte)((left.register.sbyte_8 > right.register.sbyte_8) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_9 = (sbyte)((left.register.sbyte_9 > right.register.sbyte_9) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_10 = (sbyte)((left.register.sbyte_10 > right.register.sbyte_10) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_11 = (sbyte)((left.register.sbyte_11 > right.register.sbyte_11) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_12 = (sbyte)((left.register.sbyte_12 > right.register.sbyte_12) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_13 = (sbyte)((left.register.sbyte_13 > right.register.sbyte_13) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_14 = (sbyte)((left.register.sbyte_14 > right.register.sbyte_14) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			existingRegister.sbyte_15 = (sbyte)((left.register.sbyte_15 > right.register.sbyte_15) ? ConstantHelper.GetSByteWithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(ushort))
		{
			existingRegister.uint16_0 = (ushort)((left.register.uint16_0 > right.register.uint16_0) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_1 = (ushort)((left.register.uint16_1 > right.register.uint16_1) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_2 = (ushort)((left.register.uint16_2 > right.register.uint16_2) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_3 = (ushort)((left.register.uint16_3 > right.register.uint16_3) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_4 = (ushort)((left.register.uint16_4 > right.register.uint16_4) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_5 = (ushort)((left.register.uint16_5 > right.register.uint16_5) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_6 = (ushort)((left.register.uint16_6 > right.register.uint16_6) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			existingRegister.uint16_7 = (ushort)((left.register.uint16_7 > right.register.uint16_7) ? ConstantHelper.GetUInt16WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(short))
		{
			existingRegister.int16_0 = (short)((left.register.int16_0 > right.register.int16_0) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_1 = (short)((left.register.int16_1 > right.register.int16_1) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_2 = (short)((left.register.int16_2 > right.register.int16_2) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_3 = (short)((left.register.int16_3 > right.register.int16_3) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_4 = (short)((left.register.int16_4 > right.register.int16_4) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_5 = (short)((left.register.int16_5 > right.register.int16_5) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_6 = (short)((left.register.int16_6 > right.register.int16_6) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			existingRegister.int16_7 = (short)((left.register.int16_7 > right.register.int16_7) ? ConstantHelper.GetInt16WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(uint))
		{
			existingRegister.uint32_0 = ((left.register.uint32_0 > right.register.uint32_0) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_1 = ((left.register.uint32_1 > right.register.uint32_1) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_2 = ((left.register.uint32_2 > right.register.uint32_2) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			existingRegister.uint32_3 = ((left.register.uint32_3 > right.register.uint32_3) ? ConstantHelper.GetUInt32WithAllBitsSet() : 0u);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(int))
		{
			existingRegister.int32_0 = ((left.register.int32_0 > right.register.int32_0) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_1 = ((left.register.int32_1 > right.register.int32_1) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_2 = ((left.register.int32_2 > right.register.int32_2) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			existingRegister.int32_3 = ((left.register.int32_3 > right.register.int32_3) ? ConstantHelper.GetInt32WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(ulong))
		{
			existingRegister.uint64_0 = ((left.register.uint64_0 > right.register.uint64_0) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
			existingRegister.uint64_1 = ((left.register.uint64_1 > right.register.uint64_1) ? ConstantHelper.GetUInt64WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(long))
		{
			existingRegister.int64_0 = ((left.register.int64_0 > right.register.int64_0) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
			existingRegister.int64_1 = ((left.register.int64_1 > right.register.int64_1) ? ConstantHelper.GetInt64WithAllBitsSet() : 0);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(float))
		{
			existingRegister.single_0 = ((left.register.single_0 > right.register.single_0) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_1 = ((left.register.single_1 > right.register.single_1) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_2 = ((left.register.single_2 > right.register.single_2) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			existingRegister.single_3 = ((left.register.single_3 > right.register.single_3) ? ConstantHelper.GetSingleWithAllBitsSet() : 0f);
			return new Vector<T>(ref existingRegister);
		}
		if (typeof(T) == typeof(double))
		{
			existingRegister.double_0 = ((left.register.double_0 > right.register.double_0) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
			existingRegister.double_1 = ((left.register.double_1 > right.register.double_1) ? ConstantHelper.GetDoubleWithAllBitsSet() : 0.0);
			return new Vector<T>(ref existingRegister);
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[Intrinsic]
	internal static Vector<T> GreaterThanOrEqual(Vector<T> left, Vector<T> right)
	{
		return Equals(left, right) | GreaterThan(left, right);
	}

	[Intrinsic]
	internal static Vector<T> LessThanOrEqual(Vector<T> left, Vector<T> right)
	{
		return Equals(left, right) | LessThan(left, right);
	}

	[Intrinsic]
	internal static Vector<T> ConditionalSelect(Vector<T> condition, Vector<T> left, Vector<T> right)
	{
		return (left & condition) | System.Numerics.Vector.AndNot(right, condition);
	}

	[Intrinsic]
	internal unsafe static Vector<T> Abs(Vector<T> value)
	{
		if (typeof(T) == typeof(byte))
		{
			return value;
		}
		if (typeof(T) == typeof(ushort))
		{
			return value;
		}
		if (typeof(T) == typeof(uint))
		{
			return value;
		}
		if (typeof(T) == typeof(ulong))
		{
			return value;
		}
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr = stackalloc sbyte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (sbyte)(object)Math.Abs((sbyte)(object)value[i]);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr2 = stackalloc short[Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (short)(object)Math.Abs((short)(object)value[j]);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr3 = stackalloc int[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (int)(object)Math.Abs((int)(object)value[k]);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr4 = stackalloc long[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (long)(object)Math.Abs((long)(object)value[l]);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr5 = stackalloc float[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (float)(object)Math.Abs((float)(object)value[m]);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr6 = stackalloc double[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (double)(object)Math.Abs((double)(object)value[n]);
				}
				return new Vector<T>(ptr6);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		if (typeof(T) == typeof(sbyte))
		{
			value.register.sbyte_0 = Math.Abs(value.register.sbyte_0);
			value.register.sbyte_1 = Math.Abs(value.register.sbyte_1);
			value.register.sbyte_2 = Math.Abs(value.register.sbyte_2);
			value.register.sbyte_3 = Math.Abs(value.register.sbyte_3);
			value.register.sbyte_4 = Math.Abs(value.register.sbyte_4);
			value.register.sbyte_5 = Math.Abs(value.register.sbyte_5);
			value.register.sbyte_6 = Math.Abs(value.register.sbyte_6);
			value.register.sbyte_7 = Math.Abs(value.register.sbyte_7);
			value.register.sbyte_8 = Math.Abs(value.register.sbyte_8);
			value.register.sbyte_9 = Math.Abs(value.register.sbyte_9);
			value.register.sbyte_10 = Math.Abs(value.register.sbyte_10);
			value.register.sbyte_11 = Math.Abs(value.register.sbyte_11);
			value.register.sbyte_12 = Math.Abs(value.register.sbyte_12);
			value.register.sbyte_13 = Math.Abs(value.register.sbyte_13);
			value.register.sbyte_14 = Math.Abs(value.register.sbyte_14);
			value.register.sbyte_15 = Math.Abs(value.register.sbyte_15);
			return value;
		}
		if (typeof(T) == typeof(short))
		{
			value.register.int16_0 = Math.Abs(value.register.int16_0);
			value.register.int16_1 = Math.Abs(value.register.int16_1);
			value.register.int16_2 = Math.Abs(value.register.int16_2);
			value.register.int16_3 = Math.Abs(value.register.int16_3);
			value.register.int16_4 = Math.Abs(value.register.int16_4);
			value.register.int16_5 = Math.Abs(value.register.int16_5);
			value.register.int16_6 = Math.Abs(value.register.int16_6);
			value.register.int16_7 = Math.Abs(value.register.int16_7);
			return value;
		}
		if (typeof(T) == typeof(int))
		{
			value.register.int32_0 = Math.Abs(value.register.int32_0);
			value.register.int32_1 = Math.Abs(value.register.int32_1);
			value.register.int32_2 = Math.Abs(value.register.int32_2);
			value.register.int32_3 = Math.Abs(value.register.int32_3);
			return value;
		}
		if (typeof(T) == typeof(long))
		{
			value.register.int64_0 = Math.Abs(value.register.int64_0);
			value.register.int64_1 = Math.Abs(value.register.int64_1);
			return value;
		}
		if (typeof(T) == typeof(float))
		{
			value.register.single_0 = Math.Abs(value.register.single_0);
			value.register.single_1 = Math.Abs(value.register.single_1);
			value.register.single_2 = Math.Abs(value.register.single_2);
			value.register.single_3 = Math.Abs(value.register.single_3);
			return value;
		}
		if (typeof(T) == typeof(double))
		{
			value.register.double_0 = Math.Abs(value.register.double_0);
			value.register.double_1 = Math.Abs(value.register.double_1);
			return value;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[Intrinsic]
	internal unsafe static Vector<T> Min(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (ScalarLessThan(left[i], right[i]) ? ((byte)(object)left[i]) : ((byte)(object)right[i]));
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (ScalarLessThan(left[j], right[j]) ? ((sbyte)(object)left[j]) : ((sbyte)(object)right[j]));
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ScalarLessThan(left[k], right[k]) ? ((ushort)(object)left[k]) : ((ushort)(object)right[k]));
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (ScalarLessThan(left[l], right[l]) ? ((short)(object)left[l]) : ((short)(object)right[l]));
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (ScalarLessThan(left[m], right[m]) ? ((uint)(object)left[m]) : ((uint)(object)right[m]));
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (ScalarLessThan(left[n], right[n]) ? ((int)(object)left[n]) : ((int)(object)right[n]));
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ScalarLessThan(left[num], right[num]) ? ((ulong)(object)left[num]) : ((ulong)(object)right[num]));
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (ScalarLessThan(left[num2], right[num2]) ? ((long)(object)left[num2]) : ((long)(object)right[num2]));
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (ScalarLessThan(left[num3], right[num3]) ? ((float)(object)left[num3]) : ((float)(object)right[num3]));
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (ScalarLessThan(left[num4], right[num4]) ? ((double)(object)left[num4]) : ((double)(object)right[num4]));
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = ((left.register.byte_0 < right.register.byte_0) ? left.register.byte_0 : right.register.byte_0);
			result.register.byte_1 = ((left.register.byte_1 < right.register.byte_1) ? left.register.byte_1 : right.register.byte_1);
			result.register.byte_2 = ((left.register.byte_2 < right.register.byte_2) ? left.register.byte_2 : right.register.byte_2);
			result.register.byte_3 = ((left.register.byte_3 < right.register.byte_3) ? left.register.byte_3 : right.register.byte_3);
			result.register.byte_4 = ((left.register.byte_4 < right.register.byte_4) ? left.register.byte_4 : right.register.byte_4);
			result.register.byte_5 = ((left.register.byte_5 < right.register.byte_5) ? left.register.byte_5 : right.register.byte_5);
			result.register.byte_6 = ((left.register.byte_6 < right.register.byte_6) ? left.register.byte_6 : right.register.byte_6);
			result.register.byte_7 = ((left.register.byte_7 < right.register.byte_7) ? left.register.byte_7 : right.register.byte_7);
			result.register.byte_8 = ((left.register.byte_8 < right.register.byte_8) ? left.register.byte_8 : right.register.byte_8);
			result.register.byte_9 = ((left.register.byte_9 < right.register.byte_9) ? left.register.byte_9 : right.register.byte_9);
			result.register.byte_10 = ((left.register.byte_10 < right.register.byte_10) ? left.register.byte_10 : right.register.byte_10);
			result.register.byte_11 = ((left.register.byte_11 < right.register.byte_11) ? left.register.byte_11 : right.register.byte_11);
			result.register.byte_12 = ((left.register.byte_12 < right.register.byte_12) ? left.register.byte_12 : right.register.byte_12);
			result.register.byte_13 = ((left.register.byte_13 < right.register.byte_13) ? left.register.byte_13 : right.register.byte_13);
			result.register.byte_14 = ((left.register.byte_14 < right.register.byte_14) ? left.register.byte_14 : right.register.byte_14);
			result.register.byte_15 = ((left.register.byte_15 < right.register.byte_15) ? left.register.byte_15 : right.register.byte_15);
			return result;
		}
		if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = ((left.register.sbyte_0 < right.register.sbyte_0) ? left.register.sbyte_0 : right.register.sbyte_0);
			result.register.sbyte_1 = ((left.register.sbyte_1 < right.register.sbyte_1) ? left.register.sbyte_1 : right.register.sbyte_1);
			result.register.sbyte_2 = ((left.register.sbyte_2 < right.register.sbyte_2) ? left.register.sbyte_2 : right.register.sbyte_2);
			result.register.sbyte_3 = ((left.register.sbyte_3 < right.register.sbyte_3) ? left.register.sbyte_3 : right.register.sbyte_3);
			result.register.sbyte_4 = ((left.register.sbyte_4 < right.register.sbyte_4) ? left.register.sbyte_4 : right.register.sbyte_4);
			result.register.sbyte_5 = ((left.register.sbyte_5 < right.register.sbyte_5) ? left.register.sbyte_5 : right.register.sbyte_5);
			result.register.sbyte_6 = ((left.register.sbyte_6 < right.register.sbyte_6) ? left.register.sbyte_6 : right.register.sbyte_6);
			result.register.sbyte_7 = ((left.register.sbyte_7 < right.register.sbyte_7) ? left.register.sbyte_7 : right.register.sbyte_7);
			result.register.sbyte_8 = ((left.register.sbyte_8 < right.register.sbyte_8) ? left.register.sbyte_8 : right.register.sbyte_8);
			result.register.sbyte_9 = ((left.register.sbyte_9 < right.register.sbyte_9) ? left.register.sbyte_9 : right.register.sbyte_9);
			result.register.sbyte_10 = ((left.register.sbyte_10 < right.register.sbyte_10) ? left.register.sbyte_10 : right.register.sbyte_10);
			result.register.sbyte_11 = ((left.register.sbyte_11 < right.register.sbyte_11) ? left.register.sbyte_11 : right.register.sbyte_11);
			result.register.sbyte_12 = ((left.register.sbyte_12 < right.register.sbyte_12) ? left.register.sbyte_12 : right.register.sbyte_12);
			result.register.sbyte_13 = ((left.register.sbyte_13 < right.register.sbyte_13) ? left.register.sbyte_13 : right.register.sbyte_13);
			result.register.sbyte_14 = ((left.register.sbyte_14 < right.register.sbyte_14) ? left.register.sbyte_14 : right.register.sbyte_14);
			result.register.sbyte_15 = ((left.register.sbyte_15 < right.register.sbyte_15) ? left.register.sbyte_15 : right.register.sbyte_15);
			return result;
		}
		if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = ((left.register.uint16_0 < right.register.uint16_0) ? left.register.uint16_0 : right.register.uint16_0);
			result.register.uint16_1 = ((left.register.uint16_1 < right.register.uint16_1) ? left.register.uint16_1 : right.register.uint16_1);
			result.register.uint16_2 = ((left.register.uint16_2 < right.register.uint16_2) ? left.register.uint16_2 : right.register.uint16_2);
			result.register.uint16_3 = ((left.register.uint16_3 < right.register.uint16_3) ? left.register.uint16_3 : right.register.uint16_3);
			result.register.uint16_4 = ((left.register.uint16_4 < right.register.uint16_4) ? left.register.uint16_4 : right.register.uint16_4);
			result.register.uint16_5 = ((left.register.uint16_5 < right.register.uint16_5) ? left.register.uint16_5 : right.register.uint16_5);
			result.register.uint16_6 = ((left.register.uint16_6 < right.register.uint16_6) ? left.register.uint16_6 : right.register.uint16_6);
			result.register.uint16_7 = ((left.register.uint16_7 < right.register.uint16_7) ? left.register.uint16_7 : right.register.uint16_7);
			return result;
		}
		if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = ((left.register.int16_0 < right.register.int16_0) ? left.register.int16_0 : right.register.int16_0);
			result.register.int16_1 = ((left.register.int16_1 < right.register.int16_1) ? left.register.int16_1 : right.register.int16_1);
			result.register.int16_2 = ((left.register.int16_2 < right.register.int16_2) ? left.register.int16_2 : right.register.int16_2);
			result.register.int16_3 = ((left.register.int16_3 < right.register.int16_3) ? left.register.int16_3 : right.register.int16_3);
			result.register.int16_4 = ((left.register.int16_4 < right.register.int16_4) ? left.register.int16_4 : right.register.int16_4);
			result.register.int16_5 = ((left.register.int16_5 < right.register.int16_5) ? left.register.int16_5 : right.register.int16_5);
			result.register.int16_6 = ((left.register.int16_6 < right.register.int16_6) ? left.register.int16_6 : right.register.int16_6);
			result.register.int16_7 = ((left.register.int16_7 < right.register.int16_7) ? left.register.int16_7 : right.register.int16_7);
			return result;
		}
		if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = ((left.register.uint32_0 < right.register.uint32_0) ? left.register.uint32_0 : right.register.uint32_0);
			result.register.uint32_1 = ((left.register.uint32_1 < right.register.uint32_1) ? left.register.uint32_1 : right.register.uint32_1);
			result.register.uint32_2 = ((left.register.uint32_2 < right.register.uint32_2) ? left.register.uint32_2 : right.register.uint32_2);
			result.register.uint32_3 = ((left.register.uint32_3 < right.register.uint32_3) ? left.register.uint32_3 : right.register.uint32_3);
			return result;
		}
		if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = ((left.register.int32_0 < right.register.int32_0) ? left.register.int32_0 : right.register.int32_0);
			result.register.int32_1 = ((left.register.int32_1 < right.register.int32_1) ? left.register.int32_1 : right.register.int32_1);
			result.register.int32_2 = ((left.register.int32_2 < right.register.int32_2) ? left.register.int32_2 : right.register.int32_2);
			result.register.int32_3 = ((left.register.int32_3 < right.register.int32_3) ? left.register.int32_3 : right.register.int32_3);
			return result;
		}
		if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = ((left.register.uint64_0 < right.register.uint64_0) ? left.register.uint64_0 : right.register.uint64_0);
			result.register.uint64_1 = ((left.register.uint64_1 < right.register.uint64_1) ? left.register.uint64_1 : right.register.uint64_1);
			return result;
		}
		if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = ((left.register.int64_0 < right.register.int64_0) ? left.register.int64_0 : right.register.int64_0);
			result.register.int64_1 = ((left.register.int64_1 < right.register.int64_1) ? left.register.int64_1 : right.register.int64_1);
			return result;
		}
		if (typeof(T) == typeof(float))
		{
			result.register.single_0 = ((left.register.single_0 < right.register.single_0) ? left.register.single_0 : right.register.single_0);
			result.register.single_1 = ((left.register.single_1 < right.register.single_1) ? left.register.single_1 : right.register.single_1);
			result.register.single_2 = ((left.register.single_2 < right.register.single_2) ? left.register.single_2 : right.register.single_2);
			result.register.single_3 = ((left.register.single_3 < right.register.single_3) ? left.register.single_3 : right.register.single_3);
			return result;
		}
		if (typeof(T) == typeof(double))
		{
			result.register.double_0 = ((left.register.double_0 < right.register.double_0) ? left.register.double_0 : right.register.double_0);
			result.register.double_1 = ((left.register.double_1 < right.register.double_1) ? left.register.double_1 : right.register.double_1);
			return result;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[Intrinsic]
	internal unsafe static Vector<T> Max(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (ScalarGreaterThan(left[i], right[i]) ? ((byte)(object)left[i]) : ((byte)(object)right[i]));
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (ScalarGreaterThan(left[j], right[j]) ? ((sbyte)(object)left[j]) : ((sbyte)(object)right[j]));
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ScalarGreaterThan(left[k], right[k]) ? ((ushort)(object)left[k]) : ((ushort)(object)right[k]));
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (ScalarGreaterThan(left[l], right[l]) ? ((short)(object)left[l]) : ((short)(object)right[l]));
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (ScalarGreaterThan(left[m], right[m]) ? ((uint)(object)left[m]) : ((uint)(object)right[m]));
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (ScalarGreaterThan(left[n], right[n]) ? ((int)(object)left[n]) : ((int)(object)right[n]));
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ScalarGreaterThan(left[num], right[num]) ? ((ulong)(object)left[num]) : ((ulong)(object)right[num]));
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (ScalarGreaterThan(left[num2], right[num2]) ? ((long)(object)left[num2]) : ((long)(object)right[num2]));
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (ScalarGreaterThan(left[num3], right[num3]) ? ((float)(object)left[num3]) : ((float)(object)right[num3]));
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = (ScalarGreaterThan(left[num4], right[num4]) ? ((double)(object)left[num4]) : ((double)(object)right[num4]));
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		Vector<T> result = default(Vector<T>);
		if (typeof(T) == typeof(byte))
		{
			result.register.byte_0 = ((left.register.byte_0 > right.register.byte_0) ? left.register.byte_0 : right.register.byte_0);
			result.register.byte_1 = ((left.register.byte_1 > right.register.byte_1) ? left.register.byte_1 : right.register.byte_1);
			result.register.byte_2 = ((left.register.byte_2 > right.register.byte_2) ? left.register.byte_2 : right.register.byte_2);
			result.register.byte_3 = ((left.register.byte_3 > right.register.byte_3) ? left.register.byte_3 : right.register.byte_3);
			result.register.byte_4 = ((left.register.byte_4 > right.register.byte_4) ? left.register.byte_4 : right.register.byte_4);
			result.register.byte_5 = ((left.register.byte_5 > right.register.byte_5) ? left.register.byte_5 : right.register.byte_5);
			result.register.byte_6 = ((left.register.byte_6 > right.register.byte_6) ? left.register.byte_6 : right.register.byte_6);
			result.register.byte_7 = ((left.register.byte_7 > right.register.byte_7) ? left.register.byte_7 : right.register.byte_7);
			result.register.byte_8 = ((left.register.byte_8 > right.register.byte_8) ? left.register.byte_8 : right.register.byte_8);
			result.register.byte_9 = ((left.register.byte_9 > right.register.byte_9) ? left.register.byte_9 : right.register.byte_9);
			result.register.byte_10 = ((left.register.byte_10 > right.register.byte_10) ? left.register.byte_10 : right.register.byte_10);
			result.register.byte_11 = ((left.register.byte_11 > right.register.byte_11) ? left.register.byte_11 : right.register.byte_11);
			result.register.byte_12 = ((left.register.byte_12 > right.register.byte_12) ? left.register.byte_12 : right.register.byte_12);
			result.register.byte_13 = ((left.register.byte_13 > right.register.byte_13) ? left.register.byte_13 : right.register.byte_13);
			result.register.byte_14 = ((left.register.byte_14 > right.register.byte_14) ? left.register.byte_14 : right.register.byte_14);
			result.register.byte_15 = ((left.register.byte_15 > right.register.byte_15) ? left.register.byte_15 : right.register.byte_15);
			return result;
		}
		if (typeof(T) == typeof(sbyte))
		{
			result.register.sbyte_0 = ((left.register.sbyte_0 > right.register.sbyte_0) ? left.register.sbyte_0 : right.register.sbyte_0);
			result.register.sbyte_1 = ((left.register.sbyte_1 > right.register.sbyte_1) ? left.register.sbyte_1 : right.register.sbyte_1);
			result.register.sbyte_2 = ((left.register.sbyte_2 > right.register.sbyte_2) ? left.register.sbyte_2 : right.register.sbyte_2);
			result.register.sbyte_3 = ((left.register.sbyte_3 > right.register.sbyte_3) ? left.register.sbyte_3 : right.register.sbyte_3);
			result.register.sbyte_4 = ((left.register.sbyte_4 > right.register.sbyte_4) ? left.register.sbyte_4 : right.register.sbyte_4);
			result.register.sbyte_5 = ((left.register.sbyte_5 > right.register.sbyte_5) ? left.register.sbyte_5 : right.register.sbyte_5);
			result.register.sbyte_6 = ((left.register.sbyte_6 > right.register.sbyte_6) ? left.register.sbyte_6 : right.register.sbyte_6);
			result.register.sbyte_7 = ((left.register.sbyte_7 > right.register.sbyte_7) ? left.register.sbyte_7 : right.register.sbyte_7);
			result.register.sbyte_8 = ((left.register.sbyte_8 > right.register.sbyte_8) ? left.register.sbyte_8 : right.register.sbyte_8);
			result.register.sbyte_9 = ((left.register.sbyte_9 > right.register.sbyte_9) ? left.register.sbyte_9 : right.register.sbyte_9);
			result.register.sbyte_10 = ((left.register.sbyte_10 > right.register.sbyte_10) ? left.register.sbyte_10 : right.register.sbyte_10);
			result.register.sbyte_11 = ((left.register.sbyte_11 > right.register.sbyte_11) ? left.register.sbyte_11 : right.register.sbyte_11);
			result.register.sbyte_12 = ((left.register.sbyte_12 > right.register.sbyte_12) ? left.register.sbyte_12 : right.register.sbyte_12);
			result.register.sbyte_13 = ((left.register.sbyte_13 > right.register.sbyte_13) ? left.register.sbyte_13 : right.register.sbyte_13);
			result.register.sbyte_14 = ((left.register.sbyte_14 > right.register.sbyte_14) ? left.register.sbyte_14 : right.register.sbyte_14);
			result.register.sbyte_15 = ((left.register.sbyte_15 > right.register.sbyte_15) ? left.register.sbyte_15 : right.register.sbyte_15);
			return result;
		}
		if (typeof(T) == typeof(ushort))
		{
			result.register.uint16_0 = ((left.register.uint16_0 > right.register.uint16_0) ? left.register.uint16_0 : right.register.uint16_0);
			result.register.uint16_1 = ((left.register.uint16_1 > right.register.uint16_1) ? left.register.uint16_1 : right.register.uint16_1);
			result.register.uint16_2 = ((left.register.uint16_2 > right.register.uint16_2) ? left.register.uint16_2 : right.register.uint16_2);
			result.register.uint16_3 = ((left.register.uint16_3 > right.register.uint16_3) ? left.register.uint16_3 : right.register.uint16_3);
			result.register.uint16_4 = ((left.register.uint16_4 > right.register.uint16_4) ? left.register.uint16_4 : right.register.uint16_4);
			result.register.uint16_5 = ((left.register.uint16_5 > right.register.uint16_5) ? left.register.uint16_5 : right.register.uint16_5);
			result.register.uint16_6 = ((left.register.uint16_6 > right.register.uint16_6) ? left.register.uint16_6 : right.register.uint16_6);
			result.register.uint16_7 = ((left.register.uint16_7 > right.register.uint16_7) ? left.register.uint16_7 : right.register.uint16_7);
			return result;
		}
		if (typeof(T) == typeof(short))
		{
			result.register.int16_0 = ((left.register.int16_0 > right.register.int16_0) ? left.register.int16_0 : right.register.int16_0);
			result.register.int16_1 = ((left.register.int16_1 > right.register.int16_1) ? left.register.int16_1 : right.register.int16_1);
			result.register.int16_2 = ((left.register.int16_2 > right.register.int16_2) ? left.register.int16_2 : right.register.int16_2);
			result.register.int16_3 = ((left.register.int16_3 > right.register.int16_3) ? left.register.int16_3 : right.register.int16_3);
			result.register.int16_4 = ((left.register.int16_4 > right.register.int16_4) ? left.register.int16_4 : right.register.int16_4);
			result.register.int16_5 = ((left.register.int16_5 > right.register.int16_5) ? left.register.int16_5 : right.register.int16_5);
			result.register.int16_6 = ((left.register.int16_6 > right.register.int16_6) ? left.register.int16_6 : right.register.int16_6);
			result.register.int16_7 = ((left.register.int16_7 > right.register.int16_7) ? left.register.int16_7 : right.register.int16_7);
			return result;
		}
		if (typeof(T) == typeof(uint))
		{
			result.register.uint32_0 = ((left.register.uint32_0 > right.register.uint32_0) ? left.register.uint32_0 : right.register.uint32_0);
			result.register.uint32_1 = ((left.register.uint32_1 > right.register.uint32_1) ? left.register.uint32_1 : right.register.uint32_1);
			result.register.uint32_2 = ((left.register.uint32_2 > right.register.uint32_2) ? left.register.uint32_2 : right.register.uint32_2);
			result.register.uint32_3 = ((left.register.uint32_3 > right.register.uint32_3) ? left.register.uint32_3 : right.register.uint32_3);
			return result;
		}
		if (typeof(T) == typeof(int))
		{
			result.register.int32_0 = ((left.register.int32_0 > right.register.int32_0) ? left.register.int32_0 : right.register.int32_0);
			result.register.int32_1 = ((left.register.int32_1 > right.register.int32_1) ? left.register.int32_1 : right.register.int32_1);
			result.register.int32_2 = ((left.register.int32_2 > right.register.int32_2) ? left.register.int32_2 : right.register.int32_2);
			result.register.int32_3 = ((left.register.int32_3 > right.register.int32_3) ? left.register.int32_3 : right.register.int32_3);
			return result;
		}
		if (typeof(T) == typeof(ulong))
		{
			result.register.uint64_0 = ((left.register.uint64_0 > right.register.uint64_0) ? left.register.uint64_0 : right.register.uint64_0);
			result.register.uint64_1 = ((left.register.uint64_1 > right.register.uint64_1) ? left.register.uint64_1 : right.register.uint64_1);
			return result;
		}
		if (typeof(T) == typeof(long))
		{
			result.register.int64_0 = ((left.register.int64_0 > right.register.int64_0) ? left.register.int64_0 : right.register.int64_0);
			result.register.int64_1 = ((left.register.int64_1 > right.register.int64_1) ? left.register.int64_1 : right.register.int64_1);
			return result;
		}
		if (typeof(T) == typeof(float))
		{
			result.register.single_0 = ((left.register.single_0 > right.register.single_0) ? left.register.single_0 : right.register.single_0);
			result.register.single_1 = ((left.register.single_1 > right.register.single_1) ? left.register.single_1 : right.register.single_1);
			result.register.single_2 = ((left.register.single_2 > right.register.single_2) ? left.register.single_2 : right.register.single_2);
			result.register.single_3 = ((left.register.single_3 > right.register.single_3) ? left.register.single_3 : right.register.single_3);
			return result;
		}
		if (typeof(T) == typeof(double))
		{
			result.register.double_0 = ((left.register.double_0 > right.register.double_0) ? left.register.double_0 : right.register.double_0);
			result.register.double_1 = ((left.register.double_1 > right.register.double_1) ? left.register.double_1 : right.register.double_1);
			return result;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[Intrinsic]
	internal static T DotProduct(Vector<T> left, Vector<T> right)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			T val = default(T);
			for (int i = 0; i < Count; i++)
			{
				val = ScalarAdd(val, ScalarMultiply(left[i], right[i]));
			}
			return val;
		}
		if (typeof(T) == typeof(byte))
		{
			byte b = 0;
			b += (byte)(left.register.byte_0 * right.register.byte_0);
			b += (byte)(left.register.byte_1 * right.register.byte_1);
			b += (byte)(left.register.byte_2 * right.register.byte_2);
			b += (byte)(left.register.byte_3 * right.register.byte_3);
			b += (byte)(left.register.byte_4 * right.register.byte_4);
			b += (byte)(left.register.byte_5 * right.register.byte_5);
			b += (byte)(left.register.byte_6 * right.register.byte_6);
			b += (byte)(left.register.byte_7 * right.register.byte_7);
			b += (byte)(left.register.byte_8 * right.register.byte_8);
			b += (byte)(left.register.byte_9 * right.register.byte_9);
			b += (byte)(left.register.byte_10 * right.register.byte_10);
			b += (byte)(left.register.byte_11 * right.register.byte_11);
			b += (byte)(left.register.byte_12 * right.register.byte_12);
			b += (byte)(left.register.byte_13 * right.register.byte_13);
			b += (byte)(left.register.byte_14 * right.register.byte_14);
			b += (byte)(left.register.byte_15 * right.register.byte_15);
			return (T)(object)b;
		}
		if (typeof(T) == typeof(sbyte))
		{
			sbyte b2 = 0;
			b2 += (sbyte)(left.register.sbyte_0 * right.register.sbyte_0);
			b2 += (sbyte)(left.register.sbyte_1 * right.register.sbyte_1);
			b2 += (sbyte)(left.register.sbyte_2 * right.register.sbyte_2);
			b2 += (sbyte)(left.register.sbyte_3 * right.register.sbyte_3);
			b2 += (sbyte)(left.register.sbyte_4 * right.register.sbyte_4);
			b2 += (sbyte)(left.register.sbyte_5 * right.register.sbyte_5);
			b2 += (sbyte)(left.register.sbyte_6 * right.register.sbyte_6);
			b2 += (sbyte)(left.register.sbyte_7 * right.register.sbyte_7);
			b2 += (sbyte)(left.register.sbyte_8 * right.register.sbyte_8);
			b2 += (sbyte)(left.register.sbyte_9 * right.register.sbyte_9);
			b2 += (sbyte)(left.register.sbyte_10 * right.register.sbyte_10);
			b2 += (sbyte)(left.register.sbyte_11 * right.register.sbyte_11);
			b2 += (sbyte)(left.register.sbyte_12 * right.register.sbyte_12);
			b2 += (sbyte)(left.register.sbyte_13 * right.register.sbyte_13);
			b2 += (sbyte)(left.register.sbyte_14 * right.register.sbyte_14);
			b2 += (sbyte)(left.register.sbyte_15 * right.register.sbyte_15);
			return (T)(object)b2;
		}
		if (typeof(T) == typeof(ushort))
		{
			ushort num = 0;
			num += (ushort)(left.register.uint16_0 * right.register.uint16_0);
			num += (ushort)(left.register.uint16_1 * right.register.uint16_1);
			num += (ushort)(left.register.uint16_2 * right.register.uint16_2);
			num += (ushort)(left.register.uint16_3 * right.register.uint16_3);
			num += (ushort)(left.register.uint16_4 * right.register.uint16_4);
			num += (ushort)(left.register.uint16_5 * right.register.uint16_5);
			num += (ushort)(left.register.uint16_6 * right.register.uint16_6);
			num += (ushort)(left.register.uint16_7 * right.register.uint16_7);
			return (T)(object)num;
		}
		if (typeof(T) == typeof(short))
		{
			short num2 = 0;
			num2 += (short)(left.register.int16_0 * right.register.int16_0);
			num2 += (short)(left.register.int16_1 * right.register.int16_1);
			num2 += (short)(left.register.int16_2 * right.register.int16_2);
			num2 += (short)(left.register.int16_3 * right.register.int16_3);
			num2 += (short)(left.register.int16_4 * right.register.int16_4);
			num2 += (short)(left.register.int16_5 * right.register.int16_5);
			num2 += (short)(left.register.int16_6 * right.register.int16_6);
			num2 += (short)(left.register.int16_7 * right.register.int16_7);
			return (T)(object)num2;
		}
		if (typeof(T) == typeof(uint))
		{
			uint num3 = 0u;
			num3 += left.register.uint32_0 * right.register.uint32_0;
			num3 += left.register.uint32_1 * right.register.uint32_1;
			num3 += left.register.uint32_2 * right.register.uint32_2;
			num3 += left.register.uint32_3 * right.register.uint32_3;
			return (T)(object)num3;
		}
		if (typeof(T) == typeof(int))
		{
			int num4 = 0;
			num4 += left.register.int32_0 * right.register.int32_0;
			num4 += left.register.int32_1 * right.register.int32_1;
			num4 += left.register.int32_2 * right.register.int32_2;
			num4 += left.register.int32_3 * right.register.int32_3;
			return (T)(object)num4;
		}
		if (typeof(T) == typeof(ulong))
		{
			ulong num5 = 0uL;
			num5 += left.register.uint64_0 * right.register.uint64_0;
			num5 += left.register.uint64_1 * right.register.uint64_1;
			return (T)(object)num5;
		}
		if (typeof(T) == typeof(long))
		{
			long num6 = 0L;
			num6 += left.register.int64_0 * right.register.int64_0;
			num6 += left.register.int64_1 * right.register.int64_1;
			return (T)(object)num6;
		}
		if (typeof(T) == typeof(float))
		{
			float num7 = 0f;
			num7 += left.register.single_0 * right.register.single_0;
			num7 += left.register.single_1 * right.register.single_1;
			num7 += left.register.single_2 * right.register.single_2;
			num7 += left.register.single_3 * right.register.single_3;
			return (T)(object)num7;
		}
		if (typeof(T) == typeof(double))
		{
			double num8 = 0.0;
			num8 += left.register.double_0 * right.register.double_0;
			num8 += left.register.double_1 * right.register.double_1;
			return (T)(object)num8;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[Intrinsic]
	internal unsafe static Vector<T> SquareRoot(Vector<T> value)
	{
		if (System.Numerics.Vector.IsHardwareAccelerated)
		{
			if (typeof(T) == typeof(byte))
			{
				byte* ptr = stackalloc byte[(int)(uint)Count];
				for (int i = 0; i < Count; i++)
				{
					ptr[i] = (byte)Math.Sqrt((int)(byte)(object)value[i]);
				}
				return new Vector<T>(ptr);
			}
			if (typeof(T) == typeof(sbyte))
			{
				sbyte* ptr2 = stackalloc sbyte[(int)(uint)Count];
				for (int j = 0; j < Count; j++)
				{
					ptr2[j] = (sbyte)Math.Sqrt((sbyte)(object)value[j]);
				}
				return new Vector<T>(ptr2);
			}
			if (typeof(T) == typeof(ushort))
			{
				ushort* ptr3 = stackalloc ushort[Count];
				for (int k = 0; k < Count; k++)
				{
					ptr3[k] = (ushort)Math.Sqrt((int)(ushort)(object)value[k]);
				}
				return new Vector<T>(ptr3);
			}
			if (typeof(T) == typeof(short))
			{
				short* ptr4 = stackalloc short[Count];
				for (int l = 0; l < Count; l++)
				{
					ptr4[l] = (short)Math.Sqrt((short)(object)value[l]);
				}
				return new Vector<T>(ptr4);
			}
			if (typeof(T) == typeof(uint))
			{
				uint* ptr5 = stackalloc uint[Count];
				for (int m = 0; m < Count; m++)
				{
					ptr5[m] = (uint)Math.Sqrt((uint)(object)value[m]);
				}
				return new Vector<T>(ptr5);
			}
			if (typeof(T) == typeof(int))
			{
				int* ptr6 = stackalloc int[Count];
				for (int n = 0; n < Count; n++)
				{
					ptr6[n] = (int)Math.Sqrt((int)(object)value[n]);
				}
				return new Vector<T>(ptr6);
			}
			if (typeof(T) == typeof(ulong))
			{
				ulong* ptr7 = stackalloc ulong[Count];
				for (int num = 0; num < Count; num++)
				{
					ptr7[num] = (ulong)Math.Sqrt((ulong)(object)value[num]);
				}
				return new Vector<T>(ptr7);
			}
			if (typeof(T) == typeof(long))
			{
				long* ptr8 = stackalloc long[Count];
				for (int num2 = 0; num2 < Count; num2++)
				{
					ptr8[num2] = (long)Math.Sqrt((long)(object)value[num2]);
				}
				return new Vector<T>(ptr8);
			}
			if (typeof(T) == typeof(float))
			{
				float* ptr9 = stackalloc float[Count];
				for (int num3 = 0; num3 < Count; num3++)
				{
					ptr9[num3] = (float)Math.Sqrt((float)(object)value[num3]);
				}
				return new Vector<T>(ptr9);
			}
			if (typeof(T) == typeof(double))
			{
				double* ptr10 = stackalloc double[Count];
				for (int num4 = 0; num4 < Count; num4++)
				{
					ptr10[num4] = Math.Sqrt((double)(object)value[num4]);
				}
				return new Vector<T>(ptr10);
			}
			throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
		}
		if (typeof(T) == typeof(byte))
		{
			value.register.byte_0 = (byte)Math.Sqrt((int)value.register.byte_0);
			value.register.byte_1 = (byte)Math.Sqrt((int)value.register.byte_1);
			value.register.byte_2 = (byte)Math.Sqrt((int)value.register.byte_2);
			value.register.byte_3 = (byte)Math.Sqrt((int)value.register.byte_3);
			value.register.byte_4 = (byte)Math.Sqrt((int)value.register.byte_4);
			value.register.byte_5 = (byte)Math.Sqrt((int)value.register.byte_5);
			value.register.byte_6 = (byte)Math.Sqrt((int)value.register.byte_6);
			value.register.byte_7 = (byte)Math.Sqrt((int)value.register.byte_7);
			value.register.byte_8 = (byte)Math.Sqrt((int)value.register.byte_8);
			value.register.byte_9 = (byte)Math.Sqrt((int)value.register.byte_9);
			value.register.byte_10 = (byte)Math.Sqrt((int)value.register.byte_10);
			value.register.byte_11 = (byte)Math.Sqrt((int)value.register.byte_11);
			value.register.byte_12 = (byte)Math.Sqrt((int)value.register.byte_12);
			value.register.byte_13 = (byte)Math.Sqrt((int)value.register.byte_13);
			value.register.byte_14 = (byte)Math.Sqrt((int)value.register.byte_14);
			value.register.byte_15 = (byte)Math.Sqrt((int)value.register.byte_15);
			return value;
		}
		if (typeof(T) == typeof(sbyte))
		{
			value.register.sbyte_0 = (sbyte)Math.Sqrt(value.register.sbyte_0);
			value.register.sbyte_1 = (sbyte)Math.Sqrt(value.register.sbyte_1);
			value.register.sbyte_2 = (sbyte)Math.Sqrt(value.register.sbyte_2);
			value.register.sbyte_3 = (sbyte)Math.Sqrt(value.register.sbyte_3);
			value.register.sbyte_4 = (sbyte)Math.Sqrt(value.register.sbyte_4);
			value.register.sbyte_5 = (sbyte)Math.Sqrt(value.register.sbyte_5);
			value.register.sbyte_6 = (sbyte)Math.Sqrt(value.register.sbyte_6);
			value.register.sbyte_7 = (sbyte)Math.Sqrt(value.register.sbyte_7);
			value.register.sbyte_8 = (sbyte)Math.Sqrt(value.register.sbyte_8);
			value.register.sbyte_9 = (sbyte)Math.Sqrt(value.register.sbyte_9);
			value.register.sbyte_10 = (sbyte)Math.Sqrt(value.register.sbyte_10);
			value.register.sbyte_11 = (sbyte)Math.Sqrt(value.register.sbyte_11);
			value.register.sbyte_12 = (sbyte)Math.Sqrt(value.register.sbyte_12);
			value.register.sbyte_13 = (sbyte)Math.Sqrt(value.register.sbyte_13);
			value.register.sbyte_14 = (sbyte)Math.Sqrt(value.register.sbyte_14);
			value.register.sbyte_15 = (sbyte)Math.Sqrt(value.register.sbyte_15);
			return value;
		}
		if (typeof(T) == typeof(ushort))
		{
			value.register.uint16_0 = (ushort)Math.Sqrt((int)value.register.uint16_0);
			value.register.uint16_1 = (ushort)Math.Sqrt((int)value.register.uint16_1);
			value.register.uint16_2 = (ushort)Math.Sqrt((int)value.register.uint16_2);
			value.register.uint16_3 = (ushort)Math.Sqrt((int)value.register.uint16_3);
			value.register.uint16_4 = (ushort)Math.Sqrt((int)value.register.uint16_4);
			value.register.uint16_5 = (ushort)Math.Sqrt((int)value.register.uint16_5);
			value.register.uint16_6 = (ushort)Math.Sqrt((int)value.register.uint16_6);
			value.register.uint16_7 = (ushort)Math.Sqrt((int)value.register.uint16_7);
			return value;
		}
		if (typeof(T) == typeof(short))
		{
			value.register.int16_0 = (short)Math.Sqrt(value.register.int16_0);
			value.register.int16_1 = (short)Math.Sqrt(value.register.int16_1);
			value.register.int16_2 = (short)Math.Sqrt(value.register.int16_2);
			value.register.int16_3 = (short)Math.Sqrt(value.register.int16_3);
			value.register.int16_4 = (short)Math.Sqrt(value.register.int16_4);
			value.register.int16_5 = (short)Math.Sqrt(value.register.int16_5);
			value.register.int16_6 = (short)Math.Sqrt(value.register.int16_6);
			value.register.int16_7 = (short)Math.Sqrt(value.register.int16_7);
			return value;
		}
		if (typeof(T) == typeof(uint))
		{
			value.register.uint32_0 = (uint)Math.Sqrt(value.register.uint32_0);
			value.register.uint32_1 = (uint)Math.Sqrt(value.register.uint32_1);
			value.register.uint32_2 = (uint)Math.Sqrt(value.register.uint32_2);
			value.register.uint32_3 = (uint)Math.Sqrt(value.register.uint32_3);
			return value;
		}
		if (typeof(T) == typeof(int))
		{
			value.register.int32_0 = (int)Math.Sqrt(value.register.int32_0);
			value.register.int32_1 = (int)Math.Sqrt(value.register.int32_1);
			value.register.int32_2 = (int)Math.Sqrt(value.register.int32_2);
			value.register.int32_3 = (int)Math.Sqrt(value.register.int32_3);
			return value;
		}
		if (typeof(T) == typeof(ulong))
		{
			value.register.uint64_0 = (ulong)Math.Sqrt(value.register.uint64_0);
			value.register.uint64_1 = (ulong)Math.Sqrt(value.register.uint64_1);
			return value;
		}
		if (typeof(T) == typeof(long))
		{
			value.register.int64_0 = (long)Math.Sqrt(value.register.int64_0);
			value.register.int64_1 = (long)Math.Sqrt(value.register.int64_1);
			return value;
		}
		if (typeof(T) == typeof(float))
		{
			value.register.single_0 = (float)Math.Sqrt(value.register.single_0);
			value.register.single_1 = (float)Math.Sqrt(value.register.single_1);
			value.register.single_2 = (float)Math.Sqrt(value.register.single_2);
			value.register.single_3 = (float)Math.Sqrt(value.register.single_3);
			return value;
		}
		if (typeof(T) == typeof(double))
		{
			value.register.double_0 = Math.Sqrt(value.register.double_0);
			value.register.double_1 = Math.Sqrt(value.register.double_1);
			return value;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarEquals(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left == (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left == (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left == (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left == (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left == (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left == (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left == (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left == (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left == (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left == (double)(object)right;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarLessThan(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left < (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left < (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left < (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left < (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left < (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left < (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left < (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left < (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left < (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left < (double)(object)right;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarGreaterThan(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left > (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left > (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left > (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left > (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left > (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left > (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left > (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left > (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left > (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left > (double)(object)right;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarAdd(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left + (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left + (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left + (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left + (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left + (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left + (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left + (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left + (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left + (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left + (double)(object)right);
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarSubtract(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left - (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left - (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left - (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left - (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left - (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left - (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left - (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left - (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left - (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left - (double)(object)right);
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarMultiply(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left * (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left * (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left * (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left * (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left * (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left * (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left * (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left * (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left * (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left * (double)(object)right);
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarDivide(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left / (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left / (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left / (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left / (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left / (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left / (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left / (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left / (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left / (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left / (double)(object)right);
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T GetOneValue()
	{
		if (typeof(T) == typeof(byte))
		{
			byte b = 1;
			return (T)(object)b;
		}
		if (typeof(T) == typeof(sbyte))
		{
			sbyte b2 = 1;
			return (T)(object)b2;
		}
		if (typeof(T) == typeof(ushort))
		{
			ushort num = 1;
			return (T)(object)num;
		}
		if (typeof(T) == typeof(short))
		{
			short num2 = 1;
			return (T)(object)num2;
		}
		if (typeof(T) == typeof(uint))
		{
			uint num3 = 1u;
			return (T)(object)num3;
		}
		if (typeof(T) == typeof(int))
		{
			int num4 = 1;
			return (T)(object)num4;
		}
		if (typeof(T) == typeof(ulong))
		{
			ulong num5 = 1uL;
			return (T)(object)num5;
		}
		if (typeof(T) == typeof(long))
		{
			long num6 = 1L;
			return (T)(object)num6;
		}
		if (typeof(T) == typeof(float))
		{
			float num7 = 1f;
			return (T)(object)num7;
		}
		if (typeof(T) == typeof(double))
		{
			double num8 = 1.0;
			return (T)(object)num8;
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T GetAllBitsSetValue()
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)ConstantHelper.GetByteWithAllBitsSet();
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)ConstantHelper.GetSByteWithAllBitsSet();
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)ConstantHelper.GetUInt16WithAllBitsSet();
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)ConstantHelper.GetInt16WithAllBitsSet();
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)ConstantHelper.GetUInt32WithAllBitsSet();
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)ConstantHelper.GetInt32WithAllBitsSet();
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)ConstantHelper.GetUInt64WithAllBitsSet();
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)ConstantHelper.GetInt64WithAllBitsSet();
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)ConstantHelper.GetSingleWithAllBitsSet();
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)ConstantHelper.GetDoubleWithAllBitsSet();
		}
		throw new NotSupportedException(_003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR.Arg_TypeNotSupported);
	}
}
[Intrinsic]
internal static class Vector
{
	public static bool IsHardwareAccelerated
	{
		[Intrinsic]
		get
		{
			return false;
		}
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<byte> source, out Vector<ushort> low, out Vector<ushort> high)
	{
		int count = Vector<byte>.Count;
		ushort* ptr = stackalloc ushort[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		ushort* ptr2 = stackalloc ushort[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = new Vector<ushort>(ptr);
		high = new Vector<ushort>(ptr2);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<ushort> source, out Vector<uint> low, out Vector<uint> high)
	{
		int count = Vector<ushort>.Count;
		uint* ptr = stackalloc uint[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		uint* ptr2 = stackalloc uint[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = new Vector<uint>(ptr);
		high = new Vector<uint>(ptr2);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<uint> source, out Vector<ulong> low, out Vector<ulong> high)
	{
		int count = Vector<uint>.Count;
		ulong* ptr = stackalloc ulong[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		ulong* ptr2 = stackalloc ulong[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = new Vector<ulong>(ptr);
		high = new Vector<ulong>(ptr2);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<sbyte> source, out Vector<short> low, out Vector<short> high)
	{
		int count = Vector<sbyte>.Count;
		short* ptr = stackalloc short[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		short* ptr2 = stackalloc short[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = new Vector<short>(ptr);
		high = new Vector<short>(ptr2);
	}

	[Intrinsic]
	public unsafe static void Widen(Vector<short> source, out Vector<int> low, out Vector<int> high)
	{
		int count = Vector<short>.Count;
		int* ptr = stackalloc int[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		int* ptr2 = stackalloc int[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = new Vector<int>(ptr);
		high = new Vector<int>(ptr2);
	}

	[Intrinsic]
	public unsafe static void Widen(Vector<int> source, out Vector<long> low, out Vector<long> high)
	{
		int count = Vector<int>.Count;
		long* ptr = stackalloc long[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		long* ptr2 = stackalloc long[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = new Vector<long>(ptr);
		high = new Vector<long>(ptr2);
	}

	[Intrinsic]
	public unsafe static void Widen(Vector<float> source, out Vector<double> low, out Vector<double> high)
	{
		int count = Vector<float>.Count;
		double* ptr = stackalloc double[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		double* ptr2 = stackalloc double[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = new Vector<double>(ptr);
		high = new Vector<double>(ptr2);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<byte> Narrow(Vector<ushort> low, Vector<ushort> high)
	{
		int count = Vector<byte>.Count;
		byte* ptr = stackalloc byte[(int)(uint)count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (byte)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (byte)high[j];
		}
		return new Vector<byte>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<ushort> Narrow(Vector<uint> low, Vector<uint> high)
	{
		int count = Vector<ushort>.Count;
		ushort* ptr = stackalloc ushort[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (ushort)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (ushort)high[j];
		}
		return new Vector<ushort>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<uint> Narrow(Vector<ulong> low, Vector<ulong> high)
	{
		int count = Vector<uint>.Count;
		uint* ptr = stackalloc uint[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (uint)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (uint)high[j];
		}
		return new Vector<uint>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<sbyte> Narrow(Vector<short> low, Vector<short> high)
	{
		int count = Vector<sbyte>.Count;
		sbyte* ptr = stackalloc sbyte[(int)(uint)count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (sbyte)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (sbyte)high[j];
		}
		return new Vector<sbyte>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector<short> Narrow(Vector<int> low, Vector<int> high)
	{
		int count = Vector<short>.Count;
		short* ptr = stackalloc short[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (short)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (short)high[j];
		}
		return new Vector<short>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector<int> Narrow(Vector<long> low, Vector<long> high)
	{
		int count = Vector<int>.Count;
		int* ptr = stackalloc int[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (int)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (int)high[j];
		}
		return new Vector<int>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector<float> Narrow(Vector<double> low, Vector<double> high)
	{
		int count = Vector<float>.Count;
		float* ptr = stackalloc float[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (float)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (float)high[j];
		}
		return new Vector<float>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector<float> ConvertToSingle(Vector<int> value)
	{
		int count = Vector<float>.Count;
		float* ptr = stackalloc float[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return new Vector<float>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<float> ConvertToSingle(Vector<uint> value)
	{
		int count = Vector<float>.Count;
		float* ptr = stackalloc float[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return new Vector<float>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector<double> ConvertToDouble(Vector<long> value)
	{
		int count = Vector<double>.Count;
		double* ptr = stackalloc double[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return new Vector<double>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<double> ConvertToDouble(Vector<ulong> value)
	{
		int count = Vector<double>.Count;
		double* ptr = stackalloc double[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return new Vector<double>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector<int> ConvertToInt32(Vector<float> value)
	{
		int count = Vector<int>.Count;
		int* ptr = stackalloc int[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (int)value[i];
		}
		return new Vector<int>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<uint> ConvertToUInt32(Vector<float> value)
	{
		int count = Vector<uint>.Count;
		uint* ptr = stackalloc uint[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (uint)value[i];
		}
		return new Vector<uint>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector<long> ConvertToInt64(Vector<double> value)
	{
		int count = Vector<long>.Count;
		long* ptr = stackalloc long[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (long)value[i];
		}
		return new Vector<long>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<ulong> ConvertToUInt64(Vector<double> value)
	{
		int count = Vector<ulong>.Count;
		ulong* ptr = stackalloc ulong[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (ulong)value[i];
		}
		return new Vector<ulong>(ptr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<float> ConditionalSelect(Vector<int> condition, Vector<float> left, Vector<float> right)
	{
		return Vector<float>.ConditionalSelect((Vector<float>)condition, left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<double> ConditionalSelect(Vector<long> condition, Vector<double> left, Vector<double> right)
	{
		return Vector<double>.ConditionalSelect((Vector<double>)condition, left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> ConditionalSelect<T>(Vector<T> condition, Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.ConditionalSelect(condition, left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> Equals(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> Equals(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> Equals(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> Equals(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EqualsAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left == right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EqualsAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !Vector<T>.Equals(left, right).Equals(Vector<T>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> LessThan<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> LessThan(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> LessThan(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> LessThan(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> LessThan(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.LessThan(left, right)).Equals(Vector<int>.AllOnes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.LessThan(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> LessThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> LessThanOrEqual(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> LessThanOrEqual(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> LessThanOrEqual(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> LessThanOrEqual(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.LessThanOrEqual(left, right)).Equals(Vector<int>.AllOnes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.LessThanOrEqual(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> GreaterThan(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> GreaterThan(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> GreaterThan(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> GreaterThan(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.GreaterThan(left, right)).Equals(Vector<int>.AllOnes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.GreaterThan(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> GreaterThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> GreaterThanOrEqual(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> GreaterThanOrEqual(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> GreaterThanOrEqual(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> GreaterThanOrEqual(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.GreaterThanOrEqual(left, right)).Equals(Vector<int>.AllOnes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.GreaterThanOrEqual(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Abs<T>(Vector<T> value) where T : struct
	{
		return Vector<T>.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Min<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.Min(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Max<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.Max(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Dot<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.DotProduct(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> SquareRoot<T>(Vector<T> value) where T : struct
	{
		return Vector<T>.SquareRoot(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Add<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Subtract<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Multiply<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Multiply<T>(Vector<T> left, T right) where T : struct
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Multiply<T>(T left, Vector<T> right) where T : struct
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Divide<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Negate<T>(Vector<T> value) where T : struct
	{
		return -value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left & right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left | right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> OnesComplement<T>(Vector<T> value) where T : struct
	{
		return ~value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left ^ right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left & ~right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<byte> AsVectorByte<T>(Vector<T> value) where T : struct
	{
		return (Vector<byte>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value) where T : struct
	{
		return (Vector<sbyte>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value) where T : struct
	{
		return (Vector<ushort>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<short> AsVectorInt16<T>(Vector<T> value) where T : struct
	{
		return (Vector<short>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<uint> AsVectorUInt32<T>(Vector<T> value) where T : struct
	{
		return (Vector<uint>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> AsVectorInt32<T>(Vector<T> value) where T : struct
	{
		return (Vector<int>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value) where T : struct
	{
		return (Vector<ulong>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> AsVectorInt64<T>(Vector<T> value) where T : struct
	{
		return (Vector<long>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<float> AsVectorSingle<T>(Vector<T> value) where T : struct
	{
		return (Vector<float>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<double> AsVectorDouble<T>(Vector<T> value) where T : struct
	{
		return (Vector<double>)value;
	}
}
