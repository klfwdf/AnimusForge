using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000046 RID: 70
	public class MapNotificationItemBaseVM : ViewModel
	{
		// Token: 0x170001A7 RID: 423
		// (get) Token: 0x060005F5 RID: 1525 RVA: 0x0001F3E8 File Offset: 0x0001D5E8
		// (set) Token: 0x060005F6 RID: 1526 RVA: 0x0001F3F0 File Offset: 0x0001D5F0
		public INavigationHandler NavigationHandler { get; private set; }

		// Token: 0x170001A8 RID: 424
		// (get) Token: 0x060005F7 RID: 1527 RVA: 0x0001F3F9 File Offset: 0x0001D5F9
		// (set) Token: 0x060005F8 RID: 1528 RVA: 0x0001F401 File Offset: 0x0001D601
		private protected Action<CampaignVec2> FastMoveCameraToPosition { protected get; private set; }

		// Token: 0x170001A9 RID: 425
		// (get) Token: 0x060005F9 RID: 1529 RVA: 0x0001F40A File Offset: 0x0001D60A
		// (set) Token: 0x060005FA RID: 1530 RVA: 0x0001F412 File Offset: 0x0001D612
		public InformationData Data { get; private set; }

		// Token: 0x060005FB RID: 1531 RVA: 0x0001F41C File Offset: 0x0001D61C
		public MapNotificationItemBaseVM(InformationData data)
		{
			this.Data = data;
			this.ForceInspection = false;
			this.SoundId = data.SoundEventPath;
			this.RefreshValues();
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x0001F46C File Offset: 0x0001D66C
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject titleText = this.Data.TitleText;
			this.TitleText = ((titleText != null) ? titleText.ToString() : null);
			TextObject descriptionText = this.Data.DescriptionText;
			this.DescriptionText = ((descriptionText != null) ? descriptionText.ToString() : null);
			this._removeHintText = this._removeHintTextObject.ToString();
		}

		// Token: 0x060005FD RID: 1533 RVA: 0x0001F4CA File Offset: 0x0001D6CA
		public void SetNavigationHandler(INavigationHandler navigationHandler)
		{
			this.NavigationHandler = navigationHandler;
		}

		// Token: 0x060005FE RID: 1534 RVA: 0x0001F4D3 File Offset: 0x0001D6D3
		public void SetFastMoveCameraToPosition(Action<CampaignVec2> fastMoveCameraToPosition)
		{
			this.FastMoveCameraToPosition = fastMoveCameraToPosition;
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x0001F4DC File Offset: 0x0001D6DC
		public void ExecuteAction()
		{
			Action onInspect = this._onInspect;
			if (onInspect == null)
			{
				return;
			}
			onInspect();
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x0001F4EE File Offset: 0x0001D6EE
		public void ExecuteRemove()
		{
			Action<MapNotificationItemBaseVM> onRemove = this.OnRemove;
			if (onRemove != null)
			{
				onRemove(this);
			}
			Action<MapNotificationItemBaseVM> onFocus = this.OnFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(null);
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x0001F513 File Offset: 0x0001D713
		public void ExecuteSetFocused()
		{
			this.IsFocused = true;
			Action<MapNotificationItemBaseVM> onFocus = this.OnFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(this);
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x0001F52D File Offset: 0x0001D72D
		public void ExecuteSetUnfocused()
		{
			this.IsFocused = false;
			Action<MapNotificationItemBaseVM> onFocus = this.OnFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(null);
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x0001F547 File Offset: 0x0001D747
		public virtual void ManualRefreshRelevantStatus()
		{
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x0001F549 File Offset: 0x0001D749
		internal void GoToMapPosition(CampaignVec2 position)
		{
			Action<CampaignVec2> fastMoveCameraToPosition = this.FastMoveCameraToPosition;
			if (fastMoveCameraToPosition == null)
			{
				return;
			}
			fastMoveCameraToPosition(position);
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x0001F55C File Offset: 0x0001D75C
		public void SetRemoveInputKey(HotKey hotKey)
		{
			this.RemoveInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x170001AA RID: 426
		// (get) Token: 0x06000606 RID: 1542 RVA: 0x0001F56B File Offset: 0x0001D76B
		// (set) Token: 0x06000607 RID: 1543 RVA: 0x0001F573 File Offset: 0x0001D773
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

		// Token: 0x170001AB RID: 427
		// (get) Token: 0x06000608 RID: 1544 RVA: 0x0001F591 File Offset: 0x0001D791
		// (set) Token: 0x06000609 RID: 1545 RVA: 0x0001F599 File Offset: 0x0001D799
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
					Action<MapNotificationItemBaseVM> onFocus = this.OnFocus;
					if (onFocus == null)
					{
						return;
					}
					onFocus(this);
				}
			}
		}

		// Token: 0x170001AC RID: 428
		// (get) Token: 0x0600060A RID: 1546 RVA: 0x0001F5C8 File Offset: 0x0001D7C8
		// (set) Token: 0x0600060B RID: 1547 RVA: 0x0001F5D0 File Offset: 0x0001D7D0
		[DataSourceProperty]
		public string NotificationIdentifier
		{
			get
			{
				return this._notificationIdentifier;
			}
			set
			{
				if (value != this._notificationIdentifier)
				{
					this._notificationIdentifier = value;
					base.OnPropertyChangedWithValue<string>(value, "NotificationIdentifier");
				}
			}
		}

		// Token: 0x170001AD RID: 429
		// (get) Token: 0x0600060C RID: 1548 RVA: 0x0001F5F3 File Offset: 0x0001D7F3
		// (set) Token: 0x0600060D RID: 1549 RVA: 0x0001F5FB File Offset: 0x0001D7FB
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

		// Token: 0x170001AE RID: 430
		// (get) Token: 0x0600060E RID: 1550 RVA: 0x0001F61E File Offset: 0x0001D81E
		// (set) Token: 0x0600060F RID: 1551 RVA: 0x0001F626 File Offset: 0x0001D826
		[DataSourceProperty]
		public bool ForceInspection
		{
			get
			{
				return this._forceInspection;
			}
			set
			{
				if (value != this._forceInspection)
				{
					Game game = Game.Current;
					if (game != null && !game.IsDevelopmentMode)
					{
						this._forceInspection = value;
						base.OnPropertyChangedWithValue(value, "ForceInspection");
					}
				}
			}
		}

		// Token: 0x170001AF RID: 431
		// (get) Token: 0x06000610 RID: 1552 RVA: 0x0001F65A File Offset: 0x0001D85A
		// (set) Token: 0x06000611 RID: 1553 RVA: 0x0001F662 File Offset: 0x0001D862
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

		// Token: 0x170001B0 RID: 432
		// (get) Token: 0x06000612 RID: 1554 RVA: 0x0001F685 File Offset: 0x0001D885
		// (set) Token: 0x06000613 RID: 1555 RVA: 0x0001F68D File Offset: 0x0001D88D
		[DataSourceProperty]
		public string SoundId
		{
			get
			{
				return this._soundId;
			}
			set
			{
				if (value != this._soundId)
				{
					this._soundId = value;
					base.OnPropertyChangedWithValue<string>(value, "SoundId");
				}
			}
		}

		// Token: 0x04000285 RID: 645
		internal Action<MapNotificationItemBaseVM> OnRemove;

		// Token: 0x04000286 RID: 646
		internal Action<MapNotificationItemBaseVM> OnFocus;

		// Token: 0x04000287 RID: 647
		protected Action _onInspect;

		// Token: 0x04000289 RID: 649
		private readonly TextObject _removeHintTextObject = new TextObject("{=Bcs9s2tC}Right Click to Remove", null);

		// Token: 0x0400028A RID: 650
		private string _removeHintText;

		// Token: 0x0400028B RID: 651
		private InputKeyItemVM _removeInputKey;

		// Token: 0x0400028C RID: 652
		private bool _isFocused;

		// Token: 0x0400028D RID: 653
		private string _titleText;

		// Token: 0x0400028E RID: 654
		private string _descriptionText;

		// Token: 0x0400028F RID: 655
		private string _soundId;

		// Token: 0x04000290 RID: 656
		private bool _forceInspection;

		// Token: 0x04000291 RID: 657
		private string _notificationIdentifier = "Default";
	}
}
