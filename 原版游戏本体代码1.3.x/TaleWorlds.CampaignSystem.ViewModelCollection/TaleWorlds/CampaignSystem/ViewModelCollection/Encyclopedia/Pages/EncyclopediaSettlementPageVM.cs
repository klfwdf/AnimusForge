using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D8 RID: 216
	[EncyclopediaViewModel(typeof(Settlement))]
	public class EncyclopediaSettlementPageVM : EncyclopediaContentPageVM
	{
		// Token: 0x06001474 RID: 5236 RVA: 0x00051050 File Offset: 0x0004F250
		public EncyclopediaSettlementPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this._settlement = base.Obj as Settlement;
			this.NotableCharacters = new MBBindingList<HeroVM>();
			this.Settlements = new MBBindingList<EncyclopediaSettlementVM>();
			this.History = new MBBindingList<EncyclopediaHistoryEventVM>();
			this._isVisualTrackerSelected = Campaign.Current.VisualTrackerManager.CheckTracked(this._settlement);
			this.IsFortification = this._settlement.IsFortification;
			this.SettlementImageID = this._settlement.SettlementComponent.WaitMeshName;
			base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(this._settlement);
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.RefreshValues();
			TextObject textObject;
			if (CampaignUIHelper.IsSettlementInformationHidden(this._settlement, out textObject))
			{
				Game.Current.EventManager.TriggerEvent<EncyclopediaPageChangedEvent>(new EncyclopediaPageChangedEvent(EncyclopediaPages.Settlement, true));
			}
		}

		// Token: 0x06001475 RID: 5237 RVA: 0x00051140 File Offset: 0x0004F340
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.SettlementName = this._settlement.Name.ToString();
			this.SettlementsText = GameTexts.FindText("str_villages", null).ToString();
			this.NotableCharactersText = GameTexts.FindText("str_notable_characters", null).ToString();
			this.OwnerText = GameTexts.FindText("str_owner", null).ToString();
			this.TrackText = GameTexts.FindText("str_settlement_track", null).ToString();
			this.ShowInMapHint = new HintViewModel(GameTexts.FindText("str_show_on_map", null), null);
			this.InformationText = this._settlement.EncyclopediaText.ToString();
			base.UpdateBookmarkHintText();
			this.Refresh();
		}

		// Token: 0x06001476 RID: 5238 RVA: 0x000511FC File Offset: 0x0004F3FC
		public override void Refresh()
		{
			base.IsLoadingOver = false;
			SettlementComponent settlementComponent = this._settlement.SettlementComponent;
			this.NotableCharacters.Clear();
			this.Settlements.Clear();
			this.History.Clear();
			this.IsFortification = this._settlement.IsFortification;
			if (this._settlement.IsFortification)
			{
				this.SettlementType = 0;
				EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
				using (List<Village>.Enumerator enumerator = this._settlement.BoundVillages.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Village village = enumerator.Current;
						if (pageOf.IsValidEncyclopediaItem(village.Owner.Settlement))
						{
							this.Settlements.Add(new EncyclopediaSettlementVM(village.Owner.Settlement));
						}
					}
					goto IL_F2;
				}
			}
			if (this._settlement.IsVillage)
			{
				this.SettlementType = 1;
			}
			IL_F2:
			if (!this._settlement.IsCastle)
			{
				EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
				foreach (Hero hero in this._settlement.Notables)
				{
					if (pageOf2.IsValidEncyclopediaItem(hero))
					{
						this.NotableCharacters.Add(new HeroVM(hero, false));
					}
				}
			}
			GameTexts.SetVariable("STR1", GameTexts.FindText("str_enc_sf_culture", null).ToString());
			GameTexts.SetVariable("STR2", this._settlement.Culture.Name.ToString());
			this.CultureText = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
			this.OwnerText = GameTexts.FindText("str_owner", null).ToString();
			this.Owner = new HeroVM(this._settlement.OwnerClan.Leader, false);
			this.OwnerBanner = new EncyclopediaFactionVM(this._settlement.OwnerClan);
			this.SettlementPath = settlementComponent.BackgroundMeshName;
			this.SettlementCropPosition = (double)settlementComponent.BackgroundCropPosition;
			this.HasBoundSettlement = this._settlement.IsVillage;
			this.BoundSettlement = (this.HasBoundSettlement ? new EncyclopediaSettlementVM(this._settlement.Village.Bound) : null);
			this.BoundSettlementText = "";
			if (this.HasBoundSettlement)
			{
				GameTexts.SetVariable("SETTLEMENT_LINK", this._settlement.Village.Bound.EncyclopediaLinkWithName);
				this.BoundSettlementText = GameTexts.FindText("str_bound_settlement_encyclopedia", null).ToString();
			}
			TextObject textObject;
			bool flag = CampaignUIHelper.IsSettlementInformationHidden(this._settlement, out textObject);
			string text = GameTexts.FindText("str_missing_info_indicator", null).ToString();
			string statText = (flag ? text : ((int)this._settlement.Militia).ToString());
			if (this._settlement.IsFortification)
			{
				MBBindingList<EncyclopediaSettlementPageStatItemVM> mbbindingList = new MBBindingList<EncyclopediaSettlementPageStatItemVM>();
				mbbindingList.Add(new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(this._settlement.Town)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Wall, flag ? text : this._settlement.Town.GetWallLevel().ToString()));
				BasicTooltipViewModel basicTooltipViewModel = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(this._settlement.Town));
				EncyclopediaSettlementPageStatItemVM.DescriptionType type = EncyclopediaSettlementPageStatItemVM.DescriptionType.Garrison;
				string statText2;
				if (!flag)
				{
					MobileParty garrisonParty = this._settlement.Town.GarrisonParty;
					statText2 = ((garrisonParty != null) ? garrisonParty.Party.NumberOfAllMembers.ToString() : null);
				}
				else
				{
					statText2 = text;
				}
				mbbindingList.Add(new EncyclopediaSettlementPageStatItemVM(basicTooltipViewModel, type, statText2));
				mbbindingList.Add(new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetTownMilitiaTooltip(this._settlement.Town)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Militia, statText));
				mbbindingList.Add(new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(this._settlement.Town)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Food, flag ? text : ((int)this._settlement.Town.FoodStocks).ToString()));
				this.LeftSideProperties = mbbindingList;
				this.RightSideProperties = new MBBindingList<EncyclopediaSettlementPageStatItemVM>
				{
					new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(this._settlement.Town)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Prosperity, flag ? text : ((int)this._settlement.Town.Prosperity).ToString()),
					new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(this._settlement.Town)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Loyalty, flag ? text : ((int)this._settlement.Town.Loyalty).ToString()),
					new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(this._settlement.Town)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Security, flag ? text : ((int)this._settlement.Town.Security).ToString())
				};
			}
			else
			{
				this.LeftSideProperties = new MBBindingList<EncyclopediaSettlementPageStatItemVM>
				{
					new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageMilitiaTooltip(this._settlement.Village)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Militia, statText)
				};
				this.RightSideProperties = new MBBindingList<EncyclopediaSettlementPageStatItemVM>
				{
					new EncyclopediaSettlementPageStatItemVM(new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageProsperityTooltip(this._settlement.Village)), EncyclopediaSettlementPageStatItemVM.DescriptionType.Prosperity, flag ? text : ((int)this._settlement.Village.Hearth).ToString())
				};
			}
			this.NameText = this._settlement.Name.ToString();
			for (int i = Campaign.Current.LogEntryHistory.GameActionLogs.Count - 1; i >= 0; i--)
			{
				IEncyclopediaLog encyclopediaLog;
				if ((encyclopediaLog = Campaign.Current.LogEntryHistory.GameActionLogs[i] as IEncyclopediaLog) != null && encyclopediaLog.IsVisibleInEncyclopediaPageOf<Settlement>(this._settlement))
				{
					this.History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
				}
			}
			this.IsVisualTrackerSelected = Campaign.Current.VisualTrackerManager.CheckTracked(this._settlement);
			base.IsLoadingOver = true;
		}

		// Token: 0x06001477 RID: 5239 RVA: 0x000517D0 File Offset: 0x0004F9D0
		public override string GetName()
		{
			return this._settlement.Name.ToString();
		}

		// Token: 0x06001478 RID: 5240 RVA: 0x000517E4 File Offset: 0x0004F9E4
		public void ExecuteTrack()
		{
			if (!this.IsVisualTrackerSelected)
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(this._settlement);
				this.IsVisualTrackerSelected = true;
			}
			else
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(this._settlement, false);
				this.IsVisualTrackerSelected = false;
			}
			Game.Current.EventManager.TriggerEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(new PlayerToggleTrackSettlementFromEncyclopediaEvent(this._settlement, this.IsVisualTrackerSelected));
		}

		// Token: 0x06001479 RID: 5241 RVA: 0x00051854 File Offset: 0x0004FA54
		public override string GetNavigationBarURL()
		{
			return HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ " + HyperlinkTexts.GetGenericHyperlinkText("ListPage-Settlements", GameTexts.FindText("str_encyclopedia_settlements", null).ToString()) + " \\ " + this.GetName();
		}

		// Token: 0x0600147A RID: 5242 RVA: 0x000518B9 File Offset: 0x0004FAB9
		public void ExecuteBoundSettlementLink()
		{
			if (this.HasBoundSettlement)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._settlement.Village.Bound.EncyclopediaLink);
			}
		}

		// Token: 0x0600147B RID: 5243 RVA: 0x000518E8 File Offset: 0x0004FAE8
		public override void ExecuteSwitchBookmarkedState()
		{
			base.ExecuteSwitchBookmarkedState();
			if (base.IsBookmarked)
			{
				Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(this._settlement);
				return;
			}
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(this._settlement);
		}

		// Token: 0x0600147C RID: 5244 RVA: 0x00051938 File Offset: 0x0004FB38
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent evnt)
		{
			this.IsTrackerButtonHighlightEnabled = evnt.NewNotificationElementID == "EncyclopediaItemTrackButton";
		}

		// Token: 0x0600147D RID: 5245 RVA: 0x00051950 File Offset: 0x0004FB50
		public override void OnFinalize()
		{
			base.OnFinalize();
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x170006C6 RID: 1734
		// (get) Token: 0x0600147E RID: 5246 RVA: 0x00051973 File Offset: 0x0004FB73
		// (set) Token: 0x0600147F RID: 5247 RVA: 0x0005197B File Offset: 0x0004FB7B
		[DataSourceProperty]
		public EncyclopediaFactionVM OwnerBanner
		{
			get
			{
				return this._ownerBanner;
			}
			set
			{
				if (value != this._ownerBanner)
				{
					this._ownerBanner = value;
					base.OnPropertyChangedWithValue<EncyclopediaFactionVM>(value, "OwnerBanner");
				}
			}
		}

		// Token: 0x170006C7 RID: 1735
		// (get) Token: 0x06001480 RID: 5248 RVA: 0x00051999 File Offset: 0x0004FB99
		// (set) Token: 0x06001481 RID: 5249 RVA: 0x000519A1 File Offset: 0x0004FBA1
		[DataSourceProperty]
		public EncyclopediaSettlementVM BoundSettlement
		{
			get
			{
				return this._boundSettlement;
			}
			set
			{
				if (value != this._boundSettlement)
				{
					this._boundSettlement = value;
					base.OnPropertyChangedWithValue<EncyclopediaSettlementVM>(value, "BoundSettlement");
				}
			}
		}

		// Token: 0x170006C8 RID: 1736
		// (get) Token: 0x06001482 RID: 5250 RVA: 0x000519BF File Offset: 0x0004FBBF
		// (set) Token: 0x06001483 RID: 5251 RVA: 0x000519C7 File Offset: 0x0004FBC7
		[DataSourceProperty]
		public bool IsFortification
		{
			get
			{
				return this._isFortification;
			}
			set
			{
				if (value != this._isFortification)
				{
					this._isFortification = value;
					base.OnPropertyChangedWithValue(value, "IsFortification");
				}
			}
		}

		// Token: 0x170006C9 RID: 1737
		// (get) Token: 0x06001484 RID: 5252 RVA: 0x000519E5 File Offset: 0x0004FBE5
		// (set) Token: 0x06001485 RID: 5253 RVA: 0x000519ED File Offset: 0x0004FBED
		[DataSourceProperty]
		public bool IsTrackerButtonHighlightEnabled
		{
			get
			{
				return this._isTrackerButtonHighlightEnabled;
			}
			set
			{
				if (value != this._isTrackerButtonHighlightEnabled)
				{
					this._isTrackerButtonHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsTrackerButtonHighlightEnabled");
				}
			}
		}

		// Token: 0x170006CA RID: 1738
		// (get) Token: 0x06001486 RID: 5254 RVA: 0x00051A0B File Offset: 0x0004FC0B
		// (set) Token: 0x06001487 RID: 5255 RVA: 0x00051A13 File Offset: 0x0004FC13
		[DataSourceProperty]
		public bool HasBoundSettlement
		{
			get
			{
				return this._hasBoundSettlement;
			}
			set
			{
				if (value != this._hasBoundSettlement)
				{
					this._hasBoundSettlement = value;
					base.OnPropertyChangedWithValue(value, "HasBoundSettlement");
				}
			}
		}

		// Token: 0x170006CB RID: 1739
		// (get) Token: 0x06001488 RID: 5256 RVA: 0x00051A31 File Offset: 0x0004FC31
		// (set) Token: 0x06001489 RID: 5257 RVA: 0x00051A39 File Offset: 0x0004FC39
		[DataSourceProperty]
		public double SettlementCropPosition
		{
			get
			{
				return this._settlementCropPosition;
			}
			set
			{
				if (value != this._settlementCropPosition)
				{
					this._settlementCropPosition = value;
					base.OnPropertyChangedWithValue(value, "SettlementCropPosition");
				}
			}
		}

		// Token: 0x170006CC RID: 1740
		// (get) Token: 0x0600148A RID: 5258 RVA: 0x00051A57 File Offset: 0x0004FC57
		// (set) Token: 0x0600148B RID: 5259 RVA: 0x00051A5F File Offset: 0x0004FC5F
		[DataSourceProperty]
		public string BoundSettlementText
		{
			get
			{
				return this._boundSettlementText;
			}
			set
			{
				if (value != this._boundSettlementText)
				{
					this._boundSettlementText = value;
					base.OnPropertyChangedWithValue<string>(value, "BoundSettlementText");
				}
			}
		}

		// Token: 0x170006CD RID: 1741
		// (get) Token: 0x0600148C RID: 5260 RVA: 0x00051A82 File Offset: 0x0004FC82
		// (set) Token: 0x0600148D RID: 5261 RVA: 0x00051A8A File Offset: 0x0004FC8A
		[DataSourceProperty]
		public string TrackText
		{
			get
			{
				return this._trackText;
			}
			set
			{
				if (value != this._trackText)
				{
					this._trackText = value;
					base.OnPropertyChangedWithValue<string>(value, "TrackText");
				}
			}
		}

		// Token: 0x170006CE RID: 1742
		// (get) Token: 0x0600148E RID: 5262 RVA: 0x00051AAD File Offset: 0x0004FCAD
		// (set) Token: 0x0600148F RID: 5263 RVA: 0x00051AB5 File Offset: 0x0004FCB5
		[DataSourceProperty]
		public string SettlementPath
		{
			get
			{
				return this._settlementPath;
			}
			set
			{
				if (value != this._settlementPath)
				{
					this._settlementPath = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementPath");
				}
			}
		}

		// Token: 0x170006CF RID: 1743
		// (get) Token: 0x06001490 RID: 5264 RVA: 0x00051AD8 File Offset: 0x0004FCD8
		// (set) Token: 0x06001491 RID: 5265 RVA: 0x00051AE0 File Offset: 0x0004FCE0
		[DataSourceProperty]
		public string SettlementName
		{
			get
			{
				return this._settlementName;
			}
			set
			{
				if (value != this._settlementName)
				{
					this._settlementName = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementName");
				}
			}
		}

		// Token: 0x170006D0 RID: 1744
		// (get) Token: 0x06001492 RID: 5266 RVA: 0x00051B03 File Offset: 0x0004FD03
		// (set) Token: 0x06001493 RID: 5267 RVA: 0x00051B0B File Offset: 0x0004FD0B
		[DataSourceProperty]
		public string InformationText
		{
			get
			{
				return this._informationText;
			}
			set
			{
				if (value != this._informationText)
				{
					this._informationText = value;
					base.OnPropertyChangedWithValue<string>(value, "InformationText");
				}
			}
		}

		// Token: 0x170006D1 RID: 1745
		// (get) Token: 0x06001494 RID: 5268 RVA: 0x00051B2E File Offset: 0x0004FD2E
		// (set) Token: 0x06001495 RID: 5269 RVA: 0x00051B36 File Offset: 0x0004FD36
		[DataSourceProperty]
		public HeroVM Owner
		{
			get
			{
				return this._owner;
			}
			set
			{
				if (value != this._owner)
				{
					this._owner = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Owner");
				}
			}
		}

		// Token: 0x170006D2 RID: 1746
		// (get) Token: 0x06001496 RID: 5270 RVA: 0x00051B54 File Offset: 0x0004FD54
		// (set) Token: 0x06001497 RID: 5271 RVA: 0x00051B5C File Offset: 0x0004FD5C
		[DataSourceProperty]
		public string SettlementsText
		{
			get
			{
				return this._villagesText;
			}
			set
			{
				if (value != this._villagesText)
				{
					this._villagesText = value;
					base.OnPropertyChanged("VillagesText");
				}
			}
		}

		// Token: 0x170006D3 RID: 1747
		// (get) Token: 0x06001498 RID: 5272 RVA: 0x00051B7E File Offset: 0x0004FD7E
		// (set) Token: 0x06001499 RID: 5273 RVA: 0x00051B86 File Offset: 0x0004FD86
		[DataSourceProperty]
		public string SettlementImageID
		{
			get
			{
				return this._settlementImageID;
			}
			set
			{
				if (value != this._settlementImageID)
				{
					this._settlementImageID = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementImageID");
				}
			}
		}

		// Token: 0x170006D4 RID: 1748
		// (get) Token: 0x0600149A RID: 5274 RVA: 0x00051BA9 File Offset: 0x0004FDA9
		// (set) Token: 0x0600149B RID: 5275 RVA: 0x00051BB1 File Offset: 0x0004FDB1
		[DataSourceProperty]
		public string NotableCharactersText
		{
			get
			{
				return this._notableCharactersText;
			}
			set
			{
				if (value != this._notableCharactersText)
				{
					this._notableCharactersText = value;
					base.OnPropertyChangedWithValue<string>(value, "NotableCharactersText");
				}
			}
		}

		// Token: 0x170006D5 RID: 1749
		// (get) Token: 0x0600149C RID: 5276 RVA: 0x00051BD4 File Offset: 0x0004FDD4
		// (set) Token: 0x0600149D RID: 5277 RVA: 0x00051BDC File Offset: 0x0004FDDC
		[DataSourceProperty]
		public int SettlementType
		{
			get
			{
				return this._settlementType;
			}
			set
			{
				if (value != this._settlementType)
				{
					this._settlementType = value;
					base.OnPropertyChangedWithValue(value, "SettlementType");
				}
			}
		}

		// Token: 0x170006D6 RID: 1750
		// (get) Token: 0x0600149E RID: 5278 RVA: 0x00051BFA File Offset: 0x0004FDFA
		// (set) Token: 0x0600149F RID: 5279 RVA: 0x00051C02 File Offset: 0x0004FE02
		[DataSourceProperty]
		public MBBindingList<EncyclopediaHistoryEventVM> History
		{
			get
			{
				return this._history;
			}
			set
			{
				if (value != this._history)
				{
					this._history = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaHistoryEventVM>>(value, "History");
				}
			}
		}

		// Token: 0x170006D7 RID: 1751
		// (get) Token: 0x060014A0 RID: 5280 RVA: 0x00051C20 File Offset: 0x0004FE20
		// (set) Token: 0x060014A1 RID: 5281 RVA: 0x00051C28 File Offset: 0x0004FE28
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSettlementVM> Settlements
		{
			get
			{
				return this._settlements;
			}
			set
			{
				if (value != this._settlements)
				{
					this._settlements = value;
					base.OnPropertyChanged("Villages");
				}
			}
		}

		// Token: 0x170006D8 RID: 1752
		// (get) Token: 0x060014A2 RID: 5282 RVA: 0x00051C45 File Offset: 0x0004FE45
		// (set) Token: 0x060014A3 RID: 5283 RVA: 0x00051C4D File Offset: 0x0004FE4D
		[DataSourceProperty]
		public MBBindingList<HeroVM> NotableCharacters
		{
			get
			{
				return this._notableCharacters;
			}
			set
			{
				if (value != this._notableCharacters)
				{
					this._notableCharacters = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "NotableCharacters");
				}
			}
		}

		// Token: 0x170006D9 RID: 1753
		// (get) Token: 0x060014A4 RID: 5284 RVA: 0x00051C6B File Offset: 0x0004FE6B
		// (set) Token: 0x060014A5 RID: 5285 RVA: 0x00051C73 File Offset: 0x0004FE73
		[DataSourceProperty]
		public HintViewModel ShowInMapHint
		{
			get
			{
				return this._showInMapHint;
			}
			set
			{
				if (value != this._showInMapHint)
				{
					this._showInMapHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ShowInMapHint");
				}
			}
		}

		// Token: 0x170006DA RID: 1754
		// (get) Token: 0x060014A6 RID: 5286 RVA: 0x00051C91 File Offset: 0x0004FE91
		// (set) Token: 0x060014A7 RID: 5287 RVA: 0x00051C99 File Offset: 0x0004FE99
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSettlementPageStatItemVM> LeftSideProperties
		{
			get
			{
				return this._leftSideProperties;
			}
			set
			{
				if (value != this._leftSideProperties)
				{
					this._leftSideProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSettlementPageStatItemVM>>(value, "LeftSideProperties");
				}
			}
		}

		// Token: 0x170006DB RID: 1755
		// (get) Token: 0x060014A8 RID: 5288 RVA: 0x00051CB7 File Offset: 0x0004FEB7
		// (set) Token: 0x060014A9 RID: 5289 RVA: 0x00051CBF File Offset: 0x0004FEBF
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSettlementPageStatItemVM> RightSideProperties
		{
			get
			{
				return this._rightSideProperties;
			}
			set
			{
				if (value != this._rightSideProperties)
				{
					this._rightSideProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSettlementPageStatItemVM>>(value, "RightSideProperties");
				}
			}
		}

		// Token: 0x170006DC RID: 1756
		// (get) Token: 0x060014AA RID: 5290 RVA: 0x00051CDD File Offset: 0x0004FEDD
		// (set) Token: 0x060014AB RID: 5291 RVA: 0x00051CE5 File Offset: 0x0004FEE5
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

		// Token: 0x170006DD RID: 1757
		// (get) Token: 0x060014AC RID: 5292 RVA: 0x00051D08 File Offset: 0x0004FF08
		// (set) Token: 0x060014AD RID: 5293 RVA: 0x00051D10 File Offset: 0x0004FF10
		[DataSourceProperty]
		public string CultureText
		{
			get
			{
				return this._cultureText;
			}
			set
			{
				if (value != this._cultureText)
				{
					this._cultureText = value;
					base.OnPropertyChangedWithValue<string>(value, "CultureText");
				}
			}
		}

		// Token: 0x170006DE RID: 1758
		// (get) Token: 0x060014AE RID: 5294 RVA: 0x00051D33 File Offset: 0x0004FF33
		// (set) Token: 0x060014AF RID: 5295 RVA: 0x00051D3B File Offset: 0x0004FF3B
		[DataSourceProperty]
		public string OwnerText
		{
			get
			{
				return this._ownerText;
			}
			set
			{
				if (value != this._ownerText)
				{
					this._ownerText = value;
					base.OnPropertyChangedWithValue<string>(value, "OwnerText");
				}
			}
		}

		// Token: 0x170006DF RID: 1759
		// (get) Token: 0x060014B0 RID: 5296 RVA: 0x00051D5E File Offset: 0x0004FF5E
		// (set) Token: 0x060014B1 RID: 5297 RVA: 0x00051D66 File Offset: 0x0004FF66
		[DataSourceProperty]
		public bool IsVisualTrackerSelected
		{
			get
			{
				return this._isVisualTrackerSelected;
			}
			set
			{
				if (value != this._isVisualTrackerSelected)
				{
					this._isVisualTrackerSelected = value;
					base.OnPropertyChangedWithValue(value, "IsVisualTrackerSelected");
				}
			}
		}

		// Token: 0x0400095F RID: 2399
		protected readonly Settlement _settlement;

		// Token: 0x04000960 RID: 2400
		private int _settlementType;

		// Token: 0x04000961 RID: 2401
		private MBBindingList<EncyclopediaHistoryEventVM> _history;

		// Token: 0x04000962 RID: 2402
		private MBBindingList<EncyclopediaSettlementVM> _settlements;

		// Token: 0x04000963 RID: 2403
		private EncyclopediaSettlementVM _boundSettlement;

		// Token: 0x04000964 RID: 2404
		private MBBindingList<HeroVM> _notableCharacters;

		// Token: 0x04000965 RID: 2405
		private EncyclopediaFactionVM _ownerBanner;

		// Token: 0x04000966 RID: 2406
		private HintViewModel _showInMapHint;

		// Token: 0x04000967 RID: 2407
		private MBBindingList<EncyclopediaSettlementPageStatItemVM> _leftSideProperties;

		// Token: 0x04000968 RID: 2408
		private MBBindingList<EncyclopediaSettlementPageStatItemVM> _rightSideProperties;

		// Token: 0x04000969 RID: 2409
		private HeroVM _owner;

		// Token: 0x0400096A RID: 2410
		private string _ownerText;

		// Token: 0x0400096B RID: 2411
		private string _nameText;

		// Token: 0x0400096C RID: 2412
		private string _cultureText;

		// Token: 0x0400096D RID: 2413
		private string _villagesText;

		// Token: 0x0400096E RID: 2414
		private string _notableCharactersText;

		// Token: 0x0400096F RID: 2415
		private string _settlementPath;

		// Token: 0x04000970 RID: 2416
		private string _settlementName;

		// Token: 0x04000971 RID: 2417
		private string _informationText;

		// Token: 0x04000972 RID: 2418
		private string _settlementImageID;

		// Token: 0x04000973 RID: 2419
		private string _boundSettlementText;

		// Token: 0x04000974 RID: 2420
		private string _trackText;

		// Token: 0x04000975 RID: 2421
		private double _settlementCropPosition;

		// Token: 0x04000976 RID: 2422
		private bool _isFortification;

		// Token: 0x04000977 RID: 2423
		private bool _isVisualTrackerSelected;

		// Token: 0x04000978 RID: 2424
		private bool _hasBoundSettlement;

		// Token: 0x04000979 RID: 2425
		private bool _isTrackerButtonHighlightEnabled;

		// Token: 0x02000241 RID: 577
		private enum SettlementTypes
		{
			// Token: 0x04001239 RID: 4665
			Town,
			// Token: 0x0400123A RID: 4666
			LoneVillage,
			// Token: 0x0400123B RID: 4667
			VillageWithCastle
		}
	}
}
