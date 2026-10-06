using System;

namespace TaleWorlds.Library;

public abstract class TooltipBaseVM : ViewModel
{
	protected readonly Type _invokedType;

	protected object[] _invokedArgs;

	protected bool _isPeriodicRefreshEnabled;

	protected float _periodicRefreshDelay;

	private float _periodicRefreshTimer;

	private bool _isActive;

	private bool _isExtended;

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsExtended
	{
		get
		{
			return _isExtended;
		}
		set
		{
			if (_isExtended != value)
			{
				_isExtended = value;
				OnPropertyChangedWithValue(value, "IsExtended");
				OnIsExtendedChanged();
			}
		}
	}

	public TooltipBaseVM(Type invokedType, object[] invokedArgs)
	{
		_invokedType = invokedType;
		_invokedArgs = invokedArgs;
	}

	public override void OnFinalize()
	{
		OnFinalizeInternal();
		_invokedArgs = null;
	}

	protected virtual void OnFinalizeInternal()
	{
	}

	public virtual void Tick(float dt)
	{
		if (IsActive && _isPeriodicRefreshEnabled)
		{
			_periodicRefreshTimer -= dt;
			if (_periodicRefreshTimer < 0f)
			{
				OnPeriodicRefresh();
				_periodicRefreshTimer = _periodicRefreshDelay;
			}
		}
		else
		{
			_periodicRefreshTimer = _periodicRefreshDelay;
		}
	}

	protected void InvokeRefreshData<T>(T tooltip) where T : TooltipBaseVM
	{
		if (InformationManager.RegisteredTypes.TryGetValue(_invokedType, out var value) && value.OnRefreshData is Action<T, object[]> action)
		{
			action(tooltip, _invokedArgs);
		}
	}

	protected virtual void OnPeriodicRefresh()
	{
	}

	protected virtual void OnIsExtendedChanged()
	{
	}
}
