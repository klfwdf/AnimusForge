using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

internal readonly struct AfTributePowerContext
{
	public AfTributePowerContext(
		float scorePayer,
		float scoreReceiver,
		float receiverDecisionThreshold,
		float settlementValue,
		float payerWarProgress,
		float receiverWarProgress,
		float warProgressDifference,
		float rawTributeRatio,
		float appliedTributeRatio,
		float payerFiefProsperity,
		int calculatedTribute)
	{
		ScorePayer = scorePayer;
		ScoreReceiver = scoreReceiver;
		ReceiverDecisionThreshold = receiverDecisionThreshold;
		SettlementValue = settlementValue;
		PayerWarProgress = payerWarProgress;
		ReceiverWarProgress = receiverWarProgress;
		WarProgressDifference = warProgressDifference;
		RawTributeRatio = rawTributeRatio;
		AppliedTributeRatio = appliedTributeRatio;
		PayerFiefProsperity = payerFiefProsperity;
		CalculatedTribute = calculatedTribute;
	}

	public float ScorePayer { get; }
	public float ScoreReceiver { get; }
	public float ReceiverDecisionThreshold { get; }
	public float SettlementValue { get; }
	public float PayerWarProgress { get; }
	public float ReceiverWarProgress { get; }
	public float WarProgressDifference { get; }
	public float RawTributeRatio { get; }
	public float AppliedTributeRatio { get; }
	public float PayerFiefProsperity { get; }
	public int CalculatedTribute { get; }

	public float ScoreDelta => ScoreReceiver - ScorePayer;
}

internal sealed class NpcTributeVassalageBehavior : CampaignBehaviorBase
{
	private const string LogCategory = "NpcTributeVassalage";
	private const float MinimumStrengthRatio = 1.70f;
	private const float BaseChance = 0.08f;
	private const float MaximumChance = 0.45f;
	private const float ChanceStrengthRatioRange = 1.50f;

	public static NpcTributeVassalageBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		NpcTributeVassalageDiagnosticLog.Event("behavior.register_events", new Dictionary<string, object>
		{
			["logPath"] = NpcTributeVassalageDiagnosticLog.GetDiagnosticLogPath()
		});
		Logger.Log(LogCategory, "[Lifecycle] Registered.");
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	internal static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		harmony.CreateClassProcessor(typeof(Patch_NpcTributeVassalage_MakePeaceAction)).Patch();
		NpcTributeVassalageDiagnosticLog.Event("harmony.patch_applied", new Dictionary<string, object>
		{
			["target"] = "MakePeaceAction.ApplyInternal"
		});
	}

	internal NpcTributeVassalagePeaceSnapshot CapturePeaceSnapshot(
		IFaction faction1,
		IFaction faction2,
		int dailyTributeFrom1To2,
		int dailyTributeDuration,
		MakePeaceAction.MakePeaceDetail detail)
	{
		try
		{
			Kingdom activeKingdom = ResolveFactionKingdom(faction1);
			Kingdom opponentKingdom = ResolveFactionKingdom(faction2);
			float activeStrength = GetRefreshedKingdomStrength(activeKingdom);
			float opponentStrength = GetRefreshedKingdomStrength(opponentKingdom);
			float strengthRatio = CalculateStrengthRatio(opponentStrength, activeStrength);
			return new NpcTributeVassalagePeaceSnapshot
			{
				ActiveKingdom = activeKingdom,
				OpponentKingdom = opponentKingdom,
				DailyTributeFromActiveToOpponent = dailyTributeFrom1To2,
				DailyTributeDuration = dailyTributeDuration,
				Detail = detail,
				WasAtWar = IsAtWar(activeKingdom, opponentKingdom),
				ActiveStrength = activeStrength,
				OpponentStrength = opponentStrength,
				StrengthRatio = strengthRatio
			};
		}
		catch (Exception ex)
		{
			LogError("capture_snapshot_error", ex);
			return null;
		}
	}

	internal void HandlePeaceApplied(NpcTributeVassalagePeaceSnapshot snapshot)
	{
		if (snapshot == null)
		{
			return;
		}
		try
		{
			Kingdom activeKingdom = snapshot.ActiveKingdom;
			Kingdom opponentKingdom = snapshot.OpponentKingdom;
			if (!snapshot.WasAtWar)
			{
				LogSkip("not_at_war_before_peace_action", snapshot);
				return;
			}
			if (snapshot.Detail != MakePeaceAction.MakePeaceDetail.ByKingdomDecision)
			{
				LogSkip("not_kingdom_decision", snapshot);
				return;
			}
			if (!IsValidNpcKingdom(activeKingdom) || !IsValidNpcKingdom(opponentKingdom) || activeKingdom == opponentKingdom)
			{
				LogSkip("invalid_or_player_involved_kingdom", snapshot);
				return;
			}
			if (IsAtWar(activeKingdom, opponentKingdom))
			{
				LogSkip("peace_not_applied", snapshot);
				return;
			}
			AfTributePowerContext tributeContext = default;
			DiplomacyBehavior.TryBuildTributePowerContext(activeKingdom, opponentKingdom, out tributeContext);
			float activeStrength = snapshot.ActiveStrength;
			float opponentStrength = snapshot.OpponentStrength;
			float strengthRatio = snapshot.StrengthRatio;
			if (float.IsNaN(strengthRatio) || float.IsInfinity(strengthRatio) || strengthRatio <= 0f)
			{
				strengthRatio = CalculateStrengthRatio(opponentStrength, activeStrength);
			}
			if (!DuelSettings.IsNpcTributaryVassalageEnabled())
			{
				LogCandidate("skip", "mcm_disabled", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, 0f, 0f, "");
				return;
			}
			if (!IsWeakActivePeaceSide(activeStrength, opponentStrength, strengthRatio))
			{
				LogCandidate("skip", "not_significant_active_peace_strength_gap", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, 0f, 0f, "");
				return;
			}
			float chance = CalculateVassalageChance(strengthRatio);
			float roll = MBRandom.RandomFloat;
			LogCandidate("candidate", "eligible", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, "");
			if (roll > chance)
			{
				LogCandidate("skip", "roll_failed", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, "");
				return;
			}
			if (TryCreateNpcTributaryVassalage(
				opponentKingdom,
				activeKingdom,
				"npc_tribute_vassalage",
				out string statusText,
				out string agreementId))
			{
				Logger.Log(LogCategory, "Applied NPC tributary vassalage agreement=" + agreementId + " suzerain=" + (opponentKingdom.StringId ?? "") + " vassal=" + (activeKingdom.StringId ?? ""));
				LogCandidate("applied", "agreement_created", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, agreementId, statusText);
				return;
			}
			LogCandidate("skip", "agreement_create_rejected", snapshot, tributeContext, activeStrength, opponentStrength, strengthRatio, chance, roll, agreementId, statusText);
		}
		catch (Exception ex)
		{
			LogError("handle_peace_applied_error", ex, snapshot);
		}
	}

	internal bool TryCreateNpcTributaryVassalage(Kingdom suzerainKingdom, Kingdom vassalKingdom, string source, out string statusText, out string agreementId)
	{
		statusText = "";
		agreementId = "";
		VassalageBehavior vassalageBehavior = VassalageBehavior.Instance;
		if (vassalageBehavior == null)
		{
			statusText = "NPC 臣属条约未签署：臣属系统未初始化。";
			LogAgreementCreateReject("vassalage_behavior_missing", suzerainKingdom, vassalKingdom, source, statusText);
			return false;
		}
		if (!VassalageBehavior.IsValidKingdomForVassalage(suzerainKingdom) || !VassalageBehavior.IsValidKingdomForVassalage(vassalKingdom))
		{
			statusText = "NPC 臣属条约未签署：宗主国或臣属国无效。";
			LogAgreementCreateReject("invalid_kingdom", suzerainKingdom, vassalKingdom, source, statusText);
			return false;
		}
		if (suzerainKingdom == vassalKingdom)
		{
			statusText = "NPC 臣属条约未签署：王国不能臣服于自己。";
			LogAgreementCreateReject("same_kingdom", suzerainKingdom, vassalKingdom, source, statusText);
			return false;
		}
		Kingdom playerKingdom = vassalageBehavior.GetPlayerKingdomForVassalage();
		if (VassalageBehavior.IsValidKingdomForVassalage(playerKingdom) && (suzerainKingdom == playerKingdom || vassalKingdom == playerKingdom))
		{
			statusText = "NPC 臣属条约未签署：玩家王国参与时必须走现有谈判/标签链路。";
			LogAgreementCreateReject("player_kingdom_involved", suzerainKingdom, vassalKingdom, source, statusText, playerKingdom);
			return false;
		}
		string suzerainId = (suzerainKingdom.StringId ?? "").Trim();
		string vassalId = (vassalKingdom.StringId ?? "").Trim();
		VassalageAgreement existing = vassalageBehavior.GetAnyVassalageAgreementForBridge(vassalKingdom);
		if (existing != null)
		{
			statusText = GetKingdomDisplayName(vassalKingdom, "该王国") + "已经承认" + GetKingdomDisplayName(existing.ResolveSuzerain(), "宗主国") + "的宗主权。";
			LogAgreementCreateReject("existing_vassal_agreement", suzerainKingdom, vassalKingdom, source, statusText, playerKingdom, existing);
			return false;
		}
		VassalageAgreement reverse = vassalageBehavior.GetAnyVassalageAgreementForBridge(suzerainKingdom);
		if (reverse != null && string.Equals(reverse.SuzerainKingdomId ?? "", vassalId, StringComparison.OrdinalIgnoreCase))
		{
			statusText = "NPC 臣属条约未签署：双方已经存在反向臣属关系。";
			LogAgreementCreateReject("direct_reverse_agreement", suzerainKingdom, vassalKingdom, source, statusText, playerKingdom, reverse);
			return false;
		}
		if (vassalageBehavior.WouldCreateVassalageCycleForBridge(suzerainId, vassalId, out string cycleChain))
		{
			statusText = "NPC 臣属条约未签署：该关系会形成循环臣属链。";
			LogAgreementCreateReject("vassalage_cycle", suzerainKingdom, vassalKingdom, source, statusText, playerKingdom, null, cycleChain);
			return false;
		}
		List<Kingdom> suzerainEnemies = vassalageBehavior.GetKingdomWarEnemiesForBridge(suzerainKingdom).Where((Kingdom x) => x != vassalKingdom).ToList();
		List<Kingdom> vassalEnemies = vassalageBehavior.GetKingdomWarEnemiesForBridge(vassalKingdom).Where((Kingdom x) => x != suzerainKingdom).ToList();
		VassalageAgreement agreement = new VassalageAgreement
		{
			SuzerainKingdomId = suzerainId,
			VassalKingdomId = vassalId,
			Type = AfVassalageType.Tributary,
			CreatedDay = vassalageBehavior.GetCurrentCampaignDayForBridge(),
			NegotiatedByHeroId = source ?? "",
			EstablishedNoticeShown = true
		};
		vassalageBehavior.StoreVassalageAgreementForBridge(agreement);
		agreementId = agreement.AgreementId;
		int queuedWarSyncCount = 0;
		int syncedWarCount = SynchronizeExistingWarsForNewNpcTributaryAgreement(vassalageBehavior, agreement, suzerainKingdom, vassalKingdom, vassalEnemies, out queuedWarSyncCount);
		statusText = GetKingdomDisplayName(vassalKingdom, "该王国") + "在国力悬殊的议和后承认" + GetKingdomDisplayName(suzerainKingdom, "宗主国") + "的宗主权，条约类型：朝贡国。"
			+ ((syncedWarCount > 0 || queuedWarSyncCount > 0)
				? ("宗主国已接手朝贡国现有战事：" + syncedWarCount.ToString(CultureInfo.InvariantCulture) + "项已生效，" + queuedWarSyncCount.ToString(CultureInfo.InvariantCulture) + "项将在局势安全时生效。")
				: "");
		vassalageBehavior.QueueNpcTributaryVassalageNoticeForBridge(agreement);
		Logger.Log(LogCategory, "Agreement created suzerain=" + agreement.SuzerainKingdomId + " vassal=" + agreement.VassalKingdomId + " type=" + agreement.Type);
		NpcTributeVassalageDiagnosticLog.Event("agreement.create.success", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["agreement"] = DescribeAgreementForDiagnostics(agreement),
			["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
			["vassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(vassalKingdom),
			["source"] = source ?? "",
			["suzerainEnemyCount"] = suzerainEnemies.Count,
			["vassalEnemyCount"] = vassalEnemies.Count,
			["syncedWarCount"] = syncedWarCount,
			["queuedWarSyncCount"] = queuedWarSyncCount,
			["pendingDiplomacySyncCount"] = vassalageBehavior.PendingDiplomacySyncCountForBridge,
			["statusText"] = statusText
		});
		return true;
	}

	internal bool HandleWarDeclared(Kingdom declaringKingdom, Kingdom targetKingdom, IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail, bool canInferDeclarer)
	{
		VassalageBehavior vassalageBehavior = VassalageBehavior.Instance;
		if (vassalageBehavior == null || !canInferDeclarer || !VassalageBehavior.IsValidKingdomForVassalage(declaringKingdom) || !VassalageBehavior.IsValidKingdomForVassalage(targetKingdom) || declaringKingdom == targetKingdom)
		{
			return false;
		}
		bool handled = false;
		VassalageAgreement declaringAgreement = vassalageBehavior.GetNonPlayerTributaryAgreementForBridge(declaringKingdom);
		if (declaringAgreement != null)
		{
			Kingdom suzerain = declaringAgreement.ResolveSuzerain();
			NpcTributeVassalageDiagnosticLog.Event("war_declared.tributary_autonomous", new Dictionary<string, object>
			{
				["agreementId"] = declaringAgreement.AgreementId,
				["agreement"] = DescribeAgreementForDiagnostics(declaringAgreement),
				["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerain),
				["tributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["target"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["rawFaction1"] = vassalageBehavior.DescribeFactionForBridge(faction1, declaringKingdom),
				["rawFaction2"] = vassalageBehavior.DescribeFactionForBridge(faction2, targetKingdom),
				["detail"] = detail,
				["policy"] = "tributary_declared_war_autonomous"
			});
			handled = true;
		}
		VassalageAgreement targetAgreement = vassalageBehavior.GetNonPlayerTributaryAgreementForBridge(targetKingdom);
		if (targetAgreement == null)
		{
			return handled;
		}
		Kingdom targetSuzerain = targetAgreement.ResolveSuzerain();
		Kingdom declaringSuzerain = declaringAgreement?.ResolveSuzerain();
		if (declaringAgreement != null && VassalageBehavior.IsValidKingdomForVassalage(declaringSuzerain) && declaringSuzerain == targetSuzerain)
		{
			NpcTributeVassalageDiagnosticLog.Event("protection.skip", new Dictionary<string, object>
			{
				["reason"] = "same_suzerain_tributary_war_notice_only",
				["declaringAgreement"] = DescribeAgreementForDiagnostics(declaringAgreement),
				["targetAgreement"] = DescribeAgreementForDiagnostics(targetAgreement),
				["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(targetSuzerain),
				["declaringTributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["targetTributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["detail"] = detail,
				["policy"] = "tributary_tributary_war_autonomous"
			});
			return true;
		}
		if (!VassalageBehavior.IsValidKingdomForVassalage(targetSuzerain) || targetSuzerain == declaringKingdom)
		{
			NpcTributeVassalageDiagnosticLog.Event("protection.skip", new Dictionary<string, object>
			{
				["reason"] = "invalid_suzerain_or_suzerain_is_attacker",
				["agreementId"] = targetAgreement.AgreementId,
				["agreement"] = DescribeAgreementForDiagnostics(targetAgreement),
				["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(targetSuzerain),
				["tributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["enemy"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["detail"] = detail
			});
			return true;
		}
		if (vassalageBehavior.TryFindActiveProtectedTributaryWarForBridge(targetKingdom, declaringKingdom, out string existingProtectedKey))
		{
			NpcTributeVassalageDiagnosticLog.Event("protection.skip", new Dictionary<string, object>
			{
				["reason"] = "protected_subject_war_already_recorded",
				["protectedKey"] = existingProtectedKey ?? "",
				["agreementId"] = targetAgreement.AgreementId,
				["agreement"] = DescribeAgreementForDiagnostics(targetAgreement),
				["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(targetSuzerain),
				["tributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(targetKingdom),
				["enemy"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(declaringKingdom),
				["detail"] = detail
			});
			return true;
		}
		ApplyNpcTributaryProtectionWar(vassalageBehavior, targetAgreement, targetKingdom, declaringKingdom, "npc_tributary_protection_accepted");
		return true;
	}

	private static int SynchronizeExistingWarsForNewNpcTributaryAgreement(
		VassalageBehavior vassalageBehavior,
		VassalageAgreement agreement,
		Kingdom suzerainKingdom,
		Kingdom vassalKingdom,
		List<Kingdom> vassalEnemies,
		out int queuedWarSyncCount,
		bool forceQueue = false)
	{
		int syncedWarCount = 0;
		queuedWarSyncCount = 0;
		int attemptedWarSyncCount = 0;
		int existingSubjectConflictPeaceCount = 0;
		if (vassalageBehavior == null || agreement == null || !VassalageBehavior.IsValidKingdomForVassalage(suzerainKingdom) || !VassalageBehavior.IsValidKingdomForVassalage(vassalKingdom) || suzerainKingdom == vassalKingdom)
		{
			NpcTributeVassalageDiagnosticLog.Event("sync_wars.reject", new Dictionary<string, object>
			{
				["reason"] = "invalid_context",
				["agreement"] = DescribeAgreementForDiagnostics(agreement),
				["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
				["vassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(vassalKingdom)
			});
			return 0;
		}
		foreach (Kingdom enemy in vassalEnemies ?? new List<Kingdom>())
		{
			if (!VassalageBehavior.IsValidKingdomForVassalage(enemy) || enemy == vassalKingdom || enemy == suzerainKingdom)
			{
				continue;
			}
			VassalageAgreement enemyAgreement = vassalageBehavior.GetAnyVassalageAgreementForBridge(enemy);
			if (enemyAgreement != null && string.Equals(enemyAgreement.SuzerainKingdomId ?? "", suzerainKingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				if (vassalageBehavior.MakePeaceIfNeededForBridge(vassalKingdom, enemy, "npc_tributary_treaty_existing_subject_conflict", forceQueue))
				{
					existingSubjectConflictPeaceCount++;
				}
				continue;
			}
			string syncReason = "npc_tributary_treaty_protection_accepted";
			int pendingBefore = vassalageBehavior.PendingDiplomacySyncCountForBridge;
			bool declaredNow = vassalageBehavior.DeclareWarIfNeededForBridge(suzerainKingdom, enemy, syncReason, forceQueue);
			bool queuedOrScheduled = !declaredNow && vassalageBehavior.HasPendingDeclareWarSyncForBridge(suzerainKingdom, enemy, syncReason);
			bool protectedRecordCreated = false;
			if (vassalageBehavior.IsAtWarForBridge(vassalKingdom, enemy) && (declaredNow || queuedOrScheduled || vassalageBehavior.IsAtWarForBridge(suzerainKingdom, enemy)))
			{
				vassalageBehavior.RecordProtectedTributaryWarForBridge(agreement, vassalKingdom, enemy, syncReason);
				protectedRecordCreated = true;
			}
			if (declaredNow)
			{
				syncedWarCount++;
			}
			else if (queuedOrScheduled)
			{
				queuedWarSyncCount++;
			}
			attemptedWarSyncCount++;
			NpcTributeVassalageDiagnosticLog.Event("sync_war.attempt", new Dictionary<string, object>
			{
				["direction"] = "npc_suzerain_protects_tributary_existing_war",
				["reason"] = syncReason,
				["agreementId"] = agreement.AgreementId,
				["agreement"] = DescribeAgreementForDiagnostics(agreement),
				["declaring"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
				["target"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(enemy),
				["tributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(vassalKingdom),
				["declaredNow"] = declaredNow,
				["queuedOrScheduled"] = queuedOrScheduled,
				["tributaryAtWarAfter"] = vassalageBehavior.IsAtWarForBridge(vassalKingdom, enemy),
				["suzerainAtWarAfter"] = vassalageBehavior.IsAtWarForBridge(suzerainKingdom, enemy),
				["protectedRecordCreated"] = protectedRecordCreated,
				["pendingDiplomacySyncCountBefore"] = pendingBefore,
				["pendingDiplomacySyncCountAfter"] = vassalageBehavior.PendingDiplomacySyncCountForBridge
			});
		}
		NpcTributeVassalageDiagnosticLog.Event("sync_wars", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["agreement"] = DescribeAgreementForDiagnostics(agreement),
			["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
			["vassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(vassalKingdom),
			["vassalEnemyCount"] = vassalEnemies?.Count ?? 0,
			["syncedWarCount"] = syncedWarCount,
			["queuedWarSyncCount"] = queuedWarSyncCount,
			["scheduledWarSyncCount"] = queuedWarSyncCount,
			["totalWarSyncCount"] = syncedWarCount + queuedWarSyncCount,
			["attemptedWarSyncCount"] = attemptedWarSyncCount,
			["existingSubjectConflictPeaceCount"] = existingSubjectConflictPeaceCount,
			["forceQueue"] = forceQueue,
			["policy"] = "npc_tributary_suzerain_protects_existing_wars_only"
		});
		return syncedWarCount;
	}

	private static void ApplyNpcTributaryProtectionWar(VassalageBehavior vassalageBehavior, VassalageAgreement agreement, Kingdom tributary, Kingdom enemy, string reason)
	{
		Kingdom suzerain = agreement?.ResolveSuzerain();
		if (vassalageBehavior == null
			|| agreement == null
			|| NormalizeVassalageType(agreement.Type) != AfVassalageType.Tributary
			|| !VassalageBehavior.IsValidKingdomForVassalage(suzerain)
			|| !VassalageBehavior.IsValidKingdomForVassalage(tributary)
			|| !VassalageBehavior.IsValidKingdomForVassalage(enemy)
			|| suzerain == tributary
			|| suzerain == enemy
			|| tributary == enemy)
		{
			NpcTributeVassalageDiagnosticLog.Event("protection.reject", new Dictionary<string, object>
			{
				["reason"] = "invalid_context",
				["agreement"] = DescribeAgreementForDiagnostics(agreement),
				["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerain),
				["tributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(tributary),
				["enemy"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(enemy)
			});
			return;
		}
		string protectionReason = string.IsNullOrWhiteSpace(reason) ? "npc_tributary_protection_accepted" : reason.Trim();
		bool alreadyAtWar = vassalageBehavior.IsAtWarForBridge(suzerain, enemy);
		int pendingBefore = vassalageBehavior.PendingDiplomacySyncCountForBridge;
		bool declaredNow = false;
		bool queuedOrScheduled = false;
		if (!alreadyAtWar)
		{
			declaredNow = vassalageBehavior.DeclareWarIfNeededForBridge(suzerain, enemy, protectionReason);
			queuedOrScheduled = !declaredNow && vassalageBehavior.HasPendingDeclareWarSyncForBridge(suzerain, enemy, protectionReason);
		}
		bool protectedRecordCreated = false;
		if (vassalageBehavior.IsAtWarForBridge(tributary, enemy) && (alreadyAtWar || declaredNow || queuedOrScheduled || vassalageBehavior.IsAtWarForBridge(suzerain, enemy)))
		{
			vassalageBehavior.RecordProtectedTributaryWarForBridge(agreement, tributary, enemy, protectionReason);
			protectedRecordCreated = true;
		}
		Logger.Log(LogCategory, "Protection applied suzerain=" + (suzerain.StringId ?? "") + " tributary=" + (tributary.StringId ?? "") + " enemy=" + (enemy.StringId ?? "") + " reason=" + protectionReason);
		NpcTributeVassalageDiagnosticLog.Event("protection.apply", new Dictionary<string, object>
		{
			["agreementId"] = agreement.AgreementId,
			["agreement"] = DescribeAgreementForDiagnostics(agreement),
			["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerain),
			["tributary"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(tributary),
			["enemy"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(enemy),
			["alreadyAtWar"] = alreadyAtWar,
			["declaredNow"] = declaredNow,
			["queuedOrScheduled"] = queuedOrScheduled,
			["tributaryAtWarAfter"] = vassalageBehavior.IsAtWarForBridge(tributary, enemy),
			["suzerainAtWarAfter"] = vassalageBehavior.IsAtWarForBridge(suzerain, enemy),
			["protectedRecordCreated"] = protectedRecordCreated,
			["pendingDiplomacySyncCountBefore"] = pendingBefore,
			["pendingDiplomacySyncCountAfter"] = vassalageBehavior.PendingDiplomacySyncCountForBridge,
			["reason"] = protectionReason
		});
	}

	private static void LogAgreementCreateReject(string reason, Kingdom suzerainKingdom, Kingdom vassalKingdom, string source, string statusText, Kingdom playerKingdom = null, VassalageAgreement existingAgreement = null, string cycleChain = "")
	{
		NpcTributeVassalageDiagnosticLog.Event("agreement.create.reject", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["suzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerainKingdom),
			["vassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(vassalKingdom),
			["playerKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(playerKingdom),
			["existingAgreement"] = DescribeAgreementForDiagnostics(existingAgreement),
			["cycleChain"] = cycleChain ?? "",
			["source"] = source ?? "",
			["statusText"] = statusText ?? ""
		});
	}

	private static string DescribeAgreementForDiagnostics(VassalageAgreement agreement)
	{
		if (agreement == null)
		{
			return "null";
		}
		try
		{
			Kingdom suzerain = agreement.ResolveSuzerain();
			Kingdom vassal = agreement.ResolveVassal();
			return "agreementId=" + (agreement.AgreementId ?? "")
				+ ";type=" + agreement.Type
				+ ";suzerainId=" + (agreement.SuzerainKingdomId ?? "")
				+ ";vassalId=" + (agreement.VassalKingdomId ?? "")
				+ ";createdDay=" + agreement.CreatedDay.ToString(CultureInfo.InvariantCulture)
				+ ";negotiatedByHeroId=" + (agreement.NegotiatedByHeroId ?? "")
				+ ";suzerain=" + NpcTributeVassalageDiagnosticLog.DescribeKingdom(suzerain)
				+ ";vassal=" + NpcTributeVassalageDiagnosticLog.DescribeKingdom(vassal);
		}
		catch
		{
			return "agreementId=" + (agreement.AgreementId ?? "") + ";suzerainId=" + (agreement.SuzerainKingdomId ?? "") + ";vassalId=" + (agreement.VassalKingdomId ?? "");
		}
	}

	private static AfVassalageType NormalizeVassalageType(AfVassalageType type)
	{
		if (type == AfVassalageType.Military)
		{
			return AfVassalageType.Garrison;
		}
		if (type == AfVassalageType.Protectorate)
		{
			return AfVassalageType.Tributary;
		}
		return type;
	}

	private static string GetKingdomDisplayName(Kingdom kingdom, string fallback)
	{
		try
		{
			string text = kingdom?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return kingdom?.StringId ?? fallback ?? "未知王国";
	}

	private static bool IsWeakActivePeaceSide(
		float activeStrength,
		float opponentStrength,
		float strengthRatio)
	{
		if (opponentStrength <= 0f)
		{
			return false;
		}
		if (activeStrength <= 0f)
		{
			return opponentStrength > 0f;
		}
		if (opponentStrength <= activeStrength)
		{
			return false;
		}
		return strengthRatio >= MinimumStrengthRatio;
	}

	private static float CalculateVassalageChance(float strengthRatio)
	{
		float strengthProgress = Clamp01((strengthRatio - MinimumStrengthRatio) / ChanceStrengthRatioRange);
		return Clamp(BaseChance + strengthProgress * (MaximumChance - BaseChance), BaseChance, MaximumChance);
	}

	private static Kingdom ResolveFactionKingdom(IFaction faction)
	{
		if (faction == null)
		{
			return null;
		}
		if (faction is Kingdom kingdom)
		{
			return kingdom;
		}
		try
		{
			return faction.Leader?.Clan?.Kingdom ?? faction.MapFaction as Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsValidNpcKingdom(Kingdom kingdom)
	{
		if (!IsValidKingdom(kingdom))
		{
			return false;
		}
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
		return playerKingdom == null || kingdom != playerKingdom;
	}

	private static bool IsValidKingdom(Kingdom kingdom)
	{
		try
		{
			return kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId);
		}
		catch
		{
			return kingdom != null && !string.IsNullOrWhiteSpace(kingdom.StringId);
		}
	}

	private static bool IsAtWar(Kingdom left, Kingdom right)
	{
		try
		{
			return left != null && right != null && left != right && FactionManager.IsAtWarAgainstFaction(left, right);
		}
		catch
		{
			return false;
		}
	}

	private static float GetRefreshedKingdomStrength(Kingdom kingdom)
	{
		if (!IsValidKingdom(kingdom))
		{
			return 0f;
		}
		try
		{
			if (kingdom.Clans != null)
			{
				foreach (Clan clan in kingdom.Clans)
				{
					clan?.UpdateCurrentStrength();
				}
			}
		}
		catch
		{
		}
		float strength = 0f;
		try
		{
			strength = kingdom.CurrentTotalStrength;
		}
		catch
		{
			return 0f;
		}
		if (float.IsNaN(strength) || float.IsInfinity(strength))
		{
			return 0f;
		}
		return Math.Max(0f, strength);
	}

	private static float CalculateStrengthRatio(float stronger, float weaker)
	{
		if (stronger <= 0f && weaker <= 0f)
		{
			return 1f;
		}
		if (weaker <= 0f)
		{
			return stronger > 0f ? 99f : 1f;
		}
		return Math.Max(0f, stronger) / Math.Max(1f, weaker);
	}

	private static float Clamp01(float value)
	{
		return Clamp(value, 0f, 1f);
	}

	private static float Clamp(float value, float min, float max)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return min;
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static void LogSkip(string reason, NpcTributeVassalagePeaceSnapshot snapshot)
	{
		LogCandidate("skip", reason, snapshot, default, snapshot?.ActiveStrength ?? 0f, snapshot?.OpponentStrength ?? 0f, snapshot?.StrengthRatio ?? 0f, 0f, 0f, "");
	}

	private static void LogCandidate(
		string stage,
		string reason,
		NpcTributeVassalagePeaceSnapshot snapshot,
		AfTributePowerContext tributeContext,
		float activeStrength,
		float opponentStrength,
		float strengthRatio,
		float chance,
		float roll,
		string agreementId,
		string statusText = "")
	{
		NpcTributeVassalageDiagnosticLog.Event(stage, new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["classificationBasis"] = "MakePeaceAction.ApplyInternal faction1 proposer plus active peace kingdom strength gap",
			["activePeaceKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.ActiveKingdom),
			["opponentKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.OpponentKingdom),
			["proposedVassal"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.ActiveKingdom),
			["proposedSuzerain"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.OpponentKingdom),
			["dailyTributeFromActiveToOpponent"] = snapshot?.DailyTributeFromActiveToOpponent ?? 0,
			["dailyTributeDuration"] = snapshot?.DailyTributeDuration ?? 0,
			["detail"] = snapshot?.Detail.ToString() ?? "",
			["wasAtWar"] = snapshot?.WasAtWar ?? false,
			["scorePayer"] = tributeContext.ScorePayer,
			["scoreReceiver"] = tributeContext.ScoreReceiver,
			["scoreDelta"] = tributeContext.ScoreDelta,
			["receiverDecisionThreshold"] = tributeContext.ReceiverDecisionThreshold,
			["payerWarProgress"] = tributeContext.PayerWarProgress,
			["receiverWarProgress"] = tributeContext.ReceiverWarProgress,
			["warProgressDifference"] = tributeContext.WarProgressDifference,
			["rawTributeRatio"] = tributeContext.RawTributeRatio,
			["appliedTributeRatio"] = tributeContext.AppliedTributeRatio,
			["calculatedTribute"] = tributeContext.CalculatedTribute,
			["activeStrength"] = activeStrength,
			["opponentStrength"] = opponentStrength,
			["strengthRatio"] = strengthRatio,
			["chance"] = chance,
			["roll"] = roll,
			["agreementId"] = agreementId ?? "",
			["statusText"] = statusText ?? ""
		});
	}

	private static void LogError(string reason, Exception ex, NpcTributeVassalagePeaceSnapshot snapshot = null)
	{
		Logger.Log(LogCategory, "[ERROR] " + reason + ": " + ex);
		NpcTributeVassalageDiagnosticLog.Event("error", new Dictionary<string, object>
		{
			["reason"] = reason ?? "",
			["activePeaceKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.ActiveKingdom),
			["opponentKingdom"] = NpcTributeVassalageDiagnosticLog.DescribeKingdom(snapshot?.OpponentKingdom),
			["exception"] = ex?.ToString() ?? ""
		});
	}
}

internal sealed class NpcTributeVassalagePeaceSnapshot
{
	public Kingdom ActiveKingdom { get; set; }
	public Kingdom OpponentKingdom { get; set; }
	public int DailyTributeFromActiveToOpponent { get; set; }
	public int DailyTributeDuration { get; set; }
	public MakePeaceAction.MakePeaceDetail Detail { get; set; }
	public bool WasAtWar { get; set; }
	public float ActiveStrength { get; set; }
	public float OpponentStrength { get; set; }
	public float StrengthRatio { get; set; }
}

[HarmonyPatch(typeof(MakePeaceAction), "ApplyInternal")]
internal static class Patch_NpcTributeVassalage_MakePeaceAction
{
	[ThreadStatic]
	private static NpcTributeVassalagePeaceSnapshot _snapshot;

	public static void Prefix(
		IFaction faction1,
		IFaction faction2,
		int dailyTributeFrom1To2,
		int dailyTributeDuration,
		MakePeaceAction.MakePeaceDetail detail)
	{
		if (VassalageBehavior.IsApplyingVassalageDiplomacy)
		{
			_snapshot = null;
			return;
		}
		_snapshot = NpcTributeVassalageBehavior.Instance?.CapturePeaceSnapshot(
			faction1,
			faction2,
			dailyTributeFrom1To2,
			dailyTributeDuration,
			detail);
	}

	public static void Postfix()
	{
		NpcTributeVassalagePeaceSnapshot snapshot = _snapshot;
		_snapshot = null;
		if (snapshot == null || VassalageBehavior.IsApplyingVassalageDiplomacy)
		{
			return;
		}
		NpcTributeVassalageBehavior.Instance?.HandlePeaceApplied(snapshot);
	}
}

internal static class NpcTributeVassalageDiagnosticLog
{
	private const int MaxStringLength = 300;
	private const int MaxFieldsPerEvent = 48;
	private static readonly object FileLock = new object();
	private static readonly string SessionId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
	private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private static long _sequence;

	public static void Event(string stage, IDictionary<string, object> fields = null)
	{
		try
		{
			string path = GetDiagnosticLogPath();
			string directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			Dictionary<string, object> entry = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				["tsUtc"] = DateTime.UtcNow.ToString("o"),
				["session"] = SessionId,
				["seq"] = Interlocked.Increment(ref _sequence),
				["stage"] = Preview(stage ?? "", 160),
				["campaignDay"] = SafeGetCampaignDay(),
				["playerKingdom"] = DescribeKingdom(SafeGetPlayerKingdom())
			};
			if (fields != null)
			{
				foreach (KeyValuePair<string, object> field in fields)
				{
					string key = (field.Key ?? "").Trim();
					if (string.IsNullOrWhiteSpace(key))
					{
						continue;
					}
					entry[key] = NormalizeValue(field.Value);
					if (entry.Count >= MaxFieldsPerEvent)
					{
						entry["fieldsTruncated"] = true;
						break;
					}
				}
			}
			string line = JsonConvert.SerializeObject(entry, Formatting.None) + Environment.NewLine;
			lock (FileLock)
			{
				File.AppendAllText(path, line, Utf8NoBom);
			}
		}
		catch
		{
		}
	}

	public static string GetDiagnosticLogPath()
	{
		try
		{
			return AnimusForgeModulePaths.GetLogFilePath("AF_NpcTributeVassalage_Diagnostics.jsonl");
		}
		catch
		{
			return "AF_NpcTributeVassalage_Diagnostics.jsonl";
		}
	}

	public static string DescribeKingdom(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "null";
		}
		string name = "";
		string leader = "";
		try
		{
			name = kingdom.Name?.ToString() ?? "";
			leader = kingdom.Leader?.StringId ?? "";
		}
		catch
		{
		}
		return "kingdom=" + (kingdom.StringId ?? "") + ";name=" + Preview(name, 80) + ";leader=" + leader;
	}

	private static int SafeGetCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static Kingdom SafeGetPlayerKingdom()
	{
		try
		{
			return Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static object NormalizeValue(object value)
	{
		if (value == null)
		{
			return null;
		}
		if (value is string text)
		{
			return Preview(text, MaxStringLength);
		}
		if (value is Enum)
		{
			return value.ToString();
		}
		if (value is float f)
		{
			return Math.Round(f, 4);
		}
		if (value is double d)
		{
			return Math.Round(d, 4);
		}
		if (value is Kingdom kingdom)
		{
			return DescribeKingdom(kingdom);
		}
		if (value is Settlement settlement)
		{
			return "settlement=" + (settlement.StringId ?? "") + ";name=" + Preview(settlement.Name?.ToString() ?? "", 80);
		}
		if (value is System.Collections.IEnumerable enumerable && !(value is string))
		{
			List<object> items = new List<object>();
			int count = 0;
			foreach (object item in enumerable)
			{
				if (count >= 12)
				{
					items.Add("...");
					break;
				}
				items.Add(NormalizeValue(item));
				count++;
			}
			return items;
		}
		return value;
	}

	private static string Preview(string text, int maxLength)
	{
		if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
		{
			return text ?? "";
		}
		return text.Substring(0, maxLength) + "...";
	}
}
