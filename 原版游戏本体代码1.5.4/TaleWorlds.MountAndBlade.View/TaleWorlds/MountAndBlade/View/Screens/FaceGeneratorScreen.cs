using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens;

public class FaceGeneratorScreen : ScreenBase, IFaceGeneratorScreen
{
	public IFaceGeneratorHandler Handler { get; }
}
