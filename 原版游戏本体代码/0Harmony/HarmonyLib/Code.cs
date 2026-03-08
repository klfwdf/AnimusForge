using System;
using System.Reflection.Emit;

namespace HarmonyLib
{
	/// <summary>
	///  By adding the following using statement to your source code: <c>using static HarmonyLib.Code;</c>
	///  you can for example start using <c>Ldarg_1</c> in you code instead of <c>new CodeMatch(OpCodes.Ldarg_1)</c>
	///  and then you can use array notation to add an operand and/or a name: <c>Call[myMethodInfo]</c> instead of <c>new CodeMatch(OpCodes.Call, myMethodInfo)</c></summary>
	// Token: 0x020000C4 RID: 196
	public static class Code
	{
		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000453 RID: 1107 RVA: 0x000152DB File Offset: 0x000134DB
		public static Code.Operand_ Operand
		{
			get
			{
				return new Code.Operand_();
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000454 RID: 1108 RVA: 0x000152E2 File Offset: 0x000134E2
		public static Code.Nop_ Nop
		{
			get
			{
				return new Code.Nop_(OpCodes.Nop);
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000455 RID: 1109 RVA: 0x000152EE File Offset: 0x000134EE
		public static Code.Break_ Break
		{
			get
			{
				return new Code.Break_(OpCodes.Break);
			}
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000456 RID: 1110 RVA: 0x000152FA File Offset: 0x000134FA
		public static Code.Ldarg_0_ Ldarg_0
		{
			get
			{
				return new Code.Ldarg_0_(OpCodes.Ldarg_0);
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000457 RID: 1111 RVA: 0x00015306 File Offset: 0x00013506
		public static Code.Ldarg_1_ Ldarg_1
		{
			get
			{
				return new Code.Ldarg_1_(OpCodes.Ldarg_1);
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000458 RID: 1112 RVA: 0x00015312 File Offset: 0x00013512
		public static Code.Ldarg_2_ Ldarg_2
		{
			get
			{
				return new Code.Ldarg_2_(OpCodes.Ldarg_2);
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000459 RID: 1113 RVA: 0x0001531E File Offset: 0x0001351E
		public static Code.Ldarg_3_ Ldarg_3
		{
			get
			{
				return new Code.Ldarg_3_(OpCodes.Ldarg_3);
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x0600045A RID: 1114 RVA: 0x0001532A File Offset: 0x0001352A
		public static Code.Ldloc_0_ Ldloc_0
		{
			get
			{
				return new Code.Ldloc_0_(OpCodes.Ldloc_0);
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x0600045B RID: 1115 RVA: 0x00015336 File Offset: 0x00013536
		public static Code.Ldloc_1_ Ldloc_1
		{
			get
			{
				return new Code.Ldloc_1_(OpCodes.Ldloc_1);
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600045C RID: 1116 RVA: 0x00015342 File Offset: 0x00013542
		public static Code.Ldloc_2_ Ldloc_2
		{
			get
			{
				return new Code.Ldloc_2_(OpCodes.Ldloc_2);
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600045D RID: 1117 RVA: 0x0001534E File Offset: 0x0001354E
		public static Code.Ldloc_3_ Ldloc_3
		{
			get
			{
				return new Code.Ldloc_3_(OpCodes.Ldloc_3);
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600045E RID: 1118 RVA: 0x0001535A File Offset: 0x0001355A
		public static Code.Stloc_0_ Stloc_0
		{
			get
			{
				return new Code.Stloc_0_(OpCodes.Stloc_0);
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600045F RID: 1119 RVA: 0x00015366 File Offset: 0x00013566
		public static Code.Stloc_1_ Stloc_1
		{
			get
			{
				return new Code.Stloc_1_(OpCodes.Stloc_1);
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000460 RID: 1120 RVA: 0x00015372 File Offset: 0x00013572
		public static Code.Stloc_2_ Stloc_2
		{
			get
			{
				return new Code.Stloc_2_(OpCodes.Stloc_2);
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000461 RID: 1121 RVA: 0x0001537E File Offset: 0x0001357E
		public static Code.Stloc_3_ Stloc_3
		{
			get
			{
				return new Code.Stloc_3_(OpCodes.Stloc_3);
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000462 RID: 1122 RVA: 0x0001538A File Offset: 0x0001358A
		public static Code.Ldarg_S_ Ldarg_S
		{
			get
			{
				return new Code.Ldarg_S_(OpCodes.Ldarg_S);
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000463 RID: 1123 RVA: 0x00015396 File Offset: 0x00013596
		public static Code.Ldarga_S_ Ldarga_S
		{
			get
			{
				return new Code.Ldarga_S_(OpCodes.Ldarga_S);
			}
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x06000464 RID: 1124 RVA: 0x000153A2 File Offset: 0x000135A2
		public static Code.Starg_S_ Starg_S
		{
			get
			{
				return new Code.Starg_S_(OpCodes.Starg_S);
			}
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000465 RID: 1125 RVA: 0x000153AE File Offset: 0x000135AE
		public static Code.Ldloc_S_ Ldloc_S
		{
			get
			{
				return new Code.Ldloc_S_(OpCodes.Ldloc_S);
			}
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x06000466 RID: 1126 RVA: 0x000153BA File Offset: 0x000135BA
		public static Code.Ldloca_S_ Ldloca_S
		{
			get
			{
				return new Code.Ldloca_S_(OpCodes.Ldloca_S);
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000467 RID: 1127 RVA: 0x000153C6 File Offset: 0x000135C6
		public static Code.Stloc_S_ Stloc_S
		{
			get
			{
				return new Code.Stloc_S_(OpCodes.Stloc_S);
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000468 RID: 1128 RVA: 0x000153D2 File Offset: 0x000135D2
		public static Code.Ldnull_ Ldnull
		{
			get
			{
				return new Code.Ldnull_(OpCodes.Ldnull);
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000469 RID: 1129 RVA: 0x000153DE File Offset: 0x000135DE
		public static Code.Ldc_I4_M1_ Ldc_I4_M1
		{
			get
			{
				return new Code.Ldc_I4_M1_(OpCodes.Ldc_I4_M1);
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x0600046A RID: 1130 RVA: 0x000153EA File Offset: 0x000135EA
		public static Code.Ldc_I4_0_ Ldc_I4_0
		{
			get
			{
				return new Code.Ldc_I4_0_(OpCodes.Ldc_I4_0);
			}
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x0600046B RID: 1131 RVA: 0x000153F6 File Offset: 0x000135F6
		public static Code.Ldc_I4_1_ Ldc_I4_1
		{
			get
			{
				return new Code.Ldc_I4_1_(OpCodes.Ldc_I4_1);
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600046C RID: 1132 RVA: 0x00015402 File Offset: 0x00013602
		public static Code.Ldc_I4_2_ Ldc_I4_2
		{
			get
			{
				return new Code.Ldc_I4_2_(OpCodes.Ldc_I4_2);
			}
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x0600046D RID: 1133 RVA: 0x0001540E File Offset: 0x0001360E
		public static Code.Ldc_I4_3_ Ldc_I4_3
		{
			get
			{
				return new Code.Ldc_I4_3_(OpCodes.Ldc_I4_3);
			}
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x0600046E RID: 1134 RVA: 0x0001541A File Offset: 0x0001361A
		public static Code.Ldc_I4_4_ Ldc_I4_4
		{
			get
			{
				return new Code.Ldc_I4_4_(OpCodes.Ldc_I4_4);
			}
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x0600046F RID: 1135 RVA: 0x00015426 File Offset: 0x00013626
		public static Code.Ldc_I4_5_ Ldc_I4_5
		{
			get
			{
				return new Code.Ldc_I4_5_(OpCodes.Ldc_I4_5);
			}
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000470 RID: 1136 RVA: 0x00015432 File Offset: 0x00013632
		public static Code.Ldc_I4_6_ Ldc_I4_6
		{
			get
			{
				return new Code.Ldc_I4_6_(OpCodes.Ldc_I4_6);
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000471 RID: 1137 RVA: 0x0001543E File Offset: 0x0001363E
		public static Code.Ldc_I4_7_ Ldc_I4_7
		{
			get
			{
				return new Code.Ldc_I4_7_(OpCodes.Ldc_I4_7);
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000472 RID: 1138 RVA: 0x0001544A File Offset: 0x0001364A
		public static Code.Ldc_I4_8_ Ldc_I4_8
		{
			get
			{
				return new Code.Ldc_I4_8_(OpCodes.Ldc_I4_8);
			}
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000473 RID: 1139 RVA: 0x00015456 File Offset: 0x00013656
		public static Code.Ldc_I4_S_ Ldc_I4_S
		{
			get
			{
				return new Code.Ldc_I4_S_(OpCodes.Ldc_I4_S);
			}
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000474 RID: 1140 RVA: 0x00015462 File Offset: 0x00013662
		public static Code.Ldc_I4_ Ldc_I4
		{
			get
			{
				return new Code.Ldc_I4_(OpCodes.Ldc_I4);
			}
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x06000475 RID: 1141 RVA: 0x0001546E File Offset: 0x0001366E
		public static Code.Ldc_I8_ Ldc_I8
		{
			get
			{
				return new Code.Ldc_I8_(OpCodes.Ldc_I8);
			}
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x06000476 RID: 1142 RVA: 0x0001547A File Offset: 0x0001367A
		public static Code.Ldc_R4_ Ldc_R4
		{
			get
			{
				return new Code.Ldc_R4_(OpCodes.Ldc_R4);
			}
		}

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000477 RID: 1143 RVA: 0x00015486 File Offset: 0x00013686
		public static Code.Ldc_R8_ Ldc_R8
		{
			get
			{
				return new Code.Ldc_R8_(OpCodes.Ldc_R8);
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000478 RID: 1144 RVA: 0x00015492 File Offset: 0x00013692
		public static Code.Dup_ Dup
		{
			get
			{
				return new Code.Dup_(OpCodes.Dup);
			}
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000479 RID: 1145 RVA: 0x0001549E File Offset: 0x0001369E
		public static Code.Pop_ Pop
		{
			get
			{
				return new Code.Pop_(OpCodes.Pop);
			}
		}

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x0600047A RID: 1146 RVA: 0x000154AA File Offset: 0x000136AA
		public static Code.Jmp_ Jmp
		{
			get
			{
				return new Code.Jmp_(OpCodes.Jmp);
			}
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600047B RID: 1147 RVA: 0x000154B6 File Offset: 0x000136B6
		public static Code.Call_ Call
		{
			get
			{
				return new Code.Call_(OpCodes.Call);
			}
		}

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600047C RID: 1148 RVA: 0x000154C2 File Offset: 0x000136C2
		public static Code.Calli_ Calli
		{
			get
			{
				return new Code.Calli_(OpCodes.Calli);
			}
		}

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600047D RID: 1149 RVA: 0x000154CE File Offset: 0x000136CE
		public static Code.Ret_ Ret
		{
			get
			{
				return new Code.Ret_(OpCodes.Ret);
			}
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x0600047E RID: 1150 RVA: 0x000154DA File Offset: 0x000136DA
		public static Code.Br_S_ Br_S
		{
			get
			{
				return new Code.Br_S_(OpCodes.Br_S);
			}
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x0600047F RID: 1151 RVA: 0x000154E6 File Offset: 0x000136E6
		public static Code.Brfalse_S_ Brfalse_S
		{
			get
			{
				return new Code.Brfalse_S_(OpCodes.Brfalse_S);
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000480 RID: 1152 RVA: 0x000154F2 File Offset: 0x000136F2
		public static Code.Brtrue_S_ Brtrue_S
		{
			get
			{
				return new Code.Brtrue_S_(OpCodes.Brtrue_S);
			}
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000481 RID: 1153 RVA: 0x000154FE File Offset: 0x000136FE
		public static Code.Beq_S_ Beq_S
		{
			get
			{
				return new Code.Beq_S_(OpCodes.Beq_S);
			}
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000482 RID: 1154 RVA: 0x0001550A File Offset: 0x0001370A
		public static Code.Bge_S_ Bge_S
		{
			get
			{
				return new Code.Bge_S_(OpCodes.Bge_S);
			}
		}

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000483 RID: 1155 RVA: 0x00015516 File Offset: 0x00013716
		public static Code.Bgt_S_ Bgt_S
		{
			get
			{
				return new Code.Bgt_S_(OpCodes.Bgt_S);
			}
		}

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x06000484 RID: 1156 RVA: 0x00015522 File Offset: 0x00013722
		public static Code.Ble_S_ Ble_S
		{
			get
			{
				return new Code.Ble_S_(OpCodes.Ble_S);
			}
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x06000485 RID: 1157 RVA: 0x0001552E File Offset: 0x0001372E
		public static Code.Blt_S_ Blt_S
		{
			get
			{
				return new Code.Blt_S_(OpCodes.Blt_S);
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x06000486 RID: 1158 RVA: 0x0001553A File Offset: 0x0001373A
		public static Code.Bne_Un_S_ Bne_Un_S
		{
			get
			{
				return new Code.Bne_Un_S_(OpCodes.Bne_Un_S);
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000487 RID: 1159 RVA: 0x00015546 File Offset: 0x00013746
		public static Code.Bge_Un_S_ Bge_Un_S
		{
			get
			{
				return new Code.Bge_Un_S_(OpCodes.Bge_Un_S);
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000488 RID: 1160 RVA: 0x00015552 File Offset: 0x00013752
		public static Code.Bgt_Un_S_ Bgt_Un_S
		{
			get
			{
				return new Code.Bgt_Un_S_(OpCodes.Bgt_Un_S);
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000489 RID: 1161 RVA: 0x0001555E File Offset: 0x0001375E
		public static Code.Ble_Un_S_ Ble_Un_S
		{
			get
			{
				return new Code.Ble_Un_S_(OpCodes.Ble_Un_S);
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x0600048A RID: 1162 RVA: 0x0001556A File Offset: 0x0001376A
		public static Code.Blt_Un_S_ Blt_Un_S
		{
			get
			{
				return new Code.Blt_Un_S_(OpCodes.Blt_Un_S);
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x0600048B RID: 1163 RVA: 0x00015576 File Offset: 0x00013776
		public static Code.Br_ Br
		{
			get
			{
				return new Code.Br_(OpCodes.Br);
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x0600048C RID: 1164 RVA: 0x00015582 File Offset: 0x00013782
		public static Code.Brfalse_ Brfalse
		{
			get
			{
				return new Code.Brfalse_(OpCodes.Brfalse);
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x0600048D RID: 1165 RVA: 0x0001558E File Offset: 0x0001378E
		public static Code.Brtrue_ Brtrue
		{
			get
			{
				return new Code.Brtrue_(OpCodes.Brtrue);
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x0600048E RID: 1166 RVA: 0x0001559A File Offset: 0x0001379A
		public static Code.Beq_ Beq
		{
			get
			{
				return new Code.Beq_(OpCodes.Beq);
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x0600048F RID: 1167 RVA: 0x000155A6 File Offset: 0x000137A6
		public static Code.Bge_ Bge
		{
			get
			{
				return new Code.Bge_(OpCodes.Bge);
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000490 RID: 1168 RVA: 0x000155B2 File Offset: 0x000137B2
		public static Code.Bgt_ Bgt
		{
			get
			{
				return new Code.Bgt_(OpCodes.Bgt);
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000491 RID: 1169 RVA: 0x000155BE File Offset: 0x000137BE
		public static Code.Ble_ Ble
		{
			get
			{
				return new Code.Ble_(OpCodes.Ble);
			}
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000492 RID: 1170 RVA: 0x000155CA File Offset: 0x000137CA
		public static Code.Blt_ Blt
		{
			get
			{
				return new Code.Blt_(OpCodes.Blt);
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000493 RID: 1171 RVA: 0x000155D6 File Offset: 0x000137D6
		public static Code.Bne_Un_ Bne_Un
		{
			get
			{
				return new Code.Bne_Un_(OpCodes.Bne_Un);
			}
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000494 RID: 1172 RVA: 0x000155E2 File Offset: 0x000137E2
		public static Code.Bge_Un_ Bge_Un
		{
			get
			{
				return new Code.Bge_Un_(OpCodes.Bge_Un);
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000495 RID: 1173 RVA: 0x000155EE File Offset: 0x000137EE
		public static Code.Bgt_Un_ Bgt_Un
		{
			get
			{
				return new Code.Bgt_Un_(OpCodes.Bgt_Un);
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000496 RID: 1174 RVA: 0x000155FA File Offset: 0x000137FA
		public static Code.Ble_Un_ Ble_Un
		{
			get
			{
				return new Code.Ble_Un_(OpCodes.Ble_Un);
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000497 RID: 1175 RVA: 0x00015606 File Offset: 0x00013806
		public static Code.Blt_Un_ Blt_Un
		{
			get
			{
				return new Code.Blt_Un_(OpCodes.Blt_Un);
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000498 RID: 1176 RVA: 0x00015612 File Offset: 0x00013812
		public static Code.Switch_ Switch
		{
			get
			{
				return new Code.Switch_(OpCodes.Switch);
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000499 RID: 1177 RVA: 0x0001561E File Offset: 0x0001381E
		public static Code.Ldind_I1_ Ldind_I1
		{
			get
			{
				return new Code.Ldind_I1_(OpCodes.Ldind_I1);
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x0600049A RID: 1178 RVA: 0x0001562A File Offset: 0x0001382A
		public static Code.Ldind_U1_ Ldind_U1
		{
			get
			{
				return new Code.Ldind_U1_(OpCodes.Ldind_U1);
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x0600049B RID: 1179 RVA: 0x00015636 File Offset: 0x00013836
		public static Code.Ldind_I2_ Ldind_I2
		{
			get
			{
				return new Code.Ldind_I2_(OpCodes.Ldind_I2);
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x0600049C RID: 1180 RVA: 0x00015642 File Offset: 0x00013842
		public static Code.Ldind_U2_ Ldind_U2
		{
			get
			{
				return new Code.Ldind_U2_(OpCodes.Ldind_U2);
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x0600049D RID: 1181 RVA: 0x0001564E File Offset: 0x0001384E
		public static Code.Ldind_I4_ Ldind_I4
		{
			get
			{
				return new Code.Ldind_I4_(OpCodes.Ldind_I4);
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x0001565A File Offset: 0x0001385A
		public static Code.Ldind_U4_ Ldind_U4
		{
			get
			{
				return new Code.Ldind_U4_(OpCodes.Ldind_U4);
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600049F RID: 1183 RVA: 0x00015666 File Offset: 0x00013866
		public static Code.Ldind_I8_ Ldind_I8
		{
			get
			{
				return new Code.Ldind_I8_(OpCodes.Ldind_I8);
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x060004A0 RID: 1184 RVA: 0x00015672 File Offset: 0x00013872
		public static Code.Ldind_I_ Ldind_I
		{
			get
			{
				return new Code.Ldind_I_(OpCodes.Ldind_I);
			}
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x060004A1 RID: 1185 RVA: 0x0001567E File Offset: 0x0001387E
		public static Code.Ldind_R4_ Ldind_R4
		{
			get
			{
				return new Code.Ldind_R4_(OpCodes.Ldind_R4);
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x060004A2 RID: 1186 RVA: 0x0001568A File Offset: 0x0001388A
		public static Code.Ldind_R8_ Ldind_R8
		{
			get
			{
				return new Code.Ldind_R8_(OpCodes.Ldind_R8);
			}
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x060004A3 RID: 1187 RVA: 0x00015696 File Offset: 0x00013896
		public static Code.Ldind_Ref_ Ldind_Ref
		{
			get
			{
				return new Code.Ldind_Ref_(OpCodes.Ldind_Ref);
			}
		}

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x060004A4 RID: 1188 RVA: 0x000156A2 File Offset: 0x000138A2
		public static Code.Stind_Ref_ Stind_Ref
		{
			get
			{
				return new Code.Stind_Ref_(OpCodes.Stind_Ref);
			}
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x060004A5 RID: 1189 RVA: 0x000156AE File Offset: 0x000138AE
		public static Code.Stind_I1_ Stind_I1
		{
			get
			{
				return new Code.Stind_I1_(OpCodes.Stind_I1);
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x060004A6 RID: 1190 RVA: 0x000156BA File Offset: 0x000138BA
		public static Code.Stind_I2_ Stind_I2
		{
			get
			{
				return new Code.Stind_I2_(OpCodes.Stind_I2);
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x060004A7 RID: 1191 RVA: 0x000156C6 File Offset: 0x000138C6
		public static Code.Stind_I4_ Stind_I4
		{
			get
			{
				return new Code.Stind_I4_(OpCodes.Stind_I4);
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x060004A8 RID: 1192 RVA: 0x000156D2 File Offset: 0x000138D2
		public static Code.Stind_I8_ Stind_I8
		{
			get
			{
				return new Code.Stind_I8_(OpCodes.Stind_I8);
			}
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x060004A9 RID: 1193 RVA: 0x000156DE File Offset: 0x000138DE
		public static Code.Stind_R4_ Stind_R4
		{
			get
			{
				return new Code.Stind_R4_(OpCodes.Stind_R4);
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x060004AA RID: 1194 RVA: 0x000156EA File Offset: 0x000138EA
		public static Code.Stind_R8_ Stind_R8
		{
			get
			{
				return new Code.Stind_R8_(OpCodes.Stind_R8);
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x060004AB RID: 1195 RVA: 0x000156F6 File Offset: 0x000138F6
		public static Code.Add_ Add
		{
			get
			{
				return new Code.Add_(OpCodes.Add);
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x060004AC RID: 1196 RVA: 0x00015702 File Offset: 0x00013902
		public static Code.Sub_ Sub
		{
			get
			{
				return new Code.Sub_(OpCodes.Sub);
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x060004AD RID: 1197 RVA: 0x0001570E File Offset: 0x0001390E
		public static Code.Mul_ Mul
		{
			get
			{
				return new Code.Mul_(OpCodes.Mul);
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x060004AE RID: 1198 RVA: 0x0001571A File Offset: 0x0001391A
		public static Code.Div_ Div
		{
			get
			{
				return new Code.Div_(OpCodes.Div);
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x060004AF RID: 1199 RVA: 0x00015726 File Offset: 0x00013926
		public static Code.Div_Un_ Div_Un
		{
			get
			{
				return new Code.Div_Un_(OpCodes.Div_Un);
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x060004B0 RID: 1200 RVA: 0x00015732 File Offset: 0x00013932
		public static Code.Rem_ Rem
		{
			get
			{
				return new Code.Rem_(OpCodes.Rem);
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x060004B1 RID: 1201 RVA: 0x0001573E File Offset: 0x0001393E
		public static Code.Rem_Un_ Rem_Un
		{
			get
			{
				return new Code.Rem_Un_(OpCodes.Rem_Un);
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x060004B2 RID: 1202 RVA: 0x0001574A File Offset: 0x0001394A
		public static Code.And_ And
		{
			get
			{
				return new Code.And_(OpCodes.And);
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060004B3 RID: 1203 RVA: 0x00015756 File Offset: 0x00013956
		public static Code.Or_ Or
		{
			get
			{
				return new Code.Or_(OpCodes.Or);
			}
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060004B4 RID: 1204 RVA: 0x00015762 File Offset: 0x00013962
		public static Code.Xor_ Xor
		{
			get
			{
				return new Code.Xor_(OpCodes.Xor);
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060004B5 RID: 1205 RVA: 0x0001576E File Offset: 0x0001396E
		public static Code.Shl_ Shl
		{
			get
			{
				return new Code.Shl_(OpCodes.Shl);
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060004B6 RID: 1206 RVA: 0x0001577A File Offset: 0x0001397A
		public static Code.Shr_ Shr
		{
			get
			{
				return new Code.Shr_(OpCodes.Shr);
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060004B7 RID: 1207 RVA: 0x00015786 File Offset: 0x00013986
		public static Code.Shr_Un_ Shr_Un
		{
			get
			{
				return new Code.Shr_Un_(OpCodes.Shr_Un);
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060004B8 RID: 1208 RVA: 0x00015792 File Offset: 0x00013992
		public static Code.Neg_ Neg
		{
			get
			{
				return new Code.Neg_(OpCodes.Neg);
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060004B9 RID: 1209 RVA: 0x0001579E File Offset: 0x0001399E
		public static Code.Not_ Not
		{
			get
			{
				return new Code.Not_(OpCodes.Not);
			}
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060004BA RID: 1210 RVA: 0x000157AA File Offset: 0x000139AA
		public static Code.Conv_I1_ Conv_I1
		{
			get
			{
				return new Code.Conv_I1_(OpCodes.Conv_I1);
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x060004BB RID: 1211 RVA: 0x000157B6 File Offset: 0x000139B6
		public static Code.Conv_I2_ Conv_I2
		{
			get
			{
				return new Code.Conv_I2_(OpCodes.Conv_I2);
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x060004BC RID: 1212 RVA: 0x000157C2 File Offset: 0x000139C2
		public static Code.Conv_I4_ Conv_I4
		{
			get
			{
				return new Code.Conv_I4_(OpCodes.Conv_I4);
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060004BD RID: 1213 RVA: 0x000157CE File Offset: 0x000139CE
		public static Code.Conv_I8_ Conv_I8
		{
			get
			{
				return new Code.Conv_I8_(OpCodes.Conv_I8);
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060004BE RID: 1214 RVA: 0x000157DA File Offset: 0x000139DA
		public static Code.Conv_R4_ Conv_R4
		{
			get
			{
				return new Code.Conv_R4_(OpCodes.Conv_R4);
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060004BF RID: 1215 RVA: 0x000157E6 File Offset: 0x000139E6
		public static Code.Conv_R8_ Conv_R8
		{
			get
			{
				return new Code.Conv_R8_(OpCodes.Conv_R8);
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060004C0 RID: 1216 RVA: 0x000157F2 File Offset: 0x000139F2
		public static Code.Conv_U4_ Conv_U4
		{
			get
			{
				return new Code.Conv_U4_(OpCodes.Conv_U4);
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060004C1 RID: 1217 RVA: 0x000157FE File Offset: 0x000139FE
		public static Code.Conv_U8_ Conv_U8
		{
			get
			{
				return new Code.Conv_U8_(OpCodes.Conv_U8);
			}
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060004C2 RID: 1218 RVA: 0x0001580A File Offset: 0x00013A0A
		public static Code.Callvirt_ Callvirt
		{
			get
			{
				return new Code.Callvirt_(OpCodes.Callvirt);
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060004C3 RID: 1219 RVA: 0x00015816 File Offset: 0x00013A16
		public static Code.Cpobj_ Cpobj
		{
			get
			{
				return new Code.Cpobj_(OpCodes.Cpobj);
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060004C4 RID: 1220 RVA: 0x00015822 File Offset: 0x00013A22
		public static Code.Ldobj_ Ldobj
		{
			get
			{
				return new Code.Ldobj_(OpCodes.Ldobj);
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060004C5 RID: 1221 RVA: 0x0001582E File Offset: 0x00013A2E
		public static Code.Ldstr_ Ldstr
		{
			get
			{
				return new Code.Ldstr_(OpCodes.Ldstr);
			}
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060004C6 RID: 1222 RVA: 0x0001583A File Offset: 0x00013A3A
		public static Code.Newobj_ Newobj
		{
			get
			{
				return new Code.Newobj_(OpCodes.Newobj);
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060004C7 RID: 1223 RVA: 0x00015846 File Offset: 0x00013A46
		public static Code.Castclass_ Castclass
		{
			get
			{
				return new Code.Castclass_(OpCodes.Castclass);
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060004C8 RID: 1224 RVA: 0x00015852 File Offset: 0x00013A52
		public static Code.Isinst_ Isinst
		{
			get
			{
				return new Code.Isinst_(OpCodes.Isinst);
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060004C9 RID: 1225 RVA: 0x0001585E File Offset: 0x00013A5E
		public static Code.Conv_R_Un_ Conv_R_Un
		{
			get
			{
				return new Code.Conv_R_Un_(OpCodes.Conv_R_Un);
			}
		}

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060004CA RID: 1226 RVA: 0x0001586A File Offset: 0x00013A6A
		public static Code.Unbox_ Unbox
		{
			get
			{
				return new Code.Unbox_(OpCodes.Unbox);
			}
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060004CB RID: 1227 RVA: 0x00015876 File Offset: 0x00013A76
		public static Code.Throw_ Throw
		{
			get
			{
				return new Code.Throw_(OpCodes.Throw);
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060004CC RID: 1228 RVA: 0x00015882 File Offset: 0x00013A82
		public static Code.Ldfld_ Ldfld
		{
			get
			{
				return new Code.Ldfld_(OpCodes.Ldfld);
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060004CD RID: 1229 RVA: 0x0001588E File Offset: 0x00013A8E
		public static Code.Ldflda_ Ldflda
		{
			get
			{
				return new Code.Ldflda_(OpCodes.Ldflda);
			}
		}

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060004CE RID: 1230 RVA: 0x0001589A File Offset: 0x00013A9A
		public static Code.Stfld_ Stfld
		{
			get
			{
				return new Code.Stfld_(OpCodes.Stfld);
			}
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060004CF RID: 1231 RVA: 0x000158A6 File Offset: 0x00013AA6
		public static Code.Ldsfld_ Ldsfld
		{
			get
			{
				return new Code.Ldsfld_(OpCodes.Ldsfld);
			}
		}

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060004D0 RID: 1232 RVA: 0x000158B2 File Offset: 0x00013AB2
		public static Code.Ldsflda_ Ldsflda
		{
			get
			{
				return new Code.Ldsflda_(OpCodes.Ldsflda);
			}
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060004D1 RID: 1233 RVA: 0x000158BE File Offset: 0x00013ABE
		public static Code.Stsfld_ Stsfld
		{
			get
			{
				return new Code.Stsfld_(OpCodes.Stsfld);
			}
		}

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060004D2 RID: 1234 RVA: 0x000158CA File Offset: 0x00013ACA
		public static Code.Stobj_ Stobj
		{
			get
			{
				return new Code.Stobj_(OpCodes.Stobj);
			}
		}

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x060004D3 RID: 1235 RVA: 0x000158D6 File Offset: 0x00013AD6
		public static Code.Conv_Ovf_I1_Un_ Conv_Ovf_I1_Un
		{
			get
			{
				return new Code.Conv_Ovf_I1_Un_(OpCodes.Conv_Ovf_I1_Un);
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x060004D4 RID: 1236 RVA: 0x000158E2 File Offset: 0x00013AE2
		public static Code.Conv_Ovf_I2_Un_ Conv_Ovf_I2_Un
		{
			get
			{
				return new Code.Conv_Ovf_I2_Un_(OpCodes.Conv_Ovf_I2_Un);
			}
		}

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x060004D5 RID: 1237 RVA: 0x000158EE File Offset: 0x00013AEE
		public static Code.Conv_Ovf_I4_Un_ Conv_Ovf_I4_Un
		{
			get
			{
				return new Code.Conv_Ovf_I4_Un_(OpCodes.Conv_Ovf_I4_Un);
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x060004D6 RID: 1238 RVA: 0x000158FA File Offset: 0x00013AFA
		public static Code.Conv_Ovf_I8_Un_ Conv_Ovf_I8_Un
		{
			get
			{
				return new Code.Conv_Ovf_I8_Un_(OpCodes.Conv_Ovf_I8_Un);
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x060004D7 RID: 1239 RVA: 0x00015906 File Offset: 0x00013B06
		public static Code.Conv_Ovf_U1_Un_ Conv_Ovf_U1_Un
		{
			get
			{
				return new Code.Conv_Ovf_U1_Un_(OpCodes.Conv_Ovf_U1_Un);
			}
		}

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x060004D8 RID: 1240 RVA: 0x00015912 File Offset: 0x00013B12
		public static Code.Conv_Ovf_U2_Un_ Conv_Ovf_U2_Un
		{
			get
			{
				return new Code.Conv_Ovf_U2_Un_(OpCodes.Conv_Ovf_U2_Un);
			}
		}

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x060004D9 RID: 1241 RVA: 0x0001591E File Offset: 0x00013B1E
		public static Code.Conv_Ovf_U4_Un_ Conv_Ovf_U4_Un
		{
			get
			{
				return new Code.Conv_Ovf_U4_Un_(OpCodes.Conv_Ovf_U4_Un);
			}
		}

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x060004DA RID: 1242 RVA: 0x0001592A File Offset: 0x00013B2A
		public static Code.Conv_Ovf_U8_Un_ Conv_Ovf_U8_Un
		{
			get
			{
				return new Code.Conv_Ovf_U8_Un_(OpCodes.Conv_Ovf_U8_Un);
			}
		}

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x060004DB RID: 1243 RVA: 0x00015936 File Offset: 0x00013B36
		public static Code.Conv_Ovf_I_Un_ Conv_Ovf_I_Un
		{
			get
			{
				return new Code.Conv_Ovf_I_Un_(OpCodes.Conv_Ovf_I_Un);
			}
		}

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x060004DC RID: 1244 RVA: 0x00015942 File Offset: 0x00013B42
		public static Code.Conv_Ovf_U_Un_ Conv_Ovf_U_Un
		{
			get
			{
				return new Code.Conv_Ovf_U_Un_(OpCodes.Conv_Ovf_U_Un);
			}
		}

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x060004DD RID: 1245 RVA: 0x0001594E File Offset: 0x00013B4E
		public static Code.Box_ Box
		{
			get
			{
				return new Code.Box_(OpCodes.Box);
			}
		}

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x060004DE RID: 1246 RVA: 0x0001595A File Offset: 0x00013B5A
		public static Code.Newarr_ Newarr
		{
			get
			{
				return new Code.Newarr_(OpCodes.Newarr);
			}
		}

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x060004DF RID: 1247 RVA: 0x00015966 File Offset: 0x00013B66
		public static Code.Ldlen_ Ldlen
		{
			get
			{
				return new Code.Ldlen_(OpCodes.Ldlen);
			}
		}

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x060004E0 RID: 1248 RVA: 0x00015972 File Offset: 0x00013B72
		public static Code.Ldelema_ Ldelema
		{
			get
			{
				return new Code.Ldelema_(OpCodes.Ldelema);
			}
		}

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x060004E1 RID: 1249 RVA: 0x0001597E File Offset: 0x00013B7E
		public static Code.Ldelem_I1_ Ldelem_I1
		{
			get
			{
				return new Code.Ldelem_I1_(OpCodes.Ldelem_I1);
			}
		}

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x060004E2 RID: 1250 RVA: 0x0001598A File Offset: 0x00013B8A
		public static Code.Ldelem_U1_ Ldelem_U1
		{
			get
			{
				return new Code.Ldelem_U1_(OpCodes.Ldelem_U1);
			}
		}

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x060004E3 RID: 1251 RVA: 0x00015996 File Offset: 0x00013B96
		public static Code.Ldelem_I2_ Ldelem_I2
		{
			get
			{
				return new Code.Ldelem_I2_(OpCodes.Ldelem_I2);
			}
		}

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x060004E4 RID: 1252 RVA: 0x000159A2 File Offset: 0x00013BA2
		public static Code.Ldelem_U2_ Ldelem_U2
		{
			get
			{
				return new Code.Ldelem_U2_(OpCodes.Ldelem_U2);
			}
		}

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x060004E5 RID: 1253 RVA: 0x000159AE File Offset: 0x00013BAE
		public static Code.Ldelem_I4_ Ldelem_I4
		{
			get
			{
				return new Code.Ldelem_I4_(OpCodes.Ldelem_I4);
			}
		}

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x060004E6 RID: 1254 RVA: 0x000159BA File Offset: 0x00013BBA
		public static Code.Ldelem_U4_ Ldelem_U4
		{
			get
			{
				return new Code.Ldelem_U4_(OpCodes.Ldelem_U4);
			}
		}

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x060004E7 RID: 1255 RVA: 0x000159C6 File Offset: 0x00013BC6
		public static Code.Ldelem_I8_ Ldelem_I8
		{
			get
			{
				return new Code.Ldelem_I8_(OpCodes.Ldelem_I8);
			}
		}

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x060004E8 RID: 1256 RVA: 0x000159D2 File Offset: 0x00013BD2
		public static Code.Ldelem_I_ Ldelem_I
		{
			get
			{
				return new Code.Ldelem_I_(OpCodes.Ldelem_I);
			}
		}

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x060004E9 RID: 1257 RVA: 0x000159DE File Offset: 0x00013BDE
		public static Code.Ldelem_R4_ Ldelem_R4
		{
			get
			{
				return new Code.Ldelem_R4_(OpCodes.Ldelem_R4);
			}
		}

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x060004EA RID: 1258 RVA: 0x000159EA File Offset: 0x00013BEA
		public static Code.Ldelem_R8_ Ldelem_R8
		{
			get
			{
				return new Code.Ldelem_R8_(OpCodes.Ldelem_R8);
			}
		}

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x060004EB RID: 1259 RVA: 0x000159F6 File Offset: 0x00013BF6
		public static Code.Ldelem_Ref_ Ldelem_Ref
		{
			get
			{
				return new Code.Ldelem_Ref_(OpCodes.Ldelem_Ref);
			}
		}

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x060004EC RID: 1260 RVA: 0x00015A02 File Offset: 0x00013C02
		public static Code.Stelem_I_ Stelem_I
		{
			get
			{
				return new Code.Stelem_I_(OpCodes.Stelem_I);
			}
		}

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x060004ED RID: 1261 RVA: 0x00015A0E File Offset: 0x00013C0E
		public static Code.Stelem_I1_ Stelem_I1
		{
			get
			{
				return new Code.Stelem_I1_(OpCodes.Stelem_I1);
			}
		}

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x060004EE RID: 1262 RVA: 0x00015A1A File Offset: 0x00013C1A
		public static Code.Stelem_I2_ Stelem_I2
		{
			get
			{
				return new Code.Stelem_I2_(OpCodes.Stelem_I2);
			}
		}

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x060004EF RID: 1263 RVA: 0x00015A26 File Offset: 0x00013C26
		public static Code.Stelem_I4_ Stelem_I4
		{
			get
			{
				return new Code.Stelem_I4_(OpCodes.Stelem_I4);
			}
		}

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x060004F0 RID: 1264 RVA: 0x00015A32 File Offset: 0x00013C32
		public static Code.Stelem_I8_ Stelem_I8
		{
			get
			{
				return new Code.Stelem_I8_(OpCodes.Stelem_I8);
			}
		}

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x060004F1 RID: 1265 RVA: 0x00015A3E File Offset: 0x00013C3E
		public static Code.Stelem_R4_ Stelem_R4
		{
			get
			{
				return new Code.Stelem_R4_(OpCodes.Stelem_R4);
			}
		}

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x060004F2 RID: 1266 RVA: 0x00015A4A File Offset: 0x00013C4A
		public static Code.Stelem_R8_ Stelem_R8
		{
			get
			{
				return new Code.Stelem_R8_(OpCodes.Stelem_R8);
			}
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x060004F3 RID: 1267 RVA: 0x00015A56 File Offset: 0x00013C56
		public static Code.Stelem_Ref_ Stelem_Ref
		{
			get
			{
				return new Code.Stelem_Ref_(OpCodes.Stelem_Ref);
			}
		}

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x060004F4 RID: 1268 RVA: 0x00015A62 File Offset: 0x00013C62
		public static Code.Ldelem_ Ldelem
		{
			get
			{
				return new Code.Ldelem_(OpCodes.Ldelem);
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x060004F5 RID: 1269 RVA: 0x00015A6E File Offset: 0x00013C6E
		public static Code.Stelem_ Stelem
		{
			get
			{
				return new Code.Stelem_(OpCodes.Stelem);
			}
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x060004F6 RID: 1270 RVA: 0x00015A7A File Offset: 0x00013C7A
		public static Code.Unbox_Any_ Unbox_Any
		{
			get
			{
				return new Code.Unbox_Any_(OpCodes.Unbox_Any);
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x060004F7 RID: 1271 RVA: 0x00015A86 File Offset: 0x00013C86
		public static Code.Conv_Ovf_I1_ Conv_Ovf_I1
		{
			get
			{
				return new Code.Conv_Ovf_I1_(OpCodes.Conv_Ovf_I1);
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x060004F8 RID: 1272 RVA: 0x00015A92 File Offset: 0x00013C92
		public static Code.Conv_Ovf_U1_ Conv_Ovf_U1
		{
			get
			{
				return new Code.Conv_Ovf_U1_(OpCodes.Conv_Ovf_U1);
			}
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x060004F9 RID: 1273 RVA: 0x00015A9E File Offset: 0x00013C9E
		public static Code.Conv_Ovf_I2_ Conv_Ovf_I2
		{
			get
			{
				return new Code.Conv_Ovf_I2_(OpCodes.Conv_Ovf_I2);
			}
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x060004FA RID: 1274 RVA: 0x00015AAA File Offset: 0x00013CAA
		public static Code.Conv_Ovf_U2_ Conv_Ovf_U2
		{
			get
			{
				return new Code.Conv_Ovf_U2_(OpCodes.Conv_Ovf_U2);
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x060004FB RID: 1275 RVA: 0x00015AB6 File Offset: 0x00013CB6
		public static Code.Conv_Ovf_I4_ Conv_Ovf_I4
		{
			get
			{
				return new Code.Conv_Ovf_I4_(OpCodes.Conv_Ovf_I4);
			}
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x060004FC RID: 1276 RVA: 0x00015AC2 File Offset: 0x00013CC2
		public static Code.Conv_Ovf_U4_ Conv_Ovf_U4
		{
			get
			{
				return new Code.Conv_Ovf_U4_(OpCodes.Conv_Ovf_U4);
			}
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x060004FD RID: 1277 RVA: 0x00015ACE File Offset: 0x00013CCE
		public static Code.Conv_Ovf_I8_ Conv_Ovf_I8
		{
			get
			{
				return new Code.Conv_Ovf_I8_(OpCodes.Conv_Ovf_I8);
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x060004FE RID: 1278 RVA: 0x00015ADA File Offset: 0x00013CDA
		public static Code.Conv_Ovf_U8_ Conv_Ovf_U8
		{
			get
			{
				return new Code.Conv_Ovf_U8_(OpCodes.Conv_Ovf_U8);
			}
		}

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x060004FF RID: 1279 RVA: 0x00015AE6 File Offset: 0x00013CE6
		public static Code.Refanyval_ Refanyval
		{
			get
			{
				return new Code.Refanyval_(OpCodes.Refanyval);
			}
		}

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x06000500 RID: 1280 RVA: 0x00015AF2 File Offset: 0x00013CF2
		public static Code.Ckfinite_ Ckfinite
		{
			get
			{
				return new Code.Ckfinite_(OpCodes.Ckfinite);
			}
		}

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x06000501 RID: 1281 RVA: 0x00015AFE File Offset: 0x00013CFE
		public static Code.Mkrefany_ Mkrefany
		{
			get
			{
				return new Code.Mkrefany_(OpCodes.Mkrefany);
			}
		}

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x06000502 RID: 1282 RVA: 0x00015B0A File Offset: 0x00013D0A
		public static Code.Ldtoken_ Ldtoken
		{
			get
			{
				return new Code.Ldtoken_(OpCodes.Ldtoken);
			}
		}

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x06000503 RID: 1283 RVA: 0x00015B16 File Offset: 0x00013D16
		public static Code.Conv_U2_ Conv_U2
		{
			get
			{
				return new Code.Conv_U2_(OpCodes.Conv_U2);
			}
		}

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x06000504 RID: 1284 RVA: 0x00015B22 File Offset: 0x00013D22
		public static Code.Conv_U1_ Conv_U1
		{
			get
			{
				return new Code.Conv_U1_(OpCodes.Conv_U1);
			}
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x06000505 RID: 1285 RVA: 0x00015B2E File Offset: 0x00013D2E
		public static Code.Conv_I_ Conv_I
		{
			get
			{
				return new Code.Conv_I_(OpCodes.Conv_I);
			}
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x06000506 RID: 1286 RVA: 0x00015B3A File Offset: 0x00013D3A
		public static Code.Conv_Ovf_I_ Conv_Ovf_I
		{
			get
			{
				return new Code.Conv_Ovf_I_(OpCodes.Conv_Ovf_I);
			}
		}

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x06000507 RID: 1287 RVA: 0x00015B46 File Offset: 0x00013D46
		public static Code.Conv_Ovf_U_ Conv_Ovf_U
		{
			get
			{
				return new Code.Conv_Ovf_U_(OpCodes.Conv_Ovf_U);
			}
		}

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000508 RID: 1288 RVA: 0x00015B52 File Offset: 0x00013D52
		public static Code.Add_Ovf_ Add_Ovf
		{
			get
			{
				return new Code.Add_Ovf_(OpCodes.Add_Ovf);
			}
		}

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000509 RID: 1289 RVA: 0x00015B5E File Offset: 0x00013D5E
		public static Code.Add_Ovf_Un_ Add_Ovf_Un
		{
			get
			{
				return new Code.Add_Ovf_Un_(OpCodes.Add_Ovf_Un);
			}
		}

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x0600050A RID: 1290 RVA: 0x00015B6A File Offset: 0x00013D6A
		public static Code.Mul_Ovf_ Mul_Ovf
		{
			get
			{
				return new Code.Mul_Ovf_(OpCodes.Mul_Ovf);
			}
		}

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x0600050B RID: 1291 RVA: 0x00015B76 File Offset: 0x00013D76
		public static Code.Mul_Ovf_Un_ Mul_Ovf_Un
		{
			get
			{
				return new Code.Mul_Ovf_Un_(OpCodes.Mul_Ovf_Un);
			}
		}

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x0600050C RID: 1292 RVA: 0x00015B82 File Offset: 0x00013D82
		public static Code.Sub_Ovf_ Sub_Ovf
		{
			get
			{
				return new Code.Sub_Ovf_(OpCodes.Sub_Ovf);
			}
		}

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x0600050D RID: 1293 RVA: 0x00015B8E File Offset: 0x00013D8E
		public static Code.Sub_Ovf_Un_ Sub_Ovf_Un
		{
			get
			{
				return new Code.Sub_Ovf_Un_(OpCodes.Sub_Ovf_Un);
			}
		}

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x0600050E RID: 1294 RVA: 0x00015B9A File Offset: 0x00013D9A
		public static Code.Endfinally_ Endfinally
		{
			get
			{
				return new Code.Endfinally_(OpCodes.Endfinally);
			}
		}

		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x0600050F RID: 1295 RVA: 0x00015BA6 File Offset: 0x00013DA6
		public static Code.Leave_ Leave
		{
			get
			{
				return new Code.Leave_(OpCodes.Leave);
			}
		}

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x06000510 RID: 1296 RVA: 0x00015BB2 File Offset: 0x00013DB2
		public static Code.Leave_S_ Leave_S
		{
			get
			{
				return new Code.Leave_S_(OpCodes.Leave_S);
			}
		}

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x06000511 RID: 1297 RVA: 0x00015BBE File Offset: 0x00013DBE
		public static Code.Stind_I_ Stind_I
		{
			get
			{
				return new Code.Stind_I_(OpCodes.Stind_I);
			}
		}

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x06000512 RID: 1298 RVA: 0x00015BCA File Offset: 0x00013DCA
		public static Code.Conv_U_ Conv_U
		{
			get
			{
				return new Code.Conv_U_(OpCodes.Conv_U);
			}
		}

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x06000513 RID: 1299 RVA: 0x00015BD6 File Offset: 0x00013DD6
		public static Code.Prefix7_ Prefix7
		{
			get
			{
				return new Code.Prefix7_(OpCodes.Prefix7);
			}
		}

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x06000514 RID: 1300 RVA: 0x00015BE2 File Offset: 0x00013DE2
		public static Code.Prefix6_ Prefix6
		{
			get
			{
				return new Code.Prefix6_(OpCodes.Prefix6);
			}
		}

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x06000515 RID: 1301 RVA: 0x00015BEE File Offset: 0x00013DEE
		public static Code.Prefix5_ Prefix5
		{
			get
			{
				return new Code.Prefix5_(OpCodes.Prefix5);
			}
		}

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x06000516 RID: 1302 RVA: 0x00015BFA File Offset: 0x00013DFA
		public static Code.Prefix4_ Prefix4
		{
			get
			{
				return new Code.Prefix4_(OpCodes.Prefix4);
			}
		}

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x06000517 RID: 1303 RVA: 0x00015C06 File Offset: 0x00013E06
		public static Code.Prefix3_ Prefix3
		{
			get
			{
				return new Code.Prefix3_(OpCodes.Prefix3);
			}
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x06000518 RID: 1304 RVA: 0x00015C12 File Offset: 0x00013E12
		public static Code.Prefix2_ Prefix2
		{
			get
			{
				return new Code.Prefix2_(OpCodes.Prefix2);
			}
		}

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x06000519 RID: 1305 RVA: 0x00015C1E File Offset: 0x00013E1E
		public static Code.Prefix1_ Prefix1
		{
			get
			{
				return new Code.Prefix1_(OpCodes.Prefix1);
			}
		}

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x0600051A RID: 1306 RVA: 0x00015C2A File Offset: 0x00013E2A
		public static Code.Prefixref_ Prefixref
		{
			get
			{
				return new Code.Prefixref_(OpCodes.Prefixref);
			}
		}

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x0600051B RID: 1307 RVA: 0x00015C36 File Offset: 0x00013E36
		public static Code.Arglist_ Arglist
		{
			get
			{
				return new Code.Arglist_(OpCodes.Arglist);
			}
		}

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x0600051C RID: 1308 RVA: 0x00015C42 File Offset: 0x00013E42
		public static Code.Ceq_ Ceq
		{
			get
			{
				return new Code.Ceq_(OpCodes.Ceq);
			}
		}

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x0600051D RID: 1309 RVA: 0x00015C4E File Offset: 0x00013E4E
		public static Code.Cgt_ Cgt
		{
			get
			{
				return new Code.Cgt_(OpCodes.Cgt);
			}
		}

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x0600051E RID: 1310 RVA: 0x00015C5A File Offset: 0x00013E5A
		public static Code.Cgt_Un_ Cgt_Un
		{
			get
			{
				return new Code.Cgt_Un_(OpCodes.Cgt_Un);
			}
		}

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x0600051F RID: 1311 RVA: 0x00015C66 File Offset: 0x00013E66
		public static Code.Clt_ Clt
		{
			get
			{
				return new Code.Clt_(OpCodes.Clt);
			}
		}

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x06000520 RID: 1312 RVA: 0x00015C72 File Offset: 0x00013E72
		public static Code.Clt_Un_ Clt_Un
		{
			get
			{
				return new Code.Clt_Un_(OpCodes.Clt_Un);
			}
		}

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x06000521 RID: 1313 RVA: 0x00015C7E File Offset: 0x00013E7E
		public static Code.Ldftn_ Ldftn
		{
			get
			{
				return new Code.Ldftn_(OpCodes.Ldftn);
			}
		}

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x06000522 RID: 1314 RVA: 0x00015C8A File Offset: 0x00013E8A
		public static Code.Ldvirtftn_ Ldvirtftn
		{
			get
			{
				return new Code.Ldvirtftn_(OpCodes.Ldvirtftn);
			}
		}

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x06000523 RID: 1315 RVA: 0x00015C96 File Offset: 0x00013E96
		public static Code.Ldarg_ Ldarg
		{
			get
			{
				return new Code.Ldarg_(OpCodes.Ldarg);
			}
		}

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x06000524 RID: 1316 RVA: 0x00015CA2 File Offset: 0x00013EA2
		public static Code.Ldarga_ Ldarga
		{
			get
			{
				return new Code.Ldarga_(OpCodes.Ldarga);
			}
		}

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x06000525 RID: 1317 RVA: 0x00015CAE File Offset: 0x00013EAE
		public static Code.Starg_ Starg
		{
			get
			{
				return new Code.Starg_(OpCodes.Starg);
			}
		}

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000526 RID: 1318 RVA: 0x00015CBA File Offset: 0x00013EBA
		public static Code.Ldloc_ Ldloc
		{
			get
			{
				return new Code.Ldloc_(OpCodes.Ldloc);
			}
		}

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x06000527 RID: 1319 RVA: 0x00015CC6 File Offset: 0x00013EC6
		public static Code.Ldloca_ Ldloca
		{
			get
			{
				return new Code.Ldloca_(OpCodes.Ldloca);
			}
		}

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x06000528 RID: 1320 RVA: 0x00015CD2 File Offset: 0x00013ED2
		public static Code.Stloc_ Stloc
		{
			get
			{
				return new Code.Stloc_(OpCodes.Stloc);
			}
		}

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x06000529 RID: 1321 RVA: 0x00015CDE File Offset: 0x00013EDE
		public static Code.Localloc_ Localloc
		{
			get
			{
				return new Code.Localloc_(OpCodes.Localloc);
			}
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x0600052A RID: 1322 RVA: 0x00015CEA File Offset: 0x00013EEA
		public static Code.Endfilter_ Endfilter
		{
			get
			{
				return new Code.Endfilter_(OpCodes.Endfilter);
			}
		}

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x0600052B RID: 1323 RVA: 0x00015CF6 File Offset: 0x00013EF6
		public static Code.Unaligned_ Unaligned
		{
			get
			{
				return new Code.Unaligned_(OpCodes.Unaligned);
			}
		}

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x0600052C RID: 1324 RVA: 0x00015D02 File Offset: 0x00013F02
		public static Code.Volatile_ Volatile
		{
			get
			{
				return new Code.Volatile_(OpCodes.Volatile);
			}
		}

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x0600052D RID: 1325 RVA: 0x00015D0E File Offset: 0x00013F0E
		public static Code.Tailcall_ Tailcall
		{
			get
			{
				return new Code.Tailcall_(OpCodes.Tailcall);
			}
		}

		// Token: 0x17000101 RID: 257
		// (get) Token: 0x0600052E RID: 1326 RVA: 0x00015D1A File Offset: 0x00013F1A
		public static Code.Initobj_ Initobj
		{
			get
			{
				return new Code.Initobj_(OpCodes.Initobj);
			}
		}

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x0600052F RID: 1327 RVA: 0x00015D26 File Offset: 0x00013F26
		public static Code.Constrained_ Constrained
		{
			get
			{
				return new Code.Constrained_(OpCodes.Constrained);
			}
		}

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x06000530 RID: 1328 RVA: 0x00015D32 File Offset: 0x00013F32
		public static Code.Cpblk_ Cpblk
		{
			get
			{
				return new Code.Cpblk_(OpCodes.Cpblk);
			}
		}

		// Token: 0x17000104 RID: 260
		// (get) Token: 0x06000531 RID: 1329 RVA: 0x00015D3E File Offset: 0x00013F3E
		public static Code.Initblk_ Initblk
		{
			get
			{
				return new Code.Initblk_(OpCodes.Initblk);
			}
		}

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x06000532 RID: 1330 RVA: 0x00015D4A File Offset: 0x00013F4A
		public static Code.Rethrow_ Rethrow
		{
			get
			{
				return new Code.Rethrow_(OpCodes.Rethrow);
			}
		}

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x06000533 RID: 1331 RVA: 0x00015D56 File Offset: 0x00013F56
		public static Code.Sizeof_ Sizeof
		{
			get
			{
				return new Code.Sizeof_(OpCodes.Sizeof);
			}
		}

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x06000534 RID: 1332 RVA: 0x00015D62 File Offset: 0x00013F62
		public static Code.Refanytype_ Refanytype
		{
			get
			{
				return new Code.Refanytype_(OpCodes.Refanytype);
			}
		}

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x06000535 RID: 1333 RVA: 0x00015D6E File Offset: 0x00013F6E
		public static Code.Readonly_ Readonly
		{
			get
			{
				return new Code.Readonly_(OpCodes.Readonly);
			}
		}

		// Token: 0x020000C5 RID: 197
		public class Operand_ : CodeMatch
		{
			// Token: 0x17000109 RID: 265
			public Code.Operand_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Operand_)base.Set(operand, name);
				}
			}

			// Token: 0x06000537 RID: 1335 RVA: 0x00015D8C File Offset: 0x00013F8C
			public Operand_()
				: base(null, null, null)
			{
			}
		}

		// Token: 0x020000C6 RID: 198
		public class Nop_ : CodeMatch
		{
			// Token: 0x06000538 RID: 1336 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Nop_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700010A RID: 266
			public Code.Nop_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Nop_)base.Set(OpCodes.Nop, operand, name);
				}
			}
		}

		// Token: 0x020000C7 RID: 199
		public class Break_ : CodeMatch
		{
			// Token: 0x0600053A RID: 1338 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Break_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700010B RID: 267
			public Code.Break_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Break_)base.Set(OpCodes.Break, operand, name);
				}
			}
		}

		// Token: 0x020000C8 RID: 200
		public class Ldarg_0_ : CodeMatch
		{
			// Token: 0x0600053C RID: 1340 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarg_0_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700010C RID: 268
			public Code.Ldarg_0_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarg_0_)base.Set(OpCodes.Ldarg_0, operand, name);
				}
			}
		}

		// Token: 0x020000C9 RID: 201
		public class Ldarg_1_ : CodeMatch
		{
			// Token: 0x0600053E RID: 1342 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarg_1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700010D RID: 269
			public Code.Ldarg_1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarg_1_)base.Set(OpCodes.Ldarg_1, operand, name);
				}
			}
		}

		// Token: 0x020000CA RID: 202
		public class Ldarg_2_ : CodeMatch
		{
			// Token: 0x06000540 RID: 1344 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarg_2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700010E RID: 270
			public Code.Ldarg_2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarg_2_)base.Set(OpCodes.Ldarg_2, operand, name);
				}
			}
		}

		// Token: 0x020000CB RID: 203
		public class Ldarg_3_ : CodeMatch
		{
			// Token: 0x06000542 RID: 1346 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarg_3_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700010F RID: 271
			public Code.Ldarg_3_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarg_3_)base.Set(OpCodes.Ldarg_3, operand, name);
				}
			}
		}

		// Token: 0x020000CC RID: 204
		public class Ldloc_0_ : CodeMatch
		{
			// Token: 0x06000544 RID: 1348 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloc_0_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000110 RID: 272
			public Code.Ldloc_0_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloc_0_)base.Set(OpCodes.Ldloc_0, operand, name);
				}
			}
		}

		// Token: 0x020000CD RID: 205
		public class Ldloc_1_ : CodeMatch
		{
			// Token: 0x06000546 RID: 1350 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloc_1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000111 RID: 273
			public Code.Ldloc_1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloc_1_)base.Set(OpCodes.Ldloc_1, operand, name);
				}
			}
		}

		// Token: 0x020000CE RID: 206
		public class Ldloc_2_ : CodeMatch
		{
			// Token: 0x06000548 RID: 1352 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloc_2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000112 RID: 274
			public Code.Ldloc_2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloc_2_)base.Set(OpCodes.Ldloc_2, operand, name);
				}
			}
		}

		// Token: 0x020000CF RID: 207
		public class Ldloc_3_ : CodeMatch
		{
			// Token: 0x0600054A RID: 1354 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloc_3_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000113 RID: 275
			public Code.Ldloc_3_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloc_3_)base.Set(OpCodes.Ldloc_3, operand, name);
				}
			}
		}

		// Token: 0x020000D0 RID: 208
		public class Stloc_0_ : CodeMatch
		{
			// Token: 0x0600054C RID: 1356 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stloc_0_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000114 RID: 276
			public Code.Stloc_0_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stloc_0_)base.Set(OpCodes.Stloc_0, operand, name);
				}
			}
		}

		// Token: 0x020000D1 RID: 209
		public class Stloc_1_ : CodeMatch
		{
			// Token: 0x0600054E RID: 1358 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stloc_1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000115 RID: 277
			public Code.Stloc_1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stloc_1_)base.Set(OpCodes.Stloc_1, operand, name);
				}
			}
		}

		// Token: 0x020000D2 RID: 210
		public class Stloc_2_ : CodeMatch
		{
			// Token: 0x06000550 RID: 1360 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stloc_2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000116 RID: 278
			public Code.Stloc_2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stloc_2_)base.Set(OpCodes.Stloc_2, operand, name);
				}
			}
		}

		// Token: 0x020000D3 RID: 211
		public class Stloc_3_ : CodeMatch
		{
			// Token: 0x06000552 RID: 1362 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stloc_3_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000117 RID: 279
			public Code.Stloc_3_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stloc_3_)base.Set(OpCodes.Stloc_3, operand, name);
				}
			}
		}

		// Token: 0x020000D4 RID: 212
		public class Ldarg_S_ : CodeMatch
		{
			// Token: 0x06000554 RID: 1364 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarg_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000118 RID: 280
			public Code.Ldarg_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarg_S_)base.Set(OpCodes.Ldarg_S, operand, name);
				}
			}
		}

		// Token: 0x020000D5 RID: 213
		public class Ldarga_S_ : CodeMatch
		{
			// Token: 0x06000556 RID: 1366 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarga_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000119 RID: 281
			public Code.Ldarga_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarga_S_)base.Set(OpCodes.Ldarga_S, operand, name);
				}
			}
		}

		// Token: 0x020000D6 RID: 214
		public class Starg_S_ : CodeMatch
		{
			// Token: 0x06000558 RID: 1368 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Starg_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700011A RID: 282
			public Code.Starg_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Starg_S_)base.Set(OpCodes.Starg_S, operand, name);
				}
			}
		}

		// Token: 0x020000D7 RID: 215
		public class Ldloc_S_ : CodeMatch
		{
			// Token: 0x0600055A RID: 1370 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloc_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700011B RID: 283
			public Code.Ldloc_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloc_S_)base.Set(OpCodes.Ldloc_S, operand, name);
				}
			}
		}

		// Token: 0x020000D8 RID: 216
		public class Ldloca_S_ : CodeMatch
		{
			// Token: 0x0600055C RID: 1372 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloca_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700011C RID: 284
			public Code.Ldloca_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloca_S_)base.Set(OpCodes.Ldloca_S, operand, name);
				}
			}
		}

		// Token: 0x020000D9 RID: 217
		public class Stloc_S_ : CodeMatch
		{
			// Token: 0x0600055E RID: 1374 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stloc_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700011D RID: 285
			public Code.Stloc_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stloc_S_)base.Set(OpCodes.Stloc_S, operand, name);
				}
			}
		}

		// Token: 0x020000DA RID: 218
		public class Ldnull_ : CodeMatch
		{
			// Token: 0x06000560 RID: 1376 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldnull_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700011E RID: 286
			public Code.Ldnull_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldnull_)base.Set(OpCodes.Ldnull, operand, name);
				}
			}
		}

		// Token: 0x020000DB RID: 219
		public class Ldc_I4_M1_ : CodeMatch
		{
			// Token: 0x06000562 RID: 1378 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_M1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700011F RID: 287
			public Code.Ldc_I4_M1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_M1_)base.Set(OpCodes.Ldc_I4_M1, operand, name);
				}
			}
		}

		// Token: 0x020000DC RID: 220
		public class Ldc_I4_0_ : CodeMatch
		{
			// Token: 0x06000564 RID: 1380 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_0_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000120 RID: 288
			public Code.Ldc_I4_0_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_0_)base.Set(OpCodes.Ldc_I4_0, operand, name);
				}
			}
		}

		// Token: 0x020000DD RID: 221
		public class Ldc_I4_1_ : CodeMatch
		{
			// Token: 0x06000566 RID: 1382 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000121 RID: 289
			public Code.Ldc_I4_1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_1_)base.Set(OpCodes.Ldc_I4_1, operand, name);
				}
			}
		}

		// Token: 0x020000DE RID: 222
		public class Ldc_I4_2_ : CodeMatch
		{
			// Token: 0x06000568 RID: 1384 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000122 RID: 290
			public Code.Ldc_I4_2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_2_)base.Set(OpCodes.Ldc_I4_2, operand, name);
				}
			}
		}

		// Token: 0x020000DF RID: 223
		public class Ldc_I4_3_ : CodeMatch
		{
			// Token: 0x0600056A RID: 1386 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_3_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000123 RID: 291
			public Code.Ldc_I4_3_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_3_)base.Set(OpCodes.Ldc_I4_3, operand, name);
				}
			}
		}

		// Token: 0x020000E0 RID: 224
		public class Ldc_I4_4_ : CodeMatch
		{
			// Token: 0x0600056C RID: 1388 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000124 RID: 292
			public Code.Ldc_I4_4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_4_)base.Set(OpCodes.Ldc_I4_4, operand, name);
				}
			}
		}

		// Token: 0x020000E1 RID: 225
		public class Ldc_I4_5_ : CodeMatch
		{
			// Token: 0x0600056E RID: 1390 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_5_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000125 RID: 293
			public Code.Ldc_I4_5_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_5_)base.Set(OpCodes.Ldc_I4_5, operand, name);
				}
			}
		}

		// Token: 0x020000E2 RID: 226
		public class Ldc_I4_6_ : CodeMatch
		{
			// Token: 0x06000570 RID: 1392 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_6_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000126 RID: 294
			public Code.Ldc_I4_6_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_6_)base.Set(OpCodes.Ldc_I4_6, operand, name);
				}
			}
		}

		// Token: 0x020000E3 RID: 227
		public class Ldc_I4_7_ : CodeMatch
		{
			// Token: 0x06000572 RID: 1394 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_7_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000127 RID: 295
			public Code.Ldc_I4_7_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_7_)base.Set(OpCodes.Ldc_I4_7, operand, name);
				}
			}
		}

		// Token: 0x020000E4 RID: 228
		public class Ldc_I4_8_ : CodeMatch
		{
			// Token: 0x06000574 RID: 1396 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000128 RID: 296
			public Code.Ldc_I4_8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_8_)base.Set(OpCodes.Ldc_I4_8, operand, name);
				}
			}
		}

		// Token: 0x020000E5 RID: 229
		public class Ldc_I4_S_ : CodeMatch
		{
			// Token: 0x06000576 RID: 1398 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000129 RID: 297
			public Code.Ldc_I4_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_S_)base.Set(OpCodes.Ldc_I4_S, operand, name);
				}
			}
		}

		// Token: 0x020000E6 RID: 230
		public class Ldc_I4_ : CodeMatch
		{
			// Token: 0x06000578 RID: 1400 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700012A RID: 298
			public Code.Ldc_I4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I4_)base.Set(OpCodes.Ldc_I4, operand, name);
				}
			}
		}

		// Token: 0x020000E7 RID: 231
		public class Ldc_I8_ : CodeMatch
		{
			// Token: 0x0600057A RID: 1402 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_I8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700012B RID: 299
			public Code.Ldc_I8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_I8_)base.Set(OpCodes.Ldc_I8, operand, name);
				}
			}
		}

		// Token: 0x020000E8 RID: 232
		public class Ldc_R4_ : CodeMatch
		{
			// Token: 0x0600057C RID: 1404 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_R4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700012C RID: 300
			public Code.Ldc_R4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_R4_)base.Set(OpCodes.Ldc_R4, operand, name);
				}
			}
		}

		// Token: 0x020000E9 RID: 233
		public class Ldc_R8_ : CodeMatch
		{
			// Token: 0x0600057E RID: 1406 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldc_R8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700012D RID: 301
			public Code.Ldc_R8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldc_R8_)base.Set(OpCodes.Ldc_R8, operand, name);
				}
			}
		}

		// Token: 0x020000EA RID: 234
		public class Dup_ : CodeMatch
		{
			// Token: 0x06000580 RID: 1408 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Dup_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700012E RID: 302
			public Code.Dup_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Dup_)base.Set(OpCodes.Dup, operand, name);
				}
			}
		}

		// Token: 0x020000EB RID: 235
		public class Pop_ : CodeMatch
		{
			// Token: 0x06000582 RID: 1410 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Pop_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700012F RID: 303
			public Code.Pop_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Pop_)base.Set(OpCodes.Pop, operand, name);
				}
			}
		}

		// Token: 0x020000EC RID: 236
		public class Jmp_ : CodeMatch
		{
			// Token: 0x06000584 RID: 1412 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Jmp_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000130 RID: 304
			public Code.Jmp_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Jmp_)base.Set(OpCodes.Jmp, operand, name);
				}
			}
		}

		// Token: 0x020000ED RID: 237
		public class Call_ : CodeMatch
		{
			// Token: 0x06000586 RID: 1414 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Call_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000131 RID: 305
			public Code.Call_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Call_)base.Set(OpCodes.Call, operand, name);
				}
			}
		}

		// Token: 0x020000EE RID: 238
		public class Calli_ : CodeMatch
		{
			// Token: 0x06000588 RID: 1416 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Calli_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000132 RID: 306
			public Code.Calli_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Calli_)base.Set(OpCodes.Calli, operand, name);
				}
			}
		}

		// Token: 0x020000EF RID: 239
		public class Ret_ : CodeMatch
		{
			// Token: 0x0600058A RID: 1418 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ret_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000133 RID: 307
			public Code.Ret_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ret_)base.Set(OpCodes.Ret, operand, name);
				}
			}
		}

		// Token: 0x020000F0 RID: 240
		public class Br_S_ : CodeMatch
		{
			// Token: 0x0600058C RID: 1420 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Br_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000134 RID: 308
			public Code.Br_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Br_S_)base.Set(OpCodes.Br_S, operand, name);
				}
			}
		}

		// Token: 0x020000F1 RID: 241
		public class Brfalse_S_ : CodeMatch
		{
			// Token: 0x0600058E RID: 1422 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Brfalse_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000135 RID: 309
			public Code.Brfalse_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Brfalse_S_)base.Set(OpCodes.Brfalse_S, operand, name);
				}
			}
		}

		// Token: 0x020000F2 RID: 242
		public class Brtrue_S_ : CodeMatch
		{
			// Token: 0x06000590 RID: 1424 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Brtrue_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000136 RID: 310
			public Code.Brtrue_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Brtrue_S_)base.Set(OpCodes.Brtrue_S, operand, name);
				}
			}
		}

		// Token: 0x020000F3 RID: 243
		public class Beq_S_ : CodeMatch
		{
			// Token: 0x06000592 RID: 1426 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Beq_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000137 RID: 311
			public Code.Beq_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Beq_S_)base.Set(OpCodes.Beq_S, operand, name);
				}
			}
		}

		// Token: 0x020000F4 RID: 244
		public class Bge_S_ : CodeMatch
		{
			// Token: 0x06000594 RID: 1428 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bge_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000138 RID: 312
			public Code.Bge_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bge_S_)base.Set(OpCodes.Bge_S, operand, name);
				}
			}
		}

		// Token: 0x020000F5 RID: 245
		public class Bgt_S_ : CodeMatch
		{
			// Token: 0x06000596 RID: 1430 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bgt_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000139 RID: 313
			public Code.Bgt_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bgt_S_)base.Set(OpCodes.Bgt_S, operand, name);
				}
			}
		}

		// Token: 0x020000F6 RID: 246
		public class Ble_S_ : CodeMatch
		{
			// Token: 0x06000598 RID: 1432 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ble_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700013A RID: 314
			public Code.Ble_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ble_S_)base.Set(OpCodes.Ble_S, operand, name);
				}
			}
		}

		// Token: 0x020000F7 RID: 247
		public class Blt_S_ : CodeMatch
		{
			// Token: 0x0600059A RID: 1434 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Blt_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700013B RID: 315
			public Code.Blt_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Blt_S_)base.Set(OpCodes.Blt_S, operand, name);
				}
			}
		}

		// Token: 0x020000F8 RID: 248
		public class Bne_Un_S_ : CodeMatch
		{
			// Token: 0x0600059C RID: 1436 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bne_Un_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700013C RID: 316
			public Code.Bne_Un_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bne_Un_S_)base.Set(OpCodes.Bne_Un_S, operand, name);
				}
			}
		}

		// Token: 0x020000F9 RID: 249
		public class Bge_Un_S_ : CodeMatch
		{
			// Token: 0x0600059E RID: 1438 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bge_Un_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700013D RID: 317
			public Code.Bge_Un_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bge_Un_S_)base.Set(OpCodes.Bge_Un_S, operand, name);
				}
			}
		}

		// Token: 0x020000FA RID: 250
		public class Bgt_Un_S_ : CodeMatch
		{
			// Token: 0x060005A0 RID: 1440 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bgt_Un_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700013E RID: 318
			public Code.Bgt_Un_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bgt_Un_S_)base.Set(OpCodes.Bgt_Un_S, operand, name);
				}
			}
		}

		// Token: 0x020000FB RID: 251
		public class Ble_Un_S_ : CodeMatch
		{
			// Token: 0x060005A2 RID: 1442 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ble_Un_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700013F RID: 319
			public Code.Ble_Un_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ble_Un_S_)base.Set(OpCodes.Ble_Un_S, operand, name);
				}
			}
		}

		// Token: 0x020000FC RID: 252
		public class Blt_Un_S_ : CodeMatch
		{
			// Token: 0x060005A4 RID: 1444 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Blt_Un_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000140 RID: 320
			public Code.Blt_Un_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Blt_Un_S_)base.Set(OpCodes.Blt_Un_S, operand, name);
				}
			}
		}

		// Token: 0x020000FD RID: 253
		public class Br_ : CodeMatch
		{
			// Token: 0x060005A6 RID: 1446 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Br_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000141 RID: 321
			public Code.Br_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Br_)base.Set(OpCodes.Br, operand, name);
				}
			}
		}

		// Token: 0x020000FE RID: 254
		public class Brfalse_ : CodeMatch
		{
			// Token: 0x060005A8 RID: 1448 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Brfalse_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000142 RID: 322
			public Code.Brfalse_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Brfalse_)base.Set(OpCodes.Brfalse, operand, name);
				}
			}
		}

		// Token: 0x020000FF RID: 255
		public class Brtrue_ : CodeMatch
		{
			// Token: 0x060005AA RID: 1450 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Brtrue_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000143 RID: 323
			public Code.Brtrue_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Brtrue_)base.Set(OpCodes.Brtrue, operand, name);
				}
			}
		}

		// Token: 0x02000100 RID: 256
		public class Beq_ : CodeMatch
		{
			// Token: 0x060005AC RID: 1452 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Beq_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000144 RID: 324
			public Code.Beq_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Beq_)base.Set(OpCodes.Beq, operand, name);
				}
			}
		}

		// Token: 0x02000101 RID: 257
		public class Bge_ : CodeMatch
		{
			// Token: 0x060005AE RID: 1454 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bge_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000145 RID: 325
			public Code.Bge_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bge_)base.Set(OpCodes.Bge, operand, name);
				}
			}
		}

		// Token: 0x02000102 RID: 258
		public class Bgt_ : CodeMatch
		{
			// Token: 0x060005B0 RID: 1456 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bgt_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000146 RID: 326
			public Code.Bgt_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bgt_)base.Set(OpCodes.Bgt, operand, name);
				}
			}
		}

		// Token: 0x02000103 RID: 259
		public class Ble_ : CodeMatch
		{
			// Token: 0x060005B2 RID: 1458 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ble_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000147 RID: 327
			public Code.Ble_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ble_)base.Set(OpCodes.Ble, operand, name);
				}
			}
		}

		// Token: 0x02000104 RID: 260
		public class Blt_ : CodeMatch
		{
			// Token: 0x060005B4 RID: 1460 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Blt_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000148 RID: 328
			public Code.Blt_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Blt_)base.Set(OpCodes.Blt, operand, name);
				}
			}
		}

		// Token: 0x02000105 RID: 261
		public class Bne_Un_ : CodeMatch
		{
			// Token: 0x060005B6 RID: 1462 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bne_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000149 RID: 329
			public Code.Bne_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bne_Un_)base.Set(OpCodes.Bne_Un, operand, name);
				}
			}
		}

		// Token: 0x02000106 RID: 262
		public class Bge_Un_ : CodeMatch
		{
			// Token: 0x060005B8 RID: 1464 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bge_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700014A RID: 330
			public Code.Bge_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bge_Un_)base.Set(OpCodes.Bge_Un, operand, name);
				}
			}
		}

		// Token: 0x02000107 RID: 263
		public class Bgt_Un_ : CodeMatch
		{
			// Token: 0x060005BA RID: 1466 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Bgt_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700014B RID: 331
			public Code.Bgt_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Bgt_Un_)base.Set(OpCodes.Bgt_Un, operand, name);
				}
			}
		}

		// Token: 0x02000108 RID: 264
		public class Ble_Un_ : CodeMatch
		{
			// Token: 0x060005BC RID: 1468 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ble_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700014C RID: 332
			public Code.Ble_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ble_Un_)base.Set(OpCodes.Ble_Un, operand, name);
				}
			}
		}

		// Token: 0x02000109 RID: 265
		public class Blt_Un_ : CodeMatch
		{
			// Token: 0x060005BE RID: 1470 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Blt_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700014D RID: 333
			public Code.Blt_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Blt_Un_)base.Set(OpCodes.Blt_Un, operand, name);
				}
			}
		}

		// Token: 0x0200010A RID: 266
		public class Switch_ : CodeMatch
		{
			// Token: 0x060005C0 RID: 1472 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Switch_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700014E RID: 334
			public Code.Switch_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Switch_)base.Set(OpCodes.Switch, operand, name);
				}
			}
		}

		// Token: 0x0200010B RID: 267
		public class Ldind_I1_ : CodeMatch
		{
			// Token: 0x060005C2 RID: 1474 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_I1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700014F RID: 335
			public Code.Ldind_I1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_I1_)base.Set(OpCodes.Ldind_I1, operand, name);
				}
			}
		}

		// Token: 0x0200010C RID: 268
		public class Ldind_U1_ : CodeMatch
		{
			// Token: 0x060005C4 RID: 1476 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_U1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000150 RID: 336
			public Code.Ldind_U1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_U1_)base.Set(OpCodes.Ldind_U1, operand, name);
				}
			}
		}

		// Token: 0x0200010D RID: 269
		public class Ldind_I2_ : CodeMatch
		{
			// Token: 0x060005C6 RID: 1478 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_I2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000151 RID: 337
			public Code.Ldind_I2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_I2_)base.Set(OpCodes.Ldind_I2, operand, name);
				}
			}
		}

		// Token: 0x0200010E RID: 270
		public class Ldind_U2_ : CodeMatch
		{
			// Token: 0x060005C8 RID: 1480 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_U2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000152 RID: 338
			public Code.Ldind_U2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_U2_)base.Set(OpCodes.Ldind_U2, operand, name);
				}
			}
		}

		// Token: 0x0200010F RID: 271
		public class Ldind_I4_ : CodeMatch
		{
			// Token: 0x060005CA RID: 1482 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_I4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000153 RID: 339
			public Code.Ldind_I4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_I4_)base.Set(OpCodes.Ldind_I4, operand, name);
				}
			}
		}

		// Token: 0x02000110 RID: 272
		public class Ldind_U4_ : CodeMatch
		{
			// Token: 0x060005CC RID: 1484 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_U4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000154 RID: 340
			public Code.Ldind_U4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_U4_)base.Set(OpCodes.Ldind_U4, operand, name);
				}
			}
		}

		// Token: 0x02000111 RID: 273
		public class Ldind_I8_ : CodeMatch
		{
			// Token: 0x060005CE RID: 1486 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_I8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000155 RID: 341
			public Code.Ldind_I8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_I8_)base.Set(OpCodes.Ldind_I8, operand, name);
				}
			}
		}

		// Token: 0x02000112 RID: 274
		public class Ldind_I_ : CodeMatch
		{
			// Token: 0x060005D0 RID: 1488 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_I_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000156 RID: 342
			public Code.Ldind_I_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_I_)base.Set(OpCodes.Ldind_I, operand, name);
				}
			}
		}

		// Token: 0x02000113 RID: 275
		public class Ldind_R4_ : CodeMatch
		{
			// Token: 0x060005D2 RID: 1490 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_R4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000157 RID: 343
			public Code.Ldind_R4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_R4_)base.Set(OpCodes.Ldind_R4, operand, name);
				}
			}
		}

		// Token: 0x02000114 RID: 276
		public class Ldind_R8_ : CodeMatch
		{
			// Token: 0x060005D4 RID: 1492 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_R8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000158 RID: 344
			public Code.Ldind_R8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_R8_)base.Set(OpCodes.Ldind_R8, operand, name);
				}
			}
		}

		// Token: 0x02000115 RID: 277
		public class Ldind_Ref_ : CodeMatch
		{
			// Token: 0x060005D6 RID: 1494 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldind_Ref_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000159 RID: 345
			public Code.Ldind_Ref_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldind_Ref_)base.Set(OpCodes.Ldind_Ref, operand, name);
				}
			}
		}

		// Token: 0x02000116 RID: 278
		public class Stind_Ref_ : CodeMatch
		{
			// Token: 0x060005D8 RID: 1496 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_Ref_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700015A RID: 346
			public Code.Stind_Ref_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_Ref_)base.Set(OpCodes.Stind_Ref, operand, name);
				}
			}
		}

		// Token: 0x02000117 RID: 279
		public class Stind_I1_ : CodeMatch
		{
			// Token: 0x060005DA RID: 1498 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_I1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700015B RID: 347
			public Code.Stind_I1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_I1_)base.Set(OpCodes.Stind_I1, operand, name);
				}
			}
		}

		// Token: 0x02000118 RID: 280
		public class Stind_I2_ : CodeMatch
		{
			// Token: 0x060005DC RID: 1500 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_I2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700015C RID: 348
			public Code.Stind_I2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_I2_)base.Set(OpCodes.Stind_I2, operand, name);
				}
			}
		}

		// Token: 0x02000119 RID: 281
		public class Stind_I4_ : CodeMatch
		{
			// Token: 0x060005DE RID: 1502 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_I4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700015D RID: 349
			public Code.Stind_I4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_I4_)base.Set(OpCodes.Stind_I4, operand, name);
				}
			}
		}

		// Token: 0x0200011A RID: 282
		public class Stind_I8_ : CodeMatch
		{
			// Token: 0x060005E0 RID: 1504 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_I8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700015E RID: 350
			public Code.Stind_I8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_I8_)base.Set(OpCodes.Stind_I8, operand, name);
				}
			}
		}

		// Token: 0x0200011B RID: 283
		public class Stind_R4_ : CodeMatch
		{
			// Token: 0x060005E2 RID: 1506 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_R4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700015F RID: 351
			public Code.Stind_R4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_R4_)base.Set(OpCodes.Stind_R4, operand, name);
				}
			}
		}

		// Token: 0x0200011C RID: 284
		public class Stind_R8_ : CodeMatch
		{
			// Token: 0x060005E4 RID: 1508 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_R8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000160 RID: 352
			public Code.Stind_R8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_R8_)base.Set(OpCodes.Stind_R8, operand, name);
				}
			}
		}

		// Token: 0x0200011D RID: 285
		public class Add_ : CodeMatch
		{
			// Token: 0x060005E6 RID: 1510 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Add_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000161 RID: 353
			public Code.Add_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Add_)base.Set(OpCodes.Add, operand, name);
				}
			}
		}

		// Token: 0x0200011E RID: 286
		public class Sub_ : CodeMatch
		{
			// Token: 0x060005E8 RID: 1512 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Sub_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000162 RID: 354
			public Code.Sub_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Sub_)base.Set(OpCodes.Sub, operand, name);
				}
			}
		}

		// Token: 0x0200011F RID: 287
		public class Mul_ : CodeMatch
		{
			// Token: 0x060005EA RID: 1514 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Mul_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000163 RID: 355
			public Code.Mul_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Mul_)base.Set(OpCodes.Mul, operand, name);
				}
			}
		}

		// Token: 0x02000120 RID: 288
		public class Div_ : CodeMatch
		{
			// Token: 0x060005EC RID: 1516 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Div_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000164 RID: 356
			public Code.Div_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Div_)base.Set(OpCodes.Div, operand, name);
				}
			}
		}

		// Token: 0x02000121 RID: 289
		public class Div_Un_ : CodeMatch
		{
			// Token: 0x060005EE RID: 1518 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Div_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000165 RID: 357
			public Code.Div_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Div_Un_)base.Set(OpCodes.Div_Un, operand, name);
				}
			}
		}

		// Token: 0x02000122 RID: 290
		public class Rem_ : CodeMatch
		{
			// Token: 0x060005F0 RID: 1520 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Rem_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000166 RID: 358
			public Code.Rem_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Rem_)base.Set(OpCodes.Rem, operand, name);
				}
			}
		}

		// Token: 0x02000123 RID: 291
		public class Rem_Un_ : CodeMatch
		{
			// Token: 0x060005F2 RID: 1522 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Rem_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000167 RID: 359
			public Code.Rem_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Rem_Un_)base.Set(OpCodes.Rem_Un, operand, name);
				}
			}
		}

		// Token: 0x02000124 RID: 292
		public class And_ : CodeMatch
		{
			// Token: 0x060005F4 RID: 1524 RVA: 0x00015DAA File Offset: 0x00013FAA
			public And_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000168 RID: 360
			public Code.And_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.And_)base.Set(OpCodes.And, operand, name);
				}
			}
		}

		// Token: 0x02000125 RID: 293
		public class Or_ : CodeMatch
		{
			// Token: 0x060005F6 RID: 1526 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Or_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000169 RID: 361
			public Code.Or_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Or_)base.Set(OpCodes.Or, operand, name);
				}
			}
		}

		// Token: 0x02000126 RID: 294
		public class Xor_ : CodeMatch
		{
			// Token: 0x060005F8 RID: 1528 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Xor_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700016A RID: 362
			public Code.Xor_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Xor_)base.Set(OpCodes.Xor, operand, name);
				}
			}
		}

		// Token: 0x02000127 RID: 295
		public class Shl_ : CodeMatch
		{
			// Token: 0x060005FA RID: 1530 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Shl_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700016B RID: 363
			public Code.Shl_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Shl_)base.Set(OpCodes.Shl, operand, name);
				}
			}
		}

		// Token: 0x02000128 RID: 296
		public class Shr_ : CodeMatch
		{
			// Token: 0x060005FC RID: 1532 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Shr_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700016C RID: 364
			public Code.Shr_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Shr_)base.Set(OpCodes.Shr, operand, name);
				}
			}
		}

		// Token: 0x02000129 RID: 297
		public class Shr_Un_ : CodeMatch
		{
			// Token: 0x060005FE RID: 1534 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Shr_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700016D RID: 365
			public Code.Shr_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Shr_Un_)base.Set(OpCodes.Shr_Un, operand, name);
				}
			}
		}

		// Token: 0x0200012A RID: 298
		public class Neg_ : CodeMatch
		{
			// Token: 0x06000600 RID: 1536 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Neg_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700016E RID: 366
			public Code.Neg_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Neg_)base.Set(OpCodes.Neg, operand, name);
				}
			}
		}

		// Token: 0x0200012B RID: 299
		public class Not_ : CodeMatch
		{
			// Token: 0x06000602 RID: 1538 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Not_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700016F RID: 367
			public Code.Not_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Not_)base.Set(OpCodes.Not, operand, name);
				}
			}
		}

		// Token: 0x0200012C RID: 300
		public class Conv_I1_ : CodeMatch
		{
			// Token: 0x06000604 RID: 1540 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_I1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000170 RID: 368
			public Code.Conv_I1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_I1_)base.Set(OpCodes.Conv_I1, operand, name);
				}
			}
		}

		// Token: 0x0200012D RID: 301
		public class Conv_I2_ : CodeMatch
		{
			// Token: 0x06000606 RID: 1542 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_I2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000171 RID: 369
			public Code.Conv_I2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_I2_)base.Set(OpCodes.Conv_I2, operand, name);
				}
			}
		}

		// Token: 0x0200012E RID: 302
		public class Conv_I4_ : CodeMatch
		{
			// Token: 0x06000608 RID: 1544 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_I4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000172 RID: 370
			public Code.Conv_I4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_I4_)base.Set(OpCodes.Conv_I4, operand, name);
				}
			}
		}

		// Token: 0x0200012F RID: 303
		public class Conv_I8_ : CodeMatch
		{
			// Token: 0x0600060A RID: 1546 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_I8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000173 RID: 371
			public Code.Conv_I8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_I8_)base.Set(OpCodes.Conv_I8, operand, name);
				}
			}
		}

		// Token: 0x02000130 RID: 304
		public class Conv_R4_ : CodeMatch
		{
			// Token: 0x0600060C RID: 1548 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_R4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000174 RID: 372
			public Code.Conv_R4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_R4_)base.Set(OpCodes.Conv_R4, operand, name);
				}
			}
		}

		// Token: 0x02000131 RID: 305
		public class Conv_R8_ : CodeMatch
		{
			// Token: 0x0600060E RID: 1550 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_R8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000175 RID: 373
			public Code.Conv_R8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_R8_)base.Set(OpCodes.Conv_R8, operand, name);
				}
			}
		}

		// Token: 0x02000132 RID: 306
		public class Conv_U4_ : CodeMatch
		{
			// Token: 0x06000610 RID: 1552 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_U4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000176 RID: 374
			public Code.Conv_U4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_U4_)base.Set(OpCodes.Conv_U4, operand, name);
				}
			}
		}

		// Token: 0x02000133 RID: 307
		public class Conv_U8_ : CodeMatch
		{
			// Token: 0x06000612 RID: 1554 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_U8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000177 RID: 375
			public Code.Conv_U8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_U8_)base.Set(OpCodes.Conv_U8, operand, name);
				}
			}
		}

		// Token: 0x02000134 RID: 308
		public class Callvirt_ : CodeMatch
		{
			// Token: 0x06000614 RID: 1556 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Callvirt_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000178 RID: 376
			public Code.Callvirt_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Callvirt_)base.Set(OpCodes.Callvirt, operand, name);
				}
			}
		}

		// Token: 0x02000135 RID: 309
		public class Cpobj_ : CodeMatch
		{
			// Token: 0x06000616 RID: 1558 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Cpobj_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000179 RID: 377
			public Code.Cpobj_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Cpobj_)base.Set(OpCodes.Cpobj, operand, name);
				}
			}
		}

		// Token: 0x02000136 RID: 310
		public class Ldobj_ : CodeMatch
		{
			// Token: 0x06000618 RID: 1560 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldobj_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700017A RID: 378
			public Code.Ldobj_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldobj_)base.Set(OpCodes.Ldobj, operand, name);
				}
			}
		}

		// Token: 0x02000137 RID: 311
		public class Ldstr_ : CodeMatch
		{
			// Token: 0x0600061A RID: 1562 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldstr_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700017B RID: 379
			public Code.Ldstr_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldstr_)base.Set(OpCodes.Ldstr, operand, name);
				}
			}
		}

		// Token: 0x02000138 RID: 312
		public class Newobj_ : CodeMatch
		{
			// Token: 0x0600061C RID: 1564 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Newobj_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700017C RID: 380
			public Code.Newobj_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Newobj_)base.Set(OpCodes.Newobj, operand, name);
				}
			}
		}

		// Token: 0x02000139 RID: 313
		public class Castclass_ : CodeMatch
		{
			// Token: 0x0600061E RID: 1566 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Castclass_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700017D RID: 381
			public Code.Castclass_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Castclass_)base.Set(OpCodes.Castclass, operand, name);
				}
			}
		}

		// Token: 0x0200013A RID: 314
		public class Isinst_ : CodeMatch
		{
			// Token: 0x06000620 RID: 1568 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Isinst_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700017E RID: 382
			public Code.Isinst_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Isinst_)base.Set(OpCodes.Isinst, operand, name);
				}
			}
		}

		// Token: 0x0200013B RID: 315
		public class Conv_R_Un_ : CodeMatch
		{
			// Token: 0x06000622 RID: 1570 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_R_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700017F RID: 383
			public Code.Conv_R_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_R_Un_)base.Set(OpCodes.Conv_R_Un, operand, name);
				}
			}
		}

		// Token: 0x0200013C RID: 316
		public class Unbox_ : CodeMatch
		{
			// Token: 0x06000624 RID: 1572 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Unbox_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000180 RID: 384
			public Code.Unbox_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Unbox_)base.Set(OpCodes.Unbox, operand, name);
				}
			}
		}

		// Token: 0x0200013D RID: 317
		public class Throw_ : CodeMatch
		{
			// Token: 0x06000626 RID: 1574 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Throw_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000181 RID: 385
			public Code.Throw_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Throw_)base.Set(OpCodes.Throw, operand, name);
				}
			}
		}

		// Token: 0x0200013E RID: 318
		public class Ldfld_ : CodeMatch
		{
			// Token: 0x06000628 RID: 1576 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldfld_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000182 RID: 386
			public Code.Ldfld_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldfld_)base.Set(OpCodes.Ldfld, operand, name);
				}
			}
		}

		// Token: 0x0200013F RID: 319
		public class Ldflda_ : CodeMatch
		{
			// Token: 0x0600062A RID: 1578 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldflda_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000183 RID: 387
			public Code.Ldflda_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldflda_)base.Set(OpCodes.Ldflda, operand, name);
				}
			}
		}

		// Token: 0x02000140 RID: 320
		public class Stfld_ : CodeMatch
		{
			// Token: 0x0600062C RID: 1580 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stfld_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000184 RID: 388
			public Code.Stfld_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stfld_)base.Set(OpCodes.Stfld, operand, name);
				}
			}
		}

		// Token: 0x02000141 RID: 321
		public class Ldsfld_ : CodeMatch
		{
			// Token: 0x0600062E RID: 1582 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldsfld_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000185 RID: 389
			public Code.Ldsfld_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldsfld_)base.Set(OpCodes.Ldsfld, operand, name);
				}
			}
		}

		// Token: 0x02000142 RID: 322
		public class Ldsflda_ : CodeMatch
		{
			// Token: 0x06000630 RID: 1584 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldsflda_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000186 RID: 390
			public Code.Ldsflda_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldsflda_)base.Set(OpCodes.Ldsflda, operand, name);
				}
			}
		}

		// Token: 0x02000143 RID: 323
		public class Stsfld_ : CodeMatch
		{
			// Token: 0x06000632 RID: 1586 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stsfld_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000187 RID: 391
			public Code.Stsfld_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stsfld_)base.Set(OpCodes.Stsfld, operand, name);
				}
			}
		}

		// Token: 0x02000144 RID: 324
		public class Stobj_ : CodeMatch
		{
			// Token: 0x06000634 RID: 1588 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stobj_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000188 RID: 392
			public Code.Stobj_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stobj_)base.Set(OpCodes.Stobj, operand, name);
				}
			}
		}

		// Token: 0x02000145 RID: 325
		public class Conv_Ovf_I1_Un_ : CodeMatch
		{
			// Token: 0x06000636 RID: 1590 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I1_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000189 RID: 393
			public Code.Conv_Ovf_I1_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I1_Un_)base.Set(OpCodes.Conv_Ovf_I1_Un, operand, name);
				}
			}
		}

		// Token: 0x02000146 RID: 326
		public class Conv_Ovf_I2_Un_ : CodeMatch
		{
			// Token: 0x06000638 RID: 1592 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I2_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700018A RID: 394
			public Code.Conv_Ovf_I2_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I2_Un_)base.Set(OpCodes.Conv_Ovf_I2_Un, operand, name);
				}
			}
		}

		// Token: 0x02000147 RID: 327
		public class Conv_Ovf_I4_Un_ : CodeMatch
		{
			// Token: 0x0600063A RID: 1594 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I4_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700018B RID: 395
			public Code.Conv_Ovf_I4_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I4_Un_)base.Set(OpCodes.Conv_Ovf_I4_Un, operand, name);
				}
			}
		}

		// Token: 0x02000148 RID: 328
		public class Conv_Ovf_I8_Un_ : CodeMatch
		{
			// Token: 0x0600063C RID: 1596 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I8_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700018C RID: 396
			public Code.Conv_Ovf_I8_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I8_Un_)base.Set(OpCodes.Conv_Ovf_I8_Un, operand, name);
				}
			}
		}

		// Token: 0x02000149 RID: 329
		public class Conv_Ovf_U1_Un_ : CodeMatch
		{
			// Token: 0x0600063E RID: 1598 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U1_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700018D RID: 397
			public Code.Conv_Ovf_U1_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U1_Un_)base.Set(OpCodes.Conv_Ovf_U1_Un, operand, name);
				}
			}
		}

		// Token: 0x0200014A RID: 330
		public class Conv_Ovf_U2_Un_ : CodeMatch
		{
			// Token: 0x06000640 RID: 1600 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U2_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700018E RID: 398
			public Code.Conv_Ovf_U2_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U2_Un_)base.Set(OpCodes.Conv_Ovf_U2_Un, operand, name);
				}
			}
		}

		// Token: 0x0200014B RID: 331
		public class Conv_Ovf_U4_Un_ : CodeMatch
		{
			// Token: 0x06000642 RID: 1602 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U4_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700018F RID: 399
			public Code.Conv_Ovf_U4_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U4_Un_)base.Set(OpCodes.Conv_Ovf_U4_Un, operand, name);
				}
			}
		}

		// Token: 0x0200014C RID: 332
		public class Conv_Ovf_U8_Un_ : CodeMatch
		{
			// Token: 0x06000644 RID: 1604 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U8_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000190 RID: 400
			public Code.Conv_Ovf_U8_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U8_Un_)base.Set(OpCodes.Conv_Ovf_U8_Un, operand, name);
				}
			}
		}

		// Token: 0x0200014D RID: 333
		public class Conv_Ovf_I_Un_ : CodeMatch
		{
			// Token: 0x06000646 RID: 1606 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000191 RID: 401
			public Code.Conv_Ovf_I_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I_Un_)base.Set(OpCodes.Conv_Ovf_I_Un, operand, name);
				}
			}
		}

		// Token: 0x0200014E RID: 334
		public class Conv_Ovf_U_Un_ : CodeMatch
		{
			// Token: 0x06000648 RID: 1608 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000192 RID: 402
			public Code.Conv_Ovf_U_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U_Un_)base.Set(OpCodes.Conv_Ovf_U_Un, operand, name);
				}
			}
		}

		// Token: 0x0200014F RID: 335
		public class Box_ : CodeMatch
		{
			// Token: 0x0600064A RID: 1610 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Box_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000193 RID: 403
			public Code.Box_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Box_)base.Set(OpCodes.Box, operand, name);
				}
			}
		}

		// Token: 0x02000150 RID: 336
		public class Newarr_ : CodeMatch
		{
			// Token: 0x0600064C RID: 1612 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Newarr_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000194 RID: 404
			public Code.Newarr_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Newarr_)base.Set(OpCodes.Newarr, operand, name);
				}
			}
		}

		// Token: 0x02000151 RID: 337
		public class Ldlen_ : CodeMatch
		{
			// Token: 0x0600064E RID: 1614 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldlen_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000195 RID: 405
			public Code.Ldlen_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldlen_)base.Set(OpCodes.Ldlen, operand, name);
				}
			}
		}

		// Token: 0x02000152 RID: 338
		public class Ldelema_ : CodeMatch
		{
			// Token: 0x06000650 RID: 1616 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelema_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000196 RID: 406
			public Code.Ldelema_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelema_)base.Set(OpCodes.Ldelema, operand, name);
				}
			}
		}

		// Token: 0x02000153 RID: 339
		public class Ldelem_I1_ : CodeMatch
		{
			// Token: 0x06000652 RID: 1618 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_I1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000197 RID: 407
			public Code.Ldelem_I1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_I1_)base.Set(OpCodes.Ldelem_I1, operand, name);
				}
			}
		}

		// Token: 0x02000154 RID: 340
		public class Ldelem_U1_ : CodeMatch
		{
			// Token: 0x06000654 RID: 1620 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_U1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000198 RID: 408
			public Code.Ldelem_U1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_U1_)base.Set(OpCodes.Ldelem_U1, operand, name);
				}
			}
		}

		// Token: 0x02000155 RID: 341
		public class Ldelem_I2_ : CodeMatch
		{
			// Token: 0x06000656 RID: 1622 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_I2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x17000199 RID: 409
			public Code.Ldelem_I2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_I2_)base.Set(OpCodes.Ldelem_I2, operand, name);
				}
			}
		}

		// Token: 0x02000156 RID: 342
		public class Ldelem_U2_ : CodeMatch
		{
			// Token: 0x06000658 RID: 1624 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_U2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700019A RID: 410
			public Code.Ldelem_U2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_U2_)base.Set(OpCodes.Ldelem_U2, operand, name);
				}
			}
		}

		// Token: 0x02000157 RID: 343
		public class Ldelem_I4_ : CodeMatch
		{
			// Token: 0x0600065A RID: 1626 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_I4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700019B RID: 411
			public Code.Ldelem_I4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_I4_)base.Set(OpCodes.Ldelem_I4, operand, name);
				}
			}
		}

		// Token: 0x02000158 RID: 344
		public class Ldelem_U4_ : CodeMatch
		{
			// Token: 0x0600065C RID: 1628 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_U4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700019C RID: 412
			public Code.Ldelem_U4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_U4_)base.Set(OpCodes.Ldelem_U4, operand, name);
				}
			}
		}

		// Token: 0x02000159 RID: 345
		public class Ldelem_I8_ : CodeMatch
		{
			// Token: 0x0600065E RID: 1630 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_I8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700019D RID: 413
			public Code.Ldelem_I8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_I8_)base.Set(OpCodes.Ldelem_I8, operand, name);
				}
			}
		}

		// Token: 0x0200015A RID: 346
		public class Ldelem_I_ : CodeMatch
		{
			// Token: 0x06000660 RID: 1632 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_I_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700019E RID: 414
			public Code.Ldelem_I_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_I_)base.Set(OpCodes.Ldelem_I, operand, name);
				}
			}
		}

		// Token: 0x0200015B RID: 347
		public class Ldelem_R4_ : CodeMatch
		{
			// Token: 0x06000662 RID: 1634 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_R4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x1700019F RID: 415
			public Code.Ldelem_R4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_R4_)base.Set(OpCodes.Ldelem_R4, operand, name);
				}
			}
		}

		// Token: 0x0200015C RID: 348
		public class Ldelem_R8_ : CodeMatch
		{
			// Token: 0x06000664 RID: 1636 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_R8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A0 RID: 416
			public Code.Ldelem_R8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_R8_)base.Set(OpCodes.Ldelem_R8, operand, name);
				}
			}
		}

		// Token: 0x0200015D RID: 349
		public class Ldelem_Ref_ : CodeMatch
		{
			// Token: 0x06000666 RID: 1638 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_Ref_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A1 RID: 417
			public Code.Ldelem_Ref_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_Ref_)base.Set(OpCodes.Ldelem_Ref, operand, name);
				}
			}
		}

		// Token: 0x0200015E RID: 350
		public class Stelem_I_ : CodeMatch
		{
			// Token: 0x06000668 RID: 1640 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_I_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A2 RID: 418
			public Code.Stelem_I_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_I_)base.Set(OpCodes.Stelem_I, operand, name);
				}
			}
		}

		// Token: 0x0200015F RID: 351
		public class Stelem_I1_ : CodeMatch
		{
			// Token: 0x0600066A RID: 1642 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_I1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A3 RID: 419
			public Code.Stelem_I1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_I1_)base.Set(OpCodes.Stelem_I1, operand, name);
				}
			}
		}

		// Token: 0x02000160 RID: 352
		public class Stelem_I2_ : CodeMatch
		{
			// Token: 0x0600066C RID: 1644 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_I2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A4 RID: 420
			public Code.Stelem_I2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_I2_)base.Set(OpCodes.Stelem_I2, operand, name);
				}
			}
		}

		// Token: 0x02000161 RID: 353
		public class Stelem_I4_ : CodeMatch
		{
			// Token: 0x0600066E RID: 1646 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_I4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A5 RID: 421
			public Code.Stelem_I4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_I4_)base.Set(OpCodes.Stelem_I4, operand, name);
				}
			}
		}

		// Token: 0x02000162 RID: 354
		public class Stelem_I8_ : CodeMatch
		{
			// Token: 0x06000670 RID: 1648 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_I8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A6 RID: 422
			public Code.Stelem_I8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_I8_)base.Set(OpCodes.Stelem_I8, operand, name);
				}
			}
		}

		// Token: 0x02000163 RID: 355
		public class Stelem_R4_ : CodeMatch
		{
			// Token: 0x06000672 RID: 1650 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_R4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A7 RID: 423
			public Code.Stelem_R4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_R4_)base.Set(OpCodes.Stelem_R4, operand, name);
				}
			}
		}

		// Token: 0x02000164 RID: 356
		public class Stelem_R8_ : CodeMatch
		{
			// Token: 0x06000674 RID: 1652 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_R8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A8 RID: 424
			public Code.Stelem_R8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_R8_)base.Set(OpCodes.Stelem_R8, operand, name);
				}
			}
		}

		// Token: 0x02000165 RID: 357
		public class Stelem_Ref_ : CodeMatch
		{
			// Token: 0x06000676 RID: 1654 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_Ref_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001A9 RID: 425
			public Code.Stelem_Ref_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_Ref_)base.Set(OpCodes.Stelem_Ref, operand, name);
				}
			}
		}

		// Token: 0x02000166 RID: 358
		public class Ldelem_ : CodeMatch
		{
			// Token: 0x06000678 RID: 1656 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldelem_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001AA RID: 426
			public Code.Ldelem_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldelem_)base.Set(OpCodes.Ldelem, operand, name);
				}
			}
		}

		// Token: 0x02000167 RID: 359
		public class Stelem_ : CodeMatch
		{
			// Token: 0x0600067A RID: 1658 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stelem_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001AB RID: 427
			public Code.Stelem_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stelem_)base.Set(OpCodes.Stelem, operand, name);
				}
			}
		}

		// Token: 0x02000168 RID: 360
		public class Unbox_Any_ : CodeMatch
		{
			// Token: 0x0600067C RID: 1660 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Unbox_Any_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001AC RID: 428
			public Code.Unbox_Any_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Unbox_Any_)base.Set(OpCodes.Unbox_Any, operand, name);
				}
			}
		}

		// Token: 0x02000169 RID: 361
		public class Conv_Ovf_I1_ : CodeMatch
		{
			// Token: 0x0600067E RID: 1662 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001AD RID: 429
			public Code.Conv_Ovf_I1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I1_)base.Set(OpCodes.Conv_Ovf_I1, operand, name);
				}
			}
		}

		// Token: 0x0200016A RID: 362
		public class Conv_Ovf_U1_ : CodeMatch
		{
			// Token: 0x06000680 RID: 1664 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001AE RID: 430
			public Code.Conv_Ovf_U1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U1_)base.Set(OpCodes.Conv_Ovf_U1, operand, name);
				}
			}
		}

		// Token: 0x0200016B RID: 363
		public class Conv_Ovf_I2_ : CodeMatch
		{
			// Token: 0x06000682 RID: 1666 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001AF RID: 431
			public Code.Conv_Ovf_I2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I2_)base.Set(OpCodes.Conv_Ovf_I2, operand, name);
				}
			}
		}

		// Token: 0x0200016C RID: 364
		public class Conv_Ovf_U2_ : CodeMatch
		{
			// Token: 0x06000684 RID: 1668 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B0 RID: 432
			public Code.Conv_Ovf_U2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U2_)base.Set(OpCodes.Conv_Ovf_U2, operand, name);
				}
			}
		}

		// Token: 0x0200016D RID: 365
		public class Conv_Ovf_I4_ : CodeMatch
		{
			// Token: 0x06000686 RID: 1670 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B1 RID: 433
			public Code.Conv_Ovf_I4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I4_)base.Set(OpCodes.Conv_Ovf_I4, operand, name);
				}
			}
		}

		// Token: 0x0200016E RID: 366
		public class Conv_Ovf_U4_ : CodeMatch
		{
			// Token: 0x06000688 RID: 1672 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B2 RID: 434
			public Code.Conv_Ovf_U4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U4_)base.Set(OpCodes.Conv_Ovf_U4, operand, name);
				}
			}
		}

		// Token: 0x0200016F RID: 367
		public class Conv_Ovf_I8_ : CodeMatch
		{
			// Token: 0x0600068A RID: 1674 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B3 RID: 435
			public Code.Conv_Ovf_I8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I8_)base.Set(OpCodes.Conv_Ovf_I8, operand, name);
				}
			}
		}

		// Token: 0x02000170 RID: 368
		public class Conv_Ovf_U8_ : CodeMatch
		{
			// Token: 0x0600068C RID: 1676 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U8_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B4 RID: 436
			public Code.Conv_Ovf_U8_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U8_)base.Set(OpCodes.Conv_Ovf_U8, operand, name);
				}
			}
		}

		// Token: 0x02000171 RID: 369
		public class Refanyval_ : CodeMatch
		{
			// Token: 0x0600068E RID: 1678 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Refanyval_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B5 RID: 437
			public Code.Refanyval_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Refanyval_)base.Set(OpCodes.Refanyval, operand, name);
				}
			}
		}

		// Token: 0x02000172 RID: 370
		public class Ckfinite_ : CodeMatch
		{
			// Token: 0x06000690 RID: 1680 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ckfinite_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B6 RID: 438
			public Code.Ckfinite_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ckfinite_)base.Set(OpCodes.Ckfinite, operand, name);
				}
			}
		}

		// Token: 0x02000173 RID: 371
		public class Mkrefany_ : CodeMatch
		{
			// Token: 0x06000692 RID: 1682 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Mkrefany_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B7 RID: 439
			public Code.Mkrefany_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Mkrefany_)base.Set(OpCodes.Mkrefany, operand, name);
				}
			}
		}

		// Token: 0x02000174 RID: 372
		public class Ldtoken_ : CodeMatch
		{
			// Token: 0x06000694 RID: 1684 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldtoken_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B8 RID: 440
			public Code.Ldtoken_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldtoken_)base.Set(OpCodes.Ldtoken, operand, name);
				}
			}
		}

		// Token: 0x02000175 RID: 373
		public class Conv_U2_ : CodeMatch
		{
			// Token: 0x06000696 RID: 1686 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_U2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001B9 RID: 441
			public Code.Conv_U2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_U2_)base.Set(OpCodes.Conv_U2, operand, name);
				}
			}
		}

		// Token: 0x02000176 RID: 374
		public class Conv_U1_ : CodeMatch
		{
			// Token: 0x06000698 RID: 1688 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_U1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001BA RID: 442
			public Code.Conv_U1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_U1_)base.Set(OpCodes.Conv_U1, operand, name);
				}
			}
		}

		// Token: 0x02000177 RID: 375
		public class Conv_I_ : CodeMatch
		{
			// Token: 0x0600069A RID: 1690 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_I_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001BB RID: 443
			public Code.Conv_I_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_I_)base.Set(OpCodes.Conv_I, operand, name);
				}
			}
		}

		// Token: 0x02000178 RID: 376
		public class Conv_Ovf_I_ : CodeMatch
		{
			// Token: 0x0600069C RID: 1692 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_I_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001BC RID: 444
			public Code.Conv_Ovf_I_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_I_)base.Set(OpCodes.Conv_Ovf_I, operand, name);
				}
			}
		}

		// Token: 0x02000179 RID: 377
		public class Conv_Ovf_U_ : CodeMatch
		{
			// Token: 0x0600069E RID: 1694 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_Ovf_U_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001BD RID: 445
			public Code.Conv_Ovf_U_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_Ovf_U_)base.Set(OpCodes.Conv_Ovf_U, operand, name);
				}
			}
		}

		// Token: 0x0200017A RID: 378
		public class Add_Ovf_ : CodeMatch
		{
			// Token: 0x060006A0 RID: 1696 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Add_Ovf_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001BE RID: 446
			public Code.Add_Ovf_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Add_Ovf_)base.Set(OpCodes.Add_Ovf, operand, name);
				}
			}
		}

		// Token: 0x0200017B RID: 379
		public class Add_Ovf_Un_ : CodeMatch
		{
			// Token: 0x060006A2 RID: 1698 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Add_Ovf_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001BF RID: 447
			public Code.Add_Ovf_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Add_Ovf_Un_)base.Set(OpCodes.Add_Ovf_Un, operand, name);
				}
			}
		}

		// Token: 0x0200017C RID: 380
		public class Mul_Ovf_ : CodeMatch
		{
			// Token: 0x060006A4 RID: 1700 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Mul_Ovf_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C0 RID: 448
			public Code.Mul_Ovf_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Mul_Ovf_)base.Set(OpCodes.Mul_Ovf, operand, name);
				}
			}
		}

		// Token: 0x0200017D RID: 381
		public class Mul_Ovf_Un_ : CodeMatch
		{
			// Token: 0x060006A6 RID: 1702 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Mul_Ovf_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C1 RID: 449
			public Code.Mul_Ovf_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Mul_Ovf_Un_)base.Set(OpCodes.Mul_Ovf_Un, operand, name);
				}
			}
		}

		// Token: 0x0200017E RID: 382
		public class Sub_Ovf_ : CodeMatch
		{
			// Token: 0x060006A8 RID: 1704 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Sub_Ovf_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C2 RID: 450
			public Code.Sub_Ovf_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Sub_Ovf_)base.Set(OpCodes.Sub_Ovf, operand, name);
				}
			}
		}

		// Token: 0x0200017F RID: 383
		public class Sub_Ovf_Un_ : CodeMatch
		{
			// Token: 0x060006AA RID: 1706 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Sub_Ovf_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C3 RID: 451
			public Code.Sub_Ovf_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Sub_Ovf_Un_)base.Set(OpCodes.Sub_Ovf_Un, operand, name);
				}
			}
		}

		// Token: 0x02000180 RID: 384
		public class Endfinally_ : CodeMatch
		{
			// Token: 0x060006AC RID: 1708 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Endfinally_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C4 RID: 452
			public Code.Endfinally_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Endfinally_)base.Set(OpCodes.Endfinally, operand, name);
				}
			}
		}

		// Token: 0x02000181 RID: 385
		public class Leave_ : CodeMatch
		{
			// Token: 0x060006AE RID: 1710 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Leave_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C5 RID: 453
			public Code.Leave_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Leave_)base.Set(OpCodes.Leave, operand, name);
				}
			}
		}

		// Token: 0x02000182 RID: 386
		public class Leave_S_ : CodeMatch
		{
			// Token: 0x060006B0 RID: 1712 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Leave_S_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C6 RID: 454
			public Code.Leave_S_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Leave_S_)base.Set(OpCodes.Leave_S, operand, name);
				}
			}
		}

		// Token: 0x02000183 RID: 387
		public class Stind_I_ : CodeMatch
		{
			// Token: 0x060006B2 RID: 1714 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stind_I_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C7 RID: 455
			public Code.Stind_I_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stind_I_)base.Set(OpCodes.Stind_I, operand, name);
				}
			}
		}

		// Token: 0x02000184 RID: 388
		public class Conv_U_ : CodeMatch
		{
			// Token: 0x060006B4 RID: 1716 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Conv_U_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C8 RID: 456
			public Code.Conv_U_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Conv_U_)base.Set(OpCodes.Conv_U, operand, name);
				}
			}
		}

		// Token: 0x02000185 RID: 389
		public class Prefix7_ : CodeMatch
		{
			// Token: 0x060006B6 RID: 1718 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefix7_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001C9 RID: 457
			public Code.Prefix7_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefix7_)base.Set(OpCodes.Prefix7, operand, name);
				}
			}
		}

		// Token: 0x02000186 RID: 390
		public class Prefix6_ : CodeMatch
		{
			// Token: 0x060006B8 RID: 1720 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefix6_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001CA RID: 458
			public Code.Prefix6_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefix6_)base.Set(OpCodes.Prefix6, operand, name);
				}
			}
		}

		// Token: 0x02000187 RID: 391
		public class Prefix5_ : CodeMatch
		{
			// Token: 0x060006BA RID: 1722 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefix5_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001CB RID: 459
			public Code.Prefix5_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefix5_)base.Set(OpCodes.Prefix5, operand, name);
				}
			}
		}

		// Token: 0x02000188 RID: 392
		public class Prefix4_ : CodeMatch
		{
			// Token: 0x060006BC RID: 1724 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefix4_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001CC RID: 460
			public Code.Prefix4_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefix4_)base.Set(OpCodes.Prefix4, operand, name);
				}
			}
		}

		// Token: 0x02000189 RID: 393
		public class Prefix3_ : CodeMatch
		{
			// Token: 0x060006BE RID: 1726 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefix3_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001CD RID: 461
			public Code.Prefix3_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefix3_)base.Set(OpCodes.Prefix3, operand, name);
				}
			}
		}

		// Token: 0x0200018A RID: 394
		public class Prefix2_ : CodeMatch
		{
			// Token: 0x060006C0 RID: 1728 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefix2_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001CE RID: 462
			public Code.Prefix2_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefix2_)base.Set(OpCodes.Prefix2, operand, name);
				}
			}
		}

		// Token: 0x0200018B RID: 395
		public class Prefix1_ : CodeMatch
		{
			// Token: 0x060006C2 RID: 1730 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefix1_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001CF RID: 463
			public Code.Prefix1_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefix1_)base.Set(OpCodes.Prefix1, operand, name);
				}
			}
		}

		// Token: 0x0200018C RID: 396
		public class Prefixref_ : CodeMatch
		{
			// Token: 0x060006C4 RID: 1732 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Prefixref_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D0 RID: 464
			public Code.Prefixref_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Prefixref_)base.Set(OpCodes.Prefixref, operand, name);
				}
			}
		}

		// Token: 0x0200018D RID: 397
		public class Arglist_ : CodeMatch
		{
			// Token: 0x060006C6 RID: 1734 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Arglist_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D1 RID: 465
			public Code.Arglist_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Arglist_)base.Set(OpCodes.Arglist, operand, name);
				}
			}
		}

		// Token: 0x0200018E RID: 398
		public class Ceq_ : CodeMatch
		{
			// Token: 0x060006C8 RID: 1736 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ceq_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D2 RID: 466
			public Code.Ceq_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ceq_)base.Set(OpCodes.Ceq, operand, name);
				}
			}
		}

		// Token: 0x0200018F RID: 399
		public class Cgt_ : CodeMatch
		{
			// Token: 0x060006CA RID: 1738 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Cgt_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D3 RID: 467
			public Code.Cgt_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Cgt_)base.Set(OpCodes.Cgt, operand, name);
				}
			}
		}

		// Token: 0x02000190 RID: 400
		public class Cgt_Un_ : CodeMatch
		{
			// Token: 0x060006CC RID: 1740 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Cgt_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D4 RID: 468
			public Code.Cgt_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Cgt_Un_)base.Set(OpCodes.Cgt_Un, operand, name);
				}
			}
		}

		// Token: 0x02000191 RID: 401
		public class Clt_ : CodeMatch
		{
			// Token: 0x060006CE RID: 1742 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Clt_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D5 RID: 469
			public Code.Clt_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Clt_)base.Set(OpCodes.Clt, operand, name);
				}
			}
		}

		// Token: 0x02000192 RID: 402
		public class Clt_Un_ : CodeMatch
		{
			// Token: 0x060006D0 RID: 1744 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Clt_Un_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D6 RID: 470
			public Code.Clt_Un_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Clt_Un_)base.Set(OpCodes.Clt_Un, operand, name);
				}
			}
		}

		// Token: 0x02000193 RID: 403
		public class Ldftn_ : CodeMatch
		{
			// Token: 0x060006D2 RID: 1746 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldftn_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D7 RID: 471
			public Code.Ldftn_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldftn_)base.Set(OpCodes.Ldftn, operand, name);
				}
			}
		}

		// Token: 0x02000194 RID: 404
		public class Ldvirtftn_ : CodeMatch
		{
			// Token: 0x060006D4 RID: 1748 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldvirtftn_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D8 RID: 472
			public Code.Ldvirtftn_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldvirtftn_)base.Set(OpCodes.Ldvirtftn, operand, name);
				}
			}
		}

		// Token: 0x02000195 RID: 405
		public class Ldarg_ : CodeMatch
		{
			// Token: 0x060006D6 RID: 1750 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarg_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001D9 RID: 473
			public Code.Ldarg_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarg_)base.Set(OpCodes.Ldarg, operand, name);
				}
			}
		}

		// Token: 0x02000196 RID: 406
		public class Ldarga_ : CodeMatch
		{
			// Token: 0x060006D8 RID: 1752 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldarga_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001DA RID: 474
			public Code.Ldarga_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldarga_)base.Set(OpCodes.Ldarga, operand, name);
				}
			}
		}

		// Token: 0x02000197 RID: 407
		public class Starg_ : CodeMatch
		{
			// Token: 0x060006DA RID: 1754 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Starg_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001DB RID: 475
			public Code.Starg_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Starg_)base.Set(OpCodes.Starg, operand, name);
				}
			}
		}

		// Token: 0x02000198 RID: 408
		public class Ldloc_ : CodeMatch
		{
			// Token: 0x060006DC RID: 1756 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloc_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001DC RID: 476
			public Code.Ldloc_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloc_)base.Set(OpCodes.Ldloc, operand, name);
				}
			}
		}

		// Token: 0x02000199 RID: 409
		public class Ldloca_ : CodeMatch
		{
			// Token: 0x060006DE RID: 1758 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Ldloca_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001DD RID: 477
			public Code.Ldloca_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Ldloca_)base.Set(OpCodes.Ldloca, operand, name);
				}
			}
		}

		// Token: 0x0200019A RID: 410
		public class Stloc_ : CodeMatch
		{
			// Token: 0x060006E0 RID: 1760 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Stloc_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001DE RID: 478
			public Code.Stloc_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Stloc_)base.Set(OpCodes.Stloc, operand, name);
				}
			}
		}

		// Token: 0x0200019B RID: 411
		public class Localloc_ : CodeMatch
		{
			// Token: 0x060006E2 RID: 1762 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Localloc_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001DF RID: 479
			public Code.Localloc_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Localloc_)base.Set(OpCodes.Localloc, operand, name);
				}
			}
		}

		// Token: 0x0200019C RID: 412
		public class Endfilter_ : CodeMatch
		{
			// Token: 0x060006E4 RID: 1764 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Endfilter_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E0 RID: 480
			public Code.Endfilter_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Endfilter_)base.Set(OpCodes.Endfilter, operand, name);
				}
			}
		}

		// Token: 0x0200019D RID: 413
		public class Unaligned_ : CodeMatch
		{
			// Token: 0x060006E6 RID: 1766 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Unaligned_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E1 RID: 481
			public Code.Unaligned_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Unaligned_)base.Set(OpCodes.Unaligned, operand, name);
				}
			}
		}

		// Token: 0x0200019E RID: 414
		public class Volatile_ : CodeMatch
		{
			// Token: 0x060006E8 RID: 1768 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Volatile_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E2 RID: 482
			public Code.Volatile_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Volatile_)base.Set(OpCodes.Volatile, operand, name);
				}
			}
		}

		// Token: 0x0200019F RID: 415
		public class Tailcall_ : CodeMatch
		{
			// Token: 0x060006EA RID: 1770 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Tailcall_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E3 RID: 483
			public Code.Tailcall_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Tailcall_)base.Set(OpCodes.Tailcall, operand, name);
				}
			}
		}

		// Token: 0x020001A0 RID: 416
		public class Initobj_ : CodeMatch
		{
			// Token: 0x060006EC RID: 1772 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Initobj_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E4 RID: 484
			public Code.Initobj_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Initobj_)base.Set(OpCodes.Initobj, operand, name);
				}
			}
		}

		// Token: 0x020001A1 RID: 417
		public class Constrained_ : CodeMatch
		{
			// Token: 0x060006EE RID: 1774 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Constrained_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E5 RID: 485
			public Code.Constrained_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Constrained_)base.Set(OpCodes.Constrained, operand, name);
				}
			}
		}

		// Token: 0x020001A2 RID: 418
		public class Cpblk_ : CodeMatch
		{
			// Token: 0x060006F0 RID: 1776 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Cpblk_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E6 RID: 486
			public Code.Cpblk_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Cpblk_)base.Set(OpCodes.Cpblk, operand, name);
				}
			}
		}

		// Token: 0x020001A3 RID: 419
		public class Initblk_ : CodeMatch
		{
			// Token: 0x060006F2 RID: 1778 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Initblk_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E7 RID: 487
			public Code.Initblk_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Initblk_)base.Set(OpCodes.Initblk, operand, name);
				}
			}
		}

		// Token: 0x020001A4 RID: 420
		public class Rethrow_ : CodeMatch
		{
			// Token: 0x060006F4 RID: 1780 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Rethrow_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E8 RID: 488
			public Code.Rethrow_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Rethrow_)base.Set(OpCodes.Rethrow, operand, name);
				}
			}
		}

		// Token: 0x020001A5 RID: 421
		public class Sizeof_ : CodeMatch
		{
			// Token: 0x060006F6 RID: 1782 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Sizeof_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001E9 RID: 489
			public Code.Sizeof_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Sizeof_)base.Set(OpCodes.Sizeof, operand, name);
				}
			}
		}

		// Token: 0x020001A6 RID: 422
		public class Refanytype_ : CodeMatch
		{
			// Token: 0x060006F8 RID: 1784 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Refanytype_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001EA RID: 490
			public Code.Refanytype_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Refanytype_)base.Set(OpCodes.Refanytype, operand, name);
				}
			}
		}

		// Token: 0x020001A7 RID: 423
		public class Readonly_ : CodeMatch
		{
			// Token: 0x060006FA RID: 1786 RVA: 0x00015DAA File Offset: 0x00013FAA
			public Readonly_(OpCode opcode)
				: base(new OpCode?(opcode), null, null)
			{
			}

			// Token: 0x170001EB RID: 491
			public Code.Readonly_ this[object operand = null, string name = null]
			{
				get
				{
					return (Code.Readonly_)base.Set(OpCodes.Readonly, operand, name);
				}
			}
		}
	}
}
