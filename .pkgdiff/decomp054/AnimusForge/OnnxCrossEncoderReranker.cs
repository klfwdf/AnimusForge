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

public sealed class OnnxCrossEncoderReranker
{
	private sealed class SentencePieceUnigramTokenizerLite
	{
		private sealed class PieceEntry
		{
			public string Text;

			public int Id;

			public float Score;
		}

		private readonly Dictionary<string, List<PieceEntry>> _pairBuckets;

		private readonly Dictionary<char, List<PieceEntry>> _singleBuckets;

		private readonly bool _addPrefixSpace;

		private readonly int _padId;

		private readonly int _unkId;

		private readonly int _bosId;

		private readonly int _eosId;

		public int PadId => _padId;

		private SentencePieceUnigramTokenizerLite(Dictionary<string, List<PieceEntry>> pairBuckets, Dictionary<char, List<PieceEntry>> singleBuckets, bool addPrefixSpace, int padId, int unkId, int bosId, int eosId)
		{
			_pairBuckets = pairBuckets ?? new Dictionary<string, List<PieceEntry>>(StringComparer.Ordinal);
			_singleBuckets = singleBuckets ?? new Dictionary<char, List<PieceEntry>>();
			_addPrefixSpace = addPrefixSpace;
			_padId = padId;
			_unkId = unkId;
			_bosId = bosId;
			_eosId = eosId;
		}

		public static SentencePieceUnigramTokenizerLite Load(string tokenizerJsonPath)
		{
			try
			{
				JObject val = JObject.Parse(File.ReadAllText(tokenizerJsonPath, Encoding.UTF8));
				JToken obj = val["model"];
				JToken obj2 = ((obj != null) ? obj[(object)"vocab"] : null);
				JArray val2 = (JArray)(object)((obj2 is JArray) ? obj2 : null);
				if (val2 == null || ((JContainer)val2).Count <= 0)
				{
					return null;
				}
				JToken obj3 = val["pre_tokenizer"];
				bool? obj4;
				if (obj3 == null)
				{
					obj4 = null;
				}
				else
				{
					JToken obj5 = obj3[(object)"add_prefix_space"];
					obj4 = ((obj5 != null) ? new bool?(Extensions.Value<bool>((IEnumerable<JToken>)obj5)) : ((bool?)null));
				}
				bool addPrefixSpace = obj4 ?? true;
				Dictionary<string, List<PieceEntry>> dictionary = new Dictionary<string, List<PieceEntry>>(StringComparer.Ordinal);
				Dictionary<char, List<PieceEntry>> dictionary2 = new Dictionary<char, List<PieceEntry>>();
				int padId = FindSpecialTokenId(val, "<pad>", 1);
				int unkId = FindSpecialTokenId(val, "<unk>", 3);
				int bosId = FindSpecialTokenId(val, "<s>", 0);
				int eosId = FindSpecialTokenId(val, "</s>", 2);
				for (int i = 0; i < ((JContainer)val2).Count; i++)
				{
					JToken obj6 = val2[i];
					JArray val3 = (JArray)(object)((obj6 is JArray) ? obj6 : null);
					object obj7;
					if (val3 == null)
					{
						obj7 = null;
					}
					else
					{
						JToken obj8 = ((IEnumerable<JToken>)val3).FirstOrDefault();
						obj7 = ((obj8 != null) ? Extensions.Value<string>((IEnumerable<JToken>)obj8) : null);
					}
					if (obj7 == null)
					{
						obj7 = "";
					}
					string text = (string)obj7;
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					float num;
					if (val3 == null || ((JContainer)val3).Count <= 1)
					{
						num = -100f;
					}
					else
					{
						JToken obj9 = val3[1];
						num = ((obj9 != null) ? Extensions.Value<float>((IEnumerable<JToken>)obj9) : (-100f));
					}
					float score = num;
					PieceEntry item = new PieceEntry
					{
						Text = text,
						Id = i,
						Score = score
					};
					if (text.Length >= 2)
					{
						string key = text.Substring(0, 2);
						if (!dictionary.TryGetValue(key, out var value))
						{
							value = (dictionary[key] = new List<PieceEntry>());
						}
						value.Add(item);
					}
					else
					{
						char key2 = text[0];
						if (!dictionary2.TryGetValue(key2, out var value2))
						{
							value2 = (dictionary2[key2] = new List<PieceEntry>());
						}
						value2.Add(item);
					}
				}
				foreach (List<PieceEntry> value3 in dictionary.Values)
				{
					value3.Sort(delegate(PieceEntry a, PieceEntry b)
					{
						int num2 = b.Text.Length.CompareTo(a.Text.Length);
						if (num2 != 0)
						{
							return num2;
						}
						int num3 = b.Score.CompareTo(a.Score);
						return (num3 != 0) ? num3 : string.CompareOrdinal(a.Text, b.Text);
					});
				}
				foreach (List<PieceEntry> value4 in dictionary2.Values)
				{
					value4.Sort(delegate(PieceEntry a, PieceEntry b)
					{
						int num2 = b.Score.CompareTo(a.Score);
						return (num2 != 0) ? num2 : string.CompareOrdinal(a.Text, b.Text);
					});
				}
				return new SentencePieceUnigramTokenizerLite(dictionary, dictionary2, addPrefixSpace, padId, unkId, bosId, eosId);
			}
			catch
			{
				return null;
			}
		}

		private static int FindSpecialTokenId(JObject jo, string token, int fallback)
		{
			try
			{
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

		public List<long> EncodePair(string query, string document, int maxLength, out int[] attentionMask)
		{
			attentionMask = new int[0];
			try
			{
				if (maxLength < 8)
				{
					maxLength = 8;
				}
				List<int> list = Tokenize(query);
				List<int> list2 = Tokenize(document);
				int num = maxLength - 4;
				if (num < 2)
				{
					num = 2;
				}
				TruncatePair(list, list2, num);
				List<long> list3 = new List<long>(list.Count + list2.Count + 4) { _bosId };
				for (int i = 0; i < list.Count; i++)
				{
					list3.Add(list[i]);
				}
				list3.Add(_eosId);
				list3.Add(_eosId);
				for (int j = 0; j < list2.Count; j++)
				{
					list3.Add(list2[j]);
				}
				list3.Add(_eosId);
				attentionMask = Enumerable.Repeat(1, list3.Count).ToArray();
				return list3;
			}
			catch
			{
				attentionMask = new int[4] { 1, 1, 1, 1 };
				return new List<long> { _bosId, _eosId, _eosId, _eosId };
			}
		}

		private static void TruncatePair(List<int> queryIds, List<int> docIds, int maxTokenCount)
		{
			if (queryIds == null)
			{
				queryIds = new List<int>();
			}
			if (docIds == null)
			{
				docIds = new List<int>();
			}
			while (queryIds.Count + docIds.Count > maxTokenCount)
			{
				if (docIds.Count > Math.Max(24, queryIds.Count))
				{
					docIds.RemoveAt(docIds.Count - 1);
					continue;
				}
				if (queryIds.Count > 32)
				{
					queryIds.RemoveAt(queryIds.Count - 1);
					continue;
				}
				if (docIds.Count > 0)
				{
					docIds.RemoveAt(docIds.Count - 1);
					continue;
				}
				if (queryIds.Count > 0)
				{
					queryIds.RemoveAt(queryIds.Count - 1);
					continue;
				}
				break;
			}
		}

		private List<int> Tokenize(string text)
		{
			List<int> list = new List<int>();
			try
			{
				string text2 = NormalizeText(text);
				if (string.IsNullOrEmpty(text2))
				{
					return list;
				}
				string text3 = ApplyMetaspace(text2);
				int num = 0;
				while (num < text3.Length)
				{
					PieceEntry pieceEntry = FindGreedyMatch(text3, num);
					if (pieceEntry != null && !string.IsNullOrEmpty(pieceEntry.Text))
					{
						list.Add(pieceEntry.Id);
						num += pieceEntry.Text.Length;
					}
					else
					{
						list.Add(_unkId);
						num++;
					}
				}
			}
			catch
			{
				list.Clear();
				list.Add(_unkId);
			}
			return list;
		}

		private PieceEntry FindGreedyMatch(string text, int index)
		{
			try
			{
				if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
				{
					return null;
				}
				if (index + 1 < text.Length)
				{
					string key = text.Substring(index, 2);
					if (_pairBuckets.TryGetValue(key, out var value))
					{
						for (int i = 0; i < value.Count; i++)
						{
							PieceEntry pieceEntry = value[i];
							if (pieceEntry != null && pieceEntry.Text.Length <= text.Length - index && string.CompareOrdinal(text, index, pieceEntry.Text, 0, pieceEntry.Text.Length) == 0)
							{
								return pieceEntry;
							}
						}
					}
				}
				if (_singleBuckets.TryGetValue(text[index], out var value2) && value2.Count > 0)
				{
					return value2[0];
				}
			}
			catch
			{
			}
			return null;
		}

		private string NormalizeText(string text)
		{
			try
			{
				string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ");
				if (string.IsNullOrWhiteSpace(text2))
				{
					return "";
				}
				text2 = text2.Normalize(NormalizationForm.FormKC);
				StringBuilder stringBuilder = new StringBuilder(text2.Length);
				bool flag = false;
				foreach (char c in text2)
				{
					if (char.IsWhiteSpace(c))
					{
						if (!flag)
						{
							stringBuilder.Append(' ');
							flag = true;
						}
					}
					else
					{
						stringBuilder.Append(c);
						flag = false;
					}
				}
				return stringBuilder.ToString().Trim();
			}
			catch
			{
				return (text ?? "").Trim();
			}
		}

		private string ApplyMetaspace(string text)
		{
			try
			{
				if (string.IsNullOrEmpty(text))
				{
					return "";
				}
				StringBuilder stringBuilder = new StringBuilder(text.Length + 4);
				bool flag = true;
				if (_addPrefixSpace)
				{
					stringBuilder.Append('▁');
				}
				foreach (char c in text)
				{
					if (c == ' ')
					{
						if (!flag)
						{
							stringBuilder.Append('▁');
							flag = true;
						}
					}
					else
					{
						stringBuilder.Append(c);
						flag = false;
					}
				}
				return stringBuilder.ToString();
			}
			catch
			{
				return text ?? "";
			}
		}
	}

	private static readonly Lazy<OnnxCrossEncoderReranker> _lazy = new Lazy<OnnxCrossEncoderReranker>(() => new OnnxCrossEncoderReranker());

	private readonly object _initLock = new object();

	private readonly object _cacheLock = new object();

	private bool _initialized;

	private bool _available;

	private string _lastError = "";

	private InferenceSession _session;

	private SentencePieceUnigramTokenizerLite _tokenizer;

	private string _inputIdsName = "input_ids";

	private string _inputMaskName = "attention_mask";

	private string _inputTypeName = "token_type_ids";

	private string _outputName = "logits";

	private bool _useInt64Input = true;

	private int _maxLength = 512;

	private readonly Dictionary<string, float> _scoreCache = new Dictionary<string, float>(StringComparer.Ordinal);

	private long _obsCounter;

	public static OnnxCrossEncoderReranker Instance => _lazy.Value;

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

	private OnnxCrossEncoderReranker()
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
			string text = meta.Keys.FirstOrDefault((string k) => k != null && k.IndexOf("logits", StringComparison.OrdinalIgnoreCase) >= 0);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			text = meta.Keys.FirstOrDefault((string k) => k != null && k.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0);
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
				string text = Path.Combine(basePath, "Modules", "AnimusForge", "ONNX", "reranker");
				if (!Directory.Exists(text))
				{
					_available = false;
					_lastError = "reranker 目录不存在。";
					return;
				}
				string text2 = FindFirstFile(Path.Combine(text, "model_quantized.onnx"), Path.Combine(text, "model.onnx"));
				if (string.IsNullOrEmpty(text2))
				{
					_available = false;
					_lastError = "未找到 reranker model.onnx。";
					return;
				}
				string text3 = FindFirstFile(Path.Combine(text, "tokenizer.json"));
				if (string.IsNullOrEmpty(text3))
				{
					_available = false;
					_lastError = "未找到 reranker tokenizer.json。";
					return;
				}
				string text4 = FindFirstFile(Path.Combine(text, "config.json"));
				if (!string.IsNullOrEmpty(text4))
				{
					TryReadMaxPosition(text4, out _maxLength);
				}
				_tokenizer = SentencePieceUnigramTokenizerLite.Load(text3);
				if (_tokenizer == null)
				{
					_available = false;
					_lastError = "reranker tokenizer 解析失败。";
					return;
				}
				SessionOptions sessionOptions = new SessionOptions();
				sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;
				_session = new InferenceSession(text2, sessionOptions);
				_inputIdsName = GuessInputName(_session.InputMetadata, "input_ids", "input_ids");
				_inputMaskName = GuessInputName(_session.InputMetadata, "attention_mask", "attention_mask");
				_inputTypeName = GuessInputName(_session.InputMetadata, "token_type_ids", "token_type");
				_outputName = GuessOutputName(_session.OutputMetadata);
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
					Logger.Log("OnnxReranker", string.Format("初始化成功 model={0} tokenizer={1} inputType={2} maxLen={3}", Path.GetFileName(text2), Path.GetFileName(text3), _useInt64Input ? "int64" : "int32", _maxLength));
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
					Logger.Log("OnnxReranker", "初始化失败: " + _lastError);
				}
				catch
				{
				}
			}
		}
	}

	private int GetEffectiveMaxLength()
	{
		int num = ((_maxLength > 8) ? _maxLength : 512);
		if (num > 512)
		{
			num = 512;
		}
		return num;
	}

	private static string BuildCacheKey(string query, string document)
	{
		return (query ?? "").Trim() + "\n" + (document ?? "").Trim();
	}

	private bool TryGetCachedScore(string key, out float score)
	{
		score = 0f;
		try
		{
			lock (_cacheLock)
			{
				if (_scoreCache.TryGetValue(key ?? "", out var value))
				{
					score = value;
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private int CacheScore(string key, float score)
	{
		int result = 0;
		try
		{
			lock (_cacheLock)
			{
				if (_scoreCache.Count >= 512)
				{
					_scoreCache.Clear();
				}
				_scoreCache[key ?? ""] = score;
				result = _scoreCache.Count;
			}
		}
		catch
		{
		}
		return result;
	}

	private bool TryRunBatchEncoded(List<List<long>> tokenRows, List<int[]> maskRows, out List<float> scores)
	{
		scores = new List<float>();
		try
		{
			if (tokenRows == null || maskRows == null || tokenRows.Count <= 0 || tokenRows.Count != maskRows.Count || _session == null || _tokenizer == null)
			{
				return false;
			}
			int count = tokenRows.Count;
			int num = 0;
			for (int i = 0; i < tokenRows.Count; i++)
			{
				List<long> list = tokenRows[i];
				int[] array = maskRows[i];
				if (list == null || array == null || list.Count <= 0 || array.Length != list.Count)
				{
					return false;
				}
				if (list.Count > num)
				{
					num = list.Count;
				}
			}
			if (num <= 0)
			{
				return false;
			}
			List<NamedOnnxValue> list2 = new List<NamedOnnxValue>();
			if (_useInt64Input)
			{
				DenseTensor<long> denseTensor = new DenseTensor<long>(new int[2] { count, num });
				DenseTensor<long> denseTensor2 = new DenseTensor<long>(new int[2] { count, num });
				DenseTensor<long> denseTensor3 = new DenseTensor<long>(new int[2] { count, num });
				long value = _tokenizer.PadId;
				for (int j = 0; j < count; j++)
				{
					for (int k = 0; k < num; k++)
					{
						denseTensor[new int[2] { j, k }] = value;
						denseTensor2[new int[2] { j, k }] = 0L;
						denseTensor3[new int[2] { j, k }] = 0L;
					}
					List<long> list3 = tokenRows[j];
					int[] array2 = maskRows[j];
					for (int l = 0; l < list3.Count; l++)
					{
						denseTensor[new int[2] { j, l }] = list3[l];
						denseTensor2[new int[2] { j, l }] = array2[l];
					}
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
				DenseTensor<int> denseTensor4 = new DenseTensor<int>(new int[2] { count, num });
				DenseTensor<int> denseTensor5 = new DenseTensor<int>(new int[2] { count, num });
				DenseTensor<int> denseTensor6 = new DenseTensor<int>(new int[2] { count, num });
				int padId = _tokenizer.PadId;
				for (int m = 0; m < count; m++)
				{
					for (int n = 0; n < num; n++)
					{
						denseTensor4[new int[2] { m, n }] = padId;
						denseTensor5[new int[2] { m, n }] = 0;
						denseTensor6[new int[2] { m, n }] = 0;
					}
					List<long> list4 = tokenRows[m];
					int[] array3 = maskRows[m];
					for (int num2 = 0; num2 < list4.Count; num2++)
					{
						denseTensor4[new int[2] { m, num2 }] = (int)list4[num2];
						denseTensor5[new int[2] { m, num2 }] = array3[num2];
					}
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
				if (dimensions.Length == 1)
				{
					if (dimensions[0] < count)
					{
						return false;
					}
					for (int num3 = 0; num3 < count; num3++)
					{
						scores.Add(Sigmoid(tensor[new int[1] { num3 }]));
					}
					return scores.Count == count;
				}
				if (dimensions[0] < count)
				{
					return false;
				}
				int num4 = dimensions[1];
				for (int num5 = 0; num5 < count; num5++)
				{
					float value2 = ((num4 <= 1) ? tensor[new int[2] { num5, 0 }] : (tensor[new int[2] { num5, 1 }] - tensor[new int[2] { num5, 0 }]));
					scores.Add(Sigmoid(value2));
				}
			}
			return scores.Count == count;
		}
		catch
		{
			scores = new List<float>();
			return false;
		}
	}

	public bool TryScoreBatch(string query, IReadOnlyList<string> documents, out List<float> scores)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		scores = new List<float>();
		string text = (query ?? "").Trim();
		if (string.IsNullOrEmpty(text) || documents == null || documents.Count <= 0)
		{
			return false;
		}
		for (int i = 0; i < documents.Count; i++)
		{
			scores.Add(0f);
		}
		EnsureInitialized();
		if (!_available || _session == null || _tokenizer == null)
		{
			return false;
		}
		bool flag = false;
		List<int> list = new List<int>();
		List<string> list2 = new List<string>();
		List<List<long>> list3 = new List<List<long>>();
		List<int[]> list4 = new List<int[]>();
		try
		{
			int effectiveMaxLength = GetEffectiveMaxLength();
			for (int j = 0; j < documents.Count; j++)
			{
				string text2 = (documents[j] ?? "").Trim();
				if (string.IsNullOrEmpty(text2))
				{
					continue;
				}
				string key = BuildCacheKey(text, text2);
				if (TryGetCachedScore(key, out var score))
				{
					scores[j] = score;
					flag = true;
					continue;
				}
				int[] attentionMask;
				List<long> list5 = _tokenizer.EncodePair(text, text2, effectiveMaxLength, out attentionMask);
				if (list5 != null && list5.Count > 0 && attentionMask != null && attentionMask.Length == list5.Count)
				{
					list.Add(j);
					list2.Add(text2);
					list3.Add(list5);
					list4.Add(attentionMask);
				}
			}
			if (list.Count > 0)
			{
				if (TryRunBatchEncoded(list3, list4, out var scores2) && scores2 != null && scores2.Count == list.Count)
				{
					for (int k = 0; k < list.Count; k++)
					{
						int index = list[k];
						float num = scores2[k];
						scores[index] = num;
						flag = true;
						CacheScore(BuildCacheKey(text, list2[k]), num);
					}
				}
				else
				{
					for (int l = 0; l < list.Count; l++)
					{
						if (TryScore(text, list2[l], out var score2))
						{
							scores[list[l]] = score2;
							flag = true;
						}
					}
				}
			}
			stopwatch.Stop();
			Logger.Metric("onnx.rerank.batch", flag, stopwatch.Elapsed.TotalMilliseconds);
			return flag;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			try
			{
				Logger.Log("OnnxReranker", "批量推理失败: " + ex.Message);
			}
			catch
			{
			}
			Logger.Obs("OnnxReranker", "batch_error", new Dictionary<string, object>
			{
				["ok"] = false,
				["queryLen"] = text.Length,
				["docCount"] = documents.Count,
				["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("onnx.rerank.batch", ok: false, stopwatch.Elapsed.TotalMilliseconds);
			return false;
		}
	}

	public bool TryScore(string query, string document, out float score)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		score = 0f;
		string text = (query ?? "").Trim();
		string text2 = (document ?? "").Trim();
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2))
		{
			return false;
		}
		EnsureInitialized();
		if (!_available || _session == null || _tokenizer == null)
		{
			return false;
		}
		string key = BuildCacheKey(text, text2);
		if (TryGetCachedScore(key, out var score2))
		{
			score = score2;
			stopwatch.Stop();
			Logger.Metric("onnx.rerank", ok: true, stopwatch.Elapsed.TotalMilliseconds);
			return true;
		}
		try
		{
			int effectiveMaxLength = GetEffectiveMaxLength();
			int[] attentionMask;
			List<long> list = _tokenizer.EncodePair(text, text2, effectiveMaxLength, out attentionMask);
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
			float value = 0f;
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
				if (dimensions.Length == 1)
				{
					value = tensor[new int[1]];
				}
				else if (dimensions.Length >= 2)
				{
					int num = dimensions[1];
					value = ((num > 1) ? (tensor[new int[2] { 0, 1 }] - tensor[new int[2]]) : tensor[new int[2]]);
				}
			}
			score = Sigmoid(value);
			try
			{
				int num2 = CacheScore(key, score);
				long num3 = Interlocked.Increment(ref _obsCounter);
				if (num3 % 20 == 1)
				{
					stopwatch.Stop();
					Logger.Obs("OnnxReranker", "sample", new Dictionary<string, object>
					{
						["ok"] = true,
						["queryLen"] = text.Length,
						["docLen"] = text2.Length,
						["score"] = Math.Round(score, 4),
						["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
						["cacheSize"] = num2
					});
					Logger.Metric("onnx.rerank", ok: true, stopwatch.Elapsed.TotalMilliseconds);
					return true;
				}
			}
			catch
			{
			}
			stopwatch.Stop();
			Logger.Metric("onnx.rerank", ok: true, stopwatch.Elapsed.TotalMilliseconds);
			return true;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			try
			{
				Logger.Log("OnnxReranker", "推理失败: " + ex.Message);
			}
			catch
			{
			}
			Logger.Obs("OnnxReranker", "error", new Dictionary<string, object>
			{
				["ok"] = false,
				["queryLen"] = text.Length,
				["docLen"] = text2.Length,
				["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("onnx.rerank", ok: false, stopwatch.Elapsed.TotalMilliseconds);
			return false;
		}
	}

	private static float Sigmoid(float value)
	{
		try
		{
			if (value >= 0f)
			{
				float num = (float)Math.Exp(0f - value);
				return 1f / (1f + num);
			}
			float num2 = (float)Math.Exp(value);
			return num2 / (1f + num2);
		}
		catch
		{
			return value;
		}
	}
}
