using System;
using System.IO;
using System.Net;
using System.Text;

namespace TaleWorlds.Library
{
	// Token: 0x0200005F RID: 95
	internal class HTMLDebugData
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060002B2 RID: 690 RVA: 0x000081D8 File Offset: 0x000063D8
		// (set) Token: 0x060002B3 RID: 691 RVA: 0x000081E0 File Offset: 0x000063E0
		internal HTMLDebugCategory Info { get; private set; }

		// Token: 0x060002B4 RID: 692 RVA: 0x000081EC File Offset: 0x000063EC
		internal HTMLDebugData(string log, HTMLDebugCategory info)
		{
			this._log = log;
			this.Info = info;
			this._currentTime = DateTime.Now.ToString("yyyy/M/d h:mm:ss.fff");
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060002B5 RID: 693 RVA: 0x00008228 File Offset: 0x00006428
		private string Color
		{
			get
			{
				string result = "000000";
				switch (this.Info)
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

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060002B6 RID: 694 RVA: 0x000082AC File Offset: 0x000064AC
		private ConsoleColor ConsoleColor
		{
			get
			{
				ConsoleColor result = ConsoleColor.Green;
				HTMLDebugCategory info = this.Info;
				if (info != HTMLDebugCategory.Warning)
				{
					if (info == HTMLDebugCategory.Error)
					{
						result = ConsoleColor.Red;
					}
				}
				else
				{
					result = ConsoleColor.Yellow;
				}
				return result;
			}
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x000082D4 File Offset: 0x000064D4
		internal void Print(FileStream fileStream, Encoding encoding, bool writeToConsole = true)
		{
			if (writeToConsole)
			{
				Console.ForegroundColor = this.ConsoleColor;
				Console.WriteLine(this._log);
				Console.ForegroundColor = this.ConsoleColor;
			}
			int byteCount = encoding.GetByteCount("</table>");
			string color = this.Color;
			string s = string.Concat(new string[]
			{
				"<tr>",
				this.TableCell(this._log, color).Replace("\n", "<br/>"),
				this.TableCell(this.Info.ToString(), color),
				this.TableCell(this._currentTime, color),
				"</tr></table>"
			});
			byte[] bytes = encoding.GetBytes(s);
			fileStream.Seek((long)(-(long)byteCount), SeekOrigin.End);
			fileStream.Write(bytes, 0, bytes.Length);
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x000083A0 File Offset: 0x000065A0
		private string TableCell(string innerText, string color)
		{
			return string.Concat(new string[]
			{
				"<td><font color='#",
				color,
				"'>",
				WebUtility.HtmlEncode(innerText),
				"</font></td><td>"
			});
		}

		// Token: 0x0400011B RID: 283
		private string _log;

		// Token: 0x0400011D RID: 285
		private string _currentTime;
	}
}
