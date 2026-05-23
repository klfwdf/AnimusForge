using System;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002F3 RID: 755
	internal class InstructionCollection : Collection<Instruction>
	{
		// Token: 0x060013BF RID: 5055 RVA: 0x0003E2F8 File Offset: 0x0003C4F8
		internal InstructionCollection(MethodDefinition method)
		{
			this.method = method;
		}

		// Token: 0x060013C0 RID: 5056 RVA: 0x0003E307 File Offset: 0x0003C507
		internal InstructionCollection(MethodDefinition method, int capacity)
			: base(capacity)
		{
			this.method = method;
		}

		// Token: 0x060013C1 RID: 5057 RVA: 0x0003E318 File Offset: 0x0003C518
		protected override void OnAdd(Instruction item, int index)
		{
			if (index == 0)
			{
				return;
			}
			Instruction previous = this.items[index - 1];
			previous.next = item;
			item.previous = previous;
		}

		// Token: 0x060013C2 RID: 5058 RVA: 0x0003E344 File Offset: 0x0003C544
		protected override void OnInsert(Instruction item, int index)
		{
			if (this.size != 0)
			{
				Instruction current = this.items[index];
				if (current == null)
				{
					Instruction last = this.items[index - 1];
					last.next = item;
					item.previous = last;
					return;
				}
				int offset = current.Offset;
				Instruction previous = current.previous;
				if (previous != null)
				{
					previous.next = item;
					item.previous = previous;
				}
				current.previous = item;
				item.next = current;
			}
			this.UpdateDebugInformation(null, null);
		}

		// Token: 0x060013C3 RID: 5059 RVA: 0x0003E3B4 File Offset: 0x0003C5B4
		protected override void OnSet(Instruction item, int index)
		{
			Instruction current = this.items[index];
			item.previous = current.previous;
			item.next = current.next;
			current.previous = null;
			current.next = null;
			this.UpdateDebugInformation(item, current);
		}

		// Token: 0x060013C4 RID: 5060 RVA: 0x0003E3F8 File Offset: 0x0003C5F8
		protected override void OnRemove(Instruction item, int index)
		{
			Instruction previous = item.previous;
			if (previous != null)
			{
				previous.next = item.next;
			}
			Instruction next = item.next;
			if (next != null)
			{
				next.previous = item.previous;
			}
			this.RemoveSequencePoint(item);
			this.UpdateDebugInformation(item, next ?? previous);
			item.previous = null;
			item.next = null;
		}

		// Token: 0x060013C5 RID: 5061 RVA: 0x0003E454 File Offset: 0x0003C654
		private void RemoveSequencePoint(Instruction instruction)
		{
			MethodDebugInformation debug_info = this.method.debug_info;
			if (debug_info == null || !debug_info.HasSequencePoints)
			{
				return;
			}
			Collection<SequencePoint> sequence_points = debug_info.sequence_points;
			for (int i = 0; i < sequence_points.Count; i++)
			{
				if (sequence_points[i].Offset == instruction.offset)
				{
					sequence_points.RemoveAt(i);
					return;
				}
			}
		}

		// Token: 0x060013C6 RID: 5062 RVA: 0x0003E4B0 File Offset: 0x0003C6B0
		private void UpdateDebugInformation(Instruction removedInstruction, Instruction existingInstruction)
		{
			InstructionCollection.InstructionOffsetResolver resolver = new InstructionCollection.InstructionOffsetResolver(this.items, removedInstruction, existingInstruction);
			if (this.method.debug_info != null)
			{
				this.UpdateLocalScope(this.method.debug_info.Scope, ref resolver);
			}
			Collection<CustomDebugInformation> collection;
			if ((collection = this.method.custom_infos) == null)
			{
				MethodDebugInformation debug_info = this.method.debug_info;
				collection = ((debug_info != null) ? debug_info.custom_infos : null);
			}
			Collection<CustomDebugInformation> custom_debug_infos = collection;
			if (custom_debug_infos != null)
			{
				foreach (CustomDebugInformation custom_debug_info in custom_debug_infos)
				{
					StateMachineScopeDebugInformation state_machine_scope = custom_debug_info as StateMachineScopeDebugInformation;
					if (state_machine_scope == null)
					{
						AsyncMethodBodyDebugInformation async_method_body = custom_debug_info as AsyncMethodBodyDebugInformation;
						if (async_method_body != null)
						{
							this.UpdateAsyncMethodBody(async_method_body, ref resolver);
						}
					}
					else
					{
						this.UpdateStateMachineScope(state_machine_scope, ref resolver);
					}
				}
			}
		}

		// Token: 0x060013C7 RID: 5063 RVA: 0x0003E588 File Offset: 0x0003C788
		private void UpdateLocalScope(ScopeDebugInformation scope, ref InstructionCollection.InstructionOffsetResolver resolver)
		{
			if (scope == null)
			{
				return;
			}
			scope.Start = resolver.Resolve(scope.Start);
			if (scope.HasScopes)
			{
				foreach (ScopeDebugInformation subScope in scope.Scopes)
				{
					this.UpdateLocalScope(subScope, ref resolver);
				}
			}
			scope.End = resolver.Resolve(scope.End);
		}

		// Token: 0x060013C8 RID: 5064 RVA: 0x0003E60C File Offset: 0x0003C80C
		private void UpdateStateMachineScope(StateMachineScopeDebugInformation debugInfo, ref InstructionCollection.InstructionOffsetResolver resolver)
		{
			resolver.Restart();
			foreach (StateMachineScope scope in debugInfo.Scopes)
			{
				scope.Start = resolver.Resolve(scope.Start);
				scope.End = resolver.Resolve(scope.End);
			}
		}

		// Token: 0x060013C9 RID: 5065 RVA: 0x0003E684 File Offset: 0x0003C884
		private void UpdateAsyncMethodBody(AsyncMethodBodyDebugInformation debugInfo, ref InstructionCollection.InstructionOffsetResolver resolver)
		{
			if (!debugInfo.CatchHandler.IsResolved)
			{
				resolver.Restart();
				debugInfo.CatchHandler = resolver.Resolve(debugInfo.CatchHandler);
			}
			resolver.Restart();
			for (int i = 0; i < debugInfo.Yields.Count; i++)
			{
				debugInfo.Yields[i] = resolver.Resolve(debugInfo.Yields[i]);
			}
			resolver.Restart();
			for (int j = 0; j < debugInfo.Resumes.Count; j++)
			{
				debugInfo.Resumes[j] = resolver.Resolve(debugInfo.Resumes[j]);
			}
		}

		// Token: 0x040008BA RID: 2234
		private readonly MethodDefinition method;

		// Token: 0x020002F4 RID: 756
		private struct InstructionOffsetResolver
		{
			// Token: 0x170004FE RID: 1278
			// (get) Token: 0x060013CA RID: 5066 RVA: 0x0003E72D File Offset: 0x0003C92D
			public int LastOffset
			{
				get
				{
					return this.cache_offset;
				}
			}

			// Token: 0x060013CB RID: 5067 RVA: 0x0003E735 File Offset: 0x0003C935
			public InstructionOffsetResolver(Instruction[] instructions, Instruction removedInstruction, Instruction existingInstruction)
			{
				this.items = instructions;
				this.removed_instruction = removedInstruction;
				this.existing_instruction = existingInstruction;
				this.cache_offset = 0;
				this.cache_index = 0;
				this.cache_instruction = this.items[0];
			}

			// Token: 0x060013CC RID: 5068 RVA: 0x0003E768 File Offset: 0x0003C968
			public void Restart()
			{
				this.cache_offset = 0;
				this.cache_index = 0;
				this.cache_instruction = this.items[0];
			}

			// Token: 0x060013CD RID: 5069 RVA: 0x0003E788 File Offset: 0x0003C988
			public InstructionOffset Resolve(InstructionOffset inputOffset)
			{
				InstructionOffset result = this.ResolveInstructionOffset(inputOffset);
				if (!result.IsEndOfMethod && result.ResolvedInstruction == this.removed_instruction)
				{
					result = new InstructionOffset(this.existing_instruction);
				}
				return result;
			}

			// Token: 0x060013CE RID: 5070 RVA: 0x0003E7C4 File Offset: 0x0003C9C4
			private InstructionOffset ResolveInstructionOffset(InstructionOffset inputOffset)
			{
				if (inputOffset.IsResolved)
				{
					return inputOffset;
				}
				int offset = inputOffset.Offset;
				if (this.cache_offset == offset)
				{
					return new InstructionOffset(this.cache_instruction);
				}
				if (this.cache_offset > offset)
				{
					int size = 0;
					for (int i = 0; i < this.items.Length; i++)
					{
						if (this.items[i] == null)
						{
							return new InstructionOffset((i == 0) ? this.items[0] : this.items[i - 1]);
						}
						if (size == offset)
						{
							return new InstructionOffset(this.items[i]);
						}
						if (size > offset)
						{
							return new InstructionOffset((i == 0) ? this.items[0] : this.items[i - 1]);
						}
						size += this.items[i].GetSize();
					}
					return default(InstructionOffset);
				}
				int size2 = this.cache_offset;
				for (int j = this.cache_index; j < this.items.Length; j++)
				{
					this.cache_index = j;
					this.cache_offset = size2;
					Instruction item = this.items[j];
					if (item == null)
					{
						return new InstructionOffset((j == 0) ? this.items[0] : this.items[j - 1]);
					}
					this.cache_instruction = item;
					if (this.cache_offset == offset)
					{
						return new InstructionOffset(this.cache_instruction);
					}
					if (this.cache_offset > offset)
					{
						return new InstructionOffset((j == 0) ? this.items[0] : this.items[j - 1]);
					}
					size2 += item.GetSize();
				}
				return default(InstructionOffset);
			}

			// Token: 0x040008BB RID: 2235
			private readonly Instruction[] items;

			// Token: 0x040008BC RID: 2236
			private readonly Instruction removed_instruction;

			// Token: 0x040008BD RID: 2237
			private readonly Instruction existing_instruction;

			// Token: 0x040008BE RID: 2238
			private int cache_offset;

			// Token: 0x040008BF RID: 2239
			private int cache_index;

			// Token: 0x040008C0 RID: 2240
			private Instruction cache_instruction;
		}
	}
}
