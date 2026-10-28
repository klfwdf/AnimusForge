using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class MBCampaignEvent
{
	public delegate void CampaignEventDelegate(MBCampaignEvent campaignEvent, params object[] delegateParams);

	public string description;

	protected List<CampaignEventDelegate> handlers = new List<CampaignEventDelegate>();

	[CachedData]
	protected CampaignTime NextTriggerTime;

	public CampaignTime TriggerPeriod { get; private set; }

	public CampaignTime InitialWait { get; private set; }

	public bool isEventDeleted { get; set; }

	public MBCampaignEvent(string eventName)
	{
		description = eventName;
	}

	public MBCampaignEvent(CampaignTime triggerPeriod, CampaignTime initialWait)
	{
		TriggerPeriod = triggerPeriod;
		InitialWait = initialWait;
		NextTriggerTime = CampaignTime.Now + InitialWait;
		isEventDeleted = false;
	}

	public void AddHandler(CampaignEventDelegate gameEventDelegate)
	{
		handlers.Add(gameEventDelegate);
	}

	public void RunHandlers(params object[] delegateParams)
	{
		for (int i = 0; i < handlers.Count; i++)
		{
			handlers[i](this, delegateParams);
		}
	}

	public void Unregister(object instance)
	{
		for (int i = 0; i < handlers.Count; i++)
		{
			if (handlers[i].Target == instance)
			{
				handlers.RemoveAt(i);
				i--;
			}
		}
	}

	public void CheckUpdate()
	{
		while (NextTriggerTime.IsPast && !isEventDeleted)
		{
			RunHandlers(CampaignTime.Now);
			NextTriggerTime += TriggerPeriod;
		}
	}

	public void DeletePeriodicEvent()
	{
		isEventDeleted = true;
	}
}
