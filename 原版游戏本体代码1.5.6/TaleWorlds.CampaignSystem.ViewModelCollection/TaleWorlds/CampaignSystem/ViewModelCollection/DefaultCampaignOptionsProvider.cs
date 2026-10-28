using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class DefaultCampaignOptionsProvider : ICampaignOptionProvider
{
	private CampaignOptionsDifficultyPresets _difficultyPreset;

	public IEnumerable<ICampaignOptionData> GetGameplayCampaignOptions()
	{
		yield return new SelectionCampaignOptionData("DifficultyPresets", 100, CampaignOptionEnableState.Enabled, () => (float)_difficultyPreset, delegate(float value)
		{
			_difficultyPreset = (CampaignOptionsDifficultyPresets)value;
		}, GetPresetTexts("DifficultyPresets"));
		IEnumerable<ICampaignOptionData> difficultyRelatedOptions = GetDifficultyRelatedOptions();
		foreach (ICampaignOptionData item in difficultyRelatedOptions)
		{
			yield return item;
		}
		yield return new BooleanCampaignOptionData("AutoAllocateClanMemberPerks", 1000, CampaignOptionEnableState.Enabled, () => (!CampaignOptions.AutoAllocateClanMemberPerks) ? 0f : 1f, delegate(float value)
		{
			CampaignOptions.AutoAllocateClanMemberPerks = value == 1f;
		});
		yield return new BooleanCampaignOptionData("IronmanMode", 1100, CampaignOptionEnableState.Disabled, () => (!CampaignOptions.IsIronmanMode) ? 0f : 1f, delegate(float value)
		{
			CampaignOptions.IsIronmanMode = value == 1f;
		});
		yield return new ActionCampaignOptionData("ResetTutorial", 10000, CampaignOptionEnableState.Enabled, ExecuteResetTutorial);
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			yield return new ActionCampaignOptionData("EnableCheats", 11000, CampaignOptionEnableState.Enabled, ExecuteEnableCheats);
		}
	}

	public IEnumerable<ICampaignOptionData> GetCharacterCreationCampaignOptions()
	{
		IEnumerable<ICampaignOptionData> difficultyOptions = GetDifficultyRelatedOptions();
		yield return new SelectionCampaignOptionData("DifficultyPresets", 100, CampaignOptionEnableState.Enabled, () => (float)_difficultyPreset, delegate(float value)
		{
			_difficultyPreset = (CampaignOptionsDifficultyPresets)value;
		}, GetPresetTexts("DifficultyPresets"));
		foreach (ICampaignOptionData item in difficultyOptions)
		{
			yield return item;
		}
		yield return new BooleanCampaignOptionData("AutoAllocateClanMemberPerks", 1000, CampaignOptionEnableState.Enabled, () => (!CampaignOptions.AutoAllocateClanMemberPerks) ? 0f : 1f, delegate(float value)
		{
			CampaignOptions.AutoAllocateClanMemberPerks = value == 1f;
		});
		if (MBSaveLoad.IsMaxNumberOfSavesReached())
		{
			yield return new BooleanCampaignOptionData("IronmanMode", 1100, CampaignOptionEnableState.Disabled, null, null, GetIronmanModeDisabledWithReason);
			yield break;
		}
		yield return new BooleanCampaignOptionData("IronmanMode", 1100, CampaignOptionEnableState.DisabledLater, () => (!CampaignOptions.IsIronmanMode) ? 0f : 1f, delegate(float value)
		{
			CampaignOptions.IsIronmanMode = value == 1f;
		});
	}

	private IEnumerable<ICampaignOptionData> GetDifficultyRelatedOptions()
	{
		yield return new SelectionCampaignOptionData("PlayerTroopsReceivedDamage", 300, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.PlayerTroopsReceivedDamage, delegate(float value)
		{
			CampaignOptions.PlayerTroopsReceivedDamage = (CampaignOptions.Difficulty)value;
		}, null, null, isRelatedToDifficultyPreset: true);
		yield return new SelectionCampaignOptionData("MaximumIndexPlayerCanRecruit", 400, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.RecruitmentDifficulty, delegate(float value)
		{
			CampaignOptions.RecruitmentDifficulty = (CampaignOptions.Difficulty)value;
		}, null, null, isRelatedToDifficultyPreset: true);
		yield return new SelectionCampaignOptionData("PlayerMapMovementSpeed", 500, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.PlayerMapMovementSpeed, delegate(float value)
		{
			CampaignOptions.PlayerMapMovementSpeed = (CampaignOptions.Difficulty)value;
		}, null, null, isRelatedToDifficultyPreset: true);
		yield return new SelectionCampaignOptionData("PersuasionSuccess", 600, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.PersuasionSuccessChance, delegate(float value)
		{
			CampaignOptions.PersuasionSuccessChance = (CampaignOptions.Difficulty)value;
		}, null, null, isRelatedToDifficultyPreset: true);
		yield return new SelectionCampaignOptionData("CombatAIDifficulty", 700, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.CombatAIDifficulty, delegate(float value)
		{
			CampaignOptions.CombatAIDifficulty = (CampaignOptions.Difficulty)value;
		}, null, null, isRelatedToDifficultyPreset: true);
		yield return new SelectionCampaignOptionData("ClanMemberBattleDeathPossibility", 800, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.ClanMemberDeathChance, delegate(float value)
		{
			CampaignOptions.ClanMemberDeathChance = (CampaignOptions.Difficulty)value;
		}, null, null, isRelatedToDifficultyPreset: true);
		yield return new SelectionCampaignOptionData("BattleDeath", 900, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.BattleDeath, delegate(float value)
		{
			CampaignOptions.BattleDeath = (CampaignOptions.Difficulty)value;
		}, null, GetBattleDeathDisabledStatusWithReason, isRelatedToDifficultyPreset: true);
		yield return new SelectionCampaignOptionData("StealthAndDisguiseDifficulty", 1000, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.StealthAndDisguiseDifficulty, delegate(float value)
		{
			CampaignOptions.StealthAndDisguiseDifficulty = (CampaignOptions.Difficulty)value;
		}, null, null, isRelatedToDifficultyPreset: true, GetDisguiseAndStealthDefaultPresetForValue, GetDisguiseAndStealthDefaultValueForDifficultyPreset);
	}

	private CampaignOptionsDifficultyPresets GetDisguiseAndStealthDefaultPresetForValue(float value)
	{
		switch ((int)value)
		{
		case 0:
			return CampaignOptionsDifficultyPresets.Freebooter;
		case 1:
		case 2:
			return CampaignOptionsDifficultyPresets.Warrior;
		default:
			return CampaignOptionsDifficultyPresets.Custom;
		}
	}

	private float GetDisguiseAndStealthDefaultValueForDifficultyPreset(CampaignOptionsDifficultyPresets preset)
	{
		switch (preset)
		{
		case CampaignOptionsDifficultyPresets.Freebooter:
			return 0f;
		case CampaignOptionsDifficultyPresets.Warrior:
		case CampaignOptionsDifficultyPresets.Bannerlord:
			return 1f;
		default:
			return 0f;
		}
	}

	private CampaignOptionDisableStatus GetIronmanModeDisabledWithReason()
	{
		if (!MBSaveLoad.IsMaxNumberOfSavesReached())
		{
			return new CampaignOptionDisableStatus(isDisabled: false, string.Empty);
		}
		TextObject textObject = GameTexts.FindText("str_string_newline_string").SetTextVariable("STR1", string.Empty).SetTextVariable("STR2", new TextObject("{=ld0SelSH}Ironman mode requires at least one available save slot."))
			.SetTextVariable("newline", "\n");
		return new CampaignOptionDisableStatus(isDisabled: true, textObject.ToString(), 0f);
	}

	private CampaignOptionDisableStatus GetBattleDeathDisabledStatusWithReason()
	{
		if (CampaignOptions.IsLifeDeathCycleDisabled)
		{
			TextObject variable = GameTexts.FindText("str_campaign_options_type", "IsLifeDeathCycleEnabled");
			TextObject textObject = GameTexts.FindText("str_campaign_options_dependency_warning");
			textObject.SetTextVariable("OPTION", variable);
			if (!CampaignOptionsManager.GetOptionWithIdExists("IsLifeDeathCycleEnabled"))
			{
				string variable2 = textObject.ToString();
				TextObject textObject2 = new TextObject("{=K87pubLc}The option \"{DEPENDENT_OPTION}\" can be enabled by activating \"{MODULE_NAME}\" module.");
				textObject2.SetTextVariable("DEPENDENT_OPTION", variable);
				textObject2.SetTextVariable("MODULE_NAME", "Birth and Death Options");
				textObject = GameTexts.FindText("str_string_newline_string").CopyTextObject();
				textObject.SetTextVariable("STR1", variable2);
				textObject.SetTextVariable("STR2", textObject2.ToString());
			}
			return new CampaignOptionDisableStatus(isDisabled: true, textObject.ToString(), 0f);
		}
		return new CampaignOptionDisableStatus(isDisabled: false, string.Empty, 0f);
	}

	private List<TextObject> GetPresetTexts(string identifier)
	{
		List<TextObject> list = new List<TextObject>();
		foreach (object value in Enum.GetValues(typeof(CampaignOptionsDifficultyPresets)))
		{
			list.Add(GameTexts.FindText("str_campaign_options_type_" + identifier, value.ToString()));
		}
		return list;
	}

	private void ExecuteResetTutorial()
	{
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=a4GDfSel}Reset Tutorials").ToString(), new TextObject("{=I2sZ7K28}Are you sure want to reset tutorials?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ResetTutorials, null));
	}

	private void ExecuteEnableCheats()
	{
		if (GameStateManager.Current.ActiveState is MapState mapState)
		{
			mapState.Handler.OnGameplayCheatsEnabled();
		}
	}

	private void ResetTutorials()
	{
		Game.Current.EventManager.TriggerEvent(new ResetAllTutorialsEvent());
		InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=Iefr8Fra}Tutorials have been reset.").ToString()));
	}
}
