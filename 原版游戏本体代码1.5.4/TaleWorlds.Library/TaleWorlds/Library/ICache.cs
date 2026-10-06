using System;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public interface ICache
{
	Task<TItem> GetOrUpdate<TItem>(string key, Func<Task<TItem>> factory, TimeSpan absoluteExpirationRelativeToNow, bool getFromFactoryIfCacheFails = true);

	Task SetString(string key, string value, TimeSpan? absoluteExpirationRelativeToNow);

	Task<string> GetString(string key);
}
