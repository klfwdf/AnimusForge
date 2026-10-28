using System.Collections.Generic;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace MCM.Internal;

internal sealed class MCMSettings : AttributeGlobalSettings<MCMSettings>
{
	public override string Id => "MCM_v5";

	public override string DisplayName => new TextObject("{=MCMSettings_Name}Mod Configuration Menu {VERSION}", new Dictionary<string, object> { 
	{
		"VERSION",
		typeof(MCMSettings).Assembly.GetName().Version?.ToString(3) ?? "ERROR"
	} }).ToString() ?? "ERROR";

	public override string FolderName => "MCM";

	public override string FormatType => "none";
}
