using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimusForge;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PolicyEffectModule.ContractTests;

internal static partial class Program
{
	private static void TestKingdomlessLocalJurisdiction()
	{
		PolicyEffectModuleCatalog.TryGet("prosperityPerDay", out IPolicyEffectModule prosperity);
		PolicyEffectModuleCatalog.TryGet("hearthPerDay", out IPolicyEffectModule hearth);
		PolicyEffectModuleCatalog.TryGet("clanInfluence", out IPolicyEffectModule influence);
		PolicyEffectModuleCatalog.TryGet("heroGold", out IPolicyEffectModule gold);
		PolicyEffectModuleCatalog.TryGet("kingdomStability", out IPolicyEffectModule stability);
		var fiefs = new Dictionary<string, PolicyEffectJurisdictionFief>(StringComparer.OrdinalIgnoreCase)
		{
			["local-town"] = new PolicyEffectJurisdictionFief { Id = "local-town", OwnerClanId = "publisher",
				OwnerLeaderId = "leader", VillageIds = new[] { "local-village" } },
			["other-town"] = new PolicyEffectJurisdictionFief { Id = "other-town", OwnerClanId = "publisher", OwnerLeaderId = "leader" },
			["foreign-town"] = new PolicyEffectJurisdictionFief { Id = "foreign-town", OwnerClanId = "foreign-clan", OwnerKingdomId = "foreign" }
		};
		int reads = 0;
		PolicyEffectJurisdictionContext Context(string publisher = "publisher", string[] selected = null) =>
			new PolicyEffectJurisdictionContext(PolicyEffectScopes.Local, publisher, selected ?? new[] { "local-town" },
				id => { reads++; return fiefs.TryGetValue(id, out var fief) ? fief : null; });
		var context = Context();
		PolicyEffectCanonicalTargetSet Targets() => new PolicyEffectCanonicalTargetSet
		{
			SelectorHandles = new List<string> { "S" },
			ParentSettlementIds = new List<string> { "local-town" },
			TownIds = new List<string> { "local-town" }
		};
		bool Apply(PolicyEffectCanonicalTargetSet set, IPolicyEffectModule module, PolicyEffectJurisdictionContext scope,
			out PolicyEffectCanonicalTargetSet result, bool strict = true, string kingdom = "", string[] cross = null) =>
			PolicyEffectTargetJurisdiction.TryApply(set, module, kingdom, kingdom, cross ?? Array.Empty<string>(),
				false, strict, (kind, id) => id == "foreign" || id == "foreign-clan" ? "foreign" : "home",
				out result, out _, scope);

		Check(Apply(Targets(), prosperity, context, out var authorized) && authorized.TownIds.SequenceEqual(new[] { "local-town" }),
			"A selected independent fief must authorize a local settlement effect without a kingdom.");
		Check(Apply(Targets(), prosperity, context, out _, kingdom: "home") && reads == 1,
			"Local fief evidence must be cached within one invocation and work with or without a kingdom.");
		Check(!PolicyEffectTargetJurisdiction.TryApply(Targets(), prosperity, "", "", Array.Empty<string>(), false, true, out _, out _),
			"Missing scope evidence must not silently opt a kingdom policy into local authorization.");
		Check(!Apply(Targets(), prosperity, Context(publisher: ""), out _), "Unknown publisher must not authorize publication fiefs.");
		Check(!Apply(Targets(), prosperity, Context(selected: Array.Empty<string>()), out _), "Empty selection must fail closed.");
		var extra = Targets(); extra.TownIds.Add("other-town");
		Check(!Apply(extra, prosperity, context, out _), "S must not expand to an unselected fief of the same clan.");
		extra = Targets(); extra.ParentSettlementIds.Add("foreign-town"); extra.TownIds.Add("foreign-town");
		Check(!Apply(extra, prosperity, context, out _, kingdom: "home", cross: new[] { "foreign" }),
			"Cross authorization must not widen the publication S selector.");
		Check(Apply(extra, prosperity, context, out var filtered, strict: false)
			&& filtered.TownIds.SequenceEqual(new[] { "local-town" }) && filtered.ParentSettlementIds.SequenceEqual(new[] { "local-town" }),
			"Runtime filtering must remove unauthorized roots and leaves together.");
		var village = Targets(); village.TownIds.Clear(); village.VillageIds.Add("local-village");
		Check(Apply(village, hearth, context, out var villageResult) && villageResult.VillageIds.Count == 1,
			"A bound village must inherit the selected parent boundary.");
		village.VillageIds.Add("unrelated-village");
		Check(!Apply(village, hearth, context, out _), "Unrelated villages must be rejected.");
		var clan = Targets(); clan.TownIds.Clear(); clan.ClanIds.Add("publisher");
		Check(Apply(clan, influence, context, out var clanResult) && clanResult.ClanIds.Count == 1,
			"The selected fief's independent owner clan is a valid local projection.");
		clan.ClanIds.Add("foreign-clan");
		Check(!Apply(clan, influence, context, out _), "A selected fief cannot authorize an unrelated clan.");
		var hero = Targets(); hero.TownIds.Clear(); hero.HeroIds.Add("leader");
		Check(Apply(hero, gold, context, out var heroResult) && heroResult.HeroIds.Count == 1,
			"Owner-leader modules may target the selected fief's active leader without a kingdom.");
		hero.HeroIds.Add("other-hero");
		Check(!Apply(hero, gold, context, out _), "An unrelated hero must not enter a settlement-owner projection.");
		var nation = Targets(); nation.KingdomIds.Add("home");
		Check(!Apply(nation, stability, context, out _, kingdom: "home"), "Kingdom-only modules remain unavailable locally.");

		var mentioned = Targets(); mentioned.SelectorHandles = new List<string> { "L0" };
		mentioned.ParentSettlementIds = new List<string> { "foreign-town" }; mentioned.TownIds = new List<string> { "foreign-town" };
		Check(!Apply(mentioned, prosperity, context, out _, kingdom: "home"), "Explicit foreign fiefs still require authorization.");
		Check(Apply(mentioned, prosperity, context, out var crossResult, kingdom: "home", cross: new[] { "foreign" })
			&& crossResult.AuthorizedCrossKingdomIds.SequenceEqual(new[] { "foreign" }), "Authorized cross targets retain their boundary metadata.");
		mentioned.ParentSettlementIds = new List<string> { "other-town" }; mentioned.TownIds = new List<string> { "other-town" };
		Check(Apply(mentioned, prosperity, context, out _), "An authorized explicit local handle may reference another publisher fief.");

		var restored = JsonConvert.DeserializeObject<PolicyEffectCanonicalTargetSet>(JsonConvert.SerializeObject(Targets()));
		Check(Apply(restored, prosperity, Context(), out _), "Persisted canonical targets need no new save fields to reconstruct local jurisdiction.");
		fiefs["local-town"].OwnerClanId = "new-owner";
		Check(!Apply(restored, prosperity, Context(), out _), "Registration or renewal must reject a lost publication fief.");
		Check(Apply(restored, prosperity, Context(), out var lost, strict: false) && lost.TownIds.Count == 0 && lost.ParentSettlementIds.Count == 0,
			"Runtime refresh must remove a lost fief without expanding to other clan holdings.");
		fiefs["local-town"].OwnerClanId = "publisher";
		fiefs["local-town"].OwnerKingdomId = "new-kingdom";
		Check(Apply(restored, prosperity, Context(), out _, kingdom: "old-kingdom"),
			"A still-owned publication fief remains local when the publisher changes kingdoms.");

		var leader = CreateUninitializedPolicyOwnerHero("publisher-leader");
		var owner = CreateUninitializedPolicyOwnerClan("publisher", leader);
		var town = CreateUninitializedPolicyOwnerSettlement("local-town", owner);
		var projected = new PolicyEffectCanonicalTargetSet();
		Check((bool)InvokeStatic(typeof(CustomPolicyBehavior), "AddPolicyEffectPrimaryTargetForModule", new object[]
		{
			projected, town, influence, PolicyEffectPrimaryTargetOrigin.SettlementSelector, null, PolicyEffectScopes.Local
		}, 6) && projected.ClanIds.SequenceEqual(new[] { "publisher" }),
			"Production settlement projection must retain an independent owner in local scope.");
		var unchangedNational = new PolicyEffectCanonicalTargetSet();
		InvokeStatic(typeof(CustomPolicyBehavior), "AddPolicyEffectPrimaryTargetForModule", new object[]
		{
			unchangedNational, town, influence, PolicyEffectPrimaryTargetOrigin.SettlementSelector, null
		}, 5);
		Check(unchangedNational.ClanIds.Count == 0, "Kingdom projection must retain its independent-clan opt-in contract.");
		PolicyEffectJurisdictionFief captured = PolicyEffectJurisdictionContext.CaptureFief(town);
		Check(captured.Id == "local-town" && captured.OwnerClanId == "publisher"
			&& captured.OwnerLeaderId == "publisher-leader" && captured.OwnerKingdomId == string.Empty,
			"The production capture must keep detached independent fief/owner/leader identities without a fabricated kingdom.");
		var snapshot = new PolicyTargetWorldSnapshot
		{
			JurisdictionFiefs = new Dictionary<string, PolicyEffectJurisdictionFief> { ["local-town"] = captured },
			Entities = new[] { new PolicyTargetEntitySnapshot { Kind = PolicyTargetEntityKinds.Settlement,
				EntityId = "local-town", OwnerClanId = "publisher", OwnerKingdomId = "", IsCity = true } },
			Kingdoms = new Dictionary<string, PolicyTargetKingdomSnapshot>()
		};
		Check(Apply(Targets(), prosperity, PolicyEffectJurisdictionContext.FromSnapshot(
			PolicyEffectScopes.Local, "publisher", new[] { "local-town" }, snapshot), out _),
			"Background compilation must authorize from detached snapshot evidence.");
		Check(!Apply(Targets(), prosperity, PolicyEffectJurisdictionContext.FromSnapshot(
			PolicyEffectScopes.Local, "publisher", new[] { "local-town" }, null), out _),
			"Missing snapshot evidence must not fall back to live game objects on the background compiler.");
		var plan = new PolicyTargetPlanSaveData
		{
			Branches = new List<PolicyTargetPlanBranchSaveData> { new PolicyTargetPlanBranchSaveData
			{
				Universe = PolicyTargetPlanUniverse.PrimaryFiefs,
				Relation = PolicyTargetPlanRelation.Any,
				OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.ProposerClan
			} }
		};
		Check(PolicyTargetPlanResolver.TryResolve(plan, new PolicyTargetPlanResolutionContext
		{
			Scope = PolicyEffectScopes.Local, ProposerClanId = "publisher", PlayerClanId = "publisher",
			SourceSettlementIds = new[] { "local-town" }, Snapshot = snapshot
		}, out var planResult, out string planError) && planResult.PrimarySettlementIds.SequenceEqual(new[] { "local-town" }),
			"Independent local TargetPlans must resolve a bounded fief from the same snapshot: " + planError);
		var planTargets = Targets(); planTargets.SelectorHandles = new List<string> { "P0" }; planTargets.TargetPlans.Add(plan);
		Check(Apply(planTargets, prosperity, PolicyEffectJurisdictionContext.FromSnapshot(
			PolicyEffectScopes.Local, "publisher", new[] { "local-town" }, snapshot), out _),
			"A resolved independent TargetPlan must pass the same jurisdiction gate as S.");
		TestLocalJurisdictionRegistration(town);
	}

	private static TaleWorlds.CampaignSystem.Settlements.Settlement _localJurisdictionTown;

	private static bool ResolveLocalJurisdictionTestTown(string __0, ref TaleWorlds.CampaignSystem.Settlements.Settlement __result)
	{
		__result = __0 == _localJurisdictionTown?.StringId ? _localJurisdictionTown : null;
		return false;
	}

	private static void TestLocalJurisdictionRegistration(TaleWorlds.CampaignSystem.Settlements.Settlement town)
	{
		// Intercept only the game lookup, not the production registration or refresh path.
		Type harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony", true);
		Type methodType = Type.GetType("HarmonyLib.HarmonyMethod, 0Harmony", true);
		object harmony = Activator.CreateInstance(harmonyType, "AF.Policy.LocalJurisdiction.Contract");
		MethodInfo find = typeof(TaleWorlds.CampaignSystem.Settlements.Settlement).GetMethod("Find", new[] { typeof(string) });
		MethodInfo prefix = typeof(Program).GetMethod(nameof(ResolveLocalJurisdictionTestTown), All);
		_localJurisdictionTown = town;
		harmonyType.GetMethods(All).Single(method => method.Name == "Patch" && method.GetParameters().Length == 5)
			.Invoke(harmony, new[] { (object)find, Activator.CreateInstance(methodType, prefix), null, null, null });
		try
		{
			var registration = new PolicyEffectBundleRegistration
			{
				ScopeKind = PolicyEffectScopes.Local, ProposerClanId = "publisher", EffectId = "local-registration",
				RecordId = "local-record", TargetFiefIds = new List<string> { "local-town" }, DurationDays = 30,
				ModuleEffects = new List<PolicyEffectInstanceSaveData> { new PolicyEffectInstanceSaveData
				{
					InstanceId = "local-effect", PolicyId = "local-record", ModuleId = "prosperityPerDay", SourceModuleId = "prosperityPerDay",
					PayloadSchemaVersion = 1, Payload = JObject.Parse("{\"value\":1}"), SourceScope = PolicyEffectScopes.Local,
					StartDay = 1, EndDay = 31, TargetSet = new PolicyEffectCanonicalTargetSet
					{
						SelectorHandles = new List<string> { "S" }, ParentSettlementIds = new List<string> { "local-town" },
						TownIds = new List<string> { "local-town" }
					}
				} }
			};
			var behavior = new CustomPolicyBehavior();
			object[] arguments = { registration, null, null };
			Check((bool)InvokeInstance(typeof(CustomPolicyBehavior), behavior, "TryRegisterPolicyEffectBundleInternal", arguments, 3),
				"Production local bundle registration must succeed without a kingdom: " + arguments[2]);
			var saved = (Dictionary<string, string>)typeof(CustomPolicyBehavior).GetField("_activePolicyEffects", All).GetValue(behavior);
			var restored = JsonConvert.DeserializeObject<CustomPolicyBehavior.ActivePolicyEffectSaveData>(saved["local-registration"]);
			InvokeInstance(typeof(CustomPolicyBehavior), behavior, "RefreshActivePolicyEffectCanonicalTargets",
				new object[] { restored, new[] { town }, null, false }, 4);
			Check(restored.ModuleEffects.Single().TargetSet.TownIds.SequenceEqual(new[] { "local-town" }),
				"The production refresh shared by load/structure change/renewal must retain the independent local target.");
			var newOwner = CreateUninitializedPolicyOwnerClan("new-owner", null);
			SetField(typeof(TaleWorlds.CampaignSystem.Settlements.Town), town.Town, "_ownerClan", newOwner);
			registration.EffectId = "lost-registration";
			arguments = new object[] { registration, null, null };
			Check(!(bool)InvokeInstance(typeof(CustomPolicyBehavior), behavior, "TryRegisterPolicyEffectBundleInternal", arguments, 3)
				&& !saved.ContainsKey("lost-registration"), "A fief lost before registration must not create an active bundle.");
			InvokeInstance(typeof(CustomPolicyBehavior), behavior, "RefreshActivePolicyEffectCanonicalTargets",
				new object[] { restored, Array.Empty<TaleWorlds.CampaignSystem.Settlements.Settlement>(), null, false }, 4);
			Check(restored.ModuleEffects.Single().TargetSet.TownIds.Count == 0,
				"Production refresh must remove the lost publication fief, not expand to an unrelated owner.");
		}
		finally
		{
			harmonyType.GetMethod("Unpatch", new[] { typeof(MethodBase), typeof(MethodInfo) }).Invoke(harmony, new object[] { find, prefix });
			_localJurisdictionTown = null;
		}
	}
}
