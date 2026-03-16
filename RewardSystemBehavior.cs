using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

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

	private const int TrustGainOnCleanTrade = 1;

	private const int TrustGainOnOnTimeRepay = 2;

	private const int TrustPenaltyOnOverdueRepay = -2;

	private const float DebtCoverageThreshold = 0.95f;

	private const int OverduePenaltyMaxDays = 7;

	private const int LlmManualPenaltyMax = 10;

	private const int KingdomServiceMercenaryTrustMin = 5;

	private const int KingdomServiceVassalTrustMin = 20;

	private static readonly string[] TrustLevelTexts = new string[10] { "彻底不信", "极度怀疑", "强烈戒备", "不太信任", "保留态度", "中性观望", "基本信任", "较高信任", "高度信任", "完全信赖" };

	private static readonly string[] TrustAiBehaviorTexts = new string[10] { "彻底不信任玩家，优先回避一切经济接触。", "仍高度怀疑玩家，只愿进行极端保守的交易试探。", "明显戒备，允许有限交易但条件苛刻。", "偏不信任，接受交易但会要求更高风险补偿。", "保留态度，交易可谈但会附加明显溢价。", "中性观望，可做常规交易但仍以安全为先。", "达到基本信任，可在可控风险下提供信用交易。", "较高信任，可提供中大额信用交易。", "高度信任，可提供高额信用交易。", "完全信赖，可提供接近无限额的信用交易。" };

	private static readonly string[] TrustAiActionGuideTexts = new string[10] { "L1：拒绝任何经济往来。禁止买卖、禁止赊账、禁止借款；不输出任何新增交易/欠款标签。", "L2：可进行即时交易，但应明显高于 guidePrice 定价以覆盖风险；禁止赊账/借款。", "L3：可进行即时交易，定价仍偏高；禁止赊账/借款。", "L4：可进行即时交易，定价可在“偏高到合理”之间浮动；禁止赊账/借款。", "L5：可进行即时交易，定价趋于合理但可保留风险溢价；禁止赊账/借款。", "L6：可进行即时交易，定价基本合理；禁止赊账/借款。", "L7：可赊账/借款（小到中额），额度需结合NPC财力与局势评估；默认计息。", "L8：可赊账/借款（中到较大额），额度需结合NPC财力与局势评估；默认计息。", "L9：可赊账/借款（大额），额度需结合NPC财力与局势评估；默认计息。", "L10：可赊账/借款（极高额度），仍需结合NPC财力、性格与风险控制；默认计息。" };

	private Dictionary<string, DebtRecord> _debts = new Dictionary<string, DebtRecord>();

	private Dictionary<string, string> _debtStorage = new Dictionary<string, string>();

	private Dictionary<string, int> _npcTrust = new Dictionary<string, int>();

	private Dictionary<string, int> _publicTrust = new Dictionary<string, int>();

	private Dictionary<string, int> _npcTrustStorage = new Dictionary<string, int>();

	private Dictionary<string, int> _publicTrustStorage = new Dictionary<string, int>();

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
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
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
						string value = JsonConvert.SerializeObject(debt.Value);
						_debtStorage[debt.Key] = value;
					}
				}
				catch (Exception ex)
				{
					Logger.Log("RewardSystem", "[ERROR] Serialize debt for " + debt.Key + ": " + ex.Message);
				}
			}
			dataStore.SyncData("_rewardDebts_v2", ref _debtStorage);
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
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
			_merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private static float GetNowCampaignDay()
	{
		try
		{
			return (float)CampaignTime.Now.ToDays;
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
		return (hero?.StringId ?? "").Trim();
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
			foreach (Settlement item in Settlement.All)
			{
				if (item != null && string.Equals((item.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
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
		string text = settlement?.Name?.ToString() ?? "这座城镇";
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
		string text = (settlement?.StringId ?? "").Trim();
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

	private static int ComputeTradePublicTrustDelta(int personalDelta)
	{
		if (personalDelta == 0)
		{
			return 0;
		}
		return (int)Math.Round((double)personalDelta / 3.0, MidpointRounding.AwayFromZero);
	}

	private static int GetCampaignDayIndex()
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
		if (1 == 0)
		{
		}
		string result = num switch
		{
			0 => "春", 
			1 => "夏", 
			2 => "秋", 
			_ => "冬", 
		};
		if (1 == 0)
		{
		}
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
						_merchantFactStorage[merchantFact.Key] = JsonConvert.SerializeObject(merchantFact.Value);
					}
					catch (Exception ex)
					{
						Logger.Log("RewardSystem", "[ERROR] Serialize merchant facts for " + merchantFact.Key + ": " + ex.Message);
					}
				}
			}
		}
		dataStore.SyncData("_rewardMerchantFacts_v1", ref _merchantFactStorage);
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
		dataStore.SyncData("_rewardNpcTrust_v1", ref _npcTrustStorage);
		dataStore.SyncData("_rewardPublicTrust_v1", ref _publicTrustStorage);
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
		string text = (npc?.StringId ?? "").Trim().ToLower();
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
			text = (npc?.MapFaction?.StringId ?? "").Trim().ToLower();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.Clan?.Kingdom?.StringId ?? "").Trim().ToLower();
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
				text = (npc?.Clan?.StringId ?? "").Trim().ToLower();
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
				text = (npc?.Culture?.StringId ?? "").Trim().ToLower();
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
				text = (npc?.StringId ?? "").Trim().ToLower();
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

	private static string BuildPublicTrustLabel(Hero npc)
	{
		try
		{
			string text = npc?.Clan?.Kingdom?.Name?.ToString();
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
			string text2 = npc?.MapFaction?.Name?.ToString();
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
			string text3 = npc?.Clan?.Name?.ToString();
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

	private void AdjustSettlementMerchantTrust(Settlement settlement, SettlementMerchantKind kind, int personalDelta, string reason)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || personalDelta == 0)
		{
			return;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		int settlementMerchantTrust = GetSettlementMerchantTrust(settlement, kind);
		int num = ClampTrust(settlementMerchantTrust + personalDelta);
		if (num == 0)
		{
			_npcTrust.Remove(text);
		}
		else
		{
			_npcTrust[text] = num;
		}
		Logger.Log("Trust", $"settlement={settlement.StringId} market={kind} reason={reason} trust={settlementMerchantTrust}->{num} delta={personalDelta}");
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
				return giver?.Clan?.Kingdom;
			}
			MBReadOnlyList<Kingdom> all = Kingdom.All;
			if (all == null)
			{
				return giver?.Clan?.Kingdom;
			}
			for (int i = 0; i < all.Count; i++)
			{
				Kingdom kingdom = all[i];
				if (kingdom != null && string.Equals((kingdom.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					return kingdom;
				}
			}
			for (int j = 0; j < all.Count; j++)
			{
				Kingdom kingdom2 = all[j];
				if (kingdom2 != null)
				{
					string text2 = (kingdom2.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
					{
						return kingdom2;
					}
				}
			}
		}
		catch
		{
		}
		return giver?.Clan?.Kingdom;
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
				num = (Campaign.Current?.Models?.DiplomacyModel?.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(offerKingdom)).GetValueOrDefault();
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
					for (int i = 0; i < all.Count; i++)
					{
						Kingdom kingdom = all[i];
						if (kingdom != null && Clan.PlayerClan.MapFaction != null && Clan.PlayerClan.MapFaction.IsAtWarWith(kingdom) && kingdom.CurrentTotalStrength > num)
						{
							list.Add(kingdom);
						}
					}
					for (int j = 0; j < all.Count; j++)
					{
						Kingdom kingdom2 = all[j];
						if (kingdom2 != null && offerKingdom.IsAtWarWith(kingdom2))
						{
							list2.Add(kingdom2);
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
			int num2 = list.Intersect(list2).Count();
			return num2 == list.Count;
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
				num = Campaign.Current?.Models?.ClanTierModel?.MercenaryEligibleTier ?? 1;
			}
			catch
			{
				num = 1;
			}
			int num2 = 0;
			try
			{
				num2 = (Campaign.Current?.Models?.DiplomacyModel?.MinimumRelationWithConversationCharacterToJoinKingdom).GetValueOrDefault();
			}
			catch
			{
				num2 = 0;
			}
			bool flag = playerClan.Kingdom == null;
			bool flag2 = !playerClan.IsAtWarWith(offerKingdom);
			bool flag3 = playerClan.Tier >= num;
			bool flag4 = offerKingdom.Leader != null && offerKingdom.Leader.GetRelationWithPlayer() >= (float)num2;
			bool flag5 = playerClan.Settlements == null || playerClan.Settlements.Count <= 0;
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
				num = Campaign.Current?.Models?.ClanTierModel?.VassalEligibleTier ?? 2;
			}
			catch
			{
				num = 2;
			}
			int num2 = 0;
			try
			{
				num2 = (Campaign.Current?.Models?.DiplomacyModel?.MinimumRelationWithConversationCharacterToJoinKingdom).GetValueOrDefault();
			}
			catch
			{
				num2 = 0;
			}
			bool flag = playerClan.Kingdom == null || playerClan.IsUnderMercenaryService;
			bool flag2 = !playerClan.IsAtWarWith(offerKingdom);
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
					ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(playerClan);
					statusText = $"执行成功：玩家已结束与 {kingdom.Name} 的雇佣兵契约。";
					return true;
				}
				ChangeKingdomAction.ApplyByLeaveKingdom(playerClan);
				statusText = $"执行成功：玩家已退出 {kingdom.Name}，不再是其正式封臣。";
				return true;
			}
			Kingdom kingdom2 = ResolveKingdomByTag(kingdomToken, giver);
			if (kingdom2 == null || kingdom2.IsEliminated)
			{
				statusText = "执行失败：目标势力无效（" + kingdomToken + "）。";
				return false;
			}
			Kingdom kingdom3 = giver?.Clan?.Kingdom;
			if (kingdom3 != null && kingdom3 != kingdom2)
			{
				statusText = $"执行失败：{giver.Name} 并非 {kingdom2.Name} 成员，不能代表该势力授予身份。";
				return false;
			}
			if (text == "MERCENARY")
			{
				if (giver == null || giver.Clan == null || giver.Clan.Kingdom != kingdom2)
				{
					statusText = "执行失败：雇佣兵招募必须由目标势力封臣发起（目标势力=" + kingdom2.StringId + "）。";
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
					num = Campaign.Current?.Models?.ClanTierModel?.MercenaryEligibleTier ?? 1;
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
				if (!CanPlayerOfferMercenaryServiceCompat(kingdom2))
				{
					statusText = "执行失败：不满足原版雇佣兵加入条件（势力=" + kingdom2.StringId + "）。";
					return false;
				}
				int num2 = 50;
				try
				{
					num2 = Campaign.Current?.Models?.MinorFactionsModel?.GetMercenaryAwardFactorToJoinKingdom(playerClan, kingdom2) ?? 50;
				}
				catch
				{
					num2 = 50;
				}
				ChangeKingdomAction.ApplyByJoinFactionAsMercenary(playerClan, kingdom2, default(CampaignTime), num2);
				statusText = $"执行成功：玩家已作为雇佣兵加入 {kingdom2.Name}（KingdomId={kingdom2.StringId}）。";
				return true;
			}
			if (text == "VASSAL")
			{
				if (kingdom2.Leader == null || giver == null || giver != kingdom2.Leader)
				{
					string arg = kingdom2.Leader?.Name?.ToString() ?? "该势力领袖";
					statusText = $"执行失败：封臣宣誓必须由 {kingdom2.Name} 的势力领袖 {arg} 亲自确认。请前往与其对话。";
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
					num3 = Campaign.Current?.Models?.ClanTierModel?.VassalEligibleTier ?? 2;
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
				if (playerClan.Kingdom == kingdom2 && !playerClan.IsUnderMercenaryService)
				{
					statusText = $"执行跳过：玩家已是 {kingdom2.Name} 的正式封臣。";
					return false;
				}
				if (!CanPlayerOfferVassalageCompat(kingdom2))
				{
					statusText = "执行失败：不满足原版封臣加入条件（势力=" + kingdom2.StringId + "）。";
					return false;
				}
				ChangeKingdomAction.ApplyByJoinToKingdom(playerClan, kingdom2);
				statusText = $"执行成功：玩家已加入 {kingdom2.Name} 成为正式封臣（KingdomId={kingdom2.StringId}）。";
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

	private void AdjustTrust(Hero npc, int personalDelta, int publicDelta, string reason)
	{
		if (npc == null)
		{
			return;
		}
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		int num = npcTrust;
		int num2 = publicTrust;
		if (personalDelta != 0)
		{
			if (_npcTrust == null)
			{
				_npcTrust = new Dictionary<string, int>();
			}
			string text = BuildNpcTrustKey(npc);
			if (!string.IsNullOrWhiteSpace(text))
			{
				num = ClampTrust(npcTrust + personalDelta);
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
		if (publicDelta != 0)
		{
			if (_publicTrust == null)
			{
				_publicTrust = new Dictionary<string, int>();
			}
			string text2 = BuildPublicTrustKey(npc);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				num2 = ClampTrust(publicTrust + publicDelta);
				if (num2 == 0)
				{
					_publicTrust.Remove(text2);
				}
				else
				{
					_publicTrust[text2] = num2;
				}
			}
		}
		int num3 = ClampTrust(npcTrust + publicTrust);
		int num4 = ClampTrust(num + num2);
		Logger.Log("Trust", $"npc={npc.StringId} reason={reason} personal={npcTrust}->{num} delta={personalDelta} public={publicTrust}->{num2} deltaPublic={publicDelta} effective={num3}->{num4}");
		Logger.Obs("Trust", "change", new Dictionary<string, object>
		{
			["npcId"] = npc.StringId ?? "",
			["reason"] = reason ?? "",
			["personalBefore"] = npcTrust,
			["personalAfter"] = num,
			["publicBefore"] = publicTrust,
			["publicAfter"] = num2,
			["effectiveBefore"] = num3,
			["effectiveAfter"] = num4,
			["personalDelta"] = personalDelta,
			["publicDelta"] = publicDelta
		});
		Logger.Metric("trust.change");
	}

	public void AdjustTrustForExternal(Hero npc, int personalDelta, int publicDelta, string reason = "external")
	{
		AdjustTrust(npc, personalDelta, publicDelta, reason ?? "external");
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
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, npc, delta);
			int relation2 = npc.GetRelation(Hero.MainHero);
			Logger.Log("Trust", $"npc={npc.StringId} relation_reason={reason} relation={relation}->{relation2} delta={delta}");
			Logger.Obs("Relation", "change", new Dictionary<string, object>
			{
				["npcId"] = npc.StringId ?? "",
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
				["npcId"] = npc.StringId ?? "",
				["reason"] = reason ?? "",
				["delta"] = delta,
				["message"] = ex.Message
			});
			Logger.Metric("relation.change", ok: false);
		}
	}

	private void OnDailyTick()
	{
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
				Hero hero = null;
				try
				{
					hero = Hero.Find(item);
				}
				catch
				{
					hero = null;
				}
				if (hero == null)
				{
					if (!TryParseSettlementMerchantDebtKey(item, out var settlementId, out var kind))
					{
						continue;
					}
					Settlement settlement = ResolveSettlementById(settlementId);
					if (settlement == null)
					{
						continue;
					}
					for (int j = 0; j < value.DebtLines.Count; j++)
					{
						DebtRecord.DebtLine debtLine2 = value.DebtLines[j];
						if (debtLine2 == null || debtLine2.RemainingAmount <= 0 || debtLine2.IsDueUnlimited || (!debtLine2.IsGold && debtLine2.IsItemUnavailableDeclared) || debtLine2.DueDay <= 0f || nowCampaignDay <= debtLine2.DueDay + 0.01f)
						{
							continue;
						}
						int num = Math.Max(0, (int)Math.Floor(nowCampaignDay - debtLine2.DueDay));
						if (num > 0 && num <= 7 && !(debtLine2.BestPreDueCoverage >= 0.95f) && debtLine2.OverduePenaltyDaysApplied < 7 && debtLine2.LastOverduePenaltyDay != campaignDayIndex)
						{
							int num2 = NormalizeLlmPenaltyValue(debtLine2.OverdueTrustPenaltyPerDay);
							bool flag = num2 > 0;
							int num3 = 0;
							int num4 = 0;
							if (!flag)
							{
								num3 = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine2);
								num4 = ComputeDailyOverduePenaltySeverity(debtLine2.InitialAmount, debtLine2.RemainingAmount, num3);
								num2 = num4;
							}
							if (num2 > 0)
							{
								AdjustSettlementMerchantTrust(settlement, kind, -num2, flag ? "merchant_overdue_daily_penalty_preset" : "merchant_overdue_daily_penalty_fallback");
							}
							debtLine2.OverduePenaltyDaysApplied++;
							debtLine2.LastOverduePenaltyDay = campaignDayIndex;
							Logger.Log("Trust", string.Format("[OverduePenalty] settlement={0} market={1} debtId={2} mode={3} value={4} trust={5} fallback={6} day={7}/{8}", settlement.StringId, kind, debtLine2.DebtId, flag ? "preset" : "fallback", num3, num2, num4, debtLine2.OverduePenaltyDaysApplied, 7));
						}
					}
					NormalizeDebtRecord(value);
					if (!HasDebtContent(value))
					{
						_debts.Remove(item);
						continue;
					}
					string reminderText = BuildDailyMerchantDebtReminderText(settlement, kind, value);
					if (!string.IsNullOrWhiteSpace(reminderText))
					{
						InformationManager.DisplayMessage(new InformationMessage(reminderText, Color.FromUint(4294945331u)));
					}
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
						int num3 = NormalizeLlmPenaltyValue(debtLine.OverdueRelationPenaltyPerDay);
						bool flag = num2 > 0 || num3 > 0;
						int num4 = 0;
						int num5 = 0;
						if (!flag)
						{
							num4 = EstimateDebtLineRemainingValue(hero, debtLine);
							num5 = ComputeDailyOverduePenaltySeverity(debtLine.InitialAmount, debtLine.RemainingAmount, num4);
							num2 = num5;
							num3 = num5;
						}
						int num6 = ((num2 > 0 && debtLine.OverduePenaltyDaysApplied == 0) ? num2 : 0);
						if (num2 > 0)
						{
							AdjustTrust(hero, -num2, -num6, flag ? "overdue_daily_penalty_preset" : "overdue_daily_penalty_fallback");
						}
						if (num3 > 0)
						{
							AdjustRelationWithPlayer(hero, -num3, flag ? "overdue_daily_penalty_preset" : "overdue_daily_penalty_fallback");
						}
						debtLine.OverduePenaltyDaysApplied++;
						debtLine.LastOverduePenaltyDay = campaignDayIndex;
						Logger.Log("Trust", string.Format("[OverduePenalty] npc={0} debtId={1} mode={2} value={3} trustPersonal={4} trustPublic={5} relation={6} fallback={7} day={8}/{9}", hero.StringId, debtLine.DebtId, flag ? "preset" : "fallback", num4, num2, num6, num3, num5, debtLine.OverduePenaltyDaysApplied, 7));
					}
				}
				NormalizeDebtRecord(value);
				if (!HasDebtContent(value))
				{
					_debts.Remove(item);
					continue;
				}
				string text = BuildDailyDebtReminderText(hero, value);
				if (!string.IsNullOrWhiteSpace(text))
				{
					InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(4294945331u)));
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
		ItemObject item = ResolveItemById(line.ItemId);
		int val = Math.Max(1, line.CompensationUnitPrice);
		if (line.CompensationUnitPrice <= 0)
		{
			try
			{
				if (settlement != null && item != null && TryGetSettlementBuyPrice(settlement, item, out var price))
				{
					val = Math.Max(1, price);
				}
				else
				{
					val = Math.Max(1, item?.Value ?? 1);
				}
			}
			catch
			{
				val = Math.Max(1, item?.Value ?? 1);
			}
		}
		long num = (long)Math.Max(0, line.RemainingAmount) * (long)Math.Max(1, val);
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
		string value = npc.Name?.ToString() ?? "该NPC";
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
		for (int i = 0; i < num; i++)
		{
			DebtRecord.DebtLine debtLine = list[i];
			stringBuilder.Append(" [").Append(debtLine.DebtId).Append("] ");
			if (debtLine.IsGold)
			{
				stringBuilder.Append("金币 ").Append(debtLine.RemainingAmount).Append(" 第纳尔");
			}
			else
			{
				stringBuilder.Append(debtLine.ItemId ?? "物品").Append(" x").Append(debtLine.RemainingAmount);
			}
			string text = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.Append("（").Append(text).Append("）");
			}
			if (i < num - 1)
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
		return hero?.Gold ?? 0;
	}

	private static bool TryResolveHeroMapOrigin(Hero hero, out Vec2 origin)
	{
		origin = Vec2.Invalid;
		try
		{
			if (hero?.CurrentSettlement != null && hero.CurrentSettlement.GatePosition.IsValid())
			{
				origin = hero.CurrentSettlement.GatePosition.ToVec2();
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (hero?.PartyBelongedTo != null && hero.PartyBelongedTo.Position.IsValid())
			{
				origin = hero.PartyBelongedTo.Position.ToVec2();
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty != null && MobileParty.MainParty.Position.IsValid())
			{
				origin = MobileParty.MainParty.Position.ToVec2();
				return true;
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
			return Game.Current?.ObjectManager?.GetObject<ItemObject>(itemId.Trim());
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
			price = settlementComponent.GetItemPrice(item, mainParty);
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
		if (equipmentElement.Item == null)
		{
			return false;
		}
		if (!TryGetSettlementBuyPrice(settlement, equipmentElement.Item, out price))
		{
			return false;
		}
		ItemModifier itemModifier = equipmentElement.ItemModifier;
		if (itemModifier != null)
		{
			price = Math.Max(1, (int)Math.Round((float)price * itemModifier.PriceMultiplier, MidpointRounding.AwayFromZero));
		}
		return price > 0;
	}

	private static string BuildSettlementMerchantInventoryKey(EquipmentElement equipmentElement)
	{
		string text = equipmentElement.Item?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = equipmentElement.ItemModifier?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + "@" + text2;
	}

	private static string BuildSettlementMerchantDisplayName(EquipmentElement equipmentElement)
	{
		if (equipmentElement.Item == null)
		{
			return "";
		}
		return equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? equipmentElement.Item.StringId ?? "";
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
		if (equipmentElement.Item == null || string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		return string.Equals(BuildSettlementMerchantInventoryKey(equipmentElement), promptStringId.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolveSettlementMerchantDisplayNameFromPromptStringId(string promptStringId)
	{
		if (!TryParseSettlementMerchantPromptStringId(promptStringId, out var itemId, out var modifierId))
		{
			return promptStringId ?? "";
		}
		ItemObject itemObject = ResolveItemById(itemId);
		if (itemObject == null)
		{
			return promptStringId ?? "";
		}
		ItemModifier itemModifier = null;
		if (!string.IsNullOrWhiteSpace(modifierId))
		{
			try
			{
				itemModifier = Game.Current?.ObjectManager?.GetObject<ItemModifier>(modifierId);
			}
			catch
			{
				itemModifier = null;
			}
		}
		EquipmentElement equipmentElement = new EquipmentElement(itemObject, itemModifier, null, isQuestItem: false);
		string text = BuildSettlementMerchantDisplayName(equipmentElement);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return itemObject.Name?.ToString() ?? itemId;
	}

	private static string GetItemQuantityUnit(ItemObject item)
	{
		if (item == null)
		{
			return "个";
		}
		ItemCategory itemCategory = item.ItemCategory;
		if (itemCategory == DefaultItemCategories.Beer || itemCategory == DefaultItemCategories.Wine)
		{
			return "罐";
		}
		if (item.IsFood)
		{
			return "斤";
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
		case ItemObject.ItemTypeEnum.Thrown:
		case ItemObject.ItemTypeEnum.SlingStones:
		case ItemObject.ItemTypeEnum.Bullets:
			return "袋";
		case ItemObject.ItemTypeEnum.Polearm:
			return "支";
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			return "把";
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			return "柄";
		case ItemObject.ItemTypeEnum.HeadArmor:
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.LegArmor:
		case ItemObject.ItemTypeEnum.HandArmor:
		case ItemObject.ItemTypeEnum.Cape:
			return "件";
		default:
			return "个";
		}
	}

	private static string FormatItemAmount(int amount, ItemObject item, string itemName)
	{
		return $"{amount}{GetItemQuantityUnit(item)}{itemName}";
	}

	private static int GetSettlementItemStock(Settlement settlement, string itemId)
	{
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
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				if (item != null && elementCopyAtIndex.Amount > 0 && string.Equals(item.StringId ?? "", itemId, StringComparison.OrdinalIgnoreCase))
				{
					num += elementCopyAtIndex.Amount;
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
		ItemGuidePriceInfo itemGuidePriceInfo = new ItemGuidePriceInfo
		{
			UnitPrice = Math.Max(1, item?.Value ?? 1),
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
			foreach (Settlement item3 in Settlement.All)
			{
				if (item3 != null && !item3.IsHideout && item3.IsTown && item3.SettlementComponent != null)
				{
					float item2 = 0f;
					if (flag)
					{
						Vec2 vec = item3.GatePosition.ToVec2();
						float num = vec.x - origin.x;
						float num2 = vec.y - origin.y;
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
				int settlementItemStock = GetSettlementItemStock(tuple.Item1, item.StringId);
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
		kind = SettlementMerchantKind.None;
		if (character == null || character.IsHero)
		{
			return false;
		}
		switch (character.Occupation)
		{
		case Occupation.Weaponsmith:
		case Occupation.Blacksmith:
			kind = SettlementMerchantKind.Weapon;
			return true;
		case Occupation.Armorer:
			kind = SettlementMerchantKind.Armor;
			return true;
		case Occupation.HorseTrader:
			kind = SettlementMerchantKind.Horse;
			return true;
		case Occupation.GoodsTrader:
			kind = SettlementMerchantKind.Goods;
			return true;
		default:
			return false;
		}
	}

	private static string GetSettlementMerchantRoleLabel(SettlementMerchantKind kind)
	{
		return kind switch
		{
			SettlementMerchantKind.Weapon => "武器商人", 
			SettlementMerchantKind.Armor => "盔甲商人", 
			SettlementMerchantKind.Horse => "马匹贩子", 
			SettlementMerchantKind.Goods => "杂货商人", 
			_ => "商贩", 
		};
	}

	private static string GetSettlementMerchantMarketLabel(SettlementMerchantKind kind)
	{
		return kind switch
		{
			SettlementMerchantKind.Weapon => "武器市场", 
			SettlementMerchantKind.Armor => "盔甲市场", 
			SettlementMerchantKind.Horse => "马匹市场", 
			SettlementMerchantKind.Goods => "杂货市场", 
			_ => "城镇市场", 
		};
	}

	private static string GetSettlementMerchantSpecialHint(SettlementMerchantKind kind)
	{
		return kind switch
		{
			SettlementMerchantKind.Weapon => "弓、弩、箭、弩矢、投掷武器和盾牌都归入你的武器市场。", 
			SettlementMerchantKind.Armor => "头盔、身甲、臂甲、腿甲、披风等护具都归入你的盔甲市场。", 
			SettlementMerchantKind.Horse => "马匹与马具都归入你的马匹市场。", 
			SettlementMerchantKind.Goods => "粮食、贸易品和一般杂货都归入你的杂货市场。", 
			_ => "", 
		};
	}

	private static bool MatchesSettlementMerchantKind(ItemObject item, SettlementMerchantKind kind)
	{
		if (item == null)
		{
			return false;
		}
		switch (kind)
		{
		case SettlementMerchantKind.Weapon:
		{
			ItemObject.ItemTypeEnum type2 = item.Type;
			ItemObject.ItemTypeEnum itemTypeEnum2 = type2;
			if ((uint)(itemTypeEnum2 - 2) <= 10u || (uint)(itemTypeEnum2 - 18) <= 2u)
			{
				return true;
			}
			return false;
		}
		case SettlementMerchantKind.Armor:
		{
			ItemObject.ItemTypeEnum type = item.Type;
			ItemObject.ItemTypeEnum itemTypeEnum = type;
			if ((uint)(itemTypeEnum - 14) <= 3u || (uint)(itemTypeEnum - 23) <= 1u)
			{
				return true;
			}
			return false;
		}
		case SettlementMerchantKind.Horse:
			return item.Type == ItemObject.ItemTypeEnum.Horse || item.Type == ItemObject.ItemTypeEnum.HorseHarness;
		case SettlementMerchantKind.Goods:
			return item.Type == ItemObject.ItemTypeEnum.Goods || item.Type == ItemObject.ItemTypeEnum.Animal;
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
		string text = Settlement.CurrentSettlement?.Name?.ToString() ?? "当前城镇";
		string settlementMerchantRoleLabel = GetSettlementMerchantRoleLabel(kind);
		string settlementMerchantMarketLabel = GetSettlementMerchantMarketLabel(kind);
		string settlementMerchantSpecialHint = GetSettlementMerchantSpecialHint(kind);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【城镇商贩补充】你不是在卖你个人的私人物品。");
		stringBuilder.AppendLine("你是" + text + "里的" + settlementMerchantRoleLabel + "，代表这座城镇当前的" + settlementMerchantMarketLabel + "与玩家进行即时交易。");
		stringBuilder.AppendLine("你的真实可售货物只以【当前商铺可用财富与物品】中的清单为准。");
		if (!string.IsNullOrWhiteSpace(settlementMerchantSpecialHint))
		{
			stringBuilder.AppendLine(settlementMerchantSpecialHint);
		}
		return stringBuilder.ToString().Trim();
	}

	public string BuildSettlementMerchantInventorySummaryForAI(CharacterObject character, Settlement settlement = null, int maxItems = 20, bool includeGuidePrice = true)
	{
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
			EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
			ItemObject item = equipmentElement.Item;
			if (item == null || elementCopyAtIndex.Amount <= 0 || !MatchesSettlementMerchantKind(item, kind))
			{
				continue;
			}
			string text = BuildSettlementMerchantInventoryKey(equipmentElement);
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (!dictionary.TryGetValue(text, out var value))
				{
					value = (dictionary[text] = new RewardItemInfo
					{
						Item = item,
						StringId = item.StringId ?? "",
						PromptStringId = text,
						ModifierStringId = (equipmentElement.ItemModifier?.StringId ?? ""),
						Name = BuildSettlementMerchantDisplayName(equipmentElement),
						Count = 0,
						EquipmentElement = equipmentElement
					});
				}
				value.Count += elementCopyAtIndex.Amount;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		int value2 = settlement.SettlementComponent?.Gold ?? 0;
		stringBuilder.Append("Gold: ").Append(value2).AppendLine();
		if (includeGuidePrice)
		{
			stringBuilder.AppendLine("【价格说明】每个物品后面的 guidePrice 为当前城镇市场的即时指导单价（第纳尔/当前单位；箭矢、弩矢、标枪、飞刀等远程弹药按袋计）。");
		}
		stringBuilder.AppendLine("InventoryItems:");
		foreach (RewardItemInfo item2 in dictionary.Values.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal))
		{
			stringBuilder.Append(item2.PromptStringId).Append("|").Append(item2.Name)
				.Append("|")
				.Append(item2.Count)
				.Append(GetItemQuantityUnit(item2.EquipmentElement.Item));
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
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (hero == null)
		{
			return list;
		}
		ItemRoster itemRoster = ((hero.PartyBelongedTo != null) ? hero.PartyBelongedTo.ItemRoster : null);
		if (itemRoster == null && hero.Clan?.Leader?.PartyBelongedTo != null)
		{
			itemRoster = hero.Clan.Leader.PartyBelongedTo.ItemRoster;
		}
		if (itemRoster == null && MobileParty.MainParty != null && hero == Hero.MainHero)
		{
			itemRoster = MobileParty.MainParty.ItemRoster;
		}
		if (itemRoster != null)
		{
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.EquipmentElement.Item != null)
				{
					ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
					int amount = elementCopyAtIndex.Amount;
					if (amount > 0)
					{
						list.Add(new RewardItemInfo
						{
							Item = item,
							StringId = item.StringId,
							Name = (item.Name?.ToString() ?? item.StringId),
							Count = amount
						});
					}
				}
			}
		}
		return list;
	}

	public List<RewardItemInfo> GetHeroBattleEquipmentItems(Hero hero)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (hero == null)
		{
			return list;
		}
		EquipmentIndex[] array = new EquipmentIndex[7]
		{
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Body,
			EquipmentIndex.Leg,
			EquipmentIndex.Gloves,
			EquipmentIndex.Cape,
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1
		};
		EquipmentIndex[] array2 = array;
		EquipmentIndex[] array3 = array2;
		foreach (EquipmentIndex index in array3)
		{
			EquipmentElement equipmentElement = hero.BattleEquipment[index];
			if (equipmentElement.Item != null)
			{
				ItemObject item = equipmentElement.Item;
				list.Add(new RewardItemInfo
				{
					Item = item,
					StringId = item.StringId,
					Name = (item.Name?.ToString() ?? item.StringId),
					Count = 1
				});
			}
		}
		return list;
	}

	private List<RewardItemInfo> GetAgentEquipmentItems(Agent agent)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (agent == null || !agent.IsActive())
		{
			return list;
		}
		EquipmentIndex[] array = new EquipmentIndex[7]
		{
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Body,
			EquipmentIndex.Leg,
			EquipmentIndex.Gloves,
			EquipmentIndex.Cape,
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1
		};
		EquipmentIndex[] array2 = array;
		foreach (EquipmentIndex index in array2)
		{
			ItemObject item = agent.SpawnEquipment[index].Item;
			if (item == null)
			{
				item = agent.Equipment[index].Item;
			}
			if (item != null)
			{
				list.Add(new RewardItemInfo
				{
					Item = item,
					StringId = item.StringId,
					Name = (item.Name?.ToString() ?? item.StringId),
					Count = 1
				});
			}
		}
		return list;
	}

	private List<RewardItemInfo> GetHeroVisibleEquipmentItemsForPrompt(Hero hero)
	{
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

	public string BuildVisibleEquipmentValueSummaryForAI(Hero hero, int maxItems = 8)
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
		Dictionary<string, ItemGuidePriceInfo> dictionary = new Dictionary<string, ItemGuidePriceInfo>(StringComparer.OrdinalIgnoreCase);
		long num = 0L;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【玩家可见装备估值】以下为玩家当前穿戴/携行装备的指导估值（第纳尔）：");
		int num2 = 0;
		foreach (RewardItemInfo item in heroVisibleEquipmentItemsForPrompt.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal))
		{
			if (item != null && item.Item != null)
			{
				string key = item.StringId ?? "";
				if (!dictionary.TryGetValue(key, out var value))
				{
					ItemGuidePriceInfo itemGuidePriceInfo = (dictionary[key] = GetGuidePriceForItemNearHero(hero, item.Item));
					value = itemGuidePriceInfo;
				}
				int num3 = Math.Max(1, value.UnitPrice);
				int num4 = Math.Max(1, item.Count);
				long num5 = (long)num3 * (long)num4;
				num += num5;
				stringBuilder.Append(item.StringId).Append("|").Append(item.Name ?? item.StringId)
					.Append("|")
					.Append(num4)
					.Append(GetItemQuantityUnit(item.Item))
					.Append("|guidePrice=")
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

	public string BuildInventorySummaryForAI(Hero hero, int maxItems = 20, bool includeGuidePrice = true)
	{
		int heroGold = GetHeroGold(hero);
		List<RewardItemInfo> heroInventoryItems = GetHeroInventoryItems(hero);
		List<RewardItemInfo> heroBattleEquipmentItems = GetHeroBattleEquipmentItems(hero);
		string text = hero?.Name?.ToString() ?? "该NPC";
		Dictionary<string, ItemGuidePriceInfo> dictionary = (includeGuidePrice ? new Dictionary<string, ItemGuidePriceInfo>(StringComparer.OrdinalIgnoreCase) : null);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Gold: ").Append(heroGold).AppendLine();
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
					value = itemGuidePriceInfo;
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
						value2 = itemGuidePriceInfo;
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
		for (int i = 0; i < list.Count; i++)
		{
			DebtRecord.DebtLine debtLine = list[i];
			stringBuilder.Append("- [债务ID:").Append(debtLine.DebtId).Append("] ");
			if (debtLine.IsGold)
			{
				stringBuilder.Append("金币，未还 ").Append(debtLine.RemainingAmount).Append(" 第纳尔");
			}
			else
			{
				stringBuilder.Append("物品 ").Append(debtLine.ItemId).Append("，未还 x").Append(debtLine.RemainingAmount);
			}
			string text = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.Append("，").Append(text);
			}
			int num = NormalizeLlmPenaltyValue(debtLine.OverdueTrustPenaltyPerDay);
			if (num > 0)
			{
				stringBuilder.Append("，逾期日惩罚预设：信任 -").Append(num);
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
		string stringId = npc.StringId;
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
		string stringId = npc.StringId;
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
		string stringId = npc.StringId;
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
		if (!HasDebtContent(orCreateDebtRecord) && !string.IsNullOrEmpty(npc.StringId))
		{
			_debts.Remove(npc.StringId);
		}
	}

	private void SetDebtForSettlementMerchant(Settlement settlement, SettlementMerchantKind kind, int goldAmount, string itemId, int itemAmount, int? dueDays, int? dueAbsDay, bool dueUnlimited, int? overdueTrustPenaltyPreset)
	{
		string settlementMerchantDebtKey = BuildSettlementMerchantDebtKey(settlement, kind);
		DebtRecord orCreateDebtRecordByKey = GetOrCreateDebtRecordByKey(settlementMerchantDebtKey);
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
			_debts.Remove(settlementMerchantDebtKey);
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
					AdjustTrust(npc, -num2, 0, "partial_on_time_below_95");
					AdjustRelationWithPlayer(npc, -num2, "partial_on_time_below_95");
					statusText += $"；按时部分还款低于95%，按比例惩罚：信任-{num2}，关系-{num2}。";
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
			int num3 = 2;
			int num4 = 2;
			AdjustTrust(npc, num3, num4, "repay_on_time_full");
			statusText += $"；该笔按时结清，信任变化：个人+{num3}，公共+{num4}。";
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec) && !string.IsNullOrWhiteSpace(npc.StringId))
		{
			_debts.Remove(npc.StringId);
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
			AdjustTrust(npc, -num, 0, "item_unavailable_llm_penalty");
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
					AdjustSettlementMerchantTrust(settlement, kind, -num2, "merchant_partial_on_time_below_95");
					statusText += $"；按时部分还款低于95%，市场信任-{num2}。";
				}
			}
		}
		else if (!flag)
		{
			AdjustSettlementMerchantTrust(settlement, kind, 2, "merchant_repay_on_time_full");
			statusText += "；该笔按时结清，市场信任+2。";
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
			Logger.Obs("Action", "apply_reward_tags_start", new Dictionary<string, object>
			{
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["textLen"] = (responseText ?? "").Length,
				["actionTagCount"] = num
			});
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
			string giverName = giver?.Name?.ToString() ?? "某人";
			string receiverName = receiver?.Name?.ToString() ?? "某人";
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
					Logger.Log("Logic", $"[Reward] GIVE_GOLD tag 捕获: giver={giver?.Name} receiver={receiver?.Name} amount={result8}");
					int num4 = TransferGold(giver, receiver, result8);
					if (num4 > 0)
					{
						giverFacts.Add($"你已经将 {num4} 第纳尔交给 {receiverName}。并进入了{receiverName}的库存");
						receiverFacts.Add($"你从 {giverName} 收到了 {num4} 第纳尔。");
						if (giver != Hero.MainHero && receiver == Hero.MainHero)
						{
							anyActualGiveToPlayer = true;
						}
					}
				}
				return string.Empty;
			});
			responseText = regex2.Replace(responseText, delegate(Match m)
			{
				string value4 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8))
				{
					Logger.Log("Logic", $"[Reward] GIVE_ITEM tag 捕获: giver={giver?.Name} receiver={receiver?.Name} itemId={value4} amount={result8}");
					string itemName;
					int num4 = TransferItemById(giver, receiver, value4, result8, out itemName);
					if (num4 > 0)
					{
						string text3 = (string.IsNullOrEmpty(itemName) ? value4 : itemName);
						string text4 = text3;
						giverFacts.Add($"你已经将 {FormatItemAmount(num4, ResolveItemById(value4), text4)} 交给 {receiverName}。并进入了{receiverName}的库存");
						receiverFacts.Add($"你从 {giverName} 收到了 {FormatItemAmount(num4, ResolveItemById(value4), text4)}。");
						if (num4 < result8)
						{
							giverFacts.Add($"你本轮原计划交付 {FormatItemAmount(result8, ResolveItemById(value4), text4)}，但实际仅交付 {FormatItemAmount(num4, ResolveItemById(value4), text4)}（库存不足）。");
							receiverFacts.Add($"{giverName} 原计划交付 {FormatItemAmount(result8, ResolveItemById(value4), text4)}，实际仅交付 {FormatItemAmount(num4, ResolveItemById(value4), text4)}。");
						}
						if (giver != Hero.MainHero && receiver == Hero.MainHero)
						{
							anyActualGiveToPlayer = true;
						}
					}
					else
					{
						string text5 = ResolveItemById(value4)?.Name?.ToString();
						string text6 = (string.IsNullOrWhiteSpace(text5) ? "该物品" : text5);
						ItemObject itemObject2 = ResolveItemById(value4);
						giverFacts.Add($"你尝试交付 {FormatItemAmount(result8, itemObject2, text6)}，但库存不足，本轮未实际交付。");
						receiverFacts.Add($"{giverName} 试图交付 {FormatItemAmount(result8, itemObject2, text6)}，但其库存不足，本轮未实际交付。");
					}
				}
				return string.Empty;
			});
			responseText = regex3.Replace(responseText, delegate(Match m)
			{
				if (int.TryParse(m.Groups[1].Value, out var result8))
				{
					Logger.Log("Logic", $"[Reward] DEBT_GOLD tag 捕获: giver={giver?.Name} receiver={receiver?.Name} amount={result8} hasGiveTag={hasGiveTag}");
					if (receiver == Hero.MainHero && giver != Hero.MainHero && hasGiveTag)
					{
						SetDebtForNpc(giver, result8, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset, overdueRelationPenaltyPreset);
						anyDebtRecorded = true;
						DebtRecord debtRecord = GetDebtRecord(giver);
						NormalizeDebtRecord(debtRecord);
						DebtRecord.DebtLine debtLine = (debtRecord?.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.IsGold && x.RemainingAmount > 0)).OrderByDescending((DebtRecord.DebtLine x) => x.CreatedDay).FirstOrDefault();
						string text3 = ((debtLine != null) ? BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) : "");
						string text4 = debtLine?.DebtId ?? "";
						giverFacts.Add($"你已经记下：玩家欠你 {result8} 第纳尔（债务ID:{text4}）。{text3}");
						receiverFacts.Add($"你欠 {giverName} {result8} 第纳尔（债务ID:{text4}）。{text3}");
						if (result8 > 0)
						{
							string text5 = (string.IsNullOrWhiteSpace(text3) ? "" : ("（" + text3 + "）"));
							string text6 = (string.IsNullOrWhiteSpace(text4) ? "" : ("[ID:" + text4 + "] "));
							InformationManager.DisplayMessage(new InformationMessage($"【欠款记录】{text6}你欠 {giverName} {result8} 第纳尔{text5}", Color.FromUint(4294936576u)));
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
				if (int.TryParse(m.Groups[1].Value, out var result8))
				{
					Logger.Log("Logic", $"[Reward] DEBT_ADD(兼容) tag 捕获: giver={giver?.Name} receiver={receiver?.Name} amount={result8} hasGiveTag={hasGiveTag}");
					if (receiver == Hero.MainHero && giver != Hero.MainHero && hasGiveTag)
					{
						SetDebtForNpc(giver, result8, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset, overdueRelationPenaltyPreset);
						anyDebtRecorded = true;
						DebtRecord debtRecord = GetDebtRecord(giver);
						NormalizeDebtRecord(debtRecord);
						DebtRecord.DebtLine debtLine = (debtRecord?.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.IsGold && x.RemainingAmount > 0)).OrderByDescending((DebtRecord.DebtLine x) => x.CreatedDay).FirstOrDefault();
						string text3 = ((debtLine != null) ? BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) : "");
						string text4 = debtLine?.DebtId ?? "";
						giverFacts.Add($"你已经记下：玩家欠你 {result8} 第纳尔（债务ID:{text4}）。{text3}");
						receiverFacts.Add($"你欠 {giverName} {result8} 第纳尔（债务ID:{text4}）。{text3}");
						if (result8 > 0)
						{
							string text5 = (string.IsNullOrWhiteSpace(text3) ? "" : ("（" + text3 + "）"));
							string text6 = (string.IsNullOrWhiteSpace(text4) ? "" : ("[ID:" + text4 + "] "));
							InformationManager.DisplayMessage(new InformationMessage($"【欠款记录】{text6}你欠 {giverName} {result8} 第纳尔{text5}", Color.FromUint(4294936576u)));
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
				string itemId = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8))
				{
					Logger.Log("Logic", $"[Reward] DEBT_ITEM tag 捕获: giver={giver?.Name} receiver={receiver?.Name} itemId={itemId} amount={result8} hasGiveTag={hasGiveTag}");
					if (receiver == Hero.MainHero && giver != Hero.MainHero && hasGiveTag)
					{
						SetDebtForNpc(giver, 0, itemId, result8, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset, overdueRelationPenaltyPreset);
						anyDebtRecorded = true;
						DebtRecord debtRecord = GetDebtRecord(giver);
						NormalizeDebtRecord(debtRecord);
						DebtRecord.DebtLine debtLine = (debtRecord?.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && !x.IsGold && string.Equals(x.ItemId ?? "", itemId, StringComparison.OrdinalIgnoreCase) && x.RemainingAmount > 0)).OrderByDescending((DebtRecord.DebtLine x) => x.CreatedDay).FirstOrDefault();
						string text3 = ((debtLine != null) ? BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) : "");
						string text4 = debtLine?.DebtId ?? "";
						giverFacts.Add($"你已经记下：玩家欠你 {itemId} x{result8}（债务ID:{text4}）。{text3}");
						receiverFacts.Add($"你欠 {giverName} {itemId} x{result8}（债务ID:{text4}）。{text3}");
						string text5 = (string.IsNullOrWhiteSpace(text3) ? "" : ("（" + text3 + "）"));
						string text6 = (string.IsNullOrWhiteSpace(text4) ? "" : ("[ID:" + text4 + "] "));
						InformationManager.DisplayMessage(new InformationMessage($"【欠款记录】{text6}你欠 {giverName} {itemId} x{result8}{text5}", Color.FromUint(4294936576u)));
					}
				}
				return string.Empty;
			});
			responseText = regex11.Replace(responseText, delegate(Match m)
			{
				string value4 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text3 = (value4 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3) && !settledDebtIdsThisRound.Add(text3))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text3 + " tag=DEBT_PAY_GOLD");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerGoldPaymentByDebtId(giver, value4, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value4} 的金币还款 {result8}。{statusText}");
						receiverFacts.Add($"你已偿还金币债务ID {value4} 共 {result8}。{statusText}");
						if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"【还款确认】已偿还金币债务ID {value4}：{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex12.Replace(responseText, delegate(Match m)
			{
				string value4 = m.Groups[1].Value;
				string value5 = m.Groups[2].Value;
				if (int.TryParse(m.Groups[3].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text3 = (value4 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3) && !settledDebtIdsThisRound.Add(text3))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text3 + " tag=DEBT_PAY_ITEM");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerItemPaymentByDebtId(giver, value4, value5, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value4} 的物品还款：{value5} x{result8}。{statusText}");
						receiverFacts.Add($"你已偿还物品债务ID {value4}：{value5} x{result8}。{statusText}");
						if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"【还款确认】已偿还物品债务ID {value4}：{value5} x{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex13.Replace(responseText, delegate(Match m)
			{
				string value4 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text3 = (value4 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3) && !settledDebtIdsThisRound.Add(text3))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text3 + " tag=DEBT_PAY_ITEM_GOLD");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerItemCompensationByDebtId(giver, value4, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value4} 的物品金币赔偿：{result8}。{statusText}");
						receiverFacts.Add($"你已对物品债务ID {value4} 支付金币赔偿：{result8}。{statusText}");
						if (flag2)
						{
							InformationManager.DisplayMessage(new InformationMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u)));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"【赔偿确认】已按协商支付物品债务ID {value4} 的金币赔偿：{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex14.Replace(responseText, delegate(Match m)
			{
				string value4 = m.Groups[1].Value;
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = MarkItemDebtUnavailableById(giver, value4, out statusText);
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
				string value4 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && int.TryParse(m.Groups[3].Value, out var result9) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					ApplyItemDebtLlmPenaltyById(giver, value4, result8, result9, out var statusText);
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
				string value4 = m.Groups[1].Value;
				if (int.TryParse(m.Groups[2].Value, out var result8) && receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string text3 = (value4 ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3) && !settledDebtIdsThisRound.Add(text3))
					{
						Logger.Log("Logic", "[Reward] 跳过重复还款标签: debtId=" + text3 + " tag=DEBT_PAY(legacy)");
						return string.Empty;
					}
					string statusText;
					bool flag2 = RegisterPlayerGoldPaymentByDebtId(giver, value4, result8, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认收到玩家对债务ID {value4} 的还款 {result8}。{statusText}");
						receiverFacts.Add("你尝试偿还债务ID " + value4 + "：" + statusText);
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
							InformationManager.DisplayMessage(new InformationMessage($"【还款确认】已偿还债务ID {value4}：{result8}", Color.FromUint(4278242559u)));
						}
					}
				}
				return string.Empty;
			});
			responseText = regex17.Replace(responseText, delegate(Match m)
			{
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
			if (giver != Hero.MainHero && receiver == Hero.MainHero && anyActualGiveToPlayer && !anyDebtRecorded && !anyDebtPaymentApplied && !anyDebtMetaApplied)
			{
				if (num2.HasValue)
				{
					int value3 = num2.Value;
					if (value3 != 0)
					{
						int num3 = ComputeTradePublicTrustDelta(value3);
						AdjustTrust(giver, value3, num3, "llm_clean_trade");
						if (value3 > 0)
						{
							string text = ((num3 > 0) ? ("，公共信任提升 " + num3) : "");
							giverFacts.Add($"你根据这笔交易对玩家的个人信任提升了 {value3}{text}。");
							receiverFacts.Add($"{giverName} 因这笔交易对你的个人信任提升了 {value3}{text}。");
							InformationManager.DisplayMessage(new InformationMessage($"【信任变化】{giverName} 因这笔交易对你的个人信任 +{value3}" + ((num3 > 0) ? $"，公共信任 +{num3}" : ""), Color.FromUint(4278242559u)));
						}
						else
						{
							string text2 = ((num3 < 0) ? ("，公共信任下降 " + Math.Abs(num3)) : "");
							giverFacts.Add($"你因这笔交易对玩家的个人信任下降了 {Math.Abs(value3)}{text2}。");
							receiverFacts.Add($"{giverName} 因这笔交易对你的个人信任下降了 {Math.Abs(value3)}{text2}。");
							InformationManager.DisplayMessage(new InformationMessage($"【信任变化】{giverName} 因这笔交易对你的个人信任 -{Math.Abs(value3)}" + ((num3 < 0) ? $"，公共信任 -{Math.Abs(num3)}" : ""), Color.FromUint(4294945365u)));
						}
					}
				}
			}
			else if (num2.HasValue)
			{
				Logger.Log("Logic", "[Reward] 警告: 检测到 [ACTION:TRADE_TRUST]，但本轮不满足即时交易信任结算条件，已忽略。");
			}
			responseText = responseText.Trim();
			stopwatch.Stop();
			Logger.Obs("Action", "apply_reward_tags_done", new Dictionary<string, object>
			{
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["anyActualGiveToPlayer"] = anyActualGiveToPlayer,
				["anyDebtRecorded"] = anyDebtRecorded,
				["anyDebtPaymentApplied"] = anyDebtPaymentApplied,
				["anyDebtMetaApplied"] = anyDebtMetaApplied,
				["anyKingdomServiceApplied"] = anyKingdomServiceApplied,
				["giverFactsCount"] = giverFacts.Count,
				["receiverFactsCount"] = receiverFacts.Count,
				["textLenAfter"] = (responseText ?? "").Length,
				["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
			});
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
			Logger.Obs("Action", "apply_reward_tags_error", new Dictionary<string, object>
			{
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
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
		string giverName = giverCharacter.Name?.ToString() ?? GetSettlementMerchantRoleLabel(kind);
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
		bool anyActualGiveToPlayer = false;
		bool anyDebtRecorded = false;
		bool anyDebtPaymentApplied = false;
		int? dueDaysOverride = null;
		int? dueAbsDayOverride = null;
		bool dueUnlimited = regex9.IsMatch(responseText);
		int? overdueTrustPenaltyPreset = null;
		if (!dueUnlimited)
		{
			MatchCollection matchCollection = regex7.Matches(responseText);
			if (matchCollection != null && matchCollection.Count > 0 && int.TryParse(matchCollection[matchCollection.Count - 1].Groups[1].Value, out var result2))
			{
				dueAbsDayOverride = Math.Max(1, Math.Min(200000, result2));
			}
		}
		if (!dueUnlimited && !dueAbsDayOverride.HasValue)
		{
			MatchCollection matchCollection2 = regex8.Matches(responseText);
			if (matchCollection2 != null && matchCollection2.Count > 0)
			{
				Match match = matchCollection2[matchCollection2.Count - 1];
				if (match != null && int.TryParse(match.Groups[1].Value, out var result3) && TryParseSeasonToken(match.Groups[2].Value, out var seasonIndexZeroBased) && int.TryParse(match.Groups[3].Value, out var result4))
				{
					dueAbsDayOverride = ToAbsDayFromCalendar(result3, seasonIndexZeroBased, result4);
				}
			}
		}
		if (!dueUnlimited && !dueAbsDayOverride.HasValue)
		{
			MatchCollection matchCollection3 = regex6.Matches(responseText);
			if (matchCollection3 != null && matchCollection3.Count > 0 && int.TryParse(matchCollection3[matchCollection3.Count - 1].Groups[1].Value, out var result5))
			{
				dueDaysOverride = NormalizeDueDays(result5);
			}
		}
		MatchCollection matchCollection4 = regex10.Matches(responseText);
		if (matchCollection4 != null && matchCollection4.Count > 0 && int.TryParse(matchCollection4[matchCollection4.Count - 1].Groups[1].Value, out var result6))
		{
			overdueTrustPenaltyPreset = NormalizeLlmPenaltyValue(result6);
		}
		bool anyDebtMetaApplied = dueUnlimited || dueAbsDayOverride.HasValue || dueDaysOverride.HasValue || overdueTrustPenaltyPreset.HasValue;
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
					anyActualGiveToPlayer = true;
					merchantFacts.Add($"你已经将 {num} 第纳尔交给玩家。并进入了玩家的的库存");
					playerFacts.Add($"你从 {giverName} 收到了 {num} 第纳尔。");
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
			if (int.TryParse(m.Groups[2].Value, out var result))
			{
				string itemName;
				int num = TransferItemFromSettlement(currentSettlement, receiver, value, result, giverName, out itemName);
				string text = ((!string.IsNullOrWhiteSpace(itemName)) ? itemName : ResolveSettlementMerchantDisplayNameFromPromptStringId(value));
				ItemObject itemObject = ResolveItemById(value.Split('@')[0]);
				if (num > 0)
				{
					anyActualGiveToPlayer = true;
					merchantFacts.Add($"你已经将 {FormatItemAmount(num, itemObject, text)} 交给玩家。并进入了玩家的的库存");
					playerFacts.Add($"你从 {giverName} 收到了 {FormatItemAmount(num, itemObject, text)}。");
					if (num < result)
					{
						merchantFacts.Add($"你原本打算交付 {FormatItemAmount(result, itemObject, text)}，但当前商铺库存不足，实际只交付了 {FormatItemAmount(num, itemObject, text)}。");
						playerFacts.Add($"{giverName} 原本打算交付 {FormatItemAmount(result, itemObject, text)}，但实际只交付了 {FormatItemAmount(num, itemObject, text)}。");
					}
				}
				else
				{
					merchantFacts.Add($"你试图交付 {FormatItemAmount(result, itemObject, text)}，但当前商铺库存不足，本轮未实际交货。");
				}
			}
			return string.Empty;
		});
		responseText = regex3.Replace(responseText, delegate(Match m)
		{
			if (receiver == Hero.MainHero && hasGiveTag && int.TryParse(m.Groups[1].Value, out var result8) && result8 > 0)
			{
				anyDebtRecorded = true;
				SetDebtForSettlementMerchant(currentSettlement, kind, result8, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset);
				merchantFacts.Add($"你已经把玩家欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 的 {result8} 第纳尔记入账目。");
				playerFacts.Add($"你欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} {result8} 第纳尔。");
			}
			return string.Empty;
		});
		responseText = regex4.Replace(responseText, delegate(Match m)
		{
			if (receiver == Hero.MainHero && hasGiveTag && int.TryParse(m.Groups[1].Value, out var result8) && result8 > 0)
			{
				anyDebtRecorded = true;
				SetDebtForSettlementMerchant(currentSettlement, kind, result8, null, 0, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset);
				merchantFacts.Add($"你已经把玩家欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 的 {result8} 第纳尔记入账目。");
				playerFacts.Add($"你欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} {result8} 第纳尔。");
			}
			return string.Empty;
		});
		responseText = regex5.Replace(responseText, delegate(Match m)
		{
			string value = m.Groups[1].Value;
			if (receiver == Hero.MainHero && hasGiveTag && int.TryParse(m.Groups[2].Value, out var result8) && result8 > 0)
			{
				anyDebtRecorded = true;
				SetDebtForSettlementMerchant(currentSettlement, kind, 0, value, result8, dueDaysOverride, dueAbsDayOverride, dueUnlimited, overdueTrustPenaltyPreset);
				merchantFacts.Add($"你已经把玩家欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 的 {value} x{result8} 记入账目。");
				playerFacts.Add($"你欠 {BuildSettlementMerchantDebtLabel(currentSettlement, kind)} {value} x{result8}。");
			}
			return string.Empty;
		});
		responseText = regex11.Replace(responseText, delegate(Match m)
		{
			if (int.TryParse(m.Groups[2].Value, out var result8))
			{
				string statusText;
				bool flag = RegisterPlayerGoldPaymentByMerchantDebtId(currentSettlement, kind, m.Groups[1].Value, result8, out statusText);
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					if (flag)
					{
						anyDebtPaymentApplied = true;
					}
					merchantFacts.Add(statusText);
					playerFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag ? "【市场欠款】" : "【市场还款失败】") + statusText, flag ? Color.FromUint(4278255360u) : Color.FromUint(4294923605u)));
				}
			}
			return string.Empty;
		});
		responseText = regex12.Replace(responseText, delegate(Match m)
		{
			if (int.TryParse(m.Groups[3].Value, out var result8))
			{
				string statusText;
				bool flag = RegisterPlayerItemPaymentByMerchantDebtId(currentSettlement, kind, m.Groups[1].Value, m.Groups[2].Value, result8, out statusText);
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					if (flag)
					{
						anyDebtPaymentApplied = true;
					}
					merchantFacts.Add(statusText);
					playerFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag ? "【市场欠款】" : "【市场还款失败】") + statusText, flag ? Color.FromUint(4278255360u) : Color.FromUint(4294923605u)));
				}
			}
			return string.Empty;
		});
		if (regex13.IsMatch(responseText))
		{
			MatchCollection matchCollection5 = regex13.Matches(responseText);
			int? num2 = null;
			if (matchCollection5 != null && matchCollection5.Count > 0 && int.TryParse(matchCollection5[matchCollection5.Count - 1].Groups[1].Value, out var result8))
			{
				num2 = NormalizeLlmTrustDeltaValue(result8);
			}
			if (num2.HasValue && anyActualGiveToPlayer && !anyDebtRecorded && !anyDebtPaymentApplied && !anyDebtMetaApplied)
			{
				int value2 = num2.Value;
				if (value2 != 0)
				{
					AdjustSettlementMerchantTrust(currentSettlement, kind, value2, "llm_clean_trade");
					if (value2 > 0)
					{
						merchantFacts.Add($"你根据这笔交易对玩家的市场信任提升了 {value2}。");
						playerFacts.Add($"{giverName} 代表的{BuildSettlementMerchantDebtLabel(currentSettlement, kind)}因这笔交易对你的市场信任提升了 {value2}。");
						InformationManager.DisplayMessage(new InformationMessage($"【市场信任变化】{BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 对你的市场信任 +{value2}", Color.FromUint(4278242559u)));
					}
					else
					{
						merchantFacts.Add($"你因这笔交易对玩家的市场信任下降了 {Math.Abs(value2)}。");
						playerFacts.Add($"{giverName} 代表的{BuildSettlementMerchantDebtLabel(currentSettlement, kind)}因这笔交易对你的市场信任下降了 {Math.Abs(value2)}。");
						InformationManager.DisplayMessage(new InformationMessage($"【市场信任变化】{BuildSettlementMerchantDebtLabel(currentSettlement, kind)} 对你的市场信任 -{Math.Abs(value2)}", Color.FromUint(4294945365u)));
					}
				}
			}
			else
			{
				Logger.Log("Logic", "[Reward] 警告: 非Hero商贩检测到 [ACTION:TRADE_TRUST]，但本轮不满足即时交易市场信任结算条件，已忽略。");
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
		GiveGoldAction.ApplyBetweenCharacters(giver, receiver, num);
		if (receiver == Hero.MainHero)
		{
			string arg = giver?.Name?.ToString() ?? "某人";
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {num} 第纳尔。"));
		}
		else if (giver == Hero.MainHero)
		{
			string arg2 = receiver?.Name?.ToString() ?? "某人";
			InformationManager.DisplayMessage(new InformationMessage($"你给了 {arg2} {num} 第纳尔。"));
		}
		return num;
	}

	internal int TransferGoldFromSettlement(Settlement settlement, Hero receiver, int amount, string giverName = null)
	{
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
			string arg = ((!string.IsNullOrWhiteSpace(giverName)) ? giverName : (settlement.Name?.ToString() ?? "这座城镇的商人"));
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
		GiveGoldAction.ApplyBetweenCharacters(giver, null, num, disableNotification: true);
		settlement.SettlementComponent?.ChangeGold(num);
		return num;
	}

	private static int MoveMatchingItemsByStringId(ItemRoster sourceRoster, ItemRoster targetRoster, string itemStringId, int amount, out EquipmentElement firstTransferredElement)
	{
		firstTransferredElement = EquipmentElement.Invalid;
		if (sourceRoster == null || targetRoster == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		string text = itemStringId.Trim();
		int num = amount;
		int num2 = 0;
		while (num > 0)
		{
			bool flag = false;
			for (int i = 0; i < sourceRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = sourceRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
				ItemObject item = equipmentElement.Item;
				if (item == null || elementCopyAtIndex.Amount <= 0 || !string.Equals(item.StringId ?? "", text, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				int num3 = Math.Min(elementCopyAtIndex.Amount, num);
				if (num3 <= 0)
				{
					continue;
				}
				if (firstTransferredElement.Item == null)
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
			if (!flag)
			{
				break;
			}
		}
		return num2;
	}

	internal int TransferItemById(Hero giver, Hero receiver, string itemStringId, int amount, out string itemName)
	{
		itemName = null;
		if (string.IsNullOrEmpty(itemStringId) || amount <= 0)
		{
			return 0;
		}
		if (giver == null || receiver == null)
		{
			return 0;
		}
		ItemRoster itemRoster = ((giver.PartyBelongedTo != null) ? giver.PartyBelongedTo.ItemRoster : null);
		if (itemRoster == null && giver.Clan?.Leader?.PartyBelongedTo != null)
		{
			itemRoster = giver.Clan.Leader.PartyBelongedTo.ItemRoster;
		}
		if (itemRoster == null && MobileParty.MainParty != null && giver == Hero.MainHero)
		{
			itemRoster = MobileParty.MainParty.ItemRoster;
		}
		ItemRoster itemRoster2 = ((receiver.PartyBelongedTo != null) ? receiver.PartyBelongedTo.ItemRoster : null);
		if (itemRoster2 == null && receiver.Clan?.Leader?.PartyBelongedTo != null)
		{
			itemRoster2 = receiver.Clan.Leader.PartyBelongedTo.ItemRoster;
		}
		if (itemRoster2 == null && MobileParty.MainParty != null && receiver == Hero.MainHero)
		{
			itemRoster2 = MobileParty.MainParty.ItemRoster;
		}
		if (itemRoster2 == null)
		{
			return 0;
		}
		ItemObject itemObject = null;
		int num = 0;
		if (itemRoster != null)
		{
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				if (item != null && string.Equals(item.StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
				{
					itemObject = item;
					num += elementCopyAtIndex.Amount;
				}
			}
		}
		List<EquipmentIndex> list = new List<EquipmentIndex>();
		EquipmentIndex[] array = new EquipmentIndex[7]
		{
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Body,
			EquipmentIndex.Leg,
			EquipmentIndex.Gloves,
			EquipmentIndex.Cape,
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1
		};
		EquipmentIndex[] array2 = array;
		EquipmentIndex[] array3 = array2;
		foreach (EquipmentIndex equipmentIndex in array3)
		{
			ItemObject item2 = giver.BattleEquipment[equipmentIndex].Item;
			if (item2 != null && string.Equals(item2.StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
			{
				if (itemObject == null)
				{
					itemObject = item2;
				}
				list.Add(equipmentIndex);
			}
		}
		num += list.Count;
		if (itemObject == null || num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = num2;
		if (itemRoster != null)
		{
			num3 -= MoveMatchingItemsByStringId(itemRoster, itemRoster2, itemStringId, num3, out var equipmentElement);
			if (itemObject == null && equipmentElement.Item != null)
			{
				itemObject = equipmentElement.Item;
			}
			if (string.IsNullOrWhiteSpace(itemName) && equipmentElement.Item != null)
			{
				itemName = equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? itemStringId;
			}
		}
		for (int l = 0; l < list.Count; l++)
		{
			if (num3 <= 0)
			{
				break;
			}
			EquipmentIndex index = list[l];
			EquipmentElement equipmentElement2 = giver.BattleEquipment[index];
			ItemObject item4 = equipmentElement2.Item;
			if (item4 != null && string.Equals(item4.StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
			{
				giver.BattleEquipment[index] = EquipmentElement.Invalid;
				itemRoster2.AddToCounts(equipmentElement2, 1);
				if (itemObject == null)
				{
					itemObject = item4;
				}
				if (string.IsNullOrWhiteSpace(itemName))
				{
					itemName = equipmentElement2.GetModifiedItemName()?.ToString() ?? item4.Name?.ToString() ?? itemStringId;
				}
				num3--;
			}
		}
		if (itemObject != null)
		{
			itemName = itemName ?? itemObject.Name?.ToString() ?? itemStringId;
		}
		if (receiver == Hero.MainHero)
		{
			string arg = giver?.Name?.ToString() ?? "某人";
			string arg2 = itemName ?? itemStringId;
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {FormatItemAmount(num2, itemObject, arg2)}。"));
		}
		else if (giver == Hero.MainHero)
		{
			string arg3 = receiver?.Name?.ToString() ?? "某人";
			string arg4 = itemName ?? itemStringId;
			InformationManager.DisplayMessage(new InformationMessage($"你给了 {arg3} {FormatItemAmount(num2, itemObject, arg4)}。"));
		}
		return num2;
	}

	internal int TransferItemFromSettlement(Settlement settlement, Hero receiver, string itemStringId, int amount, string giverName, out string itemName)
	{
		itemName = null;
		if (settlement == null || receiver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		ItemRoster itemRoster = settlement.ItemRoster;
		ItemRoster itemRoster2 = ((receiver.PartyBelongedTo != null) ? receiver.PartyBelongedTo.ItemRoster : null);
		if (itemRoster == null || itemRoster2 == null)
		{
			return 0;
		}
		EquipmentElement equipmentElement = EquipmentElement.Invalid;
		int num = 0;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			if (MatchesSettlementMerchantPromptStringId(elementCopyAtIndex.EquipmentElement, itemStringId))
			{
				equipmentElement = elementCopyAtIndex.EquipmentElement;
				num += Math.Max(0, elementCopyAtIndex.Amount);
			}
		}
		if (equipmentElement.Item == null || num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		itemRoster.AddToCounts(equipmentElement, -num2);
		itemRoster2.AddToCounts(equipmentElement, num2);
		itemName = BuildSettlementMerchantDisplayName(equipmentElement);
		if (receiver == Hero.MainHero)
		{
			string arg = ((!string.IsNullOrWhiteSpace(giverName)) ? giverName : (settlement.Name?.ToString() ?? "这座城镇的商人"));
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {FormatItemAmount(num2, equipmentElement.Item, itemName)}。"));
		}
		return num2;
	}

	internal int TransferItemToSettlement(Settlement settlement, Hero giver, string itemStringId, int amount, out string itemName)
	{
		itemName = null;
		if (settlement == null || giver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		ItemRoster itemRoster = ((giver.PartyBelongedTo != null) ? giver.PartyBelongedTo.ItemRoster : null) ?? MobileParty.MainParty?.ItemRoster;
		ItemRoster itemRoster2 = settlement.ItemRoster;
		if (itemRoster == null || itemRoster2 == null)
		{
			return 0;
		}
		ItemObject itemObject = null;
		int num = 0;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
			if (item != null && string.Equals(item.StringId ?? "", itemStringId, StringComparison.OrdinalIgnoreCase))
			{
				itemObject = item;
				num += Math.Max(0, elementCopyAtIndex.Amount);
			}
		}
		if (itemObject == null || num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = MoveMatchingItemsByStringId(itemRoster, itemRoster2, itemStringId, num2, out var equipmentElement);
		if (num3 > 0)
		{
			itemName = (equipmentElement.Item != null) ? (equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? itemStringId) : (itemObject.Name?.ToString() ?? itemStringId);
		}
		return num3;
	}
}
