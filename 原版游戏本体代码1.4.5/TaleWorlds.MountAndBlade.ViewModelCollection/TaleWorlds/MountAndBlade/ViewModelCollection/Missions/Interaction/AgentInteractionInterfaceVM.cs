using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Hints;
using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction.InteractionItems;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction;

public class AgentInteractionInterfaceVM : ViewModel
{
	private readonly Mission _mission;

	private bool _currentObjectInteractable;

	private IFocusable _currentFocusedObject;

	private bool _isActive;

	private bool _hasSecondaryMessages;

	private MBBindingList<MissionPrimaryInteractionItemVM> _primaryInteractionMessages;

	private MBBindingList<MissionInteractionItemBaseVM> _secondaryInteractionMessages;

	private int _targetHealth;

	private bool _showHealthBar;

	private string _backgroundColor;

	private string _textColor;

	private bool _displayInteractionText;

	private bool _hasForcedMessages;

	private MBBindingList<MissionPrimaryInteractionItemVM> _forcedInteractionMessages;

	private bool IsPlayerActive
	{
		get
		{
			Agent main = Agent.Main;
			if (main == null)
			{
				return false;
			}
			return main.Health > 0f;
		}
	}

	[DataSourceProperty]
	public int TargetHealth
	{
		get
		{
			return _targetHealth;
		}
		set
		{
			if (value != _targetHealth)
			{
				_targetHealth = value;
				OnPropertyChangedWithValue(value, "TargetHealth");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowHealthBar
	{
		get
		{
			return _showHealthBar;
		}
		set
		{
			if (value != _showHealthBar)
			{
				_showHealthBar = value;
				OnPropertyChangedWithValue(value, "ShowHealthBar");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionPrimaryInteractionItemVM> PrimaryInteractionMessages
	{
		get
		{
			return _primaryInteractionMessages;
		}
		set
		{
			if (_primaryInteractionMessages != value)
			{
				_primaryInteractionMessages = value;
				OnPropertyChangedWithValue(value, "PrimaryInteractionMessages");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionInteractionItemBaseVM> SecondaryInteractionMessages
	{
		get
		{
			return _secondaryInteractionMessages;
		}
		set
		{
			if (_secondaryInteractionMessages != value)
			{
				_secondaryInteractionMessages = value;
				OnPropertyChangedWithValue(value, "SecondaryInteractionMessages");
			}
		}
	}

	[DataSourceProperty]
	public string BackgroundColor
	{
		get
		{
			return _backgroundColor;
		}
		set
		{
			if (_backgroundColor != value)
			{
				_backgroundColor = value;
				OnPropertyChangedWithValue(value, "BackgroundColor");
			}
		}
	}

	[DataSourceProperty]
	public string TextColor
	{
		get
		{
			return _textColor;
		}
		set
		{
			if (_textColor != value)
			{
				_textColor = value;
				OnPropertyChangedWithValue(value, "TextColor");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value == _isActive)
			{
				return;
			}
			_isActive = value;
			OnPropertyChangedWithValue(value, "IsActive");
			if (!value)
			{
				ShowHealthBar = false;
				PrimaryInteractionMessages.ApplyActionOnAllItems(delegate(MissionPrimaryInteractionItemVM x)
				{
					x.ResetData();
				});
			}
		}
	}

	[DataSourceProperty]
	public bool HasSecondaryMessages
	{
		get
		{
			return _hasSecondaryMessages;
		}
		set
		{
			if (value != _hasSecondaryMessages)
			{
				_hasSecondaryMessages = value;
				OnPropertyChangedWithValue(value, "HasSecondaryMessages");
			}
		}
	}

	[DataSourceProperty]
	public bool DisplayInteractionText
	{
		get
		{
			return _displayInteractionText;
		}
		set
		{
			if (value != _displayInteractionText)
			{
				_displayInteractionText = value;
				OnPropertyChangedWithValue(value, "DisplayInteractionText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionPrimaryInteractionItemVM> ForcedInteractionMessages
	{
		get
		{
			return _forcedInteractionMessages;
		}
		set
		{
			if (_forcedInteractionMessages != value)
			{
				_forcedInteractionMessages = value;
				OnPropertyChangedWithValue(value, "ForcedInteractionMessages");
			}
		}
	}

	[DataSourceProperty]
	public bool HasForcedMessages
	{
		get
		{
			return _hasForcedMessages;
		}
		set
		{
			if (_hasForcedMessages == value)
			{
				return;
			}
			_hasForcedMessages = value;
			OnPropertyChangedWithValue(value, "HasForcedMessages");
			if (!value)
			{
				ForcedInteractionMessages.ApplyActionOnAllItems(delegate(MissionPrimaryInteractionItemVM x)
				{
					x.ResetData();
				});
			}
		}
	}

	public AgentInteractionInterfaceVM(Mission mission)
	{
		_mission = mission;
		IsActive = false;
		PrimaryInteractionMessages = new MBBindingList<MissionPrimaryInteractionItemVM>
		{
			new MissionPrimaryInteractionItemVM(),
			new MissionPrimaryInteractionItemVM()
		};
		SecondaryInteractionMessages = new MBBindingList<MissionInteractionItemBaseVM>();
		ForcedInteractionMessages = new MBBindingList<MissionPrimaryInteractionItemVM>
		{
			new MissionPrimaryInteractionItemVM(),
			new MissionPrimaryInteractionItemVM()
		};
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PrimaryInteractionMessages.ApplyActionOnAllItems(delegate(MissionPrimaryInteractionItemVM p)
		{
			p.RefreshValues();
		});
		SecondaryInteractionMessages.ApplyActionOnAllItems(delegate(MissionInteractionItemBaseVM p)
		{
			p.RefreshValues();
		});
		ForcedInteractionMessages.ApplyActionOnAllItems(delegate(MissionPrimaryInteractionItemVM p)
		{
			p.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		PrimaryInteractionMessages.ApplyActionOnAllItems(delegate(MissionPrimaryInteractionItemVM p)
		{
			p.OnFinalize();
		});
		SecondaryInteractionMessages.ApplyActionOnAllItems(delegate(MissionInteractionItemBaseVM p)
		{
			p.OnFinalize();
		});
		ForcedInteractionMessages.ApplyActionOnAllItems(delegate(MissionPrimaryInteractionItemVM p)
		{
			p.OnFinalize();
		});
	}

	internal void Tick(float dt)
	{
		if (_currentFocusedObject is Agent agent)
		{
			ResetFocus();
			OnFocusGained(Agent.Main, agent, agent.IsActive() || Agent.Main.CanInteractWithAgent(agent, -1000f));
		}
		if (IsActive && _mission.Mode == MissionMode.StartUp && _currentFocusedObject is Agent agent2 && agent2.IsEnemyOf(_mission.MainAgent))
		{
			IsActive = false;
		}
		HasSecondaryMessages = SecondaryInteractionMessages.Count > 0;
		SecondaryInteractionMessages.ApplyActionOnAllItems(delegate(MissionInteractionItemBaseVM s)
		{
			s.RefreshValues();
		});
	}

	internal void CheckAndClearFocusedAgent(Agent agent)
	{
		if (_currentFocusedObject != null && _currentFocusedObject as Agent == agent)
		{
			IsActive = false;
			ResetFocus();
		}
	}

	public void OnFocusedHealthChanged(IFocusable focusable, float healthPercentage, bool hideHealthbarWhenFull)
	{
		SetHealth(healthPercentage, hideHealthbarWhenFull);
	}

	internal void OnFocusGained(Agent mainAgent, IFocusable focusableObject, bool isInteractable)
	{
		if (!IsPlayerActive || (_currentFocusedObject == focusableObject && _currentObjectInteractable == isInteractable))
		{
			return;
		}
		ResetFocus();
		_currentFocusedObject = focusableObject;
		_currentObjectInteractable = isInteractable;
		if (focusableObject is Agent agent)
		{
			if (agent.IsHuman)
			{
				SetHumanAgent(mainAgent, agent, isInteractable);
			}
			else if (agent.IsMount)
			{
				SetMount(mainAgent, agent, isInteractable);
			}
			else
			{
				SetGenericAgent(mainAgent, agent, isInteractable);
			}
		}
		else if (focusableObject is UsableMissionObject usableMissionObject)
		{
			if (usableMissionObject is SpawnedItemEntity spawnedItemEntity)
			{
				bool canQuickPickup = Agent.Main.CanQuickPickUp(spawnedItemEntity);
				SetItem(spawnedItemEntity, canQuickPickup, isInteractable);
			}
			else
			{
				SetUsableMissionObject(usableMissionObject, isInteractable);
			}
		}
		else if (focusableObject is UsableMachine machine)
		{
			SetUsableMachine(machine, isInteractable);
		}
		else if (focusableObject is DestructableComponent machine2)
		{
			SetDestructibleComponent(machine2, isInteractable: false);
		}
	}

	internal void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
		ResetFocus();
		IsActive = false;
	}

	internal void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		if (_mission.Mode == MissionMode.Stealth && agent.IsHuman && agent.IsActive() && !agent.IsEnemyOf(userAgent))
		{
			SetHumanAgent(userAgent, agent, isInteractable: true);
		}
	}

	private void GetInteractionTexts(Agent requesterAgent, IFocusable focusable, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		focusableObjectInformation.IsActive = false;
		_mission?.FocusableObjectInformationProvider?.GetInteractionTexts(requesterAgent, focusable, isInteractable, out focusableObjectInformation);
	}

	private void SetItem(SpawnedItemEntity item, bool canQuickPickup, bool isInteractable)
	{
		SetInteractionMessages(Agent.Main, item, isInteractable);
	}

	private void SetUsableMissionObject(UsableMissionObject usableObject, bool isInteractable)
	{
		SetInteractionMessages(Agent.Main, usableObject, isInteractable);
	}

	private void SetUsableMachine(UsableMachine machine, bool isInteractable)
	{
		SetInteractionMessages(Agent.Main, machine, isInteractable);
		if (machine.DestructionComponent != null)
		{
			TargetHealth = (int)(100f * machine.DestructionComponent.HitPoint / machine.DestructionComponent.MaxHitPoint);
			ShowHealthBar = true;
		}
	}

	private void SetDestructibleComponent(DestructableComponent machine, bool isInteractable)
	{
		SetInteractionMessages(Agent.Main, machine, isInteractable);
		TargetHealth = (int)(100f * machine.HitPoint / machine.MaxHitPoint);
		ShowHealthBar = machine.HitPoint < machine.MaxHitPoint;
	}

	private void SetHumanAgent(Agent requesterAgent, Agent focusedAgent, bool isInteractable)
	{
		SetInteractionMessages(requesterAgent, focusedAgent, isInteractable);
	}

	private void SetMount(Agent agent, Agent focusedAgent, bool isInteractable)
	{
		SetInteractionMessages(agent, focusedAgent, isInteractable);
		if (focusedAgent.IsActive() && focusedAgent.IsMount && focusedAgent.RiderAgent == null)
		{
			ShowHealthBar = false;
		}
	}

	private void SetGenericAgent(Agent agent, Agent focusedAgent, bool isInteractable)
	{
		if (focusedAgent.IsActive() && !focusedAgent.IsMount && !focusedAgent.IsHuman)
		{
			SetInteractionMessages(agent, focusedAgent, isInteractable);
		}
	}

	private void SetInteractionMessages(Agent requesterAgent, IFocusable focusableObject, bool isInteractable)
	{
		GetInteractionTexts(requesterAgent, focusableObject, isInteractable, out var focusableObjectInformation);
		IsActive = focusableObjectInformation.IsActive;
		PrimaryInteractionMessages[0].SetData(focusableObjectInformation.PrimaryInteractionText);
		PrimaryInteractionMessages[1].SetData(focusableObjectInformation.SecondaryInteractionText);
		PrimaryInteractionMessages[1].FocusTypeString = focusableObject?.FocusableObjectType.ToString() ?? FocusableObjectType.None.ToString();
	}

	public void OnActiveMissionHintChanged(MissionHint previousHint, MissionHint newHint)
	{
		if (previousHint != null && newHint == null)
		{
			for (int num = SecondaryInteractionMessages.Count - 1; num >= 0; num--)
			{
				if (SecondaryInteractionMessages[num] is MissionHintInteractionItemVM missionHintInteractionItemVM && missionHintInteractionItemVM.Hint == previousHint)
				{
					SecondaryInteractionMessages.RemoveAt(num);
				}
			}
		}
		if (newHint != null)
		{
			SecondaryInteractionMessages.Add(new MissionHintInteractionItemVM(newHint));
		}
		HasSecondaryMessages = SecondaryInteractionMessages.Count > 0;
	}

	public void AddSecondaryMessage(MissionInteractionItemBaseVM message)
	{
		if (HasSecondaryInteractionMessage(message))
		{
			Debug.FailedAssert("Trying to add the same interaction message twice", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Missions\\Interaction\\MissionAgentInteractionInterfaceVM.cs", "AddSecondaryMessage", 264);
			return;
		}
		SecondaryInteractionMessages.Add(message);
		message.IsDisplayed = true;
	}

	public bool RemoveSecondaryMessage(MissionInteractionItemBaseVM message)
	{
		message.IsDisplayed = false;
		return SecondaryInteractionMessages.Remove(message);
	}

	public bool HasSecondaryInteractionMessage(MissionInteractionItemBaseVM message)
	{
		return message.IsDisplayed;
	}

	private void SetHealth(float healthPercentage, bool hideHealthBarWhenFull)
	{
		TargetHealth = (int)(100f * healthPercentage);
		if (hideHealthBarWhenFull)
		{
			ShowHealthBar = TargetHealth < 100;
		}
		else
		{
			ShowHealthBar = true;
		}
	}

	public void ResetFocus()
	{
		_currentFocusedObject = null;
		ShowHealthBar = false;
		PrimaryInteractionMessages[0].ResetData();
		PrimaryInteractionMessages[1].ResetData();
	}

	public void SetForcedInteractionTexts(TextObject text1, bool isDisabled1, TextObject text2, bool isDisabled2)
	{
		HasForcedMessages = true;
		ForcedInteractionMessages[0].SetData(text1, isDisabled1);
		ForcedInteractionMessages[1].SetData(text2, isDisabled2);
	}

	public void ClearForcedInteractionTexts()
	{
		ForcedInteractionMessages[0].ResetData();
		ForcedInteractionMessages[1].ResetData();
		HasForcedMessages = false;
	}

	private string GetWeaponSpecificText(SpawnedItemEntity spawnedItem)
	{
		MissionWeapon weaponCopy = spawnedItem.WeaponCopy;
		WeaponComponentData currentUsageItem = weaponCopy.CurrentUsageItem;
		if (currentUsageItem != null && currentUsageItem.IsShield)
		{
			MBTextManager.SetTextVariable("LEFT", weaponCopy.HitPoints);
			MBTextManager.SetTextVariable("RIGHT", weaponCopy.ModifiedMaxHitPoints);
			return GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis").ToString();
		}
		WeaponComponentData currentUsageItem2 = weaponCopy.CurrentUsageItem;
		if (currentUsageItem2 != null && currentUsageItem2.IsAmmo && weaponCopy.ModifiedMaxAmount > 1 && !spawnedItem.IsStuckMissile())
		{
			MBTextManager.SetTextVariable("LEFT", weaponCopy.Amount);
			MBTextManager.SetTextVariable("RIGHT", weaponCopy.ModifiedMaxAmount);
			return GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis").ToString();
		}
		return "";
	}
}
