using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBanditDensityModel : BanditDensityModel
{
	private const int MinimumTroopCountForHideoutMission = 25;

	private Clan _deserterClan;

	public override int NumberOfMinimumBanditPartiesInAHideoutToInfestIt => 2;

	public override int NumberOfMaximumBanditPartiesInEachHideout => 3;

	public override int NumberOfMaximumBanditPartiesAroundEachHideout => 3;

	public override int NumberOfMaximumHideoutsAtEachBanditFaction => 9;

	public override int NumberOfInitialHideoutsAtEachBanditFaction => 7;

	public override int NumberOfMinimumBanditTroopsInHideoutMission => 10;

	public override int NumberOfMaximumTroopCountForFirstFightInHideout => MathF.Floor(11f * (2f + Campaign.Current.PlayerProgress));

	public override int NumberOfMaximumTroopCountForBossFightInHideout => MathF.Floor(1f + 5f * (1f + Campaign.Current.PlayerProgress));

	public override float SpawnPercentageForFirstFightInHideoutMission => 0.8f;

	private Clan DeserterClan
	{
		get
		{
			if (_deserterClan == null)
			{
				_deserterClan = Clan.FindFirst((Clan x) => x.StringId == "deserters");
			}
			return _deserterClan;
		}
	}

	public override int GetMinimumTroopCountForHideoutMission(MobileParty party, bool isAssault)
	{
		if (!isAssault)
		{
			return 25;
		}
		return 8;
	}

	public override int GetMaxSupportedNumberOfLootersForClan(Clan clan)
	{
		if (clan == DeserterClan)
		{
			return 50;
		}
		if (clan.StringId == "looters" && DeserterClan != null)
		{
			return 270 - DeserterClan.WarPartyComponents.Count;
		}
		return 270;
	}

	public override int GetMaximumTroopCountForHideoutMission(MobileParty party, bool isAssault)
	{
		int num = (isAssault ? 15 : 40);
		if (party.HasPerk(DefaultPerks.Tactics.SmallUnitTactics))
		{
			num += (int)DefaultPerks.Tactics.SmallUnitTactics.PrimaryBonus;
		}
		return num;
	}

	public override bool IsPositionInsideNavalSafeZone(CampaignVec2 position)
	{
		return false;
	}
}
