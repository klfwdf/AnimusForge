using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200031D RID: 797
	internal sealed class MethodDebugInformation : DebugInformation
	{
		// Token: 0x1700054D RID: 1357
		// (get) Token: 0x060014A8 RID: 5288 RVA: 0x00041413 File Offset: 0x0003F613
		public MethodDefinition Method
		{
			get
			{
				return this.method;
			}
		}

		// Token: 0x1700054E RID: 1358
		// (get) Token: 0x060014A9 RID: 5289 RVA: 0x0004141B File Offset: 0x0003F61B
		public bool HasSequencePoints
		{
			get
			{
				return !this.sequence_points.IsNullOrEmpty<SequencePoint>();
			}
		}

		// Token: 0x1700054F RID: 1359
		// (get) Token: 0x060014AA RID: 5290 RVA: 0x0004142B File Offset: 0x0003F62B
		public Collection<SequencePoint> SequencePoints
		{
			get
			{
				if (this.sequence_points == null)
				{
					Interlocked.CompareExchange<Collection<SequencePoint>>(ref this.sequence_points, new Collection<SequencePoint>(), null);
				}
				return this.sequence_points;
			}
		}

		// Token: 0x17000550 RID: 1360
		// (get) Token: 0x060014AB RID: 5291 RVA: 0x0004144D File Offset: 0x0003F64D
		// (set) Token: 0x060014AC RID: 5292 RVA: 0x00041455 File Offset: 0x0003F655
		public ScopeDebugInformation Scope
		{
			get
			{
				return this.scope;
			}
			set
			{
				this.scope = value;
			}
		}

		// Token: 0x17000551 RID: 1361
		// (get) Token: 0x060014AD RID: 5293 RVA: 0x0004145E File Offset: 0x0003F65E
		// (set) Token: 0x060014AE RID: 5294 RVA: 0x00041466 File Offset: 0x0003F666
		public MethodDefinition StateMachineKickOffMethod
		{
			get
			{
				return this.kickoff_method;
			}
			set
			{
				this.kickoff_method = value;
			}
		}

		// Token: 0x060014AF RID: 5295 RVA: 0x00041470 File Offset: 0x0003F670
		internal MethodDebugInformation(MethodDefinition method)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			this.method = method;
			this.token = new MetadataToken(TokenType.MethodDebugInformation, method.MetadataToken.RID);
		}

		// Token: 0x060014B0 RID: 5296 RVA: 0x000414B8 File Offset: 0x0003F6B8
		public SequencePoint GetSequencePoint(Instruction instruction)
		{
			if (!this.HasSequencePoints)
			{
				return null;
			}
			for (int i = 0; i < this.sequence_points.Count; i++)
			{
				if (this.sequence_points[i].Offset == instruction.Offset)
				{
					return this.sequence_points[i];
				}
			}
			return null;
		}

		// Token: 0x060014B1 RID: 5297 RVA: 0x0004150C File Offset: 0x0003F70C
		public IDictionary<Instruction, SequencePoint> GetSequencePointMapping()
		{
			Dictionary<Instruction, SequencePoint> instruction_mapping = new Dictionary<Instruction, SequencePoint>();
			if (!this.HasSequencePoints || !this.method.HasBody)
			{
				return instruction_mapping;
			}
			Dictionary<int, SequencePoint> offset_mapping = new Dictionary<int, SequencePoint>(this.sequence_points.Count);
			for (int i = 0; i < this.sequence_points.Count; i++)
			{
				if (!offset_mapping.ContainsKey(this.sequence_points[i].Offset))
				{
					offset_mapping.Add(this.sequence_points[i].Offset, this.sequence_points[i]);
				}
			}
			Collection<Instruction> instructions = this.method.Body.Instructions;
			for (int j = 0; j < instructions.Count; j++)
			{
				SequencePoint sequence_point;
				if (offset_mapping.TryGetValue(instructions[j].Offset, out sequence_point))
				{
					instruction_mapping.Add(instructions[j], sequence_point);
				}
			}
			return instruction_mapping;
		}

		// Token: 0x060014B2 RID: 5298 RVA: 0x000415E7 File Offset: 0x0003F7E7
		public IEnumerable<ScopeDebugInformation> GetScopes()
		{
			if (this.scope == null)
			{
				return Empty<ScopeDebugInformation>.Array;
			}
			return MethodDebugInformation.GetScopes(new ScopeDebugInformation[] { this.scope });
		}

		// Token: 0x060014B3 RID: 5299 RVA: 0x0004160B File Offset: 0x0003F80B
		private static IEnumerable<ScopeDebugInformation> GetScopes(IList<ScopeDebugInformation> scopes)
		{
			int num;
			for (int i = 0; i < scopes.Count; i = num + 1)
			{
				ScopeDebugInformation scope = scopes[i];
				yield return scope;
				if (scope.HasScopes)
				{
					foreach (ScopeDebugInformation sub_scope in MethodDebugInformation.GetScopes(scope.Scopes))
					{
						yield return sub_scope;
					}
					IEnumerator<ScopeDebugInformation> enumerator = null;
					scope = null;
				}
				num = i;
			}
			yield break;
			yield break;
		}

		// Token: 0x060014B4 RID: 5300 RVA: 0x0004161C File Offset: 0x0003F81C
		public bool TryGetName(VariableDefinition variable, out string name)
		{
			name = null;
			bool has_name = false;
			string unique_name = "";
			using (IEnumerator<ScopeDebugInformation> enumerator = this.GetScopes().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string slot_name;
					if (enumerator.Current.TryGetName(variable, out slot_name))
					{
						if (!has_name)
						{
							has_name = true;
							unique_name = slot_name;
						}
						else if (unique_name != slot_name)
						{
							return false;
						}
					}
				}
			}
			name = unique_name;
			return has_name;
		}

		// Token: 0x04000A59 RID: 2649
		internal MethodDefinition method;

		// Token: 0x04000A5A RID: 2650
		internal Collection<SequencePoint> sequence_points;

		// Token: 0x04000A5B RID: 2651
		internal ScopeDebugInformation scope;

		// Token: 0x04000A5C RID: 2652
		internal MethodDefinition kickoff_method;

		// Token: 0x04000A5D RID: 2653
		internal int code_size;

		// Token: 0x04000A5E RID: 2654
		internal MetadataToken local_var_token;
	}
}
