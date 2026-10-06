using TaleWorlds.InputSystem;

namespace NavalDLC.HotKeyCategories;

public class NavalCheatsHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "NavalCheatsHotKeyCategory";

	public const string DebugSailingMoveToRight = "DebugSailingMoveToRight";

	public const string DebugSailingMoveToLeft = "DebugSailingMoveToLeft";

	public const string DebugRammingCollision = "DebugRammingCollision";

	public const string DebugDealSiegeEngineDamage = "DebugDealSiegeEngineDamage";

	public const string DebugSetWindDirection = "DebugSetWindDirection";

	public NavalCheatsHotKeyCategory()
		: base("NavalCheatsHotKeyCategory", 0, GameKeyContextType.AuxiliaryNotSerialized)
	{
		RegisterHotKey(new HotKey("DebugSailingMoveToLeft", "NavalCheatsHotKeyCategory", InputKey.A, HotKey.Modifiers.Alt));
		RegisterHotKey(new HotKey("DebugSailingMoveToRight", "NavalCheatsHotKeyCategory", InputKey.D, HotKey.Modifiers.Alt));
		RegisterHotKey(new HotKey("DebugRammingCollision", "NavalCheatsHotKeyCategory", InputKey.R, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt));
		RegisterHotKey(new HotKey("DebugDealSiegeEngineDamage", "NavalCheatsHotKeyCategory", InputKey.B, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt));
		RegisterHotKey(new HotKey("DebugSetWindDirection", "NavalCheatsHotKeyCategory", InputKey.W, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt));
	}
}
