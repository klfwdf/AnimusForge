using System;

namespace SandBox.View.Conversation;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ConversationViewEventHandler : Attribute
{
	public enum EventType
	{
		OnCondition,
		OnConsequence
	}

	public string Id { get; }

	public EventType Type { get; }

	public ConversationViewEventHandler(string id, EventType type)
	{
		Id = id;
		Type = type;
	}
}
