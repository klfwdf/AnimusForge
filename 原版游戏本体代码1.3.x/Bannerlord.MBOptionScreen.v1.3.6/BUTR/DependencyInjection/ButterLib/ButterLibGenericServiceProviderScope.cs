using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace BUTR.DependencyInjection.ButterLib
{
	// Token: 0x0200003F RID: 63
	internal class ButterLibGenericServiceProviderScope : IGenericServiceProviderScope, IDisposable
	{
		// Token: 0x06000203 RID: 515 RVA: 0x000084BF File Offset: 0x000066BF
		[NullableContext(1)]
		public ButterLibGenericServiceProviderScope(IServiceScope serviceScope)
		{
			this._serviceScope = serviceScope;
		}

		// Token: 0x06000204 RID: 516 RVA: 0x000084CE File Offset: 0x000066CE
		[NullableContext(1)]
		[return: Nullable(2)]
		public TService GetService<TService>() where TService : class
		{
			return ServiceProviderServiceExtensions.GetRequiredService<TService>(this._serviceScope.ServiceProvider);
		}

		// Token: 0x06000205 RID: 517 RVA: 0x000084E0 File Offset: 0x000066E0
		public void Dispose()
		{
			this._serviceScope.Dispose();
		}

		// Token: 0x0400008F RID: 143
		[Nullable(1)]
		private readonly IServiceScope _serviceScope;
	}
}
