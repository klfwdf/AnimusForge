namespace TaleWorlds.Core;

public class MissionSiegeWeapon : IMissionSiegeWeapon
{
	private float _health;

	private readonly int _index;

	private readonly SiegeEngineType _type;

	private readonly float _initialHealth;

	private readonly float _maxHealth;

	public int Index => _index;

	public SiegeEngineType Type => _type;

	public float Health => _health;

	public float InitialHealth => _initialHealth;

	public float MaxHealth => _maxHealth;

	private MissionSiegeWeapon(int index, SiegeEngineType type, float health, float maxHealth)
	{
		_index = index;
		_type = type;
		_initialHealth = health;
		_health = _initialHealth;
		_maxHealth = maxHealth;
	}

	public static MissionSiegeWeapon CreateDefaultWeapon(SiegeEngineType type)
	{
		return new MissionSiegeWeapon(-1, type, type.BaseHitPoints, type.BaseHitPoints);
	}

	public static MissionSiegeWeapon CreateCampaignWeapon(SiegeEngineType type, int index, float health, float maxHealth)
	{
		return new MissionSiegeWeapon(index, type, health, maxHealth);
	}

	public void SetHealth(float health)
	{
		_health = health;
	}
}
