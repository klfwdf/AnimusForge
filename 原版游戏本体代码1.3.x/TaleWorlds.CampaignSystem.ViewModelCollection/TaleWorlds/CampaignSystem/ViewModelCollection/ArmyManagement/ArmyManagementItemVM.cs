using System;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement
{
	// Token: 0x0200015C RID: 348
	public class ArmyManagementItemVM : ViewModel
	{
		// Token: 0x17000B3A RID: 2874
		// (get) Token: 0x060020F8 RID: 8440 RVA: 0x00077AEB File Offset: 0x00075CEB
		public float DistInTime { get; }

		// Token: 0x17000B3B RID: 2875
		// (get) Token: 0x060020F9 RID: 8441 RVA: 0x00077AF3 File Offset: 0x00075CF3
		public float _distance { get; }

		// Token: 0x17000B3C RID: 2876
		// (get) Token: 0x060020FA RID: 8442 RVA: 0x00077AFB File Offset: 0x00075CFB
		public Clan Clan { get; }

		// Token: 0x060020FB RID: 8443 RVA: 0x00077B04 File Offset: 0x00075D04
		public ArmyManagementItemVM(Action<ArmyManagementItemVM> onAddToCart, Action<ArmyManagementItemVM> onRemove, Action<ArmyManagementItemVM> onFocus, MobileParty mobileParty)
		{
			ArmyManagementCalculationModel armyManagementCalculationModel = Campaign.Current.Models.ArmyManagementCalculationModel;
			this._onAddToCart = onAddToCart;
			this._onRemove = onRemove;
			this._onFocus = onFocus;
			this.Party = mobileParty;
			this._eligibilityReason = TextObject.GetEmpty();
			this.ClanBanner = new BannerImageIdentifierVM(mobileParty.LeaderHero.ClanBanner, true);
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(mobileParty.LeaderHero.CharacterObject, false);
			this.LordFace = new CharacterImageIdentifierVM(characterCode);
			this.Relation = armyManagementCalculationModel.GetPartyRelation(mobileParty.LeaderHero);
			this.Strength = this.Party.Party.NumberOfHealthyMembers;
			this.ShipCount = this.Party.Ships.Count;
			this._distance = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(this.Party, MobileParty.MainParty, this.Party.NavigationCapability);
			if (MobileParty.MainParty.IsCurrentlyAtSea && !this.Party.HasNavalNavigationCapability)
			{
				this.DistInTime = 2.1474836E+09f;
			}
			else
			{
				this.DistInTime = (float)MathF.Ceiling(this._distance / this.Party.Speed);
				this.Cost = armyManagementCalculationModel.CalculatePartyInfluenceCost(MobileParty.MainParty, mobileParty);
			}
			this.Clan = mobileParty.LeaderHero.Clan;
			this.IsMainHero = mobileParty.IsMainParty;
			this.UpdateEligibility();
			this.IsTransferDisabled = this.IsMainHero || PlayerSiege.PlayerSiegeEvent != null;
			this.RefreshValues();
		}

		// Token: 0x060020FC RID: 8444 RVA: 0x00077CA0 File Offset: 0x00075EA0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.InArmyText = GameTexts.FindText("str_in_army", null).ToString();
			this.LeaderNameText = this.Party.LeaderHero.Name.ToString();
			this.NameText = this.Party.Name.ToString();
			if (!this.Party.IsMainParty)
			{
				this.DistanceText = (((int)this._distance < 5) ? GameTexts.FindText("str_nearby", null).ToString() : CampaignUIHelper.GetPartyDistanceByTimeTextAbbreviated((float)((int)this._distance), this.Party.Speed));
			}
		}

		// Token: 0x060020FD RID: 8445 RVA: 0x00077D41 File Offset: 0x00075F41
		public void ExecuteAction()
		{
			if (this.IsInCart)
			{
				this.OnRemove();
				return;
			}
			this.OnAddToCart();
		}

		// Token: 0x060020FE RID: 8446 RVA: 0x00077D58 File Offset: 0x00075F58
		private void OnRemove()
		{
			if (!this.IsMainHero)
			{
				this._onRemove(this);
				this.UpdateEligibility();
			}
		}

		// Token: 0x060020FF RID: 8447 RVA: 0x00077D74 File Offset: 0x00075F74
		private void OnAddToCart()
		{
			this.UpdateEligibility();
			if (this.IsEligible)
			{
				this._onAddToCart(this);
			}
			this.UpdateEligibility();
		}

		// Token: 0x06002100 RID: 8448 RVA: 0x00077D96 File Offset: 0x00075F96
		public void ExecuteSetFocused()
		{
			this.IsFocused = true;
			Action<ArmyManagementItemVM> onFocus = this._onFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(this);
		}

		// Token: 0x06002101 RID: 8449 RVA: 0x00077DB0 File Offset: 0x00075FB0
		public void ExecuteSetUnfocused()
		{
			this.IsFocused = false;
			Action<ArmyManagementItemVM> onFocus = this._onFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(null);
		}

		// Token: 0x06002102 RID: 8450 RVA: 0x00077DCC File Offset: 0x00075FCC
		public void UpdateEligibility()
		{
			GameModels models = Campaign.Current.Models;
			ArmyManagementCalculationModel armyManagementCalculationModel = ((models != null) ? models.ArmyManagementCalculationModel : null);
			bool flag = true;
			this._eligibilityReason = TextObject.GetEmpty();
			if (!this.CanJoinBackWithoutCost)
			{
				if (this.IsInCart && !this.IsAlreadyWithPlayer)
				{
					flag = false;
					this._eligibilityReason = new TextObject("{=idRXFzQ6}Already added to the army.", null);
				}
				else
				{
					flag = armyManagementCalculationModel.CheckPartyEligibility(this.Party, out this._eligibilityReason);
					if (flag)
					{
						flag = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out this._eligibilityReason);
					}
				}
			}
			this.IsEligible = flag;
		}

		// Token: 0x06002103 RID: 8451 RVA: 0x00077E53 File Offset: 0x00076053
		private void UpdateIsCostRelevant()
		{
			if (this.Cost == 0 && this.IsAlreadyWithPlayer && this.IsInCart)
			{
				this.IsCostRelevant = false;
				return;
			}
			this.IsCostRelevant = true;
		}

		// Token: 0x06002104 RID: 8452 RVA: 0x00077E7C File Offset: 0x0007607C
		public void ExecuteBeginHint()
		{
			if (!this.IsEligible)
			{
				MBInformationManager.ShowHint(this._eligibilityReason.ToString());
				return;
			}
			InformationManager.ShowTooltip(typeof(MobileParty), new object[] { this.Party, true, true });
		}

		// Token: 0x06002105 RID: 8453 RVA: 0x00077ED2 File Offset: 0x000760D2
		public void ExecuteBeginClanHint()
		{
			Type typeFromHandle = typeof(Clan);
			object[] array = new object[3];
			int num = 0;
			MobileParty party = this.Party;
			array[num] = ((party != null) ? party.ActualClan : null);
			array[1] = true;
			array[2] = true;
			InformationManager.ShowTooltip(typeFromHandle, array);
		}

		// Token: 0x06002106 RID: 8454 RVA: 0x00077F10 File Offset: 0x00076110
		public void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06002107 RID: 8455 RVA: 0x00077F17 File Offset: 0x00076117
		public void ExecuteOpenEncyclopedia()
		{
			MobileParty party = this.Party;
			if (((party != null) ? party.LeaderHero : null) != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Party.LeaderHero.EncyclopediaLink);
			}
		}

		// Token: 0x06002108 RID: 8456 RVA: 0x00077F4C File Offset: 0x0007614C
		public void ExecuteOpenClanEncyclopedia()
		{
			MobileParty party = this.Party;
			if (((party != null) ? party.ActualClan : null) != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Party.ActualClan.EncyclopediaLink);
			}
		}

		// Token: 0x17000B3D RID: 2877
		// (get) Token: 0x06002109 RID: 8457 RVA: 0x00077F81 File Offset: 0x00076181
		// (set) Token: 0x0600210A RID: 8458 RVA: 0x00077F89 File Offset: 0x00076189
		[DataSourceProperty]
		public InputKeyItemVM RemoveInputKey
		{
			get
			{
				return this._removeInputKey;
			}
			set
			{
				if (value != this._removeInputKey)
				{
					this._removeInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "RemoveInputKey");
				}
			}
		}

		// Token: 0x17000B3E RID: 2878
		// (get) Token: 0x0600210B RID: 8459 RVA: 0x00077FA7 File Offset: 0x000761A7
		// (set) Token: 0x0600210C RID: 8460 RVA: 0x00077FAF File Offset: 0x000761AF
		[DataSourceProperty]
		public bool IsEligible
		{
			get
			{
				return this._isEligible;
			}
			set
			{
				if (value != this._isEligible)
				{
					this._isEligible = value;
					base.OnPropertyChangedWithValue(value, "IsEligible");
				}
			}
		}

		// Token: 0x17000B3F RID: 2879
		// (get) Token: 0x0600210D RID: 8461 RVA: 0x00077FCD File Offset: 0x000761CD
		// (set) Token: 0x0600210E RID: 8462 RVA: 0x00077FD5 File Offset: 0x000761D5
		[DataSourceProperty]
		public bool IsInCart
		{
			get
			{
				return this._isInCart;
			}
			set
			{
				if (value != this._isInCart)
				{
					this._isInCart = value;
					base.OnPropertyChangedWithValue(value, "IsInCart");
					this.UpdateIsCostRelevant();
				}
			}
		}

		// Token: 0x17000B40 RID: 2880
		// (get) Token: 0x0600210F RID: 8463 RVA: 0x00077FF9 File Offset: 0x000761F9
		// (set) Token: 0x06002110 RID: 8464 RVA: 0x00078001 File Offset: 0x00076201
		[DataSourceProperty]
		public bool IsMainHero
		{
			get
			{
				return this._isMainHero;
			}
			set
			{
				if (value != this._isMainHero)
				{
					this._isMainHero = value;
					base.OnPropertyChangedWithValue(value, "IsMainHero");
				}
			}
		}

		// Token: 0x17000B41 RID: 2881
		// (get) Token: 0x06002111 RID: 8465 RVA: 0x0007801F File Offset: 0x0007621F
		// (set) Token: 0x06002112 RID: 8466 RVA: 0x00078027 File Offset: 0x00076227
		[DataSourceProperty]
		public int Strength
		{
			get
			{
				return this._strength;
			}
			set
			{
				if (value != this._strength)
				{
					this._strength = value;
					base.OnPropertyChangedWithValue(value, "Strength");
				}
			}
		}

		// Token: 0x17000B42 RID: 2882
		// (get) Token: 0x06002113 RID: 8467 RVA: 0x00078045 File Offset: 0x00076245
		// (set) Token: 0x06002114 RID: 8468 RVA: 0x0007804D File Offset: 0x0007624D
		[DataSourceProperty]
		public int ShipCount
		{
			get
			{
				return this._shipCount;
			}
			set
			{
				if (value != this._shipCount)
				{
					this._shipCount = value;
					base.OnPropertyChangedWithValue(value, "ShipCount");
					this.HasShip = this._shipCount > 0;
				}
			}
		}

		// Token: 0x17000B43 RID: 2883
		// (get) Token: 0x06002115 RID: 8469 RVA: 0x0007807A File Offset: 0x0007627A
		// (set) Token: 0x06002116 RID: 8470 RVA: 0x00078082 File Offset: 0x00076282
		[DataSourceProperty]
		public bool HasShip
		{
			get
			{
				return this._hasShip;
			}
			set
			{
				if (value != this._hasShip)
				{
					this._hasShip = value;
					base.OnPropertyChangedWithValue(value, "HasShip");
				}
			}
		}

		// Token: 0x17000B44 RID: 2884
		// (get) Token: 0x06002117 RID: 8471 RVA: 0x000780A0 File Offset: 0x000762A0
		// (set) Token: 0x06002118 RID: 8472 RVA: 0x000780A8 File Offset: 0x000762A8
		[DataSourceProperty]
		public string DistanceText
		{
			get
			{
				return this._distanceText;
			}
			set
			{
				if (value != this._distanceText)
				{
					this._distanceText = value;
					base.OnPropertyChangedWithValue<string>(value, "DistanceText");
				}
			}
		}

		// Token: 0x17000B45 RID: 2885
		// (get) Token: 0x06002119 RID: 8473 RVA: 0x000780CB File Offset: 0x000762CB
		// (set) Token: 0x0600211A RID: 8474 RVA: 0x000780D3 File Offset: 0x000762D3
		[DataSourceProperty]
		public string InArmyText
		{
			get
			{
				return this._inArmyText;
			}
			set
			{
				if (value != this._inArmyText)
				{
					this._inArmyText = value;
					base.OnPropertyChangedWithValue<string>(value, "InArmyText");
				}
			}
		}

		// Token: 0x17000B46 RID: 2886
		// (get) Token: 0x0600211B RID: 8475 RVA: 0x000780F6 File Offset: 0x000762F6
		// (set) Token: 0x0600211C RID: 8476 RVA: 0x000780FE File Offset: 0x000762FE
		[DataSourceProperty]
		public int Cost
		{
			get
			{
				return this._cost;
			}
			set
			{
				if (value != this._cost)
				{
					this._cost = value;
					base.OnPropertyChangedWithValue(value, "Cost");
					this.UpdateIsCostRelevant();
				}
			}
		}

		// Token: 0x17000B47 RID: 2887
		// (get) Token: 0x0600211D RID: 8477 RVA: 0x00078122 File Offset: 0x00076322
		// (set) Token: 0x0600211E RID: 8478 RVA: 0x0007812A File Offset: 0x0007632A
		[DataSourceProperty]
		public bool IsCostRelevant
		{
			get
			{
				return this._isCostRelevant;
			}
			set
			{
				if (value != this._isCostRelevant)
				{
					this._isCostRelevant = value;
					base.OnPropertyChangedWithValue(value, "IsCostRelevant");
				}
			}
		}

		// Token: 0x17000B48 RID: 2888
		// (get) Token: 0x0600211F RID: 8479 RVA: 0x00078148 File Offset: 0x00076348
		// (set) Token: 0x06002120 RID: 8480 RVA: 0x00078150 File Offset: 0x00076350
		[DataSourceProperty]
		public int Relation
		{
			get
			{
				return this._relation;
			}
			set
			{
				if (value != this._relation)
				{
					this._relation = value;
					base.OnPropertyChangedWithValue(value, "Relation");
				}
			}
		}

		// Token: 0x17000B49 RID: 2889
		// (get) Token: 0x06002121 RID: 8481 RVA: 0x0007816E File Offset: 0x0007636E
		// (set) Token: 0x06002122 RID: 8482 RVA: 0x00078176 File Offset: 0x00076376
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

		// Token: 0x17000B4A RID: 2890
		// (get) Token: 0x06002123 RID: 8483 RVA: 0x00078194 File Offset: 0x00076394
		// (set) Token: 0x06002124 RID: 8484 RVA: 0x0007819C File Offset: 0x0007639C
		[DataSourceProperty]
		public CharacterImageIdentifierVM LordFace
		{
			get
			{
				return this._lordFace;
			}
			set
			{
				if (value != this._lordFace)
				{
					this._lordFace = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "LordFace");
				}
			}
		}

		// Token: 0x17000B4B RID: 2891
		// (get) Token: 0x06002125 RID: 8485 RVA: 0x000781BA File Offset: 0x000763BA
		// (set) Token: 0x06002126 RID: 8486 RVA: 0x000781C2 File Offset: 0x000763C2
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

		// Token: 0x17000B4C RID: 2892
		// (get) Token: 0x06002127 RID: 8487 RVA: 0x000781E5 File Offset: 0x000763E5
		// (set) Token: 0x06002128 RID: 8488 RVA: 0x000781ED File Offset: 0x000763ED
		[DataSourceProperty]
		public bool IsAlreadyWithPlayer
		{
			get
			{
				return this._isAlreadyWithPlayer;
			}
			set
			{
				if (value != this._isAlreadyWithPlayer)
				{
					this._isAlreadyWithPlayer = value;
					base.OnPropertyChangedWithValue(value, "IsAlreadyWithPlayer");
					this.UpdateIsCostRelevant();
				}
			}
		}

		// Token: 0x17000B4D RID: 2893
		// (get) Token: 0x06002129 RID: 8489 RVA: 0x00078211 File Offset: 0x00076411
		// (set) Token: 0x0600212A RID: 8490 RVA: 0x00078219 File Offset: 0x00076419
		[DataSourceProperty]
		public bool IsTransferDisabled
		{
			get
			{
				return this._isTransferDisabled;
			}
			set
			{
				if (value != this._isTransferDisabled)
				{
					this._isTransferDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsTransferDisabled");
				}
			}
		}

		// Token: 0x17000B4E RID: 2894
		// (get) Token: 0x0600212B RID: 8491 RVA: 0x00078237 File Offset: 0x00076437
		// (set) Token: 0x0600212C RID: 8492 RVA: 0x0007823F File Offset: 0x0007643F
		[DataSourceProperty]
		public string LeaderNameText
		{
			get
			{
				return this._leaderNameText;
			}
			set
			{
				if (value != this._leaderNameText)
				{
					this._leaderNameText = value;
					base.OnPropertyChangedWithValue<string>(value, "LeaderNameText");
				}
			}
		}

		// Token: 0x17000B4F RID: 2895
		// (get) Token: 0x0600212D RID: 8493 RVA: 0x00078262 File Offset: 0x00076462
		// (set) Token: 0x0600212E RID: 8494 RVA: 0x0007826A File Offset: 0x0007646A
		[DataSourceProperty]
		public bool IsFocused
		{
			get
			{
				return this._isFocused;
			}
			set
			{
				if (value != this._isFocused)
				{
					this._isFocused = value;
					base.OnPropertyChangedWithValue(value, "IsFocused");
				}
			}
		}

		// Token: 0x04000F54 RID: 3924
		private readonly Action<ArmyManagementItemVM> _onAddToCart;

		// Token: 0x04000F55 RID: 3925
		private readonly Action<ArmyManagementItemVM> _onRemove;

		// Token: 0x04000F56 RID: 3926
		private readonly Action<ArmyManagementItemVM> _onFocus;

		// Token: 0x04000F57 RID: 3927
		public readonly MobileParty Party;

		// Token: 0x04000F58 RID: 3928
		private const float _minimumPartySizeScoreNeeded = 0.4f;

		// Token: 0x04000F59 RID: 3929
		public bool CanJoinBackWithoutCost;

		// Token: 0x04000F5A RID: 3930
		private TextObject _eligibilityReason;

		// Token: 0x04000F5B RID: 3931
		private InputKeyItemVM _removeInputKey;

		// Token: 0x04000F5C RID: 3932
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x04000F5D RID: 3933
		private CharacterImageIdentifierVM _lordFace;

		// Token: 0x04000F5E RID: 3934
		private string _nameText;

		// Token: 0x04000F5F RID: 3935
		private string _inArmyText;

		// Token: 0x04000F60 RID: 3936
		private string _leaderNameText;

		// Token: 0x04000F61 RID: 3937
		private int _relation = -102;

		// Token: 0x04000F62 RID: 3938
		private int _strength = -1;

		// Token: 0x04000F63 RID: 3939
		private int _shipCount = -1;

		// Token: 0x04000F64 RID: 3940
		private bool _hasShip;

		// Token: 0x04000F65 RID: 3941
		private string _distanceText;

		// Token: 0x04000F66 RID: 3942
		private int _cost = -1;

		// Token: 0x04000F67 RID: 3943
		private bool _isCostRelevant;

		// Token: 0x04000F68 RID: 3944
		private bool _isEligible;

		// Token: 0x04000F69 RID: 3945
		private bool _isMainHero;

		// Token: 0x04000F6A RID: 3946
		private bool _isInCart;

		// Token: 0x04000F6B RID: 3947
		private bool _isAlreadyWithPlayer;

		// Token: 0x04000F6C RID: 3948
		private bool _isTransferDisabled;

		// Token: 0x04000F6D RID: 3949
		private bool _isFocused;
	}
}
