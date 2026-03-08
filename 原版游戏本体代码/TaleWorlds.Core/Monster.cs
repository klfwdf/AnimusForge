using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000BD RID: 189
	public sealed class Monster : MBObjectBase
	{
		// Token: 0x17000357 RID: 855
		// (get) Token: 0x060009F0 RID: 2544 RVA: 0x00020653 File Offset: 0x0001E853
		// (set) Token: 0x060009F1 RID: 2545 RVA: 0x0002065B File Offset: 0x0001E85B
		public string BaseMonster { get; private set; }

		// Token: 0x17000358 RID: 856
		// (get) Token: 0x060009F2 RID: 2546 RVA: 0x00020664 File Offset: 0x0001E864
		// (set) Token: 0x060009F3 RID: 2547 RVA: 0x0002066C File Offset: 0x0001E86C
		public float BodyCapsuleRadius { get; private set; }

		// Token: 0x17000359 RID: 857
		// (get) Token: 0x060009F4 RID: 2548 RVA: 0x00020675 File Offset: 0x0001E875
		// (set) Token: 0x060009F5 RID: 2549 RVA: 0x0002067D File Offset: 0x0001E87D
		public Vec3 BodyCapsulePoint1 { get; private set; }

		// Token: 0x1700035A RID: 858
		// (get) Token: 0x060009F6 RID: 2550 RVA: 0x00020686 File Offset: 0x0001E886
		// (set) Token: 0x060009F7 RID: 2551 RVA: 0x0002068E File Offset: 0x0001E88E
		public Vec3 BodyCapsulePoint2 { get; private set; }

		// Token: 0x1700035B RID: 859
		// (get) Token: 0x060009F8 RID: 2552 RVA: 0x00020697 File Offset: 0x0001E897
		// (set) Token: 0x060009F9 RID: 2553 RVA: 0x0002069F File Offset: 0x0001E89F
		public float CrouchedBodyCapsuleRadius { get; private set; }

		// Token: 0x1700035C RID: 860
		// (get) Token: 0x060009FA RID: 2554 RVA: 0x000206A8 File Offset: 0x0001E8A8
		// (set) Token: 0x060009FB RID: 2555 RVA: 0x000206B0 File Offset: 0x0001E8B0
		public Vec3 CrouchedBodyCapsulePoint1 { get; private set; }

		// Token: 0x1700035D RID: 861
		// (get) Token: 0x060009FC RID: 2556 RVA: 0x000206B9 File Offset: 0x0001E8B9
		// (set) Token: 0x060009FD RID: 2557 RVA: 0x000206C1 File Offset: 0x0001E8C1
		public Vec3 CrouchedBodyCapsulePoint2 { get; private set; }

		// Token: 0x1700035E RID: 862
		// (get) Token: 0x060009FE RID: 2558 RVA: 0x000206CA File Offset: 0x0001E8CA
		// (set) Token: 0x060009FF RID: 2559 RVA: 0x000206D2 File Offset: 0x0001E8D2
		public AgentFlag Flags { get; private set; }

		// Token: 0x1700035F RID: 863
		// (get) Token: 0x06000A00 RID: 2560 RVA: 0x000206DB File Offset: 0x0001E8DB
		// (set) Token: 0x06000A01 RID: 2561 RVA: 0x000206E3 File Offset: 0x0001E8E3
		public int Weight { get; private set; }

		// Token: 0x17000360 RID: 864
		// (get) Token: 0x06000A02 RID: 2562 RVA: 0x000206EC File Offset: 0x0001E8EC
		// (set) Token: 0x06000A03 RID: 2563 RVA: 0x000206F4 File Offset: 0x0001E8F4
		public int HitPoints { get; private set; }

		// Token: 0x17000361 RID: 865
		// (get) Token: 0x06000A04 RID: 2564 RVA: 0x000206FD File Offset: 0x0001E8FD
		// (set) Token: 0x06000A05 RID: 2565 RVA: 0x00020705 File Offset: 0x0001E905
		public string ActionSetCode { get; private set; }

		// Token: 0x17000362 RID: 866
		// (get) Token: 0x06000A06 RID: 2566 RVA: 0x0002070E File Offset: 0x0001E90E
		// (set) Token: 0x06000A07 RID: 2567 RVA: 0x00020716 File Offset: 0x0001E916
		public string FemaleActionSetCode { get; private set; }

		// Token: 0x17000363 RID: 867
		// (get) Token: 0x06000A08 RID: 2568 RVA: 0x0002071F File Offset: 0x0001E91F
		// (set) Token: 0x06000A09 RID: 2569 RVA: 0x00020727 File Offset: 0x0001E927
		public int NumPaces { get; private set; }

		// Token: 0x17000364 RID: 868
		// (get) Token: 0x06000A0A RID: 2570 RVA: 0x00020730 File Offset: 0x0001E930
		// (set) Token: 0x06000A0B RID: 2571 RVA: 0x00020738 File Offset: 0x0001E938
		public string MonsterUsage { get; private set; }

		// Token: 0x17000365 RID: 869
		// (get) Token: 0x06000A0C RID: 2572 RVA: 0x00020741 File Offset: 0x0001E941
		// (set) Token: 0x06000A0D RID: 2573 RVA: 0x00020749 File Offset: 0x0001E949
		public float WalkingSpeedLimit { get; private set; }

		// Token: 0x17000366 RID: 870
		// (get) Token: 0x06000A0E RID: 2574 RVA: 0x00020752 File Offset: 0x0001E952
		// (set) Token: 0x06000A0F RID: 2575 RVA: 0x0002075A File Offset: 0x0001E95A
		public float CrouchWalkingSpeedLimit { get; private set; }

		// Token: 0x17000367 RID: 871
		// (get) Token: 0x06000A10 RID: 2576 RVA: 0x00020763 File Offset: 0x0001E963
		// (set) Token: 0x06000A11 RID: 2577 RVA: 0x0002076B File Offset: 0x0001E96B
		public float JumpAcceleration { get; private set; }

		// Token: 0x17000368 RID: 872
		// (get) Token: 0x06000A12 RID: 2578 RVA: 0x00020774 File Offset: 0x0001E974
		// (set) Token: 0x06000A13 RID: 2579 RVA: 0x0002077C File Offset: 0x0001E97C
		public float AbsorbedDamageRatio { get; private set; }

		// Token: 0x17000369 RID: 873
		// (get) Token: 0x06000A14 RID: 2580 RVA: 0x00020785 File Offset: 0x0001E985
		// (set) Token: 0x06000A15 RID: 2581 RVA: 0x0002078D File Offset: 0x0001E98D
		public string SoundAndCollisionInfoClassName { get; private set; }

		// Token: 0x1700036A RID: 874
		// (get) Token: 0x06000A16 RID: 2582 RVA: 0x00020796 File Offset: 0x0001E996
		// (set) Token: 0x06000A17 RID: 2583 RVA: 0x0002079E File Offset: 0x0001E99E
		public float RiderCameraHeightAdder { get; private set; }

		// Token: 0x1700036B RID: 875
		// (get) Token: 0x06000A18 RID: 2584 RVA: 0x000207A7 File Offset: 0x0001E9A7
		// (set) Token: 0x06000A19 RID: 2585 RVA: 0x000207AF File Offset: 0x0001E9AF
		public float RiderBodyCapsuleHeightAdder { get; private set; }

		// Token: 0x1700036C RID: 876
		// (get) Token: 0x06000A1A RID: 2586 RVA: 0x000207B8 File Offset: 0x0001E9B8
		// (set) Token: 0x06000A1B RID: 2587 RVA: 0x000207C0 File Offset: 0x0001E9C0
		public float RiderBodyCapsuleForwardAdder { get; private set; }

		// Token: 0x1700036D RID: 877
		// (get) Token: 0x06000A1C RID: 2588 RVA: 0x000207C9 File Offset: 0x0001E9C9
		// (set) Token: 0x06000A1D RID: 2589 RVA: 0x000207D1 File Offset: 0x0001E9D1
		public float StandingChestHeight { get; private set; }

		// Token: 0x1700036E RID: 878
		// (get) Token: 0x06000A1E RID: 2590 RVA: 0x000207DA File Offset: 0x0001E9DA
		// (set) Token: 0x06000A1F RID: 2591 RVA: 0x000207E2 File Offset: 0x0001E9E2
		public float StandingPelvisHeight { get; private set; }

		// Token: 0x1700036F RID: 879
		// (get) Token: 0x06000A20 RID: 2592 RVA: 0x000207EB File Offset: 0x0001E9EB
		// (set) Token: 0x06000A21 RID: 2593 RVA: 0x000207F3 File Offset: 0x0001E9F3
		public float StandingEyeHeight { get; private set; }

		// Token: 0x17000370 RID: 880
		// (get) Token: 0x06000A22 RID: 2594 RVA: 0x000207FC File Offset: 0x0001E9FC
		// (set) Token: 0x06000A23 RID: 2595 RVA: 0x00020804 File Offset: 0x0001EA04
		public float CrouchEyeHeight { get; private set; }

		// Token: 0x17000371 RID: 881
		// (get) Token: 0x06000A24 RID: 2596 RVA: 0x0002080D File Offset: 0x0001EA0D
		// (set) Token: 0x06000A25 RID: 2597 RVA: 0x00020815 File Offset: 0x0001EA15
		public float MountedEyeHeight { get; private set; }

		// Token: 0x17000372 RID: 882
		// (get) Token: 0x06000A26 RID: 2598 RVA: 0x0002081E File Offset: 0x0001EA1E
		// (set) Token: 0x06000A27 RID: 2599 RVA: 0x00020826 File Offset: 0x0001EA26
		public float RiderEyeHeightAdder { get; private set; }

		// Token: 0x17000373 RID: 883
		// (get) Token: 0x06000A28 RID: 2600 RVA: 0x0002082F File Offset: 0x0001EA2F
		// (set) Token: 0x06000A29 RID: 2601 RVA: 0x00020837 File Offset: 0x0001EA37
		public Vec3 EyeOffsetWrtHead { get; private set; }

		// Token: 0x17000374 RID: 884
		// (get) Token: 0x06000A2A RID: 2602 RVA: 0x00020840 File Offset: 0x0001EA40
		// (set) Token: 0x06000A2B RID: 2603 RVA: 0x00020848 File Offset: 0x0001EA48
		public Vec3 FirstPersonCameraOffsetWrtHead { get; private set; }

		// Token: 0x17000375 RID: 885
		// (get) Token: 0x06000A2C RID: 2604 RVA: 0x00020851 File Offset: 0x0001EA51
		// (set) Token: 0x06000A2D RID: 2605 RVA: 0x00020859 File Offset: 0x0001EA59
		public float ArmLength { get; private set; }

		// Token: 0x17000376 RID: 886
		// (get) Token: 0x06000A2E RID: 2606 RVA: 0x00020862 File Offset: 0x0001EA62
		// (set) Token: 0x06000A2F RID: 2607 RVA: 0x0002086A File Offset: 0x0001EA6A
		public float ArmWeight { get; private set; }

		// Token: 0x17000377 RID: 887
		// (get) Token: 0x06000A30 RID: 2608 RVA: 0x00020873 File Offset: 0x0001EA73
		// (set) Token: 0x06000A31 RID: 2609 RVA: 0x0002087B File Offset: 0x0001EA7B
		public float JumpSpeedLimit { get; private set; }

		// Token: 0x17000378 RID: 888
		// (get) Token: 0x06000A32 RID: 2610 RVA: 0x00020884 File Offset: 0x0001EA84
		// (set) Token: 0x06000A33 RID: 2611 RVA: 0x0002088C File Offset: 0x0001EA8C
		public float RelativeSpeedLimitForCharge { get; private set; }

		// Token: 0x17000379 RID: 889
		// (get) Token: 0x06000A34 RID: 2612 RVA: 0x00020895 File Offset: 0x0001EA95
		// (set) Token: 0x06000A35 RID: 2613 RVA: 0x0002089D File Offset: 0x0001EA9D
		public int FamilyType { get; private set; }

		// Token: 0x1700037A RID: 890
		// (get) Token: 0x06000A36 RID: 2614 RVA: 0x000208A6 File Offset: 0x0001EAA6
		// (set) Token: 0x06000A37 RID: 2615 RVA: 0x000208AE File Offset: 0x0001EAAE
		public sbyte[] IndicesOfRagdollBonesToCheckForCorpses { get; private set; }

		// Token: 0x1700037B RID: 891
		// (get) Token: 0x06000A38 RID: 2616 RVA: 0x000208B7 File Offset: 0x0001EAB7
		// (set) Token: 0x06000A39 RID: 2617 RVA: 0x000208BF File Offset: 0x0001EABF
		public sbyte[] RagdollFallSoundBoneIndices { get; private set; }

		// Token: 0x1700037C RID: 892
		// (get) Token: 0x06000A3A RID: 2618 RVA: 0x000208C8 File Offset: 0x0001EAC8
		// (set) Token: 0x06000A3B RID: 2619 RVA: 0x000208D0 File Offset: 0x0001EAD0
		public sbyte HeadLookDirectionBoneIndex { get; private set; }

		// Token: 0x1700037D RID: 893
		// (get) Token: 0x06000A3C RID: 2620 RVA: 0x000208D9 File Offset: 0x0001EAD9
		// (set) Token: 0x06000A3D RID: 2621 RVA: 0x000208E1 File Offset: 0x0001EAE1
		public sbyte SpineLowerBoneIndex { get; private set; }

		// Token: 0x1700037E RID: 894
		// (get) Token: 0x06000A3E RID: 2622 RVA: 0x000208EA File Offset: 0x0001EAEA
		// (set) Token: 0x06000A3F RID: 2623 RVA: 0x000208F2 File Offset: 0x0001EAF2
		public sbyte SpineUpperBoneIndex { get; private set; }

		// Token: 0x1700037F RID: 895
		// (get) Token: 0x06000A40 RID: 2624 RVA: 0x000208FB File Offset: 0x0001EAFB
		// (set) Token: 0x06000A41 RID: 2625 RVA: 0x00020903 File Offset: 0x0001EB03
		public sbyte ThoraxLookDirectionBoneIndex { get; private set; }

		// Token: 0x17000380 RID: 896
		// (get) Token: 0x06000A42 RID: 2626 RVA: 0x0002090C File Offset: 0x0001EB0C
		// (set) Token: 0x06000A43 RID: 2627 RVA: 0x00020914 File Offset: 0x0001EB14
		public sbyte NeckRootBoneIndex { get; private set; }

		// Token: 0x17000381 RID: 897
		// (get) Token: 0x06000A44 RID: 2628 RVA: 0x0002091D File Offset: 0x0001EB1D
		// (set) Token: 0x06000A45 RID: 2629 RVA: 0x00020925 File Offset: 0x0001EB25
		public sbyte PelvisBoneIndex { get; private set; }

		// Token: 0x17000382 RID: 898
		// (get) Token: 0x06000A46 RID: 2630 RVA: 0x0002092E File Offset: 0x0001EB2E
		// (set) Token: 0x06000A47 RID: 2631 RVA: 0x00020936 File Offset: 0x0001EB36
		public sbyte RightUpperArmBoneIndex { get; private set; }

		// Token: 0x17000383 RID: 899
		// (get) Token: 0x06000A48 RID: 2632 RVA: 0x0002093F File Offset: 0x0001EB3F
		// (set) Token: 0x06000A49 RID: 2633 RVA: 0x00020947 File Offset: 0x0001EB47
		public sbyte LeftUpperArmBoneIndex { get; private set; }

		// Token: 0x17000384 RID: 900
		// (get) Token: 0x06000A4A RID: 2634 RVA: 0x00020950 File Offset: 0x0001EB50
		// (set) Token: 0x06000A4B RID: 2635 RVA: 0x00020958 File Offset: 0x0001EB58
		public sbyte FallBlowDamageBoneIndex { get; private set; }

		// Token: 0x17000385 RID: 901
		// (get) Token: 0x06000A4C RID: 2636 RVA: 0x00020961 File Offset: 0x0001EB61
		// (set) Token: 0x06000A4D RID: 2637 RVA: 0x00020969 File Offset: 0x0001EB69
		public sbyte TerrainDecalBone0Index { get; private set; }

		// Token: 0x17000386 RID: 902
		// (get) Token: 0x06000A4E RID: 2638 RVA: 0x00020972 File Offset: 0x0001EB72
		// (set) Token: 0x06000A4F RID: 2639 RVA: 0x0002097A File Offset: 0x0001EB7A
		public sbyte TerrainDecalBone1Index { get; private set; }

		// Token: 0x17000387 RID: 903
		// (get) Token: 0x06000A50 RID: 2640 RVA: 0x00020983 File Offset: 0x0001EB83
		// (set) Token: 0x06000A51 RID: 2641 RVA: 0x0002098B File Offset: 0x0001EB8B
		public sbyte[] RagdollStationaryCheckBoneIndices { get; private set; }

		// Token: 0x17000388 RID: 904
		// (get) Token: 0x06000A52 RID: 2642 RVA: 0x00020994 File Offset: 0x0001EB94
		// (set) Token: 0x06000A53 RID: 2643 RVA: 0x0002099C File Offset: 0x0001EB9C
		public sbyte[] MoveAdderBoneIndices { get; private set; }

		// Token: 0x17000389 RID: 905
		// (get) Token: 0x06000A54 RID: 2644 RVA: 0x000209A5 File Offset: 0x0001EBA5
		// (set) Token: 0x06000A55 RID: 2645 RVA: 0x000209AD File Offset: 0x0001EBAD
		public sbyte[] SplashDecalBoneIndices { get; private set; }

		// Token: 0x1700038A RID: 906
		// (get) Token: 0x06000A56 RID: 2646 RVA: 0x000209B6 File Offset: 0x0001EBB6
		// (set) Token: 0x06000A57 RID: 2647 RVA: 0x000209BE File Offset: 0x0001EBBE
		public sbyte[] BloodBurstBoneIndices { get; private set; }

		// Token: 0x1700038B RID: 907
		// (get) Token: 0x06000A58 RID: 2648 RVA: 0x000209C7 File Offset: 0x0001EBC7
		// (set) Token: 0x06000A59 RID: 2649 RVA: 0x000209CF File Offset: 0x0001EBCF
		public sbyte MainHandBoneIndex { get; private set; }

		// Token: 0x1700038C RID: 908
		// (get) Token: 0x06000A5A RID: 2650 RVA: 0x000209D8 File Offset: 0x0001EBD8
		// (set) Token: 0x06000A5B RID: 2651 RVA: 0x000209E0 File Offset: 0x0001EBE0
		public sbyte OffHandBoneIndex { get; private set; }

		// Token: 0x1700038D RID: 909
		// (get) Token: 0x06000A5C RID: 2652 RVA: 0x000209E9 File Offset: 0x0001EBE9
		// (set) Token: 0x06000A5D RID: 2653 RVA: 0x000209F1 File Offset: 0x0001EBF1
		public sbyte MainHandItemBoneIndex { get; private set; }

		// Token: 0x1700038E RID: 910
		// (get) Token: 0x06000A5E RID: 2654 RVA: 0x000209FA File Offset: 0x0001EBFA
		// (set) Token: 0x06000A5F RID: 2655 RVA: 0x00020A02 File Offset: 0x0001EC02
		public sbyte OffHandItemBoneIndex { get; private set; }

		// Token: 0x1700038F RID: 911
		// (get) Token: 0x06000A60 RID: 2656 RVA: 0x00020A0B File Offset: 0x0001EC0B
		// (set) Token: 0x06000A61 RID: 2657 RVA: 0x00020A13 File Offset: 0x0001EC13
		public sbyte MainHandItemSecondaryBoneIndex { get; private set; }

		// Token: 0x17000390 RID: 912
		// (get) Token: 0x06000A62 RID: 2658 RVA: 0x00020A1C File Offset: 0x0001EC1C
		// (set) Token: 0x06000A63 RID: 2659 RVA: 0x00020A24 File Offset: 0x0001EC24
		public sbyte OffHandItemSecondaryBoneIndex { get; private set; }

		// Token: 0x17000391 RID: 913
		// (get) Token: 0x06000A64 RID: 2660 RVA: 0x00020A2D File Offset: 0x0001EC2D
		// (set) Token: 0x06000A65 RID: 2661 RVA: 0x00020A35 File Offset: 0x0001EC35
		public sbyte OffHandShoulderBoneIndex { get; private set; }

		// Token: 0x17000392 RID: 914
		// (get) Token: 0x06000A66 RID: 2662 RVA: 0x00020A3E File Offset: 0x0001EC3E
		// (set) Token: 0x06000A67 RID: 2663 RVA: 0x00020A46 File Offset: 0x0001EC46
		public sbyte HandNumBonesForIk { get; private set; }

		// Token: 0x17000393 RID: 915
		// (get) Token: 0x06000A68 RID: 2664 RVA: 0x00020A4F File Offset: 0x0001EC4F
		// (set) Token: 0x06000A69 RID: 2665 RVA: 0x00020A57 File Offset: 0x0001EC57
		public sbyte PrimaryFootBoneIndex { get; private set; }

		// Token: 0x17000394 RID: 916
		// (get) Token: 0x06000A6A RID: 2666 RVA: 0x00020A60 File Offset: 0x0001EC60
		// (set) Token: 0x06000A6B RID: 2667 RVA: 0x00020A68 File Offset: 0x0001EC68
		public sbyte SecondaryFootBoneIndex { get; private set; }

		// Token: 0x17000395 RID: 917
		// (get) Token: 0x06000A6C RID: 2668 RVA: 0x00020A71 File Offset: 0x0001EC71
		// (set) Token: 0x06000A6D RID: 2669 RVA: 0x00020A79 File Offset: 0x0001EC79
		public sbyte RightFootIkEndEffectorBoneIndex { get; private set; }

		// Token: 0x17000396 RID: 918
		// (get) Token: 0x06000A6E RID: 2670 RVA: 0x00020A82 File Offset: 0x0001EC82
		// (set) Token: 0x06000A6F RID: 2671 RVA: 0x00020A8A File Offset: 0x0001EC8A
		public sbyte LeftFootIkEndEffectorBoneIndex { get; private set; }

		// Token: 0x17000397 RID: 919
		// (get) Token: 0x06000A70 RID: 2672 RVA: 0x00020A93 File Offset: 0x0001EC93
		// (set) Token: 0x06000A71 RID: 2673 RVA: 0x00020A9B File Offset: 0x0001EC9B
		public sbyte RightFootIkTipBoneIndex { get; private set; }

		// Token: 0x17000398 RID: 920
		// (get) Token: 0x06000A72 RID: 2674 RVA: 0x00020AA4 File Offset: 0x0001ECA4
		// (set) Token: 0x06000A73 RID: 2675 RVA: 0x00020AAC File Offset: 0x0001ECAC
		public sbyte LeftFootIkTipBoneIndex { get; private set; }

		// Token: 0x17000399 RID: 921
		// (get) Token: 0x06000A74 RID: 2676 RVA: 0x00020AB5 File Offset: 0x0001ECB5
		// (set) Token: 0x06000A75 RID: 2677 RVA: 0x00020ABD File Offset: 0x0001ECBD
		public sbyte FootNumBonesForIk { get; private set; }

		// Token: 0x1700039A RID: 922
		// (get) Token: 0x06000A76 RID: 2678 RVA: 0x00020AC6 File Offset: 0x0001ECC6
		// (set) Token: 0x06000A77 RID: 2679 RVA: 0x00020ACE File Offset: 0x0001ECCE
		public Vec3 ReinHandleLeftLocalPosition { get; private set; }

		// Token: 0x1700039B RID: 923
		// (get) Token: 0x06000A78 RID: 2680 RVA: 0x00020AD7 File Offset: 0x0001ECD7
		// (set) Token: 0x06000A79 RID: 2681 RVA: 0x00020ADF File Offset: 0x0001ECDF
		public Vec3 ReinHandleRightLocalPosition { get; private set; }

		// Token: 0x1700039C RID: 924
		// (get) Token: 0x06000A7A RID: 2682 RVA: 0x00020AE8 File Offset: 0x0001ECE8
		// (set) Token: 0x06000A7B RID: 2683 RVA: 0x00020AF0 File Offset: 0x0001ECF0
		public string ReinSkeleton { get; private set; }

		// Token: 0x1700039D RID: 925
		// (get) Token: 0x06000A7C RID: 2684 RVA: 0x00020AF9 File Offset: 0x0001ECF9
		// (set) Token: 0x06000A7D RID: 2685 RVA: 0x00020B01 File Offset: 0x0001ED01
		public string ReinCollisionBody { get; private set; }

		// Token: 0x1700039E RID: 926
		// (get) Token: 0x06000A7E RID: 2686 RVA: 0x00020B0A File Offset: 0x0001ED0A
		// (set) Token: 0x06000A7F RID: 2687 RVA: 0x00020B12 File Offset: 0x0001ED12
		public sbyte FrontBoneToDetectGroundSlopeIndex { get; private set; }

		// Token: 0x1700039F RID: 927
		// (get) Token: 0x06000A80 RID: 2688 RVA: 0x00020B1B File Offset: 0x0001ED1B
		// (set) Token: 0x06000A81 RID: 2689 RVA: 0x00020B23 File Offset: 0x0001ED23
		public sbyte BackBoneToDetectGroundSlopeIndex { get; private set; }

		// Token: 0x170003A0 RID: 928
		// (get) Token: 0x06000A82 RID: 2690 RVA: 0x00020B2C File Offset: 0x0001ED2C
		// (set) Token: 0x06000A83 RID: 2691 RVA: 0x00020B34 File Offset: 0x0001ED34
		public sbyte[] BoneIndicesToModifyOnSlopingGround { get; private set; }

		// Token: 0x170003A1 RID: 929
		// (get) Token: 0x06000A84 RID: 2692 RVA: 0x00020B3D File Offset: 0x0001ED3D
		// (set) Token: 0x06000A85 RID: 2693 RVA: 0x00020B45 File Offset: 0x0001ED45
		public sbyte BodyRotationReferenceBoneIndex { get; private set; }

		// Token: 0x170003A2 RID: 930
		// (get) Token: 0x06000A86 RID: 2694 RVA: 0x00020B4E File Offset: 0x0001ED4E
		// (set) Token: 0x06000A87 RID: 2695 RVA: 0x00020B56 File Offset: 0x0001ED56
		public sbyte RiderSitBoneIndex { get; private set; }

		// Token: 0x170003A3 RID: 931
		// (get) Token: 0x06000A88 RID: 2696 RVA: 0x00020B5F File Offset: 0x0001ED5F
		// (set) Token: 0x06000A89 RID: 2697 RVA: 0x00020B67 File Offset: 0x0001ED67
		public sbyte ReinHandleBoneIndex { get; private set; }

		// Token: 0x170003A4 RID: 932
		// (get) Token: 0x06000A8A RID: 2698 RVA: 0x00020B70 File Offset: 0x0001ED70
		// (set) Token: 0x06000A8B RID: 2699 RVA: 0x00020B78 File Offset: 0x0001ED78
		public sbyte ReinCollision1BoneIndex { get; private set; }

		// Token: 0x170003A5 RID: 933
		// (get) Token: 0x06000A8C RID: 2700 RVA: 0x00020B81 File Offset: 0x0001ED81
		// (set) Token: 0x06000A8D RID: 2701 RVA: 0x00020B89 File Offset: 0x0001ED89
		public sbyte ReinCollision2BoneIndex { get; private set; }

		// Token: 0x170003A6 RID: 934
		// (get) Token: 0x06000A8E RID: 2702 RVA: 0x00020B92 File Offset: 0x0001ED92
		// (set) Token: 0x06000A8F RID: 2703 RVA: 0x00020B9A File Offset: 0x0001ED9A
		public sbyte ReinHeadBoneIndex { get; private set; }

		// Token: 0x170003A7 RID: 935
		// (get) Token: 0x06000A90 RID: 2704 RVA: 0x00020BA3 File Offset: 0x0001EDA3
		// (set) Token: 0x06000A91 RID: 2705 RVA: 0x00020BAB File Offset: 0x0001EDAB
		public sbyte ReinHeadRightAttachmentBoneIndex { get; private set; }

		// Token: 0x170003A8 RID: 936
		// (get) Token: 0x06000A92 RID: 2706 RVA: 0x00020BB4 File Offset: 0x0001EDB4
		// (set) Token: 0x06000A93 RID: 2707 RVA: 0x00020BBC File Offset: 0x0001EDBC
		public sbyte ReinHeadLeftAttachmentBoneIndex { get; private set; }

		// Token: 0x170003A9 RID: 937
		// (get) Token: 0x06000A94 RID: 2708 RVA: 0x00020BC5 File Offset: 0x0001EDC5
		// (set) Token: 0x06000A95 RID: 2709 RVA: 0x00020BCD File Offset: 0x0001EDCD
		public sbyte ReinRightHandBoneIndex { get; private set; }

		// Token: 0x170003AA RID: 938
		// (get) Token: 0x06000A96 RID: 2710 RVA: 0x00020BD6 File Offset: 0x0001EDD6
		// (set) Token: 0x06000A97 RID: 2711 RVA: 0x00020BDE File Offset: 0x0001EDDE
		public sbyte ReinLeftHandBoneIndex { get; private set; }

		// Token: 0x170003AB RID: 939
		// (get) Token: 0x06000A98 RID: 2712 RVA: 0x00020BE8 File Offset: 0x0001EDE8
		[CachedData]
		public IMonsterMissionData MonsterMissionData
		{
			get
			{
				IMonsterMissionData result;
				if ((result = this._monsterMissionData) == null)
				{
					result = (this._monsterMissionData = Game.Current.MonsterMissionDataCreator.CreateMonsterMissionData(this));
				}
				return result;
			}
		}

		// Token: 0x06000A99 RID: 2713 RVA: 0x00020C18 File Offset: 0x0001EE18
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			bool flag = false;
			XmlAttribute xmlAttribute = node.Attributes["base_monster"];
			List<sbyte> list;
			List<sbyte> list2;
			List<sbyte> list3;
			List<sbyte> list4;
			List<sbyte> list5;
			List<sbyte> list6;
			List<sbyte> list7;
			if (xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.Value))
			{
				flag = true;
				this.BaseMonster = xmlAttribute.Value;
				Monster @object = objectManager.GetObject<Monster>(this.BaseMonster);
				if (!string.IsNullOrEmpty(@object.BaseMonster))
				{
					this.BaseMonster = @object.BaseMonster;
				}
				this.BodyCapsuleRadius = @object.BodyCapsuleRadius;
				this.BodyCapsulePoint1 = @object.BodyCapsulePoint1;
				this.BodyCapsulePoint2 = @object.BodyCapsulePoint2;
				this.CrouchedBodyCapsuleRadius = @object.CrouchedBodyCapsuleRadius;
				this.CrouchedBodyCapsulePoint1 = @object.CrouchedBodyCapsulePoint1;
				this.CrouchedBodyCapsulePoint2 = @object.CrouchedBodyCapsulePoint2;
				this.Flags = @object.Flags;
				this.Weight = @object.Weight;
				this.HitPoints = @object.HitPoints;
				this.ActionSetCode = @object.ActionSetCode;
				this.FemaleActionSetCode = @object.FemaleActionSetCode;
				this.MonsterUsage = @object.MonsterUsage;
				this.NumPaces = @object.NumPaces;
				this.WalkingSpeedLimit = @object.WalkingSpeedLimit;
				this.CrouchWalkingSpeedLimit = @object.CrouchWalkingSpeedLimit;
				this.JumpAcceleration = @object.JumpAcceleration;
				this.AbsorbedDamageRatio = @object.AbsorbedDamageRatio;
				this.SoundAndCollisionInfoClassName = @object.SoundAndCollisionInfoClassName;
				this.RiderCameraHeightAdder = @object.RiderCameraHeightAdder;
				this.RiderBodyCapsuleHeightAdder = @object.RiderBodyCapsuleHeightAdder;
				this.RiderBodyCapsuleForwardAdder = @object.RiderBodyCapsuleForwardAdder;
				this.StandingChestHeight = @object.StandingChestHeight;
				this.StandingPelvisHeight = @object.StandingPelvisHeight;
				this.StandingEyeHeight = @object.StandingEyeHeight;
				this.CrouchEyeHeight = @object.CrouchEyeHeight;
				this.MountedEyeHeight = @object.MountedEyeHeight;
				this.RiderEyeHeightAdder = @object.RiderEyeHeightAdder;
				this.EyeOffsetWrtHead = @object.EyeOffsetWrtHead;
				this.FirstPersonCameraOffsetWrtHead = @object.FirstPersonCameraOffsetWrtHead;
				this.ArmLength = @object.ArmLength;
				this.ArmWeight = @object.ArmWeight;
				this.JumpSpeedLimit = @object.JumpSpeedLimit;
				this.RelativeSpeedLimitForCharge = @object.RelativeSpeedLimitForCharge;
				this.FamilyType = @object.FamilyType;
				list = new List<sbyte>(@object.IndicesOfRagdollBonesToCheckForCorpses);
				list2 = new List<sbyte>(@object.RagdollFallSoundBoneIndices);
				this.HeadLookDirectionBoneIndex = @object.HeadLookDirectionBoneIndex;
				this.SpineLowerBoneIndex = @object.SpineLowerBoneIndex;
				this.SpineUpperBoneIndex = @object.SpineUpperBoneIndex;
				this.ThoraxLookDirectionBoneIndex = @object.ThoraxLookDirectionBoneIndex;
				this.NeckRootBoneIndex = @object.NeckRootBoneIndex;
				this.PelvisBoneIndex = @object.PelvisBoneIndex;
				this.RightUpperArmBoneIndex = @object.RightUpperArmBoneIndex;
				this.LeftUpperArmBoneIndex = @object.LeftUpperArmBoneIndex;
				this.FallBlowDamageBoneIndex = @object.FallBlowDamageBoneIndex;
				this.TerrainDecalBone0Index = @object.TerrainDecalBone0Index;
				this.TerrainDecalBone1Index = @object.TerrainDecalBone1Index;
				list3 = new List<sbyte>(@object.RagdollStationaryCheckBoneIndices);
				list4 = new List<sbyte>(@object.MoveAdderBoneIndices);
				list5 = new List<sbyte>(@object.SplashDecalBoneIndices);
				list6 = new List<sbyte>(@object.BloodBurstBoneIndices);
				this.MainHandBoneIndex = @object.MainHandBoneIndex;
				this.OffHandBoneIndex = @object.OffHandBoneIndex;
				this.MainHandItemBoneIndex = @object.MainHandItemBoneIndex;
				this.OffHandItemBoneIndex = @object.OffHandItemBoneIndex;
				this.MainHandItemSecondaryBoneIndex = @object.MainHandItemSecondaryBoneIndex;
				this.OffHandItemSecondaryBoneIndex = @object.OffHandItemSecondaryBoneIndex;
				this.OffHandShoulderBoneIndex = @object.OffHandShoulderBoneIndex;
				this.HandNumBonesForIk = @object.HandNumBonesForIk;
				this.PrimaryFootBoneIndex = @object.PrimaryFootBoneIndex;
				this.SecondaryFootBoneIndex = @object.SecondaryFootBoneIndex;
				this.RightFootIkEndEffectorBoneIndex = @object.RightFootIkEndEffectorBoneIndex;
				this.LeftFootIkEndEffectorBoneIndex = @object.LeftFootIkEndEffectorBoneIndex;
				this.RightFootIkTipBoneIndex = @object.RightFootIkTipBoneIndex;
				this.LeftFootIkTipBoneIndex = @object.LeftFootIkTipBoneIndex;
				this.FootNumBonesForIk = @object.FootNumBonesForIk;
				this.ReinHandleLeftLocalPosition = @object.ReinHandleLeftLocalPosition;
				this.ReinHandleRightLocalPosition = @object.ReinHandleRightLocalPosition;
				this.ReinSkeleton = @object.ReinSkeleton;
				this.ReinCollisionBody = @object.ReinCollisionBody;
				this.FrontBoneToDetectGroundSlopeIndex = @object.FrontBoneToDetectGroundSlopeIndex;
				this.BackBoneToDetectGroundSlopeIndex = @object.BackBoneToDetectGroundSlopeIndex;
				list7 = new List<sbyte>(@object.BoneIndicesToModifyOnSlopingGround);
				this.BodyRotationReferenceBoneIndex = @object.BodyRotationReferenceBoneIndex;
				this.RiderSitBoneIndex = @object.RiderSitBoneIndex;
				this.ReinHandleBoneIndex = @object.ReinHandleBoneIndex;
				this.ReinCollision1BoneIndex = @object.ReinCollision1BoneIndex;
				this.ReinCollision2BoneIndex = @object.ReinCollision2BoneIndex;
				this.ReinHeadBoneIndex = @object.ReinHeadBoneIndex;
				this.ReinHeadRightAttachmentBoneIndex = @object.ReinHeadRightAttachmentBoneIndex;
				this.ReinHeadLeftAttachmentBoneIndex = @object.ReinHeadLeftAttachmentBoneIndex;
				this.ReinRightHandBoneIndex = @object.ReinRightHandBoneIndex;
				this.ReinLeftHandBoneIndex = @object.ReinLeftHandBoneIndex;
			}
			else
			{
				list = new List<sbyte>(12);
				list2 = new List<sbyte>(4);
				list3 = new List<sbyte>(8);
				list4 = new List<sbyte>(8);
				list5 = new List<sbyte>(8);
				list6 = new List<sbyte>(8);
				list7 = new List<sbyte>(8);
			}
			XmlAttribute xmlAttribute2 = node.Attributes["action_set"];
			if (xmlAttribute2 != null && !string.IsNullOrEmpty(xmlAttribute2.Value))
			{
				this.ActionSetCode = xmlAttribute2.Value;
			}
			XmlAttribute xmlAttribute3 = node.Attributes["female_action_set"];
			if (xmlAttribute3 != null && !string.IsNullOrEmpty(xmlAttribute3.Value))
			{
				this.FemaleActionSetCode = xmlAttribute3.Value;
			}
			XmlAttribute xmlAttribute4 = node.Attributes["monster_usage"];
			if (xmlAttribute4 != null && !string.IsNullOrEmpty(xmlAttribute4.Value))
			{
				this.MonsterUsage = xmlAttribute4.Value;
			}
			else if (!flag)
			{
				this.MonsterUsage = "";
			}
			if (!flag)
			{
				this.Weight = 1;
			}
			XmlAttribute xmlAttribute5 = node.Attributes["weight"];
			int weight;
			if (xmlAttribute5 != null && !string.IsNullOrEmpty(xmlAttribute5.Value) && int.TryParse(xmlAttribute5.Value, out weight))
			{
				this.Weight = weight;
			}
			if (!flag)
			{
				this.HitPoints = 1;
			}
			XmlAttribute xmlAttribute6 = node.Attributes["hit_points"];
			int hitPoints;
			if (xmlAttribute6 != null && !string.IsNullOrEmpty(xmlAttribute6.Value) && int.TryParse(xmlAttribute6.Value, out hitPoints))
			{
				this.HitPoints = hitPoints;
			}
			XmlAttribute xmlAttribute7 = node.Attributes["num_paces"];
			int numPaces;
			if (xmlAttribute7 != null && !string.IsNullOrEmpty(xmlAttribute7.Value) && int.TryParse(xmlAttribute7.Value, out numPaces))
			{
				this.NumPaces = numPaces;
			}
			XmlAttribute xmlAttribute8 = node.Attributes["walking_speed_limit"];
			float walkingSpeedLimit;
			if (xmlAttribute8 != null && !string.IsNullOrEmpty(xmlAttribute8.Value) && float.TryParse(xmlAttribute8.Value, out walkingSpeedLimit))
			{
				this.WalkingSpeedLimit = walkingSpeedLimit;
			}
			XmlAttribute xmlAttribute9 = node.Attributes["crouch_walking_speed_limit"];
			if (xmlAttribute9 != null && !string.IsNullOrEmpty(xmlAttribute9.Value))
			{
				float crouchWalkingSpeedLimit;
				if (float.TryParse(xmlAttribute9.Value, out crouchWalkingSpeedLimit))
				{
					this.CrouchWalkingSpeedLimit = crouchWalkingSpeedLimit;
				}
			}
			else if (!flag)
			{
				this.CrouchWalkingSpeedLimit = this.WalkingSpeedLimit;
			}
			XmlAttribute xmlAttribute10 = node.Attributes["jump_acceleration"];
			float jumpAcceleration;
			if (xmlAttribute10 != null && !string.IsNullOrEmpty(xmlAttribute10.Value) && float.TryParse(xmlAttribute10.Value, out jumpAcceleration))
			{
				this.JumpAcceleration = jumpAcceleration;
			}
			XmlAttribute xmlAttribute11 = node.Attributes["absorbed_damage_ratio"];
			if (xmlAttribute11 != null && !string.IsNullOrEmpty(xmlAttribute11.Value))
			{
				float num;
				if (float.TryParse(xmlAttribute11.Value, out num))
				{
					if (num < 0f)
					{
						num = 0f;
					}
					this.AbsorbedDamageRatio = num;
				}
			}
			else if (!flag)
			{
				this.AbsorbedDamageRatio = 1f;
			}
			XmlAttribute xmlAttribute12 = node.Attributes["sound_and_collision_info_class"];
			if (xmlAttribute12 != null && !string.IsNullOrEmpty(xmlAttribute12.Value))
			{
				this.SoundAndCollisionInfoClassName = xmlAttribute12.Value;
			}
			XmlAttribute xmlAttribute13 = node.Attributes["rider_camera_height_adder"];
			float riderCameraHeightAdder;
			if (xmlAttribute13 != null && !string.IsNullOrEmpty(xmlAttribute13.Value) && float.TryParse(xmlAttribute13.Value, out riderCameraHeightAdder))
			{
				this.RiderCameraHeightAdder = riderCameraHeightAdder;
			}
			XmlAttribute xmlAttribute14 = node.Attributes["rider_body_capsule_height_adder"];
			float riderBodyCapsuleHeightAdder;
			if (xmlAttribute14 != null && !string.IsNullOrEmpty(xmlAttribute14.Value) && float.TryParse(xmlAttribute14.Value, out riderBodyCapsuleHeightAdder))
			{
				this.RiderBodyCapsuleHeightAdder = riderBodyCapsuleHeightAdder;
			}
			XmlAttribute xmlAttribute15 = node.Attributes["rider_body_capsule_forward_adder"];
			float riderBodyCapsuleForwardAdder;
			if (xmlAttribute15 != null && !string.IsNullOrEmpty(xmlAttribute15.Value) && float.TryParse(xmlAttribute15.Value, out riderBodyCapsuleForwardAdder))
			{
				this.RiderBodyCapsuleForwardAdder = riderBodyCapsuleForwardAdder;
			}
			XmlAttribute xmlAttribute16 = node.Attributes["preliminary_collision_capsule_radius_multiplier"];
			if (!flag && xmlAttribute16 != null && !string.IsNullOrEmpty(xmlAttribute16.Value))
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 433);
			}
			XmlAttribute xmlAttribute17 = node.Attributes["rider_preliminary_collision_capsule_height_multiplier"];
			if (!flag && xmlAttribute17 != null && !string.IsNullOrEmpty(xmlAttribute17.Value))
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 442);
			}
			XmlAttribute xmlAttribute18 = node.Attributes["rider_preliminary_collision_capsule_height_adder"];
			if (!flag && xmlAttribute18 != null && !string.IsNullOrEmpty(xmlAttribute18.Value))
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 451);
			}
			XmlAttribute xmlAttribute19 = node.Attributes["standing_chest_height"];
			float standingChestHeight;
			if (xmlAttribute19 != null && !string.IsNullOrEmpty(xmlAttribute19.Value) && float.TryParse(xmlAttribute19.Value, out standingChestHeight))
			{
				this.StandingChestHeight = standingChestHeight;
			}
			XmlAttribute xmlAttribute20 = node.Attributes["standing_pelvis_height"];
			float standingPelvisHeight;
			if (xmlAttribute20 != null && !string.IsNullOrEmpty(xmlAttribute20.Value) && float.TryParse(xmlAttribute20.Value, out standingPelvisHeight))
			{
				this.StandingPelvisHeight = standingPelvisHeight;
			}
			XmlAttribute xmlAttribute21 = node.Attributes["standing_eye_height"];
			float standingEyeHeight;
			if (xmlAttribute21 != null && !string.IsNullOrEmpty(xmlAttribute21.Value) && float.TryParse(xmlAttribute21.Value, out standingEyeHeight))
			{
				this.StandingEyeHeight = standingEyeHeight;
			}
			XmlAttribute xmlAttribute22 = node.Attributes["crouch_eye_height"];
			float crouchEyeHeight;
			if (xmlAttribute22 != null && !string.IsNullOrEmpty(xmlAttribute22.Value) && float.TryParse(xmlAttribute22.Value, out crouchEyeHeight))
			{
				this.CrouchEyeHeight = crouchEyeHeight;
			}
			XmlAttribute xmlAttribute23 = node.Attributes["mounted_eye_height"];
			float mountedEyeHeight;
			if (xmlAttribute23 != null && !string.IsNullOrEmpty(xmlAttribute23.Value) && float.TryParse(xmlAttribute23.Value, out mountedEyeHeight))
			{
				this.MountedEyeHeight = mountedEyeHeight;
			}
			XmlAttribute xmlAttribute24 = node.Attributes["rider_eye_height_adder"];
			float riderEyeHeightAdder;
			if (xmlAttribute24 != null && !string.IsNullOrEmpty(xmlAttribute24.Value) && float.TryParse(xmlAttribute24.Value, out riderEyeHeightAdder))
			{
				this.RiderEyeHeightAdder = riderEyeHeightAdder;
			}
			if (!flag)
			{
				this.EyeOffsetWrtHead = new Vec3(0.01f, 0.01f, 0.01f, -1f);
			}
			XmlAttribute xmlAttribute25 = node.Attributes["eye_offset_wrt_head"];
			Vec3 eyeOffsetWrtHead;
			if (xmlAttribute25 != null && !string.IsNullOrEmpty(xmlAttribute25.Value) && Monster.ReadVec3(xmlAttribute25.Value, out eyeOffsetWrtHead))
			{
				this.EyeOffsetWrtHead = eyeOffsetWrtHead;
			}
			if (!flag)
			{
				this.FirstPersonCameraOffsetWrtHead = new Vec3(0.01f, 0.01f, 0.01f, -1f);
			}
			XmlAttribute xmlAttribute26 = node.Attributes["first_person_camera_offset_wrt_head"];
			Vec3 firstPersonCameraOffsetWrtHead;
			if (xmlAttribute26 != null && !string.IsNullOrEmpty(xmlAttribute26.Value) && Monster.ReadVec3(xmlAttribute26.Value, out firstPersonCameraOffsetWrtHead))
			{
				this.FirstPersonCameraOffsetWrtHead = firstPersonCameraOffsetWrtHead;
			}
			XmlAttribute xmlAttribute27 = node.Attributes["arm_length"];
			float armLength;
			if (xmlAttribute27 != null && !string.IsNullOrEmpty(xmlAttribute27.Value) && float.TryParse(xmlAttribute27.Value, out armLength))
			{
				this.ArmLength = armLength;
			}
			XmlAttribute xmlAttribute28 = node.Attributes["arm_weight"];
			float armWeight;
			if (xmlAttribute28 != null && !string.IsNullOrEmpty(xmlAttribute28.Value) && float.TryParse(xmlAttribute28.Value, out armWeight))
			{
				this.ArmWeight = armWeight;
			}
			XmlAttribute xmlAttribute29 = node.Attributes["jump_speed_limit"];
			float jumpSpeedLimit;
			if (xmlAttribute29 != null && !string.IsNullOrEmpty(xmlAttribute29.Value) && float.TryParse(xmlAttribute29.Value, out jumpSpeedLimit))
			{
				this.JumpSpeedLimit = jumpSpeedLimit;
			}
			if (!flag)
			{
				this.RelativeSpeedLimitForCharge = float.MaxValue;
			}
			XmlAttribute xmlAttribute30 = node.Attributes["relative_speed_limit_for_charge"];
			float relativeSpeedLimitForCharge;
			if (xmlAttribute30 != null && !string.IsNullOrEmpty(xmlAttribute30.Value) && float.TryParse(xmlAttribute30.Value, out relativeSpeedLimitForCharge))
			{
				this.RelativeSpeedLimitForCharge = relativeSpeedLimitForCharge;
			}
			XmlAttribute xmlAttribute31 = node.Attributes["family_type"];
			int familyType;
			if (xmlAttribute31 != null && !string.IsNullOrEmpty(xmlAttribute31.Value) && int.TryParse(xmlAttribute31.Value, out familyType))
			{
				this.FamilyType = familyType;
			}
			sbyte b = -1;
			this.DeserializeBoneIndexArray(list, node, flag, "ragdoll_bone_to_check_for_corpses_", b, false);
			this.DeserializeBoneIndexArray(list2, node, flag, "ragdoll_fall_sound_bone_", b, false);
			this.HeadLookDirectionBoneIndex = this.DeserializeBoneIndex(node, "head_look_direction_bone", flag ? this.HeadLookDirectionBoneIndex : b, b, true);
			this.SpineLowerBoneIndex = this.DeserializeBoneIndex(node, "spine_lower_bone", flag ? this.SpineLowerBoneIndex : b, b, false);
			this.SpineUpperBoneIndex = this.DeserializeBoneIndex(node, "spine_upper_bone", flag ? this.SpineUpperBoneIndex : b, b, false);
			this.ThoraxLookDirectionBoneIndex = this.DeserializeBoneIndex(node, "thorax_look_direction_bone", flag ? this.ThoraxLookDirectionBoneIndex : b, b, true);
			this.NeckRootBoneIndex = this.DeserializeBoneIndex(node, "neck_root_bone", flag ? this.NeckRootBoneIndex : b, b, true);
			this.PelvisBoneIndex = this.DeserializeBoneIndex(node, "pelvis_bone", flag ? this.PelvisBoneIndex : b, b, false);
			this.RightUpperArmBoneIndex = this.DeserializeBoneIndex(node, "right_upper_arm_bone", flag ? this.RightUpperArmBoneIndex : b, b, false);
			this.LeftUpperArmBoneIndex = this.DeserializeBoneIndex(node, "left_upper_arm_bone", flag ? this.LeftUpperArmBoneIndex : b, b, false);
			this.FallBlowDamageBoneIndex = this.DeserializeBoneIndex(node, "fall_blow_damage_bone", flag ? this.FallBlowDamageBoneIndex : b, b, false);
			this.TerrainDecalBone0Index = this.DeserializeBoneIndex(node, "terrain_decal_bone_0", flag ? this.TerrainDecalBone0Index : b, b, false);
			this.TerrainDecalBone1Index = this.DeserializeBoneIndex(node, "terrain_decal_bone_1", flag ? this.TerrainDecalBone1Index : b, b, false);
			this.DeserializeBoneIndexArray(list3, node, flag, "ragdoll_stationary_check_bone_", b, false);
			this.DeserializeBoneIndexArray(list4, node, flag, "move_adder_bone_", b, false);
			this.DeserializeBoneIndexArray(list5, node, flag, "splash_decal_bone_", b, false);
			this.DeserializeBoneIndexArray(list6, node, flag, "blood_burst_bone_", b, false);
			this.MainHandBoneIndex = this.DeserializeBoneIndex(node, "main_hand_bone", flag ? this.MainHandBoneIndex : b, b, true);
			this.OffHandBoneIndex = this.DeserializeBoneIndex(node, "off_hand_bone", flag ? this.OffHandBoneIndex : b, b, true);
			this.MainHandItemBoneIndex = this.DeserializeBoneIndex(node, "main_hand_item_bone", flag ? this.MainHandItemBoneIndex : b, b, true);
			this.OffHandItemBoneIndex = this.DeserializeBoneIndex(node, "off_hand_item_bone", flag ? this.OffHandItemBoneIndex : b, b, true);
			this.MainHandItemSecondaryBoneIndex = this.DeserializeBoneIndex(node, "main_hand_item_secondary_bone", flag ? this.MainHandItemSecondaryBoneIndex : b, b, false);
			this.OffHandItemSecondaryBoneIndex = this.DeserializeBoneIndex(node, "off_hand_item_secondary_bone", flag ? this.OffHandItemSecondaryBoneIndex : b, b, false);
			this.OffHandShoulderBoneIndex = this.DeserializeBoneIndex(node, "off_hand_shoulder_bone", flag ? this.OffHandShoulderBoneIndex : b, b, false);
			XmlAttribute xmlAttribute32 = node.Attributes["hand_num_bones_for_ik"];
			this.HandNumBonesForIk = ((xmlAttribute32 != null) ? sbyte.Parse(xmlAttribute32.Value) : (flag ? this.HandNumBonesForIk : 0));
			this.PrimaryFootBoneIndex = this.DeserializeBoneIndex(node, "primary_foot_bone", flag ? this.PrimaryFootBoneIndex : b, b, false);
			this.SecondaryFootBoneIndex = this.DeserializeBoneIndex(node, "secondary_foot_bone", flag ? this.SecondaryFootBoneIndex : b, b, false);
			this.RightFootIkEndEffectorBoneIndex = this.DeserializeBoneIndex(node, "right_foot_ik_end_effector_bone", flag ? this.RightFootIkEndEffectorBoneIndex : b, b, true);
			this.LeftFootIkEndEffectorBoneIndex = this.DeserializeBoneIndex(node, "left_foot_ik_end_effector_bone", flag ? this.LeftFootIkEndEffectorBoneIndex : b, b, true);
			this.RightFootIkTipBoneIndex = this.DeserializeBoneIndex(node, "right_foot_ik_tip_bone", flag ? this.RightFootIkTipBoneIndex : b, b, true);
			this.LeftFootIkTipBoneIndex = this.DeserializeBoneIndex(node, "left_foot_ik_tip_bone", flag ? this.LeftFootIkTipBoneIndex : b, b, true);
			XmlAttribute xmlAttribute33 = node.Attributes["foot_num_bones_for_ik"];
			this.FootNumBonesForIk = ((xmlAttribute33 != null) ? sbyte.Parse(xmlAttribute33.Value) : (flag ? this.FootNumBonesForIk : 0));
			XmlNode xmlNode = node.Attributes["rein_handle_left_local_pos"];
			Vec3 reinHandleLeftLocalPosition;
			if (xmlNode != null && Monster.ReadVec3(xmlNode.Value, out reinHandleLeftLocalPosition))
			{
				this.ReinHandleLeftLocalPosition = reinHandleLeftLocalPosition;
			}
			XmlNode xmlNode2 = node.Attributes["rein_handle_right_local_pos"];
			Vec3 reinHandleRightLocalPosition;
			if (xmlNode2 != null && Monster.ReadVec3(xmlNode2.Value, out reinHandleRightLocalPosition))
			{
				this.ReinHandleRightLocalPosition = reinHandleRightLocalPosition;
			}
			XmlAttribute xmlAttribute34 = node.Attributes["rein_skeleton"];
			this.ReinSkeleton = ((xmlAttribute34 != null) ? xmlAttribute34.Value : this.ReinSkeleton);
			XmlAttribute xmlAttribute35 = node.Attributes["rein_collision_body"];
			this.ReinCollisionBody = ((xmlAttribute35 != null) ? xmlAttribute35.Value : this.ReinCollisionBody);
			this.DeserializeBoneIndexArray(list7, node, flag, "bones_to_modify_on_sloping_ground_", b, true);
			XmlAttribute xmlAttribute36 = node.Attributes["front_bone_to_detect_ground_slope_index"];
			this.FrontBoneToDetectGroundSlopeIndex = ((xmlAttribute36 != null) ? sbyte.Parse(xmlAttribute36.Value) : (flag ? this.FrontBoneToDetectGroundSlopeIndex : -1));
			XmlAttribute xmlAttribute37 = node.Attributes["back_bone_to_detect_ground_slope_index"];
			this.BackBoneToDetectGroundSlopeIndex = ((xmlAttribute37 != null) ? sbyte.Parse(xmlAttribute37.Value) : (flag ? this.BackBoneToDetectGroundSlopeIndex : -1));
			this.BodyRotationReferenceBoneIndex = this.DeserializeBoneIndex(node, "body_rotation_reference_bone", flag ? this.BodyRotationReferenceBoneIndex : b, b, true);
			this.RiderSitBoneIndex = this.DeserializeBoneIndex(node, "rider_sit_bone", flag ? this.RiderSitBoneIndex : b, b, false);
			this.ReinHandleBoneIndex = this.DeserializeBoneIndex(node, "rein_handle_bone", flag ? this.ReinHandleBoneIndex : b, b, false);
			this.ReinCollision1BoneIndex = this.DeserializeBoneIndex(node, "rein_collision_1_bone", flag ? this.ReinCollision1BoneIndex : b, b, false);
			this.ReinCollision2BoneIndex = this.DeserializeBoneIndex(node, "rein_collision_2_bone", flag ? this.ReinCollision2BoneIndex : b, b, false);
			this.ReinHeadBoneIndex = this.DeserializeBoneIndex(node, "rein_head_bone", flag ? this.ReinHeadBoneIndex : b, b, false);
			this.ReinHeadRightAttachmentBoneIndex = this.DeserializeBoneIndex(node, "rein_head_right_attachment_bone", flag ? this.ReinHeadRightAttachmentBoneIndex : b, b, false);
			this.ReinHeadLeftAttachmentBoneIndex = this.DeserializeBoneIndex(node, "rein_head_left_attachment_bone", flag ? this.ReinHeadLeftAttachmentBoneIndex : b, b, false);
			this.ReinRightHandBoneIndex = this.DeserializeBoneIndex(node, "rein_right_hand_bone", flag ? this.ReinRightHandBoneIndex : b, b, false);
			this.ReinLeftHandBoneIndex = this.DeserializeBoneIndex(node, "rein_left_hand_bone", flag ? this.ReinLeftHandBoneIndex : b, b, false);
			this.IndicesOfRagdollBonesToCheckForCorpses = list.ToArray();
			this.RagdollFallSoundBoneIndices = list2.ToArray();
			this.RagdollStationaryCheckBoneIndices = list3.ToArray();
			this.MoveAdderBoneIndices = list4.ToArray();
			this.SplashDecalBoneIndices = list5.ToArray();
			this.BloodBurstBoneIndices = list6.ToArray();
			this.BoneIndicesToModifyOnSlopingGround = list7.ToArray();
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode3 = (XmlNode)obj;
				if (xmlNode3.Name == "Flags")
				{
					this.Flags = AgentFlag.None;
					using (IEnumerator enumerator2 = Enum.GetValues(typeof(AgentFlag)).GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							object obj2 = enumerator2.Current;
							AgentFlag agentFlag = (AgentFlag)obj2;
							XmlAttribute xmlAttribute38 = xmlNode3.Attributes[agentFlag.ToString()];
							if (xmlAttribute38 != null && !xmlAttribute38.Value.Equals("false", StringComparison.InvariantCultureIgnoreCase))
							{
								this.Flags |= agentFlag;
							}
						}
						continue;
					}
				}
				if (xmlNode3.Name == "Capsules")
				{
					foreach (object obj3 in xmlNode3.ChildNodes)
					{
						XmlNode xmlNode4 = (XmlNode)obj3;
						if (xmlNode4.Attributes != null && (xmlNode4.Name == "preliminary_collision_capsule" || xmlNode4.Name == "body_capsule" || xmlNode4.Name == "crouched_body_capsule"))
						{
							bool flag2 = true;
							Vec3 vec = new Vec3(0f, 0f, 0.01f, -1f);
							Vec3 vec2 = Vec3.Zero;
							float num2 = 0.01f;
							if (xmlNode4.Attributes["pos1"] != null)
							{
								Vec3 vec3;
								flag2 = Monster.ReadVec3(xmlNode4.Attributes["pos1"].Value, out vec3) && flag2;
								if (flag2)
								{
									vec = vec3;
								}
							}
							if (xmlNode4.Attributes["pos2"] != null)
							{
								Vec3 vec4;
								flag2 = Monster.ReadVec3(xmlNode4.Attributes["pos2"].Value, out vec4) && flag2;
								if (flag2)
								{
									vec2 = vec4;
								}
							}
							if (xmlNode4.Attributes["radius"] != null)
							{
								string text = xmlNode4.Attributes["radius"].Value;
								text = text.Trim();
								flag2 = flag2 && float.TryParse(text, out num2);
							}
							if (flag2)
							{
								if (xmlNode4.Name.StartsWith("p"))
								{
									Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 739);
								}
								else if (xmlNode4.Name.StartsWith("c"))
								{
									this.CrouchedBodyCapsuleRadius = num2;
									this.CrouchedBodyCapsulePoint1 = vec;
									this.CrouchedBodyCapsulePoint2 = vec2;
								}
								else
								{
									this.BodyCapsuleRadius = num2;
									this.BodyCapsulePoint1 = vec;
									this.BodyCapsulePoint2 = vec2;
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000A9A RID: 2714 RVA: 0x000222EC File Offset: 0x000204EC
		private sbyte DeserializeBoneIndex(XmlNode node, string attributeName, sbyte baseValue, sbyte invalidBoneIndex, bool validateHasParentBone)
		{
			XmlAttribute xmlAttribute = node.Attributes[attributeName];
			sbyte b = ((Monster.GetBoneIndexWithId != null && xmlAttribute != null) ? Monster.GetBoneIndexWithId(this.ActionSetCode, xmlAttribute.Value) : baseValue);
			if (validateHasParentBone && b != invalidBoneIndex)
			{
				Func<string, sbyte, bool> getBoneHasParentBone = Monster.GetBoneHasParentBone;
			}
			return b;
		}

		// Token: 0x06000A9B RID: 2715 RVA: 0x0002233C File Offset: 0x0002053C
		private void DeserializeBoneIndexArray(List<sbyte> boneIndices, XmlNode node, bool hasBaseMonster, string attributeNamePrefix, sbyte invalidBoneIndex, bool validateHasParentBone)
		{
			int num = 0;
			for (;;)
			{
				bool flag = hasBaseMonster && num < boneIndices.Count;
				sbyte b = this.DeserializeBoneIndex(node, attributeNamePrefix + num, flag ? boneIndices[num] : invalidBoneIndex, invalidBoneIndex, validateHasParentBone);
				if (b == invalidBoneIndex)
				{
					break;
				}
				if (flag)
				{
					boneIndices[num] = b;
				}
				else
				{
					boneIndices.Add(b);
				}
				num++;
			}
		}

		// Token: 0x06000A9C RID: 2716 RVA: 0x000223A4 File Offset: 0x000205A4
		private static bool ReadVec3(string str, out Vec3 v)
		{
			str = str.Trim();
			string[] array = str.Split(",".ToCharArray());
			v = new Vec3(0f, 0f, 0f, -1f);
			return float.TryParse(array[0], out v.x) && float.TryParse(array[1], out v.y) && float.TryParse(array[2], out v.z);
		}

		// Token: 0x06000A9D RID: 2717 RVA: 0x0002241C File Offset: 0x0002061C
		public sbyte GetBoneToAttachForItemFlags(ItemFlags itemFlags)
		{
			ItemFlags itemFlags2 = itemFlags & ItemFlags.AttachmentMask;
			if (itemFlags2 <= (ItemFlags)0U)
			{
				return this.MainHandItemBoneIndex;
			}
			if (itemFlags2 == ItemFlags.ForceAttachOffHandPrimaryItemBone)
			{
				return this.OffHandItemBoneIndex;
			}
			if (itemFlags2 != ItemFlags.ForceAttachOffHandSecondaryItemBone)
			{
				return this.MainHandItemBoneIndex;
			}
			return this.OffHandItemSecondaryBoneIndex;
		}

		// Token: 0x04000597 RID: 1431
		public static Func<string, string, sbyte> GetBoneIndexWithId;

		// Token: 0x04000598 RID: 1432
		public static Func<string, sbyte, bool> GetBoneHasParentBone;

		// Token: 0x040005ED RID: 1517
		[CachedData]
		private IMonsterMissionData _monsterMissionData;
	}
}
