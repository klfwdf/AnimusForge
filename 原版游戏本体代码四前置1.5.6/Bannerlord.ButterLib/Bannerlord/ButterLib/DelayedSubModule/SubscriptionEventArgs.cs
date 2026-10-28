using System;

namespace Bannerlord.ButterLib.DelayedSubModule;

public class SubscriptionEventArgs : EventArgs
{
	public bool IsBase { get; }

	public SubscriptionEventArgs(bool isBase)
	{
		IsBase = isBase;
	}
}
