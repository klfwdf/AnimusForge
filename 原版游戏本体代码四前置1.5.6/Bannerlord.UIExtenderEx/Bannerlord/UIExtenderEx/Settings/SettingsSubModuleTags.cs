using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ModuleManager;

namespace Bannerlord.UIExtenderEx.Settings;

public class SettingsSubModuleTags : ISettingsProvider
{
	public bool DumpXML { get; set; }

	public SettingsSubModuleTags()
	{
		try
		{
			ModuleInfoExtendedHelper moduleByType = ModuleInfoHelper.GetModuleByType(typeof(SettingsSubModuleTags));
			if ((object)moduleByType != null)
			{
				SubModuleInfoExtended subModuleInfoExtended = moduleByType.SubModules.FirstOrDefault((SubModuleInfoExtended x) => x.Name == "UIExtenderEx");
				if ((object)subModuleInfoExtended != null)
				{
					bool result = default(bool);
					DumpXML = (subModuleInfoExtended.Tags.TryGetValue("DumpXML", out IReadOnlyList<string> value) && bool.TryParse(value.FirstOrDefault(), out result)) & result;
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
