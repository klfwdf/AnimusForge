using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class WeeklyActionMaterialCursor<TAction, THero> where THero : class
{
	private readonly List<KeyValuePair<string, List<TAction>>> _owners;
	private int _ownerIndex;
	private int _actionIndex;
	private THero _hero;

	internal WeeklyActionMaterialCursor(List<KeyValuePair<string, List<TAction>>> owners)
	{
		_owners = owners ?? new List<KeyValuePair<string, List<TAction>>>();
	}

	internal bool Complete => _ownerIndex >= _owners.Count;

	// A step consumes at most one owner boundary or one action; the caller checks its time budget between steps.
	internal bool Advance(Func<string, THero> resolveHero, Action<THero, TAction> consume)
	{
		if (Complete)
		{
			return true;
		}
		KeyValuePair<string, List<TAction>> owner = _owners[_ownerIndex];
		List<TAction> actions = owner.Value;
		if (_actionIndex == 0)
		{
			_hero = resolveHero(owner.Key);
		}
		if (_hero == null || actions == null || _actionIndex >= actions.Count)
		{
			_ownerIndex++;
			_actionIndex = 0;
			_hero = null;
			return Complete;
		}
		consume(_hero, actions[_actionIndex++]);
		return false;
	}
}
