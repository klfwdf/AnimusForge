using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private const int PlayerPolicyAutoDraftMaxTokens = 1200;

	private bool _playerPolicyAutoDraftInProgress;

	private void RequestPlayerPolicyAutoDraft(
		PlayerPolicyAutoDraftRequest input,
		Action<PlayerPolicyAutoDraftResult> onCompleted)
	{
		if (input == null)
		{
			DeliverPlayerPolicyAutoDraftResult(onCompleted, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AutoDraft_MissingDescription", "Missing policy description.")));
			return;
		}
		string normalizedDescription = NormalizePolicyContent(input.PlayerDescription);
		string normalizedPolicyName = NormalizePolicyName(input.ExistingPolicyName);
		if (!PlayerPolicyAutoDraftInputContract.HasInput(normalizedPolicyName, normalizedDescription))
		{
			DeliverPlayerPolicyAutoDraftResult(onCompleted, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AutoDraft_NeedTitleOrContent", "Please enter a policy title or content first.")));
			return;
		}
		if (_generationInProgress)
		{
			DeliverPlayerPolicyAutoDraftResult(onCompleted, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AutoDraft_EvaluatingPrevious", "The previous policy is still being evaluated; cannot use AI drafting right now.")));
			return;
		}
		if (_playerPolicyAutoDraftInProgress)
		{
			DeliverPlayerPolicyAutoDraftResult(onCompleted, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AutoDraft_AlreadyDrafting", "A draft is already being written by AI, please wait.")));
			return;
		}

		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		if (!PolicyLlmClient.TryResolvePlayerPolicyAutoDraftProfile(
			DuelSettings.GetPlayerPolicyAutoDraftApiSourceForExternal(),
			out PolicyApiExecutionProfile apiProfile,
			out string apiConfigError))
		{
			PolicySystemLog.Failure("Player", "auto-draft-profile-invalid", apiConfigError ?? string.Empty);
			DeliverPlayerPolicyAutoDraftResult(onCompleted, PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AutoDraft_ApiUnavailable", "AI service configuration is unavailable, please check model, key and connection settings.")));
			return;
		}

		if (!TryPreparePlayerPolicyAutoDraftSnapshot(
			input,
			options,
			out PolicyDraftRequest autoDraftRequest,
			out string preparationError))
		{
			DeliverPlayerPolicyAutoDraftResult(onCompleted, PlayerPolicyAutoDraftResult.Failed(preparationError));
			return;
		}

		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		_playerPolicyAutoDraftInProgress = true;
		PolicySystemLog.Write("Player", "auto-draft-start",
			"request=" + (autoDraftRequest.RequestId ?? "")
			+ " scope=" + (input.ScopeKind ?? "")
			+ " descriptionChars=" + normalizedDescription.Length.ToString(CultureInfo.InvariantCulture)
			+ " editablePromptChars=" + (input.WritingPrompt?.Length ?? 0).ToString(CultureInfo.InvariantCulture));

		Task.Run(async delegate
		{
			PlayerPolicyAutoDraftResult result;
			try
			{
				List<object> messages = PlayerPolicyAutoDraftPromptBuilder.BuildMessages(input);
				string output = await CallPlayerPolicyApiOrThrowAsync(
					messages,
					apiProfile,
					runtimeGeneration,
					CancellationToken.None,
					"PlayerPolicyAutoDraft",
					PlayerPolicyAutoDraftMaxTokens);
				if (!PlayerPolicyAutoDraftPromptBuilder.TryParseResult(output, input, out result, out string parseError))
				{
					result = PlayerPolicyAutoDraftResult.Failed(parseError);
				}
			}
			catch (Exception ex)
			{
				PolicySystemLog.Failure("Player", "auto-draft-request-failed",
					"request=" + (autoDraftRequest.RequestId ?? string.Empty), ex.ToString());
				result = PlayerPolicyAutoDraftResult.Failed(PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_AutoDraft_Legacy_Closeout_0"));
			}

			MainThreadActions.Enqueue(delegate
			{
				_playerPolicyAutoDraftInProgress = false;
				if (SaveRuntimeGuard.IsStale(runtimeGeneration, "player_policy_auto_draft_complete"))
				{
					result = PlayerPolicyAutoDraftResult.Failed(PolicyCompatibilityTexts.Get("Closeout_CustomPolicyBehavior_AutoDraft_Legacy_Closeout_1"));
				}
				PolicySystemLog.Write("Player", "auto-draft-complete",
					"request=" + (autoDraftRequest.RequestId ?? "")
					+ " scope=" + (input.ScopeKind ?? "")
					+ " success=" + (result?.Success == true ? "true" : "false")
					+ " nameChars=" + (result?.PolicyName?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " contentChars=" + (result?.PolicyContent?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
				DeliverPlayerPolicyAutoDraftResult(onCompleted, result);
			});
		});
	}

	private bool TryPreparePlayerPolicyAutoDraftSnapshot(
		PlayerPolicyAutoDraftRequest input,
		PolicyRuntimeOptions options,
		out PolicyDraftRequest autoDraftRequest,
		out string error)
	{
		autoDraftRequest = null;
		error = "";
		string scopeKind = string.Equals(input.ScopeKind, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase)
			? PolicyScopeLocal
			: input.ScopeKind;
		Kingdom playerKingdom = GetPlayerKingdom();
		List<string> selectedFiefIds = new List<string>();

		if (string.Equals(scopeKind, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase))
		{
			List<Settlement> validFiefs = ResolveOwnedLocalPolicyFiefs(NormalizeIdList(input.SelectedFiefIds));
			PolicyEligibility eligibility = EvaluateLocalPolicyEligibility(options, validFiefs.Count > 0);
			if (!eligibility.CanPublish)
			{
				error = MapPlayerPolicyEligibilityReasonToDisplay(eligibility.Reason);
				return false;
			}
			selectedFiefIds = validFiefs.Select(fief => fief.StringId ?? "").Where(id => id.Length > 0).ToList();
			input.SelectedFiefIds = selectedFiefIds.ToList();
			input.SelectedScopeSummary = "selectedFiefs=" + validFiefs.Count.ToString(CultureInfo.InvariantCulture)
				+ "; names=" + string.Join(",", validFiefs.Select(fief => fief.Name?.ToString() ?? fief.StringId ?? "").Where(name => name.Length > 0));
			input.TargetKingdomId = playerKingdom?.StringId ?? "";
			input.TargetKingdomName = playerKingdom == null ? PolicyGenerationPromptTexts.Get("Closeout_IndependentClan", "Independent clan") : GetKingdomName(playerKingdom);
		}
		else
		{
			PolicyEligibility eligibility = EvaluateEligibility(options);
			if (!eligibility.CanPublish)
			{
				error = MapPlayerPolicyEligibilityReasonToDisplay(eligibility.Reason);
				return false;
			}
			string targetId = (input.TargetKingdomId ?? "").Trim();
			bool ownKingdom = playerKingdom != null
				&& (targetId.Length == 0 || string.Equals(playerKingdom.StringId ?? "", targetId, StringComparison.OrdinalIgnoreCase));
			Kingdom targetKingdom = ownKingdom
				? playerKingdom
				: VassalageBehavior.GetPlayerDirectVassalKingdomsForExternal()
					.FirstOrDefault(kingdom => kingdom != null && string.Equals(kingdom.StringId ?? "", targetId, StringComparison.OrdinalIgnoreCase));
			bool isVassal = !ownKingdom && targetKingdom != null;
			if (targetKingdom == null || (isVassal && !IsPlayerRuler(playerKingdom)))
			{
				error = PolicyUiTexts.Get("AFPOL_UI_AutoDraftTargetUnavailable", "The selected policy target is no longer valid, or the player is no longer eligible to publish.");
				return false;
			}
			scopeKind = isVassal ? PolicyScopeVassal : PolicyScopeKingdom;
			input.TargetKingdomId = targetKingdom.StringId ?? "";
			input.TargetKingdomName = GetKingdomName(targetKingdom);
			input.SelectedScopeSummary = isVassal
				? PolicyGenerationPromptTexts.Get("AutoDraft_VassalScopeGuidance", "Issued to a direct vassal kingdom; do not rewrite into suzerain or foreign kingdom policy.")
				: PolicyGenerationPromptTexts.Get("AutoDraft_PlayerKingdomScopeGuidance", "Issued to the player kingdom; do not rewrite into local, vassal or foreign kingdom policy.");
		}

		input.ScopeKind = scopeKind;
		input.WritingPrompt = DuelSettings.GetPlayerPolicyAutoDraftPromptForExternal();
		autoDraftRequest = new PolicyDraftRequest
		{
			RequestId = "auto-draft:" + Guid.NewGuid().ToString("N"),
			ScopeKind = scopeKind,
			IssuerKingdomId = playerKingdom?.StringId ?? "",
			IssuerKingdomName = playerKingdom == null ? "" : GetKingdomName(playerKingdom),
			ProposerClanId = Clan.PlayerClan?.StringId ?? "",
			SelectedFiefIds = selectedFiefIds,
			PolicyName = NormalizePolicyName(input.ExistingPolicyName),
			PolicyContent = NormalizePolicyContent(input.PlayerDescription),
			DateText = input.DateText,
			SubmittedDay = GetCurrentCampaignDay(),
			PlayerKingdomId = input.TargetKingdomId,
			PlayerKingdomName = input.TargetKingdomName
		};
		return true;
	}

	private static void DeliverPlayerPolicyAutoDraftResult(
		Action<PlayerPolicyAutoDraftResult> callback,
		PlayerPolicyAutoDraftResult result)
	{
		try
		{
			callback?.Invoke(result ?? PlayerPolicyAutoDraftResult.Failed(PolicyUiTexts.Get("AFPOL_UI_AiDraftFailed", "AI drafting failed; the original input was kept.")));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("Player", "auto-draft-callback-failed", ex.Message);
		}
	}
}
