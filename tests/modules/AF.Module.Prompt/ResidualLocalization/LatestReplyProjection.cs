using System; using System.Text; namespace AnimusForge; internal static class LatestReplyProjection {public static string BuildActionPostprocessLatestReplyBlock(string playerText, string npcReplyText, string npcName, string historyText = null)
	{
		string text = NormalizeActionPostprocessDialogueText(playerText);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ExtractLatestPlayerUtteranceForActionPostprocess(historyText);
		}
		string text2 = NormalizeActionPostprocessDialogueText(npcReplyText);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(PromptComposerTexts.Get("residual_player_colon")).Append(string.IsNullOrWhiteSpace(text) ? PromptComposerTexts.Get("residual_none") : text);
		stringBuilder.AppendLine();
		stringBuilder.Append("NPC: ").Append(string.IsNullOrWhiteSpace(text2) ? PromptComposerTexts.Get("residual_none") : text2);
		return stringBuilder.ToString().Trim();
	}private static string NormalizeActionPostprocessDialogueText(string text)=>(text ?? "").Trim(); private static string ExtractLatestPlayerUtteranceForActionPostprocess(string text)=>text ?? "";}