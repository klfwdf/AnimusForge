using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class GenericCampaignPanelsGameKeyCategory : GameKeyContext
{
	public const string CategoryId = "GenericCampaignPanelsGameKeyCategory";

	public const string FiveStackModifier = "FiveStackModifier";

	public const string EntireStackModifier = "EntireStackModifier";

	public const int BannerWindow = 36;

	public const int CharacterWindow = 37;

	public const int InventoryWindow = 38;

	public const int EncyclopediaWindow = 39;

	public const int PartyWindow = 43;

	public const int KingdomWindow = 40;

	public const int ClanWindow = 41;

	public const int QuestsWindow = 42;

	public const int FacegenWindow = 44;

	public const int ManageFleetWindow = 45;

	public static GenericCampaignPanelsGameKeyCategory Current { get; private set; }

	public GenericCampaignPanelsGameKeyCategory(string categoryId = "GenericCampaignPanelsGameKeyCategory")
		: base(categoryId, 116)
	{
		Current = this;
		RegisterHotKeys();
		RegisterGameKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.LeftShift),
			new Key(InputKey.RightShift)
		};
		RegisterHotKey(new HotKey("FiveStackModifier", "GenericCampaignPanelsGameKeyCategory", keys));
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.LeftControl),
			new Key(InputKey.RightControl)
		};
		RegisterHotKey(new HotKey("EntireStackModifier", "GenericCampaignPanelsGameKeyCategory", keys2));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(36, "BannerWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.B, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(37, "CharacterWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.C, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(38, "InventoryWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.I, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(39, "EncyclopediaWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.N, InputKey.ControllerLOption, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(40, "KingdomWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.K, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(41, "ClanWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.L, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(42, "QuestsWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.J, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(43, "PartyWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.P, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(44, "FacegenWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.V, GameKeyMainCategories.MenuShortcutCategory));
		RegisterGameKey(new GameKey(45, "ManageFleetWindow", "GenericCampaignPanelsGameKeyCategory", InputKey.U, GameKeyMainCategories.MenuShortcutCategory));
	}
}
