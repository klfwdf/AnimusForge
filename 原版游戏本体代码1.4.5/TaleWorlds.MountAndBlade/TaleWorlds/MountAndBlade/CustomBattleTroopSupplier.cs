using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleTroopSupplier : IMissionTroopSupplier
{
	private readonly CustomBattleCombatant _customBattleCombatant;

	private TaleWorlds.Library.PriorityQueue<float, BasicCharacterObject> _characters;

	private int _numAllocated;

	private int _numWounded;

	private int _numKilled;

	private int _numRouted;

	private Func<BasicCharacterObject, bool> _customAllocationConditions;

	private bool _anyTroopRemainsToBeSupplied = true;

	private readonly bool _isPlayerSide;

	private readonly bool _isPlayerGeneral;

	private readonly bool _isSallyOut;

	private int _nextTroopRank;

	public int NumRemovedTroops => _numWounded + _numKilled + _numRouted;

	public int NumTroopsNotSupplied => _characters.Count - _numAllocated;

	public bool AnyTroopRemainsToBeSupplied => _anyTroopRemainsToBeSupplied;

	public CustomBattleTroopSupplier(CustomBattleCombatant customBattleCombatant, bool isPlayerSide, bool isPlayerGeneral, bool isSallyOut, Func<BasicCharacterObject, bool> customAllocationConditions = null)
	{
		_customBattleCombatant = customBattleCombatant;
		_customAllocationConditions = customAllocationConditions;
		_isPlayerSide = isPlayerSide;
		_isPlayerGeneral = isPlayerSide && isPlayerGeneral;
		_isSallyOut = isSallyOut;
		ArrangePriorities();
		_nextTroopRank = 0;
	}

	private void ArrangePriorities()
	{
		_characters = new TaleWorlds.Library.PriorityQueue<float, BasicCharacterObject>(new TaleWorlds.Library.GenericComparer<float>());
		int[] troopCountByFormationType = new int[8];
		int[] enqueuedTroopCountByFormationType = new int[8];
		int i;
		for (i = 0; i < 8; i++)
		{
			troopCountByFormationType[i] = _customBattleCombatant.Characters.Count((BasicCharacterObject character) => character.DefaultFormationClass == (FormationClass)i);
		}
		UnitSpawnPrioritizations unitSpawnPrioritization = ((!_isPlayerSide) ? UnitSpawnPrioritizations.HighLevel : Game.Current.UnitSpawnPrioritization);
		int troopCountTotal = troopCountByFormationType.Sum();
		float heroProbability = 1000f;
		foreach (BasicCharacterObject character in _customBattleCombatant.Characters)
		{
			FormationClass formationClass = character.GetFormationClass();
			float num = 0f;
			num = ((!_isSallyOut) ? GetDefaultProbabilityOfTroop(character, troopCountTotal, unitSpawnPrioritization, ref heroProbability, ref troopCountByFormationType, ref enqueuedTroopCountByFormationType) : GetSallyOutAmbushProbabilityOfTroop(character, troopCountTotal, ref heroProbability));
			troopCountByFormationType[(int)formationClass]--;
			enqueuedTroopCountByFormationType[(int)formationClass]++;
			_characters.Enqueue(num, character);
		}
	}

	private float GetSallyOutAmbushProbabilityOfTroop(BasicCharacterObject character, int troopCountTotal, ref float heroProbability)
	{
		float num = 0f;
		if (character.IsHero)
		{
			num = heroProbability--;
		}
		else
		{
			num += (float)character.Level;
			if (character.HasMount())
			{
				num += 100f;
			}
		}
		return num;
	}

	private float GetDefaultProbabilityOfTroop(BasicCharacterObject character, int troopCountTotal, UnitSpawnPrioritizations unitSpawnPrioritization, ref float heroProbability, ref int[] troopCountByFormationType, ref int[] enqueuedTroopCountByFormationType)
	{
		FormationClass formationClass = character.GetFormationClass();
		float num = (float)troopCountByFormationType[(int)formationClass] / (float)((unitSpawnPrioritization == UnitSpawnPrioritizations.Homogeneous) ? (enqueuedTroopCountByFormationType[(int)formationClass] + 1) : troopCountTotal);
		float num2 = (character.IsHero ? heroProbability-- : num);
		if (!character.IsHero && (unitSpawnPrioritization == UnitSpawnPrioritizations.HighLevel || unitSpawnPrioritization == UnitSpawnPrioritizations.LowLevel))
		{
			num2 += (float)character.Level;
			if (unitSpawnPrioritization == UnitSpawnPrioritizations.LowLevel)
			{
				num2 *= -1f;
			}
		}
		return num2;
	}

	public IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate)
	{
		List<BasicCharacterObject> list = AllocateTroops(numberToAllocate);
		CustomBattleAgentOrigin[] array = new CustomBattleAgentOrigin[list.Count];
		_numAllocated += list.Count;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new CustomBattleAgentOrigin(uniqueNo: new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed), customBattleCombatant: _customBattleCombatant, characterObject: list[i], troopSupplier: this, isPlayerSide: _isPlayerSide, rank: _nextTroopRank++);
		}
		if (array.Length < numberToAllocate)
		{
			_anyTroopRemainsToBeSupplied = false;
		}
		return array;
	}

	public IAgentOriginBase SupplyOneTroop()
	{
		BasicCharacterObject basicCharacterObject = AllocateTroop();
		if (basicCharacterObject != null)
		{
			return new CustomBattleAgentOrigin(uniqueNo: new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed), customBattleCombatant: _customBattleCombatant, characterObject: basicCharacterObject, troopSupplier: this, isPlayerSide: _isPlayerSide, rank: _nextTroopRank++);
		}
		_anyTroopRemainsToBeSupplied = false;
		return null;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroops()
	{
		CustomBattleAgentOrigin[] array = new CustomBattleAgentOrigin[_customBattleCombatant.Characters.Count()];
		int num = 0;
		foreach (BasicCharacterObject character in _customBattleCombatant.Characters)
		{
			array[num] = new CustomBattleAgentOrigin(_customBattleCombatant, character, this, _isPlayerSide);
			num++;
		}
		return array;
	}

	public BasicCharacterObject GetGeneralCharacter()
	{
		return _customBattleCombatant.General;
	}

	private List<BasicCharacterObject> AllocateTroops(int numberToAllocate)
	{
		if (numberToAllocate > _characters.Count)
		{
			numberToAllocate = _characters.Count;
		}
		List<BasicCharacterObject> list = new List<BasicCharacterObject>();
		while (numberToAllocate > 0 && _characters.Count > 0)
		{
			BasicCharacterObject basicCharacterObject = _characters.DequeueValue();
			if (_customAllocationConditions == null || _customAllocationConditions(basicCharacterObject))
			{
				list.Add(basicCharacterObject);
				numberToAllocate--;
			}
		}
		return list;
	}

	private BasicCharacterObject AllocateTroop()
	{
		BasicCharacterObject result = null;
		while (_characters.Count > 0)
		{
			BasicCharacterObject basicCharacterObject = _characters.DequeueValue();
			if (_customAllocationConditions == null || _customAllocationConditions(basicCharacterObject))
			{
				result = basicCharacterObject;
				break;
			}
		}
		return result;
	}

	public void OnTroopWounded()
	{
		_numWounded++;
	}

	public void OnTroopKilled()
	{
		_numKilled++;
	}

	public void OnTroopRouted()
	{
		_numRouted++;
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		return _customBattleCombatant.CountOfCharacters;
	}
}
