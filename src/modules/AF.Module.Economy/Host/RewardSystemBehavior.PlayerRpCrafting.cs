using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
	// Player RP crafting is split from the large reward behavior so the UI, save overlay,
	// and three-channel AFEF entry points share one small, reviewable implementation.
	private const string PlayerRpCraftedAfefMarker = "*你的智识分辨出来了该物品由玩家亲手制造*";

	private const string PlayerRpGeneratedItemPrefix = "af_generated_reward_pc_";

	private const string PlayerRpCraftTerminalInvalidKind = "terminal_invalid";

	private const int PlayerRpTemplateCandidateLimit = 50;

	private const int PlayerRpTemplatePricePerSmithingLevel = 1000;

	private const int PlayerRpMasterSmithingLevel = 275;

	// Each surplus-investment doubling adds three points to a normal result;
	// the good result intentionally keeps its two-to-one bonus relationship.
	private const int PlayerRpNormalAttributeBonusPerUpgradeLevel = 3;

	private const int PlayerRpGoodAttributeBonusPerUpgradeLevel =
		PlayerRpNormalAttributeBonusPerUpgradeLevel * 2;

	private const int PlayerRpMasterNormalAttributeBonus = 3;

	private const int PlayerRpMasterGoodAttributeBonus =
		PlayerRpMasterNormalAttributeBonus * 2;

	private static readonly object PlayerRpCraftGenerationAuthorizationLock = new object();

	private static readonly HashSet<string> ActivePlayerRpCraftGenerationKeys =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private sealed class PlayerRpExactTemplateLookupCache
	{
		public readonly Dictionary<string, List<ItemObject>> ByStringId =
			new Dictionary<string, List<ItemObject>>(StringComparer.OrdinalIgnoreCase);

		public readonly Dictionary<string, List<ItemObject>> ByDisplayName =
			new Dictionary<string, List<ItemObject>>(StringComparer.OrdinalIgnoreCase);
	}

	private static readonly object PlayerRpExactTemplateLookupCacheLock =
		new object();

	private static object PlayerRpExactTemplateLookupCacheOwner;

	private static PlayerRpExactTemplateLookupCache PlayerRpExactTemplateLookup =
		new PlayerRpExactTemplateLookupCache();

	private static bool PlayerRpExactTemplateLookupCacheReady;

	private static DateTime PlayerRpExactTemplateLookupCacheRetryAfterUtc =
		DateTime.MinValue;

	internal static bool TryGetAvailablePlayerRpCraftersForExternal(
		out List<PlayerRpCrafterOption> crafters,
		out string error)
	{
		crafters = new List<PlayerRpCrafterOption>();
		error = "";
		Hero mainHero = Hero.MainHero;
		if (mainHero == null || PartyBase.MainParty?.MemberRoster == null)
		{
			error = "玩家主队伍当前不可用。";
			return false;
		}
		ICraftingCampaignBehavior craftingBehavior =
			Campaign.Current?.GetCampaignBehavior<ICraftingCampaignBehavior>();

		HashSet<string> seenHeroIds =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		TryAddPlayerRpCrafterOption(
			crafters,
			seenHeroIds,
			mainHero,
			craftingBehavior);
		foreach (TroopRosterElement element in
			PartyBase.MainParty.MemberRoster.GetTroopRoster())
		{
			Hero hero = element.Character?.HeroObject;
			if (element.Number <= 0
				|| hero == null
				|| hero == mainHero
				|| !AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(hero))
			{
				continue;
			}
			TryAddPlayerRpCrafterOption(
				crafters,
				seenHeroIds,
				hero,
				craftingBehavior);
		}
		if (crafters.Count == 0)
		{
			error = "当前主队伍中没有可用的家族成员或同伴。";
			return false;
		}
		return true;
	}

	private static void TryAddPlayerRpCrafterOption(
		List<PlayerRpCrafterOption> crafters,
		HashSet<string> seenHeroIds,
		Hero hero,
		ICraftingCampaignBehavior craftingBehavior)
	{
		string heroId = (hero?.StringId ?? "").Trim();
		if (hero == null
			|| hero.IsDead
			|| hero.IsChild
			|| hero.IsPrisoner
			|| string.IsNullOrWhiteSpace(heroId)
			|| !seenHeroIds.Add(heroId))
		{
			return;
		}
		int smithing = Math.Max(0, hero.GetSkillValue(DefaultSkills.Crafting));
		int stamina = 0;
		int maxStamina = 0;
		int staminaCost = 0;
		bool hasStamina = false;
		if (craftingBehavior != null)
		{
			hasStamina = TryReadPlayerRpCrafterCraftingResources(
				hero,
				craftingBehavior,
				out stamina,
				out maxStamina,
				out staminaCost,
				out _);
		}
		crafters.Add(new PlayerRpCrafterOption
		{
			HeroId = heroId,
			DisplayName = hero.Name?.ToString() ?? heroId,
			SmithingSkill = smithing,
			CraftingStamina = stamina,
			MaxCraftingStamina = maxStamina,
			HasCraftingStamina = hasStamina,
			CraftingStaminaCost = staminaCost,
			HasEnoughCraftingStamina =
				hasStamina && stamina >= staminaCost
		});
	}

	private static bool TryResolveAvailablePlayerRpCrafter(
		string crafterHeroId,
		out Hero crafter,
		out string error)
	{
		crafter = null;
		error = "";
		string heroId = (crafterHeroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(heroId))
		{
			error = "尚未选择制造者。";
			return false;
		}
		try
		{
			crafter = Hero.Find(heroId) ?? Hero.FindFirst(hero =>
				hero != null
				&& string.Equals(
					(hero.StringId ?? "").Trim(),
					heroId,
					StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			crafter = null;
		}
		if (crafter == null
			|| crafter.IsDead
			|| crafter.IsChild
			|| crafter.IsPrisoner
			|| (crafter != Hero.MainHero
				&& !AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(
					crafter)))
		{
			error = "所选制造者已经不可用，请重新选择。";
			return false;
		}
		if (crafter != Hero.MainHero)
		{
			try
			{
				Hero selectedCrafter = crafter;
				bool inMainParty = PartyBase.MainParty?.MemberRoster?
					.GetTroopRoster()
					.Any(element =>
						element.Number > 0
						&& element.Character?.HeroObject == selectedCrafter) == true;
				if (!inMainParty)
				{
					error = "所选制造者已经离开玩家主队伍，请重新选择。";
					crafter = null;
					return false;
				}
			}
			catch
			{
				error = "无法确认制造者仍在玩家主队伍中。";
				crafter = null;
				return false;
			}
		}
		return true;
	}

	private static bool TryReadPlayerRpCrafterCraftingResources(
		Hero crafter,
		out ICraftingCampaignBehavior behavior,
		out int stamina,
		out int maxStamina,
		out int staminaCost,
		out string error)
	{
		behavior = null;
		stamina = 0;
		maxStamina = 0;
		staminaCost = 0;
		error = "";
		if (crafter == null)
		{
			error = "所选制造者当前不可用。";
			return false;
		}
		if (crafter.HeroDeveloper == null)
		{
			error = "所选制造者当前无法获得锻造经验。";
			return false;
		}
		try
		{
			behavior =
				Campaign.Current?.GetCampaignBehavior<ICraftingCampaignBehavior>();
			if (behavior == null)
			{
				error = "原版锻造体力系统当前不可用。";
				return false;
			}
			return TryReadPlayerRpCrafterCraftingResources(
				crafter,
				behavior,
				out stamina,
				out maxStamina,
				out staminaCost,
				out error);
		}
		catch (Exception ex)
		{
			error = "无法读取所选制造者的锻造体力。";
			try
			{
				Logger.Log(
					"Logic",
					"[PlayerRpCraft] stamina_read_unavailable crafter="
						+ (crafter.StringId ?? "")
						+ " exception="
						+ ex.GetType().Name);
			}
			catch
			{
			}
			behavior = null;
			return false;
		}
	}

	private static bool TryReadPlayerRpCrafterCraftingResources(
		Hero crafter,
		ICraftingCampaignBehavior behavior,
		out int stamina,
		out int maxStamina,
		out int staminaCost,
		out string error)
	{
		stamina = 0;
		maxStamina = 0;
		staminaCost = 0;
		error = "";
		if (crafter == null
			|| crafter.HeroDeveloper == null
			|| behavior == null)
		{
			error = "原版锻造体力系统当前不可用。";
			return false;
		}
		try
		{
			stamina = Math.Max(
				0,
				behavior.GetHeroCraftingStamina(crafter));
			maxStamina = behavior.GetMaxHeroCraftingStamina(crafter);
			if (maxStamina <= 0)
			{
				error = "所选制造者的最大锻造体力不可用。";
				stamina = 0;
				maxStamina = 0;
				return false;
			}
			staminaCost = GetPlayerRpCraftingStaminaCost(maxStamina);
			return staminaCost > 0;
		}
		catch
		{
			error = "无法读取所选制造者的锻造体力。";
			stamina = 0;
			maxStamina = 0;
			staminaCost = 0;
			return false;
		}
	}

	private static int GetPlayerRpCraftingStaminaCost(int maxStamina)
	{
		if (maxStamina <= 0)
		{
			return 0;
		}
		long cost = ((long)maxStamina + 3L) / 4L;
		return (int)Math.Max(1L, Math.Min(int.MaxValue, cost));
	}

	private static int GetPlayerRpCraftedItemValue(int investedDenars)
	{
		return Math.Max(1, Math.Max(0, investedDenars) / 2);
	}

	private static int GetPlayerRpCraftingExperience(int craftedItemValue)
	{
		long safeValue = Math.Max(1L, craftedItemValue);
		long experience = Math.Max(1L, (safeValue + 25L) / 50L);
		return (int)Math.Min(int.MaxValue, experience);
	}

	internal static bool TryPreviewPlayerRpCraftForExternal(
		string requestedName,
		int investedDenars,
		bool isEquipment,
		string crafterHeroId,
		out PlayerRpCraftPreview preview,
		out string error)
	{
		if (!TryPreviewPlayerRpCraftExactMatchForExternal(
			requestedName,
			investedDenars,
			isEquipment,
			crafterHeroId,
			out bool exactMatchFound,
			out preview,
			out error))
		{
			return false;
		}
		if (exactMatchFound)
		{
			return true;
		}
		return TryPreviewPlayerRpCraftCore(
			requestedName,
			investedDenars,
			isEquipment,
			crafterHeroId,
			null,
			"local_rank",
			null,
			out preview,
			out error);
	}

	internal static bool TryPreviewPlayerRpCraftExactMatchForExternal(
		string requestedName,
		int investedDenars,
		bool isEquipment,
		string crafterHeroId,
		out bool exactMatchFound,
		out PlayerRpCraftPreview preview,
		out string error)
	{
		preview = null;
		if (!TryResolvePlayerRpExactRegisteredTemplate(
			requestedName,
			isEquipment,
			out exactMatchFound,
			out PlayerRpCraftTemplateCandidate exactCandidate,
			out error))
		{
			return false;
		}
		if (!exactMatchFound)
		{
			return true;
		}
		List<PlayerRpCraftTemplateCandidate> exactCandidates =
			new List<PlayerRpCraftTemplateCandidate>(1)
			{
				exactCandidate
			};
		if (!TryPreviewPlayerRpCraftCore(
			requestedName,
			investedDenars,
			isEquipment,
			crafterHeroId,
			exactCandidate.TemplateStringId,
			"exact_game_item",
			exactCandidates,
			out preview,
			out error))
		{
			return false;
		}
		try
		{
			string name = (requestedName ?? "").Trim();
			Logger.Log(
				"Logic",
				"[PlayerRpCraft] exact_template_selected"
					+ " name_hash=" + StablePromptKeyHash(name)
					+ " name_length=" + name.Length.ToString(CultureInfo.InvariantCulture)
					+ " invested=" + investedDenars.ToString(CultureInfo.InvariantCulture)
					+ " mode=" + (isEquipment ? "equipment" : "misc")
					+ " template=" + (exactCandidate.TemplateStringId ?? "")
					+ " price=" + exactCandidate.StandardPrice.ToString(CultureInfo.InvariantCulture));
		}
		catch
		{
		}
		return true;
	}

	internal static bool TryPreviewPlayerRpCraftWithPlayerSelectedTemplateForExternal(
		PlayerRpCraftTemplateSelectionRequest request,
		string selectedTemplateStringId,
		out PlayerRpCraftPreview preview,
		out string error)
	{
		if (request == null)
		{
			preview = null;
			error = "模板候选已经失效。";
			return false;
		}
		return TryPreviewPlayerRpCraftCore(
			request.RequestedName,
			request.InvestedDenars,
			request.IsEquipment,
			request.CrafterHeroId,
			selectedTemplateStringId,
			"player_choice",
			request.Candidates,
			out preview,
			out error);
	}

	private static bool TryPreviewPlayerRpCraftCore(
		string requestedName,
		int investedDenars,
		bool isEquipment,
		string crafterHeroId,
		string selectedTemplateStringId,
		string selectionSource,
		IList<PlayerRpCraftTemplateCandidate> candidateSnapshot,
		out PlayerRpCraftPreview preview,
		out string error)
	{
		preview = null;
		error = "";
		string name = (requestedName ?? "").Trim();
		IList<PlayerRpCraftTemplateCandidate> candidates = candidateSnapshot;
		if (candidates == null)
		{
			if (!TryBuildPlayerRpCraftTemplateCandidates(
				name,
				investedDenars,
				isEquipment,
				PlayerRpTemplateCandidateLimit,
				out List<PlayerRpCraftTemplateCandidate> rankedCandidates,
				out error))
			{
				return false;
			}
			candidates = rankedCandidates;
		}
		else if (candidates.Count == 0)
		{
			error = "模板候选为空。";
			return false;
		}
		string selectedId = (selectedTemplateStringId ?? "").Trim();
		PlayerRpCraftTemplateCandidate selected = string.IsNullOrWhiteSpace(selectedId)
			? candidates.FirstOrDefault()
			: candidates.FirstOrDefault(candidate =>
				string.Equals(
					candidate?.TemplateStringId,
					selectedId,
					StringComparison.OrdinalIgnoreCase));
		if (selected == null)
		{
			error = "所选模板不在当前 Top 50 安全候选中。";
			return false;
		}
		if (!TryValidatePlayerRpSelectedTemplateForCurrentRequest(
			name,
			investedDenars,
			isEquipment,
			selected,
			string.Equals(
				selectionSource,
				"exact_game_item",
				StringComparison.Ordinal),
			out ItemObject template,
			out int templateBaseValue,
			out error))
		{
			return false;
		}
		if (!TryResolveAvailablePlayerRpCrafter(
			crafterHeroId,
			out Hero crafter,
			out error))
		{
			return false;
		}

		int smithing = Math.Max(
			0,
			crafter.GetSkillValue(DefaultSkills.Crafting));
		if (!TryReadPlayerRpCrafterCraftingResources(
			crafter,
			out _,
			out int crafterStamina,
			out int crafterMaxStamina,
			out int craftingStaminaCost,
			out error))
		{
			return false;
		}
		if (crafterStamina < craftingStaminaCost)
		{
			error = (crafter.Name?.ToString() ?? "所选制造者")
				+ "的锻造体力不足：需要 "
				+ craftingStaminaCost.ToString(CultureInfo.InvariantCulture)
				+ "，当前 "
				+ crafterStamina.ToString(CultureInfo.InvariantCulture)
				+ "。";
			return false;
		}
		int craftedItemValue =
			GetPlayerRpCraftedItemValue(investedDenars);
		int craftingExperience =
			GetPlayerRpCraftingExperience(craftedItemValue);
		int goodWeight = 0;
		int normalWeight = 0;
		int badWeight = 0;
		StringBuilder confirmation = new StringBuilder(isEquipment ? 256 : 768);

		if (isEquipment)
		{
			BuildPlayerRpEquipmentProbabilityWeights(smithing, templateBaseValue, out goodWeight, out normalWeight, out badWeight);
			AppendPlayerRpEquipmentProbabilityPreview(
				confirmation,
				investedDenars,
				templateBaseValue,
				smithing,
				goodWeight,
				normalWeight,
				badWeight);
		}
		else
		{
			confirmation.Append("物品名称：").Append(name)
				.Append("\n匹配模板：").Append(template.Name?.ToString() ?? template.StringId ?? "")
				.Append(" (").Append(template.StringId ?? "").Append(')')
				.Append("\n模板类型：").Append(selected.TypeLabel ?? GetPlayerRpTemplateTypeLabel(template))
				.Append("\n标准价格：").Append(templateBaseValue.ToString(CultureInfo.InvariantCulture))
				.Append(" 第纳尔\n投入金额：").Append(investedDenars.ToString(CultureInfo.InvariantCulture))
				.Append(" 第纳尔\n成品价值：").Append(craftedItemValue.ToString(CultureInfo.InvariantCulture))
				.Append(" 第纳尔");
			if (string.Equals(selectionSource, "player_choice", StringComparison.Ordinal))
			{
				confirmation.Append("\n模板选择：玩家手动选择（Top ")
					.Append(candidates.Count.ToString(CultureInfo.InvariantCulture))
					.Append(" 候选）");
			}
			else if (string.Equals(
				selectionSource,
				"exact_game_item",
				StringComparison.Ordinal))
			{
				confirmation.Append("\n模板选择：游戏数据精确匹配");
			}

			if (investedDenars >= templateBaseValue)
			{
				confirmation.Append("\n制造成功率：100%\n结果：成功制造该物品。");
			}
			else
			{
				confirmation.Append("\n制造成功率：")
					.Append(FormatPlayerRpProbability(investedDenars, templateBaseValue))
					.Append("\n未命中时：垃圾物品（投入仍会消耗）。");
			}
			confirmation.Append("\n\n确认制造？");
		}

		preview = new PlayerRpCraftPreview
		{
			CrafterHeroId = crafter.StringId ?? "",
			CrafterDisplayName =
				crafter.Name?.ToString() ?? crafter.StringId ?? "",
			CrafterCraftingStamina = crafterStamina,
			CrafterMaxCraftingStamina = crafterMaxStamina,
			CraftingStaminaCost = craftingStaminaCost,
			RequestedName = name,
			InvestedDenars = investedDenars,
			CraftedItemValue = craftedItemValue,
			CraftingExperienceBaseAmount = craftingExperience,
			IsEquipment = isEquipment,
			TemplateStringId = template.StringId ?? "",
			TemplateDisplayName = template.Name?.ToString() ?? template.StringId ?? "",
			TemplateBaseValue = templateBaseValue,
			TemplateTypeLabel = selected.TypeLabel ?? GetPlayerRpTemplateTypeLabel(template),
			TemplateCandidateCount = candidates.Count,
			SmithingSkill = smithing,
			GoodWeight = goodWeight,
			NormalWeight = normalWeight,
			BadWeight = badWeight,
			ConfirmationText = confirmation.ToString(),
			TemplateSelectionSource = selectionSource
		};
		return true;
	}

	internal static bool TryCommitPlayerRpCraftForExternal(
		PlayerRpCraftPreview preview,
		out PlayerRpCraftResult result,
		out string error)
	{
		result = null;
		error = "";
		if (preview == null)
		{
			error = "制造请求已经失效。";
			return false;
		}
		if (Interlocked.CompareExchange(ref PlayerRpCraftCommitGate, 1, 0) != 0)
		{
			error = "正在处理上一项制造，请勿重复点击。";
			return false;
		}

		bool transactionCommitted = false;
		bool chargeAttempted = false;
		Hero chargedPlayer = null;
		int goldBeforeCharge = 0;
		int chargedAmount = 0;
		ItemRoster playerRoster = null;
		ItemObject generatedItem = null;
		string generatedStringIdForRollback = null;
		Hero staminaCrafter = null;
		ICraftingCampaignBehavior staminaBehavior = null;
		int staminaBeforeDeduction = 0;
		int maxStaminaBeforeDeduction = 0;
		int staminaCost = 0;
		bool staminaAdjustmentAttempted = false;
		try
		{
			if (!TryPreviewPlayerRpCraftCore(
				preview.RequestedName,
				preview.InvestedDenars,
				preview.IsEquipment,
				preview.CrafterHeroId,
				preview.TemplateStringId,
				preview.TemplateSelectionSource,
				new List<PlayerRpCraftTemplateCandidate>(1)
				{
					new PlayerRpCraftTemplateCandidate
					{
						Rank = 1,
						TemplateStringId = preview.TemplateStringId,
						DisplayName = preview.TemplateDisplayName,
						TypeLabel = preview.TemplateTypeLabel,
						StandardPrice = preview.TemplateBaseValue
					}
				},
				out PlayerRpCraftPreview current,
				out error))
			{
				return false;
			}
			if (!string.Equals(current.TemplateStringId, preview.TemplateStringId, StringComparison.OrdinalIgnoreCase)
				|| current.TemplateBaseValue != preview.TemplateBaseValue)
			{
				error = "模板或价格已经变化，请重新确认。";
				return false;
			}
			if (!string.Equals(
					current.CrafterHeroId,
					preview.CrafterHeroId,
					StringComparison.OrdinalIgnoreCase)
				|| current.CrafterMaxCraftingStamina
					!= preview.CrafterMaxCraftingStamina
				|| current.CraftingStaminaCost
					!= preview.CraftingStaminaCost
				|| current.CraftedItemValue
					!= preview.CraftedItemValue
				|| current.CraftingExperienceBaseAmount
					!= preview.CraftingExperienceBaseAmount)
			{
				error = "制造者资源或成品价值已经变化，请重新确认。";
				return false;
			}
			if (current.IsEquipment
				&& (current.SmithingSkill != preview.SmithingSkill
					|| current.GoodWeight != preview.GoodWeight
					|| current.NormalWeight != preview.NormalWeight
					|| current.BadWeight != preview.BadWeight))
			{
				error = "锻造等级或三档概率已经变化，请重新确认。";
				return false;
			}
			if (!TryResolveAvailablePlayerRpCrafter(
					current.CrafterHeroId,
					out staminaCrafter,
					out error)
				|| !TryReadPlayerRpCrafterCraftingResources(
					staminaCrafter,
					out staminaBehavior,
					out staminaBeforeDeduction,
					out maxStaminaBeforeDeduction,
					out staminaCost,
					out error))
			{
				return false;
			}
			if (maxStaminaBeforeDeduction
					!= current.CrafterMaxCraftingStamina
				|| staminaCost != current.CraftingStaminaCost)
			{
				error = "最大锻造体力或体力消耗已经变化，请重新确认。";
				return false;
			}
			if (staminaBeforeDeduction < staminaCost)
			{
				error = (staminaCrafter.Name?.ToString() ?? "所选制造者")
					+ "的锻造体力不足：需要 "
					+ staminaCost.ToString(CultureInfo.InvariantCulture)
					+ "，当前 "
					+ staminaBeforeDeduction.ToString(CultureInfo.InvariantCulture)
					+ "。";
				return false;
			}

			ItemObject originalTemplate = ResolveItemById(current.TemplateStringId);
			if (originalTemplate == null)
			{
				error = "匹配模板已经不可用。";
				return false;
			}

			string outcome;
			bool underfunded = current.InvestedDenars < current.TemplateBaseValue;
			double multiplier = 1d;
			int upgradeLevel = 0;
			int appliedBonus = 0;
			int roll;
			PlayerRpCraftItemStatsSnapshot statsSnapshot = null;
			ItemObject effectiveTemplate = originalTemplate;
			string displayName = current.RequestedName;
			string craftKind = current.IsEquipment
				? "equipment"
				: (IsGeneratedRpFoodTemplateItem(originalTemplate) ? "food" : "goods");

			if (current.IsEquipment)
			{
				roll = MBRandom.RandomInt(30000);
				if (roll < current.GoodWeight)
				{
					outcome = "good";
				}
				else if (roll < current.GoodWeight + current.NormalWeight)
				{
					outcome = "normal";
				}
				else
				{
					outcome = "bad";
				}
				ResolvePlayerRpEquipmentOutcome(
					current.InvestedDenars,
					current.TemplateBaseValue,
					current.SmithingSkill,
					outcome,
					out underfunded,
					out multiplier,
					out upgradeLevel,
					out appliedBonus);
				if (!PlayerRpCraftItemComponentService.TryCreateSnapshot(
					originalTemplate,
					underfunded,
					multiplier,
					appliedBonus,
					out statsSnapshot,
					out string snapshotError))
				{
					error = "无法安全复制该装备模板：" + snapshotError;
					return false;
				}
			}
			else
			{
				roll = current.InvestedDenars >= current.TemplateBaseValue
					? 0
					: MBRandom.RandomInt(Math.Max(1, current.TemplateBaseValue));
				bool success = current.InvestedDenars >= current.TemplateBaseValue
					|| roll < current.InvestedDenars;
				if (success)
				{
					outcome = "success";
				}
				else
				{
					outcome = "junk";
					craftKind = "junk";
					if (!TryResolvePlayerRpJunkTemplate(null, null, out effectiveTemplate))
					{
						error = "没有找到安全的垃圾物品模板。";
						return false;
					}
					displayName = effectiveTemplate.Name?.ToString() ?? "垃圾物品";
				}
			}

			Hero player = Hero.MainHero;
			if (player == null || player.Gold < current.InvestedDenars)
			{
				error = "第纳尔不足。";
				return false;
			}
			playerRoster = GetPlayerMainItemRoster();
			if (playerRoster == null)
			{
				error = "玩家背包不可用。";
				return false;
			}

			chargedPlayer = player;
			goldBeforeCharge = player.Gold;
			chargeAttempted = true;
			GiveGoldAction.ApplyBetweenCharacters(player, null, current.InvestedDenars, disableNotification: true);
			chargedAmount = Math.Max(0, goldBeforeCharge - player.Gold);
			if (chargedAmount != current.InvestedDenars)
			{
				error = "扣款失败，未制造物品。";
				return false;
			}

			string batchId = Guid.NewGuid().ToString("N");
			string generatedIdentity = PlayerRpGeneratedItemPrefix + batchId;
			generatedStringIdForRollback = generatedIdentity;
			AuthorizePlayerRpCraftGenerationKey(generatedIdentity);
			int generated = GenerateNamedInventoryItemToRosterForExternal(
				playerRoster,
				displayName,
				1,
				out string generatedStringId,
				out string generatedDisplayName,
				"player_rp_craft",
				generatedIdentity,
				effectiveTemplate.StringId);
			if (generated != 1 || string.IsNullOrWhiteSpace(generatedStringId)
				|| !string.Equals(generatedStringId, generatedIdentity, StringComparison.OrdinalIgnoreCase)
				|| !TryResolveGeneratedRewardItemForStringId(generatedStringId, out generatedItem, "player_rp_craft_resolve")
				|| !IsExactPlayerRpGeneratedItemForKey(generatedItem, generatedIdentity))
			{
				error = "物品创建或加入背包失败。";
				return false;
			}

			GeneratedRewardItemRecord record = Instance?.GetGeneratedRewardItemRecord(generatedStringId);
			if (record == null)
			{
				error = "制造记录创建失败。";
				return false;
			}
			int playerIntelligence = Math.Max(0, player.GetAttributeValue(DefaultCharacterAttributes.Intelligence));
			record.PlayerCraft = new PlayerRpCraftData
			{
				BatchId = batchId,
				CreatorHeroId = player.StringId ?? "",
				CrafterHeroId = current.CrafterHeroId,
				CrafterDisplayNameSnapshot = current.CrafterDisplayName,
				CrafterCraftingStaminaSnapshot =
					staminaBeforeDeduction,
				CrafterMaxCraftingStaminaSnapshot =
					maxStaminaBeforeDeduction,
				CraftingStaminaCost = staminaCost,
				CrafterCraftingStaminaAfterSnapshot =
					Math.Max(0, staminaBeforeDeduction - staminaCost),
				CraftingExperienceBaseAmount =
					current.CraftingExperienceBaseAmount,
				OriginalRequestedName = current.RequestedName,
				OriginalTemplateStringId = originalTemplate.StringId ?? "",
				EffectiveTemplateStringId = effectiveTemplate.StringId ?? "",
				CraftKind = craftKind,
				InvestedDenars = current.InvestedDenars,
				CraftedItemValue = current.CraftedItemValue,
				TemplateBaseValue = current.TemplateBaseValue,
				PlayerIntelligenceSnapshot = playerIntelligence,
				SmithingSkillSnapshot = current.SmithingSkill,
				GoodWeight = current.GoodWeight,
				NormalWeight = current.NormalWeight,
				BadWeight = current.BadWeight,
				Roll = roll,
				Outcome = outcome,
				Underfunded = underfunded,
				AppliedMultiplier = multiplier,
				UpgradeLevel = upgradeLevel,
				AppliedBonus = appliedBonus,
				CreatedDay = GetCampaignDayIndex(),
				InitialQuantity = 1,
				StatsSnapshot = statsSnapshot
			};
			record.TemplateStringId = effectiveTemplate.StringId ?? record.TemplateStringId;
			record.DisplayName = displayName;
			Instance._generatedRewardItemRecords[record.GeneratedStringId] = record;
			RegisterGeneratedRewardManifestRecord(record);

			if (!ApplyGeneratedRewardItemTemplateState(
				generatedItem,
				effectiveTemplate,
				displayName))
			{
				error = "物品属性或价格写入失败。";
				return false;
			}
			generatedItem.Initialize();
			generatedItem.IsReady = true;
			if (generatedItem.Value != current.CraftedItemValue)
			{
				error = "物品价格写入失败。";
				return false;
			}
			Instance.RememberGeneratedRewardItemRecord(record.GeneratedStringId, displayName, effectiveTemplate, generatedItem);

			string statText = statsSnapshot != null
				? PlayerRpCraftItemComponentService.BuildAttributeSummary(statsSnapshot)
				: "";
			result = new PlayerRpCraftResult
			{
				GeneratedStringId = record.GeneratedStringId,
				DisplayName = generatedDisplayName ?? displayName,
				Outcome = outcome,
				Message = "制造完成：" + displayName + "\n结果：" + GetPlayerRpOutcomeLabel(outcome)
					+ "\n成品价值："
					+ current.CraftedItemValue.ToString(CultureInfo.InvariantCulture)
					+ " 第纳尔"
					+ "\n锻造体力：-"
					+ staminaCost.ToString(CultureInfo.InvariantCulture)
					+ "　基础锻造经验：+"
					+ current.CraftingExperienceBaseAmount.ToString(CultureInfo.InvariantCulture)
					+ (string.IsNullOrWhiteSpace(statText) ? "" : "\n" + statText)
			};
			if (!TryReadPlayerRpCrafterCraftingResources(
					staminaCrafter,
					out ICraftingCampaignBehavior finalStaminaBehavior,
					out int finalStamina,
					out int finalMaxStamina,
					out int finalStaminaCost,
					out error))
			{
				return false;
			}
			if (finalStamina != staminaBeforeDeduction
				|| finalMaxStamina != maxStaminaBeforeDeduction
				|| finalStaminaCost != staminaCost)
			{
				error = "锻造体力在制造期间发生变化，请重新确认。";
				return false;
			}
			staminaBehavior = finalStaminaBehavior;
			int staminaAfterDeduction =
				Math.Max(0, staminaBeforeDeduction - staminaCost);
			staminaAdjustmentAttempted = true;
			staminaBehavior.SetHeroCraftingStamina(
				staminaCrafter,
				staminaAfterDeduction);
			int appliedStamina = Math.Max(
				0,
				staminaBehavior.GetHeroCraftingStamina(staminaCrafter));
			if (appliedStamina != staminaAfterDeduction)
			{
				error = "锻造体力扣除未能完成。";
				return false;
			}
			transactionCommitted = true;
			try
			{
				staminaCrafter.AddSkillXp(
					DefaultSkills.Crafting,
					current.CraftingExperienceBaseAmount);
			}
			catch (Exception experienceException)
			{
				result.Message +=
					"\n基础锻造经验通知被其他 Mod 中断；为防止重复经验，本次物品与消耗仍已结算。";
				try
				{
					Logger.Log(
						"Logic",
						"[PlayerRpCraft] crafting_xp_event_interrupted item="
							+ record.GeneratedStringId
							+ " crafter="
							+ (current.CrafterHeroId ?? "")
							+ " base_xp="
							+ current.CraftingExperienceBaseAmount.ToString(
								CultureInfo.InvariantCulture)
							+ " exception="
							+ experienceException);
				}
				catch
				{
				}
			}
			try
			{
				Logger.Log("Logic", "[PlayerRpCraft] committed item=" + record.GeneratedStringId
					+ " name_hash=" + StablePromptKeyHash(displayName ?? "")
					+ " name_length="
					+ (displayName ?? "").Length.ToString(CultureInfo.InvariantCulture)
					+ " template=" + (effectiveTemplate.StringId ?? "")
					+ " invested=" + current.InvestedDenars.ToString(CultureInfo.InvariantCulture)
					+ " output_value=" + current.CraftedItemValue.ToString(CultureInfo.InvariantCulture)
					+ " crafter=" + (current.CrafterHeroId ?? "")
					+ " stamina_cost=" + staminaCost.ToString(CultureInfo.InvariantCulture)
					+ " crafting_base_xp=" + current.CraftingExperienceBaseAmount.ToString(CultureInfo.InvariantCulture)
					+ " outcome=" + outcome
					+ " stats=" + statText);
			}
			catch
			{
			}
			if (current.InvestedDenars > 10000)
			{
				MyBehavior.RecordPlayerHighValueRpCraftForExternal(
					batchId,
					current.RequestedName,
					displayName,
					current.InvestedDenars,
					current.CraftedItemValue,
					current.CrafterHeroId,
					current.CrafterDisplayName,
					GetPlayerRpOutcomeLabel(outcome));
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "制造失败：" + ex.GetType().Name + ": " + ex.Message;
			try
			{
				Logger.Log("Logic", "[PlayerRpCraft] commit_failed error=" + ex);
			}
			catch
			{
			}
			return false;
		}
		finally
		{
			if (!transactionCommitted
				&& staminaAdjustmentAttempted
				&& staminaCrafter != null)
			{
				try
				{
					ICraftingCampaignBehavior restoreBehavior =
						Campaign.Current?.GetCampaignBehavior<ICraftingCampaignBehavior>()
						?? staminaBehavior;
					if (restoreBehavior == null)
					{
						throw new InvalidOperationException(
							"crafting_stamina_behavior_unavailable");
					}
					restoreBehavior.SetHeroCraftingStamina(
						staminaCrafter,
						staminaBeforeDeduction);
					int restoredStamina = Math.Max(
						0,
						restoreBehavior.GetHeroCraftingStamina(staminaCrafter));
					if (restoredStamina != staminaBeforeDeduction)
					{
						throw new InvalidOperationException(
							"crafting_stamina_restore_mismatch");
					}
				}
				catch (Exception staminaRollbackException)
				{
					error = AppendPlayerRpCraftFailureStatus(
						error,
						"锻造体力未能恢复："
							+ staminaRollbackException.GetType().Name
							+ "。");
				}
			}
			if (!transactionCommitted && chargeAttempted)
			{
				try
				{
					if (chargedAmount <= 0 && chargedPlayer != null)
					{
						chargedAmount = Math.Max(0, goldBeforeCharge - chargedPlayer.Gold);
					}
					string rollbackStatus = RollbackFailedPlayerRpCraftTransaction(
						playerRoster,
						generatedItem,
						generatedStringIdForRollback,
						chargedPlayer,
						chargedAmount);
					error = AppendPlayerRpCraftFailureStatus(error, rollbackStatus);
				}
				catch (Exception rollbackException)
				{
					error = AppendPlayerRpCraftFailureStatus(
						error,
						"退款失败：事务回滚发生 "
							+ rollbackException.GetType().Name
							+ "，请检查玩家第纳尔和背包。");
				}
			}
			RevokePlayerRpCraftGenerationKey(generatedStringIdForRollback);
			Interlocked.Exchange(ref PlayerRpCraftCommitGate, 0);
		}
	}

	private static string RollbackFailedPlayerRpCraftTransaction(
		ItemRoster playerRoster,
		ItemObject generatedItem,
		string generatedStringId,
		Hero chargedPlayer,
		int chargedAmount)
	{
		List<string> issues = new List<string>(4);
		string key = (generatedStringId ?? "").Trim();
		bool hasGeneratedKey = !string.IsNullOrWhiteSpace(key);
		bool hasExactGeneratedKey = IsExactPlayerRpGeneratedTransactionKey(key);
		HashSet<uint> objectIds = new HashSet<uint>();
		if (hasExactGeneratedKey)
		{
			CollectPlayerRpRollbackObjectIds(key, generatedItem, objectIds);
			RemovePlayerRpGeneratedItemFromRoster(
				playerRoster ?? GetPlayerMainItemRoster(),
				key,
				issues);
			RemovePlayerRpGeneratedRecordsAndCaches(key, objectIds, issues);
			UnregisterExactPlayerRpGeneratedItem(key, objectIds, issues);
		}
		else if (hasGeneratedKey)
		{
			AddPlayerRpRollbackIssue(issues, "生成标识不属于本次玩家制造命名空间，未反注册对象");
		}

		string refundStatus = RefundPlayerRpCraftCharge(chargedPlayer, chargedAmount);
		if (issues.Count > 0)
		{
			refundStatus += "\n物品回滚未完全完成：" + string.Join("；", issues);
		}
		else if (hasExactGeneratedKey)
		{
			refundStatus += "\n已清理本次生成物品、记录和运行时缓存。";
		}
		try
		{
			Logger.Log("Logic", "[PlayerRpCraft] rollback item=" + key
				+ " charged=" + Math.Max(0, chargedAmount).ToString(CultureInfo.InvariantCulture)
				+ " status=" + refundStatus.Replace('\n', ' '));
		}
		catch
		{
		}
		return refundStatus;
	}

	private static string AppendPlayerRpCraftFailureStatus(string error, string status)
	{
		string message = (error ?? "").Trim();
		string normalizedStatus = (status ?? "").Trim();
		if (string.IsNullOrWhiteSpace(message))
		{
			message = "制造失败。";
		}
		return string.IsNullOrWhiteSpace(normalizedStatus)
			? message
			: message + "\n" + normalizedStatus;
	}

	private static string RefundPlayerRpCraftCharge(Hero chargedPlayer, int chargedAmount)
	{
		int expected = Math.Max(0, chargedAmount);
		if (expected <= 0)
		{
			return "未扣除第纳尔，无需退款。";
		}
		if (chargedPlayer == null)
		{
			return "退款失败：无法定位付款玩家，应退 "
				+ expected.ToString(CultureInfo.InvariantCulture)
				+ " 第纳尔。";
		}

		int before;
		try
		{
			before = chargedPlayer.Gold;
		}
		catch (Exception ex)
		{
			return "退款失败：无法读取退款前余额（"
				+ ex.GetType().Name
				+ "），应退 "
				+ expected.ToString(CultureInfo.InvariantCulture)
				+ " 第纳尔。";
		}

		Exception refundException = null;
		try
		{
			chargedPlayer.ChangeHeroGold(expected);
		}
		catch (Exception ex)
		{
			refundException = ex;
		}

		int after;
		try
		{
			after = chargedPlayer.Gold;
		}
		catch (Exception ex)
		{
			return "退款失败：无法验证退款后余额（"
				+ ex.GetType().Name
				+ "），应退 "
				+ expected.ToString(CultureInfo.InvariantCulture)
				+ " 第纳尔。";
		}
		int actual = (int)Math.Max(
			0L,
			Math.Min(int.MaxValue, (long)after - before));
		if (actual == expected)
		{
			return "已全额退款 "
				+ expected.ToString(CultureInfo.InvariantCulture)
				+ " 第纳尔。";
		}
		return "退款失败：应退 "
			+ expected.ToString(CultureInfo.InvariantCulture)
			+ " 第纳尔，实际退回 "
			+ actual.ToString(CultureInfo.InvariantCulture)
			+ " 第纳尔"
			+ (refundException == null
				? "。"
				: "（" + refundException.GetType().Name + "）。");
	}

	private static bool IsExactPlayerRpGeneratedTransactionKey(string generatedStringId)
	{
		string key = (generatedStringId ?? "").Trim();
		if (key.Length != PlayerRpGeneratedItemPrefix.Length + 32
			|| !key.StartsWith(PlayerRpGeneratedItemPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		for (int i = PlayerRpGeneratedItemPrefix.Length; i < key.Length; i++)
		{
			char c = key[i];
			if (!((c >= '0' && c <= '9')
				|| (c >= 'a' && c <= 'f')
				|| (c >= 'A' && c <= 'F')))
			{
				return false;
			}
		}
		return true;
	}

	private static void AuthorizePlayerRpCraftGenerationKey(string generatedStringId)
	{
		if (!IsExactPlayerRpGeneratedTransactionKey(generatedStringId))
		{
			throw new InvalidOperationException("Invalid player RP crafting transaction key.");
		}
		lock (PlayerRpCraftGenerationAuthorizationLock)
		{
			ActivePlayerRpCraftGenerationKeys.Add(generatedStringId.Trim());
		}
	}

	private static void RevokePlayerRpCraftGenerationKey(string generatedStringId)
	{
		string key = (generatedStringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		lock (PlayerRpCraftGenerationAuthorizationLock)
		{
			ActivePlayerRpCraftGenerationKeys.Remove(key);
		}
	}

	private static bool IsAuthorizedPlayerRpCraftGenerationKey(string generatedStringId)
	{
		string key = (generatedStringId ?? "").Trim();
		if (!IsExactPlayerRpGeneratedTransactionKey(key))
		{
			return false;
		}
		lock (PlayerRpCraftGenerationAuthorizationLock)
		{
			return ActivePlayerRpCraftGenerationKeys.Contains(key);
		}
	}

	private static bool IsExactPlayerRpGeneratedItemForKey(
		ItemObject item,
		string generatedStringId)
	{
		string key = (generatedStringId ?? "").Trim();
		return item != null
			&& IsExactPlayerRpGeneratedTransactionKey(key)
			&& string.Equals(
				(item.StringId ?? "").Trim(),
				key,
				StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPlayerRpGeneratedRecordForKey(
		GeneratedRewardItemRecord record,
		string generatedStringId)
	{
		return record != null
			&& string.Equals(
				(record.GeneratedStringId ?? "").Trim(),
				(generatedStringId ?? "").Trim(),
				StringComparison.OrdinalIgnoreCase);
	}

	private static void CollectPlayerRpRollbackObjectIds(
		string generatedStringId,
		ItemObject generatedItem,
		HashSet<uint> objectIds)
	{
		if (objectIds == null
			|| !IsExactPlayerRpGeneratedTransactionKey(generatedStringId))
		{
			return;
		}
		if (IsExactPlayerRpGeneratedItemForKey(generatedItem, generatedStringId)
			&& generatedItem.Id.InternalValue != 0u)
		{
			objectIds.Add(generatedItem.Id.InternalValue);
		}

		RewardSystemBehavior instance = Instance;
		if (instance?._generatedRewardItemRecords != null
			&& instance._generatedRewardItemRecords.TryGetValue(
				generatedStringId,
				out GeneratedRewardItemRecord runtimeRecord))
		{
			AddPlayerRpRecordObjectIds(runtimeRecord, generatedStringId, objectIds);
		}

		ItemObject registered = TryGetRegisteredGeneratedRewardItemByStringId(generatedStringId);
		if (IsExactPlayerRpGeneratedItemForKey(registered, generatedStringId)
			&& registered.Id.InternalValue != 0u)
		{
			objectIds.Add(registered.Id.InternalValue);
		}

		lock (GeneratedRewardItemRegistrationLock)
		{
			if (GeneratedRewardManifestByStringId.TryGetValue(
				generatedStringId,
				out GeneratedRewardItemRecord manifestRecord))
			{
				AddPlayerRpRecordObjectIds(manifestRecord, generatedStringId, objectIds);
			}
			if (GeneratedRewardDetachedItemsByStringId.TryGetValue(
				generatedStringId,
				out ItemObject detached)
				&& IsExactPlayerRpGeneratedItemForKey(detached, generatedStringId)
				&& detached.Id.InternalValue != 0u)
			{
				objectIds.Add(detached.Id.InternalValue);
			}
			foreach (KeyValuePair<uint, GeneratedRewardItemRecord> pair in GeneratedRewardManifestByObjectId)
			{
				if (IsPlayerRpGeneratedRecordForKey(pair.Value, generatedStringId))
				{
					objectIds.Add(pair.Key);
					AddPlayerRpRecordObjectIds(pair.Value, generatedStringId, objectIds);
				}
			}
			foreach (KeyValuePair<uint, ItemObject> pair in GeneratedRewardDetachedItemsByObjectId)
			{
				if (IsExactPlayerRpGeneratedItemForKey(pair.Value, generatedStringId))
				{
					objectIds.Add(pair.Key);
				}
			}
			foreach (KeyValuePair<uint, ItemObject> pair in GeneratedRewardPendingItemsByObjectId)
			{
				if (IsExactPlayerRpGeneratedItemForKey(pair.Value, generatedStringId))
				{
					objectIds.Add(pair.Key);
				}
			}
		}
	}

	private static void AddPlayerRpRecordObjectIds(
		GeneratedRewardItemRecord record,
		string generatedStringId,
		HashSet<uint> objectIds)
	{
		if (objectIds == null
			|| !IsPlayerRpGeneratedRecordForKey(record, generatedStringId))
		{
			return;
		}
		if (record.ObjectId != 0u)
		{
			objectIds.Add(record.ObjectId);
		}
		if (record.LegacyObjectIds == null)
		{
			return;
		}
		foreach (uint legacyObjectId in record.LegacyObjectIds)
		{
			if (legacyObjectId != 0u)
			{
				objectIds.Add(legacyObjectId);
			}
		}
	}

	private static void RemovePlayerRpGeneratedItemFromRoster(
		ItemRoster playerRoster,
		string generatedStringId,
		List<string> issues)
	{
		if (playerRoster == null)
		{
			AddPlayerRpRollbackIssue(issues, "玩家背包不可用，无法验证生成物品是否已移除");
			return;
		}
		try
		{
			for (int i = playerRoster.Count - 1; i >= 0; i--)
			{
				ItemRosterElement element = playerRoster.GetElementCopyAtIndex(i);
				if (element.Amount <= 0
					|| !IsExactPlayerRpGeneratedItemForKey(
						element.EquipmentElement.Item,
						generatedStringId))
				{
					continue;
				}
				playerRoster.AddToCounts(element.EquipmentElement, -element.Amount);
			}
			for (int i = 0; i < playerRoster.Count; i++)
			{
				ItemRosterElement element = playerRoster.GetElementCopyAtIndex(i);
				if (element.Amount > 0
					&& IsExactPlayerRpGeneratedItemForKey(
						element.EquipmentElement.Item,
						generatedStringId))
				{
					AddPlayerRpRollbackIssue(issues, "玩家背包仍残留本次生成物品");
					break;
				}
			}
		}
		catch (Exception ex)
		{
			AddPlayerRpRollbackIssue(
				issues,
				"玩家背包清理失败（" + ex.GetType().Name + "）");
		}
	}

	private static void RemovePlayerRpGeneratedRecordsAndCaches(
		string generatedStringId,
		HashSet<uint> objectIds,
		List<string> issues)
	{
		try
		{
			RewardSystemBehavior instance = Instance;
			instance?._generatedRewardItemRecords?.Remove(generatedStringId);
			instance?._generatedRewardItemStorage?.Remove(generatedStringId);
			instance?._generatedRewardPlayerRosterRecords?.Remove(generatedStringId);
			instance?._generatedRewardPlayerRosterStorage?.Remove(generatedStringId);

			lock (GeneratedRewardItemRegistrationLock)
			{
				GeneratedRewardManifestByStringId.Remove(generatedStringId);
				GeneratedRewardDetachedItemsByStringId.Remove(generatedStringId);

				List<uint> manifestIdsToRemove = new List<uint>();
				foreach (KeyValuePair<uint, GeneratedRewardItemRecord> pair in GeneratedRewardManifestByObjectId)
				{
					if (IsPlayerRpGeneratedRecordForKey(pair.Value, generatedStringId))
					{
						manifestIdsToRemove.Add(pair.Key);
						objectIds?.Add(pair.Key);
					}
				}
				foreach (uint objectId in manifestIdsToRemove)
				{
					GeneratedRewardManifestByObjectId.Remove(objectId);
				}

				List<uint> detachedIdsToRemove = new List<uint>();
				foreach (KeyValuePair<uint, ItemObject> pair in GeneratedRewardDetachedItemsByObjectId)
				{
					if (IsExactPlayerRpGeneratedItemForKey(pair.Value, generatedStringId))
					{
						detachedIdsToRemove.Add(pair.Key);
						objectIds?.Add(pair.Key);
					}
				}
				foreach (uint objectId in detachedIdsToRemove)
				{
					GeneratedRewardDetachedItemsByObjectId.Remove(objectId);
				}

				List<uint> pendingIdsToRemove = new List<uint>();
				foreach (KeyValuePair<uint, ItemObject> pair in GeneratedRewardPendingItemsByObjectId)
				{
					bool exactGeneratedItem =
						IsExactPlayerRpGeneratedItemForKey(pair.Value, generatedStringId);
					bool transactionPendingItem = objectIds?.Contains(pair.Key) == true
						&& (pair.Value == null || IsGeneratedRewardPendingItem(pair.Value));
					if (exactGeneratedItem || transactionPendingItem)
					{
						pendingIdsToRemove.Add(pair.Key);
					}
				}
				foreach (uint objectId in pendingIdsToRemove)
				{
					GeneratedRewardPendingItemsByObjectId.Remove(objectId);
				}

				if (objectIds != null)
				{
					foreach (uint objectId in objectIds)
					{
						if (objectId == 0u)
						{
							continue;
						}
						if (GeneratedRewardManifestByObjectId.TryGetValue(
							objectId,
							out GeneratedRewardItemRecord manifestAtId))
						{
							if (manifestAtId == null
								|| IsPlayerRpGeneratedRecordForKey(
									manifestAtId,
									generatedStringId))
							{
								GeneratedRewardManifestByObjectId.Remove(objectId);
							}
							else
							{
								AddPlayerRpRollbackIssue(
									issues,
									"对象ID清单缓存存在其他物品，已保留该缓存");
							}
						}
						if (GeneratedRewardDetachedItemsByObjectId.TryGetValue(
							objectId,
							out ItemObject detachedAtId))
						{
							if (detachedAtId == null
								|| IsExactPlayerRpGeneratedItemForKey(
									detachedAtId,
									generatedStringId))
							{
								GeneratedRewardDetachedItemsByObjectId.Remove(objectId);
							}
							else
							{
								AddPlayerRpRollbackIssue(
									issues,
									"Detached 对象ID缓存存在其他物品，已保留该缓存");
							}
						}
						if (GeneratedRewardPendingItemsByObjectId.TryGetValue(
							objectId,
							out ItemObject pendingAtId))
						{
							if (pendingAtId == null
								|| IsGeneratedRewardPendingItem(pendingAtId)
								|| IsExactPlayerRpGeneratedItemForKey(
									pendingAtId,
									generatedStringId))
							{
								GeneratedRewardPendingItemsByObjectId.Remove(objectId);
							}
							else
							{
								AddPlayerRpRollbackIssue(
									issues,
									"Pending 对象ID缓存存在其他物品，已保留该缓存");
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			AddPlayerRpRollbackIssue(
				issues,
				"记录或缓存清理失败（" + ex.GetType().Name + "）");
		}
	}

	private static void UnregisterExactPlayerRpGeneratedItem(
		string generatedStringId,
		HashSet<uint> objectIds,
		List<string> issues)
	{
		if (!IsExactPlayerRpGeneratedTransactionKey(generatedStringId))
		{
			return;
		}
		MBObjectManager manager = MBObjectManager.Instance ?? Game.Current?.ObjectManager;
		if (manager == null)
		{
			AddPlayerRpRollbackIssue(issues, "对象管理器不可用，无法反注册生成物品");
			return;
		}

		bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
		bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
		try
		{
			SuppressGeneratedRewardObjectLookup = true;
			SuppressGeneratedRewardPendingLookup = true;
			List<ItemObject> registeredCandidates = new List<ItemObject>(2);
			ItemObject byString = manager.GetObject<ItemObject>(generatedStringId);
			AddExactPlayerRpRegisteredCandidate(
				registeredCandidates,
				byString,
				generatedStringId);
			if (objectIds != null)
			{
				foreach (uint objectId in objectIds)
				{
					if (objectId == 0u)
					{
						continue;
					}
					ItemObject byObjectId = manager.GetObject(new MBGUID(objectId)) as ItemObject;
					AddExactPlayerRpRegisteredCandidate(
						registeredCandidates,
						byObjectId,
						generatedStringId);
				}
			}
			if (registeredCandidates.Count > 1)
			{
				AddPlayerRpRollbackIssue(
					issues,
					"同一玩家制造标识对应多个注册对象，已拒绝不安全反注册");
				return;
			}
			if (registeredCandidates.Count == 1)
			{
				ItemObject exactRegisteredItem = registeredCandidates[0];
				if (exactRegisteredItem.Id.InternalValue != 0u)
				{
					ItemObject itemAtExactObjectId =
						manager.GetObject(exactRegisteredItem.Id) as ItemObject;
					if (itemAtExactObjectId != null
						&& !ReferenceEquals(itemAtExactObjectId, exactRegisteredItem))
					{
						AddPlayerRpRollbackIssue(
							issues,
							"玩家制造对象ID已被其他对象占用，已拒绝不安全反注册");
						return;
					}
				}
				manager.UnregisterObject(exactRegisteredItem);
			}

			ItemObject remainingByString = manager.GetObject<ItemObject>(generatedStringId);
			if (IsExactPlayerRpGeneratedItemForKey(remainingByString, generatedStringId))
			{
				AddPlayerRpRollbackIssue(issues, "本次生成对象仍存在于字符串索引");
				return;
			}
			if (objectIds == null)
			{
				return;
			}
			foreach (uint objectId in objectIds)
			{
				if (objectId == 0u)
				{
					continue;
				}
				ItemObject remainingByObjectId =
					manager.GetObject(new MBGUID(objectId)) as ItemObject;
				if (IsExactPlayerRpGeneratedItemForKey(
					remainingByObjectId,
					generatedStringId))
				{
					AddPlayerRpRollbackIssue(issues, "本次生成对象仍存在于对象ID索引");
					return;
				}
			}
		}
		catch (Exception ex)
		{
			AddPlayerRpRollbackIssue(
				issues,
				"反注册生成对象失败（" + ex.GetType().Name + "）");
		}
		finally
		{
			SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
			SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
		}
	}

	private static void AddExactPlayerRpRegisteredCandidate(
		List<ItemObject> candidates,
		ItemObject candidate,
		string generatedStringId)
	{
		if (candidates == null
			|| !IsExactPlayerRpGeneratedItemForKey(candidate, generatedStringId))
		{
			return;
		}
		foreach (ItemObject existing in candidates)
		{
			if (ReferenceEquals(existing, candidate))
			{
				return;
			}
		}
		candidates.Add(candidate);
	}

	private static void AddPlayerRpRollbackIssue(
		List<string> issues,
		string issue)
	{
		string normalized = (issue ?? "").Trim();
		if (issues == null || string.IsNullOrWhiteSpace(normalized))
		{
			return;
		}
		foreach (string existing in issues)
		{
			if (string.Equals(existing, normalized, StringComparison.Ordinal))
			{
				return;
			}
		}
		if (issues.Count < 8)
		{
			issues.Add(normalized);
		}
	}

	public static string DecoratePlayerCraftedAfefItemNameForExternal(
		string itemStringId,
		uint objectId,
		string cleanName,
		Hero observerHero,
		BasicCharacterObject observerCharacter,
		string observerKey,
		string exposureType,
		bool commit)
	{
		string name = (cleanName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(name)
			|| name.IndexOf(PlayerRpCraftedAfefMarker, StringComparison.Ordinal) >= 0)
		{
			return name;
		}
		try
		{
			GeneratedRewardItemRecord record = null;
			string itemKey = (itemStringId ?? "").Trim();
			if (IsGeneratedRewardItemStringId(itemKey))
			{
				record = Instance?.GetGeneratedRewardItemRecord(itemKey);
			}
			if (record == null && objectId != 0u)
			{
				EnsureGeneratedRewardManifestLoaded();
				lock (GeneratedRewardItemRegistrationLock)
				{
					GeneratedRewardManifestByObjectId.TryGetValue(objectId, out record);
				}
			}
			PlayerRpCraftData craft = record?.PlayerCraft;
			if (craft == null)
			{
				return name;
			}

			observerHero ??= (observerCharacter as CharacterObject)?.HeroObject;
			if (observerHero == Hero.MainHero)
			{
				return name;
			}
			string stableObserverKey = (observerKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(stableObserverKey))
			{
				stableObserverKey = observerHero?.StringId ?? observerCharacter?.StringId ?? "";
			}
			if (string.IsNullOrWhiteSpace(stableObserverKey))
			{
				return name;
			}

			// A display/give can arrive through more than one channel in the same frame.
			// Serialize the first-observation lookup and roll so a batch/NPC pair is
			// decided exactly once and every channel persists the same result.
			lock (GeneratedRewardItemRegistrationLock)
			{
				craft.Inspections ??= new Dictionary<string, PlayerRpCraftInspectionRecord>(StringComparer.OrdinalIgnoreCase);
				if (!craft.Inspections.TryGetValue(stableObserverKey, out PlayerRpCraftInspectionRecord inspection)
					|| inspection == null)
				{
					if (!commit)
					{
						return name;
					}
					int playerIntelligence = Math.Max(0, craft.PlayerIntelligenceSnapshot);
					int observerIntelligence = ResolvePlayerRpObserverIntelligence(
						observerHero,
						observerCharacter);
					int difference = playerIntelligence - observerIntelligence;
					int chanceWeight = difference <= 0
						? 10000
						: (int)Math.Round(
							10000d / Math.Pow(2d, Math.Min(30, difference)),
							MidpointRounding.AwayFromZero);
					chanceWeight = Math.Max(0, Math.Min(10000, chanceWeight));
					int inspectionRoll = MBRandom.RandomInt(10000);
					inspection = new PlayerRpCraftInspectionRecord
					{
						ObserverKey = stableObserverKey,
						ExposureType = (exposureType ?? "").Trim(),
						PlayerIntelligence = playerIntelligence,
						ObserverIntelligence = observerIntelligence,
						ChanceWeight = chanceWeight,
						Roll = inspectionRoll,
						Detected = inspectionRoll < chanceWeight,
						Day = GetCampaignDayIndex()
					};
					craft.Inspections[stableObserverKey] = inspection;
					record.PlayerCraft = craft;
					if (Instance != null)
					{
						Instance._generatedRewardItemRecords[record.GeneratedStringId] = record;
					}
					RegisterGeneratedRewardManifestRecord(record);
					Logger.Log("Logic", "[PlayerRpCraft] inspection item=" + record.GeneratedStringId
						+ " observer=" + stableObserverKey
						+ " exposure=" + inspection.ExposureType
						+ " playerInt=" + playerIntelligence.ToString(CultureInfo.InvariantCulture)
						+ " observerInt=" + observerIntelligence.ToString(CultureInfo.InvariantCulture)
						+ " chance=" + chanceWeight.ToString(CultureInfo.InvariantCulture)
						+ " roll=" + inspectionRoll.ToString(CultureInfo.InvariantCulture)
						+ " detected=" + inspection.Detected);
				}
				return inspection.Detected ? name + PlayerRpCraftedAfefMarker : name;
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[PlayerRpCraft] afef_decorate_failed item="
					+ (itemStringId ?? "")
					+ " error="
					+ ex.Message);
			}
			catch
			{
			}
			return name;
		}
	}

	private static int ResolvePlayerRpObserverIntelligence(
		Hero observerHero,
		BasicCharacterObject observerCharacter)
	{
		if (observerHero != null)
		{
			return Math.Max(
				0,
				observerHero.GetAttributeValue(DefaultCharacterAttributes.Intelligence));
		}
		if (!(observerCharacter is CharacterObject character))
		{
			return 0;
		}
		// Bannerlord troop templates have skills but no CharacterAttribute storage.
		// Use the three Intelligence-linked skills as a stable 0..10 proxy so
		// ordinary guards/merchants do not all behave as Intelligence 0.
		long total = Math.Max(0, character.GetSkillValue(DefaultSkills.Steward))
			+ Math.Max(0, character.GetSkillValue(DefaultSkills.Medicine))
			+ Math.Max(0, character.GetSkillValue(DefaultSkills.Engineering));
		int proxy = (int)Math.Round(
			total / 90d,
			MidpointRounding.AwayFromZero);
		return Math.Max(0, Math.Min(10, proxy));
	}

	internal static bool TryBuildPlayerRpCraftTemplateSelectionForExternal(
		string requestedName,
		int investedDenars,
		bool isEquipment,
		string crafterHeroId,
		out PlayerRpCraftTemplateSelectionRequest request,
		out string error)
	{
		request = null;
		if (!TryResolveAvailablePlayerRpCrafter(
			crafterHeroId,
			out Hero crafter,
			out error))
		{
			return false;
		}
		if (!TryReadPlayerRpCrafterCraftingResources(
				crafter,
				out _,
				out int stamina,
				out _,
				out int staminaCost,
				out error))
		{
			return false;
		}
		if (stamina < staminaCost)
		{
			error = (crafter.Name?.ToString() ?? "所选制造者")
				+ "的锻造体力不足：需要 "
				+ staminaCost.ToString(CultureInfo.InvariantCulture)
				+ "，当前 "
				+ stamina.ToString(CultureInfo.InvariantCulture)
				+ "。";
			return false;
		}
		if (!TryBuildPlayerRpCraftTemplateCandidates(
			requestedName,
			investedDenars,
			isEquipment,
			PlayerRpTemplateCandidateLimit,
			out List<PlayerRpCraftTemplateCandidate> candidates,
			out error))
		{
			return false;
		}
		request = new PlayerRpCraftTemplateSelectionRequest
		{
			CrafterHeroId = crafter.StringId ?? "",
			RequestedName = (requestedName ?? "").Trim(),
			InvestedDenars = investedDenars,
			IsEquipment = isEquipment,
			Candidates = candidates
		};
		return true;
	}


	private static bool TryResolvePlayerRpExactRegisteredTemplate(
		string requestedName,
		bool isEquipment,
		out bool exactMatchFound,
		out PlayerRpCraftTemplateCandidate candidate,
		out string error)
	{
		exactMatchFound = false;
		candidate = null;
		error = "";
		string lookup = (requestedName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(lookup))
		{
			return true;
		}

		PlayerRpExactTemplateLookupCache cache =
			GetPlayerRpExactTemplateLookupCache();
		bool matchedByStringId =
			cache.ByStringId.TryGetValue(lookup, out List<ItemObject> rawMatches);
		if (!matchedByStringId)
		{
			cache.ByDisplayName.TryGetValue(
				NormalizePlayerRpStrictExactLookup(lookup),
				out rawMatches);
		}
		if (rawMatches == null || rawMatches.Count == 0)
		{
			return true;
		}
		exactMatchFound = true;

		List<ItemObject> eligible = new List<ItemObject>();
		bool hasSafeEquipment = false;
		bool hasSafeMisc = false;
		foreach (ItemObject item in rawMatches)
		{
			bool safeEquipment = IsPlayerRpSafeExactEquipmentTemplate(item);
			bool safeMisc = IsPlayerRpSafeExactMiscTemplate(item);
			hasSafeEquipment |= safeEquipment;
			hasSafeMisc |= safeMisc;
			if ((isEquipment && safeEquipment)
				|| (!isEquipment && safeMisc))
			{
				eligible.Add(item);
			}
		}
		eligible = eligible
			.Where(item => item != null
				&& !string.IsNullOrWhiteSpace(item.StringId))
			.GroupBy(
				item => item.StringId.Trim(),
				StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.OrderBy(
				item => item.StringId,
				StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (eligible.Count == 0)
		{
			if (isEquipment && hasSafeMisc)
			{
				error = "名称精确匹配到游戏中的普通物品；请取消勾选 FORGE AS WEAPON/EQUIPMENT?。";
			}
			else if (!isEquipment && hasSafeEquipment)
			{
				error = "名称精确匹配到游戏中的武器装备；请勾选 FORGE AS WEAPON/EQUIPMENT?。";
			}
			else
			{
				error = "名称精确匹配到游戏物品，但该模板缺少可安全复制的组件或模型。";
			}
			return false;
		}
		if (eligible.Count > 1)
		{
			string ids = string.Join(
				", ",
				eligible
					.Take(8)
					.Select(item => item.StringId));
			error = "游戏中存在多个同名物品；请改为输入其中一个模板 StringId："
				+ ids
				+ (eligible.Count > 8 ? "……" : "");
			return false;
		}

		List<PlayerRpCraftTemplateCandidate> exactCandidates =
			new List<PlayerRpCraftTemplateCandidate>(1);
		AddPlayerRpTemplateCandidate(
			exactCandidates,
			new HashSet<string>(StringComparer.OrdinalIgnoreCase),
			lookup,
			eligible[0],
			isEquipment,
			GeneratedRpFoodKind.AnyFood,
			new[]
			{
				eligible[0].StringId,
				eligible[0].Name?.ToString()
			});
		candidate = exactCandidates.FirstOrDefault();
		if (candidate == null)
		{
			error = "精确匹配物品无法建立安全制造快照。";
			return false;
		}
		candidate.Rank = 1;
		candidate.MatchScore = 1f;
		return true;
	}

	private static bool IsPlayerRpSafeExactEquipmentTemplate(ItemObject item)
	{
		return IsCloneSafeGeneratedRewardTemplateItem(item)
			&& PlayerRpCraftItemComponentService.IsSafeEquipmentTemplate(
				item,
				out _);
	}

	private static bool IsPlayerRpSafeExactMiscTemplate(ItemObject item)
	{
		return IsCloneSafeGeneratedRpFoodTemplateItem(item)
			|| IsPlayerRpSafeGoodsTemplate(item);
	}

	private static PlayerRpExactTemplateLookupCache
		GetPlayerRpExactTemplateLookupCache()
	{
		object owner =
			(object)Game.Current?.ObjectManager ?? MBObjectManager.Instance;
		lock (PlayerRpExactTemplateLookupCacheLock)
		{
			if (owner != null
				&& ReferenceEquals(
					owner,
					PlayerRpExactTemplateLookupCacheOwner))
			{
				if (PlayerRpExactTemplateLookupCacheReady)
				{
					return PlayerRpExactTemplateLookup;
				}
				if (DateTime.UtcNow
					< PlayerRpExactTemplateLookupCacheRetryAfterUtc)
				{
					return PlayerRpExactTemplateLookup;
				}
			}

			PlayerRpExactTemplateLookupCache result =
				new PlayerRpExactTemplateLookupCache();
			bool scanCompleted = false;
			int scannedCount = 0;
			int itemErrorCount = 0;
			try
			{
				IEnumerable<ItemObject> items =
					Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>()
					?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
				foreach (ItemObject item
					in items ?? Enumerable.Empty<ItemObject>())
				{
					scannedCount++;
					try
					{
						if (item == null
							|| IsGeneratedRewardItemStringId(item.StringId))
						{
							continue;
						}
						AddPlayerRpExactTemplateIndex(
							result.ByStringId,
							item.StringId,
							item);
						AddPlayerRpExactTemplateIndex(
							result.ByDisplayName,
							NormalizePlayerRpStrictExactLookup(
								item.Name?.ToString()),
							item);
					}
					catch
					{
						itemErrorCount++;
					}
				}
				SortPlayerRpExactTemplateIndex(result.ByStringId);
				SortPlayerRpExactTemplateIndex(result.ByDisplayName);
				scanCompleted = true;
			}
			catch (Exception ex)
			{
				try
				{
					Logger.Log(
						"Logic",
						"[PlayerRpCraft] exact_template_cache_failed error="
							+ ex.GetType().Name + ":" + ex.Message);
				}
				catch
				{
				}
			}

			bool hasEntries =
				result.ByStringId.Count > 0
				|| result.ByDisplayName.Count > 0;
			PlayerRpExactTemplateLookupCacheOwner = owner;
			PlayerRpExactTemplateLookup = result;
			PlayerRpExactTemplateLookupCacheReady =
				scanCompleted && hasEntries;
			PlayerRpExactTemplateLookupCacheRetryAfterUtc =
				PlayerRpExactTemplateLookupCacheReady
					? DateTime.MinValue
					: DateTime.UtcNow.AddSeconds(1d);
			if (!PlayerRpExactTemplateLookupCacheReady
				|| itemErrorCount > 0)
			{
				try
				{
					Logger.Log(
						"Logic",
						"[PlayerRpCraft] exact_template_cache_status"
							+ " ready=" + PlayerRpExactTemplateLookupCacheReady
							+ " scan_completed=" + scanCompleted
							+ " scanned=" + scannedCount.ToString(CultureInfo.InvariantCulture)
							+ " ids=" + result.ByStringId.Count.ToString(CultureInfo.InvariantCulture)
							+ " names=" + result.ByDisplayName.Count.ToString(CultureInfo.InvariantCulture)
							+ " item_errors=" + itemErrorCount.ToString(CultureInfo.InvariantCulture));
				}
				catch
				{
				}
			}
			return PlayerRpExactTemplateLookup;
		}
	}

	private static void AddPlayerRpExactTemplateIndex(
		Dictionary<string, List<ItemObject>> index,
		string rawKey,
		ItemObject item)
	{
		string key = (rawKey ?? "").Trim();
		if (index == null
			|| item == null
			|| string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		if (!index.TryGetValue(key, out List<ItemObject> values))
		{
			values = new List<ItemObject>();
			index[key] = values;
		}
		if (!values.Contains(item))
		{
			values.Add(item);
		}
	}

	private static void SortPlayerRpExactTemplateIndex(
		Dictionary<string, List<ItemObject>> index)
	{
		foreach (List<ItemObject> values
			in index?.Values ?? Enumerable.Empty<List<ItemObject>>())
		{
			values.Sort((left, right) => string.Compare(
				left?.StringId ?? "",
				right?.StringId ?? "",
				StringComparison.OrdinalIgnoreCase));
		}
	}

	private static void ClearPlayerRpExactTemplateLookupCache()
	{
		lock (PlayerRpExactTemplateLookupCacheLock)
		{
			PlayerRpExactTemplateLookupCacheOwner = null;
			PlayerRpExactTemplateLookup =
				new PlayerRpExactTemplateLookupCache();
			PlayerRpExactTemplateLookupCacheReady = false;
			PlayerRpExactTemplateLookupCacheRetryAfterUtc =
				DateTime.MinValue;
		}
	}

	private static bool TryBuildPlayerRpCraftTemplateCandidates(
		string requestedName,
		int investedDenars,
		bool isEquipment,
		int maxCount,
		out List<PlayerRpCraftTemplateCandidate> candidates,
		out string error)
	{
		candidates = new List<PlayerRpCraftTemplateCandidate>();
		error = "";
		string name = (requestedName ?? "").Trim();
		if (name.IndexOf(PlayerRpCraftedAfefMarker, StringComparison.Ordinal) >= 0)
		{
			error = "物品名称不能包含系统保留标记 "
				+ PlayerRpCraftedAfefMarker
				+ "。";
			return false;
		}
		if (!IsValidGeneratedRpAssetNameForExternal(name))
		{
			error = "请输入有效的物品名称。";
			return false;
		}
		if (investedDenars <= 0)
		{
			error = "投入金额必须大于 0。";
			return false;
		}
		Hero player = Hero.MainHero;
		if (player == null || MobileParty.MainParty?.ItemRoster == null)
		{
			error = "玩家或玩家背包尚未就绪。";
			return false;
		}
		if (player.Gold < investedDenars)
		{
			error = "第纳尔不足。";
			return false;
		}

		HashSet<string> seen =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (isEquipment)
		{
			Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> cache =
				GetGeneratedRpEquipmentTemplateCache();
			bool hasSuffix = TryResolveGeneratedRpEquipmentSuffix(
				name,
				out GeneratedRpEquipmentKind kind,
				out _);
			if (hasSuffix
				&& (kind == GeneratedRpEquipmentKind.Horse
					|| kind == GeneratedRpEquipmentKind.Banner))
			{
				error = kind == GeneratedRpEquipmentKind.Horse
					? "玩家RP制造暂不支持直接制造马匹；马甲、马铠和马具仍受支持。"
					: "玩家RP制造暂不支持旗帜模板。";
				return false;
			}
			cache.TryGetValue(
				hasSuffix ? kind : GeneratedRpEquipmentKind.AnyEquipment,
				out List<GeneratedRpEquipmentTemplateCandidate> source);
			string exactLookup = hasSuffix ? "" : NormalizePlayerRpExactLookup(name);
			foreach (GeneratedRpEquipmentTemplateCandidate sourceCandidate
				in source ?? Enumerable.Empty<GeneratedRpEquipmentTemplateCandidate>())
			{
				ItemObject item = sourceCandidate?.Item;
				if (item == null
					|| IsGeneratedRewardItemStringId(item.StringId)
					|| !PlayerRpCraftItemComponentService.IsSafeEquipmentTemplate(item, out _)
					|| (hasSuffix
						&& !DoesGeneratedRpEquipmentTemplateMatchKind(item, kind))
					|| (!hasSuffix
						&& !string.Equals(
							NormalizePlayerRpExactLookup(item.StringId),
							exactLookup,
							StringComparison.Ordinal)
						&& !string.Equals(
							NormalizePlayerRpExactLookup(item.Name?.ToString()),
							exactLookup,
							StringComparison.Ordinal)))
				{
					continue;
				}
				AddPlayerRpTemplateCandidate(
					candidates,
					seen,
					name,
					item,
					isEquipment: true,
					aliases: sourceCandidate.Aliases);
			}
			if (!hasSuffix && candidates.Count == 0)
			{
				error = "装备名称需要以明确的中英文装备后缀结尾，或与已加载模板名称/ID完全一致。";
				return false;
			}
		}
		else
		{
			if (TryResolveGeneratedRpEquipmentSuffix(name, out _, out _))
			{
				error = "名称属于武器装备；请先勾选 FORGE AS WEAPON/EQUIPMENT?。";
				return false;
			}
			if (TryResolveGeneratedRpFoodSuffix(
				name,
				out GeneratedRpFoodKind foodKind,
				out _))
			{
				Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>> cache =
					GetGeneratedRpFoodTemplateCache();
				cache.TryGetValue(
					foodKind,
					out List<GeneratedRpFoodTemplateCandidate> source);
				if (source == null || source.Count == 0)
				{
					cache.TryGetValue(GeneratedRpFoodKind.AnyFood, out source);
				}
				foreach (GeneratedRpFoodTemplateCandidate sourceCandidate
					in source ?? Enumerable.Empty<GeneratedRpFoodTemplateCandidate>())
				{
					ItemObject item = sourceCandidate?.Item;
					if (!IsCloneSafeGeneratedRpFoodTemplateItem(item)
						|| IsGeneratedRewardItemStringId(item.StringId)
						|| !DoesGeneratedRpFoodTemplateMatchKind(item, foodKind))
					{
						continue;
					}
					AddPlayerRpTemplateCandidate(
						candidates,
						seen,
						name,
						item,
						isEquipment: false,
						foodKind: foodKind,
						aliases: sourceCandidate.Aliases);
				}
			}
			else
			{
				foreach (GeneratedRpFoodTemplateCandidate sourceCandidate
					in GetPlayerRpMiscTemplateCandidates())
				{
					ItemObject item = sourceCandidate?.Item;
					if (!IsPlayerRpSafeGoodsTemplate(item)
						|| IsGeneratedRewardItemStringId(item.StringId))
					{
						continue;
					}
					AddPlayerRpTemplateCandidate(
						candidates,
						seen,
						name,
						item,
						isEquipment: false,
						aliases: sourceCandidate.Aliases);
				}
			}
			candidates.RemoveAll(candidate => candidate == null);
		}
		if (candidates.Count == 0)
		{
			error = isEquipment
				? "该类别没有安全且可复制的武器装备模板。"
				: "没有安全的食物或杂物模板。";
			return false;
		}
		candidates.Sort(ComparePlayerRpTemplateCandidates);
		int limit = Math.Max(1, Math.Min(PlayerRpTemplateCandidateLimit, maxCount));
		if (candidates.Count > limit)
		{
			candidates.RemoveRange(limit, candidates.Count - limit);
		}
		for (int index = 0; index < candidates.Count; index++)
		{
			candidates[index].Rank = index + 1;
		}
		return true;
	}

	private static bool TryValidatePlayerRpSelectedTemplateForCurrentRequest(
		string requestedName,
		int investedDenars,
		bool isEquipment,
		PlayerRpCraftTemplateCandidate selected,
		bool requireExactGameItemMatch,
		out ItemObject template,
		out int currentStandardPrice,
		out string error)
	{
		template = null;
		currentStandardPrice = 0;
		error = "";
		string name = (requestedName ?? "").Trim();
		if (name.IndexOf(PlayerRpCraftedAfefMarker, StringComparison.Ordinal) >= 0)
		{
			error = "物品名称不能包含系统保留标记 "
				+ PlayerRpCraftedAfefMarker
				+ "。";
			return false;
		}
		if (!IsValidGeneratedRpAssetNameForExternal(name))
		{
			error = "请输入有效的物品名称。";
			return false;
		}
		if (investedDenars <= 0)
		{
			error = "投入金额必须大于 0。";
			return false;
		}
		Hero player = Hero.MainHero;
		if (player == null || MobileParty.MainParty?.ItemRoster == null)
		{
			error = "玩家或玩家背包尚未就绪。";
			return false;
		}
		if (player.Gold < investedDenars)
		{
			error = "第纳尔不足。";
			return false;
		}
		string selectedId = (selected?.TemplateStringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(selectedId))
		{
			error = "所选模板ID为空。";
			return false;
		}
		template = ResolveItemById(selectedId);
		if (template == null
			|| IsGeneratedRewardItemStringId(template.StringId))
		{
			error = "所选模板已经不可用。";
			template = null;
			return false;
		}

		if (requireExactGameItemMatch)
		{
			if (!TryResolvePlayerRpExactRegisteredTemplate(
					name,
					isEquipment,
					out bool exactMatchFound,
					out PlayerRpCraftTemplateCandidate currentExact,
					out string exactError)
				|| !exactMatchFound
				|| currentExact == null
				|| !string.Equals(
					currentExact.TemplateStringId,
					selectedId,
					StringComparison.OrdinalIgnoreCase))
			{
				error = string.IsNullOrWhiteSpace(exactError)
					? "游戏数据中的精确匹配物品已经变化，请重新确认。"
					: exactError;
				template = null;
				return false;
			}
			if (isEquipment
				? !IsPlayerRpSafeExactEquipmentTemplate(template)
				: !IsPlayerRpSafeExactMiscTemplate(template))
			{
				error = "精确匹配模板已经不再属于当前安全制造类别。";
				template = null;
				return false;
			}
		}
		else if (isEquipment)
		{
			bool hasSuffix = TryResolveGeneratedRpEquipmentSuffix(
				name,
				out GeneratedRpEquipmentKind kind,
				out _);
			if (hasSuffix
				&& (kind == GeneratedRpEquipmentKind.Horse
					|| kind == GeneratedRpEquipmentKind.Banner))
			{
				error = kind == GeneratedRpEquipmentKind.Horse
					? "玩家RP制造暂不支持直接制造马匹；马甲、马铠和马具仍受支持。"
					: "玩家RP制造暂不支持旗帜模板。";
				template = null;
				return false;
			}
			if (!PlayerRpCraftItemComponentService.IsSafeEquipmentTemplate(
					template,
					out _)
				|| (hasSuffix
					&& !DoesGeneratedRpEquipmentTemplateMatchKind(template, kind)))
			{
				error = "所选模板不再属于当前名称对应的安全武器装备类别。";
				template = null;
				return false;
			}
			if (!hasSuffix)
			{
				string exactLookup = NormalizePlayerRpExactLookup(name);
				if (!string.Equals(
						NormalizePlayerRpExactLookup(template.StringId),
						exactLookup,
						StringComparison.Ordinal)
					&& !string.Equals(
						NormalizePlayerRpExactLookup(template.Name?.ToString()),
						exactLookup,
						StringComparison.Ordinal))
				{
					error = "无装备后缀时，物品名称必须与所选模板名称或ID完全一致。";
					template = null;
					return false;
				}
			}
		}
		else
		{
			if (TryResolveGeneratedRpEquipmentSuffix(name, out _, out _))
			{
				error = "名称属于武器装备；请先勾选 FORGE AS WEAPON/EQUIPMENT?。";
				template = null;
				return false;
			}
			if (TryResolveGeneratedRpFoodSuffix(
				name,
				out GeneratedRpFoodKind foodKind,
				out _))
			{
				if (!IsCloneSafeGeneratedRpFoodTemplateItem(template)
					|| !DoesGeneratedRpFoodTemplateMatchKind(template, foodKind))
				{
					error = "所选模板不再属于当前食物、饮料、水或药物类别。";
					template = null;
					return false;
				}
			}
			else if (!IsPlayerRpSafeGoodsTemplate(template))
			{
				error = "所选模板不再是安全的杂物或文书模板。";
				template = null;
				return false;
			}
		}

		currentStandardPrice = GetPlayerRpSafeTemplateBaseValue(template);
		if (selected.StandardPrice != currentStandardPrice)
		{
			error = "所选模板价格已经变化，请重新建立候选榜单。";
			template = null;
			currentStandardPrice = 0;
			return false;
		}
		return true;
	}

	private static void AddPlayerRpTemplateCandidate(
		List<PlayerRpCraftTemplateCandidate> candidates,
		HashSet<string> seen,
		string requestedName,
		ItemObject item,
		bool isEquipment,
		GeneratedRpFoodKind foodKind = GeneratedRpFoodKind.AnyFood,
		IEnumerable<string> aliases = null)
	{
		string itemId = (item?.StringId ?? "").Trim();
		if (candidates == null
			|| seen == null
			|| string.IsNullOrWhiteSpace(itemId)
			|| !seen.Add(itemId))
		{
			return;
		}
		float score = aliases == null
			? Math.Max(
				WorldEntityRetrievalService.CalculateFuzzyScoreForExternal(
					requestedName,
					itemId),
				WorldEntityRetrievalService.CalculateFuzzyScoreForExternal(
					requestedName,
					item.Name?.ToString()))
			: WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(
				requestedName,
				aliases);
		candidates.Add(new PlayerRpCraftTemplateCandidate
		{
			TemplateStringId = itemId,
			DisplayName = item.Name?.ToString() ?? itemId,
			TypeLabel = GetPlayerRpTemplateTypeLabel(item),
			StandardPrice = GetPlayerRpSafeTemplateBaseValue(item),
			MatchScore = score,
			Suitability = isEquipment
				? GetGeneratedRpTemplateSuitability(item)
				: GetGeneratedRpFoodTemplateSuitability(
					item,
					foodKind),
			TieBreaker = GetGeneratedRpTemplateTieBreaker(requestedName, item)
		});
	}

	private static int ComparePlayerRpTemplateCandidates(
		PlayerRpCraftTemplateCandidate left,
		PlayerRpCraftTemplateCandidate right)
	{
		if (ReferenceEquals(left, right))
		{
			return 0;
		}
		if (left == null)
		{
			return 1;
		}
		if (right == null)
		{
			return -1;
		}
		int result = right.MatchScore.CompareTo(left.MatchScore);
		if (result != 0)
		{
			return result;
		}
		result = right.Suitability.CompareTo(left.Suitability);
		if (result != 0)
		{
			return result;
		}
		result = right.TieBreaker.CompareTo(left.TieBreaker);
		return result != 0
			? result
			: string.Compare(
				left.TemplateStringId ?? "",
				right.TemplateStringId ?? "",
				StringComparison.OrdinalIgnoreCase);
	}

	private static string GetPlayerRpTemplateTypeLabel(ItemObject item)
	{
		if (item == null)
		{
			return "未知";
		}
		if (IsGeneratedRpWhipWeaponTemplateItem(item))
		{
			return "鞭/Whip";
		}
		string localized = GetItemPromptTypeLabel(item);
		string technical = item.Type.ToString();
		try
		{
			WeaponComponentData primary = item.PrimaryWeapon;
			if (primary != null)
			{
				technical += "/" + primary.WeaponClass;
			}
		}
		catch
		{
		}
		return string.IsNullOrWhiteSpace(localized)
			? technical
			: localized + " (" + technical + ")";
	}


	private static bool TryResolvePlayerRpEquipmentTemplate(
		string name,
		out ItemObject template,
		out string error)
	{
		template = null;
		error = "";
		Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> cache =
			GetGeneratedRpEquipmentTemplateCache();
		List<GeneratedRpEquipmentTemplateCandidate> candidates;
		if (TryResolveGeneratedRpEquipmentSuffix(name, out GeneratedRpEquipmentKind kind, out _))
		{
			cache.TryGetValue(kind, out candidates);
		}
		else
		{
			cache.TryGetValue(GeneratedRpEquipmentKind.AnyEquipment, out candidates);
			string normalized = NormalizePlayerRpExactLookup(name);
			candidates = (candidates ?? new List<GeneratedRpEquipmentTemplateCandidate>())
				.Where(candidate => candidate?.Item != null
					&& (string.Equals(
							NormalizePlayerRpExactLookup(candidate.Item.StringId),
							normalized,
							StringComparison.Ordinal)
						|| string.Equals(
							NormalizePlayerRpExactLookup(
								candidate.Item.Name?.ToString()),
							normalized,
							StringComparison.Ordinal)))
				.ToList();
			if (candidates.Count == 0)
			{
				error = "装备名称需要以明确的中英文装备后缀结尾，或与已加载模板名称/ID完全一致。";
				return false;
			}
		}
		if (candidates == null || candidates.Count == 0)
		{
			error = "该装备类别没有可用模板。";
			return false;
		}

		float bestScore = float.MinValue;
		int bestSuitability = int.MinValue;
		float bestTieBreaker = float.MinValue;
		foreach (GeneratedRpEquipmentTemplateCandidate candidate in candidates)
		{
			ItemObject item = candidate?.Item;
			if (!PlayerRpCraftItemComponentService.IsSafeEquipmentTemplate(item, out _))
			{
				continue;
			}
			float score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(
				name,
				candidate.Aliases);
			int suitability = GetGeneratedRpTemplateSuitability(item);
			float tieBreaker = GetGeneratedRpTemplateTieBreaker(name, item);
			if (template == null
				|| score > bestScore + 0.00001f
				|| (Math.Abs(score - bestScore) <= 0.00001f
					&& tieBreaker > bestTieBreaker + 0.00001f)
				|| (Math.Abs(score - bestScore) <= 0.00001f
					&& Math.Abs(tieBreaker - bestTieBreaker) <= 0.00001f
					&& suitability > bestSuitability)
				|| (Math.Abs(score - bestScore) <= 0.00001f
					&& Math.Abs(tieBreaker - bestTieBreaker) <= 0.00001f
					&& suitability == bestSuitability
					&& string.Compare(
						item.StringId ?? "",
						template.StringId ?? "",
						StringComparison.OrdinalIgnoreCase) < 0))
			{
				template = item;
				bestScore = score;
				bestSuitability = suitability;
				bestTieBreaker = tieBreaker;
			}
		}
		if (template == null)
		{
			error = "匹配到的装备模板缺少安全模型或完整组件。";
			return false;
		}
		return true;
	}

	private static bool TryResolvePlayerRpMiscTemplate(
		string name,
		out ItemObject template,
		out string craftKind)
	{
		template = null;
		craftKind = "";
		bool hasFoodSuffix = TryResolveGeneratedRpFoodSuffix(name, out _, out _);
		if (TryResolveGeneratedRpFoodTemplate(
			name,
			out ItemObject food,
			out _,
			out _,
			out _,
			out _)
			&& food != null)
		{
			template = food;
			craftKind = "food";
			return true;
		}
		if (hasFoodSuffix)
		{
			// Never turn a recognized food, drink, water, or medicine name into a
			// non-consumable goods fallback when a conversion mod has no safe food.
			return false;
		}
		if (TryResolvePlayerRpGoodsTemplate(name, out template))
		{
			craftKind = "goods";
			return true;
		}
		return false;
	}

	private static bool TryResolvePlayerRpGoodsTemplate(
		string name,
		out ItemObject template)
	{
		template = null;
		List<GeneratedRpFoodTemplateCandidate> candidates = GetPlayerRpMiscTemplateCandidates();
		float bestScore = float.MinValue;
		foreach (GeneratedRpFoodTemplateCandidate candidate in candidates)
		{
			if (!IsPlayerRpSafeGoodsTemplate(candidate?.Item))
			{
				continue;
			}
			float score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(
				name,
				candidate.Aliases);
			if (template == null
				|| score > bestScore + 0.00001f
				|| (Math.Abs(score - bestScore) <= 0.00001f
					&& string.Compare(
						candidate.Item.StringId ?? "",
						template.StringId ?? "",
						StringComparison.OrdinalIgnoreCase) < 0))
			{
				template = candidate.Item;
				bestScore = score;
			}
		}
		if (template == null || bestScore <= 0.00001f)
		{
			ItemObject tools = DefaultItems.Tools ?? ResolveItemById("tools");
			if (IsPlayerRpSafeGoodsTemplate(tools))
			{
				template = tools;
			}
		}
		return template != null;
	}

	private static bool IsPlayerRpSafeGoodsTemplate(ItemObject item)
	{
		return item != null
			&& (item.Type == ItemObject.ItemTypeEnum.Goods
				|| item.Type == ItemObject.ItemTypeEnum.Book)
			&& !IsGeneratedRpFoodTemplateItem(item)
			&& IsCloneSafeGeneratedRewardTemplateItem(item);
	}

	private static bool TryResolvePlayerRpFoodTemplateForRestore(
		GeneratedRewardItemRecord record,
		PlayerRpCraftData craft,
		out ItemObject template)
	{
		template = null;
		foreach (string candidateId in new[]
		{
			craft?.EffectiveTemplateStringId,
			record?.TemplateStringId,
			craft?.OriginalTemplateStringId
		}.Where(value => !string.IsNullOrWhiteSpace(value))
			.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			ItemObject exact = ResolveItemById(candidateId);
			if (IsCloneSafeGeneratedRpFoodTemplateItem(exact))
			{
				template = exact;
				return true;
			}
		}

		string requestedName = craft?.OriginalRequestedName ?? record?.DisplayName ?? "";
		if (TryResolveGeneratedRpFoodTemplate(
			requestedName,
			out ItemObject resolved,
			out _,
			out _,
			out _,
			out _)
			&& IsCloneSafeGeneratedRpFoodTemplateItem(resolved))
		{
			template = resolved;
			return true;
		}

		if (!GetGeneratedRpFoodTemplateCache().TryGetValue(
			GeneratedRpFoodKind.AnyFood,
			out List<GeneratedRpFoodTemplateCandidate> candidates)
			|| candidates == null)
		{
			return false;
		}
		float bestScore = float.MinValue;
		int bestSuitability = int.MinValue;
		foreach (GeneratedRpFoodTemplateCandidate candidate in candidates)
		{
			ItemObject item = candidate?.Item;
			if (!IsCloneSafeGeneratedRpFoodTemplateItem(item))
			{
				continue;
			}
			float score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(
				requestedName,
				candidate.Aliases);
			int suitability = GetGeneratedRpFoodTemplateSuitability(
				item,
				GeneratedRpFoodKind.AnyFood);
			if (template == null
				|| score > bestScore + 0.00001f
				|| (Math.Abs(score - bestScore) <= 0.00001f
					&& suitability > bestSuitability)
				|| (Math.Abs(score - bestScore) <= 0.00001f
					&& suitability == bestSuitability
					&& string.Compare(
						item.StringId ?? "",
						template.StringId ?? "",
						StringComparison.OrdinalIgnoreCase) < 0))
			{
				template = item;
				bestScore = score;
				bestSuitability = suitability;
			}
		}
		return template != null;
	}

	private static bool TryResolvePlayerRpGoodsTemplateForRestore(
		GeneratedRewardItemRecord record,
		PlayerRpCraftData craft,
		out ItemObject template)
	{
		template = null;
		foreach (string candidateId in new[]
		{
			craft?.EffectiveTemplateStringId,
			record?.TemplateStringId,
			craft?.OriginalTemplateStringId
		}.Where(value => !string.IsNullOrWhiteSpace(value))
			.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			ItemObject exact = ResolveItemById(candidateId);
			if (IsPlayerRpSafeGoodsTemplate(exact))
			{
				template = exact;
				return true;
			}
		}
		return TryResolvePlayerRpGoodsTemplate(
			craft?.OriginalRequestedName ?? record?.DisplayName ?? "",
			out template);
	}

	private static bool TryResolvePlayerRpJunkTemplate(
		GeneratedRewardItemRecord record,
		PlayerRpCraftData craft,
		out ItemObject template)
	{
		template = DefaultItems.Trash;
		if (IsPlayerRpSafeGoodsTemplate(template))
		{
			return true;
		}
		template = ResolveItemById("trash");
		if (IsPlayerRpSafeGoodsTemplate(template))
		{
			return true;
		}
		// Junk is a defined Bannerlord item, not a low-score synonym fallback.
		// If a conversion removed or invalidated it, fail safely so the crafting
		// transaction can refund instead of presenting an unrelated good as junk.
		template = null;
		return false;
	}

	private static List<GeneratedRpFoodTemplateCandidate> GetPlayerRpMiscTemplateCandidates()
	{
		object owner = (object)Game.Current?.ObjectManager ?? MBObjectManager.Instance;
		lock (PlayerRpMiscTemplateCacheLock)
		{
			if (owner != null
				&& ReferenceEquals(owner, PlayerRpMiscTemplateCacheOwner)
				&& PlayerRpMiscTemplateCandidates != null)
			{
				return PlayerRpMiscTemplateCandidates;
			}
			List<GeneratedRpFoodTemplateCandidate> result =
				new List<GeneratedRpFoodTemplateCandidate>();
			try
			{
				IEnumerable<ItemObject> items =
					Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>()
				?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
				foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
				{
					if (!IsPlayerRpSafeGoodsTemplate(item)
						|| IsGeneratedRewardItemStringId(item.StringId))
					{
						continue;
					}
					result.Add(new GeneratedRpFoodTemplateCandidate
					{
						Item = item,
						Aliases = BuildGeneratedRpTemplateAliases(item)
					});
				}
			}
			catch
			{
			}
			PlayerRpMiscTemplateCacheOwner = owner;
			PlayerRpMiscTemplateCandidates = result;
			return PlayerRpMiscTemplateCandidates;
		}
	}

	private static int GetPlayerRpSafeTemplateBaseValue(ItemObject template)
	{
		if (template == null)
		{
			return 100;
		}
		try
		{
			if (template.Value > 0)
			{
				return template.Value;
			}
		}
		catch
		{
		}

		object owner = (object)Game.Current?.ObjectManager ?? MBObjectManager.Instance;
		lock (PlayerRpPriceCacheLock)
		{
			if (owner == null || !ReferenceEquals(owner, PlayerRpPriceCacheOwner))
			{
				Dictionary<int, List<int>> valuesByType = new Dictionary<int, List<int>>();
				try
				{
					IEnumerable<ItemObject> items =
						Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>()
						?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
					foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
					{
						if (item == null
							|| item.Value <= 0
							|| IsGeneratedRewardItemStringId(item.StringId))
						{
							continue;
						}
						int type = (int)item.Type;
						if (!valuesByType.TryGetValue(type, out List<int> values))
						{
							values = new List<int>();
							valuesByType[type] = values;
						}
						values.Add(item.Value);
					}
				}
				catch
				{
				}
				Dictionary<int, int> medians = new Dictionary<int, int>();
				foreach (KeyValuePair<int, List<int>> pair in valuesByType)
				{
					pair.Value.Sort();
					if (pair.Value.Count > 0)
					{
						int upperIndex = pair.Value.Count / 2;
						medians[pair.Key] = pair.Value.Count % 2 == 1
							? pair.Value[upperIndex]
							: (int)(((long)pair.Value[upperIndex - 1]
								+ pair.Value[upperIndex]) / 2L);
					}
				}
				PlayerRpPriceCacheOwner = owner;
				PlayerRpMedianPriceByItemType = medians;
			}
			return PlayerRpMedianPriceByItemType.TryGetValue(
				(int)template.Type,
				out int median)
				? Math.Max(1, median)
				: 100;
		}
	}

	private static ItemObject ResolveGeneratedRewardRecordTemplateItem(
		GeneratedRewardItemRecord record,
		string source)
	{
		if (record?.PlayerCraft == null)
		{
			ItemObject exactPlayerCraftTemplate = ResolveItemById(record?.TemplateStringId);
			if (IsAuthorizedPlayerRpCraftGenerationKey(record?.GeneratedStringId)
				&& IsSafePlayerRpCraftGenerationTemplate(exactPlayerCraftTemplate))
			{
				return exactPlayerCraftTemplate;
			}
			return ResolveCloneSafeGeneratedRewardTemplateItem(
				exactPlayerCraftTemplate,
				record?.DisplayName,
				source,
				record?.GeneratedStringId);
		}

		PlayerRpCraftData craft = record.PlayerCraft;
		string craftKind = (craft.CraftKind ?? "").Trim();
		PlayerRpCraftItemStatsSnapshot snapshot = craft.StatsSnapshot;
		if (string.Equals(
			craftKind,
			PlayerRpCraftTerminalInvalidKind,
			StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		if (string.Equals(craftKind, "remnant", StringComparison.OrdinalIgnoreCase))
		{
			craft.StatsSnapshot = null;
			snapshot = null;
		}
		else if (string.Equals(craftKind, "equipment", StringComparison.OrdinalIgnoreCase)
			&& snapshot == null)
		{
			TryTransitionPlayerRpCraftEquipmentToRemnant(
				record,
				craft,
				source,
				"missing_snapshot",
				out ItemObject missingSnapshotRemnant);
			return missingSnapshotRemnant;
		}
		else if (!string.Equals(craftKind, "equipment", StringComparison.OrdinalIgnoreCase)
			&& snapshot != null)
		{
			craft.StatsSnapshot = null;
			snapshot = null;
		}
		if (string.Equals(craftKind, "equipment", StringComparison.OrdinalIgnoreCase)
			&& snapshot != null)
		{
			foreach (string candidateId in new[]
			{
				craft.OriginalTemplateStringId,
				craft.EffectiveTemplateStringId,
				record.TemplateStringId
			}.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				ItemObject exact = ResolveItemById(candidateId);
				if (PlayerRpCraftItemComponentService.IsSafeEquipmentTemplate(exact, out _)
					&& PlayerRpCraftItemComponentService.IsStructurallyCompatible(exact, snapshot))
				{
					record.TemplateStringId = exact.StringId ?? record.TemplateStringId;
					craft.EffectiveTemplateStringId = record.TemplateStringId;
					craft.CraftKind = "equipment";
					return exact;
				}
			}

			if (GetGeneratedRpEquipmentTemplateCache().TryGetValue(
				GeneratedRpEquipmentKind.AnyEquipment,
				out List<GeneratedRpEquipmentTemplateCandidate> candidates))
			{
				ItemObject best = null;
				float bestScore = float.MinValue;
				foreach (GeneratedRpEquipmentTemplateCandidate candidate in candidates)
				{
					ItemObject item = candidate?.Item;
					if (!PlayerRpCraftItemComponentService.IsSafeEquipmentTemplate(item, out _)
						|| !PlayerRpCraftItemComponentService.IsStructurallyCompatible(item, snapshot))
					{
						continue;
					}
					float score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(
						craft.OriginalRequestedName,
						candidate.Aliases);
					if (best == null
						|| score > bestScore + 0.00001f
						|| (Math.Abs(score - bestScore) <= 0.00001f
							&& string.Compare(
								item.StringId ?? "",
								best.StringId ?? "",
								StringComparison.OrdinalIgnoreCase) < 0))
					{
						best = item;
						bestScore = score;
					}
				}
				if (best != null)
				{
					record.TemplateStringId = best.StringId ?? record.TemplateStringId;
					craft.EffectiveTemplateStringId = record.TemplateStringId;
					craft.CraftKind = "equipment";
					Logger.Log("Logic", "[PlayerRpCraft] restore_template_fallback item="
						+ record.GeneratedStringId
						+ " template="
						+ record.TemplateStringId
						+ " source="
						+ (source ?? ""));
					return best;
				}
			}

			TryTransitionPlayerRpCraftEquipmentToRemnant(
				record,
				craft,
				source,
				"incompatible_snapshot_or_template",
				out ItemObject incompatibleSnapshotRemnant);
			return incompatibleSnapshotRemnant;
		}

		ItemObject effective;
		bool restored;
		if (string.Equals(craftKind, "food", StringComparison.OrdinalIgnoreCase))
		{
			restored = TryResolvePlayerRpFoodTemplateForRestore(
				record,
				craft,
				out effective);
			craft.CraftKind = "food";
		}
		else if (string.Equals(craftKind, "goods", StringComparison.OrdinalIgnoreCase))
		{
			restored = TryResolvePlayerRpGoodsTemplateForRestore(
				record,
				craft,
				out effective);
			craft.CraftKind = "goods";
		}
		else if (string.Equals(craftKind, "junk", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(craftKind, "remnant", StringComparison.OrdinalIgnoreCase))
		{
			restored = TryResolvePlayerRpJunkTemplate(
				record,
				craft,
				out effective);
			craft.CraftKind = string.Equals(
				craftKind,
				"remnant",
				StringComparison.OrdinalIgnoreCase)
				? "remnant"
				: "junk";
		}
		else
		{
			try
			{
				Logger.Log("Logic", "[PlayerRpCraft] restore_rejected_unknown_kind item="
					+ (record.GeneratedStringId ?? "")
					+ " kind="
					+ craftKind
					+ " source="
					+ (source ?? ""));
			}
			catch
			{
			}
			return null;
		}
		if (restored && effective != null)
		{
			record.TemplateStringId = effective.StringId ?? record.TemplateStringId;
			craft.EffectiveTemplateStringId = record.TemplateStringId;
			return effective;
		}
		return null;
	}

	private static bool TryTransitionPlayerRpCraftEquipmentToRemnant(
		GeneratedRewardItemRecord record,
		PlayerRpCraftData craft,
		string source,
		string reason,
		out ItemObject remnant)
	{
		remnant = null;
		if (record == null || craft == null)
		{
			return false;
		}
		if (string.Equals(
			craft.CraftKind,
			PlayerRpCraftTerminalInvalidKind,
			StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (TryResolvePlayerRpJunkTemplate(record, craft, out remnant)
			&& remnant != null)
		{
			record.TemplateStringId = remnant.StringId ?? "trash";
			craft.EffectiveTemplateStringId = record.TemplateStringId;
			craft.StatsSnapshot = null;
			craft.CraftKind = "remnant";
			record.PlayerCraft = craft;
			RegisterGeneratedRewardManifestRecord(record);
			try
			{
				Logger.Log("Logic", "[PlayerRpCraft] restore_as_remnant item="
					+ (record.GeneratedStringId ?? "")
					+ " reason="
					+ (reason ?? "")
					+ " source="
					+ (source ?? ""));
			}
			catch
			{
			}
			return true;
		}

		craft.StatsSnapshot = null;
		craft.CraftKind = PlayerRpCraftTerminalInvalidKind;
		record.PlayerCraft = craft;
		RegisterGeneratedRewardManifestRecord(record);
		try
		{
			Logger.Log("Logic", "[PlayerRpCraft] restore_terminal_invalid item="
				+ (record.GeneratedStringId ?? "")
				+ " reason="
				+ (reason ?? "")
				+ " source="
				+ (source ?? ""));
		}
		catch
		{
		}
		return false;
	}

	private static void BuildPlayerRpEquipmentProbabilityWeights(
		int smithing,
		int templateBaseValue,
		out int good,
		out int normal,
		out int bad)
	{
		int safeSmithing = Math.Max(0, smithing);
		if (safeSmithing >= PlayerRpMasterSmithingLevel)
		{
			good = 20000;
			normal = 10000;
			bad = 0;
			return;
		}
		double difficulty =
			GetPlayerRpEquipmentRecommendedSmithingLevel(templateBaseValue);
		double q = (safeSmithing + 1d)
			/ (safeSmithing + difficulty + 2d);
		good = Math.Max(
			0,
			Math.Min(
				20000,
				(int)Math.Round(20000d * q, MidpointRounding.AwayFromZero)));
		normal = 10000;
		bad = 20000 - good;
	}

	private static int GetPlayerRpEquipmentRecommendedSmithingLevel(
		int templateBaseValue)
	{
		long safeTemplateValue = Math.Max(1L, (long)templateBaseValue);
		long recommendedLevel = (safeTemplateValue + PlayerRpTemplatePricePerSmithingLevel - 1L)
			/ PlayerRpTemplatePricePerSmithingLevel;
		return (int)Math.Max(1L, Math.Min(int.MaxValue, recommendedLevel));
	}

	private static void ResolvePlayerRpEquipmentOutcome(
		int invested,
		int templateBaseValue,
		int smithing,
		string outcome,
		out bool underfunded,
		out double multiplier,
		out int upgradeLevel,
		out int additiveBonus)
	{
		underfunded = invested < templateBaseValue;
		multiplier = 1d;
		upgradeLevel = 0;
		additiveBonus = 0;
		if (underfunded)
		{
			double ratio = Math.Max(
				double.Epsilon,
				Math.Min(1d, invested / (double)Math.Max(1, templateBaseValue)));
			multiplier = string.Equals(outcome, "good", StringComparison.OrdinalIgnoreCase)
				? ratio * (2d - ratio)
				: string.Equals(outcome, "normal", StringComparison.OrdinalIgnoreCase)
					? ratio
					: ratio * ratio;
			return;
		}

		long threshold = Math.Max(1, templateBaseValue);
		while (threshold <= invested / 2L)
		{
			threshold *= 2L;
			upgradeLevel++;
		}
		int normalBonus =
			upgradeLevel * PlayerRpNormalAttributeBonusPerUpgradeLevel;
		int goodBonus =
			upgradeLevel * PlayerRpGoodAttributeBonusPerUpgradeLevel;
		if (Math.Max(0, smithing) >= PlayerRpMasterSmithingLevel)
		{
			normalBonus += PlayerRpMasterNormalAttributeBonus;
			goodBonus += PlayerRpMasterGoodAttributeBonus;
		}
		additiveBonus = string.Equals(outcome, "good", StringComparison.OrdinalIgnoreCase)
			? goodBonus
			: string.Equals(outcome, "normal", StringComparison.OrdinalIgnoreCase)
				? normalBonus
				: 0;
	}

	private static void AppendPlayerRpEquipmentProbabilityPreview(
		StringBuilder builder,
		int invested,
		int templateBaseValue,
		int smithing,
		int goodWeight,
		int normalWeight,
		int badWeight)
	{
		AppendPlayerRpEquipmentProbabilityRow(
			builder,
			"good",
			goodWeight,
			invested,
			templateBaseValue,
			smithing);
		AppendPlayerRpEquipmentProbabilityRow(
			builder,
			"normal",
			normalWeight,
			invested,
			templateBaseValue,
			smithing);
		AppendPlayerRpEquipmentProbabilityRow(
			builder,
			"bad",
			badWeight,
			invested,
			templateBaseValue,
			smithing);

		// The normal tier is the fixed one-third baseline (10000 / 30000).
		// Warn only when degradation is more likely than that baseline.
		if (badWeight > normalWeight)
		{
			int recommendedSmithing =
				GetPlayerRpEquipmentRecommendedSmithingLevel(
					templateBaseValue);
			builder.Append("\n\n【警告】劣化概率高于 33.33%；建议将锻造从 ")
				.Append(Math.Max(0, smithing).ToString(CultureInfo.InvariantCulture))
				.Append(" 提高至 ")
				.Append(recommendedSmithing.ToString(CultureInfo.InvariantCulture))
				.Append(" 级（模板每 ")
				.Append(PlayerRpTemplatePricePerSmithingLevel.ToString(CultureInfo.InvariantCulture))
				.Append(" 第纳尔对应 1 级）。");
		}
	}

	private static void AppendPlayerRpEquipmentProbabilityRow(
		StringBuilder builder,
		string outcome,
		int probabilityWeight,
		int invested,
		int templateBaseValue,
		int smithing)
	{
		ResolvePlayerRpEquipmentOutcome(
			invested,
			templateBaseValue,
			smithing,
			outcome,
			out bool underfunded,
			out double multiplier,
			out _,
			out int bonus);
		if (builder.Length > 0)
		{
			builder.Append('\n');
		}
		builder.Append(GetPlayerRpOutcomeLabel(outcome))
			.Append(' ')
			.Append(FormatPlayerRpProbability(probabilityWeight, 30000))
			.Append("：");
		if (underfunded)
		{
			double retainedPercent = Math.Max(0d, multiplier) * 100d;
			double weightIncreasePercent = Math.Max(
				0d,
				(1d / Math.Sqrt(Math.Max(double.Epsilon, multiplier)) - 1d) * 100d);
			builder.Append("有效正属性保留 ")
				.Append(FormatPlayerRpEffectPercent(retainedPercent))
				.Append("；重量增加 ")
				.Append(FormatPlayerRpEffectPercent(weightIncreasePercent));
		}
		else if (bonus > 0)
		{
			double weightReductionPercent =
				(1d - Math.Pow(0.98d, bonus)) * 100d;
			builder.Append("每项有效正属性 +")
				.Append(bonus.ToString(CultureInfo.InvariantCulture))
				.Append("；重量降低 ")
				.Append(FormatPlayerRpEffectPercent(weightReductionPercent));
		}
		else
		{
			builder.Append("有效正属性与重量不变");
		}
	}

	private static string FormatPlayerRpEffectPercent(double percent)
	{
		if (double.IsNaN(percent) || percent <= 0d)
		{
			return "0%";
		}
		if (percent < 0.01d)
		{
			return "<0.01%";
		}
		return percent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
	}

	private static string GetPlayerRpOutcomeLabel(string outcome)
	{
		switch ((outcome ?? "").Trim().ToLowerInvariant())
		{
		case "good":
			return "优良";
		case "normal":
			return "正常";
		case "bad":
			return "劣化";
		case "junk":
			return "垃圾物品";
		default:
			return "成功";
		}
	}

	private static string FormatPlayerRpProbability(int numerator, int denominator)
	{
		if (denominator <= 0)
		{
			return "0%";
		}
		double percent = 100d * Math.Max(0, numerator) / denominator;
		return percent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
	}

	private static string NormalizePlayerRpExactLookup(string value)
	{
		return Regex.Replace(
			(value ?? "").Trim().ToLowerInvariant(),
			"[\\s\\u3000_\\-]+",
			"");
	}

	private static string NormalizePlayerRpStrictExactLookup(string value)
	{
		string raw = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(raw))
		{
			return "";
		}
		try
		{
			return Regex.Replace(
				raw.Normalize(NormalizationForm.FormKC),
				"[\\s\\u3000]+",
				" ");
		}
		catch
		{
			return raw;
		}
	}
}
