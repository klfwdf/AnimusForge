using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bannerlord.ButterLib.SubSystems.Settings;

public record SubSystemSettingsPropertyText<TSubSystem> : SubSystemSettingsProperty<TSubSystem, string> where TSubSystem : ISubSystem
{
	public SubSystemSettingsPropertyText(string Name, string Description, Expression<Func<TSubSystem, string>> Property)
		: base(Name, Description, Property)
	{
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[CompilerGenerated]
	protected SubSystemSettingsPropertyText(SubSystemSettingsPropertyText<TSubSystem> original)
		: base((SubSystemSettingsProperty<TSubSystem, string>)original)
	{
	}

	[CompilerGenerated]
	public new void Deconstruct(out string Name, out string Description, out Expression<Func<TSubSystem, string>> Property)
	{
		Name = base.Name;
		Description = base.Description;
		Property = base.Property;
	}
}
