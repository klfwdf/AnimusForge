using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class SetSailAndMeetTheFortuneSeekersInTargetSettlementQuest : NavalStorylineQuestBase
{
	public const float DistanceToSettlementToSpawnMerchantParty = 10f;

	private static readonly Dictionary<string, string> PlayerShipUpgradePieces = new Dictionary<string, string>
	{
		{ "sail", "sails_lvl2" },
		{ "side", "side_northern_shields_lvl2" }
	};

	public const string PlayerPartySailPatternId = "generated_square__h4_09";

	[SaveableField(1)]
	private Settlement _targetSettlement;

	[SaveableField(2)]
	private bool _willProgressStoryline;

	public override bool WillProgressStoryline => _willProgressStoryline;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=ugNRbWrI}Meet the Vlandian Merchants");
			textObject.SetTextVariable("SETTLEMENT_NAME", _targetSettlement.Name);
			return textObject;
		}
	}

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3Quest1;

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_1_main_party_template";

	private TextObject _descriptionLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=eIGO7zhf}Locate the Vlandian merchant ship in the waters off {SETTLEMENT_LINK}.");
			textObject.SetTextVariable("SETTLEMENT_LINK", _targetSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	public SetSailAndMeetTheFortuneSeekersInTargetSettlementQuest(string questId, Hero questGiver, Settlement targetSettlement)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		_willProgressStoryline = true;
		_targetSettlement = targetSettlement;
		AddTrackedObject(_targetSettlement);
		AddLog(_descriptionLogText);
	}

	protected override void OnStartQuestInternal()
	{
		NavalDLCHelpers.AddUpgradePiecesToPartyShips(MobileParty.MainParty, PlayerShipUpgradePieces, DefaultFigureheads.Dragon);
		NavalDLCHelpers.SetCustomSailPatternOfPartyShips(MobileParty.MainParty, "generated_square__h4_09");
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		if (MobileParty.MainParty.IsActive)
		{
			NavalDLCHelpers.SetCustomSailPatternOfPartyShips(MobileParty.MainParty, "generated_square__h4_09");
		}
	}

	protected override void SetDialogs()
	{
	}

	protected override void HourlyTick()
	{
		if (MobileParty.MainParty.Position.Distance(_targetSettlement.PortPosition) <= MathF.Min(10f, MobileParty.MainParty.SeeingRange))
		{
			_willProgressStoryline = false;
			new SetSailAndEscortTheFortuneSeekersQuest("naval_storyline_act3_quest1_2", NavalStorylineData.Gunnar, _targetSettlement).StartQuest();
			CompleteQuestWithSuccess();
		}
	}

	protected override void RegisterEventsInternal()
	{
	}
}
