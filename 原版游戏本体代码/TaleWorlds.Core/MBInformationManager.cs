using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x020000AF RID: 175
	public static class MBInformationManager
	{
		// Token: 0x14000003 RID: 3
		// (add) Token: 0x0600092A RID: 2346 RVA: 0x0001E00C File Offset: 0x0001C20C
		// (remove) Token: 0x0600092B RID: 2347 RVA: 0x0001E040 File Offset: 0x0001C240
		public static event Action<string, int, BasicCharacterObject, Equipment, string> FiringQuickInformation;

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x0600092C RID: 2348 RVA: 0x0001E074 File Offset: 0x0001C274
		// (remove) Token: 0x0600092D RID: 2349 RVA: 0x0001E0A8 File Offset: 0x0001C2A8
		public static event Action ClearingQuickInformations;

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x0600092E RID: 2350 RVA: 0x0001E0DC File Offset: 0x0001C2DC
		// (remove) Token: 0x0600092F RID: 2351 RVA: 0x0001E110 File Offset: 0x0001C310
		public static event Action<MultiSelectionInquiryData, bool, bool> OnShowMultiSelectionInquiry;

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x06000930 RID: 2352 RVA: 0x0001E144 File Offset: 0x0001C344
		// (remove) Token: 0x06000931 RID: 2353 RVA: 0x0001E178 File Offset: 0x0001C378
		public static event Action<InformationData> OnAddMapNotice;

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x06000932 RID: 2354 RVA: 0x0001E1AC File Offset: 0x0001C3AC
		// (remove) Token: 0x06000933 RID: 2355 RVA: 0x0001E1E0 File Offset: 0x0001C3E0
		public static event Action<InformationData> OnRemoveMapNotice;

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06000934 RID: 2356 RVA: 0x0001E214 File Offset: 0x0001C414
		// (remove) Token: 0x06000935 RID: 2357 RVA: 0x0001E248 File Offset: 0x0001C448
		public static event Action<SceneNotificationData> OnShowSceneNotification;

		// Token: 0x14000009 RID: 9
		// (add) Token: 0x06000936 RID: 2358 RVA: 0x0001E27C File Offset: 0x0001C47C
		// (remove) Token: 0x06000937 RID: 2359 RVA: 0x0001E2B0 File Offset: 0x0001C4B0
		public static event Action OnHideSceneNotification;

		// Token: 0x1400000A RID: 10
		// (add) Token: 0x06000938 RID: 2360 RVA: 0x0001E2E4 File Offset: 0x0001C4E4
		// (remove) Token: 0x06000939 RID: 2361 RVA: 0x0001E318 File Offset: 0x0001C518
		public static event Func<bool> IsAnySceneNotificationActive;

		// Token: 0x0600093A RID: 2362 RVA: 0x0001E34B File Offset: 0x0001C54B
		public static void AddQuickInformation(TextObject message, int extraTimeInMs = 0, BasicCharacterObject announcerCharacter = null, Equipment equipment = null, string soundEventPath = "")
		{
			Action<string, int, BasicCharacterObject, Equipment, string> firingQuickInformation = MBInformationManager.FiringQuickInformation;
			if (firingQuickInformation != null)
			{
				firingQuickInformation(message.ToString(), extraTimeInMs, announcerCharacter, equipment, soundEventPath);
			}
			Debug.Print(message.ToString(), 0, Debug.DebugColor.White, 1125899906842624UL);
		}

		// Token: 0x0600093B RID: 2363 RVA: 0x0001E37F File Offset: 0x0001C57F
		public static void ClearQuickInformations()
		{
			Action clearingQuickInformations = MBInformationManager.ClearingQuickInformations;
			if (clearingQuickInformations == null)
			{
				return;
			}
			clearingQuickInformations();
		}

		// Token: 0x0600093C RID: 2364 RVA: 0x0001E390 File Offset: 0x0001C590
		public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData data, bool pauseGameActiveState = false, bool prioritize = false)
		{
			Action<MultiSelectionInquiryData, bool, bool> onShowMultiSelectionInquiry = MBInformationManager.OnShowMultiSelectionInquiry;
			if (onShowMultiSelectionInquiry == null)
			{
				return;
			}
			onShowMultiSelectionInquiry(data, pauseGameActiveState, prioritize);
		}

		// Token: 0x0600093D RID: 2365 RVA: 0x0001E3A4 File Offset: 0x0001C5A4
		public static void AddNotice(InformationData data)
		{
			Action<InformationData> onAddMapNotice = MBInformationManager.OnAddMapNotice;
			if (onAddMapNotice == null)
			{
				return;
			}
			onAddMapNotice(data);
		}

		// Token: 0x0600093E RID: 2366 RVA: 0x0001E3B6 File Offset: 0x0001C5B6
		public static void MapNoticeRemoved(InformationData data)
		{
			Action<InformationData> onRemoveMapNotice = MBInformationManager.OnRemoveMapNotice;
			if (onRemoveMapNotice == null)
			{
				return;
			}
			onRemoveMapNotice(data);
		}

		// Token: 0x0600093F RID: 2367 RVA: 0x0001E3C8 File Offset: 0x0001C5C8
		public static void ShowHint(string hint)
		{
			InformationManager.ShowTooltip(typeof(string), new object[] { hint });
		}

		// Token: 0x06000940 RID: 2368 RVA: 0x0001E3E3 File Offset: 0x0001C5E3
		public static void HideInformations()
		{
			InformationManager.HideTooltip();
		}

		// Token: 0x06000941 RID: 2369 RVA: 0x0001E3EA File Offset: 0x0001C5EA
		public static void ShowSceneNotification(SceneNotificationData data)
		{
			Action<SceneNotificationData> onShowSceneNotification = MBInformationManager.OnShowSceneNotification;
			if (onShowSceneNotification == null)
			{
				return;
			}
			onShowSceneNotification(data);
		}

		// Token: 0x06000942 RID: 2370 RVA: 0x0001E3FC File Offset: 0x0001C5FC
		public static void HideSceneNotification()
		{
			Action onHideSceneNotification = MBInformationManager.OnHideSceneNotification;
			if (onHideSceneNotification == null)
			{
				return;
			}
			onHideSceneNotification();
		}

		// Token: 0x06000943 RID: 2371 RVA: 0x0001E410 File Offset: 0x0001C610
		public static bool? GetIsAnySceneNotificationActive()
		{
			Func<bool> isAnySceneNotificationActive = MBInformationManager.IsAnySceneNotificationActive;
			if (isAnySceneNotificationActive == null)
			{
				return null;
			}
			return new bool?(isAnySceneNotificationActive());
		}

		// Token: 0x06000944 RID: 2372 RVA: 0x0001E43A File Offset: 0x0001C63A
		public static void Clear()
		{
			MBInformationManager.FiringQuickInformation = null;
			MBInformationManager.OnShowMultiSelectionInquiry = null;
			MBInformationManager.OnAddMapNotice = null;
			MBInformationManager.OnRemoveMapNotice = null;
			MBInformationManager.OnShowSceneNotification = null;
			MBInformationManager.OnHideSceneNotification = null;
		}

		// Token: 0x02000120 RID: 288
		public enum NotificationPriority
		{
			// Token: 0x040007BC RID: 1980
			Lowest,
			// Token: 0x040007BD RID: 1981
			Low,
			// Token: 0x040007BE RID: 1982
			Medium,
			// Token: 0x040007BF RID: 1983
			High,
			// Token: 0x040007C0 RID: 1984
			Highest
		}

		// Token: 0x02000121 RID: 289
		public enum NotificationStatus
		{
			// Token: 0x040007C2 RID: 1986
			Inactive,
			// Token: 0x040007C3 RID: 1987
			CurrentlyActive,
			// Token: 0x040007C4 RID: 1988
			InQueue
		}

		// Token: 0x02000122 RID: 290
		public class DialogNotificationHandle
		{
		}
	}
}
