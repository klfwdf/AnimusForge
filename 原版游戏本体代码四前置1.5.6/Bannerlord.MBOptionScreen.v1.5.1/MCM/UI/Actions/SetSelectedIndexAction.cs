using MCM.Common;

namespace MCM.UI.Actions;

internal sealed class SetSelectedIndexAction : IAction
{
	public IRef Context { get; }

	public object? Value { get; }

	public object? Original { get; }

	public SetSelectedIndexAction(IRef context, object value)
	{
		Context = context;
		Value = new SelectedIndexWrapper(value).SelectedIndex;
		Original = new SelectedIndexWrapper(context.Value).SelectedIndex;
	}

	public void DoAction()
	{
		SelectedIndexWrapper selectedIndexWrapper = new SelectedIndexWrapper(Context.Value);
		selectedIndexWrapper.SelectedIndex = ((int?)Value) ?? (-1);
	}

	public void UndoAction()
	{
		SelectedIndexWrapper selectedIndexWrapper = new SelectedIndexWrapper(Context.Value);
		selectedIndexWrapper.SelectedIndex = ((int?)Original) ?? (-1);
	}
}
