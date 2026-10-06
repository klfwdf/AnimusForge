using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

[Flags]
public enum BlurBehindConstraints : uint
{
	Enable = 1u,
	BlurRegion = 2u,
	TransitionOnMaximized = 4u
}
