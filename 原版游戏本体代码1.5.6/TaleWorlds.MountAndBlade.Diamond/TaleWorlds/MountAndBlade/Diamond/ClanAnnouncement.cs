using System;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanAnnouncement
{
	[JsonProperty]
	public int Id { get; private set; }

	[JsonProperty]
	public string Announcement { get; private set; }

	[JsonProperty]
	public PlayerId AuthorId { get; private set; }

	[JsonProperty]
	public DateTime CreationTime { get; private set; }

	public ClanAnnouncement(int id, string announcement, PlayerId authorId, DateTime creationTime)
	{
		Id = id;
		Announcement = announcement;
		AuthorId = authorId;
		CreationTime = creationTime;
	}
}
