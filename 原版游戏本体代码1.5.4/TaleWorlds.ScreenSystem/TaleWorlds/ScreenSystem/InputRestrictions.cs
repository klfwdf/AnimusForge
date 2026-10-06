using System;
using TaleWorlds.Library;

namespace TaleWorlds.ScreenSystem;

public class InputRestrictions
{
	public int Order { get; private set; }

	public Guid Id { get; private set; }

	public bool MouseVisibility { get; private set; }

	public InputUsageMask InputUsageMask { get; private set; }

	public InputRestrictions(int order)
	{
		Id = default(Guid);
		InputUsageMask = InputUsageMask.Invalid;
		Order = order;
	}

	public void SetMouseVisibility(bool isVisible)
	{
		MouseVisibility = isVisible;
	}

	public void SetInputRestrictions(bool isMouseVisible = true, InputUsageMask mask = InputUsageMask.All)
	{
		InputUsageMask = mask;
		SetMouseVisibility(isMouseVisible);
	}

	public void ResetInputRestrictions()
	{
		InputUsageMask = InputUsageMask.Invalid;
		SetMouseVisibility(isVisible: false);
	}
}
