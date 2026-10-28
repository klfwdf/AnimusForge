using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Bannerlord.ButterLib.SubSystems.Settings;

public record SubSystemSettingsPropertyDropdown<TSubSystem> : SubSystemSettingsProperty<TSubSystem, IList<string>> where TSubSystem : ISubSystem
{
	public int SelectedIndex { get; set; }

	public SubSystemSettingsPropertyDropdown(string Name, string Description, Expression<Func<TSubSystem, IList<string>>> Property, int SelectedIndex)
	{
		this.SelectedIndex = SelectedIndex;
		base._002Ector(Name, Description, Property);
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return base.GetHashCode() * -1521134295 + EqualityComparer<int>.Default.GetHashCode(SelectedIndex);
	}

	[CompilerGenerated]
	protected SubSystemSettingsPropertyDropdown(SubSystemSettingsPropertyDropdown<TSubSystem> original)
		: base((SubSystemSettingsProperty<TSubSystem, IList<string>>)original)
	{
		SelectedIndex = original.SelectedIndex;
	}

	[CompilerGenerated]
	public void Deconstruct(out string Name, out string Description, out Expression<Func<TSubSystem, IList<string>>> Property, out int SelectedIndex)
	{
		Name = base.Name;
		Description = base.Description;
		Property = base.Property;
		SelectedIndex = this.SelectedIndex;
	}
}
