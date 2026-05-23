using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000436 RID: 1078
	internal class PdbFile
	{
		// Token: 0x0600178A RID: 6026 RVA: 0x00002B15 File Offset: 0x00000D15
		private PdbFile()
		{
		}

		// Token: 0x0600178B RID: 6027 RVA: 0x00048F4C File Offset: 0x0004714C
		private static void LoadInjectedSourceInformation(BitAccess bits, out Guid doctype, out Guid language, out Guid vendor, out Guid checksumAlgo, out byte[] checksum)
		{
			checksum = null;
			bits.ReadGuid(out language);
			bits.ReadGuid(out vendor);
			bits.ReadGuid(out doctype);
			bits.ReadGuid(out checksumAlgo);
			int checksumSize;
			bits.ReadInt32(out checksumSize);
			int injectedSourceSize;
			bits.ReadInt32(out injectedSourceSize);
			if (checksumSize > 0)
			{
				checksum = new byte[checksumSize];
				bits.ReadBytes(checksum);
			}
		}

		// Token: 0x0600178C RID: 6028 RVA: 0x00048FA0 File Offset: 0x000471A0
		private static Dictionary<string, int> LoadNameIndex(BitAccess bits, out int age, out Guid guid)
		{
			Dictionary<string, int> result = new Dictionary<string, int>();
			int ver;
			bits.ReadInt32(out ver);
			int sig;
			bits.ReadInt32(out sig);
			bits.ReadInt32(out age);
			bits.ReadGuid(out guid);
			int buf;
			bits.ReadInt32(out buf);
			int beg = bits.Position;
			int nxt = bits.Position + buf;
			bits.Position = nxt;
			int cnt;
			bits.ReadInt32(out cnt);
			int max;
			bits.ReadInt32(out max);
			BitSet present = new BitSet(bits);
			new BitSet(bits);
			int i = 0;
			for (int j = 0; j < max; j++)
			{
				if (present.IsSet(j))
				{
					int ns;
					bits.ReadInt32(out ns);
					int ni;
					bits.ReadInt32(out ni);
					int saved = bits.Position;
					bits.Position = beg + ns;
					string name;
					bits.ReadCString(out name);
					bits.Position = saved;
					result.Add(name.ToUpperInvariant(), ni);
					i++;
				}
			}
			if (i != cnt)
			{
				throw new PdbDebugException("Count mismatch. ({0} != {1})", new object[] { i, cnt });
			}
			return result;
		}

		// Token: 0x0600178D RID: 6029 RVA: 0x000490A8 File Offset: 0x000472A8
		private static IntHashTable LoadNameStream(BitAccess bits)
		{
			IntHashTable ht = new IntHashTable();
			uint sig;
			bits.ReadUInt32(out sig);
			int ver;
			bits.ReadInt32(out ver);
			int buf;
			bits.ReadInt32(out buf);
			if (sig != 4026462206U || ver != 1)
			{
				throw new PdbDebugException("Unsupported Name Stream version. (sig={0:x8}, ver={1})", new object[] { sig, ver });
			}
			int beg = bits.Position;
			int nxt = bits.Position + buf;
			bits.Position = nxt;
			int siz;
			bits.ReadInt32(out siz);
			nxt = bits.Position;
			for (int i = 0; i < siz; i++)
			{
				int ni;
				bits.ReadInt32(out ni);
				if (ni != 0)
				{
					int saved = bits.Position;
					bits.Position = beg + ni;
					string name;
					bits.ReadCString(out name);
					bits.Position = saved;
					ht.Add(ni, name);
				}
			}
			bits.Position = nxt;
			return ht;
		}

		// Token: 0x0600178E RID: 6030 RVA: 0x00049180 File Offset: 0x00047380
		private static int FindFunction(PdbFunction[] funcs, ushort sec, uint off)
		{
			PdbFunction match = new PdbFunction
			{
				segment = (uint)sec,
				address = off
			};
			return Array.BinarySearch(funcs, match, PdbFunction.byAddress);
		}

		// Token: 0x0600178F RID: 6031 RVA: 0x000491B0 File Offset: 0x000473B0
		private static void LoadManagedLines(PdbFunction[] funcs, IntHashTable names, BitAccess bits, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader, uint limit, Dictionary<string, PdbSource> sourceCache)
		{
			Array.Sort(funcs, PdbFunction.byAddressAndToken);
			int begin = bits.Position;
			IntHashTable checks = PdbFile.ReadSourceFileInfo(bits, limit, names, dir, nameIndex, reader, sourceCache);
			bits.Position = begin;
			while ((long)bits.Position < (long)((ulong)limit))
			{
				int sig;
				bits.ReadInt32(out sig);
				int siz;
				bits.ReadInt32(out siz);
				int endSym = bits.Position + siz;
				if (sig == 242)
				{
					CV_LineSection sec;
					bits.ReadUInt32(out sec.off);
					bits.ReadUInt16(out sec.sec);
					bits.ReadUInt16(out sec.flags);
					bits.ReadUInt32(out sec.cod);
					int funcIndex = PdbFile.FindFunction(funcs, sec.sec, sec.off);
					if (funcIndex >= 0)
					{
						PdbFunction func = funcs[funcIndex];
						if (func.lines == null)
						{
							while (funcIndex > 0)
							{
								PdbFunction f = funcs[funcIndex - 1];
								if (f.lines != null || f.segment != (uint)sec.sec || f.address != sec.off)
								{
									break;
								}
								func = f;
								funcIndex--;
							}
						}
						else
						{
							while (funcIndex < funcs.Length - 1 && func.lines != null)
							{
								PdbFunction f2 = funcs[funcIndex + 1];
								if (f2.segment != (uint)sec.sec || f2.address != sec.off)
								{
									break;
								}
								func = f2;
								funcIndex++;
							}
						}
						if (func.lines == null)
						{
							int begSym = bits.Position;
							int blocks = 0;
							while (bits.Position < endSym)
							{
								CV_SourceFile file;
								bits.ReadUInt32(out file.index);
								bits.ReadUInt32(out file.count);
								bits.ReadUInt32(out file.linsiz);
								int linsiz = (int)(file.count * (8U + (((sec.flags & 1) != 0) ? 4U : 0U)));
								bits.Position += linsiz;
								blocks++;
							}
							func.lines = new PdbLines[blocks];
							int block = 0;
							bits.Position = begSym;
							while (bits.Position < endSym)
							{
								CV_SourceFile file2;
								bits.ReadUInt32(out file2.index);
								bits.ReadUInt32(out file2.count);
								bits.ReadUInt32(out file2.linsiz);
								PdbSource pdbSource = (PdbSource)checks[(int)file2.index];
								if (pdbSource.language.Equals(PdbFile.BasicLanguageGuid))
								{
									func.AdjustVisualBasicScopes();
								}
								PdbLines tmp = new PdbLines(pdbSource, file2.count);
								func.lines[block++] = tmp;
								PdbLine[] lines = tmp.lines;
								int plin = bits.Position;
								int pcol = bits.Position + (int)(8U * file2.count);
								int i = 0;
								while ((long)i < (long)((ulong)file2.count))
								{
									CV_Column column = default(CV_Column);
									bits.Position = plin + 8 * i;
									CV_Line line;
									bits.ReadUInt32(out line.offset);
									bits.ReadUInt32(out line.flags);
									uint lineBegin = line.flags & 16777215U;
									uint delta = (line.flags & 2130706432U) >> 24;
									if ((sec.flags & 1) != 0)
									{
										bits.Position = pcol + 4 * i;
										bits.ReadUInt16(out column.offColumnStart);
										bits.ReadUInt16(out column.offColumnEnd);
									}
									lines[i] = new PdbLine(line.offset, lineBegin, column.offColumnStart, lineBegin + delta, column.offColumnEnd);
									i++;
								}
							}
						}
					}
				}
				bits.Position = endSym;
			}
		}

		// Token: 0x06001790 RID: 6032 RVA: 0x00049514 File Offset: 0x00047714
		private static void LoadFuncsFromDbiModule(BitAccess bits, DbiModuleInfo info, IntHashTable names, List<PdbFunction> funcList, bool readStrings, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader, Dictionary<string, PdbSource> sourceCache)
		{
			bits.Position = 0;
			int sig;
			bits.ReadInt32(out sig);
			if (sig != 4)
			{
				throw new PdbDebugException("Invalid signature. (sig={0})", new object[] { sig });
			}
			bits.Position = 4;
			PdbFunction[] funcs = PdbFunction.LoadManagedFunctions(bits, (uint)info.cbSyms, readStrings);
			if (funcs != null)
			{
				bits.Position = info.cbSyms + info.cbOldLines;
				PdbFile.LoadManagedLines(funcs, names, bits, dir, nameIndex, reader, (uint)(info.cbSyms + info.cbOldLines + info.cbLines), sourceCache);
				for (int i = 0; i < funcs.Length; i++)
				{
					funcList.Add(funcs[i]);
				}
			}
		}

		// Token: 0x06001791 RID: 6033 RVA: 0x000495B8 File Offset: 0x000477B8
		private static void LoadDbiStream(BitAccess bits, out DbiModuleInfo[] modules, out DbiDbgHdr header, bool readStrings)
		{
			DbiHeader dh = new DbiHeader(bits);
			header = default(DbiDbgHdr);
			List<DbiModuleInfo> modList = new List<DbiModuleInfo>();
			int end = bits.Position + dh.gpmodiSize;
			while (bits.Position < end)
			{
				DbiModuleInfo mod = new DbiModuleInfo(bits, readStrings);
				modList.Add(mod);
			}
			if (bits.Position != end)
			{
				throw new PdbDebugException("Error reading DBI stream, pos={0} != {1}", new object[] { bits.Position, end });
			}
			if (modList.Count > 0)
			{
				modules = modList.ToArray();
			}
			else
			{
				modules = null;
			}
			bits.Position += dh.secconSize;
			bits.Position += dh.secmapSize;
			bits.Position += dh.filinfSize;
			bits.Position += dh.tsmapSize;
			bits.Position += dh.ecinfoSize;
			end = bits.Position + dh.dbghdrSize;
			if (dh.dbghdrSize > 0)
			{
				header = new DbiDbgHdr(bits);
			}
			bits.Position = end;
		}

		// Token: 0x06001792 RID: 6034 RVA: 0x000496D4 File Offset: 0x000478D4
		internal static PdbInfo LoadFunctions(Stream read)
		{
			PdbInfo pdbInfo = new PdbInfo();
			pdbInfo.TokenToSourceMapping = new Dictionary<uint, PdbTokenLine>();
			BitAccess bits = new BitAccess(65536);
			PdbFileHeader head = new PdbFileHeader(read, bits);
			PdbReader reader = new PdbReader(read, head.pageSize);
			MsfDirectory dir = new MsfDirectory(reader, head, bits);
			DbiModuleInfo[] modules = null;
			Dictionary<string, PdbSource> sourceCache = new Dictionary<string, PdbSource>();
			dir.streams[1].Read(reader, bits);
			Dictionary<string, int> nameIndex = PdbFile.LoadNameIndex(bits, out pdbInfo.Age, out pdbInfo.Guid);
			int nameStream;
			if (!nameIndex.TryGetValue("/NAMES", out nameStream))
			{
				throw new PdbException("Could not find the '/NAMES' stream: the PDB file may be a public symbol file instead of a private symbol file", new object[0]);
			}
			dir.streams[nameStream].Read(reader, bits);
			IntHashTable names = PdbFile.LoadNameStream(bits);
			int srcsrvStream;
			if (!nameIndex.TryGetValue("SRCSRV", out srcsrvStream))
			{
				pdbInfo.SourceServerData = string.Empty;
			}
			else
			{
				DataStream dataStream2 = dir.streams[srcsrvStream];
				byte[] bytes = new byte[dataStream2.contentSize];
				dataStream2.Read(reader, bits);
				pdbInfo.SourceServerData = bits.ReadBString(bytes.Length);
			}
			int sourceLinkStream;
			if (nameIndex.TryGetValue("SOURCELINK", out sourceLinkStream))
			{
				DataStream dataStream = dir.streams[sourceLinkStream];
				pdbInfo.SourceLinkData = new byte[dataStream.contentSize];
				dataStream.Read(reader, bits);
				bits.ReadBytes(pdbInfo.SourceLinkData);
			}
			dir.streams[3].Read(reader, bits);
			DbiDbgHdr header;
			PdbFile.LoadDbiStream(bits, out modules, out header, true);
			List<PdbFunction> funcList = new List<PdbFunction>();
			if (modules != null)
			{
				foreach (DbiModuleInfo module in modules)
				{
					if (module.stream > 0)
					{
						dir.streams[(int)module.stream].Read(reader, bits);
						if (module.moduleName == "TokenSourceLineInfo")
						{
							PdbFile.LoadTokenToSourceInfo(bits, module, names, dir, nameIndex, reader, pdbInfo.TokenToSourceMapping, sourceCache);
						}
						else
						{
							PdbFile.LoadFuncsFromDbiModule(bits, module, names, funcList, true, dir, nameIndex, reader, sourceCache);
						}
					}
				}
			}
			PdbFunction[] funcs = funcList.ToArray();
			if (header.snTokenRidMap != 0 && header.snTokenRidMap != 65535)
			{
				dir.streams[(int)header.snTokenRidMap].Read(reader, bits);
				uint[] ridMap = new uint[dir.streams[(int)header.snTokenRidMap].Length / 4];
				bits.ReadUInt32(ridMap);
				foreach (PdbFunction func in funcs)
				{
					func.token = 100663296U | ridMap[(int)(func.token & 16777215U)];
				}
			}
			Array.Sort(funcs, PdbFunction.byAddressAndToken);
			pdbInfo.Functions = funcs;
			return pdbInfo;
		}

		// Token: 0x06001793 RID: 6035 RVA: 0x00049960 File Offset: 0x00047B60
		private static void LoadTokenToSourceInfo(BitAccess bits, DbiModuleInfo module, IntHashTable names, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader, Dictionary<uint, PdbTokenLine> tokenToSourceMapping, Dictionary<string, PdbSource> sourceCache)
		{
			bits.Position = 0;
			int sig;
			bits.ReadInt32(out sig);
			if (sig != 4)
			{
				throw new PdbDebugException("Invalid signature. (sig={0})", new object[] { sig });
			}
			bits.Position = 4;
			while (bits.Position < module.cbSyms)
			{
				ushort siz;
				bits.ReadUInt16(out siz);
				int star = bits.Position;
				int stop = bits.Position + (int)siz;
				bits.Position = star;
				ushort rec;
				bits.ReadUInt16(out rec);
				SYM sym = (SYM)rec;
				if (sym != SYM.S_END)
				{
					if (sym == SYM.S_OEM)
					{
						OemSymbol oem;
						bits.ReadGuid(out oem.idOem);
						bits.ReadUInt32(out oem.typind);
						if (!(oem.idOem == PdbFunction.msilMetaData))
						{
							throw new PdbDebugException("OEM section: guid={0} ti={1}", new object[] { oem.idOem, oem.typind });
						}
						if (bits.ReadString() == "TSLI")
						{
							uint token;
							bits.ReadUInt32(out token);
							uint file_id;
							bits.ReadUInt32(out file_id);
							uint line;
							bits.ReadUInt32(out line);
							uint column;
							bits.ReadUInt32(out column);
							uint endLine;
							bits.ReadUInt32(out endLine);
							uint endColumn;
							bits.ReadUInt32(out endColumn);
							PdbTokenLine tokenLine;
							if (!tokenToSourceMapping.TryGetValue(token, out tokenLine))
							{
								tokenToSourceMapping.Add(token, new PdbTokenLine(token, file_id, line, column, endLine, endColumn));
							}
							else
							{
								while (tokenLine.nextLine != null)
								{
									tokenLine = tokenLine.nextLine;
								}
								tokenLine.nextLine = new PdbTokenLine(token, file_id, line, column, endLine, endColumn);
							}
						}
						bits.Position = stop;
					}
					else
					{
						bits.Position = stop;
					}
				}
				else
				{
					bits.Position = stop;
				}
			}
			bits.Position = module.cbSyms + module.cbOldLines;
			int limit = module.cbSyms + module.cbOldLines + module.cbLines;
			IntHashTable sourceFiles = PdbFile.ReadSourceFileInfo(bits, (uint)limit, names, dir, nameIndex, reader, sourceCache);
			foreach (PdbTokenLine tokenLine2 in tokenToSourceMapping.Values)
			{
				tokenLine2.sourceFile = (PdbSource)sourceFiles[(int)tokenLine2.file_id];
			}
		}

		// Token: 0x06001794 RID: 6036 RVA: 0x00049BA0 File Offset: 0x00047DA0
		private static IntHashTable ReadSourceFileInfo(BitAccess bits, uint limit, IntHashTable names, MsfDirectory dir, Dictionary<string, int> nameIndex, PdbReader reader, Dictionary<string, PdbSource> sourceCache)
		{
			IntHashTable checks = new IntHashTable();
			int position = bits.Position;
			while ((long)bits.Position < (long)((ulong)limit))
			{
				int sig;
				bits.ReadInt32(out sig);
				int siz;
				bits.ReadInt32(out siz);
				int place = bits.Position;
				int endSym = bits.Position + siz;
				if (sig != 244)
				{
					bits.Position = endSym;
				}
				else
				{
					while (bits.Position < endSym)
					{
						int ni = bits.Position - place;
						CV_FileCheckSum chk;
						bits.ReadUInt32(out chk.name);
						bits.ReadUInt8(out chk.len);
						bits.ReadUInt8(out chk.type);
						string name = (string)names[(int)chk.name];
						PdbSource src;
						if (!sourceCache.TryGetValue(name, out src))
						{
							Guid doctypeGuid = PdbFile.SymDocumentType_Text;
							Guid languageGuid = Guid.Empty;
							Guid vendorGuid = Guid.Empty;
							Guid checksumAlgoGuid = Guid.Empty;
							byte[] checksum = null;
							int guidStream;
							if (nameIndex.TryGetValue("/SRC/FILES/" + name.ToUpperInvariant(), out guidStream))
							{
								BitAccess guidBits = new BitAccess(256);
								dir.streams[guidStream].Read(reader, guidBits);
								PdbFile.LoadInjectedSourceInformation(guidBits, out doctypeGuid, out languageGuid, out vendorGuid, out checksumAlgoGuid, out checksum);
							}
							src = new PdbSource(name, doctypeGuid, languageGuid, vendorGuid, checksumAlgoGuid, checksum);
							sourceCache.Add(name, src);
						}
						checks.Add(ni, src);
						bits.Position += (int)chk.len;
						bits.Align(4);
					}
					bits.Position = endSym;
				}
			}
			return checks;
		}

		// Token: 0x04001002 RID: 4098
		private static readonly Guid BasicLanguageGuid = new Guid(974311608, -15764, 4560, 180, 66, 0, 160, 36, 74, 29, 210);

		// Token: 0x04001003 RID: 4099
		public static readonly Guid SymDocumentType_Text = new Guid(1518771467, 26129, 4563, 189, 42, 0, 0, 248, 8, 73, 189);
	}
}
