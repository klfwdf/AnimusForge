using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects;

public class PassageUsePoint : StandingPoint
{
	public string ToLocationId = "";

	public bool IsMissionExit;

	private bool _initialized;

	private readonly MBList<Agent> _movingAgents;

	private Location _toLocation;

	private const float InteractionDistanceForAI = 0.5f;

	public MBReadOnlyList<Agent> MovingAgents => _movingAgents;

	public override Agent MovingAgent
	{
		get
		{
			if (_movingAgents.Count <= 0)
			{
				return null;
			}
			return _movingAgents[0];
		}
	}

	public Location ToLocation
	{
		get
		{
			if (!_initialized)
			{
				InitializeLocation();
			}
			return _toLocation;
		}
	}

	public override bool HasAIMovingTo => _movingAgents.Count > 0;

	public override FocusableObjectType FocusableObjectType => FocusableObjectType.Door;

	public override bool DisableCombatActionsOnUse => !base.IsInstantUse;

	public PassageUsePoint()
	{
		base.IsInstantUse = true;
		_movingAgents = new MBList<Agent>();
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (agent.MountAgent != null || base.IsDeactivated || (ToLocation == null && !IsMissionExit) || base.IsDisabled)
		{
			return true;
		}
		if (agent.IsAIControlled)
		{
			if (!IsMissionExit)
			{
				return !ToLocation.CanAIEnter(CampaignMission.Current.Location.GetLocationCharacter(agent.Origin));
			}
			return true;
		}
		return false;
	}

	public override void AfterMissionStart()
	{
		DescriptionMessage = GameTexts.FindText(IsMissionExit ? "str_mission_exit" : "str_ui_door");
		ActionMessage = GameTexts.FindText("str_ui_default_door");
		if (ToLocation != null || IsMissionExit)
		{
			ActionMessage = GameTexts.FindText("str_key_action");
			ActionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			ActionMessage.SetTextVariable("ACTION", (ToLocation == null) ? GameTexts.FindText("str_ui_default_door") : ToLocation.DoorName);
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		LockUserPositions = true;
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		if (Campaign.Current.GameMode != CampaignGameMode.Campaign && !userAgent.IsAIControlled)
		{
			return;
		}
		base.OnUse(userAgent, agentBoneIndex);
		bool flag = false;
		if (ToLocation != null)
		{
			if (base.UserAgent.IsMainAgent)
			{
				if (!ToLocation.CanPlayerEnter())
				{
					InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=ILnr9eCQ}Door is locked!").ToString()));
				}
				else
				{
					flag = true;
					Campaign.Current.GameMenuManager.NextLocation = ToLocation;
					Campaign.Current.GameMenuManager.PreviousLocation = CampaignMission.Current.Location;
					Mission.Current.EndMission();
				}
			}
			else if (base.UserAgent.IsAIControlled)
			{
				LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(base.UserAgent.Origin);
				if (!ToLocation.CanAIEnter(locationCharacter))
				{
					MBDebug.ShowWarning("AI should not try to use passage ");
				}
				else
				{
					flag = true;
					LocationComplex.Current.ChangeLocation(locationCharacter, CampaignMission.Current.Location, ToLocation);
					base.UserAgent.FadeOut(hideInstantly: false, hideMount: true);
				}
			}
		}
		else if (IsMissionExit)
		{
			flag = true;
			Mission.Current.EndMission();
		}
		if (flag)
		{
			Mission.Current.MakeSound(MiscSoundContainer.SoundCodeMovementFoleyDoorOpen, base.GameEntity.GetGlobalFrame().origin, soundCanBePredicted: true, isReliable: false, -1, -1);
		}
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		if (LockUserFrames || LockUserPositions)
		{
			userAgent.ClearTargetFrame();
		}
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		bool result = true;
		if (userAgent.IsAIControlled && (InteractionEntity.GetGlobalFrame().origin.AsVec2 - userAgent.Position.AsVec2).LengthSquared > 0.25f)
		{
			result = false;
		}
		return result;
	}

	private void InitializeLocation()
	{
		if (string.IsNullOrEmpty(ToLocationId) || IsMissionExit)
		{
			_toLocation = null;
			_initialized = IsMissionExit;
		}
		else if (Mission.Current != null && Campaign.Current != null)
		{
			if (PlayerEncounter.LocationEncounter != null && CampaignMission.Current.Location != null)
			{
				_toLocation = CampaignMission.Current.Location.GetPassageToLocation(ToLocationId);
			}
			_initialized = true;
		}
	}

	public override int GetMovingAgentCount()
	{
		return _movingAgents.Count;
	}

	public override Agent GetMovingAgentWithIndex(int index)
	{
		return _movingAgents[index];
	}

	public override void AddMovingAgent(Agent movingAgent)
	{
		_movingAgents.Add(movingAgent);
	}

	public override void RemoveMovingAgent(Agent movingAgent)
	{
		_movingAgents.Remove(movingAgent);
	}

	public override bool IsAIMovingTo(Agent agent)
	{
		return _movingAgents.Contains(agent);
	}
}
