namespace Bannerlord.ButterLib.SubSystems.Settings;

public abstract record SubSystemSettingsDeclaration<TSubSystem>(string Name, string Description) where TSubSystem : ISubSystem;
