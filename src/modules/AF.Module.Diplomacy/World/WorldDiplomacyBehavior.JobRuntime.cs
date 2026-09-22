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

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior : CampaignBehaviorBase
{
	private void TryStartNextLlmJob()
	{
		if (!IsWorldDiplomacyEnabled() || _llmRequestLease.IsRunning || _storage.Jobs.Count == 0)
		{
			return;
		}
		int hour = CurrentHour();
		if (_storage.ServiceCooldownUntilHour > hour)
		{
			return;
		}
		string selectedJobId = WorldDiplomacyJobRuntimeCoordinator.SelectNextJobId(
			_storage.Jobs.Where(x => x != null).Select(x => new WorldDiplomacyJobQueueItem
			{
				JobId = x.JobId,
				Priority = x.Priority,
				CreatedDay = x.CreatedDay,
				CacheAffinityKey = ResolveCacheAffinityKey(x),
				IsRunning = x.IsRunning,
				AwaitingHistoryCompression = x.AwaitingHistoryCompression
			}),
			hour >= _storage.CompressionRetryAfterHour,
			_lastLlmCacheAffinityKey);
		WorldDiplomacyJob job = _storage.Jobs.FirstOrDefault(x => x != null
			&& string.Equals(x.JobId, selectedJobId, StringComparison.OrdinalIgnoreCase));
		if (job == null)
		{
			return;
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& HasStaleDiplomaticThreatPresentation(job))
		{
			if (!RefreshDiplomaticThreatPresentationAndPrompt(job))
			{
				CommitFailedJob(job, "stale diplomatic threat presentation could not be rebuilt");
				return;
			}
			Log("refreshed queued generation for current diplomatic threat stage job=" + job.JobId
				+ " author=" + job.AuthorKingdomId);
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& HasStaleDiplomaticActionPresentation(job))
		{
			if (!RefreshDiplomaticActionPresentationAndPrompt(job))
			{
				CommitFailedJob(job, "stale diplomatic action list could not be rebuilt");
				return;
			}
			Log("refreshed queued generation for current legal diplomatic actions job=" + job.JobId
				+ " author=" + job.AuthorKingdomId);
		}
		if (!EnsureCurrentCanonicalPromptContractBeforeSend(job))
		{
			return;
		}
		if (job.LlmMessages?.Count > 0 && !IsValidSemanticRepairMessageChain(job))
		{
			Log("retired invalid persisted LLM message chain job=" + (job.JobId ?? "") + " kind=" + (job.Kind ?? ""));
			job.LlmMessages.Clear();
			job.SemanticRepairAttempts = 0;
			if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
				&& !TryRebuildPendingWorldDiplomacyJob(job))
			{
				CommitFailedJob(job, "invalid persisted LLM message chain could not be rebuilt");
				return;
			}
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& !CanAiAuthorDiplomaticDocument(ResolveKingdom(job.AuthorKingdomId), out string authorBlockReason))
		{
			Log("queued generation cancelled before request job=" + job.JobId + " author=" + (job.AuthorKingdomId ?? "")
				+ " reason=" + authorBlockReason);
			AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), authorBlockReason);
			RemoveJob(job.JobId);
			return;
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& !EnsureGenerationJobHasKingdomStrategicProfile(job))
		{
			AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), "missing_kingdom_strategic_profile");
			RemoveJob(job.JobId);
			return;
		}
		if (string.IsNullOrWhiteSpace(job.SystemPrompt))
		{
			CommitFailedJob(job, "empty prompt");
			return;
		}
		if (!WorldDiplomacyLlmClient.IsConfigured(out string configError))
		{
			CommitFailedJob(job, "api not configured: " + configError);
			return;
		}
		if (!TryConsumeDiplomacyLlmRequestBudget(consume: false)) return;
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase))
		{
			if (!EnsureGenerationJobHasKingdomStrategicProfile(job))
			{
				AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), "missing_kingdom_strategic_profile");
				RemoveJob(job.JobId);
				return;
			}
			// Ordinary queued generations consume the newest committed archive at actual send time.
			// Semantic repairs carry explicit messages and intentionally retain their rejected
			// request's frozen prefix.
			if (job.LlmMessages == null || job.LlmMessages.Count == 0)
			{
				CaptureCanonicalHistoryForJob(job, syncSources: true);
			}
		}
		JArray requestMessages = BuildLlmMessageArray(job);
		if (!EnsureRequestFitsInputBudget(job, requestMessages)) return;
		if (!TryConsumeDiplomacyLlmRequestBudget()) return;
		long generation = _runtimeGeneration;
		int requestTimeoutMilliseconds = string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase)
			? DuelSettings.LlmRequestTimeoutMilliseconds
			: DefaultApiTimeoutMilliseconds;
		if (!_llmRequestLease.TryClaim(job.JobId, generation, job.MaxTokens, requestTimeoutMilliseconds, out WorldDiplomacyRequestSnapshot request))
		{
			Log("world diplomacy request claim rejected job=" + (job.JobId ?? "") + " generation=" + generation);
			return;
		}
		job.IsRunning = true;
		job.CacheAffinityKey = ResolveCacheAffinityKey(job);
		_lastLlmCacheAffinityKey = job.CacheAffinityKey;
		LogPromptCacheShape(job);
		_ = Task.Run(async delegate
		{
			LlmJobResult result = new LlmJobResult
			{
				JobId = request.JobId,
				RuntimeGeneration = request.RuntimeGeneration
			};
			try
			{
				PromptPackage sharedPrompt = LegacyWorldDiplomacyLlmGateway.BuildPromptPackage(
					requestMessages,
					request.MaxTokens,
					"world-diplomacy");
				TraceContext trace = new TraceContext(
					"world-diplomacy-" + request.JobId,
					request.RuntimeGeneration,
					0,
					"single-player",
					"shared");
				LlmGenerateResult generated = await new LegacyWorldDiplomacyLlmGateway().GenerateAsync(
					new LlmGenerateRequest(
						trace,
						new LlmProviderSnapshot("world-diplomacy", "legacy://world-diplomacy", "world-diplomacy", request.TimeoutMilliseconds, request.MaxTokens),
						sharedPrompt,
						InteractionStage.MainReply),
					CancellationToken.None).ConfigureAwait(false);
				LlmGenerateMetadata metadata = generated.Metadata ?? LlmGenerateMetadata.Empty;
				result.Success = generated.Status == LlmResultStatus.Succeeded;
				result.Content = generated.RawText ?? "";
				result.Error = generated.Status == LlmResultStatus.Succeeded ? "" : (generated.ErrorCode ?? "world_diplomacy_gateway_failure");
				result.IsServiceFailure = metadata.IsTimeout || metadata.IsRateLimit || metadata.IsQuotaLimit || metadata.IsAuthFailure || generated.Status != LlmResultStatus.Succeeded;
				result.IsOutputTruncated = metadata.IsOutputTruncated;
				result.PromptTokens = generated.PromptTokens;
				result.CompletionTokens = generated.CompletionTokens;
				result.PromptCacheHitTokens = metadata.PromptCacheHitTokens;
				result.PromptCacheMissTokens = metadata.PromptCacheMissTokens;
				result.PromptCacheCreationTokens = metadata.PromptCacheCreationTokens;
				result.PromptUncachedTokens = metadata.PromptUncachedTokens;
			}
			catch (Exception ex)
			{
				result.Error = ex.ToString();
				result.IsServiceFailure = true;
			}
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
			WorldDiplomacyJob job = _storage.Jobs.FirstOrDefault(x => x != null && string.Equals(x.JobId, result.JobId, StringComparison.OrdinalIgnoreCase));
			if (job == null)
			{
				continue;
			}
			job.IsRunning = false;
			WorldDiplomacyJobRoute route = WorldDiplomacyJobRuntimeCoordinator.Classify(job.Kind);
			LogPromptCacheUsage(job, result);
			if (result.Success
				&& route == WorldDiplomacyJobRoute.Generate
				&& HasStaleDiplomaticThreatPresentation(job))
			{
				if (!RefreshDiplomaticThreatPresentationAndPrompt(job))
				{
					CommitFailedJob(job, "completed generation used a stale diplomatic threat stage and could not be rebuilt");
				}
				else
				{
					Log("discarded completed generation from stale diplomatic threat stage and rebuilt job=" + job.JobId
						+ " author=" + job.AuthorKingdomId);
				}
				continue;
			}
			if (result.Success
				&& route == WorldDiplomacyJobRoute.Generate
				&& HasStaleDiplomaticActionPresentation(job))
			{
				if (!RefreshDiplomaticActionPresentationAndPrompt(job))
				{
					CommitFailedJob(job, "completed generation used a stale diplomatic action list and could not be rebuilt");
				}
				else
				{
					Log("discarded completed generation from stale diplomatic action list and rebuilt job=" + job.JobId
						+ " author=" + job.AuthorKingdomId);
				}
				continue;
			}
			if (!result.Success)
			{
				if (route == WorldDiplomacyJobRoute.Generate
					&& result.IsOutputTruncated
					&& !string.IsNullOrWhiteSpace(result.Content))
				{
					_storage.ConsecutiveServiceFailures = 0;
					try
					{
						RejectGeneratedDraftBeforePublication(
							job,
							result.Content,
							ResolveKingdom(job.AuthorKingdomId),
							ResolveKingdom(job.TargetKingdomId),
							"output_truncated",
							null);
						RemoveJob(job.JobId);
					}
					catch (Exception ex)
					{
						CommitFailedJob(job, "truncated generated draft handling failed: " + ex.Message);
					}
					continue;
				}
				if (result.IsServiceFailure)
				{
					_storage.ConsecutiveServiceFailures++;
					if (_storage.ConsecutiveServiceFailures >= 2)
					{
						_storage.ServiceCooldownUntilHour = CurrentHour() + FailedServiceCooldownHours;
						_storage.ConsecutiveServiceFailures = 0;
					}
				}
				CommitFailedJob(job, result.Error);
				continue;
			}
			_storage.ConsecutiveServiceFailures = 0;
			try
			{
				switch (route)
				{
					case WorldDiplomacyJobRoute.Generate:
						CommitGeneratedDocument(job, result.Content);
						break;
					case WorldDiplomacyJobRoute.Analyze:
						CommitAnalysis(job, result.Content);
						break;
					case WorldDiplomacyJobRoute.Compress:
						CommitCompression(job, result.Content);
						break;
					case WorldDiplomacyJobRoute.RoundPlan:
						CommitRoundPlan(job, result.Content);
						break;
					case WorldDiplomacyJobRoute.RoundCompress:
						CommitRoundCompression(job, result.Content);
						break;
					default:
						CommitFailedJob(job, "unknown job kind");
						continue;
				}
				RemoveJob(job.JobId);
			}
			catch (Exception ex)
			{
				CommitFailedJob(job, ex.Message);
			}
		}
	}
}
