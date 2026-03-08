using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x0200003B RID: 59
	public static class InformationManager
	{
		// Token: 0x14000003 RID: 3
		// (add) Token: 0x060001CB RID: 459 RVA: 0x000070E0 File Offset: 0x000052E0
		// (remove) Token: 0x060001CC RID: 460 RVA: 0x00007114 File Offset: 0x00005314
		public static event Action<InformationMessage> DisplayMessageInternal;

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x060001CD RID: 461 RVA: 0x00007148 File Offset: 0x00005348
		// (remove) Token: 0x060001CE RID: 462 RVA: 0x0000717C File Offset: 0x0000537C
		public static event Action ClearAllMessagesInternal;

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x060001CF RID: 463 RVA: 0x000071B0 File Offset: 0x000053B0
		// (remove) Token: 0x060001D0 RID: 464 RVA: 0x000071E4 File Offset: 0x000053E4
		public static event Action HideAllMessagesInternal;

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x060001D1 RID: 465 RVA: 0x00007218 File Offset: 0x00005418
		// (remove) Token: 0x060001D2 RID: 466 RVA: 0x0000724C File Offset: 0x0000544C
		public static event Action<string> OnAddSystemNotification;

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x060001D3 RID: 467 RVA: 0x00007280 File Offset: 0x00005480
		// (remove) Token: 0x060001D4 RID: 468 RVA: 0x000072B4 File Offset: 0x000054B4
		public static event Action<Type, object[]> OnShowTooltip;

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x060001D5 RID: 469 RVA: 0x000072E8 File Offset: 0x000054E8
		// (remove) Token: 0x060001D6 RID: 470 RVA: 0x0000731C File Offset: 0x0000551C
		public static event Action OnHideTooltip;

		// Token: 0x14000009 RID: 9
		// (add) Token: 0x060001D7 RID: 471 RVA: 0x00007350 File Offset: 0x00005550
		// (remove) Token: 0x060001D8 RID: 472 RVA: 0x00007384 File Offset: 0x00005584
		public static event Action<InquiryData, bool, bool> OnShowInquiry;

		// Token: 0x1400000A RID: 10
		// (add) Token: 0x060001D9 RID: 473 RVA: 0x000073B8 File Offset: 0x000055B8
		// (remove) Token: 0x060001DA RID: 474 RVA: 0x000073EC File Offset: 0x000055EC
		public static event Action<TextInquiryData, bool, bool> OnShowTextInquiry;

		// Token: 0x1400000B RID: 11
		// (add) Token: 0x060001DB RID: 475 RVA: 0x00007420 File Offset: 0x00005620
		// (remove) Token: 0x060001DC RID: 476 RVA: 0x00007454 File Offset: 0x00005654
		public static event Action OnHideInquiry;

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060001DD RID: 477 RVA: 0x00007487 File Offset: 0x00005687
		public static IReadOnlyDictionary<Type, InformationManager.TooltipRegistry> RegisteredTypes
		{
			get
			{
				return InformationManager._registeredTypes;
			}
		}

		// Token: 0x060001DE RID: 478 RVA: 0x0000748E File Offset: 0x0000568E
		public static bool IsAnyInquiryActive()
		{
			return InformationManager.IsAnyInquiryActiveInternal != null && InformationManager.IsAnyInquiryActiveInternal();
		}

		// Token: 0x060001DF RID: 479 RVA: 0x000074A3 File Offset: 0x000056A3
		public static void DisplayMessage(InformationMessage message)
		{
			Action<InformationMessage> displayMessageInternal = InformationManager.DisplayMessageInternal;
			if (displayMessageInternal == null)
			{
				return;
			}
			displayMessageInternal(message);
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x000074B5 File Offset: 0x000056B5
		public static void HideAllMessages()
		{
			Action hideAllMessagesInternal = InformationManager.HideAllMessagesInternal;
			if (hideAllMessagesInternal == null)
			{
				return;
			}
			hideAllMessagesInternal();
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x000074C6 File Offset: 0x000056C6
		public static void ClearAllMessages()
		{
			Action clearAllMessagesInternal = InformationManager.ClearAllMessagesInternal;
			if (clearAllMessagesInternal == null)
			{
				return;
			}
			clearAllMessagesInternal();
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x000074D7 File Offset: 0x000056D7
		public static void AddSystemNotification(string message)
		{
			Action<string> onAddSystemNotification = InformationManager.OnAddSystemNotification;
			if (onAddSystemNotification == null)
			{
				return;
			}
			onAddSystemNotification(message);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x000074E9 File Offset: 0x000056E9
		public static void ShowTooltip(Type type, params object[] args)
		{
			Action<Type, object[]> onShowTooltip = InformationManager.OnShowTooltip;
			if (onShowTooltip == null)
			{
				return;
			}
			onShowTooltip(type, args);
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x000074FC File Offset: 0x000056FC
		public static void HideTooltip()
		{
			Action onHideTooltip = InformationManager.OnHideTooltip;
			if (onHideTooltip == null)
			{
				return;
			}
			onHideTooltip();
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0000750D File Offset: 0x0000570D
		public static void ShowInquiry(InquiryData data, bool pauseGameActiveState = false, bool prioritize = false)
		{
			Action<InquiryData, bool, bool> onShowInquiry = InformationManager.OnShowInquiry;
			if (onShowInquiry == null)
			{
				return;
			}
			onShowInquiry(data, pauseGameActiveState, prioritize);
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x00007521 File Offset: 0x00005721
		public static void ShowTextInquiry(TextInquiryData textData, bool pauseGameActiveState = false, bool prioritize = false)
		{
			Action<TextInquiryData, bool, bool> onShowTextInquiry = InformationManager.OnShowTextInquiry;
			if (onShowTextInquiry == null)
			{
				return;
			}
			onShowTextInquiry(textData, pauseGameActiveState, prioritize);
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x00007535 File Offset: 0x00005735
		public static void HideInquiry()
		{
			Action onHideInquiry = InformationManager.OnHideInquiry;
			if (onHideInquiry == null)
			{
				return;
			}
			onHideInquiry();
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00007548 File Offset: 0x00005748
		public static bool GetIsAnyTooltipActive()
		{
			if (InformationManager.IsAnyTooltipActiveInternal != null)
			{
				bool result;
				bool flag;
				InformationManager.IsAnyTooltipActiveInternal(out result, out flag);
				return result;
			}
			return false;
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x00007570 File Offset: 0x00005770
		public static bool GetIsAnyTooltipActiveAndExtended()
		{
			if (InformationManager.IsAnyTooltipActiveInternal != null)
			{
				bool flag;
				bool flag2;
				InformationManager.IsAnyTooltipActiveInternal(out flag, out flag2);
				return flag && flag2;
			}
			return false;
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00007598 File Offset: 0x00005798
		public static void RegisterTooltip<TRegistered, TTooltip>(Action<TTooltip, object[]> onRefreshData, string movieName) where TTooltip : TooltipBaseVM
		{
			Type typeFromHandle = typeof(TRegistered);
			Type typeFromHandle2 = typeof(TTooltip);
			InformationManager._registeredTypes[typeFromHandle] = new InformationManager.TooltipRegistry(typeFromHandle2, onRefreshData, movieName);
		}

		// Token: 0x060001EB RID: 491 RVA: 0x000075D0 File Offset: 0x000057D0
		public static void UnregisterTooltip<TRegistered>()
		{
			Type typeFromHandle = typeof(TRegistered);
			if (InformationManager._registeredTypes.ContainsKey(typeFromHandle))
			{
				InformationManager._registeredTypes.Remove(typeFromHandle);
				Debug.Print("Unregister tooltip for type: " + typeof(TRegistered).Name, 0, Debug.DebugColor.White, 17592186044416UL);
				return;
			}
			Debug.Print("Unable to unregister tooltip because it was not found: " + typeof(TRegistered).Name, 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060001EC RID: 492 RVA: 0x00007656 File Offset: 0x00005856
		public static void Clear()
		{
			InformationManager.DisplayMessageInternal = null;
			InformationManager.HideAllMessagesInternal = null;
			InformationManager.ClearAllMessagesInternal = null;
			InformationManager.OnShowInquiry = null;
			InformationManager.OnShowTextInquiry = null;
			InformationManager.OnHideInquiry = null;
			InformationManager.IsAnyInquiryActiveInternal = null;
			InformationManager.OnShowTooltip = null;
			InformationManager.OnHideTooltip = null;
		}

		// Token: 0x040000AB RID: 171
		public static Func<bool> IsAnyInquiryActiveInternal;

		// Token: 0x040000B0 RID: 176
		public static InformationManager.IsAnyTooltipActiveDelegate IsAnyTooltipActiveInternal;

		// Token: 0x040000B6 RID: 182
		private static Dictionary<Type, InformationManager.TooltipRegistry> _registeredTypes = new Dictionary<Type, InformationManager.TooltipRegistry>();

		// Token: 0x020000D3 RID: 211
		public struct TooltipRegistry
		{
			// Token: 0x06000756 RID: 1878 RVA: 0x0001868E File Offset: 0x0001688E
			public TooltipRegistry(Type tooltipType, object onRefreshData, string movieName)
			{
				this.TooltipType = tooltipType;
				this.OnRefreshData = onRefreshData;
				this.MovieName = movieName;
			}

			// Token: 0x040002A8 RID: 680
			public Type TooltipType;

			// Token: 0x040002A9 RID: 681
			public object OnRefreshData;

			// Token: 0x040002AA RID: 682
			public string MovieName;
		}

		// Token: 0x020000D4 RID: 212
		// (Invoke) Token: 0x06000758 RID: 1880
		public delegate void IsAnyTooltipActiveDelegate(out bool isAnyTooltipActive, out bool isAnyTooltipExtended);
	}
}
