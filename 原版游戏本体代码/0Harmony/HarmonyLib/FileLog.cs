using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil.Cil;

namespace HarmonyLib
{
	/// <summary>A file log for debugging</summary>
	// Token: 0x020001C5 RID: 453
	public static class FileLog
	{
		/// <summary>Set this to make Harmony write its log content to this stream</summary>
		// Token: 0x170001F7 RID: 503
		// (get) Token: 0x060007E4 RID: 2020 RVA: 0x00019F34 File Offset: 0x00018134
		// (set) Token: 0x060007E5 RID: 2021 RVA: 0x00019F3B File Offset: 0x0001813B
		public static StreamWriter LogWriter { get; set; }

		/// <summary>Full pathname of the log file, defaults to a file called <c>harmony.log.txt</c> on your Desktop</summary>
		// Token: 0x170001F8 RID: 504
		// (get) Token: 0x060007E6 RID: 2022 RVA: 0x00019F44 File Offset: 0x00018144
		public static string LogPath
		{
			get
			{
				object obj = FileLog.fileLock;
				string logPath;
				lock (obj)
				{
					if (!FileLog._logPathInited)
					{
						FileLog._logPathInited = true;
						string noLog = Environment.GetEnvironmentVariable("HARMONY_NO_LOG");
						if (!string.IsNullOrEmpty(noLog))
						{
							return null;
						}
						FileLog._logPath = Environment.GetEnvironmentVariable("HARMONY_LOG_FILE");
						if (string.IsNullOrEmpty(FileLog._logPath))
						{
							string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
							Directory.CreateDirectory(desktopPath);
							FileLog._logPath = Path.Combine(desktopPath, "harmony.log.txt");
						}
					}
					logPath = FileLog._logPath;
				}
				return logPath;
			}
		}

		// Token: 0x060007E7 RID: 2023 RVA: 0x00019FE4 File Offset: 0x000181E4
		private static string IndentString()
		{
			return new string(FileLog.indentChar, FileLog.indentLevel);
		}

		// Token: 0x060007E8 RID: 2024 RVA: 0x00003C93 File Offset: 0x00001E93
		private static string CodePos(int offset)
		{
			return string.Format("IL_{0:X4}: ", offset);
		}

		/// <summary>Changes the indentation level</summary>
		/// <param name="delta">The value to add to the indentation level</param>
		// Token: 0x060007E9 RID: 2025 RVA: 0x00019FF8 File Offset: 0x000181F8
		public static void ChangeIndent(int delta)
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				FileLog.indentLevel = Math.Max(0, FileLog.indentLevel + delta);
			}
		}

		/// <summary>Log a string in a buffered way. Use this method only if you are sure that FlushBuffer will be called
		///  or else logging information is incomplete in case of a crash</summary>
		/// <param name="str">The string to log</param>
		// Token: 0x060007EA RID: 2026 RVA: 0x0001A044 File Offset: 0x00018244
		public static void LogBuffered(string str)
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				FileLog.buffer.Add(FileLog.IndentString() + str);
			}
		}

		/// <summary>Logs a list of string in a buffered way. Use this method only if you are sure that FlushBuffer will be called
		///  or else logging information is incomplete in case of a crash</summary>
		/// <param name="strings">A list of strings to log (they will not be re-indented)</param>
		// Token: 0x060007EB RID: 2027 RVA: 0x0001A094 File Offset: 0x00018294
		public static void LogBuffered(List<string> strings)
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				FileLog.buffer.AddRange(strings);
			}
		}

		/// <summary>Returns the log buffer and optionally empties it</summary>
		/// <param name="clear">True to empty the buffer</param>
		/// <returns>The buffer.</returns>
		// Token: 0x060007EC RID: 2028 RVA: 0x0001A0D8 File Offset: 0x000182D8
		public static List<string> GetBuffer(bool clear)
		{
			object obj = FileLog.fileLock;
			List<string> result2;
			lock (obj)
			{
				List<string> result = FileLog.buffer;
				if (clear)
				{
					FileLog.buffer = new List<string>();
				}
				result2 = result;
			}
			return result2;
		}

		/// <summary>Replaces the buffer with new lines</summary>
		/// <param name="buffer">The lines to store</param>
		// Token: 0x060007ED RID: 2029 RVA: 0x0001A128 File Offset: 0x00018328
		public static void SetBuffer(List<string> buffer)
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				FileLog.buffer = buffer;
			}
		}

		/// <summary>Flushes the log buffer to disk (use in combination with LogBuffered)</summary>
		// Token: 0x060007EE RID: 2030 RVA: 0x0001A168 File Offset: 0x00018368
		public static void FlushBuffer()
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				if (FileLog.LogWriter != null)
				{
					foreach (string str in FileLog.buffer)
					{
						FileLog.LogWriter.WriteLine(str);
					}
					FileLog.buffer.Clear();
				}
				else if (FileLog.LogPath != null)
				{
					if (FileLog.buffer.Count > 0)
					{
						using (FileStream fs = new FileStream(FileLog.LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
						{
							using (StreamWriter writer = new StreamWriter(fs))
							{
								foreach (string str2 in FileLog.buffer)
								{
									writer.WriteLine(str2);
								}
								FileLog.buffer.Clear();
							}
						}
					}
				}
			}
		}

		/// <summary>Logs a string directly to disk to avoid losing information in case of a crash</summary>
		/// <param name="str">The string to log.</param>
		// Token: 0x060007EF RID: 2031 RVA: 0x0001A2B0 File Offset: 0x000184B0
		public static void Log(string str)
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				if (FileLog.LogWriter != null)
				{
					FileLog.LogWriter.WriteLine(FileLog.IndentString() + str);
				}
				else if (FileLog.LogPath != null)
				{
					using (FileStream fs = new FileStream(FileLog.LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
					{
						using (StreamWriter writer = new StreamWriter(fs))
						{
							writer.WriteLine(FileLog.IndentString() + str);
						}
					}
				}
			}
		}

		/// <summary>Logs an inline comment at the specified code position</summary>
		/// <remarks>This method formats the comment with the code position and logs it.</remarks>
		/// <param name="codePos">The position in the code where the comment should be logged.</param>
		/// <param name="comment">The comment text to log. Cannot be null or empty.</param>
		// Token: 0x060007F0 RID: 2032 RVA: 0x0001A364 File Offset: 0x00018564
		public static void LogILComment(int codePos, string comment)
		{
			FileLog.LogBuffered(string.Format("{0}// {1}", FileLog.CodePos(codePos), comment));
		}

		/// <summary>Logs the specified Intermediate Language (IL) operation code and its position in the code stream</summary>
		/// <remarks>This method formats the IL operation code and its position into a string and logs it.</remarks>
		/// <param name="codePos">The position of the IL operation code in the code stream.</param>
		/// <param name="opcode">The IL operation code to log.</param>
		// Token: 0x060007F1 RID: 2033 RVA: 0x0001A37C File Offset: 0x0001857C
		public static void LogIL(int codePos, System.Reflection.Emit.OpCode opcode)
		{
			FileLog.LogBuffered(string.Format("{0}{1}", FileLog.CodePos(codePos), opcode));
		}

		/// <summary>Logs information about an Intermediate Language (IL) instruction, including its position, opcode, and operand</summary>
		/// <remarks>This method formats and logs details about an IL instruction for debugging or analysis purposes. 
		/// The logged output includes the instruction's position, opcode, and operand (if any).</remarks>
		/// <param name="codePos">The position of the IL instruction within the method body.</param>
		/// <param name="opcode">The <see cref="T:System.Reflection.Emit.OpCode" /> representing the operation to be performed.</param>
		/// <param name="arg">The operand associated with the IL instruction, or <see langword="null" /> if the instruction has no operand.</param>
		// Token: 0x060007F2 RID: 2034 RVA: 0x0001A39C File Offset: 0x0001859C
		public static void LogIL(int codePos, System.Reflection.Emit.OpCode opcode, object arg)
		{
			string argStr = Emitter.FormatOperand(arg);
			string space = ((argStr.Length > 0) ? " " : "");
			string opcodeName = opcode.ToString();
			if (opcode.FlowControl == System.Reflection.Emit.FlowControl.Branch || opcode.FlowControl == System.Reflection.Emit.FlowControl.Cond_Branch)
			{
				opcodeName += " =>";
			}
			opcodeName = opcodeName.PadRight(10);
			FileLog.LogBuffered(string.Format("{0}{1}{2}{3}", new object[]
			{
				FileLog.CodePos(codePos),
				opcodeName,
				space,
				argStr
			}));
		}

		/// <summary>Logs information about a local variable in Intermediate Language (IL) code</summary>
		/// <remarks>The logged information includes the variable's index, type, and whether it is pinned.</remarks>
		/// <param name="variable">The <see cref="T:Mono.Cecil.Cil.VariableDefinition" /> representing the local variable to log. Must not be <see langword="null" />.</param>
		// Token: 0x060007F3 RID: 2035 RVA: 0x0001A428 File Offset: 0x00018628
		internal static void LogIL(VariableDefinition variable)
		{
			FileLog.LogBuffered(string.Format("{0}Local var {1}: {2}{3}", new object[]
			{
				FileLog.CodePos(0),
				variable.Index,
				variable.VariableType.FullName,
				variable.IsPinned ? "(pinned)" : ""
			}));
		}

		/// <summary>Logs the intermediate language (IL) code at the specified position with the given label operand</summary>
		/// <remarks>Formats and logs the IL code position and label operand for detailed IL tracking or debugging.</remarks>
		/// <param name="codePos">The position in the IL code to log.</param>
		/// <param name="label">The label operand associated with the IL code to log.</param>
		// Token: 0x060007F4 RID: 2036 RVA: 0x0001A486 File Offset: 0x00018686
		public static void LogIL(int codePos, Label label)
		{
			FileLog.LogBuffered(FileLog.CodePos(codePos) + Emitter.FormatOperand(label));
		}

		/// <summary>Logs the beginning of an intermediate language (IL) exception handling block</summary>
		/// <remarks>Logs the start of an exception handling block (e.g., <c>.try</c>, <c>.catch</c>, <c>.finally</c>, <c>.fault</c>),
		/// adjusts indentation, and simulates a <c>LEAVE</c> opcode for consistency.</remarks>
		/// <param name="codePos">The position of the IL code where the block begins.</param>
		/// <param name="block">The <see cref="T:HarmonyLib.ExceptionBlock" /> representing the type of exception handling block to log. This includes
		/// information about the block type (e.g., try, catch, finally) and any associated metadata.</param>
		// Token: 0x060007F5 RID: 2037 RVA: 0x0001A4A4 File Offset: 0x000186A4
		public static void LogILBlockBegin(int codePos, ExceptionBlock block)
		{
			switch (block.blockType)
			{
			case ExceptionBlockType.BeginExceptionBlock:
				FileLog.LogBuffered(".try");
				FileLog.LogBuffered("{");
				FileLog.ChangeIndent(1);
				return;
			case ExceptionBlockType.BeginCatchBlock:
			{
				FileLog.LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
				FileLog.ChangeIndent(-1);
				FileLog.LogBuffered("} // end try");
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 1);
				defaultInterpolatedStringHandler.AppendLiteral(".catch ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(block.catchType);
				FileLog.LogBuffered(defaultInterpolatedStringHandler.ToStringAndClear());
				FileLog.LogBuffered("{");
				FileLog.ChangeIndent(1);
				return;
			}
			case ExceptionBlockType.BeginExceptFilterBlock:
				FileLog.LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
				FileLog.ChangeIndent(-1);
				FileLog.LogBuffered("} // end try");
				FileLog.LogBuffered(".filter");
				FileLog.LogBuffered("{");
				FileLog.ChangeIndent(1);
				return;
			case ExceptionBlockType.BeginFaultBlock:
				FileLog.LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
				FileLog.ChangeIndent(-1);
				FileLog.LogBuffered("} // end try");
				FileLog.LogBuffered(".fault");
				FileLog.LogBuffered("{");
				FileLog.ChangeIndent(1);
				return;
			case ExceptionBlockType.BeginFinallyBlock:
				FileLog.LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
				FileLog.ChangeIndent(-1);
				FileLog.LogBuffered("} // end try");
				FileLog.LogBuffered(".finally");
				FileLog.LogBuffered("{");
				FileLog.ChangeIndent(1);
				return;
			default:
				return;
			}
		}

		/// <summary>Logs the end of an intermediate language (IL) exception block</summary>
		/// <remarks>This method handles the logging of specific types of exception blocks, such as the end of a try-catch or 
		/// similar constructs. It adjusts the indentation level and outputs relevant information about the block's conclusion.</remarks>
		/// <param name="codePos">The position in the IL code where the block ends.</param>
		/// <param name="block">The exception block to log. Must have a valid block type.</param>
		// Token: 0x060007F6 RID: 2038 RVA: 0x0001A600 File Offset: 0x00018800
		public static void LogILBlockEnd(int codePos, ExceptionBlock block)
		{
			ExceptionBlockType blockType = block.blockType;
			if (blockType == ExceptionBlockType.EndExceptionBlock)
			{
				FileLog.LogIL(codePos, System.Reflection.Emit.OpCodes.Leave, new LeaveTry());
				FileLog.ChangeIndent(-1);
				FileLog.LogBuffered("} // end handler");
			}
		}

		/// <summary>Log a string directly to disk if Harmony.DEBUG is true. Slower method that prevents missing information in case of a crash</summary>
		/// <param name="str">The string to log.</param>
		// Token: 0x060007F7 RID: 2039 RVA: 0x0001A638 File Offset: 0x00018838
		public static void Debug(string str)
		{
			if (Harmony.DEBUG)
			{
				FileLog.Log(str);
			}
		}

		/// <summary>Resets and deletes the log</summary>
		// Token: 0x060007F8 RID: 2040 RVA: 0x0001A648 File Offset: 0x00018848
		public static void Reset()
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
				defaultInterpolatedStringHandler.AppendFormatted(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
				defaultInterpolatedStringHandler.AppendFormatted<char>(Path.DirectorySeparatorChar);
				defaultInterpolatedStringHandler.AppendLiteral("harmony.log.txt");
				string path = defaultInterpolatedStringHandler.ToStringAndClear();
				File.Delete(path);
			}
		}

		/// <summary>Logs some bytes as hex values</summary>
		/// <param name="ptr">The pointer to some memory</param>
		/// <param name="len">The length of bytes to log</param>
		// Token: 0x060007F9 RID: 2041 RVA: 0x0001A6C0 File Offset: 0x000188C0
		public unsafe static void LogBytes(long ptr, int len)
		{
			object obj = FileLog.fileLock;
			lock (obj)
			{
				byte* p = ptr;
				string s = "";
				for (int i = 1; i <= len; i++)
				{
					if (s.Length == 0)
					{
						s = "#  ";
					}
					string str = s;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
					defaultInterpolatedStringHandler.AppendFormatted<byte>(*p, "X2");
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					s = str + defaultInterpolatedStringHandler.ToStringAndClear();
					if (i > 1 || len == 1)
					{
						if (i % 8 == 0 || i == len)
						{
							FileLog.Log(s);
							s = "";
						}
						else if (i % 4 == 0)
						{
							s += " ";
						}
					}
					p++;
				}
				byte[] arr = new byte[len];
				Marshal.Copy((IntPtr)ptr, arr, 0, len);
				MD5 md5Hash = MD5.Create();
				byte[] hash = md5Hash.ComputeHash(arr);
				StringBuilder sBuilder = new StringBuilder();
				for (int j = 0; j < hash.Length; j++)
				{
					sBuilder.Append(hash[j].ToString("X2"));
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(6, 1);
				defaultInterpolatedStringHandler2.AppendLiteral("HASH: ");
				defaultInterpolatedStringHandler2.AppendFormatted<StringBuilder>(sBuilder);
				FileLog.Log(defaultInterpolatedStringHandler2.ToStringAndClear());
			}
		}

		// Token: 0x040002B5 RID: 693
		private static readonly object fileLock = new object();

		// Token: 0x040002B6 RID: 694
		private static bool _logPathInited;

		// Token: 0x040002B7 RID: 695
		private static string _logPath;

		/// <summary>The indent character. The default is <c>tab</c></summary>
		// Token: 0x040002B9 RID: 697
		public static char indentChar = '\t';

		/// <summary>The current indent level</summary>
		// Token: 0x040002BA RID: 698
		public static int indentLevel = 0;

		// Token: 0x040002BB RID: 699
		private static List<string> buffer = new List<string>();
	}
}
