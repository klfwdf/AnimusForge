using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json.Linq;

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
				JObject jObject = JObject.Parse(File.ReadAllText(tokenizerJsonPath, Encoding.UTF8));
				if (!(jObject["model"]?["vocab"] is JObject jObject2))
				{
					return null;
				}
				Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
				foreach (JProperty item in jObject2.Properties())
				{
					if (item != null)
					{
						string text = item.Name ?? "";
						if (!string.IsNullOrEmpty(text))
						{
							int value = item.Value.Value<int>();
							dictionary[text] = value;
						}
					}
				}
				bool lowerCase = false;
				try
				{
					JToken jToken = jObject["normalizer"];
					if (jToken != null)
					{
						string a = (jToken["type"]?.Value<string>() ?? "").Trim();
						if (string.Equals(a, "BertNormalizer", StringComparison.OrdinalIgnoreCase))
						{
							lowerCase = jToken["lowercase"]?.Value<bool>() ?? false;
						}
						else if (jToken["normalizers"] is JArray jArray)
						{
							foreach (JToken item2 in jArray)
							{
								string a2 = (item2?["type"]?.Value<string>() ?? "").Trim();
								if (string.Equals(a2, "BertNormalizer", StringComparison.OrdinalIgnoreCase))
								{
									lowerCase = item2["lowercase"]?.Value<bool>() ?? false;
									break;
								}
							}
						}
					}
				}
				catch
				{
					lowerCase = false;
				}
				int padId = FindTokenId(jObject, dictionary, "[PAD]", 0);
				int unkId = FindTokenId(jObject, dictionary, "[UNK]", 100);
				int clsId = FindTokenId(jObject, dictionary, "[CLS]", 101);
				int sepId = FindTokenId(jObject, dictionary, "[SEP]", 102);
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
				if (jo?["added_tokens"] is JArray jArray)
				{
					foreach (JToken item in jArray)
					{
						if (string.Equals(item?["content"]?.Value<string>() ?? "", token, StringComparison.Ordinal))
						{
							return item["id"]?.Value<int>() ?? fallback;
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
			return (ch >= '\u4E00' && ch <= '\u9FFF') || (ch >= '\u3400' && ch <= '\u4DBF');
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
    private readonly object _initializationTaskLock = new object();
    private Task<bool> _initializationTask;

    // Single flight across campaign changes. The worker receives only the captured module path,
    // never Campaign/UI objects. Native session creation is not forcibly cancellable.
    internal Task<bool> InitializeAsync(string moduleRoot)
    {
        if (string.IsNullOrWhiteSpace(moduleRoot)) throw new ArgumentException("Captured module root is required.", nameof(moduleRoot));
        lock (_initializationTaskLock)
        {
            return _initializationTask ??= Task.Run(() =>
            {
                EnsureInitialized(moduleRoot);
                return _available;
            });
        }
    }

	private volatile bool _initialized;

	private bool _available;

	private string _lastError = "";

	private InferenceSession _session;

	private BertTokenizerLite _tokenizer;

	// Set instead of _tokenizer for BPE (bekko) models.
	private BpeTokenizerLite _bpeTokenizer;

	private string _inputIdsName = "input_ids";

	private string _inputMaskName = "attention_mask";

	private string _inputTypeName = "token_type_ids";

	private string _outputName = "";

	private bool _useInt64Input = true;

	// True for bge-small-en: the sentence vector is the first ([CLS]) token, not the inner-token mean.
	private bool _poolCls;

	// True for bekko: mean over every attended token, including <bos>/<eos>.
	private bool _poolMeanAll;

	private int _maxLength = 512;

	private readonly object _cacheLock = new object();

	private readonly Dictionary<string, float[]> _queryCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

	private const int QueryCacheMax = 256;

	private long _obsCounter = 0L;

	internal const int IntraOpThreadCap = 4;

	internal const int IndexBatchSize = 16;

	// Identity of the loaded model files; the persistent vector cache is keyed by it so a model swap never reuses vectors.
	private string _modelFingerprint = "";

	private EmbeddingVectorDiskCache _diskCache;

	private readonly object _diskCacheLock = new object();

	private string _vectorCacheDirectory = "";

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
			JObject jObject = JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8));
			JToken jToken = jObject["max_position_embeddings"];
			if (jToken == null)
			{
				return false;
			}
			int num = jToken.Value<int>();
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

	private void EnsureInitialized(string moduleRoot = null)
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
			try
			{
				AnimusForgeModelStore.ModelFiles files = moduleRoot == null
                    ? AnimusForgeModelStore.ResolveEmbedding() : AnimusForgeModelStore.ResolveEmbedding(moduleRoot);
				string text5 = files.ModelPath;
				string text6 = files.TokenizerPath;
					string text7 = files.ConfigPath;
					_poolCls = string.Equals(files.Pooling, "cls", StringComparison.Ordinal);
					_poolMeanAll = string.Equals(files.Pooling, "mean_all", StringComparison.Ordinal);
					if (!string.IsNullOrEmpty(text7))
				{
					TryReadMaxPosition(text7, out _maxLength);
				}
				if (_poolMeanAll)
				{
					_bpeTokenizer = BpeTokenizerLite.TryLoad(text6);
				}
				else
				{
					_tokenizer = BertTokenizerLite.Load(text6);
				}
				if (_tokenizer == null && _bpeTokenizer == null)
				{
					_available = false;
					_lastError = "Failed to parse the tokenizer.";
					return;
				}
				SessionOptions sessionOptions = new SessionOptions();
				sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;
				// ORT's default spreads one tiny inference over every core and competes with the game thread;
				// four threads keep single-query latency (~8.8 vs 8.4 ms) while freeing the rest of the CPU.
				sessionOptions.IntraOpNumThreads = Math.Max(1, Math.Min(IntraOpThreadCap, Environment.ProcessorCount));
				_session = new InferenceSession(text5, sessionOptions);
				_modelFingerprint = BuildModelFingerprint(text5, text6, files.Pooling);
				// Next to the model it belongs to: removing the model folder removes its vectors too.
				_vectorCacheDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(text5) ?? "", "vectors");
				_inputIdsName = GuessInputName(_session.InputMetadata, "input_ids", "input_ids");
				_inputMaskName = GuessInputName(_session.InputMetadata, "attention_mask", "attention_mask");
				// GuessInputName falls back to the first input; only feed token types when the graph really has them.
				_inputTypeName = _session.InputMetadata.Keys.Any((string k) => k != null && k.IndexOf("token_type", StringComparison.OrdinalIgnoreCase) >= 0)
					? GuessInputName(_session.InputMetadata, "token_type_ids", "token_type") : "";
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
					Logger.Log("OnnxEmbedding", string.Format("Initialized model={0} tokenizer={1} inputType={2} maxLen={3} pooling={4} dir={5}", System.IO.Path.GetFileName(text5), System.IO.Path.GetFileName(text6), _useInt64Input ? "int64" : "int32", _maxLength, _poolCls ? "cls" : (_poolMeanAll ? "mean_all" : "mean"), System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(text5) ?? "")));
				}
				catch
				{
				}
			}
			catch (Exception ex)
			{
				_available = false;
				_lastError = ex.Message ?? "Initialization exception";
				try
				{
					Logger.Log("OnnxEmbedding", "Initialization failed: " + _lastError);
				}
				catch
				{
				}
			}
			finally
			{
				_initialized = true;
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
		if (!_available || _session == null || (_tokenizer == null && _bpeTokenizer == null))
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
			List<long> list = _bpeTokenizer != null ? _bpeTokenizer.Encode(text2, num, out attentionMask) : _tokenizer.Encode(text2, num, out attentionMask);
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
					else if (dimensions.Length >= 3 && _poolCls)
					{
						int clsWidth = dimensions[2];
						if (dimensions[1] <= 0 || clsWidth <= 0)
						{
							return false;
						}
						array = new float[clsWidth];
						for (int c = 0; c < clsWidth; c++)
						{
							array[c] = tensor[new int[3] { 0, 0, c }];
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
					bool flag = num7 >= 3 && !_poolMeanAll;
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
				Logger.Log("OnnxEmbedding", "Inference failed: " + ex.Message);
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

	private static string BuildModelFingerprint(string modelPath, string tokenizerPath, string pooling)
	{
		string Part(string path)
		{
			try
			{
				FileInfo info = new FileInfo(path ?? "");
				return info.Exists ? info.Name + ":" + info.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
					+ ":" + info.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
			}
			catch
			{
				return "";
			}
		}
		return "v1|" + Part(modelPath) + "|" + Part(tokenizerPath) + "|" + (pooling ?? "");
	}

	/// <summary>
	/// Index-build embedding: persistent vectors first, then length-sorted batches of <see cref="IndexBatchSize"/>
	/// through the same pooling as <see cref="TryGetEmbedding"/>. New vectors are appended to the model's vector store.
	/// results[i] is null when text i is blank or failed. Background-thread only.
	/// </summary>
	public float[][] EmbedDocuments(IReadOnlyList<string> texts)
	{
		float[][] results = new float[texts?.Count ?? 0][];
		if (results.Length == 0)
		{
			return results;
		}
		EnsureInitialized();
		if (!_available || _session == null || (_tokenizer == null && _bpeTokenizer == null))
		{
			return results;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		string[] keys = new string[results.Length];
		EmbeddingVectorDiskCache store = GetVectorStore();
		List<int> pending = new List<int>();
		Dictionary<string, int> firstIndex = new Dictionary<string, int>(StringComparer.Ordinal);
		int fromStore = 0;
		for (int i = 0; i < results.Length; i++)
		{
			string key = (texts[i] ?? "").Trim();
			keys[i] = key;
			if (key.Length == 0)
			{
				continue;
			}
			if (store != null)
			{
				lock (_diskCacheLock)
				{
					if (store.TryGet(key, out float[] cached))
					{
						results[i] = cached;
						fromStore++;
						continue;
					}
				}
			}
			if (!firstIndex.ContainsKey(key))
			{
				firstIndex[key] = i;
				pending.Add(i);
			}
		}
		int maxLen = Math.Min(512, _maxLength > 8 ? _maxLength : 512);
		List<(int Index, List<long> Ids, int[] Mask)> encoded = new List<(int, List<long>, int[])>(pending.Count);
		foreach (int i in pending)
		{
			try
			{
				List<long> ids = _bpeTokenizer != null ? _bpeTokenizer.Encode(keys[i], maxLen, out int[] mask) : _tokenizer.Encode(keys[i], maxLen, out mask);
				if (ids != null && ids.Count > 0 && mask != null && mask.Length == ids.Count)
				{
					encoded.Add((i, ids, mask));
				}
			}
			catch
			{
			}
		}
		encoded.Sort((a, b) => a.Ids.Count.CompareTo(b.Ids.Count));
		List<KeyValuePair<string, float[]>> fresh = new List<KeyValuePair<string, float[]>>(encoded.Count);
		int failedBatches = 0;
		for (int start = 0; start < encoded.Count; start += IndexBatchSize)
		{
			int count = Math.Min(IndexBatchSize, encoded.Count - start);
			float[][] batch = RunBatch(encoded, start, count);
			if (batch == null)
			{
				failedBatches++;
				continue;
			}
			for (int r = 0; r < count; r++)
			{
				if (batch[r] == null)
				{
					continue;
				}
				int index = encoded[start + r].Index;
				results[index] = batch[r];
				fresh.Add(new KeyValuePair<string, float[]>(keys[index], batch[r]));
			}
		}
		for (int i = 0; i < results.Length; i++)
		{
			if (results[i] == null && keys[i].Length > 0 && firstIndex.TryGetValue(keys[i], out int first))
			{
				results[i] = results[first];
			}
		}
		int persisted = 0;
		if (store != null && fresh.Count > 0)
		{
			lock (_diskCacheLock)
			{
				persisted = store.Append(fresh);
			}
		}
		stopwatch.Stop();
		Logger.Metric("onnx.embedding.batch", failedBatches == 0, stopwatch.Elapsed.TotalMilliseconds);
		try
		{
			Logger.Log("OnnxEmbedding", "index_embed texts=" + results.Length + " fromStore=" + fromStore + " computed=" + fresh.Count
				+ " persisted=" + persisted + " failedBatches=" + failedBatches + " ms=" + Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1)
				+ " store=" + (store == null ? "(none)" : System.IO.Path.GetFileName(store.FilePath) + ":" + store.Count));
		}
		catch
		{
		}
		return results;
	}

	private EmbeddingVectorDiskCache GetVectorStore()
	{
		if (string.IsNullOrEmpty(_vectorCacheDirectory) || string.IsNullOrEmpty(_modelFingerprint))
		{
			return null;
		}
		lock (_diskCacheLock)
		{
			if (_diskCache != null)
			{
				return _diskCache;
			}
			try
			{
				int dimension = ProbeDimension();
				if (dimension <= 0)
				{
					return null;
				}
				_diskCache = EmbeddingVectorDiskCache.Open(_vectorCacheDirectory, _modelFingerprint, dimension);
				Logger.Log("OnnxEmbedding", "vector_store opened file=" + System.IO.Path.GetFileName(_diskCache.FilePath) + " entries=" + _diskCache.Count);
			}
			catch (Exception ex)
			{
				Logger.Log("OnnxEmbedding", "vector_store unavailable: " + ex.Message);
				_vectorCacheDirectory = "";
			}
			return _diskCache;
		}
	}

	private int ProbeDimension()
	{
		return TryGetEmbedding("dimension probe", out float[] v) && v != null ? v.Length : 0;
	}

	/// <summary>One padded inference over encoded[start..start+count); same pooling/normalization as the single path.</summary>
	private float[][] RunBatch(List<(int Index, List<long> Ids, int[] Mask)> encoded, int start, int count)
	{
		try
		{
			int width = 0;
			for (int r = 0; r < count; r++)
			{
				width = Math.Max(width, encoded[start + r].Ids.Count);
			}
			List<NamedOnnxValue> inputs = new List<NamedOnnxValue>(3);
			int[] shape = new int[2] { count, width };
			if (_useInt64Input)
			{
				long[] ids = new long[count * width];
				long[] mask = new long[count * width];
				for (int r = 0; r < count; r++)
				{
					var row = encoded[start + r];
					for (int t = 0; t < row.Ids.Count; t++)
					{
						ids[r * width + t] = row.Ids[t];
						mask[r * width + t] = row.Mask[t];
					}
				}
				AddInputs(inputs, new DenseTensor<long>(ids, shape), new DenseTensor<long>(mask, shape), new DenseTensor<long>(new long[count * width], shape));
			}
			else
			{
				int[] ids = new int[count * width];
				int[] mask = new int[count * width];
				for (int r = 0; r < count; r++)
				{
					var row = encoded[start + r];
					for (int t = 0; t < row.Ids.Count; t++)
					{
						ids[r * width + t] = (int)row.Ids[t];
						mask[r * width + t] = row.Mask[t];
					}
				}
				AddInputs(inputs, new DenseTensor<int>(ids, shape), new DenseTensor<int>(mask, shape), new DenseTensor<int>(new int[count * width], shape));
			}
			float[][] output = new float[count][];
			using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> source = _session.Run(inputs))
			{
				DisposableNamedOnnxValue value = source.FirstOrDefault((DisposableNamedOnnxValue x) => string.Equals(x.Name ?? "", _outputName ?? "", StringComparison.OrdinalIgnoreCase)) ?? source.FirstOrDefault();
				if (value == null)
				{
					return null;
				}
				Tensor<float> tensor = value.AsTensor<float>();
				ReadOnlySpan<int> dims = tensor.Dimensions;
				if (dims.Length < 2 || dims[0] != count)
				{
					return null;
				}
				// Row-major contiguous output; read it flat instead of through the (slow) multi-index indexer.
				float[] flat = tensor.ToArray();
				for (int r = 0; r < count; r++)
				{
					output[r] = PoolRow(flat, dims, r, encoded[start + r].Mask, encoded[start + r].Ids.Count);
				}
			}
			return output;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("OnnxEmbedding", "Batch inference failed: " + ex.Message);
			}
			catch
			{
			}
			return null;
		}
	}

	private void AddInputs<T>(List<NamedOnnxValue> inputs, DenseTensor<T> ids, DenseTensor<T> mask, DenseTensor<T> types)
	{
		if (!string.IsNullOrEmpty(_inputIdsName))
		{
			inputs.Add(NamedOnnxValue.CreateFromTensor(_inputIdsName, ids));
		}
		if (!string.IsNullOrEmpty(_inputMaskName) && !string.Equals(_inputMaskName, _inputIdsName, StringComparison.OrdinalIgnoreCase))
		{
			inputs.Add(NamedOnnxValue.CreateFromTensor(_inputMaskName, mask));
		}
		if (!string.IsNullOrEmpty(_inputTypeName) && !string.Equals(_inputTypeName, _inputIdsName, StringComparison.OrdinalIgnoreCase) && !string.Equals(_inputTypeName, _inputMaskName, StringComparison.OrdinalIgnoreCase))
		{
			inputs.Add(NamedOnnxValue.CreateFromTensor(_inputTypeName, types));
		}
	}

	/// <summary>Row r of a batched output, pooled over that row's real tokens exactly like the single-text path.</summary>
	private float[] PoolRow(float[] flat, ReadOnlySpan<int> dims, int r, int[] mask, int length)
	{
		float[] array;
		if (dims.Length == 2)
		{
			array = new float[dims[1]];
			Array.Copy(flat, r * dims[1], array, 0, dims[1]);
		}
		else
		{
			int width = dims[2];
			int rowBase = r * dims[1] * width;
			if (width <= 0)
			{
				return null;
			}
			array = new float[width];
			if (_poolCls)
			{
				Array.Copy(flat, rowBase, array, 0, width);
			}
			else
			{
				int tokens = Math.Min(Math.Min(dims[1], mask.Length), length);
				bool skipEnds = tokens >= 3 && !_poolMeanAll;
				int used = 0;
				for (int pass = 0; pass < 2 && used == 0; pass++)
				{
					for (int t = 0; t < tokens; t++)
					{
						if (mask[t] > 0 && (pass == 1 || !skipEnds || (t != 0 && t != tokens - 1)))
						{
							used++;
							int offset = rowBase + t * width;
							for (int c = 0; c < width; c++)
							{
								array[c] += flat[offset + c];
							}
						}
					}
				}
				if (used == 0)
				{
					return null;
				}
				float scale = 1f / used;
				for (int c = 0; c < width; c++)
				{
					array[c] *= scale;
				}
			}
		}
		NormalizeInPlace(array);
		return array;
	}
}
