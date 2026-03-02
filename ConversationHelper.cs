using System;
using System.Reflection;

namespace Voxforge;

public static class ConversationHelper
{
	private static object _currentVM;

	private static PropertyInfo _dialogTextProp;

	private static FieldInfo _dialogTextField;

	private static PropertyInfo _nameLabelProp;

	private static FieldInfo _nameLabelField;

	private static MethodInfo _onPropertyChangedMethod;

	private static readonly object _pendingLock = new object();

	private static string _pendingText = null;

	private static bool _hasPending = false;

	private static volatile bool _isStreaming = false;

	private static volatile string _lastStreamText = null;

	private static int _vmCaptureCount = 0;

	private static int _tickApplyCount = 0;

	private static int _refreshReapplyCount = 0;

	private static string _nameSuffix = null;

	public static bool IsStreaming => _isStreaming;

	public static bool HasActiveVM => _currentVM != null && (_dialogTextProp != null || _dialogTextField != null);

	private static object ResolveConversationVm(object vm)
	{
		if (vm == null)
		{
			return null;
		}
		try
		{
			Type type = vm.GetType();
			PropertyInfo property = type.GetProperty("DialogController", BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				object value = property.GetValue(vm, null);
				if (value != null)
				{
					return value;
				}
			}
		}
		catch
		{
		}
		return vm;
	}

	public static void SetCurrentVM(object vm)
	{
		_currentVM = ResolveConversationVm(vm);
		_dialogTextProp = null;
		_dialogTextField = null;
		_nameLabelProp = null;
		_nameLabelField = null;
		_onPropertyChangedMethod = null;
		if (_currentVM != null)
		{
			try
			{
				Type type = _currentVM.GetType();
				_dialogTextProp = type.GetProperty("DialogText", BindingFlags.Instance | BindingFlags.Public);
				_dialogTextField = type.GetField("_dialogText", BindingFlags.Instance | BindingFlags.NonPublic);
				_nameLabelProp = type.GetProperty("CurrentCharacterNameLbl", BindingFlags.Instance | BindingFlags.Public);
				_nameLabelField = type.GetField("_currentCharacterNameLbl", BindingFlags.Instance | BindingFlags.NonPublic);
				if (_nameLabelProp == null)
				{
					_nameLabelProp = type.GetProperty("CharacterName", BindingFlags.Instance | BindingFlags.Public);
				}
				if (_nameLabelField == null)
				{
					_nameLabelField = type.GetField("_characterName", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				try
				{
					Type type2 = type;
					while (type2 != null && _onPropertyChangedMethod == null)
					{
						MethodInfo[] methods = type2.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						MethodInfo[] array = methods;
						foreach (MethodInfo methodInfo in array)
						{
							if (methodInfo.Name == "OnPropertyChangedWithValue" && !methodInfo.IsGenericMethod)
							{
								ParameterInfo[] parameters = methodInfo.GetParameters();
								if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(string))
								{
									_onPropertyChangedMethod = methodInfo;
									break;
								}
							}
						}
						type2 = type2.BaseType;
					}
				}
				catch
				{
				}
				_vmCaptureCount++;
				if (_vmCaptureCount <= 3)
				{
					Logger.LogTrace("ConversationHelper", $"✅ VM 捕获 #{_vmCaptureCount}。Prop={_dialogTextProp != null}, Field={_dialogTextField != null}, OnPropChanged={_onPropertyChangedMethod != null}, VM类型={type.FullName}");
				}
			}
			catch (Exception ex)
			{
				Logger.LogTrace("ConversationHelper", "❌ 获取 DialogText 属性/字段失败: " + ex.Message);
			}
		}
		ApplyNameLabelToVM();
	}

	public static void SetNameSuffix(string suffix)
	{
		_nameSuffix = (string.IsNullOrWhiteSpace(suffix) ? null : suffix.Trim());
		ApplyNameLabelToVM();
	}

	public static void ClearNameSuffix()
	{
		_nameSuffix = null;
		ApplyNameLabelToVM();
	}

	public static void BeginStreaming()
	{
		_isStreaming = true;
		_lastStreamText = null;
		_tickApplyCount = 0;
		_refreshReapplyCount = 0;
		Logger.LogTrace("ConversationHelper", "\ud83d\udd04 BeginStreaming - 流式传输开始");
	}

	public static void EndStreaming()
	{
		_isStreaming = false;
		Logger.LogTrace("ConversationHelper", $"✅ EndStreaming - 流式传输结束 (TickApply={_tickApplyCount}, RefreshReapply={_refreshReapplyCount})");
	}

	public static void UpdateDialogText(string text)
	{
		lock (_pendingLock)
		{
			_pendingText = text;
			_hasPending = true;
		}
		_lastStreamText = text;
	}

	public static void Tick()
	{
		string text = null;
		lock (_pendingLock)
		{
			if (!_hasPending)
			{
				ApplyNameLabelToVM();
				return;
			}
			text = _pendingText;
			_hasPending = false;
		}
		if (text != null)
		{
			_tickApplyCount++;
			if (_tickApplyCount <= 5)
			{
				Logger.LogTrace("ConversationHelper", $"\ud83d\udcdd Tick #{_tickApplyCount} 应用文本 (len={text.Length}, VM={_currentVM != null}, Prop={_dialogTextProp != null})");
			}
			ApplyTextToVM(text);
			ApplyNameLabelToVM();
		}
	}

	public static void OnRefreshPostfix()
	{
		if (!_isStreaming)
		{
			ApplyNameLabelToVM();
			return;
		}
		string lastStreamText = _lastStreamText;
		if (string.IsNullOrEmpty(lastStreamText))
		{
			ApplyNameLabelToVM();
			return;
		}
		_refreshReapplyCount++;
		ApplyTextToVM(lastStreamText);
		ApplyNameLabelToVM();
	}

	private static string StripNameSuffix(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return raw ?? "";
		}
		string text = raw.TrimEnd();
		int num = text.LastIndexOf("（", StringComparison.Ordinal);
		if (num > 0 && text.EndsWith("）", StringComparison.Ordinal))
		{
			return text.Substring(0, num).TrimEnd();
		}
		int num2 = text.LastIndexOf(" [P:", StringComparison.Ordinal);
		if (num2 > 0 && text.EndsWith("]", StringComparison.Ordinal))
		{
			return text.Substring(0, num2).TrimEnd();
		}
		return text;
	}

	private static void ApplyNameLabelToVM()
	{
		object currentVM = _currentVM;
		if (currentVM == null || (_nameLabelProp == null && _nameLabelField == null))
		{
			return;
		}
		try
		{
			string text = null;
			if (_nameLabelProp != null)
			{
				text = _nameLabelProp.GetValue(currentVM, null) as string;
			}
			else if (_nameLabelField != null)
			{
				text = _nameLabelField.GetValue(currentVM) as string;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string text2 = StripNameSuffix(text);
			string nameSuffix = _nameSuffix;
			string text3 = (string.IsNullOrWhiteSpace(nameSuffix) ? text2 : (text2 + "（" + nameSuffix + "）"));
			if (string.Equals(text3, text, StringComparison.Ordinal))
			{
				return;
			}
			if (_nameLabelProp != null)
			{
				_nameLabelProp.SetValue(currentVM, text3, null);
			}
			else
			{
				if (!(_nameLabelField != null))
				{
					return;
				}
				_nameLabelField.SetValue(currentVM, text3);
				MethodInfo onPropertyChangedMethod = _onPropertyChangedMethod;
				if (onPropertyChangedMethod != null)
				{
					try
					{
						onPropertyChangedMethod.Invoke(currentVM, new object[2] { text3, "CurrentCharacterNameLbl" });
						return;
					}
					catch
					{
						return;
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void ApplyTextToVM(string text)
	{
		object currentVM = _currentVM;
		if (currentVM == null)
		{
			return;
		}
		try
		{
			PropertyInfo dialogTextProp = _dialogTextProp;
			if (dialogTextProp != null)
			{
				dialogTextProp.SetValue(currentVM, text);
				return;
			}
			FieldInfo dialogTextField = _dialogTextField;
			if (!(dialogTextField != null))
			{
				return;
			}
			dialogTextField.SetValue(currentVM, text);
			MethodInfo onPropertyChangedMethod = _onPropertyChangedMethod;
			if (!(onPropertyChangedMethod != null))
			{
				return;
			}
			try
			{
				onPropertyChangedMethod.Invoke(currentVM, new object[2] { text, "DialogText" });
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("ConversationHelper", "❌ ApplyTextToVM 失败: " + ex.Message);
		}
	}

	public static void Clear()
	{
		_isStreaming = false;
		_lastStreamText = null;
		_currentVM = null;
		_dialogTextProp = null;
		_dialogTextField = null;
		_nameLabelProp = null;
		_nameLabelField = null;
		_onPropertyChangedMethod = null;
		_nameSuffix = null;
		lock (_pendingLock)
		{
			_pendingText = null;
			_hasPending = false;
		}
	}
}
