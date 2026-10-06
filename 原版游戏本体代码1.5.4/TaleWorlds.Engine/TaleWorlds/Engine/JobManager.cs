using System.Collections.Generic;

namespace TaleWorlds.Engine;

public class JobManager
{
	private List<Job> _jobs;

	private object _locker;

	public JobManager()
	{
		_jobs = new List<Job>();
		_locker = new object();
	}

	public void AddJob(Job job)
	{
		lock (_locker)
		{
			_jobs.Add(job);
		}
	}

	internal void OnTick(float dt)
	{
		lock (_locker)
		{
			for (int i = 0; i < _jobs.Count; i++)
			{
				Job job = _jobs[i];
				job.DoJob(dt);
				if (job.Finished)
				{
					_jobs.RemoveAt(i);
					i--;
				}
			}
		}
	}
}
