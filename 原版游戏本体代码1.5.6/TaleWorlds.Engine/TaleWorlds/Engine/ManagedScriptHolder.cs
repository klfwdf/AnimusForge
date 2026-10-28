using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public sealed class ManagedScriptHolder : DotNetObject
{
	private class BehaviorTickRecord
	{
		private readonly List<ScriptComponentBehavior> _scriptComponents;

		private readonly List<ScriptComponentBehavior> _addTo;

		private readonly List<ScriptComponentBehavior> _removeFrom;

		public List<ScriptComponentBehavior> ScriptComponents => _scriptComponents;

		public BehaviorTickRecord(int initialCapacity)
		{
			_scriptComponents = new List<ScriptComponentBehavior>(initialCapacity);
			_addTo = new List<ScriptComponentBehavior>();
			_removeFrom = new List<ScriptComponentBehavior>();
		}

		internal void AddToRec(ScriptComponentBehavior sc)
		{
			int num = _removeFrom.IndexOf(sc);
			if (num != -1)
			{
				_removeFrom.RemoveAt(num);
			}
			else
			{
				_addTo.Add(sc);
			}
		}

		internal void RemoveFromRec(ScriptComponentBehavior sc, bool checkForDoubleRemove = true)
		{
			int num = _addTo.IndexOf(sc);
			if (num != -1)
			{
				_addTo.RemoveAt(num);
			}
			else if (_removeFrom.IndexOf(sc) == -1 && (!checkForDoubleRemove || ScriptComponents.IndexOf(sc) != -1))
			{
				_removeFrom.Add(sc);
			}
		}

		internal void TickRec()
		{
			foreach (ScriptComponentBehavior item in _removeFrom)
			{
				ScriptComponents.Remove(item);
			}
			_removeFrom.Clear();
			foreach (ScriptComponentBehavior item2 in _addTo)
			{
				ScriptComponents.Add(item2);
			}
			_addTo.Clear();
		}

		internal bool ContainsOrToBeAdded(ScriptComponentBehavior sc)
		{
			if (ScriptComponents.Contains(sc) || _addTo.Contains(sc))
			{
				return !_removeFrom.Contains(sc);
			}
			return false;
		}

		internal int GetWillBeRemovedCount()
		{
			return _removeFrom.Count;
		}
	}

	private static readonly ScriptComponentBehavior.TickRequirement[] TickRequirementEnumValues = (ScriptComponentBehavior.TickRequirement[])Enum.GetValues(typeof(ScriptComponentBehavior.TickRequirement));

	public object AddRemoveLockObject = new object();

	private readonly BehaviorTickRecord _toTick = new BehaviorTickRecord(512);

	private readonly BehaviorTickRecord _toParallelTick = new BehaviorTickRecord(64);

	private readonly BehaviorTickRecord _toParallelTick2 = new BehaviorTickRecord(512);

	private readonly BehaviorTickRecord _toParallelTick3 = new BehaviorTickRecord(512);

	private readonly BehaviorTickRecord _toTickOccasionally = new BehaviorTickRecord(512);

	private readonly BehaviorTickRecord _toTickForEditor = new BehaviorTickRecord(512);

	private readonly BehaviorTickRecord _toFixedParallelTick = new BehaviorTickRecord(64);

	private readonly BehaviorTickRecord _toFixedTick = new BehaviorTickRecord(32);

	private int _nextIndexToTickOccasionally;

	private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsParallelAuxMTPredicate;

	private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsParallel2AuxMTPredicate;

	private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsParallel3AuxMTPredicate;

	private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsFixedParallelAuxMTPredicate;

	private readonly TWParallel.ParallelForWithDtAuxPredicate TickComponentsOccasionallyParallelAuxMTPredicate;

	[EngineCallback(null, false)]
	internal static ManagedScriptHolder CreateManagedScriptHolder()
	{
		return new ManagedScriptHolder();
	}

	public ManagedScriptHolder()
	{
		TickComponentsParallelAuxMTPredicate = TickComponentsParallelAuxMT;
		TickComponentsParallel2AuxMTPredicate = TickComponentsParallel2AuxMT;
		TickComponentsParallel3AuxMTPredicate = TickComponentsParallel3AuxMT;
		TickComponentsOccasionallyParallelAuxMTPredicate = TickComponentsOccasionallyParallelAuxMT;
		TickComponentsFixedParallelAuxMTPredicate = TickComponentsFixedParallelAuxMT;
	}

	[EngineCallback(null, false)]
	public void SetScriptComponentHolder(ScriptComponentBehavior sc)
	{
		sc.SetOwnerManagedScriptHolder(this);
		_toTickForEditor.AddToRec(sc);
		sc.SetScriptComponentToTick(sc.GetTickRequirement());
	}

	private BehaviorTickRecord GetRecordFromEnum(ScriptComponentBehavior.TickRequirement tickRecEnum)
	{
		return tickRecEnum switch
		{
			ScriptComponentBehavior.TickRequirement.TickOccasionally => _toTickOccasionally, 
			ScriptComponentBehavior.TickRequirement.Tick => _toTick, 
			ScriptComponentBehavior.TickRequirement.TickParallel => _toParallelTick, 
			ScriptComponentBehavior.TickRequirement.TickParallel2 => _toParallelTick2, 
			ScriptComponentBehavior.TickRequirement.TickParallel3 => _toParallelTick3, 
			ScriptComponentBehavior.TickRequirement.FixedTick => _toFixedTick, 
			ScriptComponentBehavior.TickRequirement.FixedParallelTick => _toFixedParallelTick, 
			_ => null, 
		};
	}

	public void UpdateTickRequirement(ScriptComponentBehavior sc, ScriptComponentBehavior.TickRequirement oldTickRequirement, ScriptComponentBehavior.TickRequirement newTickRequirement)
	{
		ScriptComponentBehavior.TickRequirement[] tickRequirementEnumValues = TickRequirementEnumValues;
		foreach (ScriptComponentBehavior.TickRequirement tickRequirement in tickRequirementEnumValues)
		{
			if (newTickRequirement.HasAnyFlag(tickRequirement) != oldTickRequirement.HasAnyFlag(tickRequirement))
			{
				BehaviorTickRecord recordFromEnum = GetRecordFromEnum(tickRequirement);
				if (oldTickRequirement.HasAnyFlag(tickRequirement))
				{
					recordFromEnum.RemoveFromRec(sc);
				}
				else
				{
					recordFromEnum.AddToRec(sc);
				}
			}
		}
	}

	[EngineCallback(null, false)]
	public void RemoveScriptComponentFromAllTickLists(ScriptComponentBehavior sc)
	{
		lock (AddRemoveLockObject)
		{
			sc.SetScriptComponentToTickMT(ScriptComponentBehavior.TickRequirement.None);
			_toTickForEditor.RemoveFromRec(sc);
		}
	}

	[EngineCallback(null, false)]
	internal int GetNumberOfScripts()
	{
		return _toTick.ScriptComponents.Count;
	}

	private void TickComponentsParallelAuxMT(int startInclusive, int endExclusive, float dt)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_toParallelTick.ScriptComponents[i].OnTickParallel(dt);
		}
	}

	private void TickComponentsParallel2AuxMT(int startInclusive, int endExclusive, float dt)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_toParallelTick2.ScriptComponents[i].OnTickParallel2(dt);
		}
	}

	private void TickComponentsParallel3AuxMT(int startInclusive, int endExclusive, float dt)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_toParallelTick3.ScriptComponents[i].OnTickParallel3(dt);
		}
	}

	private void TickComponentsOccasionallyParallelAuxMT(int startInclusive, int endExclusive, float dt)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_toTickOccasionally.ScriptComponents[i].OnTickOccasionally(dt);
		}
	}

	private void TickComponentsFixedParallelAuxMT(int startInclusive, int endExclusive, float dt)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_toFixedParallelTick.ScriptComponents[i].OnParallelFixedTick(dt);
		}
	}

	[EngineCallback(null, false)]
	internal void FixedTickComponents(float fixedDt)
	{
		_toFixedParallelTick.TickRec();
		TWParallel.For(0, _toFixedParallelTick.ScriptComponents.Count, fixedDt, TickComponentsFixedParallelAuxMTPredicate, 1);
		_toFixedTick.TickRec();
		foreach (ScriptComponentBehavior scriptComponent in _toFixedTick.ScriptComponents)
		{
			scriptComponent.OnFixedTick(fixedDt);
		}
	}

	[EngineCallback(null, false)]
	internal void TickComponents(float dt)
	{
		_toParallelTick.TickRec();
		TWParallel.For(0, _toParallelTick.ScriptComponents.Count, dt, TickComponentsParallelAuxMTPredicate, 1);
		_toParallelTick2.TickRec();
		TWParallel.For(0, _toParallelTick2.ScriptComponents.Count, dt, TickComponentsParallel2AuxMTPredicate, 8);
		_toParallelTick3.TickRec();
		TWParallel.For(0, _toParallelTick3.ScriptComponents.Count, dt, TickComponentsParallel3AuxMTPredicate, 8);
		_toTick.TickRec();
		foreach (ScriptComponentBehavior scriptComponent in _toTick.ScriptComponents)
		{
			scriptComponent.OnTick(dt);
		}
		_nextIndexToTickOccasionally = TaleWorlds.Library.MathF.Max(0, _nextIndexToTickOccasionally - _toTickOccasionally.GetWillBeRemovedCount());
		_toTickOccasionally.TickRec();
		int num = _toTickOccasionally.ScriptComponents.Count / 10 + 1;
		int num2 = Math.Min(_nextIndexToTickOccasionally + num, _toTickOccasionally.ScriptComponents.Count);
		if (_nextIndexToTickOccasionally < num2)
		{
			TWParallel.For(_nextIndexToTickOccasionally, num2, dt, TickComponentsOccasionallyParallelAuxMTPredicate, 8);
			_nextIndexToTickOccasionally = ((num2 < _toTickOccasionally.ScriptComponents.Count) ? num2 : 0);
		}
		else
		{
			_nextIndexToTickOccasionally = 0;
		}
	}

	[EngineCallback(null, false)]
	internal void TickComponentsEditor(float dt)
	{
		_toTickForEditor.TickRec();
		for (int i = 0; i < _toTickForEditor.ScriptComponents.Count; i++)
		{
			_toTickForEditor.ScriptComponents[i].OnEditorTick(dt);
		}
	}
}
