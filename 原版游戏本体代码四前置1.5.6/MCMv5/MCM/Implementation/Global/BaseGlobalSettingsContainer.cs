using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.GameFeatures;
using MCM.Abstractions.Global;

namespace MCM.Implementation.Global;

internal abstract class BaseGlobalSettingsContainer : BaseSettingsContainer<GlobalSettings>, IGlobalSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasSettingsPack
{
	protected override GameDirectory RootFolder { get; }

	protected BaseGlobalSettingsContainer()
	{
		RootFolder = GenericServiceProvider.GetService<IFileSystemProvider>()?.GetDirectory(base.RootFolder, "Global") ?? base.RootFolder;
	}
}
