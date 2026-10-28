using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bannerlord.ButterLib.SubSystems.Settings;

public record SubSystemSettingsPropertyFloat<TSubSystem> : SubSystemSettingsProperty<TSubSystem, float> where TSubSystem : ISubSystem
{
	public float MinValue { get; init; }

	public float MaxValue { get; init; }

	public SubSystemSettingsPropertyFloat(string Name, string Description, Expression<Func<TSubSystem, float>> Property, float MinValue, float MaxValue)
	{
		this.MinValue = MinValue;
		this.MaxValue = MaxValue;
		base._002Ector(Name, Description, Property);
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return (base.GetHashCode() * -1521134295 + EqualityComparer<float>.Default.GetHashCode(MinValue)) * -1521134295 + EqualityComparer<float>.Default.GetHashCode(MaxValue);
	}

	[CompilerGenerated]
	protected SubSystemSettingsPropertyFloat(SubSystemSettingsPropertyFloat<TSubSystem> original)
		: base((SubSystemSettingsProperty<TSubSystem, float>)original)
	{
		MinValue = original.MinValue;
		MaxValue = original.MaxValue;
	}

	[CompilerGenerated]
	public void Deconstruct(out string Name, out string Description, out Expression<Func<TSubSystem, float>> Property, out float MinValue, out float MaxValue)
	{
		Name = base.Name;
		Description = base.Description;
		Property = base.Property;
		MinValue = this.MinValue;
		MaxValue = this.MaxValue;
	}
}
