using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public class AnimusForgeTerminalBehavior : CampaignBehaviorBase
{
	private const int HintIntervalDays = 2;

	private const float OpenCooldownSeconds = 0.35f;

	private int _lastTerminalHintDay = -999999;

	private bool _terminalUiActive;

	private float _lastOpenRealTime = -999f;

	private bool _wasTerminalKeyDown;

	public static AnimusForgeTerminalBehavior Instance { get; private set; }

	public AnimusForgeTerminalBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_af_terminal_last_hint_day_v1", ref _lastTerminalHintDay);
	}

	public void OnEngineTick()
	{
		InputKey configuredTerminalKey = GetConfiguredTerminalKey();
		bool flag = false;
		try
		{
			flag = Input.IsKeyDown(configuredTerminalKey);
		}
		catch
		{
			flag = false;
		}
		if (!flag)
		{
			_wasTerminalKeyDown = false;
			return;
		}
		if (HotkeyInputGuard.IsTextInputFocused())
		{
			_wasTerminalKeyDown = true;
			return;
		}
		if (_wasTerminalKeyDown || _terminalUiActive || !IsCampaignMapLikeStateActive())
		{
			return;
		}
		_wasTerminalKeyDown = true;
		float num = (float)Environment.TickCount / 1000f;
		if (num - _lastOpenRealTime < OpenCooldownSeconds)
		{
			return;
		}
		_lastOpenRealTime = num;
		OpenRootMenu();
	}

	private void OnDailyTick()
	{
		try
		{
			int campaignDayIndex = GetCampaignDayIndex();
			if (campaignDayIndex - _lastTerminalHintDay < HintIntervalDays)
			{
				return;
			}
			_lastTerminalHintDay = campaignDayIndex;
			InformationManager.DisplayMessage(new InformationMessage($"按{GetConfiguredTerminalKeyLabel()}键打开AnimusForge终端。"));
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[WARN] terminal hint failed: " + ex.Message);
		}
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

	private static bool IsCampaignMapLikeStateActive()
	{
		try
		{
			if (Campaign.Current == null || Mission.Current != null)
			{
				return false;
			}
			if (Campaign.Current.ConversationManager != null && Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				return false;
			}
			ScreenBase topScreen = ScreenManager.TopScreen;
			string text = topScreen?.GetType().Name ?? "";
			if (text.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string text2 = Game.Current?.GameStateManager?.ActiveState?.GetType().Name ?? "";
			if (text2.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			return topScreen == null && Mission.Current == null;
		}
		catch
		{
			return false;
		}
	}

	private void OpenRootMenu()
	{
		_terminalUiActive = true;
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("trust_query", "信任度查询", null, isEnabled: true, "")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("你现在想做什么？", "请选择终端功能：", list, isExitShown: true, 1, 1, "确定", "关闭", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				CloseTerminal();
				return;
			}
			string text = selected[0].Identifier as string;
			if (string.Equals(text, "trust_query", StringComparison.Ordinal))
			{
				OpenTrustQueryMenu(CloseTerminal);
			}
			else
			{
				CloseTerminal();
			}
		}, delegate
		{
			CloseTerminal();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenTrustQueryMenu(Action onReturn)
	{
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("settlement", "搜索定居点信任", null, isEnabled: true, ""),
			new InquiryElement("hero", "搜索NPC信任", null, isEnabled: true, "")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("信任度查询", "请选择查询方式：", list, isExitShown: true, 1, 1, "确定", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
				return;
			}
			string text = selected[0].Identifier as string;
			if (string.Equals(text, "settlement", StringComparison.Ordinal))
			{
				OpenSettlementBrowser(delegate
				{
					OpenTrustQueryMenu(onReturn);
				});
			}
			else if (string.Equals(text, "hero", StringComparison.Ordinal))
			{
				OpenHeroBrowser(delegate
				{
					OpenTrustQueryMenu(onReturn);
				});
			}
			else
			{
				OpenTrustQueryMenu(onReturn);
			}
		}, delegate
		{
			onReturn();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenSettlementBrowser(Action onReturn)
	{
		List<Settlement> list = Settlement.All.Where((Settlement x) => x != null).OrderBy((Settlement x) => x.Name?.ToString() ?? x.StringId ?? "").ToList();
		if (list.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可查询的定居点。"));
			onReturn();
			return;
		}
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__back__", "返回上级", null, isEnabled: true, ""));
		foreach (Settlement item in list)
		{
			int num = instance?.GetSettlementLocalPublicTrust(item) ?? 0;
			int num2 = instance?.GetSettlementSharedPublicTrust(item) ?? 0;
			int num3 = ClampTrustForDisplay(num + num2);
			string text6 = $"{item.Name} 信任度：{FormatTrustDisplay(num3)}";
			list2.Add(new InquiryElement("settlement:" + item.StringId, text6, GetSettlementImageIdentifier(item), isEnabled: true, ""));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("定居点信任查询", "可直接在上方搜索框中筛选定居点。", list2, isExitShown: true, 1, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
				return;
			}
			string text7 = selected[0].Identifier as string;
			if (text7 == "__back__")
			{
				onReturn();
				return;
			}
			if (text7 != null && text7.StartsWith("settlement:", StringComparison.OrdinalIgnoreCase))
			{
				string value = text7.Substring("settlement:".Length);
				Settlement settlement = Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals(x.StringId, value, StringComparison.OrdinalIgnoreCase));
				if (settlement == null)
				{
					OpenSettlementBrowser(onReturn);
				}
				else
				{
					OpenSettlementDetails(settlement, delegate
					{
						OpenSettlementBrowser(onReturn);
					});
				}
			}
		}, delegate
		{
			onReturn();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenHeroBrowser(Action onReturn)
	{
		List<Hero> list = Hero.AllAliveHeroes.Where((Hero x) => x != null).OrderBy((Hero x) => x.Name?.ToString() ?? x.StringId ?? "").ToList();
		if (list.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可查询的 NPC。"));
			onReturn();
			return;
		}
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__back__", "返回上级", null, isEnabled: true, ""));
		foreach (Hero item in list)
		{
			int num = instance?.GetEffectiveTrust(item) ?? 0;
			string text4 = item.Name?.ToString() ?? item.StringId ?? "未知NPC";
			list2.Add(new InquiryElement("hero:" + item.StringId, $"{text4} 信任度：{FormatTrustDisplay(num)}", GetHeroImageIdentifier(item), isEnabled: true, ""));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("NPC信任查询", "可直接在上方搜索框中筛选 NPC。", list2, isExitShown: true, 1, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
				return;
			}
			string text5 = selected[0].Identifier as string;
			if (text5 == "__back__")
			{
				onReturn();
				return;
			}
			if (text5 != null && text5.StartsWith("hero:", StringComparison.OrdinalIgnoreCase))
			{
				string value = text5.Substring("hero:".Length);
				Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x != null && string.Equals(x.StringId, value, StringComparison.OrdinalIgnoreCase));
				if (hero == null)
				{
					OpenHeroBrowser(onReturn);
				}
				else
				{
					OpenHeroDetails(hero, delegate
					{
						OpenHeroBrowser(onReturn);
					});
				}
			}
		}, delegate
		{
			onReturn();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenSettlementDetails(Settlement settlement, Action onReturn)
	{
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		string text = BuildSettlementTrustReport(settlement, instance);
		InformationManager.ShowInquiry(new InquiryData("定居点信任详情", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
		{
			onReturn();
		}, null), pauseGameActiveState: true, prioritize: false);
	}

	private static string BuildSettlementTrustReport(Settlement settlement, RewardSystemBehavior reward)
	{
		if (settlement == null)
		{
			return "未找到定居点。";
		}
		string text = settlement.Name?.ToString() ?? settlement.StringId ?? "未知定居点";
		string text2 = settlement.MapFaction?.Name?.ToString() ?? settlement.OwnerClan?.Kingdom?.Name?.ToString() ?? settlement.OwnerClan?.Name?.ToString() ?? "未知势力";
		int num = reward?.GetSettlementLocalPublicTrust(settlement) ?? 0;
		int num2 = reward?.GetSettlementSharedPublicTrust(settlement) ?? 0;
		int num3 = ClampTrustForDisplay(num + num2);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"名称：{text}");
		stringBuilder.AppendLine($"所属势力：{text2}");
		stringBuilder.AppendLine($"信任度：{FormatTrustDisplay(num3)}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("商贩信任度：");
		foreach (RewardSystemBehavior.SettlementMerchantKind value in Enum.GetValues(typeof(RewardSystemBehavior.SettlementMerchantKind)))
		{
			if (value == RewardSystemBehavior.SettlementMerchantKind.None)
			{
				continue;
			}
			int settlementMerchantEffectiveTrust = reward?.GetSettlementMerchantEffectiveTrust(settlement, value) ?? 0;
			stringBuilder.AppendLine($"{GetMerchantKindLabel(value)}：{FormatTrustDisplay(settlementMerchantEffectiveTrust)}");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenHeroDetails(Hero hero, Action onReturn)
	{
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		string text = BuildHeroTrustReport(hero, instance);
		InformationManager.ShowInquiry(new InquiryData("NPC信任详情", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
		{
			onReturn();
		}, null), pauseGameActiveState: true, prioritize: false);
	}

	private static string BuildHeroTrustReport(Hero hero, RewardSystemBehavior reward)
	{
		if (hero == null)
		{
			return "未找到 NPC。";
		}
		string text = hero.Name?.ToString() ?? hero.StringId ?? "未知NPC";
		string text2 = hero.MapFaction?.Name?.ToString() ?? hero.Clan?.Kingdom?.Name?.ToString() ?? hero.Clan?.Name?.ToString() ?? hero.Culture?.Name?.ToString() ?? "未知势力";
		int num = reward?.GetNpcTrust(hero) ?? 0;
		int num2 = reward?.GetPublicTrust(hero) ?? 0;
		int num3 = reward?.GetEffectiveTrust(hero) ?? 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"名称：{text}");
		stringBuilder.AppendLine($"所属势力：{text2}");
		stringBuilder.AppendLine($"信任度：{FormatTrustDisplay(num3)}");
		return stringBuilder.ToString().TrimEnd();
	}

	private static string GetMerchantKindLabel(RewardSystemBehavior.SettlementMerchantKind kind)
	{
		switch (kind)
		{
		case RewardSystemBehavior.SettlementMerchantKind.Weapon:
			return "武器商";
		case RewardSystemBehavior.SettlementMerchantKind.Armor:
			return "防具商";
		case RewardSystemBehavior.SettlementMerchantKind.Horse:
			return "马商";
		case RewardSystemBehavior.SettlementMerchantKind.Goods:
			return "杂货商";
		default:
			return kind.ToString();
		}
	}

	private void CloseTerminal()
	{
		_terminalUiActive = false;
	}

	private static ImageIdentifier GetHeroImageIdentifier(Hero hero)
	{
		try
		{
			CharacterObject characterObject = hero?.CharacterObject;
			if (characterObject == null)
			{
				return null;
			}
			CharacterCode characterCode = CharacterCode.CreateFrom(characterObject);
			return new CharacterImageIdentifier(characterCode);
		}
		catch
		{
			return null;
		}
	}

	private static ImageIdentifier GetSettlementImageIdentifier(Settlement settlement)
	{
		try
		{
			Banner banner = settlement?.OwnerClan?.Banner ?? settlement?.MapFaction?.Banner ?? settlement?.OwnerClan?.Kingdom?.Banner;
			if (banner == null)
			{
				return null;
			}
			return new BannerImageIdentifier(banner);
		}
		catch
		{
			return null;
		}
	}

	private static string FormatTrustDisplay(int trust)
	{
		int trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(trust);
		string trustLevelText = RewardSystemBehavior.GetTrustLevelText(trust);
		return $"({trust}，{trustLevelText}，{trustLevelIndex}/10)";
	}

	private static int ClampTrustForDisplay(int trust)
	{
		if (trust < -100)
		{
			return -100;
		}
		if (trust > 100)
		{
			return 100;
		}
		return trust;
	}

	private static InputKey GetConfiguredTerminalKey()
	{
		InputKey result = InputKey.U;
		try
		{
			string terminalKey = DuelSettings.GetSettings()?.TerminalKey;
			if (!string.IsNullOrWhiteSpace(terminalKey) && Enum.TryParse<InputKey>(terminalKey.Trim().ToUpperInvariant(), out var result2))
			{
				result = result2;
			}
		}
		catch
		{
			result = InputKey.U;
		}
		return result;
	}

	private static string GetConfiguredTerminalKeyLabel()
	{
		try
		{
			return GetConfiguredTerminalKey().ToString().ToUpperInvariant();
		}
		catch
		{
			return "U";
		}
	}
}
