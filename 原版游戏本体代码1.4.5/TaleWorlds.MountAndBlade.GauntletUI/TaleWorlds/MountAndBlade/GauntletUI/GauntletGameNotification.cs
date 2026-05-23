using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletGameNotification : GlobalLayer
{
	protected GameNotificationVM _dataSource;

	private readonly GauntletLayer _layer;

	private bool _isSuspended;

	protected static GauntletGameNotification Current { get; set; }

	protected virtual string MovieName => "GameNotificationUI";

	protected GauntletGameNotification()
	{
		_dataSource = new GameNotificationVM();
		_dataSource.CurrentNotificationChanged += OnReceiveNewNotification;
		_layer = new GauntletLayer("GameNotification", 19007);
		_layer.LoadMovie(MovieName, _dataSource);
		base.Layer = _layer;
		_layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Mouse);
	}

	protected virtual void OnReceiveNewNotification(GameNotificationItemVM notification)
	{
		if (!string.IsNullOrEmpty(notification?.NotificationSoundId))
		{
			SoundEvent.PlaySound2D(notification.NotificationSoundId);
		}
	}

	public static void Initialize()
	{
		Current?.OnFinalize();
		Current = new GauntletGameNotification();
		ScreenManager.AddGlobalLayer(Current, isFocusable: false);
		Current.RegisterEvents();
	}

	public virtual void OnFinalize()
	{
		_dataSource?.ClearNotifications();
		UnregisterEvents();
		ScreenManager.RemoveGlobalLayer(this);
		_dataSource = null;
	}

	public virtual void RegisterEvents()
	{
		MBInformationManager.FiringQuickInformation += _dataSource.AddGameNotification;
	}

	public virtual void UnregisterEvents()
	{
		MBInformationManager.FiringQuickInformation -= _dataSource.AddGameNotification;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		bool shouldBeSuspended = GetShouldBeSuspended();
		if (shouldBeSuspended != _isSuspended)
		{
			ScreenManager.SetSuspendLayer(Current._layer, shouldBeSuspended);
			_isSuspended = shouldBeSuspended;
		}
		_dataSource.IsPaused = _isSuspended;
	}

	protected virtual bool GetShouldBeSuspended()
	{
		if (!GauntletSceneNotification.Current.IsActive)
		{
			return LoadingWindow.IsLoadingWindowActive;
		}
		return true;
	}
}
