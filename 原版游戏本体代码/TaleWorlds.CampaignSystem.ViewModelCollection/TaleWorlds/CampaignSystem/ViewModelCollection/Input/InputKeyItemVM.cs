using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Input
{
	// Token: 0x0200009A RID: 154
	public class InputKeyItemVM : ViewModel
	{
		// Token: 0x170004CF RID: 1231
		// (get) Token: 0x06000EE2 RID: 3810 RVA: 0x0003DEBE File Offset: 0x0003C0BE
		// (set) Token: 0x06000EE3 RID: 3811 RVA: 0x0003DEC6 File Offset: 0x0003C0C6
		public GameKey GameKey { get; private set; }

		// Token: 0x170004D0 RID: 1232
		// (get) Token: 0x06000EE4 RID: 3812 RVA: 0x0003DECF File Offset: 0x0003C0CF
		// (set) Token: 0x06000EE5 RID: 3813 RVA: 0x0003DED7 File Offset: 0x0003C0D7
		public HotKey HotKey { get; private set; }

		// Token: 0x06000EE6 RID: 3814 RVA: 0x0003DEE0 File Offset: 0x0003C0E0
		private InputKeyItemVM()
		{
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
		}

		// Token: 0x06000EE7 RID: 3815 RVA: 0x0003DF08 File Offset: 0x0003C108
		public override void OnFinalize()
		{
			base.OnFinalize();
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
		}

		// Token: 0x06000EE8 RID: 3816 RVA: 0x0003DF30 File Offset: 0x0003C130
		private void OnGamepadActiveStateChanged()
		{
			this.ForceRefresh();
		}

		// Token: 0x06000EE9 RID: 3817 RVA: 0x0003DF38 File Offset: 0x0003C138
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ForceRefresh();
		}

		// Token: 0x06000EEA RID: 3818 RVA: 0x0003DF46 File Offset: 0x0003C146
		public void SetForcedVisibility(bool? isVisible)
		{
			this._forcedVisibility = isVisible;
			this.UpdateVisibility();
		}

		// Token: 0x06000EEB RID: 3819 RVA: 0x0003DF58 File Offset: 0x0003C158
		private void ForceRefresh()
		{
			this.UpdateVisibility();
			if (this._forcedID != null)
			{
				this.KeyID = this._forcedID;
				TextObject forcedName = this._forcedName;
				this.KeyName = ((forcedName != null) ? forcedName.ToString() : null) ?? string.Empty;
				return;
			}
			this.KeyID = this.GetKeyId();
			this.KeyName = this.GetKeyName().ToString();
		}

		// Token: 0x06000EEC RID: 3820 RVA: 0x0003DFC0 File Offset: 0x0003C1C0
		private string GetKeyId()
		{
			if (Input.IsGamepadActive)
			{
				if (this.GameKey != null)
				{
					Key controllerKey = this.GameKey.ControllerKey;
					if (controllerKey == null)
					{
						return null;
					}
					return controllerKey.InputKey.ToString();
				}
				else if (this.HotKey != null)
				{
					Key key = this.HotKey.Keys.Find((Key k) => k.IsControllerInput);
					if (key == null)
					{
						return null;
					}
					return key.InputKey.ToString();
				}
			}
			if (this.GameKey != null)
			{
				Key keyboardKey = this.GameKey.KeyboardKey;
				if (keyboardKey == null)
				{
					return null;
				}
				return keyboardKey.InputKey.ToString();
			}
			else
			{
				if (this.HotKey == null)
				{
					return string.Empty;
				}
				Key key2 = this.HotKey.Keys.Find((Key k) => !k.IsControllerInput);
				if (key2 == null)
				{
					return null;
				}
				return key2.InputKey.ToString();
			}
		}

		// Token: 0x06000EED RID: 3821 RVA: 0x0003E0D8 File Offset: 0x0003C2D8
		private TextObject GetKeyName()
		{
			if (this._forcedName != null)
			{
				return this._forcedName;
			}
			if (Game.Current != null)
			{
				if (this.HotKey != null)
				{
					return Game.Current.GameTextManager.FindText("str_key_name", this.HotKey.GroupId + "_" + this.HotKey.Id);
				}
				if (this.GameKey != null)
				{
					return Game.Current.GameTextManager.FindText("str_key_name", this.GameKey.GroupId + "_" + this.GameKey.StringId);
				}
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x06000EEE RID: 3822 RVA: 0x0003E180 File Offset: 0x0003C380
		private void UpdateVisibility()
		{
			this.IsVisible = this._forcedVisibility ?? (!this._isVisibleToConsoleOnly || Input.IsGamepadActive);
		}

		// Token: 0x06000EEF RID: 3823 RVA: 0x0003E1BC File Offset: 0x0003C3BC
		public static InputKeyItemVM CreateFromGameKey(GameKey gameKey, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.GameKey = gameKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x06000EF0 RID: 3824 RVA: 0x0003E1D7 File Offset: 0x0003C3D7
		public static InputKeyItemVM CreateFromHotKey(HotKey hotKey, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.HotKey = hotKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x06000EF1 RID: 3825 RVA: 0x0003E1F2 File Offset: 0x0003C3F2
		public static InputKeyItemVM CreateFromHotKeyWithForcedName(HotKey hotKey, TextObject forcedName, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.HotKey = hotKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM._forcedName = forcedName;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x06000EF2 RID: 3826 RVA: 0x0003E214 File Offset: 0x0003C414
		public static InputKeyItemVM CreateFromGameKeyWithForcedName(GameKey gameKey, TextObject forcedName, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.GameKey = gameKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM._forcedName = forcedName;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x06000EF3 RID: 3827 RVA: 0x0003E236 File Offset: 0x0003C436
		public static InputKeyItemVM CreateFromForcedID(string forcedID, TextObject forcedName, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM._forcedID = forcedID;
			inputKeyItemVM._forcedName = forcedName;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x170004D1 RID: 1233
		// (get) Token: 0x06000EF4 RID: 3828 RVA: 0x0003E258 File Offset: 0x0003C458
		// (set) Token: 0x06000EF5 RID: 3829 RVA: 0x0003E260 File Offset: 0x0003C460
		[DataSourceProperty]
		public string KeyID
		{
			get
			{
				return this._keyID;
			}
			set
			{
				if (value != this._keyID)
				{
					this._keyID = value;
					base.OnPropertyChangedWithValue<string>(value, "KeyID");
				}
			}
		}

		// Token: 0x170004D2 RID: 1234
		// (get) Token: 0x06000EF6 RID: 3830 RVA: 0x0003E283 File Offset: 0x0003C483
		// (set) Token: 0x06000EF7 RID: 3831 RVA: 0x0003E28B File Offset: 0x0003C48B
		[DataSourceProperty]
		public string KeyName
		{
			get
			{
				return this._keyName;
			}
			set
			{
				if (value != this._keyName)
				{
					this._keyName = value;
					base.OnPropertyChangedWithValue<string>(value, "KeyName");
				}
			}
		}

		// Token: 0x170004D3 RID: 1235
		// (get) Token: 0x06000EF8 RID: 3832 RVA: 0x0003E2AE File Offset: 0x0003C4AE
		// (set) Token: 0x06000EF9 RID: 3833 RVA: 0x0003E2B6 File Offset: 0x0003C4B6
		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return this._isVisible;
			}
			set
			{
				if (value != this._isVisible)
				{
					this._isVisible = value;
					base.OnPropertyChangedWithValue(value, "IsVisible");
				}
			}
		}

		// Token: 0x040006B9 RID: 1721
		private bool _isVisibleToConsoleOnly;

		// Token: 0x040006BA RID: 1722
		private TextObject _forcedName;

		// Token: 0x040006BB RID: 1723
		private string _forcedID;

		// Token: 0x040006BC RID: 1724
		private bool? _forcedVisibility;

		// Token: 0x040006BD RID: 1725
		private string _keyID;

		// Token: 0x040006BE RID: 1726
		private string _keyName;

		// Token: 0x040006BF RID: 1727
		private bool _isVisible;
	}
}
