using System;
using System.Linq;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineThirdActFourthQuestBehavior : CampaignBehaviorBase
{
	private const string QuestConversationMenuId = "naval_storyline_act_3_quest_4_conversation_menu";

	private bool _isQuestAcceptedThroughMission;

	private bool _initialConversationIsDone;

	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
			NavalDLCEvents.OnNavalStorylineCanceledEvent.AddNonSerializedListener(this, OnNavalStorylineCanceled);
		}
	}

	private void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddDialogs();
		AddGameMenus(starter);
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("naval_storyline_act_3_quest_4_conversation_menu", string.Empty, naval_storyline_act_3_quest_4_conversation_menu_on_init);
	}

	private void AddDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine(new TextObject("{=sob0plMW}Good news, {PLAYER.NAME}... Bjolgur’s order has given him permission to sail with us."), (IAgent agent) => agent.Character == NavalStorylineData.Gunnar.CharacterObject, (IAgent agent) => agent.Character == CharacterObject.PlayerCharacter).Condition(GunnarActivateQuestFourDialog1OnCondition)
			.Consequence(GunnarActivateQuestFourDialog1OnConsequence)
			.NpcLine(new TextObject("{=eiX98VE9}Greetings, {PLAYER.NAME}... I’ve got my longship, Corpse-Maker, and more of my brothers may yet join us on the journey. We also brought a captured vessel, agile and light, which mounts a ballista. We call it the Golden Wasp. We’ve bought up most of the ale in Ostican for our voyage, as I think we’ll be heading for the sweltering seas of the south."), (IAgent agent) => agent.Character == NavalStorylineData.Bjolgur.CharacterObject, (IAgent agent) => agent.Character == CharacterObject.PlayerCharacter)
			.NpcLine(new TextObject("{=egYc68CI}I’ve been making some inquiries. Crusas is well-known and respected in the Empire and in Vlandia. He mines sulfur from islands in the Gulf of Charas. No doubt he uses some of Purig’s slaves, but I guess the grand lords and ladies don’t know that, or choose not to know."), (IAgent agent) => agent.Character == NavalStorylineData.Gunnar.CharacterObject, (IAgent agent) => agent.Character == CharacterObject.PlayerCharacter)
			.BeginPlayerOptions()
			.PlayerOption("{=npbsJToM}I hope, then, that he should not be difficult to find.", (IAgent agent) => agent.Character == NavalStorylineData.Gunnar.CharacterObject)
			.GotoDialogState("q4_next_line")
			.PlayerOption("{=Cywj1xTj}Well respected or not, I’m ready to track him down.", (IAgent agent) => agent.Character == NavalStorylineData.Gunnar.CharacterObject)
			.GotoDialogState("q4_next_line")
			.EndPlayerOptions()
			.NpcLine(new TextObject("{=sghtD7ov}Not hard to find at all.. On the way here I hailed some fishermen who chase tuna in the Gulf of Charas, and they say he is known to frequent a string of islands known as the Skatrias. They are said to be barren and foul-smelling. I can’t think why a merchant would want to anchor there, were they not the site of these sulfur mines where the captives are sent.{NEW_LINE}{NEW_LINE}So… I say we set out for these islands and hunt for Crusas.").SetTextVariable("NEW_LINE", "\n"), (IAgent agent) => agent.Character == NavalStorylineData.Bjolgur.CharacterObject, (IAgent agent) => agent.Character == CharacterObject.PlayerCharacter, "q4_next_line", "q4_next_line_player_choices")
			.BeginPlayerOptions("q4_next_line_player_choices")
			.PlayerOption("{=el44RZG4}Let us set out, then.", (IAgent agent) => agent.Character == NavalStorylineData.Bjolgur.CharacterObject)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, NavalStorylineData.OnPlayerPostponedQuestStart);
				};
			})
			.CloseDialog()
			.PlayerOption("{=a0j86F9C}I need a bit more time.", (IAgent agent) => agent.Character == NavalStorylineData.Bjolgur.CharacterObject)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions());
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine(new TextObject("{=C8aEfvMM}Are we ready to set sail for the Skatrias? I imagine that Crusas will be docked there for some time, but we don’t want to miss this opportunity."), (IAgent agent) => agent.Character == NavalStorylineData.Gunnar.CharacterObject, (IAgent agent) => agent.Character == CharacterObject.PlayerCharacter).Condition(GunnarActivateQuestFourDialog2OnCondition)
			.BeginPlayerOptions()
			.PlayerOption("{=el44RZG4}Let us set out, then.", (IAgent agent) => agent.Character == NavalStorylineData.Gunnar.CharacterObject)
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += ActivateQuest4;
				}
				else
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, NavalStorylineData.OnPlayerPostponedQuestStart);
					};
				}
			})
			.CloseDialog()
			.PlayerOption("{=a0j86F9C}I need a bit more time.", (IAgent agent) => agent.Character == NavalStorylineData.Gunnar.CharacterObject)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions());
	}

	private void naval_storyline_act_3_quest_4_conversation_menu_on_init(MenuCallbackArgs args)
	{
		if (_isQuestAcceptedThroughMission && Mission.Current == null)
		{
			ActivateQuest4();
			_isQuestAcceptedThroughMission = false;
		}
	}

	private bool GunnarActivateQuestFourDialog1OnCondition()
	{
		int num;
		if (!_initialConversationIsDone && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !NavalStorylineData.IsNavalStoryLineActive() && NavalStorylineData.IsStorylineActivationPossible())
		{
			num = (NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3SpeakToSailors) ? 1 : 0);
			if (num != 0)
			{
				SpawnBjolgur();
				Agent item = Mission.Current.Agents.First((Agent x) => x.Character == NavalStorylineData.Bjolgur.CharacterObject);
				Campaign.Current.ConversationManager.AddConversationAgents(new MBList<IAgent> { item }, setActionsInstantly: false);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private void GunnarActivateQuestFourDialog1OnConsequence()
	{
		_initialConversationIsDone = true;
	}

	private static void SpawnBjolgur()
	{
		Agent agent = Mission.Current.Agents.First((Agent x) => x.Character == NavalStorylineData.Gunnar.CharacterObject);
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Bjolgur.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		Vec3 position = agent.Position - Agent.Main.Position;
		position.RotateAboutZ(0.34906584f);
		position += Agent.Main.Position;
		int num = 250;
		while (true)
		{
			UIntPtr? uIntPtr = Mission.Current?.Scene?.GetNavigationMeshForPosition(in position);
			UIntPtr zero = UIntPtr.Zero;
			if (!uIntPtr.HasValue || (uIntPtr.HasValue && !(uIntPtr.GetValueOrDefault() == zero)) || num == 0)
			{
				break;
			}
			if (MBRandom.RandomFloat > 0.5f)
			{
				position.RotateAboutZ(System.MathF.PI / 180f * (float)MBRandom.RandomInt(20, 45));
			}
			else
			{
				position.RotateAboutZ(System.MathF.PI / 180f * (float)MBRandom.RandomInt(-45, -20));
			}
			num--;
		}
		if (num == 0)
		{
			Debug.FailedAssert("Couldn't find a valid position for Bjolgur around Gunnar", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\CampaignBehaviors\\NavalStorylineThirdActFourthQuestBehavior.cs", "SpawnBjolgur", 169);
			position = Mission.Current.GetRandomPositionAroundPoint(agent.Position, 1f, 3f, nearFirst: true);
		}
		agentBuildData.InitialPosition(in position);
		agentBuildData.InitialDirection(-Agent.Main.LookDirection.AsVec2.Normalized());
		agentBuildData.NoHorses(noHorses: true);
		agentBuildData.CivilianEquipment(civilianEquipment: true);
		Mission.Current.SpawnAgent(agentBuildData);
	}

	private bool GunnarActivateQuestFourDialog2OnCondition()
	{
		if (_initialConversationIsDone && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !NavalStorylineData.IsNavalStoryLineActive() && NavalStorylineData.IsStorylineActivationPossible())
		{
			return NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3SpeakToSailors);
		}
		return false;
	}

	private void QuestAccepted()
	{
		_isQuestAcceptedThroughMission = true;
		OpenQuestMenu();
		Mission.Current.EndMission();
	}

	private void OpenQuestMenu()
	{
		GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_4_conversation_menu");
	}

	private void ActivateQuest4()
	{
		new GoToSkatriaIslandsQuest(corsairSpawnPosition: new CampaignVec2(new Vec2(285f, 300f), isOnLand: false), questId: "naval_storyline_act_3_quest_4", questGiver: NavalStorylineData.Gunnar).StartQuest();
	}
}
