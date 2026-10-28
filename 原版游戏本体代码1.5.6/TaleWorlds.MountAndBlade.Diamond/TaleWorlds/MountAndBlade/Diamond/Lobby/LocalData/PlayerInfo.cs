namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class PlayerInfo
{
	public string PlayerId { get; set; }

	public string Username { get; set; }

	public int ForcedIndex { get; set; }

	public int TeamNo { get; set; }

	public int Kill { get; set; }

	public int Death { get; set; }

	public int Assist { get; set; }

	public bool HasSameContentWith(PlayerInfo other)
	{
		if (PlayerId == other.PlayerId && Username == other.Username && ForcedIndex == other.ForcedIndex && TeamNo == other.TeamNo && Kill == other.Kill && Death == other.Death)
		{
			return Assist == other.Assist;
		}
		return false;
	}
}
