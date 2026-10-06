using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class HeroExecutionSceneNotificationData : SceneNotificationData
{
	private bool _runAffirmativeActionAtClose;

	private readonly Action _onAffirmativeAction;

	private readonly Action _onNegativeAction;

	protected static int MaxShownRelationChanges = 8;

	private bool _isVisualOnly;

	private bool _useExecutioner;

	private readonly bool _shouldAutoConfirm;

	public Hero Executer { get; }

	public Hero Victim { get; }

	public bool IsPlayerExecutionPrompt { get; private set; }

	public override bool IsNegativeOptionShown { get; }

	public override string SceneID => "scn_execution_notification";

	public override TextObject NegativeText => GameTexts.FindText("str_execution_negative_action");

	public override bool IsAffirmativeOptionShown => !_shouldAutoConfirm;

	public override bool ShouldAutoConfirm => _shouldAutoConfirm;

	public override TextObject TitleText { get; }

	public override TextObject DescriptionText { get; }

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
		Equipment equipment2 = Executer.Culture.Executioner.FirstBattleEquipment.Clone(cloneWithoutWeapons: true);
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(item));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, default(EquipmentElement));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, default(EquipmentElement));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, default(EquipmentElement));
		equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ExtraWeaponSlot, default(EquipmentElement));
		SceneNotificationCharacter sceneNotificationCharacter = CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(Executer, equipment2);
		if (_useExecutioner)
		{
			sceneNotificationCharacter = CreateExecutorCharacter(Executer.Culture.Executioner, equipment2);
		}
		return new SceneNotificationCharacter[2]
		{
			CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(Victim, equipment),
			sceneNotificationCharacter
		};
	}

	private SceneNotificationCharacter CreateExecutorCharacter(CharacterObject characterObject, Equipment overridenEquipment = null, bool useCivilian = false, BodyProperties overriddenBodyProperties = default(BodyProperties), uint overriddenColor1 = uint.MaxValue, uint overriddenColor2 = uint.MaxValue, bool useHorse = false)
	{
		if (overriddenColor1 == uint.MaxValue)
		{
			overriddenColor1 = characterObject.Culture.Color;
		}
		if (overriddenColor2 == uint.MaxValue)
		{
			overriddenColor2 = characterObject.Culture.Color2;
		}
		return new SceneNotificationCharacter(characterObject, overridenEquipment, overriddenBodyProperties, useCivilian, overriddenColor1, overriddenColor2, useHorse);
	}

	private HeroExecutionSceneNotificationData(Hero executingHero, Hero dyingHero, TextObject titleText, TextObject descriptionText, TextObject affirmativeTitleText, TextObject affirmativeActionText, TextObject affirmativeActionDescriptionText, TextObject affirmativeActionHintText, TextObject affirmativeActionHintExtendedText, bool isNegativeOptionShown, Action onAffirmativeAction, Action onNegativeAction = null, RelevantContextType relevantContextType = RelevantContextType.Any, bool isVisualOnly = false, bool useExecutioner = false, bool shouldAutoConfirm = false)
	{
		Executer = executingHero;
		Victim = dyingHero;
		TitleText = titleText;
		DescriptionText = descriptionText;
		AffirmativeTitleText = affirmativeTitleText;
		AffirmativeText = affirmativeActionText;
		AffirmativeDescriptionText = affirmativeActionDescriptionText;
		AffirmativeHintText = affirmativeActionHintText;
		AffirmativeHintTextExtended = affirmativeActionHintExtendedText;
		IsNegativeOptionShown = isNegativeOptionShown;
		RelevantContext = relevantContextType;
		_onAffirmativeAction = onAffirmativeAction;
		_onNegativeAction = onNegativeAction;
		_runAffirmativeActionAtClose = false;
		_isVisualOnly = isVisualOnly;
		_useExecutioner = useExecutioner;
		_shouldAutoConfirm = shouldAutoConfirm;
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

	public override void OnNegativeAction()
	{
		base.OnNegativeAction();
		_onNegativeAction?.Invoke();
		_runAffirmativeActionAtClose = false;
	}

	private void PostponedAffirmativeAction()
	{
		if (_runAffirmativeActionAtClose && !_isVisualOnly)
		{
			if (_onAffirmativeAction != null)
			{
				_onAffirmativeAction();
			}
			else if (Victim != Hero.MainHero)
			{
				if (Executer.PartyBelongedTo != null && Executer.PartyBelongedTo.MapEvent != null)
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

	public static HeroExecutionSceneNotificationData CreateForPlayerExecutingHero(Hero dyingHero, Action onAffirmativeAction, RelevantContextType relevantContextType = RelevantContextType.Any, bool showNegativeOption = true, Action onNegativeAction = null)
	{
		GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
		GameTexts.SetVariable("YEAR", CampaignTime.Now.GetYear);
		GameTexts.SetVariable("NAME", dyingHero.Name);
		GameTexts.SetVariable("CLAN_NAME", dyingHero.Clan?.Name);
		TextObject textObject = GameTexts.FindText("str_execution_positive_action");
		textObject.SetCharacterProperties("DYING_HERO", dyingHero.CharacterObject);
		return new HeroExecutionSceneNotificationData(Hero.MainHero, dyingHero, GameTexts.FindText("str_executing_prisoner"), GetExecuteTroopDescriptionText(dyingHero), GameTexts.FindText("str_executed_prisoner"), textObject, GameTexts.FindText("str_cannot_undo"), GetExecuteTroopHintText(dyingHero, showAll: false), GetExecuteTroopHintText(dyingHero, showAll: true), showNegativeOption, onAffirmativeAction, onNegativeAction, relevantContextType)
		{
			IsPlayerExecutionPrompt = true
		};
	}

	public static HeroExecutionSceneNotificationData CreateForInformingPlayer(Hero executingHero, Hero dyingHero, CampaignTime date, RelevantContextType relevantContextType = RelevantContextType.Any, Action onClose = null, bool isVisualOnly = false, bool useExecutioner = false, bool shouldAutoConfirm = false, bool showNegativeOption = false, Action onNegativeAction = null)
	{
		GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(date));
		GameTexts.SetVariable("YEAR", date.GetYear);
		GameTexts.SetVariable("NAME", dyingHero.Name);
		TextObject textObject = new TextObject("{=uYjEknNX}{VICTIM.NAME}'s execution by {EXECUTER.NAME}");
		textObject.SetCharacterProperties("VICTIM", dyingHero.CharacterObject);
		textObject.SetCharacterProperties("EXECUTER", executingHero.CharacterObject);
		if (useExecutioner)
		{
			textObject = new TextObject("{=2nOppdq8}{VICTIM.NAME}'s execution by order of {CLAN_NAME}");
			textObject.SetCharacterProperties("VICTIM", dyingHero.CharacterObject);
			textObject.SetTextVariable("CLAN_NAME", executingHero.Clan.Name);
		}
		return new HeroExecutionSceneNotificationData(executingHero, dyingHero, textObject, null, GameTexts.FindText("str_executed_prisoner"), GameTexts.FindText("str_proceed"), null, null, null, showNegativeOption, onClose, onNegativeAction, relevantContextType, isVisualOnly, useExecutioner, shouldAutoConfirm);
	}

	private static TextObject GetExecuteTroopDescriptionText(Hero dyingHero)
	{
		if (dyingHero.Clan == null)
		{
			return null;
		}
		if (dyingHero.Clan.HasBloodFeudWithPlayer)
		{
			return GameTexts.FindText("str_execute_prisoner_desc_blood_feud");
		}
		return GameTexts.FindText("str_execute_prisoner_desc_no_blood_feud");
	}

	private static TextObject GetExecuteTroopHintText(Hero dyingHero, bool showAll)
	{
		Dictionary<Clan, int> dictionary = new Dictionary<Clan, int>();
		GameTexts.SetVariable("LEFT", new TextObject("{=jxypVgl2}Relation Changes"));
		string text = GameTexts.FindText("str_LEFT_colon").ToString();
		if (dyingHero.Clan != null && !dyingHero.Clan.HasBloodFeudWithPlayer)
		{
			foreach (Clan item in Clan.All)
			{
				int bloodFeudStartRelationPenaltyToOtherClan = ExecutionCampaignBehavior.GetBloodFeudStartRelationPenaltyToOtherClan(dyingHero, item);
				if (bloodFeudStartRelationPenaltyToOtherClan == 0)
				{
					continue;
				}
				if (dictionary.ContainsKey(item))
				{
					if (bloodFeudStartRelationPenaltyToOtherClan < dictionary[item])
					{
						dictionary[item] = bloodFeudStartRelationPenaltyToOtherClan;
					}
				}
				else
				{
					dictionary.Add(item, bloodFeudStartRelationPenaltyToOtherClan);
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
