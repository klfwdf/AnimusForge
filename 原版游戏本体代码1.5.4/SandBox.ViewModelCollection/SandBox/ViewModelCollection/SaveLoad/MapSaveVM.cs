using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.SaveLoad;

public class MapSaveVM : ViewModel
{
	private readonly Action<bool> _onActiveStateChange;

	private string _savingText;

	private bool _isActive;

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
	public string SavingText
	{
		get
		{
			return _savingText;
		}
		set
		{
			if (value != _savingText)
			{
				_savingText = value;
				OnPropertyChangedWithValue(value, "SavingText");
			}
		}
	}

	public MapSaveVM(Action<bool> onActiveStateChange)
	{
		_onActiveStateChange = onActiveStateChange;
		CampaignEvents.OnSaveStartedEvent.AddNonSerializedListener(this, OnSaveStarted);
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TextObject textObject = new TextObject("{=cp2XDjeq}Saving...");
		SavingText = textObject.ToString();
	}

	private void OnSaveOver(bool isSuccessful, string saveName)
	{
		IsActive = false;
		_onActiveStateChange?.Invoke(obj: false);
	}

	private void OnSaveStarted()
	{
		IsActive = true;
		_onActiveStateChange?.Invoke(obj: true);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnSaveStartedEvent.ClearListeners(this);
		CampaignEvents.OnSaveOverEvent.ClearListeners(this);
	}
}
