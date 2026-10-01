using System;using System.Collections.Generic;using System.Linq;using System.Text;
namespace AnimusForge;
internal enum HeroPromptIdentityKind { Unknown, Wanderer, Mercenary, Ruler, ClanLeader, Lord, Notable, Merchant, Artisan, GangLeader, Headman, Preacher, RuralNotable, Common }
internal static class PersonaIntroTextRules {
 internal static string ComposeHeroIdentityTitle(HeroPromptIdentityKind kind,string name="") => kind switch {
  HeroPromptIdentityKind.Unknown=>"未知身份",HeroPromptIdentityKind.Wanderer=>"流浪者",
  HeroPromptIdentityKind.Mercenary=>name+"的雇佣兵",HeroPromptIdentityKind.Ruler=>string.IsNullOrWhiteSpace(name)?"统治者":name+"的统治者",
  HeroPromptIdentityKind.ClanLeader=>string.IsNullOrWhiteSpace(name)?"家族族长":name.EndsWith("家族",StringComparison.Ordinal)?name+"的族长":name+"家族的族长",
  HeroPromptIdentityKind.Lord=>string.IsNullOrWhiteSpace(name)?"领主":name+"的封臣",HeroPromptIdentityKind.Notable=>"地方要人",
  HeroPromptIdentityKind.Merchant=>"商人",HeroPromptIdentityKind.Artisan=>"工匠",HeroPromptIdentityKind.GangLeader=>"帮派首领",
  HeroPromptIdentityKind.Headman=>"村长",HeroPromptIdentityKind.Preacher=>"教士",HeroPromptIdentityKind.RuralNotable=>"乡绅",_=>"普通角色"
 };
 internal static bool ShouldHideSceneReputation(bool heroQualified,string role) => heroQualified||string.Equals((role??"").Trim(),"士兵",StringComparison.Ordinal);
 internal static string ComposeEquipmentSummary(string context, List<string> worn, List<string> weapons)
 {
  StringBuilder text=new StringBuilder();text.Append(context).Append("：").Append(worn.Count>0?string.Join("、",worn):"赤身裸体");
  if(weapons.Count>0)text.Append("，携带的武器：").Append(string.Join("、",weapons));return text.ToString();
 }
internal static string BuildAgeBracketLabel(float age)
	{
		if (age >= 60f)
		{
			return "老年";
		}
		if (age >= 46f)
		{
			return "中年";
		}
		if (age >= 30f)
		{
			return "壮年";
		}
		if (age >= 18f)
		{
			return "青年";
		}
		if (age > 0f)
		{
			return "少年";
		}
		return "未知";
	}
internal static string GetClanTierReputationLabel(int tier)
	{
		int num = Math.Max(0, tier);
		if (num <= 0)
		{
			return "身份低微";
		}
		return num switch
		{
			1 => "小有名气",
			2 => "崭露新贵",
			3 => "声名清贵",
			4 => "门第高华",
			5 => "威权显赫",
			_ => "贵不可言",
		};
	}
internal static string GetEquipmentContextLabelForPrompt(bool useCivilianEquipment)
	{
		return useCivilianEquipment ? "常服" : "战斗装";
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
		string title = isHeroNpcLord ? text + "携带的所有物资和财富" : text + "的商铺可用财富与物品";
		return "【" + title + "】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说)";
	}
 internal static void AddEquipmentItem(Dictionary<string,int> counts,Dictionary<string,string> names,string itemId,string slotName,string itemName)
 {
  string key=(itemId??"").Trim();if(string.IsNullOrWhiteSpace(key))key=slotName;
  string name=(itemName??"").Trim();if(string.IsNullOrWhiteSpace(name))name=key;
  if(!counts.ContainsKey(key)){counts[key]=0;names[key]=name;}counts[key]++;
 }
}
