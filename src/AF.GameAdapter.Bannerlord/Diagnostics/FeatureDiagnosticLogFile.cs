using System;
using System.IO;
using System.Text;

namespace AnimusForge;

internal sealed class FeatureDiagnosticLogFile
{
	private const int BytesPerMegabyte = 1024 * 1024;

	private const int MaxEntryBytes = 64 * 1024;

	private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private readonly object _syncRoot = new object();

	private readonly string _fileName;

	private readonly string _exportFilePrefix;

	private readonly string _defaultSource;

	private readonly Func<bool> _isEnabled;

	private readonly Func<bool> _isVerboseEnabled;

	private readonly Func<int> _getMaxSizeMegabytes;

	private string _logPath;

	private long _knownLengthBytes = -1L;

	internal FeatureDiagnosticLogFile(string fileName, string exportFilePrefix, string defaultSource, Func<bool> isEnabled, Func<bool> isVerboseEnabled, Func<int> getMaxSizeMegabytes)
	{
		_fileName = Path.GetFileName(fileName ?? string.Empty);
		_exportFilePrefix = string.IsNullOrWhiteSpace(exportFilePrefix) ? "Diagnostic" : exportFilePrefix.Trim();
		_defaultSource = string.IsNullOrWhiteSpace(defaultSource) ? "Diagnostic" : defaultSource.Trim();
		_isEnabled = isEnabled;
		_isVerboseEnabled = isVerboseEnabled;
		_getMaxSizeMegabytes = getMaxSizeMegabytes;
	}

	internal void Write(string source, string message, bool verbose)
	{
		if (!ReadEnabled() || (verbose && !ReadVerboseEnabled()))
		{
			return;
		}

		string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
			+ (verbose ? " [VERBOSE] [" : " [INFO] [")
			+ NormalizeSource(source)
			+ "] "
			+ (message ?? string.Empty)
			+ Environment.NewLine;
		line = LimitEntrySize(line, out int incomingByteCount);

		lock (_syncRoot)
		{
			string path = GetLogPathLocked();
			RotateIfNeededLocked(path, incomingByteCount);
			File.AppendAllText(path, line, Utf8WithoutBom);
			_knownLengthBytes += incomingByteCount;
		}
	}

	internal string GetLogPath()
	{
		lock (_syncRoot)
		{
			return GetLogPathLocked();
		}
	}

	internal string GetLogDirectory()
	{
		string path = GetLogPath();
		string directory = Path.GetDirectoryName(path);
		return string.IsNullOrWhiteSpace(directory) ? AnimusForgeModulePaths.GetLogsDirectory() : directory;
	}

	internal string ExportToDesktop()
	{
		lock (_syncRoot)
		{
			string sourcePath = GetLogPathLocked();
			EnsureLogFileExistsLocked(sourcePath);

			string desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			if (string.IsNullOrWhiteSpace(desktopDirectory))
			{
				desktopDirectory = Path.GetDirectoryName(sourcePath);
			}
			if (string.IsNullOrWhiteSpace(desktopDirectory))
			{
				desktopDirectory = AnimusForgeModulePaths.GetLogsDirectory();
			}
			Directory.CreateDirectory(desktopDirectory);

			string exportPath = Path.Combine(desktopDirectory, _exportFilePrefix + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".log");
			File.Copy(sourcePath, exportPath, overwrite: true);
			return exportPath;
		}
	}

	internal void Clear()
	{
		lock (_syncRoot)
		{
			string path = GetLogPathLocked();
			File.WriteAllText(path, string.Empty, Utf8WithoutBom);
			_knownLengthBytes = 0L;
		}
	}

	private string GetLogPathLocked()
	{
		if (!string.IsNullOrWhiteSpace(_logPath))
		{
			return _logPath;
		}

		string safeFileName = string.IsNullOrWhiteSpace(_fileName) ? "Diagnostic.log" : _fileName;
		_logPath = AnimusForgeModulePaths.GetLogFilePath(safeFileName);
		string directory = Path.GetDirectoryName(_logPath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}
		return _logPath;
	}

	private void RotateIfNeededLocked(string path, int incomingByteCount)
	{
		if (!File.Exists(path))
		{
			_knownLengthBytes = 0L;
			return;
		}

		if (_knownLengthBytes < 0L)
		{
			_knownLengthBytes = new FileInfo(path).Length;
		}

		long maxBytes = (long)ReadMaxSizeMegabytes() * BytesPerMegabyte;
		if (_knownLengthBytes + Math.Max(0, incomingByteCount) <= maxBytes)
		{
			return;
		}

		string directory = Path.GetDirectoryName(path) ?? string.Empty;
		string extension = Path.GetExtension(path);
		string previousPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(path) + ".previous" + extension);
		File.Copy(path, previousPath, overwrite: true);
		string marker = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
			+ " [INFO] [" + _defaultSource + "] Log rotated; previous file=" + Path.GetFileName(previousPath)
			+ Environment.NewLine;
		File.WriteAllText(path, marker, Utf8WithoutBom);
		_knownLengthBytes = Utf8WithoutBom.GetByteCount(marker);
	}

	private void EnsureLogFileExistsLocked(string path)
	{
		if (File.Exists(path))
		{
			return;
		}

		File.WriteAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
			+ " [INFO] [" + _defaultSource + "] Log created; no diagnostic events have been written yet."
			+ Environment.NewLine, Utf8WithoutBom);
		_knownLengthBytes = new FileInfo(path).Length;
	}

	private bool ReadEnabled()
	{
		try
		{
			return _isEnabled?.Invoke() ?? true;
		}
		catch
		{
			return false;
		}
	}

	private bool ReadVerboseEnabled()
	{
		try
		{
			return _isVerboseEnabled?.Invoke() ?? false;
		}
		catch
		{
			return false;
		}
	}

	private int ReadMaxSizeMegabytes()
	{
		try
		{
			return Math.Max(1, _getMaxSizeMegabytes?.Invoke() ?? 16);
		}
		catch
		{
			return 16;
		}
	}

	private string NormalizeSource(string source)
	{
		return string.IsNullOrWhiteSpace(source) ? _defaultSource : source.Trim();
	}

	private static string LimitEntrySize(string line, out int byteCount)
	{
		string safeLine = line ?? string.Empty;
		byteCount = Utf8WithoutBom.GetByteCount(safeLine);
		if (byteCount <= MaxEntryBytes)
		{
			return safeLine;
		}

		string content = safeLine.TrimEnd('\r', '\n');
		string suffix = " ... [entry truncated at 64 KiB]" + Environment.NewLine;
		int suffixBytes = Utf8WithoutBom.GetByteCount(suffix);
		int low = 0;
		int high = content.Length;
		while (low < high)
		{
			int middle = low + (high - low + 1) / 2;
			int prefixLength = AdjustTruncationBoundary(content, middle);
			int candidateBytes = Utf8WithoutBom.GetByteCount(content.Substring(0, prefixLength)) + suffixBytes;
			if (candidateBytes <= MaxEntryBytes)
			{
				low = middle;
			}
			else
			{
				high = middle - 1;
			}
		}

		int finalLength = AdjustTruncationBoundary(content, Math.Min(low, content.Length));
		string truncated = content.Substring(0, finalLength) + suffix;
		byteCount = Utf8WithoutBom.GetByteCount(truncated);
		return truncated;
	}

	private static int AdjustTruncationBoundary(string content, int length)
	{
		int safeLength = Math.Max(0, Math.Min(length, content?.Length ?? 0));
		if (safeLength <= 0 || safeLength >= content.Length)
		{
			return safeLength;
		}
		if (char.IsHighSurrogate(content[safeLength - 1]))
		{
			safeLength--;
		}
		if (safeLength > 0 && safeLength < content.Length && content[safeLength - 1] == '\r' && content[safeLength] == '\n')
		{
			safeLength--;
		}
		return safeLength;
	}
}
