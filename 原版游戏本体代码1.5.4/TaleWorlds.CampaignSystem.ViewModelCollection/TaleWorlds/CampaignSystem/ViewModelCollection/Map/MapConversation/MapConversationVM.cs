using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation;

public class MapConversationVM : ViewModel
{
	private readonly Action _onContinue;

	private MissionConversationVM _dialogController;

	private object _tableauData;

	private bool _isBarterActive;

	[DataSourceProperty]
	public MissionConversationVM DialogController
	{
		get
		{
			return _dialogController;
		}
		set
		{
			if (value != _dialogController)
			{
				_dialogController = value;
				OnPropertyChangedWithValue(value, "DialogController");
			}
		}
	}

	[DataSourceProperty]
	public object TableauData
	{
		get
		{
			return _tableauData;
		}
		set
		{
			if (value != _tableauData)
			{
				_tableauData = value;
				OnPropertyChangedWithValue(value, "TableauData");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBarterActive
	{
		get
		{
			return _isBarterActive;
		}
		set
		{
			if (value != _isBarterActive)
			{
				_isBarterActive = value;
				OnPropertyChangedWithValue(value, "IsBarterActive");
			}
		}
	}

	public MapConversationVM(Action onContinue, Func<string> getContinueInputText)
	{
		_onContinue = onContinue;
		DialogController = new MissionConversationVM(getContinueInputText);
		TableauData = null;
	}

	public void ExecuteContinue()
	{
		_onContinue?.Invoke();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DialogController?.OnFinalize();
		DialogController = null;
		TableauData = null;
	}

	public void Tick(float dt)
	{
		DialogController?.Tick(dt);
	}
}
