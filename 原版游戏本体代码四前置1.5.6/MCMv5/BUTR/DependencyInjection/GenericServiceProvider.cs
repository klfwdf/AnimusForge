namespace BUTR.DependencyInjection;

public static class GenericServiceProvider
{
	internal static IGenericServiceProvider? GlobalServiceProvider;

	internal static IGenericServiceProviderScope? GameScopeServiceProvider;

	public static TService? GetService<TService>() where TService : class
	{
		object result;
		if (GameScopeServiceProvider == null)
		{
			IGenericServiceProvider? globalServiceProvider = GlobalServiceProvider;
			result = ((globalServiceProvider != null) ? globalServiceProvider.GetService<TService>() : null);
		}
		else
		{
			result = GameScopeServiceProvider.GetService<TService>();
		}
		return (TService?)result;
	}

	public static IGenericServiceProviderScope? CreateScope()
	{
		return GlobalServiceProvider?.CreateScope();
	}
}
