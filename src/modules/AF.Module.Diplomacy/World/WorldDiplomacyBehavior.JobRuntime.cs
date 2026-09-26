using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior : CampaignBehaviorBase
{
	private void TryStartNextLlmJob()
	{
		if (!IsWorldDiplomacyEnabled() || _llmRequestLease.IsRunning || _storage.Jobs.Count == 0)
		{
			return;
		}
		WorldDiplomacyJob job = WorldDiplomacyRoundLifecycleRules.SelectAndPrepareLlmJob(
			_storage,
			CurrentHour(),
			_lastLlmCacheAffinityKey,
			HasStaleDiplomaticThreatPresentation,
			RefreshDiplomaticThreatPresentationAndPrompt,
			j => WorldDiplomacyRoundLifecycleRules.HasStaleDiplomaticActionPresentation(j, BuildGenerationLegalActionSignature),
			RefreshDiplomaticActionPresentationAndPrompt,
			TryRebuildPendingWorldDiplomacyJob,
			j => { string reason; return CanAiAuthorDiplomaticDocument(ResolveKingdom(j.AuthorKingdomId), out reason) ? null : reason; },
			(j, reason) => AbandonRejectedGeneration(j, ResolveKingdom(j.AuthorKingdomId), ResolveKingdom(j.TargetKingdomId), reason),
			EnsureGenerationJobHasKingdomStrategicProfile,
			() => { string configError; return WorldDiplomacyLlmClient.IsConfigured(out configError) ? null : configError; },
			consume => TryConsumeDiplomacyLlmRequestBudget(consume),
			j => CaptureCanonicalHistoryForJob(j, syncSources: true),
			j => WorldDiplomacyPromptContractRules.BuildLlmMessageArray(j, BuildCanonicalHistoryBlock),
			out JArray requestMessages,
			EnsureRequestFitsInputBudget,
			CommitFailedJob,
			RemoveJob,
			Log);
		if (job == null)
		{
			return;
		}
		long generation = _runtimeGeneration;
		int requestTimeoutMilliseconds = WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress")
			? DuelSettings.LlmRequestTimeoutMilliseconds
			: DefaultApiTimeoutMilliseconds;
		if (!_llmRequestLease.TryClaim(job.JobId, generation, job.MaxTokens, requestTimeoutMilliseconds, out WorldDiplomacyRequestSnapshot request))
		{
			job.IsRunning = false;
			Log("world diplomacy request claim rejected job=" + (job.JobId ?? "") + " generation=" + generation);
			return;
		}
		job.IsRunning = true;
		job.CacheAffinityKey = WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(job);
		_lastLlmCacheAffinityKey = job.CacheAffinityKey;
		LogPromptCacheShape(job);
		LlmGenerateRequest detachedRequest = WorldDiplomacyLlmApplication.PrepareRequest(
			request, requestMessages);
		ILlmGateway gateway = new LegacyWorldDiplomacyLlmGateway();
		_ = Task.Run(async delegate
		{
			LlmJobResult result = await WorldDiplomacyLlmApplication.ExecuteAsync(
				request.JobId, detachedRequest, gateway, CancellationToken.None).ConfigureAwait(false);
			_completedJobs.Enqueue(result);
		});
	}

	private void ProcessCompletedJobs()
	{
		while (_completedJobs.TryDequeue(out LlmJobResult result))
		{
			_llmRequestLease.TryRelease(result?.JobId, result?.RuntimeGeneration ?? 0L);
			// A completion from a previous save/runtime may share the same persisted
			// JobId with a rebuilt request. It must not inspect, mutate or remove the
			// current runtime's job.
			bool runtimeIsStale = result != null
				&& result.RuntimeGeneration == _runtimeGeneration
				&& SaveRuntimeGuard.IsStale(result.RuntimeGeneration, "world_diplomacy_commit");
			if (!WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion(
				result?.JobId,
				result?.RuntimeGeneration ?? 0L,
				_runtimeGeneration,
				runtimeIsStale))
			{
				continue;
			}
			WorldDiplomacyJob job = _storage.Jobs.FirstOrDefault(x => WorldDiplomacyRoundLifecycleRules.HasJobId(x, result.JobId));
			if (job == null)
			{
				continue;
			}
			job.IsRunning = false;
			LogPromptCacheUsage(job, result);
			WorldDiplomacyRoundLifecycleRules.CommitCompletedLlmJobResult(
				job,
				result.Content,
				result.Success,
				result.IsServiceFailure,
				result.IsOutputTruncated,
				result.Error,
				_storage,
				CurrentHour(),
				FailedServiceCooldownHours,
				HasStaleDiplomaticThreatPresentation,
				RefreshDiplomaticThreatPresentationAndPrompt,
				j => WorldDiplomacyRoundLifecycleRules.HasStaleDiplomaticActionPresentation(j, BuildGenerationLegalActionSignature),
				RefreshDiplomaticActionPresentationAndPrompt,
				(j, content) => RejectGeneratedDraftBeforePublication(
					j, content, ResolveKingdom(j.AuthorKingdomId), ResolveKingdom(j.TargetKingdomId), "output_truncated", null),
				CommitGeneratedDocument,
				CommitAnalysis,
				CommitCompression,
				CommitRoundPlan,
				CommitRoundCompression,
				CommitFailedJob,
				RemoveJob,
				Log);
		}
	}
}
