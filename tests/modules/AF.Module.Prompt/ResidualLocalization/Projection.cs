using System; namespace AnimusForge; internal static class Projection {internal static string FormatScenePlayerDirectSpeechLabel(string playerName, float playerDistanceMeters)
	{
		string text = (playerName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = PromptComposerTexts.Get("residual_player");
		}
		if (float.IsNaN(playerDistanceMeters) || float.IsInfinity(playerDistanceMeters) || playerDistanceMeters < 0f)
		{
			return PromptComposerTexts.Format("residual_player_says", ("NAME", text));
		}
		int num = Math.Max(0, (int)Math.Ceiling(playerDistanceMeters));
		if (playerDistanceMeters > 50f)
		{
			return PromptComposerTexts.Format("residual_player_shouts_loudly", ("NAME", text), ("DISTANCE", num));
		}
		if (playerDistanceMeters > 5f)
		{
			return PromptComposerTexts.Format("residual_player_shouts", ("NAME", text), ("DISTANCE", num));
		}
		return PromptComposerTexts.Format("residual_player_says", ("NAME", text));
	}}