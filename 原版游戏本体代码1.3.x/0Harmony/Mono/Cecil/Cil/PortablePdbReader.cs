using System;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002FD RID: 765
	internal sealed class PortablePdbReader : ISymbolReader, IDisposable
	{
		// Token: 0x1700050A RID: 1290
		// (get) Token: 0x060013E7 RID: 5095 RVA: 0x0003FD6C File Offset: 0x0003DF6C
		private bool IsEmbedded
		{
			get
			{
				return this.reader.image == this.debug_reader.image;
			}
		}

		// Token: 0x060013E8 RID: 5096 RVA: 0x0003FD86 File Offset: 0x0003DF86
		internal PortablePdbReader(Image image, ModuleDefinition module)
		{
			this.image = image;
			this.module = module;
			this.reader = module.reader;
			this.debug_reader = new MetadataReader(image, module, this.reader);
		}

		// Token: 0x060013E9 RID: 5097 RVA: 0x0003FDBB File Offset: 0x0003DFBB
		public ISymbolWriterProvider GetWriterProvider()
		{
			return new PortablePdbWriterProvider();
		}

		// Token: 0x060013EA RID: 5098 RVA: 0x0003FDC4 File Offset: 0x0003DFC4
		public bool ProcessDebugHeader(ImageDebugHeader header)
		{
			if (this.image == this.module.Image)
			{
				return true;
			}
			foreach (ImageDebugHeaderEntry entry in header.Entries)
			{
				if (PortablePdbReader.IsMatchingEntry(this.image.PdbHeap, entry))
				{
					this.ReadModule();
					return true;
				}
			}
			return false;
		}

		// Token: 0x060013EB RID: 5099 RVA: 0x0003FE1C File Offset: 0x0003E01C
		private static bool IsMatchingEntry(PdbHeap heap, ImageDebugHeaderEntry entry)
		{
			if (entry.Directory.Type != ImageDebugType.CodeView)
			{
				return false;
			}
			byte[] data = entry.Data;
			if (data.Length < 24)
			{
				return false;
			}
			if (PortablePdbReader.ReadInt32(data, 0) != 1396986706)
			{
				return false;
			}
			byte[] buffer = new byte[16];
			Buffer.BlockCopy(data, 4, buffer, 0, 16);
			Guid a = new Guid(buffer);
			Buffer.BlockCopy(heap.Id, 0, buffer, 0, 16);
			Guid pdb_guid = new Guid(buffer);
			return a == pdb_guid;
		}

		// Token: 0x060013EC RID: 5100 RVA: 0x0003FE90 File Offset: 0x0003E090
		private static int ReadInt32(byte[] bytes, int start)
		{
			return (int)bytes[start] | ((int)bytes[start + 1] << 8) | ((int)bytes[start + 2] << 16) | ((int)bytes[start + 3] << 24);
		}

		// Token: 0x060013ED RID: 5101 RVA: 0x0003FEAF File Offset: 0x0003E0AF
		private void ReadModule()
		{
			this.module.custom_infos = this.debug_reader.GetCustomDebugInformation(this.module);
		}

		// Token: 0x060013EE RID: 5102 RVA: 0x0003FED0 File Offset: 0x0003E0D0
		public MethodDebugInformation Read(MethodDefinition method)
		{
			MethodDebugInformation info = new MethodDebugInformation(method);
			this.ReadSequencePoints(info);
			this.ReadScope(info);
			this.ReadStateMachineKickOffMethod(info);
			this.ReadCustomDebugInformations(info);
			return info;
		}

		// Token: 0x060013EF RID: 5103 RVA: 0x0003FF01 File Offset: 0x0003E101
		private void ReadSequencePoints(MethodDebugInformation method_info)
		{
			method_info.sequence_points = this.debug_reader.ReadSequencePoints(method_info.method);
		}

		// Token: 0x060013F0 RID: 5104 RVA: 0x0003FF1A File Offset: 0x0003E11A
		private void ReadScope(MethodDebugInformation method_info)
		{
			method_info.scope = this.debug_reader.ReadScope(method_info.method);
		}

		// Token: 0x060013F1 RID: 5105 RVA: 0x0003FF33 File Offset: 0x0003E133
		private void ReadStateMachineKickOffMethod(MethodDebugInformation method_info)
		{
			method_info.kickoff_method = this.debug_reader.ReadStateMachineKickoffMethod(method_info.method);
		}

		// Token: 0x060013F2 RID: 5106 RVA: 0x0003FF4C File Offset: 0x0003E14C
		public Collection<CustomDebugInformation> Read(ICustomDebugInformationProvider provider)
		{
			return this.debug_reader.GetCustomDebugInformation(provider);
		}

		// Token: 0x060013F3 RID: 5107 RVA: 0x0003FF5A File Offset: 0x0003E15A
		private void ReadCustomDebugInformations(MethodDebugInformation info)
		{
			info.method.custom_infos = this.debug_reader.GetCustomDebugInformation(info.method);
		}

		// Token: 0x060013F4 RID: 5108 RVA: 0x0003FF78 File Offset: 0x0003E178
		public void Dispose()
		{
			if (this.IsEmbedded)
			{
				return;
			}
			this.image.Dispose();
		}

		// Token: 0x040009EB RID: 2539
		private readonly Image image;

		// Token: 0x040009EC RID: 2540
		private readonly ModuleDefinition module;

		// Token: 0x040009ED RID: 2541
		private readonly MetadataReader reader;

		// Token: 0x040009EE RID: 2542
		private readonly MetadataReader debug_reader;
	}
}
