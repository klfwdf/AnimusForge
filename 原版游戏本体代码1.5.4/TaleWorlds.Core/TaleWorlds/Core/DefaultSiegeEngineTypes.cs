namespace TaleWorlds.Core;

public class DefaultSiegeEngineTypes
{
	private SiegeEngineType _siegeEngineTypePreparations;

	private SiegeEngineType _siegeEngineTypeLadder;

	private SiegeEngineType _siegeEngineTypeBallista;

	private SiegeEngineType _siegeEngineTypeFireBallista;

	private SiegeEngineType _siegeEngineTypeRam;

	private SiegeEngineType _siegeEngineTypeImprovedRam;

	private SiegeEngineType _siegeEngineTypeSiegeTower;

	private SiegeEngineType _siegeEngineTypeHeavySiegeTower;

	private SiegeEngineType _siegeEngineTypeCatapult;

	private SiegeEngineType _siegeEngineTypeFireCatapult;

	private SiegeEngineType _siegeEngineTypeOnager;

	private SiegeEngineType _siegeEngineTypeFireOnager;

	private SiegeEngineType _siegeEngineTypeBricole;

	private SiegeEngineType _siegeEngineTypeTrebuchet;

	private SiegeEngineType _siegeEngineTypeFireTrebuchet;

	private static DefaultSiegeEngineTypes Instance => Game.Current.DefaultSiegeEngineTypes;

	public static SiegeEngineType Preparations => Instance._siegeEngineTypePreparations;

	public static SiegeEngineType Ladder => Instance._siegeEngineTypeLadder;

	public static SiegeEngineType Ballista => Instance._siegeEngineTypeBallista;

	public static SiegeEngineType FireBallista => Instance._siegeEngineTypeFireBallista;

	public static SiegeEngineType Ram => Instance._siegeEngineTypeRam;

	public static SiegeEngineType ImprovedRam => Instance._siegeEngineTypeImprovedRam;

	public static SiegeEngineType SiegeTower => Instance._siegeEngineTypeSiegeTower;

	public static SiegeEngineType HeavySiegeTower => Instance._siegeEngineTypeHeavySiegeTower;

	public static SiegeEngineType Catapult => Instance._siegeEngineTypeCatapult;

	public static SiegeEngineType FireCatapult => Instance._siegeEngineTypeFireCatapult;

	public static SiegeEngineType Onager => Instance._siegeEngineTypeOnager;

	public static SiegeEngineType FireOnager => Instance._siegeEngineTypeFireOnager;

	public static SiegeEngineType Bricole => Instance._siegeEngineTypeBricole;

	public static SiegeEngineType Trebuchet => Instance._siegeEngineTypeTrebuchet;

	public static SiegeEngineType FireTrebuchet => Instance._siegeEngineTypeTrebuchet;

	public DefaultSiegeEngineTypes()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		Game.Current.ObjectManager.LoadXML("SiegeEngines");
		_siegeEngineTypePreparations = GetSiegeEngine("preparations");
		_siegeEngineTypeLadder = GetSiegeEngine("ladder");
		_siegeEngineTypeSiegeTower = GetSiegeEngine("siege_tower_level1");
		_siegeEngineTypeHeavySiegeTower = GetSiegeEngine("siege_tower_level2");
		_siegeEngineTypeBallista = GetSiegeEngine("ballista");
		_siegeEngineTypeFireBallista = GetSiegeEngine("fire_ballista");
		_siegeEngineTypeCatapult = GetSiegeEngine("catapult");
		_siegeEngineTypeFireCatapult = GetSiegeEngine("fire_catapult");
		_siegeEngineTypeOnager = GetSiegeEngine("onager");
		_siegeEngineTypeFireOnager = GetSiegeEngine("fire_onager");
		_siegeEngineTypeBricole = GetSiegeEngine("bricole");
		_siegeEngineTypeTrebuchet = GetSiegeEngine("trebuchet");
		_siegeEngineTypeFireTrebuchet = GetSiegeEngine("fire_trebuchet");
		_siegeEngineTypeRam = GetSiegeEngine("ram");
		_siegeEngineTypeImprovedRam = GetSiegeEngine("improved_ram");
	}

	private SiegeEngineType GetSiegeEngine(string id)
	{
		return Game.Current.ObjectManager.GetObject<SiegeEngineType>(id);
	}
}
