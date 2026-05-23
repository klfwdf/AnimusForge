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

	public static void AddQuickInformation(TextObject message, int extraTimeInMs = 0, BasicCharacterObject announcerCharacter = null, Equipment equipment = null, string soundEventPath = "")
	{
		MBInformationManager.FiringQuickInformation?.Invoke(message.ToString(), extraTimeInMs, announcerCharacter, equipment, soundEventPath);
		Debug.Print(message.ToString(), 0, Debug.DebugColor.White, 1125899906842624uL);
	}

	public static void ClearQuickInformations()
	{
		MBInformationManager.ClearingQuickInformations?.Invoke();
	}

	public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData data, bool pauseGameActiveState = false, bool prioritize = false)
	{
		MBInformationManager.OnShowMultiSelectionInquiry?.Invoke(data, pauseGameActiveState, prioritize);
	}

	public static void AddNotice(InformationData data)
	{
		MBInformationManager.OnAddMapNotice?.Invoke(data);
	}

	public static void MapNoticeRemoved(InformationData data)
	{
		MBInformationManager.OnRemoveMapNotice?.Invoke(data);
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
		MBInformationManager.OnShowSceneNotification?.Invoke(data);
	}

	public static void HideSceneNotification()
	{
		MBInformationManager.OnHideSceneNotification?.Invoke();
	}

	public static bool? GetIsAnySceneNotificationActive()
	{
		return MBInformationManager.IsAnySceneNotificationActive?.Invoke();
	}

	public static void Clear()
	{
		MBInformationManager.FiringQuickInformation = null;
		MBInformationManager.OnShowMultiSelectionInquiry = null;
		MBInformationManager.OnAddMapNotice = null;
		MBInformationManager.OnRemoveMapNotice = null;
		MBInformationManager.OnShowSceneNotification = null;
		MBInformationManager.OnHideSceneNotification = null;
	}
}
