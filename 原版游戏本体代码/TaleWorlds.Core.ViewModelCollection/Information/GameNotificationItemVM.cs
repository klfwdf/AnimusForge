using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x02000013 RID: 19
	public class GameNotificationItemVM : ViewModel
	{
		// Token: 0x060000EC RID: 236 RVA: 0x00003C88 File Offset: 0x00001E88
		public GameNotificationItemVM(string notificationText, int extraTimeInMs, BasicCharacterObject announcerCharacter, Equipment characterEquipment, string soundId, int priority, bool isDialog, string dialogSoundPath)
		{
			this.GameNotificationText = notificationText;
			this.NotificationSoundId = soundId;
			this.Announcer = ((announcerCharacter != null) ? new CharacterImageIdentifierVM(CharacterCode.CreateFrom(announcerCharacter, characterEquipment)) : new CharacterImageIdentifierVM(null));
			this.CharacterNameText = ((announcerCharacter != null) ? announcerCharacter.Name.ToString() : "");
			this.ExtraTimeInMs = extraTimeInMs;
			this.Priority = priority;
			this.IsDialog = isDialog;
			this.DialogSoundPath = dialogSoundPath;
			if (this.IsDialog)
			{
				this.Handle = new MBInformationManager.DialogNotificationHandle();
			}
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x060000ED RID: 237 RVA: 0x00003D15 File Offset: 0x00001F15
		// (set) Token: 0x060000EE RID: 238 RVA: 0x00003D1D File Offset: 0x00001F1D
		[DataSourceProperty]
		public int ExtraTimeInMs
		{
			get
			{
				return this._extraTimeInMs;
			}
			set
			{
				if (value != this._extraTimeInMs)
				{
					this._extraTimeInMs = value;
					base.OnPropertyChangedWithValue(value, "ExtraTimeInMs");
				}
			}
		}

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x060000EF RID: 239 RVA: 0x00003D3B File Offset: 0x00001F3B
		// (set) Token: 0x060000F0 RID: 240 RVA: 0x00003D43 File Offset: 0x00001F43
		[DataSourceProperty]
		public string GameNotificationText
		{
			get
			{
				return this._gameNotificationText;
			}
			set
			{
				if (value != this._gameNotificationText)
				{
					this._gameNotificationText = value;
					base.OnPropertyChangedWithValue<string>(value, "GameNotificationText");
				}
			}
		}

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x00003D66 File Offset: 0x00001F66
		// (set) Token: 0x060000F2 RID: 242 RVA: 0x00003D6E File Offset: 0x00001F6E
		[DataSourceProperty]
		public string CharacterNameText
		{
			get
			{
				return this._characterNameText;
			}
			set
			{
				if (value != this._characterNameText)
				{
					this._characterNameText = value;
					base.OnPropertyChangedWithValue<string>(value, "CharacterNameText");
				}
			}
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00003D91 File Offset: 0x00001F91
		// (set) Token: 0x060000F4 RID: 244 RVA: 0x00003D99 File Offset: 0x00001F99
		[DataSourceProperty]
		public string NotificationSoundId
		{
			get
			{
				return this._notificationSoundId;
			}
			set
			{
				if (value != this._notificationSoundId)
				{
					this._notificationSoundId = value;
					base.OnPropertyChangedWithValue<string>(value, "NotificationSoundId");
				}
			}
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x00003DBC File Offset: 0x00001FBC
		// (set) Token: 0x060000F6 RID: 246 RVA: 0x00003DC4 File Offset: 0x00001FC4
		[DataSourceProperty]
		public string DialogSoundPath
		{
			get
			{
				return this._dialogSoundPath;
			}
			set
			{
				if (value != this._dialogSoundPath)
				{
					this._dialogSoundPath = value;
					base.OnPropertyChangedWithValue<string>(value, "DialogSoundPath");
				}
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x060000F7 RID: 247 RVA: 0x00003DE7 File Offset: 0x00001FE7
		// (set) Token: 0x060000F8 RID: 248 RVA: 0x00003DEF File Offset: 0x00001FEF
		[DataSourceProperty]
		public CharacterImageIdentifierVM Announcer
		{
			get
			{
				return this._announcer;
			}
			set
			{
				if (value != this._announcer)
				{
					this._announcer = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "Announcer");
				}
			}
		}

		// Token: 0x04000062 RID: 98
		public readonly int Priority;

		// Token: 0x04000063 RID: 99
		public readonly bool IsDialog;

		// Token: 0x04000064 RID: 100
		public readonly MBInformationManager.DialogNotificationHandle Handle;

		// Token: 0x04000065 RID: 101
		private string _gameNotificationText;

		// Token: 0x04000066 RID: 102
		private string _characterNameText;

		// Token: 0x04000067 RID: 103
		private string _notificationSoundId;

		// Token: 0x04000068 RID: 104
		private string _dialogSoundPath;

		// Token: 0x04000069 RID: 105
		private int _extraTimeInMs;

		// Token: 0x0400006A RID: 106
		private CharacterImageIdentifierVM _announcer;
	}
}
