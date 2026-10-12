using System;
using System.Collections.Generic;

namespace AnimusForge;

// Package-language grammar for transient weekly Prompt material; saved facts and UI projection stay separate.
internal static class NpcActionMaterialPreviewProjection
{
	internal static string Build(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return AdapterRecordTexts.Prompt("InvalidAction");
		}
		List<string> list = new List<string>();
		string text = port.BuildNpcActionActorNarrativeText(entry);
		string text2 = AdapterRecordTexts.PromptActionKind(entry.ActionKind, port.TranslateNpcActionKindForPrompt(entry.ActionKind));
		string text3 = port.ResolveDisplayNameBySettlementEntry(entry);
		string text4 = port.ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text5 = port.ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text6 = port.ResolveKingdomDisplay(entry.TargetKingdomId);
		string text7 = port.ResolveClanDisplay(entry.TargetClanId);
		string text8 = port.ResolveHeroDisplay(entry.TargetHeroId);
		string text9 = port.ResolveClanDisplay(entry.ActorClanId);
		string text10 = port.ResolveKingdomDisplay(entry.ActorKingdomId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text);
		}
		else if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add(AdapterRecordTexts.PromptFormat("ActionKind", ("KIND", text2)));
		}
		if (!string.IsNullOrWhiteSpace(text3) && string.IsNullOrWhiteSpace(entry.SettlementId) && !text.Contains(text3))
		{
			list.Add(AdapterRecordTexts.PromptFormat("Location", ("LOCATION", text3)));
		}
		if (!string.IsNullOrWhiteSpace(text9) && !string.IsNullOrWhiteSpace(text10))
		{
			list.Add(AdapterRecordTexts.PromptFormat("ActorClanKingdom", ("CLAN", text9), ("KINGDOM", text10)));
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add(AdapterRecordTexts.PromptFormat("ActorClan", ("CLAN", text9)));
		}
		else if (!string.IsNullOrWhiteSpace(text10))
		{
			list.Add(AdapterRecordTexts.PromptFormat("ActorKingdom", ("KINGDOM", text10)));
		}
		if (!string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(text4))
		{
			list.Add(AdapterRecordTexts.PromptFormat("OwnerClanKingdom", ("CLAN", text5), ("KINGDOM", text4)));
		}
		else if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add(AdapterRecordTexts.PromptFormat("OwnerKingdom", ("KINGDOM", text4)));
		}
		else if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add(AdapterRecordTexts.PromptFormat("OwnerClan", ("CLAN", text5)));
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add(AdapterRecordTexts.PromptFormat("TargetHero", ("HERO", text8)));
		}
		else if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add(AdapterRecordTexts.PromptFormat("TargetClan", ("CLAN", text7)));
		}
		else if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add(AdapterRecordTexts.PromptFormat("TargetKingdom", ("KINGDOM", text6)));
		}
		if (entry.Won.HasValue)
		{
			list.Add(AdapterRecordTexts.PromptFormat("Result", ("OUTCOME", AdapterRecordTexts.Prompt(entry.Won.Value ? "Victory" : "Defeat"))));
		}
		if (entry.IsMajor)
		{
			list.Add(AdapterRecordTexts.Prompt("MajorAction"));
		}
		if (list.Count == 0)
		{
			return AdapterRecordTexts.Prompt("NoSummary");
		}
		if (!port.HasStructuredNpcActionMetadata(entry))
		{
			list.Add(AdapterRecordTexts.Prompt("SparseMetadata"));
		}
		return string.Join(AdapterRecordTexts.Prompt("Separator"), list) + AdapterRecordTexts.Prompt("Period");
	}
}

