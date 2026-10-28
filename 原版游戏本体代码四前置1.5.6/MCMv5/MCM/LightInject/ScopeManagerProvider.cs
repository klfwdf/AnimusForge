using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal abstract class ScopeManagerProvider : IScopeManagerProvider
{
	private readonly object lockObject = new object();

	private IScopeManager scopeManager;

	public IScopeManager GetScopeManager(IServiceFactory serviceFactory)
	{
		if (scopeManager == null)
		{
			lock (lockObject)
			{
				if (scopeManager == null)
				{
					scopeManager = CreateScopeManager(serviceFactory);
				}
			}
		}
		return scopeManager;
	}

	protected abstract IScopeManager CreateScopeManager(IServiceFactory serviceFactory);
}
