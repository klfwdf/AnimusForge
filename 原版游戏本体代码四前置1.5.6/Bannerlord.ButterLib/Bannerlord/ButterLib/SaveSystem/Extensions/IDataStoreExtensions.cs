using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace Bannerlord.ButterLib.SaveSystem.Extensions;

public static class IDataStoreExtensions
{
	private record JsonData(int Format, string Data);

	private static IEnumerable<string> ToChunks(string str, int maxChunkSize)
	{
		for (int i = 0; i < str.Length; i += maxChunkSize)
		{
			yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
		}
	}

	private static string ChunksToString(IReadOnlyList<string> chunks)
	{
		if (chunks.Count == 0)
		{
			return string.Empty;
		}
		if (chunks.Count == 1)
		{
			return chunks[0];
		}
		StringBuilder stringBuilder = new StringBuilder(32767);
		foreach (string chunk in chunks)
		{
			stringBuilder.Append(chunk);
		}
		return stringBuilder.ToString();
	}

	private static string Serialize<T>(ref T? data, JsonSerializer serializer)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		using StringWriter textWriter = new StringWriter(stringBuilder, CultureInfo.InvariantCulture);
		using JsonTextWriter jsonTextWriter = new JsonTextWriter(textWriter);
		jsonTextWriter.Formatting = Formatting.None;
		serializer.Serialize(jsonTextWriter, data, typeof(T));
		return stringBuilder.ToString();
	}

	private static T? Deserialize<T>(string json, JsonSerializer serializer)
	{
		using JsonTextReader reader = new JsonTextReader(new StringReader(json));
		return serializer.Deserialize<T>(reader);
	}

	public static bool SyncDataAsJson<T>(this IDataStore dataStore, string key, ref T? data, JsonSerializerSettings? settings = null)
	{
		if (settings == null)
		{
			settings = new JsonSerializerSettings
			{
				ContractResolver = new TaleWorldsContractResolver(),
				Converters = 
				{
					(JsonConverter)new DictionaryToArrayConverter(),
					(JsonConverter)new MBGUIDConverter(),
					(JsonConverter)new MBObjectBaseConverter()
				},
				TypeNameHandling = TypeNameHandling.Auto
			};
		}
		return dataStore.SyncDataAsJson(key, ref data, JsonSerializer.Create(settings));
	}

	public static bool SyncDataAsJson<T>(this IDataStore dataStore, string key, ref T? data, JsonSerializer serializer)
	{
		if (typeof(T) == typeof(string) || typeof(T) == typeof(string[]))
		{
			return dataStore.SyncData(key, ref data);
		}
		if (dataStore.IsSaving)
		{
			string data2 = Serialize(ref data, serializer);
			string[] data3 = ToChunks(JsonConvert.SerializeObject(new JsonData(2, data2)), 31743).ToArray();
			return dataStore.SyncData(key, ref data3);
		}
		if (dataStore.IsLoading)
		{
			try
			{
				string[] data4 = Array.Empty<string>();
				if (dataStore.SyncData(key, ref data4))
				{
					var (num2, json) = JsonConvert.DeserializeObject<JsonData>(ChunksToString(data4 ?? Array.Empty<string>())) ?? new JsonData(-1, string.Empty);
					T val = ((num2 != 2) ? data : Deserialize<T>(json, serializer));
					data = val;
					return true;
				}
			}
			catch (Exception ex) when (ex is InvalidCastException)
			{
			}
			try
			{
				string data5 = "";
				if (dataStore.SyncData(key, ref data5))
				{
					data = Deserialize<T>(data5, serializer);
					return true;
				}
			}
			catch (Exception ex2) when (ex2 is InvalidCastException)
			{
			}
			try
			{
				return dataStore.SyncData(key, ref data);
			}
			catch (Exception ex3) when (ex3 is InvalidCastException)
			{
			}
		}
		return false;
	}
}
