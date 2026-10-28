using System;

namespace TaleWorlds.Core;

[Flags]
public enum EquipmentCategories : uint
{
	None = 0u,
	IsFemaleTemplate = 1u,
	IsLordTemplate = 2u,
	IsChildEquipmentTemplate = 4u,
	IsTeenagerEquipmentTemplate = 8u,
	IsKingdomRulerTemplate = 0x10u
}
