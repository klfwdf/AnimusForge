using System.Collections.Generic;
using System.Linq;
using MCM.Common;

namespace MCM.UI.Actions;

internal sealed class UndoRedoStack
{
	private Stack<IAction> UndoStack { get; }

	private Stack<IAction> RedoStack { get; }

	public bool CanUndo => UndoStack.Count > 0;

	public bool CanRedo => RedoStack.Count > 0;

	public bool ChangesMade => UndoStack.Count > 0;

	public UndoRedoStack()
	{
		UndoStack = new Stack<IAction>();
		RedoStack = new Stack<IAction>();
	}

	public bool RefChanged(IRef @ref)
	{
		List<IAction> list = UndoStack.Where((IAction s) => object.Equals(s.Context, @ref)).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		IAction action = list.First();
		IAction action2 = list.Last();
		object original = action.Original;
		object value = action2.Value;
		if (original == null && value == null)
		{
			return false;
		}
		if (original == null && value != null)
		{
			return true;
		}
		if (original != null && value == null)
		{
			return true;
		}
		return !original.Equals(value);
	}

	public void Do(IAction action)
	{
		action.DoAction();
		UndoStack.Push(action);
		RedoStack.Clear();
	}

	public void Undo()
	{
		if (CanUndo)
		{
			IAction action = UndoStack.Pop();
			action.UndoAction();
			RedoStack.Push(action);
		}
	}

	public void Redo()
	{
		if (CanRedo)
		{
			IAction action = RedoStack.Pop();
			action.DoAction();
			UndoStack.Push(action);
		}
	}

	public void UndoAll()
	{
		if (CanUndo)
		{
			while (UndoStack.Count > 0)
			{
				IAction action = UndoStack.Pop();
				action.UndoAction();
			}
		}
	}

	public void ClearStack()
	{
		UndoStack.Clear();
		RedoStack.Clear();
	}
}
