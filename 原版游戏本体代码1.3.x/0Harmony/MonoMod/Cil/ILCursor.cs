using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using MonoMod.SourceGen.Attributes;
using MonoMod.Utils;

namespace MonoMod.Cil
{
	// Token: 0x02000871 RID: 2161
	[NullableContext(1)]
	[Nullable(0)]
	[EmitILOverloads("ILOpcodes.txt", "ILCursor")]
	internal sealed class ILCursor
	{
		// Token: 0x1700081E RID: 2078
		// (get) Token: 0x060029A9 RID: 10665 RVA: 0x0008D417 File Offset: 0x0008B617
		public ILContext Context { get; }

		// Token: 0x1700081F RID: 2079
		// (get) Token: 0x060029AA RID: 10666 RVA: 0x0008D41F File Offset: 0x0008B61F
		// (set) Token: 0x060029AB RID: 10667 RVA: 0x0008D427 File Offset: 0x0008B627
		[Nullable(2)]
		public Instruction Next
		{
			[NullableContext(2)]
			get
			{
				return this._next;
			}
			[NullableContext(2)]
			set
			{
				this.Goto(value, MoveType.Before, false);
			}
		}

		// Token: 0x17000820 RID: 2080
		// (get) Token: 0x060029AC RID: 10668 RVA: 0x0008D433 File Offset: 0x0008B633
		// (set) Token: 0x060029AD RID: 10669 RVA: 0x0008D461 File Offset: 0x0008B661
		public Instruction Prev
		{
			get
			{
				if (this.Next != null)
				{
					return this.Next.Previous;
				}
				return this.Instrs[this.Instrs.Count - 1];
			}
			set
			{
				this.Goto(value, MoveType.After, false);
			}
		}

		// Token: 0x17000821 RID: 2081
		// (get) Token: 0x060029AE RID: 10670 RVA: 0x0008D46D File Offset: 0x0008B66D
		// (set) Token: 0x060029AF RID: 10671 RVA: 0x0008D475 File Offset: 0x0008B675
		public Instruction Previous
		{
			get
			{
				return this.Prev;
			}
			set
			{
				this.Prev = value;
			}
		}

		// Token: 0x17000822 RID: 2082
		// (get) Token: 0x060029B0 RID: 10672 RVA: 0x0008D47E File Offset: 0x0008B67E
		// (set) Token: 0x060029B1 RID: 10673 RVA: 0x0008D491 File Offset: 0x0008B691
		public int Index
		{
			get
			{
				return this.Context.IndexOf(this.Next);
			}
			set
			{
				this.Goto(value, MoveType.Before, false);
			}
		}

		// Token: 0x17000823 RID: 2083
		// (get) Token: 0x060029B2 RID: 10674 RVA: 0x0008D49D File Offset: 0x0008B69D
		// (set) Token: 0x060029B3 RID: 10675 RVA: 0x0008D4A5 File Offset: 0x0008B6A5
		public SearchTarget SearchTarget
		{
			get
			{
				return this._searchTarget;
			}
			set
			{
				if ((value == SearchTarget.Next && this.Next == null) || (value == SearchTarget.Prev && this.Prev == null))
				{
					value = SearchTarget.None;
				}
				this._searchTarget = value;
			}
		}

		// Token: 0x17000824 RID: 2084
		// (get) Token: 0x060029B4 RID: 10676 RVA: 0x0008D4C9 File Offset: 0x0008B6C9
		public IEnumerable<ILLabel> IncomingLabels
		{
			get
			{
				return this.Context.GetIncomingLabels(this.Next);
			}
		}

		// Token: 0x17000825 RID: 2085
		// (get) Token: 0x060029B5 RID: 10677 RVA: 0x0008D4DC File Offset: 0x0008B6DC
		public MethodDefinition Method
		{
			get
			{
				return this.Context.Method;
			}
		}

		// Token: 0x17000826 RID: 2086
		// (get) Token: 0x060029B6 RID: 10678 RVA: 0x0008D4E9 File Offset: 0x0008B6E9
		public ILProcessor IL
		{
			get
			{
				return this.Context.IL;
			}
		}

		// Token: 0x17000827 RID: 2087
		// (get) Token: 0x060029B7 RID: 10679 RVA: 0x0008D4F6 File Offset: 0x0008B6F6
		public Mono.Cecil.Cil.MethodBody Body
		{
			get
			{
				return this.Context.Body;
			}
		}

		// Token: 0x17000828 RID: 2088
		// (get) Token: 0x060029B8 RID: 10680 RVA: 0x0008D503 File Offset: 0x0008B703
		public ModuleDefinition Module
		{
			get
			{
				return this.Context.Module;
			}
		}

		// Token: 0x17000829 RID: 2089
		// (get) Token: 0x060029B9 RID: 10681 RVA: 0x0008D510 File Offset: 0x0008B710
		public Collection<Instruction> Instrs
		{
			get
			{
				return this.Context.Instrs;
			}
		}

		// Token: 0x060029BA RID: 10682 RVA: 0x0008D51D File Offset: 0x0008B71D
		public ILCursor(ILContext context)
		{
			this.Context = context;
			this.Index = 0;
		}

		// Token: 0x060029BB RID: 10683 RVA: 0x0008D534 File Offset: 0x0008B734
		public ILCursor(ILCursor c)
		{
			Helpers.ThrowIfArgumentNull<ILCursor>(c, "c");
			this.Context = c.Context;
			this._next = c._next;
			this._searchTarget = c._searchTarget;
			this._afterLabels = c._afterLabels;
			this._afterHandlerStarts = c._afterHandlerStarts;
			this._afterHandlerEnds = c._afterHandlerEnds;
		}

		// Token: 0x060029BC RID: 10684 RVA: 0x0008D59A File Offset: 0x0008B79A
		public ILCursor Clone()
		{
			return new ILCursor(this);
		}

		// Token: 0x060029BD RID: 10685 RVA: 0x0008D5A2 File Offset: 0x0008B7A2
		public bool IsBefore(Instruction instr)
		{
			return this.Index <= this.Context.IndexOf(instr);
		}

		// Token: 0x060029BE RID: 10686 RVA: 0x0008D5BB File Offset: 0x0008B7BB
		public bool IsAfter(Instruction instr)
		{
			return this.Index > this.Context.IndexOf(instr);
		}

		// Token: 0x060029BF RID: 10687 RVA: 0x0008D5D4 File Offset: 0x0008B7D4
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 3);
			defaultInterpolatedStringHandler.AppendLiteral("// ILCursor: ");
			defaultInterpolatedStringHandler.AppendFormatted<MethodDefinition>(this.Method);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.Index);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted<SearchTarget>(this.SearchTarget);
			stringBuilder.AppendLine(defaultInterpolatedStringHandler.ToStringAndClear());
			ILContext.ToString(stringBuilder, this.Prev);
			ILContext.ToString(stringBuilder, this.Next);
			return stringBuilder.ToString();
		}

		// Token: 0x060029C0 RID: 10688 RVA: 0x0008D668 File Offset: 0x0008B868
		public ILCursor Goto([Nullable(2)] Instruction insn, MoveType moveType = MoveType.Before, bool setTarget = false)
		{
			if (moveType == MoveType.After)
			{
				this._next = ((insn != null) ? insn.Next : null);
			}
			else
			{
				this._next = insn;
			}
			if (setTarget)
			{
				this._searchTarget = ((moveType == MoveType.After) ? SearchTarget.Prev : SearchTarget.Next);
			}
			else
			{
				this._searchTarget = SearchTarget.None;
			}
			if (moveType == MoveType.AfterLabel)
			{
				this.MoveAfterLabels();
			}
			else
			{
				this.MoveBeforeLabels();
			}
			return this;
		}

		// Token: 0x060029C1 RID: 10689 RVA: 0x0008D6C3 File Offset: 0x0008B8C3
		public ILCursor MoveAfterLabels()
		{
			this.MoveAfterLabels(true);
			return this;
		}

		// Token: 0x060029C2 RID: 10690 RVA: 0x0008D6CE File Offset: 0x0008B8CE
		public ILCursor MoveAfterLabels(bool intoEHRanges)
		{
			this._afterLabels = this.IncomingLabels.ToArray<ILLabel>();
			this._afterHandlerStarts = intoEHRanges;
			this._afterHandlerEnds = true;
			return this;
		}

		// Token: 0x060029C3 RID: 10691 RVA: 0x0008D6F0 File Offset: 0x0008B8F0
		public ILCursor MoveBeforeLabels()
		{
			this._afterLabels = null;
			this._afterHandlerStarts = false;
			this._afterHandlerEnds = false;
			return this;
		}

		// Token: 0x060029C4 RID: 10692 RVA: 0x0008D708 File Offset: 0x0008B908
		public ILCursor Goto(int index, MoveType moveType = MoveType.Before, bool setTarget = false)
		{
			if (index < 0)
			{
				index += this.Instrs.Count;
			}
			return this.Goto((index == this.Instrs.Count) ? null : this.Instrs[index], moveType, setTarget);
		}

		// Token: 0x060029C5 RID: 10693 RVA: 0x0008D742 File Offset: 0x0008B942
		public ILCursor GotoLabel(ILLabel label, MoveType moveType = MoveType.AfterLabel, bool setTarget = false)
		{
			return this.Goto(Helpers.ThrowIfNull<ILLabel>(label, "label").Target, moveType, setTarget);
		}

		// Token: 0x060029C6 RID: 10694 RVA: 0x0008D75C File Offset: 0x0008B95C
		public ILCursor GotoNext(MoveType moveType = MoveType.Before, params Func<Instruction, bool>[] predicates)
		{
			if (!this.TryGotoNext(moveType, predicates))
			{
				throw new KeyNotFoundException();
			}
			return this;
		}

		// Token: 0x060029C7 RID: 10695 RVA: 0x0008D770 File Offset: 0x0008B970
		public bool TryGotoNext(MoveType moveType = MoveType.Before, params Func<Instruction, bool>[] predicates)
		{
			Helpers.ThrowIfArgumentNull<Func<Instruction, bool>[]>(predicates, "predicates");
			Collection<Instruction> instrs = this.Instrs;
			int i = this.Index;
			if (this.SearchTarget == SearchTarget.Next)
			{
				i++;
			}
			IL_6D:
			while (i + predicates.Length <= instrs.Count)
			{
				for (int j = 0; j < predicates.Length; j++)
				{
					Func<Instruction, bool> func = predicates[j];
					if (func != null && !func(instrs[i + j]))
					{
						i++;
						goto IL_6D;
					}
				}
				this.Goto((moveType == MoveType.After) ? (i + predicates.Length - 1) : i, moveType, true);
				return true;
			}
			return false;
		}

		// Token: 0x060029C8 RID: 10696 RVA: 0x0008D7F8 File Offset: 0x0008B9F8
		public ILCursor GotoPrev(MoveType moveType = MoveType.Before, params Func<Instruction, bool>[] predicates)
		{
			if (!this.TryGotoPrev(moveType, predicates))
			{
				throw new KeyNotFoundException();
			}
			return this;
		}

		// Token: 0x060029C9 RID: 10697 RVA: 0x0008D80C File Offset: 0x0008BA0C
		public bool TryGotoPrev(MoveType moveType = MoveType.Before, params Func<Instruction, bool>[] predicates)
		{
			Helpers.ThrowIfArgumentNull<Func<Instruction, bool>[]>(predicates, "predicates");
			Collection<Instruction> instrs = this.Instrs;
			int i = this.Index - 1;
			if (this.SearchTarget == SearchTarget.Prev)
			{
				i--;
			}
			i = Math.Min(i, instrs.Count - predicates.Length);
			IL_80:
			while (i >= 0)
			{
				for (int j = 0; j < predicates.Length; j++)
				{
					Func<Instruction, bool> func = predicates[j];
					if (func != null && !func(instrs[i + j]))
					{
						i--;
						goto IL_80;
					}
				}
				this.Goto((moveType == MoveType.After) ? (i + predicates.Length - 1) : i, moveType, true);
				return true;
			}
			return false;
		}

		// Token: 0x060029CA RID: 10698 RVA: 0x0008D89E File Offset: 0x0008BA9E
		public ILCursor GotoNext(params Func<Instruction, bool>[] predicates)
		{
			return this.GotoNext(MoveType.Before, predicates);
		}

		// Token: 0x060029CB RID: 10699 RVA: 0x0008D8A8 File Offset: 0x0008BAA8
		public bool TryGotoNext(params Func<Instruction, bool>[] predicates)
		{
			return this.TryGotoNext(MoveType.Before, predicates);
		}

		// Token: 0x060029CC RID: 10700 RVA: 0x0008D8B2 File Offset: 0x0008BAB2
		public ILCursor GotoPrev(params Func<Instruction, bool>[] predicates)
		{
			return this.GotoPrev(MoveType.Before, predicates);
		}

		// Token: 0x060029CD RID: 10701 RVA: 0x0008D8BC File Offset: 0x0008BABC
		public bool TryGotoPrev(params Func<Instruction, bool>[] predicates)
		{
			return this.TryGotoPrev(MoveType.Before, predicates);
		}

		// Token: 0x060029CE RID: 10702 RVA: 0x0008D8C6 File Offset: 0x0008BAC6
		public void FindNext(out ILCursor[] cursors, params Func<Instruction, bool>[] predicates)
		{
			if (!this.TryFindNext(out cursors, predicates))
			{
				throw new KeyNotFoundException();
			}
		}

		// Token: 0x060029CF RID: 10703 RVA: 0x0008D8D8 File Offset: 0x0008BAD8
		public bool TryFindNext(out ILCursor[] cursors, params Func<Instruction, bool>[] predicates)
		{
			Helpers.ThrowIfArgumentNull<Func<Instruction, bool>[]>(predicates, "predicates");
			cursors = new ILCursor[predicates.Length];
			ILCursor c = this;
			for (int i = 0; i < predicates.Length; i++)
			{
				c = c.Clone();
				if (!c.TryGotoNext(new Func<Instruction, bool>[] { predicates[i] }))
				{
					return false;
				}
				cursors[i] = c;
			}
			return true;
		}

		// Token: 0x060029D0 RID: 10704 RVA: 0x0008D92D File Offset: 0x0008BB2D
		public void FindPrev(out ILCursor[] cursors, params Func<Instruction, bool>[] predicates)
		{
			if (!this.TryFindPrev(out cursors, predicates))
			{
				throw new KeyNotFoundException();
			}
		}

		// Token: 0x060029D1 RID: 10705 RVA: 0x0008D940 File Offset: 0x0008BB40
		public bool TryFindPrev(out ILCursor[] cursors, params Func<Instruction, bool>[] predicates)
		{
			Helpers.ThrowIfArgumentNull<Func<Instruction, bool>[]>(predicates, "predicates");
			cursors = new ILCursor[predicates.Length];
			ILCursor c = this;
			for (int i = predicates.Length - 1; i >= 0; i--)
			{
				c = c.Clone();
				if (!c.TryGotoPrev(new Func<Instruction, bool>[] { predicates[i] }))
				{
					return false;
				}
				cursors[i] = c;
			}
			return true;
		}

		// Token: 0x060029D2 RID: 10706 RVA: 0x0008D998 File Offset: 0x0008BB98
		[NullableContext(2)]
		public void MarkLabel(ILLabel label)
		{
			if (label == null)
			{
				label = new ILLabel(this.Context);
			}
			label.Target = this.Next;
			if (this._afterLabels != null)
			{
				Array.Resize<ILLabel>(ref this._afterLabels, this._afterLabels.Length + 1);
				this._afterLabels[this._afterLabels.Length - 1] = label;
				return;
			}
			this._afterLabels = new ILLabel[] { label };
		}

		// Token: 0x060029D3 RID: 10707 RVA: 0x0008DA04 File Offset: 0x0008BC04
		public ILLabel MarkLabel(Instruction inst)
		{
			ILLabel label = this.Context.DefineLabel();
			if (inst == this.Next)
			{
				this.MarkLabel(label);
				return label;
			}
			label.Target = inst;
			return label;
		}

		// Token: 0x060029D4 RID: 10708 RVA: 0x0008DA38 File Offset: 0x0008BC38
		public ILLabel MarkLabel()
		{
			ILLabel label = this.DefineLabel();
			this.MarkLabel(label);
			return label;
		}

		// Token: 0x060029D5 RID: 10709 RVA: 0x0008DA54 File Offset: 0x0008BC54
		public ILLabel DefineLabel()
		{
			return this.Context.DefineLabel();
		}

		// Token: 0x060029D6 RID: 10710 RVA: 0x0008DA61 File Offset: 0x0008BC61
		public VariableDefinition CreateLocal<[Nullable(2)] T>()
		{
			return this.Context.CreateLocal<T>();
		}

		// Token: 0x060029D7 RID: 10711 RVA: 0x0008DA6E File Offset: 0x0008BC6E
		public VariableDefinition CreateLocal(Type type)
		{
			return this.Context.CreateLocal(type);
		}

		// Token: 0x060029D8 RID: 10712 RVA: 0x0008DA7C File Offset: 0x0008BC7C
		public VariableDefinition CreateLocal(TypeReference typeRef)
		{
			return this.Context.CreateLocal(typeRef);
		}

		// Token: 0x060029D9 RID: 10713 RVA: 0x0008DA8C File Offset: 0x0008BC8C
		private ILCursor _Insert(Instruction instr)
		{
			if (this._afterLabels != null)
			{
				ILLabel[] afterLabels = this._afterLabels;
				for (int i = 0; i < afterLabels.Length; i++)
				{
					afterLabels[i].Target = instr;
				}
			}
			if (this._afterHandlerStarts)
			{
				foreach (ExceptionHandler eh in this.Body.ExceptionHandlers)
				{
					if (eh.TryStart == this.Next)
					{
						eh.TryStart = instr;
					}
					if (eh.HandlerStart == this.Next)
					{
						eh.HandlerStart = instr;
					}
					if (eh.FilterStart == this.Next)
					{
						eh.FilterStart = instr;
					}
				}
			}
			if (this._afterHandlerEnds)
			{
				foreach (ExceptionHandler eh2 in this.Body.ExceptionHandlers)
				{
					if (eh2.TryEnd == this.Next)
					{
						eh2.TryEnd = instr;
					}
					if (eh2.HandlerEnd == this.Next)
					{
						eh2.HandlerEnd = instr;
					}
				}
			}
			this.Instrs.Insert(this.Index, instr);
			this.Goto(instr, MoveType.After, false);
			return this;
		}

		// Token: 0x060029DA RID: 10714 RVA: 0x0008DBE0 File Offset: 0x0008BDE0
		public ILCursor Remove()
		{
			return this.RemoveRange(1);
		}

		// Token: 0x060029DB RID: 10715 RVA: 0x0008DBEC File Offset: 0x0008BDEC
		public ILCursor RemoveRange(int num)
		{
			int index = this.Index;
			Instruction newTarget = ((index + num < this.Instrs.Count) ? this.Instrs[index + num] : null);
			foreach (ILLabel illabel in this.IncomingLabels)
			{
				illabel.Target = newTarget;
			}
			using (Collection<ExceptionHandler>.Enumerator enumerator2 = this.Body.ExceptionHandlers.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					ExceptionHandler eh = enumerator2.Current;
					if (eh.TryStart == this.Next)
					{
						eh.TryStart = newTarget;
					}
					if (eh.TryEnd == this.Next)
					{
						eh.TryEnd = newTarget;
					}
					if (eh.HandlerStart == this.Next)
					{
						eh.HandlerStart = newTarget;
					}
					if (eh.FilterStart == this.Next)
					{
						eh.FilterStart = newTarget;
					}
					if (eh.HandlerEnd == this.Next)
					{
						eh.HandlerEnd = newTarget;
					}
				}
				goto IL_10E;
			}
			IL_102:
			this.Instrs.RemoveAt(index);
			IL_10E:
			if (num-- <= 0)
			{
				this._searchTarget = SearchTarget.None;
				this._next = newTarget;
				return this;
			}
			goto IL_102;
		}

		// Token: 0x060029DC RID: 10716 RVA: 0x0008DD3C File Offset: 0x0008BF3C
		public ILCursor Emit(OpCode opcode, ParameterDefinition parameter)
		{
			return this._Insert(this.IL.Create(opcode, parameter));
		}

		// Token: 0x060029DD RID: 10717 RVA: 0x0008DD51 File Offset: 0x0008BF51
		public ILCursor Emit(OpCode opcode, VariableDefinition variable)
		{
			return this._Insert(this.IL.Create(opcode, variable));
		}

		// Token: 0x060029DE RID: 10718 RVA: 0x0008DD66 File Offset: 0x0008BF66
		public ILCursor Emit(OpCode opcode, Instruction[] targets)
		{
			return this._Insert(this.IL.Create(opcode, targets));
		}

		// Token: 0x060029DF RID: 10719 RVA: 0x0008DD7B File Offset: 0x0008BF7B
		public ILCursor Emit(OpCode opcode, Instruction target)
		{
			return this._Insert(this.IL.Create(opcode, target));
		}

		// Token: 0x060029E0 RID: 10720 RVA: 0x0008DD90 File Offset: 0x0008BF90
		public ILCursor Emit(OpCode opcode, double value)
		{
			return this._Insert(this.IL.Create(opcode, value));
		}

		// Token: 0x060029E1 RID: 10721 RVA: 0x0008DDA5 File Offset: 0x0008BFA5
		public ILCursor Emit(OpCode opcode, float value)
		{
			return this._Insert(this.IL.Create(opcode, value));
		}

		// Token: 0x060029E2 RID: 10722 RVA: 0x0008DDBA File Offset: 0x0008BFBA
		public ILCursor Emit(OpCode opcode, long value)
		{
			return this._Insert(this.IL.Create(opcode, value));
		}

		// Token: 0x060029E3 RID: 10723 RVA: 0x0008DDCF File Offset: 0x0008BFCF
		public ILCursor Emit(OpCode opcode, sbyte value)
		{
			return this._Insert(this.IL.Create(opcode, value));
		}

		// Token: 0x060029E4 RID: 10724 RVA: 0x0008DDE4 File Offset: 0x0008BFE4
		public ILCursor Emit(OpCode opcode, byte value)
		{
			return this._Insert(this.IL.Create(opcode, value));
		}

		// Token: 0x060029E5 RID: 10725 RVA: 0x0008DDF9 File Offset: 0x0008BFF9
		public ILCursor Emit(OpCode opcode, string value)
		{
			return this._Insert(this.IL.Create(opcode, value));
		}

		// Token: 0x060029E6 RID: 10726 RVA: 0x0008DE0E File Offset: 0x0008C00E
		public ILCursor Emit(OpCode opcode, FieldReference field)
		{
			return this._Insert(this.IL.Create(opcode, field));
		}

		// Token: 0x060029E7 RID: 10727 RVA: 0x0008DE23 File Offset: 0x0008C023
		public ILCursor Emit(OpCode opcode, Mono.Cecil.CallSite site)
		{
			return this._Insert(this.IL.Create(opcode, site));
		}

		// Token: 0x060029E8 RID: 10728 RVA: 0x0008DE38 File Offset: 0x0008C038
		public ILCursor Emit(OpCode opcode, TypeReference type)
		{
			return this._Insert(this.IL.Create(opcode, type));
		}

		// Token: 0x060029E9 RID: 10729 RVA: 0x0008DE4D File Offset: 0x0008C04D
		public ILCursor Emit(OpCode opcode)
		{
			return this._Insert(this.IL.Create(opcode));
		}

		// Token: 0x060029EA RID: 10730 RVA: 0x0008DE61 File Offset: 0x0008C061
		public ILCursor Emit(OpCode opcode, int value)
		{
			return this._Insert(this.IL.Create(opcode, value));
		}

		// Token: 0x060029EB RID: 10731 RVA: 0x0008DE76 File Offset: 0x0008C076
		public ILCursor Emit(OpCode opcode, MethodReference method)
		{
			return this._Insert(this.IL.Create(opcode, method));
		}

		// Token: 0x060029EC RID: 10732 RVA: 0x0008DE8B File Offset: 0x0008C08B
		public ILCursor Emit(OpCode opcode, FieldInfo field)
		{
			return this._Insert(this.IL.Create(opcode, field));
		}

		// Token: 0x060029ED RID: 10733 RVA: 0x0008DEA0 File Offset: 0x0008C0A0
		public ILCursor Emit(OpCode opcode, MethodBase method)
		{
			return this._Insert(this.IL.Create(opcode, method));
		}

		// Token: 0x060029EE RID: 10734 RVA: 0x0008DEB5 File Offset: 0x0008C0B5
		public ILCursor Emit(OpCode opcode, Type type)
		{
			return this._Insert(this.IL.Create(opcode, type));
		}

		// Token: 0x060029EF RID: 10735 RVA: 0x0008DECA File Offset: 0x0008C0CA
		public ILCursor Emit(OpCode opcode, object operand)
		{
			return this._Insert(this.IL.Create(opcode, operand));
		}

		// Token: 0x060029F0 RID: 10736 RVA: 0x0008DEDF File Offset: 0x0008C0DF
		public ILCursor Emit<[Nullable(2)] T>(OpCode opcode, string memberName)
		{
			return this._Insert(this.IL.Create(opcode, typeof(T).GetMember(memberName, (BindingFlags)(-1)).First<MemberInfo>()));
		}

		// Token: 0x060029F1 RID: 10737 RVA: 0x0008DF09 File Offset: 0x0008C109
		public int AddReference<[Nullable(2)] T>(in T t)
		{
			return this.Context.AddReference<T>(t);
		}

		// Token: 0x060029F2 RID: 10738 RVA: 0x0008DF17 File Offset: 0x0008C117
		[NullableContext(2)]
		public void EmitGetReference<T>(int id)
		{
			this.EmitLoadTypedReference(this.Context.GetReferenceCell(id), typeof(T));
		}

		// Token: 0x060029F3 RID: 10739 RVA: 0x0008DF38 File Offset: 0x0008C138
		[NullableContext(2)]
		public int EmitReference<T>(in T t)
		{
			int id = this.AddReference<T>(t);
			this.EmitLoadTypedReferenceUnsafe(this.Context.GetReferenceCell(id), typeof(T));
			return id;
		}

		// Token: 0x060029F4 RID: 10740 RVA: 0x0008DF6C File Offset: 0x0008C16C
		public int EmitDelegate<[Nullable(0)] T>(T cb) where T : Delegate
		{
			Helpers.ThrowIfArgumentNull<T>(cb, "cb");
			if (cb.GetInvocationList().Length == 1 && cb.Target == null)
			{
				this.Emit(OpCodes.Call, cb.Method);
				return -1;
			}
			ValueTuple<MethodInfo, Type>? invoker = FastDelegateInvokers.GetDelegateInvoker(typeof(T));
			int id;
			if (invoker != null)
			{
				ValueTuple<MethodInfo, Type> pair = invoker.GetValueOrDefault();
				Delegate cast = cb.CastDelegate(pair.Item2);
				id = this.EmitReference<Delegate>(cast);
				this.AddReference<MethodInfo>(pair.Item1);
				this.Emit(OpCodes.Call, pair.Item1);
			}
			else
			{
				id = this.EmitReference<T>(cb);
				MethodInfo delInvoke = typeof(T).GetMethod("Invoke");
				this.Emit(OpCodes.Callvirt, delInvoke);
			}
			return id;
		}

		// Token: 0x060029F5 RID: 10741 RVA: 0x0008E046 File Offset: 0x0008C246
		public ILCursor EmitAdd()
		{
			return this._Insert(this.IL.Create(OpCodes.Add));
		}

		// Token: 0x060029F6 RID: 10742 RVA: 0x0008E05E File Offset: 0x0008C25E
		public ILCursor EmitAddOvf()
		{
			return this._Insert(this.IL.Create(OpCodes.Add_Ovf));
		}

		// Token: 0x060029F7 RID: 10743 RVA: 0x0008E076 File Offset: 0x0008C276
		public ILCursor EmitAddOvfUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Add_Ovf_Un));
		}

		// Token: 0x060029F8 RID: 10744 RVA: 0x0008E08E File Offset: 0x0008C28E
		public ILCursor EmitAnd()
		{
			return this._Insert(this.IL.Create(OpCodes.And));
		}

		// Token: 0x060029F9 RID: 10745 RVA: 0x0008E0A6 File Offset: 0x0008C2A6
		public ILCursor EmitArglist()
		{
			return this._Insert(this.IL.Create(OpCodes.Arglist));
		}

		// Token: 0x060029FA RID: 10746 RVA: 0x0008E0BE File Offset: 0x0008C2BE
		public ILCursor EmitBeq(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Beq, operand));
		}

		// Token: 0x060029FB RID: 10747 RVA: 0x0008E0D7 File Offset: 0x0008C2D7
		public ILCursor EmitBeq(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Beq, this.MarkLabel(operand)));
		}

		// Token: 0x060029FC RID: 10748 RVA: 0x0008E0F6 File Offset: 0x0008C2F6
		public ILCursor EmitBge(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bge, operand));
		}

		// Token: 0x060029FD RID: 10749 RVA: 0x0008E10F File Offset: 0x0008C30F
		public ILCursor EmitBge(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bge, this.MarkLabel(operand)));
		}

		// Token: 0x060029FE RID: 10750 RVA: 0x0008E12E File Offset: 0x0008C32E
		public ILCursor EmitBgeUn(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bge_Un, operand));
		}

		// Token: 0x060029FF RID: 10751 RVA: 0x0008E147 File Offset: 0x0008C347
		public ILCursor EmitBgeUn(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bge_Un, this.MarkLabel(operand)));
		}

		// Token: 0x06002A00 RID: 10752 RVA: 0x0008E166 File Offset: 0x0008C366
		public ILCursor EmitBgt(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bgt, operand));
		}

		// Token: 0x06002A01 RID: 10753 RVA: 0x0008E17F File Offset: 0x0008C37F
		public ILCursor EmitBgt(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bgt, this.MarkLabel(operand)));
		}

		// Token: 0x06002A02 RID: 10754 RVA: 0x0008E19E File Offset: 0x0008C39E
		public ILCursor EmitBgtUn(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bgt_Un, operand));
		}

		// Token: 0x06002A03 RID: 10755 RVA: 0x0008E1B7 File Offset: 0x0008C3B7
		public ILCursor EmitBgtUn(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bgt_Un, this.MarkLabel(operand)));
		}

		// Token: 0x06002A04 RID: 10756 RVA: 0x0008E1D6 File Offset: 0x0008C3D6
		public ILCursor EmitBle(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ble, operand));
		}

		// Token: 0x06002A05 RID: 10757 RVA: 0x0008E1EF File Offset: 0x0008C3EF
		public ILCursor EmitBle(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ble, this.MarkLabel(operand)));
		}

		// Token: 0x06002A06 RID: 10758 RVA: 0x0008E20E File Offset: 0x0008C40E
		public ILCursor EmitBleUn(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ble_Un, operand));
		}

		// Token: 0x06002A07 RID: 10759 RVA: 0x0008E227 File Offset: 0x0008C427
		public ILCursor EmitBleUn(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ble_Un, this.MarkLabel(operand)));
		}

		// Token: 0x06002A08 RID: 10760 RVA: 0x0008E246 File Offset: 0x0008C446
		public ILCursor EmitBlt(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Blt, operand));
		}

		// Token: 0x06002A09 RID: 10761 RVA: 0x0008E25F File Offset: 0x0008C45F
		public ILCursor EmitBlt(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Blt, this.MarkLabel(operand)));
		}

		// Token: 0x06002A0A RID: 10762 RVA: 0x0008E27E File Offset: 0x0008C47E
		public ILCursor EmitBltUn(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Blt_Un, operand));
		}

		// Token: 0x06002A0B RID: 10763 RVA: 0x0008E297 File Offset: 0x0008C497
		public ILCursor EmitBltUn(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Blt_Un, this.MarkLabel(operand)));
		}

		// Token: 0x06002A0C RID: 10764 RVA: 0x0008E2B6 File Offset: 0x0008C4B6
		public ILCursor EmitBneUn(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bne_Un, operand));
		}

		// Token: 0x06002A0D RID: 10765 RVA: 0x0008E2CF File Offset: 0x0008C4CF
		public ILCursor EmitBneUn(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Bne_Un, this.MarkLabel(operand)));
		}

		// Token: 0x06002A0E RID: 10766 RVA: 0x0008E2EE File Offset: 0x0008C4EE
		public ILCursor EmitBox(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Box, operand));
		}

		// Token: 0x06002A0F RID: 10767 RVA: 0x0008E307 File Offset: 0x0008C507
		public ILCursor EmitBox(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Box, this.Context.Import(operand)));
		}

		// Token: 0x06002A10 RID: 10768 RVA: 0x0008E32B File Offset: 0x0008C52B
		public ILCursor EmitBr(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Br, operand));
		}

		// Token: 0x06002A11 RID: 10769 RVA: 0x0008E344 File Offset: 0x0008C544
		public ILCursor EmitBr(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Br, this.MarkLabel(operand)));
		}

		// Token: 0x06002A12 RID: 10770 RVA: 0x0008E363 File Offset: 0x0008C563
		public ILCursor EmitBreak()
		{
			return this._Insert(this.IL.Create(OpCodes.Break));
		}

		// Token: 0x06002A13 RID: 10771 RVA: 0x0008E37B File Offset: 0x0008C57B
		public ILCursor EmitBrfalse(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Brfalse, operand));
		}

		// Token: 0x06002A14 RID: 10772 RVA: 0x0008E394 File Offset: 0x0008C594
		public ILCursor EmitBrfalse(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Brfalse, this.MarkLabel(operand)));
		}

		// Token: 0x06002A15 RID: 10773 RVA: 0x0008E3B3 File Offset: 0x0008C5B3
		public ILCursor EmitBrtrue(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Brtrue, operand));
		}

		// Token: 0x06002A16 RID: 10774 RVA: 0x0008E3CC File Offset: 0x0008C5CC
		public ILCursor EmitBrtrue(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Brtrue, this.MarkLabel(operand)));
		}

		// Token: 0x06002A17 RID: 10775 RVA: 0x0008E3EB File Offset: 0x0008C5EB
		public ILCursor EmitCall(MethodReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Call, operand));
		}

		// Token: 0x06002A18 RID: 10776 RVA: 0x0008E404 File Offset: 0x0008C604
		public ILCursor EmitCall(MethodBase operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Call, this.Context.Import(operand)));
		}

		// Token: 0x06002A19 RID: 10777 RVA: 0x0008E428 File Offset: 0x0008C628
		public ILCursor EmitCalli(IMethodSignature operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Calli, operand));
		}

		// Token: 0x06002A1A RID: 10778 RVA: 0x0008E441 File Offset: 0x0008C641
		public ILCursor EmitCallvirt(MethodReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Callvirt, operand));
		}

		// Token: 0x06002A1B RID: 10779 RVA: 0x0008E45A File Offset: 0x0008C65A
		public ILCursor EmitCallvirt(MethodBase operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Callvirt, this.Context.Import(operand)));
		}

		// Token: 0x06002A1C RID: 10780 RVA: 0x0008E47E File Offset: 0x0008C67E
		public ILCursor EmitCastclass(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Castclass, operand));
		}

		// Token: 0x06002A1D RID: 10781 RVA: 0x0008E497 File Offset: 0x0008C697
		public ILCursor EmitCastclass(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Castclass, this.Context.Import(operand)));
		}

		// Token: 0x06002A1E RID: 10782 RVA: 0x0008E4BB File Offset: 0x0008C6BB
		public ILCursor EmitCeq()
		{
			return this._Insert(this.IL.Create(OpCodes.Ceq));
		}

		// Token: 0x06002A1F RID: 10783 RVA: 0x0008E4D3 File Offset: 0x0008C6D3
		public ILCursor EmitCgt()
		{
			return this._Insert(this.IL.Create(OpCodes.Cgt));
		}

		// Token: 0x06002A20 RID: 10784 RVA: 0x0008E4EB File Offset: 0x0008C6EB
		public ILCursor EmitCgtUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Cgt_Un));
		}

		// Token: 0x06002A21 RID: 10785 RVA: 0x0008E503 File Offset: 0x0008C703
		public ILCursor EmitCkfinite()
		{
			return this._Insert(this.IL.Create(OpCodes.Ckfinite));
		}

		// Token: 0x06002A22 RID: 10786 RVA: 0x0008E51B File Offset: 0x0008C71B
		public ILCursor EmitClt()
		{
			return this._Insert(this.IL.Create(OpCodes.Clt));
		}

		// Token: 0x06002A23 RID: 10787 RVA: 0x0008E533 File Offset: 0x0008C733
		public ILCursor EmitCltUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Clt_Un));
		}

		// Token: 0x06002A24 RID: 10788 RVA: 0x0008E54B File Offset: 0x0008C74B
		public ILCursor EmitConstrained(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Constrained, operand));
		}

		// Token: 0x06002A25 RID: 10789 RVA: 0x0008E564 File Offset: 0x0008C764
		public ILCursor EmitConstrained(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Constrained, this.Context.Import(operand)));
		}

		// Token: 0x06002A26 RID: 10790 RVA: 0x0008E588 File Offset: 0x0008C788
		public ILCursor EmitConvI()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_I));
		}

		// Token: 0x06002A27 RID: 10791 RVA: 0x0008E5A0 File Offset: 0x0008C7A0
		public ILCursor EmitConvI1()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_I1));
		}

		// Token: 0x06002A28 RID: 10792 RVA: 0x0008E5B8 File Offset: 0x0008C7B8
		public ILCursor EmitConvI2()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_I2));
		}

		// Token: 0x06002A29 RID: 10793 RVA: 0x0008E5D0 File Offset: 0x0008C7D0
		public ILCursor EmitConvI4()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_I4));
		}

		// Token: 0x06002A2A RID: 10794 RVA: 0x0008E5E8 File Offset: 0x0008C7E8
		public ILCursor EmitConvI8()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_I8));
		}

		// Token: 0x06002A2B RID: 10795 RVA: 0x0008E600 File Offset: 0x0008C800
		public ILCursor EmitConvOvfI()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I));
		}

		// Token: 0x06002A2C RID: 10796 RVA: 0x0008E618 File Offset: 0x0008C818
		public ILCursor EmitConvOvfIUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I_Un));
		}

		// Token: 0x06002A2D RID: 10797 RVA: 0x0008E630 File Offset: 0x0008C830
		public ILCursor EmitConvOvfI1()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I1));
		}

		// Token: 0x06002A2E RID: 10798 RVA: 0x0008E648 File Offset: 0x0008C848
		public ILCursor EmitConvOvfI1Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I1_Un));
		}

		// Token: 0x06002A2F RID: 10799 RVA: 0x0008E660 File Offset: 0x0008C860
		public ILCursor EmitConvOvfI2()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I2));
		}

		// Token: 0x06002A30 RID: 10800 RVA: 0x0008E678 File Offset: 0x0008C878
		public ILCursor EmitConvOvfI2Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I2_Un));
		}

		// Token: 0x06002A31 RID: 10801 RVA: 0x0008E690 File Offset: 0x0008C890
		public ILCursor EmitConvOvfI4()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I4));
		}

		// Token: 0x06002A32 RID: 10802 RVA: 0x0008E6A8 File Offset: 0x0008C8A8
		public ILCursor EmitConvOvfI4Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I4_Un));
		}

		// Token: 0x06002A33 RID: 10803 RVA: 0x0008E6C0 File Offset: 0x0008C8C0
		public ILCursor EmitConvOvfI8()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I8));
		}

		// Token: 0x06002A34 RID: 10804 RVA: 0x0008E6D8 File Offset: 0x0008C8D8
		public ILCursor EmitConvOvfI8Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_I8_Un));
		}

		// Token: 0x06002A35 RID: 10805 RVA: 0x0008E6F0 File Offset: 0x0008C8F0
		public ILCursor EmitConvOvfU()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U));
		}

		// Token: 0x06002A36 RID: 10806 RVA: 0x0008E708 File Offset: 0x0008C908
		public ILCursor EmitConvOvfUUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U_Un));
		}

		// Token: 0x06002A37 RID: 10807 RVA: 0x0008E720 File Offset: 0x0008C920
		public ILCursor EmitConvOvfU1()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U1));
		}

		// Token: 0x06002A38 RID: 10808 RVA: 0x0008E738 File Offset: 0x0008C938
		public ILCursor EmitConvOvfU1Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U1_Un));
		}

		// Token: 0x06002A39 RID: 10809 RVA: 0x0008E750 File Offset: 0x0008C950
		public ILCursor EmitConvOvfU2()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U2));
		}

		// Token: 0x06002A3A RID: 10810 RVA: 0x0008E768 File Offset: 0x0008C968
		public ILCursor EmitConvOvfU2Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U2_Un));
		}

		// Token: 0x06002A3B RID: 10811 RVA: 0x0008E780 File Offset: 0x0008C980
		public ILCursor EmitConvOvfU4()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U4));
		}

		// Token: 0x06002A3C RID: 10812 RVA: 0x0008E798 File Offset: 0x0008C998
		public ILCursor EmitConvOvfU4Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U4_Un));
		}

		// Token: 0x06002A3D RID: 10813 RVA: 0x0008E7B0 File Offset: 0x0008C9B0
		public ILCursor EmitConvOvfU8()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U8));
		}

		// Token: 0x06002A3E RID: 10814 RVA: 0x0008E7C8 File Offset: 0x0008C9C8
		public ILCursor EmitConvOvfU8Un()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_Ovf_U8_Un));
		}

		// Token: 0x06002A3F RID: 10815 RVA: 0x0008E7E0 File Offset: 0x0008C9E0
		public ILCursor EmitConvRUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_R_Un));
		}

		// Token: 0x06002A40 RID: 10816 RVA: 0x0008E7F8 File Offset: 0x0008C9F8
		public ILCursor EmitConvR4()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_R4));
		}

		// Token: 0x06002A41 RID: 10817 RVA: 0x0008E810 File Offset: 0x0008CA10
		public ILCursor EmitConvR8()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_R8));
		}

		// Token: 0x06002A42 RID: 10818 RVA: 0x0008E828 File Offset: 0x0008CA28
		public ILCursor EmitConvU()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_U));
		}

		// Token: 0x06002A43 RID: 10819 RVA: 0x0008E840 File Offset: 0x0008CA40
		public ILCursor EmitConvU1()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_U1));
		}

		// Token: 0x06002A44 RID: 10820 RVA: 0x0008E858 File Offset: 0x0008CA58
		public ILCursor EmitConvU2()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_U2));
		}

		// Token: 0x06002A45 RID: 10821 RVA: 0x0008E870 File Offset: 0x0008CA70
		public ILCursor EmitConvU4()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_U4));
		}

		// Token: 0x06002A46 RID: 10822 RVA: 0x0008E888 File Offset: 0x0008CA88
		public ILCursor EmitConvU8()
		{
			return this._Insert(this.IL.Create(OpCodes.Conv_U8));
		}

		// Token: 0x06002A47 RID: 10823 RVA: 0x0008E8A0 File Offset: 0x0008CAA0
		public ILCursor EmitCpblk()
		{
			return this._Insert(this.IL.Create(OpCodes.Cpblk));
		}

		// Token: 0x06002A48 RID: 10824 RVA: 0x0008E8B8 File Offset: 0x0008CAB8
		public ILCursor EmitCpobj(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Cpobj, operand));
		}

		// Token: 0x06002A49 RID: 10825 RVA: 0x0008E8D1 File Offset: 0x0008CAD1
		public ILCursor EmitCpobj(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Cpobj, this.Context.Import(operand)));
		}

		// Token: 0x06002A4A RID: 10826 RVA: 0x0008E8F5 File Offset: 0x0008CAF5
		public ILCursor EmitDiv()
		{
			return this._Insert(this.IL.Create(OpCodes.Div));
		}

		// Token: 0x06002A4B RID: 10827 RVA: 0x0008E90D File Offset: 0x0008CB0D
		public ILCursor EmitDivUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Div_Un));
		}

		// Token: 0x06002A4C RID: 10828 RVA: 0x0008E925 File Offset: 0x0008CB25
		public ILCursor EmitDup()
		{
			return this._Insert(this.IL.Create(OpCodes.Dup));
		}

		// Token: 0x06002A4D RID: 10829 RVA: 0x0008E93D File Offset: 0x0008CB3D
		public ILCursor EmitEndfilter()
		{
			return this._Insert(this.IL.Create(OpCodes.Endfilter));
		}

		// Token: 0x06002A4E RID: 10830 RVA: 0x0008E955 File Offset: 0x0008CB55
		public ILCursor EmitEndfinally()
		{
			return this._Insert(this.IL.Create(OpCodes.Endfinally));
		}

		// Token: 0x06002A4F RID: 10831 RVA: 0x0008E96D File Offset: 0x0008CB6D
		public ILCursor EmitInitblk()
		{
			return this._Insert(this.IL.Create(OpCodes.Initblk));
		}

		// Token: 0x06002A50 RID: 10832 RVA: 0x0008E985 File Offset: 0x0008CB85
		public ILCursor EmitInitobj(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Initobj, operand));
		}

		// Token: 0x06002A51 RID: 10833 RVA: 0x0008E99E File Offset: 0x0008CB9E
		public ILCursor EmitInitobj(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Initobj, this.Context.Import(operand)));
		}

		// Token: 0x06002A52 RID: 10834 RVA: 0x0008E9C2 File Offset: 0x0008CBC2
		public ILCursor EmitIsinst(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Isinst, operand));
		}

		// Token: 0x06002A53 RID: 10835 RVA: 0x0008E9DB File Offset: 0x0008CBDB
		public ILCursor EmitIsinst(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Isinst, this.Context.Import(operand)));
		}

		// Token: 0x06002A54 RID: 10836 RVA: 0x0008E9FF File Offset: 0x0008CBFF
		public ILCursor EmitJmp(MethodReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Jmp, operand));
		}

		// Token: 0x06002A55 RID: 10837 RVA: 0x0008EA18 File Offset: 0x0008CC18
		public ILCursor EmitJmp(MethodBase operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Jmp, this.Context.Import(operand)));
		}

		// Token: 0x06002A56 RID: 10838 RVA: 0x0008EA3C File Offset: 0x0008CC3C
		public ILCursor EmitLdarg0()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarg_0));
		}

		// Token: 0x06002A57 RID: 10839 RVA: 0x0008EA54 File Offset: 0x0008CC54
		public ILCursor EmitLdarg1()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarg_1));
		}

		// Token: 0x06002A58 RID: 10840 RVA: 0x0008EA6C File Offset: 0x0008CC6C
		public ILCursor EmitLdarg2()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarg_2));
		}

		// Token: 0x06002A59 RID: 10841 RVA: 0x0008EA84 File Offset: 0x0008CC84
		public ILCursor EmitLdarg3()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarg_3));
		}

		// Token: 0x06002A5A RID: 10842 RVA: 0x0008EA9C File Offset: 0x0008CC9C
		public ILCursor EmitLdarg(int operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarg, operand));
		}

		// Token: 0x06002A5B RID: 10843 RVA: 0x0008EA9C File Offset: 0x0008CC9C
		public ILCursor EmitLdarg(uint operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarg, (int)operand));
		}

		// Token: 0x06002A5C RID: 10844 RVA: 0x0008EAB5 File Offset: 0x0008CCB5
		public ILCursor EmitLdarg(ParameterReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarg, operand));
		}

		// Token: 0x06002A5D RID: 10845 RVA: 0x0008EACE File Offset: 0x0008CCCE
		public ILCursor EmitLdarga(int operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarga, operand));
		}

		// Token: 0x06002A5E RID: 10846 RVA: 0x0008EACE File Offset: 0x0008CCCE
		public ILCursor EmitLdarga(uint operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarga, (int)operand));
		}

		// Token: 0x06002A5F RID: 10847 RVA: 0x0008EAE7 File Offset: 0x0008CCE7
		public ILCursor EmitLdarga(ParameterReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldarga, operand));
		}

		// Token: 0x06002A60 RID: 10848 RVA: 0x0008EB00 File Offset: 0x0008CD00
		public ILCursor EmitLdcI4(int operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldc_I4, operand));
		}

		// Token: 0x06002A61 RID: 10849 RVA: 0x0008EB00 File Offset: 0x0008CD00
		public ILCursor EmitLdcI4(uint operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldc_I4, (int)operand));
		}

		// Token: 0x06002A62 RID: 10850 RVA: 0x0008EB19 File Offset: 0x0008CD19
		public ILCursor EmitLdcI8(long operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldc_I8, operand));
		}

		// Token: 0x06002A63 RID: 10851 RVA: 0x0008EB19 File Offset: 0x0008CD19
		public ILCursor EmitLdcI8(ulong operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldc_I8, (long)operand));
		}

		// Token: 0x06002A64 RID: 10852 RVA: 0x0008EB32 File Offset: 0x0008CD32
		public ILCursor EmitLdcR4(float operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldc_R4, operand));
		}

		// Token: 0x06002A65 RID: 10853 RVA: 0x0008EB4B File Offset: 0x0008CD4B
		public ILCursor EmitLdcR8(double operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldc_R8, operand));
		}

		// Token: 0x06002A66 RID: 10854 RVA: 0x0008EB64 File Offset: 0x0008CD64
		public ILCursor EmitLdelemAny(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_Any, operand));
		}

		// Token: 0x06002A67 RID: 10855 RVA: 0x0008EB7D File Offset: 0x0008CD7D
		public ILCursor EmitLdelemAny(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_Any, this.Context.Import(operand)));
		}

		// Token: 0x06002A68 RID: 10856 RVA: 0x0008EBA1 File Offset: 0x0008CDA1
		public ILCursor EmitLdelemI()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_I));
		}

		// Token: 0x06002A69 RID: 10857 RVA: 0x0008EBB9 File Offset: 0x0008CDB9
		public ILCursor EmitLdelemI1()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_I1));
		}

		// Token: 0x06002A6A RID: 10858 RVA: 0x0008EBD1 File Offset: 0x0008CDD1
		public ILCursor EmitLdelemI2()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_I2));
		}

		// Token: 0x06002A6B RID: 10859 RVA: 0x0008EBE9 File Offset: 0x0008CDE9
		public ILCursor EmitLdelemI4()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_I4));
		}

		// Token: 0x06002A6C RID: 10860 RVA: 0x0008EC01 File Offset: 0x0008CE01
		public ILCursor EmitLdelemI8()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_I8));
		}

		// Token: 0x06002A6D RID: 10861 RVA: 0x0008EC19 File Offset: 0x0008CE19
		public ILCursor EmitLdelemR4()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_R4));
		}

		// Token: 0x06002A6E RID: 10862 RVA: 0x0008EC31 File Offset: 0x0008CE31
		public ILCursor EmitLdelemR8()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_R8));
		}

		// Token: 0x06002A6F RID: 10863 RVA: 0x0008EC49 File Offset: 0x0008CE49
		public ILCursor EmitLdelemRef()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_Ref));
		}

		// Token: 0x06002A70 RID: 10864 RVA: 0x0008EC61 File Offset: 0x0008CE61
		public ILCursor EmitLdelemU1()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_U1));
		}

		// Token: 0x06002A71 RID: 10865 RVA: 0x0008EC79 File Offset: 0x0008CE79
		public ILCursor EmitLdelemU2()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_U2));
		}

		// Token: 0x06002A72 RID: 10866 RVA: 0x0008EC91 File Offset: 0x0008CE91
		public ILCursor EmitLdelemU4()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelem_U4));
		}

		// Token: 0x06002A73 RID: 10867 RVA: 0x0008ECA9 File Offset: 0x0008CEA9
		public ILCursor EmitLdelema(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelema, operand));
		}

		// Token: 0x06002A74 RID: 10868 RVA: 0x0008ECC2 File Offset: 0x0008CEC2
		public ILCursor EmitLdelema(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldelema, this.Context.Import(operand)));
		}

		// Token: 0x06002A75 RID: 10869 RVA: 0x0008ECE6 File Offset: 0x0008CEE6
		public ILCursor EmitLdfld(FieldReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldfld, operand));
		}

		// Token: 0x06002A76 RID: 10870 RVA: 0x0008ECFF File Offset: 0x0008CEFF
		public ILCursor EmitLdfld(FieldInfo operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldfld, this.Context.Import(operand)));
		}

		// Token: 0x06002A77 RID: 10871 RVA: 0x0008ED23 File Offset: 0x0008CF23
		public ILCursor EmitLdflda(FieldReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldflda, operand));
		}

		// Token: 0x06002A78 RID: 10872 RVA: 0x0008ED3C File Offset: 0x0008CF3C
		public ILCursor EmitLdflda(FieldInfo operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldflda, this.Context.Import(operand)));
		}

		// Token: 0x06002A79 RID: 10873 RVA: 0x0008ED60 File Offset: 0x0008CF60
		public ILCursor EmitLdftn(MethodReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldftn, operand));
		}

		// Token: 0x06002A7A RID: 10874 RVA: 0x0008ED79 File Offset: 0x0008CF79
		public ILCursor EmitLdftn(MethodBase operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldftn, this.Context.Import(operand)));
		}

		// Token: 0x06002A7B RID: 10875 RVA: 0x0008ED9D File Offset: 0x0008CF9D
		public ILCursor EmitLdindI()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_I));
		}

		// Token: 0x06002A7C RID: 10876 RVA: 0x0008EDB5 File Offset: 0x0008CFB5
		public ILCursor EmitLdindI1()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_I1));
		}

		// Token: 0x06002A7D RID: 10877 RVA: 0x0008EDCD File Offset: 0x0008CFCD
		public ILCursor EmitLdindI2()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_I2));
		}

		// Token: 0x06002A7E RID: 10878 RVA: 0x0008EDE5 File Offset: 0x0008CFE5
		public ILCursor EmitLdindI4()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_I4));
		}

		// Token: 0x06002A7F RID: 10879 RVA: 0x0008EDFD File Offset: 0x0008CFFD
		public ILCursor EmitLdindI8()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_I8));
		}

		// Token: 0x06002A80 RID: 10880 RVA: 0x0008EE15 File Offset: 0x0008D015
		public ILCursor EmitLdindR4()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_R4));
		}

		// Token: 0x06002A81 RID: 10881 RVA: 0x0008EE2D File Offset: 0x0008D02D
		public ILCursor EmitLdindR8()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_R8));
		}

		// Token: 0x06002A82 RID: 10882 RVA: 0x0008EE45 File Offset: 0x0008D045
		public ILCursor EmitLdindRef()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_Ref));
		}

		// Token: 0x06002A83 RID: 10883 RVA: 0x0008EE5D File Offset: 0x0008D05D
		public ILCursor EmitLdindU1()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_U1));
		}

		// Token: 0x06002A84 RID: 10884 RVA: 0x0008EE75 File Offset: 0x0008D075
		public ILCursor EmitLdindU2()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_U2));
		}

		// Token: 0x06002A85 RID: 10885 RVA: 0x0008EE8D File Offset: 0x0008D08D
		public ILCursor EmitLdindU4()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldind_U4));
		}

		// Token: 0x06002A86 RID: 10886 RVA: 0x0008EEA5 File Offset: 0x0008D0A5
		public ILCursor EmitLdlen()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldlen));
		}

		// Token: 0x06002A87 RID: 10887 RVA: 0x0008EEBD File Offset: 0x0008D0BD
		public ILCursor EmitLdloc0()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloc_0));
		}

		// Token: 0x06002A88 RID: 10888 RVA: 0x0008EED5 File Offset: 0x0008D0D5
		public ILCursor EmitLdloc1()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloc_1));
		}

		// Token: 0x06002A89 RID: 10889 RVA: 0x0008EEED File Offset: 0x0008D0ED
		public ILCursor EmitLdloc2()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloc_2));
		}

		// Token: 0x06002A8A RID: 10890 RVA: 0x0008EF05 File Offset: 0x0008D105
		public ILCursor EmitLdloc3()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloc_3));
		}

		// Token: 0x06002A8B RID: 10891 RVA: 0x0008EF1D File Offset: 0x0008D11D
		public ILCursor EmitLdloc(int operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloc, operand));
		}

		// Token: 0x06002A8C RID: 10892 RVA: 0x0008EF1D File Offset: 0x0008D11D
		public ILCursor EmitLdloc(uint operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloc, (int)operand));
		}

		// Token: 0x06002A8D RID: 10893 RVA: 0x0008EF36 File Offset: 0x0008D136
		public ILCursor EmitLdloc(VariableReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloc, operand));
		}

		// Token: 0x06002A8E RID: 10894 RVA: 0x0008EF4F File Offset: 0x0008D14F
		public ILCursor EmitLdloca(int operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloca, operand));
		}

		// Token: 0x06002A8F RID: 10895 RVA: 0x0008EF4F File Offset: 0x0008D14F
		public ILCursor EmitLdloca(uint operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloca, (int)operand));
		}

		// Token: 0x06002A90 RID: 10896 RVA: 0x0008EF68 File Offset: 0x0008D168
		public ILCursor EmitLdloca(VariableReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldloca, operand));
		}

		// Token: 0x06002A91 RID: 10897 RVA: 0x0008EF81 File Offset: 0x0008D181
		public ILCursor EmitLdnull()
		{
			return this._Insert(this.IL.Create(OpCodes.Ldnull));
		}

		// Token: 0x06002A92 RID: 10898 RVA: 0x0008EF99 File Offset: 0x0008D199
		public ILCursor EmitLdobj(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldobj, operand));
		}

		// Token: 0x06002A93 RID: 10899 RVA: 0x0008EFB2 File Offset: 0x0008D1B2
		public ILCursor EmitLdobj(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldobj, this.Context.Import(operand)));
		}

		// Token: 0x06002A94 RID: 10900 RVA: 0x0008EFD6 File Offset: 0x0008D1D6
		public ILCursor EmitLdsfld(FieldReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldsfld, operand));
		}

		// Token: 0x06002A95 RID: 10901 RVA: 0x0008EFEF File Offset: 0x0008D1EF
		public ILCursor EmitLdsfld(FieldInfo operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldsfld, this.Context.Import(operand)));
		}

		// Token: 0x06002A96 RID: 10902 RVA: 0x0008F013 File Offset: 0x0008D213
		public ILCursor EmitLdsflda(FieldReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldsflda, operand));
		}

		// Token: 0x06002A97 RID: 10903 RVA: 0x0008F02C File Offset: 0x0008D22C
		public ILCursor EmitLdsflda(FieldInfo operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldsflda, this.Context.Import(operand)));
		}

		// Token: 0x06002A98 RID: 10904 RVA: 0x0008F050 File Offset: 0x0008D250
		public ILCursor EmitLdstr(string operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldstr, operand));
		}

		// Token: 0x06002A99 RID: 10905 RVA: 0x0008F069 File Offset: 0x0008D269
		public ILCursor EmitLdtoken(IMetadataTokenProvider operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldtoken, operand));
		}

		// Token: 0x06002A9A RID: 10906 RVA: 0x0008F082 File Offset: 0x0008D282
		public ILCursor EmitLdtoken(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldtoken, this.Context.Import(operand)));
		}

		// Token: 0x06002A9B RID: 10907 RVA: 0x0008F0A6 File Offset: 0x0008D2A6
		public ILCursor EmitLdtoken(FieldInfo operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldtoken, this.Context.Import(operand)));
		}

		// Token: 0x06002A9C RID: 10908 RVA: 0x0008F0CA File Offset: 0x0008D2CA
		public ILCursor EmitLdtoken(MethodBase operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldtoken, this.Context.Import(operand)));
		}

		// Token: 0x06002A9D RID: 10909 RVA: 0x0008F0EE File Offset: 0x0008D2EE
		public ILCursor EmitLdvirtftn(MethodReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldvirtftn, operand));
		}

		// Token: 0x06002A9E RID: 10910 RVA: 0x0008F107 File Offset: 0x0008D307
		public ILCursor EmitLdvirtftn(MethodBase operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Ldvirtftn, this.Context.Import(operand)));
		}

		// Token: 0x06002A9F RID: 10911 RVA: 0x0008F12B File Offset: 0x0008D32B
		public ILCursor EmitLeave(ILLabel operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Leave, operand));
		}

		// Token: 0x06002AA0 RID: 10912 RVA: 0x0008F144 File Offset: 0x0008D344
		public ILCursor EmitLeave(Instruction operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Leave, this.MarkLabel(operand)));
		}

		// Token: 0x06002AA1 RID: 10913 RVA: 0x0008F163 File Offset: 0x0008D363
		public ILCursor EmitLocalloc()
		{
			return this._Insert(this.IL.Create(OpCodes.Localloc));
		}

		// Token: 0x06002AA2 RID: 10914 RVA: 0x0008F17B File Offset: 0x0008D37B
		public ILCursor EmitMkrefany(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Mkrefany, operand));
		}

		// Token: 0x06002AA3 RID: 10915 RVA: 0x0008F194 File Offset: 0x0008D394
		public ILCursor EmitMkrefany(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Mkrefany, this.Context.Import(operand)));
		}

		// Token: 0x06002AA4 RID: 10916 RVA: 0x0008F1B8 File Offset: 0x0008D3B8
		public ILCursor EmitMul()
		{
			return this._Insert(this.IL.Create(OpCodes.Mul));
		}

		// Token: 0x06002AA5 RID: 10917 RVA: 0x0008F1D0 File Offset: 0x0008D3D0
		public ILCursor EmitMulOvf()
		{
			return this._Insert(this.IL.Create(OpCodes.Mul_Ovf));
		}

		// Token: 0x06002AA6 RID: 10918 RVA: 0x0008F1E8 File Offset: 0x0008D3E8
		public ILCursor EmitMulOvfUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Mul_Ovf_Un));
		}

		// Token: 0x06002AA7 RID: 10919 RVA: 0x0008F200 File Offset: 0x0008D400
		public ILCursor EmitNeg()
		{
			return this._Insert(this.IL.Create(OpCodes.Neg));
		}

		// Token: 0x06002AA8 RID: 10920 RVA: 0x0008F218 File Offset: 0x0008D418
		public ILCursor EmitNewarr(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Newarr, operand));
		}

		// Token: 0x06002AA9 RID: 10921 RVA: 0x0008F231 File Offset: 0x0008D431
		public ILCursor EmitNewarr(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Newarr, this.Context.Import(operand)));
		}

		// Token: 0x06002AAA RID: 10922 RVA: 0x0008F255 File Offset: 0x0008D455
		public ILCursor EmitNewobj(MethodReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Newobj, operand));
		}

		// Token: 0x06002AAB RID: 10923 RVA: 0x0008F26E File Offset: 0x0008D46E
		public ILCursor EmitNewobj(MethodBase operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Newobj, this.Context.Import(operand)));
		}

		// Token: 0x06002AAC RID: 10924 RVA: 0x0008F292 File Offset: 0x0008D492
		public ILCursor EmitNop()
		{
			return this._Insert(this.IL.Create(OpCodes.Nop));
		}

		// Token: 0x06002AAD RID: 10925 RVA: 0x0008F2AA File Offset: 0x0008D4AA
		public ILCursor EmitNot()
		{
			return this._Insert(this.IL.Create(OpCodes.Not));
		}

		// Token: 0x06002AAE RID: 10926 RVA: 0x0008F2C2 File Offset: 0x0008D4C2
		public ILCursor EmitOr()
		{
			return this._Insert(this.IL.Create(OpCodes.Or));
		}

		// Token: 0x06002AAF RID: 10927 RVA: 0x0008F2DA File Offset: 0x0008D4DA
		public ILCursor EmitPop()
		{
			return this._Insert(this.IL.Create(OpCodes.Pop));
		}

		// Token: 0x06002AB0 RID: 10928 RVA: 0x0008F2F2 File Offset: 0x0008D4F2
		public ILCursor EmitReadonly()
		{
			return this._Insert(this.IL.Create(OpCodes.Readonly));
		}

		// Token: 0x06002AB1 RID: 10929 RVA: 0x0008F30A File Offset: 0x0008D50A
		public ILCursor EmitRefanytype()
		{
			return this._Insert(this.IL.Create(OpCodes.Refanytype));
		}

		// Token: 0x06002AB2 RID: 10930 RVA: 0x0008F322 File Offset: 0x0008D522
		public ILCursor EmitRefanyval(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Refanyval, operand));
		}

		// Token: 0x06002AB3 RID: 10931 RVA: 0x0008F33B File Offset: 0x0008D53B
		public ILCursor EmitRefanyval(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Refanyval, this.Context.Import(operand)));
		}

		// Token: 0x06002AB4 RID: 10932 RVA: 0x0008F35F File Offset: 0x0008D55F
		public ILCursor EmitRem()
		{
			return this._Insert(this.IL.Create(OpCodes.Rem));
		}

		// Token: 0x06002AB5 RID: 10933 RVA: 0x0008F377 File Offset: 0x0008D577
		public ILCursor EmitRemUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Rem_Un));
		}

		// Token: 0x06002AB6 RID: 10934 RVA: 0x0008F38F File Offset: 0x0008D58F
		public ILCursor EmitRet()
		{
			return this._Insert(this.IL.Create(OpCodes.Ret));
		}

		// Token: 0x06002AB7 RID: 10935 RVA: 0x0008F3A7 File Offset: 0x0008D5A7
		public ILCursor EmitRethrow()
		{
			return this._Insert(this.IL.Create(OpCodes.Rethrow));
		}

		// Token: 0x06002AB8 RID: 10936 RVA: 0x0008F3BF File Offset: 0x0008D5BF
		public ILCursor EmitShl()
		{
			return this._Insert(this.IL.Create(OpCodes.Shl));
		}

		// Token: 0x06002AB9 RID: 10937 RVA: 0x0008F3D7 File Offset: 0x0008D5D7
		public ILCursor EmitShr()
		{
			return this._Insert(this.IL.Create(OpCodes.Shr));
		}

		// Token: 0x06002ABA RID: 10938 RVA: 0x0008F3EF File Offset: 0x0008D5EF
		public ILCursor EmitShrUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Shr_Un));
		}

		// Token: 0x06002ABB RID: 10939 RVA: 0x0008F407 File Offset: 0x0008D607
		public ILCursor EmitSizeof(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Sizeof, operand));
		}

		// Token: 0x06002ABC RID: 10940 RVA: 0x0008F420 File Offset: 0x0008D620
		public ILCursor EmitSizeof(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Sizeof, this.Context.Import(operand)));
		}

		// Token: 0x06002ABD RID: 10941 RVA: 0x0008F444 File Offset: 0x0008D644
		public ILCursor EmitStarg(int operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Starg, operand));
		}

		// Token: 0x06002ABE RID: 10942 RVA: 0x0008F444 File Offset: 0x0008D644
		public ILCursor EmitStarg(uint operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Starg, (int)operand));
		}

		// Token: 0x06002ABF RID: 10943 RVA: 0x0008F45D File Offset: 0x0008D65D
		public ILCursor EmitStarg(ParameterReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Starg, operand));
		}

		// Token: 0x06002AC0 RID: 10944 RVA: 0x0008F476 File Offset: 0x0008D676
		public ILCursor EmitStelemAny(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_Any, operand));
		}

		// Token: 0x06002AC1 RID: 10945 RVA: 0x0008F48F File Offset: 0x0008D68F
		public ILCursor EmitStelemAny(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_Any, this.Context.Import(operand)));
		}

		// Token: 0x06002AC2 RID: 10946 RVA: 0x0008F4B3 File Offset: 0x0008D6B3
		public ILCursor EmitStelemI()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_I));
		}

		// Token: 0x06002AC3 RID: 10947 RVA: 0x0008F4CB File Offset: 0x0008D6CB
		public ILCursor EmitStelemI1()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_I1));
		}

		// Token: 0x06002AC4 RID: 10948 RVA: 0x0008F4E3 File Offset: 0x0008D6E3
		public ILCursor EmitStelemI2()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_I2));
		}

		// Token: 0x06002AC5 RID: 10949 RVA: 0x0008F4FB File Offset: 0x0008D6FB
		public ILCursor EmitStelemI4()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_I4));
		}

		// Token: 0x06002AC6 RID: 10950 RVA: 0x0008F513 File Offset: 0x0008D713
		public ILCursor EmitStelemI8()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_I8));
		}

		// Token: 0x06002AC7 RID: 10951 RVA: 0x0008F52B File Offset: 0x0008D72B
		public ILCursor EmitStelemR4()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_R4));
		}

		// Token: 0x06002AC8 RID: 10952 RVA: 0x0008F543 File Offset: 0x0008D743
		public ILCursor EmitStelemR8()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_R8));
		}

		// Token: 0x06002AC9 RID: 10953 RVA: 0x0008F55B File Offset: 0x0008D75B
		public ILCursor EmitStelemRef()
		{
			return this._Insert(this.IL.Create(OpCodes.Stelem_Ref));
		}

		// Token: 0x06002ACA RID: 10954 RVA: 0x0008F573 File Offset: 0x0008D773
		public ILCursor EmitStfld(FieldReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stfld, operand));
		}

		// Token: 0x06002ACB RID: 10955 RVA: 0x0008F58C File Offset: 0x0008D78C
		public ILCursor EmitStfld(FieldInfo operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stfld, this.Context.Import(operand)));
		}

		// Token: 0x06002ACC RID: 10956 RVA: 0x0008F5B0 File Offset: 0x0008D7B0
		public ILCursor EmitStindI()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_I));
		}

		// Token: 0x06002ACD RID: 10957 RVA: 0x0008F5C8 File Offset: 0x0008D7C8
		public ILCursor EmitStindI1()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_I1));
		}

		// Token: 0x06002ACE RID: 10958 RVA: 0x0008F5E0 File Offset: 0x0008D7E0
		public ILCursor EmitStindI2()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_I2));
		}

		// Token: 0x06002ACF RID: 10959 RVA: 0x0008F5F8 File Offset: 0x0008D7F8
		public ILCursor EmitStindI4()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_I4));
		}

		// Token: 0x06002AD0 RID: 10960 RVA: 0x0008F610 File Offset: 0x0008D810
		public ILCursor EmitStindI8()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_I8));
		}

		// Token: 0x06002AD1 RID: 10961 RVA: 0x0008F628 File Offset: 0x0008D828
		public ILCursor EmitStindR4()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_R4));
		}

		// Token: 0x06002AD2 RID: 10962 RVA: 0x0008F640 File Offset: 0x0008D840
		public ILCursor EmitStindR8()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_R8));
		}

		// Token: 0x06002AD3 RID: 10963 RVA: 0x0008F658 File Offset: 0x0008D858
		public ILCursor EmitStindRef()
		{
			return this._Insert(this.IL.Create(OpCodes.Stind_Ref));
		}

		// Token: 0x06002AD4 RID: 10964 RVA: 0x0008F670 File Offset: 0x0008D870
		public ILCursor EmitStloc0()
		{
			return this._Insert(this.IL.Create(OpCodes.Stloc_0));
		}

		// Token: 0x06002AD5 RID: 10965 RVA: 0x0008F688 File Offset: 0x0008D888
		public ILCursor EmitStloc1()
		{
			return this._Insert(this.IL.Create(OpCodes.Stloc_1));
		}

		// Token: 0x06002AD6 RID: 10966 RVA: 0x0008F6A0 File Offset: 0x0008D8A0
		public ILCursor EmitStloc2()
		{
			return this._Insert(this.IL.Create(OpCodes.Stloc_2));
		}

		// Token: 0x06002AD7 RID: 10967 RVA: 0x0008F6B8 File Offset: 0x0008D8B8
		public ILCursor EmitStloc3()
		{
			return this._Insert(this.IL.Create(OpCodes.Stloc_3));
		}

		// Token: 0x06002AD8 RID: 10968 RVA: 0x0008F6D0 File Offset: 0x0008D8D0
		public ILCursor EmitStloc(int operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stloc, operand));
		}

		// Token: 0x06002AD9 RID: 10969 RVA: 0x0008F6D0 File Offset: 0x0008D8D0
		public ILCursor EmitStloc(uint operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stloc, (int)operand));
		}

		// Token: 0x06002ADA RID: 10970 RVA: 0x0008F6E9 File Offset: 0x0008D8E9
		public ILCursor EmitStloc(VariableReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stloc, operand));
		}

		// Token: 0x06002ADB RID: 10971 RVA: 0x0008F702 File Offset: 0x0008D902
		public ILCursor EmitStobj(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stobj, operand));
		}

		// Token: 0x06002ADC RID: 10972 RVA: 0x0008F71B File Offset: 0x0008D91B
		public ILCursor EmitStobj(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stobj, this.Context.Import(operand)));
		}

		// Token: 0x06002ADD RID: 10973 RVA: 0x0008F73F File Offset: 0x0008D93F
		public ILCursor EmitStsfld(FieldReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stsfld, operand));
		}

		// Token: 0x06002ADE RID: 10974 RVA: 0x0008F758 File Offset: 0x0008D958
		public ILCursor EmitStsfld(FieldInfo operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Stsfld, this.Context.Import(operand)));
		}

		// Token: 0x06002ADF RID: 10975 RVA: 0x0008F77C File Offset: 0x0008D97C
		public ILCursor EmitSub()
		{
			return this._Insert(this.IL.Create(OpCodes.Sub));
		}

		// Token: 0x06002AE0 RID: 10976 RVA: 0x0008F794 File Offset: 0x0008D994
		public ILCursor EmitSubOvf()
		{
			return this._Insert(this.IL.Create(OpCodes.Sub_Ovf));
		}

		// Token: 0x06002AE1 RID: 10977 RVA: 0x0008F7AC File Offset: 0x0008D9AC
		public ILCursor EmitSubOvfUn()
		{
			return this._Insert(this.IL.Create(OpCodes.Sub_Ovf_Un));
		}

		// Token: 0x06002AE2 RID: 10978 RVA: 0x0008F7C4 File Offset: 0x0008D9C4
		public ILCursor EmitSwitch(ILLabel[] operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Switch, operand));
		}

		// Token: 0x06002AE3 RID: 10979 RVA: 0x0008F7DD File Offset: 0x0008D9DD
		public ILCursor EmitSwitch(Instruction[] operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Switch, operand.Select(new Func<Instruction, ILLabel>(this.MarkLabel)).ToArray<ILLabel>()));
		}

		// Token: 0x06002AE4 RID: 10980 RVA: 0x0008F80C File Offset: 0x0008DA0C
		public ILCursor EmitTail()
		{
			return this._Insert(this.IL.Create(OpCodes.Tail));
		}

		// Token: 0x06002AE5 RID: 10981 RVA: 0x0008F824 File Offset: 0x0008DA24
		public ILCursor EmitThrow()
		{
			return this._Insert(this.IL.Create(OpCodes.Throw));
		}

		// Token: 0x06002AE6 RID: 10982 RVA: 0x0008F83C File Offset: 0x0008DA3C
		public ILCursor EmitUnaligned(byte operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Unaligned, operand));
		}

		// Token: 0x06002AE7 RID: 10983 RVA: 0x0008F855 File Offset: 0x0008DA55
		public ILCursor EmitUnbox(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Unbox, operand));
		}

		// Token: 0x06002AE8 RID: 10984 RVA: 0x0008F86E File Offset: 0x0008DA6E
		public ILCursor EmitUnbox(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Unbox, this.Context.Import(operand)));
		}

		// Token: 0x06002AE9 RID: 10985 RVA: 0x0008F892 File Offset: 0x0008DA92
		public ILCursor EmitUnboxAny(TypeReference operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Unbox_Any, operand));
		}

		// Token: 0x06002AEA RID: 10986 RVA: 0x0008F8AB File Offset: 0x0008DAAB
		public ILCursor EmitUnboxAny(Type operand)
		{
			return this._Insert(this.IL.Create(OpCodes.Unbox_Any, this.Context.Import(operand)));
		}

		// Token: 0x06002AEB RID: 10987 RVA: 0x0008F8CF File Offset: 0x0008DACF
		public ILCursor EmitVolatile()
		{
			return this._Insert(this.IL.Create(OpCodes.Volatile));
		}

		// Token: 0x06002AEC RID: 10988 RVA: 0x0008F8E7 File Offset: 0x0008DAE7
		public ILCursor EmitXor()
		{
			return this._Insert(this.IL.Create(OpCodes.Xor));
		}

		// Token: 0x04003A45 RID: 14917
		[Nullable(2)]
		private Instruction _next;

		// Token: 0x04003A46 RID: 14918
		[Nullable(new byte[] { 2, 1 })]
		private ILLabel[] _afterLabels;

		// Token: 0x04003A47 RID: 14919
		private bool _afterHandlerStarts;

		// Token: 0x04003A48 RID: 14920
		private bool _afterHandlerEnds;

		// Token: 0x04003A49 RID: 14921
		private SearchTarget _searchTarget;
	}
}
