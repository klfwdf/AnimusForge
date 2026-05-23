using System;
using System.ComponentModel;

namespace Iced.Intel
{
	// Token: 0x02000662 RID: 1634
	internal enum Register
	{
		// Token: 0x0400332E RID: 13102
		None,
		// Token: 0x0400332F RID: 13103
		AL,
		// Token: 0x04003330 RID: 13104
		CL,
		// Token: 0x04003331 RID: 13105
		DL,
		// Token: 0x04003332 RID: 13106
		BL,
		// Token: 0x04003333 RID: 13107
		AH,
		// Token: 0x04003334 RID: 13108
		CH,
		// Token: 0x04003335 RID: 13109
		DH,
		// Token: 0x04003336 RID: 13110
		BH,
		// Token: 0x04003337 RID: 13111
		SPL,
		// Token: 0x04003338 RID: 13112
		BPL,
		// Token: 0x04003339 RID: 13113
		SIL,
		// Token: 0x0400333A RID: 13114
		DIL,
		// Token: 0x0400333B RID: 13115
		R8L,
		// Token: 0x0400333C RID: 13116
		R9L,
		// Token: 0x0400333D RID: 13117
		R10L,
		// Token: 0x0400333E RID: 13118
		R11L,
		// Token: 0x0400333F RID: 13119
		R12L,
		// Token: 0x04003340 RID: 13120
		R13L,
		// Token: 0x04003341 RID: 13121
		R14L,
		// Token: 0x04003342 RID: 13122
		R15L,
		// Token: 0x04003343 RID: 13123
		AX,
		// Token: 0x04003344 RID: 13124
		CX,
		// Token: 0x04003345 RID: 13125
		DX,
		// Token: 0x04003346 RID: 13126
		BX,
		// Token: 0x04003347 RID: 13127
		SP,
		// Token: 0x04003348 RID: 13128
		BP,
		// Token: 0x04003349 RID: 13129
		SI,
		// Token: 0x0400334A RID: 13130
		DI,
		// Token: 0x0400334B RID: 13131
		R8W,
		// Token: 0x0400334C RID: 13132
		R9W,
		// Token: 0x0400334D RID: 13133
		R10W,
		// Token: 0x0400334E RID: 13134
		R11W,
		// Token: 0x0400334F RID: 13135
		R12W,
		// Token: 0x04003350 RID: 13136
		R13W,
		// Token: 0x04003351 RID: 13137
		R14W,
		// Token: 0x04003352 RID: 13138
		R15W,
		// Token: 0x04003353 RID: 13139
		EAX,
		// Token: 0x04003354 RID: 13140
		ECX,
		// Token: 0x04003355 RID: 13141
		EDX,
		// Token: 0x04003356 RID: 13142
		EBX,
		// Token: 0x04003357 RID: 13143
		ESP,
		// Token: 0x04003358 RID: 13144
		EBP,
		// Token: 0x04003359 RID: 13145
		ESI,
		// Token: 0x0400335A RID: 13146
		EDI,
		// Token: 0x0400335B RID: 13147
		R8D,
		// Token: 0x0400335C RID: 13148
		R9D,
		// Token: 0x0400335D RID: 13149
		R10D,
		// Token: 0x0400335E RID: 13150
		R11D,
		// Token: 0x0400335F RID: 13151
		R12D,
		// Token: 0x04003360 RID: 13152
		R13D,
		// Token: 0x04003361 RID: 13153
		R14D,
		// Token: 0x04003362 RID: 13154
		R15D,
		// Token: 0x04003363 RID: 13155
		RAX,
		// Token: 0x04003364 RID: 13156
		RCX,
		// Token: 0x04003365 RID: 13157
		RDX,
		// Token: 0x04003366 RID: 13158
		RBX,
		// Token: 0x04003367 RID: 13159
		RSP,
		// Token: 0x04003368 RID: 13160
		RBP,
		// Token: 0x04003369 RID: 13161
		RSI,
		// Token: 0x0400336A RID: 13162
		RDI,
		// Token: 0x0400336B RID: 13163
		R8,
		// Token: 0x0400336C RID: 13164
		R9,
		// Token: 0x0400336D RID: 13165
		R10,
		// Token: 0x0400336E RID: 13166
		R11,
		// Token: 0x0400336F RID: 13167
		R12,
		// Token: 0x04003370 RID: 13168
		R13,
		// Token: 0x04003371 RID: 13169
		R14,
		// Token: 0x04003372 RID: 13170
		R15,
		// Token: 0x04003373 RID: 13171
		EIP,
		// Token: 0x04003374 RID: 13172
		RIP,
		// Token: 0x04003375 RID: 13173
		ES,
		// Token: 0x04003376 RID: 13174
		CS,
		// Token: 0x04003377 RID: 13175
		SS,
		// Token: 0x04003378 RID: 13176
		DS,
		// Token: 0x04003379 RID: 13177
		FS,
		// Token: 0x0400337A RID: 13178
		GS,
		// Token: 0x0400337B RID: 13179
		XMM0,
		// Token: 0x0400337C RID: 13180
		XMM1,
		// Token: 0x0400337D RID: 13181
		XMM2,
		// Token: 0x0400337E RID: 13182
		XMM3,
		// Token: 0x0400337F RID: 13183
		XMM4,
		// Token: 0x04003380 RID: 13184
		XMM5,
		// Token: 0x04003381 RID: 13185
		XMM6,
		// Token: 0x04003382 RID: 13186
		XMM7,
		// Token: 0x04003383 RID: 13187
		XMM8,
		// Token: 0x04003384 RID: 13188
		XMM9,
		// Token: 0x04003385 RID: 13189
		XMM10,
		// Token: 0x04003386 RID: 13190
		XMM11,
		// Token: 0x04003387 RID: 13191
		XMM12,
		// Token: 0x04003388 RID: 13192
		XMM13,
		// Token: 0x04003389 RID: 13193
		XMM14,
		// Token: 0x0400338A RID: 13194
		XMM15,
		// Token: 0x0400338B RID: 13195
		XMM16,
		// Token: 0x0400338C RID: 13196
		XMM17,
		// Token: 0x0400338D RID: 13197
		XMM18,
		// Token: 0x0400338E RID: 13198
		XMM19,
		// Token: 0x0400338F RID: 13199
		XMM20,
		// Token: 0x04003390 RID: 13200
		XMM21,
		// Token: 0x04003391 RID: 13201
		XMM22,
		// Token: 0x04003392 RID: 13202
		XMM23,
		// Token: 0x04003393 RID: 13203
		XMM24,
		// Token: 0x04003394 RID: 13204
		XMM25,
		// Token: 0x04003395 RID: 13205
		XMM26,
		// Token: 0x04003396 RID: 13206
		XMM27,
		// Token: 0x04003397 RID: 13207
		XMM28,
		// Token: 0x04003398 RID: 13208
		XMM29,
		// Token: 0x04003399 RID: 13209
		XMM30,
		// Token: 0x0400339A RID: 13210
		XMM31,
		// Token: 0x0400339B RID: 13211
		YMM0,
		// Token: 0x0400339C RID: 13212
		YMM1,
		// Token: 0x0400339D RID: 13213
		YMM2,
		// Token: 0x0400339E RID: 13214
		YMM3,
		// Token: 0x0400339F RID: 13215
		YMM4,
		// Token: 0x040033A0 RID: 13216
		YMM5,
		// Token: 0x040033A1 RID: 13217
		YMM6,
		// Token: 0x040033A2 RID: 13218
		YMM7,
		// Token: 0x040033A3 RID: 13219
		YMM8,
		// Token: 0x040033A4 RID: 13220
		YMM9,
		// Token: 0x040033A5 RID: 13221
		YMM10,
		// Token: 0x040033A6 RID: 13222
		YMM11,
		// Token: 0x040033A7 RID: 13223
		YMM12,
		// Token: 0x040033A8 RID: 13224
		YMM13,
		// Token: 0x040033A9 RID: 13225
		YMM14,
		// Token: 0x040033AA RID: 13226
		YMM15,
		// Token: 0x040033AB RID: 13227
		YMM16,
		// Token: 0x040033AC RID: 13228
		YMM17,
		// Token: 0x040033AD RID: 13229
		YMM18,
		// Token: 0x040033AE RID: 13230
		YMM19,
		// Token: 0x040033AF RID: 13231
		YMM20,
		// Token: 0x040033B0 RID: 13232
		YMM21,
		// Token: 0x040033B1 RID: 13233
		YMM22,
		// Token: 0x040033B2 RID: 13234
		YMM23,
		// Token: 0x040033B3 RID: 13235
		YMM24,
		// Token: 0x040033B4 RID: 13236
		YMM25,
		// Token: 0x040033B5 RID: 13237
		YMM26,
		// Token: 0x040033B6 RID: 13238
		YMM27,
		// Token: 0x040033B7 RID: 13239
		YMM28,
		// Token: 0x040033B8 RID: 13240
		YMM29,
		// Token: 0x040033B9 RID: 13241
		YMM30,
		// Token: 0x040033BA RID: 13242
		YMM31,
		// Token: 0x040033BB RID: 13243
		ZMM0,
		// Token: 0x040033BC RID: 13244
		ZMM1,
		// Token: 0x040033BD RID: 13245
		ZMM2,
		// Token: 0x040033BE RID: 13246
		ZMM3,
		// Token: 0x040033BF RID: 13247
		ZMM4,
		// Token: 0x040033C0 RID: 13248
		ZMM5,
		// Token: 0x040033C1 RID: 13249
		ZMM6,
		// Token: 0x040033C2 RID: 13250
		ZMM7,
		// Token: 0x040033C3 RID: 13251
		ZMM8,
		// Token: 0x040033C4 RID: 13252
		ZMM9,
		// Token: 0x040033C5 RID: 13253
		ZMM10,
		// Token: 0x040033C6 RID: 13254
		ZMM11,
		// Token: 0x040033C7 RID: 13255
		ZMM12,
		// Token: 0x040033C8 RID: 13256
		ZMM13,
		// Token: 0x040033C9 RID: 13257
		ZMM14,
		// Token: 0x040033CA RID: 13258
		ZMM15,
		// Token: 0x040033CB RID: 13259
		ZMM16,
		// Token: 0x040033CC RID: 13260
		ZMM17,
		// Token: 0x040033CD RID: 13261
		ZMM18,
		// Token: 0x040033CE RID: 13262
		ZMM19,
		// Token: 0x040033CF RID: 13263
		ZMM20,
		// Token: 0x040033D0 RID: 13264
		ZMM21,
		// Token: 0x040033D1 RID: 13265
		ZMM22,
		// Token: 0x040033D2 RID: 13266
		ZMM23,
		// Token: 0x040033D3 RID: 13267
		ZMM24,
		// Token: 0x040033D4 RID: 13268
		ZMM25,
		// Token: 0x040033D5 RID: 13269
		ZMM26,
		// Token: 0x040033D6 RID: 13270
		ZMM27,
		// Token: 0x040033D7 RID: 13271
		ZMM28,
		// Token: 0x040033D8 RID: 13272
		ZMM29,
		// Token: 0x040033D9 RID: 13273
		ZMM30,
		// Token: 0x040033DA RID: 13274
		ZMM31,
		// Token: 0x040033DB RID: 13275
		K0,
		// Token: 0x040033DC RID: 13276
		K1,
		// Token: 0x040033DD RID: 13277
		K2,
		// Token: 0x040033DE RID: 13278
		K3,
		// Token: 0x040033DF RID: 13279
		K4,
		// Token: 0x040033E0 RID: 13280
		K5,
		// Token: 0x040033E1 RID: 13281
		K6,
		// Token: 0x040033E2 RID: 13282
		K7,
		// Token: 0x040033E3 RID: 13283
		BND0,
		// Token: 0x040033E4 RID: 13284
		BND1,
		// Token: 0x040033E5 RID: 13285
		BND2,
		// Token: 0x040033E6 RID: 13286
		BND3,
		// Token: 0x040033E7 RID: 13287
		CR0,
		// Token: 0x040033E8 RID: 13288
		CR1,
		// Token: 0x040033E9 RID: 13289
		CR2,
		// Token: 0x040033EA RID: 13290
		CR3,
		// Token: 0x040033EB RID: 13291
		CR4,
		// Token: 0x040033EC RID: 13292
		CR5,
		// Token: 0x040033ED RID: 13293
		CR6,
		// Token: 0x040033EE RID: 13294
		CR7,
		// Token: 0x040033EF RID: 13295
		CR8,
		// Token: 0x040033F0 RID: 13296
		CR9,
		// Token: 0x040033F1 RID: 13297
		CR10,
		// Token: 0x040033F2 RID: 13298
		CR11,
		// Token: 0x040033F3 RID: 13299
		CR12,
		// Token: 0x040033F4 RID: 13300
		CR13,
		// Token: 0x040033F5 RID: 13301
		CR14,
		// Token: 0x040033F6 RID: 13302
		CR15,
		// Token: 0x040033F7 RID: 13303
		DR0,
		// Token: 0x040033F8 RID: 13304
		DR1,
		// Token: 0x040033F9 RID: 13305
		DR2,
		// Token: 0x040033FA RID: 13306
		DR3,
		// Token: 0x040033FB RID: 13307
		DR4,
		// Token: 0x040033FC RID: 13308
		DR5,
		// Token: 0x040033FD RID: 13309
		DR6,
		// Token: 0x040033FE RID: 13310
		DR7,
		// Token: 0x040033FF RID: 13311
		DR8,
		// Token: 0x04003400 RID: 13312
		DR9,
		// Token: 0x04003401 RID: 13313
		DR10,
		// Token: 0x04003402 RID: 13314
		DR11,
		// Token: 0x04003403 RID: 13315
		DR12,
		// Token: 0x04003404 RID: 13316
		DR13,
		// Token: 0x04003405 RID: 13317
		DR14,
		// Token: 0x04003406 RID: 13318
		DR15,
		// Token: 0x04003407 RID: 13319
		ST0,
		// Token: 0x04003408 RID: 13320
		ST1,
		// Token: 0x04003409 RID: 13321
		ST2,
		// Token: 0x0400340A RID: 13322
		ST3,
		// Token: 0x0400340B RID: 13323
		ST4,
		// Token: 0x0400340C RID: 13324
		ST5,
		// Token: 0x0400340D RID: 13325
		ST6,
		// Token: 0x0400340E RID: 13326
		ST7,
		// Token: 0x0400340F RID: 13327
		MM0,
		// Token: 0x04003410 RID: 13328
		MM1,
		// Token: 0x04003411 RID: 13329
		MM2,
		// Token: 0x04003412 RID: 13330
		MM3,
		// Token: 0x04003413 RID: 13331
		MM4,
		// Token: 0x04003414 RID: 13332
		MM5,
		// Token: 0x04003415 RID: 13333
		MM6,
		// Token: 0x04003416 RID: 13334
		MM7,
		// Token: 0x04003417 RID: 13335
		TR0,
		// Token: 0x04003418 RID: 13336
		TR1,
		// Token: 0x04003419 RID: 13337
		TR2,
		// Token: 0x0400341A RID: 13338
		TR3,
		// Token: 0x0400341B RID: 13339
		TR4,
		// Token: 0x0400341C RID: 13340
		TR5,
		// Token: 0x0400341D RID: 13341
		TR6,
		// Token: 0x0400341E RID: 13342
		TR7,
		// Token: 0x0400341F RID: 13343
		TMM0,
		// Token: 0x04003420 RID: 13344
		TMM1,
		// Token: 0x04003421 RID: 13345
		TMM2,
		// Token: 0x04003422 RID: 13346
		TMM3,
		// Token: 0x04003423 RID: 13347
		TMM4,
		// Token: 0x04003424 RID: 13348
		TMM5,
		// Token: 0x04003425 RID: 13349
		TMM6,
		// Token: 0x04003426 RID: 13350
		TMM7,
		// Token: 0x04003427 RID: 13351
		[Obsolete("Not part of the public API", false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		DontUse0,
		// Token: 0x04003428 RID: 13352
		[Obsolete("Not part of the public API", false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		DontUseFA,
		// Token: 0x04003429 RID: 13353
		[Obsolete("Not part of the public API", false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		DontUseFB,
		// Token: 0x0400342A RID: 13354
		[Obsolete("Not part of the public API", false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		DontUseFC,
		// Token: 0x0400342B RID: 13355
		[Obsolete("Not part of the public API", false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		DontUseFD,
		// Token: 0x0400342C RID: 13356
		[Obsolete("Not part of the public API", false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		DontUseFE,
		// Token: 0x0400342D RID: 13357
		[Obsolete("Not part of the public API", false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		DontUseFF
	}
}
