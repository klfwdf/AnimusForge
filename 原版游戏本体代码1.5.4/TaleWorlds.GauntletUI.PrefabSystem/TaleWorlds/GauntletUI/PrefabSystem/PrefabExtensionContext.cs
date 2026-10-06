using System.Collections.Generic;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class PrefabExtensionContext
{
	private List<PrefabExtension> _prefabExtensions;

	public IEnumerable<PrefabExtension> PrefabExtensions => _prefabExtensions;

	public PrefabExtensionContext()
	{
		_prefabExtensions = new List<PrefabExtension>();
	}

	public void AddExtension(PrefabExtension prefabExtension)
	{
		_prefabExtensions.Add(prefabExtension);
	}
}
