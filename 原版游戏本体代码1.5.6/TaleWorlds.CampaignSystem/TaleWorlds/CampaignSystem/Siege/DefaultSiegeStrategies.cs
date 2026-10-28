using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Siege;

public class DefaultSiegeStrategies
{
	private SiegeStrategy _preserveStrength;

	private SiegeStrategy _prepareAgainstAssault;

	private SiegeStrategy _counterBombardment;

	private SiegeStrategy _prepareAssault;

	private SiegeStrategy _breachWalls;

	private SiegeStrategy _wearOutDefenders;

	private SiegeStrategy _custom;

	private static DefaultSiegeStrategies Instance => Campaign.Current.DefaultSiegeStrategies;

	public static SiegeStrategy PreserveStrength => Instance._preserveStrength;

	public static SiegeStrategy PrepareAgainstAssault => Instance._prepareAgainstAssault;

	public static SiegeStrategy CounterBombardment => Instance._counterBombardment;

	public static SiegeStrategy PrepareAssault => Instance._prepareAssault;

	public static SiegeStrategy BreachWalls => Instance._breachWalls;

	public static SiegeStrategy WearOutDefenders => Instance._wearOutDefenders;

	public static SiegeStrategy Custom => Instance._custom;

	public static IEnumerable<SiegeStrategy> AllAttackerStrategies
	{
		get
		{
			yield return PrepareAssault;
			yield return BreachWalls;
			yield return WearOutDefenders;
			yield return PreserveStrength;
			yield return Custom;
		}
	}

	public static IEnumerable<SiegeStrategy> AllDefenderStrategies
	{
		get
		{
			yield return PrepareAgainstAssault;
			yield return CounterBombardment;
			yield return PreserveStrength;
			yield return Custom;
		}
	}

	public DefaultSiegeStrategies()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_preserveStrength = Create("siege_strategy_preserve_strength");
		_prepareAgainstAssault = Create("siege_strategy_prepare_against_assault");
		_counterBombardment = Create("siege_strategy_counter_bombardment");
		_prepareAssault = Create("siege_strategy_prepare_assault");
		_breachWalls = Create("siege_strategy_breach_walls");
		_wearOutDefenders = Create("siege_strategy_wear_out_defenders");
		_custom = Create("siege_strategy_custom");
		InitializeAll();
	}

	private SiegeStrategy Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new SiegeStrategy(stringId));
	}

	private void InitializeAll()
	{
		_custom.Initialize(new TextObject("{=!}Custom"), new TextObject("{=!}Custom strategy that can be managed entirely."));
		_preserveStrength.Initialize(new TextObject("{=!}Preserve Strength"), new TextObject("{=!}Priority is set to preserving our strength."));
		_prepareAgainstAssault.Initialize(new TextObject("{=!}Prepare Against Assault"), new TextObject("{=!}Priority is set to keep advantage when the enemies' assault starts."));
		_counterBombardment.Initialize(new TextObject("{=!}Counter Bombardment"), new TextObject("{=!}Priority is set to countering enemy bombardment."));
		_prepareAssault.Initialize(new TextObject("{=!}Prepare Assault"), new TextObject("{=!}Priority is set to assaulting the walls."));
		_breachWalls.Initialize(new TextObject("{=!}Breach Walls"), new TextObject("{=!}Priority is set to breaching the walls."));
		_wearOutDefenders.Initialize(new TextObject("{=!}Wear out Defenders"), new TextObject("{=!}Priority is set to destroying engines of the enemy."));
	}
}
