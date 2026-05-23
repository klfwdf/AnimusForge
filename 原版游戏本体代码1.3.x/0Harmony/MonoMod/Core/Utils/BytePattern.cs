using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MonoMod.Utils;

namespace MonoMod.Core.Utils
{
	// Token: 0x020004E6 RID: 1254
	internal sealed class BytePattern
	{
		// Token: 0x17000604 RID: 1540
		// (get) Token: 0x06001BE5 RID: 7141 RVA: 0x000591E1 File Offset: 0x000573E1
		public int AddressBytes { get; }

		// Token: 0x17000605 RID: 1541
		// (get) Token: 0x06001BE6 RID: 7142 RVA: 0x000591E9 File Offset: 0x000573E9
		public int MinLength { get; }

		// Token: 0x17000606 RID: 1542
		// (get) Token: 0x06001BE7 RID: 7143 RVA: 0x000591F1 File Offset: 0x000573F1
		public AddressMeaning AddressMeaning { get; }

		// Token: 0x17000607 RID: 1543
		// (get) Token: 0x06001BE8 RID: 7144 RVA: 0x000591F9 File Offset: 0x000573F9
		public bool MustMatchAtStart { get; }

		// Token: 0x06001BE9 RID: 7145 RVA: 0x00059201 File Offset: 0x00057401
		[NullableContext(1)]
		public BytePattern(AddressMeaning meaning, params ushort[] pattern)
			: this(meaning, false, pattern.AsMemory<ushort>())
		{
		}

		// Token: 0x06001BEA RID: 7146 RVA: 0x00059216 File Offset: 0x00057416
		[NullableContext(1)]
		public BytePattern(AddressMeaning meaning, bool mustMatchAtStart, params ushort[] pattern)
			: this(meaning, mustMatchAtStart, pattern.AsMemory<ushort>())
		{
		}

		// Token: 0x06001BEB RID: 7147 RVA: 0x0005922B File Offset: 0x0005742B
		public BytePattern(AddressMeaning meaning, ReadOnlyMemory<ushort> pattern)
			: this(meaning, false, pattern)
		{
		}

		// Token: 0x06001BEC RID: 7148 RVA: 0x00059238 File Offset: 0x00057438
		public unsafe BytePattern(AddressMeaning meaning, bool mustMatchAtStart, ReadOnlyMemory<ushort> pattern)
		{
			this.AddressMeaning = meaning;
			this.MustMatchAtStart = mustMatchAtStart;
			BytePattern.PatternSegment[] array;
			int num;
			int num2;
			BytePattern.ComputeSegmentsFromShort(pattern).Deconstruct(out array, out num, out num2);
			this.segments = array;
			this.MinLength = num;
			this.AddressBytes = num2;
			Memory<byte> patternAlloc = new byte[pattern.Length * 2].AsMemory<byte>();
			Memory<byte> patternData = patternAlloc.Slice(0, pattern.Length);
			Memory<byte> bitmaskData = patternAlloc.Slice(pattern.Length);
			for (int i = 0; i < pattern.Length; i++)
			{
				ushort num3 = *pattern.Span[i];
				byte mask = (byte)((num3 & 65280) >> 8);
				byte data = (byte)((int)num3 & -65281);
				bool flag = mask == 0 || mask == byte.MaxValue;
				if (flag)
				{
					mask = ~mask;
				}
				*patternData.Span[i] = data & mask;
				*bitmaskData.Span[i] = mask;
			}
			this.pattern = patternData;
			this.bitmask = bitmaskData;
		}

		// Token: 0x06001BED RID: 7149 RVA: 0x0005935E File Offset: 0x0005755E
		public BytePattern(AddressMeaning meaning, ReadOnlyMemory<byte> mask, ReadOnlyMemory<byte> pattern)
			: this(meaning, false, mask, pattern)
		{
		}

		// Token: 0x06001BEE RID: 7150 RVA: 0x0005936C File Offset: 0x0005756C
		public BytePattern(AddressMeaning meaning, bool mustMatchAtStart, ReadOnlyMemory<byte> mask, ReadOnlyMemory<byte> pattern)
		{
			this.AddressMeaning = meaning;
			this.MustMatchAtStart = mustMatchAtStart;
			BytePattern.PatternSegment[] array;
			int num;
			int num2;
			BytePattern.ComputeSegmentsFromMaskPattern(mask, pattern).Deconstruct(out array, out num, out num2);
			this.segments = array;
			this.MinLength = num;
			this.AddressBytes = num2;
			this.pattern = pattern;
			this.bitmask = mask;
		}

		// Token: 0x06001BEF RID: 7151 RVA: 0x000593C7 File Offset: 0x000575C7
		private static BytePattern.ComputeSegmentsResult ComputeSegmentsFromShort(ReadOnlyMemory<ushort> pattern)
		{
			return BytePattern.ComputeSegmentsCore<ReadOnlyMemory<ushort>>(ldftn(<ComputeSegmentsFromShort>g__KindForShort|31_0), pattern.Length, pattern);
		}

		// Token: 0x06001BF0 RID: 7152 RVA: 0x000593DC File Offset: 0x000575DC
		private static BytePattern.ComputeSegmentsResult ComputeSegmentsFromMaskPattern(ReadOnlyMemory<byte> mask, ReadOnlyMemory<byte> pattern)
		{
			if (mask.Length < pattern.Length)
			{
				throw new ArgumentException("Mask buffer shorter than pattern", "mask");
			}
			return BytePattern.ComputeSegmentsCore<ValueTuple<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>(ldftn(<ComputeSegmentsFromMaskPattern>g__KindForIdx|32_0), pattern.Length, new ValueTuple<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(mask, pattern));
		}

		// Token: 0x06001BF1 RID: 7153 RVA: 0x00059418 File Offset: 0x00057618
		[NullableContext(1)]
		private static BytePattern.ComputeSegmentsResult ComputeSegmentsCore<[Nullable(2)] TPattern>([Nullable(new byte[] { 0, 1 })] method kindForIdx, int patternLength, TPattern pattern)
		{
			if (patternLength == 0)
			{
				throw new ArgumentException("Pattern cannot be empty", "pattern");
			}
			int segmentCount = 0;
			BytePattern.SegmentKind lastKind = BytePattern.SegmentKind.AnyRepeating;
			int segmentLength = 0;
			int addrLength = 0;
			int minLength = 0;
			int firstSegmentStart = -1;
			for (int i = 0; i < patternLength; i++)
			{
				BytePattern.SegmentKind thisSegmentKind = calli(MonoMod.Core.Utils.BytePattern/SegmentKind(TPattern,System.Int32), pattern, i, kindForIdx);
				int num = minLength;
				int num2;
				switch (thisSegmentKind)
				{
				case BytePattern.SegmentKind.Literal:
					num2 = 1;
					break;
				case BytePattern.SegmentKind.MaskedLiteral:
					num2 = 1;
					break;
				case BytePattern.SegmentKind.Any:
					num2 = 1;
					break;
				case BytePattern.SegmentKind.AnyRepeating:
					num2 = 0;
					break;
				case BytePattern.SegmentKind.Address:
					num2 = 1;
					break;
				default:
					num2 = 0;
					break;
				}
				minLength = num + num2;
				if (thisSegmentKind != lastKind)
				{
					if (firstSegmentStart < 0)
					{
						firstSegmentStart = i;
					}
					segmentCount++;
					segmentLength = 1;
				}
				else
				{
					segmentLength++;
				}
				if (thisSegmentKind == BytePattern.SegmentKind.Address)
				{
					addrLength++;
				}
				lastKind = thisSegmentKind;
			}
			if (segmentCount > 0 && lastKind == BytePattern.SegmentKind.AnyRepeating)
			{
				segmentCount--;
			}
			if (segmentCount == 0 || minLength <= 0)
			{
				throw new ArgumentException("Pattern has no meaningful segments", "pattern");
			}
			BytePattern.PatternSegment[] segments = new BytePattern.PatternSegment[segmentCount];
			segmentCount = 0;
			lastKind = BytePattern.SegmentKind.AnyRepeating;
			segmentLength = 0;
			int j = firstSegmentStart;
			while (j < patternLength && segmentCount <= segments.Length)
			{
				object obj = calli(MonoMod.Core.Utils.BytePattern/SegmentKind(TPattern,System.Int32), pattern, j, kindForIdx);
				if (obj != lastKind)
				{
					if (segmentCount > 0)
					{
						segments[segmentCount - 1] = new BytePattern.PatternSegment(j - segmentLength, segmentLength, lastKind);
						if (segmentCount > 1 && lastKind == BytePattern.SegmentKind.Any && segments[segmentCount - 2].Kind == BytePattern.SegmentKind.AnyRepeating)
						{
							Helpers.Swap<BytePattern.PatternSegment>(ref segments[segmentCount - 2], ref segments[segmentCount - 1]);
						}
					}
					segmentCount++;
					segmentLength = 1;
				}
				else
				{
					segmentLength++;
				}
				lastKind = obj;
				j++;
			}
			if (lastKind != BytePattern.SegmentKind.AnyRepeating && segmentCount > 0)
			{
				segments[segmentCount - 1] = new BytePattern.PatternSegment(patternLength - segmentLength, segmentLength, lastKind);
			}
			return new BytePattern.ComputeSegmentsResult(segments, minLength, addrLength);
		}

		// Token: 0x06001BF2 RID: 7154 RVA: 0x000595AC File Offset: 0x000577AC
		public unsafe bool TryMatchAt(ReadOnlySpan<byte> data, out ulong address, out int length)
		{
			if (data.Length < this.MinLength)
			{
				length = 0;
				address = 0UL;
				return false;
			}
			ReadOnlySpan<byte> patternSpan = this.pattern.Span;
			Span<byte> addr = new Span<byte>(stackalloc byte[(UIntPtr)8], 8);
			bool result = this.TryMatchAtImpl(patternSpan, data, addr, out length, 0);
			address = Unsafe.ReadUnaligned<ulong>(addr[0]);
			return result;
		}

		// Token: 0x06001BF3 RID: 7155 RVA: 0x00059604 File Offset: 0x00057804
		public bool TryMatchAt(ReadOnlySpan<byte> data, Span<byte> addrBuf, out int length)
		{
			if (data.Length < this.MinLength)
			{
				length = 0;
				return false;
			}
			ReadOnlySpan<byte> patternSpan = this.pattern.Span;
			return this.TryMatchAtImpl(patternSpan, data, addrBuf, out length, 0);
		}

		// Token: 0x06001BF4 RID: 7156 RVA: 0x0005963C File Offset: 0x0005783C
		private bool TryMatchAtImpl(ReadOnlySpan<byte> patternSpan, ReadOnlySpan<byte> data, Span<byte> addrBuf, out int length, int startAtSegment)
		{
			int pos = 0;
			int segmentIdx = startAtSegment;
			while (segmentIdx < this.segments.Length)
			{
				BytePattern.PatternSegment segment = this.segments[segmentIdx];
				switch (segment.Kind)
				{
				case BytePattern.SegmentKind.Literal:
				{
					if (data.Length - pos < segment.Length)
					{
						goto IL_1AA;
					}
					ReadOnlySpan<byte> pattern = segment.SliceOf<byte>(patternSpan);
					if (!pattern.SequenceEqual(data.Slice(pos, pattern.Length)))
					{
						goto IL_1AA;
					}
					pos += segment.Length;
					break;
				}
				case BytePattern.SegmentKind.MaskedLiteral:
				{
					if (data.Length - pos < segment.Length)
					{
						goto IL_1AA;
					}
					ReadOnlySpan<byte> pattern2 = segment.SliceOf<byte>(patternSpan);
					ReadOnlySpan<byte> mask = segment.SliceOf<byte>(this.bitmask.Span);
					if (!Helpers.MaskedSequenceEqual(pattern2, data.Slice(pos, pattern2.Length), mask))
					{
						goto IL_1AA;
					}
					pos += segment.Length;
					break;
				}
				case BytePattern.SegmentKind.Any:
					if (data.Length - pos < segment.Length)
					{
						goto IL_1AA;
					}
					pos += segment.Length;
					break;
				case BytePattern.SegmentKind.AnyRepeating:
				{
					int offs;
					int sublen;
					bool result = this.ScanForNextLiteral(patternSpan, data.Slice(pos), addrBuf, out offs, out sublen, segmentIdx);
					length = pos + offs + sublen;
					return result;
				}
				case BytePattern.SegmentKind.Address:
				{
					if (data.Length - pos < segment.Length)
					{
						goto IL_1AA;
					}
					ReadOnlySpan<byte> pattern3 = data.Slice(pos, Math.Min(segment.Length, addrBuf.Length));
					pattern3.CopyTo(addrBuf);
					addrBuf = addrBuf.Slice(Math.Min(addrBuf.Length, pattern3.Length));
					pos += segment.Length;
					break;
				}
				default:
					throw new InvalidOperationException();
				}
				segmentIdx++;
				continue;
				IL_1AA:
				length = 0;
				return false;
			}
			length = pos;
			return true;
		}

		// Token: 0x06001BF5 RID: 7157 RVA: 0x000597F8 File Offset: 0x000579F8
		public unsafe bool TryFindMatch(ReadOnlySpan<byte> data, out ulong address, out int offset, out int length)
		{
			if (data.Length < this.MinLength)
			{
				length = (offset = 0);
				address = 0UL;
				return false;
			}
			ReadOnlySpan<byte> patternSpan = this.pattern.Span;
			Span<byte> addr = new Span<byte>(stackalloc byte[(UIntPtr)8], 8);
			bool result;
			if (this.MustMatchAtStart)
			{
				offset = 0;
				result = this.TryMatchAtImpl(patternSpan, data, addr, out length, 0);
			}
			else
			{
				result = this.ScanForNextLiteral(patternSpan, data, addr, out offset, out length, 0);
			}
			address = Unsafe.ReadUnaligned<ulong>(addr[0]);
			return result;
		}

		// Token: 0x06001BF6 RID: 7158 RVA: 0x00059874 File Offset: 0x00057A74
		public bool TryFindMatch(ReadOnlySpan<byte> data, Span<byte> addrBuf, out int offset, out int length)
		{
			if (data.Length < this.MinLength)
			{
				length = (offset = 0);
				return false;
			}
			ReadOnlySpan<byte> patternSpan = this.pattern.Span;
			if (this.MustMatchAtStart)
			{
				offset = 0;
				return this.TryMatchAtImpl(patternSpan, data, addrBuf, out length, 0);
			}
			return this.ScanForNextLiteral(patternSpan, data, addrBuf, out offset, out length, 0);
		}

		// Token: 0x06001BF7 RID: 7159 RVA: 0x000598CC File Offset: 0x00057ACC
		private bool ScanForNextLiteral(ReadOnlySpan<byte> patternSpan, ReadOnlySpan<byte> data, Span<byte> addrBuf, out int offset, out int length, int segmentIndex)
		{
			ValueTuple<BytePattern.PatternSegment, int> nextLiteralSegment = this.GetNextLiteralSegment(segmentIndex);
			BytePattern.PatternSegment literalSegment = nextLiteralSegment.Item1;
			int baseOffs = nextLiteralSegment.Item2;
			if (baseOffs + literalSegment.Length > data.Length)
			{
				offset = (length = 0);
				return false;
			}
			int scanOffsFromBase = 0;
			for (;;)
			{
				int scannedOffs = data.Slice(baseOffs + scanOffsFromBase).IndexOf(literalSegment.SliceOf<byte>(patternSpan));
				if (scannedOffs < 0)
				{
					break;
				}
				if (this.TryMatchAtImpl(patternSpan, data.Slice(offset = scanOffsFromBase + scannedOffs), addrBuf, out length, segmentIndex))
				{
					return true;
				}
				scanOffsFromBase += scannedOffs + 1;
			}
			offset = (length = 0);
			return false;
		}

		// Token: 0x17000608 RID: 1544
		// (get) Token: 0x06001BF8 RID: 7160 RVA: 0x00059964 File Offset: 0x00057B64
		[TupleElementNames(new string[] { "Bytes", "Offset" })]
		public ValueTuple<ReadOnlyMemory<byte>, int> FirstLiteralSegment
		{
			[return: TupleElementNames(new string[] { "Bytes", "Offset" })]
			get
			{
				ValueTuple<ReadOnlyMemory<byte>, int> valueTuple = this.lazyFirstLiteralSegment.GetValueOrDefault();
				if (this.lazyFirstLiteralSegment == null)
				{
					valueTuple = this.GetFirstLiteralSegment();
					this.lazyFirstLiteralSegment = new ValueTuple<ReadOnlyMemory<byte>, int>?(valueTuple);
					return valueTuple;
				}
				return valueTuple;
			}
		}

		// Token: 0x06001BF9 RID: 7161 RVA: 0x000599A0 File Offset: 0x00057BA0
		[return: TupleElementNames(new string[] { "Bytes", "Offset" })]
		private ValueTuple<ReadOnlyMemory<byte>, int> GetFirstLiteralSegment()
		{
			ValueTuple<BytePattern.PatternSegment, int> nextLiteralSegment = this.GetNextLiteralSegment(0);
			BytePattern.PatternSegment segment = nextLiteralSegment.Item1;
			int offset = nextLiteralSegment.Item2;
			return new ValueTuple<ReadOnlyMemory<byte>, int>(segment.SliceOf<byte>(this.pattern), offset);
		}

		// Token: 0x06001BFA RID: 7162 RVA: 0x000599D4 File Offset: 0x00057BD4
		[return: TupleElementNames(new string[] { "Segment", "LiteralOffset" })]
		private ValueTuple<BytePattern.PatternSegment, int> GetNextLiteralSegment(int segmentIndexId)
		{
			if (segmentIndexId < 0 || segmentIndexId >= this.segments.Length)
			{
				throw new ArgumentOutOfRangeException("segmentIndexId");
			}
			int litOffset = 0;
			while (segmentIndexId < this.segments.Length)
			{
				BytePattern.PatternSegment segment = this.segments[segmentIndexId];
				if (segment.Kind == BytePattern.SegmentKind.Literal)
				{
					return new ValueTuple<BytePattern.PatternSegment, int>(segment, litOffset);
				}
				BytePattern.SegmentKind kind = segment.Kind;
				bool flag = kind - BytePattern.SegmentKind.MaskedLiteral <= 1 || kind == BytePattern.SegmentKind.Address;
				if (flag)
				{
					litOffset += segment.Length;
				}
				else if (segment.Kind != BytePattern.SegmentKind.AnyRepeating)
				{
					throw new InvalidOperationException("Unknown segment kind");
				}
				segmentIndexId++;
			}
			return new ValueTuple<BytePattern.PatternSegment, int>(default(BytePattern.PatternSegment), litOffset);
		}

		// Token: 0x06001BFB RID: 7163 RVA: 0x00059A7C File Offset: 0x00057C7C
		[CompilerGenerated]
		internal unsafe static BytePattern.SegmentKind <ComputeSegmentsFromShort>g__KindForShort|31_0(ReadOnlyMemory<ushort> pattern, int idx)
		{
			ushort value = *pattern.Span[idx];
			int num = (int)(value & 65280);
			BytePattern.SegmentKind result;
			if (num != 0)
			{
				if (num != 65280)
				{
					result = BytePattern.SegmentKind.MaskedLiteral;
				}
				else
				{
					int x = (int)(value & 255);
					BytePattern.SegmentKind segmentKind;
					switch (x)
					{
					case 0:
						segmentKind = BytePattern.SegmentKind.Any;
						break;
					case 1:
						segmentKind = BytePattern.SegmentKind.AnyRepeating;
						break;
					case 2:
						segmentKind = BytePattern.SegmentKind.Address;
						break;
					default:
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Pattern contained unknown special value ");
						defaultInterpolatedStringHandler.AppendFormatted<int>(x, "x2");
						throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "pattern");
					}
					}
					result = segmentKind;
				}
			}
			else
			{
				result = BytePattern.SegmentKind.Literal;
			}
			return result;
		}

		// Token: 0x06001BFC RID: 7164 RVA: 0x00059B20 File Offset: 0x00057D20
		[CompilerGenerated]
		internal unsafe static BytePattern.SegmentKind <ComputeSegmentsFromMaskPattern>g__KindForIdx|32_0([TupleElementNames(new string[] { "mask", "pattern" })] ValueTuple<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>> t, int idx)
		{
			byte b = *t.Item1.Span[idx];
			BytePattern.SegmentKind result;
			if (b != 0)
			{
				if (b != 255)
				{
					result = BytePattern.SegmentKind.MaskedLiteral;
				}
				else
				{
					result = BytePattern.SegmentKind.Literal;
				}
			}
			else
			{
				byte x = *t.Item2.Span[idx];
				BytePattern.SegmentKind segmentKind;
				switch (x)
				{
				case 0:
					segmentKind = BytePattern.SegmentKind.Any;
					break;
				case 1:
					segmentKind = BytePattern.SegmentKind.AnyRepeating;
					break;
				case 2:
					segmentKind = BytePattern.SegmentKind.Address;
					break;
				default:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Pattern contained unknown special value ");
					defaultInterpolatedStringHandler.AppendFormatted<byte>(x, "x2");
					throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "pattern");
				}
				}
				result = segmentKind;
			}
			return result;
		}

		// Token: 0x04001173 RID: 4467
		private const ushort MaskMask = 65280;

		// Token: 0x04001174 RID: 4468
		public const byte BAnyValue = 0;

		// Token: 0x04001175 RID: 4469
		public const ushort SAnyValue = 65280;

		// Token: 0x04001176 RID: 4470
		public const byte BAnyRepeatingValue = 1;

		// Token: 0x04001177 RID: 4471
		public const ushort SAnyRepeatingValue = 65281;

		// Token: 0x04001178 RID: 4472
		public const byte BAddressValue = 2;

		// Token: 0x04001179 RID: 4473
		public const ushort SAddressValue = 65282;

		// Token: 0x0400117A RID: 4474
		private readonly ReadOnlyMemory<byte> pattern;

		// Token: 0x0400117B RID: 4475
		private readonly ReadOnlyMemory<byte> bitmask;

		// Token: 0x0400117C RID: 4476
		[Nullable(1)]
		private readonly BytePattern.PatternSegment[] segments;

		// Token: 0x04001181 RID: 4481
		[TupleElementNames(new string[] { "Bytes", "Offset" })]
		private ValueTuple<ReadOnlyMemory<byte>, int>? lazyFirstLiteralSegment;

		// Token: 0x020004E7 RID: 1255
		private enum SegmentKind
		{
			// Token: 0x04001183 RID: 4483
			Literal,
			// Token: 0x04001184 RID: 4484
			MaskedLiteral,
			// Token: 0x04001185 RID: 4485
			Any,
			// Token: 0x04001186 RID: 4486
			AnyRepeating,
			// Token: 0x04001187 RID: 4487
			Address
		}

		// Token: 0x020004E8 RID: 1256
		private struct PatternSegment : IEquatable<BytePattern.PatternSegment>
		{
			// Token: 0x06001BFD RID: 7165 RVA: 0x00059BCD File Offset: 0x00057DCD
			public PatternSegment(int Start, int Length, BytePattern.SegmentKind Kind)
			{
				this.Start = Start;
				this.Length = Length;
				this.Kind = Kind;
			}

			// Token: 0x17000609 RID: 1545
			// (get) Token: 0x06001BFE RID: 7166 RVA: 0x00059BE4 File Offset: 0x00057DE4
			// (set) Token: 0x06001BFF RID: 7167 RVA: 0x00059BEC File Offset: 0x00057DEC
			public int Start { readonly get; set; }

			// Token: 0x1700060A RID: 1546
			// (get) Token: 0x06001C00 RID: 7168 RVA: 0x00059BF5 File Offset: 0x00057DF5
			// (set) Token: 0x06001C01 RID: 7169 RVA: 0x00059BFD File Offset: 0x00057DFD
			public int Length { readonly get; set; }

			// Token: 0x1700060B RID: 1547
			// (get) Token: 0x06001C02 RID: 7170 RVA: 0x00059C06 File Offset: 0x00057E06
			// (set) Token: 0x06001C03 RID: 7171 RVA: 0x00059C0E File Offset: 0x00057E0E
			public BytePattern.SegmentKind Kind { readonly get; set; }

			// Token: 0x06001C04 RID: 7172 RVA: 0x00059C17 File Offset: 0x00057E17
			[NullableContext(2)]
			[return: Nullable(new byte[] { 0, 1 })]
			public ReadOnlySpan<T> SliceOf<T>([Nullable(new byte[] { 0, 1 })] ReadOnlySpan<T> span)
			{
				return span.Slice(this.Start, this.Length);
			}

			// Token: 0x06001C05 RID: 7173 RVA: 0x00059C2C File Offset: 0x00057E2C
			[NullableContext(2)]
			[return: Nullable(new byte[] { 0, 1 })]
			public ReadOnlyMemory<T> SliceOf<T>([Nullable(new byte[] { 0, 1 })] ReadOnlyMemory<T> mem)
			{
				return mem.Slice(this.Start, this.Length);
			}

			// Token: 0x06001C06 RID: 7174 RVA: 0x00059C44 File Offset: 0x00057E44
			[CompilerGenerated]
			public override readonly string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("PatternSegment");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append('}');
				return stringBuilder.ToString();
			}

			// Token: 0x06001C07 RID: 7175 RVA: 0x00059C90 File Offset: 0x00057E90
			[CompilerGenerated]
			private readonly bool PrintMembers(StringBuilder builder)
			{
				builder.Append("Start = ");
				builder.Append(this.Start.ToString());
				builder.Append(", Length = ");
				builder.Append(this.Length.ToString());
				builder.Append(", Kind = ");
				builder.Append(this.Kind.ToString());
				return true;
			}

			// Token: 0x06001C08 RID: 7176 RVA: 0x00059D13 File Offset: 0x00057F13
			[CompilerGenerated]
			public static bool operator !=(BytePattern.PatternSegment left, BytePattern.PatternSegment right)
			{
				return !(left == right);
			}

			// Token: 0x06001C09 RID: 7177 RVA: 0x00059D1F File Offset: 0x00057F1F
			[CompilerGenerated]
			public static bool operator ==(BytePattern.PatternSegment left, BytePattern.PatternSegment right)
			{
				return left.Equals(right);
			}

			// Token: 0x06001C0A RID: 7178 RVA: 0x00059D29 File Offset: 0x00057F29
			[CompilerGenerated]
			public override readonly int GetHashCode()
			{
				return (EqualityComparer<int>.Default.GetHashCode(this.<Start>k__BackingField) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Length>k__BackingField)) * -1521134295 + EqualityComparer<BytePattern.SegmentKind>.Default.GetHashCode(this.<Kind>k__BackingField);
			}

			// Token: 0x06001C0B RID: 7179 RVA: 0x00059D69 File Offset: 0x00057F69
			[CompilerGenerated]
			public override readonly bool Equals(object obj)
			{
				return obj is BytePattern.PatternSegment && this.Equals((BytePattern.PatternSegment)obj);
			}

			// Token: 0x06001C0C RID: 7180 RVA: 0x00059D84 File Offset: 0x00057F84
			[CompilerGenerated]
			public readonly bool Equals(BytePattern.PatternSegment other)
			{
				return EqualityComparer<int>.Default.Equals(this.<Start>k__BackingField, other.<Start>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Length>k__BackingField, other.<Length>k__BackingField) && EqualityComparer<BytePattern.SegmentKind>.Default.Equals(this.<Kind>k__BackingField, other.<Kind>k__BackingField);
			}

			// Token: 0x06001C0D RID: 7181 RVA: 0x00059DD9 File Offset: 0x00057FD9
			[CompilerGenerated]
			public readonly void Deconstruct(out int Start, out int Length, out BytePattern.SegmentKind Kind)
			{
				Start = this.Start;
				Length = this.Length;
				Kind = this.Kind;
			}
		}

		// Token: 0x020004E9 RID: 1257
		[NullableContext(1)]
		[Nullable(0)]
		private readonly struct ComputeSegmentsResult : IEquatable<BytePattern.ComputeSegmentsResult>
		{
			// Token: 0x06001C0E RID: 7182 RVA: 0x00059DF3 File Offset: 0x00057FF3
			public ComputeSegmentsResult(BytePattern.PatternSegment[] Segments, int MinLen, int AddrBytes)
			{
				this.Segments = Segments;
				this.MinLen = MinLen;
				this.AddrBytes = AddrBytes;
			}

			// Token: 0x1700060C RID: 1548
			// (get) Token: 0x06001C0F RID: 7183 RVA: 0x00059E0A File Offset: 0x0005800A
			// (set) Token: 0x06001C10 RID: 7184 RVA: 0x00059E12 File Offset: 0x00058012
			public BytePattern.PatternSegment[] Segments { get; set; }

			// Token: 0x1700060D RID: 1549
			// (get) Token: 0x06001C11 RID: 7185 RVA: 0x00059E1B File Offset: 0x0005801B
			// (set) Token: 0x06001C12 RID: 7186 RVA: 0x00059E23 File Offset: 0x00058023
			public int MinLen { get; set; }

			// Token: 0x1700060E RID: 1550
			// (get) Token: 0x06001C13 RID: 7187 RVA: 0x00059E2C File Offset: 0x0005802C
			// (set) Token: 0x06001C14 RID: 7188 RVA: 0x00059E34 File Offset: 0x00058034
			public int AddrBytes { get; set; }

			// Token: 0x06001C15 RID: 7189 RVA: 0x00059E40 File Offset: 0x00058040
			[NullableContext(0)]
			[CompilerGenerated]
			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("ComputeSegmentsResult");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append('}');
				return stringBuilder.ToString();
			}

			// Token: 0x06001C16 RID: 7190 RVA: 0x00059E8C File Offset: 0x0005808C
			[NullableContext(0)]
			[CompilerGenerated]
			private bool PrintMembers(StringBuilder builder)
			{
				builder.Append("Segments = ");
				builder.Append(this.Segments);
				builder.Append(", MinLen = ");
				builder.Append(this.MinLen.ToString());
				builder.Append(", AddrBytes = ");
				builder.Append(this.AddrBytes.ToString());
				return true;
			}

			// Token: 0x06001C17 RID: 7191 RVA: 0x00059F01 File Offset: 0x00058101
			[CompilerGenerated]
			public static bool operator !=(BytePattern.ComputeSegmentsResult left, BytePattern.ComputeSegmentsResult right)
			{
				return !(left == right);
			}

			// Token: 0x06001C18 RID: 7192 RVA: 0x00059F0D File Offset: 0x0005810D
			[CompilerGenerated]
			public static bool operator ==(BytePattern.ComputeSegmentsResult left, BytePattern.ComputeSegmentsResult right)
			{
				return left.Equals(right);
			}

			// Token: 0x06001C19 RID: 7193 RVA: 0x00059F17 File Offset: 0x00058117
			[CompilerGenerated]
			public override int GetHashCode()
			{
				return (EqualityComparer<BytePattern.PatternSegment[]>.Default.GetHashCode(this.<Segments>k__BackingField) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<MinLen>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<AddrBytes>k__BackingField);
			}

			// Token: 0x06001C1A RID: 7194 RVA: 0x00059F57 File Offset: 0x00058157
			[NullableContext(0)]
			[CompilerGenerated]
			public override bool Equals(object obj)
			{
				return obj is BytePattern.ComputeSegmentsResult && this.Equals((BytePattern.ComputeSegmentsResult)obj);
			}

			// Token: 0x06001C1B RID: 7195 RVA: 0x00059F70 File Offset: 0x00058170
			[CompilerGenerated]
			public bool Equals(BytePattern.ComputeSegmentsResult other)
			{
				return EqualityComparer<BytePattern.PatternSegment[]>.Default.Equals(this.<Segments>k__BackingField, other.<Segments>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<MinLen>k__BackingField, other.<MinLen>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<AddrBytes>k__BackingField, other.<AddrBytes>k__BackingField);
			}

			// Token: 0x06001C1C RID: 7196 RVA: 0x00059FC5 File Offset: 0x000581C5
			[CompilerGenerated]
			public void Deconstruct(out BytePattern.PatternSegment[] Segments, out int MinLen, out int AddrBytes)
			{
				Segments = this.Segments;
				MinLen = this.MinLen;
				AddrBytes = this.AddrBytes;
			}
		}
	}
}
