using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D3 RID: 211
	[EncyclopediaViewModel(typeof(Hero))]
	public class EncyclopediaHeroPageVM : EncyclopediaContentPageVM
	{
		// Token: 0x060013F3 RID: 5107 RVA: 0x0004F9C0 File Offset: 0x0004DBC0
		public EncyclopediaHeroPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this._hero = base.Obj as Hero;
			this._relationAscendingComparer = new HeroRelationComparer(this._hero, true, true);
			this._relationDescendingComparer = new HeroRelationComparer(this._hero, false, true);
			TextObject infoHiddenReasonText;
			this.IsInformationHidden = CampaignUIHelper.IsHeroInformationHidden(this._hero, out infoHiddenReasonText);
			this._infoHiddenReasonText = infoHiddenReasonText;
			this._allRelatedHeroes = new List<Hero>
			{
				this._hero.Father,
				this._hero.Mother,
				this._hero.Spouse
			};
			this._allRelatedHeroes.AddRange(this._hero.Siblings);
			this._allRelatedHeroes.AddRange(this._hero.ExSpouses);
			this._allRelatedHeroes.AddRange(CampaignUIHelper.GetChildrenAndGrandchildrenOfHero(this._hero));
			StringHelpers.SetCharacterProperties("NPC", this._hero.CharacterObject, null, false);
			this.Settlements = new MBBindingList<EncyclopediaSettlementVM>();
			this.Dwellings = new MBBindingList<EncyclopediaDwellingVM>();
			this.Allies = new MBBindingList<HeroVM>();
			this.AdditionalAllies = new MBBindingList<HeroVM>();
			this.Enemies = new MBBindingList<HeroVM>();
			this.AdditionalEnemies = new MBBindingList<HeroVM>();
			this.Family = new MBBindingList<EncyclopediaFamilyMemberVM>();
			this.Companions = new MBBindingList<HeroVM>();
			this.History = new MBBindingList<EncyclopediaHistoryEventVM>();
			this.Skills = new MBBindingList<EncyclopediaSkillVM>();
			this.Stats = new MBBindingList<StringPairItemVM>();
			this.Traits = new MBBindingList<EncyclopediaTraitItemVM>();
			this.HeroCharacter = new HeroViewModel(CharacterViewModel.StanceTypes.EmphasizeFace);
			base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(this._hero);
			this.Faction = new EncyclopediaFactionVM(this._hero.Clan);
			this.RefreshValues();
		}

		// Token: 0x060013F4 RID: 5108 RVA: 0x0004FB8C File Offset: 0x0004DD8C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ClanText = GameTexts.FindText("str_clan", null).ToString();
			this.AlliesText = GameTexts.FindText("str_friends", null).ToString();
			this.EnemiesText = GameTexts.FindText("str_enemies", null).ToString();
			this.FamilyText = GameTexts.FindText("str_family_group", null).ToString();
			this.CompanionsText = GameTexts.FindText("str_companions", null).ToString();
			this.DwellingsText = GameTexts.FindText("str_dwellings", null).ToString();
			this.SettlementsText = GameTexts.FindText("str_settlements", null).ToString();
			this.DeceasedText = GameTexts.FindText("str_encyclopedia_deceased", null).ToString();
			this.TraitsText = GameTexts.FindText("str_traits_group", null).ToString();
			this.SkillsText = GameTexts.FindText("str_skills", null).ToString();
			this.InfoText = GameTexts.FindText("str_info", null).ToString();
			this.PregnantHint = new HintViewModel(GameTexts.FindText("str_pregnant", null), null);
			base.UpdateBookmarkHintText();
			this.UpdateInformationText();
			this.Refresh();
		}

		// Token: 0x060013F5 RID: 5109 RVA: 0x0004FCBC File Offset: 0x0004DEBC
		public override void Refresh()
		{
			base.IsLoadingOver = false;
			this.Settlements.Clear();
			this.Dwellings.Clear();
			this.Allies.Clear();
			this.Enemies.Clear();
			this.AdditionalAllies.Clear();
			this.AdditionalEnemies.Clear();
			this.Companions.Clear();
			this.Family.Clear();
			this.History.Clear();
			this.Skills.Clear();
			this.Stats.Clear();
			this.Traits.Clear();
			this.NameText = this._hero.Name.ToString();
			string text = GameTexts.FindText("str_missing_info_indicator", null).ToString();
			EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
			this.HasNeutralClan = this._hero.Clan == null;
			if (!this.IsInformationHidden)
			{
				List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList<SkillObject>();
				list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
				foreach (SkillObject skill in list)
				{
					if (this._hero.GetSkillValue(skill) >= 50)
					{
						this.Skills.Add(new EncyclopediaSkillVM(skill, this._hero.GetSkillValue(skill)));
					}
				}
				foreach (TraitObject traitObject in CampaignUIHelper.GetHeroTraits())
				{
					if (this._hero.GetTraitLevel(traitObject) != 0)
					{
						this.Traits.Add(new EncyclopediaTraitItemVM(traitObject, this._hero));
					}
				}
				if (this._hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
				{
					for (int i = 0; i < Hero.AllAliveHeroes.Count; i++)
					{
						this.AddHeroToRelatedVMList(Hero.AllAliveHeroes[i]);
					}
					for (int j = 0; j < Hero.DeadOrDisabledHeroes.Count; j++)
					{
						this.AddHeroToRelatedVMList(Hero.DeadOrDisabledHeroes[j]);
					}
					this.Allies.Sort(this._relationDescendingComparer);
					this.Enemies.Sort(this._relationAscendingComparer);
					while (this.Allies.Count > 13)
					{
						HeroVM item = this.Allies[13];
						this.Allies.Remove(item);
						this.AdditionalAllies.Add(item);
					}
					while (this.Enemies.Count > 13)
					{
						HeroVM item2 = this.Enemies[13];
						this.Enemies.Remove(item2);
						this.AdditionalEnemies.Add(item2);
					}
					this.OnAdditionalListsUpdated();
				}
				if (this._hero.Clan != null && this._hero == this._hero.Clan.Leader)
				{
					for (int k = 0; k < this._hero.Clan.Companions.Count; k++)
					{
						Hero hero = this._hero.Clan.Companions[k];
						this.Companions.Add(new HeroVM(hero, false));
					}
				}
				for (int l = 0; l < this._allRelatedHeroes.Count; l++)
				{
					Hero hero2 = this._allRelatedHeroes[l];
					if (hero2 != null && pageOf.IsValidEncyclopediaItem(hero2))
					{
						this.Family.Add(new EncyclopediaFamilyMemberVM(hero2, this._hero));
					}
				}
				for (int m = 0; m < this._hero.OwnedWorkshops.Count; m++)
				{
					this.Dwellings.Add(new EncyclopediaDwellingVM(this._hero.OwnedWorkshops[m].WorkshopType));
				}
				EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
				for (int n = 0; n < Settlement.All.Count; n++)
				{
					Settlement settlement = Settlement.All[n];
					if (settlement.OwnerClan != null && settlement.OwnerClan.Leader == this._hero && pageOf2.IsValidEncyclopediaItem(settlement))
					{
						this.Settlements.Add(new EncyclopediaSettlementVM(settlement));
					}
				}
			}
			this.HasAnySkills = this.Skills.Count > 0;
			if (this._hero.Culture != null)
			{
				string definition = GameTexts.FindText("str_enc_sf_culture", null).ToString();
				this.Stats.Add(new StringPairItemVM(definition, this._hero.Culture.Name.ToString(), null));
			}
			string definition2 = GameTexts.FindText("str_enc_sf_age", null).ToString();
			this.Stats.Add(new StringPairItemVM(definition2, this.IsInformationHidden ? text : ((int)this._hero.Age).ToString(), null));
			for (int num = Campaign.Current.LogEntryHistory.GameActionLogs.Count - 1; num >= 0; num--)
			{
				IEncyclopediaLog encyclopediaLog;
				if ((encyclopediaLog = Campaign.Current.LogEntryHistory.GameActionLogs[num] as IEncyclopediaLog) != null && encyclopediaLog.IsVisibleInEncyclopediaPageOf<Hero>(this._hero))
				{
					this.History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
				}
			}
			if (!this._hero.IsNotable && !this._hero.IsWanderer)
			{
				Clan clan = this._hero.Clan;
				if (((clan != null) ? clan.Kingdom : null) != null)
				{
					this.KingdomRankText = CampaignUIHelper.GetHeroKingdomRank(this._hero);
				}
			}
			string heroOccupationName = CampaignUIHelper.GetHeroOccupationName(this._hero);
			if (!string.IsNullOrEmpty(heroOccupationName))
			{
				string definition3 = GameTexts.FindText("str_enc_sf_occupation", null).ToString();
				this.Stats.Add(new StringPairItemVM(definition3, this.IsInformationHidden ? text : heroOccupationName, null));
			}
			if (this._hero != Hero.MainHero)
			{
				string definition4 = GameTexts.FindText("str_enc_sf_relation", null).ToString();
				this.Stats.Add(new StringPairItemVM(definition4, this.IsInformationHidden ? text : this._hero.GetRelationWithPlayer().ToString(), null));
			}
			this.LastSeenText = ((this._hero == Hero.MainHero) ? "" : HeroHelper.GetLastSeenText(this._hero).ToString());
			this.HeroCharacter.FillFrom(this._hero, -1, this._hero.IsNotable, true);
			this.HeroCharacter.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
			this.HeroCharacter.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
			this.HeroCharacter.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
			this.IsCompanion = this._hero.CompanionOf != null;
			if (this.IsCompanion)
			{
				this.MasterText = GameTexts.FindText("str_companion_of", null).ToString();
				Clan companionOf = this._hero.CompanionOf;
				this.Master = new HeroVM((companionOf != null) ? companionOf.Leader : null, false);
			}
			this.IsPregnant = this._hero.IsPregnant;
			this.IsDead = !this._hero.IsAlive;
			base.IsLoadingOver = true;
		}

		// Token: 0x060013F6 RID: 5110 RVA: 0x00050420 File Offset: 0x0004E620
		private void AddHeroToRelatedVMList(Hero hero)
		{
			if (!Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero)).IsValidEncyclopediaItem(hero) || hero.IsNotable)
			{
				return;
			}
			if (hero != this._hero && hero.IsAlive && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && !this._allRelatedHeroes.Contains(hero))
			{
				if (this._hero.IsFriend(hero))
				{
					this.Allies.Add(new HeroVM(hero, false));
					return;
				}
				if (this._hero.IsEnemy(hero))
				{
					this.Enemies.Add(new HeroVM(hero, false));
				}
			}
		}

		// Token: 0x060013F7 RID: 5111 RVA: 0x000504D4 File Offset: 0x0004E6D4
		public override string GetName()
		{
			return this._hero.Name.ToString();
		}

		// Token: 0x060013F8 RID: 5112 RVA: 0x000504E8 File Offset: 0x0004E6E8
		public override string GetNavigationBarURL()
		{
			return HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ " + HyperlinkTexts.GetGenericHyperlinkText("ListPage-Heroes", GameTexts.FindText("str_encyclopedia_heroes", null).ToString()) + " \\ " + this.GetName();
		}

		// Token: 0x060013F9 RID: 5113 RVA: 0x0005054D File Offset: 0x0004E74D
		public void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x060013FA RID: 5114 RVA: 0x00050560 File Offset: 0x0004E760
		public override void ExecuteSwitchBookmarkedState()
		{
			base.ExecuteSwitchBookmarkedState();
			if (base.IsBookmarked)
			{
				Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(this._hero);
				return;
			}
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(this._hero);
		}

		// Token: 0x060013FB RID: 5115 RVA: 0x000505B0 File Offset: 0x0004E7B0
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.HeroCharacter.OnFinalize();
		}

		// Token: 0x060013FC RID: 5116 RVA: 0x000505C4 File Offset: 0x0004E7C4
		private void UpdateInformationText()
		{
			this.InformationText = "";
			if (!TextObject.IsNullOrEmpty(this._hero.EncyclopediaText))
			{
				this.InformationText = this._hero.EncyclopediaText.ToString();
				return;
			}
			if (this._hero.CharacterObject.Occupation == Occupation.Lord)
			{
				this.InformationText = Hero.SetHeroEncyclopediaTextAndLinks(this._hero).ToString();
			}
		}

		// Token: 0x060013FD RID: 5117 RVA: 0x00050630 File Offset: 0x0004E830
		private void OnAdditionalListsUpdated()
		{
			this.AnyAdditionalAllies = this.AdditionalAllies.Count > 0;
			this.AnyAdditionalEnemies = this.AdditionalEnemies.Count > 0;
			this.AdditionalAlliesString = (this.AnyAdditionalAllies ? new TextObject("{=!}+{REMAINING}", null).SetTextVariable("REMAINING", this.AdditionalAllies.Count).ToString() : string.Empty);
			this.AdditionalEnemiesString = (this.AnyAdditionalEnemies ? new TextObject("{=!}+{REMAINING}", null).SetTextVariable("REMAINING", this.AdditionalEnemies.Count).ToString() : string.Empty);
			this.AdditionalAlliesHint = new BasicTooltipViewModel(() => this.GetOverflowTooltip(this.AdditionalAllies));
			this.AdditionalEnemiesHint = new BasicTooltipViewModel(() => this.GetOverflowTooltip(this.AdditionalEnemies));
		}

		// Token: 0x060013FE RID: 5118 RVA: 0x00050708 File Offset: 0x0004E908
		private List<TooltipProperty> GetOverflowTooltip(MBBindingList<HeroVM> overflowList)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			foreach (HeroVM heroVM in overflowList)
			{
				list.Add(new TooltipProperty(string.Empty, heroVM.NameText, 0, false, TooltipProperty.TooltipPropertyFlags.None));
			}
			return list;
		}

		// Token: 0x17000691 RID: 1681
		// (get) Token: 0x060013FF RID: 5119 RVA: 0x0005076C File Offset: 0x0004E96C
		// (set) Token: 0x06001400 RID: 5120 RVA: 0x00050774 File Offset: 0x0004E974
		[DataSourceProperty]
		public EncyclopediaFactionVM Faction
		{
			get
			{
				return this._faction;
			}
			set
			{
				if (value != this._faction)
				{
					this._faction = value;
					base.OnPropertyChangedWithValue<EncyclopediaFactionVM>(value, "Faction");
				}
			}
		}

		// Token: 0x17000692 RID: 1682
		// (get) Token: 0x06001401 RID: 5121 RVA: 0x00050792 File Offset: 0x0004E992
		// (set) Token: 0x06001402 RID: 5122 RVA: 0x0005079A File Offset: 0x0004E99A
		[DataSourceProperty]
		public bool IsCompanion
		{
			get
			{
				return this._isCompanion;
			}
			set
			{
				if (value != this._isCompanion)
				{
					this._isCompanion = value;
					base.OnPropertyChangedWithValue(value, "IsCompanion");
				}
			}
		}

		// Token: 0x17000693 RID: 1683
		// (get) Token: 0x06001403 RID: 5123 RVA: 0x000507B8 File Offset: 0x0004E9B8
		// (set) Token: 0x06001404 RID: 5124 RVA: 0x000507C0 File Offset: 0x0004E9C0
		[DataSourceProperty]
		public bool IsPregnant
		{
			get
			{
				return this._isPregnant;
			}
			set
			{
				if (value != this._isPregnant)
				{
					this._isPregnant = value;
					base.OnPropertyChangedWithValue(value, "IsPregnant");
				}
			}
		}

		// Token: 0x17000694 RID: 1684
		// (get) Token: 0x06001405 RID: 5125 RVA: 0x000507DE File Offset: 0x0004E9DE
		// (set) Token: 0x06001406 RID: 5126 RVA: 0x000507E6 File Offset: 0x0004E9E6
		[DataSourceProperty]
		public HeroVM Master
		{
			get
			{
				return this._master;
			}
			set
			{
				if (value != this._master)
				{
					this._master = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Master");
				}
			}
		}

		// Token: 0x17000695 RID: 1685
		// (get) Token: 0x06001407 RID: 5127 RVA: 0x00050804 File Offset: 0x0004EA04
		// (set) Token: 0x06001408 RID: 5128 RVA: 0x0005080C File Offset: 0x0004EA0C
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

		// Token: 0x17000696 RID: 1686
		// (get) Token: 0x06001409 RID: 5129 RVA: 0x0005082F File Offset: 0x0004EA2F
		// (set) Token: 0x0600140A RID: 5130 RVA: 0x00050837 File Offset: 0x0004EA37
		[DataSourceProperty]
		public string InfoText
		{
			get
			{
				return this._infoText;
			}
			set
			{
				if (value != this._infoText)
				{
					this._infoText = value;
					base.OnPropertyChangedWithValue<string>(value, "InfoText");
				}
			}
		}

		// Token: 0x17000697 RID: 1687
		// (get) Token: 0x0600140B RID: 5131 RVA: 0x0005085A File Offset: 0x0004EA5A
		// (set) Token: 0x0600140C RID: 5132 RVA: 0x00050862 File Offset: 0x0004EA62
		[DataSourceProperty]
		public string TraitsText
		{
			get
			{
				return this._traitsText;
			}
			set
			{
				if (value != this._traitsText)
				{
					this._traitsText = value;
					base.OnPropertyChangedWithValue<string>(value, "TraitsText");
				}
			}
		}

		// Token: 0x17000698 RID: 1688
		// (get) Token: 0x0600140D RID: 5133 RVA: 0x00050885 File Offset: 0x0004EA85
		// (set) Token: 0x0600140E RID: 5134 RVA: 0x0005088D File Offset: 0x0004EA8D
		[DataSourceProperty]
		public string MasterText
		{
			get
			{
				return this._masterText;
			}
			set
			{
				if (value != this._masterText)
				{
					this._masterText = value;
					base.OnPropertyChangedWithValue<string>(value, "MasterText");
				}
			}
		}

		// Token: 0x17000699 RID: 1689
		// (get) Token: 0x0600140F RID: 5135 RVA: 0x000508B0 File Offset: 0x0004EAB0
		// (set) Token: 0x06001410 RID: 5136 RVA: 0x000508B8 File Offset: 0x0004EAB8
		[DataSourceProperty]
		public string KingdomRankText
		{
			get
			{
				return this._kingdomRankText;
			}
			set
			{
				if (value != this._kingdomRankText)
				{
					this._kingdomRankText = value;
					base.OnPropertyChangedWithValue<string>(value, "KingdomRankText");
				}
			}
		}

		// Token: 0x1700069A RID: 1690
		// (get) Token: 0x06001411 RID: 5137 RVA: 0x000508DB File Offset: 0x0004EADB
		[DataSourceProperty]
		public string InfoHiddenReasonText
		{
			get
			{
				return this._infoHiddenReasonText.ToString();
			}
		}

		// Token: 0x1700069B RID: 1691
		// (get) Token: 0x06001412 RID: 5138 RVA: 0x000508E8 File Offset: 0x0004EAE8
		// (set) Token: 0x06001413 RID: 5139 RVA: 0x000508F0 File Offset: 0x0004EAF0
		[DataSourceProperty]
		public string SkillsText
		{
			get
			{
				return this._skillsText;
			}
			set
			{
				if (value != this._skillsText)
				{
					this._skillsText = value;
					base.OnPropertyChangedWithValue<string>(value, "SkillsText");
				}
			}
		}

		// Token: 0x1700069C RID: 1692
		// (get) Token: 0x06001414 RID: 5140 RVA: 0x00050913 File Offset: 0x0004EB13
		// (set) Token: 0x06001415 RID: 5141 RVA: 0x0005091B File Offset: 0x0004EB1B
		[DataSourceProperty]
		public HeroViewModel HeroCharacter
		{
			get
			{
				return this._heroCharacter;
			}
			set
			{
				if (value != this._heroCharacter)
				{
					this._heroCharacter = value;
					base.OnPropertyChangedWithValue<HeroViewModel>(value, "HeroCharacter");
				}
			}
		}

		// Token: 0x1700069D RID: 1693
		// (get) Token: 0x06001416 RID: 5142 RVA: 0x00050939 File Offset: 0x0004EB39
		// (set) Token: 0x06001417 RID: 5143 RVA: 0x00050941 File Offset: 0x0004EB41
		[DataSourceProperty]
		public string LastSeenText
		{
			get
			{
				return this._lastSeenText;
			}
			set
			{
				if (value != this._lastSeenText)
				{
					this._lastSeenText = value;
					base.OnPropertyChangedWithValue<string>(value, "LastSeenText");
				}
			}
		}

		// Token: 0x1700069E RID: 1694
		// (get) Token: 0x06001418 RID: 5144 RVA: 0x00050964 File Offset: 0x0004EB64
		// (set) Token: 0x06001419 RID: 5145 RVA: 0x0005096C File Offset: 0x0004EB6C
		[DataSourceProperty]
		public string DeceasedText
		{
			get
			{
				return this._deceasedText;
			}
			set
			{
				if (value != this._deceasedText)
				{
					this._deceasedText = value;
					base.OnPropertyChangedWithValue<string>(value, "DeceasedText");
				}
			}
		}

		// Token: 0x1700069F RID: 1695
		// (get) Token: 0x0600141A RID: 5146 RVA: 0x0005098F File Offset: 0x0004EB8F
		// (set) Token: 0x0600141B RID: 5147 RVA: 0x00050997 File Offset: 0x0004EB97
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

		// Token: 0x170006A0 RID: 1696
		// (get) Token: 0x0600141C RID: 5148 RVA: 0x000509BA File Offset: 0x0004EBBA
		// (set) Token: 0x0600141D RID: 5149 RVA: 0x000509C2 File Offset: 0x0004EBC2
		[DataSourceProperty]
		public string SettlementsText
		{
			get
			{
				return this._settlementsText;
			}
			set
			{
				if (value != this._settlementsText)
				{
					this._settlementsText = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementsText");
				}
			}
		}

		// Token: 0x170006A1 RID: 1697
		// (get) Token: 0x0600141E RID: 5150 RVA: 0x000509E5 File Offset: 0x0004EBE5
		// (set) Token: 0x0600141F RID: 5151 RVA: 0x000509ED File Offset: 0x0004EBED
		[DataSourceProperty]
		public string DwellingsText
		{
			get
			{
				return this._dwellingsText;
			}
			set
			{
				if (value != this._dwellingsText)
				{
					this._dwellingsText = value;
					base.OnPropertyChangedWithValue<string>(value, "DwellingsText");
				}
			}
		}

		// Token: 0x170006A2 RID: 1698
		// (get) Token: 0x06001420 RID: 5152 RVA: 0x00050A10 File Offset: 0x0004EC10
		// (set) Token: 0x06001421 RID: 5153 RVA: 0x00050A18 File Offset: 0x0004EC18
		[DataSourceProperty]
		public string CompanionsText
		{
			get
			{
				return this._companionsText;
			}
			set
			{
				if (value != this._companionsText)
				{
					this._companionsText = value;
					base.OnPropertyChangedWithValue<string>(value, "CompanionsText");
				}
			}
		}

		// Token: 0x170006A3 RID: 1699
		// (get) Token: 0x06001422 RID: 5154 RVA: 0x00050A3B File Offset: 0x0004EC3B
		// (set) Token: 0x06001423 RID: 5155 RVA: 0x00050A43 File Offset: 0x0004EC43
		[DataSourceProperty]
		public string AlliesText
		{
			get
			{
				return this._alliesText;
			}
			set
			{
				if (value != this._alliesText)
				{
					this._alliesText = value;
					base.OnPropertyChangedWithValue<string>(value, "AlliesText");
				}
			}
		}

		// Token: 0x170006A4 RID: 1700
		// (get) Token: 0x06001424 RID: 5156 RVA: 0x00050A66 File Offset: 0x0004EC66
		// (set) Token: 0x06001425 RID: 5157 RVA: 0x00050A6E File Offset: 0x0004EC6E
		[DataSourceProperty]
		public string EnemiesText
		{
			get
			{
				return this._enemiesText;
			}
			set
			{
				if (value != this._enemiesText)
				{
					this._enemiesText = value;
					base.OnPropertyChangedWithValue<string>(value, "EnemiesText");
				}
			}
		}

		// Token: 0x170006A5 RID: 1701
		// (get) Token: 0x06001426 RID: 5158 RVA: 0x00050A91 File Offset: 0x0004EC91
		// (set) Token: 0x06001427 RID: 5159 RVA: 0x00050A99 File Offset: 0x0004EC99
		[DataSourceProperty]
		public string FamilyText
		{
			get
			{
				return this._familyText;
			}
			set
			{
				if (value != this._familyText)
				{
					this._familyText = value;
					base.OnPropertyChangedWithValue<string>(value, "FamilyText");
				}
			}
		}

		// Token: 0x170006A6 RID: 1702
		// (get) Token: 0x06001428 RID: 5160 RVA: 0x00050ABC File Offset: 0x0004ECBC
		// (set) Token: 0x06001429 RID: 5161 RVA: 0x00050AC4 File Offset: 0x0004ECC4
		[DataSourceProperty]
		public MBBindingList<StringPairItemVM> Stats
		{
			get
			{
				return this._stats;
			}
			set
			{
				if (value != this._stats)
				{
					this._stats = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringPairItemVM>>(value, "Stats");
				}
			}
		}

		// Token: 0x170006A7 RID: 1703
		// (get) Token: 0x0600142A RID: 5162 RVA: 0x00050AE2 File Offset: 0x0004ECE2
		// (set) Token: 0x0600142B RID: 5163 RVA: 0x00050AEA File Offset: 0x0004ECEA
		[DataSourceProperty]
		public MBBindingList<EncyclopediaTraitItemVM> Traits
		{
			get
			{
				return this._traits;
			}
			set
			{
				if (value != this._traits)
				{
					this._traits = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaTraitItemVM>>(value, "Traits");
				}
			}
		}

		// Token: 0x170006A8 RID: 1704
		// (get) Token: 0x0600142C RID: 5164 RVA: 0x00050B08 File Offset: 0x0004ED08
		// (set) Token: 0x0600142D RID: 5165 RVA: 0x00050B10 File Offset: 0x0004ED10
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSkillVM> Skills
		{
			get
			{
				return this._skills;
			}
			set
			{
				if (value != this._skills)
				{
					this._skills = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSkillVM>>(value, "Skills");
				}
			}
		}

		// Token: 0x170006A9 RID: 1705
		// (get) Token: 0x0600142E RID: 5166 RVA: 0x00050B2E File Offset: 0x0004ED2E
		// (set) Token: 0x0600142F RID: 5167 RVA: 0x00050B36 File Offset: 0x0004ED36
		[DataSourceProperty]
		public MBBindingList<EncyclopediaDwellingVM> Dwellings
		{
			get
			{
				return this._dwellings;
			}
			set
			{
				if (value != this._dwellings)
				{
					this._dwellings = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaDwellingVM>>(value, "Dwellings");
				}
			}
		}

		// Token: 0x170006AA RID: 1706
		// (get) Token: 0x06001430 RID: 5168 RVA: 0x00050B54 File Offset: 0x0004ED54
		// (set) Token: 0x06001431 RID: 5169 RVA: 0x00050B5C File Offset: 0x0004ED5C
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
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSettlementVM>>(value, "Settlements");
				}
			}
		}

		// Token: 0x170006AB RID: 1707
		// (get) Token: 0x06001432 RID: 5170 RVA: 0x00050B7A File Offset: 0x0004ED7A
		// (set) Token: 0x06001433 RID: 5171 RVA: 0x00050B82 File Offset: 0x0004ED82
		[DataSourceProperty]
		public MBBindingList<EncyclopediaFamilyMemberVM> Family
		{
			get
			{
				return this._family;
			}
			set
			{
				if (value != this._family)
				{
					this._family = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaFamilyMemberVM>>(value, "Family");
				}
			}
		}

		// Token: 0x170006AC RID: 1708
		// (get) Token: 0x06001434 RID: 5172 RVA: 0x00050BA0 File Offset: 0x0004EDA0
		// (set) Token: 0x06001435 RID: 5173 RVA: 0x00050BA8 File Offset: 0x0004EDA8
		[DataSourceProperty]
		public MBBindingList<HeroVM> Companions
		{
			get
			{
				return this._companions;
			}
			set
			{
				if (value != this._companions)
				{
					this._companions = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "Companions");
				}
			}
		}

		// Token: 0x170006AD RID: 1709
		// (get) Token: 0x06001436 RID: 5174 RVA: 0x00050BC6 File Offset: 0x0004EDC6
		// (set) Token: 0x06001437 RID: 5175 RVA: 0x00050BCE File Offset: 0x0004EDCE
		[DataSourceProperty]
		public MBBindingList<HeroVM> Enemies
		{
			get
			{
				return this._enemies;
			}
			set
			{
				if (value != this._enemies)
				{
					this._enemies = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "Enemies");
				}
			}
		}

		// Token: 0x170006AE RID: 1710
		// (get) Token: 0x06001438 RID: 5176 RVA: 0x00050BEC File Offset: 0x0004EDEC
		// (set) Token: 0x06001439 RID: 5177 RVA: 0x00050BF4 File Offset: 0x0004EDF4
		[DataSourceProperty]
		public MBBindingList<HeroVM> Allies
		{
			get
			{
				return this._allies;
			}
			set
			{
				if (value != this._allies)
				{
					this._allies = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "Allies");
				}
			}
		}

		// Token: 0x170006AF RID: 1711
		// (get) Token: 0x0600143A RID: 5178 RVA: 0x00050C12 File Offset: 0x0004EE12
		// (set) Token: 0x0600143B RID: 5179 RVA: 0x00050C1A File Offset: 0x0004EE1A
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

		// Token: 0x170006B0 RID: 1712
		// (get) Token: 0x0600143C RID: 5180 RVA: 0x00050C38 File Offset: 0x0004EE38
		// (set) Token: 0x0600143D RID: 5181 RVA: 0x00050C40 File Offset: 0x0004EE40
		[DataSourceProperty]
		public bool HasNeutralClan
		{
			get
			{
				return this._hasNeutralClan;
			}
			set
			{
				if (value != this._hasNeutralClan)
				{
					this._hasNeutralClan = value;
					base.OnPropertyChangedWithValue(value, "HasNeutralClan");
				}
			}
		}

		// Token: 0x170006B1 RID: 1713
		// (get) Token: 0x0600143E RID: 5182 RVA: 0x00050C5E File Offset: 0x0004EE5E
		// (set) Token: 0x0600143F RID: 5183 RVA: 0x00050C66 File Offset: 0x0004EE66
		[DataSourceProperty]
		public bool IsDead
		{
			get
			{
				return this._isDead;
			}
			set
			{
				if (value != this._isDead)
				{
					this._isDead = value;
					base.OnPropertyChanged("IsAlive");
					base.OnPropertyChangedWithValue(value, "IsDead");
				}
			}
		}

		// Token: 0x170006B2 RID: 1714
		// (get) Token: 0x06001440 RID: 5184 RVA: 0x00050C8F File Offset: 0x0004EE8F
		// (set) Token: 0x06001441 RID: 5185 RVA: 0x00050C97 File Offset: 0x0004EE97
		[DataSourceProperty]
		public bool IsInformationHidden
		{
			get
			{
				return this._isInformationHidden;
			}
			set
			{
				if (value != this._isInformationHidden)
				{
					this._isInformationHidden = value;
					base.OnPropertyChangedWithValue(value, "IsInformationHidden");
				}
			}
		}

		// Token: 0x170006B3 RID: 1715
		// (get) Token: 0x06001442 RID: 5186 RVA: 0x00050CB5 File Offset: 0x0004EEB5
		// (set) Token: 0x06001443 RID: 5187 RVA: 0x00050CBD File Offset: 0x0004EEBD
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

		// Token: 0x170006B4 RID: 1716
		// (get) Token: 0x06001444 RID: 5188 RVA: 0x00050CE0 File Offset: 0x0004EEE0
		// (set) Token: 0x06001445 RID: 5189 RVA: 0x00050CE8 File Offset: 0x0004EEE8
		[DataSourceProperty]
		public HintViewModel PregnantHint
		{
			get
			{
				return this._pregnantHint;
			}
			set
			{
				if (value != this._pregnantHint)
				{
					this._pregnantHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PregnantHint");
				}
			}
		}

		// Token: 0x170006B5 RID: 1717
		// (get) Token: 0x06001446 RID: 5190 RVA: 0x00050D06 File Offset: 0x0004EF06
		// (set) Token: 0x06001447 RID: 5191 RVA: 0x00050D0E File Offset: 0x0004EF0E
		[DataSourceProperty]
		public bool HasAnySkills
		{
			get
			{
				return this._hasAnySkills;
			}
			set
			{
				if (value != this._hasAnySkills)
				{
					this._hasAnySkills = value;
					base.OnPropertyChangedWithValue(value, "HasAnySkills");
				}
			}
		}

		// Token: 0x170006B6 RID: 1718
		// (get) Token: 0x06001448 RID: 5192 RVA: 0x00050D2C File Offset: 0x0004EF2C
		// (set) Token: 0x06001449 RID: 5193 RVA: 0x00050D34 File Offset: 0x0004EF34
		[DataSourceProperty]
		public MBBindingList<HeroVM> AdditionalEnemies
		{
			get
			{
				return this._additionalEnemies;
			}
			set
			{
				if (value != this._additionalEnemies)
				{
					this._additionalEnemies = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "AdditionalEnemies");
				}
			}
		}

		// Token: 0x170006B7 RID: 1719
		// (get) Token: 0x0600144A RID: 5194 RVA: 0x00050D52 File Offset: 0x0004EF52
		// (set) Token: 0x0600144B RID: 5195 RVA: 0x00050D5A File Offset: 0x0004EF5A
		[DataSourceProperty]
		public MBBindingList<HeroVM> AdditionalAllies
		{
			get
			{
				return this._additionalAllies;
			}
			set
			{
				if (value != this._additionalAllies)
				{
					this._additionalAllies = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "AdditionalAllies");
				}
			}
		}

		// Token: 0x170006B8 RID: 1720
		// (get) Token: 0x0600144C RID: 5196 RVA: 0x00050D78 File Offset: 0x0004EF78
		// (set) Token: 0x0600144D RID: 5197 RVA: 0x00050D80 File Offset: 0x0004EF80
		[DataSourceProperty]
		public bool AnyAdditionalAllies
		{
			get
			{
				return this._anyAdditionalAllies;
			}
			set
			{
				if (value != this._anyAdditionalAllies)
				{
					this._anyAdditionalAllies = value;
					base.OnPropertyChangedWithValue(value, "AnyAdditionalAllies");
				}
			}
		}

		// Token: 0x170006B9 RID: 1721
		// (get) Token: 0x0600144E RID: 5198 RVA: 0x00050D9E File Offset: 0x0004EF9E
		// (set) Token: 0x0600144F RID: 5199 RVA: 0x00050DA6 File Offset: 0x0004EFA6
		[DataSourceProperty]
		public bool AnyAdditionalEnemies
		{
			get
			{
				return this._anyAdditionalEnemies;
			}
			set
			{
				if (value != this._anyAdditionalEnemies)
				{
					this._anyAdditionalEnemies = value;
					base.OnPropertyChangedWithValue(value, "AnyAdditionalEnemies");
				}
			}
		}

		// Token: 0x170006BA RID: 1722
		// (get) Token: 0x06001450 RID: 5200 RVA: 0x00050DC4 File Offset: 0x0004EFC4
		// (set) Token: 0x06001451 RID: 5201 RVA: 0x00050DCC File Offset: 0x0004EFCC
		[DataSourceProperty]
		public string AdditionalAlliesString
		{
			get
			{
				return this._additionalAlliesString;
			}
			set
			{
				if (value != this._additionalAlliesString)
				{
					this._additionalAlliesString = value;
					base.OnPropertyChangedWithValue<string>(value, "AdditionalAlliesString");
				}
			}
		}

		// Token: 0x170006BB RID: 1723
		// (get) Token: 0x06001452 RID: 5202 RVA: 0x00050DEF File Offset: 0x0004EFEF
		// (set) Token: 0x06001453 RID: 5203 RVA: 0x00050DF7 File Offset: 0x0004EFF7
		[DataSourceProperty]
		public string AdditionalEnemiesString
		{
			get
			{
				return this._additionalEnemiesString;
			}
			set
			{
				if (value != this._additionalEnemiesString)
				{
					this._additionalEnemiesString = value;
					base.OnPropertyChangedWithValue<string>(value, "AdditionalEnemiesString");
				}
			}
		}

		// Token: 0x170006BC RID: 1724
		// (get) Token: 0x06001454 RID: 5204 RVA: 0x00050E1A File Offset: 0x0004F01A
		// (set) Token: 0x06001455 RID: 5205 RVA: 0x00050E22 File Offset: 0x0004F022
		[DataSourceProperty]
		public BasicTooltipViewModel AdditionalAlliesHint
		{
			get
			{
				return this._additionalAlliesHint;
			}
			set
			{
				if (value != this._additionalAlliesHint)
				{
					this._additionalAlliesHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "AdditionalAlliesHint");
				}
			}
		}

		// Token: 0x170006BD RID: 1725
		// (get) Token: 0x06001456 RID: 5206 RVA: 0x00050E40 File Offset: 0x0004F040
		// (set) Token: 0x06001457 RID: 5207 RVA: 0x00050E48 File Offset: 0x0004F048
		[DataSourceProperty]
		public BasicTooltipViewModel AdditionalEnemiesHint
		{
			get
			{
				return this._additionalEnemiesHint;
			}
			set
			{
				if (value != this._additionalEnemiesHint)
				{
					this._additionalEnemiesHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "AdditionalEnemiesHint");
				}
			}
		}

		// Token: 0x04000924 RID: 2340
		private readonly Hero _hero;

		// Token: 0x04000925 RID: 2341
		private readonly TextObject _infoHiddenReasonText;

		// Token: 0x04000926 RID: 2342
		private List<Hero> _allRelatedHeroes;

		// Token: 0x04000927 RID: 2343
		private readonly HeroRelationComparer _relationAscendingComparer;

		// Token: 0x04000928 RID: 2344
		private readonly HeroRelationComparer _relationDescendingComparer;

		// Token: 0x04000929 RID: 2345
		private const int _alliesEnemiesCapacity = 13;

		// Token: 0x0400092A RID: 2346
		private MBBindingList<HeroVM> _enemies;

		// Token: 0x0400092B RID: 2347
		private MBBindingList<HeroVM> _allies;

		// Token: 0x0400092C RID: 2348
		private MBBindingList<EncyclopediaFamilyMemberVM> _family;

		// Token: 0x0400092D RID: 2349
		private MBBindingList<HeroVM> _companions;

		// Token: 0x0400092E RID: 2350
		private MBBindingList<EncyclopediaSettlementVM> _settlements;

		// Token: 0x0400092F RID: 2351
		private MBBindingList<EncyclopediaDwellingVM> _dwellings;

		// Token: 0x04000930 RID: 2352
		private MBBindingList<EncyclopediaHistoryEventVM> _history;

		// Token: 0x04000931 RID: 2353
		private MBBindingList<EncyclopediaSkillVM> _skills;

		// Token: 0x04000932 RID: 2354
		private MBBindingList<StringPairItemVM> _stats;

		// Token: 0x04000933 RID: 2355
		private MBBindingList<EncyclopediaTraitItemVM> _traits;

		// Token: 0x04000934 RID: 2356
		private string _clanText;

		// Token: 0x04000935 RID: 2357
		private string _settlementsText;

		// Token: 0x04000936 RID: 2358
		private string _dwellingsText;

		// Token: 0x04000937 RID: 2359
		private string _alliesText;

		// Token: 0x04000938 RID: 2360
		private string _enemiesText;

		// Token: 0x04000939 RID: 2361
		private string _companionsText;

		// Token: 0x0400093A RID: 2362
		private string _lastSeenText;

		// Token: 0x0400093B RID: 2363
		private string _nameText;

		// Token: 0x0400093C RID: 2364
		private string _informationText;

		// Token: 0x0400093D RID: 2365
		private string _deceasedText;

		// Token: 0x0400093E RID: 2366
		private string _traitsText;

		// Token: 0x0400093F RID: 2367
		private string _skillsText;

		// Token: 0x04000940 RID: 2368
		private string _infoText;

		// Token: 0x04000941 RID: 2369
		private string _kingdomRankText;

		// Token: 0x04000942 RID: 2370
		private string _familyText;

		// Token: 0x04000943 RID: 2371
		private HeroViewModel _heroCharacter;

		// Token: 0x04000944 RID: 2372
		private bool _isCompanion;

		// Token: 0x04000945 RID: 2373
		private bool _isPregnant;

		// Token: 0x04000946 RID: 2374
		private bool _hasNeutralClan;

		// Token: 0x04000947 RID: 2375
		private bool _isDead;

		// Token: 0x04000948 RID: 2376
		private bool _isInformationHidden;

		// Token: 0x04000949 RID: 2377
		private HeroVM _master;

		// Token: 0x0400094A RID: 2378
		private EncyclopediaFactionVM _faction;

		// Token: 0x0400094B RID: 2379
		private string _masterText;

		// Token: 0x0400094C RID: 2380
		private HintViewModel _pregnantHint;

		// Token: 0x0400094D RID: 2381
		private bool _hasAnySkills;

		// Token: 0x0400094E RID: 2382
		private MBBindingList<HeroVM> _additionalAllies;

		// Token: 0x0400094F RID: 2383
		private MBBindingList<HeroVM> _additionalEnemies;

		// Token: 0x04000950 RID: 2384
		private bool _anyAdditionalAllies;

		// Token: 0x04000951 RID: 2385
		private bool _anyAdditionalEnemies;

		// Token: 0x04000952 RID: 2386
		private string _additionalAlliesString;

		// Token: 0x04000953 RID: 2387
		private string _additionalEnemiesString;

		// Token: 0x04000954 RID: 2388
		private BasicTooltipViewModel _additionalAlliesHint;

		// Token: 0x04000955 RID: 2389
		private BasicTooltipViewModel _additionalEnemiesHint;
	}
}
