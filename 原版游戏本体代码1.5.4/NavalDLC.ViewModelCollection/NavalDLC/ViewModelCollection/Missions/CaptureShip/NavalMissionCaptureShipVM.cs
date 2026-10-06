using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Missions.CaptureShip;

public class NavalMissionCaptureShipVM : ViewModel
{
	private float _maxTime;

	private float _currentTime;

	private string _captureShipText;

	private bool _isCapturing;

	[DataSourceProperty]
	public float MaxTime
	{
		get
		{
			return _maxTime;
		}
		set
		{
			if (value != _maxTime)
			{
				_maxTime = value;
				OnPropertyChangedWithValue(value, "MaxTime");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentTime
	{
		get
		{
			return _currentTime;
		}
		set
		{
			if (value != _currentTime)
			{
				_currentTime = value;
				OnPropertyChangedWithValue(value, "CurrentTime");
			}
		}
	}

	[DataSourceProperty]
	public string CaptureShipText
	{
		get
		{
			return _captureShipText;
		}
		set
		{
			if (value != _captureShipText)
			{
				_captureShipText = value;
				OnPropertyChangedWithValue(value, "CaptureShipText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCapturing
	{
		get
		{
			return _isCapturing;
		}
		set
		{
			if (value != _isCapturing)
			{
				_isCapturing = value;
				OnPropertyChangedWithValue(value, "IsCapturing");
			}
		}
	}

	public NavalMissionCaptureShipVM(float totalCaptureTime)
	{
		MaxTime = totalCaptureTime;
		IsCapturing = false;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CaptureShipText = new TextObject("{=5qSIhAfx}Transferring troops and control").ToString();
	}

	public void UpdateCaptureTimer(float timeLeftToCapture)
	{
		IsCapturing = timeLeftToCapture >= 0f;
		if (IsCapturing)
		{
			CurrentTime = MaxTime - timeLeftToCapture;
		}
	}
}
