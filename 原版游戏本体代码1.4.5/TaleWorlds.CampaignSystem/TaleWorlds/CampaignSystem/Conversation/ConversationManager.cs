using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.Conversation;

public class ConversationManager
{
	public class TaggedString
	{
		public TextObject Text;

		public List<GameTextManager.ChoiceTag> ChoiceTags = new List<GameTextManager.ChoiceTag>();

		public int FacialAnimation;
	}

	private int _currentRepeatedDialogSetIndex;

	private int _currentRepeatIndex;

	private int _autoId;

	private int _autoToken;

	private HashSet<int> _usedIndices = new HashSet<int>();

	private int _numConversationSentencesCreated;

	private List<ConversationSentence> _sentences;

	private int _numberOfStateIndices;

	public int ActiveToken;

	private int _currentSentence;

	private TextObject _currentSentenceText;

	public List<Tuple<string, CharacterObject>> DetailedDebugLog = new List<Tuple<string, CharacterObject>>();

	public string CurrentFaceAnimationRecord;

	private object _lastSelectedDialogObject;

	private readonly List<List<object>> _dialogRepeatObjects = new List<List<object>>();

	private readonly List<TextObject> _dialogRepeatLines = new List<TextObject>();

	private bool _isActive;

	private bool _executeDoOptionContinue;

	public int LastSelectedButtonIndex;

	public ConversationAnimationManager ConversationAnimationManager;

	private IAgent _mainAgent;

	private IAgent _speakerAgent;

	private IAgent _listenerAgent;

	private Dictionary<string, ConversationTag> _tags;

	private bool _sortSentenceIsDisabled;

	private Dictionary<string, int> stateMap;

	private List<IAgent> _conversationAgents = new List<IAgent>();

	public bool CurrentConversationIsFirst;

	private MobileParty _conversationParty;

	private static TaleWorlds.CampaignSystem.Conversation.Persuasion.Persuasion _persuasion;

	public string CurrentSentenceText
	{
		get
		{
			TextObject textObject = _currentSentenceText;
			if (OneToOneConversationCharacter != null)
			{
				textObject = FindMatchingTextOrNull(textObject.GetID(), OneToOneConversationCharacter);
				if (textObject == null)
				{
					textObject = _currentSentenceText;
				}
			}
			return MBTextManager.DiscardAnimationTagsAndCheckAnimationTagPositions(textObject.CopyTextObject().ToString());
		}
	}

	private int DialogRepeatCount
	{
		get
		{
			if (_dialogRepeatObjects.Count > 0)
			{
				return _dialogRepeatObjects[_currentRepeatedDialogSetIndex].Count;
			}
			return 1;
		}
	}

	public bool IsConversationFlowActive => _isActive;

	public List<ConversationSentenceOption> CurOptions { get; protected set; }

	public IReadOnlyList<IAgent> ConversationAgents => _conversationAgents;

	public IAgent OneToOneConversationAgent
	{
		get
		{
			if (ConversationAgents.IsEmpty() || ConversationAgents.Count > 1)
			{
				return null;
			}
			return ConversationAgents[0];
		}
	}

	public IAgent SpeakerAgent
	{
		get
		{
			if (ConversationAgents != null)
			{
				return _speakerAgent;
			}
			return null;
		}
	}

	public IAgent ListenerAgent
	{
		get
		{
			if (ConversationAgents != null)
			{
				return _listenerAgent;
			}
			return null;
		}
	}

	public bool IsConversationInProgress { get; private set; }

	public Hero OneToOneConversationHero
	{
		get
		{
			if (OneToOneConversationCharacter != null)
			{
				return OneToOneConversationCharacter.HeroObject;
			}
			return null;
		}
	}

	public CharacterObject OneToOneConversationCharacter
	{
		get
		{
			if (OneToOneConversationAgent != null)
			{
				return (CharacterObject)OneToOneConversationAgent.Character;
			}
			return null;
		}
	}

	public IEnumerable<CharacterObject> ConversationCharacters
	{
		get
		{
			new List<CharacterObject>();
			foreach (IAgent conversationAgent in ConversationAgents)
			{
				yield return (CharacterObject)conversationAgent.Character;
			}
		}
	}

	public MobileParty ConversationParty => _conversationParty;

	public bool NeedsToActivateForMapConversation { get; private set; }

	public IConversationStateHandler Handler { get; set; }

	public event Action<ConversationSentence> ConsequenceRunned;

	public event Action<ConversationSentence> ConditionRunned;

	public event Action<ConversationSentence> ClickableConditionRunned;

	public event Action ConversationSetup;

	public event Action ConversationBegin;

	public event Action ConversationEnd;

	public event Action ConversationEndOneShot;

	public event Action ConversationContinued;

	public int CreateConversationSentenceIndex()
	{
		int numConversationSentencesCreated = _numConversationSentencesCreated;
		_numConversationSentencesCreated++;
		return numConversationSentencesCreated;
	}

	public ConversationManager()
	{
		_sentences = new List<ConversationSentence>();
		stateMap = new Dictionary<string, int>();
		stateMap.Add("start", 0);
		stateMap.Add("event_triggered", 1);
		stateMap.Add("member_chat", 2);
		stateMap.Add("prisoner_chat", 3);
		stateMap.Add("close_window", 4);
		_numberOfStateIndices = 5;
		_isActive = false;
		_executeDoOptionContinue = false;
		InitializeTags();
		ConversationAnimationManager = new ConversationAnimationManager();
	}

	public void StartNew(int startingToken, bool setActionsInstantly)
	{
		_usedIndices.Clear();
		ActiveToken = startingToken;
		_currentSentence = -1;
		ResetRepeatedDialogSystem();
		_lastSelectedDialogObject = null;
		Debug.Print("--------------- Conversation Start --------------- ", 0, Debug.DebugColor.White, 4503599627370496uL);
		Debug.Print(string.Concat("Conversation character name: ", OneToOneConversationCharacter.Name, "\nid: ", OneToOneConversationCharacter.StringId, "\nculture:", OneToOneConversationCharacter.Culture, "\npersona:", OneToOneConversationCharacter.GetPersona().Name));
		_mainAgent.OnConversationStarted();
		if (CampaignMission.Current != null)
		{
			foreach (IAgent conversationAgent in ConversationAgents)
			{
				CampaignMission.Current.OnConversationStart(conversationAgent, setActionsInstantly);
			}
		}
		ProcessPartnerSentence();
	}

	private bool ProcessPartnerSentence()
	{
		List<ConversationSentenceOption> sentenceOptions = GetSentenceOptions(onlyPlayer: false, processAfterOneOption: false);
		bool result = false;
		if (sentenceOptions.Count > 0)
		{
			ProcessSentence(sentenceOptions[0]);
			result = true;
		}
		Handler?.OnConversationContinue();
		return result;
	}

	public void ProcessSentence(ConversationSentenceOption conversationSentenceOption)
	{
		ConversationSentence conversationSentence = _sentences[conversationSentenceOption.SentenceNo];
		Debug.Print(conversationSentenceOption.DebugInfo, 0, Debug.DebugColor.White, 4503599627370496uL);
		ActiveToken = conversationSentence.OutputToken;
		UpdateSpeakerAndListenerAgents(conversationSentence);
		if (CampaignMission.Current != null)
		{
			CampaignMission.Current.OnProcessSentence();
		}
		_lastSelectedDialogObject = conversationSentenceOption.RepeatObject;
		_currentSentence = conversationSentenceOption.SentenceNo;
		if (Game.Current == null)
		{
			throw new MBNullParameterException("Game");
		}
		UpdateCurrentSentenceText();
		int count = _sentences.Count;
		conversationSentence.RunConsequence(Game.Current);
		if (conversationSentence.IsUsedOnce)
		{
			_usedIndices.Add(conversationSentence.Index);
		}
		if (CampaignMission.Current != null)
		{
			string[] conversationAnimations = MBTextManager.GetConversationAnimations(_currentSentenceText);
			string soundPath = "";
			if (MBTextManager.TryGetVoiceObject(_currentSentenceText, out var vo, out var _))
			{
				soundPath = Campaign.Current.Models.VoiceOverModel.GetSoundPathForCharacter((CharacterObject)SpeakerAgent.Character, vo);
			}
			CampaignMission.Current.OnConversationPlay(conversationAnimations[0], conversationAnimations[1], conversationAnimations[2], conversationAnimations[3], soundPath);
		}
		if (0 > _currentSentence || _currentSentence >= count)
		{
			Debug.FailedAssert("CurrentSentence is not valid.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs", "ProcessSentence", 417);
		}
	}

	private void UpdateSpeakerAndListenerAgents(ConversationSentence sentence)
	{
		if (sentence.IsSpeaker != null)
		{
			if (sentence.IsSpeaker(_mainAgent))
			{
				SetSpeakerAgent(_mainAgent);
			}
			else
			{
				foreach (IAgent conversationAgent in ConversationAgents)
				{
					if (sentence.IsSpeaker(conversationAgent))
					{
						SetSpeakerAgent(conversationAgent);
						break;
					}
				}
			}
		}
		else
		{
			SetSpeakerAgent((!sentence.IsPlayer) ? ConversationAgents[0] : _mainAgent);
		}
		if (sentence.IsListener != null)
		{
			if (!sentence.IsListener(_mainAgent))
			{
				foreach (IAgent conversationAgent2 in ConversationAgents)
				{
					if (sentence.IsListener(conversationAgent2))
					{
						SetListenerAgent(conversationAgent2);
						break;
					}
				}
				return;
			}
			SetListenerAgent(_mainAgent);
		}
		else
		{
			SetListenerAgent((!sentence.IsPlayer) ? _mainAgent : ConversationAgents[0]);
		}
	}

	private void SetSpeakerAgent(IAgent agent)
	{
		if (_speakerAgent != agent)
		{
			_speakerAgent = agent;
			if (_speakerAgent != null && _speakerAgent.Character is CharacterObject)
			{
				StringHelpers.SetCharacterProperties("SPEAKER", agent.Character as CharacterObject);
			}
		}
	}

	private void SetListenerAgent(IAgent agent)
	{
		if (_listenerAgent != agent)
		{
			_listenerAgent = agent;
			if (_listenerAgent != null && _listenerAgent.Character is CharacterObject)
			{
				StringHelpers.SetCharacterProperties("LISTENER", agent.Character as CharacterObject);
			}
		}
	}

	public void UpdateCurrentSentenceText()
	{
		TextObject currentSentenceText;
		if (_currentSentence >= 0)
		{
			currentSentenceText = _sentences[_currentSentence].Text;
		}
		else
		{
			if (Campaign.Current == null)
			{
				throw new MBNullParameterException("Campaign");
			}
			currentSentenceText = GameTexts.FindText("str_error_string");
		}
		_currentSentenceText = currentSentenceText;
	}

	public bool IsConversationEnded()
	{
		return ActiveToken == 4;
	}

	public void ClearCurrentOptions()
	{
		if (CurOptions == null)
		{
			CurOptions = new List<ConversationSentenceOption>();
		}
		CurOptions.Clear();
	}

	public void AddToCurrentOptions(TextObject text, string id, bool isClickable, TextObject hintText)
	{
		ConversationSentenceOption item = new ConversationSentenceOption
		{
			SentenceNo = 0,
			Text = text,
			Id = id,
			RepeatObject = null,
			DebugInfo = null,
			IsClickable = isClickable,
			HintText = hintText
		};
		CurOptions.Add(item);
	}

	public void GetPlayerSentenceOptions()
	{
		CurOptions = GetSentenceOptions(onlyPlayer: true, processAfterOneOption: true);
		if (CurOptions.Count <= 0)
		{
			return;
		}
		ConversationSentenceOption conversationSentenceOption = CurOptions[0];
		foreach (ConversationSentenceOption curOption in CurOptions)
		{
			if (_sentences[curOption.SentenceNo].IsListener != null)
			{
				conversationSentenceOption = curOption;
				break;
			}
		}
		UpdateSpeakerAndListenerAgents(_sentences[conversationSentenceOption.SentenceNo]);
	}

	public int GetStateIndex(string str)
	{
		int result;
		if (stateMap.ContainsKey(str))
		{
			result = stateMap[str];
		}
		else
		{
			result = _numberOfStateIndices;
			stateMap.Add(str, _numberOfStateIndices++);
		}
		return result;
	}

	internal void Build()
	{
		SortSentences();
	}

	public void DisableSentenceSort()
	{
		_sortSentenceIsDisabled = true;
	}

	public void EnableSentenceSort()
	{
		_sortSentenceIsDisabled = false;
		SortSentences();
	}

	private void SortSentences()
	{
		_sentences = _sentences.OrderByDescending((ConversationSentence pair) => pair.Priority).ToList();
	}

	private void SortLastSentence()
	{
		int num = _sentences.Count - 1;
		ConversationSentence conversationSentence = _sentences[num];
		int priority = conversationSentence.Priority;
		int num2 = num - 1;
		while (num2 >= 0 && _sentences[num2].Priority < priority)
		{
			_sentences[num2 + 1] = _sentences[num2];
			num = num2;
			num2--;
		}
		_sentences[num] = conversationSentence;
		if (CurOptions == null)
		{
			return;
		}
		for (int i = 0; i < CurOptions.Count; i++)
		{
			if (CurOptions[i].SentenceNo >= num)
			{
				ConversationSentenceOption value = CurOptions[i];
				value.SentenceNo = CurOptions[i].SentenceNo + 1;
				CurOptions[i] = value;
			}
		}
	}

	private List<ConversationSentenceOption> GetSentenceOptions(bool onlyPlayer, bool processAfterOneOption)
	{
		List<ConversationSentenceOption> list = new List<ConversationSentenceOption>();
		SetupTextVariables();
		for (int i = 0; i < _sentences.Count; i++)
		{
			if (!GetSentenceMatch(i, onlyPlayer))
			{
				continue;
			}
			ConversationSentence conversationSentence = _sentences[i];
			int num = 1;
			_dialogRepeatLines.Clear();
			_currentRepeatIndex = 0;
			if (conversationSentence.IsRepeatable)
			{
				num = DialogRepeatCount;
			}
			for (int j = 0; j < num; j++)
			{
				_dialogRepeatLines.Add(conversationSentence.Text.CopyTextObject());
				if (conversationSentence.RunCondition())
				{
					conversationSentence.IsClickable = conversationSentence.RunClickableCondition();
					if (conversationSentence.IsWithVariation)
					{
						TextObject content = FindMatchingTextOrNull(conversationSentence.Id, OneToOneConversationCharacter);
						GameTexts.SetVariable("VARIATION_TEXT_TAGGED_LINE", content);
					}
					string text = "";
					text = (conversationSentence.IsPlayer ? "P  -> (" : "AI -> (") + conversationSentence.Id + ") - ";
					ConversationSentenceOption item = new ConversationSentenceOption
					{
						SentenceNo = i,
						Text = GetCurrentDialogLine(),
						Id = conversationSentence.Id,
						RepeatObject = GetCurrentProcessedRepeatObject(),
						DebugInfo = text,
						IsClickable = conversationSentence.IsClickable,
						HasPersuasion = conversationSentence.HasPersuasion,
						SkillName = conversationSentence.SkillName,
						TraitName = conversationSentence.TraitName,
						IsSpecial = conversationSentence.IsSpecial,
						IsUsedOnce = conversationSentence.IsUsedOnce,
						HintText = conversationSentence.HintText,
						PersuationOptionArgs = conversationSentence.PersuationOptionArgs
					};
					list.Add(item);
					if (conversationSentence.IsRepeatable)
					{
						_currentRepeatIndex++;
					}
					if (!processAfterOneOption)
					{
						return list;
					}
				}
			}
		}
		return list;
	}

	private bool GetSentenceMatch(int sentenceIndex, bool onlyPlayer)
	{
		if (0 > sentenceIndex || sentenceIndex >= _sentences.Count)
		{
			throw new MBOutOfRangeException("Sentence index is not valid.");
		}
		bool flag = _sentences[sentenceIndex].InputToken != ActiveToken;
		if (!flag && onlyPlayer)
		{
			flag = !_sentences[sentenceIndex].IsPlayer;
		}
		if (!flag)
		{
			flag = _sentences[sentenceIndex].IsUsedOnce && _usedIndices.Contains(_sentences[sentenceIndex].Index);
		}
		return !flag;
	}

	internal object GetCurrentProcessedRepeatObject()
	{
		if (_dialogRepeatObjects.Count <= 0)
		{
			return null;
		}
		return _dialogRepeatObjects[_currentRepeatedDialogSetIndex][_currentRepeatIndex];
	}

	internal TextObject GetCurrentDialogLine()
	{
		if (_dialogRepeatLines.Count <= _currentRepeatIndex)
		{
			return null;
		}
		return _dialogRepeatLines[_currentRepeatIndex];
	}

	internal object GetSelectedRepeatObject()
	{
		return _lastSelectedDialogObject;
	}

	internal void SetDialogRepeatCount(IReadOnlyList<object> dialogRepeatObjects, int maxRepeatedDialogsInConversation)
	{
		_dialogRepeatObjects.Clear();
		bool flag = dialogRepeatObjects.Count > maxRepeatedDialogsInConversation + 1;
		List<object> list = new List<object>(maxRepeatedDialogsInConversation);
		for (int i = 0; i < dialogRepeatObjects.Count; i++)
		{
			object item = dialogRepeatObjects[i];
			if (flag && i % maxRepeatedDialogsInConversation == 0)
			{
				list = new List<object>(maxRepeatedDialogsInConversation);
				_dialogRepeatObjects.Add(list);
			}
			list.Add(item);
		}
		if (!flag && !list.IsEmpty())
		{
			_dialogRepeatObjects.Add(list);
		}
		_currentRepeatedDialogSetIndex = 0;
		_currentRepeatIndex = 0;
	}

	internal static void DialogRepeatContinueListing()
	{
		ConversationManager conversationManager = Campaign.Current?.ConversationManager;
		if (conversationManager != null)
		{
			conversationManager._currentRepeatedDialogSetIndex++;
			if (conversationManager._currentRepeatedDialogSetIndex >= conversationManager._dialogRepeatObjects.Count)
			{
				conversationManager._currentRepeatedDialogSetIndex = 0;
			}
			conversationManager._currentRepeatIndex = 0;
		}
	}

	internal static bool IsThereMultipleRepeatablePages()
	{
		Campaign current = Campaign.Current;
		if (current == null)
		{
			return false;
		}
		return current.ConversationManager?._dialogRepeatObjects.Count > 1;
	}

	private void ResetRepeatedDialogSystem()
	{
		_currentRepeatedDialogSetIndex = 0;
		_currentRepeatIndex = 0;
		_dialogRepeatObjects.Clear();
		_dialogRepeatLines.Clear();
	}

	internal ConversationSentence AddDialogLine(ConversationSentence dialogLine)
	{
		_sentences.Add(dialogLine);
		if (!_sortSentenceIsDisabled)
		{
			SortLastSentence();
		}
		return dialogLine;
	}

	public void AddDialogFlow(DialogFlow dialogFlow, object relatedObject = null)
	{
		foreach (DialogFlowLine line in dialogFlow.Lines)
		{
			string text = CreateId();
			uint flags = (uint)((line.ByPlayer ? 1 : 0) | (line.IsRepeatable ? 2 : 0) | (line.IsSpecialOption ? 4 : 0) | (line.IsUsedOnce ? 8 : 0));
			AddDialogLine(new ConversationSentence(text, line.HasVariation ? new TextObject("{=!}{VARIATION_TEXT_TAGGED_LINE}") : line.Text, line.InputToken, line.OutputToken, line.ConditionDelegate, line.ClickableConditionDelegate, line.ConsequenceDelegate, flags, dialogFlow.Priority, 0, 0, relatedObject, line.HasVariation, line.SpeakerDelegate, line.ListenerDelegate));
			GameText gameText = Game.Current.GameTextManager.AddGameText(text);
			foreach (KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>> variation in line.Variations)
			{
				gameText.AddVariationWithId("", variation.Key, variation.Value);
			}
		}
	}

	public ConversationSentence AddDialogLineMultiAgent(string id, string inputToken, string outputToken, TextObject text, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, int agentIndex, int nextAgentIndex, int priority = 100, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
	{
		return AddDialogLine(new ConversationSentence(id, text, inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, 0u, priority, agentIndex, nextAgentIndex));
	}

	internal string CreateToken()
	{
		string result = $"atk:{_autoToken}";
		_autoToken++;
		return result;
	}

	private string CreateId()
	{
		string result = $"adg:{_autoId}";
		_autoId++;
		return result;
	}

	internal void SetupGameStringsForConversation()
	{
		StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
	}

	internal void OnConsequence(ConversationSentence sentence)
	{
		this.ConsequenceRunned?.Invoke(sentence);
	}

	internal void OnCondition(ConversationSentence sentence)
	{
		this.ConditionRunned?.Invoke(sentence);
	}

	internal void OnClickableCondition(ConversationSentence sentence)
	{
		this.ClickableConditionRunned?.Invoke(sentence);
	}

	public bool IsAgentInConversation(IAgent agent)
	{
		return ConversationAgents.Contains(agent);
	}

	private void SetupConversation()
	{
		IsConversationInProgress = true;
		Handler?.OnConversationInstall();
	}

	public void BeginConversation()
	{
		IsConversationInProgress = true;
		if (this.ConversationSetup != null)
		{
			this.ConversationSetup();
		}
		if (this.ConversationBegin != null)
		{
			this.ConversationBegin();
		}
		NeedsToActivateForMapConversation = false;
	}

	public void EndConversation()
	{
		Debug.Print("--------------- Conversation End --------------- ", 0, Debug.DebugColor.White, 4503599627370496uL);
		if (CampaignMission.Current != null)
		{
			foreach (IAgent conversationAgent in ConversationAgents)
			{
				CampaignMission.Current.OnConversationEnd(conversationAgent);
			}
		}
		_conversationParty = null;
		if (this.ConversationEndOneShot != null)
		{
			this.ConversationEndOneShot();
			this.ConversationEndOneShot = null;
		}
		if (this.ConversationEnd != null)
		{
			this.ConversationEnd();
		}
		IsConversationInProgress = false;
		foreach (IAgent conversationAgent2 in ConversationAgents)
		{
			conversationAgent2.SetAsConversationAgent(set: false);
		}
		Campaign.Current.CurrentConversationContext = ConversationContext.Default;
		CampaignEventDispatcher.Instance.OnConversationEnded(ConversationCharacters);
		if (GetPersuasionIsActive())
		{
			EndPersuasion();
		}
		_conversationAgents.Clear();
		_speakerAgent = null;
		_listenerAgent = null;
		_mainAgent = null;
		if (IsConversationFlowActive)
		{
			OnConversationDeactivate();
		}
		Handler?.OnConversationUninstall();
	}

	public void DoOption(int optionIndex)
	{
		LastSelectedButtonIndex = optionIndex;
		ProcessSentence(CurOptions[optionIndex]);
		if (_isActive)
		{
			DoOptionContinue();
		}
		else
		{
			_executeDoOptionContinue = true;
		}
	}

	public void DoOption(string optionID)
	{
		int count = Campaign.Current.ConversationManager.CurOptions.Count;
		for (int i = 0; i < count; i++)
		{
			if (CurOptions[i].Id == optionID)
			{
				DoOption(i);
				break;
			}
		}
	}

	public void DoConversationContinuedCallback()
	{
		if (this.ConversationContinued != null)
		{
			this.ConversationContinued();
		}
	}

	public void DoOptionContinue()
	{
		if (IsConversationEnded() && _sentences[_currentSentence].IsPlayer)
		{
			EndConversation();
			return;
		}
		ProcessPartnerSentence();
		DoConversationContinuedCallback();
	}

	public void ContinueConversation()
	{
		if (CurOptions.Count > 1)
		{
			return;
		}
		if (IsConversationEnded())
		{
			EndConversation();
			return;
		}
		if (!ProcessPartnerSentence() && ListenerAgent.Character == Hero.MainHero.CharacterObject)
		{
			EndConversation();
			return;
		}
		DoConversationContinuedCallback();
		if (CampaignMission.Current != null)
		{
			CampaignMission.Current.OnConversationContinue();
		}
	}

	public void SetupAndStartMissionConversation(IAgent agent, IAgent mainAgent, bool setActionsInstantly)
	{
		SetupConversation();
		_mainAgent = mainAgent;
		_conversationAgents.Clear();
		AddConversationAgent(agent);
		_conversationParty = null;
		StartNew(0, setActionsInstantly);
		if (!IsConversationFlowActive)
		{
			OnConversationActivate();
		}
		BeginConversation();
	}

	public void SetupAndStartMissionConversationWithMultipleAgents(IEnumerable<IAgent> agents, IAgent mainAgent)
	{
		SetupConversation();
		_mainAgent = mainAgent;
		_conversationAgents.Clear();
		AddConversationAgents(agents, setActionsInstantly: true);
		_conversationParty = null;
		StartNew(0, setActionsInstantly: true);
		if (!IsConversationFlowActive)
		{
			OnConversationActivate();
		}
		BeginConversation();
	}

	public void SetupAndStartMapConversation(MobileParty party, IAgent agent, IAgent mainAgent)
	{
		_conversationParty = party;
		_mainAgent = mainAgent;
		_conversationAgents.Clear();
		AddConversationAgent(agent);
		SetupConversation();
		StartNew(0, setActionsInstantly: true);
		NeedsToActivateForMapConversation = true;
		if (!IsConversationFlowActive)
		{
			OnConversationActivate();
		}
	}

	public void AddConversationAgents(IEnumerable<IAgent> agents, bool setActionsInstantly)
	{
		foreach (IAgent agent in agents)
		{
			if (agent.IsActive() && !ConversationAgents.Contains(agent))
			{
				AddConversationAgent(agent);
				CampaignMission.Current.OnConversationStart(agent, setActionsInstantly);
			}
		}
	}

	public void RemoveConversationAgent(IAgent agent)
	{
		if (agent.IsActive() && ConversationAgents.Contains(agent) && ConversationAgents.Count > 1)
		{
			CampaignMission.Current.OnConversationEnd(agent);
			agent.SetAsConversationAgent(set: false);
			_conversationAgents.Remove(agent);
		}
		else
		{
			Debug.FailedAssert("Failed to remove conversation agent.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs", "RemoveConversationAgent", 1249);
		}
	}

	private void AddConversationAgent(IAgent agent)
	{
		_conversationAgents.Add(agent);
		agent.SetAsConversationAgent(set: true);
		CampaignEventDispatcher.Instance.OnAgentJoinedConversation(agent);
		agent.OnConversationStarted();
	}

	public bool IsConversationAgent(IAgent agent)
	{
		if (ConversationAgents != null)
		{
			return ConversationAgents.Contains(agent);
		}
		return false;
	}

	public void RemoveRelatedLines(object o)
	{
		_sentences.RemoveAll((ConversationSentence s) => s.RelatedObject == o);
	}

	public void OnConversationDeactivate()
	{
		_isActive = false;
		Handler?.OnConversationDeactivate();
	}

	public void OnConversationActivate()
	{
		_isActive = true;
		if (_executeDoOptionContinue)
		{
			_executeDoOptionContinue = false;
			DoOptionContinue();
		}
		Handler?.OnConversationActivate();
	}

	public TextObject FindMatchingTextOrNull(string id, CharacterObject character)
	{
		float num = -2.1474836E+09f;
		TextObject result = null;
		GameText gameText = Game.Current.GameTextManager.GetGameText(id);
		if (gameText != null)
		{
			foreach (GameText.GameTextVariation variation in gameText.Variations)
			{
				float num2 = FindMatchingScore(character, variation.Tags);
				if (num2 > num)
				{
					result = variation.Text;
					num = num2;
				}
			}
		}
		return result;
	}

	private float FindMatchingScore(CharacterObject character, GameTextManager.ChoiceTag[] choiceTags)
	{
		float num = 0f;
		for (int i = 0; i < choiceTags.Length; i++)
		{
			GameTextManager.ChoiceTag choiceTag = choiceTags[i];
			if (choiceTag.TagName != "DefaultTag")
			{
				if (IsTagApplicable(choiceTag.TagName, character) == choiceTag.IsTagReversed)
				{
					return -2.1474836E+09f;
				}
				uint weight = choiceTag.Weight;
				num += (float)weight;
			}
		}
		return num;
	}

	private void InitializeTags()
	{
		_tags = new Dictionary<string, ConversationTag>();
		string name = typeof(ConversationTag).Assembly.GetName().Name;
		foreach (Assembly activeGameAssembly in ModuleHelper.GetActiveGameAssemblies())
		{
			bool flag = false;
			if (name == activeGameAssembly.GetName().Name)
			{
				flag = true;
			}
			else
			{
				AssemblyName[] referencedAssemblies = activeGameAssembly.GetReferencedAssemblies();
				for (int i = 0; i < referencedAssemblies.Length; i++)
				{
					if (referencedAssemblies[i].Name == name)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				continue;
			}
			foreach (Type item in activeGameAssembly.GetTypesSafe())
			{
				if (item.IsSubclassOf(typeof(ConversationTag)))
				{
					ConversationTag conversationTag = Activator.CreateInstance(item) as ConversationTag;
					_tags.Add(conversationTag.StringId, conversationTag);
				}
			}
		}
	}

	private static void SetupTextVariables()
	{
		StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
		int num = 1;
		foreach (CharacterObject conversationCharacter in CharacterObject.ConversationCharacters)
		{
			string text = ((num == 1) ? "" : ("_" + num));
			StringHelpers.SetCharacterProperties("CONVERSATION_CHARACTER" + text, conversationCharacter);
		}
		MBTextManager.SetTextVariable("CURRENT_SETTLEMENT_NAME", (Settlement.CurrentSettlement == null) ? TextObject.GetEmpty() : Settlement.CurrentSettlement.Name);
		ConversationHelper.ConversationTroopCommentShown = false;
	}

	public IEnumerable<string> GetApplicableTagNames(CharacterObject character)
	{
		foreach (ConversationTag value in _tags.Values)
		{
			if (value.IsApplicableTo(character))
			{
				yield return value.StringId;
			}
		}
	}

	public bool IsTagApplicable(string tagId, CharacterObject character)
	{
		if (_tags.TryGetValue(tagId, out var value))
		{
			return value.IsApplicableTo(character);
		}
		Debug.FailedAssert("Asking for a nonexistent tag: " + tagId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs", "IsTagApplicable", 1486);
		return false;
	}

	public void OpenMapConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
	{
		(GameStateManager.Current?.ActiveState as MapState).OnMapConversationStarts(playerCharacterData, conversationPartnerData);
		SetupAndStartMapConversation(conversationPartnerData.Party?.MobileParty, new MapConversationAgent(conversationPartnerData.Character), new MapConversationAgent(CharacterObject.PlayerCharacter));
	}

	public static void StartPersuasion(float goalValue, float successValue, float failValue, float criticalSuccessValue, float criticalFailValue, float initialProgress = -1f, PersuasionDifficulty difficulty = PersuasionDifficulty.Medium)
	{
		_persuasion = new TaleWorlds.CampaignSystem.Conversation.Persuasion.Persuasion(goalValue, successValue, failValue, criticalSuccessValue, criticalFailValue, initialProgress, difficulty);
	}

	public static void EndPersuasion()
	{
		_persuasion = null;
	}

	public static void PersuasionCommitProgress(PersuasionOptionArgs persuasionOptionArgs)
	{
		_persuasion.CommitProgress(persuasionOptionArgs);
	}

	public static void Clear()
	{
		_persuasion = null;
	}

	public void GetPersuasionChanceValues(out float successValue, out float critSuccessValue, out float critFailValue)
	{
		successValue = _persuasion.SuccessValue;
		critSuccessValue = _persuasion.CriticalSuccessValue;
		critFailValue = _persuasion.CriticalFailValue;
	}

	public static bool GetPersuasionIsActive()
	{
		return _persuasion != null;
	}

	public static bool GetPersuasionProgressSatisfied()
	{
		return _persuasion.Progress >= _persuasion.GoalValue;
	}

	public static bool GetPersuasionIsFailure()
	{
		return _persuasion.Progress < 0f;
	}

	public static float GetPersuasionProgress()
	{
		return _persuasion.Progress;
	}

	public static float GetPersuasionGoalValue()
	{
		return _persuasion.GoalValue;
	}

	public static IEnumerable<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> GetPersuasionChosenOptions()
	{
		return _persuasion.GetChosenOptions();
	}

	public void GetPersuasionChances(ConversationSentenceOption conversationSentenceOption, out float successChance, out float critSuccessChance, out float critFailChance, out float failChance)
	{
		ConversationSentence conversationSentence = _sentences[conversationSentenceOption.SentenceNo];
		if (conversationSentenceOption.HasPersuasion)
		{
			Campaign.Current.Models.PersuasionModel.GetChances(conversationSentence.PersuationOptionArgs, out successChance, out critSuccessChance, out critFailChance, out failChance, _persuasion.DifficultyMultiplier);
			return;
		}
		successChance = 0f;
		critSuccessChance = 0f;
		critFailChance = 0f;
		failChance = 0f;
	}
}
