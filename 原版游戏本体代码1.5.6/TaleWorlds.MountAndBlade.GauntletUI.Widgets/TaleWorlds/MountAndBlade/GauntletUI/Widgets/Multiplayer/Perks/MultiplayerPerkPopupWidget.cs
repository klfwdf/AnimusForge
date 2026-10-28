using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Perks;

public class MultiplayerPerkPopupWidget : Widget
{
	private MultiplayerPerkContainerPanelWidget _latestContainer;

	public bool ShowAboveContainer { get; set; }

	public MultiplayerPerkPopupWidget(UIContext context)
		: base(context)
	{
	}

	public void SetPopupPerksContainer(MultiplayerPerkContainerPanelWidget container)
	{
		_latestContainer = container;
		ApplyActionToAllChildrenRecursive(SetContainersOfChildren);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.IsVisible && _latestContainer != null)
		{
			float value = _latestContainer.GlobalPosition.X - (base.Size.X / 2f - _latestContainer.Size.X / 2f);
			base.ScaledPositionXOffset = Mathf.Clamp(value, 0f, base.Context.EventManager.PageSize.X - base.Size.X);
			if (!ShowAboveContainer)
			{
				base.ScaledPositionYOffset = _latestContainer.GlobalPosition.Y + _latestContainer.Size.Y - base.EventManager.TopUsableAreaStart;
			}
			else
			{
				base.ScaledPositionYOffset = _latestContainer.GlobalPosition.Y - base.Size.Y - base.EventManager.TopUsableAreaStart;
			}
		}
	}

	private void SetContainersOfChildren(Widget obj)
	{
		if (obj is MultiplayerPerkItemToggleWidget multiplayerPerkItemToggleWidget)
		{
			multiplayerPerkItemToggleWidget.ContainerPanel = _latestContainer;
		}
	}
}
