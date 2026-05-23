using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200000F RID: 15
	public class DefaultCampaignOptionsProvider : ICampaignOptionProvider
	{
		// Token: 0x060000B1 RID: 177 RVA: 0x00003FAE File Offset: 0x000021AE
		public IEnumerable<ICampaignOptionData> GetGameplayCampaignOptions()
		{
			yield return new SelectionCampaignOptionData("DifficultyPresets", 100, CampaignOptionEnableState.Enabled, () => (float)this._difficultyPreset, delegate(float value)
			{
				this._difficultyPreset = (CampaignOptionsDifficultyPresets)value;
			}, this.GetPresetTexts("DifficultyPresets"), null, false, null, null);
			IEnumerable<ICampaignOptionData> difficultyRelatedOptions = this.GetDifficultyRelatedOptions();
			foreach (ICampaignOptionData campaignOptionData in difficultyRelatedOptions)
			{
				yield return campaignOptionData;
			}
			IEnumerator<ICampaignOptionData> enumerator = null;
			yield return new BooleanCampaignOptionData("AutoAllocateClanMemberPerks", 1000, CampaignOptionEnableState.Enabled, delegate()
			{
				if (!CampaignOptions.AutoAllocateClanMemberPerks)
				{
					return 0f;
				}
				return 1f;
			}, delegate(float value)
			{
				CampaignOptions.AutoAllocateClanMemberPerks = value == 1f;
			}, null, false, null, null);
			yield return new BooleanCampaignOptionData("IronmanMode", 1100, CampaignOptionEnableState.Disabled, delegate()
			{
				if (!CampaignOptions.IsIronmanMode)
				{
					return 0f;
				}
				return 1f;
			}, delegate(float value)
			{
				CampaignOptions.IsIronmanMode = value == 1f;
			}, null, false, null, null);
			yield return new ActionCampaignOptionData("ResetTutorial", 10000, CampaignOptionEnableState.Enabled, new Action(this.ExecuteResetTutorial), null);
			if (Input.IsGamepadActive)
			{
				yield return new ActionCampaignOptionData("EnableCheats", 11000, CampaignOptionEnableState.Enabled, new Action(this.ExecuteEnableCheats), null);
			}
			yield break;
			yield break;
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00003FBE File Offset: 0x000021BE
		public IEnumerable<ICampaignOptionData> GetCharacterCreationCampaignOptions()
		{
			IEnumerable<ICampaignOptionData> difficultyOptions = this.GetDifficultyRelatedOptions();
			yield return new SelectionCampaignOptionData("DifficultyPresets", 100, CampaignOptionEnableState.Enabled, () => (float)this._difficultyPreset, delegate(float value)
			{
				this._difficultyPreset = (CampaignOptionsDifficultyPresets)value;
			}, this.GetPresetTexts("DifficultyPresets"), null, false, null, null);
			foreach (ICampaignOptionData campaignOptionData in difficultyOptions)
			{
				yield return campaignOptionData;
			}
			IEnumerator<ICampaignOptionData> enumerator = null;
			yield return new BooleanCampaignOptionData("AutoAllocateClanMemberPerks", 1000, CampaignOptionEnableState.Enabled, delegate()
			{
				if (!CampaignOptions.AutoAllocateClanMemberPerks)
				{
					return 0f;
				}
				return 1f;
			}, delegate(float value)
			{
				CampaignOptions.AutoAllocateClanMemberPerks = value == 1f;
			}, null, false, null, null);
			if (MBSaveLoad.IsMaxNumberOfSavesReached())
			{
				yield return new BooleanCampaignOptionData("IronmanMode", 1100, CampaignOptionEnableState.Disabled, null, null, new Func<CampaignOptionDisableStatus>(this.GetIronmanModeDisabledWithReason), false, null, null);
			}
			else
			{
				yield return new BooleanCampaignOptionData("IronmanMode", 1100, CampaignOptionEnableState.DisabledLater, delegate()
				{
					if (!CampaignOptions.IsIronmanMode)
					{
						return 0f;
					}
					return 1f;
				}, delegate(float value)
				{
					CampaignOptions.IsIronmanMode = value == 1f;
				}, null, false, null, null);
			}
			yield break;
			yield break;
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00003FCE File Offset: 0x000021CE
		private IEnumerable<ICampaignOptionData> GetDifficultyRelatedOptions()
		{
			yield return new SelectionCampaignOptionData("PlayerTroopsReceivedDamage", 300, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.PlayerTroopsReceivedDamage, delegate(float value)
			{
				CampaignOptions.PlayerTroopsReceivedDamage = (CampaignOptions.Difficulty)value;
			}, null, null, true, null, null);
			yield return new SelectionCampaignOptionData("MaximumIndexPlayerCanRecruit", 400, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.RecruitmentDifficulty, delegate(float value)
			{
				CampaignOptions.RecruitmentDifficulty = (CampaignOptions.Difficulty)value;
			}, null, null, true, null, null);
			yield return new SelectionCampaignOptionData("PlayerMapMovementSpeed", 500, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.PlayerMapMovementSpeed, delegate(float value)
			{
				CampaignOptions.PlayerMapMovementSpeed = (CampaignOptions.Difficulty)value;
			}, null, null, true, null, null);
			yield return new SelectionCampaignOptionData("PersuasionSuccess", 600, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.PersuasionSuccessChance, delegate(float value)
			{
				CampaignOptions.PersuasionSuccessChance = (CampaignOptions.Difficulty)value;
			}, null, null, true, null, null);
			yield return new SelectionCampaignOptionData("CombatAIDifficulty", 700, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.CombatAIDifficulty, delegate(float value)
			{
				CampaignOptions.CombatAIDifficulty = (CampaignOptions.Difficulty)value;
			}, null, null, true, null, null);
			yield return new SelectionCampaignOptionData("ClanMemberBattleDeathPossibility", 800, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.ClanMemberDeathChance, delegate(float value)
			{
				CampaignOptions.ClanMemberDeathChance = (CampaignOptions.Difficulty)value;
			}, null, null, true, null, null);
			yield return new SelectionCampaignOptionData("BattleDeath", 900, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.BattleDeath, delegate(float value)
			{
				CampaignOptions.BattleDeath = (CampaignOptions.Difficulty)value;
			}, null, new Func<CampaignOptionDisableStatus>(this.GetBattleDeathDisabledStatusWithReason), true, null, null);
			yield return new SelectionCampaignOptionData("StealthAndDisguiseDifficulty", 1000, CampaignOptionEnableState.Enabled, () => (float)CampaignOptions.StealthAndDisguiseDifficulty, delegate(float value)
			{
				CampaignOptions.StealthAndDisguiseDifficulty = (CampaignOptions.Difficulty)value;
			}, null, null, true, new Func<float, CampaignOptionsDifficultyPresets>(this.GetDisguiseAndStealthDefaultPresetForValue), new Func<CampaignOptionsDifficultyPresets, float>(this.GetDisguiseAndStealthDefaultValueForDifficultyPreset));
			yield break;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00003FE0 File Offset: 0x000021E0
		private CampaignOptionsDifficultyPresets GetDisguiseAndStealthDefaultPresetForValue(float value)
		{
			int num = (int)value;
			if (num == 0)
			{
				return CampaignOptionsDifficultyPresets.Freebooter;
			}
			if (num - 1 > 1)
			{
				return CampaignOptionsDifficultyPresets.Custom;
			}
			return CampaignOptionsDifficultyPresets.Warrior;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00004000 File Offset: 0x00002200
		private float GetDisguiseAndStealthDefaultValueForDifficultyPreset(CampaignOptionsDifficultyPresets preset)
		{
			if (preset == CampaignOptionsDifficultyPresets.Freebooter)
			{
				return 0f;
			}
			if (preset - CampaignOptionsDifficultyPresets.Warrior > 1)
			{
				return 0f;
			}
			return 1f;
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00004020 File Offset: 0x00002220
		private CampaignOptionDisableStatus GetIronmanModeDisabledWithReason()
		{
			if (!MBSaveLoad.IsMaxNumberOfSavesReached())
			{
				return new CampaignOptionDisableStatus(false, string.Empty, -1f);
			}
			TextObject textObject = GameTexts.FindText("str_string_newline_string", null).SetTextVariable("STR1", string.Empty).SetTextVariable("STR2", new TextObject("{=ld0SelSH}Ironman mode requires at least one available save slot.", null))
				.SetTextVariable("newline", "\n");
			return new CampaignOptionDisableStatus(true, textObject.ToString(), 0f);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00004098 File Offset: 0x00002298
		private CampaignOptionDisableStatus GetBattleDeathDisabledStatusWithReason()
		{
			if (CampaignOptions.IsLifeDeathCycleDisabled)
			{
				TextObject variable = GameTexts.FindText("str_campaign_options_type", "IsLifeDeathCycleEnabled");
				TextObject textObject = GameTexts.FindText("str_campaign_options_dependency_warning", null);
				textObject.SetTextVariable("OPTION", variable);
				if (!CampaignOptionsManager.GetOptionWithIdExists("IsLifeDeathCycleEnabled"))
				{
					string variable2 = textObject.ToString();
					TextObject textObject2 = new TextObject("{=K87pubLc}The option \"{DEPENDENT_OPTION}\" can be enabled by activating \"{MODULE_NAME}\" module.", null);
					textObject2.SetTextVariable("DEPENDENT_OPTION", variable);
					textObject2.SetTextVariable("MODULE_NAME", "Birth and Death Options");
					textObject = GameTexts.FindText("str_string_newline_string", null).CopyTextObject();
					textObject.SetTextVariable("STR1", variable2);
					textObject.SetTextVariable("STR2", textObject2.ToString());
				}
				return new CampaignOptionDisableStatus(true, textObject.ToString(), 0f);
			}
			return new CampaignOptionDisableStatus(false, string.Empty, 0f);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00004168 File Offset: 0x00002368
		private List<TextObject> GetPresetTexts(string identifier)
		{
			List<TextObject> list = new List<TextObject>();
			foreach (object obj in Enum.GetValues(typeof(CampaignOptionsDifficultyPresets)))
			{
				list.Add(GameTexts.FindText("str_campaign_options_type_" + identifier, obj.ToString()));
			}
			return list;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x000041E4 File Offset: 0x000023E4
		private void ExecuteResetTutorial()
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=a4GDfSel}Reset Tutorials", null).ToString(), new TextObject("{=I2sZ7K28}Are you sure want to reset tutorials?", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.ResetTutorials), null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x060000BA RID: 186 RVA: 0x0000425C File Offset: 0x0000245C
		private void ExecuteEnableCheats()
		{
			MapState mapState;
			if ((mapState = GameStateManager.Current.ActiveState as MapState) != null)
			{
				mapState.Handler.OnGameplayCheatsEnabled();
			}
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00004287 File Offset: 0x00002487
		private void ResetTutorials()
		{
			Game.Current.EventManager.TriggerEvent<ResetAllTutorialsEvent>(new ResetAllTutorialsEvent());
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=Iefr8Fra}Tutorials have been reset.", null).ToString()));
		}

		// Token: 0x04000060 RID: 96
		private CampaignOptionsDifficultyPresets _difficultyPreset;
	}
}
