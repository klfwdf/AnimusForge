using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

[Flags]
internal enum PixelFormatDescriptorFlags : uint
{
	DoubleBuffer = 1u,
	Stereo = 2u,
	DrawToWindow = 4u,
	DrawToBitmap = 8u,
	SupportGDI = 0x10u,
	SupportOpengl = 0x20u,
	GenericFormat = 0x40u,
	NeedPalette = 0x80u,
	NeedSystemPalette = 0x100u,
	SwapExchange = 0x200u,
	SwapCopy = 0x400u,
	SwapLayerBuffers = 0x800u,
	GenericAccelerated = 0x1000u,
	SupportDirectDraw = 0x2000u,
	Direct3DAccelerated = 0x4000u,
	SupportComposition = 0x8000u
}
