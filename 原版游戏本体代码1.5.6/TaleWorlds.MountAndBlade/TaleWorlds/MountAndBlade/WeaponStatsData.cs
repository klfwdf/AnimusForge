using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Weapon_stats_data", false, null)]
public struct WeaponStatsData
{
	public MatrixFrame WeaponFrame;

	public Vec3 RotationSpeed;

	public ulong WeaponFlags;

	public uint Properties;

	public int WeaponClass;

	public int AmmoClass;

	public int ItemUsageIndex;

	public int ThrustSpeed;

	public int SwingSpeed;

	public int MissileSpeed;

	public int ShieldArmor;

	public int ThrustDamage;

	public int SwingDamage;

	public int DefendSpeed;

	public int Accuracy;

	public int WeaponLength;

	public float WeaponBalance;

	public float SweetSpot;

	public short MaxDataValue;

	public short ReloadPhaseCount;

	public int ThrustDamageType;

	public int SwingDamageType;
}
