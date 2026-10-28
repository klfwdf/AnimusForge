using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Missions.Multiplayer;

public readonly struct MultiplayerBattleColors(MultiplayerBattleColors.MultiplayerCultureColorInfo attackerColors, MultiplayerBattleColors.MultiplayerCultureColorInfo defenderColors)
{
	public readonly struct MultiplayerCultureColorInfo(BasicCultureObject culture, bool swapColors)
	{
		public readonly BasicCultureObject Culture = culture;

		public readonly Color Color1 = Color.FromUint(Color1Uint = ((!swapColors) ? culture?.Color : culture?.Color2) ?? 0);

		public readonly uint Color1Uint;

		public readonly Color Color2 = Color.FromUint(Color2Uint = ((!swapColors) ? culture?.Color2 : culture?.Color) ?? 0);

		public readonly uint Color2Uint;

		public readonly Color ClothingColor1 = Color.FromUint(ClothingColor1Uint = ((!swapColors) ? culture?.Color : culture?.Color2) ?? 0);

		public readonly uint ClothingColor1Uint;

		public readonly Color ClothingColor2 = Color.FromUint(ClothingColor2Uint = ((!swapColors) ? culture?.Color2 : culture?.Color) ?? 0);

		public readonly uint ClothingColor2Uint;

		public readonly Color BannerBackgroundColor = Color.FromUint(BannerBackgroundColorUint = ((!swapColors) ? culture?.BackgroundColor1 : culture?.BackgroundColor2) ?? 0);

		public readonly uint BannerBackgroundColorUint;

		public readonly Color BannerForegroundColor = Color.FromUint(BannerForegroundColorUint = ((!swapColors) ? culture?.ForegroundColor1 : culture?.ForegroundColor2) ?? 0);

		public readonly uint BannerForegroundColorUint;
	}

	public readonly MultiplayerCultureColorInfo AttackerColors = attackerColors;

	public readonly MultiplayerCultureColorInfo DefenderColors = defenderColors;

	public static MultiplayerBattleColors CreateWith(BasicCultureObject attackerCulture, BasicCultureObject defenderCulture)
	{
		return GetCultureColors(attackerCulture, defenderCulture);
	}

	public MultiplayerCultureColorInfo GetPeerColors(MissionPeer peer)
	{
		if (peer == null)
		{
			return AttackerColors;
		}
		if (AttackerColors.Culture == DefenderColors.Culture)
		{
			if (peer.Team != null)
			{
				if (peer.Team.Side != BattleSideEnum.Attacker)
				{
					return DefenderColors;
				}
				return AttackerColors;
			}
			return AttackerColors;
		}
		if (peer.Culture != AttackerColors.Culture)
		{
			return DefenderColors;
		}
		return AttackerColors;
	}

	private static MultiplayerBattleColors GetCultureColors(BasicCultureObject attackerCulture, BasicCultureObject defenderCulture)
	{
		if (attackerCulture == null)
		{
			attackerCulture = GetFallbackCulture();
		}
		if (defenderCulture == null)
		{
			defenderCulture = GetFallbackCulture();
		}
		bool swapColors = !string.IsNullOrEmpty(attackerCulture.StringId) && !string.IsNullOrEmpty(defenderCulture.StringId) && attackerCulture.StringId == defenderCulture.StringId;
		MultiplayerCultureColorInfo attackerColors = new MultiplayerCultureColorInfo(attackerCulture, swapColors: false);
		MultiplayerCultureColorInfo defenderColors = new MultiplayerCultureColorInfo(defenderCulture, swapColors);
		return new MultiplayerBattleColors(attackerColors, defenderColors);
	}

	private static BasicCultureObject GetFallbackCulture()
	{
		MBReadOnlyList<BasicCultureObject> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>();
		if (objectTypeList != null && objectTypeList.Count > 0)
		{
			return objectTypeList.FirstOrDefault();
		}
		Debug.FailedAssert("No culture objects in the object manager", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MultiplayerBattleColors.cs", "GetFallbackCulture", 114);
		return null;
	}
}
