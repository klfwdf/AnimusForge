using TaleWorlds.Core;
using TaleWorlds.Library.EventSystem;

namespace SandBox.ViewModelCollection.MapSiege;

public class PlayerStartEngineConstructionEvent : EventBase
{
	public SiegeEngineType Engine { get; private set; }

	public PlayerStartEngineConstructionEvent(SiegeEngineType engine)
	{
		Engine = engine;
	}
}
