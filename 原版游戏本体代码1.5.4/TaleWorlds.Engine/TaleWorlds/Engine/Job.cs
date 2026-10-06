namespace TaleWorlds.Engine;

public class Job
{
	public bool Finished { get; protected set; }

	public virtual void DoJob(float dt)
	{
	}
}
