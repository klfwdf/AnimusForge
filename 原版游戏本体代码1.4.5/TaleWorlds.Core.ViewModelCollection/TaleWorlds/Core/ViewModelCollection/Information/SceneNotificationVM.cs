using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class SceneNotificationVM : ViewModel
{
	private readonly Action _closeNotification;

	private readonly Action _onPositiveTrigger;

	private readonly Func<string> _getContinueInputText;

	private List<TooltipProperty> _affirmativeHintTooltipProperties;

	private bool _isShown;

	private bool _isReady;

	private object _scene;

	private float _endProgress;

	private string _clickToContinueText;

	private BasicTooltipViewModel _affirmativeHint;

	public SceneNotificationData ActiveData { get; private set; }

	[DataSourceProperty]
	public bool IsShown
	{
		get
		{
			return _isShown;
		}
		set
		{
			if (_isShown != value)
			{
				_isShown = value;
				OnPropertyChangedWithValue(value, "IsShown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsReady
	{
		get
		{
			return _isReady;
		}
		set
		{
			if (_isReady != value)
			{
				_isReady = value;
				OnPropertyChangedWithValue(value, "IsReady");
			}
		}
	}

	[DataSourceProperty]
	public string ClickToContinueText
	{
		get
		{
			return _clickToContinueText;
		}
		set
		{
			if (_clickToContinueText != value)
			{
				_clickToContinueText = value;
				OnPropertyChangedWithValue(value, "ClickToContinueText");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText => ActiveData?.TitleText.ToString() ?? string.Empty;

	[DataSourceProperty]
	public string AffirmativeDescription => ActiveData?.AffirmativeDescriptionText?.ToString() ?? string.Empty;

	[DataSourceProperty]
	public string CancelDescription => ActiveData?.NegativeDescriptionText?.ToString() ?? string.Empty;

	[DataSourceProperty]
	public string SceneID => ActiveData?.SceneID ?? string.Empty;

	[DataSourceProperty]
	public string ButtonOkLabel => ActiveData?.AffirmativeText?.ToString() ?? string.Empty;

	[DataSourceProperty]
	public string ButtonCancelLabel => ActiveData?.NegativeText?.ToString() ?? string.Empty;

	[DataSourceProperty]
	public bool IsButtonOkShown => ActiveData?.IsAffirmativeOptionShown ?? false;

	[DataSourceProperty]
	public bool IsButtonCancelShown => ActiveData?.IsNegativeOptionShown ?? false;

	[DataSourceProperty]
	public string AffirmativeTitleText => ActiveData?.AffirmativeTitleText?.ToString() ?? string.Empty;

	[DataSourceProperty]
	public string NegativeTitleText => ActiveData?.NegativeTitleText?.ToString() ?? string.Empty;

	[DataSourceProperty]
	public object Scene
	{
		get
		{
			return _scene;
		}
		set
		{
			if (_scene != value)
			{
				_scene = value;
				OnPropertyChangedWithValue(value, "Scene");
			}
		}
	}

	[DataSourceProperty]
	public float EndProgress
	{
		get
		{
			return _endProgress;
		}
		set
		{
			if (_endProgress != value)
			{
				_endProgress = value;
				OnPropertyChangedWithValue(value, "EndProgress");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AffirmativeHint
	{
		get
		{
			return _affirmativeHint;
		}
		set
		{
			if (value != _affirmativeHint)
			{
				_affirmativeHint = value;
				OnPropertyChanged("AffirmativeHint");
			}
		}
	}

	public SceneNotificationVM(Action onPositiveTrigger, Action closeNotification, Func<string> getContinueInputText)
	{
		_onPositiveTrigger = onPositiveTrigger;
		_closeNotification = closeNotification;
		_getContinueInputText = getContinueInputText;
		IsShown = false;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ClickToContinueText = _getContinueInputText();
	}

	public void CreateNotification(SceneNotificationData data)
	{
		SetData(data);
	}

	public void ClearData()
	{
		IsShown = false;
		ActiveData = null;
		Scene = null;
		OnPropertyChanged("TitleText");
		OnPropertyChanged("AffirmativeDescription");
		OnPropertyChanged("CancelDescription");
		OnPropertyChanged("SceneID");
		OnPropertyChanged("IsAffirmativeOptionShown");
		OnPropertyChanged("IsNegativeOptionShown");
		OnPropertyChanged("AffirmativeText");
		OnPropertyChanged("NegativeText");
		OnPropertyChanged("AffirmativeAction");
		OnPropertyChanged("NegativeAction");
		OnPropertyChanged("AffirmativeTitleText");
		OnPropertyChanged("NegativeTitleText");
	}

	private void SetData(SceneNotificationData data)
	{
		ActiveData = data;
		OnPropertyChanged("TitleText");
		OnPropertyChanged("AffirmativeDescription");
		OnPropertyChanged("CancelDescription");
		OnPropertyChanged("SceneID");
		OnPropertyChanged("IsButtonOkShown");
		OnPropertyChanged("IsButtonCancelShown");
		OnPropertyChanged("ButtonOkLabel");
		OnPropertyChanged("ButtonCancelLabel");
		OnPropertyChanged("AffirmativeAction");
		OnPropertyChanged("NegativeAction");
		OnPropertyChanged("AffirmativeTitleText");
		OnPropertyChanged("NegativeTitleText");
		SetAffirmativeHintProperties(ActiveData.AffirmativeHintText, ActiveData.AffirmativeHintTextExtended);
		AffirmativeHint = new BasicTooltipViewModel(() => _affirmativeHintTooltipProperties);
		RefreshValues();
		IsShown = true;
	}

	private void SetAffirmativeHintProperties(TextObject defaultHint, TextObject extendedHint)
	{
		_affirmativeHintTooltipProperties = new List<TooltipProperty>();
		if (!string.IsNullOrEmpty(defaultHint?.ToString()))
		{
			if (!string.IsNullOrEmpty(extendedHint?.ToString()))
			{
				_affirmativeHintTooltipProperties.Add(new TooltipProperty("", defaultHint.ToString(), 0)
				{
					OnlyShowWhenNotExtended = true
				});
				_affirmativeHintTooltipProperties.Add(new TooltipProperty("", extendedHint.ToString(), 0, onlyShowWhenExtended: true));
			}
			else
			{
				_affirmativeHintTooltipProperties.Add(new TooltipProperty("", defaultHint.ToString(), 0));
			}
		}
	}

	public void ExecuteAffirmativeProcess()
	{
		_onPositiveTrigger?.Invoke();
		ActiveData?.OnAffirmativeAction();
	}

	public void ExecuteClose()
	{
		SceneNotificationData activeData = ActiveData;
		_closeNotification();
		activeData?.OnCloseAction();
	}

	public void ExecuteNegativeProcess()
	{
		SceneNotificationData activeData = ActiveData;
		_closeNotification();
		activeData?.OnNegativeAction();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}
}
