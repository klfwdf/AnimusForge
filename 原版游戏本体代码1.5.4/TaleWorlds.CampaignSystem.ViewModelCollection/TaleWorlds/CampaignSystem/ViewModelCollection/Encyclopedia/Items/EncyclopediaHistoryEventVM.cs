using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaHistoryEventVM : EncyclopediaLinkVM
{
	private readonly IEncyclopediaLog _log;

	private string _historyEventText;

	private string _historyEventTimeText;

	[DataSourceProperty]
	public string HistoryEventTimeText
	{
		get
		{
			return _historyEventTimeText;
		}
		set
		{
			if (value != _historyEventTimeText)
			{
				_historyEventTimeText = value;
				OnPropertyChangedWithValue(value, "HistoryEventTimeText");
			}
		}
	}

	[DataSourceProperty]
	public string HistoryEventText
	{
		get
		{
			return _historyEventText;
		}
		set
		{
			if (value != _historyEventText)
			{
				_historyEventText = value;
				OnPropertyChangedWithValue(value, "HistoryEventText");
			}
		}
	}

	public EncyclopediaHistoryEventVM(IEncyclopediaLog log)
	{
		_log = log;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		HistoryEventTimeText = _log.GameTime.ToString();
		HistoryEventText = _log.GetEncyclopediaText().ToString();
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}
}
