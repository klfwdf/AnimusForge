using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GameOver;

public class GameOverCategoryButtonWidget : ButtonWidget
{
	public string CategoryID { get; set; }

	public GameOverCategoryButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void HandleClick()
	{
		HandleSoundEvent();
		base.HandleClick();
	}

	private void HandleSoundEvent()
	{
		EventFired(CategoryID);
	}
}
