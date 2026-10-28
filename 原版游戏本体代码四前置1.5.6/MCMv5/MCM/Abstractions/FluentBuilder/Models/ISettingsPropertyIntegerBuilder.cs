namespace MCM.Abstractions.FluentBuilder.Models;

public interface ISettingsPropertyIntegerBuilder : ISettingsPropertyBuilder<ISettingsPropertyIntegerBuilder>, ISettingsPropertyBuilder
{
	ISettingsPropertyBuilder AddValueFormat(string value);
}
