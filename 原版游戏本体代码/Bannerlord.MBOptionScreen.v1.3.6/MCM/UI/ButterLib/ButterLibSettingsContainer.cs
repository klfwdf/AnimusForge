using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.FluentBuilder.Models;
using MCM.Abstractions.Global;
using MCM.Common;
using MCM.Implementation;
using MCM.UI.Extensions;
using Microsoft.Extensions.Logging;
using TaleWorlds.Localization;

namespace MCM.UI.ButterLib
{
	// Token: 0x02000031 RID: 49
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class ButterLibSettingsContainer : BaseSettingsContainer<BaseSettings>, IGlobalSettingsContainer, ISettingsContainer, ISettingsContainerHasSettingsDefinitions, ISettingsContainerCanOverride, ISettingsContainerCanReset, ISettingsContainerPresets, ISettingsContainerHasSettingsPack
	{
		// Token: 0x060001B2 RID: 434 RVA: 0x00007940 File Offset: 0x00005B40
		[NullableContext(1)]
		public ButterLibSettingsContainer(ILogger<ButterLibSettingsContainer> logger)
		{
			StorageRef<Dropdown<string>> minLogLevelProp = new StorageRef<Dropdown<string>>(new Dropdown<string>(new string[]
			{
				string.Format("{{=2Tp85Cpa}}{0}", 0),
				string.Format("{{=Es0LPYu1}}{0}", 1),
				string.Format("{{=fgLroxa7}}{0}", 2),
				string.Format("{{=yBflFuRG}}{0}", 3),
				string.Format("{{=7tpjjYSV}}{0}", 4),
				string.Format("{{=CarGIPlL}}{0}", 5),
				string.Format("{{=T3FtC5hh}}{0}", 6)
			}, 2));
			string value = "{=ButterLibSettings_Name}ButterLib {VERSION}";
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			Dictionary<string, object> dictionary2 = dictionary;
			string key = "VERSION";
			Version version = typeof(ButterLibSubModule).Assembly.GetName().Version;
			dictionary2.Add(key, ((version != null) ? version.ToString(3) : null) ?? "ERROR");
			string displayName = new TextObject(value, dictionary).ToString();
			ISettingsBuilder settingsBuilder = BaseSettingsBuilder.Create("Options", displayName);
			FluentGlobalSettings settings = ((settingsBuilder != null) ? settingsBuilder.SetFolderName("ButterLib").SetFormat("json2").CreateGroup("{=ButterLibSettings_Name_Logging}Logging", delegate(ISettingsPropertyGroupBuilder builder)
			{
				builder.AddDropdown("MinLogLevel", "{=ButterLibSettings_Name_LogLevel}Log Level", 0, minLogLevelProp, delegate(ISettingsPropertyDropdownBuilder dBuilder)
				{
					dBuilder.SetOrder(1).SetRequireRestart(true).SetHintText("{=ButterLibSettings_Name_LogLevelDesc}Level of logs to write.");
				});
			})
				.AddButterLibSubSystems()
				.BuildAsGlobal() : null);
			this.RegisterSettings(settings);
		}
	}
}
