using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.View.Map;

public abstract class MapView : SandboxView
{
	protected const float ContextAlphaModifier = 8.5f;

	public MapScreen MapScreen { get; internal set; }

	public MapState MapState { get; internal set; }

	protected internal virtual void CreateLayout()
	{
	}

	protected internal virtual void OnResume()
	{
	}

	protected internal virtual void OnHourlyTick()
	{
	}

	protected internal virtual void OnStartWait(string waitMenuId)
	{
	}

	protected internal virtual void OnMainPartyEncounter()
	{
	}

	protected internal virtual void OnDispersePlayerLeadedArmy()
	{
	}

	protected internal virtual void OnArmyLeft()
	{
	}

	protected internal virtual bool IsEscaped()
	{
		return false;
	}

	protected internal virtual bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return true;
	}

	protected internal virtual void OnOverlayCreated()
	{
	}

	protected internal virtual void OnOverlayClosed()
	{
	}

	protected internal virtual void OnMenuModeTick(float dt)
	{
	}

	protected internal virtual void OnMapScreenUpdate(float dt)
	{
	}

	protected internal virtual void OnIdleTick(float dt)
	{
	}

	protected internal virtual void OnMapTerrainClick()
	{
	}

	protected internal virtual void OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
	{
	}

	protected internal virtual void OnMapConversationStart()
	{
	}

	protected internal virtual void OnMapConversationOver()
	{
	}

	protected internal virtual TutorialContexts GetTutorialContext()
	{
		return TutorialContexts.MapWindow;
	}
}
