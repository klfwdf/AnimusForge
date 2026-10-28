using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MeansImplicitUseAttribute : Attribute
{
	[UsedImplicitly]
	public ImplicitUseKindFlags UseKindFlags { get; private set; }

	[UsedImplicitly]
	public ImplicitUseTargetFlags TargetFlags { get; private set; }

	[UsedImplicitly]
	public MeansImplicitUseAttribute()
		: this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default)
	{
	}

	[UsedImplicitly]
	public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
	{
		UseKindFlags = useKindFlags;
		TargetFlags = targetFlags;
	}

	[UsedImplicitly]
	public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags)
		: this(useKindFlags, ImplicitUseTargetFlags.Default)
	{
	}

	[UsedImplicitly]
	public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags)
		: this(ImplicitUseKindFlags.Default, targetFlags)
	{
	}
}
