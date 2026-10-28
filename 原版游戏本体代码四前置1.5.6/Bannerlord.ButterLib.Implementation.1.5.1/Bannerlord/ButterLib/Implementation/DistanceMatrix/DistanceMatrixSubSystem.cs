using System.Collections.Generic;
using Bannerlord.ButterLib.Options;
using Bannerlord.ButterLib.SubSystems;
using Bannerlord.ButterLib.SubSystems.Settings;

namespace Bannerlord.ButterLib.Implementation.DistanceMatrix;

internal class DistanceMatrixSubSystem : ISubSystem, ISubSystemSettings<DistanceMatrixSubSystem>
{
	private bool _wasInitialized;

	public static DistanceMatrixSubSystem? Instance { get; private set; }

	public string Id => "Distance Matrix";

	public string Name => "{=Ox6uK8fZWs}Distance Matrix";

	public string Description => "{=WQ4r2n0mYj}Mod Developer feature! Provides helpers to calculate distance between objects on map.";

	public bool IsEnabled { get; private set; }

	public bool CanBeDisabled => true;

	public bool CanBeSwitchedAtRuntime => false;

	internal bool GameInitialized { get; set; }

	public bool ConsiderVillages { get; set; } = true;

	public IReadOnlyCollection<SubSystemSettingsDeclaration<DistanceMatrixSubSystem>> Declarations { get; } = new global::_003C_003Ez__ReadOnlySingleElementList<SubSystemSettingsDeclaration<DistanceMatrixSubSystem>>(new SubSystemSettingsPropertyBool<DistanceMatrixSubSystem>("{=MSJe8ih4yp}Consider Villages", "{=kvR54SiOFn}Allow villages to be used for built-in distance matrix calculations. May negatively affect performance.", (DistanceMatrixSubSystem x) => x.ConsiderVillages));

	public DistanceMatrixSubSystem()
	{
		Instance = this;
	}

	public void Enable()
	{
		if (!_wasInitialized)
		{
			_wasInitialized = true;
			if (!(SettingsProvider.PopulateSubSystemSettings(this) ?? true))
			{
				return;
			}
		}
		if (!IsEnabled && !GameInitialized)
		{
			IsEnabled = true;
		}
	}

	public void Disable()
	{
		if (IsEnabled && !GameInitialized)
		{
			IsEnabled = false;
		}
	}
}
