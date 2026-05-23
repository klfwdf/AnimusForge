using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000BA RID: 186
	public class MissionSiegeWeapon : IMissionSiegeWeapon
	{
		// Token: 0x1700034F RID: 847
		// (get) Token: 0x060009DF RID: 2527 RVA: 0x000205CF File Offset: 0x0001E7CF
		public int Index
		{
			get
			{
				return this._index;
			}
		}

		// Token: 0x17000350 RID: 848
		// (get) Token: 0x060009E0 RID: 2528 RVA: 0x000205D7 File Offset: 0x0001E7D7
		public SiegeEngineType Type
		{
			get
			{
				return this._type;
			}
		}

		// Token: 0x17000351 RID: 849
		// (get) Token: 0x060009E1 RID: 2529 RVA: 0x000205DF File Offset: 0x0001E7DF
		public float Health
		{
			get
			{
				return this._health;
			}
		}

		// Token: 0x17000352 RID: 850
		// (get) Token: 0x060009E2 RID: 2530 RVA: 0x000205E7 File Offset: 0x0001E7E7
		public float InitialHealth
		{
			get
			{
				return this._initialHealth;
			}
		}

		// Token: 0x17000353 RID: 851
		// (get) Token: 0x060009E3 RID: 2531 RVA: 0x000205EF File Offset: 0x0001E7EF
		public float MaxHealth
		{
			get
			{
				return this._maxHealth;
			}
		}

		// Token: 0x060009E4 RID: 2532 RVA: 0x000205F7 File Offset: 0x0001E7F7
		private MissionSiegeWeapon(int index, SiegeEngineType type, float health, float maxHealth)
		{
			this._index = index;
			this._type = type;
			this._initialHealth = health;
			this._health = this._initialHealth;
			this._maxHealth = maxHealth;
		}

		// Token: 0x060009E5 RID: 2533 RVA: 0x00020628 File Offset: 0x0001E828
		public static MissionSiegeWeapon CreateDefaultWeapon(SiegeEngineType type)
		{
			return new MissionSiegeWeapon(-1, type, (float)type.BaseHitPoints, (float)type.BaseHitPoints);
		}

		// Token: 0x060009E6 RID: 2534 RVA: 0x0002063F File Offset: 0x0001E83F
		public static MissionSiegeWeapon CreateCampaignWeapon(SiegeEngineType type, int index, float health, float maxHealth)
		{
			return new MissionSiegeWeapon(index, type, health, maxHealth);
		}

		// Token: 0x060009E7 RID: 2535 RVA: 0x0002064A File Offset: 0x0001E84A
		public void SetHealth(float health)
		{
			this._health = health;
		}

		// Token: 0x04000574 RID: 1396
		private float _health;

		// Token: 0x04000575 RID: 1397
		private readonly int _index;

		// Token: 0x04000576 RID: 1398
		private readonly SiegeEngineType _type;

		// Token: 0x04000577 RID: 1399
		private readonly float _initialHealth;

		// Token: 0x04000578 RID: 1400
		private readonly float _maxHealth;
	}
}
