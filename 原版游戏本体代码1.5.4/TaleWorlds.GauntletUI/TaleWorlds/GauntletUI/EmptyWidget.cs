using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI;

internal class EmptyWidget : Widget
{
	public EmptyWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
	}

	protected override void OnParallelUpdate(float dt)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
	}

	public override void UpdateBrushes(float dt)
	{
	}
}
