using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;
using TaleWorlds.ServiceDiscovery.Client;

namespace TaleWorlds.Diamond.ClientApplication
{
	// Token: 0x02000044 RID: 68
	public class DiamondClientApplication
	{
		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000195 RID: 405 RVA: 0x000051E4 File Offset: 0x000033E4
		// (set) Token: 0x06000196 RID: 406 RVA: 0x000051EC File Offset: 0x000033EC
		public ApplicationVersion ApplicationVersion { get; private set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000197 RID: 407 RVA: 0x000051F5 File Offset: 0x000033F5
		public ParameterContainer Parameters
		{
			get
			{
				return this._parameters;
			}
		}

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x06000198 RID: 408 RVA: 0x000051FD File Offset: 0x000033FD
		// (set) Token: 0x06000199 RID: 409 RVA: 0x00005205 File Offset: 0x00003405
		public IReadOnlyDictionary<string, string> ProxyAddressMap { get; private set; }

		// Token: 0x0600019A RID: 410 RVA: 0x00005210 File Offset: 0x00003410
		public DiamondClientApplication(ApplicationVersion applicationVersion, ParameterContainer parameters)
		{
			this.ApplicationVersion = applicationVersion;
			this._parameters = parameters;
			this._clientApplicationObjects = new Dictionary<string, DiamondClientApplicationObject>();
			this._clientObjects = new Dictionary<string, IClient>();
			this.ProxyAddressMap = new Dictionary<string, string>();
			ServicePointManager.DefaultConnectionLimit = 1000;
			ServicePointManager.Expect100Continue = false;
		}

		// Token: 0x0600019B RID: 411 RVA: 0x00005262 File Offset: 0x00003462
		public DiamondClientApplication(ApplicationVersion applicationVersion)
			: this(applicationVersion, new ParameterContainer())
		{
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00005270 File Offset: 0x00003470
		public object GetObject(string name)
		{
			DiamondClientApplicationObject result;
			this._clientApplicationObjects.TryGetValue(name, out result);
			return result;
		}

		// Token: 0x0600019D RID: 413 RVA: 0x0000528D File Offset: 0x0000348D
		public void AddObject(string name, DiamondClientApplicationObject applicationObject)
		{
			this._clientApplicationObjects.Add(name, applicationObject);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x0000529C File Offset: 0x0000349C
		public void Initialize(ClientApplicationConfiguration applicationConfiguration)
		{
			this._parameters = applicationConfiguration.Parameters;
			foreach (string clientConfiguration in applicationConfiguration.Clients)
			{
				this.CreateClient(clientConfiguration, applicationConfiguration.SessionProviderType);
			}
		}

		// Token: 0x0600019F RID: 415 RVA: 0x000052DC File Offset: 0x000034DC
		private void CreateClient(string clientConfiguration, SessionProviderType sessionProviderType)
		{
			Type type = DiamondClientApplication.FindType(clientConfiguration);
			object obj = this.CreateClientSessionProvider(clientConfiguration, type, sessionProviderType, this._parameters);
			IClient value = (IClient)Activator.CreateInstance(type, new object[] { this, obj });
			this._clientObjects.Add(clientConfiguration, value);
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00005328 File Offset: 0x00003528
		public object CreateClientSessionProvider(string clientName, Type clientType, SessionProviderType sessionProviderType, ParameterContainer parameters)
		{
			if (sessionProviderType == SessionProviderType.ThreadedRest)
			{
				Type type = ((sessionProviderType == SessionProviderType.Rest) ? typeof(GenericRestSessionProvider<>) : typeof(GenericThreadedRestSessionProvider<>)).MakeGenericType(new Type[] { clientType });
				string text;
				parameters.TryGetParameter(clientName + ".Address", out text);
				if (ServiceAddress.IsServiceAddress(text))
				{
					string serviceDiscoveryAddress;
					parameters.TryGetParameter(clientName + ".ServiceDiscovery.Address", out serviceDiscoveryAddress);
					ServiceAddressManager.ResolveAddress(serviceDiscoveryAddress, ref text);
				}
				string text2 = clientName + ".Proxy.";
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				foreach (KeyValuePair<string, string> keyValuePair in parameters.Iterator)
				{
					if (keyValuePair.Key.StartsWith(text2) && keyValuePair.Key.Length > text2.Length)
					{
						dictionary[keyValuePair.Key.Substring(text2.Length)] = keyValuePair.Value;
					}
				}
				this.ProxyAddressMap = dictionary;
				string text3;
				if (dictionary.TryGetValue(text, out text3))
				{
					text = text3;
				}
				string name;
				IHttpDriver httpDriver;
				if (parameters.TryGetParameter(clientName + ".HttpDriver", out name))
				{
					httpDriver = HttpDriverManager.GetHttpDriver(name);
				}
				else
				{
					httpDriver = HttpDriverManager.GetDefaultHttpDriver();
				}
				return Activator.CreateInstance(type, new object[] { text, httpDriver });
			}
			throw new NotImplementedException("Other session provider types are not supported yet.");
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x000054A8 File Offset: 0x000036A8
		private static Assembly[] GetDiamondAssemblies()
		{
			List<Assembly> list = new List<Assembly>();
			Assembly assembly = typeof(PeerId).Assembly;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			list.Add(assembly);
			foreach (Assembly assembly2 in assemblies)
			{
				AssemblyName[] referencedAssemblies = assembly2.GetReferencedAssemblies();
				for (int j = 0; j < referencedAssemblies.Length; j++)
				{
					if (referencedAssemblies[j].ToString() == assembly.GetName().ToString())
					{
						list.Add(assembly2);
						break;
					}
				}
			}
			return list.ToArray();
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00005538 File Offset: 0x00003738
		private static Type FindType(string name)
		{
			Assembly[] diamondAssemblies = DiamondClientApplication.GetDiamondAssemblies();
			Type result = null;
			Assembly[] array = diamondAssemblies;
			for (int i = 0; i < array.Length; i++)
			{
				foreach (Type type in array[i].GetTypesSafe(null))
				{
					if (type.Name == name)
					{
						result = type;
					}
				}
			}
			return result;
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x000055B4 File Offset: 0x000037B4
		public T GetClient<T>(string name) where T : class, IClient
		{
			IClient client;
			if (this._clientObjects.TryGetValue(name, out client))
			{
				return client as T;
			}
			return default(T);
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x000055E8 File Offset: 0x000037E8
		public void Update()
		{
			foreach (IClient client in this._clientObjects.Values)
			{
			}
		}

		// Token: 0x04000092 RID: 146
		private ParameterContainer _parameters;

		// Token: 0x04000093 RID: 147
		private Dictionary<string, DiamondClientApplicationObject> _clientApplicationObjects;

		// Token: 0x04000094 RID: 148
		private Dictionary<string, IClient> _clientObjects;
	}
}
