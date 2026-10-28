using System;
using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class AvailableScenes
{
	public static AvailableScenes Empty { get; private set; }

	public Dictionary<string, string[]> ScenesByGameTypes { get; set; }

	static AvailableScenes()
	{
		Empty = new AvailableScenes(new Dictionary<string, string[]>());
	}

	public AvailableScenes()
	{
	}

	public AvailableScenes(Dictionary<string, string[]> scenesByGameTypes)
	{
		ScenesByGameTypes = scenesByGameTypes;
	}
}
