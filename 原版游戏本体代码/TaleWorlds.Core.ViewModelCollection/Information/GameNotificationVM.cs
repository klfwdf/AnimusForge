using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x02000014 RID: 20
	public class GameNotificationVM : ViewModel
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060000F9 RID: 249 RVA: 0x00003E10 File Offset: 0x00002010
		// (remove) Token: 0x060000FA RID: 250 RVA: 0x00003E48 File Offset: 0x00002048
		public event Action<GameNotificationItemVM> CurrentNotificationChanged;

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060000FB RID: 251 RVA: 0x00003E80 File Offset: 0x00002080
		private float CurrentNotificationOnScreenTime
		{
			get
			{
				float num = 1f;
				num += (float)this.CurrentNotification.ExtraTimeInMs / 1000f;
				int numberOfWords = this.GetNumberOfWords(this.CurrentNotification.GameNotificationText);
				if (numberOfWords > 4)
				{
					num += (float)(numberOfWords - 4) / 5f;
				}
				if (this.CurrentNotification.IsDialog)
				{
					num += 10000f;
				}
				return num + 1f / (float)(this._items.Count + 1);
			}
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00003EF8 File Offset: 0x000020F8
		public void FadeOutCurrentNotification(bool useExtraDisplayTime = false)
		{
			if (this.GotNotification)
			{
				this.Timer = this.TotalTime - 0.2f;
				if (useExtraDisplayTime)
				{
					this.Timer -= (float)this.CurrentNotification.ExtraTimeInMs / 1000f;
				}
			}
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00003F38 File Offset: 0x00002138
		public void SkipCurrentNotification()
		{
			this.Timer = 0f;
			if (this._items.Count > 0)
			{
				this.CurrentNotification = this._items[0];
				this._items.RemoveAt(0);
				this.TotalTime = this.CurrentNotificationOnScreenTime;
				return;
			}
			this.GotNotification = false;
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00003F90 File Offset: 0x00002190
		public GameNotificationVM()
		{
			this._items = new List<GameNotificationItemVM>();
			this.CurrentNotification = new GameNotificationItemVM("NULL", 0, null, null, "NULL", 0, false, null);
			this.GotNotification = false;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00003FD0 File Offset: 0x000021D0
		public void ClearNotifications()
		{
			this._items.Clear();
			this.GotNotification = false;
			this.Timer = this.CurrentNotificationOnScreenTime * 2f;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00003FF8 File Offset: 0x000021F8
		public void Tick(float dt)
		{
			if (this.IsPaused)
			{
				return;
			}
			this.Timer += dt;
			if (this.GotNotification && this.Timer >= this.CurrentNotificationOnScreenTime)
			{
				this.Timer = 0f;
				if (this._items.Count > 0)
				{
					this.CurrentNotification = this._items[0];
					this._items.RemoveAt(0);
					this.TotalTime = this.CurrentNotificationOnScreenTime;
					return;
				}
				this.GotNotification = false;
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00004080 File Offset: 0x00002280
		public MBInformationManager.DialogNotificationHandle AddDialogNotification(TextObject text, int extraTimeInMs, BasicCharacterObject announcerCharacter, Equipment equipment, MBInformationManager.NotificationPriority priority, string dialogSoundPath)
		{
			GameNotificationItemVM gameNotificationItemVM = new GameNotificationItemVM(text.ToString(), extraTimeInMs, announcerCharacter, equipment, null, (int)priority, true, dialogSoundPath);
			if (this.GotNotification && this.CurrentNotification.GameNotificationText == text.ToString())
			{
				return this.CurrentNotification.Handle;
			}
			if (this._items.Any((GameNotificationItemVM i) => i.GameNotificationText == text.ToString()))
			{
				return this._items.First((GameNotificationItemVM i) => i.GameNotificationText == text.ToString()).Handle;
			}
			if (this.GotNotification && this.CurrentNotification.Priority >= (int)priority)
			{
				int index = this._items.FindLastIndex((GameNotificationItemVM i) => i.Priority >= (int)priority) + 1;
				this._items.Insert(index, gameNotificationItemVM);
			}
			else
			{
				this.CurrentNotification = gameNotificationItemVM;
				this.TotalTime = this.CurrentNotificationOnScreenTime;
				this.GotNotification = true;
				this.Timer = 0f;
			}
			return gameNotificationItemVM.Handle;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00004198 File Offset: 0x00002398
		public MBInformationManager.NotificationStatus GetStatusOfDialogNotification(MBInformationManager.DialogNotificationHandle handle)
		{
			if (handle == null)
			{
				return MBInformationManager.NotificationStatus.Inactive;
			}
			if (this.GotNotification && this.CurrentNotification.Handle == handle)
			{
				return MBInformationManager.NotificationStatus.CurrentlyActive;
			}
			if (this._items.Any((GameNotificationItemVM i) => i.Handle == handle))
			{
				return MBInformationManager.NotificationStatus.InQueue;
			}
			return MBInformationManager.NotificationStatus.Inactive;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000041F8 File Offset: 0x000023F8
		public void ClearDialogNotification(MBInformationManager.DialogNotificationHandle handle, bool fadeOut)
		{
			if (handle == null)
			{
				return;
			}
			this._items.RemoveAll((GameNotificationItemVM x) => x.Handle == handle);
			if (this.GotNotification && this.CurrentNotification.Handle == handle)
			{
				if (fadeOut)
				{
					this.FadeOutCurrentNotification(false);
					return;
				}
				this.SkipCurrentNotification();
			}
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00004260 File Offset: 0x00002460
		public bool GetIsAnyDialogNotificationActiveOrQueued()
		{
			if (!this.GotNotification || !this.CurrentNotification.IsDialog)
			{
				return this._items.Any((GameNotificationItemVM x) => x.IsDialog);
			}
			return true;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x000042B0 File Offset: 0x000024B0
		public void ClearAllDialogNotifications(bool fadeOut)
		{
			this._items.RemoveAll((GameNotificationItemVM x) => x.IsDialog);
			if (this.GotNotification && this.CurrentNotification.IsDialog)
			{
				if (fadeOut)
				{
					this.FadeOutCurrentNotification(false);
					return;
				}
				this.SkipCurrentNotification();
			}
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00004310 File Offset: 0x00002510
		public void AddGameNotification(string notificationText, int extraTimeInMs, BasicCharacterObject announcerCharacter, Equipment equipment, string soundId)
		{
			if (string.IsNullOrEmpty(notificationText))
			{
				Debug.FailedAssert("Quick information message is empty", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core.ViewModelCollection\\Information\\GameNotificationVM.cs", "AddGameNotification", 216);
			}
			GameNotificationItemVM gameNotificationItemVM = new GameNotificationItemVM(notificationText, extraTimeInMs, announcerCharacter, equipment, soundId, 0, false, null);
			if ((!this.GotNotification || this.CurrentNotification.GameNotificationText != notificationText) && !this._items.Any((GameNotificationItemVM i) => i.GameNotificationText == notificationText))
			{
				if (this.GotNotification)
				{
					this._items.Add(gameNotificationItemVM);
					return;
				}
				this.CurrentNotification = gameNotificationItemVM;
				this.TotalTime = this.CurrentNotificationOnScreenTime;
				this.GotNotification = true;
				this.Timer = 0f;
			}
		}

		// Token: 0x06000107 RID: 263 RVA: 0x000043D8 File Offset: 0x000025D8
		private int GetNumberOfWords(string text)
		{
			string text2 = text.Trim();
			int num = 0;
			int i = 0;
			while (i < text2.Length)
			{
				while (i < text2.Length && !char.IsWhiteSpace(text2[i]))
				{
					i++;
				}
				num++;
				while (i < text2.Length && char.IsWhiteSpace(text2[i]))
				{
					i++;
				}
			}
			return num;
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000108 RID: 264 RVA: 0x00004438 File Offset: 0x00002638
		// (set) Token: 0x06000109 RID: 265 RVA: 0x00004440 File Offset: 0x00002640
		[DataSourceProperty]
		public GameNotificationItemVM CurrentNotification
		{
			get
			{
				return this._currentNotification;
			}
			set
			{
				if (this._currentNotification != value)
				{
					this._currentNotification = value;
					int notificationId = this.NotificationId;
					this.NotificationId = notificationId + 1;
					base.OnPropertyChangedWithValue<GameNotificationItemVM>(value, "CurrentNotification");
					Action<GameNotificationItemVM> currentNotificationChanged = this.CurrentNotificationChanged;
					if (currentNotificationChanged == null)
					{
						return;
					}
					currentNotificationChanged(value);
				}
			}
		}

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x0600010A RID: 266 RVA: 0x0000448A File Offset: 0x0000268A
		// (set) Token: 0x0600010B RID: 267 RVA: 0x00004492 File Offset: 0x00002692
		[DataSourceProperty]
		public bool GotNotification
		{
			get
			{
				return this._gotNotification;
			}
			set
			{
				if (value != this._gotNotification)
				{
					this._gotNotification = value;
					base.OnPropertyChangedWithValue(value, "GotNotification");
				}
			}
		}

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600010C RID: 268 RVA: 0x000044B0 File Offset: 0x000026B0
		// (set) Token: 0x0600010D RID: 269 RVA: 0x000044B8 File Offset: 0x000026B8
		[DataSourceProperty]
		public int NotificationId
		{
			get
			{
				return this._notificationId;
			}
			set
			{
				if (value != this._notificationId)
				{
					this._notificationId = value;
					base.OnPropertyChangedWithValue(value, "NotificationId");
				}
			}
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x0600010E RID: 270 RVA: 0x000044D6 File Offset: 0x000026D6
		// (set) Token: 0x0600010F RID: 271 RVA: 0x000044DE File Offset: 0x000026DE
		[DataSourceProperty]
		public float TotalTime
		{
			get
			{
				return this._totalTime;
			}
			set
			{
				if (value != this._totalTime)
				{
					this._totalTime = value;
					base.OnPropertyChangedWithValue(value, "TotalTime");
				}
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x06000110 RID: 272 RVA: 0x000044FC File Offset: 0x000026FC
		// (set) Token: 0x06000111 RID: 273 RVA: 0x00004504 File Offset: 0x00002704
		[DataSourceProperty]
		public float Timer
		{
			get
			{
				return this._timer;
			}
			set
			{
				if (value != this._timer)
				{
					this._timer = value;
					base.OnPropertyChangedWithValue(value, "Timer");
				}
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000112 RID: 274 RVA: 0x00004522 File Offset: 0x00002722
		// (set) Token: 0x06000113 RID: 275 RVA: 0x0000452A File Offset: 0x0000272A
		[DataSourceProperty]
		public bool IsPaused
		{
			get
			{
				return this._isPaused;
			}
			set
			{
				if (value != this._isPaused)
				{
					this._isPaused = value;
					base.OnPropertyChangedWithValue(value, "IsPaused");
				}
			}
		}

		// Token: 0x0400006C RID: 108
		private readonly List<GameNotificationItemVM> _items;

		// Token: 0x0400006D RID: 109
		private const float MinimumDisplayTimeInSeconds = 1f;

		// Token: 0x0400006E RID: 110
		private const float ExtraDisplayTimeInSeconds = 1f;

		// Token: 0x0400006F RID: 111
		private GameNotificationItemVM _currentNotification;

		// Token: 0x04000070 RID: 112
		private bool _gotNotification;

		// Token: 0x04000071 RID: 113
		private int _notificationId;

		// Token: 0x04000072 RID: 114
		private float _totalTime;

		// Token: 0x04000073 RID: 115
		private float _timer;

		// Token: 0x04000074 RID: 116
		private bool _isPaused;
	}
}
