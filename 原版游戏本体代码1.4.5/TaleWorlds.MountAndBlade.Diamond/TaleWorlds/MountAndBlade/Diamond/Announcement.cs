using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class Announcement
{
	public int Id { get; set; }

	public Guid BattleId { get; set; }

	public AnnouncementType Type { get; set; }

	public string Text { get; set; }

	public bool IsEnabled { get; set; }

	public Announcement()
	{
	}

	public Announcement(int id, Guid battleId, AnnouncementType type, string text, bool isEnabled)
	{
		Id = id;
		BattleId = battleId;
		Type = type;
		Text = text;
		IsEnabled = isEnabled;
	}
}
