using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;

public class KingdomWarLogItemVM : ViewModel
{
	private readonly IEncyclopediaLog _log;

	private string _warLogText;

	private string _warLogTimeText;

	private BannerImageIdentifierVM _banner;

	[DataSourceProperty]
	public string WarLogTimeText
	{
		get
		{
			return _warLogTimeText;
		}
		set
		{
			if (value != _warLogTimeText)
			{
				_warLogTimeText = value;
				OnPropertyChangedWithValue(value, "WarLogTimeText");
			}
		}
	}

	[DataSourceProperty]
	public string WarLogText
	{
		get
		{
			return _warLogText;
		}
		set
		{
			if (value != _warLogText)
			{
				_warLogText = value;
				OnPropertyChangedWithValue(value, "WarLogText");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner
	{
		get
		{
			return _banner;
		}
		set
		{
			if (value != _banner)
			{
				_banner = value;
				OnPropertyChangedWithValue(value, "Banner");
			}
		}
	}

	public KingdomWarLogItemVM(IEncyclopediaLog log, IFaction effectorFaction)
	{
		_log = log;
		Banner = new BannerImageIdentifierVM(effectorFaction.Banner, nineGrid: true);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		WarLogTimeText = _log.GameTime.ToString();
		WarLogText = _log.GetEncyclopediaText().ToString();
	}

	private void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}
}
