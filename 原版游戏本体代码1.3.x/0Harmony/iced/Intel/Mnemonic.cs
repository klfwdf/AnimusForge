using System;

namespace Iced.Intel
{
	// Token: 0x0200065E RID: 1630
	internal enum Mnemonic
	{
		// Token: 0x04002BAC RID: 11180
		INVALID,
		// Token: 0x04002BAD RID: 11181
		Aaa,
		// Token: 0x04002BAE RID: 11182
		Aad,
		// Token: 0x04002BAF RID: 11183
		Aam,
		// Token: 0x04002BB0 RID: 11184
		Aas,
		// Token: 0x04002BB1 RID: 11185
		Adc,
		// Token: 0x04002BB2 RID: 11186
		Adcx,
		// Token: 0x04002BB3 RID: 11187
		Add,
		// Token: 0x04002BB4 RID: 11188
		Addpd,
		// Token: 0x04002BB5 RID: 11189
		Addps,
		// Token: 0x04002BB6 RID: 11190
		Addsd,
		// Token: 0x04002BB7 RID: 11191
		Addss,
		// Token: 0x04002BB8 RID: 11192
		Addsubpd,
		// Token: 0x04002BB9 RID: 11193
		Addsubps,
		// Token: 0x04002BBA RID: 11194
		Adox,
		// Token: 0x04002BBB RID: 11195
		Aesdec,
		// Token: 0x04002BBC RID: 11196
		Aesdeclast,
		// Token: 0x04002BBD RID: 11197
		Aesenc,
		// Token: 0x04002BBE RID: 11198
		Aesenclast,
		// Token: 0x04002BBF RID: 11199
		Aesimc,
		// Token: 0x04002BC0 RID: 11200
		Aeskeygenassist,
		// Token: 0x04002BC1 RID: 11201
		And,
		// Token: 0x04002BC2 RID: 11202
		Andn,
		// Token: 0x04002BC3 RID: 11203
		Andnpd,
		// Token: 0x04002BC4 RID: 11204
		Andnps,
		// Token: 0x04002BC5 RID: 11205
		Andpd,
		// Token: 0x04002BC6 RID: 11206
		Andps,
		// Token: 0x04002BC7 RID: 11207
		Arpl,
		// Token: 0x04002BC8 RID: 11208
		Bextr,
		// Token: 0x04002BC9 RID: 11209
		Blcfill,
		// Token: 0x04002BCA RID: 11210
		Blci,
		// Token: 0x04002BCB RID: 11211
		Blcic,
		// Token: 0x04002BCC RID: 11212
		Blcmsk,
		// Token: 0x04002BCD RID: 11213
		Blcs,
		// Token: 0x04002BCE RID: 11214
		Blendpd,
		// Token: 0x04002BCF RID: 11215
		Blendps,
		// Token: 0x04002BD0 RID: 11216
		Blendvpd,
		// Token: 0x04002BD1 RID: 11217
		Blendvps,
		// Token: 0x04002BD2 RID: 11218
		Blsfill,
		// Token: 0x04002BD3 RID: 11219
		Blsi,
		// Token: 0x04002BD4 RID: 11220
		Blsic,
		// Token: 0x04002BD5 RID: 11221
		Blsmsk,
		// Token: 0x04002BD6 RID: 11222
		Blsr,
		// Token: 0x04002BD7 RID: 11223
		Bndcl,
		// Token: 0x04002BD8 RID: 11224
		Bndcn,
		// Token: 0x04002BD9 RID: 11225
		Bndcu,
		// Token: 0x04002BDA RID: 11226
		Bndldx,
		// Token: 0x04002BDB RID: 11227
		Bndmk,
		// Token: 0x04002BDC RID: 11228
		Bndmov,
		// Token: 0x04002BDD RID: 11229
		Bndstx,
		// Token: 0x04002BDE RID: 11230
		Bound,
		// Token: 0x04002BDF RID: 11231
		Bsf,
		// Token: 0x04002BE0 RID: 11232
		Bsr,
		// Token: 0x04002BE1 RID: 11233
		Bswap,
		// Token: 0x04002BE2 RID: 11234
		Bt,
		// Token: 0x04002BE3 RID: 11235
		Btc,
		// Token: 0x04002BE4 RID: 11236
		Btr,
		// Token: 0x04002BE5 RID: 11237
		Bts,
		// Token: 0x04002BE6 RID: 11238
		Bzhi,
		// Token: 0x04002BE7 RID: 11239
		Call,
		// Token: 0x04002BE8 RID: 11240
		Cbw,
		// Token: 0x04002BE9 RID: 11241
		Cdq,
		// Token: 0x04002BEA RID: 11242
		Cdqe,
		// Token: 0x04002BEB RID: 11243
		Cl1invmb,
		// Token: 0x04002BEC RID: 11244
		Clac,
		// Token: 0x04002BED RID: 11245
		Clc,
		// Token: 0x04002BEE RID: 11246
		Cld,
		// Token: 0x04002BEF RID: 11247
		Cldemote,
		// Token: 0x04002BF0 RID: 11248
		Clflush,
		// Token: 0x04002BF1 RID: 11249
		Clflushopt,
		// Token: 0x04002BF2 RID: 11250
		Clgi,
		// Token: 0x04002BF3 RID: 11251
		Cli,
		// Token: 0x04002BF4 RID: 11252
		Clrssbsy,
		// Token: 0x04002BF5 RID: 11253
		Clts,
		// Token: 0x04002BF6 RID: 11254
		Clwb,
		// Token: 0x04002BF7 RID: 11255
		Clzero,
		// Token: 0x04002BF8 RID: 11256
		Cmc,
		// Token: 0x04002BF9 RID: 11257
		Cmova,
		// Token: 0x04002BFA RID: 11258
		Cmovae,
		// Token: 0x04002BFB RID: 11259
		Cmovb,
		// Token: 0x04002BFC RID: 11260
		Cmovbe,
		// Token: 0x04002BFD RID: 11261
		Cmove,
		// Token: 0x04002BFE RID: 11262
		Cmovg,
		// Token: 0x04002BFF RID: 11263
		Cmovge,
		// Token: 0x04002C00 RID: 11264
		Cmovl,
		// Token: 0x04002C01 RID: 11265
		Cmovle,
		// Token: 0x04002C02 RID: 11266
		Cmovne,
		// Token: 0x04002C03 RID: 11267
		Cmovno,
		// Token: 0x04002C04 RID: 11268
		Cmovnp,
		// Token: 0x04002C05 RID: 11269
		Cmovns,
		// Token: 0x04002C06 RID: 11270
		Cmovo,
		// Token: 0x04002C07 RID: 11271
		Cmovp,
		// Token: 0x04002C08 RID: 11272
		Cmovs,
		// Token: 0x04002C09 RID: 11273
		Cmp,
		// Token: 0x04002C0A RID: 11274
		Cmppd,
		// Token: 0x04002C0B RID: 11275
		Cmpps,
		// Token: 0x04002C0C RID: 11276
		Cmpsb,
		// Token: 0x04002C0D RID: 11277
		Cmpsd,
		// Token: 0x04002C0E RID: 11278
		Cmpsq,
		// Token: 0x04002C0F RID: 11279
		Cmpss,
		// Token: 0x04002C10 RID: 11280
		Cmpsw,
		// Token: 0x04002C11 RID: 11281
		Cmpxchg,
		// Token: 0x04002C12 RID: 11282
		Cmpxchg16b,
		// Token: 0x04002C13 RID: 11283
		Cmpxchg8b,
		// Token: 0x04002C14 RID: 11284
		Comisd,
		// Token: 0x04002C15 RID: 11285
		Comiss,
		// Token: 0x04002C16 RID: 11286
		Cpuid,
		// Token: 0x04002C17 RID: 11287
		Cqo,
		// Token: 0x04002C18 RID: 11288
		Crc32,
		// Token: 0x04002C19 RID: 11289
		Cvtdq2pd,
		// Token: 0x04002C1A RID: 11290
		Cvtdq2ps,
		// Token: 0x04002C1B RID: 11291
		Cvtpd2dq,
		// Token: 0x04002C1C RID: 11292
		Cvtpd2pi,
		// Token: 0x04002C1D RID: 11293
		Cvtpd2ps,
		// Token: 0x04002C1E RID: 11294
		Cvtpi2pd,
		// Token: 0x04002C1F RID: 11295
		Cvtpi2ps,
		// Token: 0x04002C20 RID: 11296
		Cvtps2dq,
		// Token: 0x04002C21 RID: 11297
		Cvtps2pd,
		// Token: 0x04002C22 RID: 11298
		Cvtps2pi,
		// Token: 0x04002C23 RID: 11299
		Cvtsd2si,
		// Token: 0x04002C24 RID: 11300
		Cvtsd2ss,
		// Token: 0x04002C25 RID: 11301
		Cvtsi2sd,
		// Token: 0x04002C26 RID: 11302
		Cvtsi2ss,
		// Token: 0x04002C27 RID: 11303
		Cvtss2sd,
		// Token: 0x04002C28 RID: 11304
		Cvtss2si,
		// Token: 0x04002C29 RID: 11305
		Cvttpd2dq,
		// Token: 0x04002C2A RID: 11306
		Cvttpd2pi,
		// Token: 0x04002C2B RID: 11307
		Cvttps2dq,
		// Token: 0x04002C2C RID: 11308
		Cvttps2pi,
		// Token: 0x04002C2D RID: 11309
		Cvttsd2si,
		// Token: 0x04002C2E RID: 11310
		Cvttss2si,
		// Token: 0x04002C2F RID: 11311
		Cwd,
		// Token: 0x04002C30 RID: 11312
		Cwde,
		// Token: 0x04002C31 RID: 11313
		Daa,
		// Token: 0x04002C32 RID: 11314
		Das,
		// Token: 0x04002C33 RID: 11315
		Db,
		// Token: 0x04002C34 RID: 11316
		Dd,
		// Token: 0x04002C35 RID: 11317
		Dec,
		// Token: 0x04002C36 RID: 11318
		Div,
		// Token: 0x04002C37 RID: 11319
		Divpd,
		// Token: 0x04002C38 RID: 11320
		Divps,
		// Token: 0x04002C39 RID: 11321
		Divsd,
		// Token: 0x04002C3A RID: 11322
		Divss,
		// Token: 0x04002C3B RID: 11323
		Dppd,
		// Token: 0x04002C3C RID: 11324
		Dpps,
		// Token: 0x04002C3D RID: 11325
		Dq,
		// Token: 0x04002C3E RID: 11326
		Dw,
		// Token: 0x04002C3F RID: 11327
		Emms,
		// Token: 0x04002C40 RID: 11328
		Encls,
		// Token: 0x04002C41 RID: 11329
		Enclu,
		// Token: 0x04002C42 RID: 11330
		Enclv,
		// Token: 0x04002C43 RID: 11331
		Endbr32,
		// Token: 0x04002C44 RID: 11332
		Endbr64,
		// Token: 0x04002C45 RID: 11333
		Enqcmd,
		// Token: 0x04002C46 RID: 11334
		Enqcmds,
		// Token: 0x04002C47 RID: 11335
		Enter,
		// Token: 0x04002C48 RID: 11336
		Extractps,
		// Token: 0x04002C49 RID: 11337
		Extrq,
		// Token: 0x04002C4A RID: 11338
		F2xm1,
		// Token: 0x04002C4B RID: 11339
		Fabs,
		// Token: 0x04002C4C RID: 11340
		Fadd,
		// Token: 0x04002C4D RID: 11341
		Faddp,
		// Token: 0x04002C4E RID: 11342
		Fbld,
		// Token: 0x04002C4F RID: 11343
		Fbstp,
		// Token: 0x04002C50 RID: 11344
		Fchs,
		// Token: 0x04002C51 RID: 11345
		Fclex,
		// Token: 0x04002C52 RID: 11346
		Fcmovb,
		// Token: 0x04002C53 RID: 11347
		Fcmovbe,
		// Token: 0x04002C54 RID: 11348
		Fcmove,
		// Token: 0x04002C55 RID: 11349
		Fcmovnb,
		// Token: 0x04002C56 RID: 11350
		Fcmovnbe,
		// Token: 0x04002C57 RID: 11351
		Fcmovne,
		// Token: 0x04002C58 RID: 11352
		Fcmovnu,
		// Token: 0x04002C59 RID: 11353
		Fcmovu,
		// Token: 0x04002C5A RID: 11354
		Fcom,
		// Token: 0x04002C5B RID: 11355
		Fcomi,
		// Token: 0x04002C5C RID: 11356
		Fcomip,
		// Token: 0x04002C5D RID: 11357
		Fcomp,
		// Token: 0x04002C5E RID: 11358
		Fcompp,
		// Token: 0x04002C5F RID: 11359
		Fcos,
		// Token: 0x04002C60 RID: 11360
		Fdecstp,
		// Token: 0x04002C61 RID: 11361
		Fdisi,
		// Token: 0x04002C62 RID: 11362
		Fdiv,
		// Token: 0x04002C63 RID: 11363
		Fdivp,
		// Token: 0x04002C64 RID: 11364
		Fdivr,
		// Token: 0x04002C65 RID: 11365
		Fdivrp,
		// Token: 0x04002C66 RID: 11366
		Femms,
		// Token: 0x04002C67 RID: 11367
		Feni,
		// Token: 0x04002C68 RID: 11368
		Ffree,
		// Token: 0x04002C69 RID: 11369
		Ffreep,
		// Token: 0x04002C6A RID: 11370
		Fiadd,
		// Token: 0x04002C6B RID: 11371
		Ficom,
		// Token: 0x04002C6C RID: 11372
		Ficomp,
		// Token: 0x04002C6D RID: 11373
		Fidiv,
		// Token: 0x04002C6E RID: 11374
		Fidivr,
		// Token: 0x04002C6F RID: 11375
		Fild,
		// Token: 0x04002C70 RID: 11376
		Fimul,
		// Token: 0x04002C71 RID: 11377
		Fincstp,
		// Token: 0x04002C72 RID: 11378
		Finit,
		// Token: 0x04002C73 RID: 11379
		Fist,
		// Token: 0x04002C74 RID: 11380
		Fistp,
		// Token: 0x04002C75 RID: 11381
		Fisttp,
		// Token: 0x04002C76 RID: 11382
		Fisub,
		// Token: 0x04002C77 RID: 11383
		Fisubr,
		// Token: 0x04002C78 RID: 11384
		Fld,
		// Token: 0x04002C79 RID: 11385
		Fld1,
		// Token: 0x04002C7A RID: 11386
		Fldcw,
		// Token: 0x04002C7B RID: 11387
		Fldenv,
		// Token: 0x04002C7C RID: 11388
		Fldl2e,
		// Token: 0x04002C7D RID: 11389
		Fldl2t,
		// Token: 0x04002C7E RID: 11390
		Fldlg2,
		// Token: 0x04002C7F RID: 11391
		Fldln2,
		// Token: 0x04002C80 RID: 11392
		Fldpi,
		// Token: 0x04002C81 RID: 11393
		Fldz,
		// Token: 0x04002C82 RID: 11394
		Fmul,
		// Token: 0x04002C83 RID: 11395
		Fmulp,
		// Token: 0x04002C84 RID: 11396
		Fnclex,
		// Token: 0x04002C85 RID: 11397
		Fndisi,
		// Token: 0x04002C86 RID: 11398
		Fneni,
		// Token: 0x04002C87 RID: 11399
		Fninit,
		// Token: 0x04002C88 RID: 11400
		Fnop,
		// Token: 0x04002C89 RID: 11401
		Fnsave,
		// Token: 0x04002C8A RID: 11402
		Fnsetpm,
		// Token: 0x04002C8B RID: 11403
		Fnstcw,
		// Token: 0x04002C8C RID: 11404
		Fnstenv,
		// Token: 0x04002C8D RID: 11405
		Fnstsw,
		// Token: 0x04002C8E RID: 11406
		Fpatan,
		// Token: 0x04002C8F RID: 11407
		Fprem,
		// Token: 0x04002C90 RID: 11408
		Fprem1,
		// Token: 0x04002C91 RID: 11409
		Fptan,
		// Token: 0x04002C92 RID: 11410
		Frndint,
		// Token: 0x04002C93 RID: 11411
		Frstor,
		// Token: 0x04002C94 RID: 11412
		Frstpm,
		// Token: 0x04002C95 RID: 11413
		Fsave,
		// Token: 0x04002C96 RID: 11414
		Fscale,
		// Token: 0x04002C97 RID: 11415
		Fsetpm,
		// Token: 0x04002C98 RID: 11416
		Fsin,
		// Token: 0x04002C99 RID: 11417
		Fsincos,
		// Token: 0x04002C9A RID: 11418
		Fsqrt,
		// Token: 0x04002C9B RID: 11419
		Fst,
		// Token: 0x04002C9C RID: 11420
		Fstcw,
		// Token: 0x04002C9D RID: 11421
		Fstdw,
		// Token: 0x04002C9E RID: 11422
		Fstenv,
		// Token: 0x04002C9F RID: 11423
		Fstp,
		// Token: 0x04002CA0 RID: 11424
		Fstpnce,
		// Token: 0x04002CA1 RID: 11425
		Fstsg,
		// Token: 0x04002CA2 RID: 11426
		Fstsw,
		// Token: 0x04002CA3 RID: 11427
		Fsub,
		// Token: 0x04002CA4 RID: 11428
		Fsubp,
		// Token: 0x04002CA5 RID: 11429
		Fsubr,
		// Token: 0x04002CA6 RID: 11430
		Fsubrp,
		// Token: 0x04002CA7 RID: 11431
		Ftst,
		// Token: 0x04002CA8 RID: 11432
		Fucom,
		// Token: 0x04002CA9 RID: 11433
		Fucomi,
		// Token: 0x04002CAA RID: 11434
		Fucomip,
		// Token: 0x04002CAB RID: 11435
		Fucomp,
		// Token: 0x04002CAC RID: 11436
		Fucompp,
		// Token: 0x04002CAD RID: 11437
		Fxam,
		// Token: 0x04002CAE RID: 11438
		Fxch,
		// Token: 0x04002CAF RID: 11439
		Fxrstor,
		// Token: 0x04002CB0 RID: 11440
		Fxrstor64,
		// Token: 0x04002CB1 RID: 11441
		Fxsave,
		// Token: 0x04002CB2 RID: 11442
		Fxsave64,
		// Token: 0x04002CB3 RID: 11443
		Fxtract,
		// Token: 0x04002CB4 RID: 11444
		Fyl2x,
		// Token: 0x04002CB5 RID: 11445
		Fyl2xp1,
		// Token: 0x04002CB6 RID: 11446
		Getsec,
		// Token: 0x04002CB7 RID: 11447
		Gf2p8affineinvqb,
		// Token: 0x04002CB8 RID: 11448
		Gf2p8affineqb,
		// Token: 0x04002CB9 RID: 11449
		Gf2p8mulb,
		// Token: 0x04002CBA RID: 11450
		Haddpd,
		// Token: 0x04002CBB RID: 11451
		Haddps,
		// Token: 0x04002CBC RID: 11452
		Hlt,
		// Token: 0x04002CBD RID: 11453
		Hsubpd,
		// Token: 0x04002CBE RID: 11454
		Hsubps,
		// Token: 0x04002CBF RID: 11455
		Ibts,
		// Token: 0x04002CC0 RID: 11456
		Idiv,
		// Token: 0x04002CC1 RID: 11457
		Imul,
		// Token: 0x04002CC2 RID: 11458
		In,
		// Token: 0x04002CC3 RID: 11459
		Inc,
		// Token: 0x04002CC4 RID: 11460
		Incsspd,
		// Token: 0x04002CC5 RID: 11461
		Incsspq,
		// Token: 0x04002CC6 RID: 11462
		Insb,
		// Token: 0x04002CC7 RID: 11463
		Insd,
		// Token: 0x04002CC8 RID: 11464
		Insertps,
		// Token: 0x04002CC9 RID: 11465
		Insertq,
		// Token: 0x04002CCA RID: 11466
		Insw,
		// Token: 0x04002CCB RID: 11467
		Int,
		// Token: 0x04002CCC RID: 11468
		Int1,
		// Token: 0x04002CCD RID: 11469
		Into,
		// Token: 0x04002CCE RID: 11470
		Invd,
		// Token: 0x04002CCF RID: 11471
		Invept,
		// Token: 0x04002CD0 RID: 11472
		Invlpg,
		// Token: 0x04002CD1 RID: 11473
		Invlpga,
		// Token: 0x04002CD2 RID: 11474
		Invpcid,
		// Token: 0x04002CD3 RID: 11475
		Invvpid,
		// Token: 0x04002CD4 RID: 11476
		Iret,
		// Token: 0x04002CD5 RID: 11477
		Ja,
		// Token: 0x04002CD6 RID: 11478
		Jae,
		// Token: 0x04002CD7 RID: 11479
		Jb,
		// Token: 0x04002CD8 RID: 11480
		Jbe,
		// Token: 0x04002CD9 RID: 11481
		Jcxz,
		// Token: 0x04002CDA RID: 11482
		Je,
		// Token: 0x04002CDB RID: 11483
		Jecxz,
		// Token: 0x04002CDC RID: 11484
		Jg,
		// Token: 0x04002CDD RID: 11485
		Jge,
		// Token: 0x04002CDE RID: 11486
		Jl,
		// Token: 0x04002CDF RID: 11487
		Jle,
		// Token: 0x04002CE0 RID: 11488
		Jmp,
		// Token: 0x04002CE1 RID: 11489
		Jmpe,
		// Token: 0x04002CE2 RID: 11490
		Jne,
		// Token: 0x04002CE3 RID: 11491
		Jno,
		// Token: 0x04002CE4 RID: 11492
		Jnp,
		// Token: 0x04002CE5 RID: 11493
		Jns,
		// Token: 0x04002CE6 RID: 11494
		Jo,
		// Token: 0x04002CE7 RID: 11495
		Jp,
		// Token: 0x04002CE8 RID: 11496
		Jrcxz,
		// Token: 0x04002CE9 RID: 11497
		Js,
		// Token: 0x04002CEA RID: 11498
		Kaddb,
		// Token: 0x04002CEB RID: 11499
		Kaddd,
		// Token: 0x04002CEC RID: 11500
		Kaddq,
		// Token: 0x04002CED RID: 11501
		Kaddw,
		// Token: 0x04002CEE RID: 11502
		Kandb,
		// Token: 0x04002CEF RID: 11503
		Kandd,
		// Token: 0x04002CF0 RID: 11504
		Kandnb,
		// Token: 0x04002CF1 RID: 11505
		Kandnd,
		// Token: 0x04002CF2 RID: 11506
		Kandnq,
		// Token: 0x04002CF3 RID: 11507
		Kandnw,
		// Token: 0x04002CF4 RID: 11508
		Kandq,
		// Token: 0x04002CF5 RID: 11509
		Kandw,
		// Token: 0x04002CF6 RID: 11510
		Kmovb,
		// Token: 0x04002CF7 RID: 11511
		Kmovd,
		// Token: 0x04002CF8 RID: 11512
		Kmovq,
		// Token: 0x04002CF9 RID: 11513
		Kmovw,
		// Token: 0x04002CFA RID: 11514
		Knotb,
		// Token: 0x04002CFB RID: 11515
		Knotd,
		// Token: 0x04002CFC RID: 11516
		Knotq,
		// Token: 0x04002CFD RID: 11517
		Knotw,
		// Token: 0x04002CFE RID: 11518
		Korb,
		// Token: 0x04002CFF RID: 11519
		Kord,
		// Token: 0x04002D00 RID: 11520
		Korq,
		// Token: 0x04002D01 RID: 11521
		Kortestb,
		// Token: 0x04002D02 RID: 11522
		Kortestd,
		// Token: 0x04002D03 RID: 11523
		Kortestq,
		// Token: 0x04002D04 RID: 11524
		Kortestw,
		// Token: 0x04002D05 RID: 11525
		Korw,
		// Token: 0x04002D06 RID: 11526
		Kshiftlb,
		// Token: 0x04002D07 RID: 11527
		Kshiftld,
		// Token: 0x04002D08 RID: 11528
		Kshiftlq,
		// Token: 0x04002D09 RID: 11529
		Kshiftlw,
		// Token: 0x04002D0A RID: 11530
		Kshiftrb,
		// Token: 0x04002D0B RID: 11531
		Kshiftrd,
		// Token: 0x04002D0C RID: 11532
		Kshiftrq,
		// Token: 0x04002D0D RID: 11533
		Kshiftrw,
		// Token: 0x04002D0E RID: 11534
		Ktestb,
		// Token: 0x04002D0F RID: 11535
		Ktestd,
		// Token: 0x04002D10 RID: 11536
		Ktestq,
		// Token: 0x04002D11 RID: 11537
		Ktestw,
		// Token: 0x04002D12 RID: 11538
		Kunpckbw,
		// Token: 0x04002D13 RID: 11539
		Kunpckdq,
		// Token: 0x04002D14 RID: 11540
		Kunpckwd,
		// Token: 0x04002D15 RID: 11541
		Kxnorb,
		// Token: 0x04002D16 RID: 11542
		Kxnord,
		// Token: 0x04002D17 RID: 11543
		Kxnorq,
		// Token: 0x04002D18 RID: 11544
		Kxnorw,
		// Token: 0x04002D19 RID: 11545
		Kxorb,
		// Token: 0x04002D1A RID: 11546
		Kxord,
		// Token: 0x04002D1B RID: 11547
		Kxorq,
		// Token: 0x04002D1C RID: 11548
		Kxorw,
		// Token: 0x04002D1D RID: 11549
		Lahf,
		// Token: 0x04002D1E RID: 11550
		Lar,
		// Token: 0x04002D1F RID: 11551
		Lddqu,
		// Token: 0x04002D20 RID: 11552
		Ldmxcsr,
		// Token: 0x04002D21 RID: 11553
		Lds,
		// Token: 0x04002D22 RID: 11554
		Lea,
		// Token: 0x04002D23 RID: 11555
		Leave,
		// Token: 0x04002D24 RID: 11556
		Les,
		// Token: 0x04002D25 RID: 11557
		Lfence,
		// Token: 0x04002D26 RID: 11558
		Lfs,
		// Token: 0x04002D27 RID: 11559
		Lgdt,
		// Token: 0x04002D28 RID: 11560
		Lgs,
		// Token: 0x04002D29 RID: 11561
		Lidt,
		// Token: 0x04002D2A RID: 11562
		Lldt,
		// Token: 0x04002D2B RID: 11563
		Llwpcb,
		// Token: 0x04002D2C RID: 11564
		Lmsw,
		// Token: 0x04002D2D RID: 11565
		Loadall,
		// Token: 0x04002D2E RID: 11566
		Lodsb,
		// Token: 0x04002D2F RID: 11567
		Lodsd,
		// Token: 0x04002D30 RID: 11568
		Lodsq,
		// Token: 0x04002D31 RID: 11569
		Lodsw,
		// Token: 0x04002D32 RID: 11570
		Loop,
		// Token: 0x04002D33 RID: 11571
		Loope,
		// Token: 0x04002D34 RID: 11572
		Loopne,
		// Token: 0x04002D35 RID: 11573
		Lsl,
		// Token: 0x04002D36 RID: 11574
		Lss,
		// Token: 0x04002D37 RID: 11575
		Ltr,
		// Token: 0x04002D38 RID: 11576
		Lwpins,
		// Token: 0x04002D39 RID: 11577
		Lwpval,
		// Token: 0x04002D3A RID: 11578
		Lzcnt,
		// Token: 0x04002D3B RID: 11579
		Maskmovdqu,
		// Token: 0x04002D3C RID: 11580
		Maskmovq,
		// Token: 0x04002D3D RID: 11581
		Maxpd,
		// Token: 0x04002D3E RID: 11582
		Maxps,
		// Token: 0x04002D3F RID: 11583
		Maxsd,
		// Token: 0x04002D40 RID: 11584
		Maxss,
		// Token: 0x04002D41 RID: 11585
		Mcommit,
		// Token: 0x04002D42 RID: 11586
		Mfence,
		// Token: 0x04002D43 RID: 11587
		Minpd,
		// Token: 0x04002D44 RID: 11588
		Minps,
		// Token: 0x04002D45 RID: 11589
		Minsd,
		// Token: 0x04002D46 RID: 11590
		Minss,
		// Token: 0x04002D47 RID: 11591
		Monitor,
		// Token: 0x04002D48 RID: 11592
		Monitorx,
		// Token: 0x04002D49 RID: 11593
		Montmul,
		// Token: 0x04002D4A RID: 11594
		Mov,
		// Token: 0x04002D4B RID: 11595
		Movapd,
		// Token: 0x04002D4C RID: 11596
		Movaps,
		// Token: 0x04002D4D RID: 11597
		Movbe,
		// Token: 0x04002D4E RID: 11598
		Movd,
		// Token: 0x04002D4F RID: 11599
		Movddup,
		// Token: 0x04002D50 RID: 11600
		Movdir64b,
		// Token: 0x04002D51 RID: 11601
		Movdiri,
		// Token: 0x04002D52 RID: 11602
		Movdq2q,
		// Token: 0x04002D53 RID: 11603
		Movdqa,
		// Token: 0x04002D54 RID: 11604
		Movdqu,
		// Token: 0x04002D55 RID: 11605
		Movhlps,
		// Token: 0x04002D56 RID: 11606
		Movhpd,
		// Token: 0x04002D57 RID: 11607
		Movhps,
		// Token: 0x04002D58 RID: 11608
		Movlhps,
		// Token: 0x04002D59 RID: 11609
		Movlpd,
		// Token: 0x04002D5A RID: 11610
		Movlps,
		// Token: 0x04002D5B RID: 11611
		Movmskpd,
		// Token: 0x04002D5C RID: 11612
		Movmskps,
		// Token: 0x04002D5D RID: 11613
		Movntdq,
		// Token: 0x04002D5E RID: 11614
		Movntdqa,
		// Token: 0x04002D5F RID: 11615
		Movnti,
		// Token: 0x04002D60 RID: 11616
		Movntpd,
		// Token: 0x04002D61 RID: 11617
		Movntps,
		// Token: 0x04002D62 RID: 11618
		Movntq,
		// Token: 0x04002D63 RID: 11619
		Movntsd,
		// Token: 0x04002D64 RID: 11620
		Movntss,
		// Token: 0x04002D65 RID: 11621
		Movq,
		// Token: 0x04002D66 RID: 11622
		Movq2dq,
		// Token: 0x04002D67 RID: 11623
		Movsb,
		// Token: 0x04002D68 RID: 11624
		Movsd,
		// Token: 0x04002D69 RID: 11625
		Movshdup,
		// Token: 0x04002D6A RID: 11626
		Movsldup,
		// Token: 0x04002D6B RID: 11627
		Movsq,
		// Token: 0x04002D6C RID: 11628
		Movss,
		// Token: 0x04002D6D RID: 11629
		Movsw,
		// Token: 0x04002D6E RID: 11630
		Movsx,
		// Token: 0x04002D6F RID: 11631
		Movsxd,
		// Token: 0x04002D70 RID: 11632
		Movupd,
		// Token: 0x04002D71 RID: 11633
		Movups,
		// Token: 0x04002D72 RID: 11634
		Movzx,
		// Token: 0x04002D73 RID: 11635
		Mpsadbw,
		// Token: 0x04002D74 RID: 11636
		Mul,
		// Token: 0x04002D75 RID: 11637
		Mulpd,
		// Token: 0x04002D76 RID: 11638
		Mulps,
		// Token: 0x04002D77 RID: 11639
		Mulsd,
		// Token: 0x04002D78 RID: 11640
		Mulss,
		// Token: 0x04002D79 RID: 11641
		Mulx,
		// Token: 0x04002D7A RID: 11642
		Mwait,
		// Token: 0x04002D7B RID: 11643
		Mwaitx,
		// Token: 0x04002D7C RID: 11644
		Neg,
		// Token: 0x04002D7D RID: 11645
		Nop,
		// Token: 0x04002D7E RID: 11646
		Not,
		// Token: 0x04002D7F RID: 11647
		Or,
		// Token: 0x04002D80 RID: 11648
		Orpd,
		// Token: 0x04002D81 RID: 11649
		Orps,
		// Token: 0x04002D82 RID: 11650
		Out,
		// Token: 0x04002D83 RID: 11651
		Outsb,
		// Token: 0x04002D84 RID: 11652
		Outsd,
		// Token: 0x04002D85 RID: 11653
		Outsw,
		// Token: 0x04002D86 RID: 11654
		Pabsb,
		// Token: 0x04002D87 RID: 11655
		Pabsd,
		// Token: 0x04002D88 RID: 11656
		Pabsw,
		// Token: 0x04002D89 RID: 11657
		Packssdw,
		// Token: 0x04002D8A RID: 11658
		Packsswb,
		// Token: 0x04002D8B RID: 11659
		Packusdw,
		// Token: 0x04002D8C RID: 11660
		Packuswb,
		// Token: 0x04002D8D RID: 11661
		Paddb,
		// Token: 0x04002D8E RID: 11662
		Paddd,
		// Token: 0x04002D8F RID: 11663
		Paddq,
		// Token: 0x04002D90 RID: 11664
		Paddsb,
		// Token: 0x04002D91 RID: 11665
		Paddsw,
		// Token: 0x04002D92 RID: 11666
		Paddusb,
		// Token: 0x04002D93 RID: 11667
		Paddusw,
		// Token: 0x04002D94 RID: 11668
		Paddw,
		// Token: 0x04002D95 RID: 11669
		Palignr,
		// Token: 0x04002D96 RID: 11670
		Pand,
		// Token: 0x04002D97 RID: 11671
		Pandn,
		// Token: 0x04002D98 RID: 11672
		Pause,
		// Token: 0x04002D99 RID: 11673
		Pavgb,
		// Token: 0x04002D9A RID: 11674
		Pavgusb,
		// Token: 0x04002D9B RID: 11675
		Pavgw,
		// Token: 0x04002D9C RID: 11676
		Pblendvb,
		// Token: 0x04002D9D RID: 11677
		Pblendw,
		// Token: 0x04002D9E RID: 11678
		Pclmulqdq,
		// Token: 0x04002D9F RID: 11679
		Pcmpeqb,
		// Token: 0x04002DA0 RID: 11680
		Pcmpeqd,
		// Token: 0x04002DA1 RID: 11681
		Pcmpeqq,
		// Token: 0x04002DA2 RID: 11682
		Pcmpeqw,
		// Token: 0x04002DA3 RID: 11683
		Pcmpestri,
		// Token: 0x04002DA4 RID: 11684
		Pcmpestri64,
		// Token: 0x04002DA5 RID: 11685
		Pcmpestrm,
		// Token: 0x04002DA6 RID: 11686
		Pcmpestrm64,
		// Token: 0x04002DA7 RID: 11687
		Pcmpgtb,
		// Token: 0x04002DA8 RID: 11688
		Pcmpgtd,
		// Token: 0x04002DA9 RID: 11689
		Pcmpgtq,
		// Token: 0x04002DAA RID: 11690
		Pcmpgtw,
		// Token: 0x04002DAB RID: 11691
		Pcmpistri,
		// Token: 0x04002DAC RID: 11692
		Pcmpistrm,
		// Token: 0x04002DAD RID: 11693
		Pcommit,
		// Token: 0x04002DAE RID: 11694
		Pconfig,
		// Token: 0x04002DAF RID: 11695
		Pdep,
		// Token: 0x04002DB0 RID: 11696
		Pext,
		// Token: 0x04002DB1 RID: 11697
		Pextrb,
		// Token: 0x04002DB2 RID: 11698
		Pextrd,
		// Token: 0x04002DB3 RID: 11699
		Pextrq,
		// Token: 0x04002DB4 RID: 11700
		Pextrw,
		// Token: 0x04002DB5 RID: 11701
		Pf2id,
		// Token: 0x04002DB6 RID: 11702
		Pf2iw,
		// Token: 0x04002DB7 RID: 11703
		Pfacc,
		// Token: 0x04002DB8 RID: 11704
		Pfadd,
		// Token: 0x04002DB9 RID: 11705
		Pfcmpeq,
		// Token: 0x04002DBA RID: 11706
		Pfcmpge,
		// Token: 0x04002DBB RID: 11707
		Pfcmpgt,
		// Token: 0x04002DBC RID: 11708
		Pfmax,
		// Token: 0x04002DBD RID: 11709
		Pfmin,
		// Token: 0x04002DBE RID: 11710
		Pfmul,
		// Token: 0x04002DBF RID: 11711
		Pfnacc,
		// Token: 0x04002DC0 RID: 11712
		Pfpnacc,
		// Token: 0x04002DC1 RID: 11713
		Pfrcp,
		// Token: 0x04002DC2 RID: 11714
		Pfrcpit1,
		// Token: 0x04002DC3 RID: 11715
		Pfrcpit2,
		// Token: 0x04002DC4 RID: 11716
		Pfrcpv,
		// Token: 0x04002DC5 RID: 11717
		Pfrsqit1,
		// Token: 0x04002DC6 RID: 11718
		Pfrsqrt,
		// Token: 0x04002DC7 RID: 11719
		Pfrsqrtv,
		// Token: 0x04002DC8 RID: 11720
		Pfsub,
		// Token: 0x04002DC9 RID: 11721
		Pfsubr,
		// Token: 0x04002DCA RID: 11722
		Phaddd,
		// Token: 0x04002DCB RID: 11723
		Phaddsw,
		// Token: 0x04002DCC RID: 11724
		Phaddw,
		// Token: 0x04002DCD RID: 11725
		Phminposuw,
		// Token: 0x04002DCE RID: 11726
		Phsubd,
		// Token: 0x04002DCF RID: 11727
		Phsubsw,
		// Token: 0x04002DD0 RID: 11728
		Phsubw,
		// Token: 0x04002DD1 RID: 11729
		Pi2fd,
		// Token: 0x04002DD2 RID: 11730
		Pi2fw,
		// Token: 0x04002DD3 RID: 11731
		Pinsrb,
		// Token: 0x04002DD4 RID: 11732
		Pinsrd,
		// Token: 0x04002DD5 RID: 11733
		Pinsrq,
		// Token: 0x04002DD6 RID: 11734
		Pinsrw,
		// Token: 0x04002DD7 RID: 11735
		Pmaddubsw,
		// Token: 0x04002DD8 RID: 11736
		Pmaddwd,
		// Token: 0x04002DD9 RID: 11737
		Pmaxsb,
		// Token: 0x04002DDA RID: 11738
		Pmaxsd,
		// Token: 0x04002DDB RID: 11739
		Pmaxsw,
		// Token: 0x04002DDC RID: 11740
		Pmaxub,
		// Token: 0x04002DDD RID: 11741
		Pmaxud,
		// Token: 0x04002DDE RID: 11742
		Pmaxuw,
		// Token: 0x04002DDF RID: 11743
		Pminsb,
		// Token: 0x04002DE0 RID: 11744
		Pminsd,
		// Token: 0x04002DE1 RID: 11745
		Pminsw,
		// Token: 0x04002DE2 RID: 11746
		Pminub,
		// Token: 0x04002DE3 RID: 11747
		Pminud,
		// Token: 0x04002DE4 RID: 11748
		Pminuw,
		// Token: 0x04002DE5 RID: 11749
		Pmovmskb,
		// Token: 0x04002DE6 RID: 11750
		Pmovsxbd,
		// Token: 0x04002DE7 RID: 11751
		Pmovsxbq,
		// Token: 0x04002DE8 RID: 11752
		Pmovsxbw,
		// Token: 0x04002DE9 RID: 11753
		Pmovsxdq,
		// Token: 0x04002DEA RID: 11754
		Pmovsxwd,
		// Token: 0x04002DEB RID: 11755
		Pmovsxwq,
		// Token: 0x04002DEC RID: 11756
		Pmovzxbd,
		// Token: 0x04002DED RID: 11757
		Pmovzxbq,
		// Token: 0x04002DEE RID: 11758
		Pmovzxbw,
		// Token: 0x04002DEF RID: 11759
		Pmovzxdq,
		// Token: 0x04002DF0 RID: 11760
		Pmovzxwd,
		// Token: 0x04002DF1 RID: 11761
		Pmovzxwq,
		// Token: 0x04002DF2 RID: 11762
		Pmuldq,
		// Token: 0x04002DF3 RID: 11763
		Pmulhrsw,
		// Token: 0x04002DF4 RID: 11764
		Pmulhrw,
		// Token: 0x04002DF5 RID: 11765
		Pmulhuw,
		// Token: 0x04002DF6 RID: 11766
		Pmulhw,
		// Token: 0x04002DF7 RID: 11767
		Pmulld,
		// Token: 0x04002DF8 RID: 11768
		Pmullw,
		// Token: 0x04002DF9 RID: 11769
		Pmuludq,
		// Token: 0x04002DFA RID: 11770
		Pop,
		// Token: 0x04002DFB RID: 11771
		Popa,
		// Token: 0x04002DFC RID: 11772
		Popcnt,
		// Token: 0x04002DFD RID: 11773
		Popf,
		// Token: 0x04002DFE RID: 11774
		Por,
		// Token: 0x04002DFF RID: 11775
		Prefetch,
		// Token: 0x04002E00 RID: 11776
		Prefetchnta,
		// Token: 0x04002E01 RID: 11777
		Prefetcht0,
		// Token: 0x04002E02 RID: 11778
		Prefetcht1,
		// Token: 0x04002E03 RID: 11779
		Prefetcht2,
		// Token: 0x04002E04 RID: 11780
		Prefetchw,
		// Token: 0x04002E05 RID: 11781
		Prefetchwt1,
		// Token: 0x04002E06 RID: 11782
		Psadbw,
		// Token: 0x04002E07 RID: 11783
		Pshufb,
		// Token: 0x04002E08 RID: 11784
		Pshufd,
		// Token: 0x04002E09 RID: 11785
		Pshufhw,
		// Token: 0x04002E0A RID: 11786
		Pshuflw,
		// Token: 0x04002E0B RID: 11787
		Pshufw,
		// Token: 0x04002E0C RID: 11788
		Psignb,
		// Token: 0x04002E0D RID: 11789
		Psignd,
		// Token: 0x04002E0E RID: 11790
		Psignw,
		// Token: 0x04002E0F RID: 11791
		Pslld,
		// Token: 0x04002E10 RID: 11792
		Pslldq,
		// Token: 0x04002E11 RID: 11793
		Psllq,
		// Token: 0x04002E12 RID: 11794
		Psllw,
		// Token: 0x04002E13 RID: 11795
		Psrad,
		// Token: 0x04002E14 RID: 11796
		Psraw,
		// Token: 0x04002E15 RID: 11797
		Psrld,
		// Token: 0x04002E16 RID: 11798
		Psrldq,
		// Token: 0x04002E17 RID: 11799
		Psrlq,
		// Token: 0x04002E18 RID: 11800
		Psrlw,
		// Token: 0x04002E19 RID: 11801
		Psubb,
		// Token: 0x04002E1A RID: 11802
		Psubd,
		// Token: 0x04002E1B RID: 11803
		Psubq,
		// Token: 0x04002E1C RID: 11804
		Psubsb,
		// Token: 0x04002E1D RID: 11805
		Psubsw,
		// Token: 0x04002E1E RID: 11806
		Psubusb,
		// Token: 0x04002E1F RID: 11807
		Psubusw,
		// Token: 0x04002E20 RID: 11808
		Psubw,
		// Token: 0x04002E21 RID: 11809
		Pswapd,
		// Token: 0x04002E22 RID: 11810
		Ptest,
		// Token: 0x04002E23 RID: 11811
		Ptwrite,
		// Token: 0x04002E24 RID: 11812
		Punpckhbw,
		// Token: 0x04002E25 RID: 11813
		Punpckhdq,
		// Token: 0x04002E26 RID: 11814
		Punpckhqdq,
		// Token: 0x04002E27 RID: 11815
		Punpckhwd,
		// Token: 0x04002E28 RID: 11816
		Punpcklbw,
		// Token: 0x04002E29 RID: 11817
		Punpckldq,
		// Token: 0x04002E2A RID: 11818
		Punpcklqdq,
		// Token: 0x04002E2B RID: 11819
		Punpcklwd,
		// Token: 0x04002E2C RID: 11820
		Push,
		// Token: 0x04002E2D RID: 11821
		Pusha,
		// Token: 0x04002E2E RID: 11822
		Pushf,
		// Token: 0x04002E2F RID: 11823
		Pxor,
		// Token: 0x04002E30 RID: 11824
		Rcl,
		// Token: 0x04002E31 RID: 11825
		Rcpps,
		// Token: 0x04002E32 RID: 11826
		Rcpss,
		// Token: 0x04002E33 RID: 11827
		Rcr,
		// Token: 0x04002E34 RID: 11828
		Rdfsbase,
		// Token: 0x04002E35 RID: 11829
		Rdgsbase,
		// Token: 0x04002E36 RID: 11830
		Rdmsr,
		// Token: 0x04002E37 RID: 11831
		Rdpid,
		// Token: 0x04002E38 RID: 11832
		Rdpkru,
		// Token: 0x04002E39 RID: 11833
		Rdpmc,
		// Token: 0x04002E3A RID: 11834
		Rdpru,
		// Token: 0x04002E3B RID: 11835
		Rdrand,
		// Token: 0x04002E3C RID: 11836
		Rdseed,
		// Token: 0x04002E3D RID: 11837
		Rdsspd,
		// Token: 0x04002E3E RID: 11838
		Rdsspq,
		// Token: 0x04002E3F RID: 11839
		Rdtsc,
		// Token: 0x04002E40 RID: 11840
		Rdtscp,
		// Token: 0x04002E41 RID: 11841
		Reservednop,
		// Token: 0x04002E42 RID: 11842
		Ret,
		// Token: 0x04002E43 RID: 11843
		Retf,
		// Token: 0x04002E44 RID: 11844
		Rol,
		// Token: 0x04002E45 RID: 11845
		Ror,
		// Token: 0x04002E46 RID: 11846
		Rorx,
		// Token: 0x04002E47 RID: 11847
		Roundpd,
		// Token: 0x04002E48 RID: 11848
		Roundps,
		// Token: 0x04002E49 RID: 11849
		Roundsd,
		// Token: 0x04002E4A RID: 11850
		Roundss,
		// Token: 0x04002E4B RID: 11851
		Rsm,
		// Token: 0x04002E4C RID: 11852
		Rsqrtps,
		// Token: 0x04002E4D RID: 11853
		Rsqrtss,
		// Token: 0x04002E4E RID: 11854
		Rstorssp,
		// Token: 0x04002E4F RID: 11855
		Sahf,
		// Token: 0x04002E50 RID: 11856
		Sal,
		// Token: 0x04002E51 RID: 11857
		Salc,
		// Token: 0x04002E52 RID: 11858
		Sar,
		// Token: 0x04002E53 RID: 11859
		Sarx,
		// Token: 0x04002E54 RID: 11860
		Saveprevssp,
		// Token: 0x04002E55 RID: 11861
		Sbb,
		// Token: 0x04002E56 RID: 11862
		Scasb,
		// Token: 0x04002E57 RID: 11863
		Scasd,
		// Token: 0x04002E58 RID: 11864
		Scasq,
		// Token: 0x04002E59 RID: 11865
		Scasw,
		// Token: 0x04002E5A RID: 11866
		Seta,
		// Token: 0x04002E5B RID: 11867
		Setae,
		// Token: 0x04002E5C RID: 11868
		Setb,
		// Token: 0x04002E5D RID: 11869
		Setbe,
		// Token: 0x04002E5E RID: 11870
		Sete,
		// Token: 0x04002E5F RID: 11871
		Setg,
		// Token: 0x04002E60 RID: 11872
		Setge,
		// Token: 0x04002E61 RID: 11873
		Setl,
		// Token: 0x04002E62 RID: 11874
		Setle,
		// Token: 0x04002E63 RID: 11875
		Setne,
		// Token: 0x04002E64 RID: 11876
		Setno,
		// Token: 0x04002E65 RID: 11877
		Setnp,
		// Token: 0x04002E66 RID: 11878
		Setns,
		// Token: 0x04002E67 RID: 11879
		Seto,
		// Token: 0x04002E68 RID: 11880
		Setp,
		// Token: 0x04002E69 RID: 11881
		Sets,
		// Token: 0x04002E6A RID: 11882
		Setssbsy,
		// Token: 0x04002E6B RID: 11883
		Sfence,
		// Token: 0x04002E6C RID: 11884
		Sgdt,
		// Token: 0x04002E6D RID: 11885
		Sha1msg1,
		// Token: 0x04002E6E RID: 11886
		Sha1msg2,
		// Token: 0x04002E6F RID: 11887
		Sha1nexte,
		// Token: 0x04002E70 RID: 11888
		Sha1rnds4,
		// Token: 0x04002E71 RID: 11889
		Sha256msg1,
		// Token: 0x04002E72 RID: 11890
		Sha256msg2,
		// Token: 0x04002E73 RID: 11891
		Sha256rnds2,
		// Token: 0x04002E74 RID: 11892
		Shl,
		// Token: 0x04002E75 RID: 11893
		Shld,
		// Token: 0x04002E76 RID: 11894
		Shlx,
		// Token: 0x04002E77 RID: 11895
		Shr,
		// Token: 0x04002E78 RID: 11896
		Shrd,
		// Token: 0x04002E79 RID: 11897
		Shrx,
		// Token: 0x04002E7A RID: 11898
		Shufpd,
		// Token: 0x04002E7B RID: 11899
		Shufps,
		// Token: 0x04002E7C RID: 11900
		Sidt,
		// Token: 0x04002E7D RID: 11901
		Skinit,
		// Token: 0x04002E7E RID: 11902
		Sldt,
		// Token: 0x04002E7F RID: 11903
		Slwpcb,
		// Token: 0x04002E80 RID: 11904
		Smsw,
		// Token: 0x04002E81 RID: 11905
		Sqrtpd,
		// Token: 0x04002E82 RID: 11906
		Sqrtps,
		// Token: 0x04002E83 RID: 11907
		Sqrtsd,
		// Token: 0x04002E84 RID: 11908
		Sqrtss,
		// Token: 0x04002E85 RID: 11909
		Stac,
		// Token: 0x04002E86 RID: 11910
		Stc,
		// Token: 0x04002E87 RID: 11911
		Std,
		// Token: 0x04002E88 RID: 11912
		Stgi,
		// Token: 0x04002E89 RID: 11913
		Sti,
		// Token: 0x04002E8A RID: 11914
		Stmxcsr,
		// Token: 0x04002E8B RID: 11915
		Stosb,
		// Token: 0x04002E8C RID: 11916
		Stosd,
		// Token: 0x04002E8D RID: 11917
		Stosq,
		// Token: 0x04002E8E RID: 11918
		Stosw,
		// Token: 0x04002E8F RID: 11919
		Str,
		// Token: 0x04002E90 RID: 11920
		Sub,
		// Token: 0x04002E91 RID: 11921
		Subpd,
		// Token: 0x04002E92 RID: 11922
		Subps,
		// Token: 0x04002E93 RID: 11923
		Subsd,
		// Token: 0x04002E94 RID: 11924
		Subss,
		// Token: 0x04002E95 RID: 11925
		Swapgs,
		// Token: 0x04002E96 RID: 11926
		Syscall,
		// Token: 0x04002E97 RID: 11927
		Sysenter,
		// Token: 0x04002E98 RID: 11928
		Sysexit,
		// Token: 0x04002E99 RID: 11929
		Sysret,
		// Token: 0x04002E9A RID: 11930
		T1mskc,
		// Token: 0x04002E9B RID: 11931
		Test,
		// Token: 0x04002E9C RID: 11932
		Tpause,
		// Token: 0x04002E9D RID: 11933
		Tzcnt,
		// Token: 0x04002E9E RID: 11934
		Tzmsk,
		// Token: 0x04002E9F RID: 11935
		Ucomisd,
		// Token: 0x04002EA0 RID: 11936
		Ucomiss,
		// Token: 0x04002EA1 RID: 11937
		Ud0,
		// Token: 0x04002EA2 RID: 11938
		Ud1,
		// Token: 0x04002EA3 RID: 11939
		Ud2,
		// Token: 0x04002EA4 RID: 11940
		Umonitor,
		// Token: 0x04002EA5 RID: 11941
		Umov,
		// Token: 0x04002EA6 RID: 11942
		Umwait,
		// Token: 0x04002EA7 RID: 11943
		Unpckhpd,
		// Token: 0x04002EA8 RID: 11944
		Unpckhps,
		// Token: 0x04002EA9 RID: 11945
		Unpcklpd,
		// Token: 0x04002EAA RID: 11946
		Unpcklps,
		// Token: 0x04002EAB RID: 11947
		V4fmaddps,
		// Token: 0x04002EAC RID: 11948
		V4fmaddss,
		// Token: 0x04002EAD RID: 11949
		V4fnmaddps,
		// Token: 0x04002EAE RID: 11950
		V4fnmaddss,
		// Token: 0x04002EAF RID: 11951
		Vaddpd,
		// Token: 0x04002EB0 RID: 11952
		Vaddps,
		// Token: 0x04002EB1 RID: 11953
		Vaddsd,
		// Token: 0x04002EB2 RID: 11954
		Vaddss,
		// Token: 0x04002EB3 RID: 11955
		Vaddsubpd,
		// Token: 0x04002EB4 RID: 11956
		Vaddsubps,
		// Token: 0x04002EB5 RID: 11957
		Vaesdec,
		// Token: 0x04002EB6 RID: 11958
		Vaesdeclast,
		// Token: 0x04002EB7 RID: 11959
		Vaesenc,
		// Token: 0x04002EB8 RID: 11960
		Vaesenclast,
		// Token: 0x04002EB9 RID: 11961
		Vaesimc,
		// Token: 0x04002EBA RID: 11962
		Vaeskeygenassist,
		// Token: 0x04002EBB RID: 11963
		Valignd,
		// Token: 0x04002EBC RID: 11964
		Valignq,
		// Token: 0x04002EBD RID: 11965
		Vandnpd,
		// Token: 0x04002EBE RID: 11966
		Vandnps,
		// Token: 0x04002EBF RID: 11967
		Vandpd,
		// Token: 0x04002EC0 RID: 11968
		Vandps,
		// Token: 0x04002EC1 RID: 11969
		Vblendmpd,
		// Token: 0x04002EC2 RID: 11970
		Vblendmps,
		// Token: 0x04002EC3 RID: 11971
		Vblendpd,
		// Token: 0x04002EC4 RID: 11972
		Vblendps,
		// Token: 0x04002EC5 RID: 11973
		Vblendvpd,
		// Token: 0x04002EC6 RID: 11974
		Vblendvps,
		// Token: 0x04002EC7 RID: 11975
		Vbroadcastf128,
		// Token: 0x04002EC8 RID: 11976
		Vbroadcastf32x2,
		// Token: 0x04002EC9 RID: 11977
		Vbroadcastf32x4,
		// Token: 0x04002ECA RID: 11978
		Vbroadcastf32x8,
		// Token: 0x04002ECB RID: 11979
		Vbroadcastf64x2,
		// Token: 0x04002ECC RID: 11980
		Vbroadcastf64x4,
		// Token: 0x04002ECD RID: 11981
		Vbroadcasti128,
		// Token: 0x04002ECE RID: 11982
		Vbroadcasti32x2,
		// Token: 0x04002ECF RID: 11983
		Vbroadcasti32x4,
		// Token: 0x04002ED0 RID: 11984
		Vbroadcasti32x8,
		// Token: 0x04002ED1 RID: 11985
		Vbroadcasti64x2,
		// Token: 0x04002ED2 RID: 11986
		Vbroadcasti64x4,
		// Token: 0x04002ED3 RID: 11987
		Vbroadcastsd,
		// Token: 0x04002ED4 RID: 11988
		Vbroadcastss,
		// Token: 0x04002ED5 RID: 11989
		Vcmppd,
		// Token: 0x04002ED6 RID: 11990
		Vcmpps,
		// Token: 0x04002ED7 RID: 11991
		Vcmpsd,
		// Token: 0x04002ED8 RID: 11992
		Vcmpss,
		// Token: 0x04002ED9 RID: 11993
		Vcomisd,
		// Token: 0x04002EDA RID: 11994
		Vcomiss,
		// Token: 0x04002EDB RID: 11995
		Vcompresspd,
		// Token: 0x04002EDC RID: 11996
		Vcompressps,
		// Token: 0x04002EDD RID: 11997
		Vcvtdq2pd,
		// Token: 0x04002EDE RID: 11998
		Vcvtdq2ps,
		// Token: 0x04002EDF RID: 11999
		Vcvtne2ps2bf16,
		// Token: 0x04002EE0 RID: 12000
		Vcvtneps2bf16,
		// Token: 0x04002EE1 RID: 12001
		Vcvtpd2dq,
		// Token: 0x04002EE2 RID: 12002
		Vcvtpd2ps,
		// Token: 0x04002EE3 RID: 12003
		Vcvtpd2qq,
		// Token: 0x04002EE4 RID: 12004
		Vcvtpd2udq,
		// Token: 0x04002EE5 RID: 12005
		Vcvtpd2uqq,
		// Token: 0x04002EE6 RID: 12006
		Vcvtph2ps,
		// Token: 0x04002EE7 RID: 12007
		Vcvtps2dq,
		// Token: 0x04002EE8 RID: 12008
		Vcvtps2pd,
		// Token: 0x04002EE9 RID: 12009
		Vcvtps2ph,
		// Token: 0x04002EEA RID: 12010
		Vcvtps2qq,
		// Token: 0x04002EEB RID: 12011
		Vcvtps2udq,
		// Token: 0x04002EEC RID: 12012
		Vcvtps2uqq,
		// Token: 0x04002EED RID: 12013
		Vcvtqq2pd,
		// Token: 0x04002EEE RID: 12014
		Vcvtqq2ps,
		// Token: 0x04002EEF RID: 12015
		Vcvtsd2si,
		// Token: 0x04002EF0 RID: 12016
		Vcvtsd2ss,
		// Token: 0x04002EF1 RID: 12017
		Vcvtsd2usi,
		// Token: 0x04002EF2 RID: 12018
		Vcvtsi2sd,
		// Token: 0x04002EF3 RID: 12019
		Vcvtsi2ss,
		// Token: 0x04002EF4 RID: 12020
		Vcvtss2sd,
		// Token: 0x04002EF5 RID: 12021
		Vcvtss2si,
		// Token: 0x04002EF6 RID: 12022
		Vcvtss2usi,
		// Token: 0x04002EF7 RID: 12023
		Vcvttpd2dq,
		// Token: 0x04002EF8 RID: 12024
		Vcvttpd2qq,
		// Token: 0x04002EF9 RID: 12025
		Vcvttpd2udq,
		// Token: 0x04002EFA RID: 12026
		Vcvttpd2uqq,
		// Token: 0x04002EFB RID: 12027
		Vcvttps2dq,
		// Token: 0x04002EFC RID: 12028
		Vcvttps2qq,
		// Token: 0x04002EFD RID: 12029
		Vcvttps2udq,
		// Token: 0x04002EFE RID: 12030
		Vcvttps2uqq,
		// Token: 0x04002EFF RID: 12031
		Vcvttsd2si,
		// Token: 0x04002F00 RID: 12032
		Vcvttsd2usi,
		// Token: 0x04002F01 RID: 12033
		Vcvttss2si,
		// Token: 0x04002F02 RID: 12034
		Vcvttss2usi,
		// Token: 0x04002F03 RID: 12035
		Vcvtudq2pd,
		// Token: 0x04002F04 RID: 12036
		Vcvtudq2ps,
		// Token: 0x04002F05 RID: 12037
		Vcvtuqq2pd,
		// Token: 0x04002F06 RID: 12038
		Vcvtuqq2ps,
		// Token: 0x04002F07 RID: 12039
		Vcvtusi2sd,
		// Token: 0x04002F08 RID: 12040
		Vcvtusi2ss,
		// Token: 0x04002F09 RID: 12041
		Vdbpsadbw,
		// Token: 0x04002F0A RID: 12042
		Vdivpd,
		// Token: 0x04002F0B RID: 12043
		Vdivps,
		// Token: 0x04002F0C RID: 12044
		Vdivsd,
		// Token: 0x04002F0D RID: 12045
		Vdivss,
		// Token: 0x04002F0E RID: 12046
		Vdpbf16ps,
		// Token: 0x04002F0F RID: 12047
		Vdppd,
		// Token: 0x04002F10 RID: 12048
		Vdpps,
		// Token: 0x04002F11 RID: 12049
		Verr,
		// Token: 0x04002F12 RID: 12050
		Verw,
		// Token: 0x04002F13 RID: 12051
		Vexp2pd,
		// Token: 0x04002F14 RID: 12052
		Vexp2ps,
		// Token: 0x04002F15 RID: 12053
		Vexpandpd,
		// Token: 0x04002F16 RID: 12054
		Vexpandps,
		// Token: 0x04002F17 RID: 12055
		Vextractf128,
		// Token: 0x04002F18 RID: 12056
		Vextractf32x4,
		// Token: 0x04002F19 RID: 12057
		Vextractf32x8,
		// Token: 0x04002F1A RID: 12058
		Vextractf64x2,
		// Token: 0x04002F1B RID: 12059
		Vextractf64x4,
		// Token: 0x04002F1C RID: 12060
		Vextracti128,
		// Token: 0x04002F1D RID: 12061
		Vextracti32x4,
		// Token: 0x04002F1E RID: 12062
		Vextracti32x8,
		// Token: 0x04002F1F RID: 12063
		Vextracti64x2,
		// Token: 0x04002F20 RID: 12064
		Vextracti64x4,
		// Token: 0x04002F21 RID: 12065
		Vextractps,
		// Token: 0x04002F22 RID: 12066
		Vfixupimmpd,
		// Token: 0x04002F23 RID: 12067
		Vfixupimmps,
		// Token: 0x04002F24 RID: 12068
		Vfixupimmsd,
		// Token: 0x04002F25 RID: 12069
		Vfixupimmss,
		// Token: 0x04002F26 RID: 12070
		Vfmadd132pd,
		// Token: 0x04002F27 RID: 12071
		Vfmadd132ps,
		// Token: 0x04002F28 RID: 12072
		Vfmadd132sd,
		// Token: 0x04002F29 RID: 12073
		Vfmadd132ss,
		// Token: 0x04002F2A RID: 12074
		Vfmadd213pd,
		// Token: 0x04002F2B RID: 12075
		Vfmadd213ps,
		// Token: 0x04002F2C RID: 12076
		Vfmadd213sd,
		// Token: 0x04002F2D RID: 12077
		Vfmadd213ss,
		// Token: 0x04002F2E RID: 12078
		Vfmadd231pd,
		// Token: 0x04002F2F RID: 12079
		Vfmadd231ps,
		// Token: 0x04002F30 RID: 12080
		Vfmadd231sd,
		// Token: 0x04002F31 RID: 12081
		Vfmadd231ss,
		// Token: 0x04002F32 RID: 12082
		Vfmaddpd,
		// Token: 0x04002F33 RID: 12083
		Vfmaddps,
		// Token: 0x04002F34 RID: 12084
		Vfmaddsd,
		// Token: 0x04002F35 RID: 12085
		Vfmaddss,
		// Token: 0x04002F36 RID: 12086
		Vfmaddsub132pd,
		// Token: 0x04002F37 RID: 12087
		Vfmaddsub132ps,
		// Token: 0x04002F38 RID: 12088
		Vfmaddsub213pd,
		// Token: 0x04002F39 RID: 12089
		Vfmaddsub213ps,
		// Token: 0x04002F3A RID: 12090
		Vfmaddsub231pd,
		// Token: 0x04002F3B RID: 12091
		Vfmaddsub231ps,
		// Token: 0x04002F3C RID: 12092
		Vfmaddsubpd,
		// Token: 0x04002F3D RID: 12093
		Vfmaddsubps,
		// Token: 0x04002F3E RID: 12094
		Vfmsub132pd,
		// Token: 0x04002F3F RID: 12095
		Vfmsub132ps,
		// Token: 0x04002F40 RID: 12096
		Vfmsub132sd,
		// Token: 0x04002F41 RID: 12097
		Vfmsub132ss,
		// Token: 0x04002F42 RID: 12098
		Vfmsub213pd,
		// Token: 0x04002F43 RID: 12099
		Vfmsub213ps,
		// Token: 0x04002F44 RID: 12100
		Vfmsub213sd,
		// Token: 0x04002F45 RID: 12101
		Vfmsub213ss,
		// Token: 0x04002F46 RID: 12102
		Vfmsub231pd,
		// Token: 0x04002F47 RID: 12103
		Vfmsub231ps,
		// Token: 0x04002F48 RID: 12104
		Vfmsub231sd,
		// Token: 0x04002F49 RID: 12105
		Vfmsub231ss,
		// Token: 0x04002F4A RID: 12106
		Vfmsubadd132pd,
		// Token: 0x04002F4B RID: 12107
		Vfmsubadd132ps,
		// Token: 0x04002F4C RID: 12108
		Vfmsubadd213pd,
		// Token: 0x04002F4D RID: 12109
		Vfmsubadd213ps,
		// Token: 0x04002F4E RID: 12110
		Vfmsubadd231pd,
		// Token: 0x04002F4F RID: 12111
		Vfmsubadd231ps,
		// Token: 0x04002F50 RID: 12112
		Vfmsubaddpd,
		// Token: 0x04002F51 RID: 12113
		Vfmsubaddps,
		// Token: 0x04002F52 RID: 12114
		Vfmsubpd,
		// Token: 0x04002F53 RID: 12115
		Vfmsubps,
		// Token: 0x04002F54 RID: 12116
		Vfmsubsd,
		// Token: 0x04002F55 RID: 12117
		Vfmsubss,
		// Token: 0x04002F56 RID: 12118
		Vfnmadd132pd,
		// Token: 0x04002F57 RID: 12119
		Vfnmadd132ps,
		// Token: 0x04002F58 RID: 12120
		Vfnmadd132sd,
		// Token: 0x04002F59 RID: 12121
		Vfnmadd132ss,
		// Token: 0x04002F5A RID: 12122
		Vfnmadd213pd,
		// Token: 0x04002F5B RID: 12123
		Vfnmadd213ps,
		// Token: 0x04002F5C RID: 12124
		Vfnmadd213sd,
		// Token: 0x04002F5D RID: 12125
		Vfnmadd213ss,
		// Token: 0x04002F5E RID: 12126
		Vfnmadd231pd,
		// Token: 0x04002F5F RID: 12127
		Vfnmadd231ps,
		// Token: 0x04002F60 RID: 12128
		Vfnmadd231sd,
		// Token: 0x04002F61 RID: 12129
		Vfnmadd231ss,
		// Token: 0x04002F62 RID: 12130
		Vfnmaddpd,
		// Token: 0x04002F63 RID: 12131
		Vfnmaddps,
		// Token: 0x04002F64 RID: 12132
		Vfnmaddsd,
		// Token: 0x04002F65 RID: 12133
		Vfnmaddss,
		// Token: 0x04002F66 RID: 12134
		Vfnmsub132pd,
		// Token: 0x04002F67 RID: 12135
		Vfnmsub132ps,
		// Token: 0x04002F68 RID: 12136
		Vfnmsub132sd,
		// Token: 0x04002F69 RID: 12137
		Vfnmsub132ss,
		// Token: 0x04002F6A RID: 12138
		Vfnmsub213pd,
		// Token: 0x04002F6B RID: 12139
		Vfnmsub213ps,
		// Token: 0x04002F6C RID: 12140
		Vfnmsub213sd,
		// Token: 0x04002F6D RID: 12141
		Vfnmsub213ss,
		// Token: 0x04002F6E RID: 12142
		Vfnmsub231pd,
		// Token: 0x04002F6F RID: 12143
		Vfnmsub231ps,
		// Token: 0x04002F70 RID: 12144
		Vfnmsub231sd,
		// Token: 0x04002F71 RID: 12145
		Vfnmsub231ss,
		// Token: 0x04002F72 RID: 12146
		Vfnmsubpd,
		// Token: 0x04002F73 RID: 12147
		Vfnmsubps,
		// Token: 0x04002F74 RID: 12148
		Vfnmsubsd,
		// Token: 0x04002F75 RID: 12149
		Vfnmsubss,
		// Token: 0x04002F76 RID: 12150
		Vfpclasspd,
		// Token: 0x04002F77 RID: 12151
		Vfpclassps,
		// Token: 0x04002F78 RID: 12152
		Vfpclasssd,
		// Token: 0x04002F79 RID: 12153
		Vfpclassss,
		// Token: 0x04002F7A RID: 12154
		Vfrczpd,
		// Token: 0x04002F7B RID: 12155
		Vfrczps,
		// Token: 0x04002F7C RID: 12156
		Vfrczsd,
		// Token: 0x04002F7D RID: 12157
		Vfrczss,
		// Token: 0x04002F7E RID: 12158
		Vgatherdpd,
		// Token: 0x04002F7F RID: 12159
		Vgatherdps,
		// Token: 0x04002F80 RID: 12160
		Vgatherpf0dpd,
		// Token: 0x04002F81 RID: 12161
		Vgatherpf0dps,
		// Token: 0x04002F82 RID: 12162
		Vgatherpf0qpd,
		// Token: 0x04002F83 RID: 12163
		Vgatherpf0qps,
		// Token: 0x04002F84 RID: 12164
		Vgatherpf1dpd,
		// Token: 0x04002F85 RID: 12165
		Vgatherpf1dps,
		// Token: 0x04002F86 RID: 12166
		Vgatherpf1qpd,
		// Token: 0x04002F87 RID: 12167
		Vgatherpf1qps,
		// Token: 0x04002F88 RID: 12168
		Vgatherqpd,
		// Token: 0x04002F89 RID: 12169
		Vgatherqps,
		// Token: 0x04002F8A RID: 12170
		Vgetexppd,
		// Token: 0x04002F8B RID: 12171
		Vgetexpps,
		// Token: 0x04002F8C RID: 12172
		Vgetexpsd,
		// Token: 0x04002F8D RID: 12173
		Vgetexpss,
		// Token: 0x04002F8E RID: 12174
		Vgetmantpd,
		// Token: 0x04002F8F RID: 12175
		Vgetmantps,
		// Token: 0x04002F90 RID: 12176
		Vgetmantsd,
		// Token: 0x04002F91 RID: 12177
		Vgetmantss,
		// Token: 0x04002F92 RID: 12178
		Vgf2p8affineinvqb,
		// Token: 0x04002F93 RID: 12179
		Vgf2p8affineqb,
		// Token: 0x04002F94 RID: 12180
		Vgf2p8mulb,
		// Token: 0x04002F95 RID: 12181
		Vhaddpd,
		// Token: 0x04002F96 RID: 12182
		Vhaddps,
		// Token: 0x04002F97 RID: 12183
		Vhsubpd,
		// Token: 0x04002F98 RID: 12184
		Vhsubps,
		// Token: 0x04002F99 RID: 12185
		Vinsertf128,
		// Token: 0x04002F9A RID: 12186
		Vinsertf32x4,
		// Token: 0x04002F9B RID: 12187
		Vinsertf32x8,
		// Token: 0x04002F9C RID: 12188
		Vinsertf64x2,
		// Token: 0x04002F9D RID: 12189
		Vinsertf64x4,
		// Token: 0x04002F9E RID: 12190
		Vinserti128,
		// Token: 0x04002F9F RID: 12191
		Vinserti32x4,
		// Token: 0x04002FA0 RID: 12192
		Vinserti32x8,
		// Token: 0x04002FA1 RID: 12193
		Vinserti64x2,
		// Token: 0x04002FA2 RID: 12194
		Vinserti64x4,
		// Token: 0x04002FA3 RID: 12195
		Vinsertps,
		// Token: 0x04002FA4 RID: 12196
		Vlddqu,
		// Token: 0x04002FA5 RID: 12197
		Vldmxcsr,
		// Token: 0x04002FA6 RID: 12198
		Vmaskmovdqu,
		// Token: 0x04002FA7 RID: 12199
		Vmaskmovpd,
		// Token: 0x04002FA8 RID: 12200
		Vmaskmovps,
		// Token: 0x04002FA9 RID: 12201
		Vmaxpd,
		// Token: 0x04002FAA RID: 12202
		Vmaxps,
		// Token: 0x04002FAB RID: 12203
		Vmaxsd,
		// Token: 0x04002FAC RID: 12204
		Vmaxss,
		// Token: 0x04002FAD RID: 12205
		Vmcall,
		// Token: 0x04002FAE RID: 12206
		Vmclear,
		// Token: 0x04002FAF RID: 12207
		Vmfunc,
		// Token: 0x04002FB0 RID: 12208
		Vminpd,
		// Token: 0x04002FB1 RID: 12209
		Vminps,
		// Token: 0x04002FB2 RID: 12210
		Vminsd,
		// Token: 0x04002FB3 RID: 12211
		Vminss,
		// Token: 0x04002FB4 RID: 12212
		Vmlaunch,
		// Token: 0x04002FB5 RID: 12213
		Vmload,
		// Token: 0x04002FB6 RID: 12214
		Vmmcall,
		// Token: 0x04002FB7 RID: 12215
		Vmovapd,
		// Token: 0x04002FB8 RID: 12216
		Vmovaps,
		// Token: 0x04002FB9 RID: 12217
		Vmovd,
		// Token: 0x04002FBA RID: 12218
		Vmovddup,
		// Token: 0x04002FBB RID: 12219
		Vmovdqa,
		// Token: 0x04002FBC RID: 12220
		Vmovdqa32,
		// Token: 0x04002FBD RID: 12221
		Vmovdqa64,
		// Token: 0x04002FBE RID: 12222
		Vmovdqu,
		// Token: 0x04002FBF RID: 12223
		Vmovdqu16,
		// Token: 0x04002FC0 RID: 12224
		Vmovdqu32,
		// Token: 0x04002FC1 RID: 12225
		Vmovdqu64,
		// Token: 0x04002FC2 RID: 12226
		Vmovdqu8,
		// Token: 0x04002FC3 RID: 12227
		Vmovhlps,
		// Token: 0x04002FC4 RID: 12228
		Vmovhpd,
		// Token: 0x04002FC5 RID: 12229
		Vmovhps,
		// Token: 0x04002FC6 RID: 12230
		Vmovlhps,
		// Token: 0x04002FC7 RID: 12231
		Vmovlpd,
		// Token: 0x04002FC8 RID: 12232
		Vmovlps,
		// Token: 0x04002FC9 RID: 12233
		Vmovmskpd,
		// Token: 0x04002FCA RID: 12234
		Vmovmskps,
		// Token: 0x04002FCB RID: 12235
		Vmovntdq,
		// Token: 0x04002FCC RID: 12236
		Vmovntdqa,
		// Token: 0x04002FCD RID: 12237
		Vmovntpd,
		// Token: 0x04002FCE RID: 12238
		Vmovntps,
		// Token: 0x04002FCF RID: 12239
		Vmovq,
		// Token: 0x04002FD0 RID: 12240
		Vmovsd,
		// Token: 0x04002FD1 RID: 12241
		Vmovshdup,
		// Token: 0x04002FD2 RID: 12242
		Vmovsldup,
		// Token: 0x04002FD3 RID: 12243
		Vmovss,
		// Token: 0x04002FD4 RID: 12244
		Vmovupd,
		// Token: 0x04002FD5 RID: 12245
		Vmovups,
		// Token: 0x04002FD6 RID: 12246
		Vmpsadbw,
		// Token: 0x04002FD7 RID: 12247
		Vmptrld,
		// Token: 0x04002FD8 RID: 12248
		Vmptrst,
		// Token: 0x04002FD9 RID: 12249
		Vmread,
		// Token: 0x04002FDA RID: 12250
		Vmresume,
		// Token: 0x04002FDB RID: 12251
		Vmrun,
		// Token: 0x04002FDC RID: 12252
		Vmsave,
		// Token: 0x04002FDD RID: 12253
		Vmulpd,
		// Token: 0x04002FDE RID: 12254
		Vmulps,
		// Token: 0x04002FDF RID: 12255
		Vmulsd,
		// Token: 0x04002FE0 RID: 12256
		Vmulss,
		// Token: 0x04002FE1 RID: 12257
		Vmwrite,
		// Token: 0x04002FE2 RID: 12258
		Vmxoff,
		// Token: 0x04002FE3 RID: 12259
		Vmxon,
		// Token: 0x04002FE4 RID: 12260
		Vorpd,
		// Token: 0x04002FE5 RID: 12261
		Vorps,
		// Token: 0x04002FE6 RID: 12262
		Vp2intersectd,
		// Token: 0x04002FE7 RID: 12263
		Vp2intersectq,
		// Token: 0x04002FE8 RID: 12264
		Vp4dpwssd,
		// Token: 0x04002FE9 RID: 12265
		Vp4dpwssds,
		// Token: 0x04002FEA RID: 12266
		Vpabsb,
		// Token: 0x04002FEB RID: 12267
		Vpabsd,
		// Token: 0x04002FEC RID: 12268
		Vpabsq,
		// Token: 0x04002FED RID: 12269
		Vpabsw,
		// Token: 0x04002FEE RID: 12270
		Vpackssdw,
		// Token: 0x04002FEF RID: 12271
		Vpacksswb,
		// Token: 0x04002FF0 RID: 12272
		Vpackusdw,
		// Token: 0x04002FF1 RID: 12273
		Vpackuswb,
		// Token: 0x04002FF2 RID: 12274
		Vpaddb,
		// Token: 0x04002FF3 RID: 12275
		Vpaddd,
		// Token: 0x04002FF4 RID: 12276
		Vpaddq,
		// Token: 0x04002FF5 RID: 12277
		Vpaddsb,
		// Token: 0x04002FF6 RID: 12278
		Vpaddsw,
		// Token: 0x04002FF7 RID: 12279
		Vpaddusb,
		// Token: 0x04002FF8 RID: 12280
		Vpaddusw,
		// Token: 0x04002FF9 RID: 12281
		Vpaddw,
		// Token: 0x04002FFA RID: 12282
		Vpalignr,
		// Token: 0x04002FFB RID: 12283
		Vpand,
		// Token: 0x04002FFC RID: 12284
		Vpandd,
		// Token: 0x04002FFD RID: 12285
		Vpandn,
		// Token: 0x04002FFE RID: 12286
		Vpandnd,
		// Token: 0x04002FFF RID: 12287
		Vpandnq,
		// Token: 0x04003000 RID: 12288
		Vpandq,
		// Token: 0x04003001 RID: 12289
		Vpavgb,
		// Token: 0x04003002 RID: 12290
		Vpavgw,
		// Token: 0x04003003 RID: 12291
		Vpblendd,
		// Token: 0x04003004 RID: 12292
		Vpblendmb,
		// Token: 0x04003005 RID: 12293
		Vpblendmd,
		// Token: 0x04003006 RID: 12294
		Vpblendmq,
		// Token: 0x04003007 RID: 12295
		Vpblendmw,
		// Token: 0x04003008 RID: 12296
		Vpblendvb,
		// Token: 0x04003009 RID: 12297
		Vpblendw,
		// Token: 0x0400300A RID: 12298
		Vpbroadcastb,
		// Token: 0x0400300B RID: 12299
		Vpbroadcastd,
		// Token: 0x0400300C RID: 12300
		Vpbroadcastmb2q,
		// Token: 0x0400300D RID: 12301
		Vpbroadcastmw2d,
		// Token: 0x0400300E RID: 12302
		Vpbroadcastq,
		// Token: 0x0400300F RID: 12303
		Vpbroadcastw,
		// Token: 0x04003010 RID: 12304
		Vpclmulqdq,
		// Token: 0x04003011 RID: 12305
		Vpcmov,
		// Token: 0x04003012 RID: 12306
		Vpcmpb,
		// Token: 0x04003013 RID: 12307
		Vpcmpd,
		// Token: 0x04003014 RID: 12308
		Vpcmpeqb,
		// Token: 0x04003015 RID: 12309
		Vpcmpeqd,
		// Token: 0x04003016 RID: 12310
		Vpcmpeqq,
		// Token: 0x04003017 RID: 12311
		Vpcmpeqw,
		// Token: 0x04003018 RID: 12312
		Vpcmpestri,
		// Token: 0x04003019 RID: 12313
		Vpcmpestri64,
		// Token: 0x0400301A RID: 12314
		Vpcmpestrm,
		// Token: 0x0400301B RID: 12315
		Vpcmpestrm64,
		// Token: 0x0400301C RID: 12316
		Vpcmpgtb,
		// Token: 0x0400301D RID: 12317
		Vpcmpgtd,
		// Token: 0x0400301E RID: 12318
		Vpcmpgtq,
		// Token: 0x0400301F RID: 12319
		Vpcmpgtw,
		// Token: 0x04003020 RID: 12320
		Vpcmpistri,
		// Token: 0x04003021 RID: 12321
		Vpcmpistrm,
		// Token: 0x04003022 RID: 12322
		Vpcmpq,
		// Token: 0x04003023 RID: 12323
		Vpcmpub,
		// Token: 0x04003024 RID: 12324
		Vpcmpud,
		// Token: 0x04003025 RID: 12325
		Vpcmpuq,
		// Token: 0x04003026 RID: 12326
		Vpcmpuw,
		// Token: 0x04003027 RID: 12327
		Vpcmpw,
		// Token: 0x04003028 RID: 12328
		Vpcomb,
		// Token: 0x04003029 RID: 12329
		Vpcomd,
		// Token: 0x0400302A RID: 12330
		Vpcompressb,
		// Token: 0x0400302B RID: 12331
		Vpcompressd,
		// Token: 0x0400302C RID: 12332
		Vpcompressq,
		// Token: 0x0400302D RID: 12333
		Vpcompressw,
		// Token: 0x0400302E RID: 12334
		Vpcomq,
		// Token: 0x0400302F RID: 12335
		Vpcomub,
		// Token: 0x04003030 RID: 12336
		Vpcomud,
		// Token: 0x04003031 RID: 12337
		Vpcomuq,
		// Token: 0x04003032 RID: 12338
		Vpcomuw,
		// Token: 0x04003033 RID: 12339
		Vpcomw,
		// Token: 0x04003034 RID: 12340
		Vpconflictd,
		// Token: 0x04003035 RID: 12341
		Vpconflictq,
		// Token: 0x04003036 RID: 12342
		Vpdpbusd,
		// Token: 0x04003037 RID: 12343
		Vpdpbusds,
		// Token: 0x04003038 RID: 12344
		Vpdpwssd,
		// Token: 0x04003039 RID: 12345
		Vpdpwssds,
		// Token: 0x0400303A RID: 12346
		Vperm2f128,
		// Token: 0x0400303B RID: 12347
		Vperm2i128,
		// Token: 0x0400303C RID: 12348
		Vpermb,
		// Token: 0x0400303D RID: 12349
		Vpermd,
		// Token: 0x0400303E RID: 12350
		Vpermi2b,
		// Token: 0x0400303F RID: 12351
		Vpermi2d,
		// Token: 0x04003040 RID: 12352
		Vpermi2pd,
		// Token: 0x04003041 RID: 12353
		Vpermi2ps,
		// Token: 0x04003042 RID: 12354
		Vpermi2q,
		// Token: 0x04003043 RID: 12355
		Vpermi2w,
		// Token: 0x04003044 RID: 12356
		Vpermil2pd,
		// Token: 0x04003045 RID: 12357
		Vpermil2ps,
		// Token: 0x04003046 RID: 12358
		Vpermilpd,
		// Token: 0x04003047 RID: 12359
		Vpermilps,
		// Token: 0x04003048 RID: 12360
		Vpermpd,
		// Token: 0x04003049 RID: 12361
		Vpermps,
		// Token: 0x0400304A RID: 12362
		Vpermq,
		// Token: 0x0400304B RID: 12363
		Vpermt2b,
		// Token: 0x0400304C RID: 12364
		Vpermt2d,
		// Token: 0x0400304D RID: 12365
		Vpermt2pd,
		// Token: 0x0400304E RID: 12366
		Vpermt2ps,
		// Token: 0x0400304F RID: 12367
		Vpermt2q,
		// Token: 0x04003050 RID: 12368
		Vpermt2w,
		// Token: 0x04003051 RID: 12369
		Vpermw,
		// Token: 0x04003052 RID: 12370
		Vpexpandb,
		// Token: 0x04003053 RID: 12371
		Vpexpandd,
		// Token: 0x04003054 RID: 12372
		Vpexpandq,
		// Token: 0x04003055 RID: 12373
		Vpexpandw,
		// Token: 0x04003056 RID: 12374
		Vpextrb,
		// Token: 0x04003057 RID: 12375
		Vpextrd,
		// Token: 0x04003058 RID: 12376
		Vpextrq,
		// Token: 0x04003059 RID: 12377
		Vpextrw,
		// Token: 0x0400305A RID: 12378
		Vpgatherdd,
		// Token: 0x0400305B RID: 12379
		Vpgatherdq,
		// Token: 0x0400305C RID: 12380
		Vpgatherqd,
		// Token: 0x0400305D RID: 12381
		Vpgatherqq,
		// Token: 0x0400305E RID: 12382
		Vphaddbd,
		// Token: 0x0400305F RID: 12383
		Vphaddbq,
		// Token: 0x04003060 RID: 12384
		Vphaddbw,
		// Token: 0x04003061 RID: 12385
		Vphaddd,
		// Token: 0x04003062 RID: 12386
		Vphadddq,
		// Token: 0x04003063 RID: 12387
		Vphaddsw,
		// Token: 0x04003064 RID: 12388
		Vphaddubd,
		// Token: 0x04003065 RID: 12389
		Vphaddubq,
		// Token: 0x04003066 RID: 12390
		Vphaddubw,
		// Token: 0x04003067 RID: 12391
		Vphaddudq,
		// Token: 0x04003068 RID: 12392
		Vphadduwd,
		// Token: 0x04003069 RID: 12393
		Vphadduwq,
		// Token: 0x0400306A RID: 12394
		Vphaddw,
		// Token: 0x0400306B RID: 12395
		Vphaddwd,
		// Token: 0x0400306C RID: 12396
		Vphaddwq,
		// Token: 0x0400306D RID: 12397
		Vphminposuw,
		// Token: 0x0400306E RID: 12398
		Vphsubbw,
		// Token: 0x0400306F RID: 12399
		Vphsubd,
		// Token: 0x04003070 RID: 12400
		Vphsubdq,
		// Token: 0x04003071 RID: 12401
		Vphsubsw,
		// Token: 0x04003072 RID: 12402
		Vphsubw,
		// Token: 0x04003073 RID: 12403
		Vphsubwd,
		// Token: 0x04003074 RID: 12404
		Vpinsrb,
		// Token: 0x04003075 RID: 12405
		Vpinsrd,
		// Token: 0x04003076 RID: 12406
		Vpinsrq,
		// Token: 0x04003077 RID: 12407
		Vpinsrw,
		// Token: 0x04003078 RID: 12408
		Vplzcntd,
		// Token: 0x04003079 RID: 12409
		Vplzcntq,
		// Token: 0x0400307A RID: 12410
		Vpmacsdd,
		// Token: 0x0400307B RID: 12411
		Vpmacsdqh,
		// Token: 0x0400307C RID: 12412
		Vpmacsdql,
		// Token: 0x0400307D RID: 12413
		Vpmacssdd,
		// Token: 0x0400307E RID: 12414
		Vpmacssdqh,
		// Token: 0x0400307F RID: 12415
		Vpmacssdql,
		// Token: 0x04003080 RID: 12416
		Vpmacsswd,
		// Token: 0x04003081 RID: 12417
		Vpmacssww,
		// Token: 0x04003082 RID: 12418
		Vpmacswd,
		// Token: 0x04003083 RID: 12419
		Vpmacsww,
		// Token: 0x04003084 RID: 12420
		Vpmadcsswd,
		// Token: 0x04003085 RID: 12421
		Vpmadcswd,
		// Token: 0x04003086 RID: 12422
		Vpmadd52huq,
		// Token: 0x04003087 RID: 12423
		Vpmadd52luq,
		// Token: 0x04003088 RID: 12424
		Vpmaddubsw,
		// Token: 0x04003089 RID: 12425
		Vpmaddwd,
		// Token: 0x0400308A RID: 12426
		Vpmaskmovd,
		// Token: 0x0400308B RID: 12427
		Vpmaskmovq,
		// Token: 0x0400308C RID: 12428
		Vpmaxsb,
		// Token: 0x0400308D RID: 12429
		Vpmaxsd,
		// Token: 0x0400308E RID: 12430
		Vpmaxsq,
		// Token: 0x0400308F RID: 12431
		Vpmaxsw,
		// Token: 0x04003090 RID: 12432
		Vpmaxub,
		// Token: 0x04003091 RID: 12433
		Vpmaxud,
		// Token: 0x04003092 RID: 12434
		Vpmaxuq,
		// Token: 0x04003093 RID: 12435
		Vpmaxuw,
		// Token: 0x04003094 RID: 12436
		Vpminsb,
		// Token: 0x04003095 RID: 12437
		Vpminsd,
		// Token: 0x04003096 RID: 12438
		Vpminsq,
		// Token: 0x04003097 RID: 12439
		Vpminsw,
		// Token: 0x04003098 RID: 12440
		Vpminub,
		// Token: 0x04003099 RID: 12441
		Vpminud,
		// Token: 0x0400309A RID: 12442
		Vpminuq,
		// Token: 0x0400309B RID: 12443
		Vpminuw,
		// Token: 0x0400309C RID: 12444
		Vpmovb2m,
		// Token: 0x0400309D RID: 12445
		Vpmovd2m,
		// Token: 0x0400309E RID: 12446
		Vpmovdb,
		// Token: 0x0400309F RID: 12447
		Vpmovdw,
		// Token: 0x040030A0 RID: 12448
		Vpmovm2b,
		// Token: 0x040030A1 RID: 12449
		Vpmovm2d,
		// Token: 0x040030A2 RID: 12450
		Vpmovm2q,
		// Token: 0x040030A3 RID: 12451
		Vpmovm2w,
		// Token: 0x040030A4 RID: 12452
		Vpmovmskb,
		// Token: 0x040030A5 RID: 12453
		Vpmovq2m,
		// Token: 0x040030A6 RID: 12454
		Vpmovqb,
		// Token: 0x040030A7 RID: 12455
		Vpmovqd,
		// Token: 0x040030A8 RID: 12456
		Vpmovqw,
		// Token: 0x040030A9 RID: 12457
		Vpmovsdb,
		// Token: 0x040030AA RID: 12458
		Vpmovsdw,
		// Token: 0x040030AB RID: 12459
		Vpmovsqb,
		// Token: 0x040030AC RID: 12460
		Vpmovsqd,
		// Token: 0x040030AD RID: 12461
		Vpmovsqw,
		// Token: 0x040030AE RID: 12462
		Vpmovswb,
		// Token: 0x040030AF RID: 12463
		Vpmovsxbd,
		// Token: 0x040030B0 RID: 12464
		Vpmovsxbq,
		// Token: 0x040030B1 RID: 12465
		Vpmovsxbw,
		// Token: 0x040030B2 RID: 12466
		Vpmovsxdq,
		// Token: 0x040030B3 RID: 12467
		Vpmovsxwd,
		// Token: 0x040030B4 RID: 12468
		Vpmovsxwq,
		// Token: 0x040030B5 RID: 12469
		Vpmovusdb,
		// Token: 0x040030B6 RID: 12470
		Vpmovusdw,
		// Token: 0x040030B7 RID: 12471
		Vpmovusqb,
		// Token: 0x040030B8 RID: 12472
		Vpmovusqd,
		// Token: 0x040030B9 RID: 12473
		Vpmovusqw,
		// Token: 0x040030BA RID: 12474
		Vpmovuswb,
		// Token: 0x040030BB RID: 12475
		Vpmovw2m,
		// Token: 0x040030BC RID: 12476
		Vpmovwb,
		// Token: 0x040030BD RID: 12477
		Vpmovzxbd,
		// Token: 0x040030BE RID: 12478
		Vpmovzxbq,
		// Token: 0x040030BF RID: 12479
		Vpmovzxbw,
		// Token: 0x040030C0 RID: 12480
		Vpmovzxdq,
		// Token: 0x040030C1 RID: 12481
		Vpmovzxwd,
		// Token: 0x040030C2 RID: 12482
		Vpmovzxwq,
		// Token: 0x040030C3 RID: 12483
		Vpmuldq,
		// Token: 0x040030C4 RID: 12484
		Vpmulhrsw,
		// Token: 0x040030C5 RID: 12485
		Vpmulhuw,
		// Token: 0x040030C6 RID: 12486
		Vpmulhw,
		// Token: 0x040030C7 RID: 12487
		Vpmulld,
		// Token: 0x040030C8 RID: 12488
		Vpmullq,
		// Token: 0x040030C9 RID: 12489
		Vpmullw,
		// Token: 0x040030CA RID: 12490
		Vpmultishiftqb,
		// Token: 0x040030CB RID: 12491
		Vpmuludq,
		// Token: 0x040030CC RID: 12492
		Vpopcntb,
		// Token: 0x040030CD RID: 12493
		Vpopcntd,
		// Token: 0x040030CE RID: 12494
		Vpopcntq,
		// Token: 0x040030CF RID: 12495
		Vpopcntw,
		// Token: 0x040030D0 RID: 12496
		Vpor,
		// Token: 0x040030D1 RID: 12497
		Vpord,
		// Token: 0x040030D2 RID: 12498
		Vporq,
		// Token: 0x040030D3 RID: 12499
		Vpperm,
		// Token: 0x040030D4 RID: 12500
		Vprold,
		// Token: 0x040030D5 RID: 12501
		Vprolq,
		// Token: 0x040030D6 RID: 12502
		Vprolvd,
		// Token: 0x040030D7 RID: 12503
		Vprolvq,
		// Token: 0x040030D8 RID: 12504
		Vprord,
		// Token: 0x040030D9 RID: 12505
		Vprorq,
		// Token: 0x040030DA RID: 12506
		Vprorvd,
		// Token: 0x040030DB RID: 12507
		Vprorvq,
		// Token: 0x040030DC RID: 12508
		Vprotb,
		// Token: 0x040030DD RID: 12509
		Vprotd,
		// Token: 0x040030DE RID: 12510
		Vprotq,
		// Token: 0x040030DF RID: 12511
		Vprotw,
		// Token: 0x040030E0 RID: 12512
		Vpsadbw,
		// Token: 0x040030E1 RID: 12513
		Vpscatterdd,
		// Token: 0x040030E2 RID: 12514
		Vpscatterdq,
		// Token: 0x040030E3 RID: 12515
		Vpscatterqd,
		// Token: 0x040030E4 RID: 12516
		Vpscatterqq,
		// Token: 0x040030E5 RID: 12517
		Vpshab,
		// Token: 0x040030E6 RID: 12518
		Vpshad,
		// Token: 0x040030E7 RID: 12519
		Vpshaq,
		// Token: 0x040030E8 RID: 12520
		Vpshaw,
		// Token: 0x040030E9 RID: 12521
		Vpshlb,
		// Token: 0x040030EA RID: 12522
		Vpshld,
		// Token: 0x040030EB RID: 12523
		Vpshldd,
		// Token: 0x040030EC RID: 12524
		Vpshldq,
		// Token: 0x040030ED RID: 12525
		Vpshldvd,
		// Token: 0x040030EE RID: 12526
		Vpshldvq,
		// Token: 0x040030EF RID: 12527
		Vpshldvw,
		// Token: 0x040030F0 RID: 12528
		Vpshldw,
		// Token: 0x040030F1 RID: 12529
		Vpshlq,
		// Token: 0x040030F2 RID: 12530
		Vpshlw,
		// Token: 0x040030F3 RID: 12531
		Vpshrdd,
		// Token: 0x040030F4 RID: 12532
		Vpshrdq,
		// Token: 0x040030F5 RID: 12533
		Vpshrdvd,
		// Token: 0x040030F6 RID: 12534
		Vpshrdvq,
		// Token: 0x040030F7 RID: 12535
		Vpshrdvw,
		// Token: 0x040030F8 RID: 12536
		Vpshrdw,
		// Token: 0x040030F9 RID: 12537
		Vpshufb,
		// Token: 0x040030FA RID: 12538
		Vpshufbitqmb,
		// Token: 0x040030FB RID: 12539
		Vpshufd,
		// Token: 0x040030FC RID: 12540
		Vpshufhw,
		// Token: 0x040030FD RID: 12541
		Vpshuflw,
		// Token: 0x040030FE RID: 12542
		Vpsignb,
		// Token: 0x040030FF RID: 12543
		Vpsignd,
		// Token: 0x04003100 RID: 12544
		Vpsignw,
		// Token: 0x04003101 RID: 12545
		Vpslld,
		// Token: 0x04003102 RID: 12546
		Vpslldq,
		// Token: 0x04003103 RID: 12547
		Vpsllq,
		// Token: 0x04003104 RID: 12548
		Vpsllvd,
		// Token: 0x04003105 RID: 12549
		Vpsllvq,
		// Token: 0x04003106 RID: 12550
		Vpsllvw,
		// Token: 0x04003107 RID: 12551
		Vpsllw,
		// Token: 0x04003108 RID: 12552
		Vpsrad,
		// Token: 0x04003109 RID: 12553
		Vpsraq,
		// Token: 0x0400310A RID: 12554
		Vpsravd,
		// Token: 0x0400310B RID: 12555
		Vpsravq,
		// Token: 0x0400310C RID: 12556
		Vpsravw,
		// Token: 0x0400310D RID: 12557
		Vpsraw,
		// Token: 0x0400310E RID: 12558
		Vpsrld,
		// Token: 0x0400310F RID: 12559
		Vpsrldq,
		// Token: 0x04003110 RID: 12560
		Vpsrlq,
		// Token: 0x04003111 RID: 12561
		Vpsrlvd,
		// Token: 0x04003112 RID: 12562
		Vpsrlvq,
		// Token: 0x04003113 RID: 12563
		Vpsrlvw,
		// Token: 0x04003114 RID: 12564
		Vpsrlw,
		// Token: 0x04003115 RID: 12565
		Vpsubb,
		// Token: 0x04003116 RID: 12566
		Vpsubd,
		// Token: 0x04003117 RID: 12567
		Vpsubq,
		// Token: 0x04003118 RID: 12568
		Vpsubsb,
		// Token: 0x04003119 RID: 12569
		Vpsubsw,
		// Token: 0x0400311A RID: 12570
		Vpsubusb,
		// Token: 0x0400311B RID: 12571
		Vpsubusw,
		// Token: 0x0400311C RID: 12572
		Vpsubw,
		// Token: 0x0400311D RID: 12573
		Vpternlogd,
		// Token: 0x0400311E RID: 12574
		Vpternlogq,
		// Token: 0x0400311F RID: 12575
		Vptest,
		// Token: 0x04003120 RID: 12576
		Vptestmb,
		// Token: 0x04003121 RID: 12577
		Vptestmd,
		// Token: 0x04003122 RID: 12578
		Vptestmq,
		// Token: 0x04003123 RID: 12579
		Vptestmw,
		// Token: 0x04003124 RID: 12580
		Vptestnmb,
		// Token: 0x04003125 RID: 12581
		Vptestnmd,
		// Token: 0x04003126 RID: 12582
		Vptestnmq,
		// Token: 0x04003127 RID: 12583
		Vptestnmw,
		// Token: 0x04003128 RID: 12584
		Vpunpckhbw,
		// Token: 0x04003129 RID: 12585
		Vpunpckhdq,
		// Token: 0x0400312A RID: 12586
		Vpunpckhqdq,
		// Token: 0x0400312B RID: 12587
		Vpunpckhwd,
		// Token: 0x0400312C RID: 12588
		Vpunpcklbw,
		// Token: 0x0400312D RID: 12589
		Vpunpckldq,
		// Token: 0x0400312E RID: 12590
		Vpunpcklqdq,
		// Token: 0x0400312F RID: 12591
		Vpunpcklwd,
		// Token: 0x04003130 RID: 12592
		Vpxor,
		// Token: 0x04003131 RID: 12593
		Vpxord,
		// Token: 0x04003132 RID: 12594
		Vpxorq,
		// Token: 0x04003133 RID: 12595
		Vrangepd,
		// Token: 0x04003134 RID: 12596
		Vrangeps,
		// Token: 0x04003135 RID: 12597
		Vrangesd,
		// Token: 0x04003136 RID: 12598
		Vrangess,
		// Token: 0x04003137 RID: 12599
		Vrcp14pd,
		// Token: 0x04003138 RID: 12600
		Vrcp14ps,
		// Token: 0x04003139 RID: 12601
		Vrcp14sd,
		// Token: 0x0400313A RID: 12602
		Vrcp14ss,
		// Token: 0x0400313B RID: 12603
		Vrcp28pd,
		// Token: 0x0400313C RID: 12604
		Vrcp28ps,
		// Token: 0x0400313D RID: 12605
		Vrcp28sd,
		// Token: 0x0400313E RID: 12606
		Vrcp28ss,
		// Token: 0x0400313F RID: 12607
		Vrcpps,
		// Token: 0x04003140 RID: 12608
		Vrcpss,
		// Token: 0x04003141 RID: 12609
		Vreducepd,
		// Token: 0x04003142 RID: 12610
		Vreduceps,
		// Token: 0x04003143 RID: 12611
		Vreducesd,
		// Token: 0x04003144 RID: 12612
		Vreducess,
		// Token: 0x04003145 RID: 12613
		Vrndscalepd,
		// Token: 0x04003146 RID: 12614
		Vrndscaleps,
		// Token: 0x04003147 RID: 12615
		Vrndscalesd,
		// Token: 0x04003148 RID: 12616
		Vrndscaless,
		// Token: 0x04003149 RID: 12617
		Vroundpd,
		// Token: 0x0400314A RID: 12618
		Vroundps,
		// Token: 0x0400314B RID: 12619
		Vroundsd,
		// Token: 0x0400314C RID: 12620
		Vroundss,
		// Token: 0x0400314D RID: 12621
		Vrsqrt14pd,
		// Token: 0x0400314E RID: 12622
		Vrsqrt14ps,
		// Token: 0x0400314F RID: 12623
		Vrsqrt14sd,
		// Token: 0x04003150 RID: 12624
		Vrsqrt14ss,
		// Token: 0x04003151 RID: 12625
		Vrsqrt28pd,
		// Token: 0x04003152 RID: 12626
		Vrsqrt28ps,
		// Token: 0x04003153 RID: 12627
		Vrsqrt28sd,
		// Token: 0x04003154 RID: 12628
		Vrsqrt28ss,
		// Token: 0x04003155 RID: 12629
		Vrsqrtps,
		// Token: 0x04003156 RID: 12630
		Vrsqrtss,
		// Token: 0x04003157 RID: 12631
		Vscalefpd,
		// Token: 0x04003158 RID: 12632
		Vscalefps,
		// Token: 0x04003159 RID: 12633
		Vscalefsd,
		// Token: 0x0400315A RID: 12634
		Vscalefss,
		// Token: 0x0400315B RID: 12635
		Vscatterdpd,
		// Token: 0x0400315C RID: 12636
		Vscatterdps,
		// Token: 0x0400315D RID: 12637
		Vscatterpf0dpd,
		// Token: 0x0400315E RID: 12638
		Vscatterpf0dps,
		// Token: 0x0400315F RID: 12639
		Vscatterpf0qpd,
		// Token: 0x04003160 RID: 12640
		Vscatterpf0qps,
		// Token: 0x04003161 RID: 12641
		Vscatterpf1dpd,
		// Token: 0x04003162 RID: 12642
		Vscatterpf1dps,
		// Token: 0x04003163 RID: 12643
		Vscatterpf1qpd,
		// Token: 0x04003164 RID: 12644
		Vscatterpf1qps,
		// Token: 0x04003165 RID: 12645
		Vscatterqpd,
		// Token: 0x04003166 RID: 12646
		Vscatterqps,
		// Token: 0x04003167 RID: 12647
		Vshuff32x4,
		// Token: 0x04003168 RID: 12648
		Vshuff64x2,
		// Token: 0x04003169 RID: 12649
		Vshufi32x4,
		// Token: 0x0400316A RID: 12650
		Vshufi64x2,
		// Token: 0x0400316B RID: 12651
		Vshufpd,
		// Token: 0x0400316C RID: 12652
		Vshufps,
		// Token: 0x0400316D RID: 12653
		Vsqrtpd,
		// Token: 0x0400316E RID: 12654
		Vsqrtps,
		// Token: 0x0400316F RID: 12655
		Vsqrtsd,
		// Token: 0x04003170 RID: 12656
		Vsqrtss,
		// Token: 0x04003171 RID: 12657
		Vstmxcsr,
		// Token: 0x04003172 RID: 12658
		Vsubpd,
		// Token: 0x04003173 RID: 12659
		Vsubps,
		// Token: 0x04003174 RID: 12660
		Vsubsd,
		// Token: 0x04003175 RID: 12661
		Vsubss,
		// Token: 0x04003176 RID: 12662
		Vtestpd,
		// Token: 0x04003177 RID: 12663
		Vtestps,
		// Token: 0x04003178 RID: 12664
		Vucomisd,
		// Token: 0x04003179 RID: 12665
		Vucomiss,
		// Token: 0x0400317A RID: 12666
		Vunpckhpd,
		// Token: 0x0400317B RID: 12667
		Vunpckhps,
		// Token: 0x0400317C RID: 12668
		Vunpcklpd,
		// Token: 0x0400317D RID: 12669
		Vunpcklps,
		// Token: 0x0400317E RID: 12670
		Vxorpd,
		// Token: 0x0400317F RID: 12671
		Vxorps,
		// Token: 0x04003180 RID: 12672
		Vzeroall,
		// Token: 0x04003181 RID: 12673
		Vzeroupper,
		// Token: 0x04003182 RID: 12674
		Wait,
		// Token: 0x04003183 RID: 12675
		Wbinvd,
		// Token: 0x04003184 RID: 12676
		Wbnoinvd,
		// Token: 0x04003185 RID: 12677
		Wrfsbase,
		// Token: 0x04003186 RID: 12678
		Wrgsbase,
		// Token: 0x04003187 RID: 12679
		Wrmsr,
		// Token: 0x04003188 RID: 12680
		Wrpkru,
		// Token: 0x04003189 RID: 12681
		Wrssd,
		// Token: 0x0400318A RID: 12682
		Wrssq,
		// Token: 0x0400318B RID: 12683
		Wrussd,
		// Token: 0x0400318C RID: 12684
		Wrussq,
		// Token: 0x0400318D RID: 12685
		Xabort,
		// Token: 0x0400318E RID: 12686
		Xadd,
		// Token: 0x0400318F RID: 12687
		Xbegin,
		// Token: 0x04003190 RID: 12688
		Xbts,
		// Token: 0x04003191 RID: 12689
		Xchg,
		// Token: 0x04003192 RID: 12690
		Xcryptcbc,
		// Token: 0x04003193 RID: 12691
		Xcryptcfb,
		// Token: 0x04003194 RID: 12692
		Xcryptctr,
		// Token: 0x04003195 RID: 12693
		Xcryptecb,
		// Token: 0x04003196 RID: 12694
		Xcryptofb,
		// Token: 0x04003197 RID: 12695
		Xend,
		// Token: 0x04003198 RID: 12696
		Xgetbv,
		// Token: 0x04003199 RID: 12697
		Xlatb,
		// Token: 0x0400319A RID: 12698
		Xor,
		// Token: 0x0400319B RID: 12699
		Xorpd,
		// Token: 0x0400319C RID: 12700
		Xorps,
		// Token: 0x0400319D RID: 12701
		Xrstor,
		// Token: 0x0400319E RID: 12702
		Xrstor64,
		// Token: 0x0400319F RID: 12703
		Xrstors,
		// Token: 0x040031A0 RID: 12704
		Xrstors64,
		// Token: 0x040031A1 RID: 12705
		Xsave,
		// Token: 0x040031A2 RID: 12706
		Xsave64,
		// Token: 0x040031A3 RID: 12707
		Xsavec,
		// Token: 0x040031A4 RID: 12708
		Xsavec64,
		// Token: 0x040031A5 RID: 12709
		Xsaveopt,
		// Token: 0x040031A6 RID: 12710
		Xsaveopt64,
		// Token: 0x040031A7 RID: 12711
		Xsaves,
		// Token: 0x040031A8 RID: 12712
		Xsaves64,
		// Token: 0x040031A9 RID: 12713
		Xsetbv,
		// Token: 0x040031AA RID: 12714
		Xsha1,
		// Token: 0x040031AB RID: 12715
		Xsha256,
		// Token: 0x040031AC RID: 12716
		Xstore,
		// Token: 0x040031AD RID: 12717
		Xtest,
		// Token: 0x040031AE RID: 12718
		Rmpadjust,
		// Token: 0x040031AF RID: 12719
		Rmpupdate,
		// Token: 0x040031B0 RID: 12720
		Psmash,
		// Token: 0x040031B1 RID: 12721
		Pvalidate,
		// Token: 0x040031B2 RID: 12722
		Serialize,
		// Token: 0x040031B3 RID: 12723
		Xsusldtrk,
		// Token: 0x040031B4 RID: 12724
		Xresldtrk,
		// Token: 0x040031B5 RID: 12725
		Invlpgb,
		// Token: 0x040031B6 RID: 12726
		Tlbsync,
		// Token: 0x040031B7 RID: 12727
		Vmgexit,
		// Token: 0x040031B8 RID: 12728
		Getsecq,
		// Token: 0x040031B9 RID: 12729
		Sysexitq,
		// Token: 0x040031BA RID: 12730
		Ldtilecfg,
		// Token: 0x040031BB RID: 12731
		Tilerelease,
		// Token: 0x040031BC RID: 12732
		Sttilecfg,
		// Token: 0x040031BD RID: 12733
		Tilezero,
		// Token: 0x040031BE RID: 12734
		Tileloaddt1,
		// Token: 0x040031BF RID: 12735
		Tilestored,
		// Token: 0x040031C0 RID: 12736
		Tileloadd,
		// Token: 0x040031C1 RID: 12737
		Tdpbf16ps,
		// Token: 0x040031C2 RID: 12738
		Tdpbuud,
		// Token: 0x040031C3 RID: 12739
		Tdpbusd,
		// Token: 0x040031C4 RID: 12740
		Tdpbsud,
		// Token: 0x040031C5 RID: 12741
		Tdpbssd,
		// Token: 0x040031C6 RID: 12742
		Sysretq,
		// Token: 0x040031C7 RID: 12743
		Fnstdw,
		// Token: 0x040031C8 RID: 12744
		Fnstsg,
		// Token: 0x040031C9 RID: 12745
		Rdshr,
		// Token: 0x040031CA RID: 12746
		Wrshr,
		// Token: 0x040031CB RID: 12747
		Smint,
		// Token: 0x040031CC RID: 12748
		Dmint,
		// Token: 0x040031CD RID: 12749
		Rdm,
		// Token: 0x040031CE RID: 12750
		Svdc,
		// Token: 0x040031CF RID: 12751
		Rsdc,
		// Token: 0x040031D0 RID: 12752
		Svldt,
		// Token: 0x040031D1 RID: 12753
		Rsldt,
		// Token: 0x040031D2 RID: 12754
		Svts,
		// Token: 0x040031D3 RID: 12755
		Rsts,
		// Token: 0x040031D4 RID: 12756
		Bb0_reset,
		// Token: 0x040031D5 RID: 12757
		Bb1_reset,
		// Token: 0x040031D6 RID: 12758
		Cpu_write,
		// Token: 0x040031D7 RID: 12759
		Cpu_read,
		// Token: 0x040031D8 RID: 12760
		Altinst,
		// Token: 0x040031D9 RID: 12761
		Paveb,
		// Token: 0x040031DA RID: 12762
		Paddsiw,
		// Token: 0x040031DB RID: 12763
		Pmagw,
		// Token: 0x040031DC RID: 12764
		Pdistib,
		// Token: 0x040031DD RID: 12765
		Psubsiw,
		// Token: 0x040031DE RID: 12766
		Pmvzb,
		// Token: 0x040031DF RID: 12767
		Pmvnzb,
		// Token: 0x040031E0 RID: 12768
		Pmvlzb,
		// Token: 0x040031E1 RID: 12769
		Pmvgezb,
		// Token: 0x040031E2 RID: 12770
		Pmulhriw,
		// Token: 0x040031E3 RID: 12771
		Pmachriw,
		// Token: 0x040031E4 RID: 12772
		Ftstp,
		// Token: 0x040031E5 RID: 12773
		Frint2,
		// Token: 0x040031E6 RID: 12774
		Frichop,
		// Token: 0x040031E7 RID: 12775
		Frinear,
		// Token: 0x040031E8 RID: 12776
		Undoc,
		// Token: 0x040031E9 RID: 12777
		Tdcall,
		// Token: 0x040031EA RID: 12778
		Seamret,
		// Token: 0x040031EB RID: 12779
		Seamops,
		// Token: 0x040031EC RID: 12780
		Seamcall,
		// Token: 0x040031ED RID: 12781
		Aesencwide128kl,
		// Token: 0x040031EE RID: 12782
		Aesdecwide128kl,
		// Token: 0x040031EF RID: 12783
		Aesencwide256kl,
		// Token: 0x040031F0 RID: 12784
		Aesdecwide256kl,
		// Token: 0x040031F1 RID: 12785
		Loadiwkey,
		// Token: 0x040031F2 RID: 12786
		Aesenc128kl,
		// Token: 0x040031F3 RID: 12787
		Aesdec128kl,
		// Token: 0x040031F4 RID: 12788
		Aesenc256kl,
		// Token: 0x040031F5 RID: 12789
		Aesdec256kl,
		// Token: 0x040031F6 RID: 12790
		Encodekey128,
		// Token: 0x040031F7 RID: 12791
		Encodekey256,
		// Token: 0x040031F8 RID: 12792
		Pushad,
		// Token: 0x040031F9 RID: 12793
		Popad,
		// Token: 0x040031FA RID: 12794
		Pushfd,
		// Token: 0x040031FB RID: 12795
		Pushfq,
		// Token: 0x040031FC RID: 12796
		Popfd,
		// Token: 0x040031FD RID: 12797
		Popfq,
		// Token: 0x040031FE RID: 12798
		Iretd,
		// Token: 0x040031FF RID: 12799
		Iretq,
		// Token: 0x04003200 RID: 12800
		Int3,
		// Token: 0x04003201 RID: 12801
		Uiret,
		// Token: 0x04003202 RID: 12802
		Testui,
		// Token: 0x04003203 RID: 12803
		Clui,
		// Token: 0x04003204 RID: 12804
		Stui,
		// Token: 0x04003205 RID: 12805
		Senduipi,
		// Token: 0x04003206 RID: 12806
		Hreset,
		// Token: 0x04003207 RID: 12807
		Ccs_hash,
		// Token: 0x04003208 RID: 12808
		Ccs_encrypt,
		// Token: 0x04003209 RID: 12809
		Lkgs,
		// Token: 0x0400320A RID: 12810
		Eretu,
		// Token: 0x0400320B RID: 12811
		Erets,
		// Token: 0x0400320C RID: 12812
		Storeall,
		// Token: 0x0400320D RID: 12813
		Vaddph,
		// Token: 0x0400320E RID: 12814
		Vaddsh,
		// Token: 0x0400320F RID: 12815
		Vcmpph,
		// Token: 0x04003210 RID: 12816
		Vcmpsh,
		// Token: 0x04003211 RID: 12817
		Vcomish,
		// Token: 0x04003212 RID: 12818
		Vcvtdq2ph,
		// Token: 0x04003213 RID: 12819
		Vcvtpd2ph,
		// Token: 0x04003214 RID: 12820
		Vcvtph2dq,
		// Token: 0x04003215 RID: 12821
		Vcvtph2pd,
		// Token: 0x04003216 RID: 12822
		Vcvtph2psx,
		// Token: 0x04003217 RID: 12823
		Vcvtph2qq,
		// Token: 0x04003218 RID: 12824
		Vcvtph2udq,
		// Token: 0x04003219 RID: 12825
		Vcvtph2uqq,
		// Token: 0x0400321A RID: 12826
		Vcvtph2uw,
		// Token: 0x0400321B RID: 12827
		Vcvtph2w,
		// Token: 0x0400321C RID: 12828
		Vcvtps2phx,
		// Token: 0x0400321D RID: 12829
		Vcvtqq2ph,
		// Token: 0x0400321E RID: 12830
		Vcvtsd2sh,
		// Token: 0x0400321F RID: 12831
		Vcvtsh2sd,
		// Token: 0x04003220 RID: 12832
		Vcvtsh2si,
		// Token: 0x04003221 RID: 12833
		Vcvtsh2ss,
		// Token: 0x04003222 RID: 12834
		Vcvtsh2usi,
		// Token: 0x04003223 RID: 12835
		Vcvtsi2sh,
		// Token: 0x04003224 RID: 12836
		Vcvtss2sh,
		// Token: 0x04003225 RID: 12837
		Vcvttph2dq,
		// Token: 0x04003226 RID: 12838
		Vcvttph2qq,
		// Token: 0x04003227 RID: 12839
		Vcvttph2udq,
		// Token: 0x04003228 RID: 12840
		Vcvttph2uqq,
		// Token: 0x04003229 RID: 12841
		Vcvttph2uw,
		// Token: 0x0400322A RID: 12842
		Vcvttph2w,
		// Token: 0x0400322B RID: 12843
		Vcvttsh2si,
		// Token: 0x0400322C RID: 12844
		Vcvttsh2usi,
		// Token: 0x0400322D RID: 12845
		Vcvtudq2ph,
		// Token: 0x0400322E RID: 12846
		Vcvtuqq2ph,
		// Token: 0x0400322F RID: 12847
		Vcvtusi2sh,
		// Token: 0x04003230 RID: 12848
		Vcvtuw2ph,
		// Token: 0x04003231 RID: 12849
		Vcvtw2ph,
		// Token: 0x04003232 RID: 12850
		Vdivph,
		// Token: 0x04003233 RID: 12851
		Vdivsh,
		// Token: 0x04003234 RID: 12852
		Vfcmaddcph,
		// Token: 0x04003235 RID: 12853
		Vfmaddcph,
		// Token: 0x04003236 RID: 12854
		Vfcmaddcsh,
		// Token: 0x04003237 RID: 12855
		Vfmaddcsh,
		// Token: 0x04003238 RID: 12856
		Vfcmulcph,
		// Token: 0x04003239 RID: 12857
		Vfmulcph,
		// Token: 0x0400323A RID: 12858
		Vfcmulcsh,
		// Token: 0x0400323B RID: 12859
		Vfmulcsh,
		// Token: 0x0400323C RID: 12860
		Vfmaddsub132ph,
		// Token: 0x0400323D RID: 12861
		Vfmaddsub213ph,
		// Token: 0x0400323E RID: 12862
		Vfmaddsub231ph,
		// Token: 0x0400323F RID: 12863
		Vfmsubadd132ph,
		// Token: 0x04003240 RID: 12864
		Vfmsubadd213ph,
		// Token: 0x04003241 RID: 12865
		Vfmsubadd231ph,
		// Token: 0x04003242 RID: 12866
		Vfmadd132ph,
		// Token: 0x04003243 RID: 12867
		Vfmadd213ph,
		// Token: 0x04003244 RID: 12868
		Vfmadd231ph,
		// Token: 0x04003245 RID: 12869
		Vfnmadd132ph,
		// Token: 0x04003246 RID: 12870
		Vfnmadd213ph,
		// Token: 0x04003247 RID: 12871
		Vfnmadd231ph,
		// Token: 0x04003248 RID: 12872
		Vfmadd132sh,
		// Token: 0x04003249 RID: 12873
		Vfmadd213sh,
		// Token: 0x0400324A RID: 12874
		Vfmadd231sh,
		// Token: 0x0400324B RID: 12875
		Vfnmadd132sh,
		// Token: 0x0400324C RID: 12876
		Vfnmadd213sh,
		// Token: 0x0400324D RID: 12877
		Vfnmadd231sh,
		// Token: 0x0400324E RID: 12878
		Vfmsub132ph,
		// Token: 0x0400324F RID: 12879
		Vfmsub213ph,
		// Token: 0x04003250 RID: 12880
		Vfmsub231ph,
		// Token: 0x04003251 RID: 12881
		Vfnmsub132ph,
		// Token: 0x04003252 RID: 12882
		Vfnmsub213ph,
		// Token: 0x04003253 RID: 12883
		Vfnmsub231ph,
		// Token: 0x04003254 RID: 12884
		Vfmsub132sh,
		// Token: 0x04003255 RID: 12885
		Vfmsub213sh,
		// Token: 0x04003256 RID: 12886
		Vfmsub231sh,
		// Token: 0x04003257 RID: 12887
		Vfnmsub132sh,
		// Token: 0x04003258 RID: 12888
		Vfnmsub213sh,
		// Token: 0x04003259 RID: 12889
		Vfnmsub231sh,
		// Token: 0x0400325A RID: 12890
		Vfpclassph,
		// Token: 0x0400325B RID: 12891
		Vfpclasssh,
		// Token: 0x0400325C RID: 12892
		Vgetexpph,
		// Token: 0x0400325D RID: 12893
		Vgetexpsh,
		// Token: 0x0400325E RID: 12894
		Vgetmantph,
		// Token: 0x0400325F RID: 12895
		Vgetmantsh,
		// Token: 0x04003260 RID: 12896
		Vmaxph,
		// Token: 0x04003261 RID: 12897
		Vmaxsh,
		// Token: 0x04003262 RID: 12898
		Vminph,
		// Token: 0x04003263 RID: 12899
		Vminsh,
		// Token: 0x04003264 RID: 12900
		Vmovsh,
		// Token: 0x04003265 RID: 12901
		Vmovw,
		// Token: 0x04003266 RID: 12902
		Vmulph,
		// Token: 0x04003267 RID: 12903
		Vmulsh,
		// Token: 0x04003268 RID: 12904
		Vrcpph,
		// Token: 0x04003269 RID: 12905
		Vrcpsh,
		// Token: 0x0400326A RID: 12906
		Vreduceph,
		// Token: 0x0400326B RID: 12907
		Vreducesh,
		// Token: 0x0400326C RID: 12908
		Vrndscaleph,
		// Token: 0x0400326D RID: 12909
		Vrndscalesh,
		// Token: 0x0400326E RID: 12910
		Vrsqrtph,
		// Token: 0x0400326F RID: 12911
		Vrsqrtsh,
		// Token: 0x04003270 RID: 12912
		Vscalefph,
		// Token: 0x04003271 RID: 12913
		Vscalefsh,
		// Token: 0x04003272 RID: 12914
		Vsqrtph,
		// Token: 0x04003273 RID: 12915
		Vsqrtsh,
		// Token: 0x04003274 RID: 12916
		Vsubph,
		// Token: 0x04003275 RID: 12917
		Vsubsh,
		// Token: 0x04003276 RID: 12918
		Vucomish,
		// Token: 0x04003277 RID: 12919
		Rdudbg,
		// Token: 0x04003278 RID: 12920
		Wrudbg,
		// Token: 0x04003279 RID: 12921
		Clevict0,
		// Token: 0x0400327A RID: 12922
		Clevict1,
		// Token: 0x0400327B RID: 12923
		Delay,
		// Token: 0x0400327C RID: 12924
		Jknzd,
		// Token: 0x0400327D RID: 12925
		Jkzd,
		// Token: 0x0400327E RID: 12926
		Kand,
		// Token: 0x0400327F RID: 12927
		Kandn,
		// Token: 0x04003280 RID: 12928
		Kandnr,
		// Token: 0x04003281 RID: 12929
		Kconcath,
		// Token: 0x04003282 RID: 12930
		Kconcatl,
		// Token: 0x04003283 RID: 12931
		Kextract,
		// Token: 0x04003284 RID: 12932
		Kmerge2l1h,
		// Token: 0x04003285 RID: 12933
		Kmerge2l1l,
		// Token: 0x04003286 RID: 12934
		Kmov,
		// Token: 0x04003287 RID: 12935
		Knot,
		// Token: 0x04003288 RID: 12936
		Kor,
		// Token: 0x04003289 RID: 12937
		Kortest,
		// Token: 0x0400328A RID: 12938
		Kxnor,
		// Token: 0x0400328B RID: 12939
		Kxor,
		// Token: 0x0400328C RID: 12940
		Spflt,
		// Token: 0x0400328D RID: 12941
		Tzcnti,
		// Token: 0x0400328E RID: 12942
		Vaddnpd,
		// Token: 0x0400328F RID: 12943
		Vaddnps,
		// Token: 0x04003290 RID: 12944
		Vaddsetsps,
		// Token: 0x04003291 RID: 12945
		Vcvtfxpntdq2ps,
		// Token: 0x04003292 RID: 12946
		Vcvtfxpntpd2dq,
		// Token: 0x04003293 RID: 12947
		Vcvtfxpntpd2udq,
		// Token: 0x04003294 RID: 12948
		Vcvtfxpntps2dq,
		// Token: 0x04003295 RID: 12949
		Vcvtfxpntps2udq,
		// Token: 0x04003296 RID: 12950
		Vcvtfxpntudq2ps,
		// Token: 0x04003297 RID: 12951
		Vexp223ps,
		// Token: 0x04003298 RID: 12952
		Vfixupnanpd,
		// Token: 0x04003299 RID: 12953
		Vfixupnanps,
		// Token: 0x0400329A RID: 12954
		Vfmadd233ps,
		// Token: 0x0400329B RID: 12955
		Vgatherpf0hintdpd,
		// Token: 0x0400329C RID: 12956
		Vgatherpf0hintdps,
		// Token: 0x0400329D RID: 12957
		Vgmaxabsps,
		// Token: 0x0400329E RID: 12958
		Vgmaxpd,
		// Token: 0x0400329F RID: 12959
		Vgmaxps,
		// Token: 0x040032A0 RID: 12960
		Vgminpd,
		// Token: 0x040032A1 RID: 12961
		Vgminps,
		// Token: 0x040032A2 RID: 12962
		Vloadunpackhd,
		// Token: 0x040032A3 RID: 12963
		Vloadunpackhpd,
		// Token: 0x040032A4 RID: 12964
		Vloadunpackhps,
		// Token: 0x040032A5 RID: 12965
		Vloadunpackhq,
		// Token: 0x040032A6 RID: 12966
		Vloadunpackld,
		// Token: 0x040032A7 RID: 12967
		Vloadunpacklpd,
		// Token: 0x040032A8 RID: 12968
		Vloadunpacklps,
		// Token: 0x040032A9 RID: 12969
		Vloadunpacklq,
		// Token: 0x040032AA RID: 12970
		Vlog2ps,
		// Token: 0x040032AB RID: 12971
		Vmovnrapd,
		// Token: 0x040032AC RID: 12972
		Vmovnraps,
		// Token: 0x040032AD RID: 12973
		Vmovnrngoapd,
		// Token: 0x040032AE RID: 12974
		Vmovnrngoaps,
		// Token: 0x040032AF RID: 12975
		Vpackstorehd,
		// Token: 0x040032B0 RID: 12976
		Vpackstorehpd,
		// Token: 0x040032B1 RID: 12977
		Vpackstorehps,
		// Token: 0x040032B2 RID: 12978
		Vpackstorehq,
		// Token: 0x040032B3 RID: 12979
		Vpackstoreld,
		// Token: 0x040032B4 RID: 12980
		Vpackstorelpd,
		// Token: 0x040032B5 RID: 12981
		Vpackstorelps,
		// Token: 0x040032B6 RID: 12982
		Vpackstorelq,
		// Token: 0x040032B7 RID: 12983
		Vpadcd,
		// Token: 0x040032B8 RID: 12984
		Vpaddsetcd,
		// Token: 0x040032B9 RID: 12985
		Vpaddsetsd,
		// Token: 0x040032BA RID: 12986
		Vpcmpltd,
		// Token: 0x040032BB RID: 12987
		Vpermf32x4,
		// Token: 0x040032BC RID: 12988
		Vpmadd231d,
		// Token: 0x040032BD RID: 12989
		Vpmadd233d,
		// Token: 0x040032BE RID: 12990
		Vpmulhd,
		// Token: 0x040032BF RID: 12991
		Vpmulhud,
		// Token: 0x040032C0 RID: 12992
		Vprefetch0,
		// Token: 0x040032C1 RID: 12993
		Vprefetch1,
		// Token: 0x040032C2 RID: 12994
		Vprefetch2,
		// Token: 0x040032C3 RID: 12995
		Vprefetche0,
		// Token: 0x040032C4 RID: 12996
		Vprefetche1,
		// Token: 0x040032C5 RID: 12997
		Vprefetche2,
		// Token: 0x040032C6 RID: 12998
		Vprefetchenta,
		// Token: 0x040032C7 RID: 12999
		Vprefetchnta,
		// Token: 0x040032C8 RID: 13000
		Vpsbbd,
		// Token: 0x040032C9 RID: 13001
		Vpsbbrd,
		// Token: 0x040032CA RID: 13002
		Vpsubrd,
		// Token: 0x040032CB RID: 13003
		Vpsubrsetbd,
		// Token: 0x040032CC RID: 13004
		Vpsubsetbd,
		// Token: 0x040032CD RID: 13005
		Vrcp23ps,
		// Token: 0x040032CE RID: 13006
		Vrndfxpntpd,
		// Token: 0x040032CF RID: 13007
		Vrndfxpntps,
		// Token: 0x040032D0 RID: 13008
		Vrsqrt23ps,
		// Token: 0x040032D1 RID: 13009
		Vscaleps,
		// Token: 0x040032D2 RID: 13010
		Vscatterpf0hintdpd,
		// Token: 0x040032D3 RID: 13011
		Vscatterpf0hintdps,
		// Token: 0x040032D4 RID: 13012
		Vsubrpd,
		// Token: 0x040032D5 RID: 13013
		Vsubrps,
		// Token: 0x040032D6 RID: 13014
		Xsha512,
		// Token: 0x040032D7 RID: 13015
		Xstore_alt,
		// Token: 0x040032D8 RID: 13016
		Xsha512_alt,
		// Token: 0x040032D9 RID: 13017
		Zero_bytes,
		// Token: 0x040032DA RID: 13018
		Aadd,
		// Token: 0x040032DB RID: 13019
		Aand,
		// Token: 0x040032DC RID: 13020
		Aor,
		// Token: 0x040032DD RID: 13021
		Axor,
		// Token: 0x040032DE RID: 13022
		Cmpbexadd,
		// Token: 0x040032DF RID: 13023
		Cmpbxadd,
		// Token: 0x040032E0 RID: 13024
		Cmplexadd,
		// Token: 0x040032E1 RID: 13025
		Cmplxadd,
		// Token: 0x040032E2 RID: 13026
		Cmpnbexadd,
		// Token: 0x040032E3 RID: 13027
		Cmpnbxadd,
		// Token: 0x040032E4 RID: 13028
		Cmpnlexadd,
		// Token: 0x040032E5 RID: 13029
		Cmpnlxadd,
		// Token: 0x040032E6 RID: 13030
		Cmpnoxadd,
		// Token: 0x040032E7 RID: 13031
		Cmpnpxadd,
		// Token: 0x040032E8 RID: 13032
		Cmpnsxadd,
		// Token: 0x040032E9 RID: 13033
		Cmpnzxadd,
		// Token: 0x040032EA RID: 13034
		Cmpoxadd,
		// Token: 0x040032EB RID: 13035
		Cmppxadd,
		// Token: 0x040032EC RID: 13036
		Cmpsxadd,
		// Token: 0x040032ED RID: 13037
		Cmpzxadd,
		// Token: 0x040032EE RID: 13038
		Prefetchit0,
		// Token: 0x040032EF RID: 13039
		Prefetchit1,
		// Token: 0x040032F0 RID: 13040
		Rdmsrlist,
		// Token: 0x040032F1 RID: 13041
		Rmpquery,
		// Token: 0x040032F2 RID: 13042
		Tdpfp16ps,
		// Token: 0x040032F3 RID: 13043
		Vbcstnebf162ps,
		// Token: 0x040032F4 RID: 13044
		Vbcstnesh2ps,
		// Token: 0x040032F5 RID: 13045
		Vcvtneebf162ps,
		// Token: 0x040032F6 RID: 13046
		Vcvtneeph2ps,
		// Token: 0x040032F7 RID: 13047
		Vcvtneobf162ps,
		// Token: 0x040032F8 RID: 13048
		Vcvtneoph2ps,
		// Token: 0x040032F9 RID: 13049
		Vpdpbssd,
		// Token: 0x040032FA RID: 13050
		Vpdpbssds,
		// Token: 0x040032FB RID: 13051
		Vpdpbsud,
		// Token: 0x040032FC RID: 13052
		Vpdpbsuds,
		// Token: 0x040032FD RID: 13053
		Vpdpbuud,
		// Token: 0x040032FE RID: 13054
		Vpdpbuuds,
		// Token: 0x040032FF RID: 13055
		Wrmsrlist,
		// Token: 0x04003300 RID: 13056
		Wrmsrns,
		// Token: 0x04003301 RID: 13057
		Tcmmrlfp16ps,
		// Token: 0x04003302 RID: 13058
		Tcmmimfp16ps,
		// Token: 0x04003303 RID: 13059
		Pbndkb,
		// Token: 0x04003304 RID: 13060
		Vpdpwsud,
		// Token: 0x04003305 RID: 13061
		Vpdpwsuds,
		// Token: 0x04003306 RID: 13062
		Vpdpwusd,
		// Token: 0x04003307 RID: 13063
		Vpdpwusds,
		// Token: 0x04003308 RID: 13064
		Vpdpwuud,
		// Token: 0x04003309 RID: 13065
		Vpdpwuuds,
		// Token: 0x0400330A RID: 13066
		Vsha512msg1,
		// Token: 0x0400330B RID: 13067
		Vsha512msg2,
		// Token: 0x0400330C RID: 13068
		Vsha512rnds2,
		// Token: 0x0400330D RID: 13069
		Vsm3msg1,
		// Token: 0x0400330E RID: 13070
		Vsm3msg2,
		// Token: 0x0400330F RID: 13071
		Vsm3rnds2,
		// Token: 0x04003310 RID: 13072
		Vsm4key4,
		// Token: 0x04003311 RID: 13073
		Vsm4rnds4
	}
}
