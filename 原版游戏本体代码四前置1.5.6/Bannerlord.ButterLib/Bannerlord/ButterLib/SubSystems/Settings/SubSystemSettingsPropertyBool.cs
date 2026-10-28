using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bannerlord.ButterLib.SubSystems.Settings;

public record SubSystemSettingsPropertyBool<TSubSystem> : SubSystemSettingsProperty<TSubSystem, bool> where TSubSystem : ISubSystem
{
	public SubSystemSettingsPropertyBool(string Name, string Description, Expression<Func<TSubSystem, bool>> Property)
		: base(Name, Description, Property)
	{
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[CompilerGenerated]
	protected SubSystemSettingsPropertyBool(SubSystemSettingsPropertyBool<TSubSystem> original)
		: base((SubSystemSettingsProperty<TSubSystem, bool>)original)
	{
	}

	[CompilerGenerated]
	public new void Deconstruct(out string Name, out string Description, out Expression<Func<TSubSystem, bool>> Property)
	{
		Name = base.Name;
		Description = base.Description;
		Property = base.Property;
	}
}
