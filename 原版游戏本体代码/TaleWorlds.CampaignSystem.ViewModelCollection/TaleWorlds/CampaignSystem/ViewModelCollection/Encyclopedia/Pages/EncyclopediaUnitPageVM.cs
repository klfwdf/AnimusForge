using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000DA RID: 218
	[EncyclopediaViewModel(typeof(CharacterObject))]
	public class EncyclopediaUnitPageVM : EncyclopediaContentPageVM
	{
		// Token: 0x060014DB RID: 5339 RVA: 0x00052888 File Offset: 0x00050A88
		public EncyclopediaUnitPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this._character = base.Obj as CharacterObject;
			this.UnitCharacter = new CharacterViewModel(CharacterViewModel.StanceTypes.OnMount);
			this.UnitCharacter.FillFrom(this._character, -1, null);
			this.HasErrors = this.DoesCharacterHaveCircularUpgradePaths(this._character, null);
			if (!this.HasErrors)
			{
				CharacterObject rootCharacter = CharacterHelper.FindUpgradeRootOf(this._character);
				this.Tree = new EncyclopediaTroopTreeNodeVM(rootCharacter, this._character, false, null);
			}
			this.PropertiesList = new MBBindingList<StringItemWithHintVM>();
			this.EquipmentSetSelector = new SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM>(0, new Action<SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM>>(this.OnEquipmentSetChange));
			base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(this._character);
			this.RefreshValues();
		}

		// Token: 0x060014DC RID: 5340 RVA: 0x00052950 File Offset: 0x00050B50
		private bool DoesCharacterHaveCircularUpgradePaths(CharacterObject baseCharacter, CharacterObject character = null)
		{
			bool result = false;
			if (character == null)
			{
				character = baseCharacter;
			}
			for (int i = 0; i < character.UpgradeTargets.Length; i++)
			{
				if (character.UpgradeTargets[i] == baseCharacter)
				{
					Debug.FailedAssert(string.Format("Circular dependency on troop upgrade paths: {0} --> {1}", character.Name, baseCharacter.Name), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Pages\\EncyclopediaUnitPageVM.cs", "DoesCharacterHaveCircularUpgradePaths", 56);
					result = true;
					break;
				}
				result = this.DoesCharacterHaveCircularUpgradePaths(baseCharacter, character.UpgradeTargets[i]);
			}
			return result;
		}

		// Token: 0x060014DD RID: 5341 RVA: 0x000529C0 File Offset: 0x00050BC0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this._equipmentSetTextObj = new TextObject("{=vggt7exj}Set {CURINDEX}/{COUNT}", null);
			this.PropertiesList.Clear();
			this.PropertiesList.Add(CampaignUIHelper.GetCharacterTierData(this._character, true));
			this.PropertiesList.Add(CampaignUIHelper.GetCharacterTypeData(this._character, true));
			this.EquipmentSetSelector.ItemList.Clear();
			using (IEnumerator<Equipment> enumerator = this._character.BattleEquipments.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Equipment equipment = enumerator.Current;
					if (!this.EquipmentSetSelector.ItemList.Any((EncyclopediaUnitEquipmentSetSelectorItemVM x) => x.EquipmentSet.IsEquipmentEqualTo(equipment)))
					{
						this.EquipmentSetSelector.AddItem(new EncyclopediaUnitEquipmentSetSelectorItemVM(equipment, ""));
					}
				}
			}
			if (this.EquipmentSetSelector.ItemList.Count > 0)
			{
				this.EquipmentSetSelector.SelectedIndex = 0;
			}
			this._equipmentSetTextObj.SetTextVariable("CURINDEX", this.EquipmentSetSelector.SelectedIndex + 1);
			this._equipmentSetTextObj.SetTextVariable("COUNT", this.EquipmentSetSelector.ItemList.Count);
			this.EquipmentSetText = this._equipmentSetTextObj.ToString();
			this.TreeDisplayErrorText = new TextObject("{=BkDycbdq}Error while displaying the troop tree", null).ToString();
			this.Skills = new MBBindingList<EncyclopediaSkillVM>();
			List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList<SkillObject>();
			list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			foreach (SkillObject skill in list)
			{
				if (this._character.GetSkillValue(skill) > 0)
				{
					this.Skills.Add(new EncyclopediaSkillVM(skill, this._character.GetSkillValue(skill)));
				}
			}
			this.DescriptionText = GameTexts.FindText("str_encyclopedia_unit_description", this._character.StringId).ToString();
			this.NameText = this._character.Name.ToString();
			EncyclopediaTroopTreeNodeVM tree = this.Tree;
			if (tree != null)
			{
				tree.RefreshValues();
			}
			CharacterViewModel unitCharacter = this.UnitCharacter;
			if (unitCharacter != null)
			{
				unitCharacter.RefreshValues();
			}
			base.UpdateBookmarkHintText();
		}

		// Token: 0x060014DE RID: 5342 RVA: 0x00052C14 File Offset: 0x00050E14
		private void OnEquipmentSetChange(SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM> selector)
		{
			this.CurrentSelectedEquipmentSet = selector.SelectedItem;
			this.UnitCharacter.SetEquipment(this.CurrentSelectedEquipmentSet.EquipmentSet);
			this._equipmentSetTextObj.SetTextVariable("CURINDEX", selector.SelectedIndex + 1);
			this._equipmentSetTextObj.SetTextVariable("COUNT", selector.ItemList.Count);
			this.EquipmentSetText = this._equipmentSetTextObj.ToString();
		}

		// Token: 0x060014DF RID: 5343 RVA: 0x00052C89 File Offset: 0x00050E89
		public override string GetName()
		{
			return this._character.Name.ToString();
		}

		// Token: 0x060014E0 RID: 5344 RVA: 0x00052C9C File Offset: 0x00050E9C
		public override string GetNavigationBarURL()
		{
			return HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ " + HyperlinkTexts.GetGenericHyperlinkText("ListPage-Units", GameTexts.FindText("str_encyclopedia_troops", null).ToString()) + " \\ " + this.GetName();
		}

		// Token: 0x060014E1 RID: 5345 RVA: 0x00052D04 File Offset: 0x00050F04
		public override void ExecuteSwitchBookmarkedState()
		{
			base.ExecuteSwitchBookmarkedState();
			if (base.IsBookmarked)
			{
				Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(this._character);
				return;
			}
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(this._character);
		}

		// Token: 0x170006E9 RID: 1769
		// (get) Token: 0x060014E2 RID: 5346 RVA: 0x00052D54 File Offset: 0x00050F54
		// (set) Token: 0x060014E3 RID: 5347 RVA: 0x00052D5C File Offset: 0x00050F5C
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

		// Token: 0x170006EA RID: 1770
		// (get) Token: 0x060014E4 RID: 5348 RVA: 0x00052D7A File Offset: 0x00050F7A
		// (set) Token: 0x060014E5 RID: 5349 RVA: 0x00052D82 File Offset: 0x00050F82
		[DataSourceProperty]
		public MBBindingList<StringItemWithHintVM> PropertiesList
		{
			get
			{
				return this._propertiesList;
			}
			set
			{
				if (value != this._propertiesList)
				{
					this._propertiesList = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringItemWithHintVM>>(value, "PropertiesList");
				}
			}
		}

		// Token: 0x170006EB RID: 1771
		// (get) Token: 0x060014E6 RID: 5350 RVA: 0x00052DA0 File Offset: 0x00050FA0
		// (set) Token: 0x060014E7 RID: 5351 RVA: 0x00052DA8 File Offset: 0x00050FA8
		[DataSourceProperty]
		public SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM> EquipmentSetSelector
		{
			get
			{
				return this._equipmentSetSelector;
			}
			set
			{
				if (value != this._equipmentSetSelector)
				{
					this._equipmentSetSelector = value;
					base.OnPropertyChangedWithValue<SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM>>(value, "EquipmentSetSelector");
				}
			}
		}

		// Token: 0x170006EC RID: 1772
		// (get) Token: 0x060014E8 RID: 5352 RVA: 0x00052DC6 File Offset: 0x00050FC6
		// (set) Token: 0x060014E9 RID: 5353 RVA: 0x00052DCE File Offset: 0x00050FCE
		[DataSourceProperty]
		public EncyclopediaUnitEquipmentSetSelectorItemVM CurrentSelectedEquipmentSet
		{
			get
			{
				return this._currentSelectedEquipmentSet;
			}
			set
			{
				if (value != this._currentSelectedEquipmentSet)
				{
					this._currentSelectedEquipmentSet = value;
					base.OnPropertyChangedWithValue<EncyclopediaUnitEquipmentSetSelectorItemVM>(value, "CurrentSelectedEquipmentSet");
				}
			}
		}

		// Token: 0x170006ED RID: 1773
		// (get) Token: 0x060014EA RID: 5354 RVA: 0x00052DEC File Offset: 0x00050FEC
		// (set) Token: 0x060014EB RID: 5355 RVA: 0x00052DF4 File Offset: 0x00050FF4
		[DataSourceProperty]
		public CharacterViewModel UnitCharacter
		{
			get
			{
				return this._unitCharacter;
			}
			set
			{
				if (value != this._unitCharacter)
				{
					this._unitCharacter = value;
					base.OnPropertyChangedWithValue<CharacterViewModel>(value, "UnitCharacter");
				}
			}
		}

		// Token: 0x170006EE RID: 1774
		// (get) Token: 0x060014EC RID: 5356 RVA: 0x00052E12 File Offset: 0x00051012
		// (set) Token: 0x060014ED RID: 5357 RVA: 0x00052E1A File Offset: 0x0005101A
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

		// Token: 0x170006EF RID: 1775
		// (get) Token: 0x060014EE RID: 5358 RVA: 0x00052E3D File Offset: 0x0005103D
		// (set) Token: 0x060014EF RID: 5359 RVA: 0x00052E45 File Offset: 0x00051045
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

		// Token: 0x170006F0 RID: 1776
		// (get) Token: 0x060014F0 RID: 5360 RVA: 0x00052E68 File Offset: 0x00051068
		// (set) Token: 0x060014F1 RID: 5361 RVA: 0x00052E70 File Offset: 0x00051070
		[DataSourceProperty]
		public EncyclopediaTroopTreeNodeVM Tree
		{
			get
			{
				return this._tree;
			}
			set
			{
				if (value != this._tree)
				{
					this._tree = value;
					base.OnPropertyChangedWithValue<EncyclopediaTroopTreeNodeVM>(value, "Tree");
				}
			}
		}

		// Token: 0x170006F1 RID: 1777
		// (get) Token: 0x060014F2 RID: 5362 RVA: 0x00052E8E File Offset: 0x0005108E
		// (set) Token: 0x060014F3 RID: 5363 RVA: 0x00052E96 File Offset: 0x00051096
		[DataSourceProperty]
		public string TreeDisplayErrorText
		{
			get
			{
				return this._treeDisplayErrorText;
			}
			set
			{
				if (value != this._treeDisplayErrorText)
				{
					this._treeDisplayErrorText = value;
					base.OnPropertyChangedWithValue<string>(value, "TreeDisplayErrorText");
				}
			}
		}

		// Token: 0x170006F2 RID: 1778
		// (get) Token: 0x060014F4 RID: 5364 RVA: 0x00052EB9 File Offset: 0x000510B9
		// (set) Token: 0x060014F5 RID: 5365 RVA: 0x00052EC1 File Offset: 0x000510C1
		[DataSourceProperty]
		public string EquipmentSetText
		{
			get
			{
				return this._equipmentSetText;
			}
			set
			{
				if (value != this._equipmentSetText)
				{
					this._equipmentSetText = value;
					base.OnPropertyChangedWithValue<string>(value, "EquipmentSetText");
				}
			}
		}

		// Token: 0x170006F3 RID: 1779
		// (get) Token: 0x060014F6 RID: 5366 RVA: 0x00052EE4 File Offset: 0x000510E4
		// (set) Token: 0x060014F7 RID: 5367 RVA: 0x00052EEC File Offset: 0x000510EC
		[DataSourceProperty]
		public bool HasErrors
		{
			get
			{
				return this._hasErrors;
			}
			set
			{
				if (value != this._hasErrors)
				{
					this._hasErrors = value;
					base.OnPropertyChangedWithValue(value, "HasErrors");
				}
			}
		}

		// Token: 0x04000985 RID: 2437
		private readonly CharacterObject _character;

		// Token: 0x04000986 RID: 2438
		private TextObject _equipmentSetTextObj;

		// Token: 0x04000987 RID: 2439
		private MBBindingList<EncyclopediaSkillVM> _skills;

		// Token: 0x04000988 RID: 2440
		private MBBindingList<StringItemWithHintVM> _propertiesList;

		// Token: 0x04000989 RID: 2441
		private SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM> _equipmentSetSelector;

		// Token: 0x0400098A RID: 2442
		private EncyclopediaUnitEquipmentSetSelectorItemVM _currentSelectedEquipmentSet;

		// Token: 0x0400098B RID: 2443
		private EncyclopediaTroopTreeNodeVM _tree;

		// Token: 0x0400098C RID: 2444
		private string _descriptionText;

		// Token: 0x0400098D RID: 2445
		private CharacterViewModel _unitCharacter;

		// Token: 0x0400098E RID: 2446
		private string _nameText;

		// Token: 0x0400098F RID: 2447
		private string _treeDisplayErrorText;

		// Token: 0x04000990 RID: 2448
		private string _equipmentSetText;

		// Token: 0x04000991 RID: 2449
		private bool _hasErrors;
	}
}
