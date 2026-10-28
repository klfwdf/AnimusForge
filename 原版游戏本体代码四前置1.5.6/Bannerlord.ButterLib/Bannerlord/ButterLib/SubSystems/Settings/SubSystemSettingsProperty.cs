using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bannerlord.ButterLib.SubSystems.Settings;

public abstract record SubSystemSettingsProperty<TSubSystem, TProperty>(string Name, string Description, Expression<Func<TSubSystem, TProperty>> Property) : SubSystemSettingsDeclaration<TSubSystem>(Name, Description) where TSubSystem : ISubSystem
{
	[CompilerGenerated]
	public override int GetHashCode()
	{
		return base.GetHashCode() * -1521134295 + EqualityComparer<Expression<Func<TSubSystem, TProperty>>>.Default.GetHashCode(Property);
	}

	[CompilerGenerated]
	protected SubSystemSettingsProperty(SubSystemSettingsProperty<TSubSystem, TProperty> original)
		: base((SubSystemSettingsDeclaration<TSubSystem>)original)
	{
		Property = original.Property;
	}
}
