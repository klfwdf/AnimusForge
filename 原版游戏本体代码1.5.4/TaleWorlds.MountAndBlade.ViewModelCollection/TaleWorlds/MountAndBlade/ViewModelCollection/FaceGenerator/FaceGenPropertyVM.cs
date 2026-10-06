using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;

public class FaceGenPropertyVM : ViewModel
{
	public int KeyNo;

	public double PrevValue = -1.0;

	private bool _updateOnValueChange = true;

	private readonly TextObject _nameObj;

	private readonly Action<int, float, bool, bool> _updateFace;

	private readonly Action _resetSliderPrevValuesCommand;

	private readonly Action _addCommand;

	private readonly bool _addCommandOnValueChange;

	private readonly bool _calledFromInit;

	private readonly float _initialValue;

	private int _tabID = -1;

	private string _name;

	private float _value;

	private float _max;

	private float _min;

	private bool _isEnabled;

	private bool _isDiscrete;

	public int KeyTimePoint { get; }

	[DataSourceProperty]
	public float Min
	{
		get
		{
			return _min;
		}
		set
		{
			if (value != _min)
			{
				_min = value;
				OnPropertyChangedWithValue(value, "Min");
			}
		}
	}

	[DataSourceProperty]
	public int TabID
	{
		get
		{
			return _tabID;
		}
		set
		{
			if (value != _tabID)
			{
				_tabID = value;
				OnPropertyChangedWithValue(value, "TabID");
			}
		}
	}

	[DataSourceProperty]
	public float Max
	{
		get
		{
			return _max;
		}
		set
		{
			if (value != _max)
			{
				_max = value;
				OnPropertyChangedWithValue(value, "Max");
			}
		}
	}

	[DataSourceProperty]
	public float Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (TaleWorlds.Library.MathF.Abs(value - _value) > 0.01f)
			{
				if (!_calledFromInit && PrevValue < 0.0 && _updateOnValueChange && _addCommandOnValueChange)
				{
					_addCommand();
				}
				_resetSliderPrevValuesCommand();
				if (KeyNo >= 0)
				{
					PrevValue = _value;
				}
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
				_updateFace?.Invoke(KeyNo, value, _calledFromInit, _updateOnValueChange);
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDiscrete
	{
		get
		{
			return _isDiscrete;
		}
		set
		{
			if (value != _isDiscrete)
			{
				_isDiscrete = value;
				OnPropertyChangedWithValue(value, "IsDiscrete");
			}
		}
	}

	public FaceGenPropertyVM(int keyNo, double min, double max, TextObject name, int keyTimePoint, int tabId, double value, float initialValue, Action<int, float, bool, bool> updateFace, Action addCommand, Action resetSliderPrevValuesCommand, bool isEnabled = true, bool isDiscrete = false, bool addCommandOnValueChange = true)
	{
		_calledFromInit = true;
		_updateFace = updateFace;
		_addCommand = addCommand;
		_nameObj = name;
		_resetSliderPrevValuesCommand = resetSliderPrevValuesCommand;
		KeyNo = keyNo;
		Min = (float)min;
		Max = (float)max;
		KeyTimePoint = keyTimePoint;
		TabID = tabId;
		_initialValue = initialValue;
		Value = (float)value;
		PrevValue = -1.0;
		IsEnabled = isEnabled;
		IsDiscrete = isDiscrete;
		_calledFromInit = false;
		_addCommandOnValueChange = addCommandOnValueChange;
		RefreshValues();
	}

	public void Reset()
	{
		_updateOnValueChange = false;
		Value = _initialValue;
		_updateOnValueChange = true;
	}

	public void Randomize()
	{
		_updateOnValueChange = false;
		float num = 0.5f * (MBRandom.RandomFloat + MBRandom.RandomFloat);
		Value = num * (Max - Min) + Min;
		_updateOnValueChange = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameObj.ToString();
	}

	public void AddCommand()
	{
		_addCommand();
	}
}
