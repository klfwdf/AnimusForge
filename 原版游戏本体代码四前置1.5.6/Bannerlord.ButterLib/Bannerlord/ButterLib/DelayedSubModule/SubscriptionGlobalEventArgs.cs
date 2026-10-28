using System;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.DelayedSubModule;

public class SubscriptionGlobalEventArgs : EventArgs
{
	public Type Type { get; }

	public bool IsBase { get; }

	public SubscriptionType SubscriptionType { get; }

	public string MethodName { get; }

	public SubscriptionGlobalEventArgs(Type type, bool isBase, SubscriptionType subscriptionType, string methodName)
	{
		if (!typeof(MBSubModuleBase).IsAssignableFrom(type))
		{
			throw new ArgumentException(type.FullName + " is not supported type.", "type");
		}
		if (!Enum.IsDefined(typeof(SubscriptionType), subscriptionType))
		{
			throw new ArgumentOutOfRangeException("subscriptionType", subscriptionType, "DelayedSubModuleEventArgs .ctor is supplied with not supported SubscriptionType value.");
		}
		Type = type;
		IsBase = isBase;
		SubscriptionType = subscriptionType;
		MethodName = methodName;
	}

	public bool IsValid<T>(string methodName, SubscriptionType subscriptionType) where T : MBSubModuleBase
	{
		return IsValid(typeof(T), methodName, subscriptionType);
	}

	public bool IsValid(Type type, string methodName, SubscriptionType subscriptionType)
	{
		if (type.IsSubclassOf(typeof(MBSubModuleBase)) && Type == type && MethodName == methodName)
		{
			return SubscriptionType == subscriptionType;
		}
		return false;
	}
}
