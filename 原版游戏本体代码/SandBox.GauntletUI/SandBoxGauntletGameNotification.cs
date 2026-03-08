using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI
{
	// Token: 0x02000011 RID: 17
	public class SandBoxGauntletGameNotification : GauntletGameNotification
	{
		// Token: 0x060000C7 RID: 199 RVA: 0x000076F0 File Offset: 0x000058F0
		public new static void Initialize()
		{
			GauntletGameNotification gauntletGameNotification = GauntletGameNotification.Current;
			if (gauntletGameNotification != null)
			{
				gauntletGameNotification.OnFinalize();
			}
			GauntletGameNotification.Current = new SandBoxGauntletGameNotification();
			ScreenManager.AddGlobalLayer(GauntletGameNotification.Current, false);
			GauntletGameNotification.Current.RegisterEvents();
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00007724 File Offset: 0x00005924
		protected override void OnReceiveNewNotification(GameNotificationItemVM notification)
		{
			base.OnReceiveNewNotification(notification);
			SoundEvent currentNotificationSoundEvent = this._currentNotificationSoundEvent;
			if (currentNotificationSoundEvent != null)
			{
				currentNotificationSoundEvent.Release();
			}
			this._currentNotificationSoundEvent = null;
			if (notification != null && notification.IsDialog)
			{
				this._currentNotificationSoundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", notification.DialogSoundPath, null, false, false);
				SoundEvent currentNotificationSoundEvent2 = this._currentNotificationSoundEvent;
				if (currentNotificationSoundEvent2 == null)
				{
					return;
				}
				currentNotificationSoundEvent2.Play();
			}
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00007785 File Offset: 0x00005985
		public override void OnFinalize()
		{
			base.OnFinalize();
			SoundEvent currentNotificationSoundEvent = this._currentNotificationSoundEvent;
			if (currentNotificationSoundEvent != null)
			{
				currentNotificationSoundEvent.Release();
			}
			this._currentNotificationSoundEvent = null;
		}

		// Token: 0x060000CA RID: 202 RVA: 0x000077A8 File Offset: 0x000059A8
		public override void RegisterEvents()
		{
			base.RegisterEvents();
			CampaignInformationManager.OnDisplayDialog += this._dataSource.AddDialogNotification;
			CampaignInformationManager.OnGetStatusOfDialogNotification += this._dataSource.GetStatusOfDialogNotification;
			CampaignInformationManager.OnClearDialogNotification += this._dataSource.ClearDialogNotification;
			CampaignInformationManager.IsAnyDialogNotificationActiveOrQueued += this._dataSource.GetIsAnyDialogNotificationActiveOrQueued;
			CampaignInformationManager.OnClearAllDialogNotifications += this._dataSource.ClearAllDialogNotifications;
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000782C File Offset: 0x00005A2C
		public override void UnregisterEvents()
		{
			base.UnregisterEvents();
			CampaignInformationManager.OnDisplayDialog -= this._dataSource.AddDialogNotification;
			CampaignInformationManager.OnGetStatusOfDialogNotification -= this._dataSource.GetStatusOfDialogNotification;
			CampaignInformationManager.OnClearDialogNotification -= this._dataSource.ClearDialogNotification;
			CampaignInformationManager.IsAnyDialogNotificationActiveOrQueued -= this._dataSource.GetIsAnyDialogNotificationActiveOrQueued;
			CampaignInformationManager.OnClearAllDialogNotifications -= this._dataSource.ClearAllDialogNotifications;
		}

		// Token: 0x060000CC RID: 204 RVA: 0x000078AD File Offset: 0x00005AAD
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			this.TickSoundEvent();
		}

		// Token: 0x060000CD RID: 205 RVA: 0x000078BC File Offset: 0x00005ABC
		private void TickSoundEvent()
		{
			if (this._currentNotificationSoundEvent != null)
			{
				if (this._dataSource.GotNotification && this._dataSource.CurrentNotification.IsDialog)
				{
					if (this._latestIsSoundPaused != this._dataSource.IsPaused)
					{
						this._latestIsSoundPaused = this._dataSource.IsPaused;
						if (this._latestIsSoundPaused && this._currentNotificationSoundEvent.IsPlaying())
						{
							this._currentNotificationSoundEvent.Pause();
							return;
						}
						if (!this._latestIsSoundPaused && this._currentNotificationSoundEvent.IsPaused())
						{
							this._currentNotificationSoundEvent.Resume();
							return;
						}
					}
					else if (!this._latestIsSoundPaused && !this._currentNotificationSoundEvent.IsPlaying())
					{
						this._currentNotificationSoundEvent.Release();
						this._currentNotificationSoundEvent = null;
						this._dataSource.FadeOutCurrentNotification(true);
						return;
					}
				}
				else
				{
					this._currentNotificationSoundEvent.Release();
					this._currentNotificationSoundEvent = null;
				}
			}
		}

		// Token: 0x060000CE RID: 206 RVA: 0x000079A8 File Offset: 0x00005BA8
		protected override bool GetShouldBeSuspended()
		{
			bool flag = base.GetShouldBeSuspended();
			if (this._dataSource.GotNotification && this._dataSource.CurrentNotification.IsDialog)
			{
				bool flag2;
				if (!flag && !MBCommon.IsPaused)
				{
					GameStateManager gameStateManager = GameStateManager.Current;
					flag2 = gameStateManager != null && gameStateManager.ActiveStateDisabledByUser;
				}
				else
				{
					flag2 = true;
				}
				flag = flag2;
			}
			return flag;
		}

		// Token: 0x04000057 RID: 87
		private SoundEvent _currentNotificationSoundEvent;

		// Token: 0x04000058 RID: 88
		private bool _latestIsSoundPaused;
	}
}
