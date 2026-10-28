using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Inquiries;

public class SingleQueryPopUpVM : PopUpBaseVM
{
	private InquiryData _data;

	private float _queryTimer;

	private string _lastButtonOkHint;

	private string _lastButtonCancelHint;

	private float _remainingQueryTime;

	private float _totalQueryTime;

	private bool _isTimerShown;

	[DataSourceProperty]
	public float RemainingQueryTime
	{
		get
		{
			return _remainingQueryTime;
		}
		set
		{
			if (value != _remainingQueryTime)
			{
				_remainingQueryTime = value;
				OnPropertyChangedWithValue(value, "RemainingQueryTime");
			}
		}
	}

	[DataSourceProperty]
	public float TotalQueryTime
	{
		get
		{
			return _totalQueryTime;
		}
		set
		{
			if (value != _totalQueryTime)
			{
				_totalQueryTime = value;
				OnPropertyChangedWithValue(value, "TotalQueryTime");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTimerShown
	{
		get
		{
			return _isTimerShown;
		}
		set
		{
			if (value != _isTimerShown)
			{
				_isTimerShown = value;
				OnPropertyChangedWithValue(value, "IsTimerShown");
			}
		}
	}

	public SingleQueryPopUpVM(Action closeQuery)
		: base(closeQuery)
	{
		base.ButtonOkHint = new HintViewModel();
		base.ButtonCancelHint = new HintViewModel();
	}

	public override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_data == null)
		{
			return;
		}
		UpdateButtonEnabledStates();
		if (_data.ExpireTime > 0f)
		{
			if (_queryTimer > _data.ExpireTime)
			{
				_data.TimeoutAction?.Invoke();
				CloseQuery();
			}
			else
			{
				_queryTimer += dt;
				RemainingQueryTime = _data.ExpireTime - _queryTimer;
			}
		}
	}

	public override void ExecuteAffirmativeAction()
	{
		_data.AffirmativeAction?.Invoke();
		CloseQuery();
	}

	public override void ExecuteNegativeAction()
	{
		_data.NegativeAction?.Invoke();
		CloseQuery();
	}

	public override void OnClearData()
	{
		base.OnClearData();
		_data = null;
	}

	private void UpdateButtonEnabledStates()
	{
		if (_data.GetIsAffirmativeOptionEnabled != null)
		{
			(bool, string) tuple = _data.GetIsAffirmativeOptionEnabled();
			(base.IsButtonOkEnabled, _) = tuple;
			if (!string.Equals(_lastButtonOkHint, tuple.Item2, StringComparison.OrdinalIgnoreCase))
			{
				base.ButtonOkHint.HintText = (string.IsNullOrEmpty(tuple.Item2) ? TextObject.GetEmpty() : new TextObject("{=!}" + tuple.Item2));
				_lastButtonOkHint = tuple.Item2;
			}
		}
		else
		{
			base.IsButtonOkEnabled = true;
			base.ButtonOkHint.HintText = TextObject.GetEmpty();
			_lastButtonOkHint = string.Empty;
		}
		if (_data.GetIsNegativeOptionEnabled != null)
		{
			(bool, string) tuple3 = _data.GetIsNegativeOptionEnabled();
			(base.IsButtonCancelEnabled, _) = tuple3;
			if (!string.Equals(_lastButtonCancelHint, tuple3.Item2, StringComparison.OrdinalIgnoreCase))
			{
				base.ButtonCancelHint.HintText = (string.IsNullOrEmpty(tuple3.Item2) ? TextObject.GetEmpty() : new TextObject("{=!}" + tuple3.Item2));
				_lastButtonCancelHint = tuple3.Item2;
			}
		}
		else
		{
			base.IsButtonCancelEnabled = true;
			base.ButtonCancelHint.HintText = TextObject.GetEmpty();
			_lastButtonCancelHint = string.Empty;
		}
	}

	public void SetData(InquiryData data)
	{
		_data = data;
		base.TitleText = _data.TitleText;
		base.PopUpLabel = _data.Text;
		base.ButtonOkLabel = _data.AffirmativeText;
		base.ButtonCancelLabel = _data.NegativeText;
		base.IsButtonOkShown = _data.IsAffirmativeOptionShown;
		base.IsButtonCancelShown = _data.IsNegativeOptionShown;
		IsTimerShown = _data.ExpireTime > 0f;
		base.IsButtonOkEnabled = true;
		base.IsButtonCancelEnabled = true;
		UpdateButtonEnabledStates();
		_queryTimer = 0f;
		TotalQueryTime = TaleWorlds.Library.MathF.Round(_data.ExpireTime);
	}
}
