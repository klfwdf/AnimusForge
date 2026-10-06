using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.GauntletUI;

public class GauntletMovieIdentifier
{
	public string MovieName { get; internal set; }

	public IGauntletMovie Movie { get; set; }

	public ViewModel DataSource { get; set; }

	internal GauntletMovieIdentifier(string movieName, ViewModel viewModel)
	{
		MovieName = movieName;
		DataSource = viewModel;
	}
}
