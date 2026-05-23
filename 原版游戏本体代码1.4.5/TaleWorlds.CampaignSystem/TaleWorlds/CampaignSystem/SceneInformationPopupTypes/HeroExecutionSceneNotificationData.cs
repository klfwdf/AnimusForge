using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class HeroExecutionSceneNotificationData : SceneNotificationData
{
	private bool _runAffirmativeActionAtClose;

	private readonly Action _onAffirmativeAction;

	protected static int MaxShownRelationChanges = 8;

	public Hero Executer { get; }

	public Hero Victim { get; }

	public override bool IsNegativeOptionShown { get; }

	public override string SceneID => "scn_execution_notification";

	public override TextObject NegativeText => GameTexts.FindText("str_execution_negative_action");

	public override bool IsAffirmativeOptionShown => true;

	public override TextObject TitleText { get; }

	public override TextObject AffirmativeText { get; }

	public override TextObject AffirmativeTitleText { get; }

	public override TextObject AffirmativeHintText { get; }

	public override TextObject AffirmativeHintTextExtended { get; }

	public override TextObject AffirmativeDescriptionText { get; }

	public override RelevantContextType RelevantContext { get; }

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		Equipment equipment = Victim.BattleEquipment.Clone(cloneWithoutWeapons: true);
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, default(EquipmentElement));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, default(EquipmentElement));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, default(EquipmentElement));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, default(EquipmentElement));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ExtraWeaponSlot, default(EquipmentElement));
		ItemObject item = Items.All.FirstOrDefault((ItemObject i) => i.StringId == "execution_axe");
		Equipment equipment2 = Executer.BattleEquipment.Clone(cloneWithoutWeapons: true);
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(item));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, default(EquipmentElement));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, default(EquipmentElement));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, default(EquipmentElement));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ExtraWeaponSlot, default(EquipmentElement));
		return new SceneNotificationCharacter[2]
		{
			CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(Victim, equipment),
			CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(Executer, equipment2)
		};
	}

	private HeroExecutionSceneNotificationData(Hero executingHero, Hero dyingHero, TextObject titleText, TextObject affirmativeTitleText, TextObject affirmativeActionText, TextObject affirmativeActionDescriptionText, TextObject affirmativeActionHintText, TextObject affirmativeActionHintExtendedText, bool isNegativeOptionShown, Action onAffirmativeAction, RelevantContextType relevantContextType = RelevantContextType.Any)
	{
		Executer = executingHero;
		Victim = dyingHero;
		TitleText = titleText;
		AffirmativeTitleText = affirmativeTitleText;
		AffirmativeText = affirmativeActionText;
		AffirmativeDescriptionText = affirmativeActionDescriptionText;
		AffirmativeHintText = affirmativeActionHintText;
		AffirmativeHintTextExtended = affirmativeActionHintExtendedText;
		IsNegativeOptionShown = isNegativeOptionShown;
		RelevantContext = relevantContextType;
		_onAffirmativeAction = onAffirmativeAction;
		_runAffirmativeActionAtClose = false;
	}

	public override void OnCloseAction()
	{
		PostponedAffirmativeAction();
	}

	public override void OnAffirmativeAction()
	{
		base.OnAffirmativeAction();
		_runAffirmativeActionAtClose = true;
	}

	private void PostponedAffirmativeAction()
	{
		if (_runAffirmativeActionAtClose)
		{
			if (_onAffirmativeAction != null)
			{
				_onAffirmativeAction();
			}
			else if (Victim != Hero.MainHero)
			{
				if (MobileParty.MainParty.MapEvent != null)
				{
					KillCharacterAction.ApplyByExecutionAfterMapEvent(Victim, Executer, showNotification: true, isForced: true);
				}
				else
				{
					KillCharacterAction.ApplyByExecution(Victim, Executer, showNotification: true, isForced: true);
				}
			}
		}
		_runAffirmativeActionAtClose = false;
	}

	public static HeroExecutionSceneNotificationData CreateForPlayerExecutingHero(Hero dyingHero, Action onAffirmativeAction, RelevantContextType relevantContextType = RelevantContextType.Any, bool showNegativeOption = true)
	{
		GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
		GameTexts.SetVariable("YEAR", CampaignTime.Now.GetYear);
		GameTexts.SetVariable("NAME", dyingHero.Name);
		TextObject textObject = GameTexts.FindText("str_execution_positive_action");
		textObject.SetCharacterProperties("DYING_HERO", dyingHero.CharacterObject);
		return new HeroExecutionSceneNotificationData(Hero.MainHero, dyingHero, GameTexts.FindText("str_executing_prisoner"), GameTexts.FindText("str_executed_prisoner"), textObject, GameTexts.FindText("str_execute_prisoner_desc"), GetExecuteTroopHintText(dyingHero, showAll: false), GetExecuteTroopHintText(dyingHero, showAll: true), showNegativeOption, onAffirmativeAction, relevantContextType);
	}

	public static HeroExecutionSceneNotificationData CreateForInformingPlayer(Hero executingHero, Hero dyingHero, RelevantContextType relevantContextType = RelevantContextType.Any, Action onClose = null)
	{
		GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
		GameTexts.SetVariable("YEAR", CampaignTime.Now.GetYear);
		GameTexts.SetVariable("NAME", dyingHero.Name);
		TextObject textObject = new TextObject("{=uYjEknNX}{VICTIM.NAME}'s execution by {EXECUTER.NAME}");
		textObject.SetCharacterProperties("VICTIM", dyingHero.CharacterObject);
		textObject.SetCharacterProperties("EXECUTER", executingHero.CharacterObject);
		return new HeroExecutionSceneNotificationData(executingHero, dyingHero, textObject, GameTexts.FindText("str_executed_prisoner"), GameTexts.FindText("str_proceed"), null, null, null, isNegativeOptionShown: false, onClose, relevantContextType);
	}

	private static TextObject GetExecuteTroopHintText(Hero dyingHero, bool showAll)
	{
		Dictionary<Clan, int> dictionary = new Dictionary<Clan, int>();
		GameTexts.SetVariable("LEFT", new TextObject("{=jxypVgl2}Relation Changes"));
		string text = GameTexts.FindText("str_LEFT_colon").ToString();
		if (dyingHero.Clan != null)
		{
			foreach (Clan item in Clan.All)
			{
				foreach (Hero hero in item.Heroes)
				{
					if (hero.IsHumanPlayerCharacter || !hero.IsAlive || hero == dyingHero || (hero.IsLord && hero.Clan.Leader != hero))
					{
						continue;
					}
					bool showQuickNotification;
					int relationChangeForExecutingHero = Campaign.Current.Models.ExecutionRelationModel.GetRelationChangeForExecutingHero(dyingHero, hero, out showQuickNotification);
					if (relationChangeForExecutingHero == 0)
					{
						continue;
					}
					if (dictionary.ContainsKey(item))
					{
						if (relationChangeForExecutingHero < dictionary[item])
						{
							dictionary[item] = relationChangeForExecutingHero;
						}
					}
					else
					{
						dictionary.Add(item, relationChangeForExecutingHero);
					}
				}
			}
			GameTexts.SetVariable("newline", "\n");
			List<KeyValuePair<Clan, int>> list = dictionary.OrderBy((KeyValuePair<Clan, int> change) => change.Value).ToList();
			int num = 0;
			foreach (KeyValuePair<Clan, int> item2 in list)
			{
				Clan key = item2.Key;
				int value = item2.Value;
				GameTexts.SetVariable("LEFT", key.Name);
				GameTexts.SetVariable("RIGHT", value);
				string content = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", content);
				text = GameTexts.FindText("str_string_newline_string").ToString();
				num++;
				if (!showAll && num == MaxShownRelationChanges)
				{
					TextObject content2 = new TextObject("{=DPTPuyip}And {NUMBER} more...");
					GameTexts.SetVariable("NUMBER", dictionary.Count - num);
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", content2);
					text = GameTexts.FindText("str_string_newline_string").ToString();
					TextObject textObject = new TextObject("{=u12ocP9f}Hold '{EXTEND_KEY}' for more info.");
					textObject.SetTextVariable("EXTEND_KEY", GameTexts.FindText("str_game_key_text", "anyalt"));
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", textObject);
					text = GameTexts.FindText("str_string_newline_string").ToString();
					break;
				}
			}
			return new TextObject("{=!}" + text);
		}
		return TextObject.GetEmpty();
	}
}
