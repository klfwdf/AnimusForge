using System;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BUTR.DependencyInjection.ButterLib
{
	// Token: 0x0200003E RID: 62
	internal class ButterLibGenericServiceProvider : IGenericServiceProvider, IDisposable
	{
		// Token: 0x170000AB RID: 171
		// (get) Token: 0x060001FE RID: 510 RVA: 0x0000846B File Offset: 0x0000666B
		[Nullable(2)]
		private static IServiceProvider ServiceProvider
		{
			[NullableContext(2)]
			get
			{
				return DependencyInjectionExtensions.GetTempServiceProvider(null) ?? DependencyInjectionExtensions.GetServiceProvider(null);
			}
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0000847D File Offset: 0x0000667D
		[NullableContext(1)]
		public IGenericServiceProviderScope CreateScope()
		{
			return new ButterLibGenericServiceProviderScope(ServiceProviderServiceExtensions.CreateScope(ButterLibGenericServiceProvider.ServiceProvider));
		}

		// Token: 0x06000200 RID: 512 RVA: 0x00008490 File Offset: 0x00006690
		[NullableContext(1)]
		[return: Nullable(2)]
		public TService GetService<TService>() where TService : class
		{
			IServiceProvider serviceProvider = ButterLibGenericServiceProvider.ServiceProvider;
			if (serviceProvider == null)
			{
				return default(TService);
			}
			return ServiceProviderServiceExtensions.GetRequiredService<TService>(serviceProvider);
		}

		// Token: 0x06000201 RID: 513 RVA: 0x000084B5 File Offset: 0x000066B5
		public void Dispose()
		{
		}
	}
}
