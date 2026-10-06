using TaleWorlds.Library;

namespace NavalDLC;

public class VirtualFolders
{
	[VirtualDirectory("__MODULE_NAME__NavalDLC__MODULE_NAME__\\Parameters")]
	public class Parameters
	{
		[VirtualFile("Version.xml", "<Version>\t<Singleplayer Value=\"v1.3.4.123627\"/></Version>")]
		public string Version;
	}
}
