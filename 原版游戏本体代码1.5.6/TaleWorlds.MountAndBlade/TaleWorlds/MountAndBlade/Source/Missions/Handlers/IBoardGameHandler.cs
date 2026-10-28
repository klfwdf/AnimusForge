namespace TaleWorlds.MountAndBlade.Source.Missions.Handlers;

public interface IBoardGameHandler
{
	void SwitchTurns();

	void DiceRoll(int roll);

	void Install();

	void Uninstall();

	void Activate();
}
