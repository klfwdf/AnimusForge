using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FormationAI
{
	public class BehaviorData
	{
		public BehaviorComponent Behavior;

		public float Preference = 1f;

		public float Weight;

		public bool IsRemovedOnCancel;

		public bool IsPreprocessed;
	}

	public enum BehaviorSide
	{
		Left = 0,
		Middle = 1,
		Right = 2,
		BehaviorSideNotSet = 3,
		ValidBehaviorSideCount = 3
	}

	private const float BehaviorPreserveTime = 5f;

	private readonly Formation _formation;

	private readonly List<BehaviorData> _specialBehaviorData;

	private readonly List<BehaviorComponent> _behaviors = new List<BehaviorComponent>();

	private BehaviorComponent _activeBehavior;

	private BehaviorSide _side = BehaviorSide.Middle;

	private readonly Timer _tickTimer;

	public BehaviorComponent ActiveBehavior
	{
		get
		{
			return _activeBehavior;
		}
		private set
		{
			if (_activeBehavior != value)
			{
				_activeBehavior?.OnBehaviorCanceled();
				BehaviorComponent activeBehavior = _activeBehavior;
				_activeBehavior = value;
				_activeBehavior.OnBehaviorActivated();
				ActiveBehavior.PreserveExpireTime = Mission.Current.CurrentTime + 10f;
				if (this.OnActiveBehaviorChanged != null && (activeBehavior == null || !activeBehavior.Equals(value)))
				{
					this.OnActiveBehaviorChanged(_formation);
				}
			}
		}
	}

	public BehaviorSide Side
	{
		get
		{
			return _side;
		}
		set
		{
			if (_side == value)
			{
				return;
			}
			_side = value;
			if (_side == BehaviorSide.BehaviorSideNotSet)
			{
				return;
			}
			foreach (BehaviorComponent behavior in _behaviors)
			{
				behavior.OnValidBehaviorSideChanged();
			}
		}
	}

	public bool IsMainFormation { get; set; }

	public int BehaviorCount => _behaviors.Count;

	public event Action<Formation> OnActiveBehaviorChanged;

	public FormationAI(Formation formation)
	{
		_formation = formation;
		float num = 0f;
		if (formation.Team != null)
		{
			float num2 = 0.1f * (float)formation.FormationIndex;
			float num3 = 0f;
			if (formation.Team.TeamIndex >= 0)
			{
				num3 = (float)formation.Team.TeamIndex * 0.5f * 0.1f;
			}
			num = num2 + num3;
		}
		_tickTimer = new Timer(Mission.Current.CurrentTime + 0.5f * num, 0.5f);
		_specialBehaviorData = new List<BehaviorData>();
	}

	public T SetBehaviorWeight<T>(float w) where T : BehaviorComponent
	{
		foreach (BehaviorComponent behavior in _behaviors)
		{
			if (behavior is T val)
			{
				val.WeightFactor = w;
				return val;
			}
		}
		throw new MBException("Behavior weight could not be set.");
	}

	public void AddAiBehavior(BehaviorComponent behaviorComponent)
	{
		_behaviors.Add(behaviorComponent);
	}

	public T GetBehavior<T>() where T : BehaviorComponent
	{
		foreach (BehaviorComponent behavior in _behaviors)
		{
			if (behavior is T result)
			{
				return result;
			}
		}
		foreach (BehaviorData specialBehaviorDatum in _specialBehaviorData)
		{
			if (specialBehaviorDatum.Behavior is T result2)
			{
				return result2;
			}
		}
		return null;
	}

	public void AddSpecialBehavior(BehaviorComponent behavior, bool purgePreviousSpecialBehaviors = false)
	{
		if (purgePreviousSpecialBehaviors)
		{
			_specialBehaviorData.Clear();
		}
		_specialBehaviorData.Add(new BehaviorData
		{
			Behavior = behavior
		});
	}

	private bool FindBestBehavior()
	{
		BehaviorComponent behaviorComponent = null;
		float num = float.MinValue;
		foreach (BehaviorComponent behavior in _behaviors)
		{
			if (!(behavior.WeightFactor > 1E-07f))
			{
				continue;
			}
			float num2 = behavior.GetAIWeight() * behavior.WeightFactor;
			if (behavior == ActiveBehavior)
			{
				num2 *= MBMath.Lerp(1.2f, 2f, MBMath.ClampFloat((behavior.PreserveExpireTime - Mission.Current.CurrentTime) / 5f, 0f, 1f), float.MinValue);
			}
			if (num2 > num)
			{
				if (behavior.NavmeshlessTargetPositionPenalty > 0f)
				{
					num2 /= behavior.NavmeshlessTargetPositionPenalty;
				}
				behavior.PrecalculateMovementOrder();
				num2 *= behavior.NavmeshlessTargetPositionPenalty;
				if (num2 > num)
				{
					behaviorComponent = behavior;
					num = num2;
				}
			}
		}
		if (behaviorComponent != null)
		{
			ActiveBehavior = behaviorComponent;
			if (behaviorComponent != _behaviors[0])
			{
				_behaviors.Remove(behaviorComponent);
				_behaviors.Insert(0, behaviorComponent);
			}
			return true;
		}
		return false;
	}

	private void PreprocessBehaviors()
	{
		if (!_formation.HasAnyEnemyFormationsThatIsNotEmpty())
		{
			return;
		}
		BehaviorData behaviorData = _specialBehaviorData.FirstOrDefault((BehaviorData sd) => !sd.IsPreprocessed);
		if (behaviorData != null)
		{
			behaviorData.Behavior.TickOccasionally();
			float num = behaviorData.Behavior.GetAIWeight();
			if (behaviorData.Behavior == ActiveBehavior)
			{
				num *= MBMath.Lerp(1.01f, 1.5f, MBMath.ClampFloat((behaviorData.Behavior.PreserveExpireTime - Mission.Current.CurrentTime) / 5f, 0f, 1f), float.MinValue);
			}
			behaviorData.Weight = num * behaviorData.Preference;
			behaviorData.IsPreprocessed = true;
		}
	}

	public void Tick()
	{
		if (Mission.Current.AllowAiTicking && (Mission.Current.ForceTickOccasionally || _tickTimer.Check(Mission.Current.CurrentTime)))
		{
			TickOccasionally(_tickTimer.PreviousDeltaTime);
		}
	}

	private void TickOccasionally(float dt)
	{
		_formation.IsAITickedAfterSplit = true;
		if (FindBestBehavior())
		{
			if (!_formation.IsAIControlled)
			{
				if (GameNetwork.IsMultiplayer && Mission.Current.MainAgent != null && !_formation.Team.IsPlayerGeneral && _formation.Team.IsPlayerSergeant && _formation.PlayerOwner == Agent.Main)
				{
					ActiveBehavior.RemindSergeantPlayer();
				}
			}
			else
			{
				ActiveBehavior.TickOccasionally();
			}
			return;
		}
		BehaviorComponent behaviorComponent = ActiveBehavior;
		if (!_formation.HasAnyEnemyFormationsThatIsNotEmpty())
		{
			return;
		}
		PreprocessBehaviors();
		foreach (BehaviorData specialBehaviorDatum in _specialBehaviorData)
		{
			specialBehaviorDatum.IsPreprocessed = false;
		}
		if (behaviorComponent is BehaviorStop && _specialBehaviorData.Count > 0)
		{
			IEnumerable<BehaviorData> source = _specialBehaviorData.Where((BehaviorData sbd) => sbd.Weight > 0f);
			if (source.Any())
			{
				behaviorComponent = TaleWorlds.Core.Extensions.MaxBy(source, (BehaviorData abd) => abd.Weight).Behavior;
			}
		}
		_ = _formation.IsAIControlled;
		bool flag = false;
		if (ActiveBehavior != behaviorComponent)
		{
			_ = ActiveBehavior;
			ActiveBehavior = behaviorComponent;
			flag = true;
		}
		if (flag || (behaviorComponent != null && behaviorComponent.IsCurrentOrderChanged))
		{
			if (_formation.IsAIControlled)
			{
				_formation.SetMovementOrder(behaviorComponent.CurrentOrder);
			}
			behaviorComponent.IsCurrentOrderChanged = false;
		}
	}

	public void OnDeploymentFinished()
	{
		foreach (BehaviorComponent behavior in _behaviors)
		{
			behavior.OnDeploymentFinished();
		}
	}

	public void OnAgentRemoved(Agent agent)
	{
		foreach (BehaviorComponent behavior in _behaviors)
		{
			behavior.OnAgentRemoved(agent);
		}
	}

	public BehaviorComponent GetBehaviorAtIndex(int index)
	{
		if (index >= 0 && index < _behaviors.Count)
		{
			return _behaviors[index];
		}
		return null;
	}

	[Conditional("DEBUG")]
	public void DebugMore()
	{
		if (!MBDebug.IsDisplayingHighLevelAI)
		{
			return;
		}
		foreach (BehaviorData item in _specialBehaviorData.OrderBy((BehaviorData d) => d.Behavior.GetType().ToString()))
		{
			item.Behavior.GetType().ToString().Replace("MBModule.Behavior", "");
			item.Weight.ToString("0.00");
			item.Preference.ToString("0.00");
		}
	}

	[Conditional("DEBUG")]
	public void DebugScores()
	{
		if (_formation.PhysicalClass.IsRanged())
		{
			MBDebug.Print("Ranged");
		}
		else if (_formation.PhysicalClass.IsMeleeCavalry())
		{
			MBDebug.Print("Cavalry");
		}
		else
		{
			MBDebug.Print("Infantry");
		}
		foreach (BehaviorData item in _specialBehaviorData.OrderBy((BehaviorData d) => d.Behavior.GetType().ToString()))
		{
			string text = item.Behavior.GetType().ToString().Replace("MBModule.Behavior", "");
			string text2 = item.Weight.ToString("0.00");
			string text3 = item.Preference.ToString("0.00");
			MBDebug.Print(text + " \t\t w:" + text2 + "\t p:" + text3);
		}
	}

	public void ResetBehaviorWeights()
	{
		foreach (BehaviorComponent behavior in _behaviors)
		{
			behavior.ResetBehavior();
		}
	}
}
