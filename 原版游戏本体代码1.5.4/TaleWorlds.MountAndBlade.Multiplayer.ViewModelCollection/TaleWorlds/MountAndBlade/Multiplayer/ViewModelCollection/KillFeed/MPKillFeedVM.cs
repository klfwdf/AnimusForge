using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed.General;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed.Personal;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed;

public class MPKillFeedVM : ViewModel
{
	private MPGeneralKillNotificationVM _generalCasualty;

	private MPPersonalKillNotificationVM _personalCasualty;

	[DataSourceProperty]
	public MPGeneralKillNotificationVM GeneralCasualty
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
	public MPPersonalKillNotificationVM PersonalCasualty
	{
		get
		{
			return _personalCasualty;
		}
		set
		{
			if (value != _personalCasualty)
			{
				_personalCasualty = value;
				OnPropertyChangedWithValue(value, "PersonalCasualty");
			}
		}
	}

	public MPKillFeedVM()
	{
		GeneralCasualty = new MPGeneralKillNotificationVM();
		PersonalCasualty = new MPPersonalKillNotificationVM();
	}

	public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, bool isPersonalFeedEnabled, WeaponClass killWeaponClass = WeaponClass.Undefined)
	{
		Agent assistedAgent = GetAssistedAgent(affectedAgent, affectorAgent);
		if ((assistedAgent?.IsMainAgent ?? false) && isPersonalFeedEnabled)
		{
			string victimAgentName = affectedAgent.Name;
			if (affectedAgent.MissionPeer != null)
			{
				victimAgentName = affectedAgent.MissionPeer.DisplayedName;
			}
			OnPersonalAssist(victimAgentName);
		}
		GeneralCasualty.OnAgentRemoved(affectedAgent, affectorAgent, assistedAgent, killWeaponClass);
	}

	private void OnPersonalAssist(string victimAgentName)
	{
		PersonalCasualty.OnPersonalAssist(victimAgentName);
	}

	public void OnPersonalDamage(int damageAmount, bool isFatal, bool isMountDamage, bool isFriendlyDamage, bool isHeadshot, string killedAgentName)
	{
		PersonalCasualty.OnPersonalHit(damageAmount, isFatal, isMountDamage, isFriendlyDamage, isHeadshot, killedAgentName);
	}

	private Agent GetAssistedAgent(Agent affectedAgent, Agent affectorAgent)
	{
		return affectedAgent?.GetAssistingHitter(affectorAgent?.MissionPeer)?.HitterPeer?.ControlledAgent;
	}
}
