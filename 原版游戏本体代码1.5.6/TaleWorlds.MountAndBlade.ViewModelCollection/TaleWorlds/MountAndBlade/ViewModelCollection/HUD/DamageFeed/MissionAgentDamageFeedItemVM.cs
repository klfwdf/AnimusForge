using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.DamageFeed;

public class MissionAgentDamageFeedItemVM : ViewModel
{
	private readonly Action<MissionAgentDamageFeedItemVM> _onRemove;

	private string _feedText;

	[DataSourceProperty]
	public string FeedText
	{
		get
		{
			return _feedText;
		}
		set
		{
			if (value != _feedText)
			{
				_feedText = value;
				OnPropertyChangedWithValue(value, "FeedText");
			}
		}
	}

	public MissionAgentDamageFeedItemVM(string feedText, Action<MissionAgentDamageFeedItemVM> onRemove)
	{
		_onRemove = onRemove;
		FeedText = feedText;
	}

	public void ExecuteRemove()
	{
		_onRemove(this);
	}
}
