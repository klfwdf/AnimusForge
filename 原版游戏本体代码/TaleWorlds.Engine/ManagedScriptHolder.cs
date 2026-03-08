using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200005C RID: 92
	public sealed class ManagedScriptHolder : DotNetObject
	{
		// Token: 0x0600090D RID: 2317 RVA: 0x000080D9 File Offset: 0x000062D9
		[EngineCallback(null, false)]
		internal static ManagedScriptHolder CreateManagedScriptHolder()
		{
			return new ManagedScriptHolder();
		}

		// Token: 0x0600090E RID: 2318 RVA: 0x000080E0 File Offset: 0x000062E0
		public ManagedScriptHolder()
		{
			this.TickComponentsParallelAuxMTPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.TickComponentsParallelAuxMT);
			this.TickComponentsParallel2AuxMTPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.TickComponentsParallel2AuxMT);
			this.TickComponentsParallel3AuxMTPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.TickComponentsParallel3AuxMT);
			this.TickComponentsOccasionallyParallelAuxMTPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.TickComponentsOccasionallyParallelAuxMT);
			this.TickComponentsFixedParallelAuxMTPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.TickComponentsFixedParallelAuxMT);
		}

		// Token: 0x0600090F RID: 2319 RVA: 0x000081CF File Offset: 0x000063CF
		[EngineCallback(null, false)]
		public void SetScriptComponentHolder(ScriptComponentBehavior sc)
		{
			sc.SetOwnerManagedScriptHolder(this);
			this._toTickForEditor.AddToRec(sc);
			sc.SetScriptComponentToTick(sc.GetTickRequirement());
		}

		// Token: 0x06000910 RID: 2320 RVA: 0x000081F0 File Offset: 0x000063F0
		private ManagedScriptHolder.BehaviorTickRecord GetRecordFromEnum(ScriptComponentBehavior.TickRequirement tickRecEnum)
		{
			if (tickRecEnum <= ScriptComponentBehavior.TickRequirement.TickParallel2)
			{
				switch (tickRecEnum)
				{
				case ScriptComponentBehavior.TickRequirement.TickOccasionally:
					return this._toTickOccasionally;
				case ScriptComponentBehavior.TickRequirement.Tick:
					return this._toTick;
				case ScriptComponentBehavior.TickRequirement.TickOccasionally | ScriptComponentBehavior.TickRequirement.Tick:
					break;
				case ScriptComponentBehavior.TickRequirement.TickParallel:
					return this._toParallelTick;
				default:
					if (tickRecEnum == ScriptComponentBehavior.TickRequirement.TickParallel2)
					{
						return this._toParallelTick2;
					}
					break;
				}
			}
			else
			{
				if (tickRecEnum == ScriptComponentBehavior.TickRequirement.FixedTick)
				{
					return this._toFixedTick;
				}
				if (tickRecEnum == ScriptComponentBehavior.TickRequirement.FixedParallelTick)
				{
					return this._toFixedParallelTick;
				}
				if (tickRecEnum == ScriptComponentBehavior.TickRequirement.TickParallel3)
				{
					return this._toParallelTick3;
				}
			}
			return null;
		}

		// Token: 0x06000911 RID: 2321 RVA: 0x00008264 File Offset: 0x00006464
		public void UpdateTickRequirement(ScriptComponentBehavior sc, ScriptComponentBehavior.TickRequirement oldTickRequirement, ScriptComponentBehavior.TickRequirement newTickRequirement)
		{
			foreach (ScriptComponentBehavior.TickRequirement tickRequirement in ManagedScriptHolder.TickRequirementEnumValues)
			{
				if (newTickRequirement.HasAnyFlag(tickRequirement) != oldTickRequirement.HasAnyFlag(tickRequirement))
				{
					ManagedScriptHolder.BehaviorTickRecord recordFromEnum = this.GetRecordFromEnum(tickRequirement);
					if (oldTickRequirement.HasAnyFlag(tickRequirement))
					{
						recordFromEnum.RemoveFromRec(sc, true);
					}
					else
					{
						recordFromEnum.AddToRec(sc);
					}
				}
			}
		}

		// Token: 0x06000912 RID: 2322 RVA: 0x000082BC File Offset: 0x000064BC
		[EngineCallback(null, false)]
		public void RemoveScriptComponentFromAllTickLists(ScriptComponentBehavior sc)
		{
			object addRemoveLockObject = this.AddRemoveLockObject;
			lock (addRemoveLockObject)
			{
				sc.SetScriptComponentToTickMT(ScriptComponentBehavior.TickRequirement.None);
				this._toTickForEditor.RemoveFromRec(sc, true);
			}
		}

		// Token: 0x06000913 RID: 2323 RVA: 0x0000830C File Offset: 0x0000650C
		[EngineCallback(null, false)]
		internal int GetNumberOfScripts()
		{
			return this._toTick.ScriptComponents.Count;
		}

		// Token: 0x06000914 RID: 2324 RVA: 0x00008320 File Offset: 0x00006520
		private void TickComponentsParallelAuxMT(int startInclusive, int endExclusive, float dt)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this._toParallelTick.ScriptComponents[i].OnTickParallel(dt);
			}
		}

		// Token: 0x06000915 RID: 2325 RVA: 0x00008350 File Offset: 0x00006550
		private void TickComponentsParallel2AuxMT(int startInclusive, int endExclusive, float dt)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this._toParallelTick2.ScriptComponents[i].OnTickParallel2(dt);
			}
		}

		// Token: 0x06000916 RID: 2326 RVA: 0x00008380 File Offset: 0x00006580
		private void TickComponentsParallel3AuxMT(int startInclusive, int endExclusive, float dt)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this._toParallelTick3.ScriptComponents[i].OnTickParallel3(dt);
			}
		}

		// Token: 0x06000917 RID: 2327 RVA: 0x000083B0 File Offset: 0x000065B0
		private void TickComponentsOccasionallyParallelAuxMT(int startInclusive, int endExclusive, float dt)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this._toTickOccasionally.ScriptComponents[i].OnTickOccasionally(dt);
			}
		}

		// Token: 0x06000918 RID: 2328 RVA: 0x000083E0 File Offset: 0x000065E0
		private void TickComponentsFixedParallelAuxMT(int startInclusive, int endExclusive, float dt)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this._toFixedParallelTick.ScriptComponents[i].OnParallelFixedTick(dt);
			}
		}

		// Token: 0x06000919 RID: 2329 RVA: 0x00008410 File Offset: 0x00006610
		[EngineCallback(null, false)]
		internal void FixedTickComponents(float fixedDt)
		{
			this._toFixedParallelTick.TickRec();
			TWParallel.For(0, this._toFixedParallelTick.ScriptComponents.Count, fixedDt, this.TickComponentsFixedParallelAuxMTPredicate, 1);
			this._toFixedTick.TickRec();
			foreach (ScriptComponentBehavior scriptComponentBehavior in this._toFixedTick.ScriptComponents)
			{
				scriptComponentBehavior.OnFixedTick(fixedDt);
			}
		}

		// Token: 0x0600091A RID: 2330 RVA: 0x0000849C File Offset: 0x0000669C
		[EngineCallback(null, false)]
		internal void TickComponents(float dt)
		{
			this._toParallelTick.TickRec();
			TWParallel.For(0, this._toParallelTick.ScriptComponents.Count, dt, this.TickComponentsParallelAuxMTPredicate, 1);
			this._toParallelTick2.TickRec();
			TWParallel.For(0, this._toParallelTick2.ScriptComponents.Count, dt, this.TickComponentsParallel2AuxMTPredicate, 8);
			this._toParallelTick3.TickRec();
			TWParallel.For(0, this._toParallelTick3.ScriptComponents.Count, dt, this.TickComponentsParallel3AuxMTPredicate, 8);
			this._toTick.TickRec();
			foreach (ScriptComponentBehavior scriptComponentBehavior in this._toTick.ScriptComponents)
			{
				scriptComponentBehavior.OnTick(dt);
			}
			this._nextIndexToTickOccasionally = MathF.Max(0, this._nextIndexToTickOccasionally - this._toTickOccasionally.GetWillBeRemovedCount());
			this._toTickOccasionally.TickRec();
			int num = this._toTickOccasionally.ScriptComponents.Count / 10 + 1;
			int num2 = Math.Min(this._nextIndexToTickOccasionally + num, this._toTickOccasionally.ScriptComponents.Count);
			if (this._nextIndexToTickOccasionally < num2)
			{
				TWParallel.For(this._nextIndexToTickOccasionally, num2, dt, this.TickComponentsOccasionallyParallelAuxMTPredicate, 8);
				this._nextIndexToTickOccasionally = ((num2 >= this._toTickOccasionally.ScriptComponents.Count) ? 0 : num2);
				return;
			}
			this._nextIndexToTickOccasionally = 0;
		}

		// Token: 0x0600091B RID: 2331 RVA: 0x00008618 File Offset: 0x00006818
		[EngineCallback(null, false)]
		internal void TickComponentsEditor(float dt)
		{
			this._toTickForEditor.TickRec();
			for (int i = 0; i < this._toTickForEditor.ScriptComponents.Count; i++)
			{
				this._toTickForEditor.ScriptComponents[i].OnEditorTick(dt);
			}
		}

		// Token: 0x040000D8 RID: 216
		private static readonly ScriptComponentBehavior.TickRequirement[] TickRequirementEnumValues = (ScriptComponentBehavior.TickRequirement[])Enum.GetValues(typeof(ScriptComponentBehavior.TickRequirement));

		// Token: 0x040000D9 RID: 217
		public object AddRemoveLockObject = new object();

		// Token: 0x040000DA RID: 218
		private readonly ManagedScriptHolder.BehaviorTickRecord _toTick = new ManagedScriptHolder.BehaviorTickRecord(512);

		// Token: 0x040000DB RID: 219
		private readonly ManagedScriptHolder.BehaviorTickRecord _toParallelTick = new ManagedScriptHolder.BehaviorTickRecord(64);

		// Token: 0x040000DC RID: 220
		private readonly ManagedScriptHolder.BehaviorTickRecord _toParallelTick2 = new ManagedScriptHolder.BehaviorTickRecord(512);

		// Token: 0x040000DD RID: 221
		private readonly ManagedScriptHolder.BehaviorTickRecord _toParallelTick3 = new ManagedScriptHolder.BehaviorTickRecord(512);

		// Token: 0x040000DE RID: 222
		private readonly ManagedScriptHolder.BehaviorTickRecord _toTickOccasionally = new ManagedScriptHolder.BehaviorTickRecord(512);

		// Token: 0x040000DF RID: 223
		private readonly ManagedScriptHolder.BehaviorTickRecord _toTickForEditor = new ManagedScriptHolder.BehaviorTickRecord(512);

		// Token: 0x040000E0 RID: 224
		private readonly ManagedScriptHolder.BehaviorTickRecord _toFixedParallelTick = new ManagedScriptHolder.BehaviorTickRecord(64);

		// Token: 0x040000E1 RID: 225
		private readonly ManagedScriptHolder.BehaviorTickRecord _toFixedTick = new ManagedScriptHolder.BehaviorTickRecord(32);

		// Token: 0x040000E2 RID: 226
		private int _nextIndexToTickOccasionally;

		// Token: 0x040000E3 RID: 227
		private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsParallelAuxMTPredicate;

		// Token: 0x040000E4 RID: 228
		private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsParallel2AuxMTPredicate;

		// Token: 0x040000E5 RID: 229
		private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsParallel3AuxMTPredicate;

		// Token: 0x040000E6 RID: 230
		private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsFixedParallelAuxMTPredicate;

		// Token: 0x040000E7 RID: 231
		private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsOccasionallyParallelAuxMTPredicate;

		// Token: 0x020000C3 RID: 195
		private class BehaviorTickRecord
		{
			// Token: 0x170000D2 RID: 210
			// (get) Token: 0x06000FE1 RID: 4065 RVA: 0x00014257 File Offset: 0x00012457
			public List<ScriptComponentBehavior> ScriptComponents
			{
				get
				{
					return this._scriptComponents;
				}
			}

			// Token: 0x06000FE2 RID: 4066 RVA: 0x0001425F File Offset: 0x0001245F
			public BehaviorTickRecord(int initialCapacity)
			{
				this._scriptComponents = new List<ScriptComponentBehavior>(initialCapacity);
				this._addTo = new List<ScriptComponentBehavior>();
				this._removeFrom = new List<ScriptComponentBehavior>();
			}

			// Token: 0x06000FE3 RID: 4067 RVA: 0x0001428C File Offset: 0x0001248C
			internal void AddToRec(ScriptComponentBehavior sc)
			{
				int num = this._removeFrom.IndexOf(sc);
				if (num != -1)
				{
					this._removeFrom.RemoveAt(num);
					return;
				}
				this._addTo.Add(sc);
			}

			// Token: 0x06000FE4 RID: 4068 RVA: 0x000142C4 File Offset: 0x000124C4
			internal void RemoveFromRec(ScriptComponentBehavior sc, bool checkForDoubleRemove = true)
			{
				int num = this._addTo.IndexOf(sc);
				if (num != -1)
				{
					this._addTo.RemoveAt(num);
					return;
				}
				if (this._removeFrom.IndexOf(sc) == -1 && (!checkForDoubleRemove || this.ScriptComponents.IndexOf(sc) != -1))
				{
					this._removeFrom.Add(sc);
				}
			}

			// Token: 0x06000FE5 RID: 4069 RVA: 0x0001431C File Offset: 0x0001251C
			internal void TickRec()
			{
				foreach (ScriptComponentBehavior item in this._removeFrom)
				{
					this.ScriptComponents.Remove(item);
				}
				this._removeFrom.Clear();
				foreach (ScriptComponentBehavior item2 in this._addTo)
				{
					this.ScriptComponents.Add(item2);
				}
				this._addTo.Clear();
			}

			// Token: 0x06000FE6 RID: 4070 RVA: 0x000143D4 File Offset: 0x000125D4
			internal bool ContainsOrToBeAdded(ScriptComponentBehavior sc)
			{
				return (this.ScriptComponents.Contains(sc) || this._addTo.Contains(sc)) && !this._removeFrom.Contains(sc);
			}

			// Token: 0x06000FE7 RID: 4071 RVA: 0x00014403 File Offset: 0x00012603
			internal int GetWillBeRemovedCount()
			{
				return this._removeFrom.Count;
			}

			// Token: 0x040003E9 RID: 1001
			private readonly List<ScriptComponentBehavior> _scriptComponents;

			// Token: 0x040003EA RID: 1002
			private readonly List<ScriptComponentBehavior> _addTo;

			// Token: 0x040003EB RID: 1003
			private readonly List<ScriptComponentBehavior> _removeFrom;
		}
	}
}
