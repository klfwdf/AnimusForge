using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002FB RID: 763
	internal static class OpCodes
	{
		// Token: 0x0400090E RID: 2318
		internal static readonly OpCode[] OneByteOpCode = new OpCode[225];

		// Token: 0x0400090F RID: 2319
		internal static readonly OpCode[] TwoBytesOpCode = new OpCode[31];

		// Token: 0x04000910 RID: 2320
		public static readonly OpCode Nop = new OpCode(83886335, 318768389);

		// Token: 0x04000911 RID: 2321
		public static readonly OpCode Break = new OpCode(16843263, 318768389);

		// Token: 0x04000912 RID: 2322
		public static readonly OpCode Ldarg_0 = new OpCode(84017919, 335545601);

		// Token: 0x04000913 RID: 2323
		public static readonly OpCode Ldarg_1 = new OpCode(84083711, 335545601);

		// Token: 0x04000914 RID: 2324
		public static readonly OpCode Ldarg_2 = new OpCode(84149503, 335545601);

		// Token: 0x04000915 RID: 2325
		public static readonly OpCode Ldarg_3 = new OpCode(84215295, 335545601);

		// Token: 0x04000916 RID: 2326
		public static readonly OpCode Ldloc_0 = new OpCode(84281087, 335545601);

		// Token: 0x04000917 RID: 2327
		public static readonly OpCode Ldloc_1 = new OpCode(84346879, 335545601);

		// Token: 0x04000918 RID: 2328
		public static readonly OpCode Ldloc_2 = new OpCode(84412671, 335545601);

		// Token: 0x04000919 RID: 2329
		public static readonly OpCode Ldloc_3 = new OpCode(84478463, 335545601);

		// Token: 0x0400091A RID: 2330
		public static readonly OpCode Stloc_0 = new OpCode(84544255, 318833921);

		// Token: 0x0400091B RID: 2331
		public static readonly OpCode Stloc_1 = new OpCode(84610047, 318833921);

		// Token: 0x0400091C RID: 2332
		public static readonly OpCode Stloc_2 = new OpCode(84675839, 318833921);

		// Token: 0x0400091D RID: 2333
		public static readonly OpCode Stloc_3 = new OpCode(84741631, 318833921);

		// Token: 0x0400091E RID: 2334
		public static readonly OpCode Ldarg_S = new OpCode(84807423, 335549185);

		// Token: 0x0400091F RID: 2335
		public static readonly OpCode Ldarga_S = new OpCode(84873215, 369103617);

		// Token: 0x04000920 RID: 2336
		public static readonly OpCode Starg_S = new OpCode(84939007, 318837505);

		// Token: 0x04000921 RID: 2337
		public static readonly OpCode Ldloc_S = new OpCode(85004799, 335548929);

		// Token: 0x04000922 RID: 2338
		public static readonly OpCode Ldloca_S = new OpCode(85070591, 369103361);

		// Token: 0x04000923 RID: 2339
		public static readonly OpCode Stloc_S = new OpCode(85136383, 318837249);

		// Token: 0x04000924 RID: 2340
		public static readonly OpCode Ldnull = new OpCode(85202175, 436208901);

		// Token: 0x04000925 RID: 2341
		public static readonly OpCode Ldc_I4_M1 = new OpCode(85267967, 369100033);

		// Token: 0x04000926 RID: 2342
		public static readonly OpCode Ldc_I4_0 = new OpCode(85333759, 369100033);

		// Token: 0x04000927 RID: 2343
		public static readonly OpCode Ldc_I4_1 = new OpCode(85399551, 369100033);

		// Token: 0x04000928 RID: 2344
		public static readonly OpCode Ldc_I4_2 = new OpCode(85465343, 369100033);

		// Token: 0x04000929 RID: 2345
		public static readonly OpCode Ldc_I4_3 = new OpCode(85531135, 369100033);

		// Token: 0x0400092A RID: 2346
		public static readonly OpCode Ldc_I4_4 = new OpCode(85596927, 369100033);

		// Token: 0x0400092B RID: 2347
		public static readonly OpCode Ldc_I4_5 = new OpCode(85662719, 369100033);

		// Token: 0x0400092C RID: 2348
		public static readonly OpCode Ldc_I4_6 = new OpCode(85728511, 369100033);

		// Token: 0x0400092D RID: 2349
		public static readonly OpCode Ldc_I4_7 = new OpCode(85794303, 369100033);

		// Token: 0x0400092E RID: 2350
		public static readonly OpCode Ldc_I4_8 = new OpCode(85860095, 369100033);

		// Token: 0x0400092F RID: 2351
		public static readonly OpCode Ldc_I4_S = new OpCode(85925887, 369102849);

		// Token: 0x04000930 RID: 2352
		public static readonly OpCode Ldc_I4 = new OpCode(85991679, 369099269);

		// Token: 0x04000931 RID: 2353
		public static readonly OpCode Ldc_I8 = new OpCode(86057471, 385876741);

		// Token: 0x04000932 RID: 2354
		public static readonly OpCode Ldc_R4 = new OpCode(86123263, 402657541);

		// Token: 0x04000933 RID: 2355
		public static readonly OpCode Ldc_R8 = new OpCode(86189055, 419432197);

		// Token: 0x04000934 RID: 2356
		public static readonly OpCode Dup = new OpCode(86255103, 352388357);

		// Token: 0x04000935 RID: 2357
		public static readonly OpCode Pop = new OpCode(86320895, 318833925);

		// Token: 0x04000936 RID: 2358
		public static readonly OpCode Jmp = new OpCode(36055039, 318768133);

		// Token: 0x04000937 RID: 2359
		public static readonly OpCode Call = new OpCode(36120831, 471532549);

		// Token: 0x04000938 RID: 2360
		public static readonly OpCode Calli = new OpCode(36186623, 471533573);

		// Token: 0x04000939 RID: 2361
		public static readonly OpCode Ret = new OpCode(120138495, 320537861);

		// Token: 0x0400093A RID: 2362
		public static readonly OpCode Br_S = new OpCode(2763775, 318770945);

		// Token: 0x0400093B RID: 2363
		public static readonly OpCode Brfalse_S = new OpCode(53161215, 318967553);

		// Token: 0x0400093C RID: 2364
		public static readonly OpCode Brtrue_S = new OpCode(53227007, 318967553);

		// Token: 0x0400093D RID: 2365
		public static readonly OpCode Beq_S = new OpCode(53292799, 318902017);

		// Token: 0x0400093E RID: 2366
		public static readonly OpCode Bge_S = new OpCode(53358591, 318902017);

		// Token: 0x0400093F RID: 2367
		public static readonly OpCode Bgt_S = new OpCode(53424383, 318902017);

		// Token: 0x04000940 RID: 2368
		public static readonly OpCode Ble_S = new OpCode(53490175, 318902017);

		// Token: 0x04000941 RID: 2369
		public static readonly OpCode Blt_S = new OpCode(53555967, 318902017);

		// Token: 0x04000942 RID: 2370
		public static readonly OpCode Bne_Un_S = new OpCode(53621759, 318902017);

		// Token: 0x04000943 RID: 2371
		public static readonly OpCode Bge_Un_S = new OpCode(53687551, 318902017);

		// Token: 0x04000944 RID: 2372
		public static readonly OpCode Bgt_Un_S = new OpCode(53753343, 318902017);

		// Token: 0x04000945 RID: 2373
		public static readonly OpCode Ble_Un_S = new OpCode(53819135, 318902017);

		// Token: 0x04000946 RID: 2374
		public static readonly OpCode Blt_Un_S = new OpCode(53884927, 318902017);

		// Token: 0x04000947 RID: 2375
		public static readonly OpCode Br = new OpCode(3619071, 318767109);

		// Token: 0x04000948 RID: 2376
		public static readonly OpCode Brfalse = new OpCode(54016511, 318963717);

		// Token: 0x04000949 RID: 2377
		public static readonly OpCode Brtrue = new OpCode(54082303, 318963717);

		// Token: 0x0400094A RID: 2378
		public static readonly OpCode Beq = new OpCode(54148095, 318898177);

		// Token: 0x0400094B RID: 2379
		public static readonly OpCode Bge = new OpCode(54213887, 318898177);

		// Token: 0x0400094C RID: 2380
		public static readonly OpCode Bgt = new OpCode(54279679, 318898177);

		// Token: 0x0400094D RID: 2381
		public static readonly OpCode Ble = new OpCode(54345471, 318898177);

		// Token: 0x0400094E RID: 2382
		public static readonly OpCode Blt = new OpCode(54411263, 318898177);

		// Token: 0x0400094F RID: 2383
		public static readonly OpCode Bne_Un = new OpCode(54477055, 318898177);

		// Token: 0x04000950 RID: 2384
		public static readonly OpCode Bge_Un = new OpCode(54542847, 318898177);

		// Token: 0x04000951 RID: 2385
		public static readonly OpCode Bgt_Un = new OpCode(54608639, 318898177);

		// Token: 0x04000952 RID: 2386
		public static readonly OpCode Ble_Un = new OpCode(54674431, 318898177);

		// Token: 0x04000953 RID: 2387
		public static readonly OpCode Blt_Un = new OpCode(54740223, 318898177);

		// Token: 0x04000954 RID: 2388
		public static readonly OpCode Switch = new OpCode(54806015, 318966277);

		// Token: 0x04000955 RID: 2389
		public static readonly OpCode Ldind_I1 = new OpCode(88426239, 369296645);

		// Token: 0x04000956 RID: 2390
		public static readonly OpCode Ldind_U1 = new OpCode(88492031, 369296645);

		// Token: 0x04000957 RID: 2391
		public static readonly OpCode Ldind_I2 = new OpCode(88557823, 369296645);

		// Token: 0x04000958 RID: 2392
		public static readonly OpCode Ldind_U2 = new OpCode(88623615, 369296645);

		// Token: 0x04000959 RID: 2393
		public static readonly OpCode Ldind_I4 = new OpCode(88689407, 369296645);

		// Token: 0x0400095A RID: 2394
		public static readonly OpCode Ldind_U4 = new OpCode(88755199, 369296645);

		// Token: 0x0400095B RID: 2395
		public static readonly OpCode Ldind_I8 = new OpCode(88820991, 386073861);

		// Token: 0x0400095C RID: 2396
		public static readonly OpCode Ldind_I = new OpCode(88886783, 369296645);

		// Token: 0x0400095D RID: 2397
		public static readonly OpCode Ldind_R4 = new OpCode(88952575, 402851077);

		// Token: 0x0400095E RID: 2398
		public static readonly OpCode Ldind_R8 = new OpCode(89018367, 419628293);

		// Token: 0x0400095F RID: 2399
		public static readonly OpCode Ldind_Ref = new OpCode(89084159, 436405509);

		// Token: 0x04000960 RID: 2400
		public static readonly OpCode Stind_Ref = new OpCode(89149951, 319096069);

		// Token: 0x04000961 RID: 2401
		public static readonly OpCode Stind_I1 = new OpCode(89215743, 319096069);

		// Token: 0x04000962 RID: 2402
		public static readonly OpCode Stind_I2 = new OpCode(89281535, 319096069);

		// Token: 0x04000963 RID: 2403
		public static readonly OpCode Stind_I4 = new OpCode(89347327, 319096069);

		// Token: 0x04000964 RID: 2404
		public static readonly OpCode Stind_I8 = new OpCode(89413119, 319161605);

		// Token: 0x04000965 RID: 2405
		public static readonly OpCode Stind_R4 = new OpCode(89478911, 319292677);

		// Token: 0x04000966 RID: 2406
		public static readonly OpCode Stind_R8 = new OpCode(89544703, 319358213);

		// Token: 0x04000967 RID: 2407
		public static readonly OpCode Add = new OpCode(89610495, 335676677);

		// Token: 0x04000968 RID: 2408
		public static readonly OpCode Sub = new OpCode(89676287, 335676677);

		// Token: 0x04000969 RID: 2409
		public static readonly OpCode Mul = new OpCode(89742079, 335676677);

		// Token: 0x0400096A RID: 2410
		public static readonly OpCode Div = new OpCode(89807871, 335676677);

		// Token: 0x0400096B RID: 2411
		public static readonly OpCode Div_Un = new OpCode(89873663, 335676677);

		// Token: 0x0400096C RID: 2412
		public static readonly OpCode Rem = new OpCode(89939455, 335676677);

		// Token: 0x0400096D RID: 2413
		public static readonly OpCode Rem_Un = new OpCode(90005247, 335676677);

		// Token: 0x0400096E RID: 2414
		public static readonly OpCode And = new OpCode(90071039, 335676677);

		// Token: 0x0400096F RID: 2415
		public static readonly OpCode Or = new OpCode(90136831, 335676677);

		// Token: 0x04000970 RID: 2416
		public static readonly OpCode Xor = new OpCode(90202623, 335676677);

		// Token: 0x04000971 RID: 2417
		public static readonly OpCode Shl = new OpCode(90268415, 335676677);

		// Token: 0x04000972 RID: 2418
		public static readonly OpCode Shr = new OpCode(90334207, 335676677);

		// Token: 0x04000973 RID: 2419
		public static readonly OpCode Shr_Un = new OpCode(90399999, 335676677);

		// Token: 0x04000974 RID: 2420
		public static readonly OpCode Neg = new OpCode(90465791, 335611141);

		// Token: 0x04000975 RID: 2421
		public static readonly OpCode Not = new OpCode(90531583, 335611141);

		// Token: 0x04000976 RID: 2422
		public static readonly OpCode Conv_I1 = new OpCode(90597375, 369165573);

		// Token: 0x04000977 RID: 2423
		public static readonly OpCode Conv_I2 = new OpCode(90663167, 369165573);

		// Token: 0x04000978 RID: 2424
		public static readonly OpCode Conv_I4 = new OpCode(90728959, 369165573);

		// Token: 0x04000979 RID: 2425
		public static readonly OpCode Conv_I8 = new OpCode(90794751, 385942789);

		// Token: 0x0400097A RID: 2426
		public static readonly OpCode Conv_R4 = new OpCode(90860543, 402720005);

		// Token: 0x0400097B RID: 2427
		public static readonly OpCode Conv_R8 = new OpCode(90926335, 419497221);

		// Token: 0x0400097C RID: 2428
		public static readonly OpCode Conv_U4 = new OpCode(90992127, 369165573);

		// Token: 0x0400097D RID: 2429
		public static readonly OpCode Conv_U8 = new OpCode(91057919, 385942789);

		// Token: 0x0400097E RID: 2430
		public static readonly OpCode Callvirt = new OpCode(40792063, 471532547);

		// Token: 0x0400097F RID: 2431
		public static readonly OpCode Cpobj = new OpCode(91189503, 319097859);

		// Token: 0x04000980 RID: 2432
		public static readonly OpCode Ldobj = new OpCode(91255295, 335744003);

		// Token: 0x04000981 RID: 2433
		public static readonly OpCode Ldstr = new OpCode(91321087, 436209923);

		// Token: 0x04000982 RID: 2434
		public static readonly OpCode Newobj = new OpCode(41055231, 437978115);

		// Token: 0x04000983 RID: 2435
		public static readonly OpCode Castclass = new OpCode(91452671, 436866051);

		// Token: 0x04000984 RID: 2436
		public static readonly OpCode Isinst = new OpCode(91518463, 369757187);

		// Token: 0x04000985 RID: 2437
		public static readonly OpCode Conv_R_Un = new OpCode(91584255, 419497221);

		// Token: 0x04000986 RID: 2438
		public static readonly OpCode Unbox = new OpCode(91650559, 369757189);

		// Token: 0x04000987 RID: 2439
		public static readonly OpCode Throw = new OpCode(142047999, 319423747);

		// Token: 0x04000988 RID: 2440
		public static readonly OpCode Ldfld = new OpCode(91782143, 336199939);

		// Token: 0x04000989 RID: 2441
		public static readonly OpCode Ldflda = new OpCode(91847935, 369754371);

		// Token: 0x0400098A RID: 2442
		public static readonly OpCode Stfld = new OpCode(91913727, 319488259);

		// Token: 0x0400098B RID: 2443
		public static readonly OpCode Ldsfld = new OpCode(91979519, 335544579);

		// Token: 0x0400098C RID: 2444
		public static readonly OpCode Ldsflda = new OpCode(92045311, 369099011);

		// Token: 0x0400098D RID: 2445
		public static readonly OpCode Stsfld = new OpCode(92111103, 318832899);

		// Token: 0x0400098E RID: 2446
		public static readonly OpCode Stobj = new OpCode(92176895, 319032323);

		// Token: 0x0400098F RID: 2447
		public static readonly OpCode Conv_Ovf_I1_Un = new OpCode(92242687, 369165573);

		// Token: 0x04000990 RID: 2448
		public static readonly OpCode Conv_Ovf_I2_Un = new OpCode(92308479, 369165573);

		// Token: 0x04000991 RID: 2449
		public static readonly OpCode Conv_Ovf_I4_Un = new OpCode(92374271, 369165573);

		// Token: 0x04000992 RID: 2450
		public static readonly OpCode Conv_Ovf_I8_Un = new OpCode(92440063, 385942789);

		// Token: 0x04000993 RID: 2451
		public static readonly OpCode Conv_Ovf_U1_Un = new OpCode(92505855, 369165573);

		// Token: 0x04000994 RID: 2452
		public static readonly OpCode Conv_Ovf_U2_Un = new OpCode(92571647, 369165573);

		// Token: 0x04000995 RID: 2453
		public static readonly OpCode Conv_Ovf_U4_Un = new OpCode(92637439, 369165573);

		// Token: 0x04000996 RID: 2454
		public static readonly OpCode Conv_Ovf_U8_Un = new OpCode(92703231, 385942789);

		// Token: 0x04000997 RID: 2455
		public static readonly OpCode Conv_Ovf_I_Un = new OpCode(92769023, 369165573);

		// Token: 0x04000998 RID: 2456
		public static readonly OpCode Conv_Ovf_U_Un = new OpCode(92834815, 369165573);

		// Token: 0x04000999 RID: 2457
		public static readonly OpCode Box = new OpCode(92900607, 436276229);

		// Token: 0x0400099A RID: 2458
		public static readonly OpCode Newarr = new OpCode(92966399, 436407299);

		// Token: 0x0400099B RID: 2459
		public static readonly OpCode Ldlen = new OpCode(93032191, 369755395);

		// Token: 0x0400099C RID: 2460
		public static readonly OpCode Ldelema = new OpCode(93097983, 369888259);

		// Token: 0x0400099D RID: 2461
		public static readonly OpCode Ldelem_I1 = new OpCode(93163775, 369886467);

		// Token: 0x0400099E RID: 2462
		public static readonly OpCode Ldelem_U1 = new OpCode(93229567, 369886467);

		// Token: 0x0400099F RID: 2463
		public static readonly OpCode Ldelem_I2 = new OpCode(93295359, 369886467);

		// Token: 0x040009A0 RID: 2464
		public static readonly OpCode Ldelem_U2 = new OpCode(93361151, 369886467);

		// Token: 0x040009A1 RID: 2465
		public static readonly OpCode Ldelem_I4 = new OpCode(93426943, 369886467);

		// Token: 0x040009A2 RID: 2466
		public static readonly OpCode Ldelem_U4 = new OpCode(93492735, 369886467);

		// Token: 0x040009A3 RID: 2467
		public static readonly OpCode Ldelem_I8 = new OpCode(93558527, 386663683);

		// Token: 0x040009A4 RID: 2468
		public static readonly OpCode Ldelem_I = new OpCode(93624319, 369886467);

		// Token: 0x040009A5 RID: 2469
		public static readonly OpCode Ldelem_R4 = new OpCode(93690111, 403440899);

		// Token: 0x040009A6 RID: 2470
		public static readonly OpCode Ldelem_R8 = new OpCode(93755903, 420218115);

		// Token: 0x040009A7 RID: 2471
		public static readonly OpCode Ldelem_Ref = new OpCode(93821695, 436995331);

		// Token: 0x040009A8 RID: 2472
		public static readonly OpCode Stelem_I = new OpCode(93887487, 319620355);

		// Token: 0x040009A9 RID: 2473
		public static readonly OpCode Stelem_I1 = new OpCode(93953279, 319620355);

		// Token: 0x040009AA RID: 2474
		public static readonly OpCode Stelem_I2 = new OpCode(94019071, 319620355);

		// Token: 0x040009AB RID: 2475
		public static readonly OpCode Stelem_I4 = new OpCode(94084863, 319620355);

		// Token: 0x040009AC RID: 2476
		public static readonly OpCode Stelem_I8 = new OpCode(94150655, 319685891);

		// Token: 0x040009AD RID: 2477
		public static readonly OpCode Stelem_R4 = new OpCode(94216447, 319751427);

		// Token: 0x040009AE RID: 2478
		public static readonly OpCode Stelem_R8 = new OpCode(94282239, 319816963);

		// Token: 0x040009AF RID: 2479
		public static readonly OpCode Stelem_Ref = new OpCode(94348031, 319882499);

		// Token: 0x040009B0 RID: 2480
		public static readonly OpCode Ldelem_Any = new OpCode(94413823, 336333827);

		// Token: 0x040009B1 RID: 2481
		public static readonly OpCode Stelem_Any = new OpCode(94479615, 319884291);

		// Token: 0x040009B2 RID: 2482
		public static readonly OpCode Unbox_Any = new OpCode(94545407, 336202755);

		// Token: 0x040009B3 RID: 2483
		public static readonly OpCode Conv_Ovf_I1 = new OpCode(94614527, 369165573);

		// Token: 0x040009B4 RID: 2484
		public static readonly OpCode Conv_Ovf_U1 = new OpCode(94680319, 369165573);

		// Token: 0x040009B5 RID: 2485
		public static readonly OpCode Conv_Ovf_I2 = new OpCode(94746111, 369165573);

		// Token: 0x040009B6 RID: 2486
		public static readonly OpCode Conv_Ovf_U2 = new OpCode(94811903, 369165573);

		// Token: 0x040009B7 RID: 2487
		public static readonly OpCode Conv_Ovf_I4 = new OpCode(94877695, 369165573);

		// Token: 0x040009B8 RID: 2488
		public static readonly OpCode Conv_Ovf_U4 = new OpCode(94943487, 369165573);

		// Token: 0x040009B9 RID: 2489
		public static readonly OpCode Conv_Ovf_I8 = new OpCode(95009279, 385942789);

		// Token: 0x040009BA RID: 2490
		public static readonly OpCode Conv_Ovf_U8 = new OpCode(95075071, 385942789);

		// Token: 0x040009BB RID: 2491
		public static readonly OpCode Refanyval = new OpCode(95142655, 369167365);

		// Token: 0x040009BC RID: 2492
		public static readonly OpCode Ckfinite = new OpCode(95208447, 419497221);

		// Token: 0x040009BD RID: 2493
		public static readonly OpCode Mkrefany = new OpCode(95274751, 335744005);

		// Token: 0x040009BE RID: 2494
		public static readonly OpCode Ldtoken = new OpCode(95342847, 369101573);

		// Token: 0x040009BF RID: 2495
		public static readonly OpCode Conv_U2 = new OpCode(95408639, 369165573);

		// Token: 0x040009C0 RID: 2496
		public static readonly OpCode Conv_U1 = new OpCode(95474431, 369165573);

		// Token: 0x040009C1 RID: 2497
		public static readonly OpCode Conv_I = new OpCode(95540223, 369165573);

		// Token: 0x040009C2 RID: 2498
		public static readonly OpCode Conv_Ovf_I = new OpCode(95606015, 369165573);

		// Token: 0x040009C3 RID: 2499
		public static readonly OpCode Conv_Ovf_U = new OpCode(95671807, 369165573);

		// Token: 0x040009C4 RID: 2500
		public static readonly OpCode Add_Ovf = new OpCode(95737599, 335676677);

		// Token: 0x040009C5 RID: 2501
		public static readonly OpCode Add_Ovf_Un = new OpCode(95803391, 335676677);

		// Token: 0x040009C6 RID: 2502
		public static readonly OpCode Mul_Ovf = new OpCode(95869183, 335676677);

		// Token: 0x040009C7 RID: 2503
		public static readonly OpCode Mul_Ovf_Un = new OpCode(95934975, 335676677);

		// Token: 0x040009C8 RID: 2504
		public static readonly OpCode Sub_Ovf = new OpCode(96000767, 335676677);

		// Token: 0x040009C9 RID: 2505
		public static readonly OpCode Sub_Ovf_Un = new OpCode(96066559, 335676677);

		// Token: 0x040009CA RID: 2506
		public static readonly OpCode Endfinally = new OpCode(129686783, 318768389);

		// Token: 0x040009CB RID: 2507
		public static readonly OpCode Leave = new OpCode(12312063, 319946757);

		// Token: 0x040009CC RID: 2508
		public static readonly OpCode Leave_S = new OpCode(12377855, 319950593);

		// Token: 0x040009CD RID: 2509
		public static readonly OpCode Stind_I = new OpCode(96329727, 319096069);

		// Token: 0x040009CE RID: 2510
		public static readonly OpCode Conv_U = new OpCode(96395519, 369165573);

		// Token: 0x040009CF RID: 2511
		public static readonly OpCode Arglist = new OpCode(96403710, 369100037);

		// Token: 0x040009D0 RID: 2512
		public static readonly OpCode Ceq = new OpCode(96469502, 369231109);

		// Token: 0x040009D1 RID: 2513
		public static readonly OpCode Cgt = new OpCode(96535294, 369231109);

		// Token: 0x040009D2 RID: 2514
		public static readonly OpCode Cgt_Un = new OpCode(96601086, 369231109);

		// Token: 0x040009D3 RID: 2515
		public static readonly OpCode Clt = new OpCode(96666878, 369231109);

		// Token: 0x040009D4 RID: 2516
		public static readonly OpCode Clt_Un = new OpCode(96732670, 369231109);

		// Token: 0x040009D5 RID: 2517
		public static readonly OpCode Ldftn = new OpCode(96798462, 369099781);

		// Token: 0x040009D6 RID: 2518
		public static readonly OpCode Ldvirtftn = new OpCode(96864254, 369755141);

		// Token: 0x040009D7 RID: 2519
		public static readonly OpCode Ldarg = new OpCode(96930302, 335547909);

		// Token: 0x040009D8 RID: 2520
		public static readonly OpCode Ldarga = new OpCode(96996094, 369102341);

		// Token: 0x040009D9 RID: 2521
		public static readonly OpCode Starg = new OpCode(97061886, 318836229);

		// Token: 0x040009DA RID: 2522
		public static readonly OpCode Ldloc = new OpCode(97127678, 335547653);

		// Token: 0x040009DB RID: 2523
		public static readonly OpCode Ldloca = new OpCode(97193470, 369102085);

		// Token: 0x040009DC RID: 2524
		public static readonly OpCode Stloc = new OpCode(97259262, 318835973);

		// Token: 0x040009DD RID: 2525
		public static readonly OpCode Localloc = new OpCode(97325054, 369296645);

		// Token: 0x040009DE RID: 2526
		public static readonly OpCode Endfilter = new OpCode(130945534, 318964997);

		// Token: 0x040009DF RID: 2527
		public static readonly OpCode Unaligned = new OpCode(80679678, 318771204);

		// Token: 0x040009E0 RID: 2528
		public static readonly OpCode Volatile = new OpCode(80745470, 318768388);

		// Token: 0x040009E1 RID: 2529
		public static readonly OpCode Tail = new OpCode(80811262, 318768388);

		// Token: 0x040009E2 RID: 2530
		public static readonly OpCode Initobj = new OpCode(97654270, 318966787);

		// Token: 0x040009E3 RID: 2531
		public static readonly OpCode Constrained = new OpCode(97720062, 318770180);

		// Token: 0x040009E4 RID: 2532
		public static readonly OpCode Cpblk = new OpCode(97785854, 319227141);

		// Token: 0x040009E5 RID: 2533
		public static readonly OpCode Initblk = new OpCode(97851646, 319227141);

		// Token: 0x040009E6 RID: 2534
		public static readonly OpCode No = new OpCode(97917438, 318771204);

		// Token: 0x040009E7 RID: 2535
		public static readonly OpCode Rethrow = new OpCode(148314878, 318768387);

		// Token: 0x040009E8 RID: 2536
		public static readonly OpCode Sizeof = new OpCode(98049278, 369101829);

		// Token: 0x040009E9 RID: 2537
		public static readonly OpCode Refanytype = new OpCode(98115070, 369165573);

		// Token: 0x040009EA RID: 2538
		public static readonly OpCode Readonly = new OpCode(98180862, 318768388);
	}
}
