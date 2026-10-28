using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class LogicalThreadStorage<T>
{
	private readonly AsyncLocal<T> asyncLocal = new AsyncLocal<T>();

	public T Value
	{
		get
		{
			return asyncLocal.Value;
		}
		set
		{
			asyncLocal.Value = value;
		}
	}
}
