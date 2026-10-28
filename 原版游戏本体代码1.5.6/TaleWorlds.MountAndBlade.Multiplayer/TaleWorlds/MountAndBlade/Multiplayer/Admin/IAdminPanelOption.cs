using System;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public interface IAdminPanelOption
{
	string UniqueId { get; }

	string Name { get; }

	string Description { get; }

	bool RequiresMissionRestart { get; }

	bool IsRequired { get; }

	bool IsDirty { get; }

	bool CanRevertToDefaultValue { get; }

	bool GetIsDisabled(out string reason);

	bool GetIsAvailable();

	void RevertChanges();

	void RestoreDefaults();

	void SetOnRefreshCallback(Action callback);
}
public interface IAdminPanelOption<T> : IAdminPanelOption
{
	T GetValue();

	void SetValue(T value);
}
