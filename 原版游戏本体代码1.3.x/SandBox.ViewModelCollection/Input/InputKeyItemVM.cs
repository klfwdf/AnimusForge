using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Input
{
	// Token: 0x02000057 RID: 87
	public class InputKeyItemVM : ViewModel
	{
		// Token: 0x1700019A RID: 410
		// (get) Token: 0x0600055F RID: 1375 RVA: 0x00014493 File Offset: 0x00012693
		// (set) Token: 0x06000560 RID: 1376 RVA: 0x0001449B File Offset: 0x0001269B
		public GameKey GameKey { get; private set; }

		// Token: 0x1700019B RID: 411
		// (get) Token: 0x06000561 RID: 1377 RVA: 0x000144A4 File Offset: 0x000126A4
		// (set) Token: 0x06000562 RID: 1378 RVA: 0x000144AC File Offset: 0x000126AC
		public HotKey HotKey { get; private set; }

		// Token: 0x06000563 RID: 1379 RVA: 0x000144B5 File Offset: 0x000126B5
		private InputKeyItemVM()
		{
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
		}

		// Token: 0x06000564 RID: 1380 RVA: 0x000144DD File Offset: 0x000126DD
		public override void OnFinalize()
		{
			base.OnFinalize();
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x00014505 File Offset: 0x00012705
		private void OnGamepadActiveStateChanged()
		{
			this.ForceRefresh();
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x0001450D File Offset: 0x0001270D
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ForceRefresh();
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x0001451B File Offset: 0x0001271B
		public void SetForcedVisibility(bool? isVisible)
		{
			this._forcedVisibility = isVisible;
			this.UpdateVisibility();
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x0001452C File Offset: 0x0001272C
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

		// Token: 0x06000569 RID: 1385 RVA: 0x00014594 File Offset: 0x00012794
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

		// Token: 0x0600056A RID: 1386 RVA: 0x000146AC File Offset: 0x000128AC
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

		// Token: 0x0600056B RID: 1387 RVA: 0x00014754 File Offset: 0x00012954
		private void UpdateVisibility()
		{
			this.IsVisible = this._forcedVisibility ?? (!this._isVisibleToConsoleOnly || Input.IsGamepadActive);
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x00014790 File Offset: 0x00012990
		public static InputKeyItemVM CreateFromGameKey(GameKey gameKey, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.GameKey = gameKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x0600056D RID: 1389 RVA: 0x000147AB File Offset: 0x000129AB
		public static InputKeyItemVM CreateFromHotKey(HotKey hotKey, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.HotKey = hotKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x0600056E RID: 1390 RVA: 0x000147C6 File Offset: 0x000129C6
		public static InputKeyItemVM CreateFromHotKeyWithForcedName(HotKey hotKey, TextObject forcedName, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.HotKey = hotKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM._forcedName = forcedName;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x0600056F RID: 1391 RVA: 0x000147E8 File Offset: 0x000129E8
		public static InputKeyItemVM CreateFromGameKeyWithForcedName(GameKey gameKey, TextObject forcedName, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM.GameKey = gameKey;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM._forcedName = forcedName;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x06000570 RID: 1392 RVA: 0x0001480A File Offset: 0x00012A0A
		public static InputKeyItemVM CreateFromForcedID(string forcedID, TextObject forcedName, bool isConsoleOnly)
		{
			InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
			inputKeyItemVM._forcedID = forcedID;
			inputKeyItemVM._forcedName = forcedName;
			inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
			inputKeyItemVM.ForceRefresh();
			return inputKeyItemVM;
		}

		// Token: 0x1700019C RID: 412
		// (get) Token: 0x06000571 RID: 1393 RVA: 0x0001482C File Offset: 0x00012A2C
		// (set) Token: 0x06000572 RID: 1394 RVA: 0x00014834 File Offset: 0x00012A34
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

		// Token: 0x1700019D RID: 413
		// (get) Token: 0x06000573 RID: 1395 RVA: 0x00014857 File Offset: 0x00012A57
		// (set) Token: 0x06000574 RID: 1396 RVA: 0x0001485F File Offset: 0x00012A5F
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

		// Token: 0x1700019E RID: 414
		// (get) Token: 0x06000575 RID: 1397 RVA: 0x00014882 File Offset: 0x00012A82
		// (set) Token: 0x06000576 RID: 1398 RVA: 0x0001488A File Offset: 0x00012A8A
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

		// Token: 0x040002AA RID: 682
		private bool _isVisibleToConsoleOnly;

		// Token: 0x040002AB RID: 683
		private TextObject _forcedName;

		// Token: 0x040002AC RID: 684
		private string _forcedID;

		// Token: 0x040002AD RID: 685
		private bool? _forcedVisibility;

		// Token: 0x040002AE RID: 686
		private string _keyID;

		// Token: 0x040002AF RID: 687
		private string _keyName;

		// Token: 0x040002B0 RID: 688
		private bool _isVisible;
	}
}
