using System.Collections.Generic;

namespace AnimusForge;

// Mirrors MyBehavior.WorldBulletinPanel.cs, which also holds TaleWorlds-bound code and cannot be linked here.
internal sealed class WorldBulletinLayout
{
	public string EventId = "";

	public string MajorKind = "";

	public List<string> KingdomIds = new List<string>();

	public List<string> MinorKinds = new List<string>();
}
