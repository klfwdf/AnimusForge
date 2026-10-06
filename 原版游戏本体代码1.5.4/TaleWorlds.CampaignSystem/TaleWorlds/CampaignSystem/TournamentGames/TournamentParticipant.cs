using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TournamentGames;

public class TournamentParticipant
{
	public int Score { get; private set; }

	public CharacterObject Character { get; private set; }

	public UniqueTroopDescriptor Descriptor { get; private set; }

	public TournamentTeam Team { get; private set; }

	public Equipment MatchEquipment { get; set; }

	public bool IsAssigned { get; set; }

	public bool IsPlayer => Character?.IsPlayerCharacter ?? false;

	public TournamentParticipant(CharacterObject character, UniqueTroopDescriptor descriptor = default(UniqueTroopDescriptor))
	{
		Character = character;
		Descriptor = (descriptor.IsValid ? descriptor : new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed));
		Team = null;
		IsAssigned = false;
	}

	public void SetTeam(TournamentTeam team)
	{
		Team = team;
	}

	public int AddScore(int score)
	{
		Score += score;
		return Score;
	}

	public void ResetScore()
	{
		Score = 0;
	}
}
