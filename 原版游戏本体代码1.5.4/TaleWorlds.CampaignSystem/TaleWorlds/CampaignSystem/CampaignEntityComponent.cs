using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public class CampaignEntityComponent : IEntityComponent
{
	void IEntityComponent.OnInitialize()
	{
		OnInitialize();
	}

	void IEntityComponent.OnFinalize()
	{
		OnFinalize();
	}

	protected virtual void OnInitialize()
	{
	}

	protected virtual void OnFinalize()
	{
	}

	public virtual void OnTick(float realDt, float dt)
	{
	}
}
