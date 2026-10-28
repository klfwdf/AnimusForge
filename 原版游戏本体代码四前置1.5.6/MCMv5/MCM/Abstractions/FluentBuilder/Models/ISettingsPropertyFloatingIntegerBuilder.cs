namespace MCM.Abstractions.FluentBuilder.Models;

public interface ISettingsPropertyFloatingIntegerBuilder : ISettingsPropertyBuilder<ISettingsPropertyFloatingIntegerBuilder>, ISettingsPropertyBuilder
{
	ISettingsPropertyBuilder AddValueFormat(string value);
}
