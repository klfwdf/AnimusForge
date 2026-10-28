using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bannerlord.ButterLib.SubSystems.Settings;

public record SubSystemSettingsPropertyInt<TSubSystem> : SubSystemSettingsProperty<TSubSystem, int> where TSubSystem : ISubSystem
{
	public int MinValue { get; init; }

	public int MaxValue { get; init; }

	public SubSystemSettingsPropertyInt(string Name, string Description, Expression<Func<TSubSystem, int>> Property, int MinValue, int MaxValue)
	{
		this.MinValue = MinValue;
		this.MaxValue = MaxValue;
		base._002Ector(Name, Description, Property);
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return (base.GetHashCode() * -1521134295 + EqualityComparer<int>.Default.GetHashCode(MinValue)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(MaxValue);
	}

	[CompilerGenerated]
	protected SubSystemSettingsPropertyInt(SubSystemSettingsPropertyInt<TSubSystem> original)
		: base((SubSystemSettingsProperty<TSubSystem, int>)original)
	{
		MinValue = original.MinValue;
		MaxValue = original.MaxValue;
	}

	[CompilerGenerated]
	public void Deconstruct(out string Name, out string Description, out Expression<Func<TSubSystem, int>> Property, out int MinValue, out int MaxValue)
	{
		Name = base.Name;
		Description = base.Description;
		Property = base.Property;
		MinValue = this.MinValue;
		MaxValue = this.MaxValue;
	}
}
