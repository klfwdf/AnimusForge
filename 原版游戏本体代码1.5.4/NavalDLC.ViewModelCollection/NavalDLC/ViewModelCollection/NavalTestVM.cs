using TaleWorlds.Library;

namespace NavalDLC.ViewModelCollection;

public class NavalTestVM : ViewModel
{
	[DataSourceProperty]
	public string NavalText => "Text from NavalTestVM";
}
