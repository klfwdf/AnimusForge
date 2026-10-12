using System;using System.Collections.Generic;using System.Linq;using System.Text;
namespace AnimusForge;
internal enum HeroPromptIdentityKind { Unknown, Wanderer, Mercenary, Ruler, ClanLeader, Lord, Notable, Merchant, Artisan, GangLeader, Headman, Preacher, RuralNotable, Common }
internal static class PersonaIntroTextRules {
 internal static string ComposeHeroIdentityTitle(HeroPromptIdentityKind kind,string name="") => kind switch {
  HeroPromptIdentityKind.Unknown=>PromptComposerTexts.Get("identity_unknown"),HeroPromptIdentityKind.Wanderer=>PromptComposerTexts.Get("identity_wanderer"),
  HeroPromptIdentityKind.Mercenary=>PromptComposerTexts.Format("identity_mercenary",("NAME",name)),HeroPromptIdentityKind.Ruler=>string.IsNullOrWhiteSpace(name)?PromptComposerTexts.Get("identity_ruler"):PromptComposerTexts.Format("identity_named_ruler",("NAME",name)),
  HeroPromptIdentityKind.ClanLeader=>string.IsNullOrWhiteSpace(name)?PromptComposerTexts.Get("identity_clan_leader"):PromptComposerTexts.Format(name.EndsWith(PromptComposerTexts.Raw("identity_clan_suffix"),StringComparison.Ordinal)?"identity_named_clan_leader":"identity_named_clan_leader_add_suffix",("NAME",name)),
  HeroPromptIdentityKind.Lord=>string.IsNullOrWhiteSpace(name)?PromptComposerTexts.Get("identity_lord"):PromptComposerTexts.Format("identity_vassal",("NAME",name)),HeroPromptIdentityKind.Notable=>PromptComposerTexts.Get("identity_notable"),
  HeroPromptIdentityKind.Merchant=>PromptComposerTexts.Get("identity_merchant"),HeroPromptIdentityKind.Artisan=>PromptComposerTexts.Get("identity_artisan"),HeroPromptIdentityKind.GangLeader=>PromptComposerTexts.Get("identity_gang_leader"),
  HeroPromptIdentityKind.Headman=>PromptComposerTexts.Get("identity_headman"),HeroPromptIdentityKind.Preacher=>PromptComposerTexts.Get("identity_preacher"),HeroPromptIdentityKind.RuralNotable=>PromptComposerTexts.Get("identity_rural_notable"),_=>PromptComposerTexts.Get("identity_common")
 };
 internal static bool ShouldHideSceneReputation(bool heroQualified,string role) => heroQualified||PromptComposerTexts.IsLabel((role??"").Trim(),"identity_soldier");
 internal static string ComposeEquipmentSummary(string context, List<string> worn, List<string> weapons)
 {
  StringBuilder text=new StringBuilder();text.Append(context).Append(PromptComposerTexts.Get("equipment_colon")).Append(worn.Count>0?string.Join(PromptComposerTexts.Get("equipment_separator"),worn):PromptComposerTexts.Get("equipment_naked"));
  if(weapons.Count>0)text.Append(PromptComposerTexts.Get("equipment_weapons")).Append(string.Join(PromptComposerTexts.Get("equipment_separator"),weapons));return text.ToString();
 }
internal static string BuildAgeBracketLabel(float age)
	{
		if (age >= 60f)
		{
			return PromptComposerTexts.Get("age_elderly");
		}
		if (age >= 46f)
		{
			return PromptComposerTexts.Get("age_middle");
		}
		if (age >= 30f)
		{
			return PromptComposerTexts.Get("age_prime");
		}
		if (age >= 18f)
		{
			return PromptComposerTexts.Get("age_young_adult");
		}
		if (age > 0f)
		{
			return PromptComposerTexts.Get("age_youth");
		}
		return PromptComposerTexts.Get("age_unknown");
	}
internal static string GetClanTierReputationLabel(int tier)
	{
		int num = Math.Max(0, tier);
		if (num <= 0)
		{
			return PromptComposerTexts.Get("reputation_humble");
		}
		return num switch
		{
			1 => PromptComposerTexts.Get("reputation_known"),
			2 => PromptComposerTexts.Get("reputation_rising"),
			3 => PromptComposerTexts.Get("reputation_respected"),
			4 => PromptComposerTexts.Get("reputation_highborn"),
			5 => PromptComposerTexts.Get("reputation_powerful"),
			_ => PromptComposerTexts.Get("reputation_exalted"),
		};
	}
internal static string GetEquipmentContextLabelForPrompt(bool useCivilianEquipment)
	{
		return PromptComposerTexts.Get(useCivilianEquipment ? "equipment_civilian" : "equipment_battle");
	}
internal static List<string> BuildEquipmentSummaryItemLinesForPrompt(Dictionary<string, int> counts, Dictionary<string, string> names, int maxEntries)
	{
		if (counts == null || counts.Count == 0)
		{
			return new List<string>();
		}
		return (from x in (from x in counts.Select(delegate(KeyValuePair<string, int> kv)
				{
					string value;
					string name = (names != null && names.TryGetValue(kv.Key, out value)) ? value : kv.Key;
					return new
					{
						Name = name,
						Count = kv.Value
					};
				})
				orderby x.Count descending
				select x).ThenBy(x => x.Name, StringComparer.Ordinal).Take(Math.Max(1, maxEntries))
			select x.Name + "x" + x.Count).ToList();
	}
internal static string BuildNpcInventorySummaryHeader(string npcName, bool isHeroNpcLord = false)
	{
		string text = (npcName ?? "").Replace("\r", "").Replace("\n", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		string title = PromptComposerTexts.Format(isHeroNpcLord ? "inventory_carried_title" : "inventory_shop_title", ("NAME", text));
		return "【" + title + PromptComposerTexts.Get("inventory_limit_suffix");
	}
 internal static void AddEquipmentItem(Dictionary<string,int> counts,Dictionary<string,string> names,string itemId,string slotName,string itemName)
 {
  string key=(itemId??"").Trim();if(string.IsNullOrWhiteSpace(key))key=slotName;
  string name=(itemName??"").Trim();if(string.IsNullOrWhiteSpace(name))name=key;
  if(!counts.ContainsKey(key)){counts[key]=0;names[key]=name;}counts[key]++;
 }
}
