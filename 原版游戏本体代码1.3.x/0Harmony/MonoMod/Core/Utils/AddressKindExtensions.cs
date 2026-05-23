using System;
using System.Runtime.CompilerServices;
using MonoMod.Logs;

namespace MonoMod.Core.Utils
{
	// Token: 0x020004E4 RID: 1252
	[NullableContext(1)]
	[Nullable(0)]
	internal static class AddressKindExtensions
	{
		// Token: 0x06001BCE RID: 7118 RVA: 0x00058EA4 File Offset: 0x000570A4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRelative(this AddressKind value)
		{
			return (value & AddressKind.Abs32) == AddressKind.Rel32;
		}

		// Token: 0x06001BCF RID: 7119 RVA: 0x00058EAC File Offset: 0x000570AC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAbsolute(this AddressKind value)
		{
			return !value.IsRelative();
		}

		// Token: 0x06001BD0 RID: 7120 RVA: 0x00058EB7 File Offset: 0x000570B7
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Is32Bit(this AddressKind value)
		{
			return (value & AddressKind.Rel64) == AddressKind.Rel32;
		}

		// Token: 0x06001BD1 RID: 7121 RVA: 0x00058EBF File Offset: 0x000570BF
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Is64Bit(this AddressKind value)
		{
			return !value.Is32Bit();
		}

		// Token: 0x06001BD2 RID: 7122 RVA: 0x00058ECA File Offset: 0x000570CA
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPrecodeFixup(this AddressKind value)
		{
			return (value & AddressKind.PrecodeFixupThunkRel32) > AddressKind.Rel32;
		}

		// Token: 0x06001BD3 RID: 7123 RVA: 0x00058ED2 File Offset: 0x000570D2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsIndirect(this AddressKind value)
		{
			return (value & AddressKind.Indirect) > AddressKind.Rel32;
		}

		// Token: 0x06001BD4 RID: 7124 RVA: 0x00058EDA File Offset: 0x000570DA
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsConstant(this AddressKind value)
		{
			return (value & AddressKind.ConstantAddr) > AddressKind.Rel32;
		}

		// Token: 0x06001BD5 RID: 7125 RVA: 0x00058EE3 File Offset: 0x000570E3
		public static void Validate(this AddressKind value, [CallerArgumentExpression("value")] string argName = "")
		{
			if ((value & ~(AddressKind.Rel64 | AddressKind.Abs32 | AddressKind.PrecodeFixupThunkRel32 | AddressKind.Indirect | AddressKind.ConstantAddr)) != AddressKind.Rel32)
			{
				throw new ArgumentOutOfRangeException(argName);
			}
		}

		// Token: 0x06001BD6 RID: 7126 RVA: 0x00058EF4 File Offset: 0x000570F4
		public static string FastToString(this AddressKind value)
		{
			FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(0, 4);
			formatInterpolatedStringHandler.AppendFormatted(value.IsPrecodeFixup() ? "PrecodeFixupThunk" : "");
			formatInterpolatedStringHandler.AppendFormatted(value.IsRelative() ? "Rel" : "Abs");
			formatInterpolatedStringHandler.AppendFormatted(value.Is32Bit() ? "32" : "64");
			formatInterpolatedStringHandler.AppendFormatted(value.IsIndirect() ? "Indirect" : "");
			return DebugFormatter.Format(ref formatInterpolatedStringHandler);
		}

		// Token: 0x0400116B RID: 4459
		public const AddressKind IsAbsoluteField = AddressKind.Abs32;

		// Token: 0x0400116C RID: 4460
		public const AddressKind Is64BitField = AddressKind.Rel64;

		// Token: 0x0400116D RID: 4461
		public const AddressKind IsPrecodeFixupField = AddressKind.PrecodeFixupThunkRel32;

		// Token: 0x0400116E RID: 4462
		public const AddressKind IsIndirectField = AddressKind.Indirect;

		// Token: 0x0400116F RID: 4463
		public const AddressKind IsConstantField = AddressKind.ConstantAddr;
	}
}
