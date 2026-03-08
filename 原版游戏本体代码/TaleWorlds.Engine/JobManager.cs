using System;
using System.Collections.Generic;

namespace TaleWorlds.Engine
{
	// Token: 0x02000053 RID: 83
	public class JobManager
	{
		// Token: 0x06000893 RID: 2195 RVA: 0x00006BF4 File Offset: 0x00004DF4
		public JobManager()
		{
			this._jobs = new List<Job>();
			this._locker = new object();
		}

		// Token: 0x06000894 RID: 2196 RVA: 0x00006C14 File Offset: 0x00004E14
		public void AddJob(Job job)
		{
			object locker = this._locker;
			lock (locker)
			{
				this._jobs.Add(job);
			}
		}

		// Token: 0x06000895 RID: 2197 RVA: 0x00006C5C File Offset: 0x00004E5C
		internal void OnTick(float dt)
		{
			object locker = this._locker;
			lock (locker)
			{
				for (int i = 0; i < this._jobs.Count; i++)
				{
					Job job = this._jobs[i];
					job.DoJob(dt);
					if (job.Finished)
					{
						this._jobs.RemoveAt(i);
						i--;
					}
				}
			}
		}

		// Token: 0x040000B6 RID: 182
		private List<Job> _jobs;

		// Token: 0x040000B7 RID: 183
		private object _locker;
	}
}
