using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TaleWorlds.SaveSystem;

public class MetaData
{
	[JsonProperty("List")]
	private Dictionary<string, string> _list = new Dictionary<string, string>();

	[JsonIgnore]
	public int Count => _list.Count;

	public string this[string key]
	{
		get
		{
			if (!_list.ContainsKey(key))
			{
				return null;
			}
			return _list[key];
		}
		set
		{
			_list[key] = value;
		}
	}

	[JsonIgnore]
	public Dictionary<string, string>.KeyCollection Keys => _list.Keys;

	public void Add(string key, string value)
	{
		_list.Add(key, value);
	}

	public bool TryGetValue(string key, out string value)
	{
		return _list.TryGetValue(key, out value);
	}

	public void Serialize(Stream stream)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
		byte[] bytes2 = BitConverter.GetBytes(bytes.Length);
		stream.Write(bytes2, 0, 4);
		stream.Write(bytes, 0, bytes.Length);
	}

	public static MetaData Deserialize(Stream stream)
	{
		try
		{
			byte[] array = new byte[4];
			stream.Read(array, 0, 4);
			int num = BitConverter.ToInt32(array, 0);
			byte[] array2 = new byte[num];
			stream.Read(array2, 0, num);
			return JsonConvert.DeserializeObject<MetaData>(Encoding.UTF8.GetString(array2));
		}
		catch
		{
			return null;
		}
	}
}
