using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public sealed class UsedImplicitlyAttribute : Attribute
{
	[UsedImplicitly]
	public ImplicitUseKindFlags UseKindFlags { get; private set; }

	[UsedImplicitly]
	public ImplicitUseTargetFlags TargetFlags { get; private set; }

	[UsedImplicitly]
	public UsedImplicitlyAttribute()
		: this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default)
	{
	}

	[UsedImplicitly]
	public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
	{
		UseKindFlags = useKindFlags;
		TargetFlags = targetFlags;
	}

	[UsedImplicitly]
	public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags)
		: this(useKindFlags, ImplicitUseTargetFlags.Default)
	{
	}

	[UsedImplicitly]
	public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags)
		: this(ImplicitUseKindFlags.Default, targetFlags)
	{
	}
}
