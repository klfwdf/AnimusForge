using System;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

internal interface IAdminPanelOptionInternal
{
	MultiplayerOptions.OptionType GetOptionType();

	MultiplayerOptions.MultiplayerOptionsAccessMode GetOptionAccessMode();

	void OnApplyChanges();

	void AddValueChangedCallback(Action callback);

	void RemoveValueChangedCallback(Action callback);

	void OnFinalize();
}
internal interface IAdminPanelOptionInternal<T> : IAdminPanelOptionInternal, IAdminPanelOption<T>, IAdminPanelOption
{
}
