using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class MissionFocusableObjectInformationProvider
{
	private List<GetFocusableObjectInteractionTextsDelegate> _getFocusableObjectTextsCallbacks;

	private List<GetFocusableObjectInteractionTextsDelegate> _getDefaultFocusableObjectTextsCallbacks;

	public MissionFocusableObjectInformationProvider()
	{
		_getFocusableObjectTextsCallbacks = new List<GetFocusableObjectInteractionTextsDelegate>();
		_getDefaultFocusableObjectTextsCallbacks = new List<GetFocusableObjectInteractionTextsDelegate>();
		AddDefaultInteractionTexts();
	}

	private void AddDefaultInteractionTexts()
	{
		_getDefaultFocusableObjectTextsCallbacks.Add(GetSpawnedItemEntityTexts);
		_getDefaultFocusableObjectTextsCallbacks.Add(GetUsableMissionObjectTexts);
		_getDefaultFocusableObjectTextsCallbacks.Add(GetDestructibleComponentTexts);
		_getDefaultFocusableObjectTextsCallbacks.Add(GetUsableMachineTexts);
		_getDefaultFocusableObjectTextsCallbacks.Add(GetHumanAgentTexts);
		_getDefaultFocusableObjectTextsCallbacks.Add(GetMountTexts);
		_getDefaultFocusableObjectTextsCallbacks.Add(GetGenericAgentTexts);
	}

	public void OnFinalize()
	{
		_getFocusableObjectTextsCallbacks.Clear();
		_getDefaultFocusableObjectTextsCallbacks.Clear();
		_getFocusableObjectTextsCallbacks = null;
		_getDefaultFocusableObjectTextsCallbacks = null;
	}

	public void AddInfoCallback(GetFocusableObjectInteractionTextsDelegate callback)
	{
		_getFocusableObjectTextsCallbacks.Add(callback);
	}

	public void RemoveInfoCallback(GetFocusableObjectInteractionTextsDelegate callback)
	{
		_getFocusableObjectTextsCallbacks.Remove(callback);
	}

	public void GetInteractionTexts(Agent requesterAgent, IFocusable focusable, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		for (int num = _getFocusableObjectTextsCallbacks.Count - 1; num >= 0; num--)
		{
			_getFocusableObjectTextsCallbacks[num](requesterAgent, focusable, isInteractable, out focusableObjectInformation);
			if (focusableObjectInformation.IsActive)
			{
				return;
			}
		}
		for (int i = 0; i < _getDefaultFocusableObjectTextsCallbacks.Count; i++)
		{
			_getDefaultFocusableObjectTextsCallbacks[i](requesterAgent, focusable, isInteractable, out focusableObjectInformation);
			if (focusableObjectInformation.IsActive)
			{
				return;
			}
		}
		focusableObjectInformation.IsActive = false;
		focusableObjectInformation.PrimaryInteractionText = TextObject.GetEmpty();
		focusableObjectInformation.SecondaryInteractionText = TextObject.GetEmpty();
	}

	private void GetSpawnedItemEntityTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (focusableObject is SpawnedItemEntity { IsDeactivated: false, IsDisabled: false } spawnedItemEntity)
		{
			EquipmentIndex possibleSlotIndex;
			ItemObject weaponToReplaceOnQuickAction = Agent.Main.GetWeaponToReplaceOnQuickAction(spawnedItemEntity, out possibleSlotIndex);
			bool flag = Agent.Main.CanQuickPickUp(spawnedItemEntity);
			bool fillUp = possibleSlotIndex != EquipmentIndex.None && !Agent.Main.Equipment[possibleSlotIndex].IsEmpty && Agent.Main.Equipment[possibleSlotIndex].IsAnyConsumable() && Agent.Main.Equipment[possibleSlotIndex].Amount < Agent.Main.Equipment[possibleSlotIndex].ModifiedMaxAmount;
			TextObject actionMessage = spawnedItemEntity.GetActionMessage(weaponToReplaceOnQuickAction, fillUp);
			TextObject descriptionMessage = spawnedItemEntity.GetDescriptionMessage(fillUp);
			if (!TextObject.IsNullOrEmpty(actionMessage) && !TextObject.IsNullOrEmpty(descriptionMessage))
			{
				if (isInteractable)
				{
					MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
					if (flag)
					{
						Agent main = Agent.Main;
						if (main != null && main.CanInteractableWeaponBePickedUp(spawnedItemEntity))
						{
							TextObject textObject = GameTexts.FindText("str_key_action");
							textObject.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use").ToString());
							textObject.SetTextVariable("ACTION", actionMessage.ToString());
							TextObject textObject2 = GameTexts.FindText("str_hold_key_action");
							textObject2.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use").ToString());
							textObject2.SetTextVariable("ACTION", GameTexts.FindText("str_select_item_to_replace").ToString());
							focusableObjectInformation.PrimaryInteractionText = GameTexts.FindText("str_string_newline_string");
							focusableObjectInformation.PrimaryInteractionText.SetTextVariable("STR1", textObject.ToString());
							focusableObjectInformation.PrimaryInteractionText.SetTextVariable("STR2", textObject2.ToString());
							focusableObjectInformation.SecondaryInteractionText = GetItemNameText(descriptionMessage, GetWeaponSpecificText(spawnedItemEntity));
							focusableObjectInformation.IsActive = true;
						}
						else
						{
							focusableObjectInformation.PrimaryInteractionText = GameTexts.FindText("str_key_action");
							focusableObjectInformation.PrimaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use").ToString());
							focusableObjectInformation.PrimaryInteractionText.SetTextVariable("ACTION", actionMessage.ToString());
							focusableObjectInformation.SecondaryInteractionText = GetItemNameText(descriptionMessage, GetWeaponSpecificText(spawnedItemEntity));
							focusableObjectInformation.IsActive = true;
						}
					}
					else
					{
						focusableObjectInformation.PrimaryInteractionText = GameTexts.FindText("str_hold_key_action");
						focusableObjectInformation.PrimaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use").ToString());
						focusableObjectInformation.PrimaryInteractionText.SetTextVariable("ACTION", GameTexts.FindText("str_select_item_to_replace").ToString());
						focusableObjectInformation.SecondaryInteractionText = GetItemNameText(descriptionMessage, GetWeaponSpecificText(spawnedItemEntity));
						focusableObjectInformation.IsActive = true;
					}
				}
				else
				{
					focusableObjectInformation.PrimaryInteractionText = spawnedItemEntity.GetInfoTextForBeingNotInteractable(Agent.Main);
					focusableObjectInformation.SecondaryInteractionText = GetItemNameText(descriptionMessage, GetWeaponSpecificText(spawnedItemEntity));
					focusableObjectInformation.IsActive = true;
				}
			}
			else
			{
				focusableObjectInformation.IsActive = false;
			}
		}
		else
		{
			focusableObjectInformation.IsActive = false;
		}
	}

	private TextObject GetItemNameText(TextObject descriptionMessage, TextObject weaponSpecificText)
	{
		TextObject textObject;
		if (!TextObject.IsNullOrEmpty(weaponSpecificText))
		{
			textObject = GameTexts.FindText("str_STR1_space_STR2");
			textObject.SetTextVariable("STR1", descriptionMessage.ToString());
			textObject.SetTextVariable("STR2", weaponSpecificText.ToString());
		}
		else
		{
			textObject = descriptionMessage;
		}
		return textObject;
	}

	private TextObject GetWeaponSpecificText(SpawnedItemEntity spawnedItem)
	{
		MissionWeapon weaponCopy = spawnedItem.WeaponCopy;
		WeaponComponentData currentUsageItem = weaponCopy.CurrentUsageItem;
		if (currentUsageItem != null && currentUsageItem.IsShield)
		{
			TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis");
			textObject.SetTextVariable("LEFT", weaponCopy.HitPoints);
			textObject.SetTextVariable("RIGHT", weaponCopy.ModifiedMaxHitPoints);
			return textObject;
		}
		WeaponComponentData currentUsageItem2 = weaponCopy.CurrentUsageItem;
		if (currentUsageItem2 != null && currentUsageItem2.IsAmmo && weaponCopy.ModifiedMaxAmount > 1 && !spawnedItem.IsStuckMissile())
		{
			TextObject textObject2 = GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis");
			textObject2.SetTextVariable("LEFT", weaponCopy.Amount);
			textObject2.SetTextVariable("RIGHT", weaponCopy.ModifiedMaxAmount);
			return textObject2;
		}
		return TextObject.GetEmpty();
	}

	private void GetUsableMissionObjectTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (!(focusableObject is UsableMissionObject usableMissionObject))
		{
			focusableObjectInformation.IsActive = false;
			return;
		}
		if (!TextObject.IsNullOrEmpty(usableMissionObject.ActionMessage) || !TextObject.IsNullOrEmpty(usableMissionObject.DescriptionMessage))
		{
			focusableObjectInformation.PrimaryInteractionText = usableMissionObject.DescriptionMessage;
			focusableObjectInformation.SecondaryInteractionText = (isInteractable ? usableMissionObject.ActionMessage : TextObject.GetEmpty());
			focusableObjectInformation.IsActive = true;
			return;
		}
		UsableMachine usableMachineFromPoint = GetUsableMachineFromPoint(usableMissionObject);
		if (usableMachineFromPoint != null)
		{
			focusableObjectInformation.PrimaryInteractionText = usableMachineFromPoint.GetDescriptionText(usableMissionObject.GameEntity);
			focusableObjectInformation.SecondaryInteractionText = (isInteractable ? (usableMachineFromPoint.GetActionTextForStandingPoint(usableMissionObject) ?? TextObject.GetEmpty()) : TextObject.GetEmpty());
			focusableObjectInformation.IsActive = true;
		}
		else
		{
			focusableObjectInformation.IsActive = false;
		}
	}

	private UsableMachine GetUsableMachineFromPoint(UsableMissionObject standingPoint)
	{
		WeakGameEntity weakGameEntity = standingPoint.GameEntity;
		while (weakGameEntity.IsValid && !weakGameEntity.HasScriptOfType<UsableMachine>())
		{
			weakGameEntity = weakGameEntity.Parent;
		}
		UsableMachine result = null;
		if (weakGameEntity.IsValid)
		{
			result = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
		}
		return result;
	}

	private void GetDestructibleComponentTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (!(focusableObject is DestructableComponent destructableComponent))
		{
			focusableObjectInformation.IsActive = false;
			return;
		}
		TextObject descriptionText = destructableComponent.GetDescriptionText(destructableComponent.GameEntity);
		bool flag = !TextObject.IsNullOrEmpty(descriptionText);
		focusableObjectInformation.PrimaryInteractionText = (flag ? descriptionText : TextObject.GetEmpty());
		focusableObjectInformation.SecondaryInteractionText = TextObject.GetEmpty();
		focusableObjectInformation.IsActive = flag;
	}

	private void GetUsableMachineTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (!(focusableObject is UsableMachine usableMachine))
		{
			focusableObjectInformation.IsActive = false;
			return;
		}
		focusableObjectInformation.PrimaryInteractionText = usableMachine.GetDescriptionText(usableMachine.GameEntity) ?? TextObject.GetEmpty();
		focusableObjectInformation.SecondaryInteractionText = TextObject.GetEmpty();
		focusableObjectInformation.IsActive = true;
	}

	private void GetHumanAgentTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (focusableObject is Agent { IsHuman: not false } agent)
		{
			Mission current = Mission.Current;
			if (isInteractable && (current.Mode == MissionMode.StartUp || current.Mode == MissionMode.Duel || current.Mode == MissionMode.Battle || current.Mode == MissionMode.Stealth) && agent.IsHuman)
			{
				MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
				if (agent.IsActive())
				{
					if (current.Mode == MissionMode.Duel || current.Mode == MissionMode.Battle)
					{
						focusableObjectInformation.PrimaryInteractionText = agent.NameTextObject;
						focusableObjectInformation.IsActive = true;
						return;
					}
					if (agent.IsEnemyOf(requesterAgent))
					{
						focusableObjectInformation.PrimaryInteractionText = agent.NameTextObject;
						focusableObjectInformation.IsActive = true;
						return;
					}
					if (!Mission.Current.IsAgentInteractionAllowed())
					{
						focusableObjectInformation.IsActive = false;
						return;
					}
					focusableObjectInformation.PrimaryInteractionText = agent.NameTextObject;
					if (CanInteractWithAgent(requesterAgent, agent))
					{
						focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action");
						focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
						focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", GameTexts.FindText("str_ui_talk"));
					}
					focusableObjectInformation.IsActive = true;
					return;
				}
				if (current.Mode != MissionMode.Battle && current.Mode != MissionMode.Duel)
				{
					focusableObjectInformation.PrimaryInteractionText = agent.NameTextObject;
					focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action");
					focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
					focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", GameTexts.FindText("str_ui_search"));
					focusableObjectInformation.IsActive = true;
					return;
				}
			}
			focusableObjectInformation.IsActive = false;
		}
		else
		{
			focusableObjectInformation.IsActive = false;
		}
	}

	private bool CanInteractWithAgent(Agent requesterAgent, Agent focusedAgent)
	{
		if (!requesterAgent.CanInteractWithAgent(focusedAgent, -1000f))
		{
			return requesterAgent.IsEnemyOf(focusedAgent);
		}
		return true;
	}

	private void GetMountTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (focusableObject is Agent { IsMount: not false } agent)
		{
			if (agent.IsActive() && agent.IsMount)
			{
				string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13));
				focusableObjectInformation.PrimaryInteractionText = agent.NameTextObject;
				if (agent.RiderAgent == null)
				{
					if ((float)requesterAgent.Character.GetSkillValue(DefaultSkills.Riding) < agent.GetAgentDrivenPropertyValue(DrivenProperty.MountDifficulty))
					{
						focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_ui_riding_skill_not_adequate_to_mount");
					}
					else if ((requesterAgent.GetAgentFlags() & AgentFlag.CanRide) != AgentFlag.None)
					{
						focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action");
						focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", keyHyperlinkText);
						focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", GameTexts.FindText("str_ui_mount"));
					}
				}
				else if (agent.RiderAgent == requesterAgent)
				{
					focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action");
					focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", keyHyperlinkText);
					focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", GameTexts.FindText("str_ui_dismount"));
				}
				focusableObjectInformation.IsActive = true;
			}
			else
			{
				focusableObjectInformation.IsActive = false;
			}
		}
		else
		{
			focusableObjectInformation.IsActive = false;
		}
	}

	private void GetGenericAgentTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (focusableObject is Agent { IsHuman: false, IsMount: false } agent)
		{
			if (agent.IsActive() && !agent.IsMount && !agent.IsHuman)
			{
				AgentComponent agentComponent = null;
				foreach (AgentComponent component in agent.Components)
				{
					if (component is IFocusable)
					{
						agentComponent = component;
						break;
					}
				}
				if (agentComponent != null)
				{
					focusableObjectInformation.PrimaryInteractionText = agent.NameTextObject;
					if (isInteractable)
					{
						focusableObjectInformation.SecondaryInteractionText = ((IFocusable)agentComponent).GetDescriptionText(WeakGameEntity.Invalid);
					}
					else
					{
						focusableObjectInformation.SecondaryInteractionText = ((IFocusable)agentComponent).GetInfoTextForBeingNotInteractable(requesterAgent);
					}
					focusableObjectInformation.IsActive = true;
					return;
				}
			}
			focusableObjectInformation.IsActive = false;
		}
		else
		{
			focusableObjectInformation.IsActive = false;
		}
	}
}
