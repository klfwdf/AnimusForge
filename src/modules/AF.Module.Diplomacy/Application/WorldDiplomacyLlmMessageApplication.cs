using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyPromptContractRules;
namespace AnimusForge;

// Resolve history lazily at the use-case boundary; semantic repair keeps its frozen chain.
internal static class WorldDiplomacyLlmMessageApplication
{
public static List<WorldDiplomacyLlmMessage> BuildLlmMessagesForJob(WorldDiplomacyJob job,
		Func<long, string> buildCanonicalHistoryBlock)
	{
		if (IsValidSemanticRepairMessageChain(job)) return job.LlmMessages;
		List<WorldDiplomacyLlmMessage> source = new List<WorldDiplomacyLlmMessage>
		{
			new WorldDiplomacyLlmMessage { Role = "system", Content = job?.SystemPrompt ?? "" }
		};
		if (WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(job) && string.IsNullOrEmpty(job.CompressionFragmentId))
		{
			source.Add(new WorldDiplomacyLlmMessage
			{
				Role = "system",
				Content = WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")
					? job.DeclarationHistoryBlock ?? WorldDiplomacyRequestHistoryApplication.ContextMarker
					: buildCanonicalHistoryBlock(job?.HistoryThroughSequence ?? long.MaxValue)
			});
		}
		source.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = job?.UserPrompt ?? "" });
		return source;
	}

public static JArray BuildLlmMessageArray(WorldDiplomacyJob job,
		Func<long, string> buildCanonicalHistoryBlock, Action<string> log = null)
	{
		List<WorldDiplomacyLlmMessage> source = BuildLlmMessagesForJob(job, buildCanonicalHistoryBlock);
		JArray messages = new JArray();
		foreach (WorldDiplomacyLlmMessage message in source.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Role)))
		{
			messages.Add(new JObject
			{
				["role"] = message.Role,
				["content"] = message.Content ?? ""
			});
		}
		if (!IsValidSemanticRepairMessageChain(job))
		{
			var user = messages.OfType<JObject>().LastOrDefault(x => (string)x["role"] == "user");
			var system = messages.OfType<JObject>().FirstOrDefault(x => (string)x["role"] == "system");
			if (user != null && system != null)
			{
				string original = (string)user["content"];
				string projected = DiplomacyRequestProjection.Project(job?.Kind, original, out bool world);
				string contract = world ? DiplomacyPromptTexts.Get("RequestWorldProjection", "") : "";
				if ((!world || !string.IsNullOrWhiteSpace(contract)) && projected.Length + contract.Length + 64 < original.Length)
				{
					user["content"] = projected;
					if (world) system["content"] = (string)system["content"] + "\n" + contract;
					log?.Invoke("diplomacy.context_projection job=" + job?.JobId + " kind=" + job?.Kind
						+ " beforeChars=" + original.Length + " afterChars=" + (projected.Length + contract.Length + 1)
						+ " world=" + world);
				}
			}
		}
		return messages;
	}
}
