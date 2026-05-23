using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200077E RID: 1918
	internal static class IOUtil
	{
		// Token: 0x060053BA RID: 21434 RVA: 0x00126DA9 File Offset: 0x00124FA9
		internal static bool FlagTest(MessageEnum flag, MessageEnum target)
		{
			return (flag & target) == target;
		}

		// Token: 0x060053BB RID: 21435 RVA: 0x00126DB4 File Offset: 0x00124FB4
		internal static void WriteStringWithCode(string value, __BinaryWriter sout)
		{
			if (value == null)
			{
				sout.WriteByte(17);
				return;
			}
			sout.WriteByte(18);
			sout.WriteString(value);
		}

		// Token: 0x060053BC RID: 21436 RVA: 0x00126DD4 File Offset: 0x00124FD4
		internal static void WriteWithCode(Type type, object value, __BinaryWriter sout)
		{
			if (type == null)
			{
				sout.WriteByte(17);
				return;
			}
			if (type == Converter.typeofString)
			{
				IOUtil.WriteStringWithCode((string)value, sout);
				return;
			}
			InternalPrimitiveTypeE internalPrimitiveTypeE = Converter.ToCode(type);
			sout.WriteByte((byte)internalPrimitiveTypeE);
			sout.WriteValue(internalPrimitiveTypeE, value);
		}

		// Token: 0x060053BD RID: 21437 RVA: 0x00126E1C File Offset: 0x0012501C
		internal static object ReadWithCode(__BinaryParser input)
		{
			InternalPrimitiveTypeE internalPrimitiveTypeE = (InternalPrimitiveTypeE)input.ReadByte();
			if (internalPrimitiveTypeE == InternalPrimitiveTypeE.Null)
			{
				return null;
			}
			if (internalPrimitiveTypeE == InternalPrimitiveTypeE.String)
			{
				return input.ReadString();
			}
			return input.ReadValue(internalPrimitiveTypeE);
		}

		// Token: 0x060053BE RID: 21438 RVA: 0x00126E4C File Offset: 0x0012504C
		internal static object[] ReadArgs(__BinaryParser input)
		{
			int num = input.ReadInt32();
			object[] array = new object[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = IOUtil.ReadWithCode(input);
			}
			return array;
		}
	}
}
