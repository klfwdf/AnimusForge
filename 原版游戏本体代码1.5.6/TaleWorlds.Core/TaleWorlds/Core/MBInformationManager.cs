using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public static class MBInformationManager
{
	public enum NotificationPriority
	{
		Lowest,
		Low,
		Medium,
		High,
		Highest
	}

	public enum NotificationStatus
	{
		Inactive,
		CurrentlyActive,
		InQueue
	}

	public class DialogNotificationHandle
	{
	}

	public static event Action<string, int, BasicCharacterObject, Equipment, string> FiringQuickInformation;

	public static event Action ClearingQuickInformations;

	public static event Action<MultiSelectionInquiryData, bool, bool> OnShowMultiSelectionInquiry;

	public static event Action<InformationData> OnAddMapNotice;

	public static event Action<InformationData> OnRemoveMapNotice;

	public static event Action<SceneNotificationData> OnShowSceneNotification;

	public static event Action OnHideSceneNotification;

	public static event Func<bool> IsAnySceneNotificationActive;

	public static event Func<SceneNotificationData> ActiveSceneNotificationData;

	public static void AddQuickInformation(TextObject message, int extraTimeInMs = 0, BasicCharacterObject announcerCharacter = null, Equipment equipment = null, string soundEventPath = "")
	{
		FiringQuickInformation?.Invoke(message.ToString(), extraTimeInMs, announcerCharacter, equipment, soundEventPath);
		Debug.Print(message.ToString(), 0, Debug.DebugColor.White, 1125899906842624uL);
	}

	public static void ClearQuickInformations()
	{
		ClearingQuickInformations?.Invoke();
	}

	public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData data, bool pauseGameActiveState = false, bool prioritize = false)
	{
		OnShowMultiSelectionInquiry?.Invoke(data, pauseGameActiveState, prioritize);
	}

	public static void AddNotice(InformationData data)
	{
		OnAddMapNotice?.Invoke(data);
	}

	public static void MapNoticeRemoved(InformationData data)
	{
		OnRemoveMapNotice?.Invoke(data);
	}

	public static void ShowHint(string hint)
	{
		InformationManager.ShowTooltip(typeof(string), hint);
	}

	public static void HideInformations()
	{
		InformationManager.HideTooltip();
	}

	public static void ShowSceneNotification(SceneNotificationData data)
	{
		OnShowSceneNotification?.Invoke(data);
	}

	public static void HideSceneNotification()
	{
		OnHideSceneNotification?.Invoke();
	}

	public static bool? GetIsAnySceneNotificationActive()
	{
		return IsAnySceneNotificationActive?.Invoke();
	}

	public static SceneNotificationData GetActiveSceneNotificationData()
	{
		return ActiveSceneNotificationData?.Invoke();
	}

	public static void Clear()
	{
		FiringQuickInformation = null;
		OnShowMultiSelectionInquiry = null;
		OnAddMapNotice = null;
		OnRemoveMapNotice = null;
		OnShowSceneNotification = null;
		OnHideSceneNotification = null;
	}
}
