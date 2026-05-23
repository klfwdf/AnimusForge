using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.KillFeed;

public class MultiplayerPersonalKillFeedItemWidget : Widget
{
	private float _speedModifier = 1f;

	private float _maxTargetAlpha = 1f;

	private bool _initialized;

	private string _goldGainedSound = "multiplayer/coin_add";

	private bool _isDamage;

	private int _itemType;

	private int _amount = -1;

	private string _message;

	public Widget NotificationTypeIconWidget { get; set; }

	public Widget NotificationBackgroundWidget { get; set; }

	public TextWidget AmountTextWidget { get; set; }

	public RichTextWidget MessageTextWidget { get; set; }

	public float FadeInTime { get; set; } = 1f;

	public float StayTime { get; set; } = 3f;

	public float FadeOutTime { get; set; } = 0.5f;

	private float CurrentAlpha => base.AlphaFactor;

	public float TimeSinceCreation { get; private set; }

	public bool IsDamage
	{
		get
		{
			return _isDamage;
		}
		set
		{
			if (value != _isDamage)
			{
				_isDamage = value;
				OnPropertyChanged(value, "IsDamage");
			}
		}
	}

	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			if (value != _message)
			{
				_message = value;
				OnPropertyChanged(value, "Message");
			}
		}
	}

	public int ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (value != _itemType)
			{
				_itemType = value;
				OnPropertyChanged(value, "ItemType");
			}
		}
	}

	public int Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			if (value != _amount)
			{
				_amount = value;
				OnPropertyChanged(value, "Amount");
			}
		}
	}

	public MultiplayerPersonalKillFeedItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			base.PositionYOffset = 0f;
		}
		if (!_initialized)
		{
			this.SetGlobalAlphaRecursively(0f);
			UpdateNotificationBackgroundWidget();
			UpdateNotificationTypeIconWidget();
			UpdateNotificationMessageWidget();
			UpdateNotificationAmountWidget();
			DetermineSoundEvent();
			_initialized = true;
		}
		UpdateAlphaValues(dt);
	}

	private void DetermineSoundEvent()
	{
		if (ItemType == 6)
		{
			base.Context.TwoDimensionContext.PlaySound(_goldGainedSound);
		}
	}

	private void UpdateAlphaValues(float dt)
	{
		float end = 0f;
		float amount = 0f;
		TimeSinceCreation += dt;
		if (_maxTargetAlpha == 0f)
		{
			EventFired("OnRemove");
			return;
		}
		if (TimeSinceCreation <= FadeInTime)
		{
			end = MathF.Min(1f, _maxTargetAlpha);
			amount = TimeSinceCreation / FadeInTime;
		}
		else if (TimeSinceCreation - FadeInTime <= StayTime)
		{
			end = MathF.Min(1f, _maxTargetAlpha);
			amount = 1f;
		}
		else if (TimeSinceCreation - (FadeInTime + StayTime) <= FadeOutTime)
		{
			end = 0f;
			amount = (TimeSinceCreation - (FadeInTime + StayTime)) / FadeOutTime;
			if (CurrentAlpha <= 0.1f)
			{
				EventFired("OnRemove");
			}
		}
		else
		{
			EventFired("OnRemove");
		}
		this.SetGlobalAlphaRecursively(Mathf.Lerp(CurrentAlpha, end, amount));
	}

	public void SetSpeedModifier(float newSpeed)
	{
		if (newSpeed > _speedModifier)
		{
			_speedModifier = newSpeed;
		}
	}

	public void SetMaxAlphaValue(float newMaxAlpha)
	{
		if (newMaxAlpha < _maxTargetAlpha)
		{
			_maxTargetAlpha = newMaxAlpha;
		}
	}

	private void UpdateNotificationTypeIconWidget()
	{
		if (ItemType == 0)
		{
			NotificationTypeIconWidget.IsVisible = false;
			return;
		}
		switch (ItemType)
		{
		case 1:
			NotificationTypeIconWidget.SetState("FriendlyFireDamage");
			break;
		case 2:
			NotificationTypeIconWidget.SetState("FriendlyFireKill");
			break;
		case 3:
			NotificationTypeIconWidget.SetState("MountDamage");
			break;
		case 4:
			NotificationTypeIconWidget.SetState("NormalKill");
			break;
		case 5:
			NotificationTypeIconWidget.SetState("Assist");
			break;
		case 6:
			NotificationTypeIconWidget.SetState("GoldChange");
			break;
		case 7:
			NotificationTypeIconWidget.SetState("NormalKillHeadshot");
			break;
		default:
			Debug.FailedAssert("Undefined personal feed notification type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Multiplayer\\KillFeed\\MultiplayerPersonalKillFeedItemWidget.cs", "UpdateNotificationTypeIconWidget", 172);
			NotificationTypeIconWidget.IsVisible = false;
			break;
		}
	}

	private void UpdateNotificationMessageWidget()
	{
		MessageTextWidget.Text = Message;
		if (string.IsNullOrEmpty(Message))
		{
			MessageTextWidget.IsVisible = false;
			return;
		}
		switch (ItemType)
		{
		case 1:
		case 2:
			MessageTextWidget.SetState("FriendlyFire");
			break;
		case 0:
		case 3:
		case 4:
		case 5:
		case 7:
			MessageTextWidget.SetState("Normal");
			break;
		case 6:
			if (Amount >= 0)
			{
				MessageTextWidget.SetState("GoldChangePositive");
			}
			else
			{
				MessageTextWidget.SetState("GoldChangeNegative");
			}
			break;
		default:
			Debug.FailedAssert("Undefined personal feed notification type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Multiplayer\\KillFeed\\MultiplayerPersonalKillFeedItemWidget.cs", "UpdateNotificationMessageWidget", 213);
			MessageTextWidget.IsVisible = false;
			break;
		}
	}

	private void UpdateNotificationAmountWidget()
	{
		if (ItemType != 6 && Amount == -1)
		{
			AmountTextWidget.IsVisible = false;
			return;
		}
		switch (ItemType)
		{
		case 1:
		case 2:
			AmountTextWidget.SetState("FriendlyFire");
			AmountTextWidget.IntText = Amount;
			break;
		case 0:
		case 3:
		case 4:
		case 7:
			AmountTextWidget.SetState("Normal");
			AmountTextWidget.IntText = Amount;
			break;
		case 5:
			AmountTextWidget.IsVisible = false;
			break;
		case 6:
			if (Amount >= 0)
			{
				AmountTextWidget.SetState("GoldChangePositive");
				AmountTextWidget.Text = "+" + Amount;
			}
			else
			{
				AmountTextWidget.SetState("GoldChangeNegative");
				AmountTextWidget.Text = Amount.ToString();
			}
			break;
		default:
			Debug.FailedAssert("Undefined personal feed notification type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Multiplayer\\KillFeed\\MultiplayerPersonalKillFeedItemWidget.cs", "UpdateNotificationAmountWidget", 259);
			AmountTextWidget.IsVisible = false;
			break;
		}
	}

	private void UpdateNotificationBackgroundWidget()
	{
		switch (ItemType)
		{
		case 0:
		case 1:
		case 3:
			NotificationBackgroundWidget.SetState("Hidden");
			break;
		case 2:
			NotificationBackgroundWidget.SetState("FriendlyFire");
			break;
		case 4:
		case 7:
			NotificationBackgroundWidget.SetState("Normal");
			break;
		case 6:
			if (Amount >= 0)
			{
				NotificationBackgroundWidget.SetState("GoldChangePositive");
			}
			else
			{
				NotificationBackgroundWidget.SetState("GoldChangeNegative");
			}
			break;
		default:
			Debug.FailedAssert("Undefined personal feed notification type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Multiplayer\\KillFeed\\MultiplayerPersonalKillFeedItemWidget.cs", "UpdateNotificationBackgroundWidget", 295);
			NotificationBackgroundWidget.SetState("Hidden");
			break;
		case 5:
			break;
		}
	}

	private float GetInitialVerticalPositionOfSelf()
	{
		float num = 0f;
		for (int i = 0; i < GetSiblingIndex(); i++)
		{
			num += base.ParentWidget.GetChild(i).Size.Y * base._inverseScaleToUse;
		}
		return num;
	}
}
