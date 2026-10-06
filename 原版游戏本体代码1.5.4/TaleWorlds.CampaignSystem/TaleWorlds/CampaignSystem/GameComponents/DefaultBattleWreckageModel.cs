using TaleWorlds.CampaignSystem.BattleWreckages;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBattleWreckageModel : BattleWreckageModel
{
	private const int MaxLandWreckageCount = 50;

	private const int SmallWreckageBattleSizeThreshold = 15;

	private const int NormalWreckageBattleSizeThreshold = 50;

	private const int EpicWreckageBattleSizeThreshold = 150;

	public override bool CanPlayerInteractWithWreckage(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return true;
	}

	public override int GetMaxWreckageCountForMapEventType(MapEvent mapEvent)
	{
		return 50;
	}

	public override int GetWreckageCreationBattleSizeThreshold(MapEvent mapEvent)
	{
		return 15;
	}

	public override BattleWreckage.WreckageType GetWreckageTypeForMapEvent(MapEvent mapEvent)
	{
		int num = mapEvent.AttackerSide.Parties.SumQ((MapEventParty x) => x.WoundedInBattle.TotalRegulars + x.DiedInBattle.TotalRegulars) + mapEvent.DefenderSide.Parties.SumQ((MapEventParty x) => x.WoundedInBattle.TotalRegulars + x.DiedInBattle.TotalRegulars);
		if (num > 150)
		{
			PartyBase leaderParty = mapEvent.AttackerSide.LeaderParty;
			if (leaderParty != null && leaderParty.MobileParty?.IsLordParty == true)
			{
				PartyBase leaderParty2 = mapEvent.DefenderSide.LeaderParty;
				if (leaderParty2 != null && leaderParty2.MobileParty?.IsLordParty == true)
				{
					return BattleWreckage.WreckageType.Epic;
				}
			}
			return BattleWreckage.WreckageType.Normal;
		}
		if (num > 50)
		{
			return BattleWreckage.WreckageType.Normal;
		}
		if (num >= 15)
		{
			return BattleWreckage.WreckageType.Small;
		}
		Debug.FailedAssert("This case for wreckage should not be possible, check this", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultBattleWreckageModel.cs", "GetWreckageTypeForMapEvent", 64);
		return BattleWreckage.WreckageType.Invalid;
	}
}
