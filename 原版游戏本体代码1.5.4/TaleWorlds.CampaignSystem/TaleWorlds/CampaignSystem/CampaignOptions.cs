using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AdvancedStartOptions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem;

public class CampaignOptions
{
	public enum Difficulty : short
	{
		VeryEasy,
		Easy,
		Realistic
	}

	[SaveableField(26)]
	private readonly AdvancedStartOptionsData _advancedStartOptionsData;

	[SaveableField(4)]
	private bool _autoAllocateClanMemberPerks;

	[SaveableField(5)]
	private Difficulty _playerTroopsReceivedDamage;

	[SaveableField(8)]
	private Difficulty _recruitmentDifficulty;

	[SaveableField(9)]
	private Difficulty _playerMapMovementSpeed;

	[SaveableField(18)]
	private Difficulty _stealthAndDisguiseDifficulty;

	[SaveableField(11)]
	private Difficulty _combatAIDifficulty;

	[SaveableField(12)]
	private bool _isLifeDeathCycleDisabled;

	[SaveableField(13)]
	private Difficulty _persuasionSuccessChance;

	[SaveableField(14)]
	private Difficulty _clanMemberDeathChance;

	[SaveableField(15)]
	private bool _isIronmanMode;

	[SaveableField(17)]
	private Difficulty _battleDeath;

	[SaveableField(19)]
	public GameAccelerationMode AccelerationMode;

	[SaveableField(20)]
	private readonly uint _seed;

	[SaveableField(21)]
	private readonly bool _risenBanditsEnabled;

	[SaveableField(22)]
	private readonly bool _highRebellion;

	[SaveableField(23)]
	private readonly bool _recruitmentRate;

	[SaveableField(24)]
	private readonly bool _increasedGlobalMovementSpeed;

	private static CampaignOptions _current => Campaign.Current?.Options;

	public AdvancedStartOptionsData AdvancedStartOptionsData => _advancedStartOptionsData;

	public uint Seed => _seed;

	public bool IsHighRebellionEnabled => _highRebellion;

	public bool IsRecruitmentRateModifierEnabled => _recruitmentRate;

	public bool IsIncreasedGlobalMovementSpeedEnabled => _increasedGlobalMovementSpeed;

	public bool IsRisenBanditsEnabled => _risenBanditsEnabled;

	public static bool IsLifeDeathCycleDisabled
	{
		get
		{
			return _current?._isLifeDeathCycleDisabled ?? false;
		}
		set
		{
			if (_current != null)
			{
				_current._isLifeDeathCycleDisabled = value;
			}
		}
	}

	public static bool AutoAllocateClanMemberPerks
	{
		get
		{
			return _current?._autoAllocateClanMemberPerks ?? false;
		}
		set
		{
			if (_current != null)
			{
				_current._autoAllocateClanMemberPerks = value;
			}
		}
	}

	public static bool IsIronmanMode
	{
		get
		{
			return _current?._isIronmanMode ?? false;
		}
		set
		{
			if (_current != null)
			{
				_current._isIronmanMode = value;
			}
		}
	}

	public static Difficulty PlayerTroopsReceivedDamage
	{
		get
		{
			return _current?._playerTroopsReceivedDamage ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._playerTroopsReceivedDamage = value;
			}
		}
	}

	public static Difficulty RecruitmentDifficulty
	{
		get
		{
			return _current?._recruitmentDifficulty ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._recruitmentDifficulty = value;
			}
		}
	}

	public static Difficulty PlayerMapMovementSpeed
	{
		get
		{
			return _current?._playerMapMovementSpeed ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._playerMapMovementSpeed = value;
			}
		}
	}

	public static Difficulty StealthAndDisguiseDifficulty
	{
		get
		{
			return _current?._stealthAndDisguiseDifficulty ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._stealthAndDisguiseDifficulty = value;
			}
		}
	}

	public static Difficulty CombatAIDifficulty
	{
		get
		{
			return _current?._combatAIDifficulty ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._combatAIDifficulty = value;
			}
		}
	}

	public static Difficulty PersuasionSuccessChance
	{
		get
		{
			return _current?._persuasionSuccessChance ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._persuasionSuccessChance = value;
			}
		}
	}

	public static Difficulty ClanMemberDeathChance
	{
		get
		{
			return _current?._clanMemberDeathChance ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._clanMemberDeathChance = value;
			}
		}
	}

	public static Difficulty BattleDeath
	{
		get
		{
			return _current?._battleDeath ?? Difficulty.Realistic;
		}
		set
		{
			if (_current != null)
			{
				_current._battleDeath = value;
			}
		}
	}

	public CampaignOptions()
	{
		_playerTroopsReceivedDamage = Difficulty.VeryEasy;
		_recruitmentDifficulty = Difficulty.VeryEasy;
		_playerMapMovementSpeed = Difficulty.VeryEasy;
		_combatAIDifficulty = Difficulty.VeryEasy;
		_persuasionSuccessChance = Difficulty.VeryEasy;
		_clanMemberDeathChance = Difficulty.VeryEasy;
		_battleDeath = Difficulty.VeryEasy;
		_stealthAndDisguiseDifficulty = Difficulty.VeryEasy;
		_isLifeDeathCycleDisabled = false;
		_autoAllocateClanMemberPerks = false;
		_isIronmanMode = false;
		AccelerationMode = GameAccelerationMode.Default;
	}

	public CampaignOptions(AdvancedStartOptionsData startOptions)
		: this()
	{
		_advancedStartOptionsData = startOptions;
		if (!startOptions.TryGetSeed(out _seed))
		{
			_seed = (uint)Environment.TickCount;
		}
		_highRebellion = startOptions.IsHighRebellionEnabled();
		_recruitmentRate = startOptions.IsRecruitmentRateModifierEnabled();
		_increasedGlobalMovementSpeed = startOptions.IsIncreasedGlobalMovementSpeedEnabled();
		_risenBanditsEnabled = startOptions.IsRisenBanditsEnabled();
		AccelerationMode = (startOptions.IsFastModeEnabled() ? GameAccelerationMode.Fast : GameAccelerationMode.Default);
	}

	internal static void AutoGeneratedStaticCollectObjectsCampaignOptions(object o, List<object> collectedObjects)
	{
		((CampaignOptions)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		collectedObjects.Add(_advancedStartOptionsData);
	}

	internal static object AutoGeneratedGetMemberValueAccelerationMode(object o)
	{
		return ((CampaignOptions)o).AccelerationMode;
	}

	internal static object AutoGeneratedGetMemberValue_advancedStartOptionsData(object o)
	{
		return ((CampaignOptions)o)._advancedStartOptionsData;
	}

	internal static object AutoGeneratedGetMemberValue_autoAllocateClanMemberPerks(object o)
	{
		return ((CampaignOptions)o)._autoAllocateClanMemberPerks;
	}

	internal static object AutoGeneratedGetMemberValue_playerTroopsReceivedDamage(object o)
	{
		return ((CampaignOptions)o)._playerTroopsReceivedDamage;
	}

	internal static object AutoGeneratedGetMemberValue_recruitmentDifficulty(object o)
	{
		return ((CampaignOptions)o)._recruitmentDifficulty;
	}

	internal static object AutoGeneratedGetMemberValue_playerMapMovementSpeed(object o)
	{
		return ((CampaignOptions)o)._playerMapMovementSpeed;
	}

	internal static object AutoGeneratedGetMemberValue_stealthAndDisguiseDifficulty(object o)
	{
		return ((CampaignOptions)o)._stealthAndDisguiseDifficulty;
	}

	internal static object AutoGeneratedGetMemberValue_combatAIDifficulty(object o)
	{
		return ((CampaignOptions)o)._combatAIDifficulty;
	}

	internal static object AutoGeneratedGetMemberValue_isLifeDeathCycleDisabled(object o)
	{
		return ((CampaignOptions)o)._isLifeDeathCycleDisabled;
	}

	internal static object AutoGeneratedGetMemberValue_persuasionSuccessChance(object o)
	{
		return ((CampaignOptions)o)._persuasionSuccessChance;
	}

	internal static object AutoGeneratedGetMemberValue_clanMemberDeathChance(object o)
	{
		return ((CampaignOptions)o)._clanMemberDeathChance;
	}

	internal static object AutoGeneratedGetMemberValue_isIronmanMode(object o)
	{
		return ((CampaignOptions)o)._isIronmanMode;
	}

	internal static object AutoGeneratedGetMemberValue_battleDeath(object o)
	{
		return ((CampaignOptions)o)._battleDeath;
	}

	internal static object AutoGeneratedGetMemberValue_seed(object o)
	{
		return ((CampaignOptions)o)._seed;
	}

	internal static object AutoGeneratedGetMemberValue_risenBanditsEnabled(object o)
	{
		return ((CampaignOptions)o)._risenBanditsEnabled;
	}

	internal static object AutoGeneratedGetMemberValue_highRebellion(object o)
	{
		return ((CampaignOptions)o)._highRebellion;
	}

	internal static object AutoGeneratedGetMemberValue_recruitmentRate(object o)
	{
		return ((CampaignOptions)o)._recruitmentRate;
	}

	internal static object AutoGeneratedGetMemberValue_increasedGlobalMovementSpeed(object o)
	{
		return ((CampaignOptions)o)._increasedGlobalMovementSpeed;
	}
}
