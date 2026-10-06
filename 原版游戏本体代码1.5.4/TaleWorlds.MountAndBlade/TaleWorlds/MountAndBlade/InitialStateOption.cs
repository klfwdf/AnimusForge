using System;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class InitialStateOption
{
	private Action _action;

	public int OrderIndex { get; private set; }

	public TextObject Name { get; private set; }

	public string Id { get; private set; }

	public Func<bool> IsHidden { get; private set; }

	public Func<(bool, TextObject)> IsDisabledAndReason { get; private set; }

	public TextObject EnabledHint { get; private set; }

	public InitialStateOption(string id, TextObject name, int orderIndex, Action action, Func<(bool, TextObject)> isDisabledAndReason, TextObject enabledHint = null, Func<bool> isHidden = null)
	{
		Name = name;
		Id = id;
		OrderIndex = orderIndex;
		_action = action;
		IsHidden = isHidden;
		IsDisabledAndReason = isDisabledAndReason;
		EnabledHint = enabledHint;
		string.IsNullOrEmpty(IsDisabledAndReason().Item2?.ToString());
	}

	public void DoAction()
	{
		_action?.Invoke();
	}
}
