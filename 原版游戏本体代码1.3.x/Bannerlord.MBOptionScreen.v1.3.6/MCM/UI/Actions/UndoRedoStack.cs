using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MCM.Common;

namespace MCM.UI.Actions
{
	// Token: 0x0200003C RID: 60
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class UndoRedoStack
	{
		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x060001F0 RID: 496 RVA: 0x00008291 File Offset: 0x00006491
		private Stack<IAction> UndoStack { get; }

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x060001F1 RID: 497 RVA: 0x00008299 File Offset: 0x00006499
		private Stack<IAction> RedoStack { get; }

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x060001F2 RID: 498 RVA: 0x000082A1 File Offset: 0x000064A1
		public bool CanUndo
		{
			get
			{
				return this.UndoStack.Count > 0;
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x060001F3 RID: 499 RVA: 0x000082B1 File Offset: 0x000064B1
		public bool CanRedo
		{
			get
			{
				return this.RedoStack.Count > 0;
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x060001F4 RID: 500 RVA: 0x000082C1 File Offset: 0x000064C1
		public bool ChangesMade
		{
			get
			{
				return this.UndoStack.Count > 0;
			}
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x000082D1 File Offset: 0x000064D1
		public UndoRedoStack()
		{
			this.UndoStack = new Stack<IAction>();
			this.RedoStack = new Stack<IAction>();
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x000082F0 File Offset: 0x000064F0
		public bool RefChanged(IRef @ref)
		{
			List<IAction> stack = (from s in this.UndoStack
				where object.Equals(s.Context, @ref)
				select s).ToList<IAction>();
			if (stack.Count == 0)
			{
				return false;
			}
			IAction firstChange = stack.First<IAction>();
			IAction lastChange = stack.Last<IAction>();
			object originalValue = firstChange.Original;
			object currentValue = lastChange.Value;
			return (originalValue != null || currentValue != null) && ((originalValue == null && currentValue != null) || (originalValue != null && currentValue == null) || !originalValue.Equals(currentValue));
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x00008379 File Offset: 0x00006579
		public void Do(IAction action)
		{
			action.DoAction();
			this.UndoStack.Push(action);
			this.RedoStack.Clear();
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x00008398 File Offset: 0x00006598
		public void Undo()
		{
			if (this.CanUndo)
			{
				IAction a = this.UndoStack.Pop();
				a.UndoAction();
				this.RedoStack.Push(a);
			}
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x000083CC File Offset: 0x000065CC
		public void Redo()
		{
			if (this.CanRedo)
			{
				IAction a = this.RedoStack.Pop();
				a.DoAction();
				this.UndoStack.Push(a);
			}
		}

		// Token: 0x060001FA RID: 506 RVA: 0x00008400 File Offset: 0x00006600
		public void UndoAll()
		{
			if (this.CanUndo)
			{
				while (this.UndoStack.Count > 0)
				{
					IAction a = this.UndoStack.Pop();
					a.UndoAction();
				}
			}
		}

		// Token: 0x060001FB RID: 507 RVA: 0x00008437 File Offset: 0x00006637
		public void ClearStack()
		{
			this.UndoStack.Clear();
			this.RedoStack.Clear();
		}
	}
}
