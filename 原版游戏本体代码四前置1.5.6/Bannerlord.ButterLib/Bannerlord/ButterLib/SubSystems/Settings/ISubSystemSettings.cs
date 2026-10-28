using System.Collections.Generic;

namespace Bannerlord.ButterLib.SubSystems.Settings;

public interface ISubSystemSettings<TSubSystem> where TSubSystem : ISubSystem
{
	IReadOnlyCollection<SubSystemSettingsDeclaration<TSubSystem>> Declarations { get; }
}
