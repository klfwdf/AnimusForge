using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Conversation;

public class ConversationAnimData
{
	[SaveableField(0)]
	public string IdleAnimStart;

	[SaveableField(1)]
	public string IdleAnimLoop;

	[SaveableField(2)]
	public int FamilyType;

	[SaveableField(3)]
	public int MountFamilyType;

	[SaveableField(4)]
	public Dictionary<string, string> Reactions;

	public ConversationAnimData()
	{
		Reactions = new Dictionary<string, string>();
	}
}
