using System;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006A4 RID: 1700
	internal enum EvexOpCodeHandlerKind : byte
	{
		// Token: 0x04003539 RID: 13625
		Invalid,
		// Token: 0x0400353A RID: 13626
		Invalid2,
		// Token: 0x0400353B RID: 13627
		Dup,
		// Token: 0x0400353C RID: 13628
		HandlerReference,
		// Token: 0x0400353D RID: 13629
		ArrayReference,
		// Token: 0x0400353E RID: 13630
		RM,
		// Token: 0x0400353F RID: 13631
		Group,
		// Token: 0x04003540 RID: 13632
		W,
		// Token: 0x04003541 RID: 13633
		MandatoryPrefix2,
		// Token: 0x04003542 RID: 13634
		VectorLength,
		// Token: 0x04003543 RID: 13635
		VectorLength_er,
		// Token: 0x04003544 RID: 13636
		Ed_V_Ib,
		// Token: 0x04003545 RID: 13637
		Ev_VX,
		// Token: 0x04003546 RID: 13638
		Ev_VX_Ib,
		// Token: 0x04003547 RID: 13639
		Gv_W_er,
		// Token: 0x04003548 RID: 13640
		GvM_VX_Ib,
		// Token: 0x04003549 RID: 13641
		HkWIb_3,
		// Token: 0x0400354A RID: 13642
		HkWIb_3b,
		// Token: 0x0400354B RID: 13643
		HWIb,
		// Token: 0x0400354C RID: 13644
		KkHW_3,
		// Token: 0x0400354D RID: 13645
		KkHW_3b,
		// Token: 0x0400354E RID: 13646
		KkHWIb_sae_3,
		// Token: 0x0400354F RID: 13647
		KkHWIb_sae_3b,
		// Token: 0x04003550 RID: 13648
		KkHWIb_3,
		// Token: 0x04003551 RID: 13649
		KkHWIb_3b,
		// Token: 0x04003552 RID: 13650
		KkWIb_3,
		// Token: 0x04003553 RID: 13651
		KkWIb_3b,
		// Token: 0x04003554 RID: 13652
		KP1HW,
		// Token: 0x04003555 RID: 13653
		KR,
		// Token: 0x04003556 RID: 13654
		MV,
		// Token: 0x04003557 RID: 13655
		V_H_Ev_er,
		// Token: 0x04003558 RID: 13656
		V_H_Ev_Ib,
		// Token: 0x04003559 RID: 13657
		VHM,
		// Token: 0x0400355A RID: 13658
		VHW_3,
		// Token: 0x0400355B RID: 13659
		VHW_4,
		// Token: 0x0400355C RID: 13660
		VHWIb,
		// Token: 0x0400355D RID: 13661
		VK,
		// Token: 0x0400355E RID: 13662
		Vk_VSIB,
		// Token: 0x0400355F RID: 13663
		VkEv_REXW_2,
		// Token: 0x04003560 RID: 13664
		VkEv_REXW_3,
		// Token: 0x04003561 RID: 13665
		VkHM,
		// Token: 0x04003562 RID: 13666
		VkHW_3,
		// Token: 0x04003563 RID: 13667
		VkHW_3b,
		// Token: 0x04003564 RID: 13668
		VkHW_5,
		// Token: 0x04003565 RID: 13669
		VkHW_er_4,
		// Token: 0x04003566 RID: 13670
		VkHW_er_4b,
		// Token: 0x04003567 RID: 13671
		VkHWIb_3,
		// Token: 0x04003568 RID: 13672
		VkHWIb_3b,
		// Token: 0x04003569 RID: 13673
		VkHWIb_5,
		// Token: 0x0400356A RID: 13674
		VkHWIb_er_4,
		// Token: 0x0400356B RID: 13675
		VkHWIb_er_4b,
		// Token: 0x0400356C RID: 13676
		VkM,
		// Token: 0x0400356D RID: 13677
		VkW_3,
		// Token: 0x0400356E RID: 13678
		VkW_3b,
		// Token: 0x0400356F RID: 13679
		VkW_4,
		// Token: 0x04003570 RID: 13680
		VkW_4b,
		// Token: 0x04003571 RID: 13681
		VkW_er_4,
		// Token: 0x04003572 RID: 13682
		VkW_er_5,
		// Token: 0x04003573 RID: 13683
		VkW_er_6,
		// Token: 0x04003574 RID: 13684
		VkWIb_3,
		// Token: 0x04003575 RID: 13685
		VkWIb_3b,
		// Token: 0x04003576 RID: 13686
		VkWIb_er,
		// Token: 0x04003577 RID: 13687
		VM,
		// Token: 0x04003578 RID: 13688
		VSIB_k1,
		// Token: 0x04003579 RID: 13689
		VSIB_k1_VX,
		// Token: 0x0400357A RID: 13690
		VW,
		// Token: 0x0400357B RID: 13691
		VW_er,
		// Token: 0x0400357C RID: 13692
		VX_Ev,
		// Token: 0x0400357D RID: 13693
		WkHV,
		// Token: 0x0400357E RID: 13694
		WkV_3,
		// Token: 0x0400357F RID: 13695
		WkV_4a,
		// Token: 0x04003580 RID: 13696
		WkV_4b,
		// Token: 0x04003581 RID: 13697
		WkVIb,
		// Token: 0x04003582 RID: 13698
		WkVIb_er,
		// Token: 0x04003583 RID: 13699
		WV,
		// Token: 0x04003584 RID: 13700
		VkHW_er_ur_3,
		// Token: 0x04003585 RID: 13701
		VkHW_er_ur_3b
	}
}
