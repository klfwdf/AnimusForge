using System;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000542 RID: 1346
	internal abstract class FxBaseRuntime : FxCoreBaseRuntime
	{
		// Token: 0x17000690 RID: 1680
		// (get) Token: 0x06001E27 RID: 7719 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override RuntimeKind Target
		{
			get
			{
				return RuntimeKind.Framework;
			}
		}

		// Token: 0x06001E28 RID: 7720 RVA: 0x00061D04 File Offset: 0x0005FF04
		[NullableContext(1)]
		public static FxBaseRuntime CreateForVersion(Version version, ISystem system)
		{
			if (version.Major == 4)
			{
				return new FxCLR4Runtime(system);
			}
			if (version.Major == 2)
			{
				return new FxCLR2Runtime(system);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
			defaultInterpolatedStringHandler.AppendLiteral("CLR version ");
			defaultInterpolatedStringHandler.AppendFormatted<Version>(version);
			defaultInterpolatedStringHandler.AppendLiteral(" is not suppoted.");
			throw new PlatformNotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
