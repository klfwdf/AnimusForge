using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D9 RID: 217
	[EncyclopediaViewModel(typeof(ShipHull))]
	public class EncyclopediaShipPageVM : EncyclopediaContentPageVM
	{
		// Token: 0x060014BB RID: 5307 RVA: 0x00051E28 File Offset: 0x00050028
		public EncyclopediaShipPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this._shipHull = base.Obj as ShipHull;
			this._missionShip = MBObjectManager.Instance.GetObject<MissionShipObject>(this._shipHull.MissionShipObjectId);
			this.StatList = new MBBindingList<EncyclopediaShipStatVM>();
			this.AllShipSlots = new MBBindingList<EncyclopediaShipSlotVM>();
			base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(this._shipHull);
			this.SailType = this.GetSailType();
			this.RefreshValues();
		}

		// Token: 0x060014BC RID: 5308 RVA: 0x00051EB0 File Offset: 0x000500B0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NameText = this.GetName();
			this.PrefabId = EncyclopediaShipPageVM.GetPrefabIdOfShipHull(this._shipHull);
			TextObject description = this._shipHull.Description;
			this.DescriptionText = ((description != null) ? description.ToString() : null) ?? "";
			this.AvailableUpgradesText = new TextObject("{=0xN2FaYa}Available Upgrades", null).ToString();
			this.RefreshShipSlots();
			this.RefreshStats();
			base.UpdateBookmarkHintText();
		}

		// Token: 0x060014BD RID: 5309 RVA: 0x00051F30 File Offset: 0x00050130
		private void RefreshShipSlots()
		{
			this.AllShipSlots.Clear();
			using (List<ShipSlot>.Enumerator enumerator = MBObjectManager.Instance.GetObjectTypeList<ShipSlot>().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ShipSlot shipSlot = enumerator.Current;
					this.AllShipSlots.Add(new EncyclopediaShipSlotVM(shipSlot.TypeId, this._shipHull.AvailableSlots.Values.Any((ShipSlot x) => x.TypeId == shipSlot.TypeId)));
				}
			}
			this.AllShipSlots.Add(new EncyclopediaShipSlotVM("figurehead", this._shipHull.CanEquipFigurehead));
		}

		// Token: 0x060014BE RID: 5310 RVA: 0x00051FF4 File Offset: 0x000501F4
		private void RefreshStats()
		{
			this.StatList.Clear();
			this.StatsText = new TextObject("{=ffjTMejn}Stats", null).ToString();
			this.StatList.Add(new EncyclopediaShipStatVM("hull", new TextObject("{=wEmx6fZi}Hull", null), this._shipHull.Name.ToString(), null));
			this.StatList.Add(new EncyclopediaShipStatVM("class", new TextObject("{=sqdzHOPe}Class", null), GameTexts.FindText("str_ship_type", this._shipHull.Type.ToString().ToLowerInvariant()).ToString(), null));
			this.StatList.Add(new EncyclopediaShipStatVM("crew", new TextObject("{=wXCM8BnW}Crew", null), this.GetCrewCapacityStr(), new Func<List<TooltipProperty>>(this.GetCrewCapacityTooltip)));
			this.StatList.Add(new EncyclopediaShipStatVM("cargo_capacity", new TextObject("{=IE1KbkaH}Cargo Capacity", null), this._shipHull.InventoryCapacity.ToString(), null));
			this.StatList.Add(new EncyclopediaShipStatVM("weight", new TextObject("{=4Dd2xgPm}Weight", null), this._missionShip.Mass.ToString("0"), null));
			this.StatList.Add(new EncyclopediaShipStatVM("travel_speed", new TextObject("{=DbERaPfF}Travel Speed", null), this._shipHull.BaseSpeed.ToString("0.##"), null));
			this.SailTypeStat = new EncyclopediaShipStatVM("sail_type", new TextObject("{=PJyFY05L}Sail", null), this.GetSailTypeDescription(), null);
			this.StatList.Add(this.SailTypeStat);
			this.StatList.Add(new EncyclopediaShipStatVM("draft_type", new TextObject("{=I4bu7cLr}Draft", null), this.GetDraftTypeStr(), null));
			this.StatList.Add(new EncyclopediaShipStatVM("sea_worthiness", new TextObject("{=yCzuXN3O}Seaworthiness", null), this._shipHull.SeaWorthiness.ToString(), null));
			this.StatList.Add(new EncyclopediaShipStatVM("hit_points", new TextObject("{=oBbiVeKE}Hit Points", null), this._shipHull.MaxHitPoints.ToString(), null));
		}

		// Token: 0x060014BF RID: 5311 RVA: 0x0005223C File Offset: 0x0005043C
		private string GetSailType()
		{
			if (this._missionShip.HasSails)
			{
				bool flag = this._missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Lateen);
				bool flag2 = this._missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Square);
				if (flag && flag2)
				{
					return "Hybrid";
				}
				if (flag)
				{
					return "Lateen";
				}
				if (flag2)
				{
					return "Square";
				}
			}
			return "None";
		}

		// Token: 0x060014C0 RID: 5312 RVA: 0x000522D8 File Offset: 0x000504D8
		private string GetSailTypeDescription()
		{
			if (this._missionShip.HasSails)
			{
				bool flag = this._missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Lateen);
				bool flag2 = this._missionShip.Sails.Any((ShipSail x) => x.Type == TaleWorlds.Core.SailType.Square);
				if (flag && flag2)
				{
					return new TextObject("{=bXJLb0BE}Hybrid", null).ToString();
				}
				if (flag)
				{
					return new TextObject("{=kNxD2oer}Lateen", null).ToString();
				}
				if (flag2)
				{
					return new TextObject("{=E3tCWX7w}Square", null).ToString();
				}
			}
			return new TextObject("{=koX9okuG}None", null).ToString();
		}

		// Token: 0x060014C1 RID: 5313 RVA: 0x000523A3 File Offset: 0x000505A3
		private string GetDraftTypeStr()
		{
			if (!this._shipHull.HasHold)
			{
				return new TextObject("{=ShipDraftTypeShallow}Shallow", null).ToString();
			}
			return new TextObject("{=ShipDraftTypeDeep}Deep", null).ToString();
		}

		// Token: 0x060014C2 RID: 5314 RVA: 0x000523D4 File Offset: 0x000505D4
		private string GetCrewCapacityStr()
		{
			int skeletalCrewCapacity = this._shipHull.SkeletalCrewCapacity;
			int mainDeckCrewCapacity = this._shipHull.MainDeckCrewCapacity;
			int num = this._shipHull.TotalCrewCapacity - this._shipHull.MainDeckCrewCapacity;
			TextObject textObject;
			if (num > 0)
			{
				textObject = new TextObject("{=!}{SKELETAL} • {DECK} + {RESERVE}", null);
			}
			else
			{
				textObject = new TextObject("{=!}{SKELETAL} • {DECK}", null);
			}
			return textObject.SetTextVariable("SKELETAL", skeletalCrewCapacity).SetTextVariable("DECK", mainDeckCrewCapacity).SetTextVariable("RESERVE", num)
				.ToString();
		}

		// Token: 0x060014C3 RID: 5315 RVA: 0x00052458 File Offset: 0x00050658
		private List<TooltipProperty> GetCrewCapacityTooltip()
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			int skeletalCrewCapacity = this._shipHull.SkeletalCrewCapacity;
			int mainDeckCrewCapacity = this._shipHull.MainDeckCrewCapacity;
			int totalCrewCapacity = this._shipHull.TotalCrewCapacity;
			int num = totalCrewCapacity - mainDeckCrewCapacity;
			list.Add(new TooltipProperty(new TextObject("{=kalMphFt}Skeletal Capacity", null).ToString(), skeletalCrewCapacity.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewskeletal").ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
			list.Add(new TooltipProperty(new TextObject("{=Bt82dbKu}Deck Capacity", null).ToString(), mainDeckCrewCapacity.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewdeck").ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty(new TextObject("{=HThruy9f}Reserve Capacity", null).ToString(), num.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewreserve").ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty(new TextObject("{=kLvWPxIK}Total Capacity", null).ToString(), totalCrewCapacity.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_ship_stat_explanation", "crewtotal").ToString(), -1, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
			return list;
		}

		// Token: 0x060014C4 RID: 5316 RVA: 0x00052612 File Offset: 0x00050812
		public override string GetName()
		{
			return this._shipHull.Name.ToString();
		}

		// Token: 0x060014C5 RID: 5317 RVA: 0x00052624 File Offset: 0x00050824
		private static string GetPrefabIdOfShipHull(ShipHull shipHull)
		{
			MissionShipObject @object = MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId);
			return ((@object != null) ? @object.Prefab : null) ?? string.Empty;
		}

		// Token: 0x060014C6 RID: 5318 RVA: 0x0005264C File Offset: 0x0005084C
		public override string GetNavigationBarURL()
		{
			return HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ " + HyperlinkTexts.GetGenericHyperlinkText("ListPage-Ships", GameTexts.FindText("str_encyclopedia_ships", null).ToString()) + " \\ " + this.GetName();
		}

		// Token: 0x060014C7 RID: 5319 RVA: 0x000526B1 File Offset: 0x000508B1
		public void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x060014C8 RID: 5320 RVA: 0x000526C4 File Offset: 0x000508C4
		public override void ExecuteSwitchBookmarkedState()
		{
			base.ExecuteSwitchBookmarkedState();
			if (base.IsBookmarked)
			{
				Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(this._shipHull);
				return;
			}
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(this._shipHull);
		}

		// Token: 0x170006E0 RID: 1760
		// (get) Token: 0x060014C9 RID: 5321 RVA: 0x00052714 File Offset: 0x00050914
		// (set) Token: 0x060014CA RID: 5322 RVA: 0x0005271C File Offset: 0x0005091C
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

		// Token: 0x170006E1 RID: 1761
		// (get) Token: 0x060014CB RID: 5323 RVA: 0x0005273F File Offset: 0x0005093F
		// (set) Token: 0x060014CC RID: 5324 RVA: 0x00052747 File Offset: 0x00050947
		[DataSourceProperty]
		public string AvailableUpgradesText
		{
			get
			{
				return this._availableUpgradesText;
			}
			set
			{
				if (value != this._availableUpgradesText)
				{
					this._availableUpgradesText = value;
					base.OnPropertyChangedWithValue<string>(value, "AvailableUpgradesText");
				}
			}
		}

		// Token: 0x170006E2 RID: 1762
		// (get) Token: 0x060014CD RID: 5325 RVA: 0x0005276A File Offset: 0x0005096A
		// (set) Token: 0x060014CE RID: 5326 RVA: 0x00052772 File Offset: 0x00050972
		[DataSourceProperty]
		public string DescriptionText
		{
			get
			{
				return this._descriptionText;
			}
			set
			{
				if (value != this._descriptionText)
				{
					this._descriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptionText");
				}
			}
		}

		// Token: 0x170006E3 RID: 1763
		// (get) Token: 0x060014CF RID: 5327 RVA: 0x00052795 File Offset: 0x00050995
		// (set) Token: 0x060014D0 RID: 5328 RVA: 0x0005279D File Offset: 0x0005099D
		[DataSourceProperty]
		public string PrefabId
		{
			get
			{
				return this._prefabId;
			}
			set
			{
				if (value != this._prefabId)
				{
					this._prefabId = value;
					base.OnPropertyChangedWithValue<string>(value, "PrefabId");
				}
			}
		}

		// Token: 0x170006E4 RID: 1764
		// (get) Token: 0x060014D1 RID: 5329 RVA: 0x000527C0 File Offset: 0x000509C0
		// (set) Token: 0x060014D2 RID: 5330 RVA: 0x000527C8 File Offset: 0x000509C8
		[DataSourceProperty]
		public string StatsText
		{
			get
			{
				return this._statsText;
			}
			set
			{
				if (value != this._statsText)
				{
					this._statsText = value;
					base.OnPropertyChangedWithValue<string>(value, "StatsText");
				}
			}
		}

		// Token: 0x170006E5 RID: 1765
		// (get) Token: 0x060014D3 RID: 5331 RVA: 0x000527EB File Offset: 0x000509EB
		// (set) Token: 0x060014D4 RID: 5332 RVA: 0x000527F3 File Offset: 0x000509F3
		[DataSourceProperty]
		public string SailType
		{
			get
			{
				return this._sailType;
			}
			set
			{
				if (value != this._sailType)
				{
					this._sailType = value;
					base.OnPropertyChangedWithValue<string>(value, "SailType");
				}
			}
		}

		// Token: 0x170006E6 RID: 1766
		// (get) Token: 0x060014D5 RID: 5333 RVA: 0x00052816 File Offset: 0x00050A16
		// (set) Token: 0x060014D6 RID: 5334 RVA: 0x0005281E File Offset: 0x00050A1E
		[DataSourceProperty]
		public EncyclopediaShipStatVM SailTypeStat
		{
			get
			{
				return this._sailTypeStat;
			}
			set
			{
				if (value != this._sailTypeStat)
				{
					this._sailTypeStat = value;
					base.OnPropertyChangedWithValue<EncyclopediaShipStatVM>(value, "SailTypeStat");
				}
			}
		}

		// Token: 0x170006E7 RID: 1767
		// (get) Token: 0x060014D7 RID: 5335 RVA: 0x0005283C File Offset: 0x00050A3C
		// (set) Token: 0x060014D8 RID: 5336 RVA: 0x00052844 File Offset: 0x00050A44
		[DataSourceProperty]
		public MBBindingList<EncyclopediaShipStatVM> StatList
		{
			get
			{
				return this._statList;
			}
			set
			{
				if (value != this._statList)
				{
					this._statList = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaShipStatVM>>(value, "StatList");
				}
			}
		}

		// Token: 0x170006E8 RID: 1768
		// (get) Token: 0x060014D9 RID: 5337 RVA: 0x00052862 File Offset: 0x00050A62
		// (set) Token: 0x060014DA RID: 5338 RVA: 0x0005286A File Offset: 0x00050A6A
		[DataSourceProperty]
		public MBBindingList<EncyclopediaShipSlotVM> AllShipSlots
		{
			get
			{
				return this._allShipSlots;
			}
			set
			{
				if (value != this._allShipSlots)
				{
					this._allShipSlots = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaShipSlotVM>>(value, "AllShipSlots");
				}
			}
		}

		// Token: 0x0400097A RID: 2426
		private readonly ShipHull _shipHull;

		// Token: 0x0400097B RID: 2427
		private readonly MissionShipObject _missionShip;

		// Token: 0x0400097C RID: 2428
		private string _descriptionText;

		// Token: 0x0400097D RID: 2429
		private string _prefabId;

		// Token: 0x0400097E RID: 2430
		private string _nameText;

		// Token: 0x0400097F RID: 2431
		private string _availableUpgradesText;

		// Token: 0x04000980 RID: 2432
		private string _statsText;

		// Token: 0x04000981 RID: 2433
		private string _sailType;

		// Token: 0x04000982 RID: 2434
		private EncyclopediaShipStatVM _sailTypeStat;

		// Token: 0x04000983 RID: 2435
		private MBBindingList<EncyclopediaShipStatVM> _statList;

		// Token: 0x04000984 RID: 2436
		private MBBindingList<EncyclopediaShipSlotVM> _allShipSlots;
	}
}
