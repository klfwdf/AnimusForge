using System;
using System.Collections.Generic;

namespace TaleWorlds.Library;

public static class InformationManager
{
	public struct TooltipRegistry
	{
		public Type TooltipType;

		public object OnRefreshData;

		public string MovieName;

		public TooltipRegistry(Type tooltipType, object onRefreshData, string movieName)
		{
			TooltipType = tooltipType;
			OnRefreshData = onRefreshData;
			MovieName = movieName;
		}
	}

	public delegate void IsAnyTooltipActiveDelegate(out bool isAnyTooltipActive, out bool isAnyTooltipExtended);

	public static Func<bool> IsAnyInquiryActiveInternal;

	public static IsAnyTooltipActiveDelegate IsAnyTooltipActiveInternal;

	private static Dictionary<Type, TooltipRegistry> _registeredTypes = new Dictionary<Type, TooltipRegistry>();

	public static IReadOnlyDictionary<Type, TooltipRegistry> RegisteredTypes => _registeredTypes;

	public static event Action<InformationMessage> DisplayMessageInternal;

	public static event Action ClearAllMessagesInternal;

	public static event Action HideAllMessagesInternal;

	public static event Action<string> OnAddSystemNotification;

	public static event Action<Type, object[]> OnShowTooltip;

	public static event Action OnHideTooltip;

	public static event Action<InquiryData, bool, bool> OnShowInquiry;

	public static event Action<TextInquiryData, bool, bool> OnShowTextInquiry;

	public static event Action OnHideInquiry;

	public static bool IsAnyInquiryActive()
	{
		if (IsAnyInquiryActiveInternal != null)
		{
			return IsAnyInquiryActiveInternal();
		}
		return false;
	}

	public static void DisplayMessage(InformationMessage message)
	{
		InformationManager.DisplayMessageInternal?.Invoke(message);
	}

	public static void HideAllMessages()
	{
		InformationManager.HideAllMessagesInternal?.Invoke();
	}

	public static void ClearAllMessages()
	{
		InformationManager.ClearAllMessagesInternal?.Invoke();
	}

	public static void AddSystemNotification(string message)
	{
		InformationManager.OnAddSystemNotification?.Invoke(message);
	}

	public static void ShowTooltip(Type type, params object[] args)
	{
		InformationManager.OnShowTooltip?.Invoke(type, args);
	}

	public static void HideTooltip()
	{
		InformationManager.OnHideTooltip?.Invoke();
	}

	public static void ShowInquiry(InquiryData data, bool pauseGameActiveState = false, bool prioritize = false)
	{
		InformationManager.OnShowInquiry?.Invoke(data, pauseGameActiveState, prioritize);
	}

	public static void ShowTextInquiry(TextInquiryData textData, bool pauseGameActiveState = false, bool prioritize = false)
	{
		InformationManager.OnShowTextInquiry?.Invoke(textData, pauseGameActiveState, prioritize);
	}

	public static void HideInquiry()
	{
		InformationManager.OnHideInquiry?.Invoke();
	}

	public static bool GetIsAnyTooltipActive()
	{
		if (IsAnyTooltipActiveInternal != null)
		{
			IsAnyTooltipActiveInternal(out var isAnyTooltipActive, out var _);
			return isAnyTooltipActive;
		}
		return false;
	}

	public static bool GetIsAnyTooltipActiveAndExtended()
	{
		if (IsAnyTooltipActiveInternal != null)
		{
			IsAnyTooltipActiveInternal(out var isAnyTooltipActive, out var isAnyTooltipExtended);
			return isAnyTooltipActive && isAnyTooltipExtended;
		}
		return false;
	}

	public static void RegisterTooltip<TRegistered, TTooltip>(Action<TTooltip, object[]> onRefreshData, string movieName) where TTooltip : TooltipBaseVM
	{
		Type typeFromHandle = typeof(TRegistered);
		Type typeFromHandle2 = typeof(TTooltip);
		_registeredTypes[typeFromHandle] = new TooltipRegistry(typeFromHandle2, onRefreshData, movieName);
	}

	public static void UnregisterTooltip<TRegistered>()
	{
		Type typeFromHandle = typeof(TRegistered);
		if (_registeredTypes.ContainsKey(typeFromHandle))
		{
			_registeredTypes.Remove(typeFromHandle);
			Debug.Print("Unregister tooltip for type: " + typeof(TRegistered).Name);
		}
		else
		{
			Debug.Print("Unable to unregister tooltip because it was not found: " + typeof(TRegistered).Name);
		}
	}

	public static void Clear()
	{
		InformationManager.DisplayMessageInternal = null;
		InformationManager.HideAllMessagesInternal = null;
		InformationManager.ClearAllMessagesInternal = null;
		InformationManager.OnShowInquiry = null;
		InformationManager.OnShowTextInquiry = null;
		InformationManager.OnHideInquiry = null;
		IsAnyInquiryActiveInternal = null;
		InformationManager.OnShowTooltip = null;
		InformationManager.OnHideTooltip = null;
	}
}
