using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AnimusForge;

namespace PolicyEffectModule.ContractTests;

internal static partial class Program
{
	private static void TestPolicyEvaluationReferenceContracts()
	{
		Type service = SutType("AnimusForge.PolicyHistoryRetrievalService");
		Type entryType = SutType("AnimusForge.NpcPolicyHistoryEntry");
		Type resultType = SutType("AnimusForge.PolicyHistoryRetrievalResult");
		Type sessionType = SutType("AnimusForge.PolicyTextEmbeddingSession");
		MethodInfo retrieve = Method(service, "RetrieveForEvaluation", 7, true);
		InvokeStatic(service, "ClearTransientCache", Array.Empty<object>(), 0);
		List<string> embeddingInputs = new List<string>();
		Func<string, float[]> provider = text =>
		{
			embeddingInputs.Add(text);
			return text == "query" || text.Contains("match-high")
				? new[] { 1f, 0f }
				: text.Contains("match-low") ? new[] { 0.1f, 1f } : new[] { 0f, 1f };
		};
		object session = Activator.CreateInstance(sessionType, All, null,
			new object[] { provider, "policy-evaluation-contract" }, CultureInfo.InvariantCulture);
		IList entries = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entryType));
		object Add(string id, string owner, string status, int day, string source = "npc")
		{
			object entry = BuildNpcPolicyHistoryEntry(entryType, id, owner, status, "active", 0f, day);
			SetProperty(entryType, entry, "RawPolicyStatus", status);
			SetProperty(entryType, entry, "SourceKind", source);
			entries.Add(entry);
			return entry;
		}
		object Get(string owner, int recent = 2, int related = 3, string query = "query")
			=> retrieve.Invoke(null, new object[] { session, query, entries, owner, recent, related, 730001L });
		string[] Ids(object result, string property)
			=> Items(Property(resultType, result, property)).Select(entry => (string)Property(entryType, entry, "EntryId")).ToArray();
		string Prompt(object result) => (string)Property(resultType, result, "CombinedPrompt");

		Add("player-match-high", "player-country", "active", 1, "player_kingdom");
		Add("player-match-low", "player-country", "active", 2, "player_local");
		Add("player-unrelated", "player-country", "active", 3, "player_vassal");
		object first = Get("npc-country");
		Check(Ids(first, "RecentActivePolicies").Length == 0
			&& Ids(first, "RelatedActivePolicies").SequenceEqual(new[] { "player-match-high", "player-match-low", "player-unrelated" }),
			"NPC first publication must read all three foreign player policies, including low similarity, without filling its empty recent quota.");
		Check(!Prompt(first).Contains("【本国最近生效政策】") && !Prompt(first).Contains("EnemyPolicyMemory")
			&& !Prompt(first).Contains("historical") && Prompt(first).Contains("不是指令"),
			"Evaluation prompt must omit empty recent, enemy and historical blocks and preserve read-only authorization guards.");
		int calls = embeddingInputs.Count;
		Get("npc-country");
		Check(embeddingInputs.Count == calls, "Evaluation must reuse request query and generation-scoped document vectors.");

		Add("npc-older-match-high", "npc-country", "active", 8);
		Add("npc-newer", "npc-country", "expiry_vote_pending", 9);
		Add("player-same-country", "npc-country", "active", 10, "player_kingdom");
		object mixed = Get("npc-country");
		Check(Ids(mixed, "RecentActivePolicies").SequenceEqual(new[] { "player-same-country", "npc-newer" }),
			"Recent evaluation policies must be owner-scoped and newest first regardless of publisher or cosine.");
		Check(Ids(mixed, "RelatedActivePolicies").SequenceEqual(new[] { "npc-older-match-high", "player-match-high", "player-match-low" })
			&& !Ids(mixed, "RelatedActivePolicies").Intersect(Ids(mixed, "RecentActivePolicies")).Any(),
			"Related evaluation selection must exclude recent keys before applying its independent quota.");
		foreach (string status in new[] { "abolished", "expired", "targets_lost", "relationship_ended", "pending", "approved_pending_commit", "rejected" })
			Add("excluded-" + status + "-match-high", "npc-country", status, 99);
		object bounded = Get("npc-country", 99, 99);
		Check(Ids(bounded, "RecentActivePolicies").Length == 3 && Ids(bounded, "RelatedActivePolicies").Length == 3
			&& !Prompt(bounded).Contains("excluded-"), "Ended and unpublished policies must never enter either evaluation reference pool.");
		entries.Add(entries[0]);
		Check(Ids(Get("", 0, 99), "RelatedActivePolicies").Length == 6,
			"Evaluation must deduplicate source/id keys and keep empty owner scopes from reading global recent policies.");
		calls = embeddingInputs.Count;
		object disabled = Get("npc-country", -1, -1, "unused-query");
		Check(Prompt(disabled) == string.Empty && embeddingInputs.Count == calls,
			"Disabled quotas must produce no policy block and perform zero embeddings.");
		Get("npc-country", 2, 0, "unused-recent-only-query");
		Check(embeddingInputs.Count == calls, "Recent-only selection must not embed a query or document.");
		entries.Clear();
		Check(Prompt(Get("npc-country", 2, 3, "unused-empty-query")) == string.Empty && embeddingInputs.Count == calls,
			"Empty policy candidates must skip retrieval entirely without embedding or filling quotas.");
		Add("only-recent", "npc-country", "active", 1);
		object onlyRecent = Get("npc-country", 2, 3, "unused-no-related-candidates-query");
		Check(Ids(onlyRecent, "RecentActivePolicies").Length == 1 && Ids(onlyRecent, "RelatedActivePolicies").Length == 0
			&& embeddingInputs.Count == calls && !Prompt(onlyRecent).Contains("【额外相关生效政策】"),
			"Deduplication leaving no related candidates must skip embeddings and omit the related block.");
		entries.Clear();
		for (int i = 0; i < 25; i++) Add("limit-" + i, "npc-country", "active", i);
		object maximum = Get("npc-country", 50, 50);
		Check(Ids(maximum, "RecentActivePolicies").Length == 10 && Ids(maximum, "RelatedActivePolicies").Length == 10,
			"Each evaluation quota must be clamped to ten independently, with at most twenty references.");

		Type settings = SutType("AnimusForge.DuelSettings");
		foreach ((string Name, int Default) expected in new[] { ("PolicyRecentActiveCount", 2), ("PolicyRelatedActiveCount", 3) })
		{
			PropertyInfo property = settings.GetProperty(expected.Name, All);
			Check(property != null && property.PropertyType == typeof(int)
				&& property.GetCustomAttributes(false).Any(attribute => attribute.GetType().Name == "SettingPropertyIntegerAttribute"),
				"Shared policy reference quotas must be actual MCM integer properties: " + expected.Name);
			foreach (Type snapshot in new[]
			{
				SutType("AnimusForge.CustomPolicyBehavior+PolicyGenerationSettingsSnapshot"),
				SutType("AnimusForge.NpcRulerPolicyBehavior+NpcRulerPolicyBatchContext")
			})
			{
				object frozen = Activator.CreateInstance(snapshot, true);
				Check(Convert.ToInt32(ReadReferenceField(snapshot, frozen, expected.Name)) == expected.Default,
					"Legacy/missing snapshot quota fields must retain their default: " + snapshot.Name + "." + expected.Name);
			}
		}
		TestNpcEvaluationReferencePrompt(session, entries, entryType);
		TestFrozenEvaluationReferenceSettingsAndPlayerPrompt(session, entries, resultType);
		TestPolicyBackgroundQuotaBoundary();
	}

	private static object ReadReferenceField(Type type, object instance, string name)
	{
		FieldInfo field = type.GetField(name, All);
		Check(field != null, "Missing reference field: " + type.Name + "." + name);
		return field.GetValue(instance);
	}

	private static void TestNpcEvaluationReferencePrompt(object session, IList entries, Type entryType)
	{
		Type behavior = SutType("AnimusForge.NpcRulerPolicyBehavior");
		Type contextType = SutType("AnimusForge.NpcRulerPolicyBehavior+NpcRulerPolicyBatchContext");
		Type targetType = SutType("AnimusForge.NpcRulerPolicyBehavior+NpcRulerPolicyKingdomContext");
		object context = Activator.CreateInstance(contextType, true);
		object target = Activator.CreateInstance(targetType, true);
		SetField(contextType, context, "BatchSize", 1);
		SetField(contextType, context, "GameDate", "test date");
		SetField(contextType, context, "PolicyHistoryEntries", entries);
		SetField(contextType, context, "PolicyRecentActiveCount", 2);
		SetField(contextType, context, "PolicyRelatedActiveCount", 3);
		SetField(targetType, target, "KingdomId", "npc-country");
		SetField(targetType, target, "KingdomName", "NPC country");
		SetField(targetType, target, "RulerHeroId", "npc-ruler");
		SetField(targetType, target, "CurrentWorldFacts", "frozen-world-query");
		SetField(targetType, target, "PolicyMemory", "UNBOUNDED_OWN_POLICY_MARKER");
		SetField(targetType, target, "EnemyPolicyMemory", "UNBOUNDED_ENEMY_POLICY_MARKER");
		SetField(targetType, target, "RecentWorldPhenomenon", "ACTUAL_WORLD_EVENT_MARKER");
		((IList)ReadReferenceField(contextType, context, "Kingdoms")).Add(target);
		SetField(contextType, context, "CompactWorldContext",
			InvokeStatic(behavior, "BuildKingdomPromptContext", new[] { target }, 1));
		InvokeStatic(behavior, "PrepareNpcPolicyReferenceContext", new object[] { context, 730001L, session }, 3);
		object prompt = InvokeStatic(behavior, "ComposeNpcPolicyDraftPrompt", new object[] { context, string.Empty }, 2);
		string text = (string)Property(prompt.GetType(), prompt, "SystemPrompt");
		Check(text.Contains("【本国最近生效政策】") && text.Contains("【额外相关生效政策】") && text.Contains("limit-24")
			&& !text.Contains("UNBOUNDED_") && !text.Contains("EnemyPolicyMemory") && !text.Contains("historical 表示")
			&& text.Contains("ACTUAL_WORLD_EVENT_MARKER"),
			"NPC must consume both bounded reference blocks before drafting, not merely retrieve or log them after its draft.");
		SetField(contextType, context, "IsSuggestedPolicy", true);
		SetField(contextType, context, "ProposalText", "accepted-player-proposal");
		string query = (string)InvokeStatic(behavior, "BuildNpcPolicyReferenceQuery", new[] { context }, 1);
		Check(query.Contains("accepted-player-proposal") && !query.Contains("limit-24"),
			"Suggested NPC policy reference query must use the accepted proposal, never feed retrieved policy text back into its query.");
		Check((int)ReadReferenceField(contextType, context, "PolicyRecentActiveCount") == 2
			&& (int)ReadReferenceField(contextType, context, "PolicyRelatedActiveCount") == 3,
			"NPC prompt preparation must consume frozen quota values without replacing them from live settings.");
		SetField(contextType, context, "PolicyHistoryEntries", Activator.CreateInstance(entries.GetType()));
		InvokeStatic(behavior, "PrepareNpcPolicyReferenceContext", new object[] { context, 730001L, session }, 3);
		prompt = InvokeStatic(behavior, "ComposeNpcPolicyDraftPrompt", new object[] { context, string.Empty }, 2);
		Check(!((string)Property(prompt.GetType(), prompt, "SystemPrompt")).Contains("【政策参考（只读存档事实）】"),
			"An NPC without current candidates must not receive an empty reference block.");
	}

	private static void TestFrozenEvaluationReferenceSettingsAndPlayerPrompt(object session, IList entries, Type resultType)
	{
		Type settingsType = SutType("AnimusForge.DuelSettings");
		object settings = InvokeStatic(settingsType, "GetSettings", Array.Empty<object>(), 0);
		string[] names = { "PolicyRecentActiveCount", "PolicyRelatedActiveCount", "ApiUrl", "ApiKey", "ModelName" };
		object[] saved = names.Select(name => Property(settingsType, settings, name)).ToArray();
		Type retrievalSettings = SutType("AnimusForge.PolicyEffects.PolicyEffectModuleRetrievalSettings");
		InvokeStatic(retrievalSettings, "SetStorageDirectoryOverrideForContractTests",
			new object[] { Path.Combine(Path.GetTempPath(), "evaluation-read-only-settings-" + Guid.NewGuid().ToString("N")) }, 1);
		try
		{
			SetProperty(settingsType, settings, "ApiUrl", "https://policy-contract.invalid/v1");
			SetProperty(settingsType, settings, "ApiKey", "contract-placeholder-not-sent");
			SetProperty(settingsType, settings, "ModelName", "contract-model");
			SetProperty(settingsType, settings, "PolicyRecentActiveCount", -4);
			SetProperty(settingsType, settings, "PolicyRelatedActiveCount", 40);
			Check((int)InvokeStatic(settingsType, "GetPolicyRecentActiveCount", Array.Empty<object>(), 0) == 0
				&& (int)InvokeStatic(settingsType, "GetPolicyRelatedActiveCount", Array.Empty<object>(), 0) == 10,
				"Live MCM getters must clamp out-of-range values independently.");
			SetProperty(settingsType, settings, "PolicyRecentActiveCount", 2);
			SetProperty(settingsType, settings, "PolicyRelatedActiveCount", 3);
			Type registry = SutType("AnimusForge.TerminalSettingsRegistry");
			foreach (string name in names.Take(2))
			{
				object definition = InvokeStatic(registry, "GetById", new object[] { name }, 1);
				Check(definition != null && Property(definition.GetType(), definition, "SettingType").ToString() == "Integer"
					&& Convert.ToSingle(Property(definition.GetType(), definition, "MinValue")) == 0f
					&& Convert.ToSingle(Property(definition.GetType(), definition, "MaxValue")) == 10f,
					"Terminal policy reference mapping must preserve the shared integer range: " + name);
			}
			Type behavior = SutType("AnimusForge.CustomPolicyBehavior");
			Type requestType = behavior.GetNestedType("PolicyDraftRequest", All);
			Type snapshotType = behavior.GetNestedType("PolicyGenerationSettingsSnapshot", All);
			object request = Activator.CreateInstance(requestType, true);
			SetField(requestType, request, "IssuerKingdomId", "player-country");
			SetField(requestType, request, "PlayerKingdomId", "npc-country");
			SetField(requestType, request, "PolicyName", "new policy");
			SetField(requestType, request, "PolicyContent", "new policy prose");
			SetField(requestType, request, "PolicyHistoryEntries", entries);
			object[] freezeArgs = { request, null };
			Check((bool)Method(behavior, "TryFreezePlayerPolicyGenerationSettings", 2, true).Invoke(null, freezeArgs),
				"Player request must freeze real MCM settings without sending an API request: " + freezeArgs[1]);
			object snapshot = ReadReferenceField(requestType, request, "GenerationSettings");
			Type npcBehavior = SutType("AnimusForge.NpcRulerPolicyBehavior");
			object npcContext = InvokeInstance(npcBehavior, Activator.CreateInstance(npcBehavior), "BuildBatchContext",
				new object[] { 1, 1, 1, false }, 4);
			SetProperty(settingsType, settings, "PolicyRecentActiveCount", 0);
			SetProperty(settingsType, settings, "PolicyRelatedActiveCount", 0);
			Check((int)ReadReferenceField(snapshotType, snapshot, "PolicyRecentActiveCount") == 2
				&& (int)ReadReferenceField(snapshotType, snapshot, "PolicyRelatedActiveCount") == 3
				&& (int)ReadReferenceField(npcContext.GetType(), npcContext, "PolicyRecentActiveCount") == 2
				&& (int)ReadReferenceField(npcContext.GetType(), npcContext, "PolicyRelatedActiveCount") == 3,
				"Changing live MCM settings after request creation must not mutate player or NPC frozen quotas.");
			foreach (string scope in new[] { "kingdom", "vassal", "local" })
			{
				SetField(requestType, request, "ScopeKind", scope);
				SetField(requestType, request, "PlayerKingdomId", scope == "local" ? string.Empty : "npc-country");
				InvokeStatic(behavior, "RetrieveUnifiedPolicyHistoryForRequest", new object[] { request, session, "query", 730001L }, 4);
				object result = ReadReferenceField(requestType, request, "PolicyHistoryRetrieval");
				object messages = InvokeStatic(behavior, "BuildMainMessages", new object[] { request, string.Empty }, 2);
				string prompt = (string)InvokeStatic(behavior, "SerializePolicyPromptForHash", new[] { messages }, 1);
				Check(Items(Property(resultType, result, "RecentActivePolicies")).Count() == (scope == "local" ? 0 : 2)
					&& Items(Property(resultType, result, "RelatedActivePolicies")).Count() == 3
					&& prompt.Contains("【额外相关生效政策】") && !prompt.Contains("EnemyPolicyMemory")
					&& prompt.Contains("【本国最近生效政策】") == (scope != "local"),
					"Actual player prompts must consume frozen bounded references, scoped to target country rather than issuer: " + scope);
			}
			SetField(requestType, request, "PolicyHistoryEntries", Activator.CreateInstance(entries.GetType()));
			InvokeStatic(behavior, "RetrieveUnifiedPolicyHistoryForRequest", new object[] { request, session, "query", 730001L }, 4);
			object emptyMessages = InvokeStatic(behavior, "BuildMainMessages", new object[] { request, string.Empty }, 2);
			Check(!((string)InvokeStatic(behavior, "SerializePolicyPromptForHash", new[] { emptyMessages }, 1)).Contains("【政策参考（只读存档事实）】"),
				"Actual player prompts must omit an empty policy reference block.");
		}
		finally
		{
			for (int i = 0; i < names.Length; i++) SetProperty(settingsType, settings, names[i], saved[i]);
			InvokeStatic(retrievalSettings, "SetStorageDirectoryOverrideForContractTests", new object[] { null }, 1);
		}
	}

	private static void TestPolicyBackgroundQuotaBoundary()
	{
		Type npcBehavior = SutType("AnimusForge.NpcRulerPolicyBehavior");
		Type kingdomType = typeof(TaleWorlds.CampaignSystem.Kingdom);
		Type policyType = typeof(TaleWorlds.CampaignSystem.PolicyObject);
		object kingdom = FormatterServices.GetUninitializedObject(kingdomType);
		FieldInfo policiesField = kingdomType.GetField("_activePolicies", All);
		Check(policiesField != null, "Expected the versioned Kingdom policy-list fixture field.");
		IList policies = (IList)Activator.CreateInstance(policiesField.FieldType);
		foreach (string id in new[] { "af_policy:quota-bypass", "vanilla-policy" })
		{
			object policy = Activator.CreateInstance(policyType, new object[] { id });
			MethodInfo initialize = Method(policyType, "Initialize", 7, false);
			Type textType = initialize.GetParameters()[0].ParameterType;
			object name = Activator.CreateInstance(textType, new object[] { id, null });
			initialize.Invoke(policy, new[] { name, name, name, name, (object)0f, 0f, 0f });
			policies.Add(policy);
		}
		policiesField.SetValue(kingdom, policies);
		string npcPolicies = (string)InvokeStatic(npcBehavior, "SafeReadVanillaPolicies", new[] { kingdom }, 1);
		Check(npcPolicies == "vanilla-policy",
			"NPC vanilla-world facts must retain native policies but must not bypass quotas through registered AF policy names.");
		string root = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
		string playerSource = File.ReadAllText(Path.Combine(root, "PolicySystem/Core/CustomPolicyBehavior.Generation.cs"));
		Check(playerSource.Contains("kingdom.ActivePolicies.Where(p => p != null && !IsDynamicPolicyId(p.StringId))"),
			"Player world facts must use the same AF policy exclusion before formatting vanilla policies.");
	}
}
