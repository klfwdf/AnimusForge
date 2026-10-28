using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class PerThreadScopeManager(IServiceFactory serviceFactory) : ScopeManager(serviceFactory)
{
	private readonly ThreadLocal<Scope> threadLocalScope = new ThreadLocal<Scope>();

	public override Scope CurrentScope
	{
		get
		{
			return GetThisScopeOrFirstValidAncestor(threadLocalScope.Value);
		}
		set
		{
			threadLocalScope.Value = value;
		}
	}
}
