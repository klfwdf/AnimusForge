using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.DamageFeed;

public class MissionAgentDamageFeedVM : ViewModel
{
	private readonly TextObject _takenDamageText;

	private MBBindingList<MissionAgentDamageFeedItemVM> _feedList;

	[DataSourceProperty]
	public MBBindingList<MissionAgentDamageFeedItemVM> FeedList
	{
		get
		{
			return _feedList;
		}
		set
		{
			if (value != _feedList)
			{
				_feedList = value;
				OnPropertyChangedWithValue(value, "FeedList");
			}
		}
	}

	public MissionAgentDamageFeedVM()
	{
		_takenDamageText = new TextObject("{=meFS5F4V}-{DAMAGE}");
		FeedList = new MBBindingList<MissionAgentDamageFeedItemVM>();
	}

	public void OnMainAgentHit(float damage)
	{
		if (damage > 0f)
		{
			_takenDamageText.SetTextVariable("DAMAGE", damage);
			MissionAgentDamageFeedItemVM item = new MissionAgentDamageFeedItemVM(_takenDamageText.ToString(), RemoveItem);
			FeedList.Add(item);
		}
	}

	private void RemoveItem(MissionAgentDamageFeedItemVM item)
	{
		FeedList.Remove(item);
	}
}
