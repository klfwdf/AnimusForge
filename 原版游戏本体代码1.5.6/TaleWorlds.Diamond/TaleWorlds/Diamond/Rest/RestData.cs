using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
[DataContract]
public abstract class RestData
{
	[DataMember]
	public string TypeName { get; set; }

	protected RestData()
	{
		TypeName = GetType().FullName;
	}

	public string SerializeAsJson()
	{
		return JsonConvert.SerializeObject(this);
	}
}
