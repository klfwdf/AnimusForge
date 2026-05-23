using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace BUTR.DependencyInjection.ButterLib
{
	// Token: 0x0200003D RID: 61
	[NullableContext(1)]
	[Nullable(0)]
	internal class ButterLibGenericServiceFactory : IGenericServiceFactory
	{
		// Token: 0x060001FC RID: 508 RVA: 0x0000844F File Offset: 0x0000664F
		public ButterLibGenericServiceFactory(IServiceProvider factory)
		{
			this._serviceProvider = factory;
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0000845E File Offset: 0x0000665E
		public TService GetService<TService>() where TService : class
		{
			return ServiceProviderServiceExtensions.GetRequiredService<TService>(this._serviceProvider);
		}

		// Token: 0x0400008E RID: 142
		private readonly IServiceProvider _serviceProvider;
	}
}
