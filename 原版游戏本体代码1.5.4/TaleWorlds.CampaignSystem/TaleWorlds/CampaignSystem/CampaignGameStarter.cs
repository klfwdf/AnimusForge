using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public class CampaignGameStarter : IGameStarter
{
	private readonly GameMenuManager _gameMenuManager;

	private readonly ConversationManager _conversationManager;

	private readonly List<CampaignBehaviorBase> _campaignBehaviors = new List<CampaignBehaviorBase>();

	private readonly List<GameModel> _models = new List<GameModel>();

	public ICollection<CampaignBehaviorBase> CampaignBehaviors => _campaignBehaviors;

	public IEnumerable<GameModel> Models => _models;

	public CampaignGameStarter(GameMenuManager gameMenuManager, ConversationManager conversationManager)
	{
		_conversationManager = conversationManager;
		_gameMenuManager = gameMenuManager;
	}

	public void UnregisterNonReadyObjects()
	{
		Game.Current.ObjectManager.UnregisterNonReadyObjects();
		_gameMenuManager.UnregisterNonReadyObjects();
	}

	public void AddBehavior(CampaignBehaviorBase campaignBehavior)
	{
		if (campaignBehavior != null)
		{
			_campaignBehaviors.Add(campaignBehavior);
		}
	}

	public void RemoveBehaviors<T>() where T : CampaignBehaviorBase
	{
		for (int num = _campaignBehaviors.Count - 1; num >= 0; num--)
		{
			if (_campaignBehaviors[num] is T)
			{
				_campaignBehaviors.RemoveAt(num);
			}
		}
	}

	public bool RemoveBehavior<T>(T behavior) where T : CampaignBehaviorBase
	{
		return _campaignBehaviors.Remove(behavior);
	}

	public T GetModel<T>() where T : GameModel
	{
		for (int num = _models.Count - 1; num >= 0; num--)
		{
			if (_models[num] is T result)
			{
				return result;
			}
		}
		return null;
	}

	public void AddModel(GameModel gameModel)
	{
		_models.Add(gameModel);
	}

	public void AddModel<T>(MBGameModel<T> gameModel) where T : GameModel
	{
		T model = GetModel<T>();
		gameModel.Initialize(model);
		_models.Add(gameModel);
	}

	public void AddGameMenu(string menuId, string menuText, OnInitDelegate initDelegate, GameMenu.MenuOverlayType overlay = GameMenu.MenuOverlayType.None, GameMenu.MenuFlags menuFlags = GameMenu.MenuFlags.None, object relatedObject = null)
	{
		GetPresumedGameMenu(menuId).Initialize(new TextObject(menuText), initDelegate, overlay, menuFlags, relatedObject);
	}

	public void AddWaitGameMenu(string idString, string text, OnInitDelegate initDelegate, OnConditionDelegate condition, OnConsequenceDelegate consequence, OnTickDelegate tick, GameMenu.MenuAndOptionType type, GameMenu.MenuOverlayType overlay = GameMenu.MenuOverlayType.None, float targetWaitHours = 0f, GameMenu.MenuFlags flags = GameMenu.MenuFlags.None, object relatedObject = null)
	{
		GetPresumedGameMenu(idString).Initialize(new TextObject(text), initDelegate, condition, consequence, tick, type, overlay, targetWaitHours, flags, relatedObject);
	}

	public void AddGameMenuOption(string menuId, string optionId, string optionText, GameMenuOption.OnConditionDelegate condition, GameMenuOption.OnConsequenceDelegate consequence, bool isLeave = false, int index = -1, bool isRepeatable = false, object relatedObject = null)
	{
		GetPresumedGameMenu(menuId).AddOption(optionId, new TextObject(optionText), condition, consequence, index, isLeave, isRepeatable, relatedObject);
	}

	public GameMenu GetPresumedGameMenu(string stringId)
	{
		GameMenu gameMenu = _gameMenuManager.GetGameMenu(stringId);
		if (gameMenu == null)
		{
			gameMenu = new GameMenu(stringId);
			_gameMenuManager.AddGameMenu(gameMenu);
		}
		return gameMenu;
	}

	private ConversationSentence AddDialogLine(ConversationSentence dialogLine)
	{
		_conversationManager.AddDialogLine(dialogLine);
		return dialogLine;
	}

	public void AddDialogFlow(DialogFlow dialogFlow, object relatedObject = null)
	{
		_conversationManager.AddDialogFlow(dialogFlow, relatedObject);
	}

	public ConversationSentence AddPlayerLine(string id, string inputToken, string outputToken, string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null, ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = null)
	{
		return AddDialogLine(new ConversationSentence(id, new TextObject(text), inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 1u, priority, 0, 0, null, withVariation: false, null, null, persuasionOptionDelegate));
	}

	public ConversationSentence AddRepeatablePlayerLine(string id, string inputToken, string outputToken, string text, string continueListingRepeatedObjectsText, string continueListingOptionOutputToken, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
	{
		ConversationSentence result = AddDialogLine(new ConversationSentence(id, new TextObject(text), inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 3u, priority));
		AddDialogLine(new ConversationSentence(id + "_continue", new TextObject(continueListingRepeatedObjectsText), inputToken, continueListingOptionOutputToken, ConversationManager.IsThereMultipleRepeatablePages, null, ConversationManager.DialogRepeatContinueListing, 1u, priority));
		return result;
	}

	public ConversationSentence AddDialogLineWithVariation(string id, string inputToken, string outputToken, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int priority = 100, string idleActionId = "", string idleFaceAnimId = "", string reactionId = "", string reactionFaceAnimId = "", ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
	{
		return AddDialogLine(new ConversationSentence(id, new TextObject("{=!}{VARIATION_TEXT_TAGGED_LINE}"), inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 0u, priority, 0, 0, null, withVariation: true));
	}

	public ConversationSentence AddDialogLine(string id, string inputToken, string outputToken, string text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
	{
		return AddDialogLine(new ConversationSentence(id, new TextObject(text), inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 0u, priority));
	}

	public ConversationSentence AddDialogLineMultiAgent(string id, string inputToken, string outputToken, TextObject text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int agentIndex, int nextAgentIndex, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
	{
		return AddDialogLine(new ConversationSentence(id, text, inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 0u, priority, agentIndex, nextAgentIndex));
	}
}
