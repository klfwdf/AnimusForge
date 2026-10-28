using System;
using TaleWorlds.Engine.Options;

namespace TaleWorlds.MountAndBlade.Options;

public class ActionOptionData : IOptionData
{
	private TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType _managedType;

	private NativeOptions.NativeOptionsType _nativeType;

	private string _actionOptionTypeId;

	public Action OnAction { get; private set; }

	public ActionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType managedType, Action onAction)
	{
		_managedType = managedType;
		OnAction = onAction;
	}

	public ActionOptionData(NativeOptions.NativeOptionsType nativeType, Action onAction)
	{
		_nativeType = nativeType;
		OnAction = onAction;
	}

	public ActionOptionData(string optionTypeId, Action onAction)
	{
		_actionOptionTypeId = optionTypeId;
		_nativeType = NativeOptions.NativeOptionsType.None;
		OnAction = onAction;
	}

	public void Commit()
	{
	}

	public float GetDefaultValue()
	{
		return 0f;
	}

	public object GetOptionType()
	{
		if (_nativeType != NativeOptions.NativeOptionsType.None)
		{
			return _nativeType;
		}
		if (_managedType != TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.Language)
		{
			return _managedType;
		}
		return _actionOptionTypeId;
	}

	public float GetValue(bool forceRefresh)
	{
		return 0f;
	}

	public bool IsNative()
	{
		return _nativeType != NativeOptions.NativeOptionsType.None;
	}

	public void SetValue(float value)
	{
	}

	public bool IsAction()
	{
		if (_nativeType == NativeOptions.NativeOptionsType.None)
		{
			return _managedType == TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.Language;
		}
		return false;
	}

	public (string, bool) GetIsDisabledAndReasonID()
	{
		return (string.Empty, false);
	}
}
