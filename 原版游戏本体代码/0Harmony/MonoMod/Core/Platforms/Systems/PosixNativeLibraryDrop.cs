using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Systems
{
	// Token: 0x02000522 RID: 1314
	internal abstract class PosixNativeLibraryDrop
	{
		// Token: 0x06001D80 RID: 7552
		[return: NativeInteger]
		protected abstract IntPtr Mkstemp(Span<byte> template);

		// Token: 0x06001D81 RID: 7553
		protected abstract void CloseFileDescriptor([NativeInteger] IntPtr fd);

		// Token: 0x06001D82 RID: 7554 RVA: 0x0005F424 File Offset: 0x0005D624
		[NullableContext(1)]
		public unsafe string DropLibrary(Stream sourceStream, [Nullable(0)] ReadOnlySpan<byte> defaultTemplate)
		{
			object value;
			byte[] templ;
			int templateLength;
			if (Switches.TryGetSwitchValue("HelperDropPath", out value))
			{
				string dropPath = value as string;
				if (dropPath != null)
				{
					int endOfDefaultTemplateDir = defaultTemplate.LastIndexOf(47);
					Helpers.Assert(endOfDefaultTemplateDir >= 0, null, "endOfDefaultTemplateDir >= 0");
					ReadOnlySpan<byte> templateBasename = defaultTemplate.Slice(endOfDefaultTemplateDir);
					dropPath = Path.GetFullPath(dropPath);
					Directory.CreateDirectory(dropPath);
					int byteCount = Encoding.UTF8.GetByteCount(dropPath);
					templ = ArrayPool<byte>.Shared.Rent(byteCount + templateBasename.Length + 1);
					templ.AsSpan<byte>().Clear();
					int pos;
					fixed (char* pinnableReference = dropPath.AsSpan().GetPinnableReference())
					{
						char* pchars = pinnableReference;
						byte[] array;
						byte* pbytes;
						if ((array = templ) == null || array.Length == 0)
						{
							pbytes = null;
						}
						else
						{
							pbytes = &array[0];
						}
						pos = Encoding.UTF8.GetBytes(pchars, dropPath.Length, pbytes, templ.Length);
						array = null;
					}
					if (templ[pos - 1] == 47)
					{
						pos--;
					}
					templateBasename.CopyTo(templ.AsSpan(pos));
					templ[pos + templateBasename.Length] = 0;
					templateLength = pos + templateBasename.Length;
					goto IL_14B;
				}
			}
			templ = ArrayPool<byte>.Shared.Rent(defaultTemplate.Length + 1);
			templ.AsSpan<byte>().Clear();
			defaultTemplate.CopyTo(templ);
			templateLength = defaultTemplate.Length;
			IL_14B:
			IntPtr fd = this.Mkstemp(templ);
			string fname = Encoding.UTF8.GetString(templ, 0, templateLength);
			ArrayPool<byte>.Shared.Return(templ, false);
			if (PlatformDetection.Runtime == RuntimeKind.Mono && PlatformDetection.Corelib != CorelibKind.Core)
			{
				this.CloseFileDescriptor(fd);
				using (FileStream fs = new FileStream(fname, FileMode.Create, FileAccess.Write))
				{
					sourceStream.CopyTo(fs);
					return fname;
				}
			}
			try
			{
				using (FileStream fs2 = new FileStream(fd, FileAccess.Write))
				{
					sourceStream.CopyTo(fs2);
				}
			}
			finally
			{
				this.CloseFileDescriptor(fd);
			}
			return fname;
		}
	}
}
