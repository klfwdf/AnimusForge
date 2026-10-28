using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class PerLogicalCallContextScopeManager(IServiceFactory serviceFactory) : ScopeManager(serviceFactory)
{
	private readonly LogicalThreadStorage<Scope> currentScope = new LogicalThreadStorage<Scope>();

	public override Scope CurrentScope
	{
		get
		{
			return GetThisScopeOrFirstValidAncestor(currentScope.Value);
		}
		set
		{
			currentScope.Value = value;
		}
	}
}
