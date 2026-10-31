using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AnimusForge;

/// <summary>
/// Append-only on-disk store of normalized document vectors for one embedding model (file name = model fingerprint).
/// Record: [int32 textByteLength][utf8 text][int32 dim][dim float32]. Loaded once per process into memory; a torn
/// tail record (crash mid-append) is ignored and truncated away on the next append. Query-time texts are not stored:
/// only the knowledge index build calls <see cref="Append"/>, so the file grows with world-book seeds, not chat input.
/// </summary>
internal sealed class EmbeddingVectorDiskCache
{
	internal const int MaxTextBytes = 4096;
	internal const int MaxDimension = 4096;
	// Rebuild from scratch past this size instead of growing forever (each world book is a few MB at 384 dims).
	internal const long MaxFileBytes = 256L * 1024 * 1024;
	private const uint Magic = 0x31564641; // "AFV1"

	private readonly string _path;
	private readonly int _dimension;
	private readonly Dictionary<string, float[]> _vectors = new Dictionary<string, float[]>(StringComparer.Ordinal);
	private long _validLength;

	internal string FilePath => _path;
	internal int Count => _vectors.Count;

	private EmbeddingVectorDiskCache(string path, int dimension)
	{
		_path = path;
		_dimension = dimension;
	}

	internal static string FileNameFor(string modelFingerprint)
	{
		using (SHA256 sha = SHA256.Create())
		{
			byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(modelFingerprint ?? ""));
			StringBuilder sb = new StringBuilder("embeddings-", 11 + 32 + 4);
			for (int i = 0; i < 16; i++) sb.Append(hash[i].ToString("x2"));
			return sb.Append(".bin").ToString();
		}
	}

	/// <summary>Opens (or starts) the store; vectors of a different dimension or a corrupt header reset it.</summary>
	internal static EmbeddingVectorDiskCache Open(string directory, string modelFingerprint, int dimension)
	{
		Directory.CreateDirectory(directory);
		var cache = new EmbeddingVectorDiskCache(Path.Combine(directory, FileNameFor(modelFingerprint)), dimension);
		cache.Load();
		return cache;
	}

	internal bool TryGet(string text, out float[] vector) => _vectors.TryGetValue(text ?? "", out vector);

	private void Load()
	{
		_validLength = 0;
		if (!File.Exists(_path)) return;
		try
		{
			using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var reader = new BinaryReader(stream, Encoding.UTF8))
			{
				if (stream.Length < 8 || reader.ReadUInt32() != Magic || reader.ReadInt32() != _dimension) return;
				_validLength = 8;
				while (stream.Length - stream.Position >= 8)
				{
					int textBytes = reader.ReadInt32();
					if (textBytes <= 0 || textBytes > MaxTextBytes || stream.Length - stream.Position < textBytes + 4L) break;
					string text = Encoding.UTF8.GetString(reader.ReadBytes(textBytes));
					int dim = reader.ReadInt32();
					if (dim != _dimension || stream.Length - stream.Position < dim * 4L) break;
					float[] vector = new float[dim];
					for (int i = 0; i < dim; i++) vector[i] = reader.ReadSingle();
					_vectors[text] = vector;
					_validLength = stream.Position;
				}
			}
		}
		catch (IOException)
		{
			_vectors.Clear();
			_validLength = 0;
		}
	}

	/// <summary>Persists new vectors in one append; returns how many were written. Never throws.</summary>
	internal int Append(IReadOnlyList<KeyValuePair<string, float[]>> items)
	{
		if (items == null || items.Count == 0) return 0;
		try
		{
			bool reset = _validLength < 8 || _validLength > MaxFileBytes;
			if (reset) _vectors.Clear();
			using (var stream = new FileStream(_path, reset ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
			{
				if (reset)
				{
					WriteHeader(stream);
					_validLength = 8;
				}
				stream.SetLength(_validLength);
				stream.Position = _validLength;
				int written = 0;
				using (var buffer = new MemoryStream())
				using (var writer = new BinaryWriter(buffer, Encoding.UTF8))
				{
					foreach (KeyValuePair<string, float[]> item in items)
					{
						string text = item.Key ?? "";
						float[] vector = item.Value;
						byte[] bytes = Encoding.UTF8.GetBytes(text);
						if (bytes.Length == 0 || bytes.Length > MaxTextBytes || vector == null || vector.Length != _dimension
							|| _vectors.ContainsKey(text)) continue;
						writer.Write(bytes.Length);
						writer.Write(bytes);
						writer.Write(vector.Length);
						for (int i = 0; i < vector.Length; i++) writer.Write(vector[i]);
						_vectors[text] = vector;
						written++;
					}
					writer.Flush();
					buffer.Position = 0;
					buffer.CopyTo(stream);
				}
				stream.Flush();
				_validLength = stream.Position;
				return written;
			}
		}
		catch (IOException)
		{
			return 0;
		}
		catch (UnauthorizedAccessException)
		{
			return 0;
		}
	}

	private void WriteHeader(Stream stream)
	{
		using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
		{
			writer.Write(Magic);
			writer.Write(_dimension);
		}
	}
}
