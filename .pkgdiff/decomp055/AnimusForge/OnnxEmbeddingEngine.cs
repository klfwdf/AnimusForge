using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;

namespace AnimusForge;

public sealed class OnnxEmbeddingEngine
{
	private sealed class BertTokenizerLite
	{
		private readonly Dictionary<string, int> _vocab;

		private readonly bool _lowerCase;

		private readonly int _padId;

		private readonly int _unkId;

		private readonly int _clsId;

		private readonly int _sepId;

		private BertTokenizerLite(Dictionary<string, int> vocab, bool lowerCase, int padId, int unkId, int clsId, int sepId)
		{
			_vocab = vocab ?? new Dictionary<string, int>(StringComparer.Ordinal);
			_lowerCase = lowerCase;
			_padId = padId;
			_unkId = unkId;
			_clsId = clsId;
			_sepId = sepId;
		}

		public static BertTokenizerLite Load(string tokenizerJsonPath)
		{
			try
			{
				JObject val = JObject.Parse(File.ReadAllText(tokenizerJsonPath, Encoding.UTF8));
				JToken obj = val["model"];
				JToken obj2 = ((obj != null) ? obj[(object)"vocab"] : null);
				JObject val2 = (JObject)(object)((obj2 is JObject) ? obj2 : null);
				if (val2 == null)
				{
					return null;
				}
				Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
				foreach (JProperty item in val2.Properties())
				{
					if (item != null)
					{
						string text = item.Name ?? "";
						if (!string.IsNullOrEmpty(text))
						{
							int value = Extensions.Value<int>((IEnumerable<JToken>)item.Value);
							dictionary[text] = value;
						}
					}
				}
				bool lowerCase = false;
				try
				{
					JToken val3 = val["normalizer"];
					if (val3 != null)
					{
						JToken obj3 = val3[(object)"type"];
						string a = (((obj3 != null) ? Extensions.Value<string>((IEnumerable<JToken>)obj3) : null) ?? "").Trim();
						if (string.Equals(a, "BertNormalizer", StringComparison.OrdinalIgnoreCase))
						{
							JToken obj4 = val3[(object)"lowercase"];
							lowerCase = obj4 != null && Extensions.Value<bool>((IEnumerable<JToken>)obj4);
						}
						else
						{
							JToken obj5 = val3[(object)"normalizers"];
							JArray val4 = (JArray)(object)((obj5 is JArray) ? obj5 : null);
							if (val4 != null)
							{
								foreach (JToken item2 in val4)
								{
									object obj6;
									if (item2 == null)
									{
										obj6 = null;
									}
									else
									{
										JToken obj7 = item2[(object)"type"];
										obj6 = ((obj7 != null) ? Extensions.Value<string>((IEnumerable<JToken>)obj7) : null);
									}
									if (obj6 == null)
									{
										obj6 = "";
									}
									string a2 = ((string)obj6).Trim();
									if (string.Equals(a2, "BertNormalizer", StringComparison.OrdinalIgnoreCase))
									{
										JToken obj8 = item2[(object)"lowercase"];
										lowerCase = obj8 != null && Extensions.Value<bool>((IEnumerable<JToken>)obj8);
										break;
									}
								}
							}
						}
					}
				}
				catch
				{
					lowerCase = false;
				}
				int padId = FindTokenId(val, dictionary, "[PAD]", 0);
				int unkId = FindTokenId(val, dictionary, "[UNK]", 100);
				int clsId = FindTokenId(val, dictionary, "[CLS]", 101);
				int sepId = FindTokenId(val, dictionary, "[SEP]", 102);
				return new BertTokenizerLite(dictionary, lowerCase, padId, unkId, clsId, sepId);
			}
			catch
			{
				return null;
			}
		}

		private static int FindTokenId(JObject jo, Dictionary<string, int> vocab, string token, int fallback)
		{
			try
			{
				if (vocab != null && vocab.TryGetValue(token, out var value))
				{
					return value;
				}
				JToken obj = ((jo != null) ? jo["added_tokens"] : null);
				JArray val = (JArray)(object)((obj is JArray) ? obj : null);
				if (val != null)
				{
					foreach (JToken item in val)
					{
						object obj2;
						if (item == null)
						{
							obj2 = null;
						}
						else
						{
							JToken obj3 = item[(object)"content"];
							obj2 = ((obj3 != null) ? Extensions.Value<string>((IEnumerable<JToken>)obj3) : null);
						}
						if (obj2 == null)
						{
							obj2 = "";
						}
						if (string.Equals((string)obj2, token, StringComparison.Ordinal))
						{
							JToken obj4 = item[(object)"id"];
							return (obj4 != null) ? Extensions.Value<int>((IEnumerable<JToken>)obj4) : fallback;
						}
					}
				}
			}
			catch
			{
			}
			return fallback;
		}

		public List<long> Encode(string text, int maxLength, out int[] attentionMask)
		{
			attentionMask = new int[0];
			try
			{
				if (maxLength < 8)
				{
					maxLength = 8;
				}
				string text2 = (text ?? "").Trim();
				if (_lowerCase)
				{
					text2 = text2.ToLowerInvariant();
				}
				List<string> list = BasicTokenize(text2);
				List<string> list2 = new List<string>();
				for (int i = 0; i < list.Count; i++)
				{
					List<string> list3 = WordPieceTokenize(list[i]);
					if (list3 != null && list3.Count > 0)
					{
						list2.AddRange(list3);
					}
				}
				int num = maxLength - 2;
				if (list2.Count > num)
				{
					list2 = list2.Take(num).ToList();
				}
				List<long> list4 = new List<long>(list2.Count + 2) { _clsId };
				for (int j = 0; j < list2.Count; j++)
				{
					string key = list2[j];
					if (_vocab.TryGetValue(key, out var value))
					{
						list4.Add(value);
					}
					else
					{
						list4.Add(_unkId);
					}
				}
				list4.Add(_sepId);
				attentionMask = new int[list4.Count];
				for (int k = 0; k < attentionMask.Length; k++)
				{
					attentionMask[k] = 1;
				}
				return list4;
			}
			catch
			{
				attentionMask = new int[2] { 1, 1 };
				return new List<long> { _clsId, _sepId };
			}
		}

		private List<string> BasicTokenize(string text)
		{
			List<string> list = new List<string>();
			try
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					return list;
				}
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < text.Length; i++)
				{
					char c = text[i];
					if (char.IsWhiteSpace(c))
					{
						FlushWord(stringBuilder, list);
					}
					else if (IsCjk(c))
					{
						FlushWord(stringBuilder, list);
						list.Add(c.ToString());
					}
					else if (char.IsPunctuation(c))
					{
						FlushWord(stringBuilder, list);
						list.Add(c.ToString());
					}
					else
					{
						stringBuilder.Append(c);
					}
				}
				FlushWord(stringBuilder, list);
			}
			catch
			{
			}
			return list;
		}

		private static void FlushWord(StringBuilder sb, List<string> outList)
		{
			if (sb != null && sb.Length > 0 && outList != null)
			{
				string text = sb.ToString();
				sb.Clear();
				if (!string.IsNullOrWhiteSpace(text))
				{
					outList.Add(text);
				}
			}
		}

		private static bool IsCjk(char ch)
		{
			return (ch >= '一' && ch <= '鿿') || (ch >= '㐀' && ch <= '䶿');
		}

		private List<string> WordPieceTokenize(string token)
		{
			List<string> list = new List<string>();
			try
			{
				string text = (token ?? "").Trim();
				if (string.IsNullOrEmpty(text))
				{
					return list;
				}
				if (_vocab.ContainsKey(text))
				{
					list.Add(text);
					return list;
				}
				if (text.Length > 100)
				{
					list.Add("[UNK]");
					return list;
				}
				int num = 0;
				while (num < text.Length)
				{
					int num2 = text.Length;
					string text2 = null;
					while (num2 > num)
					{
						string text3 = text.Substring(num, num2 - num);
						if (num > 0)
						{
							text3 = "##" + text3;
						}
						if (_vocab.ContainsKey(text3))
						{
							text2 = text3;
							break;
						}
						num2--;
					}
					if (text2 == null)
					{
						list.Clear();
						list.Add("[UNK]");
						return list;
					}
					list.Add(text2);
					num = num2;
				}
			}
			catch
			{
				list.Clear();
				list.Add("[UNK]");
			}
			return list;
		}
	}

	private static readonly Lazy<OnnxEmbeddingEngine> _lazy = new Lazy<OnnxEmbeddingEngine>(() => new OnnxEmbeddingEngine());

	private readonly object _initLock = new object();

	private bool _initialized;

	private bool _available;

	private string _lastError = "";

	private InferenceSession _session;

	private BertTokenizerLite _tokenizer;

	private string _inputIdsName = "input_ids";

	private string _inputMaskName = "attention_mask";

	private string _inputTypeName = "token_type_ids";

	private string _outputName = "";

	private bool _useInt64Input = true;

	private int _maxLength = 512;

	private readonly object _cacheLock = new object();

	private readonly Dictionary<string, float[]> _queryCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

	private const int QueryCacheMax = 256;

	private long _obsCounter = 0L;

	public static OnnxEmbeddingEngine Instance => _lazy.Value;

	public bool IsAvailable
	{
		get
		{
			EnsureInitialized();
			return _available;
		}
	}

	public string LastError
	{
		get
		{
			EnsureInitialized();
			return _lastError ?? "";
		}
	}

	private OnnxEmbeddingEngine()
	{
	}

	private static string FindFirstFile(params string[] files)
	{
		try
		{
			foreach (string text in files)
			{
				if (!string.IsNullOrEmpty(text) && File.Exists(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool TryReadMaxPosition(string configPath, out int maxLen)
	{
		maxLen = 512;
		try
		{
			if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
			{
				return false;
			}
			JObject val = JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8));
			JToken val2 = val["max_position_embeddings"];
			if (val2 == null)
			{
				return false;
			}
			int num = Extensions.Value<int>((IEnumerable<JToken>)val2);
			if (num > 8 && num <= 8192)
			{
				maxLen = num;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string GuessInputName(IReadOnlyDictionary<string, NodeMetadata> meta, string prefer, string contains)
	{
		try
		{
			if (meta == null || meta.Count <= 0)
			{
				return "";
			}
			if (!string.IsNullOrEmpty(prefer) && meta.ContainsKey(prefer))
			{
				return prefer;
			}
			string text = meta.Keys.FirstOrDefault((string k) => k != null && k.IndexOf(contains ?? "", StringComparison.OrdinalIgnoreCase) >= 0);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return meta.Keys.FirstOrDefault() ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static string GuessOutputName(IReadOnlyDictionary<string, NodeMetadata> meta)
	{
		try
		{
			if (meta == null || meta.Count <= 0)
			{
				return "";
			}
			string text = meta.Keys.FirstOrDefault((string k) => k != null && k.IndexOf("last_hidden_state", StringComparison.OrdinalIgnoreCase) >= 0);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			text = meta.Keys.FirstOrDefault((string k) => k != null && k.IndexOf("sentence_embedding", StringComparison.OrdinalIgnoreCase) >= 0);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return meta.Keys.FirstOrDefault() ?? "";
		}
		catch
		{
			return "";
		}
	}

	private void EnsureInitialized()
	{
		if (_initialized)
		{
			return;
		}
		lock (_initLock)
		{
			if (_initialized)
			{
				return;
			}
			_initialized = true;
			try
			{
				string basePath = Utilities.GetBasePath();
				string text = Path.Combine(basePath, "Modules", "AnimusForge");
				string text2 = Path.Combine(basePath, "Modules", "AnimusForge");
				string text3 = (Directory.Exists(text) ? text : text2);
				if (!Directory.Exists(text3))
				{
					_available = false;
					_lastError = "模块目录不存在。";
					return;
				}
				string text4 = Path.Combine(text3, "ONNX");
				if (!Directory.Exists(text4))
				{
					_available = false;
					_lastError = "ONNX 目录不存在。";
					return;
				}
				string path = Path.Combine(text4, "onnx");
				string text5 = FindFirstFile(Path.Combine(path, "model_quantized.onnx"), Path.Combine(text4, "model_quantized.onnx"), Path.Combine(path, "model.onnx"), Path.Combine(text4, "model.onnx"));
				if (string.IsNullOrEmpty(text5))
				{
					_available = false;
					_lastError = "未找到 model.onnx 或 model_quantized.onnx。";
					return;
				}
				string text6 = FindFirstFile(Path.Combine(text4, "tokenizer.json"), Path.Combine(path, "tokenizer.json"));
				if (string.IsNullOrEmpty(text6))
				{
					_available = false;
					_lastError = "未找到 tokenizer.json。";
					return;
				}
				string text7 = FindFirstFile(Path.Combine(text4, "config.json"), Path.Combine(path, "config.json"));
				if (!string.IsNullOrEmpty(text7))
				{
					TryReadMaxPosition(text7, out _maxLength);
				}
				_tokenizer = BertTokenizerLite.Load(text6);
				if (_tokenizer == null)
				{
					_available = false;
					_lastError = "tokenizer 解析失败。";
					return;
				}
				SessionOptions sessionOptions = new SessionOptions();
				sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;
				_session = new InferenceSession(text5, sessionOptions);
				_inputIdsName = GuessInputName(_session.InputMetadata, "input_ids", "input_ids");
				_inputMaskName = GuessInputName(_session.InputMetadata, "attention_mask", "attention_mask");
				_inputTypeName = GuessInputName(_session.InputMetadata, "token_type_ids", "token_type");
				_outputName = GuessOutputName(_session.OutputMetadata);
				_useInt64Input = true;
				try
				{
					if (_session.InputMetadata.TryGetValue(_inputIdsName, out var value))
					{
						_useInt64Input = value != null && value.ElementType == typeof(long);
					}
				}
				catch
				{
					_useInt64Input = true;
				}
				_available = true;
				_lastError = "";
				try
				{
					Logger.Log("OnnxEmbedding", string.Format("初始化成功 model={0} tokenizer={1} inputType={2} maxLen={3}", Path.GetFileName(text5), Path.GetFileName(text6), _useInt64Input ? "int64" : "int32", _maxLength));
				}
				catch
				{
				}
			}
			catch (Exception ex)
			{
				_available = false;
				_lastError = ex.Message ?? "初始化异常";
				try
				{
					Logger.Log("OnnxEmbedding", "初始化失败: " + _lastError);
				}
				catch
				{
				}
			}
		}
	}

	public bool TryGetEmbedding(string text, out float[] vector)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		vector = null;
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			return false;
		}
		EnsureInitialized();
		if (!_available || _session == null || _tokenizer == null)
		{
			return false;
		}
		try
		{
			lock (_cacheLock)
			{
				if (_queryCache.TryGetValue(text2, out var value) && value != null && value.Length != 0)
				{
					vector = value;
					stopwatch.Stop();
					Logger.Metric("onnx.embedding", ok: true, stopwatch.Elapsed.TotalMilliseconds);
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			int num = ((_maxLength > 8) ? _maxLength : 512);
			if (num > 512)
			{
				num = 512;
			}
			int[] attentionMask;
			List<long> list = _tokenizer.Encode(text2, num, out attentionMask);
			if (list == null || list.Count <= 0 || attentionMask == null || attentionMask.Length != list.Count)
			{
				return false;
			}
			List<NamedOnnxValue> list2 = new List<NamedOnnxValue>();
			int count = list.Count;
			if (_useInt64Input)
			{
				DenseTensor<long> denseTensor = new DenseTensor<long>(new int[2] { 1, count });
				DenseTensor<long> denseTensor2 = new DenseTensor<long>(new int[2] { 1, count });
				DenseTensor<long> denseTensor3 = new DenseTensor<long>(new int[2] { 1, count });
				for (int i = 0; i < count; i++)
				{
					denseTensor[new int[2] { 0, i }] = list[i];
					denseTensor2[new int[2] { 0, i }] = attentionMask[i];
					denseTensor3[new int[2] { 0, i }] = 0L;
				}
				if (!string.IsNullOrEmpty(_inputIdsName))
				{
					list2.Add(NamedOnnxValue.CreateFromTensor(_inputIdsName, denseTensor));
				}
				if (!string.IsNullOrEmpty(_inputMaskName) && !string.Equals(_inputMaskName, _inputIdsName, StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(NamedOnnxValue.CreateFromTensor(_inputMaskName, denseTensor2));
				}
				if (!string.IsNullOrEmpty(_inputTypeName) && !string.Equals(_inputTypeName, _inputIdsName, StringComparison.OrdinalIgnoreCase) && !string.Equals(_inputTypeName, _inputMaskName, StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(NamedOnnxValue.CreateFromTensor(_inputTypeName, denseTensor3));
				}
			}
			else
			{
				DenseTensor<int> denseTensor4 = new DenseTensor<int>(new int[2] { 1, count });
				DenseTensor<int> denseTensor5 = new DenseTensor<int>(new int[2] { 1, count });
				DenseTensor<int> denseTensor6 = new DenseTensor<int>(new int[2] { 1, count });
				for (int j = 0; j < count; j++)
				{
					denseTensor4[new int[2] { 0, j }] = (int)list[j];
					denseTensor5[new int[2] { 0, j }] = attentionMask[j];
					denseTensor6[new int[2] { 0, j }] = 0;
				}
				if (!string.IsNullOrEmpty(_inputIdsName))
				{
					list2.Add(NamedOnnxValue.CreateFromTensor(_inputIdsName, denseTensor4));
				}
				if (!string.IsNullOrEmpty(_inputMaskName) && !string.Equals(_inputMaskName, _inputIdsName, StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(NamedOnnxValue.CreateFromTensor(_inputMaskName, denseTensor5));
				}
				if (!string.IsNullOrEmpty(_inputTypeName) && !string.Equals(_inputTypeName, _inputIdsName, StringComparison.OrdinalIgnoreCase) && !string.Equals(_inputTypeName, _inputMaskName, StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(NamedOnnxValue.CreateFromTensor(_inputTypeName, denseTensor6));
				}
			}
			float[] array = null;
			using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> source = _session.Run(list2))
			{
				DisposableNamedOnnxValue disposableNamedOnnxValue = source.FirstOrDefault((DisposableNamedOnnxValue x) => string.Equals(x.Name ?? "", _outputName ?? "", StringComparison.OrdinalIgnoreCase)) ?? source.FirstOrDefault();
				if (disposableNamedOnnxValue == null)
				{
					return false;
				}
				Tensor<float> tensor = disposableNamedOnnxValue.AsTensor<float>();
				ReadOnlySpan<int> dimensions = tensor.Dimensions;
				if (dimensions == null || dimensions.Length <= 0)
				{
					return false;
				}
				if (dimensions.Length == 2)
				{
					int num2 = dimensions[1];
					array = new float[num2];
					for (int num3 = 0; num3 < num2; num3++)
					{
						array[num3] = tensor[new int[2] { 0, num3 }];
					}
				}
				else if (dimensions.Length >= 3)
				{
					int num4 = dimensions[1];
					int num5 = dimensions[2];
					if (num4 <= 0 || num5 <= 0)
					{
						return false;
					}
					array = new float[num5];
					int num6 = 0;
					int num7 = num4;
					if (num7 > attentionMask.Length)
					{
						num7 = attentionMask.Length;
					}
					if (num7 > count)
					{
						num7 = count;
					}
					bool flag = num7 >= 3;
					for (int num8 = 0; num8 < num7; num8++)
					{
						if (attentionMask[num8] > 0 && (!flag || (num8 != 0 && num8 != num7 - 1)))
						{
							num6++;
							for (int num9 = 0; num9 < num5; num9++)
							{
								array[num9] += tensor[new int[3] { 0, num8, num9 }];
							}
						}
					}
					if (num6 <= 0)
					{
						for (int num10 = 0; num10 < num7; num10++)
						{
							if (attentionMask[num10] > 0)
							{
								num6++;
								for (int num11 = 0; num11 < num5; num11++)
								{
									array[num11] += tensor[new int[3] { 0, num10, num11 }];
								}
							}
						}
					}
					if (num6 <= 0)
					{
						return false;
					}
					float num12 = 1f / (float)num6;
					for (int num13 = 0; num13 < num5; num13++)
					{
						array[num13] *= num12;
					}
				}
			}
			if (array == null || array.Length == 0)
			{
				return false;
			}
			NormalizeInPlace(array);
			vector = array;
			try
			{
				int count2;
				lock (_cacheLock)
				{
					if (_queryCache.Count >= 256)
					{
						_queryCache.Clear();
					}
					_queryCache[text2] = array;
					count2 = _queryCache.Count;
				}
				long num14 = Interlocked.Increment(ref _obsCounter);
				if (num14 % 25 == 1)
				{
					stopwatch.Stop();
					Logger.Obs("OnnxEmbedding", "sample", new Dictionary<string, object>
					{
						["ok"] = true,
						["textLen"] = text2.Length,
						["dim"] = array.Length,
						["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
						["cacheSize"] = count2
					});
					Logger.Metric("onnx.embedding", ok: true, stopwatch.Elapsed.TotalMilliseconds);
					return true;
				}
			}
			catch
			{
			}
			stopwatch.Stop();
			Logger.Metric("onnx.embedding", ok: true, stopwatch.Elapsed.TotalMilliseconds);
			return true;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			try
			{
				Logger.Log("OnnxEmbedding", "推理失败: " + ex.Message);
			}
			catch
			{
			}
			Logger.Obs("OnnxEmbedding", "error", new Dictionary<string, object>
			{
				["ok"] = false,
				["textLen"] = text2.Length,
				["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("onnx.embedding", ok: false, stopwatch.Elapsed.TotalMilliseconds);
			return false;
		}
	}

	private static void NormalizeInPlace(float[] v)
	{
		if (v == null || v.Length == 0)
		{
			return;
		}
		double num = 0.0;
		for (int i = 0; i < v.Length; i++)
		{
			num += (double)v[i] * (double)v[i];
		}
		if (!(num <= 1E-18))
		{
			float num2 = (float)(1.0 / Math.Sqrt(num));
			for (int j = 0; j < v.Length; j++)
			{
				v[j] *= num2;
			}
		}
	}
}
