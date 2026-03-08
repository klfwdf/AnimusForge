using System;
using System.IO;
using Mono.Cecil.PE;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000323 RID: 803
	internal class DefaultSymbolReaderProvider : ISymbolReaderProvider
	{
		// Token: 0x060014C8 RID: 5320 RVA: 0x000418BC File Offset: 0x0003FABC
		public DefaultSymbolReaderProvider()
			: this(true)
		{
		}

		// Token: 0x060014C9 RID: 5321 RVA: 0x000418C5 File Offset: 0x0003FAC5
		public DefaultSymbolReaderProvider(bool throwIfNoSymbol)
		{
			this.throw_if_no_symbol = throwIfNoSymbol;
		}

		// Token: 0x060014CA RID: 5322 RVA: 0x000418D4 File Offset: 0x0003FAD4
		public ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName)
		{
			if (module.Image.HasDebugTables())
			{
				return null;
			}
			if (module.HasDebugHeader && module.GetDebugHeader().GetEmbeddedPortablePdbEntry() != null)
			{
				return new EmbeddedPortablePdbReaderProvider().GetSymbolReader(module, fileName);
			}
			if (File.Exists(Mixin.GetPdbFileName(fileName)))
			{
				if (Mixin.IsPortablePdb(Mixin.GetPdbFileName(fileName)))
				{
					return new PortablePdbReaderProvider().GetSymbolReader(module, fileName);
				}
				try
				{
					return SymbolProvider.GetReaderProvider(SymbolKind.NativePdb).GetSymbolReader(module, fileName);
				}
				catch (Exception)
				{
				}
			}
			if (File.Exists(Mixin.GetMdbFileName(fileName)))
			{
				try
				{
					return SymbolProvider.GetReaderProvider(SymbolKind.Mdb).GetSymbolReader(module, fileName);
				}
				catch (Exception)
				{
				}
			}
			if (this.throw_if_no_symbol)
			{
				throw new SymbolsNotFoundException(string.Format("No symbol found for file: {0}", fileName));
			}
			return null;
		}

		// Token: 0x060014CB RID: 5323 RVA: 0x000419A8 File Offset: 0x0003FBA8
		public ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream)
		{
			if (module.Image.HasDebugTables())
			{
				return null;
			}
			if (module.HasDebugHeader && module.GetDebugHeader().GetEmbeddedPortablePdbEntry() != null)
			{
				return new EmbeddedPortablePdbReaderProvider().GetSymbolReader(module, "");
			}
			Mixin.CheckStream(symbolStream);
			Mixin.CheckReadSeek(symbolStream);
			long position = symbolStream.Position;
			BinaryStreamReader reader = new BinaryStreamReader(symbolStream);
			int num = reader.ReadInt32();
			symbolStream.Position = position;
			if (num == 1112167234)
			{
				return new PortablePdbReaderProvider().GetSymbolReader(module, symbolStream);
			}
			byte[] bytesHeader = reader.ReadBytes("Microsoft C/C++ MSF 7.00".Length);
			symbolStream.Position = position;
			bool isNativePdb = true;
			for (int i = 0; i < bytesHeader.Length; i++)
			{
				if (bytesHeader[i] != (byte)"Microsoft C/C++ MSF 7.00"[i])
				{
					isNativePdb = false;
					break;
				}
			}
			if (isNativePdb)
			{
				try
				{
					return SymbolProvider.GetReaderProvider(SymbolKind.NativePdb).GetSymbolReader(module, symbolStream);
				}
				catch (Exception)
				{
				}
			}
			long num2 = reader.ReadInt64();
			symbolStream.Position = position;
			if (num2 == 5037318119232611860L)
			{
				try
				{
					return SymbolProvider.GetReaderProvider(SymbolKind.Mdb).GetSymbolReader(module, symbolStream);
				}
				catch (Exception)
				{
				}
			}
			if (this.throw_if_no_symbol)
			{
				throw new SymbolsNotFoundException(string.Format("No symbols found in stream", new object[0]));
			}
			return null;
		}

		// Token: 0x04000A67 RID: 2663
		private readonly bool throw_if_no_symbol;
	}
}
