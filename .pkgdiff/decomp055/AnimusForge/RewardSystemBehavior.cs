using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class RewardSystemBehavior : CampaignBehaviorBase
{
	public enum SettlementMerchantKind
	{
		None,
		Weapon,
		Armor,
		Horse,
		Goods
	}

	public class RewardItemInfo
	{
		public ItemObject Item { get; set; }

		public string StringId { get; set; }

		public string PromptStringId { get; set; }

		public string ModifierStringId { get; set; }

		public string Name { get; set; }

		public int Count { get; set; }

		public EquipmentElement EquipmentElement { get; set; }
	}

	public class DebtExportEntry
	{
		public int OwedGold;

		public Dictionary<string, int> OwedItems = new Dictionary<string, int>();

		public float CreatedDay;

		public float DueDay;

		public List<DebtLineExportEntry> DebtLines = new List<DebtLineExportEntry>();
	}

	public class DebtLineExportEntry
	{
		public string DebtId;

		public bool IsGold;

		public string ItemId;

		public bool IsDueUnlimited;

		public bool IsItemUnavailableDeclared;

		public int InitialAmount;

		public int RemainingAmount;

		public float CreatedDay;

		public float DueDay;

		public float BestPreDueCoverage;

		public int OnTimePenaltyTierApplied;

		public int OverduePenaltyDaysApplied;

		public int LastOverduePenaltyDay;

		public int OverdueTrustPenaltyPerDay;

		public int OverdueRelationPenaltyPerDay;

		public int CompensationUnitPrice;

		public int CompensationGoldCredit;
	}

	private class DebtRecord
	{
		public class DebtLine
		{
			public string DebtId;

			public bool IsGold;

			public string ItemId;

			public bool IsDueUnlimited;

			public bool IsItemUnavailableDeclared;

			public int InitialAmount;

			public int RemainingAmount;

			public float CreatedDay;

			public float DueDay;

			public float BestPreDueCoverage;

			public int OnTimePenaltyTierApplied;

			public int OverduePenaltyDaysApplied;

			public int LastOverduePenaltyDay;

			public int OverdueTrustPenaltyPerDay;

			public int OverdueRelationPenaltyPerDay;

			public int CompensationUnitPrice;

			public int CompensationGoldCredit;
		}

		public int OwedGold;

		public Dictionary<string, int> OwedItems = new Dictionary<string, int>();

		public float CreatedDay;

		public float DueDay;

		public List<DebtLine> DebtLines = new List<DebtLine>();
	}

	private class PendingPlayerTransfer
	{
		public int Gold;

		public int LastTouchedDay;

		public Dictionary<string, int> Items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	}

	private class MerchantFactRecord
	{
		public int LastTouchedDay;

		public List<string> Facts = new List<string>();
	}

	private class ItemGuidePriceInfo
	{
		public int UnitPrice;

		public int SampleCount;

		public bool ExpandedSearch;

		public bool UsedNoStockFallback;

		public bool UsedBaseValueFallback;
	}

	private const float DefaultDebtDueDays = 1f;

	private const int TrustMin = -100;

	private const int TrustMax = 100;

	private const int PublicTrustDeltaUnit = 2;

	private const int AutoTrustValuePerPoint = 1000;

	private const int TrustGainUnitsPerPoint = 1600;

	private const int SettlementTrustUnitsPerTier = 48;

	private const int PublicTrustPoolPointsPerTrust = 3;

	private const int TrustGainHalvingStart = 20;

	private const int TrustGainHalvingStep = 20;

	private const int TrustGainHalvingMaxStage = 4;

	private const int SettlementTrustContributionSharePercent = 30;

	private const float SettlementTrustBattleEffectRadius = 25f;

	private const int TrustGainOnCleanTrade = 1;

	private const int TrustGainOnOnTimeRepay = 2;

	private const int TrustPenaltyOnOverdueRepay = -2;

	private const float DebtCoverageThreshold = 0.95f;

	private const int OverduePenaltyMaxDays = 7;

	private const int LlmManualPenaltyMax = 10;

	private const int KingdomServiceMercenaryTrustMin = 5;

	private const int KingdomServiceVassalTrustMin = 20;

	private const int TrustGainOnQuestSuccess = 5;

	private static readonly string[] TrustLevelTexts = new string[10] { "彻底不信", "极度怀疑", "强烈戒备", "不太信任", "保留态度", "中性观望", "基本信任", "较高信任", "高度信任", "完全信赖" };

	private static readonly string[] TrustAiBehaviorTexts = new string[10] { "彻底不信任玩家，优先回避一切经济接触。", "仍高度怀疑玩家，只愿进行极端保守的交易试探。", "明显戒备，允许有限交易但条件苛刻。", "偏不信任，接受交易但会要求更高风险补偿。", "保留态度，交易可谈但会附加明显溢价。", "中性观望，可做常规交易但仍以安全为先。", "达到基本信任，可在可控风险下提供信用交易。", "较高信任，可提供中大额信用交易。", "高度信任，可提供高额信用交易。", "完全信赖，可提供接近无限额的信用交易。" };

	private static readonly string[] TrustAiActionGuideTexts = new string[10] { "L1：拒绝任何经济往来。禁止买卖、禁止赊账、禁止借款；不输出任何新增交易/欠款标签。", "L2：可进行即时交易，但应明显高于 guidePrice 定价以覆盖风险；禁止赊账/借款。", "L3：可进行即时交易，定价仍偏高；禁止赊账/借款。", "L4：可进行即时交易，定价可在“偏高到合理”之间浮动；禁止赊账/借款。", "L5：可进行即时交易，定价趋于合理但可保留风险溢价；禁止赊账/借款。", "L6：可进行即时交易，定价基本合理；禁止赊账/借款。", "L7：可赊账/借款（小到中额），额度需结合NPC财力与局势评估；默认计息。", "L8：可赊账/借款（中到较大额），额度需结合NPC财力与局势评估；默认计息。", "L9：可赊账/借款（大额），额度需结合NPC财力与局势评估；默认计息。", "L10：可赊账/借款（极高额度），仍需结合NPC财力、性格与风险控制；默认计息。" };

	private Dictionary<string, DebtRecord> _debts = new Dictionary<string, DebtRecord>();

	private Dictionary<string, string> _debtStorage = new Dictionary<string, string>();

	private Dictionary<string, int> _npcTrust = new Dictionary<string, int>();

	private Dictionary<string, int> _publicTrust = new Dictionary<string, int>();

	private Dictionary<string, int> _npcTrustStorage = new Dictionary<string, int>();

	private Dictionary<string, int> _publicTrustStorage = new Dictionary<string, int>();

	private Dictionary<string, int> _tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private int _currentBattlePlayerActualSettlementTrustUnits;

	private Dictionary<string, PendingPlayerTransfer> _pendingPlayerTransfers = new Dictionary<string, PendingPlayerTransfer>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, MerchantFactRecord> _merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private List<string> _lastGeneratedNpcFactLines = new List<string>();

	public static RewardSystemBehavior Instance { get; private set; }

	public RewardSystemBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this, (Action)OnDailyTick);
		CampaignEvents.MapEventEnded.AddNonSerializedListener((object)this, (Action<MapEvent>)OnMapEventEnded);
		CampaignEvents.OnPlayerPartyKnockedOrKilledTroopEvent.AddNonSerializedListener((object)this, (Action<CharacterObject>)OnPlayerPartyKnockedOrKilledTroop);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener((object)this, (Action<QuestBase, QuestCompleteDetails>)OnQuestCompleted);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debtStorage == null)
		{
			_debtStorage = new Dictionary<string, string>();
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		if (_npcTrustStorage == null)
		{
			_npcTrustStorage = new Dictionary<string, int>();
		}
		if (_publicTrustStorage == null)
		{
			_publicTrustStorage = new Dictionary<string, int>();
		}
		if (_tradeTrustValueCarry == null)
		{
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_tradeTrustValueCarryStorage == null)
		{
			_tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_directTrustProgressCarryStorage == null)
		{
			_directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustCentiCarryStorage == null)
		{
			_settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarry == null)
		{
			_settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarryStorage == null)
		{
			_settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_publicTrustProgressCarry == null)
		{
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_publicTrustProgressCarryStorage == null)
		{
			_publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_merchantFacts == null)
		{
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_merchantFactStorage == null)
		{
			_merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		try
		{
			_debtStorage.Clear();
			foreach (KeyValuePair<string, DebtRecord> debt in _debts)
			{
				if (string.IsNullOrEmpty(debt.Key) || debt.Value == null)
				{
					continue;
				}
				try
				{
					NormalizeDebtRecord(debt.Value);
					if (HasDebtContent(debt.Value))
					{
						string value = JsonConvert.SerializeObject((object)debt.Value);
						_debtStorage[debt.Key] = value;
					}
				}
				catch (Exception ex)
				{
					Logger.Log("RewardSystem", "[ERROR] Serialize debt for " + debt.Key + ": " + ex.Message);
				}
			}
			Dictionary<string, string> stored = CampaignSaveChunkHelper.FlattenStringDictionary(_debtStorage);
			dataStore.SyncData<Dictionary<string, string>>("_rewardDebts_v2", ref stored);
			_debtStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored, "RewardSystem");
			_debts.Clear();
			if (_debtStorage != null)
			{
				foreach (KeyValuePair<string, string> item in _debtStorage)
				{
					if (string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Value))
					{
						continue;
					}
					try
					{
						DebtRecord debtRecord = JsonConvert.DeserializeObject<DebtRecord>(item.Value);
						if (debtRecord != null)
						{
							NormalizeDebtRecord(debtRecord);
							if (HasDebtContent(debtRecord))
							{
								_debts[item.Key] = debtRecord;
							}
						}
					}
					catch (Exception ex2)
					{
						Logger.Log("RewardSystem", "[ERROR] Deserialize debt for " + item.Key + ": " + ex2.Message);
					}
				}
			}
			SyncMerchantFactData(dataStore);
			SyncTrustData(dataStore);
			SyncTradeTrustCarryData(dataStore);
			SyncDirectTrustProgressCarryData(dataStore);
			SyncSettlementTrustCarryData(dataStore);
			SyncPublicTrustProgressCarryData(dataStore);
		}
		catch (Exception ex3)
		{
			Logger.Log("RewardSystem", "[ERROR] SyncData v2 failed: " + ex3.ToString());
			_debts = new Dictionary<string, DebtRecord>();
			_debtStorage = new Dictionary<string, string>();
			_npcTrust = new Dictionary<string, int>();
			_publicTrust = new Dictionary<string, int>();
			_npcTrustStorage = new Dictionary<string, int>();
			_publicTrustStorage = new Dictionary<string, int>();
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
			_merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private static float GetNowCampaignDay()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CampaignTime now = CampaignTime.Now;
			return (float)((CampaignTime)(ref now)).ToDays;
		}
		catch
		{
			return 0f;
		}
	}

	private static int ToDisplayDay(float day)
	{
		if (day <= 0f)
		{
			return 0;
		}
		return Math.Max(1, (int)Math.Ceiling(day));
	}

	private static string NormalizeHeroId(Hero hero)
	{
		return (((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "").Trim();
	}

	private static string BuildSettlementMerchantPendingTransferKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "merchant:" + text;
	}

	private static string BuildSettlementMerchantDebtKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "merchant_debt:" + text;
	}

	private static string BuildSettlementMerchantTrustKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "merchant_trust:" + text;
	}

	private static bool TryParseSettlementMerchantDebtKey(string debtKey, out string settlementId, out SettlementMerchantKind kind)
	{
		settlementId = "";
		kind = SettlementMerchantKind.None;
		string text = (debtKey ?? "").Trim();
		if (!text.StartsWith("merchant_debt:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string[] array = text.Substring("merchant_debt:".Length).Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 2)
		{
			return false;
		}
		settlementId = array[0].Trim();
		return Enum.TryParse<SettlementMerchantKind>(array[1].Trim(), ignoreCase: true, out kind) && !string.IsNullOrWhiteSpace(settlementId) && kind != SettlementMerchantKind.None;
	}

	private static Settlement ResolveSettlementById(string settlementId)
	{
		try
		{
			string text = (settlementId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (item != null && string.Equals((((MBObjectBase)item).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string BuildSettlementMerchantDebtLabel(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = ((settlement == null) ? null : ((object)settlement.Name)?.ToString()) ?? "这座城镇";
		return text + "的" + GetSettlementMerchantMarketLabel(kind);
	}

	private PendingPlayerTransfer GetOrCreatePendingPlayerTransferByKey(string transferKey)
	{
		string text = (transferKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (_pendingPlayerTransfers == null)
		{
			_pendingPlayerTransfers = new Dictionary<string, PendingPlayerTransfer>(StringComparer.OrdinalIgnoreCase);
		}
		if (!_pendingPlayerTransfers.TryGetValue(text, out var value) || value == null)
		{
			value = new PendingPlayerTransfer();
			_pendingPlayerTransfers[text] = value;
		}
		value.LastTouchedDay = GetCampaignDayIndex();
		if (value.Items == null)
		{
			value.Items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		return value;
	}

	private PendingPlayerTransfer GetPendingPlayerTransferByKey(string transferKey)
	{
		string text = (transferKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || _pendingPlayerTransfers == null)
		{
			return null;
		}
		if (_pendingPlayerTransfers.TryGetValue(text, out var value) && value != null)
		{
			if (value.Items == null)
			{
				value.Items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			}
			return value;
		}
		return null;
	}

	private PendingPlayerTransfer GetOrCreatePendingPlayerTransfer(Hero npc)
	{
		return GetOrCreatePendingPlayerTransferByKey(NormalizeHeroId(npc));
	}

	private PendingPlayerTransfer GetPendingPlayerTransfer(Hero npc)
	{
		return GetPendingPlayerTransferByKey(NormalizeHeroId(npc));
	}

	private void CleanupPendingPlayerTransfers(int currentDay)
	{
		if (_pendingPlayerTransfers == null || _pendingPlayerTransfers.Count <= 0)
		{
			return;
		}
		List<string> list = _pendingPlayerTransfers.Keys.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!_pendingPlayerTransfers.TryGetValue(text, out var value) || value == null)
			{
				_pendingPlayerTransfers.Remove(text);
				continue;
			}
			int num = 0;
			try
			{
				if (value.Items != null)
				{
					foreach (KeyValuePair<string, int> item in value.Items)
					{
						if (item.Value > 0)
						{
							num += item.Value;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			bool flag = value.Gold > 0 || num > 0;
			bool flag2 = currentDay - value.LastTouchedDay > 2;
			if (!flag || flag2)
			{
				_pendingPlayerTransfers.Remove(text);
			}
		}
	}

	private static string BuildSettlementMerchantFactKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = (((settlement != null) ? ((MBObjectBase)settlement).StringId : null) ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || kind == SettlementMerchantKind.None)
		{
			return "";
		}
		return text.ToLowerInvariant() + ":" + kind.ToString().ToLowerInvariant();
	}

	private static void CleanupMerchantFactRecord(MerchantFactRecord record)
	{
		if (record == null)
		{
			return;
		}
		if (record.Facts == null)
		{
			record.Facts = new List<string>();
			return;
		}
		List<string> list = record.Facts.Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		if (list.Count > 8)
		{
			list = list.Skip(list.Count - 8).ToList();
		}
		record.Facts = list;
	}

	private MerchantFactRecord GetOrCreateMerchantFactRecord(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (_merchantFacts == null)
		{
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (!_merchantFacts.TryGetValue(text, out var value) || value == null)
		{
			value = new MerchantFactRecord();
			_merchantFacts[text] = value;
		}
		if (value.Facts == null)
		{
			value.Facts = new List<string>();
		}
		CleanupMerchantFactRecord(value);
		value.LastTouchedDay = GetCampaignDayIndex();
		return value;
	}

	private MerchantFactRecord GetMerchantFactRecord(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text) || _merchantFacts == null)
		{
			return null;
		}
		if (!_merchantFacts.TryGetValue(text, out var value) || value == null)
		{
			return null;
		}
		if (value.Facts == null)
		{
			value.Facts = new List<string>();
		}
		CleanupMerchantFactRecord(value);
		return value;
	}

	public void AppendSettlementMerchantNpcFact(Settlement settlement, SettlementMerchantKind kind, string factText, string speakerName = null)
	{
		try
		{
			string text = (factText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				MerchantFactRecord orCreateMerchantFactRecord = GetOrCreateMerchantFactRecord(settlement, kind);
				if (orCreateMerchantFactRecord != null)
				{
					string text2 = (speakerName ?? GetSettlementMerchantRoleLabel(kind) ?? "商贩").Trim();
					orCreateMerchantFactRecord.Facts.Add("[AFEF NPC行为补充] " + text2 + ": " + text);
					CleanupMerchantFactRecord(orCreateMerchantFactRecord);
				}
			}
		}
		catch
		{
		}
	}

	public string BuildSettlementMerchantNpcFactSummaryForAI(CharacterObject character, Settlement settlement = null, int maxLines = 4)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		MerchantFactRecord merchantFactRecord = GetMerchantFactRecord(settlement, kind);
		if (merchantFactRecord?.Facts == null || merchantFactRecord.Facts.Count <= 0)
		{
			return "";
		}
		List<string> list = merchantFactRecord.Facts;
		int num = Math.Max(1, maxLines);
		if (list.Count > num)
		{
			list = list.Skip(list.Count - num).ToList();
		}
		return string.Join("\n", list);
	}

	public string BuildNpcBehaviorSupplementForAI(Hero hero, CharacterObject character = null, int maxLines = 4)
	{
		if (hero != null)
		{
			return MyBehavior.BuildRecentNpcFactContextForExternal(hero, maxLines);
		}
		if (character != null)
		{
			return BuildSettlementMerchantNpcFactSummaryForAI(character, null, maxLines);
		}
		return "";
	}

	private void SetLastGeneratedNpcFactLines(IEnumerable<string> lines)
	{
		_lastGeneratedNpcFactLines = ((lines != null) ? lines.Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList() : new List<string>());
	}

	public List<string> ConsumeLastGeneratedNpcFactLines()
	{
		List<string> result = ((_lastGeneratedNpcFactLines != null) ? new List<string>(_lastGeneratedNpcFactLines) : new List<string>());
		_lastGeneratedNpcFactLines = new List<string>();
		return result;
	}

	public void RecordPlayerPrepaidTransfer(Hero npc, int goldAmount, string itemId, int itemAmount)
	{
		try
		{
			PendingPlayerTransfer orCreatePendingPlayerTransfer = GetOrCreatePendingPlayerTransfer(npc);
			if (orCreatePendingPlayerTransfer == null)
			{
				return;
			}
			if (goldAmount > 0)
			{
				orCreatePendingPlayerTransfer.Gold = Math.Max(0, orCreatePendingPlayerTransfer.Gold) + goldAmount;
			}
			if (!string.IsNullOrWhiteSpace(itemId) && itemAmount > 0)
			{
				string key = itemId.Trim();
				if (orCreatePendingPlayerTransfer.Items.TryGetValue(key, out var value))
				{
					orCreatePendingPlayerTransfer.Items[key] = value + itemAmount;
				}
				else
				{
					orCreatePendingPlayerTransfer.Items[key] = itemAmount;
				}
			}
		}
		catch
		{
		}
	}

	public void RecordPlayerPrepaidTransferForMerchant(Settlement settlement, SettlementMerchantKind kind, int goldAmount, string itemId, int itemAmount)
	{
		try
		{
			PendingPlayerTransfer orCreatePendingPlayerTransferByKey = GetOrCreatePendingPlayerTransferByKey(BuildSettlementMerchantPendingTransferKey(settlement, kind));
			if (orCreatePendingPlayerTransferByKey == null)
			{
				return;
			}
			if (goldAmount > 0)
			{
				orCreatePendingPlayerTransferByKey.Gold = Math.Max(0, orCreatePendingPlayerTransferByKey.Gold) + goldAmount;
			}
			if (!string.IsNullOrWhiteSpace(itemId) && itemAmount > 0)
			{
				string key = itemId.Trim();
				if (orCreatePendingPlayerTransferByKey.Items.TryGetValue(key, out var value))
				{
					orCreatePendingPlayerTransferByKey.Items[key] = value + itemAmount;
				}
				else
				{
					orCreatePendingPlayerTransferByKey.Items[key] = itemAmount;
				}
			}
		}
		catch
		{
		}
	}

	public int GetPlayerPrepaidGoldForExternal(Hero npc)
	{
		try
		{
			return Math.Max(0, GetPendingPlayerTransfer(npc)?.Gold ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	public int ConsumePlayerPrepaidGoldForExternal(Hero npc, int request)
	{
		try
		{
			return ConsumePlayerPrepaidGold(npc, request);
		}
		catch
		{
			return 0;
		}
	}

	private int ConsumePlayerPrepaidGold(Hero npc, int request)
	{
		if (request <= 0)
		{
			return 0;
		}
		PendingPlayerTransfer pendingPlayerTransfer = GetPendingPlayerTransfer(npc);
		if (pendingPlayerTransfer == null)
		{
			return 0;
		}
		int num = Math.Min(Math.Max(0, pendingPlayerTransfer.Gold), request);
		if (num <= 0)
		{
			return 0;
		}
		pendingPlayerTransfer.Gold = Math.Max(0, pendingPlayerTransfer.Gold - num);
		pendingPlayerTransfer.LastTouchedDay = GetCampaignDayIndex();
		return num;
	}

	private int ConsumePlayerPrepaidItem(Hero npc, string itemId, int request)
	{
		if (request <= 0 || string.IsNullOrWhiteSpace(itemId))
		{
			return 0;
		}
		PendingPlayerTransfer pendingPlayerTransfer = GetPendingPlayerTransfer(npc);
		if (pendingPlayerTransfer == null || pendingPlayerTransfer.Items == null)
		{
			return 0;
		}
		string key = itemId.Trim();
		if (!pendingPlayerTransfer.Items.TryGetValue(key, out var value) || value <= 0)
		{
			return 0;
		}
		int num = Math.Min(value, request);
		if (num <= 0)
		{
			return 0;
		}
		int num2 = value - num;
		if (num2 > 0)
		{
			pendingPlayerTransfer.Items[key] = num2;
		}
		else
		{
			pendingPlayerTransfer.Items.Remove(key);
		}
		pendingPlayerTransfer.LastTouchedDay = GetCampaignDayIndex();
		return num;
	}

	private int ConsumePlayerPrepaidGoldForMerchant(Settlement settlement, SettlementMerchantKind kind, int request)
	{
		if (request <= 0)
		{
			return 0;
		}
		PendingPlayerTransfer pendingPlayerTransferByKey = GetPendingPlayerTransferByKey(BuildSettlementMerchantPendingTransferKey(settlement, kind));
		if (pendingPlayerTransferByKey == null)
		{
			return 0;
		}
		int num = Math.Min(Math.Max(0, pendingPlayerTransferByKey.Gold), request);
		if (num <= 0)
		{
			return 0;
		}
		pendingPlayerTransferByKey.Gold = Math.Max(0, pendingPlayerTransferByKey.Gold - num);
		pendingPlayerTransferByKey.LastTouchedDay = GetCampaignDayIndex();
		return num;
	}

	private int ConsumePlayerPrepaidItemForMerchant(Settlement settlement, SettlementMerchantKind kind, string itemId, int request)
	{
		if (request <= 0 || string.IsNullOrWhiteSpace(itemId))
		{
			return 0;
		}
		PendingPlayerTransfer pendingPlayerTransferByKey = GetPendingPlayerTransferByKey(BuildSettlementMerchantPendingTransferKey(settlement, kind));
		if (pendingPlayerTransferByKey == null || pendingPlayerTransferByKey.Items == null)
		{
			return 0;
		}
		string key = itemId.Trim();
		if (!pendingPlayerTransferByKey.Items.TryGetValue(key, out var value) || value <= 0)
		{
			return 0;
		}
		int num = Math.Min(value, request);
		if (num <= 0)
		{
			return 0;
		}
		int num2 = value - num;
		if (num2 > 0)
		{
			pendingPlayerTransferByKey.Items[key] = num2;
		}
		else
		{
			pendingPlayerTransferByKey.Items.Remove(key);
		}
		pendingPlayerTransferByKey.LastTouchedDay = GetCampaignDayIndex();
		return num;
	}

	private static bool HasDebtContent(DebtRecord rec)
	{
		if (rec == null)
		{
			return false;
		}
		if (rec.OwedGold > 0)
		{
			return true;
		}
		if (rec.OwedItems == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, int> owedItem in rec.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static int NormalizeDueDays(int days)
	{
		if (days < 1)
		{
			return 1;
		}
		if (days > 120)
		{
			return 120;
		}
		return days;
	}

	private static float Clamp01(float v)
	{
		if (v < 0f)
		{
			return 0f;
		}
		if (v > 1f)
		{
			return 1f;
		}
		return v;
	}

	private static float ComputeCoverageRatio(int initialAmount, int remainingAmount)
	{
		int num = Math.Max(1, initialAmount);
		int num2 = Math.Max(0, remainingAmount);
		float num3 = num - num2;
		return Clamp01(num3 / (float)num);
	}

	private static int ComputeOnTimePartialPenaltyTier(float coverage)
	{
		if (coverage >= 0.95f)
		{
			return 0;
		}
		float num = 0.95f - coverage;
		int num2 = (int)Math.Ceiling(num * 10f);
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 5)
		{
			num2 = 5;
		}
		return num2;
	}

	private static int ComputeDailyOverduePenaltyByCoverage(int initialAmount, int remainingAmount)
	{
		int num = Math.Max(1, initialAmount);
		int num2 = Math.Max(0, remainingAmount);
		float num3 = Clamp01((float)num2 / (float)num);
		if (num3 >= 0.7f)
		{
			return 3;
		}
		if (num3 >= 0.3f)
		{
			return 2;
		}
		return 1;
	}

	private static int ComputeDailyOverduePenaltyByDebtValue(int debtValue)
	{
		int num = Math.Max(0, debtValue);
		if (num >= 1000000)
		{
			return 5;
		}
		if (num >= 500000)
		{
			return 4;
		}
		if (num >= 100000)
		{
			return 3;
		}
		if (num >= 20000)
		{
			return 2;
		}
		return 1;
	}

	private static int ComputeDailyOverduePenaltySeverity(int initialAmount, int remainingAmount, int debtValue)
	{
		int val = ComputeDailyOverduePenaltyByCoverage(initialAmount, remainingAmount);
		int val2 = ComputeDailyOverduePenaltyByDebtValue(debtValue);
		int num = Math.Max(val, val2);
		if (num < 1)
		{
			num = 1;
		}
		if (num > 5)
		{
			num = 5;
		}
		return num;
	}

	private static int NormalizeLlmPenaltyValue(int value)
	{
		if (value < 0)
		{
			return 0;
		}
		if (value > 10)
		{
			return 10;
		}
		return value;
	}

	private static int NormalizeLlmTrustDeltaValue(int value)
	{
		if (value < -10)
		{
			return -10;
		}
		if (value > 10)
		{
			return 10;
		}
		return value;
	}

	private static int ClampPositiveLongToInt(long value)
	{
		if (value <= 0)
		{
			return 0;
		}
		if (value > int.MaxValue)
		{
			return int.MaxValue;
		}
		return (int)value;
	}

	private static long ClampLong(long value, long min, long max)
	{
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

	private static int GetTrustGainHalvingStage(int currentTrust)
	{
		int num = ClampTrust(currentTrust);
		if (num < 20)
		{
			return 0;
		}
		int num2 = (num - 20) / 20 + 1;
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (num2 > 4)
		{
			num2 = 4;
		}
		return num2;
	}

	private static int GetTrustLossBoostStage(int currentTrust)
	{
		int num = ClampTrust(currentTrust);
		if (num > -20)
		{
			return 0;
		}
		int num2 = (-num - 20) / 20 + 1;
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (num2 > 4)
		{
			num2 = 4;
		}
		return num2;
	}

	private static int GetTrustGainDivisorByCurrentTrust(int currentTrust)
	{
		return 1 << GetTrustGainHalvingStage(currentTrust);
	}

	private static int GetTrustLossMultiplierByCurrentTrust(int currentTrust)
	{
		return 1 << GetTrustLossBoostStage(currentTrust);
	}

	private int ConvertRawTrustDeltaToUnits(int rawDelta, int currentTrust)
	{
		if (rawDelta == 0)
		{
			return 0;
		}
		if (currentTrust <= -20)
		{
			return rawDelta * 1600 * GetTrustLossMultiplierByCurrentTrust(currentTrust);
		}
		int trustGainDivisorByCurrentTrust = GetTrustGainDivisorByCurrentTrust(currentTrust);
		return rawDelta * 1600 / trustGainDivisorByCurrentTrust;
	}

	private int ApplyDirectTrustDeltaUnits(string trustKey, int currentTrust, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		string text = (trustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || rawDelta == 0)
		{
			return 0;
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_directTrustProgressCarry.TryGetValue(text, out var value);
		long num = (long)currentTrust * 1600L + value;
		int num2 = ConvertRawTrustDeltaToUnits(rawDelta, currentTrust);
		long num3 = ClampLong(num + num2, -160000L, 160000L);
		appliedUnits = (int)(num3 - num);
		int num4 = (int)(num3 / 1600);
		int num5 = (int)(num3 % 1600);
		if (num5 != 0)
		{
			_directTrustProgressCarry[text] = num5;
		}
		else
		{
			_directTrustProgressCarry.Remove(text);
		}
		return num4 - currentTrust;
	}

	private int ApplySettlementTrustUnits(Settlement settlement, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || rawDelta == 0)
		{
			return 0;
		}
		string text = BuildSettlementTrustCarryKey(settlement);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_settlementTrustCentiCarry.TryGetValue(text, out var value);
		int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(settlement);
		long num = (long)settlementLocalPublicTrust * 1600L + value;
		int num2 = ConvertRawTrustDeltaToUnits(rawDelta, settlementLocalPublicTrust);
		long num3 = ClampLong(num + num2, -160000L, 160000L);
		appliedUnits = (int)(num3 - num);
		int num4 = (int)(num3 / 1600);
		int num5 = (int)(num3 % 1600);
		if (num5 != 0)
		{
			_settlementTrustCentiCarry[text] = num5;
		}
		else
		{
			_settlementTrustCentiCarry.Remove(text);
		}
		return num4 - settlementLocalPublicTrust;
	}

	private void ApplySettlementLocalTrustWholeDeltaDirect(Settlement settlement, int localTrustDelta, string reason)
	{
		if (settlement == null || localTrustDelta == 0)
		{
			return;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementLocalPublicTrustKey(settlement);
		if (!string.IsNullOrWhiteSpace(text))
		{
			int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(settlement);
			int num = ClampTrust(settlementLocalPublicTrust + localTrustDelta);
			if (num == 0)
			{
				_publicTrust.Remove(text);
			}
			else
			{
				_publicTrust[text] = num;
			}
			Logger.Log("Trust", $"settlement={((MBObjectBase)settlement).StringId} reason={reason} settlementTrust={settlementLocalPublicTrust}->{num} delta={localTrustDelta}");
		}
	}

	private static string FormatTrustUnits(int units)
	{
		string text = ((decimal)units / 1600m).ToString("0.######");
		if (text == "-0")
		{
			return "0";
		}
		return text;
	}

	private int AccumulateTradeTrustValueByKey(string carryKey, int addedValue)
	{
		string text = (carryKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || addedValue <= 0)
		{
			return 0;
		}
		if (_tradeTrustValueCarry == null)
		{
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_tradeTrustValueCarry.TryGetValue(text, out var value);
		long num = Math.Max(0, value);
		long num2 = num + addedValue;
		int result = ClampPositiveLongToInt(num2 / 1000);
		int num3 = (int)(num2 % 1000);
		if (num3 > 0)
		{
			_tradeTrustValueCarry[text] = num3;
		}
		else
		{
			_tradeTrustValueCarry.Remove(text);
		}
		return result;
	}

	private int GetItemTrustValueForHeroGift(Hero hero, ItemObject item, int amount)
	{
		if (item == null || amount <= 0)
		{
			return 0;
		}
		ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero ?? Hero.MainHero, item);
		return ClampPositiveLongToInt((long)Math.Max(1, guidePriceForItemNearHero.UnitPrice) * (long)amount);
	}

	private int GetItemTrustValueForMerchantGift(Settlement settlement, ItemObject item, int amount)
	{
		if (item == null || amount <= 0)
		{
			return 0;
		}
		if (settlement != null && TryGetSettlementBuyPrice(settlement, item, out var price) && price > 0)
		{
			return ClampPositiveLongToInt((long)price * (long)amount);
		}
		return GetItemTrustValueForHeroGift(Hero.MainHero, item, amount);
	}

	private void ApplyAutoTrustGainFromHeroGiftValue(Hero giver, int addedValue, List<string> giverFacts, List<string> receiverFacts, string giverName)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Expected O, but got Unknown
		if (giver != null && addedValue > 0)
		{
			int num = AccumulateTradeTrustValueByKey(NormalizeHeroId(giver), addedValue);
			if (num > 0)
			{
				int appliedUnits;
				int num2 = AdjustTrust(giver, num, 0, "auto_gift_value_accumulated", out appliedUnits);
				string text = ((num2 > 0) ? $"，公共信任提升 {num2}" : "");
				string text2 = FormatTrustUnits(appliedUnits);
				giverFacts?.Add("你因累计向玩家实际交付的价值达到阈值，对玩家的个人信任提升了 " + text2 + text + "。");
				receiverFacts?.Add(giverName + " 因累计向你实际交付的价值达到阈值，对你的个人信任提升了 " + text2 + text + "。");
				InformationManager.DisplayMessage(new InformationMessage("【信任变化】" + giverName + " 因累计向你实际交付的价值，对你的个人信任 +" + text2 + ((num2 > 0) ? $"，公共信任 +{num2}" : ""), Color.FromUint(4278242559u)));
			}
		}
	}

	private void ApplyAutoTrustGainFromMerchantGiftValue(Settlement settlement, SettlementMerchantKind kind, int addedValue, List<string> merchantFacts, List<string> playerFacts, string giverName)
	{
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		if (settlement != null && kind != SettlementMerchantKind.None && addedValue > 0)
		{
			int num = AccumulateTradeTrustValueByKey(BuildSettlementMerchantTrustKey(settlement, kind), addedValue);
			if (num > 0)
			{
				int appliedUnits;
				int num2 = AdjustSettlementMerchantTrust(settlement, kind, num, "merchant_auto_gift_value_accumulated", out appliedUnits);
				string text = BuildSettlementMerchantDebtLabel(settlement, kind);
				string text2 = ((num2 > 0) ? $"，公共信任提升 {num2}" : "");
				string text3 = FormatTrustUnits(appliedUnits);
				merchantFacts?.Add("你因累计向玩家实际交付的价值达到阈值，对玩家的市场信任提升了 " + text3 + text2 + "。");
				playerFacts?.Add(giverName + " 代表的" + text + "因累计向你实际交付的价值达到阈值，对你的市场信任提升了 " + text3 + text2 + "。");
				InformationManager.DisplayMessage(new InformationMessage("【市场信任变化】" + text + " 对你的市场信任 +" + text3 + ((num2 > 0) ? $"，公共信任 +{num2}" : ""), Color.FromUint(4278242559u)));
			}
		}
	}

	private static int TruncateDivisionTowardsZero(int dividend, int divisor, out int remainder)
	{
		remainder = 0;
		if (divisor == 0)
		{
			return 0;
		}
		int result = dividend / divisor;
		remainder = dividend % divisor;
		return result;
	}

	private int AccumulatePublicTrustProgressByKey(string publicTrustKey, int sourceUnits)
	{
		string text = (publicTrustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || sourceUnits == 0)
		{
			return 0;
		}
		if (_publicTrustProgressCarry == null)
		{
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_publicTrustProgressCarry.TryGetValue(text, out var value);
		int dividend = value + sourceUnits;
		int remainder;
		int result = TruncateDivisionTowardsZero(dividend, 4800, out remainder);
		if (remainder != 0)
		{
			_publicTrustProgressCarry[text] = remainder;
		}
		else
		{
			_publicTrustProgressCarry.Remove(text);
		}
		return result;
	}

	private int AdjustPublicTrustByKey(string publicTrustKey, int publicDelta, string reason)
	{
		string text = (publicTrustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || publicDelta == 0)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		int value = 0;
		_publicTrust.TryGetValue(text, out value);
		value = ClampTrust(value);
		int num = ClampTrust(value + publicDelta);
		if (num == 0)
		{
			_publicTrust.Remove(text);
		}
		else
		{
			_publicTrust[text] = num;
		}
		Logger.Log("Trust", $"publicKey={text} reason={reason} publicTrust={value}->{num} delta={publicDelta}");
		return num - value;
	}

	private int ApplyPublicTrustPoolDeltaByKey(string publicTrustKey, int sourceUnits, string reason)
	{
		int num = AccumulatePublicTrustProgressByKey(publicTrustKey, sourceUnits);
		if (num == 0)
		{
			return 0;
		}
		return AdjustPublicTrustByKey(publicTrustKey, num, reason);
	}

	private static string BuildSettlementTrustCarryKey(Settlement settlement)
	{
		return BuildSettlementLocalPublicTrustKey(settlement);
	}

	private int AccumulateSettlementTrustCenti(Settlement settlement, int centiDelta)
	{
		string text = BuildSettlementTrustCarryKey(settlement);
		if (string.IsNullOrWhiteSpace(text) || centiDelta == 0)
		{
			return 0;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_settlementTrustCentiCarry.TryGetValue(text, out var value);
		int dividend = value + centiDelta;
		int remainder;
		int result = TruncateDivisionTowardsZero(dividend, 1600, out remainder);
		if (remainder != 0)
		{
			_settlementTrustCentiCarry[text] = remainder;
		}
		else
		{
			_settlementTrustCentiCarry.Remove(text);
		}
		return result;
	}

	private static int ComputeSettlementTrustCentiForTroop(CharacterObject troop, int count)
	{
		if (troop == null || count <= 0)
		{
			return 0;
		}
		int num = Math.Max(1, troop.Tier);
		return 48 * num * count;
	}

	private void OnPlayerPartyKnockedOrKilledTroop(CharacterObject strikedTroop)
	{
		try
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			if (playerMapEvent != null && playerMapEvent.IsPlayerMapEvent)
			{
				_currentBattlePlayerActualSettlementTrustUnits += ComputeSettlementTrustCentiForTroop(strikedTroop, 1);
			}
		}
		catch
		{
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestCompleteDetails details)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		try
		{
			if (quest != null && (int)details == 1)
			{
				Hero val = null;
				try
				{
					val = quest.QuestGiver;
				}
				catch
				{
					val = null;
				}
				if (val != null)
				{
					int appliedUnits;
					int num = AdjustTrust(val, 5, 0, "quest_completed_success", out appliedUnits);
					string text = FormatTrustUnits(appliedUnits);
					string text2 = ((object)val.Name)?.ToString() ?? "任务发布人";
					Logger.Log("Trust", $"quest={((MBObjectBase)quest).StringId} giver={((MBObjectBase)val).StringId} completed=success personalGain={text} publicGain={num}");
					InformationManager.DisplayMessage(new InformationMessage("【信任变化】完成" + text2 + "交付的任务，个人信任 +" + text + ((num > 0) ? $"，公共信任 +{num}" : ""), Color.FromUint(4278242559u)));
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[ERROR] quest completion trust reward failed: " + ex);
		}
	}

	private static bool IsPartyHostileForSettlementTrust(PartyBase party, Settlement settlement)
	{
		if (party == null || settlement == null)
		{
			return false;
		}
		try
		{
			if (party.MobileParty != null && party.MobileParty.IsBandit)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			IFaction mapFaction = party.MapFaction;
			IFaction mapFaction2 = settlement.MapFaction;
			return mapFaction != null && mapFaction2 != null && mapFaction.IsAtWarWith(mapFaction2);
		}
		catch
		{
			return false;
		}
	}

	private static IEnumerable<Settlement> GetNearbySettlementsForTrust(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return Enumerable.Empty<Settlement>();
		}
		float num = 25f;
		float num2 = num * num;
		return ((IEnumerable<Settlement>)Settlement.All).Where(delegate(Settlement x)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			int result;
			if (x != null && !x.IsHideout)
			{
				CampaignVec2 position = x.Position;
				result = ((((CampaignVec2)(ref position)).DistanceSquared(mapEvent.Position) < num2) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}).ToList();
	}

	private static int ComputeSettlementTrustCentiFromRoster(TroopRoster roster)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (roster == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			num += ComputeSettlementTrustCentiForTroop(elementCopyAtIndex.Character, ((TroopRosterElement)(ref elementCopyAtIndex)).Number);
		}
		return num;
	}

	private static int ComputeSettlementTrustCentiFromBattleRosters(MapEventParty party, bool includeSurrenderedActiveTroops)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		if (party == null)
		{
			return 0;
		}
		int num = ComputeSettlementTrustCentiFromRoster(party.DiedInBattle) + ComputeSettlementTrustCentiFromRoster(party.WoundedInBattle);
		if (!includeSurrenderedActiveTroops || party.Troops == null)
		{
			return num;
		}
		foreach (FlattenedTroopRosterElement troop in party.Troops)
		{
			FlattenedTroopRosterElement current = troop;
			if (((FlattenedTroopRosterElement)(ref current)).Troop != null && (int)((FlattenedTroopRosterElement)(ref current)).State == 0)
			{
				num += ComputeSettlementTrustCentiForTroop(((FlattenedTroopRosterElement)(ref current)).Troop, 1);
			}
		}
		return num;
	}

	private int ComputeSettlementTrustCentiFromDefeatedHostileTroops(MapEvent mapEvent, Settlement settlement)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (mapEvent == null || settlement == null || !mapEvent.HasWinner)
		{
			return 0;
		}
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
		if (((mapEventSide != null) ? mapEventSide.Parties : null) == null)
		{
			return 0;
		}
		bool includeSurrenderedActiveTroops = IsMapEventSideSurrendered(mapEventSide);
		int num = 0;
		foreach (MapEventParty item in (List<MapEventParty>)(object)mapEventSide.Parties)
		{
			if (((item != null) ? item.Party : null) != null && IsPartyHostileForSettlementTrust(item.Party, settlement))
			{
				num += ComputeSettlementTrustCentiFromBattleRosters(item, includeSurrenderedActiveTroops);
			}
		}
		return num;
	}

	private static bool IsMapEventSideSurrendered(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			FieldInfo field = typeof(MapEventSide).GetField("IsSurrendered", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool))
			{
				return (bool)field.GetValue(side);
			}
		}
		catch
		{
		}
		return false;
	}

	private int ComputePlayerContributionSharePercentForWinningSide(MapEvent mapEvent)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (mapEvent == null || !mapEvent.HasWinner)
		{
			return 0;
		}
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.WinningSide);
		if (((mapEventSide != null) ? mapEventSide.Parties : null) == null)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		foreach (MapEventParty item in (List<MapEventParty>)(object)mapEventSide.Parties)
		{
			if (item != null)
			{
				int num3 = Math.Max(0, item.ContributionToBattle);
				num += num3;
				if (item.Party == PartyBase.MainParty)
				{
					num2 = num3;
				}
			}
		}
		if (num <= 0 || num2 <= 0)
		{
			return 0;
		}
		return Math.Max(0, Math.Min(100, (int)Math.Round((double)(num2 * 100) / (double)num, MidpointRounding.AwayFromZero)));
	}

	private int AdjustSettlementLocalTrustInternal(Settlement settlement, int localTrustDelta, string reason)
	{
		if (settlement == null || localTrustDelta == 0)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string value = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(value))
		{
			return 0;
		}
		int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(settlement);
		int appliedUnits;
		int num = ApplySettlementTrustUnits(settlement, localTrustDelta, out appliedUnits);
		int num2 = ClampTrust(settlementLocalPublicTrust + num);
		ApplySettlementLocalTrustWholeDeltaDirect(settlement, num, reason);
		int num3 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "external") + "_local_public_pool");
		Logger.Log("Trust", $"settlement={((MBObjectBase)settlement).StringId} reason={reason} settlementTrust={settlementLocalPublicTrust}->{num2} rawDelta={localTrustDelta} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={num3}");
		return num3;
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Expected O, but got Unknown
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Expected O, but got Unknown
		try
		{
			int currentBattlePlayerActualSettlementTrustUnits = _currentBattlePlayerActualSettlementTrustUnits;
			_currentBattlePlayerActualSettlementTrustUnits = 0;
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner)
			{
				return;
			}
			if (mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗未获胜，未获得定居点信任。", Color.FromUint(4294945365u)));
				return;
			}
			List<Settlement> list = GetNearbySettlementsForTrust(mapEvent).ToList();
			if (list.Count <= 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗附近没有可受影响的定居点。", Color.FromUint(4291611750u)));
				return;
			}
			List<string> list2 = new List<string>();
			int num = ComputePlayerContributionSharePercentForWinningSide(mapEvent);
			foreach (Settlement item in list)
			{
				int num2 = ComputeSettlementTrustCentiFromDefeatedHostileTroops(mapEvent, item);
				int num3 = Math.Max(0, currentBattlePlayerActualSettlementTrustUnits);
				int num4 = 0;
				if (num3 <= 0 && num > 0 && num2 > 0)
				{
					num4 = (int)Math.Round((double)(num2 * num) / 100.0, MidpointRounding.AwayFromZero);
				}
				int num5 = Math.Max(num3, num4);
				int num6 = Math.Max(0, num2 - num5);
				int num7 = 0;
				if (num > 0 && num6 > 0)
				{
					num7 = (int)Math.Round((double)(num6 * num * 30) / 10000.0, MidpointRounding.AwayFromZero);
				}
				int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(item);
				int trustGainDivisorByCurrentTrust = GetTrustGainDivisorByCurrentTrust(settlementLocalPublicTrust);
				if (trustGainDivisorByCurrentTrust > 1)
				{
					num5 /= trustGainDivisorByCurrentTrust;
					num7 /= trustGainDivisorByCurrentTrust;
				}
				else if (settlementLocalPublicTrust <= -20)
				{
					int trustLossMultiplierByCurrentTrust = GetTrustLossMultiplierByCurrentTrust(settlementLocalPublicTrust);
					if (trustLossMultiplierByCurrentTrust > 1)
					{
						num5 *= trustLossMultiplierByCurrentTrust;
						num7 *= trustLossMultiplierByCurrentTrust;
					}
				}
				int num8 = num5 + num7;
				if (num8 > 0)
				{
					int num9 = AccumulateSettlementTrustCenti(item, num8);
					int num10 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(item), num8, "battle_hostile_party_defeated_local_public_pool");
					string text = ((num4 > 0) ? "估算实击" : "实击");
					if (num9 != 0)
					{
						ApplySettlementLocalTrustWholeDeltaDirect(item, num9, "battle_hostile_party_defeated");
						list2.Add($"{item.Name}: 定居点信任 +{num9}" + ((num10 > 0) ? $"，公共信任 +{num10}" : "") + "\n" + text + " " + FormatTrustUnits(num5) + "，分成 " + FormatTrustUnits(num7) + "，本次累计 " + FormatTrustUnits(num8));
					}
					else
					{
						list2.Add($"{item.Name}: 定居点信任累计 +{FormatTrustUnits(num8)}" + ((num10 > 0) ? $"，公共信任 +{num10}" : "") + "\n" + text + " " + FormatTrustUnits(num5) + "，分成 " + FormatTrustUnits(num7) + "，未满1点");
					}
				}
			}
			if (list2.Count > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】\n" + string.Join("\n\n", list2), Color.FromUint(4278242559u)));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗未击败附近定居点的敌对部队，因此没有获得定居点信任。", Color.FromUint(4291611750u)));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[ERROR] OnMapEventEnded settlement trust failed: " + ex);
		}
	}

	private static int GetCampaignDayIndex()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CampaignTime now = CampaignTime.Now;
			return Math.Max(0, (int)Math.Floor(((CampaignTime)(ref now)).ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static int GetDaysInSeasonSafe()
	{
		try
		{
			int daysInSeason = CampaignTime.DaysInSeason;
			if (daysInSeason > 0)
			{
				return daysInSeason;
			}
		}
		catch
		{
		}
		return 21;
	}

	private static int GetDaysInYearSafe()
	{
		try
		{
			int daysInYear = CampaignTime.DaysInYear;
			if (daysInYear > 0)
			{
				return daysInYear;
			}
		}
		catch
		{
		}
		return GetDaysInSeasonSafe() * 4;
	}

	private static int NormalizeSeasonIndex(int seasonIndex)
	{
		int num = seasonIndex % 4;
		if (num < 0)
		{
			num += 4;
		}
		return num;
	}

	private static int ToAbsDayFromCalendar(int year, int seasonIndexZeroBased, int dayOfSeasonOneBased)
	{
		int daysInSeasonSafe = GetDaysInSeasonSafe();
		int daysInYearSafe = GetDaysInYearSafe();
		int num = Math.Max(0, year);
		int num2 = NormalizeSeasonIndex(seasonIndexZeroBased);
		int num3 = dayOfSeasonOneBased;
		if (num3 < 1)
		{
			num3 = 1;
		}
		if (num3 > daysInSeasonSafe)
		{
			num3 = daysInSeasonSafe;
		}
		long num4 = (long)num * (long)daysInYearSafe + (long)num2 * (long)daysInSeasonSafe + (num3 - 1);
		if (num4 < 1)
		{
			num4 = 1L;
		}
		if (num4 > 200000)
		{
			num4 = 200000L;
		}
		return (int)num4;
	}

	private static bool TryParseSeasonToken(string token, out int seasonIndexZeroBased)
	{
		seasonIndexZeroBased = 0;
		if (string.IsNullOrWhiteSpace(token))
		{
			return false;
		}
		string text = token.Trim().ToLowerInvariant();
		switch (text)
		{
		default:
			if (text == "spring")
			{
				break;
			}
			switch (text)
			{
			default:
				if (text == "summer")
				{
					break;
				}
				switch (text)
				{
				default:
					if (text == "fall")
					{
						break;
					}
					switch (text)
					{
					default:
						if (!(text == "winter"))
						{
							return false;
						}
						break;
					case "4":
					case "冬":
					case "冬季":
						break;
					}
					seasonIndexZeroBased = 3;
					return true;
				case "3":
				case "秋":
				case "秋季":
				case "autumn":
					break;
				}
				seasonIndexZeroBased = 2;
				return true;
			case "2":
			case "夏":
			case "夏季":
				break;
			}
			seasonIndexZeroBased = 1;
			return true;
		case "1":
		case "春":
		case "春季":
			break;
		}
		seasonIndexZeroBased = 0;
		return true;
	}

	private static string GetSeasonTextZh(int seasonIndexZeroBased)
	{
		int num = NormalizeSeasonIndex(seasonIndexZeroBased);
		bool flag = false;
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "春", 
			1 => "夏", 
			2 => "秋", 
			_ => "冬", 
		};
		if (1 == 0)
		{
		}
		string result = text;
		bool flag2 = false;
		return result;
	}

	private static string FormatAbsDayAsGameDate(int absDay)
	{
		int daysInSeasonSafe = GetDaysInSeasonSafe();
		int daysInYearSafe = GetDaysInYearSafe();
		int num = Math.Max(0, absDay);
		int num2 = num / daysInYearSafe;
		int num3 = num % daysInYearSafe;
		int seasonIndexZeroBased = num3 / daysInSeasonSafe;
		int num4 = num3 % daysInSeasonSafe + 1;
		return $"{num2}年{GetSeasonTextZh(seasonIndexZeroBased)}第{num4}天";
	}

	public string BuildDueDateReferenceForAI()
	{
		try
		{
			int daysInSeasonSafe = GetDaysInSeasonSafe();
			int daysInYearSafe = GetDaysInYearSafe();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"换算：1季={daysInSeasonSafe}天，1年={daysInYearSafe}天。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string BuildDebtId()
	{
		try
		{
			return "D" + Guid.NewGuid().ToString("N").Substring(0, 8)
				.ToUpperInvariant();
		}
		catch
		{
			return "D" + DateTime.UtcNow.Ticks;
		}
	}

	private void SyncMerchantFactData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_merchantFacts == null)
		{
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_merchantFactStorage == null)
		{
			_merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_merchantFactStorage.Clear();
			foreach (KeyValuePair<string, MerchantFactRecord> merchantFact in _merchantFacts)
			{
				if (string.IsNullOrWhiteSpace(merchantFact.Key) || merchantFact.Value == null)
				{
					continue;
				}
				CleanupMerchantFactRecord(merchantFact.Value);
				if (merchantFact.Value.Facts != null && merchantFact.Value.Facts.Count > 0)
				{
					try
					{
						_merchantFactStorage[merchantFact.Key] = JsonConvert.SerializeObject((object)merchantFact.Value);
					}
					catch (Exception ex)
					{
						Logger.Log("RewardSystem", "[ERROR] Serialize merchant facts for " + merchantFact.Key + ": " + ex.Message);
					}
				}
			}
		}
		Dictionary<string, string> stored = (dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_merchantFactStorage) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
		dataStore.SyncData<Dictionary<string, string>>("_rewardMerchantFacts_v1", ref stored);
		_merchantFactStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored, "RewardSystem");
		if (dataStore.IsSaving)
		{
			return;
		}
		_merchantFacts.Clear();
		if (_merchantFactStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, string> item in _merchantFactStorage)
		{
			if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
			{
				continue;
			}
			try
			{
				MerchantFactRecord merchantFactRecord = JsonConvert.DeserializeObject<MerchantFactRecord>(item.Value);
				if (merchantFactRecord != null)
				{
					CleanupMerchantFactRecord(merchantFactRecord);
					if (merchantFactRecord.Facts != null && merchantFactRecord.Facts.Count > 0)
					{
						_merchantFacts[item.Key] = merchantFactRecord;
					}
				}
			}
			catch (Exception ex2)
			{
				Logger.Log("RewardSystem", "[ERROR] Deserialize merchant facts for " + item.Key + ": " + ex2.Message);
			}
		}
	}

	private void SyncTrustData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		if (_npcTrustStorage == null)
		{
			_npcTrustStorage = new Dictionary<string, int>();
		}
		if (_publicTrustStorage == null)
		{
			_publicTrustStorage = new Dictionary<string, int>();
		}
		if (dataStore.IsSaving)
		{
			_npcTrustStorage.Clear();
			foreach (KeyValuePair<string, int> item in _npcTrust)
			{
				if (!string.IsNullOrWhiteSpace(item.Key))
				{
					int num = ClampTrust(item.Value);
					if (num != 0)
					{
						_npcTrustStorage[item.Key] = num;
					}
				}
			}
			_publicTrustStorage.Clear();
			foreach (KeyValuePair<string, int> item2 in _publicTrust)
			{
				if (!string.IsNullOrWhiteSpace(item2.Key))
				{
					int num2 = ClampTrust(item2.Value);
					if (num2 != 0)
					{
						_publicTrustStorage[item2.Key] = num2;
					}
				}
			}
		}
		dataStore.SyncData<Dictionary<string, int>>("_rewardNpcTrust_v1", ref _npcTrustStorage);
		dataStore.SyncData<Dictionary<string, int>>("_rewardPublicTrust_v1", ref _publicTrustStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_npcTrust.Clear();
		if (_npcTrustStorage != null)
		{
			foreach (KeyValuePair<string, int> item3 in _npcTrustStorage)
			{
				if (!string.IsNullOrWhiteSpace(item3.Key))
				{
					int num3 = ClampTrust(item3.Value);
					if (num3 != 0)
					{
						_npcTrust[item3.Key] = num3;
					}
				}
			}
		}
		_publicTrust.Clear();
		if (_publicTrustStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item4 in _publicTrustStorage)
		{
			if (!string.IsNullOrWhiteSpace(item4.Key))
			{
				int num4 = ClampTrust(item4.Value);
				if (num4 != 0)
				{
					_publicTrust[item4.Key] = num4;
				}
			}
		}
	}

	private void SyncTradeTrustCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_tradeTrustValueCarry == null)
		{
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_tradeTrustValueCarryStorage == null)
		{
			_tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_tradeTrustValueCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _tradeTrustValueCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value > 0)
				{
					_tradeTrustValueCarryStorage[item.Key] = Math.Min(999, Math.Max(0, item.Value));
				}
			}
		}
		dataStore.SyncData<Dictionary<string, int>>("_rewardTradeTrustCarry_v1", ref _tradeTrustValueCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_tradeTrustValueCarry.Clear();
		if (_tradeTrustValueCarryStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item2 in _tradeTrustValueCarryStorage)
		{
			if (!string.IsNullOrWhiteSpace(item2.Key))
			{
				int num = Math.Min(999, Math.Max(0, item2.Value));
				if (num > 0)
				{
					_tradeTrustValueCarry[item2.Key] = num;
				}
			}
		}
	}

	private void SyncDirectTrustProgressCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_directTrustProgressCarryStorage == null)
		{
			_directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_directTrustProgressCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _directTrustProgressCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value != 0)
				{
					_directTrustProgressCarryStorage[item.Key] = item.Value;
				}
			}
		}
		dataStore.SyncData<Dictionary<string, int>>("_rewardDirectTrustProgressCarry_v1", ref _directTrustProgressCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_directTrustProgressCarry.Clear();
		if (_directTrustProgressCarryStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item2 in _directTrustProgressCarryStorage)
		{
			if (!string.IsNullOrWhiteSpace(item2.Key) && item2.Value != 0)
			{
				_directTrustProgressCarry[item2.Key] = item2.Value;
			}
		}
	}

	private void SyncSettlementTrustCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustCentiCarryStorage == null)
		{
			_settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarry == null)
		{
			_settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarryStorage == null)
		{
			_settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_settlementTrustCentiCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _settlementTrustCentiCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value != 0)
				{
					_settlementTrustCentiCarryStorage[item.Key] = item.Value;
				}
			}
			_settlementTrustSharedPublicCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item2 in _settlementTrustSharedPublicCarry)
			{
				if (!string.IsNullOrWhiteSpace(item2.Key) && item2.Value != 0)
				{
					_settlementTrustSharedPublicCarryStorage[item2.Key] = item2.Value;
				}
			}
		}
		dataStore.SyncData<Dictionary<string, int>>("_rewardSettlementTrustCentiCarry_v2", ref _settlementTrustCentiCarryStorage);
		dataStore.SyncData<Dictionary<string, int>>("_rewardSettlementTrustSharedPublicCarry_v1", ref _settlementTrustSharedPublicCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_settlementTrustCentiCarry.Clear();
		if (_settlementTrustCentiCarryStorage != null)
		{
			foreach (KeyValuePair<string, int> item3 in _settlementTrustCentiCarryStorage)
			{
				if (!string.IsNullOrWhiteSpace(item3.Key) && item3.Value != 0)
				{
					_settlementTrustCentiCarry[item3.Key] = item3.Value;
				}
			}
		}
		_settlementTrustSharedPublicCarry.Clear();
		if (_settlementTrustSharedPublicCarryStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item4 in _settlementTrustSharedPublicCarryStorage)
		{
			if (!string.IsNullOrWhiteSpace(item4.Key) && item4.Value != 0)
			{
				_settlementTrustSharedPublicCarry[item4.Key] = item4.Value;
			}
		}
	}

	private void SyncPublicTrustProgressCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_publicTrustProgressCarry == null)
		{
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_publicTrustProgressCarryStorage == null)
		{
			_publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_publicTrustProgressCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _publicTrustProgressCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value != 0)
				{
					_publicTrustProgressCarryStorage[item.Key] = item.Value;
				}
			}
		}
		dataStore.SyncData<Dictionary<string, int>>("_rewardPublicTrustProgressCarry_v2", ref _publicTrustProgressCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_publicTrustProgressCarry.Clear();
		if (_publicTrustProgressCarryStorage != null)
		{
			foreach (KeyValuePair<string, int> item2 in _publicTrustProgressCarryStorage)
			{
				if (!string.IsNullOrWhiteSpace(item2.Key) && item2.Value != 0)
				{
					_publicTrustProgressCarry[item2.Key] = item2.Value;
				}
			}
		}
		MigrateLegacySettlementSharedPublicCarryToUnifiedPool();
	}

	private static bool TryResolveSettlementByLocalPublicTrustKey(string key, out Settlement settlement)
	{
		settlement = null;
		string text = (key ?? "").Trim();
		if (!text.StartsWith("public:settlement:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text3 = text.Substring("public:settlement:".Length).Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			return false;
		}
		try
		{
			settlement = ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Settlement x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text3, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			settlement = null;
		}
		return settlement != null;
	}

	private void MigrateLegacySettlementSharedPublicCarryToUnifiedPool()
	{
		if (_settlementTrustSharedPublicCarry == null || _settlementTrustSharedPublicCarry.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item in _settlementTrustSharedPublicCarry.ToList())
		{
			if (item.Value != 0 && TryResolveSettlementByLocalPublicTrustKey(item.Key, out var settlement))
			{
				string text = BuildSettlementSharedPublicTrustKey(settlement);
				if (!string.IsNullOrWhiteSpace(text))
				{
					ApplyPublicTrustPoolDeltaByKey(text, item.Value * 1600, "legacy_settlement_public_pool_migration");
				}
			}
		}
		_settlementTrustSharedPublicCarry.Clear();
	}

	private static int ClampTrust(int value)
	{
		if (value < -100)
		{
			return -100;
		}
		if (value > 100)
		{
			return 100;
		}
		return value;
	}

	private static int ToTenLevelIndexByTrust(int trust)
	{
		double num = ((double)ClampTrust(trust) + 100.0) / 200.0;
		int num2 = (int)Math.Floor(num * 10.0) + 1;
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		return num2;
	}

	public static int GetTrustLevelIndex(int trust)
	{
		return ToTenLevelIndexByTrust(trust);
	}

	public static string GetTrustLevelText(int trust)
	{
		int num = ToTenLevelIndexByTrust(trust);
		return TrustLevelTexts[num - 1];
	}

	public static string GetTrustBehaviorText(int trust)
	{
		int num = ToTenLevelIndexByTrust(trust);
		return TrustAiBehaviorTexts[num - 1];
	}

	public static string GetTrustActionGuideText(int trust)
	{
		int num = ToTenLevelIndexByTrust(trust);
		return TrustAiActionGuideTexts[num - 1];
	}

	private static string BuildNpcTrustKey(Hero npc)
	{
		string text = (((npc != null) ? ((MBObjectBase)npc).StringId : null) ?? "").Trim().ToLower();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	private static string BuildPublicTrustKey(Hero npc)
	{
		string text = "";
		try
		{
			object obj;
			if (npc == null)
			{
				obj = null;
			}
			else
			{
				IFaction mapFaction = npc.MapFaction;
				obj = ((mapFaction != null) ? mapFaction.StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			text = ((string)obj).Trim().ToLower();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				object obj3;
				if (npc == null)
				{
					obj3 = null;
				}
				else
				{
					Clan clan = npc.Clan;
					if (clan == null)
					{
						obj3 = null;
					}
					else
					{
						Kingdom kingdom = clan.Kingdom;
						obj3 = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
					}
				}
				if (obj3 == null)
				{
					obj3 = "";
				}
				text = ((string)obj3).Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				object obj5;
				if (npc == null)
				{
					obj5 = null;
				}
				else
				{
					Clan clan2 = npc.Clan;
					obj5 = ((clan2 != null) ? ((MBObjectBase)clan2).StringId : null);
				}
				if (obj5 == null)
				{
					obj5 = "";
				}
				text = ((string)obj5).Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				object obj7;
				if (npc == null)
				{
					obj7 = null;
				}
				else
				{
					CultureObject culture = npc.Culture;
					obj7 = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
				}
				if (obj7 == null)
				{
					obj7 = "";
				}
				text = ((string)obj7).Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (((npc != null) ? ((MBObjectBase)npc).StringId : null) ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:" + text;
	}

	private static string BuildSettlementLocalPublicTrustKey(Settlement settlement)
	{
		string text = (((settlement != null) ? ((MBObjectBase)settlement).StringId : null) ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:settlement:" + text;
	}

	private static string BuildSettlementPublicTrustKey(Settlement settlement)
	{
		return BuildSettlementLocalPublicTrustKey(settlement);
	}

	private static string BuildSettlementSharedPublicTrustKey(Settlement settlement)
	{
		string text = "";
		try
		{
			object obj;
			if (settlement == null)
			{
				obj = null;
			}
			else
			{
				IFaction mapFaction = settlement.MapFaction;
				obj = ((mapFaction != null) ? mapFaction.StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			text = ((string)obj).Trim().ToLowerInvariant();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				object obj3;
				if (settlement == null)
				{
					obj3 = null;
				}
				else
				{
					Clan ownerClan = settlement.OwnerClan;
					if (ownerClan == null)
					{
						obj3 = null;
					}
					else
					{
						Kingdom kingdom = ownerClan.Kingdom;
						obj3 = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
					}
				}
				if (obj3 == null)
				{
					obj3 = "";
				}
				text = ((string)obj3).Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				object obj5;
				if (settlement == null)
				{
					obj5 = null;
				}
				else
				{
					Clan ownerClan2 = settlement.OwnerClan;
					obj5 = ((ownerClan2 != null) ? ((MBObjectBase)ownerClan2).StringId : null);
				}
				if (obj5 == null)
				{
					obj5 = "";
				}
				text = ((string)obj5).Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				object obj7;
				if (settlement == null)
				{
					obj7 = null;
				}
				else
				{
					CultureObject culture = settlement.Culture;
					obj7 = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
				}
				if (obj7 == null)
				{
					obj7 = "";
				}
				text = ((string)obj7).Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:" + text;
	}

	private static string BuildSettlementFactionPublicTrustKey(Settlement settlement)
	{
		return BuildSettlementSharedPublicTrustKey(settlement);
	}

	private static string BuildPublicTrustLabel(Hero npc)
	{
		try
		{
			object obj;
			if (npc == null)
			{
				obj = null;
			}
			else
			{
				Clan clan = npc.Clan;
				if (clan == null)
				{
					obj = null;
				}
				else
				{
					Kingdom kingdom = clan.Kingdom;
					obj = ((kingdom == null) ? null : ((object)kingdom.Name)?.ToString());
				}
			}
			string text = (string)obj;
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		try
		{
			object obj3;
			if (npc == null)
			{
				obj3 = null;
			}
			else
			{
				IFaction mapFaction = npc.MapFaction;
				obj3 = ((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString());
			}
			string text2 = (string)obj3;
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.Trim();
			}
		}
		catch
		{
		}
		try
		{
			object obj5;
			if (npc == null)
			{
				obj5 = null;
			}
			else
			{
				Clan clan2 = npc.Clan;
				obj5 = ((clan2 == null) ? null : ((object)clan2.Name)?.ToString());
			}
			string text3 = (string)obj5;
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3.Trim();
			}
		}
		catch
		{
		}
		return "其所属势力";
	}

	public int GetNpcTrust(Hero npc)
	{
		if (npc == null)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildNpcTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_npcTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetPublicTrust(Hero npc)
	{
		if (npc == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string text = BuildPublicTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetEffectiveTrust(Hero npc)
	{
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		return ClampTrust(npcTrust + publicTrust);
	}

	public int GetSettlementMerchantTrust(Settlement settlement, SettlementMerchantKind kind)
	{
		if (settlement == null || kind == SettlementMerchantKind.None)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_npcTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	private int AdjustSettlementMerchantTrust(Settlement settlement, SettlementMerchantKind kind, int personalDelta, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || kind == SettlementMerchantKind.None || personalDelta == 0)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int settlementMerchantTrust = GetSettlementMerchantTrust(settlement, kind);
		int num = ApplyDirectTrustDeltaUnits(text, settlementMerchantTrust, personalDelta, out appliedUnits);
		int num2 = ClampTrust(settlementMerchantTrust + num);
		if (num2 == 0)
		{
			_npcTrust.Remove(text);
		}
		else
		{
			_npcTrust[text] = num2;
		}
		int num3 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "merchant") + "_public_pool");
		Logger.Log("Trust", $"settlement={((MBObjectBase)settlement).StringId} market={kind} reason={reason} trust={settlementMerchantTrust}->{num2} rawDelta={personalDelta} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={num3}");
		return num3;
	}

	public string BuildTrustStatusInlineForAI(Hero npc)
	{
		if (npc == null)
		{
			return "综合信任 0（中性观望，6/10）";
		}
		int effectiveTrust = GetEffectiveTrust(npc);
		int trustLevelIndex = GetTrustLevelIndex(effectiveTrust);
		return $"综合信任 {effectiveTrust}（{GetTrustLevelText(effectiveTrust)}，{trustLevelIndex}/10）";
	}

	public string BuildTrustPromptForAI(Hero npc)
	{
		int effectiveTrust = GetEffectiveTrust(npc);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("本级语义：" + GetTrustBehaviorText(effectiveTrust));
		stringBuilder.AppendLine("本级信用规则：" + GetTrustActionGuideText(effectiveTrust));
		stringBuilder.AppendLine("价值口径：总价值=第纳尔金额+物品估值（guidePrice * 数量）。");
		return stringBuilder.ToString().TrimEnd();
	}

	private static Kingdom ResolveKingdomByTag(string kingdomToken, Hero giver)
	{
		try
		{
			string text = (kingdomToken ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || text.Equals("self", StringComparison.OrdinalIgnoreCase) || text.Equals("npc", StringComparison.OrdinalIgnoreCase) || text.Equals("current", StringComparison.OrdinalIgnoreCase) || text.Equals("auto", StringComparison.OrdinalIgnoreCase))
			{
				object result;
				if (giver == null)
				{
					result = null;
				}
				else
				{
					Clan clan = giver.Clan;
					result = ((clan != null) ? clan.Kingdom : null);
				}
				return (Kingdom)result;
			}
			MBReadOnlyList<Kingdom> all = Kingdom.All;
			if (all == null)
			{
				object result2;
				if (giver == null)
				{
					result2 = null;
				}
				else
				{
					Clan clan2 = giver.Clan;
					result2 = ((clan2 != null) ? clan2.Kingdom : null);
				}
				return (Kingdom)result2;
			}
			for (int i = 0; i < ((List<Kingdom>)(object)all).Count; i++)
			{
				Kingdom val = ((List<Kingdom>)(object)all)[i];
				if (val != null && string.Equals((((MBObjectBase)val).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					return val;
				}
			}
			for (int j = 0; j < ((List<Kingdom>)(object)all).Count; j++)
			{
				Kingdom val2 = ((List<Kingdom>)(object)all)[j];
				if (val2 != null)
				{
					string text2 = (((object)val2.Name)?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
					{
						return val2;
					}
				}
			}
		}
		catch
		{
		}
		object result3;
		if (giver == null)
		{
			result3 = null;
		}
		else
		{
			Clan clan3 = giver.Clan;
			result3 = ((clan3 != null) ? clan3.Kingdom : null);
		}
		return (Kingdom)result3;
	}

	private static bool IsPlayerWarsCompatibleWithKingdom(Kingdom offerKingdom)
	{
		try
		{
			if (offerKingdom == null || Clan.PlayerClan == null)
			{
				return false;
			}
			float num = 0f;
			try
			{
				Campaign current = Campaign.Current;
				float? obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					GameModels models = current.Models;
					if (models == null)
					{
						obj = null;
					}
					else
					{
						DiplomacyModel diplomacyModel = models.DiplomacyModel;
						obj = ((diplomacyModel != null) ? new float?(diplomacyModel.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(offerKingdom)) : ((float?)null));
					}
				}
				float? num2 = obj;
				num = num2.GetValueOrDefault();
			}
			catch
			{
				num = 0f;
			}
			List<IFaction> list = new List<IFaction>();
			List<IFaction> list2 = new List<IFaction>();
			try
			{
				MBReadOnlyList<Kingdom> all = Kingdom.All;
				if (all != null)
				{
					for (int i = 0; i < ((List<Kingdom>)(object)all).Count; i++)
					{
						Kingdom val = ((List<Kingdom>)(object)all)[i];
						if (val != null && Clan.PlayerClan.MapFaction != null && Clan.PlayerClan.MapFaction.IsAtWarWith((IFaction)(object)val) && val.CurrentTotalStrength > num)
						{
							list.Add((IFaction)(object)val);
						}
					}
					for (int j = 0; j < ((List<Kingdom>)(object)all).Count; j++)
					{
						Kingdom val2 = ((List<Kingdom>)(object)all)[j];
						if (val2 != null && offerKingdom.IsAtWarWith((IFaction)(object)val2))
						{
							list2.Add((IFaction)(object)val2);
						}
					}
				}
			}
			catch
			{
			}
			if (list.Count <= 0)
			{
				return true;
			}
			int num3 = list.Intersect(list2).Count();
			return num3 == list.Count;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanPlayerOfferMercenaryServiceCompat(Kingdom offerKingdom)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || offerKingdom == null)
			{
				return false;
			}
			int num = 1;
			try
			{
				Campaign current = Campaign.Current;
				int? obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					GameModels models = current.Models;
					if (models == null)
					{
						obj = null;
					}
					else
					{
						ClanTierModel clanTierModel = models.ClanTierModel;
						obj = ((clanTierModel != null) ? new int?(clanTierModel.MercenaryEligibleTier) : ((int?)null));
					}
				}
				num = obj ?? 1;
			}
			catch
			{
				num = 1;
			}
			int num2 = 0;
			try
			{
				Campaign current2 = Campaign.Current;
				int? obj3;
				if (current2 == null)
				{
					obj3 = null;
				}
				else
				{
					GameModels models2 = current2.Models;
					if (models2 == null)
					{
						obj3 = null;
					}
					else
					{
						DiplomacyModel diplomacyModel = models2.DiplomacyModel;
						obj3 = ((diplomacyModel != null) ? new int?(diplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom) : ((int?)null));
					}
				}
				int? num3 = obj3;
				num2 = num3.GetValueOrDefault();
			}
			catch
			{
				num2 = 0;
			}
			bool flag = playerClan.Kingdom == null;
			bool flag2 = !playerClan.IsAtWarWith((IFaction)(object)offerKingdom);
			bool flag3 = playerClan.Tier >= num;
			bool flag4 = offerKingdom.Leader != null && offerKingdom.Leader.GetRelationWithPlayer() >= (float)num2;
			bool flag5 = playerClan.Settlements == null || ((List<Settlement>)(object)playerClan.Settlements).Count <= 0;
			bool flag6 = IsPlayerWarsCompatibleWithKingdom(offerKingdom);
			return flag && flag2 && flag3 && flag4 && flag5 && flag6;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanPlayerOfferVassalageCompat(Kingdom offerKingdom)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || offerKingdom == null)
			{
				return false;
			}
			int num = 2;
			try
			{
				Campaign current = Campaign.Current;
				int? obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					GameModels models = current.Models;
					if (models == null)
					{
						obj = null;
					}
					else
					{
						ClanTierModel clanTierModel = models.ClanTierModel;
						obj = ((clanTierModel != null) ? new int?(clanTierModel.VassalEligibleTier) : ((int?)null));
					}
				}
				num = obj ?? 2;
			}
			catch
			{
				num = 2;
			}
			int num2 = 0;
			try
			{
				Campaign current2 = Campaign.Current;
				int? obj3;
				if (current2 == null)
				{
					obj3 = null;
				}
				else
				{
					GameModels models2 = current2.Models;
					if (models2 == null)
					{
						obj3 = null;
					}
					else
					{
						DiplomacyModel diplomacyModel = models2.DiplomacyModel;
						obj3 = ((diplomacyModel != null) ? new int?(diplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom) : ((int?)null));
					}
				}
				int? num3 = obj3;
				num2 = num3.GetValueOrDefault();
			}
			catch
			{
				num2 = 0;
			}
			bool flag = playerClan.Kingdom == null || playerClan.IsUnderMercenaryService;
			bool flag2 = !playerClan.IsAtWarWith((IFaction)(object)offerKingdom);
			bool flag3 = playerClan.Tier >= num;
			bool flag4 = !offerKingdom.IsEliminated;
			bool flag5 = offerKingdom.Leader != null && offerKingdom.Leader.GetRelationWithPlayer() >= (float)num2;
			bool flag6 = IsPlayerWarsCompatibleWithKingdom(offerKingdom);
			return flag && flag2 && flag3 && flag4 && flag5 && flag6;
		}
		catch
		{
			return false;
		}
	}

	private bool TryApplyKingdomServiceAction(Hero giver, string serviceType, string kingdomToken, out string statusText)
	{
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
		//IL_0559: Unknown result type (might be due to invalid IL or missing references)
		statusText = "";
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null)
			{
				statusText = "执行失败：未找到玩家家族。";
				return false;
			}
			string text = (serviceType ?? "").Trim().ToUpperInvariant();
			if (text == "LEAVE")
			{
				Kingdom kingdom = playerClan.Kingdom;
				if (kingdom == null)
				{
					statusText = "执行失败：玩家当前未加入任何势力，无需退出。";
					return false;
				}
				if (playerClan.IsUnderMercenaryService)
				{
					ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(playerClan, true);
					statusText = $"执行成功：玩家已结束与 {kingdom.Name} 的雇佣兵契约。";
					return true;
				}
				ChangeKingdomAction.ApplyByLeaveKingdom(playerClan, true);
				statusText = $"执行成功：玩家已退出 {kingdom.Name}，不再是其正式封臣。";
				return true;
			}
			Kingdom val = ResolveKingdomByTag(kingdomToken, giver);
			if (val == null || val.IsEliminated)
			{
				statusText = "执行失败：目标势力无效（" + kingdomToken + "）。";
				return false;
			}
			object obj;
			if (giver == null)
			{
				obj = null;
			}
			else
			{
				Clan clan = giver.Clan;
				obj = ((clan != null) ? clan.Kingdom : null);
			}
			Kingdom val2 = (Kingdom)obj;
			if (val2 != null && val2 != val)
			{
				statusText = $"执行失败：{giver.Name} 并非 {val.Name} 成员，不能代表该势力授予身份。";
				return false;
			}
			if (text == "MERCENARY")
			{
				if (giver == null || giver.Clan == null || giver.Clan.Kingdom != val)
				{
					statusText = "执行失败：雇佣兵招募必须由目标势力封臣发起（目标势力=" + ((MBObjectBase)val).StringId + "）。";
					return false;
				}
				if (giver.Clan.IsUnderMercenaryService)
				{
					statusText = "执行失败：当前对话对象并非该势力正式封臣，不能签订雇佣兵契约。";
					return false;
				}
				int effectiveTrust = GetEffectiveTrust(giver);
				if (effectiveTrust < 5)
				{
					statusText = $"执行失败：综合信任不足，当前 {effectiveTrust}，至少需要 {5} 才能以雇佣兵身份加入。";
					return false;
				}
				int num = 1;
				try
				{
					Campaign current = Campaign.Current;
					int? obj2;
					if (current == null)
					{
						obj2 = null;
					}
					else
					{
						GameModels models = current.Models;
						if (models == null)
						{
							obj2 = null;
						}
						else
						{
							ClanTierModel clanTierModel = models.ClanTierModel;
							obj2 = ((clanTierModel != null) ? new int?(clanTierModel.MercenaryEligibleTier) : ((int?)null));
						}
					}
					num = obj2 ?? 1;
				}
				catch
				{
					num = 1;
				}
				if (playerClan.Tier < num)
				{
					statusText = $"执行失败：玩家家族等级不足，当前 {playerClan.Tier}，至少需要 {num} 才能作为雇佣兵加入。";
					return false;
				}
				if (playerClan.Kingdom != null && !playerClan.IsUnderMercenaryService)
				{
					statusText = "执行失败：玩家已是某王国正式封臣，不能再作为雇佣兵加入。";
					return false;
				}
				if (!CanPlayerOfferMercenaryServiceCompat(val))
				{
					statusText = "执行失败：不满足原版雇佣兵加入条件（势力=" + ((MBObjectBase)val).StringId + "）。";
					return false;
				}
				int num2 = 50;
				try
				{
					Campaign current2 = Campaign.Current;
					int? obj4;
					if (current2 == null)
					{
						obj4 = null;
					}
					else
					{
						GameModels models2 = current2.Models;
						if (models2 == null)
						{
							obj4 = null;
						}
						else
						{
							MinorFactionsModel minorFactionsModel = models2.MinorFactionsModel;
							obj4 = ((minorFactionsModel != null) ? new int?(minorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(playerClan, val, false)) : ((int?)null));
						}
					}
					num2 = obj4 ?? 50;
				}
				catch
				{
					num2 = 50;
				}
				ChangeKingdomAction.ApplyByJoinFactionAsMercenary(playerClan, val, default(CampaignTime), num2, true);
				statusText = $"执行成功：玩家已作为雇佣兵加入 {val.Name}（KingdomId={((MBObjectBase)val).StringId}）。";
				return true;
			}
			if (text == "VASSAL")
			{
				if (val.Leader == null || giver == null || giver != val.Leader)
				{
					Hero leader = val.Leader;
					string arg = ((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "该势力领袖";
					statusText = $"执行失败：封臣宣誓必须由 {val.Name} 的势力领袖 {arg} 亲自确认。请前往与其对话。";
					return false;
				}
				int effectiveTrust2 = GetEffectiveTrust(giver);
				if (effectiveTrust2 < 20)
				{
					statusText = $"执行失败：综合信任不足，当前 {effectiveTrust2}，至少需要 {20} 才能成为封臣。";
					return false;
				}
				int num3 = 2;
				try
				{
					Campaign current3 = Campaign.Current;
					int? obj6;
					if (current3 == null)
					{
						obj6 = null;
					}
					else
					{
						GameModels models3 = current3.Models;
						if (models3 == null)
						{
							obj6 = null;
						}
						else
						{
							ClanTierModel clanTierModel2 = models3.ClanTierModel;
							obj6 = ((clanTierModel2 != null) ? new int?(clanTierModel2.VassalEligibleTier) : ((int?)null));
						}
					}
					num3 = obj6 ?? 2;
				}
				catch
				{
					num3 = 2;
				}
				if (playerClan.Tier < num3)
				{
					statusText = $"执行失败：玩家家族等级不足，当前 {playerClan.Tier}，至少需要 {num3} 才能成为封臣。";
					return false;
				}
				if (playerClan.Kingdom == val && !playerClan.IsUnderMercenaryService)
				{
					statusText = $"执行跳过：玩家已是 {val.Name} 的正式封臣。";
					return false;
				}
				if (!CanPlayerOfferVassalageCompat(val))
				{
					statusText = "执行失败：不满足原版封臣加入条件（势力=" + ((MBObjectBase)val).StringId + "）。";
					return false;
				}
				ChangeKingdomAction.ApplyByJoinToKingdom(playerClan, val, default(CampaignTime), true);
				statusText = $"执行成功：玩家已加入 {val.Name} 成为正式封臣（KingdomId={((MBObjectBase)val).StringId}）。";
				return true;
			}
			statusText = "执行失败：未知势力效力类型 " + serviceType + "。";
			return false;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	private int AdjustTrust(Hero npc, int personalDelta, int publicDelta, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (npc == null)
		{
			return 0;
		}
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		int num = npcTrust;
		int num2 = publicTrust;
		string publicTrustKey = BuildPublicTrustKey(npc);
		if (personalDelta != 0)
		{
			if (_npcTrust == null)
			{
				_npcTrust = new Dictionary<string, int>();
			}
			string text = BuildNpcTrustKey(npc);
			if (!string.IsNullOrWhiteSpace(text))
			{
				int num3 = ApplyDirectTrustDeltaUnits(text, npcTrust, personalDelta, out appliedUnits);
				num = ClampTrust(npcTrust + num3);
				if (num == 0)
				{
					_npcTrust.Remove(text);
				}
				else
				{
					_npcTrust[text] = num;
				}
			}
		}
		int num4 = 0;
		if (personalDelta != 0)
		{
			num4 += ApplyPublicTrustPoolDeltaByKey(publicTrustKey, appliedUnits, (reason ?? "external") + "_public_pool");
			num2 = GetPublicTrust(npc);
		}
		if (publicDelta != 0)
		{
			num4 += AdjustPublicTrustByKey(publicTrustKey, publicDelta, (reason ?? "external") + "_direct");
			num2 = GetPublicTrust(npc);
		}
		int num5 = ClampTrust(npcTrust + publicTrust);
		int num6 = ClampTrust(num + num2);
		Logger.Log("Trust", $"npc={((MBObjectBase)npc).StringId} reason={reason} personal={npcTrust}->{num} rawDelta={personalDelta} appliedDelta={FormatTrustUnits(appliedUnits)} public={publicTrust}->{num2} deltaPublic={num4} requestedPublicDelta={publicDelta} effective={num5}->{num6}");
		Logger.Obs("Trust", "change", new Dictionary<string, object>
		{
			["npcId"] = ((MBObjectBase)npc).StringId ?? "",
			["reason"] = reason ?? "",
			["personalBefore"] = npcTrust,
			["personalAfter"] = num,
			["publicBefore"] = publicTrust,
			["publicAfter"] = num2,
			["effectiveBefore"] = num5,
			["effectiveAfter"] = num6,
			["personalDelta"] = personalDelta,
			["appliedPersonalDelta"] = FormatTrustUnits(appliedUnits),
			["publicDelta"] = num4,
			["requestedPublicDelta"] = publicDelta
		});
		Logger.Metric("trust.change");
		return num4;
	}

	public void AdjustTrustForExternal(Hero npc, int personalDelta, int publicDelta, string reason = "external")
	{
		AdjustTrust(npc, personalDelta, publicDelta, reason ?? "external", out var _);
	}

	public void AdjustSettlementMerchantTrustForExternal(Settlement settlement, SettlementMerchantKind kind, int personalDelta, string reason = "external")
	{
		AdjustSettlementMerchantTrust(settlement, kind, personalDelta, reason ?? "external", out var _);
	}

	public int GetSettlementLocalPublicTrust(Settlement settlement)
	{
		if (settlement == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetSettlementPublicTrust(Settlement settlement)
	{
		return GetSettlementLocalPublicTrust(settlement);
	}

	public int GetSettlementSharedPublicTrust(Settlement settlement)
	{
		if (settlement == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementSharedPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetSettlementFactionPublicTrust(Settlement settlement)
	{
		return GetSettlementSharedPublicTrust(settlement);
	}

	public int GetSettlementMerchantEffectiveTrust(Settlement settlement, SettlementMerchantKind kind)
	{
		return ClampTrust(GetSettlementMerchantTrust(settlement, kind) + GetSettlementLocalPublicTrust(settlement) + GetSettlementSharedPublicTrust(settlement));
	}

	public void AdjustSettlementLocalPublicTrustForExternal(Settlement settlement, int publicDelta, string reason = "external")
	{
		AdjustSettlementLocalTrustInternal(settlement, publicDelta, reason);
	}

	public void AdjustSettlementPublicTrustForExternal(Settlement settlement, int publicDelta, string reason = "external")
	{
		AdjustSettlementLocalPublicTrustForExternal(settlement, publicDelta, reason);
	}

	private void AdjustSettlementSharedPublicTrust(Settlement settlement, int publicDelta, string reason)
	{
		if (settlement != null && publicDelta != 0)
		{
			string text = BuildSettlementSharedPublicTrustKey(settlement);
			if (!string.IsNullOrWhiteSpace(text))
			{
				AdjustPublicTrustByKey(text, publicDelta, reason);
			}
		}
	}

	private void AdjustSettlementFactionPublicTrust(Settlement settlement, int publicDelta, string reason)
	{
		AdjustSettlementSharedPublicTrust(settlement, publicDelta, reason);
	}

	private void AdjustRelationWithPlayer(Hero npc, int delta, string reason)
	{
		if (npc == null || delta == 0 || Hero.MainHero == null)
		{
			return;
		}
		try
		{
			int relation = npc.GetRelation(Hero.MainHero);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, npc, delta, true);
			int relation2 = npc.GetRelation(Hero.MainHero);
			Logger.Log("Trust", $"npc={((MBObjectBase)npc).StringId} relation_reason={reason} relation={relation}->{relation2} delta={delta}");
			Logger.Obs("Relation", "change", new Dictionary<string, object>
			{
				["npcId"] = ((MBObjectBase)npc).StringId ?? "",
				["reason"] = reason ?? "",
				["before"] = relation,
				["after"] = relation2,
				["delta"] = delta
			});
			Logger.Metric("relation.change");
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[WARN] relation adjust failed: " + ex.Message);
			Logger.Obs("Relation", "change_error", new Dictionary<string, object>
			{
				["npcId"] = ((MBObjectBase)npc).StringId ?? "",
				["reason"] = reason ?? "",
				["delta"] = delta,
				["message"] = ex.Message
			});
			Logger.Metric("relation.change", ok: false);
		}
	}

	private void OnDailyTick()
	{
		//IL_0634: Unknown result type (might be due to invalid IL or missing references)
		//IL_0639: Unknown result type (might be due to invalid IL or missing references)
		//IL_0643: Expected O, but got Unknown
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Expected O, but got Unknown
		try
		{
			if (_debts == null || _debts.Count <= 0)
			{
				return;
			}
			float nowCampaignDay = GetNowCampaignDay();
			int campaignDayIndex = GetCampaignDayIndex();
			CleanupPendingPlayerTransfers(campaignDayIndex);
			List<string> list = _debts.Keys.ToList();
			foreach (string item in list)
			{
				if (string.IsNullOrWhiteSpace(item) || !_debts.TryGetValue(item, out var value) || value == null)
				{
					continue;
				}
				NormalizeDebtRecord(value);
				if (value.DebtLines == null || value.DebtLines.Count <= 0)
				{
					continue;
				}
				Hero val = null;
				try
				{
					val = Hero.Find(item);
				}
				catch
				{
					val = null;
				}
				int appliedUnits;
				if (val == null)
				{
					if (!TryParseSettlementMerchantDebtKey(item, out var settlementId, out var kind))
					{
						continue;
					}
					Settlement val2 = ResolveSettlementById(settlementId);
					if (val2 == null)
					{
						continue;
					}
					for (int i = 0; i < value.DebtLines.Count; i++)
					{
						DebtRecord.DebtLine debtLine = value.DebtLines[i];
						if (debtLine == null || debtLine.RemainingAmount <= 0 || debtLine.IsDueUnlimited || (!debtLine.IsGold && debtLine.IsItemUnavailableDeclared) || debtLine.DueDay <= 0f || nowCampaignDay <= debtLine.DueDay + 0.01f)
						{
							continue;
						}
						int num = Math.Max(0, (int)Math.Floor(nowCampaignDay - debtLine.DueDay));
						if (num > 0 && num <= 7 && !(debtLine.BestPreDueCoverage >= 0.95f) && debtLine.OverduePenaltyDaysApplied < 7 && debtLine.LastOverduePenaltyDay != campaignDayIndex)
						{
							int num2 = NormalizeLlmPenaltyValue(debtLine.OverdueTrustPenaltyPerDay);
							bool flag = num2 > 0;
							int num3 = 0;
							int num4 = 0;
							if (!flag)
							{
								num3 = EstimateDebtLineRemainingValueForSettlement(val2, debtLine);
								num4 = ComputeDailyOverduePenaltySeverity(debtLine.InitialAmount, debtLine.RemainingAmount, num3);
								num2 = num4;
							}
							int num5 = 0;
							if (num2 > 0)
							{
								num5 = AdjustSettlementMerchantTrust(val2, kind, -num2, flag ? "merchant_overdue_daily_penalty_preset" : "merchant_overdue_daily_penalty_fallback", out appliedUnits);
							}
							debtLine.OverduePenaltyDaysApplied++;
							debtLine.LastOverduePenaltyDay = campaignDayIndex;
							Logger.Log("Trust", string.Format("[OverduePenalty] settlement={0} market={1} debtId={2} mode={3} value={4} trust={5} public={6} fallback={7} day={8}/{9}", ((MBObjectBase)val2).StringId, kind, debtLine.DebtId, flag ? "preset" : "fallback", num3, num2, num5, num4, debtLine.OverduePenaltyDaysApplied, 7));
						}
					}
					NormalizeDebtRecord(value);
					if (!HasDebtContent(value))
					{
						_debts.Remove(item);
						continue;
					}
					string text = BuildDailyMerchantDebtReminderText(val2, kind, value);
					if (!string.IsNullOrWhiteSpace(text))
					{
						InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(4294945331u)));
					}
					continue;
				}
				for (int j = 0; j < value.DebtLines.Count; j++)
				{
					DebtRecord.DebtLine debtLine2 = value.DebtLines[j];
					if (debtLine2 == null || debtLine2.RemainingAmount <= 0 || debtLine2.IsDueUnlimited || (!debtLine2.IsGold && debtLine2.IsItemUnavailableDeclared) || debtLine2.DueDay <= 0f || nowCampaignDay <= debtLine2.DueDay + 0.01f)
					{
						continue;
					}
					int num6 = Math.Max(0, (int)Math.Floor(nowCampaignDay - debtLine2.DueDay));
					if (num6 > 0 && num6 <= 7 && !(debtLine2.BestPreDueCoverage >= 0.95f) && debtLine2.OverduePenaltyDaysApplied < 7 && debtLine2.LastOverduePenaltyDay != campaignDayIndex)
					{
						int num7 = NormalizeLlmPenaltyValue(debtLine2.OverdueTrustPenaltyPerDay);
						int num8 = NormalizeLlmPenaltyValue(debtLine2.OverdueRelationPenaltyPerDay);
						bool flag2 = num7 > 0 || num8 > 0;
						int num9 = 0;
						int num10 = 0;
						if (!flag2)
						{
							num9 = EstimateDebtLineRemainingValue(val, debtLine2);
							num10 = ComputeDailyOverduePenaltySeverity(debtLine2.InitialAmount, debtLine2.RemainingAmount, num9);
							num7 = num10;
							num8 = num10;
						}
						int num11 = 0;
						if (num7 > 0)
						{
							num11 = AdjustTrust(val, -num7, 0, flag2 ? "overdue_daily_penalty_preset" : "overdue_daily_penalty_fallback", out appliedUnits);
						}
						if (num8 > 0)
						{
							AdjustRelationWithPlayer(val, -num8, flag2 ? "overdue_daily_penalty_preset" : "overdue_daily_penalty_fallback");
						}
						debtLine2.OverduePenaltyDaysApplied++;
						debtLine2.LastOverduePenaltyDay = campaignDayIndex;
						Logger.Log("Trust", string.Format("[OverduePenalty] npc={0} debtId={1} mode={2} value={3} trustPersonal={4} trustPublic={5} relation={6} fallback={7} day={8}/{9}", ((MBObjectBase)val).StringId, debtLine2.DebtId, flag2 ? "preset" : "fallback", num9, num7, num11, num8, num10, debtLine2.OverduePenaltyDaysApplied, 7));
					}
				}
				NormalizeDebtRecord(value);
				if (!HasDebtContent(value))
				{
					_debts.Remove(item);
					continue;
				}
				string text2 = BuildDailyDebtReminderText(val, value);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage(text2, Color.FromUint(4294945331u)));
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[WARN] OnDailyTick overdue penalty failed: " + ex.Message);
		}
	}

	private void NormalizeDebtRecord(DebtRecord rec)
	{
		if (rec == null)
		{
			return;
		}
		float nowCampaignDay = GetNowCampaignDay();
		if (rec.DebtLines == null)
		{
			rec.DebtLines = new List<DebtRecord.DebtLine>();
		}
		if (rec.OwedItems == null)
		{
			rec.OwedItems = new Dictionary<string, int>();
		}
		if (rec.DebtLines.Count == 0)
		{
			if (rec.OwedGold > 0)
			{
				float num = ((rec.CreatedDay > 0f) ? rec.CreatedDay : nowCampaignDay);
				float dueDay = ((rec.DueDay > 0f) ? rec.DueDay : (num + 1f));
				rec.DebtLines.Add(new DebtRecord.DebtLine
				{
					DebtId = BuildDebtId(),
					IsGold = true,
					ItemId = null,
					IsDueUnlimited = false,
					IsItemUnavailableDeclared = false,
					InitialAmount = rec.OwedGold,
					RemainingAmount = rec.OwedGold,
					CreatedDay = num,
					DueDay = dueDay,
					BestPreDueCoverage = 0f,
					OnTimePenaltyTierApplied = 0,
					OverduePenaltyDaysApplied = 0,
					LastOverduePenaltyDay = -1,
					OverdueTrustPenaltyPerDay = 0,
					OverdueRelationPenaltyPerDay = 0,
					CompensationUnitPrice = 0,
					CompensationGoldCredit = 0
				});
			}
			foreach (KeyValuePair<string, int> owedItem in rec.OwedItems)
			{
				if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
				{
					float num2 = ((rec.CreatedDay > 0f) ? rec.CreatedDay : nowCampaignDay);
					float dueDay2 = ((rec.DueDay > 0f) ? rec.DueDay : (num2 + 1f));
					rec.DebtLines.Add(new DebtRecord.DebtLine
					{
						DebtId = BuildDebtId(),
						IsGold = false,
						ItemId = owedItem.Key,
						IsDueUnlimited = false,
						IsItemUnavailableDeclared = false,
						InitialAmount = owedItem.Value,
						RemainingAmount = owedItem.Value,
						CreatedDay = num2,
						DueDay = dueDay2,
						BestPreDueCoverage = 0f,
						OnTimePenaltyTierApplied = 0,
						OverduePenaltyDaysApplied = 0,
						LastOverduePenaltyDay = -1,
						OverdueTrustPenaltyPerDay = 0,
						OverdueRelationPenaltyPerDay = 0,
						CompensationUnitPrice = 0,
						CompensationGoldCredit = 0
					});
				}
			}
		}
		List<DebtRecord.DebtLine> list = new List<DebtRecord.DebtLine>();
		for (int i = 0; i < rec.DebtLines.Count; i++)
		{
			DebtRecord.DebtLine debtLine = rec.DebtLines[i];
			if (debtLine == null)
			{
				continue;
			}
			debtLine.RemainingAmount = Math.Max(0, debtLine.RemainingAmount);
			if (debtLine.RemainingAmount > 0 && (debtLine.IsGold || !string.IsNullOrWhiteSpace(debtLine.ItemId)))
			{
				if (string.IsNullOrWhiteSpace(debtLine.DebtId))
				{
					debtLine.DebtId = BuildDebtId();
				}
				if (debtLine.InitialAmount <= 0)
				{
					debtLine.InitialAmount = debtLine.RemainingAmount;
				}
				if (debtLine.InitialAmount < debtLine.RemainingAmount)
				{
					debtLine.InitialAmount = debtLine.RemainingAmount;
				}
				if (debtLine.CreatedDay <= 0f)
				{
					debtLine.CreatedDay = nowCampaignDay;
				}
				if (debtLine.IsGold)
				{
					debtLine.IsItemUnavailableDeclared = false;
				}
				if (debtLine.IsDueUnlimited)
				{
					debtLine.DueDay = 0f;
				}
				else if (debtLine.DueDay <= 0f)
				{
					debtLine.DueDay = debtLine.CreatedDay + 1f;
				}
				debtLine.BestPreDueCoverage = Clamp01(debtLine.BestPreDueCoverage);
				debtLine.OnTimePenaltyTierApplied = Math.Max(0, Math.Min(5, debtLine.OnTimePenaltyTierApplied));
				debtLine.OverduePenaltyDaysApplied = Math.Max(0, Math.Min(7, debtLine.OverduePenaltyDaysApplied));
				if (debtLine.LastOverduePenaltyDay < -1)
				{
					debtLine.LastOverduePenaltyDay = -1;
				}
				debtLine.OverdueTrustPenaltyPerDay = NormalizeLlmPenaltyValue(debtLine.OverdueTrustPenaltyPerDay);
				debtLine.OverdueRelationPenaltyPerDay = NormalizeLlmPenaltyValue(debtLine.OverdueRelationPenaltyPerDay);
				debtLine.CompensationUnitPrice = Math.Max(0, debtLine.CompensationUnitPrice);
				debtLine.CompensationGoldCredit = Math.Max(0, debtLine.CompensationGoldCredit);
				list.Add(debtLine);
			}
		}
		rec.DebtLines = list;
		rec.OwedGold = 0;
		rec.OwedItems = new Dictionary<string, int>();
		float num3 = 0f;
		float num4 = 0f;
		for (int j = 0; j < rec.DebtLines.Count; j++)
		{
			DebtRecord.DebtLine debtLine2 = rec.DebtLines[j];
			if (debtLine2 == null || debtLine2.RemainingAmount <= 0)
			{
				continue;
			}
			if (debtLine2.IsGold)
			{
				rec.OwedGold += debtLine2.RemainingAmount;
			}
			else
			{
				string text = debtLine2.ItemId ?? "";
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				if (rec.OwedItems.TryGetValue(text, out var value))
				{
					rec.OwedItems[text] = value + debtLine2.RemainingAmount;
				}
				else
				{
					rec.OwedItems[text] = debtLine2.RemainingAmount;
				}
			}
			if (num3 <= 0f || debtLine2.CreatedDay < num3)
			{
				num3 = debtLine2.CreatedDay;
			}
			if (!debtLine2.IsDueUnlimited && debtLine2.DueDay > 0f && (num4 <= 0f || debtLine2.DueDay < num4))
			{
				num4 = debtLine2.DueDay;
			}
		}
		rec.CreatedDay = num3;
		rec.DueDay = num4;
		if (!HasDebtContent(rec))
		{
			rec.CreatedDay = 0f;
			rec.DueDay = 0f;
		}
	}

	private static string BuildDebtDueStatusText(float dueDay, bool isDueUnlimited = false)
	{
		if (isDueUnlimited)
		{
			return "还款期限：无限期（债务仍有效）";
		}
		if (dueDay <= 0f)
		{
			return "";
		}
		float nowCampaignDay = GetNowCampaignDay();
		float num = dueDay - nowCampaignDay;
		int absDay = ToDisplayDay(dueDay);
		string text = FormatAbsDayAsGameDate(absDay);
		if (num > 0.01f)
		{
			int num2 = Math.Max(1, (int)Math.Ceiling(num));
			return $"还款期限：约 {num2} 天内（截止 {text}）";
		}
		if (num >= -0.01f)
		{
			return "还款期限：今日到期（" + text + "）";
		}
		int num3 = Math.Max(1, (int)Math.Ceiling(0f - num));
		return $"还款期限：已逾期 {num3} 天（截止 {text}）";
	}

	private int EstimateDebtLineRemainingValue(Hero npc, DebtRecord.DebtLine line)
	{
		if (line == null || line.RemainingAmount <= 0)
		{
			return 0;
		}
		if (line.IsGold)
		{
			return Math.Max(0, line.RemainingAmount);
		}
		if (string.IsNullOrWhiteSpace(line.ItemId))
		{
			return 0;
		}
		int val = Math.Max(1, line.CompensationUnitPrice);
		if (line.CompensationUnitPrice <= 0)
		{
			ItemObject item = ResolveItemById(line.ItemId);
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(npc, item);
			val = Math.Max(1, guidePriceForItemNearHero.UnitPrice);
		}
		long num = (long)Math.Max(0, line.RemainingAmount) * (long)Math.Max(1, val);
		if (num <= 0)
		{
			return 0;
		}
		if (num > int.MaxValue)
		{
			return int.MaxValue;
		}
		return (int)num;
	}

	private int EstimateDebtLineRemainingValueForSettlement(Settlement settlement, DebtRecord.DebtLine line)
	{
		if (line == null || line.RemainingAmount <= 0)
		{
			return 0;
		}
		if (line.IsGold)
		{
			return Math.Max(0, line.RemainingAmount);
		}
		ItemObject val = ResolveItemById(line.ItemId);
		int val2 = Math.Max(1, line.CompensationUnitPrice);
		if (line.CompensationUnitPrice <= 0)
		{
			try
			{
				val2 = ((settlement == null || val == null || !TryGetSettlementBuyPrice(settlement, val, out var price)) ? Math.Max(1, (val == null) ? 1 : val.Value) : Math.Max(1, price));
			}
			catch
			{
				val2 = Math.Max(1, (val == null) ? 1 : val.Value);
			}
		}
		long num = (long)Math.Max(0, line.RemainingAmount) * (long)Math.Max(1, val2);
		if (num > int.MaxValue)
		{
			return int.MaxValue;
		}
		return Math.Max(0, (int)num);
	}

	private string BuildDailyDebtReminderText(Hero npc, DebtRecord rec, int maxLines = 2)
	{
		if (npc == null || rec == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec))
		{
			return string.Empty;
		}
		List<DebtRecord.DebtLine> list = (from x in rec.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.RemainingAmount > 0)
			orderby x.IsDueUnlimited ? 1 : 0, x.DueDay, x.CreatedDay
			select x).ToList() ?? new List<DebtRecord.DebtLine>();
		if (list.Count <= 0)
		{
			return string.Empty;
		}
		if (maxLines < 1)
		{
			maxLines = 1;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string value = ((object)npc.Name)?.ToString() ?? "该NPC";
		stringBuilder.Append("【债务提醒】你当前欠 ").Append(value).Append("：");
		if (rec.OwedGold > 0)
		{
			stringBuilder.Append(rec.OwedGold).Append(" 第纳尔");
		}
		else
		{
			stringBuilder.Append("金币 0");
		}
		stringBuilder.Append("，分笔 ").Append(list.Count).Append(" 笔。");
		int num = Math.Min(maxLines, list.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			DebtRecord.DebtLine debtLine = list[num2];
			string value2 = (debtLine.IsGold ? "金币" : (debtLine.ItemId ?? "物品"));
			string value3 = (debtLine.IsGold ? (debtLine.RemainingAmount + " 第纳尔") : ("x" + debtLine.RemainingAmount));
			string value4 = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
			stringBuilder.Append(" [").Append(debtLine.DebtId).Append("] ")
				.Append(value2)
				.Append(" ")
				.Append(value3);
			if (!string.IsNullOrWhiteSpace(value4))
			{
				stringBuilder.Append("（").Append(value4).Append("）");
			}
			if (!debtLine.IsGold && debtLine.IsItemUnavailableDeclared)
			{
				stringBuilder.Append("（已声明无法归还原物）");
			}
			if (num2 < num - 1)
			{
				stringBuilder.Append("；");
			}
		}
		if (list.Count > num)
		{
			stringBuilder.Append("；...还有 ").Append(list.Count - num).Append(" 笔");
		}
		return stringBuilder.ToString();
	}

	private string BuildDailyMerchantDebtReminderText(Settlement settlement, SettlementMerchantKind kind, DebtRecord rec, int maxLines = 2)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || rec == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec))
		{
			return string.Empty;
		}
		List<DebtRecord.DebtLine> list = (from x in rec.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.RemainingAmount > 0)
			orderby x.IsDueUnlimited ? 1 : 0, x.DueDay, x.CreatedDay
			select x).ToList() ?? new List<DebtRecord.DebtLine>();
		if (list.Count <= 0)
		{
			return string.Empty;
		}
		if (maxLines < 1)
		{
			maxLines = 1;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("【市场欠款提醒】你当前欠 ").Append(BuildSettlementMerchantDebtLabel(settlement, kind)).Append("：");
		int num = Math.Min(maxLines, list.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			DebtRecord.DebtLine debtLine = list[num2];
			stringBuilder.Append(" [").Append(debtLine.DebtId).Append("] ");
			if (debtLine.IsGold)
			{
				stringBuilder.Append("金币 ").Append(debtLine.RemainingAmount).Append(" 第纳尔");
			}
			else
			{
				stringBuilder.Append(debtLine.ItemId ?? "物品").Append(" x").Append(debtLine.RemainingAmount);
			}
			string value = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
			if (!string.IsNullOrWhiteSpace(value))
			{
				stringBuilder.Append("（").Append(value).Append("）");
			}
			if (num2 < num - 1)
			{
				stringBuilder.Append("；");
			}
		}
		if (list.Count > num)
		{
			stringBuilder.Append("；...还有 ").Append(list.Count - num).Append(" 笔");
		}
		return stringBuilder.ToString();
	}

	public Dictionary<string, DebtExportEntry> ExportDebtEntries()
	{
		Dictionary<string, DebtExportEntry> dictionary = new Dictionary<string, DebtExportEntry>();
		if (_debts == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			if (string.IsNullOrEmpty(debt.Key) || debt.Value == null)
			{
				continue;
			}
			DebtRecord value = debt.Value;
			NormalizeDebtRecord(value);
			bool flag = value.OwedGold > 0;
			if (!flag && value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem in value.OwedItems)
				{
					if (owedItem.Value > 0)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				continue;
			}
			DebtExportEntry debtExportEntry = new DebtExportEntry();
			debtExportEntry.OwedGold = Math.Max(0, value.OwedGold);
			debtExportEntry.OwedItems = new Dictionary<string, int>();
			debtExportEntry.CreatedDay = value.CreatedDay;
			debtExportEntry.DueDay = value.DueDay;
			debtExportEntry.DebtLines = new List<DebtLineExportEntry>();
			if (value.DebtLines != null)
			{
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtRecord.DebtLine debtLine = value.DebtLines[i];
					if (debtLine != null && debtLine.RemainingAmount > 0)
					{
						debtExportEntry.DebtLines.Add(new DebtLineExportEntry
						{
							DebtId = debtLine.DebtId,
							IsGold = debtLine.IsGold,
							ItemId = debtLine.ItemId,
							IsDueUnlimited = debtLine.IsDueUnlimited,
							IsItemUnavailableDeclared = debtLine.IsItemUnavailableDeclared,
							InitialAmount = debtLine.InitialAmount,
							RemainingAmount = debtLine.RemainingAmount,
							CreatedDay = debtLine.CreatedDay,
							DueDay = debtLine.DueDay,
							BestPreDueCoverage = debtLine.BestPreDueCoverage,
							OnTimePenaltyTierApplied = debtLine.OnTimePenaltyTierApplied,
							OverduePenaltyDaysApplied = debtLine.OverduePenaltyDaysApplied,
							LastOverduePenaltyDay = debtLine.LastOverduePenaltyDay,
							OverdueTrustPenaltyPerDay = debtLine.OverdueTrustPenaltyPerDay,
							OverdueRelationPenaltyPerDay = debtLine.OverdueRelationPenaltyPerDay,
							CompensationUnitPrice = debtLine.CompensationUnitPrice,
							CompensationGoldCredit = debtLine.CompensationGoldCredit
						});
					}
				}
			}
			if (value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem2 in value.OwedItems)
				{
					if (!string.IsNullOrEmpty(owedItem2.Key) && owedItem2.Value > 0)
					{
						debtExportEntry.OwedItems[owedItem2.Key] = owedItem2.Value;
					}
				}
			}
			dictionary[debt.Key] = debtExportEntry;
		}
		return dictionary;
	}

	public void ImportDebtEntries(Dictionary<string, DebtExportEntry> entries)
	{
		if (entries == null)
		{
			return;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		_debts.Clear();
		foreach (KeyValuePair<string, DebtExportEntry> entry in entries)
		{
			if (string.IsNullOrEmpty(entry.Key) || entry.Value == null)
			{
				continue;
			}
			DebtExportEntry value = entry.Value;
			int num = Math.Max(0, value.OwedGold);
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			if (value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem in value.OwedItems)
				{
					if (!string.IsNullOrEmpty(owedItem.Key) && owedItem.Value > 0)
					{
						dictionary[owedItem.Key] = owedItem.Value;
					}
				}
			}
			bool flag = num > 0 || dictionary.Count > 0;
			bool flag2 = value.DebtLines != null && value.DebtLines.Count > 0;
			if (!flag && !flag2)
			{
				continue;
			}
			DebtRecord debtRecord = new DebtRecord();
			debtRecord.DebtLines = new List<DebtRecord.DebtLine>();
			if (flag2)
			{
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtLineExportEntry debtLineExportEntry = value.DebtLines[i];
					if (debtLineExportEntry != null && debtLineExportEntry.RemainingAmount > 0)
					{
						debtRecord.DebtLines.Add(new DebtRecord.DebtLine
						{
							DebtId = debtLineExportEntry.DebtId,
							IsGold = debtLineExportEntry.IsGold,
							ItemId = debtLineExportEntry.ItemId,
							IsDueUnlimited = debtLineExportEntry.IsDueUnlimited,
							IsItemUnavailableDeclared = debtLineExportEntry.IsItemUnavailableDeclared,
							InitialAmount = debtLineExportEntry.InitialAmount,
							RemainingAmount = debtLineExportEntry.RemainingAmount,
							CreatedDay = debtLineExportEntry.CreatedDay,
							DueDay = debtLineExportEntry.DueDay,
							BestPreDueCoverage = debtLineExportEntry.BestPreDueCoverage,
							OnTimePenaltyTierApplied = debtLineExportEntry.OnTimePenaltyTierApplied,
							OverduePenaltyDaysApplied = debtLineExportEntry.OverduePenaltyDaysApplied,
							LastOverduePenaltyDay = debtLineExportEntry.LastOverduePenaltyDay,
							OverdueTrustPenaltyPerDay = debtLineExportEntry.OverdueTrustPenaltyPerDay,
							OverdueRelationPenaltyPerDay = debtLineExportEntry.OverdueRelationPenaltyPerDay,
							CompensationUnitPrice = debtLineExportEntry.CompensationUnitPrice,
							CompensationGoldCredit = debtLineExportEntry.CompensationGoldCredit
						});
					}
				}
			}
			else
			{
				debtRecord.OwedGold = num;
				debtRecord.OwedItems = dictionary;
				debtRecord.CreatedDay = value.CreatedDay;
				debtRecord.DueDay = value.DueDay;
			}
			NormalizeDebtRecord(debtRecord);
			_debts[entry.Key] = debtRecord;
		}
	}

	public List<string> GetAllDebtorHeroIds()
	{
		List<string> list = new List<string>();
		if (_debts == null)
		{
			return list;
		}
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			DebtRecord value = debt.Value;
			if (value != null)
			{
				NormalizeDebtRecord(value);
				if (HasDebtContent(value) && !string.IsNullOrEmpty(debt.Key))
				{
					list.Add(debt.Key);
				}
			}
		}
		return list;
	}

	public int GetHeroGold(Hero hero)
	{
		return (hero != null) ? hero.Gold : 0;
	}

	private static bool TryResolveHeroMapOrigin(Hero hero, out Vec2 origin)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		origin = Vec2.Invalid;
		CampaignVec2 val;
		try
		{
			if (((hero != null) ? hero.CurrentSettlement : null) != null)
			{
				val = hero.CurrentSettlement.GatePosition;
				if (((CampaignVec2)(ref val)).IsValid())
				{
					val = hero.CurrentSettlement.GatePosition;
					origin = ((CampaignVec2)(ref val)).ToVec2();
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (((hero != null) ? hero.PartyBelongedTo : null) != null)
			{
				val = hero.PartyBelongedTo.Position;
				if (((CampaignVec2)(ref val)).IsValid())
				{
					val = hero.PartyBelongedTo.Position;
					origin = ((CampaignVec2)(ref val)).ToVec2();
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty != null)
			{
				val = MobileParty.MainParty.Position;
				if (((CampaignVec2)(ref val)).IsValid())
				{
					val = MobileParty.MainParty.Position;
					origin = ((CampaignVec2)(ref val)).ToVec2();
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static ItemObject ResolveItemById(string itemId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(itemId))
			{
				return null;
			}
			Game current = Game.Current;
			object result;
			if (current == null)
			{
				result = null;
			}
			else
			{
				MBObjectManager objectManager = current.ObjectManager;
				result = ((objectManager != null) ? objectManager.GetObject<ItemObject>(itemId.Trim()) : null);
			}
			return (ItemObject)result;
		}
		catch
		{
			return null;
		}
	}

	private static bool TryGetSettlementBuyPrice(Settlement settlement, ItemObject item, out int price)
	{
		price = 0;
		try
		{
			if (settlement == null || item == null)
			{
				return false;
			}
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			if (settlementComponent == null)
			{
				return false;
			}
			MobileParty mainParty = MobileParty.MainParty;
			price = settlementComponent.GetItemPrice(item, mainParty, false);
			if (price > 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryGetSettlementBuyPrice(Settlement settlement, EquipmentElement equipmentElement, out int price)
	{
		price = 0;
		if (((EquipmentElement)(ref equipmentElement)).Item == null)
		{
			return false;
		}
		if (!TryGetSettlementBuyPrice(settlement, ((EquipmentElement)(ref equipmentElement)).Item, out price))
		{
			return false;
		}
		ItemModifier itemModifier = ((EquipmentElement)(ref equipmentElement)).ItemModifier;
		if (itemModifier != null)
		{
			price = Math.Max(1, (int)Math.Round((float)price * itemModifier.PriceMultiplier, MidpointRounding.AwayFromZero));
		}
		return price > 0;
	}

	private static string BuildSettlementMerchantInventoryKey(EquipmentElement equipmentElement)
	{
		ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
		string text = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		ItemModifier itemModifier = ((EquipmentElement)(ref equipmentElement)).ItemModifier;
		string text2 = ((itemModifier != null) ? ((MBObjectBase)itemModifier).StringId : null) ?? "";
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + "@" + text2;
	}

	private static string BuildSettlementMerchantDisplayName(EquipmentElement equipmentElement)
	{
		if (((EquipmentElement)(ref equipmentElement)).Item == null)
		{
			return "";
		}
		return ((object)((EquipmentElement)(ref equipmentElement)).GetModifiedItemName())?.ToString() ?? ((object)((EquipmentElement)(ref equipmentElement)).Item.Name)?.ToString() ?? ((MBObjectBase)((EquipmentElement)(ref equipmentElement)).Item).StringId ?? "";
	}

	private static bool TryParseSettlementMerchantPromptStringId(string promptStringId, out string itemId, out string modifierId)
	{
		itemId = "";
		modifierId = "";
		if (string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		string text = promptStringId.Trim();
		int num = text.IndexOf('@');
		if (num < 0)
		{
			itemId = text;
			return !string.IsNullOrWhiteSpace(itemId);
		}
		itemId = text.Substring(0, num).Trim();
		modifierId = text.Substring(num + 1).Trim();
		return !string.IsNullOrWhiteSpace(itemId);
	}

	private static bool MatchesSettlementMerchantPromptStringId(EquipmentElement equipmentElement, string promptStringId)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (((EquipmentElement)(ref equipmentElement)).Item == null || string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		return string.Equals(BuildSettlementMerchantInventoryKey(equipmentElement), promptStringId.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolveSettlementMerchantDisplayNameFromPromptStringId(string promptStringId)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		if (!TryParseSettlementMerchantPromptStringId(promptStringId, out var itemId, out var modifierId))
		{
			return promptStringId ?? "";
		}
		ItemObject val = ResolveItemById(itemId);
		if (val == null)
		{
			return promptStringId ?? "";
		}
		ItemModifier val2 = null;
		if (!string.IsNullOrWhiteSpace(modifierId))
		{
			try
			{
				Game current = Game.Current;
				object obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					MBObjectManager objectManager = current.ObjectManager;
					obj = ((objectManager != null) ? objectManager.GetObject<ItemModifier>(modifierId) : null);
				}
				val2 = (ItemModifier)obj;
			}
			catch
			{
				val2 = null;
			}
		}
		EquipmentElement equipmentElement = default(EquipmentElement);
		((EquipmentElement)(ref equipmentElement))._002Ector(val, val2, (ItemObject)null, false);
		string text = BuildSettlementMerchantDisplayName(equipmentElement);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return ((object)val.Name)?.ToString() ?? itemId;
	}

	private static string GetItemQuantityUnit(ItemObject item)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected I4, but got Unknown
		if (item == null)
		{
			return "个";
		}
		ItemCategory itemCategory = item.ItemCategory;
		if (itemCategory == DefaultItemCategories.Beer || itemCategory == DefaultItemCategories.Wine)
		{
			return "桶";
		}
		if (item.IsFood)
		{
			return "斤";
		}
		ItemTypeEnum type = item.Type;
		ItemTypeEnum val = type;
		switch (val - 2)
		{
		case 3:
		case 4:
		case 5:
		case 10:
		case 18:
			return "袋";
		case 2:
			return "支";
		case 0:
			return "把";
		case 1:
			return "柄";
		case 12:
		case 13:
		case 14:
		case 15:
		case 22:
			return "件";
		default:
			return "个";
		}
	}

	private static string FormatItemAmount(int amount, ItemObject item, string itemName)
	{
		return $"{amount}{GetItemQuantityUnit(item)}{itemName}";
	}

	private static string BuildItemValueFactSuffixCore(ItemObject item, int amount, int unitPrice)
	{
		if (item == null || amount <= 0 || unitPrice <= 0)
		{
			return "";
		}
		int num = Math.Max(1, amount);
		int num2 = Math.Max(1, unitPrice);
		long num3 = (long)num * (long)num2;
		return $"（指导单价约 {num2} 第纳尔/{GetItemQuantityUnit(item)}，总值约 {num3} 第纳尔）";
	}

	private static int GetInventoryActualItemUnitValueCore(EquipmentElement equipmentElement)
	{
		try
		{
			int itemValue = ((EquipmentElement)(ref equipmentElement)).ItemValue;
			if (itemValue > 0)
			{
				return itemValue;
			}
		}
		catch
		{
		}
		try
		{
			ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
			int num = ((item != null) ? item.Value : 0);
			if (num > 0)
			{
				return num;
			}
		}
		catch
		{
		}
		return 1;
	}

	private static string BuildInventoryActualItemValueFactSuffixCore(ItemObject item, int amount, int unitValue)
	{
		if (item == null || amount <= 0 || unitValue <= 0)
		{
			return "";
		}
		int num = Math.Max(1, amount);
		int num2 = Math.Max(1, unitValue);
		long num3 = (long)num * (long)num2;
		return $"（库存实际单价约 {num2} 第纳尔/{GetItemQuantityUnit(item)}，该项总值约 {num3} 第纳尔）";
	}

	public int GetInventoryActualItemUnitValueForExternal(EquipmentElement equipmentElement)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return GetInventoryActualItemUnitValueCore(equipmentElement);
		}
		catch
		{
			return 1;
		}
	}

	public string BuildInventoryActualItemValueFactSuffixForExternal(ItemObject item, int amount, int inventoryUnitValue)
	{
		try
		{
			return BuildInventoryActualItemValueFactSuffixCore(item, amount, inventoryUnitValue);
		}
		catch
		{
			return "";
		}
	}

	public long EstimateInventoryActualItemValueForExternal(ItemObject item, int amount, int inventoryUnitValue)
	{
		try
		{
			if (item == null || amount <= 0 || inventoryUnitValue <= 0)
			{
				return 0L;
			}
			return (long)Math.Max(1, amount) * (long)Math.Max(1, inventoryUnitValue);
		}
		catch
		{
			return 0L;
		}
	}

	public string BuildItemValueFactSuffixForExternal(Hero hero, string itemId, int amount)
	{
		try
		{
			return BuildItemValueFactSuffixForExternal(hero, ResolveItemById(itemId), amount);
		}
		catch
		{
			return "";
		}
	}

	public string BuildItemValueFactSuffixForExternal(Hero hero, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return "";
			}
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero ?? Hero.MainHero, item);
			return BuildItemValueFactSuffixCore(item, amount, Math.Max(1, guidePriceForItemNearHero.UnitPrice));
		}
		catch
		{
			return "";
		}
	}

	public string BuildSettlementItemValueFactSuffixForExternal(Settlement settlement, string itemId, int amount)
	{
		try
		{
			string text = (itemId ?? "").Trim();
			int num = text.IndexOf('@');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			return BuildSettlementItemValueFactSuffixForExternal(settlement, ResolveItemById(text), amount);
		}
		catch
		{
			return "";
		}
	}

	public string BuildSettlementItemValueFactSuffixForExternal(Settlement settlement, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return "";
			}
			if (settlement != null && TryGetSettlementBuyPrice(settlement, item, out var price) && price > 0)
			{
				return BuildItemValueFactSuffixCore(item, amount, price);
			}
			return BuildItemValueFactSuffixForExternal(Hero.MainHero, item, amount);
		}
		catch
		{
			return "";
		}
	}

	public long EstimateItemValueForExternal(Hero hero, string itemId, int amount)
	{
		try
		{
			return EstimateItemValueForExternal(hero, ResolveItemById(itemId), amount);
		}
		catch
		{
			return 0L;
		}
	}

	public long EstimateItemValueForExternal(Hero hero, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return 0L;
			}
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero ?? Hero.MainHero, item);
			return (long)Math.Max(1, amount) * (long)Math.Max(1, guidePriceForItemNearHero.UnitPrice);
		}
		catch
		{
			return 0L;
		}
	}

	public long EstimateSettlementItemValueForExternal(Settlement settlement, string itemId, int amount)
	{
		try
		{
			string text = (itemId ?? "").Trim();
			int num = text.IndexOf('@');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			return EstimateSettlementItemValueForExternal(settlement, ResolveItemById(text), amount);
		}
		catch
		{
			return 0L;
		}
	}

	public long EstimateSettlementItemValueForExternal(Settlement settlement, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return 0L;
			}
			if (settlement != null && TryGetSettlementBuyPrice(settlement, item, out var price) && price > 0)
			{
				return (long)Math.Max(1, amount) * (long)Math.Max(1, price);
			}
			return EstimateItemValueForExternal(Hero.MainHero, item, amount);
		}
		catch
		{
			return 0L;
		}
	}

	private static int GetSettlementItemStock(Settlement settlement, string itemId)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (settlement == null || string.IsNullOrWhiteSpace(itemId))
			{
				return 0;
			}
			ItemRoster itemRoster = settlement.ItemRoster;
			if (itemRoster == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
				if (item != null && ((ItemRosterElement)(ref elementCopyAtIndex)).Amount > 0 && string.Equals(((MBObjectBase)item).StringId ?? "", itemId, StringComparison.OrdinalIgnoreCase))
				{
					num += ((ItemRosterElement)(ref elementCopyAtIndex)).Amount;
				}
			}
			return num;
		}
		catch
		{
			return 0;
		}
	}

	private ItemGuidePriceInfo GetGuidePriceForItemNearHero(Hero hero, ItemObject item)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		ItemGuidePriceInfo itemGuidePriceInfo = new ItemGuidePriceInfo
		{
			UnitPrice = Math.Max(1, (item == null) ? 1 : item.Value),
			SampleCount = 0,
			ExpandedSearch = false,
			UsedNoStockFallback = false,
			UsedBaseValueFallback = true
		};
		if (item == null)
		{
			return itemGuidePriceInfo;
		}
		Vec2 origin;
		bool flag = TryResolveHeroMapOrigin(hero, out origin);
		List<(Settlement, float)> list = new List<(Settlement, float)>();
		try
		{
			foreach (Settlement item3 in (List<Settlement>)(object)Settlement.All)
			{
				if (item3 != null && !item3.IsHideout && item3.IsTown && item3.SettlementComponent != null)
				{
					float item2 = 0f;
					if (flag)
					{
						CampaignVec2 gatePosition = item3.GatePosition;
						Vec2 val = ((CampaignVec2)(ref gatePosition)).ToVec2();
						float num = val.x - origin.x;
						float num2 = val.y - origin.y;
						item2 = num * num + num2 * num2;
					}
					list.Add((item3, item2));
				}
			}
		}
		catch
		{
		}
		if (list.Count <= 0)
		{
			return itemGuidePriceInfo;
		}
		if (flag)
		{
			list = list.OrderBy<(Settlement, float), float>(((Settlement St, float D2) x) => x.D2).ToList();
		}
		float[] array = new float[6] { 20f, 40f, 80f, 140f, 240f, 400f };
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			float num4 = array[num3] * array[num3];
			int num5 = 0;
			int num6 = 0;
			for (int num7 = 0; num7 < list.Count; num7++)
			{
				(Settlement, float) tuple = list[num7];
				if (flag && tuple.Item2 > num4)
				{
					break;
				}
				int settlementItemStock = GetSettlementItemStock(tuple.Item1, ((MBObjectBase)item).StringId);
				if (settlementItemStock > 0 && TryGetSettlementBuyPrice(tuple.Item1, item, out var price) && price > 0)
				{
					num5 += price;
					num6++;
					if (num6 >= 4)
					{
						break;
					}
				}
			}
			if (num6 > 0)
			{
				itemGuidePriceInfo.UnitPrice = Math.Max(1, (int)Math.Round((double)num5 / (double)num6));
				itemGuidePriceInfo.SampleCount = num6;
				itemGuidePriceInfo.ExpandedSearch = num3 > 0;
				itemGuidePriceInfo.UsedNoStockFallback = false;
				itemGuidePriceInfo.UsedBaseValueFallback = false;
				return itemGuidePriceInfo;
			}
		}
		int num8 = 0;
		int num9 = 0;
		for (int num10 = 0; num10 < list.Count; num10++)
		{
			if (TryGetSettlementBuyPrice(list[num10].Item1, item, out var price2) && price2 > 0)
			{
				num8 += price2;
				num9++;
				if (num9 >= 4)
				{
					break;
				}
			}
		}
		if (num9 > 0)
		{
			itemGuidePriceInfo.UnitPrice = Math.Max(1, (int)Math.Round((double)num8 / (double)num9));
			itemGuidePriceInfo.SampleCount = num9;
			itemGuidePriceInfo.ExpandedSearch = true;
			itemGuidePriceInfo.UsedNoStockFallback = true;
			itemGuidePriceInfo.UsedBaseValueFallback = false;
			return itemGuidePriceInfo;
		}
		return itemGuidePriceInfo;
	}

	public bool TryGetSettlementMerchantKind(CharacterObject character, out SettlementMerchantKind kind)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected I4, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		kind = SettlementMerchantKind.None;
		if (character == null || ((BasicCharacterObject)character).IsHero)
		{
			return false;
		}
		Occupation occupation = character.Occupation;
		Occupation val = occupation;
		if ((int)val != 4)
		{
			switch (val - 10)
			{
			default:
				if ((int)val != 28)
				{
					return false;
				}
				goto case 0;
			case 0:
				kind = SettlementMerchantKind.Weapon;
				return true;
			case 1:
				kind = SettlementMerchantKind.Armor;
				return true;
			case 2:
				kind = SettlementMerchantKind.Horse;
				return true;
			}
		}
		kind = SettlementMerchantKind.Goods;
		return true;
	}

	private static string GetSettlementMerchantRoleLabel(SettlementMerchantKind kind)
	{
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			SettlementMerchantKind.Weapon => "武器商人", 
			SettlementMerchantKind.Armor => "盔甲商人", 
			SettlementMerchantKind.Horse => "马匹贩子", 
			SettlementMerchantKind.Goods => "杂货商人", 
			_ => "商贩", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetSettlementMerchantMarketLabel(SettlementMerchantKind kind)
	{
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			SettlementMerchantKind.Weapon => "武器市场", 
			SettlementMerchantKind.Armor => "盔甲市场", 
			SettlementMerchantKind.Horse => "马匹市场", 
			SettlementMerchantKind.Goods => "杂货市场", 
			_ => "城镇市场", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetSettlementMerchantSpecialHint(SettlementMerchantKind kind)
	{
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			SettlementMerchantKind.Weapon => "弓、弩、箭、弩矢、投掷武器和盾牌都归入你的武器市场。", 
			SettlementMerchantKind.Armor => "头盔、身甲、臂甲、腿甲、披风等护具都归入你的盔甲市场。", 
			SettlementMerchantKind.Horse => "马匹与马具都归入你的马匹市场。", 
			SettlementMerchantKind.Goods => "粮食、贸易品和一般杂货都归入你的杂货市场。", 
			_ => "", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static bool MatchesSettlementMerchantKind(ItemObject item, SettlementMerchantKind kind)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Invalid comparison between Unknown and I4
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Invalid comparison between Unknown and I4
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Invalid comparison between Unknown and I4
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Invalid comparison between Unknown and I4
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Invalid comparison between Unknown and I4
		if (item == null)
		{
			return false;
		}
		switch (kind)
		{
		case SettlementMerchantKind.Weapon:
		{
			ItemTypeEnum type2 = item.Type;
			ItemTypeEnum val2 = type2;
			if (val2 - 2 <= 10 || val2 - 18 <= 2)
			{
				return true;
			}
			return false;
		}
		case SettlementMerchantKind.Armor:
		{
			ItemTypeEnum type = item.Type;
			ItemTypeEnum val = type;
			if (val - 14 <= 3 || val - 23 <= 1)
			{
				return true;
			}
			return false;
		}
		case SettlementMerchantKind.Horse:
			return (int)item.Type == 1 || (int)item.Type == 25;
		case SettlementMerchantKind.Goods:
			return (int)item.Type == 13 || (int)item.Type == 21;
		default:
			return false;
		}
	}

	public string BuildSettlementMerchantRewardInstruction(CharacterObject character)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		string text = ((currentSettlement == null) ? null : ((object)currentSettlement.Name)?.ToString()) ?? "当前城镇";
		string settlementMerchantRoleLabel = GetSettlementMerchantRoleLabel(kind);
		string settlementMerchantMarketLabel = GetSettlementMerchantMarketLabel(kind);
		string settlementMerchantSpecialHint = GetSettlementMerchantSpecialHint(kind);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【城镇商贩补充】你不是在卖你个人的私人物品。");
		stringBuilder.AppendLine("你是" + text + "里的" + settlementMerchantRoleLabel + "，代表这座城镇当前的" + settlementMerchantMarketLabel + "与玩家进行即时交易。");
		stringBuilder.AppendLine("你的真实可售货物只以你当前摊位资产清单中的内容为准。");
		if (!string.IsNullOrWhiteSpace(settlementMerchantSpecialHint))
		{
			stringBuilder.AppendLine(settlementMerchantSpecialHint);
		}
		return stringBuilder.ToString().Trim();
	}

	public string BuildSettlementMerchantInventorySummaryForAI(CharacterObject character, Settlement settlement = null, int maxItems = 20, bool includeGuidePrice = true)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		if (settlement == null || !settlement.IsTown || settlement.ItemRoster == null)
		{
			return "";
		}
		Dictionary<string, RewardItemInfo> dictionary = new Dictionary<string, RewardItemInfo>(StringComparer.OrdinalIgnoreCase);
		ItemRoster itemRoster = settlement.ItemRoster;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
			ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
			if (item == null || ((ItemRosterElement)(ref elementCopyAtIndex)).Amount <= 0 || !MatchesSettlementMerchantKind(item, kind))
			{
				continue;
			}
			string text = BuildSettlementMerchantInventoryKey(equipmentElement);
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (!dictionary.TryGetValue(text, out var value))
				{
					RewardItemInfo obj = new RewardItemInfo
					{
						Item = item,
						StringId = (((MBObjectBase)item).StringId ?? ""),
						PromptStringId = text
					};
					ItemModifier itemModifier = ((EquipmentElement)(ref equipmentElement)).ItemModifier;
					obj.ModifierStringId = ((itemModifier != null) ? ((MBObjectBase)itemModifier).StringId : null) ?? "";
					obj.Name = BuildSettlementMerchantDisplayName(equipmentElement);
					obj.Count = 0;
					obj.EquipmentElement = equipmentElement;
					RewardItemInfo rewardItemInfo = obj;
					dictionary[text] = obj;
					value = rewardItemInfo;
				}
				value.Count += ((ItemRosterElement)(ref elementCopyAtIndex)).Amount;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		int value2 = ((settlementComponent != null) ? settlementComponent.Gold : 0);
		stringBuilder.Append("第纳尔: ").Append(value2).AppendLine();
		if (includeGuidePrice)
		{
			stringBuilder.AppendLine("【价格说明】每个物品后面的 guidePrice 为当前城镇市场的即时指导单价（第纳尔/当前单位；箭矢、弩矢、标枪、飞刀等远程弹药按袋计）。");
		}
		stringBuilder.AppendLine("InventoryItems:");
		foreach (RewardItemInfo item2 in dictionary.Values.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal))
		{
			StringBuilder stringBuilder2 = stringBuilder.Append(item2.PromptStringId).Append("|").Append(item2.Name)
				.Append("|")
				.Append(item2.Count);
			EquipmentElement equipmentElement2 = item2.EquipmentElement;
			stringBuilder2.Append(GetItemQuantityUnit(((EquipmentElement)(ref equipmentElement2)).Item));
			if (includeGuidePrice && TryGetSettlementBuyPrice(settlement, item2.EquipmentElement, out var price))
			{
				stringBuilder.Append("|guidePrice=").Append(Math.Max(1, price));
			}
			stringBuilder.AppendLine();
		}
		if (dictionary.Count == 0)
		{
			stringBuilder.AppendLine("（当前没有可售货物）");
		}
		return stringBuilder.ToString().Trim();
	}

	public List<RewardItemInfo> GetHeroInventoryItems(Hero hero)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (hero == null)
		{
			return list;
		}
		ItemRoster val = ((hero.PartyBelongedTo != null) ? hero.PartyBelongedTo.ItemRoster : null);
		if (val == null)
		{
			Clan clan = hero.Clan;
			object obj;
			if (clan == null)
			{
				obj = null;
			}
			else
			{
				Hero leader = clan.Leader;
				obj = ((leader != null) ? leader.PartyBelongedTo : null);
			}
			if (obj != null)
			{
				val = hero.Clan.Leader.PartyBelongedTo.ItemRoster;
			}
		}
		if (val == null && MobileParty.MainParty != null && hero == Hero.MainHero)
		{
			val = MobileParty.MainParty.ItemRoster;
		}
		if (val != null)
		{
			for (int i = 0; i < val.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = val.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				if (((EquipmentElement)(ref equipmentElement)).Item != null)
				{
					equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
					ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
					int amount = ((ItemRosterElement)(ref elementCopyAtIndex)).Amount;
					if (amount > 0)
					{
						list.Add(new RewardItemInfo
						{
							Item = item,
							StringId = ((MBObjectBase)item).StringId,
							Name = (((object)item.Name)?.ToString() ?? ((MBObjectBase)item).StringId),
							Count = amount
						});
					}
				}
			}
		}
		return list;
	}

	private static bool TryResolvePromptEquipmentContext(Hero hero, out bool useCivilianEquipment)
	{
		useCivilianEquipment = false;
		try
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				useCivilianEquipment = current.DoesMissionRequireCivilianEquipment;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement val = Settlement.CurrentSettlement ?? ((hero != null) ? hero.CurrentSettlement : null);
			if (val != null)
			{
				useCivilianEquipment = !val.IsVillage;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private List<RewardItemInfo> GetHeroEquipmentItems(Hero hero, bool useCivilianEquipment)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (hero == null)
		{
			return list;
		}
		EquipmentIndex[] array = new EquipmentIndex[7];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		EquipmentIndex[] array2 = (EquipmentIndex[])(object)array;
		EquipmentIndex[] array3 = array2;
		EquipmentIndex[] array4 = array3;
		EquipmentIndex[] array5 = array4;
		foreach (EquipmentIndex val in array5)
		{
			Equipment val2 = (useCivilianEquipment ? hero.CivilianEquipment : hero.BattleEquipment);
			EquipmentElement equipmentElement = val2[val];
			if (((EquipmentElement)(ref equipmentElement)).Item != null)
			{
				ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
				list.Add(new RewardItemInfo
				{
					Item = item,
					StringId = ((MBObjectBase)item).StringId,
					Name = (((object)item.Name)?.ToString() ?? ((MBObjectBase)item).StringId),
					Count = 1,
					EquipmentElement = equipmentElement
				});
			}
		}
		return list;
	}

	public List<RewardItemInfo> GetHeroBattleEquipmentItems(Hero hero)
	{
		return GetHeroEquipmentItems(hero, useCivilianEquipment: false);
	}

	private List<RewardItemInfo> GetAgentEquipmentItems(Agent agent)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (agent == null || !agent.IsActive())
		{
			return list;
		}
		EquipmentIndex[] array = new EquipmentIndex[7];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		EquipmentIndex[] array2 = (EquipmentIndex[])(object)array;
		EquipmentIndex[] array3 = array2;
		EquipmentIndex[] array4 = array3;
		foreach (EquipmentIndex val in array4)
		{
			EquipmentElement val2 = agent.SpawnEquipment[val];
			ItemObject item = ((EquipmentElement)(ref val2)).Item;
			if (item == null)
			{
				MissionWeapon val3 = agent.Equipment[val];
				item = ((MissionWeapon)(ref val3)).Item;
			}
			if (item != null)
			{
				list.Add(new RewardItemInfo
				{
					Item = item,
					StringId = ((MBObjectBase)item).StringId,
					Name = (((object)item.Name)?.ToString() ?? ((MBObjectBase)item).StringId),
					Count = 1
				});
			}
		}
		return list;
	}

	private List<RewardItemInfo> GetHeroVisibleEquipmentItemsForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return new List<RewardItemInfo>();
		}
		bool useCivilianEquipment = false;
		TryResolvePromptEquipmentContext(hero, out useCivilianEquipment);
		List<RewardItemInfo> heroEquipmentItems = GetHeroEquipmentItems(hero, useCivilianEquipment);
		if (heroEquipmentItems.Count > 0)
		{
			return heroEquipmentItems;
		}
		if (hero == Hero.MainHero && Agent.Main != null && Agent.Main.IsActive())
		{
			List<RewardItemInfo> agentEquipmentItems = GetAgentEquipmentItems(Agent.Main);
			if (agentEquipmentItems.Count > 0)
			{
				return agentEquipmentItems;
			}
		}
		return GetHeroBattleEquipmentItems(hero);
	}

	private int GetVisibleEquipmentActualUnitValue(RewardItemInfo itemInfo)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (itemInfo == null)
		{
			return 0;
		}
		try
		{
			EquipmentElement equipmentElement = itemInfo.EquipmentElement;
			if (((EquipmentElement)(ref equipmentElement)).Item != null)
			{
				return Math.Max(1, GetInventoryActualItemUnitValueForExternal(itemInfo.EquipmentElement));
			}
		}
		catch
		{
		}
		try
		{
			ItemObject item = itemInfo.Item;
			return Math.Max(1, (item == null) ? 1 : item.Value);
		}
		catch
		{
			return 1;
		}
	}

	public long EstimateVisibleEquipmentActualValueForAI(Hero hero, int maxItems = 8)
	{
		if (hero == null)
		{
			return 0L;
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return 0L;
		}
		long num = 0L;
		int num2 = 0;
		foreach (RewardItemInfo item in heroVisibleEquipmentItemsForPrompt.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal))
		{
			if (item != null && item.Item != null)
			{
				int num3 = Math.Max(1, GetVisibleEquipmentActualUnitValue(item));
				int num4 = Math.Max(1, item.Count);
				num += (long)num3 * (long)num4;
				num2++;
				if (num2 >= Math.Max(1, maxItems))
				{
					break;
				}
			}
		}
		return Math.Max(0L, num);
	}

	public string BuildVisibleEquipmentActualValueInlineFactForAI(Hero hero, int maxItems = 8)
	{
		try
		{
			long num = EstimateVisibleEquipmentActualValueForAI(hero, maxItems);
			if (num <= 0)
			{
				return "";
			}
			return "这身当前可见装备按玩家库存中的实际价值计算，总值约 " + num + " 第纳尔";
		}
		catch
		{
			return "";
		}
	}

	public string BuildVisibleEquipmentActualValueSummaryForAI(Hero hero, int maxItems = 8)
	{
		if (hero == null)
		{
			return string.Empty;
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return string.Empty;
		}
		long num = 0L;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【玩家可见装备实际估值】以下为玩家当前穿戴/携行装备按库存实际价值计算（第纳尔）：");
		int num2 = 0;
		foreach (RewardItemInfo item in heroVisibleEquipmentItemsForPrompt.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal))
		{
			if (item != null && item.Item != null)
			{
				int num3 = Math.Max(1, GetVisibleEquipmentActualUnitValue(item));
				int num4 = Math.Max(1, item.Count);
				long num5 = (long)num3 * (long)num4;
				num += num5;
				stringBuilder.Append(item.StringId).Append("|").Append(item.Name ?? item.StringId)
					.Append("|")
					.Append(num4)
					.Append(GetItemQuantityUnit(item.Item))
					.Append("|inventoryUnitValue=")
					.Append(num3)
					.Append("|lineValue=")
					.Append(num5)
					.AppendLine();
				num2++;
				if (num2 >= Math.Max(1, maxItems))
				{
					break;
				}
			}
		}
		stringBuilder.AppendLine("总估值约 " + Math.Max(0L, num) + " 第纳尔。");
		return stringBuilder.ToString().Trim();
	}

	public string BuildVisibleEquipmentValueSummaryForAI(Hero hero, int maxItems = 8)
	{
		return BuildVisibleEquipmentActualValueSummaryForAI(hero, maxItems);
	}

	public string BuildInventorySummaryForAI(Hero hero, int maxItems = 20, bool includeGuidePrice = true)
	{
		int heroGold = GetHeroGold(hero);
		List<RewardItemInfo> heroInventoryItems = GetHeroInventoryItems(hero);
		List<RewardItemInfo> heroBattleEquipmentItems = GetHeroBattleEquipmentItems(hero);
		string text = ((hero == null) ? null : ((object)hero.Name)?.ToString()) ?? "该NPC";
		Dictionary<string, ItemGuidePriceInfo> dictionary = (includeGuidePrice ? new Dictionary<string, ItemGuidePriceInfo>(StringComparer.OrdinalIgnoreCase) : null);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("第纳尔: ").Append(heroGold).AppendLine();
		if (includeGuidePrice)
		{
			stringBuilder.AppendLine("【价格说明】每个物品后面的 guidePrice 为指导单价（第纳尔/当前单位；箭矢、弩矢、标枪、飞刀等远程弹药按袋计）。");
		}
		int num = 0;
		stringBuilder.AppendLine("InventoryItems:");
		foreach (RewardItemInfo item in heroInventoryItems.OrderByDescending((RewardItemInfo i) => i.Count))
		{
			stringBuilder.Append(item.StringId).Append("|").Append(item.Name)
				.Append("|")
				.Append(item.Count)
				.Append(GetItemQuantityUnit(item.Item));
			if (includeGuidePrice)
			{
				string key = item.StringId ?? "";
				if (!dictionary.TryGetValue(key, out var value))
				{
					ItemGuidePriceInfo itemGuidePriceInfo = (dictionary[key] = GetGuidePriceForItemNearHero(hero, item.Item));
					ItemGuidePriceInfo itemGuidePriceInfo2 = itemGuidePriceInfo;
					value = itemGuidePriceInfo2;
				}
				stringBuilder.Append("|guidePrice=").Append(Math.Max(1, value.UnitPrice));
			}
			stringBuilder.AppendLine();
			num++;
			if (num >= maxItems)
			{
				break;
			}
		}
		if (heroBattleEquipmentItems != null && heroBattleEquipmentItems.Count > 0 && num < maxItems)
		{
			stringBuilder.AppendLine(text + "的私人战斗装备:");
			foreach (RewardItemInfo item2 in heroBattleEquipmentItems)
			{
				stringBuilder.Append(item2.StringId).Append("|").Append(item2.Name)
					.Append("|")
					.Append(item2.Count)
					.Append(GetItemQuantityUnit(item2.Item));
				if (includeGuidePrice)
				{
					string key2 = item2.StringId ?? "";
					if (!dictionary.TryGetValue(key2, out var value2))
					{
						ItemGuidePriceInfo itemGuidePriceInfo = (dictionary[key2] = GetGuidePriceForItemNearHero(hero, item2.Item));
						ItemGuidePriceInfo itemGuidePriceInfo3 = itemGuidePriceInfo;
						value2 = itemGuidePriceInfo3;
					}
					stringBuilder.Append("|guidePrice=").Append(Math.Max(1, value2.UnitPrice));
				}
				stringBuilder.AppendLine();
				num++;
				if (num >= maxItems)
				{
					break;
				}
			}
			ICampaignMission current3 = CampaignMission.Current;
			if (current3 != null && current3.Location != null)
			{
				string text2 = current3.Location.StringId ?? string.Empty;
				switch (text2)
				{
				default:
					if (!(text2 == "tavern"))
					{
						break;
					}
					goto case "center";
				case "center":
				case "lordshall":
				case "castle":
					stringBuilder.AppendLine("【" + text + "的战斗装备栏说明】当前" + text + "所在的是城镇、领主大厅或城堡等日常场景，外表通常只穿着日常衣物，玩家无法直接看见这些战斗装备。你可以把这些武器盔甲理解为" + text + "随身携带的备用武装，可结合当下关系与谈判情况，酌情决定是否将其作为赌注或交易物品。");
					break;
				}
			}
		}
		return stringBuilder.ToString();
	}

	public string BuildDebtHintForAI(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【系统账目提示】玩家对你有以下欠款（分笔记录）：");
		stringBuilder.AppendLine("【还款确认】若玩家声称要还钱，并且[AFEF玩家行为补充] 中确实明确写了玩家把钱交给了你或写出了你的名字，你应立即核对债务ID并在本轮回复末尾用 [ACTION:DEBT_PAY_GOLD:债务ID:数量]（金币债）或 [ACTION:DEBT_PAY_ITEM:债务ID:物品ID:数量]（物品债）确认还款；禁止嘴炮还款，禁止自动冲抵到别的债，禁止拖沓，必须在本轮输出这些标签。");
		if (debtRecord.DebtLines != null)
		{
			List<DebtRecord.DebtLine> list = (from x in debtRecord.DebtLines
				where x != null && x.RemainingAmount > 0
				orderby x.DueDay, x.CreatedDay
				select x).ToList();
			for (int num = 0; num < list.Count; num++)
			{
				DebtRecord.DebtLine debtLine = list[num];
				string value = (debtLine.IsGold ? "金币" : ("物品 " + debtLine.ItemId));
				string value2 = (debtLine.IsGold ? (debtLine.RemainingAmount + " 第纳尔") : ("x" + debtLine.RemainingAmount));
				string value3 = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
				int num2 = EstimateDebtLineRemainingValue(npc, debtLine);
				stringBuilder.Append("- [债务ID:").Append(debtLine.DebtId).Append("] ")
					.Append(value)
					.Append("，未还 ")
					.Append(value2);
				if (!debtLine.IsGold && debtLine.IsItemUnavailableDeclared)
				{
					stringBuilder.Append("，状态：已声明无法归还原物（该笔关系/信任改动由你决定）");
				}
				if (!debtLine.IsGold)
				{
					ItemObject item = ResolveItemById(debtLine.ItemId);
					ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(npc, item);
					int value4 = Math.Max(1, guidePriceForItemNearHero.UnitPrice);
					stringBuilder.Append("，指导赔偿单价约 ").Append(value4).Append(" 第纳尔/个");
				}
				stringBuilder.Append("，剩余估值约 ").Append(Math.Max(0, num2)).Append(" 第纳尔");
				int num3 = NormalizeLlmPenaltyValue(debtLine.OverdueTrustPenaltyPerDay);
				int num4 = NormalizeLlmPenaltyValue(debtLine.OverdueRelationPenaltyPerDay);
				int num5 = ComputeDailyOverduePenaltySeverity(debtLine.InitialAmount, debtLine.RemainingAmount, num2);
				int value5 = ((num3 > 0) ? num3 : num5);
				int value6 = ((num4 > 0) ? num4 : num5);
				if (num3 > 0 || num4 > 0)
				{
					stringBuilder.Append("，逾期日惩罚预设：信任 -").Append(num3).Append(" / 关系 -")
						.Append(num4);
				}
				else
				{
					stringBuilder.Append("，逾期日惩罚预设：未指定（将使用兼容推断）");
				}
				if (!string.IsNullOrWhiteSpace(value3))
				{
					stringBuilder.Append("，").Append(value3);
				}
				if (!debtLine.IsDueUnlimited && debtLine.DueDay > 0f && GetNowCampaignDay() > debtLine.DueDay + 0.01f && (!debtLine.IsItemUnavailableDeclared || debtLine.IsGold))
				{
					stringBuilder.Append("，当前逾期日惩罚预计：信任 -").Append(value5).Append(" / 关系 -")
						.Append(value6);
				}
				stringBuilder.AppendLine();
			}
		}
		return stringBuilder.ToString().Trim();
	}

	public string BuildSettlementMerchantDebtHintForAI(CharacterObject character, Settlement settlement = null)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		DebtRecord settlementMerchantDebtRecord = GetSettlementMerchantDebtRecord(settlement, kind);
		if (settlementMerchantDebtRecord == null)
		{
			return "";
		}
		NormalizeDebtRecord(settlementMerchantDebtRecord);
		if (!HasDebtContent(settlementMerchantDebtRecord))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【系统账目提示】玩家对你代表的" + BuildSettlementMerchantDebtLabel(settlement, kind) + "有以下欠款（分笔记录）：");
		stringBuilder.AppendLine("【还款确认】若玩家声称要还钱，并且[AFEF玩家行为补充] 中确实明确写了玩家把钱交给了你或写出了你的名字，你应立即核对债务ID并在本轮回复末尾用 [ACTION:DEBT_PAY_GOLD:债务ID:数量]（金币债）或 [ACTION:DEBT_PAY_ITEM:债务ID:物品ID:数量]（物品债）确认还款；禁止嘴炮还款，禁止自动冲抵到别的债，禁止拖沓，必须在本轮输出这些标签。");
		List<DebtRecord.DebtLine> list = (from x in settlementMerchantDebtRecord.DebtLines
			where x != null && x.RemainingAmount > 0
			orderby x.DueDay, x.CreatedDay
			select x).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			DebtRecord.DebtLine debtLine = list[num];
			stringBuilder.Append("- [债务ID:").Append(debtLine.DebtId).Append("] ");
			if (debtLine.IsGold)
			{
				stringBuilder.Append("金币，未还 ").Append(debtLine.RemainingAmount).Append(" 第纳尔");
			}
			else
			{
				stringBuilder.Append("物品 ").Append(debtLine.ItemId).Append("，未还 x")
					.Append(debtLine.RemainingAmount);
			}
			string value = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
			if (!string.IsNullOrWhiteSpace(value))
			{
				stringBuilder.Append("，").Append(value);
			}
			int num2 = NormalizeLlmPenaltyValue(debtLine.OverdueTrustPenaltyPerDay);
			if (num2 > 0)
			{
				stringBuilder.Append("，逾期日惩罚预设：信任 -").Append(num2);
			}
			else
			{
				stringBuilder.Append("，逾期日惩罚预设：未指定（将使用兼容推断）");
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().Trim();
	}

	public string BuildDebtEditorSummary(Hero npc, int maxLines = 12)
	{
		try
		{
			if (maxLines < 1)
			{
				maxLines = 1;
			}
			DebtRecord debtRecord = GetDebtRecord(npc);
			if (debtRecord == null)
			{
				return "当前无欠款。";
			}
			NormalizeDebtRecord(debtRecord);
			if (!HasDebtContent(debtRecord))
			{
				return "当前无欠款。";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("金币总欠款：").Append(debtRecord.OwedGold).AppendLine();
			if (debtRecord.OwedItems != null && debtRecord.OwedItems.Count > 0)
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
				{
					if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
					{
						list.Add(owedItem.Key + "x" + owedItem.Value);
					}
				}
				if (list.Count > 0)
				{
					stringBuilder.AppendLine("物品总欠款：" + string.Join("，", list));
				}
			}
			List<DebtRecord.DebtLine> list2 = (from x in debtRecord.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.RemainingAmount > 0)
				orderby x.IsDueUnlimited ? 1 : 0, x.DueDay, x.CreatedDay
				select x).ToList() ?? new List<DebtRecord.DebtLine>();
			stringBuilder.Append("分笔未清：").Append(list2.Count).Append(" 笔")
				.AppendLine();
			int num = 0;
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				if (num >= maxLines)
				{
					break;
				}
				DebtRecord.DebtLine debtLine = list2[num2];
				string value = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
				string value2 = (debtLine.IsGold ? "金币" : ("物品:" + debtLine.ItemId));
				string value3 = (debtLine.IsGold ? (debtLine.RemainingAmount + " 第纳尔") : ("x" + debtLine.RemainingAmount));
				stringBuilder.Append("- [").Append(debtLine.DebtId).Append("] ")
					.Append(value2)
					.Append("，剩余 ")
					.Append(value3);
				if (!string.IsNullOrWhiteSpace(value))
				{
					stringBuilder.Append("，").Append(value);
				}
				if (!debtLine.IsGold && debtLine.IsItemUnavailableDeclared)
				{
					stringBuilder.Append("，已标记无法归还原物");
				}
				stringBuilder.AppendLine();
				num++;
			}
			if (list2.Count > num)
			{
				stringBuilder.Append("... 还有 ").Append(list2.Count - num).Append(" 笔未显示。");
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "欠款摘要读取失败。";
		}
	}

	private DebtRecord GetOrCreateDebtRecord(Hero npc)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = ((MBObjectBase)npc).StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (!_debts.TryGetValue(stringId, out var value))
		{
			value = new DebtRecord();
			_debts[stringId] = value;
		}
		return value;
	}

	private DebtRecord GetDebtRecord(Hero npc)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = ((MBObjectBase)npc).StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debts.TryGetValue(stringId, out var value))
		{
			return value;
		}
		return null;
	}

	private DebtRecord GetDebtRecordByKey(string debtKey)
	{
		if (string.IsNullOrWhiteSpace(debtKey))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debts.TryGetValue(debtKey, out var value))
		{
			return value;
		}
		return null;
	}

	private DebtRecord GetOrCreateDebtRecordByKey(string debtKey)
	{
		if (string.IsNullOrWhiteSpace(debtKey))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (!_debts.TryGetValue(debtKey, out var value) || value == null)
		{
			value = new DebtRecord();
			_debts[debtKey] = value;
		}
		return value;
	}

	private DebtRecord GetSettlementMerchantDebtRecord(Settlement settlement, SettlementMerchantKind kind)
	{
		return GetDebtRecordByKey(BuildSettlementMerchantDebtKey(settlement, kind));
	}

	public bool HasUnpaidDebt(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		if (debtRecord.OwedGold > 0)
		{
			return true;
		}
		if (debtRecord.OwedItems == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDebtOverdue(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord) || debtRecord.DueDay <= 0f)
		{
			return false;
		}
		return GetNowCampaignDay() > debtRecord.DueDay + 0.01f;
	}

	public int GetDebtDaysToDue(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return 0;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord) || debtRecord.DueDay <= 0f)
		{
			return 0;
		}
		float num = debtRecord.DueDay - GetNowCampaignDay();
		if (num >= 0f)
		{
			return (int)Math.Ceiling(num);
		}
		return -(int)Math.Ceiling(0f - num);
	}

	public void GetDebtSnapshot(Hero npc, out int owedGold, out Dictionary<string, int> owedItems)
	{
		owedGold = 0;
		owedItems = new Dictionary<string, int>();
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return;
		}
		NormalizeDebtRecord(debtRecord);
		owedGold = debtRecord.OwedGold;
		if (debtRecord.OwedItems == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				owedItems[owedItem.Key] = owedItem.Value;
			}
		}
	}

	public void SetDebt(Hero npc, int owedGold, Dictionary<string, int> owedItems, float dueDay = 0f)
	{
		if (npc == null)
		{
			return;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		string stringId = ((MBObjectBase)npc).StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return;
		}
		if (!_debts.TryGetValue(stringId, out var value))
		{
			value = new DebtRecord();
			_debts[stringId] = value;
		}
		value.OwedGold = 0;
		if (value.OwedItems == null)
		{
			value.OwedItems = new Dictionary<string, int>();
		}
		else
		{
			value.OwedItems.Clear();
		}
		value.CreatedDay = 0f;
		value.DueDay = 0f;
		float nowCampaignDay = GetNowCampaignDay();
		float dueDay2 = ((dueDay > 0f) ? dueDay : (nowCampaignDay + 1f));
		value.DebtLines = new List<DebtRecord.DebtLine>();
		if (owedGold > 0)
		{
			value.DebtLines.Add(new DebtRecord.DebtLine
			{
				DebtId = BuildDebtId(),
				IsGold = true,
				ItemId = null,
				IsDueUnlimited = false,
				IsItemUnavailableDeclared = false,
				InitialAmount = Math.Max(0, owedGold),
				RemainingAmount = Math.Max(0, owedGold),
				CreatedDay = nowCampaignDay,
				DueDay = dueDay2,
				BestPreDueCoverage = 0f,
				OnTimePenaltyTierApplied = 0,
				OverduePenaltyDaysApplied = 0,
				LastOverduePenaltyDay = -1,
				OverdueTrustPenaltyPerDay = 0,
				OverdueRelationPenaltyPerDay = 0,
				CompensationUnitPrice = 0,
				CompensationGoldCredit = 0
			});
		}
		if (owedItems != null)
		{
			foreach (KeyValuePair<string, int> owedItem in owedItems)
			{
				if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
				{
					value.DebtLines.Add(new DebtRecord.DebtLine
					{
						DebtId = BuildDebtId(),
						IsGold = false,
						ItemId = owedItem.Key,
						IsDueUnlimited = false,
						IsItemUnavailableDeclared = false,
						InitialAmount = owedItem.Value,
						RemainingAmount = owedItem.Value,
						CreatedDay = nowCampaignDay,
						DueDay = dueDay2,
						BestPreDueCoverage = 0f,
						OnTimePenaltyTierApplied = 0,
						OverduePenaltyDaysApplied = 0,
						LastOverduePenaltyDay = -1,
						OverdueTrustPenaltyPerDay = 0,
						OverdueRelationPenaltyPerDay = 0,
						CompensationUnitPrice = 0,
						CompensationGoldCredit = 0
					});
				}
			}
		}
		NormalizeDebtRecord(value);
		if (!HasDebtContent(value))
		{
			_debts.Remove(stringId);
		}
	}

	private void SetDebtForNpc(Hero npc, int goldAmount, string itemId, int itemAmount, int? dueDays, int? dueAbsDay, bool dueUnlimited, int? overdueTrustPenaltyPreset, int? overdueRelationPenaltyPreset)
	{
		if (npc == null)
		{
			return;
		}
		DebtRecord orCreateDebtRecord = GetOrCreateDebtRecord(npc);
		if (orCreateDebtRecord == null)
		{
			return;
		}
		NormalizeDebtRecord(orCreateDebtRecord);
		float nowCampaignDay = GetNowCampaignDay();
		float num = 0f;
		bool flag = dueUnlimited;
		int overdueTrustPenaltyPerDay = NormalizeLlmPenaltyValue(overdueTrustPenaltyPreset.GetValueOrDefault());
		int overdueRelationPenaltyPerDay = NormalizeLlmPenaltyValue(overdueRelationPenaltyPreset.GetValueOrDefault());
		if (!flag)
		{
			if (dueAbsDay.HasValue && dueAbsDay.Value > 0)
			{
				num = dueAbsDay.Value;
			}
			else
			{
				int num2 = ((!dueDays.HasValue) ? 1 : NormalizeDueDays(dueDays.Value));
				num = nowCampaignDay + (float)num2;
			}
			if (num <= 0f)
			{
				num = nowCampaignDay + 1f;
			}
		}
		if (goldAmount > 0)
		{
			orCreateDebtRecord.DebtLines.Add(new DebtRecord.DebtLine
			{
				DebtId = BuildDebtId(),
				IsGold = true,
				ItemId = null,
				IsDueUnlimited = flag,
				IsItemUnavailableDeclared = false,
				InitialAmount = goldAmount,
				RemainingAmount = goldAmount,
				CreatedDay = nowCampaignDay,
				DueDay = (flag ? 0f : num),
				BestPreDueCoverage = 0f,
				OnTimePenaltyTierApplied = 0,
				OverduePenaltyDaysApplied = 0,
				LastOverduePenaltyDay = -1,
				OverdueTrustPenaltyPerDay = overdueTrustPenaltyPerDay,
				OverdueRelationPenaltyPerDay = overdueRelationPenaltyPerDay,
				CompensationUnitPrice = 0,
				CompensationGoldCredit = 0
			});
		}
		if (!string.IsNullOrEmpty(itemId) && itemAmount > 0)
		{
			orCreateDebtRecord.DebtLines.Add(new DebtRecord.DebtLine
			{
				DebtId = BuildDebtId(),
				IsGold = false,
				ItemId = itemId,
				IsDueUnlimited = flag,
				IsItemUnavailableDeclared = false,
				InitialAmount = itemAmount,
				RemainingAmount = itemAmount,
				CreatedDay = nowCampaignDay,
				DueDay = (flag ? 0f : num),
				BestPreDueCoverage = 0f,
				OnTimePenaltyTierApplied = 0,
				OverduePenaltyDaysApplied = 0,
				LastOverduePenaltyDay = -1,
				OverdueTrustPenaltyPerDay = overdueTrustPenaltyPerDay,
				OverdueRelationPenaltyPerDay = overdueRelationPenaltyPerDay,
				CompensationUnitPrice = 0,
				CompensationGoldCredit = 0
			});
		}
		NormalizeDebtRecord(orCreateDebtRecord);
		if (!HasDebtContent(orCreateDebtRecord) && !string.IsNullOrEmpty(((MBObjectBase)npc).StringId))
		{
			_debts.Remove(((MBObjectBase)npc).StringId);
		}
	}

	private void SetDebtForSettlementMerchant(Settlement settlement, SettlementMerchantKind kind, int goldAmount, string itemId, int itemAmount, int? dueDays, int? dueAbsDay, bool dueUnlimited, int? overdueTrustPenaltyPreset)
	{
		string text = BuildSettlementMerchantDebtKey(settlement, kind);
		DebtRecord orCreateDebtRecordByKey = GetOrCreateDebtRecordByKey(text);
		if (orCreateDebtRecordByKey == null)
		{
			return;
		}
		NormalizeDebtRecord(orCreateDebtRecordByKey);
		float nowCampaignDay = GetNowCampaignDay();
		float num = 0f;
		bool flag = dueUnlimited;
		int overdueTrustPenaltyPerDay = NormalizeLlmPenaltyValue(overdueTrustPenaltyPreset.GetValueOrDefault());
		if (!flag)
		{
			if (dueAbsDay.HasValue && dueAbsDay.Value > 0)
			{
				num = dueAbsDay.Value;
			}
			else
			{
				int num2 = ((!dueDays.HasValue) ? 1 : NormalizeDueDays(dueDays.Value));
				num = nowCampaignDay + (float)num2;
			}
			if (num <= 0f)
			{
				num = nowCampaignDay + 1f;
			}
		}
		if (goldAmount > 0)
		{
			orCreateDebtRecordByKey.DebtLines.Add(new DebtRecord.DebtLine
			{
				DebtId = BuildDebtId(),
				IsGold = true,
				InitialAmount = goldAmount,
				RemainingAmount = goldAmount,
				CreatedDay = nowCampaignDay,
				DueDay = (flag ? 0f : num),
				IsDueUnlimited = flag,
				OverdueTrustPenaltyPerDay = overdueTrustPenaltyPerDay,
				OverdueRelationPenaltyPerDay = 0
			});
		}
		if (!string.IsNullOrWhiteSpace(itemId) && itemAmount > 0)
		{
			orCreateDebtRecordByKey.DebtLines.Add(new DebtRecord.DebtLine
			{
				DebtId = BuildDebtId(),
				IsGold = false,
				ItemId = itemId,
				InitialAmount = itemAmount,
				RemainingAmount = itemAmount,
				CreatedDay = nowCampaignDay,
				DueDay = (flag ? 0f : num),
				IsDueUnlimited = flag,
				OverdueTrustPenaltyPerDay = overdueTrustPenaltyPerDay,
				OverdueRelationPenaltyPerDay = 0
			});
		}
		NormalizeDebtRecord(orCreateDebtRecordByKey);
		if (!HasDebtContent(orCreateDebtRecordByKey))
		{
			_debts.Remove(text);
		}
	}

	public bool RegisterPlayerPayment(Hero npc, bool isGold, string itemId, int amount)
	{
		return false;
	}

	private bool TryFindDebtLineById(Hero npc, string debtId, out DebtRecord rec, out DebtRecord.DebtLine line, out string statusText)
	{
		statusText = "";
		rec = null;
		line = null;
		if (npc == null || string.IsNullOrWhiteSpace(debtId))
		{
			statusText = "参数无效：缺少NPC或债务ID。";
			return false;
		}
		rec = GetDebtRecord(npc);
		if (rec == null)
		{
			statusText = "未找到该NPC的债务记录。";
			return false;
		}
		NormalizeDebtRecord(rec);
		if (rec.DebtLines == null || rec.DebtLines.Count <= 0)
		{
			statusText = "该NPC当前没有可还债务。";
			return false;
		}
		line = rec.DebtLines.FirstOrDefault((DebtRecord.DebtLine x) => x != null && string.Equals(x.DebtId ?? "", debtId.Trim(), StringComparison.OrdinalIgnoreCase));
		if (line == null || line.RemainingAmount <= 0)
		{
			statusText = "未找到可结算的债务ID，或该债务已清。";
			return false;
		}
		return true;
	}

	private bool ApplyDebtLinePayment(Hero npc, DebtRecord rec, DebtRecord.DebtLine line, int amount, out string statusText)
	{
		statusText = "";
		if (npc == null || rec == null || line == null || amount <= 0)
		{
			return false;
		}
		if (line.InitialAmount <= 0)
		{
			line.InitialAmount = Math.Max(1, line.RemainingAmount);
		}
		if (line.InitialAmount < line.RemainingAmount)
		{
			line.InitialAmount = line.RemainingAmount;
		}
		float nowCampaignDay = GetNowCampaignDay();
		bool isDueUnlimited = line.IsDueUnlimited;
		bool flag = !isDueUnlimited && line.DueDay > 0f && nowCampaignDay > line.DueDay + 0.01f;
		bool flag2 = !line.IsGold && line.IsItemUnavailableDeclared;
		int remainingAmount = line.RemainingAmount;
		line.RemainingAmount = Math.Max(0, line.RemainingAmount - amount);
		int remainingAmount2 = line.RemainingAmount;
		float num = ComputeCoverageRatio(line.InitialAmount, remainingAmount2);
		if (!flag && !isDueUnlimited)
		{
			line.BestPreDueCoverage = Math.Max(line.BestPreDueCoverage, num);
		}
		string text = BuildDebtDueStatusText(line.DueDay, line.IsDueUnlimited);
		statusText = $"债务ID {line.DebtId}：{remainingAmount} -> {remainingAmount2}";
		if (!string.IsNullOrWhiteSpace(text))
		{
			statusText = statusText + "（" + text + "）";
		}
		if (remainingAmount2 > 0)
		{
			if (flag2)
			{
				statusText += "；该笔已声明无法归还原物：关系/信任由 LLM 决策，本次不自动奖惩。";
			}
			else if (isDueUnlimited)
			{
				statusText += "；无限期部分还款：本次不追加奖惩。";
			}
			else if (flag)
			{
				statusText += "；逾期部分还款：本次不追加奖惩。";
			}
			else if (num >= 0.95f)
			{
				statusText += "；按时部分还款已达到95%，本次无奖励无惩罚。";
			}
			else
			{
				int num2 = ComputeOnTimePartialPenaltyTier(num);
				if (num2 > line.OnTimePenaltyTierApplied)
				{
					line.OnTimePenaltyTierApplied = num2;
					int appliedUnits;
					int num3 = AdjustTrust(npc, -num2, 0, "partial_on_time_below_95", out appliedUnits);
					AdjustRelationWithPlayer(npc, -num2, "partial_on_time_below_95");
					statusText = statusText + $"；按时部分还款低于95%，按比例惩罚：信任-{num2}" + ((num3 != 0) ? string.Format("，公共{0}{1}", (num3 > 0) ? "+" : "", num3) : "") + $"，关系-{num2}。";
				}
				else
				{
					statusText += "；按时部分还款低于95%，惩罚已计入，不重复扣减。";
				}
			}
		}
		else if (flag2)
		{
			statusText += "；该笔已声明无法归还原物并结清：关系/信任由 LLM 决策，本次不自动奖惩。";
		}
		else if (flag)
		{
			if (line.BestPreDueCoverage >= 0.95f)
			{
				statusText += "；到期前已达到95%，虽逾期结清但不奖不罚。";
			}
			else
			{
				statusText += "；逾期结清：本次不追加奖惩。";
			}
		}
		else
		{
			int personalDelta = 2;
			int appliedUnits2;
			int num4 = AdjustTrust(npc, personalDelta, 0, "repay_on_time_full", out appliedUnits2);
			statusText = statusText + "；该笔按时结清，信任变化：个人+" + FormatTrustUnits(appliedUnits2) + ((num4 != 0) ? $"，公共+{num4}" : "") + "。";
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec) && !string.IsNullOrWhiteSpace(((MBObjectBase)npc).StringId))
		{
			_debts.Remove(((MBObjectBase)npc).StringId);
			return true;
		}
		return false;
	}

	public bool RegisterPlayerGoldPaymentByDebtId(Hero npc, string debtId, int amount, out string statusText)
	{
		statusText = "";
		if (amount <= 0)
		{
			statusText = "参数无效：还款数量必须大于0。";
			return false;
		}
		if (!TryFindDebtLineById(npc, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		if (!line.IsGold)
		{
			statusText = "债务ID " + line.DebtId + " 是物品债，默认应使用 [ACTION:DEBT_PAY_ITEM:债务ID:物品ID:数量]；若物品遗失且已协商赔偿，可用 [ACTION:DEBT_PAY_ITEM_GOLD:债务ID:金额]，并可用 [ACTION:DEBT_ITEM_PENALTY:债务ID:信任扣减:关系扣减] 由 LLM 决定扣减。";
			return false;
		}
		int num = ConsumePlayerPrepaidGold(npc, amount);
		int num2 = Math.Max(0, amount - num);
		int num3 = 0;
		if (num2 > 0)
		{
			num3 = TransferGold(Hero.MainHero, npc, num2);
		}
		int num4 = num + num3;
		if (num4 <= 0)
		{
			statusText = "还款失败：玩家当前第纳尔不足。";
			return false;
		}
		string statusText2;
		bool result = ApplyDebtLinePayment(npc, rec, line, num4, out statusText2);
		if (num > 0)
		{
			statusText = $"本轮已先行交付 {num} 第纳尔，补扣 {num3} 第纳尔；{statusText2}";
		}
		else
		{
			statusText = statusText2;
		}
		return result;
	}

	public bool RegisterPlayerItemPaymentByDebtId(Hero npc, string debtId, string itemId, int amount, out string statusText)
	{
		statusText = "";
		if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
		{
			statusText = "参数无效：物品ID为空或数量不正确。";
			return false;
		}
		if (!TryFindDebtLineById(npc, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		if (line.IsGold)
		{
			statusText = "债务ID " + line.DebtId + " 是金币债，必须使用 [ACTION:DEBT_PAY_GOLD:债务ID:数量]。";
			return false;
		}
		if (!string.Equals((line.ItemId ?? "").Trim(), itemId.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			statusText = "物品不匹配：该债务要求 " + line.ItemId + "，你提交的是 " + itemId + "。";
			return false;
		}
		string text = itemId.Trim();
		int num = ConsumePlayerPrepaidItem(npc, text, amount);
		int num2 = Math.Max(0, amount - num);
		int num3 = 0;
		string itemName = null;
		if (num2 > 0)
		{
			num3 = TransferItemById(Hero.MainHero, npc, text, num2, out itemName);
		}
		int num4 = num + num3;
		if (num4 <= 0)
		{
			statusText = "还款失败：玩家当前没有足够的 " + itemId + "。";
			return false;
		}
		string statusText2;
		bool result = ApplyDebtLinePayment(npc, rec, line, num4, out statusText2);
		string text2 = (string.IsNullOrWhiteSpace(itemName) ? itemId : itemName);
		statusText = $"物品还款 {num4} x {text2}（本轮已先行交付 {num}，补扣 {num3}）；{statusText2}";
		return result;
	}

	public bool MarkItemDebtUnavailableById(Hero npc, string debtId, out string statusText)
	{
		statusText = "";
		if (!TryFindDebtLineById(npc, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		if (line.IsGold)
		{
			statusText = "债务ID " + line.DebtId + " 是金币债，不能标记为“无法归还原物”。";
			return false;
		}
		line.IsItemUnavailableDeclared = true;
		NormalizeDebtRecord(rec);
		statusText = "债务ID " + line.DebtId + " 已标记为“无法归还原物”。后续关系/信任改动由 LLM 决策。";
		return true;
	}

	public bool ApplyItemDebtLlmPenaltyById(Hero npc, string debtId, int trustLoss, int relationLoss, out string statusText)
	{
		statusText = "";
		if (!TryFindDebtLineById(npc, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		if (line.IsGold)
		{
			statusText = "债务ID " + line.DebtId + " 是金币债，不适用物品无法归还惩罚标签。";
			return false;
		}
		if (!line.IsItemUnavailableDeclared)
		{
			statusText = "债务ID " + line.DebtId + " 尚未声明“无法归还原物”，请先使用 [ACTION:DEBT_ITEM_UNAVAILABLE:" + line.DebtId + "]。";
			return false;
		}
		int num = NormalizeLlmPenaltyValue(trustLoss);
		int num2 = NormalizeLlmPenaltyValue(relationLoss);
		if (num <= 0 && num2 <= 0)
		{
			statusText = "债务ID " + line.DebtId + "：本次 LLM 决定不追加关系/信任扣减。";
			return false;
		}
		if (num > 0)
		{
			AdjustTrust(npc, -num, 0, "item_unavailable_llm_penalty", out var _);
		}
		if (num2 > 0)
		{
			AdjustRelationWithPlayer(npc, -num2, "item_unavailable_llm_penalty");
		}
		NormalizeDebtRecord(rec);
		statusText = $"债务ID {line.DebtId}：已按 LLM 决策执行扣减（信任-{num}，关系-{num2}）。";
		return false;
	}

	public bool RegisterPlayerItemCompensationByDebtId(Hero npc, string debtId, int goldAmount, out string statusText)
	{
		statusText = "";
		if (goldAmount <= 0)
		{
			statusText = "参数无效：赔偿金额必须大于0。";
			return false;
		}
		if (!TryFindDebtLineById(npc, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		if (line.IsGold)
		{
			statusText = "债务ID " + line.DebtId + " 是金币债，不适用物品赔偿标签。";
			return false;
		}
		ItemObject item = ResolveItemById(line.ItemId);
		ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(npc, item);
		int compensationUnitPrice = Math.Max(1, guidePriceForItemNearHero.UnitPrice);
		line.IsItemUnavailableDeclared = true;
		if (line.CompensationUnitPrice <= 0)
		{
			line.CompensationUnitPrice = compensationUnitPrice;
		}
		int num = Math.Max(1, line.CompensationUnitPrice);
		int num2 = ConsumePlayerPrepaidGold(npc, goldAmount);
		int num3 = Math.Max(0, goldAmount - num2);
		int num4 = 0;
		if (num3 > 0)
		{
			num4 = TransferGold(Hero.MainHero, npc, num3);
		}
		int num5 = num2 + num4;
		if (num5 <= 0)
		{
			statusText = "赔偿失败：玩家当前第纳尔不足。";
			return false;
		}
		line.CompensationGoldCredit = Math.Max(0, line.CompensationGoldCredit) + num5;
		int num6 = line.CompensationGoldCredit / num;
		line.CompensationGoldCredit %= num;
		if (num6 <= 0)
		{
			NormalizeDebtRecord(rec);
			statusText = $"已支付金币赔偿 {num5}（本轮已先行交付 {num2}，补扣 {num4}）；当前折算规则 {num} 第纳尔/个，累计折算进度 {line.CompensationGoldCredit}/{num}。";
			return false;
		}
		int num7 = Math.Min(num6, Math.Max(0, line.RemainingAmount));
		int num8 = Math.Max(0, num6 - num7);
		if (num8 > 0)
		{
			line.CompensationGoldCredit = 0;
		}
		string statusText2;
		bool result = ApplyDebtLinePayment(npc, rec, line, num7, out statusText2);
		if (num8 > 0)
		{
			int num9 = num8 * num;
			statusText = $"金币赔偿 {num5}（本轮已先行交付 {num2}，补扣 {num4}；折算 {num7} 个，单价 {num}）；{statusText2}；超额 {num9} 第纳尔视为额外支付，不自动冲抵其他债务。";
		}
		else
		{
			statusText = $"金币赔偿 {num5}（本轮已先行交付 {num2}，补扣 {num4}；折算 {num7} 个，单价 {num}）；{statusText2}";
		}
		return result;
	}

	private bool TryFindSettlementMerchantDebtLineById(Settlement settlement, SettlementMerchantKind kind, string debtId, out DebtRecord rec, out DebtRecord.DebtLine line, out string statusText)
	{
		statusText = "";
		rec = null;
		line = null;
		if (settlement == null || kind == SettlementMerchantKind.None || string.IsNullOrWhiteSpace(debtId))
		{
			statusText = "参数无效：缺少市场债主或债务ID。";
			return false;
		}
		rec = GetSettlementMerchantDebtRecord(settlement, kind);
		if (rec == null)
		{
			statusText = "未找到该市场的债务记录。";
			return false;
		}
		NormalizeDebtRecord(rec);
		line = rec.DebtLines.FirstOrDefault((DebtRecord.DebtLine x) => x != null && string.Equals(x.DebtId ?? "", debtId.Trim(), StringComparison.OrdinalIgnoreCase));
		if (line == null || line.RemainingAmount <= 0)
		{
			statusText = "未找到可结算的债务ID，或该债务已清。";
			return false;
		}
		return true;
	}

	private bool ApplySettlementMerchantDebtLinePayment(Settlement settlement, SettlementMerchantKind kind, DebtRecord rec, DebtRecord.DebtLine line, int amount, out string statusText)
	{
		statusText = "";
		if (settlement == null || kind == SettlementMerchantKind.None || rec == null || line == null || amount <= 0)
		{
			return false;
		}
		if (line.InitialAmount <= 0)
		{
			line.InitialAmount = Math.Max(1, line.RemainingAmount);
		}
		if (line.InitialAmount < line.RemainingAmount)
		{
			line.InitialAmount = line.RemainingAmount;
		}
		float nowCampaignDay = GetNowCampaignDay();
		bool flag = !line.IsDueUnlimited && line.DueDay > 0f && nowCampaignDay > line.DueDay + 0.01f;
		int remainingAmount = line.RemainingAmount;
		line.RemainingAmount = Math.Max(0, line.RemainingAmount - amount);
		int remainingAmount2 = line.RemainingAmount;
		float num = ComputeCoverageRatio(line.InitialAmount, remainingAmount2);
		if (!flag && !line.IsDueUnlimited)
		{
			line.BestPreDueCoverage = Math.Max(line.BestPreDueCoverage, num);
		}
		statusText = $"债务ID {line.DebtId}：{remainingAmount} -> {remainingAmount2}";
		if (remainingAmount2 > 0)
		{
			if (line.IsDueUnlimited)
			{
				statusText += "；无限期部分还款：本次不追加奖惩。";
			}
			else if (flag)
			{
				statusText += "；逾期部分还款：本次不追加奖惩。";
			}
			else if (num >= 0.95f)
			{
				statusText += "；按时部分还款已达到95%，本次无奖励无惩罚。";
			}
			else
			{
				int num2 = ComputeOnTimePartialPenaltyTier(num);
				if (num2 > line.OnTimePenaltyTierApplied)
				{
					line.OnTimePenaltyTierApplied = num2;
					int appliedUnits;
					int num3 = AdjustSettlementMerchantTrust(settlement, kind, -num2, "merchant_partial_on_time_below_95", out appliedUnits);
					statusText = statusText + $"；按时部分还款低于95%，市场信任-{num2}" + ((num3 != 0) ? string.Format("，公共{0}{1}", (num3 > 0) ? "+" : "", num3) : "") + "。";
				}
			}
		}
		else if (!flag)
		{
			int appliedUnits2;
			int num4 = AdjustSettlementMerchantTrust(settlement, kind, 2, "merchant_repay_on_time_full", out appliedUnits2);
			statusText = statusText + "；该笔按时结清，市场信任+" + FormatTrustUnits(appliedUnits2) + ((num4 != 0) ? $"，公共+{num4}" : "") + "。";
		}
		else if (line.BestPreDueCoverage >= 0.95f)
		{
			statusText += "；到期前已达到95%，虽逾期结清但不奖不罚。";
		}
		else
		{
			statusText += "；逾期结清：本次不追加奖惩。";
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec))
		{
			_debts.Remove(BuildSettlementMerchantDebtKey(settlement, kind));
			return true;
		}
		return false;
	}

	public bool RegisterPlayerGoldPaymentByMerchantDebtId(Settlement settlement, SettlementMerchantKind kind, string debtId, int amount, out string statusText)
	{
		statusText = "";
		if (amount <= 0)
		{
			statusText = "参数无效：还款数量必须大于0。";
			return false;
		}
		if (!TryFindSettlementMerchantDebtLineById(settlement, kind, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		if (!line.IsGold)
		{
			statusText = "债务ID " + line.DebtId + " 是物品债，必须使用 [ACTION:DEBT_PAY_ITEM:债务ID:物品ID:数量]。";
			return false;
		}
		int num = ConsumePlayerPrepaidGoldForMerchant(settlement, kind, amount);
		int num2 = Math.Max(0, amount - num);
		int num3 = 0;
		if (num2 > 0)
		{
			num3 = TransferGoldToSettlement(settlement, Hero.MainHero, num2);
		}
		int num4 = num + num3;
		if (num4 <= 0)
		{
			statusText = "还款失败：玩家当前第纳尔不足。";
			return false;
		}
		string statusText2;
		bool result = ApplySettlementMerchantDebtLinePayment(settlement, kind, rec, line, num4, out statusText2);
		statusText = ((num > 0) ? $"本轮已先行交付 {num} 第纳尔，补扣 {num3} 第纳尔；{statusText2}" : statusText2);
		return result;
	}

	public bool RegisterPlayerItemPaymentByMerchantDebtId(Settlement settlement, SettlementMerchantKind kind, string debtId, string itemId, int amount, out string statusText)
	{
		statusText = "";
		if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
		{
			statusText = "参数无效：物品ID为空或数量不正确。";
			return false;
		}
		if (!TryFindSettlementMerchantDebtLineById(settlement, kind, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		if (line.IsGold)
		{
			statusText = "债务ID " + line.DebtId + " 是金币债，必须使用 [ACTION:DEBT_PAY_GOLD:债务ID:数量]。";
			return false;
		}
		if (!string.Equals((line.ItemId ?? "").Trim(), itemId.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			statusText = "物品不匹配：该债务要求 " + line.ItemId + "，你提交的是 " + itemId + "。";
			return false;
		}
		string text = itemId.Trim();
		int num = ConsumePlayerPrepaidItemForMerchant(settlement, kind, text, amount);
		int num2 = Math.Max(0, amount - num);
		int num3 = 0;
		string itemName = null;
		if (num2 > 0)
		{
			num3 = TransferItemToSettlement(settlement, Hero.MainHero, text, num2, out itemName);
		}
		int num4 = num + num3;
		if (num4 <= 0)
		{
			statusText = "还款失败：玩家当前没有足够的 " + itemId + "。";
			return false;
		}
		string statusText2;
		bool result = ApplySettlementMerchantDebtLinePayment(settlement, kind, rec, line, num4, out statusText2);
		statusText = $"物品还款 {num4} x {(string.IsNullOrWhiteSpace(itemName) ? itemId : itemName)}；{statusText2}";
		return result;
	}

	public void ApplyRewardTags(Hero giver, Hero receiver, ref string responseText)
	{
		SetLastGeneratedNpcFactLines(null);
		if (giver == null || receiver == null || string.IsNullOrEmpty(responseText))
		{
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			Regex regex = new Regex("\\[ACTION:GIVE_GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex2 = new Regex("\\[ACTION:GIVE_ITEM:([a-zA-Z0-9_]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex3 = new Regex("\\[ACTION:DEBT_GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex4 = new Regex("\\[ACTION:DEBT_ADD:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex5 = new Regex("\\[ACTION:DEBT_ITEM:([a-zA-Z0-9_]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex6 = new Regex("\\[ACTION:DEBT_(?:DUE_)?DAYS:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex7 = new Regex("\\[ACTION:DEBT_DUE_ABS_DAY:(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex8 = new Regex("\\[ACTION:DEBT_DUE_DATE:(\\d+):([^\\]:]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex9 = new Regex("\\[ACTION:DEBT_DUE_NONE\\]", RegexOptions.IgnoreCase);
			Regex regex10 = new Regex("\\[ACTION:DEBT_OVERDUE_PRESET:(\\d+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex11 = new Regex("\\[ACTION:DEBT_PAY_GOLD:([a-zA-Z0-9_\\-]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex12 = new Regex("\\[ACTION:DEBT_PAY_ITEM:([a-zA-Z0-9_\\-]+):([a-zA-Z0-9_]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex13 = new Regex("\\[ACTION:DEBT_PAY_ITEM_GOLD:([a-zA-Z0-9_\\-]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex14 = new Regex("\\[ACTION:DEBT_ITEM_UNAVAILABLE:([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regex15 = new Regex("\\[ACTION:DEBT_ITEM_PENALTY:([a-zA-Z0-9_\\-]+):(\\d+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex16 = new Regex("\\[ACTION:DEBT_PAY:([a-zA-Z0-9_\\-]+):(\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex17 = new Regex("\\[ACTION:KINGDOM_SERVICE:(MERCENARY|VASSAL|LEAVE):([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regex18 = new Regex("\\[ACTION:JOIN_MERCENARY:([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regex19 = new Regex("\\[ACTION:JOIN_VASSAL:([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regex20 = new Regex("\\[ACTION:KINGDOM_SERVICE:LEAVE\\]", RegexOptions.IgnoreCase);
			Regex regex21 = new Regex("\\[ACTION:TRADE_TRUST:(-?\\d+)\\]", RegexOptions.IgnoreCase);
			HashSet<string> settledDebtIdsThisRound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int num = 0;
			try
			{
				num = Regex.Matches(responseText ?? "", "\\[ACTION:[^\\]]+\\]", RegexOptions.IgnoreCase).Count;
			}
			catch
			{
				num = 0;
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			Hero obj2 = giver;
			dictionary["giverId"] = ((obj2 != null) ? ((MBObjectBase)obj2).StringId : null) ?? "";
			Hero obj3 = receiver;
			dictionary["receiverId"] = ((obj3 != null) ? ((MBObjectBase)obj3).StringId : null) ?? "";
			dictionary["textLen"] = (responseText ?? "").Length;
			dictionary["actionTagCount"] = num;
			Logger.Obs("Action", "apply_reward_tags_start", dictionary);
			int? dueDaysOverride = null;
			int? dueAbsDayOverride = null;
			bool dueUnlimited = regex9.IsMatch(responseText);
			int? overdueTrustPenaltyPreset = null;
			int? overdueRelationPenaltyPreset = null;
			if (!dueUnlimited)
			{
				MatchCollection matchCollection = regex7.Matches(responseText);
				if (matchCollection != null && matchCollection.Count > 0)
				{
					string value = matchCollection[matchCollection.Count - 1].Groups[1].Value;
					if (int.TryParse(value, out var result))
					{
						if (result < 1)
						{
							result = 1;
						}
						if (result > 200000)
						{
							result = 200000;
						}
						dueAbsDayOverride = result;
					}
				}
			}
			if (!dueUnlimited && !dueAbsDayOverride.HasValue)
			{
				MatchCollection matchCollection2 = regex8.Matches(responseText);
				if (matchCollection2 != null && matchCollection2.Count > 0)
				{
					Match match = matchCollection2[matchCollection2.Count - 1];
					if (match != null && int.TryParse(match.Groups[1].Value, out var result2) && TryParseSeasonToken(match.Groups[2].Value, out var seasonIndexZeroBased) && int.TryParse(match.Groups[3].Value, out var result3))
					{
						dueAbsDayOverride = ToAbsDayFromCalendar(result2, seasonIndexZeroBased, result3);
					}
				}
			}
			if (!dueUnlimited && !dueAbsDayOverride.HasValue)
			{
				MatchCollection matchCollection3 = regex6.Matches(responseText);
				if (matchCollection3 != null && matchCollection3.Count > 0)
				{
					string value2 = matchCollection3[matchCollection3.Count - 1].Groups[1].Value;
					if (int.TryParse(value2, out var result4))
					{
						dueDaysOverride = NormalizeDueDays(result4);
					}
				}
			}
			responseText = regex9.Replace(responseText, string.Empty);
			responseText = regex8.Replace(responseText, string.Empty);
			responseText = regex7.Replace(responseText, string.Empty);
			responseText = regex6.Replace(responseText, string.Empty);
			MatchCollection matchCollection4 = regex10.Matches(responseText);
			if (matchCollection4 != null && matchCollection4.Count > 0)
			{
				Match match2 = matchCollection4[matchCollection4.Count - 1];
				if (match2 != null)
				{
					if (int.TryParse(match2.Groups[1].Value, out var result5))
					{
						overdueTrustPenaltyPreset = NormalizeLlmPenaltyValue(result5);
					}
					if (int.TryParse(match2.Groups[2].Value, out var result6))
					{
						overdueRelationPenaltyPreset = NormalizeLlmPenaltyValue(result6);
					}
				}
			}
			responseText = regex10.Replace(responseText, string.Empty);
			int? num2 = null;
			MatchCollection matchCollection5 = regex21.Matches(responseText);
			if (matchCollection5 != null && matchCollection5.Count > 0)
			{
				Match match3 = matchCollection5[matchCollection5.Count - 1];
				if (match3 != null && int.TryParse(match3.Groups[1].Value, out var result7))
				{
					num2 = NormalizeLlmTrustDeltaValue(result7);
				}
			}
			responseText = regex21.Replace(responseText, string.Empty);
			bool hasGiveTag = regex.IsMatch(responseText) || regex2.IsMatch(responseText);
			bool flag = regex3.IsMatch(responseText) || regex4.IsMatch(responseText) || regex5.IsMatch(responseText);
			if (flag && !hasGiveTag)
			{
				Logger.Log("Logic", "[Reward] 警告: 检测到DEBT标签但没有GIVE标签，欠款不会被记录（需要同时交付物品才能记录欠款）");
			}
			if (flag && (!overdueTrustPenaltyPreset.HasValue || !overdueRelationPenaltyPreset.HasValue))
			{
				Logger.Log("Logic", "[Reward] 警告: 本轮创建欠款未提供 [ACTION:DEBT_OVERDUE_PRESET:信任扣减:关系扣减]，将使用兼容推断。");
			}
			Hero obj4 = giver;
			string giverName = ((obj4 == null) ? null : ((object)obj4.Name)?.ToString()) ?? "某人";
			Hero obj5 = receiver;
			string receiverName = ((obj5 == null) ? null : ((object)obj5.Name)?.ToString()) ?? "某人";
			List<string> giverFacts = new List<string>();
			List<string> receiverFacts = new List<string>();
			bool anyActualGiveToPlayer = false;
			bool anyDebtRecorded = false;
			bool anyDebtPaymentApplied = false;
			bool anyDebtMetaApplied = false;
			bool anyKingdomServiceApplied = false;
			responseText = regex.Replace(responseText, delegate(Match m)
			{
				if (int.TryParse(m.Groups[1].Value, out var result8))
				{
					Hero obj10 = giver;
					TextObject arg = ((obj10 != null) ? obj10.Name : null);
					Hero obj11 = receiver;
					Logger.Log("Logic", $"[Reward] GIVE_GOLD tag 捕获: giver={arg} receiver={((obj11 != null) ? obj11.Name : null)} amount={result8}");
					int num3 = TransferGold(giver, receiver, result8);
					if (num3 > 0)
					{
						giverFacts.Add($"你已经将 {num3} 第纳尔交给 {receiverName}。并进入了{receiverName}的库存");
						receiverFacts.Add($"你从 {giverName} 收到了 {num3} 第纳尔。");
						if (giver != Hero.MainHero && receiver == Hero.MainHero)
						{
							anyActualGiveToPlayer = true;
							ApplyAutoTrustGainFromHeroGiftValue(giver, num3, giverFacts, receiverFacts, giverName);
						}
					}
				}
				return string.Empty;
			});
			responseText = regex2.Replace(responseText, delegate(Match m)
			{
				string value3 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8))
				{
					object[] array = new object[4];
					Hero obj10 = giver;
					array[0] = ((obj10 != null) ? obj10.Name : null);
					Hero obj11 = receiver;
					array[1] = ((obj11 != null) ? obj11.Name : null);
					array[2] = value3;
					array[3] = result8;
					Logger.Log("Logic", string.Format("[Reward] GIVE_ITEM tag 捕获: giver={0} receiver={1} itemId={2} amount={3}", array));
					string itemName;
					int num3 = TransferItemById(giver, receiver, value3, result8, out itemName);
					if (num3 > 0)
					{
						string text = (string.IsNullOrEmpty(itemName) ? value3 : itemName);
						string itemName2 = text;
						ItemObject item = ResolveItemById(value3);
						string text2 = BuildItemValueFactSuffixForExternal(giver ?? receiver, item, num3);
						giverFacts.Add("你已经将 " + FormatItemAmount(num3, item, itemName2) + " 交给 " + receiverName + text2 + "。并进入了" + receiverName + "的库存");
						receiverFacts.Add("你从 " + giverName + " 收到了 " + FormatItemAmount(num3, item, itemName2) + text2 + "。");
						if (num3 < result8)
						{
							string text3 = BuildItemValueFactSuffixForExternal(giver ?? receiver, item, result8);
							giverFacts.Add("你本轮原计划交付 " + FormatItemAmount(result8, item, itemName2) + text3 + "，但实际仅交付 " + FormatItemAmount(num3, item, itemName2) + text2 + "（库存不足）。");
							receiverFacts.Add(giverName + " 原计划交付 " + FormatItemAmount(result8, item, itemName2) + text3 + "，实际仅交付 " + FormatItemAmount(num3, item, itemName2) + text2 + "。");
						}
						if (giver != Hero.MainHero && receiver == Hero.MainHero)
						{
							anyActualGiveToPlayer = true;
							ApplyAutoTrustGainFromHeroGiftValue(giver, GetItemTrustValueForHeroGift(giver ?? receiver, item, num3), giverFacts, receiverFacts, giverName);
						}
					}
					else
					{
						ItemObject obj12 = ResolveItemById(value3);
						string text4 = ((obj12 == null) ? null : ((object)obj12.Name)?.ToString());
						string itemName3 = (string.IsNullOrWhiteSpace(text4) ? "该物品" : text4);
						ItemObject item2 = ResolveItemById(value3);
						string text5 = BuildItemValueFactSuffixForExternal(giver ?? receiver, item2, result8);
						giverFacts.Add("你尝试交付 " + FormatItemAmount(result8, item2, itemName3) + text5 + "，但库存不足，本轮未实际交付。");
						receiverFacts.Add(giverName + " 试图交付 " + FormatItemAmount(result8, item2, itemName3) + text5 + "，但其库存不足，本轮未实际交付。");
					}
				}
				return string.Empty;
			});
			responseText = regex3.Replace(responseText, delegate(Match m)
			{
				//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
				//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
				//IL_02e7: Expected O, but got Unknown
				//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
				//IL_02af: Unknown result type (might be due to invalid IL or missing references)
				//IL_02b9: Expected O, but got Unknown
				if (int.TryParse(m.Groups[1].Value, out var result8))
				{
					object[] array = new object[4];
					Hero obj10 = giver;
					array[0] = ((obj10 != null) ? obj10.Name : null);
					Hero obj11 = receiver;
					array[1] = ((obj11 != null) ? obj11.Name : null);
					array[2] = result8;
					array[3] = hasGiveTag;
					Logger.Log("Logic", string.Format("[Reward] DEBT_GOLD tag 捕获: giver={0} receiver={1} amount={2} hasGiveTag={3}", array));
					if (receiver == Hero.MainHero && giver != Hero.MainHero && hasGiveTag)
					{
						SetDebtForNpc(giver, result8, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset, overdueRelationPenaltyPreset);
						anyDebtRecorded = true;
						DebtRecord debtRecord = GetDebtRecord(giver);
						NormalizeDebtRecord(debtRecord);
						DebtRecord.DebtLine debtLine = (debtRecord?.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.IsGold && x.RemainingAmount > 0)).OrderByDescending((DebtRecord.DebtLine x) => x.CreatedDay).FirstOrDefault();
						string text = ((debtLine != null) ? BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) : "");
						string text2 = debtLine?.DebtId ?? "";
						giverFacts.Add($"你已经记下：玩家欠你 {result8} 第纳尔（债务ID:{text2}）。{text}");
						receiverFacts.Add($"你欠 {giverName} {result8} 第纳尔（债务ID:{text2}）。{text}");
						if (result8 > 0)
						{
							string text3 = (string.IsNullOrWhiteSpace(text) ? "" : ("（" + text + "）"));
							string text4 = (string.IsNullOrWhiteSpace(text2) ? "" : ("[ID:" + text2 + "] "));
							InformationManager.DisplayMessage(new InformationMessage($"【欠款记录】{text4}你欠 {giverName} {result8} 第纳尔{text3}", Color.FromUint(4294936576u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的金币欠款已还清！", Color.FromUint(4278255360u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex4.Replace(responseText, delegate(Match m)
			{
				//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
				//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
				//IL_02e7: Expected O, but got Unknown
				//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
				//IL_02af: Unknown result type (might be due to invalid IL or missing references)
				//IL_02b9: Expected O, but got Unknown
				if (int.TryParse(m.Groups[1].Value, out var result8))
				{
					object[] array = new object[4];
					Hero obj10 = giver;
					array[0] = ((obj10 != null) ? obj10.Name : null);
					Hero obj11 = receiver;
					array[1] = ((obj11 != null) ? obj11.Name : null);
					array[2] = result8;
					array[3] = hasGiveTag;
					Logger.Log("Logic", string.Format("[Reward] DEBT_ADD(兼容) tag 捕获: giver={0} receiver={1} amount={2} hasGiveTag={3}", array));
					if (receiver == Hero.MainHero && giver != Hero.MainHero && hasGiveTag)
					{
						SetDebtForNpc(giver, result8, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset, overdueRelationPenaltyPreset);
						anyDebtRecorded = true;
						DebtRecord debtRecord = GetDebtRecord(giver);
						NormalizeDebtRecord(debtRecord);
						DebtRecord.DebtLine debtLine = (debtRecord?.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.IsGold && x.RemainingAmount > 0)).OrderByDescending((DebtRecord.DebtLine x) => x.CreatedDay).FirstOrDefault();
						string text = ((debtLine != null) ? BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) : "");
						string text2 = debtLine?.DebtId ?? "";
						giverFacts.Add($"你已经记下：玩家欠你 {result8} 第纳尔（债务ID:{text2}）。{text}");
						receiverFacts.Add($"你欠 {giverName} {result8} 第纳尔（债务ID:{text2}）。{text}");
						if (result8 > 0)
						{
							string text3 = (string.IsNullOrWhiteSpace(text) ? "" : ("（" + text + "）"));
							string text4 = (string.IsNullOrWhiteSpace(text2) ? "" : ("[ID:" + text2 + "] "));
							InformationManager.DisplayMessage(new InformationMessage($"【欠款记录】{text4}你欠 {giverName} {result8} 第纳尔{text3}", Color.FromUint(4294936576u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的金币欠款已还清！", Color.FromUint(4278255360u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex5.Replace(responseText, delegate(Match m)
			{
				//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
				//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
				//IL_02f1: Expected O, but got Unknown
				string itemId = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8))
				{
					object[] array = new object[5];
					Hero obj10 = giver;
					array[0] = ((obj10 != null) ? obj10.Name : null);
					Hero obj11 = receiver;
					array[1] = ((obj11 != null) ? obj11.Name : null);
					array[2] = itemId;
					array[3] = result8;
					array[4] = hasGiveTag;
					Logger.Log("Logic", string.Format("[Reward] DEBT_ITEM tag 捕获: giver={0} receiver={1} itemId={2} amount={3} hasGiveTag={4}", array));
					if (receiver == Hero.MainHero && giver != Hero.MainHero && hasGiveTag)
					{
						SetDebtForNpc(giver, 0, itemId, result8, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset, overdueRelationPenaltyPreset);
						anyDebtRecorded = true;
						DebtRecord debtRecord = GetDebtRecord(giver);
						NormalizeDebtRecord(debtRecord);
						DebtRecord.DebtLine debtLine = (debtRecord?.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && !x.IsGold && string.Equals(x.ItemId ?? "", itemId, StringComparison.OrdinalIgnoreCase) && x.RemainingAmount > 0)).OrderByDescending((DebtRecord.DebtLine x) => x.CreatedDay).FirstOrDefault();
						string text = ((debtLine != null) ? BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) : "");
						string text2 = debtLine?.DebtId ?? "";
						giverFacts.Add($"你已经记下：玩家欠你 {itemId} x{result8}（债务ID:{text2}）。{text}");
						receiverFacts.Add($"你欠 {giverName} {itemId} x{result8}（债务ID:{text2}）。{text}");
						string text3 = (string.IsNullOrWhiteSpace(text) ? "" : ("（" + text + "）"));
						string text4 = (string.IsNullOrWhiteSpace(text2) ? "" : ("[ID:" + text2 + "] "));
						InformationManager.DisplayMessage(new InformationMessage($"【欠款记录】{text4}你欠 {giverName} {itemId} x{result8}{text3}", Color.FromUint(4294936576u)));
					}
				}
				return string.Empty;
			});
			responseText = regex11.Replace(responseText, delegate(Match m)
			{
				//IL_017e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0183: Unknown result type (might be due to invalid IL or missing references)
				//IL_018d: Expected O, but got Unknown
				//IL_0154: Unknown result type (might be due to invalid IL or missing references)
				//IL_0159: Unknown result type (might be due to invalid IL or missing references)
				//IL_0163: Expected O, but got Unknown
				string value3 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text = (value3 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !settledDebtIdsThisRound.Add(text))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text + " tag=DEBT_PAY_GOLD");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerGoldPaymentByDebtId(giver, value3, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value3} 的金币还款 {result8}。{statusText}");
						receiverFacts.Add($"你已偿还金币债务ID {value3} 共 {result8}。{statusText}");
						if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"【还款确认】已偿还金币债务ID {value3}：{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex12.Replace(responseText, delegate(Match m)
			{
				//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
				//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
				//IL_01cb: Expected O, but got Unknown
				//IL_0191: Unknown result type (might be due to invalid IL or missing references)
				//IL_0196: Unknown result type (might be due to invalid IL or missing references)
				//IL_01a0: Expected O, but got Unknown
				string value3 = m.Groups[1].Value;
				string value4 = m.Groups[2].Value;
				if (int.TryParse(m.Groups[3].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text = (value3 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !settledDebtIdsThisRound.Add(text))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text + " tag=DEBT_PAY_ITEM");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerItemPaymentByDebtId(giver, value3, value4, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value3} 的物品还款：{value4} x{result8}。{statusText}");
						receiverFacts.Add($"你已偿还物品债务ID {value3}：{value4} x{result8}。{statusText}");
						if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"【还款确认】已偿还物品债务ID {value3}：{value4} x{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex13.Replace(responseText, delegate(Match m)
			{
				//IL_017e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0183: Unknown result type (might be due to invalid IL or missing references)
				//IL_018d: Expected O, but got Unknown
				//IL_0154: Unknown result type (might be due to invalid IL or missing references)
				//IL_0159: Unknown result type (might be due to invalid IL or missing references)
				//IL_0163: Expected O, but got Unknown
				string value3 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text = (value3 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !settledDebtIdsThisRound.Add(text))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text + " tag=DEBT_PAY_ITEM_GOLD");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerItemCompensationByDebtId(giver, value3, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value3} 的物品金币赔偿：{result8}。{statusText}");
						receiverFacts.Add($"你已对物品债务ID {value3} 支付金币赔偿：{result8}。{statusText}");
						if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"【赔偿确认】已按协商支付物品债务ID {value3} 的金币赔偿：{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex14.Replace(responseText, delegate(Match m)
			{
				//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
				//IL_00af: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b9: Expected O, but got Unknown
				string value3 = m.Groups[1].Value;
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = MarkItemDebtUnavailableById(giver, value3, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtMetaApplied = true;
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【债务状态】" + statusText, Color.FromUint(4291611750u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex15.Replace(responseText, delegate(Match m)
			{
				//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
				//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
				//IL_0108: Expected O, but got Unknown
				string value3 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && int.TryParse(m.Groups[3].Value, out var result9) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					ApplyItemDebtLlmPenaltyById(giver, value3, result8, result9, out var statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtMetaApplied = true;
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						if (statusText.IndexOf("执行扣减", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							InformationManager.DisplayMessage(new InformationMessage("【关系变化】" + statusText, Color.FromUint(4294945365u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex16.Replace(responseText, delegate(Match m)
			{
				//IL_0154: Unknown result type (might be due to invalid IL or missing references)
				//IL_0159: Unknown result type (might be due to invalid IL or missing references)
				//IL_0163: Expected O, but got Unknown
				//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
				//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
				//IL_01c3: Expected O, but got Unknown
				//IL_018a: Unknown result type (might be due to invalid IL or missing references)
				//IL_018f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0199: Expected O, but got Unknown
				string value3 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text = (value3 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !settledDebtIdsThisRound.Add(text))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text + " tag=DEBT_PAY(legacy)");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerGoldPaymentByDebtId(giver, value3, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value3} 的还款 {result8}。{statusText}");
						receiverFacts.Add("你尝试偿还债务ID " + value3 + "：" + statusText);
						if (statusText.IndexOf("必须使用 [ACTION:DEBT_PAY_ITEM", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							InformationManager.DisplayMessage(new InformationMessage("【还款失败】该债务是物品债，请使用 [ACTION:DEBT_PAY_ITEM:债务ID:物品ID:数量]。", Color.FromUint(4294923605u)));
						}
						else if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"【还款确认】已偿还债务ID {value3}：{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex17.Replace(responseText, delegate(Match m)
			{
				//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
				//IL_0109: Expected O, but got Unknown
				string serviceType = (m.Groups[1].Value ?? "").Trim();
				string kingdomToken = (m.Groups[2].Value ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, serviceType, kingdomToken, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			responseText = regex18.Replace(responseText, delegate(Match m)
			{
				//IL_00da: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
				//IL_00df: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e9: Expected O, but got Unknown
				string kingdomToken = (m.Groups[1].Value ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, "MERCENARY", kingdomToken, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			responseText = regex19.Replace(responseText, delegate(Match m)
			{
				//IL_00da: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
				//IL_00df: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e9: Expected O, but got Unknown
				string kingdomToken = (m.Groups[1].Value ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, "VASSAL", kingdomToken, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			responseText = regex20.Replace(responseText, delegate
			{
				//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
				//IL_00cb: Expected O, but got Unknown
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, "LEAVE", "current", out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			if (num2.HasValue)
			{
				Logger.Log("Logic", "[Reward] 提示: 检测到 [ACTION:TRADE_TRUST]，但即时交易信任现已改为按NPC实际交付价值自动累计，本标签已忽略。");
			}
			responseText = responseText.Trim();
			stopwatch.Stop();
			Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
			Hero obj6 = giver;
			dictionary2["giverId"] = ((obj6 != null) ? ((MBObjectBase)obj6).StringId : null) ?? "";
			Hero obj7 = receiver;
			dictionary2["receiverId"] = ((obj7 != null) ? ((MBObjectBase)obj7).StringId : null) ?? "";
			dictionary2["anyActualGiveToPlayer"] = anyActualGiveToPlayer;
			dictionary2["anyDebtRecorded"] = anyDebtRecorded;
			dictionary2["anyDebtPaymentApplied"] = anyDebtPaymentApplied;
			dictionary2["anyDebtMetaApplied"] = anyDebtMetaApplied;
			dictionary2["anyKingdomServiceApplied"] = anyKingdomServiceApplied;
			dictionary2["giverFactsCount"] = giverFacts.Count;
			dictionary2["receiverFactsCount"] = receiverFacts.Count;
			dictionary2["textLenAfter"] = (responseText ?? "").Length;
			dictionary2["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
			Logger.Obs("Action", "apply_reward_tags_done", dictionary2);
			Logger.Metric("action.apply_reward_tags", ok: true, stopwatch.Elapsed.TotalMilliseconds);
			if (giverFacts.Count > 0)
			{
				SetLastGeneratedNpcFactLines(new string[1] { "[AFEF NPC行为补充] " + giverName + ": " + string.Join(" ", giverFacts) });
				MyBehavior.AppendExternalNpcFact(giver, string.Join(" ", giverFacts));
			}
			if (receiverFacts.Count > 0)
			{
				if (receiver == Hero.MainHero)
				{
					MyBehavior.AppendExternalPlayerFact(receiver, string.Join(" ", receiverFacts));
				}
				else
				{
					MyBehavior.AppendExternalNpcFact(receiver, string.Join(" ", receiverFacts));
				}
			}
		}
		catch (Exception ex)
		{
			SetLastGeneratedNpcFactLines(null);
			stopwatch.Stop();
			Logger.Log("Logic", "[ERROR] ApplyRewardTags 异常: " + ex.ToString());
			Dictionary<string, object> dictionary3 = new Dictionary<string, object>();
			Hero obj8 = giver;
			dictionary3["giverId"] = ((obj8 != null) ? ((MBObjectBase)obj8).StringId : null) ?? "";
			Hero obj9 = receiver;
			dictionary3["receiverId"] = ((obj9 != null) ? ((MBObjectBase)obj9).StringId : null) ?? "";
			dictionary3["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
			dictionary3["message"] = ex.Message;
			dictionary3["type"] = ex.GetType().Name;
			Logger.Obs("Action", "apply_reward_tags_error", dictionary3);
			Logger.Metric("action.apply_reward_tags", ok: false, stopwatch.Elapsed.TotalMilliseconds);
		}
	}

	public void ApplyMerchantRewardTags(CharacterObject giverCharacter, Hero receiver, ref string responseText)
	{
		SetLastGeneratedNpcFactLines(null);
		if (giverCharacter == null || receiver == null || string.IsNullOrEmpty(responseText) || !TryGetSettlementMerchantKind(giverCharacter, out var kind))
		{
			return;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null || !currentSettlement.IsTown)
		{
			responseText = Regex.Replace(responseText ?? "", "\\[ACTION:[^\\]]+\\]", string.Empty, RegexOptions.IgnoreCase).Trim();
			return;
		}
		string giverName = ((object)((BasicCharacterObject)giverCharacter).Name)?.ToString() ?? GetSettlementMerchantRoleLabel(kind);
		Regex regex = new Regex("\\[ACTION:GIVE_GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex2 = new Regex("\\[ACTION:GIVE_ITEM:([a-zA-Z0-9_@\\-]+):(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex3 = new Regex("\\[ACTION:DEBT_GOLD:(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex4 = new Regex("\\[ACTION:DEBT_ADD:(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex5 = new Regex("\\[ACTION:DEBT_ITEM:([a-zA-Z0-9_@\\-]+):(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex6 = new Regex("\\[ACTION:DEBT_(?:DUE_)?DAYS:(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex7 = new Regex("\\[ACTION:DEBT_DUE_ABS_DAY:(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex8 = new Regex("\\[ACTION:DEBT_DUE_DATE:(\\d+):([^\\]:]+):(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex9 = new Regex("\\[ACTION:DEBT_DUE_NONE\\]", RegexOptions.IgnoreCase);
		Regex regex10 = new Regex("\\[ACTION:DEBT_OVERDUE_PRESET:(\\d+):(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex11 = new Regex("\\[ACTION:DEBT_PAY_GOLD:([a-zA-Z0-9_\\-]+):(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex12 = new Regex("\\[ACTION:DEBT_PAY_ITEM:([a-zA-Z0-9_\\-]+):([a-zA-Z0-9_@\\-]+):(\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex13 = new Regex("\\[ACTION:TRADE_TRUST:(-?\\d+)\\]", RegexOptions.IgnoreCase);
		List<string> merchantFacts = new List<string>();
		List<string> playerFacts = new List<string>();
		int? dueDaysOverride = null;
		int? dueAbsDayOverride = null;
		bool dueUnlimited = regex9.IsMatch(responseText);
		int? overdueTrustPenaltyPreset = null;
		if (!dueUnlimited)
		{
			MatchCollection matchCollection = regex7.Matches(responseText);
			if (matchCollection != null && matchCollection.Count > 0 && int.TryParse(matchCollection[matchCollection.Count - 1].Groups[1].Value, out var result))
			{
				dueAbsDayOverride = Math.Max(1, Math.Min(200000, result));
			}
		}
		if (!dueUnlimited && !dueAbsDayOverride.HasValue)
		{
			MatchCollection matchCollection2 = regex8.Matches(responseText);
			if (matchCollection2 != null && matchCollection2.Count > 0)
			{
				Match match = matchCollection2[matchCollection2.Count - 1];
				if (match != null && int.TryParse(match.Groups[1].Value, out var result2) && TryParseSeasonToken(match.Groups[2].Value, out var seasonIndexZeroBased) && int.TryParse(match.Groups[3].Value, out var result3))
				{
					dueAbsDayOverride = ToAbsDayFromCalendar(result2, seasonIndexZeroBased, result3);
				}
			}
		}
		if (!dueUnlimited && !dueAbsDayOverride.HasValue)
		{
			MatchCollection matchCollection3 = regex6.Matches(responseText);
			if (matchCollection3 != null && matchCollection3.Count > 0 && int.TryParse(matchCollection3[matchCollection3.Count - 1].Groups[1].Value, out var result4))
			{
				dueDaysOverride = NormalizeDueDays(result4);
			}
		}
		MatchCollection matchCollection4 = regex10.Matches(responseText);
		if (matchCollection4 != null && matchCollection4.Count > 0 && int.TryParse(matchCollection4[matchCollection4.Count - 1].Groups[1].Value, out var result5))
		{
			overdueTrustPenaltyPreset = NormalizeLlmPenaltyValue(result5);
		}
		bool flag = dueUnlimited || dueAbsDayOverride.HasValue || dueDaysOverride.HasValue || overdueTrustPenaltyPreset.HasValue;
		responseText = regex10.Replace(responseText, string.Empty);
		responseText = regex9.Replace(responseText, string.Empty);
		responseText = regex8.Replace(responseText, string.Empty);
		responseText = regex7.Replace(responseText, string.Empty);
		responseText = regex6.Replace(responseText, string.Empty);
		bool hasGiveTag = regex.IsMatch(responseText) || regex2.IsMatch(responseText);
		responseText = regex.Replace(responseText, delegate(Match m)
		{
			if (int.TryParse(m.Groups[1].Value, out var result7))
			{
				int num = TransferGoldFromSettlement(currentSettlement, receiver, result7, giverName);
				if (num > 0)
				{
					merchantFacts.Add($"你已经将 {num} 第纳尔交给玩家。并进入了玩家的的库存");
					playerFacts.Add($"你从 {giverName} 收到了 {num} 第纳尔。");
					ApplyAutoTrustGainFromMerchantGiftValue(currentSettlement, kind, num, merchantFacts, playerFacts, giverName);
				}
				else
				{
					merchantFacts.Add($"你试图交付 {result7} 第纳尔，但当前商铺现钱不足，本轮未实际支付。");
				}
			}
			return string.Empty;
		});
		responseText = regex2.Replace(responseText, delegate(Match m)
		{
			string value = m.Groups[1].Value;
			if (int.TryParse(m.Groups[2].Value, out var result7))
			{
				string itemName;
				int num = TransferItemFromSettlement(currentSettlement, receiver, value, result7, giverName, out itemName);
				string itemName2 = ((!string.IsNullOrWhiteSpace(itemName)) ? itemName : ResolveSettlementMerchantDisplayNameFromPromptStringId(value));
				ItemObject item = ResolveItemById(value.Split('@')[0]);
				if (num > 0)
				{
					string text = BuildSettlementItemValueFactSuffixForExternal(currentSettlement, item, num);
					merchantFacts.Add("你已经将 " + FormatItemAmount(num, item, itemName2) + " 交给玩家" + text + "。并进入了玩家的的库存");
					playerFacts.Add("你从 " + giverName + " 收到了 " + FormatItemAmount(num, item, itemName2) + text + "。");
					ApplyAutoTrustGainFromMerchantGiftValue(currentSettlement, kind, GetItemTrustValueForMerchantGift(currentSettlement, item, num), merchantFacts, playerFacts, giverName);
					if (num < result7)
					{
						string text2 = BuildSettlementItemValueFactSuffixForExternal(currentSettlement, item, result7);
						merchantFacts.Add("你原本打算交付 " + FormatItemAmount(result7, item, itemName2) + text2 + "，但当前商铺库存不足，实际只交付了 " + FormatItemAmount(num, item, itemName2) + text + "。");
						playerFacts.Add(giverName + " 原本打算交付 " + FormatItemAmount(result7, item, itemName2) + text2 + "，但实际只交付了 " + FormatItemAmount(num, item, itemName2) + text + "。");
					}
				}
				else
				{
					string text3 = BuildSettlementItemValueFactSuffixForExternal(currentSettlement, item, result7);
					merchantFacts.Add("你试图交付 " + FormatItemAmount(result7, item, itemName2) + text3 + "，但当前商铺库存不足，本轮未实际交货。");
				}
			}
			return string.Empty;
		});
		responseText = regex3.Replace(responseText, delegate(Match m)
		{
			if (receiver == Hero.MainHero && hasGiveTag && int.TryParse(m.Groups[1].Value, out var result7) && result7 > 0)
			{
				SetDebtForSettlementMerchant(currentSettlement, kind, result7, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset);
				merchantFacts.Add($"你已经把玩家欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 的 {result7} 第纳尔记入账目。");
				playerFacts.Add($"你欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} {result7} 第纳尔。");
			}
			return string.Empty;
		});
		responseText = regex4.Replace(responseText, delegate(Match m)
		{
			if (receiver == Hero.MainHero && hasGiveTag && int.TryParse(m.Groups[1].Value, out var result7) && result7 > 0)
			{
				SetDebtForSettlementMerchant(currentSettlement, kind, result7, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset);
				merchantFacts.Add($"你已经把玩家欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 的 {result7} 第纳尔记入账目。");
				playerFacts.Add($"你欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} {result7} 第纳尔。");
			}
			return string.Empty;
		});
		responseText = regex5.Replace(responseText, delegate(Match m)
		{
			string value = m.Groups[1].Value;
			if (receiver == Hero.MainHero && hasGiveTag && int.TryParse(m.Groups[2].Value, out var result7) && result7 > 0)
			{
				SetDebtForSettlementMerchant(currentSettlement, kind, 0, value, result7, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset);
				merchantFacts.Add($"你已经把玩家欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 的 {value} x{result7} 记入账目。");
				playerFacts.Add($"你欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} {value} x{result7}。");
			}
			return string.Empty;
		});
		responseText = regex11.Replace(responseText, delegate(Match m)
		{
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Expected O, but got Unknown
			if (int.TryParse(m.Groups[2].Value, out var result7))
			{
				string statusText;
				bool flag2 = RegisterPlayerGoldPaymentByMerchantDebtId(currentSettlement, kind, m.Groups[1].Value, result7, out statusText);
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					merchantFacts.Add(statusText);
					playerFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【市场欠款】" : "【市场还款失败】") + statusText, flag2 ? Color.FromUint(4278255360u) : Color.FromUint(4294923605u)));
				}
			}
			return string.Empty;
		});
		responseText = regex12.Replace(responseText, delegate(Match m)
		{
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Expected O, but got Unknown
			if (int.TryParse(m.Groups[3].Value, out var result7))
			{
				string statusText;
				bool flag2 = RegisterPlayerItemPaymentByMerchantDebtId(currentSettlement, kind, m.Groups[1].Value, m.Groups[2].Value, result7, out statusText);
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					merchantFacts.Add(statusText);
					playerFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【市场欠款】" : "【市场还款失败】") + statusText, flag2 ? Color.FromUint(4278255360u) : Color.FromUint(4294923605u)));
				}
			}
			return string.Empty;
		});
		if (regex13.IsMatch(responseText))
		{
			MatchCollection matchCollection5 = regex13.Matches(responseText);
			if (matchCollection5 != null && matchCollection5.Count > 0 && int.TryParse(matchCollection5[matchCollection5.Count - 1].Groups[1].Value, out var result6))
			{
				NormalizeLlmTrustDeltaValue(result6);
				Logger.Log("Logic", "[Reward] 提示: 非Hero商贩检测到 [ACTION:TRADE_TRUST]，但即时交易信任现已改为按实际交付价值自动累计，本标签已忽略。");
			}
			responseText = regex13.Replace(responseText, string.Empty);
		}
		responseText = responseText.Trim();
		if (merchantFacts.Count > 0)
		{
			SetLastGeneratedNpcFactLines(new string[1] { "[AFEF NPC行为补充] " + giverName + ": " + string.Join(" ", merchantFacts) });
			AppendSettlementMerchantNpcFact(currentSettlement, kind, string.Join(" ", merchantFacts), giverName);
		}
		if (playerFacts.Count > 0 && receiver == Hero.MainHero)
		{
			MyBehavior.AppendExternalPlayerFact(receiver, string.Join(" ", playerFacts));
		}
	}

	internal int TransferGold(Hero giver, Hero receiver, int amount)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		if (amount <= 0)
		{
			return 0;
		}
		int heroGold = GetHeroGold(giver);
		int num = Math.Min(amount, heroGold);
		if (num <= 0)
		{
			return 0;
		}
		GiveGoldAction.ApplyBetweenCharacters(giver, receiver, num, false);
		if (receiver == Hero.MainHero)
		{
			string arg = ((giver == null) ? null : ((object)giver.Name)?.ToString()) ?? "某人";
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {num} 第纳尔。"));
		}
		else if (giver == Hero.MainHero)
		{
			string arg2 = ((receiver == null) ? null : ((object)receiver.Name)?.ToString()) ?? "某人";
			InformationManager.DisplayMessage(new InformationMessage($"你给了 {arg2} {num} 第纳尔。"));
		}
		return num;
	}

	internal int TransferGoldFromSettlement(Settlement settlement, Hero receiver, int amount, string giverName = null)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		if (settlement == null || receiver == null || amount <= 0)
		{
			return 0;
		}
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		if (settlementComponent == null)
		{
			return 0;
		}
		int num = Math.Min(amount, Math.Max(0, settlementComponent.Gold));
		if (num <= 0)
		{
			return 0;
		}
		settlementComponent.ChangeGold(-num);
		receiver.ChangeHeroGold(num);
		if (receiver == Hero.MainHero)
		{
			string arg = ((!string.IsNullOrWhiteSpace(giverName)) ? giverName : (((object)settlement.Name)?.ToString() ?? "这座城镇的商人"));
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {num} 第纳尔。"));
		}
		return num;
	}

	internal int TransferGoldToSettlement(Settlement settlement, Hero giver, int amount)
	{
		if (settlement == null || giver == null || amount <= 0)
		{
			return 0;
		}
		int num = Math.Min(amount, Math.Max(0, giver.Gold));
		if (num <= 0)
		{
			return 0;
		}
		GiveGoldAction.ApplyBetweenCharacters(giver, (Hero)null, num, true);
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		if (settlementComponent != null)
		{
			settlementComponent.ChangeGold(num);
		}
		return num;
	}

	private static int MoveMatchingItemsByStringId(ItemRoster sourceRoster, ItemRoster targetRoster, string itemStringId, int amount, out EquipmentElement firstTransferredElement)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		firstTransferredElement = EquipmentElement.Invalid;
		if (sourceRoster == null || targetRoster == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		string b = itemStringId.Trim();
		int num = amount;
		int num2 = 0;
		while (num > 0)
		{
			bool flag = false;
			for (int i = 0; i < sourceRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = sourceRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
				if (item == null || ((ItemRosterElement)(ref elementCopyAtIndex)).Amount <= 0 || !string.Equals(((MBObjectBase)item).StringId ?? "", b, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				int num3 = Math.Min(((ItemRosterElement)(ref elementCopyAtIndex)).Amount, num);
				if (num3 > 0)
				{
					if (((EquipmentElement)(ref firstTransferredElement)).Item == null)
					{
						firstTransferredElement = equipmentElement;
					}
					sourceRoster.AddToCounts(equipmentElement, -num3);
					targetRoster.AddToCounts(equipmentElement, num3);
					num -= num3;
					num2 += num3;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		return num2;
	}

	internal int TransferItemById(Hero giver, Hero receiver, string itemStringId, int amount, out string itemName)
	{
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Expected O, but got Unknown
		//IL_054a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0554: Expected O, but got Unknown
		itemName = null;
		if (string.IsNullOrEmpty(itemStringId) || amount <= 0)
		{
			return 0;
		}
		if (giver == null || receiver == null)
		{
			return 0;
		}
		ItemRoster val = ((giver.PartyBelongedTo != null) ? giver.PartyBelongedTo.ItemRoster : null);
		if (val == null)
		{
			Clan clan = giver.Clan;
			object obj;
			if (clan == null)
			{
				obj = null;
			}
			else
			{
				Hero leader = clan.Leader;
				obj = ((leader != null) ? leader.PartyBelongedTo : null);
			}
			if (obj != null)
			{
				val = giver.Clan.Leader.PartyBelongedTo.ItemRoster;
			}
		}
		if (val == null && MobileParty.MainParty != null && giver == Hero.MainHero)
		{
			val = MobileParty.MainParty.ItemRoster;
		}
		ItemRoster val2 = ((receiver.PartyBelongedTo != null) ? receiver.PartyBelongedTo.ItemRoster : null);
		if (val2 == null)
		{
			Clan clan2 = receiver.Clan;
			object obj2;
			if (clan2 == null)
			{
				obj2 = null;
			}
			else
			{
				Hero leader2 = clan2.Leader;
				obj2 = ((leader2 != null) ? leader2.PartyBelongedTo : null);
			}
			if (obj2 != null)
			{
				val2 = receiver.Clan.Leader.PartyBelongedTo.ItemRoster;
			}
		}
		if (val2 == null && MobileParty.MainParty != null && receiver == Hero.MainHero)
		{
			val2 = MobileParty.MainParty.ItemRoster;
		}
		if (val2 == null)
		{
			return 0;
		}
		ItemObject val3 = null;
		int num = 0;
		EquipmentElement val4;
		if (val != null)
		{
			for (int i = 0; i < val.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = val.GetElementCopyAtIndex(i);
				val4 = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				ItemObject item = ((EquipmentElement)(ref val4)).Item;
				if (item != null && string.Equals(((MBObjectBase)item).StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
				{
					val3 = item;
					num += ((ItemRosterElement)(ref elementCopyAtIndex)).Amount;
				}
			}
		}
		List<EquipmentIndex> list = new List<EquipmentIndex>();
		EquipmentIndex[] array = new EquipmentIndex[7];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		EquipmentIndex[] array2 = (EquipmentIndex[])(object)array;
		EquipmentIndex[] array3 = array2;
		EquipmentIndex[] array4 = array3;
		EquipmentIndex[] array5 = array4;
		foreach (EquipmentIndex val5 in array5)
		{
			val4 = giver.BattleEquipment[val5];
			ItemObject item2 = ((EquipmentElement)(ref val4)).Item;
			if (item2 != null && string.Equals(((MBObjectBase)item2).StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
			{
				if (val3 == null)
				{
					val3 = item2;
				}
				list.Add(val5);
			}
		}
		num += list.Count;
		if (val3 == null || num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = num2;
		if (val != null)
		{
			num3 -= MoveMatchingItemsByStringId(val, val2, itemStringId, num3, out var firstTransferredElement);
			if (val3 == null && ((EquipmentElement)(ref firstTransferredElement)).Item != null)
			{
				val3 = ((EquipmentElement)(ref firstTransferredElement)).Item;
			}
			if (string.IsNullOrWhiteSpace(itemName) && ((EquipmentElement)(ref firstTransferredElement)).Item != null)
			{
				itemName = ((object)((EquipmentElement)(ref firstTransferredElement)).GetModifiedItemName())?.ToString() ?? ((object)((EquipmentElement)(ref firstTransferredElement)).Item.Name)?.ToString() ?? itemStringId;
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			if (num3 <= 0)
			{
				break;
			}
			EquipmentIndex val6 = list[k];
			EquipmentElement val7 = giver.BattleEquipment[val6];
			ItemObject item3 = ((EquipmentElement)(ref val7)).Item;
			if (item3 != null && string.Equals(((MBObjectBase)item3).StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
			{
				giver.BattleEquipment[val6] = EquipmentElement.Invalid;
				val2.AddToCounts(val7, 1);
				if (val3 == null)
				{
					val3 = item3;
				}
				if (string.IsNullOrWhiteSpace(itemName))
				{
					itemName = ((object)((EquipmentElement)(ref val7)).GetModifiedItemName())?.ToString() ?? ((object)item3.Name)?.ToString() ?? itemStringId;
				}
				num3--;
			}
		}
		if (val3 != null)
		{
			itemName = itemName ?? ((object)val3.Name)?.ToString() ?? itemStringId;
		}
		if (receiver == Hero.MainHero)
		{
			string text = ((giver == null) ? null : ((object)giver.Name)?.ToString()) ?? "某人";
			string itemName2 = itemName ?? itemStringId;
			InformationManager.DisplayMessage(new InformationMessage(text + " 给了你 " + FormatItemAmount(num2, val3, itemName2) + "。"));
		}
		else if (giver == Hero.MainHero)
		{
			string text2 = ((receiver == null) ? null : ((object)receiver.Name)?.ToString()) ?? "某人";
			string itemName3 = itemName ?? itemStringId;
			InformationManager.DisplayMessage(new InformationMessage("你给了 " + text2 + " " + FormatItemAmount(num2, val3, itemName3) + "。"));
		}
		return num2;
	}

	internal int TransferItemFromSettlement(Settlement settlement, Hero receiver, string itemStringId, int amount, string giverName, out string itemName)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Expected O, but got Unknown
		itemName = null;
		if (settlement == null || receiver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		ItemRoster itemRoster = settlement.ItemRoster;
		ItemRoster val = ((receiver.PartyBelongedTo != null) ? receiver.PartyBelongedTo.ItemRoster : null);
		if (itemRoster == null || val == null)
		{
			return 0;
		}
		EquipmentElement val2 = EquipmentElement.Invalid;
		int num = 0;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			if (MatchesSettlementMerchantPromptStringId(((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement, itemStringId))
			{
				val2 = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				num += Math.Max(0, ((ItemRosterElement)(ref elementCopyAtIndex)).Amount);
			}
		}
		if (((EquipmentElement)(ref val2)).Item == null || num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		itemRoster.AddToCounts(val2, -num2);
		val.AddToCounts(val2, num2);
		itemName = BuildSettlementMerchantDisplayName(val2);
		if (receiver == Hero.MainHero)
		{
			string text = ((!string.IsNullOrWhiteSpace(giverName)) ? giverName : (((object)settlement.Name)?.ToString() ?? "这座城镇的商人"));
			InformationManager.DisplayMessage(new InformationMessage(text + " 给了你 " + FormatItemAmount(num2, ((EquipmentElement)(ref val2)).Item, itemName) + "。"));
		}
		return num2;
	}

	internal int TransferItemToSettlement(Settlement settlement, Hero giver, string itemStringId, int amount, out string itemName)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		itemName = null;
		if (settlement == null || giver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		object obj = ((giver.PartyBelongedTo != null) ? giver.PartyBelongedTo.ItemRoster : null);
		if (obj == null)
		{
			MobileParty mainParty = MobileParty.MainParty;
			obj = ((mainParty != null) ? mainParty.ItemRoster : null);
		}
		ItemRoster val = (ItemRoster)obj;
		ItemRoster itemRoster = settlement.ItemRoster;
		if (val == null || itemRoster == null)
		{
			return 0;
		}
		ItemObject val2 = null;
		int num = 0;
		for (int i = 0; i < val.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = val.GetElementCopyAtIndex(i);
			EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
			ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
			if (item != null && string.Equals(((MBObjectBase)item).StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
			{
				val2 = item;
				num += Math.Max(0, ((ItemRosterElement)(ref elementCopyAtIndex)).Amount);
			}
		}
		if (val2 == null || num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		EquipmentElement firstTransferredElement;
		int num3 = MoveMatchingItemsByStringId(val, itemRoster, itemStringId, num2, out firstTransferredElement);
		if (num3 > 0)
		{
			itemName = ((((EquipmentElement)(ref firstTransferredElement)).Item == null) ? (((object)val2.Name)?.ToString() ?? itemStringId) : (((object)((EquipmentElement)(ref firstTransferredElement)).GetModifiedItemName())?.ToString() ?? ((object)((EquipmentElement)(ref firstTransferredElement)).Item.Name)?.ToString() ?? itemStringId));
		}
		return num3;
	}
}
