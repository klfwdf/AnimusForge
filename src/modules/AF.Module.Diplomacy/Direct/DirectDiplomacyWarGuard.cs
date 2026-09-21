namespace AnimusForge;

/// <summary>
/// Pure authorization and observation rules for direct kingdom war actions.
/// Prompt guidance is not an authorization boundary; the game-thread caller
/// supplies current sovereign and faction state immediately before/after mutation.
/// </summary>
internal static class DirectDiplomacyWarGuard
{
	internal static bool CanDeclareForPlayerKingdom(bool isPlayerSovereign, bool payloadMatchesPlayerKingdom)
	{
		return isPlayerSovereign && payloadMatchesPlayerKingdom;
	}

	internal static bool DidDeclarationTakeEffect(bool isAtWarAfterApply)
	{
		return isAtWarAfterApply;
	}
}
