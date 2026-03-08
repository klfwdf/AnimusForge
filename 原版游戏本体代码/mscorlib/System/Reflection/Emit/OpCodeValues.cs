using System;

namespace System.Reflection.Emit
{
	// Token: 0x02000654 RID: 1620
	internal enum OpCodeValues
	{
		// Token: 0x04001F6D RID: 8045
		Nop,
		// Token: 0x04001F6E RID: 8046
		Break,
		// Token: 0x04001F6F RID: 8047
		Ldarg_0,
		// Token: 0x04001F70 RID: 8048
		Ldarg_1,
		// Token: 0x04001F71 RID: 8049
		Ldarg_2,
		// Token: 0x04001F72 RID: 8050
		Ldarg_3,
		// Token: 0x04001F73 RID: 8051
		Ldloc_0,
		// Token: 0x04001F74 RID: 8052
		Ldloc_1,
		// Token: 0x04001F75 RID: 8053
		Ldloc_2,
		// Token: 0x04001F76 RID: 8054
		Ldloc_3,
		// Token: 0x04001F77 RID: 8055
		Stloc_0,
		// Token: 0x04001F78 RID: 8056
		Stloc_1,
		// Token: 0x04001F79 RID: 8057
		Stloc_2,
		// Token: 0x04001F7A RID: 8058
		Stloc_3,
		// Token: 0x04001F7B RID: 8059
		Ldarg_S,
		// Token: 0x04001F7C RID: 8060
		Ldarga_S,
		// Token: 0x04001F7D RID: 8061
		Starg_S,
		// Token: 0x04001F7E RID: 8062
		Ldloc_S,
		// Token: 0x04001F7F RID: 8063
		Ldloca_S,
		// Token: 0x04001F80 RID: 8064
		Stloc_S,
		// Token: 0x04001F81 RID: 8065
		Ldnull,
		// Token: 0x04001F82 RID: 8066
		Ldc_I4_M1,
		// Token: 0x04001F83 RID: 8067
		Ldc_I4_0,
		// Token: 0x04001F84 RID: 8068
		Ldc_I4_1,
		// Token: 0x04001F85 RID: 8069
		Ldc_I4_2,
		// Token: 0x04001F86 RID: 8070
		Ldc_I4_3,
		// Token: 0x04001F87 RID: 8071
		Ldc_I4_4,
		// Token: 0x04001F88 RID: 8072
		Ldc_I4_5,
		// Token: 0x04001F89 RID: 8073
		Ldc_I4_6,
		// Token: 0x04001F8A RID: 8074
		Ldc_I4_7,
		// Token: 0x04001F8B RID: 8075
		Ldc_I4_8,
		// Token: 0x04001F8C RID: 8076
		Ldc_I4_S,
		// Token: 0x04001F8D RID: 8077
		Ldc_I4,
		// Token: 0x04001F8E RID: 8078
		Ldc_I8,
		// Token: 0x04001F8F RID: 8079
		Ldc_R4,
		// Token: 0x04001F90 RID: 8080
		Ldc_R8,
		// Token: 0x04001F91 RID: 8081
		Dup = 37,
		// Token: 0x04001F92 RID: 8082
		Pop,
		// Token: 0x04001F93 RID: 8083
		Jmp,
		// Token: 0x04001F94 RID: 8084
		Call,
		// Token: 0x04001F95 RID: 8085
		Calli,
		// Token: 0x04001F96 RID: 8086
		Ret,
		// Token: 0x04001F97 RID: 8087
		Br_S,
		// Token: 0x04001F98 RID: 8088
		Brfalse_S,
		// Token: 0x04001F99 RID: 8089
		Brtrue_S,
		// Token: 0x04001F9A RID: 8090
		Beq_S,
		// Token: 0x04001F9B RID: 8091
		Bge_S,
		// Token: 0x04001F9C RID: 8092
		Bgt_S,
		// Token: 0x04001F9D RID: 8093
		Ble_S,
		// Token: 0x04001F9E RID: 8094
		Blt_S,
		// Token: 0x04001F9F RID: 8095
		Bne_Un_S,
		// Token: 0x04001FA0 RID: 8096
		Bge_Un_S,
		// Token: 0x04001FA1 RID: 8097
		Bgt_Un_S,
		// Token: 0x04001FA2 RID: 8098
		Ble_Un_S,
		// Token: 0x04001FA3 RID: 8099
		Blt_Un_S,
		// Token: 0x04001FA4 RID: 8100
		Br,
		// Token: 0x04001FA5 RID: 8101
		Brfalse,
		// Token: 0x04001FA6 RID: 8102
		Brtrue,
		// Token: 0x04001FA7 RID: 8103
		Beq,
		// Token: 0x04001FA8 RID: 8104
		Bge,
		// Token: 0x04001FA9 RID: 8105
		Bgt,
		// Token: 0x04001FAA RID: 8106
		Ble,
		// Token: 0x04001FAB RID: 8107
		Blt,
		// Token: 0x04001FAC RID: 8108
		Bne_Un,
		// Token: 0x04001FAD RID: 8109
		Bge_Un,
		// Token: 0x04001FAE RID: 8110
		Bgt_Un,
		// Token: 0x04001FAF RID: 8111
		Ble_Un,
		// Token: 0x04001FB0 RID: 8112
		Blt_Un,
		// Token: 0x04001FB1 RID: 8113
		Switch,
		// Token: 0x04001FB2 RID: 8114
		Ldind_I1,
		// Token: 0x04001FB3 RID: 8115
		Ldind_U1,
		// Token: 0x04001FB4 RID: 8116
		Ldind_I2,
		// Token: 0x04001FB5 RID: 8117
		Ldind_U2,
		// Token: 0x04001FB6 RID: 8118
		Ldind_I4,
		// Token: 0x04001FB7 RID: 8119
		Ldind_U4,
		// Token: 0x04001FB8 RID: 8120
		Ldind_I8,
		// Token: 0x04001FB9 RID: 8121
		Ldind_I,
		// Token: 0x04001FBA RID: 8122
		Ldind_R4,
		// Token: 0x04001FBB RID: 8123
		Ldind_R8,
		// Token: 0x04001FBC RID: 8124
		Ldind_Ref,
		// Token: 0x04001FBD RID: 8125
		Stind_Ref,
		// Token: 0x04001FBE RID: 8126
		Stind_I1,
		// Token: 0x04001FBF RID: 8127
		Stind_I2,
		// Token: 0x04001FC0 RID: 8128
		Stind_I4,
		// Token: 0x04001FC1 RID: 8129
		Stind_I8,
		// Token: 0x04001FC2 RID: 8130
		Stind_R4,
		// Token: 0x04001FC3 RID: 8131
		Stind_R8,
		// Token: 0x04001FC4 RID: 8132
		Add,
		// Token: 0x04001FC5 RID: 8133
		Sub,
		// Token: 0x04001FC6 RID: 8134
		Mul,
		// Token: 0x04001FC7 RID: 8135
		Div,
		// Token: 0x04001FC8 RID: 8136
		Div_Un,
		// Token: 0x04001FC9 RID: 8137
		Rem,
		// Token: 0x04001FCA RID: 8138
		Rem_Un,
		// Token: 0x04001FCB RID: 8139
		And,
		// Token: 0x04001FCC RID: 8140
		Or,
		// Token: 0x04001FCD RID: 8141
		Xor,
		// Token: 0x04001FCE RID: 8142
		Shl,
		// Token: 0x04001FCF RID: 8143
		Shr,
		// Token: 0x04001FD0 RID: 8144
		Shr_Un,
		// Token: 0x04001FD1 RID: 8145
		Neg,
		// Token: 0x04001FD2 RID: 8146
		Not,
		// Token: 0x04001FD3 RID: 8147
		Conv_I1,
		// Token: 0x04001FD4 RID: 8148
		Conv_I2,
		// Token: 0x04001FD5 RID: 8149
		Conv_I4,
		// Token: 0x04001FD6 RID: 8150
		Conv_I8,
		// Token: 0x04001FD7 RID: 8151
		Conv_R4,
		// Token: 0x04001FD8 RID: 8152
		Conv_R8,
		// Token: 0x04001FD9 RID: 8153
		Conv_U4,
		// Token: 0x04001FDA RID: 8154
		Conv_U8,
		// Token: 0x04001FDB RID: 8155
		Callvirt,
		// Token: 0x04001FDC RID: 8156
		Cpobj,
		// Token: 0x04001FDD RID: 8157
		Ldobj,
		// Token: 0x04001FDE RID: 8158
		Ldstr,
		// Token: 0x04001FDF RID: 8159
		Newobj,
		// Token: 0x04001FE0 RID: 8160
		Castclass,
		// Token: 0x04001FE1 RID: 8161
		Isinst,
		// Token: 0x04001FE2 RID: 8162
		Conv_R_Un,
		// Token: 0x04001FE3 RID: 8163
		Unbox = 121,
		// Token: 0x04001FE4 RID: 8164
		Throw,
		// Token: 0x04001FE5 RID: 8165
		Ldfld,
		// Token: 0x04001FE6 RID: 8166
		Ldflda,
		// Token: 0x04001FE7 RID: 8167
		Stfld,
		// Token: 0x04001FE8 RID: 8168
		Ldsfld,
		// Token: 0x04001FE9 RID: 8169
		Ldsflda,
		// Token: 0x04001FEA RID: 8170
		Stsfld,
		// Token: 0x04001FEB RID: 8171
		Stobj,
		// Token: 0x04001FEC RID: 8172
		Conv_Ovf_I1_Un,
		// Token: 0x04001FED RID: 8173
		Conv_Ovf_I2_Un,
		// Token: 0x04001FEE RID: 8174
		Conv_Ovf_I4_Un,
		// Token: 0x04001FEF RID: 8175
		Conv_Ovf_I8_Un,
		// Token: 0x04001FF0 RID: 8176
		Conv_Ovf_U1_Un,
		// Token: 0x04001FF1 RID: 8177
		Conv_Ovf_U2_Un,
		// Token: 0x04001FF2 RID: 8178
		Conv_Ovf_U4_Un,
		// Token: 0x04001FF3 RID: 8179
		Conv_Ovf_U8_Un,
		// Token: 0x04001FF4 RID: 8180
		Conv_Ovf_I_Un,
		// Token: 0x04001FF5 RID: 8181
		Conv_Ovf_U_Un,
		// Token: 0x04001FF6 RID: 8182
		Box,
		// Token: 0x04001FF7 RID: 8183
		Newarr,
		// Token: 0x04001FF8 RID: 8184
		Ldlen,
		// Token: 0x04001FF9 RID: 8185
		Ldelema,
		// Token: 0x04001FFA RID: 8186
		Ldelem_I1,
		// Token: 0x04001FFB RID: 8187
		Ldelem_U1,
		// Token: 0x04001FFC RID: 8188
		Ldelem_I2,
		// Token: 0x04001FFD RID: 8189
		Ldelem_U2,
		// Token: 0x04001FFE RID: 8190
		Ldelem_I4,
		// Token: 0x04001FFF RID: 8191
		Ldelem_U4,
		// Token: 0x04002000 RID: 8192
		Ldelem_I8,
		// Token: 0x04002001 RID: 8193
		Ldelem_I,
		// Token: 0x04002002 RID: 8194
		Ldelem_R4,
		// Token: 0x04002003 RID: 8195
		Ldelem_R8,
		// Token: 0x04002004 RID: 8196
		Ldelem_Ref,
		// Token: 0x04002005 RID: 8197
		Stelem_I,
		// Token: 0x04002006 RID: 8198
		Stelem_I1,
		// Token: 0x04002007 RID: 8199
		Stelem_I2,
		// Token: 0x04002008 RID: 8200
		Stelem_I4,
		// Token: 0x04002009 RID: 8201
		Stelem_I8,
		// Token: 0x0400200A RID: 8202
		Stelem_R4,
		// Token: 0x0400200B RID: 8203
		Stelem_R8,
		// Token: 0x0400200C RID: 8204
		Stelem_Ref,
		// Token: 0x0400200D RID: 8205
		Ldelem,
		// Token: 0x0400200E RID: 8206
		Stelem,
		// Token: 0x0400200F RID: 8207
		Unbox_Any,
		// Token: 0x04002010 RID: 8208
		Conv_Ovf_I1 = 179,
		// Token: 0x04002011 RID: 8209
		Conv_Ovf_U1,
		// Token: 0x04002012 RID: 8210
		Conv_Ovf_I2,
		// Token: 0x04002013 RID: 8211
		Conv_Ovf_U2,
		// Token: 0x04002014 RID: 8212
		Conv_Ovf_I4,
		// Token: 0x04002015 RID: 8213
		Conv_Ovf_U4,
		// Token: 0x04002016 RID: 8214
		Conv_Ovf_I8,
		// Token: 0x04002017 RID: 8215
		Conv_Ovf_U8,
		// Token: 0x04002018 RID: 8216
		Refanyval = 194,
		// Token: 0x04002019 RID: 8217
		Ckfinite,
		// Token: 0x0400201A RID: 8218
		Mkrefany = 198,
		// Token: 0x0400201B RID: 8219
		Ldtoken = 208,
		// Token: 0x0400201C RID: 8220
		Conv_U2,
		// Token: 0x0400201D RID: 8221
		Conv_U1,
		// Token: 0x0400201E RID: 8222
		Conv_I,
		// Token: 0x0400201F RID: 8223
		Conv_Ovf_I,
		// Token: 0x04002020 RID: 8224
		Conv_Ovf_U,
		// Token: 0x04002021 RID: 8225
		Add_Ovf,
		// Token: 0x04002022 RID: 8226
		Add_Ovf_Un,
		// Token: 0x04002023 RID: 8227
		Mul_Ovf,
		// Token: 0x04002024 RID: 8228
		Mul_Ovf_Un,
		// Token: 0x04002025 RID: 8229
		Sub_Ovf,
		// Token: 0x04002026 RID: 8230
		Sub_Ovf_Un,
		// Token: 0x04002027 RID: 8231
		Endfinally,
		// Token: 0x04002028 RID: 8232
		Leave,
		// Token: 0x04002029 RID: 8233
		Leave_S,
		// Token: 0x0400202A RID: 8234
		Stind_I,
		// Token: 0x0400202B RID: 8235
		Conv_U,
		// Token: 0x0400202C RID: 8236
		Prefix7 = 248,
		// Token: 0x0400202D RID: 8237
		Prefix6,
		// Token: 0x0400202E RID: 8238
		Prefix5,
		// Token: 0x0400202F RID: 8239
		Prefix4,
		// Token: 0x04002030 RID: 8240
		Prefix3,
		// Token: 0x04002031 RID: 8241
		Prefix2,
		// Token: 0x04002032 RID: 8242
		Prefix1,
		// Token: 0x04002033 RID: 8243
		Prefixref,
		// Token: 0x04002034 RID: 8244
		Arglist = 65024,
		// Token: 0x04002035 RID: 8245
		Ceq,
		// Token: 0x04002036 RID: 8246
		Cgt,
		// Token: 0x04002037 RID: 8247
		Cgt_Un,
		// Token: 0x04002038 RID: 8248
		Clt,
		// Token: 0x04002039 RID: 8249
		Clt_Un,
		// Token: 0x0400203A RID: 8250
		Ldftn,
		// Token: 0x0400203B RID: 8251
		Ldvirtftn,
		// Token: 0x0400203C RID: 8252
		Ldarg = 65033,
		// Token: 0x0400203D RID: 8253
		Ldarga,
		// Token: 0x0400203E RID: 8254
		Starg,
		// Token: 0x0400203F RID: 8255
		Ldloc,
		// Token: 0x04002040 RID: 8256
		Ldloca,
		// Token: 0x04002041 RID: 8257
		Stloc,
		// Token: 0x04002042 RID: 8258
		Localloc,
		// Token: 0x04002043 RID: 8259
		Endfilter = 65041,
		// Token: 0x04002044 RID: 8260
		Unaligned_,
		// Token: 0x04002045 RID: 8261
		Volatile_,
		// Token: 0x04002046 RID: 8262
		Tail_,
		// Token: 0x04002047 RID: 8263
		Initobj,
		// Token: 0x04002048 RID: 8264
		Constrained_,
		// Token: 0x04002049 RID: 8265
		Cpblk,
		// Token: 0x0400204A RID: 8266
		Initblk,
		// Token: 0x0400204B RID: 8267
		Rethrow = 65050,
		// Token: 0x0400204C RID: 8268
		Sizeof = 65052,
		// Token: 0x0400204D RID: 8269
		Refanytype,
		// Token: 0x0400204E RID: 8270
		Readonly_
	}
}
