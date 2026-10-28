using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.Core;

public class TutorialContextChangedEvent : EventBase
{
	public TutorialContexts NewContext { get; private set; }

	public TutorialContextChangedEvent(TutorialContexts newContext)
	{
		NewContext = newContext;
	}
}
