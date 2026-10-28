namespace BUTR.DependencyInjection;

public interface IGenericServiceFactory
{
	TService GetService<TService>() where TService : class;
}
