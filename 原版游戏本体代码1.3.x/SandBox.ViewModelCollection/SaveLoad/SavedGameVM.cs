using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.ScreenSystem;

namespace SandBox.ViewModelCollection.SaveLoad
{
	// Token: 0x02000016 RID: 22
	public class SavedGameVM : ViewModel
	{
		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x000083C6 File Offset: 0x000065C6
		public SaveGameFileInfo Save { get; }

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060001B4 RID: 436 RVA: 0x000083CE File Offset: 0x000065CE
		// (set) Token: 0x060001B5 RID: 437 RVA: 0x000083D6 File Offset: 0x000065D6
		public bool RequiresInquiryOnLoad { get; private set; }

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060001B6 RID: 438 RVA: 0x000083DF File Offset: 0x000065DF
		// (set) Token: 0x060001B7 RID: 439 RVA: 0x000083E7 File Offset: 0x000065E7
		public bool IsModuleDiscrepancyDetected { get; private set; }

		// Token: 0x060001B8 RID: 440 RVA: 0x000083F0 File Offset: 0x000065F0
		public SavedGameVM(SaveGameFileInfo save, bool isSaving, Action<SavedGameVM> onDelete, Action<SavedGameVM> onSelection, Action onCancelLoadSave, Action onDone, bool isCorruptedSave = false, bool isIronman = false)
		{
			this.Save = save;
			this._isSaving = isSaving;
			this._onDelete = onDelete;
			this._onSelection = onSelection;
			this._onCancelLoadSave = onCancelLoadSave;
			this._onDone = onDone;
			this.IsCorrupted = isCorruptedSave;
			this.SavedGameProperties = new MBBindingList<SavedGamePropertyVM>();
			this.LoadedModulesInSave = new MBBindingList<SavedGameModuleInfoVM>();
			if (isIronman)
			{
				GameTexts.SetVariable("RANK", this.Save.MetaData.GetCharacterName());
				GameTexts.SetVariable("NUMBER", new TextObject("{=Fm0rjkH7}Ironman", null));
				this.NameText = new TextObject("{=AVoWvlue}{RANK} ({NUMBER})", null).ToString();
			}
			else
			{
				this.NameText = this.Save.Name;
			}
			this._newlineTextObject.SetTextVariable("newline", "\n");
			this._gameVersion = MBSaveLoad.CurrentVersion;
			this._saveVersion = this.Save.MetaData.GetApplicationVersion();
			this._moduleCheckResults = SandBoxSaveHelper.CheckMetaDataCompatibilityErrors(save.MetaData);
			bool isModuleDiscrepancyDetected;
			if (!isCorruptedSave)
			{
				isModuleDiscrepancyDetected = this._moduleCheckResults.Any((SandBoxSaveHelper.ModuleCheckResult x) => x.Type != ModuleCheckResultType.VersionMismatch);
			}
			else
			{
				isModuleDiscrepancyDetected = true;
			}
			this.IsModuleDiscrepancyDetected = isModuleDiscrepancyDetected;
			this.MainHeroVisualCode = (this.IsModuleDiscrepancyDetected ? string.Empty : this.Save.MetaData.GetCharacterVisualCode());
			this.BannerTextCode = (this.IsModuleDiscrepancyDetected ? string.Empty : this.Save.MetaData.GetClanBannerCode());
			TextObject hintText;
			this.IsDisabled = SandBoxSaveHelper.GetIsDisabledWithReason(this.Save, out hintText);
			this.DisabledReasonHint = new HintViewModel(hintText, null);
			this.RefreshValues();
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x000085AC File Offset: 0x000067AC
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.LoadedModulesInSave.Clear();
			this.SavedGameProperties.Clear();
			this.SaveVersionAsString = this._saveVersion.ToString();
			if (this._gameVersion != this._saveVersion)
			{
				this.RequiresInquiryOnLoad = true;
			}
			foreach (string text in this.Save.MetaData.GetModules())
			{
				string value = this.Save.MetaData.GetModuleVersion(text).ToString();
				this.LoadedModulesInSave.Add(new SavedGameModuleInfoVM(SandBoxSaveHelper.GetModuleNameFromModuleId(text), "", value));
			}
			this.CharacterNameText = this.Save.MetaData.GetCharacterName();
			this.ClanBanner = new BannerImageIdentifierVM(new Banner(this.Save.MetaData.GetClanBannerCode()), true);
			this.DeleteText = new TextObject("{=deleteaction}Delete", null).ToString();
			this.ModulesText = new TextObject("{=JXyxj1J5}Modules", null).ToString();
			DateTime creationTime = this.Save.MetaData.GetCreationTime();
			this.RealTimeText1 = LocalizedTextManager.GetDateFormattedByLanguage(BannerlordConfig.Language, creationTime);
			this.RealTimeText2 = LocalizedTextManager.GetTimeFormattedByLanguage(BannerlordConfig.Language, creationTime);
			int playerHealthPercentage = this.Save.MetaData.GetPlayerHealthPercentage();
			TextObject textObject = new TextObject("{=gYATKZJp}{NUMBER}%", null);
			textObject.SetTextVariable("NUMBER", playerHealthPercentage.ToString());
			this.SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Health, textObject, new TextObject("{=hZrwUIaq}Health", null)));
			int mainHeroGold = this.Save.MetaData.GetMainHeroGold();
			this.SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Gold, SavedGameVM.GetAbbreviatedValueTextFromValue(mainHeroGold), new TextObject("{=Hxf6bzmR}Current Denars", null)));
			int valueAmount = (int)this.Save.MetaData.GetClanInfluence();
			this.SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Influence, SavedGameVM.GetAbbreviatedValueTextFromValue(valueAmount), new TextObject("{=RVPidk5a}Influence", null)));
			int num = this.Save.MetaData.GetMainPartyHealthyMemberCount() + this.Save.MetaData.GetMainPartyWoundedMemberCount();
			int mainPartyPrisonerMemberCount = this.Save.MetaData.GetMainPartyPrisonerMemberCount();
			TextObject textObject2;
			if (mainPartyPrisonerMemberCount > 0)
			{
				textObject2 = new TextObject("{=6qYaQkDD}{COUNT} + {PRISONER_COUNT}p", null);
				textObject2.SetTextVariable("COUNT", num);
				textObject2.SetTextVariable("PRISONER_COUNT", mainPartyPrisonerMemberCount);
			}
			else
			{
				textObject2 = new TextObject(num, null);
			}
			this.SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.PartySize, textObject2, new TextObject("{=IXwOaa98}Party Size", null)));
			int value2 = (int)this.Save.MetaData.GetMainPartyFood();
			this.SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Food, new TextObject(value2, null), new TextObject("{=qSi4DlT4}Food", null)));
			int clanFiefs = this.Save.MetaData.GetClanFiefs();
			this.SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Fiefs, new TextObject(clanFiefs, null), new TextObject("{=SRjrhb0A}Owned Fief Count", null)));
			TextObject textObject3 = new TextObject("{=GZWPHmAw}Day : {DAY}", null);
			string variable = ((int)this.Save.MetaData.GetDayLong()).ToString();
			textObject3.SetTextVariable("DAY", variable);
			this.GameTimeText = textObject3.ToString();
			TextObject textObject4 = new TextObject("{=IwhpeT8C}Level : {PLAYER_LEVEL}", null);
			textObject4.SetTextVariable("PLAYER_LEVEL", this.Save.MetaData.GetMainHeroLevel().ToString());
			this.LevelText = textObject4.ToString();
			this.DateTimeHint = new HintViewModel(new TextObject("{=!}" + this.RealTimeText1, null), null);
			this.UpdateButtonHint = new HintViewModel(new TextObject("{=ZDPIq4hi}Load the selected save game, overwrite it with the current version of the game and get back to this screen.", null), null);
			this.SaveLoadText = (this._isSaving ? new TextObject("{=bV75iwKa}Save", null).ToString() : new TextObject("{=9NuttOBC}Load", null).ToString());
			this.OverrideSaveText = new TextObject("{=hYL3CFHX}Do you want to overwrite this saved game?", null).ToString();
			this.UpdateSaveText = new TextObject("{=FFiPLPbs}Update Save", null).ToString();
			this.CorruptedSaveText = new TextObject("{=RoYPofhK}Corrupted Save", null).ToString();
		}

		// Token: 0x060001BA RID: 442 RVA: 0x000089F0 File Offset: 0x00006BF0
		public void ExecuteSaveLoad()
		{
			if (!this.IsCorrupted && !this.IsDisabled)
			{
				if (this._isSaving)
				{
					InformationManager.ShowInquiry(new InquiryData(new TextObject("{=Q1HIlJxe}Overwrite", null).ToString(), this.OverrideSaveText, true, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=8OkPHu4f}No", null).ToString(), new Action(this.OnOverrideSaveAccept), delegate()
					{
						Action onCancelLoadSave = this._onCancelLoadSave;
						if (onCancelLoadSave == null)
						{
							return;
						}
						onCancelLoadSave();
					}, "", 0f, null, null, null), false, false);
					return;
				}
				SandBoxSaveHelper.TryLoadSave(this.Save, new Action<LoadResult>(this.StartGame), this._onCancelLoadSave);
			}
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00008AA2 File Offset: 0x00006CA2
		private void StartGame(LoadResult loadResult)
		{
			if (Game.Current != null)
			{
				ScreenManager.PopScreen();
				GameStateManager.Current.CleanStates(0);
				GameStateManager.Current = Module.CurrentModule.GlobalGameStateManager;
			}
			MBSaveLoad.OnStartGame(loadResult);
			MBGameManager.StartNewGame(new SandBoxGameManager(loadResult));
		}

		// Token: 0x060001BC RID: 444 RVA: 0x00008ADB File Offset: 0x00006CDB
		private void OnOverrideSaveAccept()
		{
			Campaign.Current.SaveHandler.SaveAs(this.Save.Name);
			this._onDone();
		}

		// Token: 0x060001BD RID: 445 RVA: 0x00008B04 File Offset: 0x00006D04
		private static TextObject GetAbbreviatedValueTextFromValue(int valueAmount)
		{
			string variable = "";
			decimal num = valueAmount;
			if (valueAmount < 10000)
			{
				return new TextObject(valueAmount, null);
			}
			if (valueAmount >= 10000 && valueAmount < 1000000)
			{
				variable = new TextObject("{=thousandabbr}k", null).ToString();
				num /= 1000m;
			}
			else if (valueAmount >= 1000000 && valueAmount < 1000000000)
			{
				variable = new TextObject("{=millionabbr}m", null).ToString();
				num /= 1000000m;
			}
			else if (valueAmount >= 1000000000 && valueAmount <= 2147483647)
			{
				variable = new TextObject("{=billionabbr}b", null).ToString();
				num /= 1000000000m;
			}
			int num2 = (int)num;
			string text = num2.ToString();
			if (text.Length < 3)
			{
				text += ".";
				string text2 = num.ToString("F3").Split(new char[] { '.' }).ElementAtOrDefault(1);
				if (text2 != null)
				{
					for (int i = 0; i < 3 - num2.ToString().Length; i++)
					{
						if (text2.ElementAtOrDefault(i) != '\0')
						{
							text += text2.ElementAtOrDefault(i).ToString();
						}
					}
				}
			}
			TextObject textObject = new TextObject("{=mapbardenarvalue}{DENAR_AMOUNT}{VALUE_ABBREVIATION}", null);
			textObject.SetTextVariable("DENAR_AMOUNT", text);
			textObject.SetTextVariable("VALUE_ABBREVIATION", variable);
			return textObject;
		}

		// Token: 0x060001BE RID: 446 RVA: 0x00008C7A File Offset: 0x00006E7A
		public void ExecuteUpdate()
		{
		}

		// Token: 0x060001BF RID: 447 RVA: 0x00008C7C File Offset: 0x00006E7C
		public void ExecuteDelete()
		{
			this._onDelete(this);
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x00008C8A File Offset: 0x00006E8A
		public void ExecuteSelection()
		{
			this._onSelection(this);
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060001C1 RID: 449 RVA: 0x00008C98 File Offset: 0x00006E98
		// (set) Token: 0x060001C2 RID: 450 RVA: 0x00008CA0 File Offset: 0x00006EA0
		[DataSourceProperty]
		public MBBindingList<SavedGamePropertyVM> SavedGameProperties
		{
			get
			{
				return this._savedGameProperties;
			}
			set
			{
				if (value != this._savedGameProperties)
				{
					this._savedGameProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<SavedGamePropertyVM>>(value, "SavedGameProperties");
				}
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060001C3 RID: 451 RVA: 0x00008CBE File Offset: 0x00006EBE
		// (set) Token: 0x060001C4 RID: 452 RVA: 0x00008CC6 File Offset: 0x00006EC6
		[DataSourceProperty]
		public MBBindingList<SavedGameModuleInfoVM> LoadedModulesInSave
		{
			get
			{
				return this._loadedModulesInSave;
			}
			set
			{
				if (value != this._loadedModulesInSave)
				{
					this._loadedModulesInSave = value;
					base.OnPropertyChangedWithValue<MBBindingList<SavedGameModuleInfoVM>>(value, "LoadedModulesInSave");
				}
			}
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060001C5 RID: 453 RVA: 0x00008CE4 File Offset: 0x00006EE4
		// (set) Token: 0x060001C6 RID: 454 RVA: 0x00008CEC File Offset: 0x00006EEC
		[DataSourceProperty]
		public string SaveVersionAsString
		{
			get
			{
				return this._saveVersionAsString;
			}
			set
			{
				if (value != this._saveVersionAsString)
				{
					this._saveVersionAsString = value;
					base.OnPropertyChangedWithValue<string>(value, "SaveVersionAsString");
				}
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060001C7 RID: 455 RVA: 0x00008D0F File Offset: 0x00006F0F
		// (set) Token: 0x060001C8 RID: 456 RVA: 0x00008D17 File Offset: 0x00006F17
		[DataSourceProperty]
		public string DeleteText
		{
			get
			{
				return this._deleteText;
			}
			set
			{
				if (value != this._deleteText)
				{
					this._deleteText = value;
					base.OnPropertyChangedWithValue<string>(value, "DeleteText");
				}
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x00008D3A File Offset: 0x00006F3A
		// (set) Token: 0x060001CA RID: 458 RVA: 0x00008D42 File Offset: 0x00006F42
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060001CB RID: 459 RVA: 0x00008D60 File Offset: 0x00006F60
		// (set) Token: 0x060001CC RID: 460 RVA: 0x00008D68 File Offset: 0x00006F68
		[DataSourceProperty]
		public bool IsCorrupted
		{
			get
			{
				return this._isCorrupted;
			}
			set
			{
				if (value != this._isCorrupted)
				{
					this._isCorrupted = value;
					base.OnPropertyChangedWithValue(value, "IsCorrupted");
				}
			}
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060001CD RID: 461 RVA: 0x00008D86 File Offset: 0x00006F86
		// (set) Token: 0x060001CE RID: 462 RVA: 0x00008D8E File Offset: 0x00006F8E
		[DataSourceProperty]
		public string BannerTextCode
		{
			get
			{
				return this._bannerTextCode;
			}
			set
			{
				if (value != this._bannerTextCode)
				{
					this._bannerTextCode = value;
					base.OnPropertyChangedWithValue<string>(value, "BannerTextCode");
				}
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060001CF RID: 463 RVA: 0x00008DB1 File Offset: 0x00006FB1
		// (set) Token: 0x060001D0 RID: 464 RVA: 0x00008DB9 File Offset: 0x00006FB9
		[DataSourceProperty]
		public string SaveLoadText
		{
			get
			{
				return this._saveLoadText;
			}
			set
			{
				if (value != this._saveLoadText)
				{
					this._saveLoadText = value;
					base.OnPropertyChangedWithValue<string>(value, "SaveLoadText");
				}
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060001D1 RID: 465 RVA: 0x00008DDC File Offset: 0x00006FDC
		// (set) Token: 0x060001D2 RID: 466 RVA: 0x00008DE4 File Offset: 0x00006FE4
		[DataSourceProperty]
		public string OverrideSaveText
		{
			get
			{
				return this._overwriteSaveText;
			}
			set
			{
				if (value != this._overwriteSaveText)
				{
					this._overwriteSaveText = value;
					base.OnPropertyChangedWithValue<string>(value, "OverrideSaveText");
				}
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060001D3 RID: 467 RVA: 0x00008E07 File Offset: 0x00007007
		// (set) Token: 0x060001D4 RID: 468 RVA: 0x00008E0F File Offset: 0x0000700F
		[DataSourceProperty]
		public string UpdateSaveText
		{
			get
			{
				return this._updateSaveText;
			}
			set
			{
				if (value != this._updateSaveText)
				{
					this._updateSaveText = value;
					base.OnPropertyChangedWithValue<string>(value, "UpdateSaveText");
				}
			}
		}

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x00008E32 File Offset: 0x00007032
		// (set) Token: 0x060001D6 RID: 470 RVA: 0x00008E3A File Offset: 0x0000703A
		[DataSourceProperty]
		public string ModulesText
		{
			get
			{
				return this._modulesText;
			}
			set
			{
				if (value != this._modulesText)
				{
					this._modulesText = value;
					base.OnPropertyChangedWithValue<string>(value, "ModulesText");
				}
			}
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060001D7 RID: 471 RVA: 0x00008E5D File Offset: 0x0000705D
		// (set) Token: 0x060001D8 RID: 472 RVA: 0x00008E65 File Offset: 0x00007065
		[DataSourceProperty]
		public string CorruptedSaveText
		{
			get
			{
				return this._corruptedSaveText;
			}
			set
			{
				if (value != this._corruptedSaveText)
				{
					this._corruptedSaveText = value;
					base.OnPropertyChangedWithValue<string>(value, "CorruptedSaveText");
				}
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060001D9 RID: 473 RVA: 0x00008E88 File Offset: 0x00007088
		// (set) Token: 0x060001DA RID: 474 RVA: 0x00008E90 File Offset: 0x00007090
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060001DB RID: 475 RVA: 0x00008EB3 File Offset: 0x000070B3
		// (set) Token: 0x060001DC RID: 476 RVA: 0x00008EBB File Offset: 0x000070BB
		[DataSourceProperty]
		public string GameTimeText
		{
			get
			{
				return this._gameTimeText;
			}
			set
			{
				if (value != this._gameTimeText)
				{
					this._gameTimeText = value;
					base.OnPropertyChangedWithValue<string>(value, "GameTimeText");
				}
			}
		}

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060001DD RID: 477 RVA: 0x00008EDE File Offset: 0x000070DE
		// (set) Token: 0x060001DE RID: 478 RVA: 0x00008EE6 File Offset: 0x000070E6
		[DataSourceProperty]
		public string CharacterNameText
		{
			get
			{
				return this._characterNameText;
			}
			set
			{
				if (value != this._characterNameText)
				{
					this._characterNameText = value;
					base.OnPropertyChangedWithValue<string>(value, "CharacterNameText");
				}
			}
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060001DF RID: 479 RVA: 0x00008F09 File Offset: 0x00007109
		// (set) Token: 0x060001E0 RID: 480 RVA: 0x00008F11 File Offset: 0x00007111
		[DataSourceProperty]
		public string MainHeroVisualCode
		{
			get
			{
				return this._mainHeroVisualCode;
			}
			set
			{
				if (value != this._mainHeroVisualCode)
				{
					this._mainHeroVisualCode = value;
					base.OnPropertyChangedWithValue<string>(value, "MainHeroVisualCode");
				}
			}
		}

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060001E1 RID: 481 RVA: 0x00008F34 File Offset: 0x00007134
		// (set) Token: 0x060001E2 RID: 482 RVA: 0x00008F3C File Offset: 0x0000713C
		[DataSourceProperty]
		public CharacterViewModel CharacterVisual
		{
			get
			{
				return this._characterVisual;
			}
			set
			{
				if (value != this._characterVisual)
				{
					this._characterVisual = value;
					base.OnPropertyChangedWithValue<CharacterViewModel>(value, "CharacterVisual");
				}
			}
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060001E3 RID: 483 RVA: 0x00008F5A File Offset: 0x0000715A
		// (set) Token: 0x060001E4 RID: 484 RVA: 0x00008F62 File Offset: 0x00007162
		[DataSourceProperty]
		public BannerImageIdentifierVM ClanBanner
		{
			get
			{
				return this._clanBanner;
			}
			set
			{
				if (value != this._clanBanner)
				{
					this._clanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ClanBanner");
				}
			}
		}

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060001E5 RID: 485 RVA: 0x00008F80 File Offset: 0x00007180
		// (set) Token: 0x060001E6 RID: 486 RVA: 0x00008F88 File Offset: 0x00007188
		[DataSourceProperty]
		public string RealTimeText1
		{
			get
			{
				return this._realTimeText1;
			}
			set
			{
				if (value != this._realTimeText1)
				{
					this._realTimeText1 = value;
					base.OnPropertyChangedWithValue<string>(value, "RealTimeText1");
				}
			}
		}

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x060001E7 RID: 487 RVA: 0x00008FAB File Offset: 0x000071AB
		// (set) Token: 0x060001E8 RID: 488 RVA: 0x00008FB3 File Offset: 0x000071B3
		[DataSourceProperty]
		public string RealTimeText2
		{
			get
			{
				return this._realTimeText2;
			}
			set
			{
				if (value != this._realTimeText2)
				{
					this._realTimeText2 = value;
					base.OnPropertyChangedWithValue<string>(value, "RealTimeText2");
				}
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x060001E9 RID: 489 RVA: 0x00008FD6 File Offset: 0x000071D6
		// (set) Token: 0x060001EA RID: 490 RVA: 0x00008FDE File Offset: 0x000071DE
		[DataSourceProperty]
		public string LevelText
		{
			get
			{
				return this._levelText;
			}
			set
			{
				if (value != this._levelText)
				{
					this._levelText = value;
					base.OnPropertyChangedWithValue<string>(value, "LevelText");
				}
			}
		}

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x060001EB RID: 491 RVA: 0x00009001 File Offset: 0x00007201
		// (set) Token: 0x060001EC RID: 492 RVA: 0x00009009 File Offset: 0x00007209
		[DataSourceProperty]
		public HintViewModel DateTimeHint
		{
			get
			{
				return this._dateTimeHint;
			}
			set
			{
				if (value != this._dateTimeHint)
				{
					this._dateTimeHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DateTimeHint");
				}
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x060001ED RID: 493 RVA: 0x00009027 File Offset: 0x00007227
		// (set) Token: 0x060001EE RID: 494 RVA: 0x0000902F File Offset: 0x0000722F
		[DataSourceProperty]
		public HintViewModel UpdateButtonHint
		{
			get
			{
				return this._updateButtonHint;
			}
			set
			{
				if (value != this._updateButtonHint)
				{
					this._updateButtonHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "UpdateButtonHint");
				}
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x060001EF RID: 495 RVA: 0x0000904D File Offset: 0x0000724D
		// (set) Token: 0x060001F0 RID: 496 RVA: 0x00009055 File Offset: 0x00007255
		[DataSourceProperty]
		public HintViewModel DisabledReasonHint
		{
			get
			{
				return this._disabledReasonHint;
			}
			set
			{
				if (value != this._disabledReasonHint)
				{
					this._disabledReasonHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DisabledReasonHint");
				}
			}
		}

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x060001F1 RID: 497 RVA: 0x00009073 File Offset: 0x00007273
		// (set) Token: 0x060001F2 RID: 498 RVA: 0x0000907B File Offset: 0x0000727B
		[DataSourceProperty]
		public bool IsFilteredOut
		{
			get
			{
				return this._isFilteredOut;
			}
			set
			{
				if (value != this._isFilteredOut)
				{
					this._isFilteredOut = value;
					base.OnPropertyChangedWithValue(value, "IsFilteredOut");
				}
			}
		}

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x060001F3 RID: 499 RVA: 0x00009099 File Offset: 0x00007299
		// (set) Token: 0x060001F4 RID: 500 RVA: 0x000090A1 File Offset: 0x000072A1
		[DataSourceProperty]
		public bool IsDisabled
		{
			get
			{
				return this._isDisabled;
			}
			set
			{
				if (value != this._isDisabled)
				{
					this._isDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsDisabled");
				}
			}
		}

		// Token: 0x040000C0 RID: 192
		private readonly bool _isSaving;

		// Token: 0x040000C1 RID: 193
		private readonly Action _onDone;

		// Token: 0x040000C2 RID: 194
		private readonly Action<SavedGameVM> _onDelete;

		// Token: 0x040000C3 RID: 195
		private readonly Action<SavedGameVM> _onSelection;

		// Token: 0x040000C4 RID: 196
		private readonly Action _onCancelLoadSave;

		// Token: 0x040000C5 RID: 197
		private readonly TextObject _newlineTextObject = new TextObject("{=ol0rBSrb}{STR1}{newline}{STR2}", null);

		// Token: 0x040000C6 RID: 198
		private readonly ApplicationVersion _gameVersion;

		// Token: 0x040000C7 RID: 199
		private readonly ApplicationVersion _saveVersion;

		// Token: 0x040000C8 RID: 200
		private readonly MBReadOnlyList<SandBoxSaveHelper.ModuleCheckResult> _moduleCheckResults;

		// Token: 0x040000C9 RID: 201
		private MBBindingList<SavedGamePropertyVM> _savedGameProperties;

		// Token: 0x040000CA RID: 202
		private MBBindingList<SavedGameModuleInfoVM> _loadedModulesInSave;

		// Token: 0x040000CB RID: 203
		private HintViewModel _dateTimeHint;

		// Token: 0x040000CC RID: 204
		private HintViewModel _updateButtonHint;

		// Token: 0x040000CD RID: 205
		private HintViewModel _disabledReasonHint;

		// Token: 0x040000CE RID: 206
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x040000CF RID: 207
		private CharacterViewModel _characterVisual;

		// Token: 0x040000D0 RID: 208
		private string _deleteText;

		// Token: 0x040000D1 RID: 209
		private string _nameText;

		// Token: 0x040000D2 RID: 210
		private string _gameTimeText;

		// Token: 0x040000D3 RID: 211
		private string _realTimeText1;

		// Token: 0x040000D4 RID: 212
		private string _realTimeText2;

		// Token: 0x040000D5 RID: 213
		private string _levelText;

		// Token: 0x040000D6 RID: 214
		private string _characterNameText;

		// Token: 0x040000D7 RID: 215
		private string _saveLoadText;

		// Token: 0x040000D8 RID: 216
		private string _overwriteSaveText;

		// Token: 0x040000D9 RID: 217
		private string _updateSaveText;

		// Token: 0x040000DA RID: 218
		private string _modulesText;

		// Token: 0x040000DB RID: 219
		private string _corruptedSaveText;

		// Token: 0x040000DC RID: 220
		private string _saveVersionAsString;

		// Token: 0x040000DD RID: 221
		private string _mainHeroVisualCode;

		// Token: 0x040000DE RID: 222
		private string _bannerTextCode;

		// Token: 0x040000DF RID: 223
		private bool _isSelected;

		// Token: 0x040000E0 RID: 224
		private bool _isCorrupted;

		// Token: 0x040000E1 RID: 225
		private bool _isFilteredOut;

		// Token: 0x040000E2 RID: 226
		private bool _isDisabled;
	}
}
