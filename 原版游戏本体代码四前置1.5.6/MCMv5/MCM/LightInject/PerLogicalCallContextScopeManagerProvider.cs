using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class PerLogicalCallContextScopeManagerProvider : ScopeManagerProvider
{
	protected override IScopeManager CreateScopeManager(IServiceFactory serviceFactory)
	{
		return new PerLogicalCallContextScopeManager(serviceFactory);
	}
}
