using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Library;

namespace SandBox.View.Conversation;

public class ConversationViewManager
{
	private Dictionary<string, ConversationViewEventHandlerDelegate> _conditionEventHandlers;

	private Dictionary<string, ConversationViewEventHandlerDelegate> _consequenceEventHandlers;

	public static ConversationViewManager Instance => SandBoxViewSubModule.ConversationViewManager;

	public ConversationViewManager()
	{
		FillEventHandlers();
		Campaign.Current.ConversationManager.ConditionRunned += OnCondition;
		Campaign.Current.ConversationManager.ConsequenceRunned += OnConsequence;
	}

	private void FillEventHandlers()
	{
		_conditionEventHandlers = new Dictionary<string, ConversationViewEventHandlerDelegate>();
		_consequenceEventHandlers = new Dictionary<string, ConversationViewEventHandlerDelegate>();
		Assembly assembly = typeof(ConversationViewEventHandlerDelegate).Assembly;
		FillEventHandlersWith(assembly);
		Assembly[] referencingAssembliesSafe = assembly.GetReferencingAssembliesSafe();
		foreach (Assembly assembly2 in referencingAssembliesSafe)
		{
			FillEventHandlersWith(assembly2);
		}
	}

	private void FillEventHandlersWith(Assembly assembly)
	{
		foreach (Type item in assembly.GetTypesSafe())
		{
			MethodInfo[] methods = item.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo method in methods)
			{
				object[] customAttributesSafe = method.GetCustomAttributesSafe(typeof(ConversationViewEventHandler), inherit: false);
				if (customAttributesSafe == null || customAttributesSafe.Length == 0)
				{
					continue;
				}
				object[] array = customAttributesSafe;
				for (int j = 0; j < array.Length; j++)
				{
					ConversationViewEventHandler conversationViewEventHandler = (ConversationViewEventHandler)array[j];
					ConversationViewEventHandlerDelegate value = Delegate.CreateDelegate(typeof(ConversationViewEventHandlerDelegate), method) as ConversationViewEventHandlerDelegate;
					if (conversationViewEventHandler.Type == ConversationViewEventHandler.EventType.OnCondition)
					{
						if (!_conditionEventHandlers.ContainsKey(conversationViewEventHandler.Id))
						{
							_conditionEventHandlers.Add(conversationViewEventHandler.Id, value);
						}
						else
						{
							_conditionEventHandlers[conversationViewEventHandler.Id] = value;
						}
					}
					else if (conversationViewEventHandler.Type == ConversationViewEventHandler.EventType.OnConsequence)
					{
						if (!_consequenceEventHandlers.ContainsKey(conversationViewEventHandler.Id))
						{
							_consequenceEventHandlers.Add(conversationViewEventHandler.Id, value);
						}
						else
						{
							_consequenceEventHandlers[conversationViewEventHandler.Id] = value;
						}
					}
				}
			}
		}
	}

	private void OnConsequence(ConversationSentence sentence)
	{
		if (_consequenceEventHandlers.TryGetValue(sentence.Id, out var value))
		{
			value();
		}
	}

	private void OnCondition(ConversationSentence sentence)
	{
		if (_conditionEventHandlers.TryGetValue(sentence.Id, out var value))
		{
			value();
		}
	}
}
