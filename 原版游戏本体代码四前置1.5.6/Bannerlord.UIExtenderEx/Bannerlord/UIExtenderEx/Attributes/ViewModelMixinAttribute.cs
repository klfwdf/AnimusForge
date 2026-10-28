using System;

namespace Bannerlord.UIExtenderEx.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ViewModelMixinAttribute : BaseUIExtenderAttribute
{
	public string? RefreshMethodName { get; }

	public bool HandleDerived { get; }

	public ViewModelMixinAttribute()
	{
	}

	public ViewModelMixinAttribute(string refreshMethodName)
	{
		RefreshMethodName = refreshMethodName;
	}

	public ViewModelMixinAttribute(bool handleDerived)
	{
		HandleDerived = handleDerived;
	}

	public ViewModelMixinAttribute(string? refreshMethodName = null, bool handleDerived = false)
	{
		RefreshMethodName = refreshMethodName;
		HandleDerived = handleDerived;
	}
}
