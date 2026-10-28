using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil.Cil;

namespace HarmonyLib;

public static class FileLog
{
	private static readonly object fileLock = new object();

	private static bool _logPathInited;

	private static string _logPath;

	public static char indentChar = '\t';

	public static int indentLevel = 0;

	private static List<string> buffer = new List<string>();

	public static StreamWriter LogWriter { get; set; }

	public static string LogPath
	{
		get
		{
			lock (fileLock)
			{
				if (!_logPathInited)
				{
					_logPathInited = true;
					string environmentVariable = Environment.GetEnvironmentVariable("HARMONY_NO_LOG");
					if (!string.IsNullOrEmpty(environmentVariable))
					{
						return null;
					}
					_logPath = Environment.GetEnvironmentVariable("HARMONY_LOG_FILE");
					if (string.IsNullOrEmpty(_logPath))
					{
						string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
						Directory.CreateDirectory(folderPath);
						_logPath = Path.Combine(folderPath, "harmony.log.txt");
					}
				}
				return _logPath;
			}
		}
	}

	private static string IndentString()
	{
		return new string(indentChar, indentLevel);
	}

	private static string CodePos(int offset)
	{
		return $"IL_{offset:X4}: ";
	}

	public static void ChangeIndent(int delta)
	{
		lock (fileLock)
		{
			indentLevel = Math.Max(0, indentLevel + delta);
		}
	}

	public static void LogBuffered(string str)
	{
		lock (fileLock)
		{
			buffer.Add(IndentString() + str);
		}
	}

	public static void LogBuffered(List<string> strings)
	{
		lock (fileLock)
		{
			buffer.AddRange(strings);
		}
	}

	public static List<string> GetBuffer(bool clear)
	{
		lock (fileLock)
		{
			List<string> result = buffer;
			if (clear)
			{
				buffer = new List<string>();
			}
			return result;
		}
	}

	public static void SetBuffer(List<string> buffer)
	{
		lock (fileLock)
		{
			FileLog.buffer = buffer;
		}
	}

	public static void FlushBuffer()
	{
		lock (fileLock)
		{
			if (LogWriter != null)
			{
				foreach (string item in buffer)
				{
					LogWriter.WriteLine(item);
				}
				buffer.Clear();
			}
			else
			{
				if (LogPath == null || buffer.Count <= 0)
				{
					return;
				}
				using FileStream stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				using StreamWriter streamWriter = new StreamWriter(stream);
				foreach (string item2 in buffer)
				{
					streamWriter.WriteLine(item2);
				}
				buffer.Clear();
				return;
			}
		}
	}

	public static void Log(string str)
	{
		lock (fileLock)
		{
			if (LogWriter != null)
			{
				LogWriter.WriteLine(IndentString() + str);
			}
			else
			{
				if (LogPath == null)
				{
					return;
				}
				using FileStream stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				using StreamWriter streamWriter = new StreamWriter(stream);
				streamWriter.WriteLine(IndentString() + str);
				return;
			}
		}
	}

	public static void LogILComment(int codePos, string comment)
	{
		LogBuffered($"{CodePos(codePos)}// {comment}");
	}

	public static void LogIL(int codePos, System.Reflection.Emit.OpCode opcode)
	{
		LogBuffered($"{CodePos(codePos)}{opcode}");
	}

	public static void LogIL(int codePos, System.Reflection.Emit.OpCode opcode, object arg)
	{
		string text = Emitter.FormatOperand(arg);
		string text2 = ((text.Length > 0) ? " " : "");
		string text3 = opcode.ToString();
		if (opcode.FlowControl == System.Reflection.Emit.FlowControl.Branch || opcode.FlowControl == System.Reflection.Emit.FlowControl.Cond_Branch)
		{
			text3 += " =>";
		}
		text3 = text3.PadRight(10);
		LogBuffered($"{CodePos(codePos)}{text3}{text2}{text}");
	}

	internal static void LogIL(VariableDefinition variable)
	{
		LogBuffered(string.Format("{0}Local var {1}: {2}{3}", CodePos(0), variable.Index, variable.VariableType.FullName, variable.IsPinned ? "(pinned)" : ""));
	}

	public static void LogIL(int codePos, Label label)
	{
		LogBuffered(CodePos(codePos) + Emitter.FormatOperand(label));
	}

	public static void LogILBlockBegin(int codePos, ExceptionBlock block)
	{
		switch (block.blockType)
		{
		case ExceptionBlockType.BeginExceptionBlock:
			LogBuffered(".try");
			LogBuffered("{");
			ChangeIndent(1);
			break;
		case ExceptionBlockType.BeginCatchBlock:
			LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
			ChangeIndent(-1);
			LogBuffered("} // end try");
			LogBuffered($".catch {block.catchType}");
			LogBuffered("{");
			ChangeIndent(1);
			break;
		case ExceptionBlockType.BeginExceptFilterBlock:
			LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
			ChangeIndent(-1);
			LogBuffered("} // end try");
			LogBuffered(".filter");
			LogBuffered("{");
			ChangeIndent(1);
			break;
		case ExceptionBlockType.BeginFaultBlock:
			LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
			ChangeIndent(-1);
			LogBuffered("} // end try");
			LogBuffered(".fault");
			LogBuffered("{");
			ChangeIndent(1);
			break;
		case ExceptionBlockType.BeginFinallyBlock:
			LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
			ChangeIndent(-1);
			LogBuffered("} // end try");
			LogBuffered(".finally");
			LogBuffered("{");
			ChangeIndent(1);
			break;
		}
	}

	public static void LogILBlockEnd(int codePos, ExceptionBlock block)
	{
		ExceptionBlockType blockType = block.blockType;
		if (blockType == ExceptionBlockType.EndExceptionBlock)
		{
			LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
			ChangeIndent(-1);
			LogBuffered("} // end handler");
		}
	}

	public static void Debug(string str)
	{
		if (Harmony.DEBUG)
		{
			Log(str);
		}
	}

	public static void Reset()
	{
		lock (fileLock)
		{
			string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{Path.DirectorySeparatorChar}harmony.log.txt";
			File.Delete(path);
		}
	}

	public unsafe static void LogBytes(long ptr, int len)
	{
		lock (fileLock)
		{
			byte* ptr2 = (byte*)ptr;
			string text = "";
			for (int i = 1; i <= len; i++)
			{
				if (text.Length == 0)
				{
					text = "#  ";
				}
				text += $"{*ptr2:X2} ";
				if (i > 1 || len == 1)
				{
					if (i % 8 == 0 || i == len)
					{
						Log(text);
						text = "";
					}
					else if (i % 4 == 0)
					{
						text += " ";
					}
				}
				ptr2++;
			}
			byte[] destination = new byte[len];
			Marshal.Copy((IntPtr)ptr, destination, 0, len);
			MD5 mD = MD5.Create();
			byte[] array = mD.ComputeHash(destination);
			StringBuilder stringBuilder = new StringBuilder();
			for (int j = 0; j < array.Length; j++)
			{
				stringBuilder.Append(array[j].ToString("X2"));
			}
			Log($"HASH: {stringBuilder}");
		}
	}
}
