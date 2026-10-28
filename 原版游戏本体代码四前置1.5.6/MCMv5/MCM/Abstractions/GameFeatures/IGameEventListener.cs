using System;

namespace MCM.Abstractions.GameFeatures;

public interface IGameEventListener
{
	event Action GameStarted;

	event Action GameLoaded;

	event Action GameEnded;
}
