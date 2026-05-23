using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000D6 RID: 214
	public class Timer
	{
		// Token: 0x170003D4 RID: 980
		// (get) Token: 0x06000B24 RID: 2852 RVA: 0x00024389 File Offset: 0x00022589
		// (set) Token: 0x06000B25 RID: 2853 RVA: 0x00024391 File Offset: 0x00022591
		public float StartTime { get; protected set; }

		// Token: 0x170003D5 RID: 981
		// (get) Token: 0x06000B26 RID: 2854 RVA: 0x0002439A File Offset: 0x0002259A
		// (set) Token: 0x06000B27 RID: 2855 RVA: 0x000243A2 File Offset: 0x000225A2
		public float Duration { get; protected set; }

		// Token: 0x06000B28 RID: 2856 RVA: 0x000243AB File Offset: 0x000225AB
		public Timer(float gameTime, float duration, bool autoReset = true)
		{
			this.StartTime = gameTime;
			this._latestGameTime = gameTime;
			this._autoReset = autoReset;
			this.Duration = duration;
		}

		// Token: 0x06000B29 RID: 2857 RVA: 0x000243D0 File Offset: 0x000225D0
		public virtual bool Check(float gameTime)
		{
			this._latestGameTime = gameTime;
			if (this.Duration <= 0f)
			{
				this.PreviousDeltaTime = this.ElapsedTime();
				this.StartTime = gameTime;
				return true;
			}
			bool result = false;
			if (this.ElapsedTime() >= this.Duration)
			{
				this.PreviousDeltaTime = this.ElapsedTime();
				if (this._autoReset)
				{
					while (this.ElapsedTime() >= this.Duration)
					{
						this.StartTime += this.Duration;
					}
				}
				result = true;
			}
			return result;
		}

		// Token: 0x06000B2A RID: 2858 RVA: 0x00024450 File Offset: 0x00022650
		public float ElapsedTime()
		{
			return this._latestGameTime - this.StartTime;
		}

		// Token: 0x170003D6 RID: 982
		// (get) Token: 0x06000B2B RID: 2859 RVA: 0x0002445F File Offset: 0x0002265F
		// (set) Token: 0x06000B2C RID: 2860 RVA: 0x00024467 File Offset: 0x00022667
		public float PreviousDeltaTime { get; private set; }

		// Token: 0x06000B2D RID: 2861 RVA: 0x00024470 File Offset: 0x00022670
		public void Reset(float gameTime)
		{
			this.Reset(gameTime, this.Duration);
		}

		// Token: 0x06000B2E RID: 2862 RVA: 0x0002447F File Offset: 0x0002267F
		public void Reset(float gameTime, float newDuration)
		{
			this.StartTime = gameTime;
			this._latestGameTime = gameTime;
			this.Duration = newDuration;
		}

		// Token: 0x06000B2F RID: 2863 RVA: 0x00024496 File Offset: 0x00022696
		public void AdjustStartTime(float deltaTime)
		{
			this.StartTime += deltaTime;
		}

		// Token: 0x0400065B RID: 1627
		private float _latestGameTime;

		// Token: 0x0400065C RID: 1628
		private bool _autoReset;
	}
}
