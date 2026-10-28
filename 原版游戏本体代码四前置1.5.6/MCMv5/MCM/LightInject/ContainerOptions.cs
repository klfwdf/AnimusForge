using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ContainerOptions
{
	private static readonly Lazy<ContainerOptions> DefaultOptions = new Lazy<ContainerOptions>(CreateDefaultContainerOptions);

	public static ContainerOptions Default => DefaultOptions.Value;

	public bool EnableVariance { get; set; }

	public Func<Type, bool> VarianceFilter { get; set; } = (Type _) => true;

	public Func<Type, Action<LogEntry>> LogFactory { get; set; }

	public Func<string[], string> DefaultServiceSelector { get; set; } = (string[] services) => string.Empty;

	public bool EnablePropertyInjection { get; set; }

	public bool EnableCurrentScope { get; set; }

	public bool EnableOptionalArguments { get; set; }

	public bool OptimizeForLargeObjectGraphs { get; set; }

	public ContainerOptions()
	{
		EnableVariance = true;
		EnablePropertyInjection = true;
		LogFactory = (Type t) => delegate
		{
		};
		EnableCurrentScope = true;
		EnableOptionalArguments = false;
		OptimizeForLargeObjectGraphs = false;
	}

	private static ContainerOptions CreateDefaultContainerOptions()
	{
		return new ContainerOptions();
	}
}
