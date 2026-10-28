using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct CompassItemUpdateParams
{
	public readonly object Item;

	public readonly TargetIconType TargetType;

	public readonly Vec3 WorldPosition;

	public readonly uint Color;

	public readonly uint Color2;

	public readonly Banner Banner;

	public readonly bool IsAttacker;

	public readonly bool IsAlly;

	public CompassItemUpdateParams(object item, TargetIconType targetType, Vec3 worldPosition, uint color, uint color2)
	{
		this = default(CompassItemUpdateParams);
		Item = item;
		TargetType = targetType;
		WorldPosition = worldPosition;
		Color = color;
		Color2 = color2;
		IsAttacker = false;
		IsAlly = false;
	}

	public CompassItemUpdateParams(object item, TargetIconType targetType, Vec3 worldPosition, Banner banner, bool isAttacker, bool isAlly)
	{
		this = default(CompassItemUpdateParams);
		Item = item;
		TargetType = targetType;
		WorldPosition = worldPosition;
		Banner = banner;
		IsAttacker = isAttacker;
		IsAlly = isAlly;
	}
}
