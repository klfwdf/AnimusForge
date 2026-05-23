using System;

namespace TaleWorlds.MountAndBlade;

public readonly struct ActionIndexCache : IEquatable<ActionIndexCache>
{
	public static readonly ActionIndexCache act_none = new ActionIndexCache(-1);

	public static readonly ActionIndexCache act_pickup_down_begin = Create("act_pickup_down_begin");

	public static readonly ActionIndexCache act_pickup_down_end = Create("act_pickup_down_end");

	public static readonly ActionIndexCache act_pickup_down_begin_left_stance = Create("act_pickup_down_begin_left_stance");

	public static readonly ActionIndexCache act_pickup_down_end_left_stance = Create("act_pickup_down_end_left_stance");

	public static readonly ActionIndexCache act_pickup_down_left_begin = Create("act_pickup_down_left_begin");

	public static readonly ActionIndexCache act_pickup_down_left_end = Create("act_pickup_down_left_end");

	public static readonly ActionIndexCache act_pickup_down_left_begin_left_stance = Create("act_pickup_down_left_begin_left_stance");

	public static readonly ActionIndexCache act_pickup_down_left_end_left_stance = Create("act_pickup_down_left_end_left_stance");

	public static readonly ActionIndexCache act_pickup_middle_begin = Create("act_pickup_middle_begin");

	public static readonly ActionIndexCache act_pickup_middle_end = Create("act_pickup_middle_end");

	public static readonly ActionIndexCache act_pickup_middle_begin_left_stance = Create("act_pickup_middle_begin_left_stance");

	public static readonly ActionIndexCache act_pickup_middle_end_left_stance = Create("act_pickup_middle_end_left_stance");

	public static readonly ActionIndexCache act_pickup_middle_left_begin = Create("act_pickup_middle_left_begin");

	public static readonly ActionIndexCache act_pickup_middle_left_end = Create("act_pickup_middle_left_end");

	public static readonly ActionIndexCache act_pickup_middle_left_begin_left_stance = Create("act_pickup_middle_left_begin_left_stance");

	public static readonly ActionIndexCache act_pickup_middle_left_end_left_stance = Create("act_pickup_middle_left_end_left_stance");

	public static readonly ActionIndexCache act_pickup_up_begin = Create("act_pickup_up_begin");

	public static readonly ActionIndexCache act_pickup_up_end = Create("act_pickup_up_end");

	public static readonly ActionIndexCache act_pickup_up_begin_left_stance = Create("act_pickup_up_begin_left_stance");

	public static readonly ActionIndexCache act_pickup_up_end_left_stance = Create("act_pickup_up_end_left_stance");

	public static readonly ActionIndexCache act_pickup_up_left_begin = Create("act_pickup_up_left_begin");

	public static readonly ActionIndexCache act_pickup_up_left_end = Create("act_pickup_up_left_end");

	public static readonly ActionIndexCache act_pickup_up_left_begin_left_stance = Create("act_pickup_up_left_begin_left_stance");

	public static readonly ActionIndexCache act_pickup_up_left_end_left_stance = Create("act_pickup_up_left_end_left_stance");

	public static readonly ActionIndexCache act_pickup_from_right_down_horseback_begin = Create("act_pickup_from_right_down_horseback_begin");

	public static readonly ActionIndexCache act_pickup_from_right_down_horseback_end = Create("act_pickup_from_right_down_horseback_end");

	public static readonly ActionIndexCache act_pickup_from_right_down_horseback_left_begin = Create("act_pickup_from_right_down_horseback_left_begin");

	public static readonly ActionIndexCache act_pickup_from_right_down_horseback_left_end = Create("act_pickup_from_right_down_horseback_left_end");

	public static readonly ActionIndexCache act_pickup_from_right_middle_horseback_begin = Create("act_pickup_from_right_middle_horseback_begin");

	public static readonly ActionIndexCache act_pickup_from_right_middle_horseback_end = Create("act_pickup_from_right_middle_horseback_end");

	public static readonly ActionIndexCache act_pickup_from_right_middle_horseback_left_begin = Create("act_pickup_from_right_middle_horseback_left_begin");

	public static readonly ActionIndexCache act_pickup_from_right_middle_horseback_left_end = Create("act_pickup_from_right_middle_horseback_left_end");

	public static readonly ActionIndexCache act_pickup_from_right_up_horseback_begin = Create("act_pickup_from_right_up_horseback_begin");

	public static readonly ActionIndexCache act_pickup_from_right_up_horseback_end = Create("act_pickup_from_right_up_horseback_end");

	public static readonly ActionIndexCache act_pickup_from_right_up_horseback_left_begin = Create("act_pickup_from_right_up_horseback_left_begin");

	public static readonly ActionIndexCache act_pickup_from_right_up_horseback_left_end = Create("act_pickup_from_right_up_horseback_left_end");

	public static readonly ActionIndexCache act_pickup_from_left_down_horseback_begin = Create("act_pickup_from_left_down_horseback_begin");

	public static readonly ActionIndexCache act_pickup_from_left_down_horseback_end = Create("act_pickup_from_left_down_horseback_end");

	public static readonly ActionIndexCache act_pickup_from_left_down_horseback_left_begin = Create("act_pickup_from_left_down_horseback_left_begin");

	public static readonly ActionIndexCache act_pickup_from_left_down_horseback_left_end = Create("act_pickup_from_left_down_horseback_left_end");

	public static readonly ActionIndexCache act_pickup_from_left_middle_horseback_begin = Create("act_pickup_from_left_middle_horseback_begin");

	public static readonly ActionIndexCache act_pickup_from_left_middle_horseback_end = Create("act_pickup_from_left_middle_horseback_end");

	public static readonly ActionIndexCache act_pickup_from_left_middle_horseback_left_begin = Create("act_pickup_from_left_middle_horseback_left_begin");

	public static readonly ActionIndexCache act_pickup_from_left_middle_horseback_left_end = Create("act_pickup_from_left_middle_horseback_left_end");

	public static readonly ActionIndexCache act_pickup_from_left_up_horseback_begin = Create("act_pickup_from_left_up_horseback_begin");

	public static readonly ActionIndexCache act_pickup_from_left_up_horseback_end = Create("act_pickup_from_left_up_horseback_end");

	public static readonly ActionIndexCache act_pickup_from_left_up_horseback_left_begin = Create("act_pickup_from_left_up_horseback_left_begin");

	public static readonly ActionIndexCache act_pickup_from_left_up_horseback_left_end = Create("act_pickup_from_left_up_horseback_left_end");

	public static readonly ActionIndexCache act_pickup_boulder_begin = Create("act_pickup_boulder_begin");

	public static readonly ActionIndexCache act_pickup_boulder_end = Create("act_pickup_boulder_end");

	public static readonly ActionIndexCache act_usage_trebuchet_idle = Create("act_usage_trebuchet_idle");

	public static readonly ActionIndexCache act_usage_trebuchet_reload = Create("act_usage_trebuchet_reload");

	public static readonly ActionIndexCache act_usage_trebuchet_reload_2 = Create("act_usage_trebuchet_reload_2");

	public static readonly ActionIndexCache act_usage_trebuchet_reload_idle = Create("act_usage_trebuchet_reload_idle");

	public static readonly ActionIndexCache act_usage_trebuchet_reload_2_idle = Create("act_usage_trebuchet_reload_2_idle");

	public static readonly ActionIndexCache act_usage_trebuchet_load_ammo = Create("act_usage_trebuchet_load_ammo");

	public static readonly ActionIndexCache act_usage_trebuchet_shoot = Create("act_usage_trebuchet_shoot");

	public static readonly ActionIndexCache act_usage_siege_machine_push = Create("act_usage_siege_machine_push");

	public static readonly ActionIndexCache act_usage_ladder_lift_from_left_1_start = Create("act_usage_ladder_lift_from_left_1_start");

	public static readonly ActionIndexCache act_usage_ladder_lift_from_left_2_start = Create("act_usage_ladder_lift_from_left_2_start");

	public static readonly ActionIndexCache act_usage_ladder_lift_from_right_1_start = Create("act_usage_ladder_lift_from_right_1_start");

	public static readonly ActionIndexCache act_usage_ladder_lift_from_right_2_start = Create("act_usage_ladder_lift_from_right_2_start");

	public static readonly ActionIndexCache act_usage_ladder_pick_up_fork_begin = Create("act_usage_ladder_pick_up_fork_begin");

	public static readonly ActionIndexCache act_usage_ladder_pick_up_fork_end = Create("act_usage_ladder_pick_up_fork_end");

	public static readonly ActionIndexCache act_usage_ladder_push_back = Create("act_usage_ladder_push_back");

	public static readonly ActionIndexCache act_usage_ladder_push_back_stopped = Create("act_usage_ladder_push_back_stopped");

	public static readonly ActionIndexCache act_usage_batteringram_left = Create("act_usage_batteringram_left");

	public static readonly ActionIndexCache act_usage_batteringram_left_slower = Create("act_usage_batteringram_left_slower");

	public static readonly ActionIndexCache act_usage_batteringram_left_slowest = Create("act_usage_batteringram_left_slowest");

	public static readonly ActionIndexCache act_usage_batteringram_right = Create("act_usage_batteringram_right");

	public static readonly ActionIndexCache act_usage_batteringram_right_slower = Create("act_usage_batteringram_right_slower");

	public static readonly ActionIndexCache act_usage_batteringram_right_slowest = Create("act_usage_batteringram_right_slowest");

	public static readonly ActionIndexCache act_strike_bent_over = Create("act_strike_bent_over");

	public static readonly ActionIndexCache act_strike_fall_back_back_rise = Create("act_strike_fall_back_back_rise");

	public static readonly ActionIndexCache act_row_strike = Create("act_row_strike");

	public static readonly ActionIndexCache act_stagger_forward = Create("act_stagger_forward");

	public static readonly ActionIndexCache act_stagger_backward = Create("act_stagger_backward");

	public static readonly ActionIndexCache act_stagger_right = Create("act_stagger_right");

	public static readonly ActionIndexCache act_stagger_left = Create("act_stagger_left");

	public static readonly ActionIndexCache act_stagger_forward_2 = Create("act_stagger_forward_2");

	public static readonly ActionIndexCache act_stagger_backward_2 = Create("act_stagger_backward_2");

	public static readonly ActionIndexCache act_stagger_right_2 = Create("act_stagger_right_2");

	public static readonly ActionIndexCache act_stagger_left_2 = Create("act_stagger_left_2");

	public static readonly ActionIndexCache act_stagger_forward_3 = Create("act_stagger_forward_3");

	public static readonly ActionIndexCache act_stagger_backward_3 = Create("act_stagger_backward_3");

	public static readonly ActionIndexCache act_stagger_right_3 = Create("act_stagger_right_3");

	public static readonly ActionIndexCache act_stagger_left_3 = Create("act_stagger_left_3");

	public static readonly ActionIndexCache act_command = Create("act_command");

	public static readonly ActionIndexCache act_command_leftstance = Create("act_command_leftstance");

	public static readonly ActionIndexCache act_command_unarmed = Create("act_command_unarmed");

	public static readonly ActionIndexCache act_command_unarmed_leftstance = Create("act_command_unarmed_leftstance");

	public static readonly ActionIndexCache act_command_2h = Create("act_command_2h");

	public static readonly ActionIndexCache act_command_2h_leftstance = Create("act_command_2h_leftstance");

	public static readonly ActionIndexCache act_command_bow = Create("act_command_bow");

	public static readonly ActionIndexCache act_command_follow = Create("act_command_follow");

	public static readonly ActionIndexCache act_command_follow_leftstance = Create("act_command_follow_leftstance");

	public static readonly ActionIndexCache act_command_follow_unarmed = Create("act_command_follow_unarmed");

	public static readonly ActionIndexCache act_command_follow_unarmed_leftstance = Create("act_command_follow_unarmed_leftstance");

	public static readonly ActionIndexCache act_command_follow_2h = Create("act_command_follow_2h");

	public static readonly ActionIndexCache act_command_follow_2h_leftstance = Create("act_command_follow_2h_leftstance");

	public static readonly ActionIndexCache act_command_follow_bow = Create("act_command_follow_bow");

	public static readonly ActionIndexCache act_horse_command = Create("act_horse_command");

	public static readonly ActionIndexCache act_horse_command_unarmed = Create("act_horse_command_unarmed");

	public static readonly ActionIndexCache act_horse_command_2h = Create("act_horse_command_2h");

	public static readonly ActionIndexCache act_horse_command_bow = Create("act_horse_command_bow");

	public static readonly ActionIndexCache act_horse_command_follow = Create("act_horse_command_follow");

	public static readonly ActionIndexCache act_horse_command_follow_unarmed = Create("act_horse_command_follow_unarmed");

	public static readonly ActionIndexCache act_horse_command_follow_2h = Create("act_horse_command_follow_2h");

	public static readonly ActionIndexCache act_horse_command_follow_bow = Create("act_horse_command_follow_bow");

	public static readonly ActionIndexCache act_ship_connection_break = Create("act_ship_connection_break");

	public static readonly ActionIndexCache act_usage_hook_ready = Create("act_usage_hook_ready");

	public static readonly ActionIndexCache act_usage_hook_release = Create("act_usage_hook_release");

	public static readonly ActionIndexCache act_usage_row_idle_no_hold = Create("act_usage_row_idle_no_hold");

	public static readonly ActionIndexCache act_t_pose = Create("act_t_pose");

	public static readonly ActionIndexCache act_jump_loop = Create("act_jump_loop");

	public static readonly ActionIndexCache act_stand_1 = Create("act_stand_1");

	public static readonly ActionIndexCache act_idle_unarmed_1 = Create("act_idle_unarmed_1");

	public static readonly ActionIndexCache act_walk_idle_1h_with_shield_left_stance = Create("act_walk_idle_1h_with_shield_left_stance");

	public static readonly ActionIndexCache act_crouch_walk_idle_unarmed = Create("act_crouch_walk_idle_unarmed");

	public static readonly ActionIndexCache act_beggar_idle = Create("act_beggar_idle");

	public static readonly ActionIndexCache act_walk_idle_unarmed = Create("act_walk_idle_unarmed");

	public static readonly ActionIndexCache act_horse_stand_1 = Create("act_horse_stand_1");

	public static readonly ActionIndexCache act_hero_mount_idle_camel = Create("act_hero_mount_idle_camel");

	public static readonly ActionIndexCache act_camel_idle_1 = Create("act_camel_idle_1");

	public static readonly ActionIndexCache act_tableau_hand_armor_pose = Create("act_tableau_hand_armor_pose");

	public static readonly ActionIndexCache act_inventory_idle_start = Create("act_inventory_idle_start");

	public static readonly ActionIndexCache act_inventory_idle = Create("act_inventory_idle");

	public static readonly ActionIndexCache act_inventory_glove_equip = Create("act_inventory_glove_equip");

	public static readonly ActionIndexCache act_inventory_cloth_equip = Create("act_inventory_cloth_equip");

	public static readonly ActionIndexCache act_conversation_normal_loop = Create("act_conversation_normal_loop");

	public static readonly ActionIndexCache act_conversation_warrior_loop = Create("act_conversation_warrior_loop");

	public static readonly ActionIndexCache act_conversation_hip_loop = Create("act_conversation_hip_loop");

	public static readonly ActionIndexCache act_conversation_closed_loop = Create("act_conversation_closed_loop");

	public static readonly ActionIndexCache act_conversation_demure_loop = Create("act_conversation_demure_loop");

	public static readonly ActionIndexCache act_scared_reaction_1 = Create("act_scared_reaction_1");

	public static readonly ActionIndexCache act_scared_idle_1 = Create("act_scared_idle_1");

	public static readonly ActionIndexCache act_greeting_front_1 = Create("act_greeting_front_1");

	public static readonly ActionIndexCache act_greeting_front_2 = Create("act_greeting_front_2");

	public static readonly ActionIndexCache act_greeting_front_3 = Create("act_greeting_front_3");

	public static readonly ActionIndexCache act_greeting_front_4 = Create("act_greeting_front_4");

	public static readonly ActionIndexCache act_greeting_right_1 = Create("act_greeting_right_1");

	public static readonly ActionIndexCache act_greeting_right_2 = Create("act_greeting_right_2");

	public static readonly ActionIndexCache act_greeting_right_3 = Create("act_greeting_right_3");

	public static readonly ActionIndexCache act_greeting_right_4 = Create("act_greeting_right_4");

	public static readonly ActionIndexCache act_greeting_left_1 = Create("act_greeting_left_1");

	public static readonly ActionIndexCache act_greeting_left_2 = Create("act_greeting_left_2");

	public static readonly ActionIndexCache act_greeting_left_3 = Create("act_greeting_left_3");

	public static readonly ActionIndexCache act_greeting_left_4 = Create("act_greeting_left_4");

	public static readonly ActionIndexCache act_guard_cautious_look_around_1 = Create("act_guard_cautious_look_around_1");

	public static readonly ActionIndexCache act_guard_patrolling_cautious_look_around_1 = Create("act_guard_patrolling_cautious_look_around_1");

	public static readonly ActionIndexCache act_use_smithing_machine_ready = Create("act_use_smithing_machine_ready");

	public static readonly ActionIndexCache act_use_smithing_machine_loop = Create("act_use_smithing_machine_loop");

	public static readonly ActionIndexCache act_smithing_machine_anvil_start = Create("act_smithing_machine_anvil_start");

	public static readonly ActionIndexCache act_smithing_machine_anvil_part_2 = Create("act_smithing_machine_anvil_part_2");

	public static readonly ActionIndexCache act_smithing_machine_anvil_part_4 = Create("act_smithing_machine_anvil_part_4");

	public static readonly ActionIndexCache act_smithing_machine_anvil_part_5 = Create("act_smithing_machine_anvil_part_5");

	public static readonly ActionIndexCache act_childhood_schooled = Create("act_childhood_schooled");

	public static readonly ActionIndexCache act_arena_spectator = Create("act_arena_spectator");

	public static readonly ActionIndexCache act_argue_trio_middle = Create("act_argue_trio_middle");

	public static readonly ActionIndexCache act_argue_trio_middle_2 = Create("act_argue_trio_middle_2");

	public static readonly ActionIndexCache act_argue_trio_left = Create("act_argue_trio_left");

	public static readonly ActionIndexCache act_argue_trio_right = Create("act_argue_trio_right");

	public static readonly ActionIndexCache act_taunt_cheer_1 = Create("act_taunt_cheer_1");

	public static readonly ActionIndexCache act_taunt_cheer_2 = Create("act_taunt_cheer_2");

	public static readonly ActionIndexCache act_taunt_cheer_3 = Create("act_taunt_cheer_3");

	public static readonly ActionIndexCache act_taunt_cheer_4 = Create("act_taunt_cheer_4");

	public static readonly ActionIndexCache act_cheering_low_01 = Create("act_cheering_low_01");

	public static readonly ActionIndexCache act_cheering_low_02 = Create("act_cheering_low_02");

	public static readonly ActionIndexCache act_cheering_low_03 = Create("act_cheering_low_03");

	public static readonly ActionIndexCache act_cheering_low_04 = Create("act_cheering_low_04");

	public static readonly ActionIndexCache act_cheering_low_05 = Create("act_cheering_low_05");

	public static readonly ActionIndexCache act_cheering_low_06 = Create("act_cheering_low_06");

	public static readonly ActionIndexCache act_cheering_low_07 = Create("act_cheering_low_07");

	public static readonly ActionIndexCache act_cheering_low_08 = Create("act_cheering_low_08");

	public static readonly ActionIndexCache act_cheering_low_09 = Create("act_cheering_low_09");

	public static readonly ActionIndexCache act_cheering_low_10 = Create("act_cheering_low_10");

	public static readonly ActionIndexCache act_cheer_1 = Create("act_cheer_1");

	public static readonly ActionIndexCache act_cheer_2 = Create("act_cheer_2");

	public static readonly ActionIndexCache act_cheer_3 = Create("act_cheer_3");

	public static readonly ActionIndexCache act_cheer_4 = Create("act_cheer_4");

	public static readonly ActionIndexCache act_cheering_high_01 = Create("act_cheering_high_01");

	public static readonly ActionIndexCache act_cheering_high_02 = Create("act_cheering_high_02");

	public static readonly ActionIndexCache act_cheering_high_03 = Create("act_cheering_high_03");

	public static readonly ActionIndexCache act_cheering_high_04 = Create("act_cheering_high_04");

	public static readonly ActionIndexCache act_cheering_high_05 = Create("act_cheering_high_05");

	public static readonly ActionIndexCache act_cheering_high_06 = Create("act_cheering_high_06");

	public static readonly ActionIndexCache act_cheering_high_07 = Create("act_cheering_high_07");

	public static readonly ActionIndexCache act_cheering_high_08 = Create("act_cheering_high_08");

	public static readonly ActionIndexCache act_map_raid = Create("act_map_raid");

	public static readonly ActionIndexCache act_map_rider_camel_attack_1h = Create("act_map_rider_camel_attack_1h");

	public static readonly ActionIndexCache act_map_rider_camel_attack_1h_spear = Create("act_map_rider_camel_attack_1h_spear");

	public static readonly ActionIndexCache act_map_rider_camel_attack_1h_swing = Create("act_map_rider_camel_attack_1h_swing");

	public static readonly ActionIndexCache act_map_rider_camel_attack_2h_swing = Create("act_map_rider_camel_attack_2h_swing");

	public static readonly ActionIndexCache act_map_rider_camel_attack_unarmed = Create("act_map_rider_camel_attack_unarmed");

	public static readonly ActionIndexCache act_map_rider_horse_attack_1h = Create("act_map_rider_horse_attack_1h");

	public static readonly ActionIndexCache act_map_rider_horse_attack_1h_spear = Create("act_map_rider_horse_attack_1h_spear");

	public static readonly ActionIndexCache act_map_rider_horse_attack_1h_swing = Create("act_map_rider_horse_attack_1h_swing");

	public static readonly ActionIndexCache act_map_rider_horse_attack_2h_swing = Create("act_map_rider_horse_attack_2h_swing");

	public static readonly ActionIndexCache act_map_rider_horse_attack_unarmed = Create("act_map_rider_horse_attack_unarmed");

	public static readonly ActionIndexCache act_map_mount_attack_1h = Create("act_map_mount_attack_1h");

	public static readonly ActionIndexCache act_map_mount_attack_spear = Create("act_map_mount_attack_spear");

	public static readonly ActionIndexCache act_map_mount_attack_swing = Create("act_map_mount_attack_swing");

	public static readonly ActionIndexCache act_map_mount_attack_unarmed = Create("act_map_mount_attack_unarmed");

	public static readonly ActionIndexCache act_map_attack_1h = Create("act_map_attack_1h");

	public static readonly ActionIndexCache act_map_attack_2h = Create("act_map_attack_2h");

	public static readonly ActionIndexCache act_map_attack_spear_1h_or_2h = Create("act_map_attack_spear_1h_or_2h");

	public static readonly ActionIndexCache act_map_attack_unarmed = Create("act_map_attack_unarmed");

	public static readonly ActionIndexCache act_conversation_naval_start = Create("act_conversation_naval_start");

	public static readonly ActionIndexCache act_conversation_naval_idle_loop = Create("act_conversation_naval_idle_loop");

	public static readonly ActionIndexCache act_death_by_arrow_pelvis = Create("act_death_by_arrow_pelvis");

	public static readonly ActionIndexCache act_horse_fall_right = Create("act_horse_fall_right");

	public static readonly ActionIndexCache act_cutscene_npc_argue_player_1 = Create("act_cutscene_npc_argue_player_1");

	public static readonly ActionIndexCache act_escape_jump = Create("act_escape_jump");

	public static readonly ActionIndexCache act_raid_jump = Create("act_raid_jump_1");

	public int Index { get; }

	public static ActionIndexCache Create(string actName)
	{
		if (!string.IsNullOrWhiteSpace(actName))
		{
			return new ActionIndexCache(actName);
		}
		return act_none;
	}

	private ActionIndexCache(string name)
	{
		Index = MBAnimation.GetActionCodeWithName(name);
	}

	internal ActionIndexCache(int actionIndex)
	{
		Index = actionIndex;
	}

	public string GetName()
	{
		if (Index != -1)
		{
			return MBAPI.IMBAnimation.GetActionNameWithCode(Index);
		}
		return "act_none";
	}

	public override bool Equals(object obj)
	{
		return Equals((ActionIndexCache)obj);
	}

	public bool Equals(ActionIndexCache other)
	{
		return Index == other.Index;
	}

	public static bool operator ==(ActionIndexCache action0, ActionIndexCache action1)
	{
		return action0.Index == action1.Index;
	}

	public static bool operator !=(ActionIndexCache action0, ActionIndexCache action1)
	{
		return action0.Index != action1.Index;
	}

	public override int GetHashCode()
	{
		return Index.GetHashCode();
	}
}
