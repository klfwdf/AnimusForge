using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI;

public class SandBoxGauntletGameNotification : GauntletGameNotification
{
	private SoundEvent _currentNotificationSoundEvent;

	public new static void Initialize()
	{
		GauntletGameNotification.Current?.OnFinalize();
		GauntletGameNotification.Current = new SandBoxGauntletGameNotification();
		ScreenManager.AddGlobalLayer(GauntletGameNotification.Current, isFocusable: false);
		GauntletGameNotification.Current.RegisterEvents();
	}

	protected override void OnReceiveNewNotification(GameNotificationItemVM notification)
	{
		base.OnReceiveNewNotification(notification);
		_currentNotificationSoundEvent?.Release();
		_currentNotificationSoundEvent = null;
		if (notification != null && notification.IsDialog)
		{
			_currentNotificationSoundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", notification.DialogSoundPath, null, is3d: false, isBlocking: false);
			_currentNotificationSoundEvent?.Play();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_currentNotificationSoundEvent?.Release();
		_currentNotificationSoundEvent = null;
	}

	public override void RegisterEvents()
	{
		base.RegisterEvents();
		CampaignInformationManager.OnDisplayDialog += _dataSource.AddDialogNotification;
		CampaignInformationManager.OnGetStatusOfDialogNotification += _dataSource.GetStatusOfDialogNotification;
		CampaignInformationManager.OnClearDialogNotification += _dataSource.ClearDialogNotification;
		CampaignInformationManager.IsAnyDialogNotificationActiveOrQueued += _dataSource.GetIsAnyDialogNotificationActiveOrQueued;
		CampaignInformationManager.OnClearAllDialogNotifications += _dataSource.ClearAllDialogNotifications;
	}

	public override void UnregisterEvents()
	{
		base.UnregisterEvents();
		CampaignInformationManager.OnDisplayDialog -= _dataSource.AddDialogNotification;
		CampaignInformationManager.OnGetStatusOfDialogNotification -= _dataSource.GetStatusOfDialogNotification;
		CampaignInformationManager.OnClearDialogNotification -= _dataSource.ClearDialogNotification;
		CampaignInformationManager.IsAnyDialogNotificationActiveOrQueued -= _dataSource.GetIsAnyDialogNotificationActiveOrQueued;
		CampaignInformationManager.OnClearAllDialogNotifications -= _dataSource.ClearAllDialogNotifications;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		TickSoundEvent();
	}

	private void TickSoundEvent()
	{
		if (_currentNotificationSoundEvent == null)
		{
			return;
		}
		if (_dataSource.GotNotification && _dataSource.CurrentNotification.IsDialog)
		{
			if (!_currentNotificationSoundEvent.IsValid || _currentNotificationSoundEvent.IsStopped())
			{
				_currentNotificationSoundEvent.Release();
				_currentNotificationSoundEvent = null;
				_dataSource.FadeOutCurrentNotification(useExtraDisplayTime: true);
			}
			else if (_dataSource.IsPaused && _currentNotificationSoundEvent.IsPlaying())
			{
				_currentNotificationSoundEvent.Pause();
			}
			else if (!_dataSource.IsPaused && _currentNotificationSoundEvent.IsPaused())
			{
				_currentNotificationSoundEvent.Resume();
			}
		}
		else
		{
			_currentNotificationSoundEvent.Release();
			_currentNotificationSoundEvent = null;
		}
	}

	protected override bool GetShouldBeSuspended()
	{
		bool flag = base.GetShouldBeSuspended();
		if (_dataSource.GotNotification && _dataSource.CurrentNotification.IsDialog)
		{
			flag = flag || MBCommon.IsPaused || (GameStateManager.Current?.ActiveStateDisabledByUser ?? false);
		}
		return flag;
	}
}
