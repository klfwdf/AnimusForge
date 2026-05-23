using System.Collections.Generic;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.SecondPhase;

public class AssembleEmpireQuestBehavior : CampaignBehaviorBase
{
	public class AssembleEmpireQuestBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public AssembleEmpireQuestBehaviorTypeDefiner()
			: base(1002000)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(AssembleEmpireQuest), 1);
		}
	}

	public class AssembleEmpireQuest : StoryModeQuestBase
	{
		private int _imperialCultureTowns;

		private int _ownedByPlayerImperialTowns;

		private bool _assembledEmpire;

		private const float _ratioOfSettlementToTake = 0.66f;

		[SaveableField(1)]
		private JournalLog _numberOfCapturedSettlementsLog;

		public override TextObject Title => new TextObject("{=ya8eMCpj}Unify the Empire");

		private TextObject _questCanceledLogText => new TextObject("{=tVlZTOst}You have chosen a different path.");

		public AssembleEmpireQuest(Hero questGiver)
			: base("assemble_empire_quest", questGiver, CampaignTime.Never)
		{
			_assembledEmpire = false;
			CacheSettlementCounts();
			SetDialogs();
			InitializeQuestOnCreation();
			_numberOfCapturedSettlementsLog = AddDiscreteLog(new TextObject("{=3deb2lMd}To restore the Empire you should capture two thirds of settlements with imperial culture."), new TextObject("{=Dp6newHS}Conquered Settlements"), _ownedByPlayerImperialTowns, MathF.Ceiling((float)_imperialCultureTowns * 0.66f));
		}

		protected override void SetDialogs()
		{
			DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=mxKhvbn7}You have decided to unify the Empire.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
				.CloseDialog();
		}

		protected override void InitializeQuestOnGameLoad()
		{
			CacheSettlementCounts();
			SetDialogs();
			if (_numberOfCapturedSettlementsLog == null)
			{
				_numberOfCapturedSettlementsLog = AddDiscreteLog(new TextObject("{=3deb2lMd}To restore the Empire you should capture two thirds of settlements with imperial culture."), new TextObject("{=Dp6newHS}Conquered Settlements"), _ownedByPlayerImperialTowns, MathF.Ceiling((float)_imperialCultureTowns * 0.66f));
			}
			_numberOfCapturedSettlementsLog.UpdateCurrentProgress((int)MathF.Clamp(_ownedByPlayerImperialTowns, 0f, _imperialCultureTowns));
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
			StoryModeEvents.OnConspiracyActivatedEvent.AddNonSerializedListener(this, OnConspiracyActivated);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		}

		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan && oldKingdom == StoryModeManager.Current.MainStoryLine.PlayerSupportedKingdom)
			{
				CompleteQuestWithCancel(_questCanceledLogText);
				StoryModeManager.Current.MainStoryLine.CancelSecondAndThirdPhase();
			}
		}

		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement.IsTown && settlement.Culture.StringId == "empire")
			{
				if (settlement.OwnerClan.Kingdom == Clan.PlayerClan.Kingdom && oldOwner.Clan.Kingdom != Clan.PlayerClan.Kingdom)
				{
					_ownedByPlayerImperialTowns++;
				}
				if (oldOwner.Clan.Kingdom == Clan.PlayerClan.Kingdom && newOwner.Clan.Kingdom != Clan.PlayerClan.Kingdom)
				{
					_ownedByPlayerImperialTowns--;
				}
				_numberOfCapturedSettlementsLog.UpdateCurrentProgress((int)MathF.Clamp(_ownedByPlayerImperialTowns, 0f, _imperialCultureTowns));
			}
		}

		protected override void HourlyTick()
		{
			if (QuestConditionsHold())
			{
				SuccessQuest();
			}
		}

		private void OnConspiracyActivated()
		{
			if (!_assembledEmpire)
			{
				CompleteQuestWithFail(new TextObject("{=80NOk1Ee}You could not unify the Empire."));
			}
		}

		private void CacheSettlementCounts()
		{
			_imperialCultureTowns = 0;
			_ownedByPlayerImperialTowns = 0;
			foreach (Settlement item in Settlement.All)
			{
				if (item.IsTown && item.Culture.StringId == "empire")
				{
					_imperialCultureTowns++;
					if (item.OwnerClan.Kingdom == Clan.PlayerClan.Kingdom)
					{
						_ownedByPlayerImperialTowns++;
					}
				}
			}
		}

		private bool QuestConditionsHold()
		{
			return _ownedByPlayerImperialTowns >= MathF.Ceiling((float)_imperialCultureTowns * 0.66f);
		}

		private void SuccessQuest()
		{
			AddLog(new TextObject("{=sJeYHMGG}You have unified the Empire."));
			CompleteQuestWithSuccess();
			_assembledEmpire = true;
			StoryMode.StoryModePhases.SecondPhase.Instance.ActivateConspiracy();
		}

		internal static void AutoGeneratedStaticCollectObjectsAssembleEmpireQuest(object o, List<object> collectedObjects)
		{
			((AssembleEmpireQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_numberOfCapturedSettlementsLog);
		}

		internal static object AutoGeneratedGetMemberValue_numberOfCapturedSettlementsLog(object o)
		{
			return ((AssembleEmpireQuest)o)._numberOfCapturedSettlementsLog;
		}
	}

	public override void RegisterEvents()
	{
		StoryModeEvents.OnMainStoryLineSideChosenEvent.AddNonSerializedListener(this, OnMainStoryLineSideChosen);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnMainStoryLineSideChosen(MainStoryLineSide side)
	{
		if (side == MainStoryLineSide.CreateImperialKingdom || side == MainStoryLineSide.SupportImperialKingdom)
		{
			new AssembleEmpireQuest(StoryModeHeroes.ImperialMentor).StartQuest();
		}
	}
}
