using System.Collections.Generic;
using MCM.Abstractions.Base;

namespace MCM.Abstractions;

public interface ISettingsContainerHasSettingsPack
{
	IEnumerable<SettingSnapshot> SaveAvailableSnapshots();

	IEnumerable<BaseSettings> LoadAvailableSnapshots(IEnumerable<SettingSnapshot> snapshots);
}
