using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ServiceRegistration : Registration
{
	public string ServiceName { get; set; } = string.Empty;

	public ILifetime Lifetime { get; set; }

	public object Value { get; set; }

	public override int GetHashCode()
	{
		return base.ServiceType.GetHashCode() ^ ServiceName.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (!(obj is ServiceRegistration serviceRegistration))
		{
			return false;
		}
		return ServiceName == serviceRegistration.ServiceName && base.ServiceType == serviceRegistration.ServiceType;
	}

	public override string ToString()
	{
		string text = Lifetime?.ToString() ?? "Transient";
		return $"ServiceType: '{base.ServiceType}', ServiceName: '{ServiceName}', ImplementingType: '{ImplementingType}', Lifetime: '{text}'";
	}
}
