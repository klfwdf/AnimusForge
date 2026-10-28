using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed.General;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed.Personal;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed;

public class SPKillFeedVM : ViewModel
{
	private SPGeneralKillNotificationVM _generalCasualty;

	private SPPersonalKillNotificationVM _personalFeed;

	[DataSourceProperty]
	public SPGeneralKillNotificationVM GeneralCasualty
	{
		get
		{
			return _generalCasualty;
		}
		set
		{
			if (value != _generalCasualty)
			{
				_generalCasualty = value;
				OnPropertyChangedWithValue(value, "GeneralCasualty");
			}
		}
	}

	[DataSourceProperty]
	public SPPersonalKillNotificationVM PersonalFeed
	{
		get
		{
			return _personalFeed;
		}
		set
		{
			if (value != _personalFeed)
			{
				_personalFeed = value;
				OnPropertyChangedWithValue(value, "PersonalFeed");
			}
		}
	}

	public SPKillFeedVM()
	{
		GeneralCasualty = new SPGeneralKillNotificationVM();
		PersonalFeed = new SPPersonalKillNotificationVM();
	}

	public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, bool isHeadshot, bool isSuicide, bool isDrowning)
	{
		GeneralCasualty.OnAgentRemoved(affectedAgent, affectorAgent, isHeadshot, isSuicide, isDrowning);
	}

	public void OnPersonalKill(int damageAmount, bool isMountDamage, bool isFriendlyFire, bool isHeadshot, string killedAgentName, bool isUnconscious)
	{
		PersonalFeed.OnPersonalKill(damageAmount, isMountDamage, isFriendlyFire, isHeadshot, killedAgentName, isUnconscious);
	}

	public void OnPersonalDamage(int totalDamage, bool isVictimAgentMount, bool isFriendlyFire, string victimAgentName)
	{
		PersonalFeed.OnPersonalHit(totalDamage, isVictimAgentMount, isFriendlyFire, victimAgentName);
	}

	public void OnPersonalMessage(string message)
	{
		PersonalFeed.OnPersonalMessage(message);
	}
}
