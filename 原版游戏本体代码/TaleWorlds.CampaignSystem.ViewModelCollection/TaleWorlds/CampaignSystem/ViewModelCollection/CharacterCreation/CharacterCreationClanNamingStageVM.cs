using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x0200014B RID: 331
	public class CharacterCreationClanNamingStageVM : CharacterCreationStageBaseVM
	{
		// Token: 0x17000AB1 RID: 2737
		// (get) Token: 0x06001F62 RID: 8034 RVA: 0x00072E94 File Offset: 0x00071094
		// (set) Token: 0x06001F63 RID: 8035 RVA: 0x00072E9C File Offset: 0x0007109C
		public BasicCharacterObject Character { get; private set; }

		// Token: 0x17000AB2 RID: 2738
		// (get) Token: 0x06001F64 RID: 8036 RVA: 0x00072EA5 File Offset: 0x000710A5
		// (set) Token: 0x06001F65 RID: 8037 RVA: 0x00072EAD File Offset: 0x000710AD
		public int ShieldSlotIndex { get; private set; } = 3;

		// Token: 0x17000AB3 RID: 2739
		// (get) Token: 0x06001F66 RID: 8038 RVA: 0x00072EB6 File Offset: 0x000710B6
		// (set) Token: 0x06001F67 RID: 8039 RVA: 0x00072EBE File Offset: 0x000710BE
		public ItemRosterElement ShieldRosterElement { get; private set; }

		// Token: 0x06001F68 RID: 8040 RVA: 0x00072EC8 File Offset: 0x000710C8
		public CharacterCreationClanNamingStageVM(BasicCharacterObject character, CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText)
			: base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
		{
			this.Character = character;
			this.ClanName = Hero.MainHero.Clan.Name.ToString();
			ItemObject item = this.FindShield();
			this.ShieldRosterElement = new ItemRosterElement(item, 1, null);
			this.ClanBanner = new BannerImageIdentifierVM(Hero.MainHero.Clan.Banner, true);
			this.CameraControlKeys = new MBBindingList<InputKeyItemVM>();
			this.RefreshValues();
		}

		// Token: 0x06001F69 RID: 8041 RVA: 0x00072F4C File Offset: 0x0007114C
		public override void RefreshValues()
		{
			base.RefreshValues();
			base.Title = new TextObject("{=wNUcqcJP}Clan Name", null).ToString();
			base.Description = new TextObject("{=JJiKk4ow}Select your family name: ", null).ToString();
			this.BottomHintText = new TextObject("{=dbBAJ8yi}You can change your banner and clan name later on clan screen", null).ToString();
		}

		// Token: 0x06001F6A RID: 8042 RVA: 0x00072FA4 File Offset: 0x000711A4
		public override bool CanAdvanceToNextStage()
		{
			Tuple<bool, string> tuple = FactionHelper.IsClanNameApplicable(this.ClanName);
			this.ClanNameNotApplicableReason = tuple.Item2;
			return tuple.Item1;
		}

		// Token: 0x06001F6B RID: 8043 RVA: 0x00072FCF File Offset: 0x000711CF
		public override void OnNextStage()
		{
			this._affirmativeAction();
		}

		// Token: 0x06001F6C RID: 8044 RVA: 0x00072FDC File Offset: 0x000711DC
		public override void OnPreviousStage()
		{
			this._negativeAction();
		}

		// Token: 0x06001F6D RID: 8045 RVA: 0x00072FEC File Offset: 0x000711EC
		private ItemObject FindShield()
		{
			for (int i = 0; i < 4; i++)
			{
				EquipmentElement equipmentFromSlot = this.Character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
				ItemObject item = equipmentFromSlot.Item;
				if (((item != null) ? item.PrimaryWeapon : null) != null && equipmentFromSlot.Item.PrimaryWeapon.IsShield && equipmentFromSlot.Item.IsUsingTableau)
				{
					return equipmentFromSlot.Item;
				}
			}
			foreach (ItemObject itemObject in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
			{
				if (itemObject.PrimaryWeapon != null && itemObject.PrimaryWeapon.IsShield && itemObject.IsUsingTableau)
				{
					return itemObject;
				}
			}
			return null;
		}

		// Token: 0x06001F6E RID: 8046 RVA: 0x000730C4 File Offset: 0x000712C4
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey != null)
			{
				cancelInputKey.OnFinalize();
			}
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			foreach (InputKeyItemVM inputKeyItemVM in this.CameraControlKeys)
			{
				inputKeyItemVM.OnFinalize();
			}
		}

		// Token: 0x06001F6F RID: 8047 RVA: 0x00073138 File Offset: 0x00071338
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001F70 RID: 8048 RVA: 0x00073147 File Offset: 0x00071347
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001F71 RID: 8049 RVA: 0x00073158 File Offset: 0x00071358
		public void AddCameraControlInputKey(HotKey hotKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x06001F72 RID: 8050 RVA: 0x0007317C File Offset: 0x0007137C
		public void AddCameraControlInputKey(GameKey gameKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x06001F73 RID: 8051 RVA: 0x000731A0 File Offset: 0x000713A0
		public void AddCameraControlInputKey(GameAxisKey gameAxisKey, TextObject keyName)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), keyName, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x17000AB4 RID: 2740
		// (get) Token: 0x06001F74 RID: 8052 RVA: 0x000731CC File Offset: 0x000713CC
		// (set) Token: 0x06001F75 RID: 8053 RVA: 0x000731D4 File Offset: 0x000713D4
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

		// Token: 0x17000AB5 RID: 2741
		// (get) Token: 0x06001F76 RID: 8054 RVA: 0x000731F2 File Offset: 0x000713F2
		// (set) Token: 0x06001F77 RID: 8055 RVA: 0x000731FA File Offset: 0x000713FA
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

		// Token: 0x17000AB6 RID: 2742
		// (get) Token: 0x06001F78 RID: 8056 RVA: 0x00073218 File Offset: 0x00071418
		// (set) Token: 0x06001F79 RID: 8057 RVA: 0x00073220 File Offset: 0x00071420
		[DataSourceProperty]
		public MBBindingList<InputKeyItemVM> CameraControlKeys
		{
			get
			{
				return this._cameraControlKeys;
			}
			set
			{
				if (value != this._cameraControlKeys)
				{
					this._cameraControlKeys = value;
					base.OnPropertyChangedWithValue<MBBindingList<InputKeyItemVM>>(value, "CameraControlKeys");
				}
			}
		}

		// Token: 0x17000AB7 RID: 2743
		// (get) Token: 0x06001F7A RID: 8058 RVA: 0x0007323E File Offset: 0x0007143E
		// (set) Token: 0x06001F7B RID: 8059 RVA: 0x00073246 File Offset: 0x00071446
		[DataSourceProperty]
		public string ClanName
		{
			get
			{
				return this._clanName;
			}
			set
			{
				if (value != this._clanName)
				{
					this._clanName = value;
					base.OnPropertyChangedWithValue<string>(value, "ClanName");
					base.CanAdvance = this.CanAdvanceToNextStage();
				}
			}
		}

		// Token: 0x17000AB8 RID: 2744
		// (get) Token: 0x06001F7C RID: 8060 RVA: 0x00073275 File Offset: 0x00071475
		// (set) Token: 0x06001F7D RID: 8061 RVA: 0x0007327D File Offset: 0x0007147D
		[DataSourceProperty]
		public string ClanNameNotApplicableReason
		{
			get
			{
				return this._clanNameNotApplicableReason;
			}
			set
			{
				if (value != this._clanNameNotApplicableReason)
				{
					this._clanNameNotApplicableReason = value;
					base.OnPropertyChangedWithValue<string>(value, "ClanNameNotApplicableReason");
				}
			}
		}

		// Token: 0x17000AB9 RID: 2745
		// (get) Token: 0x06001F7E RID: 8062 RVA: 0x000732A0 File Offset: 0x000714A0
		// (set) Token: 0x06001F7F RID: 8063 RVA: 0x000732A8 File Offset: 0x000714A8
		[DataSourceProperty]
		public string BottomHintText
		{
			get
			{
				return this._bottomHintText;
			}
			set
			{
				if (value != this._bottomHintText)
				{
					this._bottomHintText = value;
					base.OnPropertyChangedWithValue<string>(value, "BottomHintText");
				}
			}
		}

		// Token: 0x17000ABA RID: 2746
		// (get) Token: 0x06001F80 RID: 8064 RVA: 0x000732CB File Offset: 0x000714CB
		// (set) Token: 0x06001F81 RID: 8065 RVA: 0x000732D3 File Offset: 0x000714D3
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

		// Token: 0x17000ABB RID: 2747
		// (get) Token: 0x06001F82 RID: 8066 RVA: 0x000732F1 File Offset: 0x000714F1
		// (set) Token: 0x06001F83 RID: 8067 RVA: 0x000732F9 File Offset: 0x000714F9
		[DataSourceProperty]
		public bool CharacterGamepadControlsEnabled
		{
			get
			{
				return this._characterGamepadControlsEnabled;
			}
			set
			{
				if (value != this._characterGamepadControlsEnabled)
				{
					this._characterGamepadControlsEnabled = value;
					base.OnPropertyChangedWithValue(value, "CharacterGamepadControlsEnabled");
				}
			}
		}

		// Token: 0x04000EAB RID: 3755
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000EAC RID: 3756
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000EAD RID: 3757
		private MBBindingList<InputKeyItemVM> _cameraControlKeys;

		// Token: 0x04000EAE RID: 3758
		private string _clanName;

		// Token: 0x04000EAF RID: 3759
		private string _clanNameNotApplicableReason;

		// Token: 0x04000EB0 RID: 3760
		private string _bottomHintText;

		// Token: 0x04000EB1 RID: 3761
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x04000EB2 RID: 3762
		private bool _characterGamepadControlsEnabled;
	}
}
