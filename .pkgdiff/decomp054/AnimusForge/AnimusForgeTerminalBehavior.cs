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
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
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
		CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this, (Action)OnDailyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData<int>("_af_terminal_last_hint_day_v1", ref _lastTerminalHintDay);
	}

	public void OnEngineTick()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
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
		}
		else if (HotkeyInputGuard.IsTextInputFocused())
		{
			_wasTerminalKeyDown = true;
		}
		else if (!_wasTerminalKeyDown && !_terminalUiActive && IsCampaignMapLikeStateActive())
		{
			_wasTerminalKeyDown = true;
			float num = (float)Environment.TickCount / 1000f;
			if (!(num - _lastOpenRealTime < 0.35f))
			{
				_lastOpenRealTime = num;
				OpenRootMenu();
			}
		}
	}

	private void OnDailyTick()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		try
		{
			int campaignDayIndex = GetCampaignDayIndex();
			if (campaignDayIndex - _lastTerminalHintDay >= 2)
			{
				_lastTerminalHintDay = campaignDayIndex;
				InformationManager.DisplayMessage(new InformationMessage("按" + GetConfiguredTerminalKeyLabel() + "键打开AnimusForge终端。"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[WARN] terminal hint failed: " + ex.Message);
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
			string text = ((object)topScreen)?.GetType().Name ?? "";
			if (text.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager == null) ? null : ((object)gameStateManager.ActiveState)?.GetType().Name);
			}
			if (obj == null)
			{
				obj = "";
			}
			string text2 = (string)obj;
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
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		_terminalUiActive = true;
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement((object)"trust_query", "信任度查询", (ImageIdentifier)null, true, "")
		};
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("你现在想做什么？", "请选择终端功能：", list, true, 1, 1, "确定", "关闭", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				CloseTerminal();
			}
			else
			{
				string a = selected[0].Identifier as string;
				if (string.Equals(a, "trust_query", StringComparison.Ordinal))
				{
					OpenTrustQueryMenu(CloseTerminal);
				}
				else
				{
					CloseTerminal();
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			CloseTerminal();
		}, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, true, false);
	}

	private void OpenTrustQueryMenu(Action onReturn)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement((object)"settlement", "搜索定居点信任", (ImageIdentifier)null, true, ""),
			new InquiryElement((object)"hero", "搜索NPC信任", (ImageIdentifier)null, true, "")
		};
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("信任度查询", "请选择查询方式：", list, true, 1, 1, "确定", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string a = selected[0].Identifier as string;
				if (string.Equals(a, "settlement", StringComparison.Ordinal))
				{
					OpenSettlementBrowser(delegate
					{
						OpenTrustQueryMenu(onReturn);
					});
				}
				else if (string.Equals(a, "hero", StringComparison.Ordinal))
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
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, true, false);
	}

	private void OpenSettlementBrowser(Action onReturn)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Expected O, but got Unknown
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Expected O, but got Unknown
		List<Settlement> list = (from x in (IEnumerable<Settlement>)Settlement.All
			where x != null
			orderby ((object)x.Name)?.ToString() ?? ((MBObjectBase)x).StringId ?? ""
			select x).ToList();
		if (list.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可查询的定居点。"));
			onReturn();
			return;
		}
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"__back__", "返回上级", (ImageIdentifier)null, true, ""));
		foreach (Settlement item in list)
		{
			int num = instance?.GetSettlementLocalPublicTrust(item) ?? 0;
			int num2 = instance?.GetSettlementSharedPublicTrust(item) ?? 0;
			int trust = ClampTrustForDisplay(num + num2);
			string text = $"{item.Name} 信任度：{FormatTrustDisplay(trust)}";
			list2.Add(new InquiryElement((object)("settlement:" + ((MBObjectBase)item).StringId), text, GetSettlementImageIdentifier(item), true, ""));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("定居点信任查询", "可直接在上方搜索框中筛选定居点。", list2, true, 1, 1, "查看", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "__back__")
				{
					onReturn();
				}
				else if (text2 != null && text2.StartsWith("settlement:", StringComparison.OrdinalIgnoreCase))
				{
					string value = text2.Substring("settlement:".Length);
					Settlement val2 = ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Settlement x) => x != null && string.Equals(((MBObjectBase)x).StringId, value, StringComparison.OrdinalIgnoreCase));
					if (val2 == null)
					{
						OpenSettlementBrowser(onReturn);
					}
					else
					{
						OpenSettlementDetails(val2, delegate
						{
							OpenSettlementBrowser(onReturn);
						});
					}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, true, false);
	}

	private void OpenHeroBrowser(Action onReturn)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Expected O, but got Unknown
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Expected O, but got Unknown
		List<Hero> list = (from x in (IEnumerable<Hero>)Hero.AllAliveHeroes
			where x != null
			orderby ((object)x.Name)?.ToString() ?? ((MBObjectBase)x).StringId ?? ""
			select x).ToList();
		if (list.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可查询的 NPC。"));
			onReturn();
			return;
		}
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"__back__", "返回上级", (ImageIdentifier)null, true, ""));
		foreach (Hero item in list)
		{
			int trust = instance?.GetEffectiveTrust(item) ?? 0;
			string text = ((object)item.Name)?.ToString() ?? ((MBObjectBase)item).StringId ?? "未知NPC";
			list2.Add(new InquiryElement((object)("hero:" + ((MBObjectBase)item).StringId), text + " 信任度：" + FormatTrustDisplay(trust), GetHeroImageIdentifier(item), true, ""));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("NPC信任查询", "可直接在上方搜索框中筛选 NPC。", list2, true, 1, 1, "查看", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "__back__")
				{
					onReturn();
				}
				else if (text2 != null && text2.StartsWith("hero:", StringComparison.OrdinalIgnoreCase))
				{
					string value = text2.Substring("hero:".Length);
					Hero val2 = ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Hero x) => x != null && string.Equals(((MBObjectBase)x).StringId, value, StringComparison.OrdinalIgnoreCase));
					if (val2 == null)
					{
						OpenHeroBrowser(onReturn);
					}
					else
					{
						OpenHeroDetails(val2, delegate
						{
							OpenHeroBrowser(onReturn);
						});
					}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, true, false);
	}

	private void OpenSettlementDetails(Settlement settlement, Action onReturn)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		string text = BuildSettlementTrustReport(settlement, instance);
		InformationManager.ShowInquiry(new InquiryData("定居点信任详情", text, true, false, "返回", "", (Action)delegate
		{
			onReturn();
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
	}

	private static string BuildSettlementTrustReport(Settlement settlement, RewardSystemBehavior reward)
	{
		if (settlement == null)
		{
			return "未找到定居点。";
		}
		string text = ((object)settlement.Name)?.ToString() ?? ((MBObjectBase)settlement).StringId ?? "未知定居点";
		IFaction mapFaction = settlement.MapFaction;
		object obj = ((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString());
		if (obj == null)
		{
			Clan ownerClan = settlement.OwnerClan;
			if (ownerClan == null)
			{
				obj = null;
			}
			else
			{
				Kingdom kingdom = ownerClan.Kingdom;
				obj = ((kingdom == null) ? null : ((object)kingdom.Name)?.ToString());
			}
			if (obj == null)
			{
				Clan ownerClan2 = settlement.OwnerClan;
				obj = ((ownerClan2 == null) ? null : ((object)ownerClan2.Name)?.ToString()) ?? "未知势力";
			}
		}
		string text2 = (string)obj;
		int num = reward?.GetSettlementLocalPublicTrust(settlement) ?? 0;
		int num2 = reward?.GetSettlementSharedPublicTrust(settlement) ?? 0;
		int trust = ClampTrustForDisplay(num + num2);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("名称：" + text);
		stringBuilder.AppendLine("所属势力：" + text2);
		stringBuilder.AppendLine("信任度：" + FormatTrustDisplay(trust));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("商贩信任度：");
		foreach (RewardSystemBehavior.SettlementMerchantKind value in Enum.GetValues(typeof(RewardSystemBehavior.SettlementMerchantKind)))
		{
			if (value != RewardSystemBehavior.SettlementMerchantKind.None)
			{
				int trust2 = reward?.GetSettlementMerchantEffectiveTrust(settlement, value) ?? 0;
				stringBuilder.AppendLine(GetMerchantKindLabel(value) + "：" + FormatTrustDisplay(trust2));
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenHeroDetails(Hero hero, Action onReturn)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		RewardSystemBehavior instance = RewardSystemBehavior.Instance;
		string text = BuildHeroTrustReport(hero, instance);
		InformationManager.ShowInquiry(new InquiryData("NPC信任详情", text, true, false, "返回", "", (Action)delegate
		{
			onReturn();
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
	}

	private static string BuildHeroTrustReport(Hero hero, RewardSystemBehavior reward)
	{
		if (hero == null)
		{
			return "未找到 NPC。";
		}
		string text = ((object)hero.Name)?.ToString() ?? ((MBObjectBase)hero).StringId ?? "未知NPC";
		IFaction mapFaction = hero.MapFaction;
		object obj = ((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString());
		if (obj == null)
		{
			Clan clan = hero.Clan;
			if (clan == null)
			{
				obj = null;
			}
			else
			{
				Kingdom kingdom = clan.Kingdom;
				obj = ((kingdom == null) ? null : ((object)kingdom.Name)?.ToString());
			}
			if (obj == null)
			{
				Clan clan2 = hero.Clan;
				obj = ((clan2 == null) ? null : ((object)clan2.Name)?.ToString());
				if (obj == null)
				{
					CultureObject culture = hero.Culture;
					obj = ((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "未知势力";
				}
			}
		}
		string text2 = (string)obj;
		int num = reward?.GetNpcTrust(hero) ?? 0;
		int num2 = reward?.GetPublicTrust(hero) ?? 0;
		int trust = reward?.GetEffectiveTrust(hero) ?? 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("名称：" + text);
		stringBuilder.AppendLine("所属势力：" + text2);
		stringBuilder.AppendLine("信任度：" + FormatTrustDisplay(trust));
		return stringBuilder.ToString().TrimEnd();
	}

	private static string GetMerchantKindLabel(RewardSystemBehavior.SettlementMerchantKind kind)
	{
		return kind switch
		{
			RewardSystemBehavior.SettlementMerchantKind.Weapon => "武器商", 
			RewardSystemBehavior.SettlementMerchantKind.Armor => "防具商", 
			RewardSystemBehavior.SettlementMerchantKind.Horse => "马商", 
			RewardSystemBehavior.SettlementMerchantKind.Goods => "杂货商", 
			_ => kind.ToString(), 
		};
	}

	private void CloseTerminal()
	{
		_terminalUiActive = false;
	}

	private static ImageIdentifier GetHeroImageIdentifier(Hero hero)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		try
		{
			CharacterObject val = ((hero != null) ? hero.CharacterObject : null);
			if (val == null)
			{
				return null;
			}
			CharacterCode val2 = CharacterCode.CreateFrom((BasicCharacterObject)(object)val);
			return (ImageIdentifier)new CharacterImageIdentifier(val2);
		}
		catch
		{
			return null;
		}
	}

	private static ImageIdentifier GetSettlementImageIdentifier(Settlement settlement)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		try
		{
			object obj;
			if (settlement == null)
			{
				obj = null;
			}
			else
			{
				Clan ownerClan = settlement.OwnerClan;
				obj = ((ownerClan != null) ? ownerClan.Banner : null);
			}
			if (obj == null)
			{
				if (settlement == null)
				{
					obj = null;
				}
				else
				{
					IFaction mapFaction = settlement.MapFaction;
					obj = ((mapFaction != null) ? mapFaction.Banner : null);
				}
				if (obj == null)
				{
					if (settlement == null)
					{
						obj = null;
					}
					else
					{
						Clan ownerClan2 = settlement.OwnerClan;
						if (ownerClan2 == null)
						{
							obj = null;
						}
						else
						{
							Kingdom kingdom = ownerClan2.Kingdom;
							obj = ((kingdom != null) ? kingdom.Banner : null);
						}
					}
				}
			}
			Banner val = (Banner)obj;
			if (val == null)
			{
				return null;
			}
			return (ImageIdentifier)new BannerImageIdentifier(val, false);
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
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		InputKey result = (InputKey)22;
		try
		{
			string text = DuelSettings.GetSettings()?.TerminalKey;
			if (!string.IsNullOrWhiteSpace(text) && Enum.TryParse<InputKey>(text.Trim().ToUpperInvariant(), out InputKey result2))
			{
				result = result2;
			}
		}
		catch
		{
			result = (InputKey)22;
		}
		return result;
	}

	private static string GetConfiguredTerminalKeyLabel()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return ((object)GetConfiguredTerminalKey()/*cast due to .constrained prefix*/).ToString().ToUpperInvariant();
		}
		catch
		{
			return "U";
		}
	}
}
