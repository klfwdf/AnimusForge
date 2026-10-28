using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class PerThreadScopeManagerProvider : ScopeManagerProvider
{
	protected override IScopeManager CreateScopeManager(IServiceFactory serviceFactory)
	{
		return new PerThreadScopeManager(serviceFactory);
	}
}
