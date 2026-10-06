using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public abstract class BehaviorComponent
{
	protected FormationAI.BehaviorSide _behaviorSide;

	protected const float FormArrangementDistanceToOrderPosition = 10f;

	private const float _playerInformCooldown = 60f;

	protected float _lastPlayerInformTime;

	private Timer _navmeshlessTargetPenaltyTime;

	private float _navmeshlessTargetPositionPenalty = 1f;

	public bool IsCurrentOrderChanged;

	private MovementOrder _currentOrder;

	protected FacingOrder CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;

	public Formation Formation { get; private set; }

	public float BehaviorCoherence { get; set; }

	public virtual float NavmeshlessTargetPositionPenalty
	{
		get
		{
			if (_navmeshlessTargetPositionPenalty == 1f)
			{
				return 1f;
			}
			_navmeshlessTargetPenaltyTime.Check(Mission.Current.CurrentTime);
			float num = _navmeshlessTargetPenaltyTime.ElapsedTime();
			if (num >= 10f)
			{
				_navmeshlessTargetPositionPenalty = 1f;
				return 1f;
			}
			if (num <= 5f)
			{
				return _navmeshlessTargetPositionPenalty;
			}
			return MBMath.Lerp(_navmeshlessTargetPositionPenalty, 1f, (num - 5f) / 5f);
		}
		set
		{
			_navmeshlessTargetPenaltyTime.Reset(Mission.Current.CurrentTime);
			_navmeshlessTargetPositionPenalty = value;
		}
	}

	public MovementOrder CurrentOrder
	{
		get
		{
			return _currentOrder;
		}
		protected set
		{
			_currentOrder = value;
			IsCurrentOrderChanged = true;
		}
	}

	public float PreserveExpireTime { get; set; }

	public float WeightFactor { get; set; }

	protected BehaviorComponent(Formation formation)
	{
		Formation = formation;
		PreserveExpireTime = 0f;
		_navmeshlessTargetPenaltyTime = new Timer(Mission.Current.CurrentTime, 50f);
	}

	protected BehaviorComponent()
	{
	}

	private void InformSergeantPlayer()
	{
		if (Mission.Current.MainAgent != null && Formation.Team.GeneralAgent != null && !Formation.Team.IsPlayerGeneral && Formation.Team.IsPlayerSergeant && Formation.PlayerOwner == Agent.Main)
		{
			TextObject behaviorString = GetBehaviorString();
			MBTextManager.SetTextVariable("BEHAVIOR", behaviorString);
			MBTextManager.SetTextVariable("PLAYER_NAME", Mission.Current.MainAgent.NameTextObject);
			MBTextManager.SetTextVariable("TEAM_LEADER", Formation.Team.GeneralAgent.NameTextObject);
			MBInformationManager.AddQuickInformation(new TextObject("{=L91XKoMD}{TEAM_LEADER}: {PLAYER_NAME}, {BEHAVIOR}"), 4000, Formation.Team.GeneralAgent.Character);
		}
	}

	protected virtual void OnBehaviorActivatedAux()
	{
	}

	internal void OnBehaviorActivated()
	{
		if (!Formation.Team.IsPlayerGeneral && !Formation.Team.IsPlayerSergeant && Formation.IsPlayerTroopInFormation && Mission.Current.MainAgent != null)
		{
			TextObject text = new TextObject(ToString().Replace("MBModule.Behavior", ""));
			MBTextManager.SetTextVariable("BEHAVIOUR_NAME_BEGIN", text);
			text = GameTexts.FindText("str_formation_ai_soldier_instruction_text");
			MBInformationManager.AddQuickInformation(text, 2000, Mission.Current.MainAgent.Character);
		}
		if (!GameNetwork.IsMultiplayer)
		{
			InformSergeantPlayer();
			_lastPlayerInformTime = Mission.Current.CurrentTime;
		}
		if (Formation.IsAIControlled)
		{
			OnBehaviorActivatedAux();
		}
	}

	public virtual void OnBehaviorCanceled()
	{
	}

	public virtual void OnLostAIControl()
	{
	}

	public virtual void OnAgentRemoved(Agent agent)
	{
	}

	public void RemindSergeantPlayer()
	{
		float currentTime = Mission.Current.CurrentTime;
		if (this == Formation.AI.ActiveBehavior && _lastPlayerInformTime + 60f < currentTime)
		{
			InformSergeantPlayer();
			_lastPlayerInformTime = currentTime;
		}
	}

	public virtual void TickOccasionally()
	{
	}

	public float GetAIWeight()
	{
		return GetAiWeight() * NavmeshlessTargetPositionPenalty;
	}

	protected abstract float GetAiWeight();

	public virtual void ResetBehavior()
	{
		WeightFactor = 0f;
	}

	public virtual TextObject GetBehaviorString()
	{
		string name = GetType().Name;
		return GameTexts.FindText("str_formation_ai_sergeant_instruction_behavior_text", name);
	}

	public virtual void OnValidBehaviorSideChanged()
	{
		_behaviorSide = Formation.AI.Side;
	}

	protected virtual void CalculateCurrentOrder()
	{
	}

	public void PrecalculateMovementOrder()
	{
		CalculateCurrentOrder();
		CurrentOrder.GetPosition(Formation);
	}

	public override bool Equals(object obj)
	{
		return GetType() == obj.GetType();
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public virtual void OnDeploymentFinished()
	{
	}
}
