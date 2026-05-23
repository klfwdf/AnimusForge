using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup
{
	// Token: 0x0200003A RID: 58
	public class MarriageOfferPopupVM : ViewModel
	{
		// Token: 0x060005A1 RID: 1441 RVA: 0x0001E128 File Offset: 0x0001C328
		public MarriageOfferPopupVM(Hero suitor, Hero maiden, Action onClose)
		{
			this._marriageBehavior = Campaign.Current.GetCampaignBehavior<IMarriageOfferCampaignBehavior>();
			this._onClose = onClose;
			if (suitor.Clan == Clan.PlayerClan)
			{
				this.OffereeClanMember = new MarriageOfferPopupHeroVM(suitor);
				this.OffererClanMember = new MarriageOfferPopupHeroVM(maiden);
			}
			else
			{
				this.OffereeClanMember = new MarriageOfferPopupHeroVM(maiden);
				this.OffererClanMember = new MarriageOfferPopupHeroVM(suitor);
			}
			this.ConsequencesList = new MBBindingList<BindingListStringItem>();
			this.RefreshValues();
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x0001E1A2 File Offset: 0x0001C3A2
		public void Update()
		{
			MarriageOfferPopupHeroVM offereeClanMember = this.OffereeClanMember;
			if (offereeClanMember != null)
			{
				offereeClanMember.Update();
			}
			MarriageOfferPopupHeroVM offererClanMember = this.OffererClanMember;
			if (offererClanMember == null)
			{
				return;
			}
			offererClanMember.Update();
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x0001E1C5 File Offset: 0x0001C3C5
		public void ExecuteAcceptOffer()
		{
			IMarriageOfferCampaignBehavior marriageBehavior = this._marriageBehavior;
			if (marriageBehavior != null)
			{
				marriageBehavior.OnMarriageOfferAcceptedOnPopUp();
			}
			Action onClose = this._onClose;
			if (onClose == null)
			{
				return;
			}
			onClose();
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x0001E1E8 File Offset: 0x0001C3E8
		public void ExecuteDeclineOffer()
		{
			IMarriageOfferCampaignBehavior marriageBehavior = this._marriageBehavior;
			if (marriageBehavior != null)
			{
				marriageBehavior.OnMarriageOfferDeclinedOnPopUp();
			}
			Action onClose = this._onClose;
			if (onClose == null)
			{
				return;
			}
			onClose();
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x0001E20C File Offset: 0x0001C40C
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject textObject = GameTexts.FindText("str_marriage_offer_from_clan", null);
			textObject.SetTextVariable("CLAN_NAME", this.OffererClanMember.Hero.Clan.Name);
			this.TitleText = textObject.ToString();
			this.ClanText = GameTexts.FindText("str_clan", null).ToString();
			this.AgeText = new TextObject("{=jaaQijQs}Age", null).ToString();
			this.OccupationText = new TextObject("{=GZxFIeiJ}Occupation", null).ToString();
			this.RelationText = new TextObject("{=BlidMNGT}Relation", null).ToString();
			this.ConsequencesText = new TextObject("{=Lm6Mkhru}Consequences", null).ToString();
			this.ButtonOkLabel = new TextObject("{=Y94H6XnK}Accept", null).ToString();
			this.ButtonCancelLabel = new TextObject("{=cOgmdp9e}Decline", null).ToString();
			this.ConsequencesList.Clear();
			IMarriageOfferCampaignBehavior marriageBehavior = this._marriageBehavior;
			foreach (TextObject textObject2 in (((marriageBehavior != null) ? marriageBehavior.GetMarriageAcceptedConsequences() : null) ?? new MBBindingList<TextObject>()))
			{
				this.ConsequencesList.Add(new BindingListStringItem("- " + textObject2.ToString()));
			}
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x0001E36C File Offset: 0x0001C56C
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey != null)
			{
				cancelInputKey.OnFinalize();
			}
			MarriageOfferPopupHeroVM offereeClanMember = this.OffereeClanMember;
			if (offereeClanMember != null)
			{
				offereeClanMember.OnFinalize();
			}
			MarriageOfferPopupHeroVM offererClanMember = this.OffererClanMember;
			if (offererClanMember == null)
			{
				return;
			}
			offererClanMember.OnFinalize();
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x0001E3C2 File Offset: 0x0001C5C2
		public void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x17000198 RID: 408
		// (get) Token: 0x060005A8 RID: 1448 RVA: 0x0001E3D4 File Offset: 0x0001C5D4
		// (set) Token: 0x060005A9 RID: 1449 RVA: 0x0001E3DC File Offset: 0x0001C5DC
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x17000199 RID: 409
		// (get) Token: 0x060005AA RID: 1450 RVA: 0x0001E3FF File Offset: 0x0001C5FF
		// (set) Token: 0x060005AB RID: 1451 RVA: 0x0001E407 File Offset: 0x0001C607
		[DataSourceProperty]
		public string ClanText
		{
			get
			{
				return this._clanText;
			}
			set
			{
				if (value != this._clanText)
				{
					this._clanText = value;
					base.OnPropertyChangedWithValue<string>(value, "ClanText");
				}
			}
		}

		// Token: 0x1700019A RID: 410
		// (get) Token: 0x060005AC RID: 1452 RVA: 0x0001E42A File Offset: 0x0001C62A
		// (set) Token: 0x060005AD RID: 1453 RVA: 0x0001E432 File Offset: 0x0001C632
		[DataSourceProperty]
		public string AgeText
		{
			get
			{
				return this._ageText;
			}
			set
			{
				if (value != this._ageText)
				{
					this._ageText = value;
					base.OnPropertyChangedWithValue<string>(value, "AgeText");
				}
			}
		}

		// Token: 0x1700019B RID: 411
		// (get) Token: 0x060005AE RID: 1454 RVA: 0x0001E455 File Offset: 0x0001C655
		// (set) Token: 0x060005AF RID: 1455 RVA: 0x0001E45D File Offset: 0x0001C65D
		[DataSourceProperty]
		public string OccupationText
		{
			get
			{
				return this._occupationText;
			}
			set
			{
				if (value != this._occupationText)
				{
					this._occupationText = value;
					base.OnPropertyChangedWithValue<string>(value, "OccupationText");
				}
			}
		}

		// Token: 0x1700019C RID: 412
		// (get) Token: 0x060005B0 RID: 1456 RVA: 0x0001E480 File Offset: 0x0001C680
		// (set) Token: 0x060005B1 RID: 1457 RVA: 0x0001E488 File Offset: 0x0001C688
		[DataSourceProperty]
		public string RelationText
		{
			get
			{
				return this._relationText;
			}
			set
			{
				if (value != this._relationText)
				{
					this._relationText = value;
					base.OnPropertyChangedWithValue<string>(value, "RelationText");
				}
			}
		}

		// Token: 0x1700019D RID: 413
		// (get) Token: 0x060005B2 RID: 1458 RVA: 0x0001E4AB File Offset: 0x0001C6AB
		// (set) Token: 0x060005B3 RID: 1459 RVA: 0x0001E4B3 File Offset: 0x0001C6B3
		[DataSourceProperty]
		public string ConsequencesText
		{
			get
			{
				return this._consequencesText;
			}
			set
			{
				if (value != this._consequencesText)
				{
					this._consequencesText = value;
					base.OnPropertyChangedWithValue<string>(value, "ConsequencesText");
				}
			}
		}

		// Token: 0x1700019E RID: 414
		// (get) Token: 0x060005B4 RID: 1460 RVA: 0x0001E4D6 File Offset: 0x0001C6D6
		// (set) Token: 0x060005B5 RID: 1461 RVA: 0x0001E4DE File Offset: 0x0001C6DE
		[DataSourceProperty]
		public MBBindingList<BindingListStringItem> ConsequencesList
		{
			get
			{
				return this._consequencesList;
			}
			set
			{
				if (value != this._consequencesList)
				{
					this._consequencesList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BindingListStringItem>>(value, "ConsequencesList");
				}
			}
		}

		// Token: 0x1700019F RID: 415
		// (get) Token: 0x060005B6 RID: 1462 RVA: 0x0001E4FC File Offset: 0x0001C6FC
		// (set) Token: 0x060005B7 RID: 1463 RVA: 0x0001E504 File Offset: 0x0001C704
		[DataSourceProperty]
		public string ButtonOkLabel
		{
			get
			{
				return this._buttonOkLabel;
			}
			set
			{
				if (value != this._buttonOkLabel)
				{
					this._buttonOkLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "ButtonOkLabel");
				}
			}
		}

		// Token: 0x170001A0 RID: 416
		// (get) Token: 0x060005B8 RID: 1464 RVA: 0x0001E527 File Offset: 0x0001C727
		// (set) Token: 0x060005B9 RID: 1465 RVA: 0x0001E52F File Offset: 0x0001C72F
		[DataSourceProperty]
		public string ButtonCancelLabel
		{
			get
			{
				return this._buttonCancelLabel;
			}
			set
			{
				if (value != this._buttonCancelLabel)
				{
					this._buttonCancelLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "ButtonCancelLabel");
				}
			}
		}

		// Token: 0x170001A1 RID: 417
		// (get) Token: 0x060005BA RID: 1466 RVA: 0x0001E552 File Offset: 0x0001C752
		// (set) Token: 0x060005BB RID: 1467 RVA: 0x0001E55A File Offset: 0x0001C75A
		[DataSourceProperty]
		public bool IsEncyclopediaOpen
		{
			get
			{
				return this._isEncyclopediaOpen;
			}
			set
			{
				if (value != this._isEncyclopediaOpen)
				{
					this._isEncyclopediaOpen = value;
					base.OnPropertyChangedWithValue(value, "IsEncyclopediaOpen");
				}
			}
		}

		// Token: 0x170001A2 RID: 418
		// (get) Token: 0x060005BC RID: 1468 RVA: 0x0001E578 File Offset: 0x0001C778
		// (set) Token: 0x060005BD RID: 1469 RVA: 0x0001E580 File Offset: 0x0001C780
		[DataSourceProperty]
		public MarriageOfferPopupHeroVM OffereeClanMember
		{
			get
			{
				return this._offereeClanMember;
			}
			set
			{
				if (value != this._offereeClanMember)
				{
					this._offereeClanMember = value;
					base.OnPropertyChangedWithValue<MarriageOfferPopupHeroVM>(value, "OffereeClanMember");
				}
			}
		}

		// Token: 0x170001A3 RID: 419
		// (get) Token: 0x060005BE RID: 1470 RVA: 0x0001E59E File Offset: 0x0001C79E
		// (set) Token: 0x060005BF RID: 1471 RVA: 0x0001E5A6 File Offset: 0x0001C7A6
		[DataSourceProperty]
		public MarriageOfferPopupHeroVM OffererClanMember
		{
			get
			{
				return this._offererClanMember;
			}
			set
			{
				if (value != this._offererClanMember)
				{
					this._offererClanMember = value;
					base.OnPropertyChangedWithValue<MarriageOfferPopupHeroVM>(value, "OffererClanMember");
				}
			}
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x0001E5C4 File Offset: 0x0001C7C4
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x0001E5D3 File Offset: 0x0001C7D3
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x170001A4 RID: 420
		// (get) Token: 0x060005C2 RID: 1474 RVA: 0x0001E5E2 File Offset: 0x0001C7E2
		// (set) Token: 0x060005C3 RID: 1475 RVA: 0x0001E5EA File Offset: 0x0001C7EA
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x170001A5 RID: 421
		// (get) Token: 0x060005C4 RID: 1476 RVA: 0x0001E608 File Offset: 0x0001C808
		// (set) Token: 0x060005C5 RID: 1477 RVA: 0x0001E610 File Offset: 0x0001C810
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x04000266 RID: 614
		private readonly IMarriageOfferCampaignBehavior _marriageBehavior;

		// Token: 0x04000267 RID: 615
		private Action _onClose;

		// Token: 0x04000268 RID: 616
		private string _titleText;

		// Token: 0x04000269 RID: 617
		private string _clanText;

		// Token: 0x0400026A RID: 618
		private string _ageText;

		// Token: 0x0400026B RID: 619
		private string _occupationText;

		// Token: 0x0400026C RID: 620
		private string _relationText;

		// Token: 0x0400026D RID: 621
		private string _consequencesText;

		// Token: 0x0400026E RID: 622
		private MBBindingList<BindingListStringItem> _consequencesList;

		// Token: 0x0400026F RID: 623
		private string _buttonOkLabel;

		// Token: 0x04000270 RID: 624
		private string _buttonCancelLabel;

		// Token: 0x04000271 RID: 625
		private bool _isEncyclopediaOpen;

		// Token: 0x04000272 RID: 626
		private MarriageOfferPopupHeroVM _offereeClanMember;

		// Token: 0x04000273 RID: 627
		private MarriageOfferPopupHeroVM _offererClanMember;

		// Token: 0x04000274 RID: 628
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000275 RID: 629
		private InputKeyItemVM _doneInputKey;
	}
}
