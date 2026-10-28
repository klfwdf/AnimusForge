using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Map;

public interface IMapPoint
{
	TextObject Name { get; }

	CampaignVec2 Position { get; }

	PathFaceRecord CurrentNavigationFace { get; }

	IFaction MapFaction { get; }

	bool IsInspected { get; }

	bool IsVisible { get; }

	bool IsActive { get; set; }

	Vec3 GetPositionAsVec3();
}
