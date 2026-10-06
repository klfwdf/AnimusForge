using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace TaleWorlds.Library;

public class Logger
{
	private class FileManager
	{
		private bool _isCheckingFileSize;

		private int _maxFileSize;

		private int _numFiles;

		private FileStream[] _streams;

		private int _currentStreamIndex;

		private FileStream _errorStream;

		public FileManager(string path, string name, int numFiles, int maxTotalSize, bool overwrite, bool logErrorsToDifferentFile)
		{
			if (maxTotalSize < numFiles * 64 * 1024)
			{
				_numFiles = 1;
				_isCheckingFileSize = false;
			}
			else
			{
				_numFiles = numFiles;
				if (numFiles <= 0)
				{
					_numFiles = 1;
					_isCheckingFileSize = false;
				}
				_maxFileSize = maxTotalSize / _numFiles;
				_isCheckingFileSize = true;
			}
			_streams = new FileStream[_numFiles];
			_currentStreamIndex = 0;
			try
			{
				for (int i = 0; i < _numFiles; i++)
				{
					string text = name + "_" + i;
					string path2 = path + "/" + text + ".html";
					_streams[i] = (overwrite ? new FileStream(path2, FileMode.Create) : new FileStream(path2, FileMode.OpenOrCreate));
					FillEmptyStream(_streams[i]);
				}
				if (logErrorsToDifferentFile)
				{
					string path3 = path + "/" + name + "_errors.html";
					_errorStream = (overwrite ? new FileStream(path3, FileMode.Create) : new FileStream(path3, FileMode.OpenOrCreate));
					FillEmptyStream(_errorStream);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error when creating log file(s): " + ex.GetBaseException().Message);
				for (int j = 0; j < _numFiles; j++)
				{
					string text2 = name + "__" + j;
					string path4 = path + "/" + text2 + ".html";
					_streams[j] = (overwrite ? new FileStream(path4, FileMode.Create) : new FileStream(path4, FileMode.OpenOrCreate));
					FillEmptyStream(_streams[j]);
				}
				if (logErrorsToDifferentFile)
				{
					string path5 = path + "/" + name + "_errors.html";
					_errorStream = (overwrite ? new FileStream(path5, FileMode.Create) : new FileStream(path5, FileMode.OpenOrCreate));
					FillEmptyStream(_errorStream);
				}
			}
		}

		public FileStream GetFileStream()
		{
			return _streams[_currentStreamIndex];
		}

		public FileStream GetErrorFileStream()
		{
			return _errorStream;
		}

		public void CheckForFileSize()
		{
			if (_isCheckingFileSize && _streams[_currentStreamIndex].Length > _maxFileSize)
			{
				_currentStreamIndex = (_currentStreamIndex + 1) % _numFiles;
				ResetFileStream(_streams[_currentStreamIndex]);
			}
		}

		public void ShutDown()
		{
			for (int i = 0; i < _numFiles; i++)
			{
				_streams[i].Close();
				_streams[i] = null;
			}
			if (_errorStream != null)
			{
				_errorStream.Close();
				_errorStream = null;
			}
		}

		private void FillEmptyStream(FileStream stream)
		{
			if (stream.Length == 0L)
			{
				string s = "<table></table>";
				byte[] bytes = _logFileEncoding.GetBytes(s);
				stream.Write(bytes, 0, bytes.Length);
			}
		}

		private void ResetFileStream(FileStream stream)
		{
			stream.SetLength(0L);
			FillEmptyStream(stream);
		}
	}

	private Queue<HTMLDebugData> _logQueue;

	private static Encoding _logFileEncoding;

	private string _name;

	private bool _writeErrorsToDifferentFile;

	private static List<Logger> _loggers;

	private FileManager _fileManager;

	private static Thread _thread;

	private static bool _running;

	private static bool _printedOnThisCycle;

	private static bool _isOver;

	public static string LogsFolder;

	public bool LogOnlyErrors { get; set; }

	static Logger()
	{
		_running = true;
		_printedOnThisCycle = false;
		_isOver = false;
		LogsFolder = "";
		_logFileEncoding = Encoding.UTF8;
		LogsFolder = Environment.CurrentDirectory + "\\logs";
		_loggers = new List<Logger>();
	}

	public Logger(string name)
		: this(name, writeErrorsToDifferentFile: false, logOnlyErrors: false, doNotUseProcessId: false)
	{
	}

	public Logger(string name, bool writeErrorsToDifferentFile, bool logOnlyErrors, bool doNotUseProcessId, int numFiles = 1, int totalFileSize = -1, bool overwrite = false)
	{
		string friendlyName = AppDomain.CurrentDomain.FriendlyName;
		friendlyName = Path.GetFileNameWithoutExtension(friendlyName);
		_name = name;
		_writeErrorsToDifferentFile = writeErrorsToDifferentFile;
		LogOnlyErrors = logOnlyErrors;
		_logQueue = new Queue<HTMLDebugData>();
		int id = Process.GetCurrentProcess().Id;
		DateTime now = DateTime.Now;
		string text = LogsFolder;
		if (!doNotUseProcessId)
		{
			string text2 = friendlyName + "_" + now.ToString("yyyyMMdd") + "_" + now.ToString("hhmmss") + "_" + id;
			text = text + "/" + text2;
		}
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		_fileManager = new FileManager(text, _name, numFiles, totalFileSize, overwrite, writeErrorsToDifferentFile);
		lock (_loggers)
		{
			if (_thread == null)
			{
				_thread = new Thread(ThreadMain);
				_thread.IsBackground = true;
				_thread.Priority = ThreadPriority.BelowNormal;
				_thread.Start();
			}
			_loggers.Add(this);
		}
	}

	private static void ThreadMain()
	{
		while (_running)
		{
			try
			{
				Printer();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception on network debug thread: " + ex.Message);
			}
		}
		_isOver = true;
	}

	private static void Printer()
	{
		while ((_running || _printedOnThisCycle) && _loggers.Count > 0)
		{
			_printedOnThisCycle = false;
			lock (_loggers)
			{
				foreach (Logger logger in _loggers)
				{
					if (logger.DoLoggingJob())
					{
						_printedOnThisCycle = true;
					}
				}
			}
			if (!_printedOnThisCycle)
			{
				Thread.Sleep(1);
			}
		}
	}

	private bool DoLoggingJob()
	{
		bool result = false;
		HTMLDebugData hTMLDebugData = null;
		lock (_logQueue)
		{
			if (_logQueue.Count > 0)
			{
				hTMLDebugData = _logQueue.Dequeue();
			}
		}
		if (hTMLDebugData != null)
		{
			FileStream fileStream = _fileManager.GetFileStream();
			result = true;
			hTMLDebugData.Print(fileStream, _logFileEncoding);
			if ((hTMLDebugData.Info == HTMLDebugCategory.Error || hTMLDebugData.Info == HTMLDebugCategory.Warning) && _writeErrorsToDifferentFile)
			{
				hTMLDebugData.Print(_fileManager.GetErrorFileStream(), _logFileEncoding, writeToConsole: false);
			}
			_fileManager.CheckForFileSize();
		}
		return result;
	}

	public void Print(string log, HTMLDebugCategory debugInfo = HTMLDebugCategory.General)
	{
		Print(log, debugInfo, printOnGlobal: true);
	}

	public void Print(string log, HTMLDebugCategory debugInfo, bool printOnGlobal)
	{
		if (!LogOnlyErrors || (LogOnlyErrors && debugInfo == HTMLDebugCategory.Error) || (LogOnlyErrors && debugInfo == HTMLDebugCategory.Warning))
		{
			HTMLDebugData item = new HTMLDebugData(log, debugInfo);
			lock (_logQueue)
			{
				_logQueue.Enqueue(item);
			}
			if (printOnGlobal)
			{
				Debug.Print(log);
			}
		}
	}

	public static void FinishAndCloseAll()
	{
		lock (_loggers)
		{
			_running = false;
			_printedOnThisCycle = true;
		}
		while (!_isOver)
		{
		}
	}
}
