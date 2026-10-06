using System;
using System.IO;
using System.Net;
using System.Text;

namespace TaleWorlds.Library;

internal class HTMLDebugData
{
	private string _log;

	private string _currentTime;

	internal HTMLDebugCategory Info { get; private set; }

	private string Color
	{
		get
		{
			string result = "000000";
			switch (Info)
			{
			case HTMLDebugCategory.General:
				result = "000000";
				break;
			case HTMLDebugCategory.Connection:
				result = "FF00FF";
				break;
			case HTMLDebugCategory.IncomingMessage:
				result = "EE8800";
				break;
			case HTMLDebugCategory.OutgoingMessage:
				result = "AA6600";
				break;
			case HTMLDebugCategory.Database:
				result = "00008B";
				break;
			case HTMLDebugCategory.Warning:
				result = "0000FF";
				break;
			case HTMLDebugCategory.Error:
				result = "FF0000";
				break;
			case HTMLDebugCategory.Other:
				result = "000000";
				break;
			}
			return result;
		}
	}

	private ConsoleColor ConsoleColor
	{
		get
		{
			ConsoleColor result = ConsoleColor.Green;
			switch (Info)
			{
			case HTMLDebugCategory.Error:
				result = ConsoleColor.Red;
				break;
			case HTMLDebugCategory.Warning:
				result = ConsoleColor.Yellow;
				break;
			}
			return result;
		}
	}

	internal HTMLDebugData(string log, HTMLDebugCategory info)
	{
		_log = log;
		Info = info;
		_currentTime = DateTime.Now.ToString("yyyy/M/d h:mm:ss.fff");
	}

	internal void Print(FileStream fileStream, Encoding encoding, bool writeToConsole = true)
	{
		if (writeToConsole)
		{
			Console.ForegroundColor = ConsoleColor;
			Console.WriteLine(_log);
			Console.ForegroundColor = ConsoleColor;
		}
		int byteCount = encoding.GetByteCount("</table>");
		string color = Color;
		string s = "<tr>" + TableCell(_log, color).Replace("\n", "<br/>") + TableCell(Info.ToString(), color) + TableCell(_currentTime, color) + "</tr></table>";
		byte[] bytes = encoding.GetBytes(s);
		fileStream.Seek(-byteCount, SeekOrigin.End);
		fileStream.Write(bytes, 0, bytes.Length);
	}

	private string TableCell(string innerText, string color)
	{
		return "<td><font color='#" + color + "'>" + WebUtility.HtmlEncode(innerText) + "</font></td><td>";
	}
}
