using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation;

public class ConversationSentence
{
	public enum DialogLineFlags
	{
		PlayerLine = 1,
		RepeatForObjects = 2,
		SpecialLine = 4,
		UsedOnce = 8
	}

	public delegate bool OnConditionDelegate();

	public delegate bool OnClickableConditionDelegate(out TextObject explanation);

	public delegate PersuasionOptionArgs OnPersuasionOptionDelegate();

	public delegate void OnConsequenceDelegate();

	public delegate bool OnMultipleConversationConsequenceDelegate(IAgent agent);

	public const int DefaultPriority = 100;

	public int AgentIndex;

	public int NextAgentIndex;

	public bool IsClickable = true;

	public TextObject HintText;

	private MethodInfo _methodOnCondition;

	public OnConditionDelegate OnCondition;

	private MethodInfo _methodOnClickableCondition;

	public OnClickableConditionDelegate OnClickableCondition;

	private MethodInfo _methodOnConsequence;

	public OnConsequenceDelegate OnConsequence;

	public OnMultipleConversationConsequenceDelegate IsSpeaker;

	public OnMultipleConversationConsequenceDelegate IsListener;

	private uint _flags;

	private OnPersuasionOptionDelegate _onPersuasionOption;

	public TextObject Text { get; private set; }

	public int Index { get; internal set; }

	public string Id { get; private set; }

	public bool IsPlayer
	{
		get
		{
			return GetFlags(DialogLineFlags.PlayerLine);
		}
		internal set
		{
			set_flags(value, DialogLineFlags.PlayerLine);
		}
	}

	public bool IsRepeatable
	{
		get
		{
			return GetFlags(DialogLineFlags.RepeatForObjects);
		}
		internal set
		{
			set_flags(value, DialogLineFlags.RepeatForObjects);
		}
	}

	public bool IsSpecial
	{
		get
		{
			return GetFlags(DialogLineFlags.SpecialLine);
		}
		internal set
		{
			set_flags(value, DialogLineFlags.SpecialLine);
		}
	}

	public bool IsUsedOnce
	{
		get
		{
			return GetFlags(DialogLineFlags.UsedOnce);
		}
		internal set
		{
			set_flags(value, DialogLineFlags.UsedOnce);
		}
	}

	public int Priority { get; private set; }

	public int InputToken { get; private set; }

	public int OutputToken { get; private set; }

	public object RelatedObject { get; private set; }

	public bool IsWithVariation { get; private set; }

	public PersuasionOptionArgs PersuationOptionArgs { get; private set; }

	public bool HasPersuasion => _onPersuasionOption != null;

	public string SkillName
	{
		get
		{
			if (!HasPersuasion)
			{
				return "";
			}
			return PersuationOptionArgs.SkillUsed.ToString();
		}
	}

	public string TraitName
	{
		get
		{
			if (!HasPersuasion)
			{
				return "";
			}
			if (PersuationOptionArgs.TraitUsed == null)
			{
				return "";
			}
			return PersuationOptionArgs.TraitUsed.ToString();
		}
	}

	public static object CurrentProcessedRepeatObject => Campaign.Current.ConversationManager.GetCurrentProcessedRepeatObject();

	public static object SelectedRepeatObject => Campaign.Current.ConversationManager.GetSelectedRepeatObject();

	public static TextObject SelectedRepeatLine => Campaign.Current.ConversationManager.GetCurrentDialogLine();

	private bool GetFlags(DialogLineFlags flag)
	{
		return (_flags & (uint)flag) != 0;
	}

	private void set_flags(bool val, DialogLineFlags newFlag)
	{
		if (val)
		{
			_flags |= (uint)newFlag;
		}
		else
		{
			_flags &= (uint)(~newFlag);
		}
	}

	internal ConversationSentence(string idString, TextObject text, string inputToken, string outputToken, OnConditionDelegate conditionDelegate, OnClickableConditionDelegate clickableConditionDelegate, OnConsequenceDelegate consequenceDelegate, uint flags = 0u, int priority = 100, int agentIndex = 0, int nextAgentIndex = 0, object relatedObject = null, bool withVariation = false, OnMultipleConversationConsequenceDelegate speakerDelegate = null, OnMultipleConversationConsequenceDelegate listenerDelegate = null, OnPersuasionOptionDelegate persuasionOptionDelegate = null)
	{
		Index = Campaign.Current.ConversationManager.CreateConversationSentenceIndex();
		Id = idString;
		Text = text;
		InputToken = Campaign.Current.ConversationManager.GetStateIndex(inputToken);
		OutputToken = Campaign.Current.ConversationManager.GetStateIndex(outputToken);
		OnCondition = conditionDelegate;
		OnClickableCondition = clickableConditionDelegate;
		OnConsequence = consequenceDelegate;
		_flags = flags;
		Priority = priority;
		AgentIndex = agentIndex;
		NextAgentIndex = nextAgentIndex;
		RelatedObject = relatedObject;
		IsWithVariation = withVariation;
		IsSpeaker = speakerDelegate;
		IsListener = listenerDelegate;
		_onPersuasionOption = persuasionOptionDelegate;
	}

	internal ConversationSentence(int index)
	{
		Index = index;
	}

	public ConversationSentence Variation(params object[] list)
	{
		Game.Current.GameTextManager.AddGameText(Id).AddVariation((string)list[0], list.Skip(1).ToArray());
		return this;
	}

	internal void RunConsequence(Game game)
	{
		if (OnConsequence != null)
		{
			OnConsequence();
		}
		Campaign.Current.ConversationManager.OnConsequence(this);
		if (HasPersuasion)
		{
			ConversationManager.PersuasionCommitProgress(PersuationOptionArgs);
		}
	}

	internal bool RunCondition()
	{
		bool flag = true;
		if (OnCondition != null)
		{
			flag = OnCondition();
		}
		if (flag && HasPersuasion)
		{
			PersuationOptionArgs = _onPersuasionOption();
		}
		Campaign.Current.ConversationManager.OnCondition(this);
		return flag;
	}

	internal bool RunClickableCondition()
	{
		bool result = true;
		if (OnClickableCondition != null)
		{
			result = OnClickableCondition(out HintText);
		}
		Campaign.Current.ConversationManager.OnClickableCondition(this);
		return result;
	}

	public void Deserialize(XmlNode node, Type typeOfConversationCallbacks, ConversationManager conversationManager, int defaultPriority)
	{
		if (node.Attributes == null)
		{
			throw new TWXmlLoadException("node.Attributes != null");
		}
		Id = node.Attributes["id"].Value;
		XmlNode xmlNode = node.Attributes["on_condition"];
		if (xmlNode != null)
		{
			string innerText = xmlNode.InnerText;
			_methodOnCondition = typeOfConversationCallbacks.GetMethod(innerText);
			if (_methodOnCondition == null)
			{
				throw new MBMethodNameNotFoundException(innerText);
			}
			OnCondition = Delegate.CreateDelegate(typeof(OnConditionDelegate), null, _methodOnCondition) as OnConditionDelegate;
		}
		XmlNode xmlNode2 = node.Attributes["on_clickable_condition"];
		if (xmlNode2 != null)
		{
			string innerText2 = xmlNode2.InnerText;
			_methodOnClickableCondition = typeOfConversationCallbacks.GetMethod(innerText2);
			if (_methodOnClickableCondition == null)
			{
				throw new MBMethodNameNotFoundException(innerText2);
			}
			OnClickableCondition = Delegate.CreateDelegate(typeof(OnClickableConditionDelegate), null, _methodOnClickableCondition) as OnClickableConditionDelegate;
		}
		XmlNode xmlNode3 = node.Attributes["on_consequence"];
		if (xmlNode3 != null)
		{
			string innerText3 = xmlNode3.InnerText;
			_methodOnConsequence = typeOfConversationCallbacks.GetMethod(innerText3);
			if (_methodOnConsequence == null)
			{
				throw new MBMethodNameNotFoundException(innerText3);
			}
			OnConsequence = Delegate.CreateDelegate(typeof(OnConsequenceDelegate), null, _methodOnConsequence) as OnConsequenceDelegate;
		}
		XmlNode xmlNode4 = node.Attributes["is_player"];
		if (xmlNode4 != null)
		{
			string innerText4 = xmlNode4.InnerText;
			IsPlayer = Convert.ToBoolean(innerText4);
		}
		XmlNode xmlNode5 = node.Attributes["is_repeatable"];
		if (xmlNode5 != null)
		{
			string innerText5 = xmlNode5.InnerText;
			IsRepeatable = Convert.ToBoolean(innerText5);
		}
		XmlNode xmlNode6 = node.Attributes["is_speacial_option"];
		if (xmlNode6 != null)
		{
			string innerText6 = xmlNode6.InnerText;
			IsSpecial = Convert.ToBoolean(innerText6);
		}
		XmlNode xmlNode7 = node.Attributes["is_used_once"];
		if (xmlNode7 != null)
		{
			string innerText7 = xmlNode7.InnerText;
			IsUsedOnce = Convert.ToBoolean(innerText7);
		}
		XmlNode xmlNode8 = node.Attributes["text"];
		if (xmlNode8 != null)
		{
			Text = new TextObject(xmlNode8.InnerText);
		}
		XmlNode xmlNode9 = node.Attributes["istate"];
		if (xmlNode9 != null)
		{
			InputToken = conversationManager.GetStateIndex(xmlNode9.InnerText);
		}
		XmlNode xmlNode10 = node.Attributes["ostate"];
		if (xmlNode10 != null)
		{
			OutputToken = conversationManager.GetStateIndex(xmlNode10.InnerText);
		}
		XmlNode xmlNode11 = node.Attributes["priority"];
		Priority = ((xmlNode11 != null) ? int.Parse(xmlNode11.InnerText) : defaultPriority);
	}

	public static void SetObjectsToRepeatOver(IReadOnlyList<object> objectsToRepeatOver, int maxRepeatedDialogsInConversation = 5)
	{
		Campaign.Current.ConversationManager.SetDialogRepeatCount(objectsToRepeatOver, maxRepeatedDialogsInConversation);
	}
}
