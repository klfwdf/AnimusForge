using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[Flags]
[EngineStruct("rglText_flags", false, null, FirstCharacterUppercase = false)]
public enum TextFlags
{
	RglTfNone = 0,
	RglTfHAlignLeft = 1,
	RglTfHAlignRight = 2,
	RglTfHAlignCenter = 3,
	RglTfVAlignTop = 4,
	RglTfVAlignDown = 8,
	RglTfVAlignCenter = 0xC,
	RglTfSingleLine = 0x10,
	RglTfMultiline = 0x20,
	RglTfItalic = 0x40,
	RglTfCutTextFromLeft = 0x80,
	RglTfDoubleSpace = 0x100,
	RglTfWithOutline = 0x200,
	RglTfHalfSpace = 0x400
}
