using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem;

public interface ICampaignBehaviorManager
{
	void RegisterEvents();

	T GetBehavior<T>();

	IEnumerable<T> GetBehaviors<T>();

	void AddBehavior(CampaignBehaviorBase campaignBehavior);

	void RemoveBehavior<T>() where T : CampaignBehaviorBase;

	void ClearBehaviors();

	void LoadBehaviorData();

	void InitializeCampaignBehaviors(IEnumerable<CampaignBehaviorBase> inputComponents);
}
