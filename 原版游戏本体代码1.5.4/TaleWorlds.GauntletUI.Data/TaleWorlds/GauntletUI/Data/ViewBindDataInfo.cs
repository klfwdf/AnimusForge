using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.Data;

internal readonly struct ViewBindDataInfo
{
	internal readonly bool IsValid;

	internal readonly GauntletView Owner;

	internal readonly string Property;

	internal readonly BindingPath Path;

	internal ViewBindDataInfo(GauntletView view, string property, BindingPath path)
	{
		IsValid = true;
		Owner = view;
		Property = property;
		Path = path;
	}
}
