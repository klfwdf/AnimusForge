using System;
using System.Runtime.CompilerServices;
using MonoMod.Logs;

namespace MonoMod.Core.Utils
{
	// Token: 0x020004E5 RID: 1253
	internal readonly struct AddressMeaning : IEquatable<AddressMeaning>
	{
		// Token: 0x17000601 RID: 1537
		// (get) Token: 0x06001BD7 RID: 7127 RVA: 0x00058F7D File Offset: 0x0005717D
		public AddressKind Kind { get; }

		// Token: 0x17000602 RID: 1538
		// (get) Token: 0x06001BD8 RID: 7128 RVA: 0x00058F85 File Offset: 0x00057185
		public int RelativeToOffset { get; }

		// Token: 0x17000603 RID: 1539
		// (get) Token: 0x06001BD9 RID: 7129 RVA: 0x00058F8D File Offset: 0x0005718D
		public ulong ConstantValue { get; }

		// Token: 0x06001BDA RID: 7130 RVA: 0x00058F95 File Offset: 0x00057195
		public AddressMeaning(AddressKind kind)
		{
			this.ConstantValue = 0L;
			kind.Validate("kind");
			if (!kind.IsAbsolute())
			{
				throw new ArgumentOutOfRangeException("kind");
			}
			this.Kind = kind;
			this.RelativeToOffset = 0;
		}

		// Token: 0x06001BDB RID: 7131 RVA: 0x00058FCC File Offset: 0x000571CC
		public AddressMeaning(AddressKind kind, int relativeOffset)
		{
			this.ConstantValue = 0L;
			kind.Validate("kind");
			if (!kind.IsRelative())
			{
				throw new ArgumentOutOfRangeException("kind");
			}
			if (relativeOffset < 0)
			{
				throw new ArgumentOutOfRangeException("relativeOffset");
			}
			this.Kind = kind;
			this.RelativeToOffset = relativeOffset;
		}

		// Token: 0x06001BDC RID: 7132 RVA: 0x0005901C File Offset: 0x0005721C
		public AddressMeaning(AddressKind kind, int relativeOffset, ulong constantValue)
		{
			kind.Validate("kind");
			if (!kind.IsRelative())
			{
				throw new ArgumentOutOfRangeException("kind");
			}
			if (relativeOffset < 0)
			{
				throw new ArgumentOutOfRangeException("relativeOffset");
			}
			this.Kind = kind;
			this.RelativeToOffset = relativeOffset;
			this.ConstantValue = constantValue;
		}

		// Token: 0x06001BDD RID: 7133 RVA: 0x0005906C File Offset: 0x0005726C
		[return: NativeInteger]
		private unsafe static IntPtr DoProcessAddress(AddressKind kind, [NativeInteger] IntPtr basePtr, int offset, ulong constantValue, ulong address)
		{
			if (kind.IsConstant())
			{
				address = constantValue;
			}
			IntPtr addr;
			if (kind.IsAbsolute())
			{
				addr = (IntPtr)address;
			}
			else
			{
				long offs = (kind.Is32Bit() ? ((long)(*Unsafe.As<ulong, int>(ref address))) : (*Unsafe.As<ulong, long>(ref address)));
				addr = (IntPtr)((long)(basePtr + (IntPtr)offset) + offs);
			}
			if (kind.IsIndirect())
			{
				addr = *addr;
			}
			return addr;
		}

		// Token: 0x06001BDE RID: 7134 RVA: 0x000590C3 File Offset: 0x000572C3
		[return: NativeInteger]
		public IntPtr ProcessAddress([NativeInteger] IntPtr basePtr, int offset, ulong address)
		{
			return AddressMeaning.DoProcessAddress(this.Kind, basePtr, offset + this.RelativeToOffset, this.ConstantValue, address);
		}

		// Token: 0x06001BDF RID: 7135 RVA: 0x000590E0 File Offset: 0x000572E0
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (obj is AddressMeaning)
			{
				AddressMeaning meaning = (AddressMeaning)obj;
				return this.Equals(meaning);
			}
			return false;
		}

		// Token: 0x06001BE0 RID: 7136 RVA: 0x00059105 File Offset: 0x00057305
		public bool Equals(AddressMeaning other)
		{
			return this.Kind == other.Kind && this.RelativeToOffset == other.RelativeToOffset && this.ConstantValue == other.ConstantValue;
		}

		// Token: 0x06001BE1 RID: 7137 RVA: 0x00059138 File Offset: 0x00057338
		[NullableContext(1)]
		public override string ToString()
		{
			FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(38, 3);
			formatInterpolatedStringHandler.AppendLiteral("AddressMeaning(");
			formatInterpolatedStringHandler.AppendFormatted(this.Kind.FastToString());
			formatInterpolatedStringHandler.AppendLiteral(", offset: ");
			formatInterpolatedStringHandler.AppendFormatted<int>(this.RelativeToOffset);
			formatInterpolatedStringHandler.AppendLiteral(", constant: ");
			formatInterpolatedStringHandler.AppendFormatted<ulong>(this.ConstantValue);
			formatInterpolatedStringHandler.AppendLiteral(")");
			return DebugFormatter.Format(ref formatInterpolatedStringHandler);
		}

		// Token: 0x06001BE2 RID: 7138 RVA: 0x000591B2 File Offset: 0x000573B2
		public override int GetHashCode()
		{
			return HashCode.Combine<AddressKind, int, ulong>(this.Kind, this.RelativeToOffset, this.ConstantValue);
		}

		// Token: 0x06001BE3 RID: 7139 RVA: 0x000591CB File Offset: 0x000573CB
		public static bool operator ==(AddressMeaning left, AddressMeaning right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001BE4 RID: 7140 RVA: 0x000591D5 File Offset: 0x000573D5
		public static bool operator !=(AddressMeaning left, AddressMeaning right)
		{
			return !(left == right);
		}
	}
}
