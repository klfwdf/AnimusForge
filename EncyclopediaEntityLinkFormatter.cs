using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AnimusForge;

// Formats a disposable RichText UI copy only; callers must keep the original reply for TTS, history, and LLM context.
internal static class EncyclopediaEntityLinkFormatter
{
	private const char LinkPlaceholderStart = '\uE000';

	private const char LinkPlaceholderEnd = '\uE001';

	private sealed class LinkCandidate
	{
		public readonly string Name;

		public readonly string Markup;

		public readonly int Order;

		public LinkCandidate(string name, string markup, int order)
		{
			Name = name;
			Markup = markup;
			Order = order;
		}
	}

	// Prevent untrusted model output from being interpreted as RichText or TextObject syntax before native markup is inserted.
	internal static string SanitizeUntrustedRichText(string value)
	{
		return (value ?? string.Empty)
			.Replace("<", "＜")
			.Replace(">", "＞")
			.Replace("{", "（")
			.Replace("}", "）");
	}

	// A display session snapshots names only for one opened UI, so multi-line history never rescans every campaign entity per row.
	internal static DisplaySession CreateDisplaySession()
	{
		return new DisplaySession();
	}

	internal sealed class DisplaySession
	{
		private enum DescriptorKind
		{
			Hero,
			Settlement,
			Clan,
			Kingdom,
			Troop
		}

		private sealed class LinkDescriptor
		{
			public readonly string Name;
			public readonly object Entity;
			public readonly DescriptorKind Kind;
			public readonly int Order;

			public LinkDescriptor(string name, object entity, DescriptorKind kind, int order)
			{
				Name = name;
				Entity = entity;
				Kind = kind;
				Order = order;
			}

			public TextObject GetNativeLink()
			{
				switch (Kind)
				{
					case DescriptorKind.Hero:
						return ((Hero)Entity).EncyclopediaLinkWithName;
					case DescriptorKind.Settlement:
						return ((Settlement)Entity).EncyclopediaLinkWithName;
					case DescriptorKind.Clan:
						return ((Clan)Entity).EncyclopediaLinkWithName;
					case DescriptorKind.Kingdom:
						return ((Kingdom)Entity).EncyclopediaLinkWithName;
					case DescriptorKind.Troop:
						return ((CharacterObject)Entity).EncyclopediaLinkWithName;
					default:
						return null;
				}
			}
		}

		private readonly Dictionary<int, List<LinkDescriptor>> _descriptorsByPrefix = new Dictionary<int, List<LinkDescriptor>>();

		private readonly Dictionary<string, LinkDescriptor> _descriptorsByName = new Dictionary<string, LinkDescriptor>(StringComparer.Ordinal);

		private int _nextOrder;

		internal DisplaySession()
		{
			BuildDescriptorSnapshot();
		}

		// The caller owns this disposable UI copy; raw history, reports, letters, AFEF, TTS and LLM input remain untouched.
		internal string Format(string rawText, Hero conversationTargetHero = null, CharacterObject conversationTargetCharacter = null)
		{
			string text = SanitizeUntrustedRichText(rawText);
			if (string.IsNullOrWhiteSpace(text) || text.IndexOf(LinkPlaceholderStart) >= 0 || text.IndexOf(LinkPlaceholderEnd) >= 0)
			{
				return text;
			}

			HashSet<int> textTwoCharacterPairs = BuildTextTwoCharacterPairs(text);
			List<LinkCandidate> candidates = new List<LinkCandidate>();
			HashSet<string> seenNames = new HashSet<string>(StringComparer.Ordinal);
			int replacementOrder = 0;
			AddConversationContextCandidates(text, textTwoCharacterPairs, conversationTargetHero, conversationTargetCharacter, candidates, seenNames, ref replacementOrder);

			// The prefix buckets reduce a report line to only names that can actually occur in that line.
			Dictionary<string, LinkDescriptor> matchingByName = new Dictionary<string, LinkDescriptor>(StringComparer.Ordinal);
			foreach (int pair in textTwoCharacterPairs)
			{
				if (!_descriptorsByPrefix.TryGetValue(pair, out List<LinkDescriptor> descriptors))
				{
					continue;
				}
				for (int index = 0; index < descriptors.Count; index++)
				{
					LinkDescriptor descriptor = descriptors[index];
					if (!matchingByName.ContainsKey(descriptor.Name))
					{
						matchingByName.Add(descriptor.Name, descriptor);
					}
				}
			}

			List<LinkDescriptor> matchingDescriptors = new List<LinkDescriptor>(matchingByName.Values);
			matchingDescriptors.Sort(delegate(LinkDescriptor left, LinkDescriptor right)
			{
				return left.Order.CompareTo(right.Order);
			});
			for (int index = 0; index < matchingDescriptors.Count; index++)
			{
				LinkDescriptor descriptor = matchingDescriptors[index];
				if (seenNames.Contains(descriptor.Name) || text.IndexOf(descriptor.Name, StringComparison.Ordinal) < 0)
				{
					continue;
				}
				try
				{
					AddLinkCandidate(descriptor.Name, descriptor.GetNativeLink(), candidates, seenNames, ref replacementOrder);
				}
				catch
				{
					// A campaign entity disappearing while its popup is open leaves only that name as plain text.
				}
			}
			return ReplaceMatchesWithNativeLinks(text, candidates);
		}

		private void BuildDescriptorSnapshot()
		{
			AddHeroDescriptors();
			AddSettlementDescriptors();
			AddClanDescriptors();
			AddKingdomDescriptors();
			AddTroopDescriptors();
		}

		private void AddHeroDescriptors()
		{
			try
			{
				var allHeroes = Hero.AllAliveHeroes;
				for (int index = 0; index < allHeroes.Count; index++)
				{
					Hero hero = allHeroes[index];
					if (hero != null)
					{
						AddDescriptor(hero.Name?.ToString(), hero, DescriptorKind.Hero);
					}
				}
			}
			catch
			{
				// Campaign collections are transient during save/load; an incomplete UI snapshot is still safe to display.
			}
		}

		private void AddSettlementDescriptors()
		{
			try
			{
				var allSettlements = Settlement.All;
				for (int index = 0; index < allSettlements.Count; index++)
				{
					Settlement settlement = allSettlements[index];
					if (settlement != null)
					{
						AddDescriptor(settlement.Name?.ToString(), settlement, DescriptorKind.Settlement);
					}
				}
			}
			catch
			{
				// Keep the popup usable if settlement data is not ready yet.
			}
		}

		private void AddClanDescriptors()
		{
			try
			{
				var allClans = Clan.All;
				for (int index = 0; index < allClans.Count; index++)
				{
					Clan clan = allClans[index];
					if (clan != null)
					{
						AddDescriptor(clan.Name?.ToString(), clan, DescriptorKind.Clan);
					}
				}
			}
			catch
			{
				// Keep the popup usable if clan data is not ready yet.
			}
		}

		private void AddKingdomDescriptors()
		{
			try
			{
				var allKingdoms = Kingdom.All;
				for (int index = 0; index < allKingdoms.Count; index++)
				{
					Kingdom kingdom = allKingdoms[index];
					if (kingdom == null)
					{
						continue;
					}
					AddDescriptor(kingdom.Name?.ToString(), kingdom, DescriptorKind.Kingdom);
					AddDescriptor(kingdom.InformalName?.ToString(), kingdom, DescriptorKind.Kingdom);
				}
			}
			catch
			{
				// Keep the popup usable if kingdom data is not ready yet.
			}
		}

		private void AddTroopDescriptors()
		{
			try
			{
				var allCharacters = CharacterObject.All;
				for (int index = 0; index < allCharacters.Count; index++)
				{
					CharacterObject troop = allCharacters[index];
					if (troop != null && !troop.IsHero)
					{
						AddDescriptor(troop.Name?.ToString(), troop, DescriptorKind.Troop);
					}
				}
			}
			catch
			{
				// Keep the popup usable if character data is not ready yet.
			}
		}

		private void AddDescriptor(string rawName, object entity, DescriptorKind kind)
		{
			string name = (rawName ?? string.Empty).Trim();
			if (entity == null || name.Length < 2 || _descriptorsByName.ContainsKey(name))
			{
				return;
			}
			LinkDescriptor descriptor = new LinkDescriptor(name, entity, kind, _nextOrder++);
			_descriptorsByName.Add(name, descriptor);
			int prefix = PackTwoCharacters(name[0], name[1]);
			if (!_descriptorsByPrefix.TryGetValue(prefix, out List<LinkDescriptor> descriptors))
			{
				descriptors = new List<LinkDescriptor>();
				_descriptorsByPrefix.Add(prefix, descriptors);
			}
			descriptors.Add(descriptor);
		}
	}

	// This deliberately runs only once for a completed reply, never from a streaming or campaign-tick path.
	internal static string FormatNativeConversationText(string rawText, Hero conversationTargetHero, CharacterObject conversationTargetCharacter)
	{
		string text = SanitizeUntrustedRichText(rawText);
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		// Private-use delimiters make replacements independent of names that overlap each other or link markup.
		if (text.IndexOf(LinkPlaceholderStart) >= 0 || text.IndexOf(LinkPlaceholderEnd) >= 0)
		{
			return text;
		}
		List<LinkCandidate> candidates = new List<LinkCandidate>();
		HashSet<string> seenNames = new HashSet<string>(StringComparer.Ordinal);
		// A single reply-length pass rejects nearly all entity names before any whole-text IndexOf call is needed.
		HashSet<int> textTwoCharacterPairs = BuildTextTwoCharacterPairs(text);
		int order = 0;
		AddConversationContextCandidates(text, textTwoCharacterPairs, conversationTargetHero, conversationTargetCharacter, candidates, seenNames, ref order);
		AddHeroLinkCandidates(text, textTwoCharacterPairs, candidates, seenNames, ref order);
		AddSettlementLinkCandidates(text, textTwoCharacterPairs, candidates, seenNames, ref order);
		AddClanLinkCandidates(text, textTwoCharacterPairs, candidates, seenNames, ref order);
		AddKingdomLinkCandidates(text, textTwoCharacterPairs, candidates, seenNames, ref order);
		AddTroopLinkCandidates(text, textTwoCharacterPairs, candidates, seenNames, ref order);
		return ReplaceMatchesWithNativeLinks(text, candidates);
	}

	// NPC and 玩家 are contextual tokens: their visible label becomes the current conversation target or the player hero.
	private static void AddConversationContextCandidates(string text, HashSet<int> textTwoCharacterPairs, Hero conversationTargetHero, CharacterObject conversationTargetCharacter, List<LinkCandidate> candidates, HashSet<string> seenNames, ref int order)
	{
		if (TryGetMentionedName(text, "NPC", textTwoCharacterPairs, seenNames, out string npcToken))
		{
			try
			{
				TextObject targetLink = conversationTargetHero != null
					? conversationTargetHero.EncyclopediaLinkWithName
					: conversationTargetCharacter?.EncyclopediaLinkWithName;
				AddLinkCandidate(npcToken, targetLink, candidates, seenNames, ref order);
			}
			catch
			{
				// A transient conversation target must leave the reply readable rather than interrupting its display.
			}
		}
		if (!TryGetMentionedName(text, "玩家", textTwoCharacterPairs, seenNames, out string playerToken))
		{
			return;
		}
		try
		{
			AddLinkCandidate(playerToken, Hero.MainHero?.EncyclopediaLinkWithName, candidates, seenNames, ref order);
		}
		catch
		{
			// Loading screens can temporarily withhold MainHero; keep the plain 玩家 token in that case.
		}
	}

	// Hero names are considered before general troop names so an identical label always resolves to the named character.
	private static void AddHeroLinkCandidates(string text, HashSet<int> textTwoCharacterPairs, List<LinkCandidate> candidates, HashSet<string> seenNames, ref int order)
	{
		try
		{
			var allHeroes = Hero.AllAliveHeroes;
			for (int index = 0; index < allHeroes.Count; index++)
			{
				Hero hero = allHeroes[index];
				if (hero == null || !TryGetMentionedName(text, hero.Name?.ToString(), textTwoCharacterPairs, seenNames, out string name))
				{
					continue;
				}
				try
				{
					AddLinkCandidate(name, hero.EncyclopediaLinkWithName, candidates, seenNames, ref order);
				}
				catch
				{
					// A malformed hero entry is skipped without degrading the rest of the completed reply.
				}
			}
		}
		catch
		{
			// Campaign collections can be unavailable during a save/load transition; later replies will retry naturally.
		}
	}

	// Settlement collection access is bounded to this one completed-reply pass, never a UI frame or stream fragment.
	private static void AddSettlementLinkCandidates(string text, HashSet<int> textTwoCharacterPairs, List<LinkCandidate> candidates, HashSet<string> seenNames, ref int order)
	{
		try
		{
			var allSettlements = Settlement.All;
			for (int index = 0; index < allSettlements.Count; index++)
			{
				Settlement settlement = allSettlements[index];
				if (settlement == null || !TryGetMentionedName(text, settlement.Name?.ToString(), textTwoCharacterPairs, seenNames, out string name))
				{
					continue;
				}
				try
				{
					AddLinkCandidate(name, settlement.EncyclopediaLinkWithName, candidates, seenNames, ref order);
				}
				catch
				{
					// A single unavailable settlement must not prevent the rest of the links from rendering.
				}
			}
		}
		catch
		{
			// Preserve the plain reply if settlement data is unavailable while campaign state is changing.
		}
	}

	// Clan links cover both player-created and native families without relying on a localized name convention.
	private static void AddClanLinkCandidates(string text, HashSet<int> textTwoCharacterPairs, List<LinkCandidate> candidates, HashSet<string> seenNames, ref int order)
	{
		try
		{
			var allClans = Clan.All;
			for (int index = 0; index < allClans.Count; index++)
			{
				Clan clan = allClans[index];
				if (clan == null || !TryGetMentionedName(text, clan.Name?.ToString(), textTwoCharacterPairs, seenNames, out string name))
				{
					continue;
				}
				try
				{
					AddLinkCandidate(name, clan.EncyclopediaLinkWithName, candidates, seenNames, ref order);
				}
				catch
				{
					// Keep one malformed clan record local to this candidate rather than failing the entire answer.
				}
			}
		}
		catch
		{
			// Leave ordinary text intact if the clan registry is not ready yet.
		}
	}

	// Kingdom replies may use either official or informal labels; both map to the same native encyclopedia entry.
	private static void AddKingdomLinkCandidates(string text, HashSet<int> textTwoCharacterPairs, List<LinkCandidate> candidates, HashSet<string> seenNames, ref int order)
	{
		try
		{
			var allKingdoms = Kingdom.All;
			for (int index = 0; index < allKingdoms.Count; index++)
			{
				Kingdom kingdom = allKingdoms[index];
				if (kingdom == null)
				{
					continue;
				}
				bool hasOfficialName = TryGetMentionedName(text, kingdom.Name?.ToString(), textTwoCharacterPairs, seenNames, out string officialName);
				bool hasInformalName = TryGetMentionedName(text, kingdom.InformalName?.ToString(), textTwoCharacterPairs, seenNames, out string informalName);
				if (!hasOfficialName && !hasInformalName)
				{
					continue;
				}
				try
				{
					TextObject link = kingdom.EncyclopediaLinkWithName;
					if (hasOfficialName)
					{
						AddLinkCandidate(officialName, link, candidates, seenNames, ref order);
					}
					if (hasInformalName)
					{
						AddLinkCandidate(informalName, link, candidates, seenNames, ref order);
					}
				}
				catch
				{
					// An unavailable kingdom page leaves only that kingdom name as ordinary text.
				}
			}
		}
		catch
		{
			// The reply remains usable if the kingdom registry changes during the display pass.
		}
	}

	// Only non-hero troop templates are added here because heroes were resolved first with their hero encyclopedia pages.
	private static void AddTroopLinkCandidates(string text, HashSet<int> textTwoCharacterPairs, List<LinkCandidate> candidates, HashSet<string> seenNames, ref int order)
	{
		try
		{
			var allCharacters = CharacterObject.All;
			for (int index = 0; index < allCharacters.Count; index++)
			{
				CharacterObject troop = allCharacters[index];
				if (troop == null || troop.IsHero || !TryGetMentionedName(text, troop.Name?.ToString(), textTwoCharacterPairs, seenNames, out string name))
				{
					continue;
				}
				try
				{
					AddLinkCandidate(name, troop.EncyclopediaLinkWithName, candidates, seenNames, ref order);
				}
				catch
				{
					// Hidden or invalid troop encyclopedia entries are safely ignored.
				}
			}
		}
		catch
		{
			// Never let a temporary character-registry failure suppress a completed conversation reply.
		}
	}

	// Short labels are intentionally excluded: one-character Chinese terms are too ambiguous in natural dialogue.
	private static bool TryGetMentionedName(string text, string rawName, HashSet<int> textTwoCharacterPairs, HashSet<string> seenNames, out string name)
	{
		name = (rawName ?? string.Empty).Trim();
		return name.Length >= 2
			&& !seenNames.Contains(name)
			&& textTwoCharacterPairs.Contains(PackTwoCharacters(name[0], name[1]))
			&& text.IndexOf(name, StringComparison.Ordinal) >= 0;
	}

	// Packing UTF-16 pairs avoids per-candidate substring allocation while keeping the prefilter exact for every two-code-unit name prefix.
	private static HashSet<int> BuildTextTwoCharacterPairs(string text)
	{
		HashSet<int> pairs = new HashSet<int>();
		for (int index = 0; index + 1 < text.Length; index++)
		{
			pairs.Add(PackTwoCharacters(text[index], text[index + 1]));
		}
		return pairs;
	}

	// The packed int is collision-free because each UTF-16 code unit occupies one fixed half of the value.
	private static int PackTwoCharacters(char first, char second)
	{
		return (first << 16) | second;
	}

	// Native TextObject links are the sole allowed markup source, so LLM-provided links can never drive encyclopedia navigation.
	private static void AddLinkCandidate(string name, TextObject nativeLink, List<LinkCandidate> candidates, HashSet<string> seenNames, ref int order)
	{
		if (string.IsNullOrWhiteSpace(name) || nativeLink == null || seenNames.Contains(name))
		{
			return;
		}
		string markup = nativeLink.ToString();
		if (string.IsNullOrWhiteSpace(markup) || markup.IndexOf("<a ", StringComparison.Ordinal) < 0 || markup.IndexOf("href=", StringComparison.Ordinal) < 0)
		{
			return;
		}
		if (seenNames.Add(name))
		{
			candidates.Add(new LinkCandidate(name, markup, order++));
		}
	}

	// Placeholders prevent subsequent shorter-name passes from matching inside the trusted link markup we inject.
	private static string ReplaceMatchesWithNativeLinks(string text, List<LinkCandidate> candidates)
	{
		if (candidates.Count == 0)
		{
			return text;
		}
		candidates.Sort(delegate(LinkCandidate left, LinkCandidate right)
		{
			int lengthComparison = right.Name.Length.CompareTo(left.Name.Length);
			return lengthComparison != 0 ? lengthComparison : left.Order.CompareTo(right.Order);
		});
		List<LinkCandidate> replacements = new List<LinkCandidate>();
		for (int index = 0; index < candidates.Count; index++)
		{
			LinkCandidate candidate = candidates[index];
			if (text.IndexOf(candidate.Name, StringComparison.Ordinal) < 0)
			{
				continue;
			}
			string placeholder = CreatePlaceholder(replacements.Count);
			text = text.Replace(candidate.Name, placeholder);
			replacements.Add(candidate);
		}
		for (int index = 0; index < replacements.Count; index++)
		{
			text = text.Replace(CreatePlaceholder(index), replacements[index].Markup);
		}
		return text;
	}

	// Delimiters are private-use code points and checked above, so a plain reply cannot collide with a generated placeholder.
	private static string CreatePlaceholder(int index)
	{
		return string.Concat(LinkPlaceholderStart.ToString(), index.ToString(), LinkPlaceholderEnd.ToString());
	}
}
