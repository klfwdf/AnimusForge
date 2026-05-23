using System;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BUTR.DependencyInjection.ButterLib
{
	// Token: 0x02000040 RID: 64
	[NullableContext(1)]
	[Nullable(0)]
	internal class ButterLibServiceContainer : IGenericServiceContainer
	{
		// Token: 0x170000AC RID: 172
		// (get) Token: 0x06000206 RID: 518 RVA: 0x000084ED File Offset: 0x000066ED
		[Nullable(2)]
		private static IServiceCollection ServiceContainer
		{
			[NullableContext(2)]
			get
			{
				return DependencyInjectionExtensions.GetServices(null);
			}
		}

		// Token: 0x06000207 RID: 519 RVA: 0x000084F5 File Offset: 0x000066F5
		public IGenericServiceContainer RegisterSingleton<TService>() where TService : class
		{
			ServiceCollectionServiceExtensions.AddSingleton<TService>(ButterLibServiceContainer.ServiceContainer);
			return this;
		}

		// Token: 0x06000208 RID: 520 RVA: 0x00008504 File Offset: 0x00006704
		public IGenericServiceContainer RegisterSingleton<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
		{
			ServiceCollectionServiceExtensions.AddSingleton<TService>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
			return this;
		}

		// Token: 0x06000209 RID: 521 RVA: 0x00008536 File Offset: 0x00006736
		public IGenericServiceContainer RegisterSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
		{
			ServiceCollectionServiceExtensions.AddSingleton<TService, TImplementation>(ButterLibServiceContainer.ServiceContainer);
			return this;
		}

		// Token: 0x0600020A RID: 522 RVA: 0x00008544 File Offset: 0x00006744
		public IGenericServiceContainer RegisterSingleton<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
		{
			ServiceCollectionServiceExtensions.AddSingleton<TService, TImplementation>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
			return this;
		}

		// Token: 0x0600020B RID: 523 RVA: 0x00008576 File Offset: 0x00006776
		public IGenericServiceContainer RegisterSingleton(Type serviceType, Type implementationType)
		{
			ServiceCollectionServiceExtensions.AddSingleton(ButterLibServiceContainer.ServiceContainer, serviceType, implementationType);
			return this;
		}

		// Token: 0x0600020C RID: 524 RVA: 0x00008588 File Offset: 0x00006788
		public IGenericServiceContainer RegisterSingleton(Type serviceType, Func<object> factory)
		{
			ServiceCollectionServiceExtensions.AddSingleton<object>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider _) => factory());
			return this;
		}

		// Token: 0x0600020D RID: 525 RVA: 0x000085BA File Offset: 0x000067BA
		public IGenericServiceContainer RegisterScoped<TService>() where TService : class
		{
			ServiceCollectionServiceExtensions.AddScoped<TService>(ButterLibServiceContainer.ServiceContainer);
			return this;
		}

		// Token: 0x0600020E RID: 526 RVA: 0x000085C8 File Offset: 0x000067C8
		public IGenericServiceContainer RegisterScoped<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
		{
			ServiceCollectionServiceExtensions.AddScoped<TService>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
			return this;
		}

		// Token: 0x0600020F RID: 527 RVA: 0x000085FA File Offset: 0x000067FA
		public IGenericServiceContainer RegisterScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
		{
			ServiceCollectionServiceExtensions.AddScoped<TService, TImplementation>(ButterLibServiceContainer.ServiceContainer);
			return this;
		}

		// Token: 0x06000210 RID: 528 RVA: 0x00008608 File Offset: 0x00006808
		public IGenericServiceContainer RegisterScoped<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
		{
			ServiceCollectionServiceExtensions.AddScoped<TService, TImplementation>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
			return this;
		}

		// Token: 0x06000211 RID: 529 RVA: 0x0000863A File Offset: 0x0000683A
		public IGenericServiceContainer RegisterScoped(Type serviceType, Type implementationType)
		{
			ServiceCollectionServiceExtensions.AddScoped(ButterLibServiceContainer.ServiceContainer, serviceType, implementationType);
			return this;
		}

		// Token: 0x06000212 RID: 530 RVA: 0x0000864C File Offset: 0x0000684C
		public IGenericServiceContainer RegisterScoped(Type serviceType, Func<object> factory)
		{
			ServiceCollectionServiceExtensions.AddScoped<object>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider _) => factory());
			return this;
		}

		// Token: 0x06000213 RID: 531 RVA: 0x0000867E File Offset: 0x0000687E
		public IGenericServiceContainer RegisterTransient<TService>() where TService : class
		{
			ServiceCollectionServiceExtensions.AddTransient<TService>(ButterLibServiceContainer.ServiceContainer);
			return this;
		}

		// Token: 0x06000214 RID: 532 RVA: 0x0000868C File Offset: 0x0000688C
		public IGenericServiceContainer RegisterTransient<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
		{
			ServiceCollectionServiceExtensions.AddTransient<TService>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
			return this;
		}

		// Token: 0x06000215 RID: 533 RVA: 0x000086BE File Offset: 0x000068BE
		public IGenericServiceContainer RegisterTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
		{
			ServiceCollectionServiceExtensions.AddTransient<TService, TImplementation>(ButterLibServiceContainer.ServiceContainer);
			return this;
		}

		// Token: 0x06000216 RID: 534 RVA: 0x000086CC File Offset: 0x000068CC
		public IGenericServiceContainer RegisterTransient<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
		{
			ServiceCollectionServiceExtensions.AddTransient<TService, TImplementation>(ButterLibServiceContainer.ServiceContainer, (IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
			return this;
		}

		// Token: 0x06000217 RID: 535 RVA: 0x000086FE File Offset: 0x000068FE
		public IGenericServiceContainer RegisterTransient(Type serviceType, Type implementationType)
		{
			ServiceCollectionServiceExtensions.AddTransient(ButterLibServiceContainer.ServiceContainer, serviceType, implementationType);
			return this;
		}

		// Token: 0x06000218 RID: 536 RVA: 0x00008710 File Offset: 0x00006910
		public IGenericServiceContainer RegisterTransient(Type serviceType, Func<object> factory)
		{
			ServiceCollectionServiceExtensions.AddTransient(ButterLibServiceContainer.ServiceContainer, serviceType, (IServiceProvider _) => factory());
			return this;
		}

		// Token: 0x06000219 RID: 537 RVA: 0x00008743 File Offset: 0x00006943
		public IGenericServiceProvider Build()
		{
			return new ButterLibGenericServiceProvider();
		}
	}
}
