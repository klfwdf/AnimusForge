using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem.BattleWreckages;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCBattleWreckageModel : BattleWreckageModel
{
	private const int MaxNavalWreckageCount = 50;

	private const int NavalSmallWreckageBattleSizeThreshold = 20;

	private const int NavalNormalWreckageBattleSizeThreshold = 100;

	private const int NavalEpicWreckageBattleSizeThreshold = 120;

	public override bool CanPlayerInteractWithWreckage(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		if (NavalStorylineData.IsNavalStoryLineActive())
		{
			explanation = new TextObject("{=sNjXlCkB}The seas are rough. It is unlikely that there are any survivors, and you risk damaging your ship by colliding with floating wreckage. Gunnar urges you not to risk going any closer.");
			return false;
		}
		return true;
	}

	public override int GetMaxWreckageCountForMapEventType(MapEvent mapEvent)
	{
		if (mapEvent.IsNavalMapEvent)
		{
			return 50;
		}
		return base.BaseModel.GetMaxWreckageCountForMapEventType(mapEvent);
	}

	public override int GetWreckageCreationBattleSizeThreshold(MapEvent mapEvent)
	{
		if (mapEvent.IsNavalMapEvent)
		{
			return 20;
		}
		return base.BaseModel.GetWreckageCreationBattleSizeThreshold(mapEvent);
	}

	public override BattleWreckage.WreckageType GetWreckageTypeForMapEvent(MapEvent mapEvent)
	{
		if (mapEvent.IsNavalMapEvent)
		{
			int num = mapEvent.AttackerSide.Parties.SumQ((MapEventParty x) => x.WoundedInBattle.TotalRegulars + x.DiedInBattle.TotalRegulars) + mapEvent.DefenderSide.Parties.SumQ((MapEventParty x) => x.WoundedInBattle.TotalRegulars + x.DiedInBattle.TotalRegulars);
			if (num > 120)
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
			if (num > 100)
			{
				return BattleWreckage.WreckageType.Normal;
			}
			if (num >= 20)
			{
				return BattleWreckage.WreckageType.Small;
			}
			Debug.FailedAssert("This case for wreckage should not be possible, check this", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCBattleWreckageModel.cs", "GetWreckageTypeForMapEvent", 85);
			return BattleWreckage.WreckageType.Invalid;
		}
		return base.BaseModel.GetWreckageTypeForMapEvent(mapEvent);
	}
}
