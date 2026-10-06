namespace TaleWorlds.MountAndBlade;

public class AgentController
{
	public Agent Owner { get; set; }

	public Mission Mission { get; set; }

	public virtual void OnInitialize()
	{
	}
}
