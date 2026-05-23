using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ManagedCallbacks;

internal static class ScriptingInterfaceObjects
{
	private enum CoreInterfaceGeneratedEnum
	{
		enm_IMono_MBActionSet_are_actions_alternatives = 0,
		enm_IMono_MBActionSet_get_animation_name = 1,
		enm_IMono_MBActionSet_get_bone_has_parent_bone = 2,
		enm_IMono_MBActionSet_get_bone_index_with_id = 3,
		enm_IMono_MBActionSet_get_index_with_id = 4,
		enm_IMono_MBActionSet_get_name_with_index = 5,
		enm_IMono_MBActionSet_get_number_of_action_sets = 6,
		enm_IMono_MBActionSet_get_number_of_monster_usage_sets = 7,
		enm_IMono_MBActionSet_get_skeleton_name = 8,
		enm_IMono_MBAgent_add_acceleration = 9,
		enm_IMono_MBAgent_add_as_corpse = 10,
		enm_IMono_MBAgent_add_mesh_to_bone = 11,
		enm_IMono_MBAgent_add_prefab_to_agent_bone = 12,
		enm_IMono_MBAgent_apply_force_on_ragdoll = 13,
		enm_IMono_MBAgent_attach_weapon_to_bone = 14,
		enm_IMono_MBAgent_attach_weapon_to_weapon_in_slot = 15,
		enm_IMono_MBAgent_attack_direction_to_movement_flag = 16,
		enm_IMono_MBAgent_build = 17,
		enm_IMono_MBAgent_can_move_directly_to_position = 18,
		enm_IMono_MBAgent_check_path_to_ai_target_agent_passes_through_navigation_face_id_from_direction = 19,
		enm_IMono_MBAgent_clear_equipment = 20,
		enm_IMono_MBAgent_clear_hand_inverse_kinematics = 21,
		enm_IMono_MBAgent_clear_target_frame = 22,
		enm_IMono_MBAgent_clear_target_z = 23,
		enm_IMono_MBAgent_compute_animation_displacement = 24,
		enm_IMono_MBAgent_create_blood_burst_at_limb = 25,
		enm_IMono_MBAgent_debug_more = 26,
		enm_IMono_MBAgent_defend_direction_to_movement_flag = 28,
		enm_IMono_MBAgent_delete_attached_weapon_from_bone = 29,
		enm_IMono_MBAgent_die = 30,
		enm_IMono_MBAgent_disable_look_to_point_of_interest = 31,
		enm_IMono_MBAgent_disable_scripted_combat_movement = 32,
		enm_IMono_MBAgent_disable_scripted_movement = 33,
		enm_IMono_MBAgent_drop_item = 34,
		enm_IMono_MBAgent_end_ragdoll_as_corpse = 35,
		enm_IMono_MBAgent_enforce_shield_usage = 36,
		enm_IMono_MBAgent_fade_in = 37,
		enm_IMono_MBAgent_fade_out = 38,
		enm_IMono_MBAgent_find_longest_direct_move_to_position = 39,
		enm_IMono_MBAgent_force_ai_behavior_selection = 40,
		enm_IMono_MBAgent_get_action_channel_current_action_weight = 41,
		enm_IMono_MBAgent_get_action_channel_weight = 42,
		enm_IMono_MBAgent_get_action_direction = 43,
		enm_IMono_MBAgent_get_action_set_no = 44,
		enm_IMono_MBAgent_get_agent_facial_animation = 45,
		enm_IMono_MBAgent_get_agent_parent_entity = 46,
		enm_IMono_MBAgent_get_agent_scale = 47,
		enm_IMono_MBAgent_get_agent_visuals = 48,
		enm_IMono_MBAgent_get_agent_voice_definiton = 49,
		enm_IMono_MBAgent_get_ai_last_suspicious_position = 50,
		enm_IMono_MBAgent_get_aiming_timer = 51,
		enm_IMono_MBAgent_get_ai_move_destination = 52,
		enm_IMono_MBAgent_get_ai_move_stop_tolerance = 53,
		enm_IMono_MBAgent_get_ai_state_flags = 54,
		enm_IMono_MBAgent_get_attack_direction = 55,
		enm_IMono_MBAgent_get_attack_direction_usage = 56,
		enm_IMono_MBAgent_get_average_real_global_velocity = 57,
		enm_IMono_MBAgent_get_average_velocity = 58,
		enm_IMono_MBAgent_get_body_rotation_constraint = 59,
		enm_IMono_MBAgent_get_quick_bone_entitial_frame = 60,
		enm_IMono_MBAgent_get_bone_entitial_frame_at_animation_progress = 61,
		enm_IMono_MBAgent_get_chest_global_position = 62,
		enm_IMono_MBAgent_get_collision_capsule = 63,
		enm_IMono_MBAgent_get_crouch_mode = 64,
		enm_IMono_MBAgent_get_current_action_direction = 65,
		enm_IMono_MBAgent_get_current_action_priority = 66,
		enm_IMono_MBAgent_get_current_action_progress = 67,
		enm_IMono_MBAgent_get_current_action_stage = 68,
		enm_IMono_MBAgent_get_current_action_type = 69,
		enm_IMono_MBAgent_get_current_aiming_error = 70,
		enm_IMono_MBAgent_get_current_aiming_turbulance = 71,
		enm_IMono_MBAgent_get_current_animation_flags = 72,
		enm_IMono_MBAgent_get_current_guard_mode = 73,
		enm_IMono_MBAgent_get_current_navigation_face_id = 74,
		enm_IMono_MBAgent_get_current_speed_limit = 75,
		enm_IMono_MBAgent_get_current_velocity = 76,
		enm_IMono_MBAgent_get_cur_weapon_offset = 77,
		enm_IMono_MBAgent_get_defend_movement_flag = 78,
		enm_IMono_MBAgent_get_event_control_flags = 79,
		enm_IMono_MBAgent_get_eye_global_height = 80,
		enm_IMono_MBAgent_get_eye_global_position = 81,
		enm_IMono_MBAgent_get_firing_order = 82,
		enm_IMono_MBAgent_get_ground_material_for_collision_effect = 83,
		enm_IMono_MBAgent_get_has_on_ai_input_set_callback = 84,
		enm_IMono_MBAgent_get_head_camera_mode = 85,
		enm_IMono_MBAgent_get_immediate_enemy = 86,
		enm_IMono_MBAgent_get_is_doing_passive_attack = 87,
		enm_IMono_MBAgent_get_is_left_stance = 88,
		enm_IMono_MBAgent_get_is_look_direction_locked = 89,
		enm_IMono_MBAgent_get_is_passive_usage_conditions_are_met = 90,
		enm_IMono_MBAgent_get_last_target_visibility_state = 91,
		enm_IMono_MBAgent_get_look_agent = 92,
		enm_IMono_MBAgent_get_look_direction = 93,
		enm_IMono_MBAgent_get_look_direction_as_angle = 94,
		enm_IMono_MBAgent_get_look_down_limit = 95,
		enm_IMono_MBAgent_get_maximum_number_of_agents = 96,
		enm_IMono_MBAgent_get_maximum_speed_limit = 97,
		enm_IMono_MBAgent_get_missile_range = 98,
		enm_IMono_MBAgent_get_missile_range_with_height_difference = 99,
		enm_IMono_MBAgent_get_monster_usage_index = 100,
		enm_IMono_MBAgent_get_mount_agent = 101,
		enm_IMono_MBAgent_get_movement_flags = 102,
		enm_IMono_MBAgent_get_movement_input_vector = 103,
		enm_IMono_MBAgent_get_movement_locked_state = 104,
		enm_IMono_MBAgent_get_movement_velocity = 105,
		enm_IMono_MBAgent_get_native_action_index = 106,
		enm_IMono_MBAgent_get_old_wielded_item_info = 107,
		enm_IMono_MBAgent_get_path_distance_to_point = 108,
		enm_IMono_MBAgent_get_position = 109,
		enm_IMono_MBAgent_get_real_global_velocity = 110,
		enm_IMono_MBAgent_get_render_check_enabled = 111,
		enm_IMono_MBAgent_get_retreat_pos = 112,
		enm_IMono_MBAgent_get_rider_agent = 113,
		enm_IMono_MBAgent_get_riding_order = 114,
		enm_IMono_MBAgent_get_rotation_frame = 115,
		enm_IMono_MBAgent_get_running_simulation_data_until_maximum_speed_reached = 116,
		enm_IMono_MBAgent_get_scripted_combat_flags = 117,
		enm_IMono_MBAgent_get_scripted_flags = 118,
		enm_IMono_MBAgent_get_selected_mount_index = 119,
		enm_IMono_MBAgent_get_state_flags = 120,
		enm_IMono_MBAgent_get_stepped_body_flags = 121,
		enm_IMono_MBAgent_get_stepped_entity_id = 122,
		enm_IMono_MBAgent_get_stepped_root_entity_id = 123,
		enm_IMono_MBAgent_get_target_agent = 124,
		enm_IMono_MBAgent_get_target_direction = 125,
		enm_IMono_MBAgent_get_target_formation_index = 126,
		enm_IMono_MBAgent_get_target_position = 127,
		enm_IMono_MBAgent_get_team = 128,
		enm_IMono_MBAgent_get_total_mass = 129,
		enm_IMono_MBAgent_get_turn_speed = 130,
		enm_IMono_MBAgent_get_visual_position = 131,
		enm_IMono_MBAgent_get_walk_mode = 132,
		enm_IMono_MBAgent_get_walking_speed_limit_of_mountable = 133,
		enm_IMono_MBAgent_get_weapon_entity_from_equipment_slot = 134,
		enm_IMono_MBAgent_get_wielded_weapon_info = 135,
		enm_IMono_MBAgent_get_world_position = 136,
		enm_IMono_MBAgent_handle_blow_aux = 137,
		enm_IMono_MBAgent_has_path_through_navigation_face_id_from_direction = 138,
		enm_IMono_MBAgent_has_path_through_navigation_faces_id_from_direction = 139,
		enm_IMono_MBAgent_initialize_agent_record = 140,
		enm_IMono_MBAgent_invalidate_ai_weapon_selections = 141,
		enm_IMono_MBAgent_invalidate_target_agent = 142,
		enm_IMono_MBAgent_is_added_as_corpse = 143,
		enm_IMono_MBAgent_is_crouching_allowed = 144,
		enm_IMono_MBAgent_is_enemy = 145,
		enm_IMono_MBAgent_is_fading_out = 146,
		enm_IMono_MBAgent_is_friend = 147,
		enm_IMono_MBAgent_is_look_rotation_in_slow_motion = 148,
		enm_IMono_MBAgent_is_retreating = 149,
		enm_IMono_MBAgent_is_running_away = 150,
		enm_IMono_MBAgent_is_sliding = 151,
		enm_IMono_MBAgent_is_target_navigation_face_id_between = 152,
		enm_IMono_MBAgent_is_wandering = 153,
		enm_IMono_MBAgent_kick_clear = 154,
		enm_IMono_MBAgent_lock_agent_replication_table_with_current_reliable_sequence_no = 155,
		enm_IMono_MBAgent_make_dead = 156,
		enm_IMono_MBAgent_make_voice = 157,
		enm_IMono_MBAgent_player_attack_direction = 158,
		enm_IMono_MBAgent_preload_for_rendering = 159,
		enm_IMono_MBAgent_prepare_weapon_for_drop_in_equipment_slot = 160,
		enm_IMono_MBAgent_remove_mesh_from_bone = 161,
		enm_IMono_MBAgent_reset_enemy_caches = 162,
		enm_IMono_MBAgent_reset_guard = 163,
		enm_IMono_MBAgent_set_action_channel = 164,
		enm_IMono_MBAgent_set_action_set = 165,
		enm_IMono_MBAgent_set_agent_exclude_state_for_face_group_id = 166,
		enm_IMono_MBAgent_set_agent_facial_animation = 167,
		enm_IMono_MBAgent_set_agent_flags = 168,
		enm_IMono_MBAgent_set_agent_idle_animation_status = 169,
		enm_IMono_MBAgent_set_agent_scale = 170,
		enm_IMono_MBAgent_set_ai_alarm_state = 171,
		enm_IMono_MBAgent_set_ai_behavior_params = 172,
		enm_IMono_MBAgent_set_ai_last_suspicious_position = 173,
		enm_IMono_MBAgent_set_ai_state_flags = 174,
		enm_IMono_MBAgent_set_all_ai_behavior_params = 175,
		enm_IMono_MBAgent_set_attack_state = 176,
		enm_IMono_MBAgent_set_automatic_target_agent_selection = 177,
		enm_IMono_MBAgent_set_average_ping_in_milliseconds = 178,
		enm_IMono_MBAgent_set_body_armor_material_type = 179,
		enm_IMono_MBAgent_set_columnwise_follow_agent = 180,
		enm_IMono_MBAgent_set_controller = 181,
		enm_IMono_MBAgent_set_courage = 182,
		enm_IMono_MBAgent_set_current_action_progress = 183,
		enm_IMono_MBAgent_set_current_action_speed = 184,
		enm_IMono_MBAgent_set_direction_change_tendency = 185,
		enm_IMono_MBAgent_set_event_control_flags = 186,
		enm_IMono_MBAgent_set_excluded_from_gravity = 187,
		enm_IMono_MBAgent_set_firing_order = 188,
		enm_IMono_MBAgent_set_force_attached_entity = 189,
		enm_IMono_MBAgent_set_formation_frame_disabled = 190,
		enm_IMono_MBAgent_set_formation_frame_enabled = 191,
		enm_IMono_MBAgent_set_formation_info = 192,
		enm_IMono_MBAgent_set_formation_integrity_data = 193,
		enm_IMono_MBAgent_set_formation_no = 194,
		enm_IMono_MBAgent_set_hand_inverse_kinematics_frame = 195,
		enm_IMono_MBAgent_set_hand_inverse_kinematics_frame_for_mission_object_usage = 196,
		enm_IMono_MBAgent_set_has_on_ai_input_set_callback = 197,
		enm_IMono_MBAgent_set_head_camera_mode = 198,
		enm_IMono_MBAgent_set_initial_frame = 199,
		enm_IMono_MBAgent_set_interaction_agent = 200,
		enm_IMono_MBAgent_set_is_look_direction_locked = 201,
		enm_IMono_MBAgent_set_is_physics_force_closed = 202,
		enm_IMono_MBAgent_set_look_agent = 203,
		enm_IMono_MBAgent_set_look_direction = 204,
		enm_IMono_MBAgent_set_look_direction_as_angle = 205,
		enm_IMono_MBAgent_set_look_to_point_of_interest = 206,
		enm_IMono_MBAgent_set_maximum_speed_limit = 207,
		enm_IMono_MBAgent_set_mono_object = 208,
		enm_IMono_MBAgent_set_mount_agent = 209,
		enm_IMono_MBAgent_set_movement_direction = 210,
		enm_IMono_MBAgent_set_movement_flags = 211,
		enm_IMono_MBAgent_set_movement_input_vector = 212,
		enm_IMono_MBAgent_set_network_peer = 213,
		enm_IMono_MBAgent_set_overriden_strike_and_death_action = 214,
		enm_IMono_MBAgent_set_position = 215,
		enm_IMono_MBAgent_set_reload_ammo_in_slot = 216,
		enm_IMono_MBAgent_set_render_check_enabled = 217,
		enm_IMono_MBAgent_set_retreat_mode = 218,
		enm_IMono_MBAgent_set_riding_order = 219,
		enm_IMono_MBAgent_set_scripted_combat_flags = 220,
		enm_IMono_MBAgent_set_scripted_flags = 221,
		enm_IMono_MBAgent_set_scripted_position = 222,
		enm_IMono_MBAgent_set_scripted_position_and_direction = 223,
		enm_IMono_MBAgent_set_scripted_target_entity = 224,
		enm_IMono_MBAgent_set_selected_mount_index = 225,
		enm_IMono_MBAgent_set_should_catch_up_with_formation = 226,
		enm_IMono_MBAgent_set_state_flags = 227,
		enm_IMono_MBAgent_set_target_agent = 228,
		enm_IMono_MBAgent_set_target_formation_index = 229,
		enm_IMono_MBAgent_set_target_position = 230,
		enm_IMono_MBAgent_set_target_position_and_direction = 231,
		enm_IMono_MBAgent_set_target_up = 232,
		enm_IMono_MBAgent_set_target_z = 233,
		enm_IMono_MBAgent_set_team = 234,
		enm_IMono_MBAgent_set_usage_index_of_weapon_in_slot_as_client = 235,
		enm_IMono_MBAgent_set_velocity_limits_on_ragdoll = 236,
		enm_IMono_MBAgent_set_weapon_ammo_as_client = 237,
		enm_IMono_MBAgent_set_weapon_amount_in_slot = 238,
		enm_IMono_MBAgent_set_weapon_guard = 239,
		enm_IMono_MBAgent_set_weapon_hit_points_in_slot = 240,
		enm_IMono_MBAgent_set_weapon_reload_phase_as_client = 241,
		enm_IMono_MBAgent_set_wielded_item_index_as_client = 242,
		enm_IMono_MBAgent_start_fading_out = 243,
		enm_IMono_MBAgent_start_ragdoll_as_corpse = 244,
		enm_IMono_MBAgent_start_switching_weapon_usage_index_as_client = 245,
		enm_IMono_MBAgent_tick_action_channels = 246,
		enm_IMono_MBAgent_try_get_immediate_agent_movement_data = 247,
		enm_IMono_MBAgent_try_to_sheath_weapon_in_hand = 248,
		enm_IMono_MBAgent_try_to_wield_weapon_in_slot = 249,
		enm_IMono_MBAgent_update_driven_properties = 250,
		enm_IMono_MBAgent_update_weapons = 251,
		enm_IMono_MBAgent_weapon_equipped = 252,
		enm_IMono_MBAgent_wield_next_weapon = 253,
		enm_IMono_MBAgent_yell_after_delay = 254,
		enm_IMono_MBAgentVisuals_add_child_entity = 255,
		enm_IMono_MBAgentVisuals_add_horse_reins_cloth_mesh = 256,
		enm_IMono_MBAgentVisuals_add_mesh = 257,
		enm_IMono_MBAgentVisuals_add_multi_mesh = 258,
		enm_IMono_MBAgentVisuals_add_prefab_to_agent_visual_bone_by_bone_type = 259,
		enm_IMono_MBAgentVisuals_add_prefab_to_agent_visual_bone_by_real_bone_index = 260,
		enm_IMono_MBAgentVisuals_add_skin_meshes_to_agent_visuals = 261,
		enm_IMono_MBAgentVisuals_add_weapon_to_agent_entity = 262,
		enm_IMono_MBAgentVisuals_apply_skeleton_scale = 263,
		enm_IMono_MBAgentVisuals_batch_last_lod_meshes = 264,
		enm_IMono_MBAgentVisuals_check_resources = 265,
		enm_IMono_MBAgentVisuals_clear_all_weapon_meshes = 266,
		enm_IMono_MBAgentVisuals_clear_visual_components = 267,
		enm_IMono_MBAgentVisuals_clear_weapon_meshes = 268,
		enm_IMono_MBAgentVisuals_create_agent_renderer_scene_controller = 269,
		enm_IMono_MBAgentVisuals_create_agent_visuals = 270,
		enm_IMono_MBAgentVisuals_create_particle_system_attached_to_bone = 271,
		enm_IMono_MBAgentVisuals_destruct_agent_renderer_scene_controller = 272,
		enm_IMono_MBAgentVisuals_disable_contour = 273,
		enm_IMono_MBAgentVisuals_fill_entity_with_body_meshes_without_agent_visuals = 274,
		enm_IMono_MBAgentVisuals_get_attached_weapon_entity = 275,
		enm_IMono_MBAgentVisuals_get_quick_bone_entitial_frame = 276,
		enm_IMono_MBAgentVisuals_get_bone_entitial_frame_at_animation_progress = 277,
		enm_IMono_MBAgentVisuals_get_bone_type_data = 278,
		enm_IMono_MBAgentVisuals_get_current_head_look_direction = 279,
		enm_IMono_MBAgentVisuals_get_current_helmet_scaling_factor = 280,
		enm_IMono_MBAgentVisuals_get_current_ragdoll_state = 281,
		enm_IMono_MBAgentVisuals_get_entity = 282,
		enm_IMono_MBAgentVisuals_get_entity_pointer = 283,
		enm_IMono_MBAgentVisuals_get_frame = 284,
		enm_IMono_MBAgentVisuals_get_global_frame = 285,
		enm_IMono_MBAgentVisuals_get_global_stable_eye_point = 286,
		enm_IMono_MBAgentVisuals_get_global_stable_neck_point = 287,
		enm_IMono_MBAgentVisuals_get_movement_mode = 288,
		enm_IMono_MBAgentVisuals_get_real_bone_index = 289,
		enm_IMono_MBAgentVisuals_get_skeleton = 290,
		enm_IMono_MBAgentVisuals_get_visible = 291,
		enm_IMono_MBAgentVisuals_get_visual_strength_of_agent_visual = 292,
		enm_IMono_MBAgentVisuals_is_valid = 293,
		enm_IMono_MBAgentVisuals_lazy_update_agent_renderer_data = 294,
		enm_IMono_MBAgentVisuals_make_voice = 295,
		enm_IMono_MBAgentVisuals_remove_child_entity = 296,
		enm_IMono_MBAgentVisuals_remove_mesh = 297,
		enm_IMono_MBAgentVisuals_remove_multi_mesh = 298,
		enm_IMono_MBAgentVisuals_reset = 299,
		enm_IMono_MBAgentVisuals_reset_next_frame = 300,
		enm_IMono_MBAgentVisuals_set_agent_local_speed = 301,
		enm_IMono_MBAgentVisuals_set_agent_lod_make_zero_or_max = 302,
		enm_IMono_MBAgentVisuals_set_as_contour_entity = 303,
		enm_IMono_MBAgentVisuals_set_attached_position_for_rope_entity_after_animation_post_integrate = 304,
		enm_IMono_MBAgentVisuals_set_cloth_component_keep_state_of_all_meshes = 305,
		enm_IMono_MBAgentVisuals_set_cloth_wind_to_weapon_at_index = 306,
		enm_IMono_MBAgentVisuals_set_contour_state = 307,
		enm_IMono_MBAgentVisuals_set_do_timer_based_skeleton_forced_updates = 308,
		enm_IMono_MBAgentVisuals_set_enable_occlusion_culling = 309,
		enm_IMono_MBAgentVisuals_set_enforced_visibility_for_all_agents = 310,
		enm_IMono_MBAgentVisuals_set_entity = 311,
		enm_IMono_MBAgentVisuals_set_face_generation_params = 312,
		enm_IMono_MBAgentVisuals_set_frame = 313,
		enm_IMono_MBAgentVisuals_set_lod_atlas_shading_index = 314,
		enm_IMono_MBAgentVisuals_set_look_direction = 315,
		enm_IMono_MBAgentVisuals_set_setup_morph_node = 316,
		enm_IMono_MBAgentVisuals_set_skeleton = 317,
		enm_IMono_MBAgentVisuals_set_visible = 318,
		enm_IMono_MBAgentVisuals_set_voice_definition_index = 319,
		enm_IMono_MBAgentVisuals_set_wielded_weapon_indices = 320,
		enm_IMono_MBAgentVisuals_start_rhubarb_record = 321,
		enm_IMono_MBAgentVisuals_tick = 322,
		enm_IMono_MBAgentVisuals_update_quiver_mesh_of_weapon_in_slot = 323,
		enm_IMono_MBAgentVisuals_update_skeleton_scale = 324,
		enm_IMono_MBAgentVisuals_use_scaled_weapons = 325,
		enm_IMono_MBAgentVisuals_validate_agent_visuals_reseted = 326,
		enm_IMono_MBAnimation_get_animation_index_of_action_code = 327,
		enm_IMono_MBAnimation_check_animation_clip_exists = 328,
		enm_IMono_MBAnimation_get_action_animation_duration = 329,
		enm_IMono_MBAnimation_get_action_blend_out_start_progress = 330,
		enm_IMono_MBAnimation_get_action_code_with_name = 331,
		enm_IMono_MBAnimation_get_action_name_with_code = 332,
		enm_IMono_MBAnimation_get_action_type = 333,
		enm_IMono_MBAnimation_get_animation_blend_in_period = 334,
		enm_IMono_MBAnimation_get_animation_blends_with_action_index = 335,
		enm_IMono_MBAnimation_get_animation_continue_to_action = 336,
		enm_IMono_MBAnimation_get_animation_displacement_at_progress = 337,
		enm_IMono_MBAnimation_get_animation_duration = 338,
		enm_IMono_MBAnimation_get_animation_flags = 339,
		enm_IMono_MBAnimation_get_animation_name = 340,
		enm_IMono_MBAnimation_get_animation_parameter1 = 341,
		enm_IMono_MBAnimation_get_animation_parameter2 = 342,
		enm_IMono_MBAnimation_get_animation_parameter3 = 343,
		enm_IMono_MBAnimation_get_displacement_vector = 344,
		enm_IMono_MBAnimation_get_id_with_index = 345,
		enm_IMono_MBAnimation_get_index_with_id = 346,
		enm_IMono_MBAnimation_get_num_action_codes = 347,
		enm_IMono_MBAnimation_get_num_animations = 348,
		enm_IMono_MBAnimation_is_any_animation_loading_from_disk = 349,
		enm_IMono_MBAnimation_prefetch_animation_clip = 350,
		enm_IMono_MBBannerlordChecker_get_engine_struct_member_offset = 351,
		enm_IMono_MBBannerlordChecker_get_engine_struct_size = 352,
		enm_IMono_MBBannerlordConfig_validate_options = 353,
		enm_IMono_MBBannerlordTableauManager_get_number_of_pending_tableau_requests = 354,
		enm_IMono_MBBannerlordTableauManager_initialize_character_tableau_render_system = 355,
		enm_IMono_MBBannerlordTableauManager_request_character_tableau_render = 356,
		enm_IMono_MBDebugExtensions_override_native_parameter = 357,
		enm_IMono_MBDebugExtensions_reload_native_parameters = 358,
		enm_IMono_MBDebugExtensions_render_debug_arc_on_terrain = 359,
		enm_IMono_MBDebugExtensions_render_debug_circle_on_terrain = 360,
		enm_IMono_MBDebugExtensions_render_debug_line_on_terrain = 361,
		enm_IMono_MBEditor_activate_scene_editor_presentation = 362,
		enm_IMono_MBEditor_add_editor_warning = 363,
		enm_IMono_MBEditor_add_entity_warning = 364,
		enm_IMono_MBEditor_add_nav_mesh_warning = 365,
		enm_IMono_MBEditor_apply_delta_to_editor_camera = 366,
		enm_IMono_MBEditor_border_helpers_enabled = 367,
		enm_IMono_MBEditor_deactivate_scene_editor_presentation = 368,
		enm_IMono_MBEditor_enter_edit_mission_mode = 369,
		enm_IMono_MBEditor_enter_edit_mode = 370,
		enm_IMono_MBEditor_exit_edit_mode = 371,
		enm_IMono_MBEditor_get_all_prefabs_and_child_with_tag = 372,
		enm_IMono_MBEditor_get_editor_scene_view = 373,
		enm_IMono_MBEditor_helpers_enabled = 374,
		enm_IMono_MBEditor_is_edit_mode = 375,
		enm_IMono_MBEditor_is_edit_mode_enabled = 376,
		enm_IMono_MBEditor_is_entity_selected = 377,
		enm_IMono_MBEditor_is_replay_manager_recording = 378,
		enm_IMono_MBEditor_is_replay_manager_rendering = 379,
		enm_IMono_MBEditor_is_replay_manager_replaying = 380,
		enm_IMono_MBEditor_leave_edit_mission_mode = 381,
		enm_IMono_MBEditor_leave_edit_mode = 382,
		enm_IMono_MBEditor_render_editor_mesh = 383,
		enm_IMono_MBEditor_set_level_visibility = 384,
		enm_IMono_MBEditor_set_upgrade_level_visibility = 385,
		enm_IMono_MBEditor_tick_edit_mode = 386,
		enm_IMono_MBEditor_tick_scene_editor_presentation = 387,
		enm_IMono_MBEditor_toggle_enable_editor_physics = 388,
		enm_IMono_MBEditor_update_scene_tree = 389,
		enm_IMono_MBEditor_zoom_to_position = 390,
		enm_IMono_MBFaceGen_enforce_constraints = 391,
		enm_IMono_MBFaceGen_flush_face_cache = 392,
		enm_IMono_MBFaceGen_get_deform_key_data = 393,
		enm_IMono_MBFaceGen_get_face_gen_instances_length = 394,
		enm_IMono_MBFaceGen_get_facial_indices_by_tag = 395,
		enm_IMono_MBFaceGen_get_hair_color_count = 396,
		enm_IMono_MBFaceGen_get_hair_color_gradient_points = 397,
		enm_IMono_MBFaceGen_get_hair_indices_by_tag = 398,
		enm_IMono_MBFaceGen_get_maturity_type = 399,
		enm_IMono_MBFaceGen_get_num_editable_deform_keys = 400,
		enm_IMono_MBFaceGen_get_params_from_key = 401,
		enm_IMono_MBFaceGen_get_params_max = 402,
		enm_IMono_MBFaceGen_get_race_ids = 403,
		enm_IMono_MBFaceGen_get_random_body_properties = 404,
		enm_IMono_MBFaceGen_get_scale = 405,
		enm_IMono_MBFaceGen_get_skin_color_count = 406,
		enm_IMono_MBFaceGen_get_skin_color_gradient_points = 407,
		enm_IMono_MBFaceGen_get_tatoo_color_count = 408,
		enm_IMono_MBFaceGen_get_tatoo_color_gradient_points = 409,
		enm_IMono_MBFaceGen_get_tattoo_indices_by_tag = 410,
		enm_IMono_MBFaceGen_get_voice_records_count = 411,
		enm_IMono_MBFaceGen_get_voice_type_usable_for_player_data = 412,
		enm_IMono_MBFaceGen_get_zero_probabilities = 413,
		enm_IMono_MBFaceGen_produce_numeric_key_with_default_values = 414,
		enm_IMono_MBFaceGen_produce_numeric_key_with_params = 415,
		enm_IMono_MBFaceGen_transform_face_keys_to_default_face = 416,
		enm_IMono_MBGame_load_module_data = 417,
		enm_IMono_MBGame_start_new = 418,
		enm_IMono_MBGameEntityExtensions_create_from_weapon = 419,
		enm_IMono_MBGameEntityExtensions_fade_in = 420,
		enm_IMono_MBGameEntityExtensions_fade_out = 421,
		enm_IMono_MBGameEntityExtensions_hide_if_not_fading_out = 422,
		enm_IMono_MBItem_get_holster_frame_by_index = 423,
		enm_IMono_MBItem_get_item_holster_index = 424,
		enm_IMono_MBItem_get_item_is_passive_usage = 425,
		enm_IMono_MBItem_get_item_usage_index = 426,
		enm_IMono_MBItem_get_item_usage_reload_action_code = 427,
		enm_IMono_MBItem_get_item_usage_set_flags = 428,
		enm_IMono_MBItem_get_item_usage_strike_type = 429,
		enm_IMono_MBItem_get_missile_range = 430,
		enm_IMono_MBMapScene_get_accessible_point_near_position = 431,
		enm_IMono_MBMapScene_get_battle_scene_index_map = 432,
		enm_IMono_MBMapScene_get_battle_scene_index_map_resolution = 433,
		enm_IMono_MBMapScene_get_color_grade_grid_data = 434,
		enm_IMono_MBMapScene_get_mouse_visible = 435,
		enm_IMono_MBMapScene_get_nearest_nav_mesh_face_center_position_between_regions_using_path = 436,
		enm_IMono_MBMapScene_get_nearest_nav_mesh_face_center_position_for_position = 437,
		enm_IMono_MBMapScene_get_season_time_factor = 438,
		enm_IMono_MBMapScene_load_atmosphere_data = 439,
		enm_IMono_MBMapScene_remove_zero_corner_bodies = 440,
		enm_IMono_MBMapScene_send_mouse_key_down_event = 441,
		enm_IMono_MBMapScene_set_frame_for_atmosphere = 442,
		enm_IMono_MBMapScene_set_mouse_pos = 443,
		enm_IMono_MBMapScene_set_mouse_visible = 444,
		enm_IMono_MBMapScene_set_political_color = 445,
		enm_IMono_MBMapScene_set_season_time_factor = 446,
		enm_IMono_MBMapScene_set_terrain_dynamic_params = 447,
		enm_IMono_MBMapScene_tick_ambient_sounds = 448,
		enm_IMono_MBMapScene_tick_step_sound = 449,
		enm_IMono_MBMapScene_tick_visuals = 450,
		enm_IMono_MBMapScene_validate_terrain_sound_ids = 451,
		enm_IMono_MBMessageManager_display_message = 452,
		enm_IMono_MBMessageManager_display_message_with_color = 453,
		enm_IMono_MBMessageManager_set_message_manager = 454,
		enm_IMono_MBMission_add_ai_debug_text = 455,
		enm_IMono_MBMission_add_boundary = 456,
		enm_IMono_MBMission_add_missile = 457,
		enm_IMono_MBMission_add_missile_single_usage = 458,
		enm_IMono_MBMission_add_particle_system_burst_by_name = 459,
		enm_IMono_MBMission_add_team = 460,
		enm_IMono_MBMission_backup_record_to_file = 461,
		enm_IMono_MBMission_batch_formation_unit_positions = 462,
		enm_IMono_MBMission_clear_agent_actions = 463,
		enm_IMono_MBMission_clear_corpses = 464,
		enm_IMono_MBMission_clear_missiles = 465,
		enm_IMono_MBMission_clear_record_buffers = 466,
		enm_IMono_MBMission_clear_resources = 467,
		enm_IMono_MBMission_clear_scene = 468,
		enm_IMono_MBMission_compute_exact_missile_range_at_height_difference = 469,
		enm_IMono_MBMission_create_agent = 470,
		enm_IMono_MBMission_create_mission = 471,
		enm_IMono_MBMission_end_of_record = 472,
		enm_IMono_MBMission_finalize_mission = 473,
		enm_IMono_MBMission_find_agent_with_index = 474,
		enm_IMono_MBMission_find_convex_hull = 475,
		enm_IMono_MBMission_force_disable_occlusion = 476,
		enm_IMono_MBMission_get_agent_count_around_position = 477,
		enm_IMono_MBMission_get_alternate_position_for_navmeshless_or_out_of_bounds_position = 478,
		enm_IMono_MBMission_get_atmosphere_name_for_replay = 479,
		enm_IMono_MBMission_get_atmosphere_season_for_replay = 480,
		enm_IMono_MBMission_get_average_fps = 481,
		enm_IMono_MBMission_get_average_morale_of_agents = 482,
		enm_IMono_MBMission_get_best_slope_angle_height_pos_for_defending = 483,
		enm_IMono_MBMission_get_best_slope_towards_direction = 484,
		enm_IMono_MBMission_get_biggest_agent_collision_padding = 485,
		enm_IMono_MBMission_get_boundary_count = 486,
		enm_IMono_MBMission_get_boundary_name = 487,
		enm_IMono_MBMission_get_boundary_points = 488,
		enm_IMono_MBMission_get_boundary_radius = 489,
		enm_IMono_MBMission_get_camera_frame = 490,
		enm_IMono_MBMission_get_clear_scene_timer_elapsed_time = 491,
		enm_IMono_MBMission_get_closest_ally = 492,
		enm_IMono_MBMission_get_closest_boundary_position = 493,
		enm_IMono_MBMission_get_closest_enemy = 494,
		enm_IMono_MBMission_get_combat_type = 495,
		enm_IMono_MBMission_get_current_volume_generator_version = 496,
		enm_IMono_MBMission_get_debug_agent = 497,
		enm_IMono_MBMission_get_fall_avoid_system_active = 498,
		enm_IMono_MBMission_get_game_type_for_replay = 499,
		enm_IMono_MBMission_get_is_loading_finished = 500,
		enm_IMono_MBMission_get_missile_collision_point = 501,
		enm_IMono_MBMission_get_missile_has_rigid_body = 502,
		enm_IMono_MBMission_get_missile_range = 503,
		enm_IMono_MBMission_get_missile_vertical_aim_correction = 504,
		enm_IMono_MBMission_get_navigation_points = 505,
		enm_IMono_MBMission_get_nearby_agents_aux = 506,
		enm_IMono_MBMission_get_number_of_teams = 507,
		enm_IMono_MBMission_get_old_position_of_missile = 508,
		enm_IMono_MBMission_get_pause_ai_tick = 509,
		enm_IMono_MBMission_get_position_of_missile = 510,
		enm_IMono_MBMission_get_scene_levels_for_replay = 511,
		enm_IMono_MBMission_get_scene_name_for_replay = 512,
		enm_IMono_MBMission_get_straight_path_to_target = 513,
		enm_IMono_MBMission_get_tick_debug_paused = 514,
		enm_IMono_MBMission_get_time = 515,
		enm_IMono_MBMission_get_velocity_of_missile = 516,
		enm_IMono_MBMission_get_water_level_at_position = 517,
		enm_IMono_MBMission_get_weighted_point_of_enemies = 518,
		enm_IMono_MBMission_has_any_agents_of_team_around = 519,
		enm_IMono_MBMission_idle_tick = 520,
		enm_IMono_MBMission_initialize_mission = 521,
		enm_IMono_MBMission_is_agent_in_proximity_map = 522,
		enm_IMono_MBMission_is_formation_unit_position_available = 523,
		enm_IMono_MBMission_is_position_inside_any_blocker_nav_mesh_face_2d = 524,
		enm_IMono_MBMission_is_position_inside_boundaries = 525,
		enm_IMono_MBMission_is_position_inside_hard_boundaries = 526,
		enm_IMono_MBMission_is_position_on_any_blocker_nav_mesh_face = 527,
		enm_IMono_MBMission_make_sound = 528,
		enm_IMono_MBMission_make_sound_only_on_related_peer = 529,
		enm_IMono_MBMission_make_sound_with_parameter = 530,
		enm_IMono_MBMission_on_fast_forward_state_changed = 531,
		enm_IMono_MBMission_pause_mission_scene_sounds = 532,
		enm_IMono_MBMission_prepare_missile_weapon_for_drop = 533,
		enm_IMono_MBMission_process_record_until_time = 534,
		enm_IMono_MBMission_agent_proximity_map_begin_search = 535,
		enm_IMono_MBMission_agent_proximity_map_find_next = 536,
		enm_IMono_MBMission_agent_proximity_map_get_max_search_radius = 537,
		enm_IMono_MBMission_ray_cast_for_closest_agent = 538,
		enm_IMono_MBMission_ray_cast_for_closest_agents_limbs = 539,
		enm_IMono_MBMission_ray_cast_for_given_agents_limbs = 540,
		enm_IMono_MBMission_record_current_state = 541,
		enm_IMono_MBMission_remove_boundary = 542,
		enm_IMono_MBMission_remove_missile = 543,
		enm_IMono_MBMission_reset_first_third_person_view = 544,
		enm_IMono_MBMission_reset_teams = 545,
		enm_IMono_MBMission_restart_record = 546,
		enm_IMono_MBMission_restore_record_from_file = 547,
		enm_IMono_MBMission_resume_mission_scene_sounds = 548,
		enm_IMono_MBMission_set_bow_missile_speed_modifier = 549,
		enm_IMono_MBMission_set_camera_frame = 550,
		enm_IMono_MBMission_set_camera_is_first_person = 551,
		enm_IMono_MBMission_set_close_proximity_wave_sounds_enabled = 552,
		enm_IMono_MBMission_set_combat_type = 553,
		enm_IMono_MBMission_set_crossbow_missile_speed_modifier = 554,
		enm_IMono_MBMission_set_debug_agent = 555,
		enm_IMono_MBMission_set_fall_avoid_system_active = 556,
		enm_IMono_MBMission_set_last_movement_key_pressed = 557,
		enm_IMono_MBMission_set_missile_range_modifier = 558,
		enm_IMono_MBMission_set_mission_corpse_fade_out_time_in_seconds = 559,
		enm_IMono_MBMission_set_navigation_face_cost_with_id_around_position = 560,
		enm_IMono_MBMission_set_override_corpse_count = 561,
		enm_IMono_MBMission_set_pause_ai_tick = 562,
		enm_IMono_MBMission_set_random_decide_time_of_agents = 563,
		enm_IMono_MBMission_set_render_parallel_logic_in_progress = 564,
		enm_IMono_MBMission_set_report_stuck_agents_mode = 565,
		enm_IMono_MBMission_set_throwing_missile_speed_modifier = 566,
		enm_IMono_MBMission_set_velocity_of_missile = 567,
		enm_IMono_MBMission_skip_forward_mission_replay = 568,
		enm_IMono_MBMission_start_recording = 569,
		enm_IMono_MBMission_tick = 570,
		enm_IMono_MBMission_tick_agents_and_teams_async = 571,
		enm_IMono_MBNetwork_add_new_bot_on_server = 572,
		enm_IMono_MBNetwork_add_new_player_on_server = 573,
		enm_IMono_MBNetwork_add_peer_to_disconnect = 574,
		enm_IMono_MBNetwork_begin_broadcast_module_event = 575,
		enm_IMono_MBNetwork_begin_module_event_as_client = 576,
		enm_IMono_MBNetwork_can_add_new_players_on_server = 577,
		enm_IMono_MBNetwork_clear_replication_table_statistics = 578,
		enm_IMono_MBNetwork_elapsed_time_since_last_udp_packet_arrived = 579,
		enm_IMono_MBNetwork_end_broadcast_module_event = 580,
		enm_IMono_MBNetwork_end_module_event_as_client = 581,
		enm_IMono_MBNetwork_get_active_udp_sessions_ip_address = 582,
		enm_IMono_MBNetwork_get_average_packet_loss_ratio = 583,
		enm_IMono_MBNetwork_get_debug_uploads_in_bits = 584,
		enm_IMono_MBNetwork_get_multiplayer_disabled = 585,
		enm_IMono_MBNetwork_initialize_client_side = 586,
		enm_IMono_MBNetwork_initialize_server_side = 587,
		enm_IMono_MBNetwork_is_dedicated_server = 588,
		enm_IMono_MBNetwork_prepare_new_udp_session = 589,
		enm_IMono_MBNetwork_print_debug_stats = 590,
		enm_IMono_MBNetwork_print_replication_table_statistics = 591,
		enm_IMono_MBNetwork_read_byte_array_from_packet = 592,
		enm_IMono_MBNetwork_read_float_from_packet = 593,
		enm_IMono_MBNetwork_read_int_from_packet = 594,
		enm_IMono_MBNetwork_read_long_from_packet = 595,
		enm_IMono_MBNetwork_read_string_from_packet = 596,
		enm_IMono_MBNetwork_read_uint_from_packet = 597,
		enm_IMono_MBNetwork_read_ulong_from_packet = 598,
		enm_IMono_MBNetwork_remove_bot_on_server = 599,
		enm_IMono_MBNetwork_reset_debug_uploads = 600,
		enm_IMono_MBNetwork_reset_debug_variables = 601,
		enm_IMono_MBNetwork_reset_mission_data = 602,
		enm_IMono_MBNetwork_server_ping = 603,
		enm_IMono_MBNetwork_set_server_bandwidth_limit_in_mbps = 604,
		enm_IMono_MBNetwork_set_server_frame_rate = 605,
		enm_IMono_MBNetwork_set_server_tick_rate = 606,
		enm_IMono_MBNetwork_terminate_client_side = 607,
		enm_IMono_MBNetwork_terminate_server_side = 608,
		enm_IMono_MBNetwork_write_byte_array_to_packet = 609,
		enm_IMono_MBNetwork_write_float_to_packet = 610,
		enm_IMono_MBNetwork_write_int_to_packet = 611,
		enm_IMono_MBNetwork_write_long_to_packet = 612,
		enm_IMono_MBNetwork_write_string_to_packet = 613,
		enm_IMono_MBNetwork_write_uint_to_packet = 614,
		enm_IMono_MBNetwork_write_ulong_to_packet = 615,
		enm_IMono_MBPeer_begin_module_event = 616,
		enm_IMono_MBPeer_end_module_event = 617,
		enm_IMono_MBPeer_get_average_loss_percent = 618,
		enm_IMono_MBPeer_get_average_ping_in_milliseconds = 619,
		enm_IMono_MBPeer_get_host = 620,
		enm_IMono_MBPeer_get_is_synchronized = 621,
		enm_IMono_MBPeer_get_port = 622,
		enm_IMono_MBPeer_get_reversed_host = 623,
		enm_IMono_MBPeer_is_active = 624,
		enm_IMono_MBPeer_send_existing_objects = 625,
		enm_IMono_MBPeer_set_controlled_agent = 626,
		enm_IMono_MBPeer_set_is_synchronized = 627,
		enm_IMono_MBPeer_set_relevant_game_options = 628,
		enm_IMono_MBPeer_set_team = 629,
		enm_IMono_MBPeer_set_user_data = 630,
		enm_IMono_MBScreen_on_edit_mode_enter_press = 631,
		enm_IMono_MBScreen_on_edit_mode_enter_release = 632,
		enm_IMono_MBScreen_on_exit_button_click = 633,
		enm_IMono_MBSkeletonExtensions_create_agent_skeleton = 634,
		enm_IMono_MBSkeletonExtensions_create_simple_skeleton = 635,
		enm_IMono_MBSkeletonExtensions_create_with_action_set = 636,
		enm_IMono_MBSkeletonExtensions_does_action_continue_with_current_action_at_channel = 637,
		enm_IMono_MBSkeletonExtensions_get_action_at_channel = 638,
		enm_IMono_MBSkeletonExtensions_get_bone_entitial_frame = 639,
		enm_IMono_MBSkeletonExtensions_get_bone_entitial_frame_at_animation_progress = 640,
		enm_IMono_MBSkeletonExtensions_get_skeleton_face_animation_name = 641,
		enm_IMono_MBSkeletonExtensions_get_skeleton_face_animation_time = 642,
		enm_IMono_MBSkeletonExtensions_set_agent_action_channel = 643,
		enm_IMono_MBSkeletonExtensions_set_animation_at_channel = 644,
		enm_IMono_MBSkeletonExtensions_set_facial_animation_of_channel = 645,
		enm_IMono_MBSkeletonExtensions_set_skeleton_face_animation_time = 646,
		enm_IMono_MBSkeletonExtensions_tick_action_channels = 647,
		enm_IMono_MBSoundEvent_create_event_from_external_file = 648,
		enm_IMono_MBSoundEvent_create_event_from_sound_buffer = 649,
		enm_IMono_MBSoundEvent_play_sound = 650,
		enm_IMono_MBSoundEvent_play_sound_with_int_param = 651,
		enm_IMono_MBSoundEvent_play_sound_with_param = 652,
		enm_IMono_MBSoundEvent_play_sound_with_str_param = 653,
		enm_IMono_MBTeam_is_enemy = 654,
		enm_IMono_MBTeam_set_is_enemy = 655,
		enm_IMono_MBTestRun_auto_continue = 656,
		enm_IMono_MBTestRun_close_scene = 657,
		enm_IMono_MBTestRun_enter_edit_mode = 658,
		enm_IMono_MBTestRun_get_fps = 659,
		enm_IMono_MBTestRun_leave_edit_mode = 660,
		enm_IMono_MBTestRun_new_scene = 661,
		enm_IMono_MBTestRun_open_default_scene = 662,
		enm_IMono_MBTestRun_open_scene = 663,
		enm_IMono_MBTestRun_save_scene = 664,
		enm_IMono_MBTestRun_start_mission = 665,
		enm_IMono_MBVoiceManager_get_voice_definition_count_with_monster_sound_and_collision_info_class_name = 666,
		enm_IMono_MBVoiceManager_get_voice_definitions_with_monster_sound_and_collision_info_class_name = 667,
		enm_IMono_MBVoiceManager_get_voice_type_index = 668,
		enm_IMono_MBWindowManager_dont_change_cursor_pos = 669,
		enm_IMono_MBWindowManager_erase_message_lines = 670,
		enm_IMono_MBWindowManager_get_screen_resolution = 671,
		enm_IMono_MBWindowManager_pre_display = 672,
		enm_IMono_MBWindowManager_screen_to_world = 673,
		enm_IMono_MBWindowManager_world_to_screen = 674,
		enm_IMono_MBWindowManager_world_to_screen_with_fixed_z = 675,
		enm_IMono_MBWorld_check_resource_modifications = 676,
		enm_IMono_MBWorld_fix_skeletons = 677,
		enm_IMono_MBWorld_get_game_type = 678,
		enm_IMono_MBWorld_get_global_time = 679,
		enm_IMono_MBWorld_get_last_messages = 680,
		enm_IMono_MBWorld_pause_game = 681,
		enm_IMono_MBWorld_set_body_used = 682,
		enm_IMono_MBWorld_set_game_type = 683,
		enm_IMono_MBWorld_set_material_used = 684,
		enm_IMono_MBWorld_set_mesh_used = 685,
		enm_IMono_MBWorld_unpause_game = 686
	}

	public static Dictionary<string, object> GetObjects()
	{
		return new Dictionary<string, object>
		{
			{
				"TaleWorlds.MountAndBlade.IMBActionSet",
				new ScriptingInterfaceOfIMBActionSet()
			},
			{
				"TaleWorlds.MountAndBlade.IMBAgent",
				new ScriptingInterfaceOfIMBAgent()
			},
			{
				"TaleWorlds.MountAndBlade.IMBAgentVisuals",
				new ScriptingInterfaceOfIMBAgentVisuals()
			},
			{
				"TaleWorlds.MountAndBlade.IMBAnimation",
				new ScriptingInterfaceOfIMBAnimation()
			},
			{
				"TaleWorlds.MountAndBlade.IMBBannerlordChecker",
				new ScriptingInterfaceOfIMBBannerlordChecker()
			},
			{
				"TaleWorlds.MountAndBlade.IMBBannerlordConfig",
				new ScriptingInterfaceOfIMBBannerlordConfig()
			},
			{
				"TaleWorlds.MountAndBlade.IMBBannerlordTableauManager",
				new ScriptingInterfaceOfIMBBannerlordTableauManager()
			},
			{
				"TaleWorlds.MountAndBlade.IMBDebugExtensions",
				new ScriptingInterfaceOfIMBDebugExtensions()
			},
			{
				"TaleWorlds.MountAndBlade.IMBDelegate",
				new ScriptingInterfaceOfIMBDelegate()
			},
			{
				"TaleWorlds.MountAndBlade.IMBEditor",
				new ScriptingInterfaceOfIMBEditor()
			},
			{
				"TaleWorlds.MountAndBlade.IMBFaceGen",
				new ScriptingInterfaceOfIMBFaceGen()
			},
			{
				"TaleWorlds.MountAndBlade.IMBGame",
				new ScriptingInterfaceOfIMBGame()
			},
			{
				"TaleWorlds.MountAndBlade.IMBGameEntityExtensions",
				new ScriptingInterfaceOfIMBGameEntityExtensions()
			},
			{
				"TaleWorlds.MountAndBlade.IMBItem",
				new ScriptingInterfaceOfIMBItem()
			},
			{
				"TaleWorlds.MountAndBlade.IMBMapScene",
				new ScriptingInterfaceOfIMBMapScene()
			},
			{
				"TaleWorlds.MountAndBlade.IMBMessageManager",
				new ScriptingInterfaceOfIMBMessageManager()
			},
			{
				"TaleWorlds.MountAndBlade.IMBMission",
				new ScriptingInterfaceOfIMBMission()
			},
			{
				"TaleWorlds.MountAndBlade.IMBMultiplayerData",
				new ScriptingInterfaceOfIMBMultiplayerData()
			},
			{
				"TaleWorlds.MountAndBlade.IMBNetwork",
				new ScriptingInterfaceOfIMBNetwork()
			},
			{
				"TaleWorlds.MountAndBlade.IMBPeer",
				new ScriptingInterfaceOfIMBPeer()
			},
			{
				"TaleWorlds.MountAndBlade.IMBScreen",
				new ScriptingInterfaceOfIMBScreen()
			},
			{
				"TaleWorlds.MountAndBlade.IMBSkeletonExtensions",
				new ScriptingInterfaceOfIMBSkeletonExtensions()
			},
			{
				"TaleWorlds.MountAndBlade.IMBSoundEvent",
				new ScriptingInterfaceOfIMBSoundEvent()
			},
			{
				"TaleWorlds.MountAndBlade.IMBTeam",
				new ScriptingInterfaceOfIMBTeam()
			},
			{
				"TaleWorlds.MountAndBlade.IMBTestRun",
				new ScriptingInterfaceOfIMBTestRun()
			},
			{
				"TaleWorlds.MountAndBlade.IMBVoiceManager",
				new ScriptingInterfaceOfIMBVoiceManager()
			},
			{
				"TaleWorlds.MountAndBlade.IMBWindowManager",
				new ScriptingInterfaceOfIMBWindowManager()
			},
			{
				"TaleWorlds.MountAndBlade.IMBWorld",
				new ScriptingInterfaceOfIMBWorld()
			}
		};
	}

	public static void SetFunctionPointer(int id, IntPtr pointer)
	{
		switch ((CoreInterfaceGeneratedEnum)id)
		{
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_are_actions_alternatives:
			ScriptingInterfaceOfIMBActionSet.call_AreActionsAlternativesDelegate = (ScriptingInterfaceOfIMBActionSet.AreActionsAlternativesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.AreActionsAlternativesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_animation_name:
			ScriptingInterfaceOfIMBActionSet.call_GetAnimationNameDelegate = (ScriptingInterfaceOfIMBActionSet.GetAnimationNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetAnimationNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_bone_has_parent_bone:
			ScriptingInterfaceOfIMBActionSet.call_GetBoneHasParentBoneDelegate = (ScriptingInterfaceOfIMBActionSet.GetBoneHasParentBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetBoneHasParentBoneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_bone_index_with_id:
			ScriptingInterfaceOfIMBActionSet.call_GetBoneIndexWithIdDelegate = (ScriptingInterfaceOfIMBActionSet.GetBoneIndexWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetBoneIndexWithIdDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_index_with_id:
			ScriptingInterfaceOfIMBActionSet.call_GetIndexWithIDDelegate = (ScriptingInterfaceOfIMBActionSet.GetIndexWithIDDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetIndexWithIDDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_name_with_index:
			ScriptingInterfaceOfIMBActionSet.call_GetNameWithIndexDelegate = (ScriptingInterfaceOfIMBActionSet.GetNameWithIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetNameWithIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_number_of_action_sets:
			ScriptingInterfaceOfIMBActionSet.call_GetNumberOfActionSetsDelegate = (ScriptingInterfaceOfIMBActionSet.GetNumberOfActionSetsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetNumberOfActionSetsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_number_of_monster_usage_sets:
			ScriptingInterfaceOfIMBActionSet.call_GetNumberOfMonsterUsageSetsDelegate = (ScriptingInterfaceOfIMBActionSet.GetNumberOfMonsterUsageSetsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetNumberOfMonsterUsageSetsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBActionSet_get_skeleton_name:
			ScriptingInterfaceOfIMBActionSet.call_GetSkeletonNameDelegate = (ScriptingInterfaceOfIMBActionSet.GetSkeletonNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBActionSet.GetSkeletonNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_add_acceleration:
			ScriptingInterfaceOfIMBAgent.call_AddAccelerationDelegate = (ScriptingInterfaceOfIMBAgent.AddAccelerationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.AddAccelerationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_add_as_corpse:
			ScriptingInterfaceOfIMBAgent.call_AddAsCorpseDelegate = (ScriptingInterfaceOfIMBAgent.AddAsCorpseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.AddAsCorpseDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_add_mesh_to_bone:
			ScriptingInterfaceOfIMBAgent.call_AddMeshToBoneDelegate = (ScriptingInterfaceOfIMBAgent.AddMeshToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.AddMeshToBoneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_add_prefab_to_agent_bone:
			ScriptingInterfaceOfIMBAgent.call_AddPrefabToAgentBoneDelegate = (ScriptingInterfaceOfIMBAgent.AddPrefabToAgentBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.AddPrefabToAgentBoneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_apply_force_on_ragdoll:
			ScriptingInterfaceOfIMBAgent.call_ApplyForceOnRagdollDelegate = (ScriptingInterfaceOfIMBAgent.ApplyForceOnRagdollDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ApplyForceOnRagdollDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_attach_weapon_to_bone:
			ScriptingInterfaceOfIMBAgent.call_AttachWeaponToBoneDelegate = (ScriptingInterfaceOfIMBAgent.AttachWeaponToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.AttachWeaponToBoneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_attach_weapon_to_weapon_in_slot:
			ScriptingInterfaceOfIMBAgent.call_AttachWeaponToWeaponInSlotDelegate = (ScriptingInterfaceOfIMBAgent.AttachWeaponToWeaponInSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.AttachWeaponToWeaponInSlotDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_attack_direction_to_movement_flag:
			ScriptingInterfaceOfIMBAgent.call_AttackDirectionToMovementFlagDelegate = (ScriptingInterfaceOfIMBAgent.AttackDirectionToMovementFlagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.AttackDirectionToMovementFlagDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_build:
			ScriptingInterfaceOfIMBAgent.call_BuildDelegate = (ScriptingInterfaceOfIMBAgent.BuildDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.BuildDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_can_move_directly_to_position:
			ScriptingInterfaceOfIMBAgent.call_CanMoveDirectlyToPositionDelegate = (ScriptingInterfaceOfIMBAgent.CanMoveDirectlyToPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.CanMoveDirectlyToPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_check_path_to_ai_target_agent_passes_through_navigation_face_id_from_direction:
			ScriptingInterfaceOfIMBAgent.call_CheckPathToAITargetAgentPassesThroughNavigationFaceIdFromDirectionDelegate = (ScriptingInterfaceOfIMBAgent.CheckPathToAITargetAgentPassesThroughNavigationFaceIdFromDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.CheckPathToAITargetAgentPassesThroughNavigationFaceIdFromDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_clear_equipment:
			ScriptingInterfaceOfIMBAgent.call_ClearEquipmentDelegate = (ScriptingInterfaceOfIMBAgent.ClearEquipmentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ClearEquipmentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_clear_hand_inverse_kinematics:
			ScriptingInterfaceOfIMBAgent.call_ClearHandInverseKinematicsDelegate = (ScriptingInterfaceOfIMBAgent.ClearHandInverseKinematicsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ClearHandInverseKinematicsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_clear_target_frame:
			ScriptingInterfaceOfIMBAgent.call_ClearTargetFrameDelegate = (ScriptingInterfaceOfIMBAgent.ClearTargetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ClearTargetFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_clear_target_z:
			ScriptingInterfaceOfIMBAgent.call_ClearTargetZDelegate = (ScriptingInterfaceOfIMBAgent.ClearTargetZDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ClearTargetZDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_compute_animation_displacement:
			ScriptingInterfaceOfIMBAgent.call_ComputeAnimationDisplacementDelegate = (ScriptingInterfaceOfIMBAgent.ComputeAnimationDisplacementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ComputeAnimationDisplacementDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_create_blood_burst_at_limb:
			ScriptingInterfaceOfIMBAgent.call_CreateBloodBurstAtLimbDelegate = (ScriptingInterfaceOfIMBAgent.CreateBloodBurstAtLimbDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.CreateBloodBurstAtLimbDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_debug_more:
			ScriptingInterfaceOfIMBAgent.call_DebugMoreDelegate = (ScriptingInterfaceOfIMBAgent.DebugMoreDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DebugMoreDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_defend_direction_to_movement_flag:
			ScriptingInterfaceOfIMBAgent.call_DefendDirectionToMovementFlagDelegate = (ScriptingInterfaceOfIMBAgent.DefendDirectionToMovementFlagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DefendDirectionToMovementFlagDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_delete_attached_weapon_from_bone:
			ScriptingInterfaceOfIMBAgent.call_DeleteAttachedWeaponFromBoneDelegate = (ScriptingInterfaceOfIMBAgent.DeleteAttachedWeaponFromBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DeleteAttachedWeaponFromBoneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_die:
			ScriptingInterfaceOfIMBAgent.call_DieDelegate = (ScriptingInterfaceOfIMBAgent.DieDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DieDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_disable_look_to_point_of_interest:
			ScriptingInterfaceOfIMBAgent.call_DisableLookToPointOfInterestDelegate = (ScriptingInterfaceOfIMBAgent.DisableLookToPointOfInterestDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DisableLookToPointOfInterestDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_disable_scripted_combat_movement:
			ScriptingInterfaceOfIMBAgent.call_DisableScriptedCombatMovementDelegate = (ScriptingInterfaceOfIMBAgent.DisableScriptedCombatMovementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DisableScriptedCombatMovementDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_disable_scripted_movement:
			ScriptingInterfaceOfIMBAgent.call_DisableScriptedMovementDelegate = (ScriptingInterfaceOfIMBAgent.DisableScriptedMovementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DisableScriptedMovementDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_drop_item:
			ScriptingInterfaceOfIMBAgent.call_DropItemDelegate = (ScriptingInterfaceOfIMBAgent.DropItemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.DropItemDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_end_ragdoll_as_corpse:
			ScriptingInterfaceOfIMBAgent.call_EndRagdollAsCorpseDelegate = (ScriptingInterfaceOfIMBAgent.EndRagdollAsCorpseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.EndRagdollAsCorpseDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_enforce_shield_usage:
			ScriptingInterfaceOfIMBAgent.call_EnforceShieldUsageDelegate = (ScriptingInterfaceOfIMBAgent.EnforceShieldUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.EnforceShieldUsageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_fade_in:
			ScriptingInterfaceOfIMBAgent.call_FadeInDelegate = (ScriptingInterfaceOfIMBAgent.FadeInDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.FadeInDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_fade_out:
			ScriptingInterfaceOfIMBAgent.call_FadeOutDelegate = (ScriptingInterfaceOfIMBAgent.FadeOutDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.FadeOutDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_find_longest_direct_move_to_position:
			ScriptingInterfaceOfIMBAgent.call_FindLongestDirectMoveToPositionDelegate = (ScriptingInterfaceOfIMBAgent.FindLongestDirectMoveToPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.FindLongestDirectMoveToPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_force_ai_behavior_selection:
			ScriptingInterfaceOfIMBAgent.call_ForceAiBehaviorSelectionDelegate = (ScriptingInterfaceOfIMBAgent.ForceAiBehaviorSelectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ForceAiBehaviorSelectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_action_channel_current_action_weight:
			ScriptingInterfaceOfIMBAgent.call_GetActionChannelCurrentActionWeightDelegate = (ScriptingInterfaceOfIMBAgent.GetActionChannelCurrentActionWeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetActionChannelCurrentActionWeightDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_action_channel_weight:
			ScriptingInterfaceOfIMBAgent.call_GetActionChannelWeightDelegate = (ScriptingInterfaceOfIMBAgent.GetActionChannelWeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetActionChannelWeightDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_action_direction:
			ScriptingInterfaceOfIMBAgent.call_GetActionDirectionDelegate = (ScriptingInterfaceOfIMBAgent.GetActionDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetActionDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_action_set_no:
			ScriptingInterfaceOfIMBAgent.call_GetActionSetNoDelegate = (ScriptingInterfaceOfIMBAgent.GetActionSetNoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetActionSetNoDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_agent_facial_animation:
			ScriptingInterfaceOfIMBAgent.call_GetAgentFacialAnimationDelegate = (ScriptingInterfaceOfIMBAgent.GetAgentFacialAnimationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAgentFacialAnimationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_agent_parent_entity:
			ScriptingInterfaceOfIMBAgent.call_GetAgentParentEntityDelegate = (ScriptingInterfaceOfIMBAgent.GetAgentParentEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAgentParentEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_agent_scale:
			ScriptingInterfaceOfIMBAgent.call_GetAgentScaleDelegate = (ScriptingInterfaceOfIMBAgent.GetAgentScaleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAgentScaleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_agent_visuals:
			ScriptingInterfaceOfIMBAgent.call_GetAgentVisualsDelegate = (ScriptingInterfaceOfIMBAgent.GetAgentVisualsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAgentVisualsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_agent_voice_definiton:
			ScriptingInterfaceOfIMBAgent.call_GetAgentVoiceDefinitionDelegate = (ScriptingInterfaceOfIMBAgent.GetAgentVoiceDefinitionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAgentVoiceDefinitionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_ai_last_suspicious_position:
			ScriptingInterfaceOfIMBAgent.call_GetAILastSuspiciousPositionDelegate = (ScriptingInterfaceOfIMBAgent.GetAILastSuspiciousPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAILastSuspiciousPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_aiming_timer:
			ScriptingInterfaceOfIMBAgent.call_GetAimingTimerDelegate = (ScriptingInterfaceOfIMBAgent.GetAimingTimerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAimingTimerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_ai_move_destination:
			ScriptingInterfaceOfIMBAgent.call_GetAIMoveDestinationDelegate = (ScriptingInterfaceOfIMBAgent.GetAIMoveDestinationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAIMoveDestinationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_ai_move_stop_tolerance:
			ScriptingInterfaceOfIMBAgent.call_GetAIMoveStopToleranceDelegate = (ScriptingInterfaceOfIMBAgent.GetAIMoveStopToleranceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAIMoveStopToleranceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_ai_state_flags:
			ScriptingInterfaceOfIMBAgent.call_GetAIStateFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetAIStateFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAIStateFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_attack_direction:
			ScriptingInterfaceOfIMBAgent.call_GetAttackDirectionDelegate = (ScriptingInterfaceOfIMBAgent.GetAttackDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAttackDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_attack_direction_usage:
			ScriptingInterfaceOfIMBAgent.call_GetAttackDirectionUsageDelegate = (ScriptingInterfaceOfIMBAgent.GetAttackDirectionUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAttackDirectionUsageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_average_real_global_velocity:
			ScriptingInterfaceOfIMBAgent.call_GetAverageRealGlobalVelocityDelegate = (ScriptingInterfaceOfIMBAgent.GetAverageRealGlobalVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAverageRealGlobalVelocityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_average_velocity:
			ScriptingInterfaceOfIMBAgent.call_GetAverageVelocityDelegate = (ScriptingInterfaceOfIMBAgent.GetAverageVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetAverageVelocityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_body_rotation_constraint:
			ScriptingInterfaceOfIMBAgent.call_GetBodyRotationConstraintDelegate = (ScriptingInterfaceOfIMBAgent.GetBodyRotationConstraintDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetBodyRotationConstraintDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_quick_bone_entitial_frame:
			ScriptingInterfaceOfIMBAgent.call_GetBoneEntitialFrameDelegate = (ScriptingInterfaceOfIMBAgent.GetBoneEntitialFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetBoneEntitialFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_bone_entitial_frame_at_animation_progress:
			ScriptingInterfaceOfIMBAgent.call_GetBoneEntitialFrameAtAnimationProgressDelegate = (ScriptingInterfaceOfIMBAgent.GetBoneEntitialFrameAtAnimationProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetBoneEntitialFrameAtAnimationProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_chest_global_position:
			ScriptingInterfaceOfIMBAgent.call_GetChestGlobalPositionDelegate = (ScriptingInterfaceOfIMBAgent.GetChestGlobalPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetChestGlobalPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_collision_capsule:
			ScriptingInterfaceOfIMBAgent.call_GetCollisionCapsuleDelegate = (ScriptingInterfaceOfIMBAgent.GetCollisionCapsuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCollisionCapsuleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_crouch_mode:
			ScriptingInterfaceOfIMBAgent.call_GetCrouchModeDelegate = (ScriptingInterfaceOfIMBAgent.GetCrouchModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCrouchModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_action_direction:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentActionDirectionDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentActionDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentActionDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_action_priority:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentActionPriorityDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentActionPriorityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentActionPriorityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_action_progress:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentActionProgressDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentActionProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentActionProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_action_stage:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentActionStageDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentActionStageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentActionStageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_action_type:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentActionTypeDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentActionTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentActionTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_aiming_error:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentAimingErrorDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentAimingErrorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentAimingErrorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_aiming_turbulance:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentAimingTurbulanceDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentAimingTurbulanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentAimingTurbulanceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_animation_flags:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentAnimationFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentAnimationFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentAnimationFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_guard_mode:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentGuardModeDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentGuardModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentGuardModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_navigation_face_id:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentNavigationFaceIdDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentNavigationFaceIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentNavigationFaceIdDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_speed_limit:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentSpeedLimitDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentSpeedLimitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentSpeedLimitDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_current_velocity:
			ScriptingInterfaceOfIMBAgent.call_GetCurrentVelocityDelegate = (ScriptingInterfaceOfIMBAgent.GetCurrentVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurrentVelocityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_cur_weapon_offset:
			ScriptingInterfaceOfIMBAgent.call_GetCurWeaponOffsetDelegate = (ScriptingInterfaceOfIMBAgent.GetCurWeaponOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetCurWeaponOffsetDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_defend_movement_flag:
			ScriptingInterfaceOfIMBAgent.call_GetDefendMovementFlagDelegate = (ScriptingInterfaceOfIMBAgent.GetDefendMovementFlagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetDefendMovementFlagDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_event_control_flags:
			ScriptingInterfaceOfIMBAgent.call_GetEventControlFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetEventControlFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetEventControlFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_eye_global_height:
			ScriptingInterfaceOfIMBAgent.call_GetEyeGlobalHeightDelegate = (ScriptingInterfaceOfIMBAgent.GetEyeGlobalHeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetEyeGlobalHeightDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_eye_global_position:
			ScriptingInterfaceOfIMBAgent.call_GetEyeGlobalPositionDelegate = (ScriptingInterfaceOfIMBAgent.GetEyeGlobalPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetEyeGlobalPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_firing_order:
			ScriptingInterfaceOfIMBAgent.call_GetFiringOrderDelegate = (ScriptingInterfaceOfIMBAgent.GetFiringOrderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetFiringOrderDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_ground_material_for_collision_effect:
			ScriptingInterfaceOfIMBAgent.call_GetGroundMaterialForCollisionEffectDelegate = (ScriptingInterfaceOfIMBAgent.GetGroundMaterialForCollisionEffectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetGroundMaterialForCollisionEffectDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_has_on_ai_input_set_callback:
			ScriptingInterfaceOfIMBAgent.call_GetHasOnAiInputSetCallbackDelegate = (ScriptingInterfaceOfIMBAgent.GetHasOnAiInputSetCallbackDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetHasOnAiInputSetCallbackDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_head_camera_mode:
			ScriptingInterfaceOfIMBAgent.call_GetHeadCameraModeDelegate = (ScriptingInterfaceOfIMBAgent.GetHeadCameraModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetHeadCameraModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_immediate_enemy:
			ScriptingInterfaceOfIMBAgent.call_GetImmediateEnemyDelegate = (ScriptingInterfaceOfIMBAgent.GetImmediateEnemyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetImmediateEnemyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_is_doing_passive_attack:
			ScriptingInterfaceOfIMBAgent.call_GetIsDoingPassiveAttackDelegate = (ScriptingInterfaceOfIMBAgent.GetIsDoingPassiveAttackDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetIsDoingPassiveAttackDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_is_left_stance:
			ScriptingInterfaceOfIMBAgent.call_GetIsLeftStanceDelegate = (ScriptingInterfaceOfIMBAgent.GetIsLeftStanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetIsLeftStanceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_is_look_direction_locked:
			ScriptingInterfaceOfIMBAgent.call_GetIsLookDirectionLockedDelegate = (ScriptingInterfaceOfIMBAgent.GetIsLookDirectionLockedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetIsLookDirectionLockedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_is_passive_usage_conditions_are_met:
			ScriptingInterfaceOfIMBAgent.call_GetIsPassiveUsageConditionsAreMetDelegate = (ScriptingInterfaceOfIMBAgent.GetIsPassiveUsageConditionsAreMetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetIsPassiveUsageConditionsAreMetDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_last_target_visibility_state:
			ScriptingInterfaceOfIMBAgent.call_GetLastTargetVisibilityStateDelegate = (ScriptingInterfaceOfIMBAgent.GetLastTargetVisibilityStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetLastTargetVisibilityStateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_look_agent:
			ScriptingInterfaceOfIMBAgent.call_GetLookAgentDelegate = (ScriptingInterfaceOfIMBAgent.GetLookAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetLookAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_look_direction:
			ScriptingInterfaceOfIMBAgent.call_GetLookDirectionDelegate = (ScriptingInterfaceOfIMBAgent.GetLookDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetLookDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_look_direction_as_angle:
			ScriptingInterfaceOfIMBAgent.call_GetLookDirectionAsAngleDelegate = (ScriptingInterfaceOfIMBAgent.GetLookDirectionAsAngleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetLookDirectionAsAngleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_look_down_limit:
			ScriptingInterfaceOfIMBAgent.call_GetLookDownLimitDelegate = (ScriptingInterfaceOfIMBAgent.GetLookDownLimitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetLookDownLimitDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_maximum_number_of_agents:
			ScriptingInterfaceOfIMBAgent.call_GetMaximumNumberOfAgentsDelegate = (ScriptingInterfaceOfIMBAgent.GetMaximumNumberOfAgentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMaximumNumberOfAgentsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_maximum_speed_limit:
			ScriptingInterfaceOfIMBAgent.call_GetMaximumSpeedLimitDelegate = (ScriptingInterfaceOfIMBAgent.GetMaximumSpeedLimitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMaximumSpeedLimitDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_missile_range:
			ScriptingInterfaceOfIMBAgent.call_GetMissileRangeDelegate = (ScriptingInterfaceOfIMBAgent.GetMissileRangeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMissileRangeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_missile_range_with_height_difference:
			ScriptingInterfaceOfIMBAgent.call_GetMissileRangeWithHeightDifferenceDelegate = (ScriptingInterfaceOfIMBAgent.GetMissileRangeWithHeightDifferenceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMissileRangeWithHeightDifferenceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_monster_usage_index:
			ScriptingInterfaceOfIMBAgent.call_GetMonsterUsageIndexDelegate = (ScriptingInterfaceOfIMBAgent.GetMonsterUsageIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMonsterUsageIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_mount_agent:
			ScriptingInterfaceOfIMBAgent.call_GetMountAgentDelegate = (ScriptingInterfaceOfIMBAgent.GetMountAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMountAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_movement_flags:
			ScriptingInterfaceOfIMBAgent.call_GetMovementFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetMovementFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMovementFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_movement_input_vector:
			ScriptingInterfaceOfIMBAgent.call_GetMovementInputVectorDelegate = (ScriptingInterfaceOfIMBAgent.GetMovementInputVectorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMovementInputVectorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_movement_locked_state:
			ScriptingInterfaceOfIMBAgent.call_GetMovementLockedStateDelegate = (ScriptingInterfaceOfIMBAgent.GetMovementLockedStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMovementLockedStateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_movement_velocity:
			ScriptingInterfaceOfIMBAgent.call_GetMovementVelocityDelegate = (ScriptingInterfaceOfIMBAgent.GetMovementVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetMovementVelocityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_native_action_index:
			ScriptingInterfaceOfIMBAgent.call_GetNativeActionIndexDelegate = (ScriptingInterfaceOfIMBAgent.GetNativeActionIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetNativeActionIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_old_wielded_item_info:
			ScriptingInterfaceOfIMBAgent.call_GetOldWieldedItemInfoDelegate = (ScriptingInterfaceOfIMBAgent.GetOldWieldedItemInfoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetOldWieldedItemInfoDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_path_distance_to_point:
			ScriptingInterfaceOfIMBAgent.call_GetPathDistanceToPointDelegate = (ScriptingInterfaceOfIMBAgent.GetPathDistanceToPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetPathDistanceToPointDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_position:
			ScriptingInterfaceOfIMBAgent.call_GetPositionDelegate = (ScriptingInterfaceOfIMBAgent.GetPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_real_global_velocity:
			ScriptingInterfaceOfIMBAgent.call_GetRealGlobalVelocityDelegate = (ScriptingInterfaceOfIMBAgent.GetRealGlobalVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetRealGlobalVelocityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_render_check_enabled:
			ScriptingInterfaceOfIMBAgent.call_GetRenderCheckEnabledDelegate = (ScriptingInterfaceOfIMBAgent.GetRenderCheckEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetRenderCheckEnabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_retreat_pos:
			ScriptingInterfaceOfIMBAgent.call_GetRetreatPosDelegate = (ScriptingInterfaceOfIMBAgent.GetRetreatPosDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetRetreatPosDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_rider_agent:
			ScriptingInterfaceOfIMBAgent.call_GetRiderAgentDelegate = (ScriptingInterfaceOfIMBAgent.GetRiderAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetRiderAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_riding_order:
			ScriptingInterfaceOfIMBAgent.call_GetRidingOrderDelegate = (ScriptingInterfaceOfIMBAgent.GetRidingOrderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetRidingOrderDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_rotation_frame:
			ScriptingInterfaceOfIMBAgent.call_GetRotationFrameDelegate = (ScriptingInterfaceOfIMBAgent.GetRotationFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetRotationFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_running_simulation_data_until_maximum_speed_reached:
			ScriptingInterfaceOfIMBAgent.call_GetRunningSimulationDataUntilMaximumSpeedReachedDelegate = (ScriptingInterfaceOfIMBAgent.GetRunningSimulationDataUntilMaximumSpeedReachedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetRunningSimulationDataUntilMaximumSpeedReachedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_scripted_combat_flags:
			ScriptingInterfaceOfIMBAgent.call_GetScriptedCombatFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetScriptedCombatFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetScriptedCombatFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_scripted_flags:
			ScriptingInterfaceOfIMBAgent.call_GetScriptedFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetScriptedFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetScriptedFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_selected_mount_index:
			ScriptingInterfaceOfIMBAgent.call_GetSelectedMountIndexDelegate = (ScriptingInterfaceOfIMBAgent.GetSelectedMountIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetSelectedMountIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_state_flags:
			ScriptingInterfaceOfIMBAgent.call_GetStateFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetStateFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetStateFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_stepped_body_flags:
			ScriptingInterfaceOfIMBAgent.call_GetSteppedBodyFlagsDelegate = (ScriptingInterfaceOfIMBAgent.GetSteppedBodyFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetSteppedBodyFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_stepped_entity_id:
			ScriptingInterfaceOfIMBAgent.call_GetSteppedEntityIdDelegate = (ScriptingInterfaceOfIMBAgent.GetSteppedEntityIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetSteppedEntityIdDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_stepped_root_entity_id:
			ScriptingInterfaceOfIMBAgent.call_GetSteppedRootEntityDelegate = (ScriptingInterfaceOfIMBAgent.GetSteppedRootEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetSteppedRootEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_target_agent:
			ScriptingInterfaceOfIMBAgent.call_GetTargetAgentDelegate = (ScriptingInterfaceOfIMBAgent.GetTargetAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetTargetAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_target_direction:
			ScriptingInterfaceOfIMBAgent.call_GetTargetDirectionDelegate = (ScriptingInterfaceOfIMBAgent.GetTargetDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetTargetDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_target_formation_index:
			ScriptingInterfaceOfIMBAgent.call_GetTargetFormationIndexDelegate = (ScriptingInterfaceOfIMBAgent.GetTargetFormationIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetTargetFormationIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_target_position:
			ScriptingInterfaceOfIMBAgent.call_GetTargetPositionDelegate = (ScriptingInterfaceOfIMBAgent.GetTargetPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetTargetPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_team:
			ScriptingInterfaceOfIMBAgent.call_GetTeamDelegate = (ScriptingInterfaceOfIMBAgent.GetTeamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetTeamDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_total_mass:
			ScriptingInterfaceOfIMBAgent.call_GetTotalMassDelegate = (ScriptingInterfaceOfIMBAgent.GetTotalMassDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetTotalMassDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_turn_speed:
			ScriptingInterfaceOfIMBAgent.call_GetTurnSpeedDelegate = (ScriptingInterfaceOfIMBAgent.GetTurnSpeedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetTurnSpeedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_visual_position:
			ScriptingInterfaceOfIMBAgent.call_GetVisualPositionDelegate = (ScriptingInterfaceOfIMBAgent.GetVisualPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetVisualPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_walk_mode:
			ScriptingInterfaceOfIMBAgent.call_GetWalkModeDelegate = (ScriptingInterfaceOfIMBAgent.GetWalkModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetWalkModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_walking_speed_limit_of_mountable:
			ScriptingInterfaceOfIMBAgent.call_GetWalkSpeedLimitOfMountableDelegate = (ScriptingInterfaceOfIMBAgent.GetWalkSpeedLimitOfMountableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetWalkSpeedLimitOfMountableDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_weapon_entity_from_equipment_slot:
			ScriptingInterfaceOfIMBAgent.call_GetWeaponEntityFromEquipmentSlotDelegate = (ScriptingInterfaceOfIMBAgent.GetWeaponEntityFromEquipmentSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetWeaponEntityFromEquipmentSlotDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_wielded_weapon_info:
			ScriptingInterfaceOfIMBAgent.call_GetWieldedWeaponInfoDelegate = (ScriptingInterfaceOfIMBAgent.GetWieldedWeaponInfoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetWieldedWeaponInfoDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_get_world_position:
			ScriptingInterfaceOfIMBAgent.call_GetWorldPositionDelegate = (ScriptingInterfaceOfIMBAgent.GetWorldPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.GetWorldPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_handle_blow_aux:
			ScriptingInterfaceOfIMBAgent.call_HandleBlowAuxDelegate = (ScriptingInterfaceOfIMBAgent.HandleBlowAuxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.HandleBlowAuxDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_has_path_through_navigation_face_id_from_direction:
			ScriptingInterfaceOfIMBAgent.call_HasPathThroughNavigationFaceIdFromDirectionDelegate = (ScriptingInterfaceOfIMBAgent.HasPathThroughNavigationFaceIdFromDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.HasPathThroughNavigationFaceIdFromDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_has_path_through_navigation_faces_id_from_direction:
			ScriptingInterfaceOfIMBAgent.call_HasPathThroughNavigationFacesIDFromDirectionDelegate = (ScriptingInterfaceOfIMBAgent.HasPathThroughNavigationFacesIDFromDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.HasPathThroughNavigationFacesIDFromDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_initialize_agent_record:
			ScriptingInterfaceOfIMBAgent.call_InitializeAgentRecordDelegate = (ScriptingInterfaceOfIMBAgent.InitializeAgentRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.InitializeAgentRecordDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_invalidate_ai_weapon_selections:
			ScriptingInterfaceOfIMBAgent.call_InvalidateAIWeaponSelectionsDelegate = (ScriptingInterfaceOfIMBAgent.InvalidateAIWeaponSelectionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.InvalidateAIWeaponSelectionsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_invalidate_target_agent:
			ScriptingInterfaceOfIMBAgent.call_InvalidateTargetAgentDelegate = (ScriptingInterfaceOfIMBAgent.InvalidateTargetAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.InvalidateTargetAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_added_as_corpse:
			ScriptingInterfaceOfIMBAgent.call_IsAddedAsCorpseDelegate = (ScriptingInterfaceOfIMBAgent.IsAddedAsCorpseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsAddedAsCorpseDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_crouching_allowed:
			ScriptingInterfaceOfIMBAgent.call_IsCrouchingAllowedDelegate = (ScriptingInterfaceOfIMBAgent.IsCrouchingAllowedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsCrouchingAllowedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_enemy:
			ScriptingInterfaceOfIMBAgent.call_IsEnemyDelegate = (ScriptingInterfaceOfIMBAgent.IsEnemyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsEnemyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_fading_out:
			ScriptingInterfaceOfIMBAgent.call_IsFadingOutDelegate = (ScriptingInterfaceOfIMBAgent.IsFadingOutDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsFadingOutDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_friend:
			ScriptingInterfaceOfIMBAgent.call_IsFriendDelegate = (ScriptingInterfaceOfIMBAgent.IsFriendDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsFriendDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_look_rotation_in_slow_motion:
			ScriptingInterfaceOfIMBAgent.call_IsLookRotationInSlowMotionDelegate = (ScriptingInterfaceOfIMBAgent.IsLookRotationInSlowMotionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsLookRotationInSlowMotionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_retreating:
			ScriptingInterfaceOfIMBAgent.call_IsRetreatingDelegate = (ScriptingInterfaceOfIMBAgent.IsRetreatingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsRetreatingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_running_away:
			ScriptingInterfaceOfIMBAgent.call_IsRunningAwayDelegate = (ScriptingInterfaceOfIMBAgent.IsRunningAwayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsRunningAwayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_sliding:
			ScriptingInterfaceOfIMBAgent.call_IsSlidingDelegate = (ScriptingInterfaceOfIMBAgent.IsSlidingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsSlidingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_target_navigation_face_id_between:
			ScriptingInterfaceOfIMBAgent.call_IsTargetNavigationFaceIdBetweenDelegate = (ScriptingInterfaceOfIMBAgent.IsTargetNavigationFaceIdBetweenDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsTargetNavigationFaceIdBetweenDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_is_wandering:
			ScriptingInterfaceOfIMBAgent.call_IsWanderingDelegate = (ScriptingInterfaceOfIMBAgent.IsWanderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.IsWanderingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_kick_clear:
			ScriptingInterfaceOfIMBAgent.call_KickClearDelegate = (ScriptingInterfaceOfIMBAgent.KickClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.KickClearDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_lock_agent_replication_table_with_current_reliable_sequence_no:
			ScriptingInterfaceOfIMBAgent.call_LockAgentReplicationTableDataWithCurrentReliableSequenceNoDelegate = (ScriptingInterfaceOfIMBAgent.LockAgentReplicationTableDataWithCurrentReliableSequenceNoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.LockAgentReplicationTableDataWithCurrentReliableSequenceNoDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_make_dead:
			ScriptingInterfaceOfIMBAgent.call_MakeDeadDelegate = (ScriptingInterfaceOfIMBAgent.MakeDeadDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.MakeDeadDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_make_voice:
			ScriptingInterfaceOfIMBAgent.call_MakeVoiceDelegate = (ScriptingInterfaceOfIMBAgent.MakeVoiceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.MakeVoiceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_player_attack_direction:
			ScriptingInterfaceOfIMBAgent.call_PlayerAttackDirectionDelegate = (ScriptingInterfaceOfIMBAgent.PlayerAttackDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.PlayerAttackDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_preload_for_rendering:
			ScriptingInterfaceOfIMBAgent.call_PreloadForRenderingDelegate = (ScriptingInterfaceOfIMBAgent.PreloadForRenderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.PreloadForRenderingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_prepare_weapon_for_drop_in_equipment_slot:
			ScriptingInterfaceOfIMBAgent.call_PrepareWeaponForDropInEquipmentSlotDelegate = (ScriptingInterfaceOfIMBAgent.PrepareWeaponForDropInEquipmentSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.PrepareWeaponForDropInEquipmentSlotDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_remove_mesh_from_bone:
			ScriptingInterfaceOfIMBAgent.call_RemoveMeshFromBoneDelegate = (ScriptingInterfaceOfIMBAgent.RemoveMeshFromBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.RemoveMeshFromBoneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_reset_enemy_caches:
			ScriptingInterfaceOfIMBAgent.call_ResetEnemyCachesDelegate = (ScriptingInterfaceOfIMBAgent.ResetEnemyCachesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ResetEnemyCachesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_reset_guard:
			ScriptingInterfaceOfIMBAgent.call_ResetGuardDelegate = (ScriptingInterfaceOfIMBAgent.ResetGuardDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.ResetGuardDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_action_channel:
			ScriptingInterfaceOfIMBAgent.call_SetActionChannelDelegate = (ScriptingInterfaceOfIMBAgent.SetActionChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetActionChannelDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_action_set:
			ScriptingInterfaceOfIMBAgent.call_SetActionSetDelegate = (ScriptingInterfaceOfIMBAgent.SetActionSetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetActionSetDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_agent_exclude_state_for_face_group_id:
			ScriptingInterfaceOfIMBAgent.call_SetAgentExcludeStateForFaceGroupIdDelegate = (ScriptingInterfaceOfIMBAgent.SetAgentExcludeStateForFaceGroupIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAgentExcludeStateForFaceGroupIdDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_agent_facial_animation:
			ScriptingInterfaceOfIMBAgent.call_SetAgentFacialAnimationDelegate = (ScriptingInterfaceOfIMBAgent.SetAgentFacialAnimationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAgentFacialAnimationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_agent_flags:
			ScriptingInterfaceOfIMBAgent.call_SetAgentFlagsDelegate = (ScriptingInterfaceOfIMBAgent.SetAgentFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAgentFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_agent_idle_animation_status:
			ScriptingInterfaceOfIMBAgent.call_SetAgentIdleAnimationStatusDelegate = (ScriptingInterfaceOfIMBAgent.SetAgentIdleAnimationStatusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAgentIdleAnimationStatusDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_agent_scale:
			ScriptingInterfaceOfIMBAgent.call_SetAgentScaleDelegate = (ScriptingInterfaceOfIMBAgent.SetAgentScaleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAgentScaleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_ai_alarm_state:
			ScriptingInterfaceOfIMBAgent.call_SetAIAlarmStateDelegate = (ScriptingInterfaceOfIMBAgent.SetAIAlarmStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAIAlarmStateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_ai_behavior_params:
			ScriptingInterfaceOfIMBAgent.call_SetAIBehaviorParamsDelegate = (ScriptingInterfaceOfIMBAgent.SetAIBehaviorParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAIBehaviorParamsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_ai_last_suspicious_position:
			ScriptingInterfaceOfIMBAgent.call_SetAILastSuspiciousPositionDelegate = (ScriptingInterfaceOfIMBAgent.SetAILastSuspiciousPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAILastSuspiciousPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_ai_state_flags:
			ScriptingInterfaceOfIMBAgent.call_SetAIStateFlagsDelegate = (ScriptingInterfaceOfIMBAgent.SetAIStateFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAIStateFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_all_ai_behavior_params:
			ScriptingInterfaceOfIMBAgent.call_SetAllAIBehaviorParamsDelegate = (ScriptingInterfaceOfIMBAgent.SetAllAIBehaviorParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAllAIBehaviorParamsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_attack_state:
			ScriptingInterfaceOfIMBAgent.call_SetAttackStateDelegate = (ScriptingInterfaceOfIMBAgent.SetAttackStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAttackStateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_automatic_target_agent_selection:
			ScriptingInterfaceOfIMBAgent.call_SetAutomaticTargetSelectionDelegate = (ScriptingInterfaceOfIMBAgent.SetAutomaticTargetSelectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAutomaticTargetSelectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_average_ping_in_milliseconds:
			ScriptingInterfaceOfIMBAgent.call_SetAveragePingInMillisecondsDelegate = (ScriptingInterfaceOfIMBAgent.SetAveragePingInMillisecondsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetAveragePingInMillisecondsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_body_armor_material_type:
			ScriptingInterfaceOfIMBAgent.call_SetBodyArmorMaterialTypeDelegate = (ScriptingInterfaceOfIMBAgent.SetBodyArmorMaterialTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetBodyArmorMaterialTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_columnwise_follow_agent:
			ScriptingInterfaceOfIMBAgent.call_SetColumnwiseFollowAgentDelegate = (ScriptingInterfaceOfIMBAgent.SetColumnwiseFollowAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetColumnwiseFollowAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_controller:
			ScriptingInterfaceOfIMBAgent.call_SetControllerDelegate = (ScriptingInterfaceOfIMBAgent.SetControllerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetControllerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_courage:
			ScriptingInterfaceOfIMBAgent.call_SetCourageDelegate = (ScriptingInterfaceOfIMBAgent.SetCourageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetCourageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_current_action_progress:
			ScriptingInterfaceOfIMBAgent.call_SetCurrentActionProgressDelegate = (ScriptingInterfaceOfIMBAgent.SetCurrentActionProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetCurrentActionProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_current_action_speed:
			ScriptingInterfaceOfIMBAgent.call_SetCurrentActionSpeedDelegate = (ScriptingInterfaceOfIMBAgent.SetCurrentActionSpeedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetCurrentActionSpeedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_direction_change_tendency:
			ScriptingInterfaceOfIMBAgent.call_SetDirectionChangeTendencyDelegate = (ScriptingInterfaceOfIMBAgent.SetDirectionChangeTendencyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetDirectionChangeTendencyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_event_control_flags:
			ScriptingInterfaceOfIMBAgent.call_SetEventControlFlagsDelegate = (ScriptingInterfaceOfIMBAgent.SetEventControlFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetEventControlFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_excluded_from_gravity:
			ScriptingInterfaceOfIMBAgent.call_SetExcludedFromGravityDelegate = (ScriptingInterfaceOfIMBAgent.SetExcludedFromGravityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetExcludedFromGravityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_firing_order:
			ScriptingInterfaceOfIMBAgent.call_SetFiringOrderDelegate = (ScriptingInterfaceOfIMBAgent.SetFiringOrderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetFiringOrderDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_force_attached_entity:
			ScriptingInterfaceOfIMBAgent.call_SetForceAttachedEntityDelegate = (ScriptingInterfaceOfIMBAgent.SetForceAttachedEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetForceAttachedEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_formation_frame_disabled:
			ScriptingInterfaceOfIMBAgent.call_SetFormationFrameDisabledDelegate = (ScriptingInterfaceOfIMBAgent.SetFormationFrameDisabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetFormationFrameDisabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_formation_frame_enabled:
			ScriptingInterfaceOfIMBAgent.call_SetFormationFrameEnabledDelegate = (ScriptingInterfaceOfIMBAgent.SetFormationFrameEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetFormationFrameEnabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_formation_info:
			ScriptingInterfaceOfIMBAgent.call_SetFormationInfoDelegate = (ScriptingInterfaceOfIMBAgent.SetFormationInfoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetFormationInfoDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_formation_integrity_data:
			ScriptingInterfaceOfIMBAgent.call_SetFormationIntegrityDataDelegate = (ScriptingInterfaceOfIMBAgent.SetFormationIntegrityDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetFormationIntegrityDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_formation_no:
			ScriptingInterfaceOfIMBAgent.call_SetFormationNoDelegate = (ScriptingInterfaceOfIMBAgent.SetFormationNoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetFormationNoDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_hand_inverse_kinematics_frame:
			ScriptingInterfaceOfIMBAgent.call_SetHandInverseKinematicsFrameDelegate = (ScriptingInterfaceOfIMBAgent.SetHandInverseKinematicsFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetHandInverseKinematicsFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_hand_inverse_kinematics_frame_for_mission_object_usage:
			ScriptingInterfaceOfIMBAgent.call_SetHandInverseKinematicsFrameForMissionObjectUsageDelegate = (ScriptingInterfaceOfIMBAgent.SetHandInverseKinematicsFrameForMissionObjectUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetHandInverseKinematicsFrameForMissionObjectUsageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_has_on_ai_input_set_callback:
			ScriptingInterfaceOfIMBAgent.call_SetHasOnAiInputSetCallbackDelegate = (ScriptingInterfaceOfIMBAgent.SetHasOnAiInputSetCallbackDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetHasOnAiInputSetCallbackDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_head_camera_mode:
			ScriptingInterfaceOfIMBAgent.call_SetHeadCameraModeDelegate = (ScriptingInterfaceOfIMBAgent.SetHeadCameraModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetHeadCameraModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_initial_frame:
			ScriptingInterfaceOfIMBAgent.call_SetInitialFrameDelegate = (ScriptingInterfaceOfIMBAgent.SetInitialFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetInitialFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_interaction_agent:
			ScriptingInterfaceOfIMBAgent.call_SetInteractionAgentDelegate = (ScriptingInterfaceOfIMBAgent.SetInteractionAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetInteractionAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_is_look_direction_locked:
			ScriptingInterfaceOfIMBAgent.call_SetIsLookDirectionLockedDelegate = (ScriptingInterfaceOfIMBAgent.SetIsLookDirectionLockedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetIsLookDirectionLockedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_is_physics_force_closed:
			ScriptingInterfaceOfIMBAgent.call_SetIsPhysicsForceClosedDelegate = (ScriptingInterfaceOfIMBAgent.SetIsPhysicsForceClosedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetIsPhysicsForceClosedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_look_agent:
			ScriptingInterfaceOfIMBAgent.call_SetLookAgentDelegate = (ScriptingInterfaceOfIMBAgent.SetLookAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetLookAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_look_direction:
			ScriptingInterfaceOfIMBAgent.call_SetLookDirectionDelegate = (ScriptingInterfaceOfIMBAgent.SetLookDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetLookDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_look_direction_as_angle:
			ScriptingInterfaceOfIMBAgent.call_SetLookDirectionAsAngleDelegate = (ScriptingInterfaceOfIMBAgent.SetLookDirectionAsAngleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetLookDirectionAsAngleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_look_to_point_of_interest:
			ScriptingInterfaceOfIMBAgent.call_SetLookToPointOfInterestDelegate = (ScriptingInterfaceOfIMBAgent.SetLookToPointOfInterestDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetLookToPointOfInterestDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_maximum_speed_limit:
			ScriptingInterfaceOfIMBAgent.call_SetMaximumSpeedLimitDelegate = (ScriptingInterfaceOfIMBAgent.SetMaximumSpeedLimitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetMaximumSpeedLimitDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_mono_object:
			ScriptingInterfaceOfIMBAgent.call_SetMonoObjectDelegate = (ScriptingInterfaceOfIMBAgent.SetMonoObjectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetMonoObjectDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_mount_agent:
			ScriptingInterfaceOfIMBAgent.call_SetMountAgentDelegate = (ScriptingInterfaceOfIMBAgent.SetMountAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetMountAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_movement_direction:
			ScriptingInterfaceOfIMBAgent.call_SetMovementDirectionDelegate = (ScriptingInterfaceOfIMBAgent.SetMovementDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetMovementDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_movement_flags:
			ScriptingInterfaceOfIMBAgent.call_SetMovementFlagsDelegate = (ScriptingInterfaceOfIMBAgent.SetMovementFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetMovementFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_movement_input_vector:
			ScriptingInterfaceOfIMBAgent.call_SetMovementInputVectorDelegate = (ScriptingInterfaceOfIMBAgent.SetMovementInputVectorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetMovementInputVectorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_network_peer:
			ScriptingInterfaceOfIMBAgent.call_SetNetworkPeerDelegate = (ScriptingInterfaceOfIMBAgent.SetNetworkPeerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetNetworkPeerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_overriden_strike_and_death_action:
			ScriptingInterfaceOfIMBAgent.call_SetOverridenStrikeAndDeathActionDelegate = (ScriptingInterfaceOfIMBAgent.SetOverridenStrikeAndDeathActionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetOverridenStrikeAndDeathActionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_position:
			ScriptingInterfaceOfIMBAgent.call_SetPositionDelegate = (ScriptingInterfaceOfIMBAgent.SetPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_reload_ammo_in_slot:
			ScriptingInterfaceOfIMBAgent.call_SetReloadAmmoInSlotDelegate = (ScriptingInterfaceOfIMBAgent.SetReloadAmmoInSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetReloadAmmoInSlotDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_render_check_enabled:
			ScriptingInterfaceOfIMBAgent.call_SetRenderCheckEnabledDelegate = (ScriptingInterfaceOfIMBAgent.SetRenderCheckEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetRenderCheckEnabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_retreat_mode:
			ScriptingInterfaceOfIMBAgent.call_SetRetreatModeDelegate = (ScriptingInterfaceOfIMBAgent.SetRetreatModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetRetreatModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_riding_order:
			ScriptingInterfaceOfIMBAgent.call_SetRidingOrderDelegate = (ScriptingInterfaceOfIMBAgent.SetRidingOrderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetRidingOrderDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_scripted_combat_flags:
			ScriptingInterfaceOfIMBAgent.call_SetScriptedCombatFlagsDelegate = (ScriptingInterfaceOfIMBAgent.SetScriptedCombatFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetScriptedCombatFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_scripted_flags:
			ScriptingInterfaceOfIMBAgent.call_SetScriptedFlagsDelegate = (ScriptingInterfaceOfIMBAgent.SetScriptedFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetScriptedFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_scripted_position:
			ScriptingInterfaceOfIMBAgent.call_SetScriptedPositionDelegate = (ScriptingInterfaceOfIMBAgent.SetScriptedPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetScriptedPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_scripted_position_and_direction:
			ScriptingInterfaceOfIMBAgent.call_SetScriptedPositionAndDirectionDelegate = (ScriptingInterfaceOfIMBAgent.SetScriptedPositionAndDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetScriptedPositionAndDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_scripted_target_entity:
			ScriptingInterfaceOfIMBAgent.call_SetScriptedTargetEntityDelegate = (ScriptingInterfaceOfIMBAgent.SetScriptedTargetEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetScriptedTargetEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_selected_mount_index:
			ScriptingInterfaceOfIMBAgent.call_SetSelectedMountIndexDelegate = (ScriptingInterfaceOfIMBAgent.SetSelectedMountIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetSelectedMountIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_should_catch_up_with_formation:
			ScriptingInterfaceOfIMBAgent.call_SetShouldCatchUpWithFormationDelegate = (ScriptingInterfaceOfIMBAgent.SetShouldCatchUpWithFormationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetShouldCatchUpWithFormationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_state_flags:
			ScriptingInterfaceOfIMBAgent.call_SetStateFlagsDelegate = (ScriptingInterfaceOfIMBAgent.SetStateFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetStateFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_target_agent:
			ScriptingInterfaceOfIMBAgent.call_SetTargetAgentDelegate = (ScriptingInterfaceOfIMBAgent.SetTargetAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetTargetAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_target_formation_index:
			ScriptingInterfaceOfIMBAgent.call_SetTargetFormationIndexDelegate = (ScriptingInterfaceOfIMBAgent.SetTargetFormationIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetTargetFormationIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_target_position:
			ScriptingInterfaceOfIMBAgent.call_SetTargetPositionDelegate = (ScriptingInterfaceOfIMBAgent.SetTargetPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetTargetPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_target_position_and_direction:
			ScriptingInterfaceOfIMBAgent.call_SetTargetPositionAndDirectionDelegate = (ScriptingInterfaceOfIMBAgent.SetTargetPositionAndDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetTargetPositionAndDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_target_up:
			ScriptingInterfaceOfIMBAgent.call_SetTargetUpDelegate = (ScriptingInterfaceOfIMBAgent.SetTargetUpDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetTargetUpDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_target_z:
			ScriptingInterfaceOfIMBAgent.call_SetTargetZDelegate = (ScriptingInterfaceOfIMBAgent.SetTargetZDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetTargetZDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_team:
			ScriptingInterfaceOfIMBAgent.call_SetTeamDelegate = (ScriptingInterfaceOfIMBAgent.SetTeamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetTeamDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_usage_index_of_weapon_in_slot_as_client:
			ScriptingInterfaceOfIMBAgent.call_SetUsageIndexOfWeaponInSlotAsClientDelegate = (ScriptingInterfaceOfIMBAgent.SetUsageIndexOfWeaponInSlotAsClientDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetUsageIndexOfWeaponInSlotAsClientDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_velocity_limits_on_ragdoll:
			ScriptingInterfaceOfIMBAgent.call_SetVelocityLimitsOnRagdollDelegate = (ScriptingInterfaceOfIMBAgent.SetVelocityLimitsOnRagdollDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetVelocityLimitsOnRagdollDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_weapon_ammo_as_client:
			ScriptingInterfaceOfIMBAgent.call_SetWeaponAmmoAsClientDelegate = (ScriptingInterfaceOfIMBAgent.SetWeaponAmmoAsClientDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetWeaponAmmoAsClientDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_weapon_amount_in_slot:
			ScriptingInterfaceOfIMBAgent.call_SetWeaponAmountInSlotDelegate = (ScriptingInterfaceOfIMBAgent.SetWeaponAmountInSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetWeaponAmountInSlotDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_weapon_guard:
			ScriptingInterfaceOfIMBAgent.call_SetWeaponGuardDelegate = (ScriptingInterfaceOfIMBAgent.SetWeaponGuardDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetWeaponGuardDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_weapon_hit_points_in_slot:
			ScriptingInterfaceOfIMBAgent.call_SetWeaponHitPointsInSlotDelegate = (ScriptingInterfaceOfIMBAgent.SetWeaponHitPointsInSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetWeaponHitPointsInSlotDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_weapon_reload_phase_as_client:
			ScriptingInterfaceOfIMBAgent.call_SetWeaponReloadPhaseAsClientDelegate = (ScriptingInterfaceOfIMBAgent.SetWeaponReloadPhaseAsClientDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetWeaponReloadPhaseAsClientDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_set_wielded_item_index_as_client:
			ScriptingInterfaceOfIMBAgent.call_SetWieldedItemIndexAsClientDelegate = (ScriptingInterfaceOfIMBAgent.SetWieldedItemIndexAsClientDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.SetWieldedItemIndexAsClientDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_start_fading_out:
			ScriptingInterfaceOfIMBAgent.call_StartFadingOutDelegate = (ScriptingInterfaceOfIMBAgent.StartFadingOutDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.StartFadingOutDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_start_ragdoll_as_corpse:
			ScriptingInterfaceOfIMBAgent.call_StartRagdollAsCorpseDelegate = (ScriptingInterfaceOfIMBAgent.StartRagdollAsCorpseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.StartRagdollAsCorpseDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_start_switching_weapon_usage_index_as_client:
			ScriptingInterfaceOfIMBAgent.call_StartSwitchingWeaponUsageIndexAsClientDelegate = (ScriptingInterfaceOfIMBAgent.StartSwitchingWeaponUsageIndexAsClientDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.StartSwitchingWeaponUsageIndexAsClientDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_tick_action_channels:
			ScriptingInterfaceOfIMBAgent.call_TickActionChannelsDelegate = (ScriptingInterfaceOfIMBAgent.TickActionChannelsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.TickActionChannelsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_try_get_immediate_agent_movement_data:
			ScriptingInterfaceOfIMBAgent.call_TryGetImmediateEnemyAgentMovementDataDelegate = (ScriptingInterfaceOfIMBAgent.TryGetImmediateEnemyAgentMovementDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.TryGetImmediateEnemyAgentMovementDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_try_to_sheath_weapon_in_hand:
			ScriptingInterfaceOfIMBAgent.call_TryToSheathWeaponInHandDelegate = (ScriptingInterfaceOfIMBAgent.TryToSheathWeaponInHandDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.TryToSheathWeaponInHandDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_try_to_wield_weapon_in_slot:
			ScriptingInterfaceOfIMBAgent.call_TryToWieldWeaponInSlotDelegate = (ScriptingInterfaceOfIMBAgent.TryToWieldWeaponInSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.TryToWieldWeaponInSlotDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_update_driven_properties:
			ScriptingInterfaceOfIMBAgent.call_UpdateDrivenPropertiesDelegate = (ScriptingInterfaceOfIMBAgent.UpdateDrivenPropertiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.UpdateDrivenPropertiesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_update_weapons:
			ScriptingInterfaceOfIMBAgent.call_UpdateWeaponsDelegate = (ScriptingInterfaceOfIMBAgent.UpdateWeaponsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.UpdateWeaponsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_weapon_equipped:
			ScriptingInterfaceOfIMBAgent.call_WeaponEquippedDelegate = (ScriptingInterfaceOfIMBAgent.WeaponEquippedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.WeaponEquippedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_wield_next_weapon:
			ScriptingInterfaceOfIMBAgent.call_WieldNextWeaponDelegate = (ScriptingInterfaceOfIMBAgent.WieldNextWeaponDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.WieldNextWeaponDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgent_yell_after_delay:
			ScriptingInterfaceOfIMBAgent.call_YellAfterDelayDelegate = (ScriptingInterfaceOfIMBAgent.YellAfterDelayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgent.YellAfterDelayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_child_entity:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddChildEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddChildEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddChildEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_horse_reins_cloth_mesh:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddHorseReinsClothMeshDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddHorseReinsClothMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddHorseReinsClothMeshDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_mesh:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddMeshDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddMeshDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_multi_mesh:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddMultiMeshDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddMultiMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddMultiMeshDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_prefab_to_agent_visual_bone_by_bone_type:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddPrefabToAgentVisualBoneByBoneTypeDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddPrefabToAgentVisualBoneByBoneTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddPrefabToAgentVisualBoneByBoneTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_prefab_to_agent_visual_bone_by_real_bone_index:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddPrefabToAgentVisualBoneByRealBoneIndexDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_skin_meshes_to_agent_visuals:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddSkinMeshesToAgentEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddSkinMeshesToAgentEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddSkinMeshesToAgentEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_add_weapon_to_agent_entity:
			ScriptingInterfaceOfIMBAgentVisuals.call_AddWeaponToAgentEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.AddWeaponToAgentEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.AddWeaponToAgentEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_apply_skeleton_scale:
			ScriptingInterfaceOfIMBAgentVisuals.call_ApplySkeletonScaleDelegate = (ScriptingInterfaceOfIMBAgentVisuals.ApplySkeletonScaleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.ApplySkeletonScaleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_batch_last_lod_meshes:
			ScriptingInterfaceOfIMBAgentVisuals.call_BatchLastLodMeshesDelegate = (ScriptingInterfaceOfIMBAgentVisuals.BatchLastLodMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.BatchLastLodMeshesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_check_resources:
			ScriptingInterfaceOfIMBAgentVisuals.call_CheckResourcesDelegate = (ScriptingInterfaceOfIMBAgentVisuals.CheckResourcesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.CheckResourcesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_clear_all_weapon_meshes:
			ScriptingInterfaceOfIMBAgentVisuals.call_ClearAllWeaponMeshesDelegate = (ScriptingInterfaceOfIMBAgentVisuals.ClearAllWeaponMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.ClearAllWeaponMeshesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_clear_visual_components:
			ScriptingInterfaceOfIMBAgentVisuals.call_ClearVisualComponentsDelegate = (ScriptingInterfaceOfIMBAgentVisuals.ClearVisualComponentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.ClearVisualComponentsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_clear_weapon_meshes:
			ScriptingInterfaceOfIMBAgentVisuals.call_ClearWeaponMeshesDelegate = (ScriptingInterfaceOfIMBAgentVisuals.ClearWeaponMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.ClearWeaponMeshesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_create_agent_renderer_scene_controller:
			ScriptingInterfaceOfIMBAgentVisuals.call_CreateAgentRendererSceneControllerDelegate = (ScriptingInterfaceOfIMBAgentVisuals.CreateAgentRendererSceneControllerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.CreateAgentRendererSceneControllerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_create_agent_visuals:
			ScriptingInterfaceOfIMBAgentVisuals.call_CreateAgentVisualsDelegate = (ScriptingInterfaceOfIMBAgentVisuals.CreateAgentVisualsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.CreateAgentVisualsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_create_particle_system_attached_to_bone:
			ScriptingInterfaceOfIMBAgentVisuals.call_CreateParticleSystemAttachedToBoneDelegate = (ScriptingInterfaceOfIMBAgentVisuals.CreateParticleSystemAttachedToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.CreateParticleSystemAttachedToBoneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_destruct_agent_renderer_scene_controller:
			ScriptingInterfaceOfIMBAgentVisuals.call_DestructAgentRendererSceneControllerDelegate = (ScriptingInterfaceOfIMBAgentVisuals.DestructAgentRendererSceneControllerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.DestructAgentRendererSceneControllerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_disable_contour:
			ScriptingInterfaceOfIMBAgentVisuals.call_DisableContourDelegate = (ScriptingInterfaceOfIMBAgentVisuals.DisableContourDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.DisableContourDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_fill_entity_with_body_meshes_without_agent_visuals:
			ScriptingInterfaceOfIMBAgentVisuals.call_FillEntityWithBodyMeshesWithoutAgentVisualsDelegate = (ScriptingInterfaceOfIMBAgentVisuals.FillEntityWithBodyMeshesWithoutAgentVisualsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.FillEntityWithBodyMeshesWithoutAgentVisualsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_attached_weapon_entity:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetAttachedWeaponEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetAttachedWeaponEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetAttachedWeaponEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_quick_bone_entitial_frame:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetBoneEntitialFrameDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetBoneEntitialFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetBoneEntitialFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_bone_entitial_frame_at_animation_progress:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetBoneEntitialFrameAtAnimationProgressDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetBoneEntitialFrameAtAnimationProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetBoneEntitialFrameAtAnimationProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_bone_type_data:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetBoneTypeDataDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetBoneTypeDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetBoneTypeDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_current_head_look_direction:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetCurrentHeadLookDirectionDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetCurrentHeadLookDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetCurrentHeadLookDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_current_helmet_scaling_factor:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetCurrentHelmetScalingFactorDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetCurrentHelmetScalingFactorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetCurrentHelmetScalingFactorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_current_ragdoll_state:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetCurrentRagdollStateDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetCurrentRagdollStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetCurrentRagdollStateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_entity:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_entity_pointer:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetEntityPointerDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetEntityPointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetEntityPointerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_frame:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetFrameDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_global_frame:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetGlobalFrameDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetGlobalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetGlobalFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_global_stable_eye_point:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetGlobalStableEyePointDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetGlobalStableEyePointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetGlobalStableEyePointDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_global_stable_neck_point:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetGlobalStableNeckPointDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetGlobalStableNeckPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetGlobalStableNeckPointDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_movement_mode:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetMovementModeDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetMovementModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetMovementModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_real_bone_index:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetRealBoneIndexDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetRealBoneIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetRealBoneIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_skeleton:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetSkeletonDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetSkeletonDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_visible:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetVisibleDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetVisibleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_get_visual_strength_of_agent_visual:
			ScriptingInterfaceOfIMBAgentVisuals.call_GetVisualStrengthOfAgentVisualDelegate = (ScriptingInterfaceOfIMBAgentVisuals.GetVisualStrengthOfAgentVisualDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.GetVisualStrengthOfAgentVisualDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_is_valid:
			ScriptingInterfaceOfIMBAgentVisuals.call_IsValidDelegate = (ScriptingInterfaceOfIMBAgentVisuals.IsValidDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.IsValidDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_lazy_update_agent_renderer_data:
			ScriptingInterfaceOfIMBAgentVisuals.call_LazyUpdateAgentRendererDataDelegate = (ScriptingInterfaceOfIMBAgentVisuals.LazyUpdateAgentRendererDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.LazyUpdateAgentRendererDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_make_voice:
			ScriptingInterfaceOfIMBAgentVisuals.call_MakeVoiceDelegate = (ScriptingInterfaceOfIMBAgentVisuals.MakeVoiceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.MakeVoiceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_remove_child_entity:
			ScriptingInterfaceOfIMBAgentVisuals.call_RemoveChildEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.RemoveChildEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.RemoveChildEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_remove_mesh:
			ScriptingInterfaceOfIMBAgentVisuals.call_RemoveMeshDelegate = (ScriptingInterfaceOfIMBAgentVisuals.RemoveMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.RemoveMeshDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_remove_multi_mesh:
			ScriptingInterfaceOfIMBAgentVisuals.call_RemoveMultiMeshDelegate = (ScriptingInterfaceOfIMBAgentVisuals.RemoveMultiMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.RemoveMultiMeshDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_reset:
			ScriptingInterfaceOfIMBAgentVisuals.call_ResetDelegate = (ScriptingInterfaceOfIMBAgentVisuals.ResetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.ResetDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_reset_next_frame:
			ScriptingInterfaceOfIMBAgentVisuals.call_ResetNextFrameDelegate = (ScriptingInterfaceOfIMBAgentVisuals.ResetNextFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.ResetNextFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_agent_local_speed:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetAgentLocalSpeedDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetAgentLocalSpeedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetAgentLocalSpeedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_agent_lod_make_zero_or_max:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetAgentLodMakeZeroOrMaxDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetAgentLodMakeZeroOrMaxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetAgentLodMakeZeroOrMaxDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_as_contour_entity:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetAsContourEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetAsContourEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetAsContourEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_attached_position_for_rope_entity_after_animation_post_integrate:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetAttachedPositionForRopeEntityAfterAnimationPostIntegrateDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetAttachedPositionForRopeEntityAfterAnimationPostIntegrateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetAttachedPositionForRopeEntityAfterAnimationPostIntegrateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_cloth_component_keep_state_of_all_meshes:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetClothComponentKeepStateOfAllMeshesDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetClothComponentKeepStateOfAllMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetClothComponentKeepStateOfAllMeshesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_cloth_wind_to_weapon_at_index:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetClothWindToWeaponAtIndexDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetClothWindToWeaponAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetClothWindToWeaponAtIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_contour_state:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetContourStateDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetContourStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetContourStateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_do_timer_based_skeleton_forced_updates:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetDoTimerBasedForcedSkeletonUpdatesDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetDoTimerBasedForcedSkeletonUpdatesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetDoTimerBasedForcedSkeletonUpdatesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_enable_occlusion_culling:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetEnableOcclusionCullingDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetEnableOcclusionCullingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetEnableOcclusionCullingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_enforced_visibility_for_all_agents:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetEnforcedVisibilityForAllAgentsDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetEnforcedVisibilityForAllAgentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetEnforcedVisibilityForAllAgentsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_entity:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetEntityDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetEntityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_face_generation_params:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetFaceGenerationParamsDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetFaceGenerationParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetFaceGenerationParamsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_frame:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetFrameDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_lod_atlas_shading_index:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetLodAtlasShadingIndexDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetLodAtlasShadingIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetLodAtlasShadingIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_look_direction:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetLookDirectionDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetLookDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetLookDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_setup_morph_node:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetSetupMorphNodeDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetSetupMorphNodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetSetupMorphNodeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_skeleton:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetSkeletonDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetSkeletonDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_visible:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetVisibleDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetVisibleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_voice_definition_index:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetVoiceDefinitionIndexDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetVoiceDefinitionIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetVoiceDefinitionIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_set_wielded_weapon_indices:
			ScriptingInterfaceOfIMBAgentVisuals.call_SetWieldedWeaponIndicesDelegate = (ScriptingInterfaceOfIMBAgentVisuals.SetWieldedWeaponIndicesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.SetWieldedWeaponIndicesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_start_rhubarb_record:
			ScriptingInterfaceOfIMBAgentVisuals.call_StartRhubarbRecordDelegate = (ScriptingInterfaceOfIMBAgentVisuals.StartRhubarbRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.StartRhubarbRecordDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_tick:
			ScriptingInterfaceOfIMBAgentVisuals.call_TickDelegate = (ScriptingInterfaceOfIMBAgentVisuals.TickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.TickDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_update_quiver_mesh_of_weapon_in_slot:
			ScriptingInterfaceOfIMBAgentVisuals.call_UpdateQuiverMeshesWithoutAgentDelegate = (ScriptingInterfaceOfIMBAgentVisuals.UpdateQuiverMeshesWithoutAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.UpdateQuiverMeshesWithoutAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_update_skeleton_scale:
			ScriptingInterfaceOfIMBAgentVisuals.call_UpdateSkeletonScaleDelegate = (ScriptingInterfaceOfIMBAgentVisuals.UpdateSkeletonScaleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.UpdateSkeletonScaleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_use_scaled_weapons:
			ScriptingInterfaceOfIMBAgentVisuals.call_UseScaledWeaponsDelegate = (ScriptingInterfaceOfIMBAgentVisuals.UseScaledWeaponsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.UseScaledWeaponsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAgentVisuals_validate_agent_visuals_reseted:
			ScriptingInterfaceOfIMBAgentVisuals.call_ValidateAgentVisualsResetedDelegate = (ScriptingInterfaceOfIMBAgentVisuals.ValidateAgentVisualsResetedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAgentVisuals.ValidateAgentVisualsResetedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_index_of_action_code:
			ScriptingInterfaceOfIMBAnimation.call_AnimationIndexOfActionCodeDelegate = (ScriptingInterfaceOfIMBAnimation.AnimationIndexOfActionCodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.AnimationIndexOfActionCodeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_check_animation_clip_exists:
			ScriptingInterfaceOfIMBAnimation.call_CheckAnimationClipExistsDelegate = (ScriptingInterfaceOfIMBAnimation.CheckAnimationClipExistsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.CheckAnimationClipExistsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_action_animation_duration:
			ScriptingInterfaceOfIMBAnimation.call_GetActionAnimationDurationDelegate = (ScriptingInterfaceOfIMBAnimation.GetActionAnimationDurationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetActionAnimationDurationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_action_blend_out_start_progress:
			ScriptingInterfaceOfIMBAnimation.call_GetActionBlendOutStartProgressDelegate = (ScriptingInterfaceOfIMBAnimation.GetActionBlendOutStartProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetActionBlendOutStartProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_action_code_with_name:
			ScriptingInterfaceOfIMBAnimation.call_GetActionCodeWithNameDelegate = (ScriptingInterfaceOfIMBAnimation.GetActionCodeWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetActionCodeWithNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_action_name_with_code:
			ScriptingInterfaceOfIMBAnimation.call_GetActionNameWithCodeDelegate = (ScriptingInterfaceOfIMBAnimation.GetActionNameWithCodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetActionNameWithCodeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_action_type:
			ScriptingInterfaceOfIMBAnimation.call_GetActionTypeDelegate = (ScriptingInterfaceOfIMBAnimation.GetActionTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetActionTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_blend_in_period:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationBlendInPeriodDelegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationBlendInPeriodDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationBlendInPeriodDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_blends_with_action_index:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationBlendsWithActionIndexDelegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationBlendsWithActionIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationBlendsWithActionIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_continue_to_action:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationContinueToActionDelegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationContinueToActionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationContinueToActionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_displacement_at_progress:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationDisplacementAtProgressDelegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationDisplacementAtProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationDisplacementAtProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_duration:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationDurationDelegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationDurationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationDurationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_flags:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationFlagsDelegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_name:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationNameDelegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_parameter1:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationParameter1Delegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationParameter1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationParameter1Delegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_parameter2:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationParameter2Delegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationParameter2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationParameter2Delegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_animation_parameter3:
			ScriptingInterfaceOfIMBAnimation.call_GetAnimationParameter3Delegate = (ScriptingInterfaceOfIMBAnimation.GetAnimationParameter3Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetAnimationParameter3Delegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_displacement_vector:
			ScriptingInterfaceOfIMBAnimation.call_GetDisplacementVectorDelegate = (ScriptingInterfaceOfIMBAnimation.GetDisplacementVectorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetDisplacementVectorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_id_with_index:
			ScriptingInterfaceOfIMBAnimation.call_GetIDWithIndexDelegate = (ScriptingInterfaceOfIMBAnimation.GetIDWithIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetIDWithIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_index_with_id:
			ScriptingInterfaceOfIMBAnimation.call_GetIndexWithIDDelegate = (ScriptingInterfaceOfIMBAnimation.GetIndexWithIDDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetIndexWithIDDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_num_action_codes:
			ScriptingInterfaceOfIMBAnimation.call_GetNumActionCodesDelegate = (ScriptingInterfaceOfIMBAnimation.GetNumActionCodesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetNumActionCodesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_get_num_animations:
			ScriptingInterfaceOfIMBAnimation.call_GetNumAnimationsDelegate = (ScriptingInterfaceOfIMBAnimation.GetNumAnimationsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.GetNumAnimationsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_is_any_animation_loading_from_disk:
			ScriptingInterfaceOfIMBAnimation.call_IsAnyAnimationLoadingFromDiskDelegate = (ScriptingInterfaceOfIMBAnimation.IsAnyAnimationLoadingFromDiskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.IsAnyAnimationLoadingFromDiskDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBAnimation_prefetch_animation_clip:
			ScriptingInterfaceOfIMBAnimation.call_PrefetchAnimationClipDelegate = (ScriptingInterfaceOfIMBAnimation.PrefetchAnimationClipDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBAnimation.PrefetchAnimationClipDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBBannerlordChecker_get_engine_struct_member_offset:
			ScriptingInterfaceOfIMBBannerlordChecker.call_GetEngineStructMemberOffsetDelegate = (ScriptingInterfaceOfIMBBannerlordChecker.GetEngineStructMemberOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBBannerlordChecker.GetEngineStructMemberOffsetDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBBannerlordChecker_get_engine_struct_size:
			ScriptingInterfaceOfIMBBannerlordChecker.call_GetEngineStructSizeDelegate = (ScriptingInterfaceOfIMBBannerlordChecker.GetEngineStructSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBBannerlordChecker.GetEngineStructSizeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBBannerlordConfig_validate_options:
			ScriptingInterfaceOfIMBBannerlordConfig.call_ValidateOptionsDelegate = (ScriptingInterfaceOfIMBBannerlordConfig.ValidateOptionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBBannerlordConfig.ValidateOptionsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBBannerlordTableauManager_get_number_of_pending_tableau_requests:
			ScriptingInterfaceOfIMBBannerlordTableauManager.call_GetNumberOfPendingTableauRequestsDelegate = (ScriptingInterfaceOfIMBBannerlordTableauManager.GetNumberOfPendingTableauRequestsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBBannerlordTableauManager.GetNumberOfPendingTableauRequestsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBBannerlordTableauManager_initialize_character_tableau_render_system:
			ScriptingInterfaceOfIMBBannerlordTableauManager.call_InitializeCharacterTableauRenderSystemDelegate = (ScriptingInterfaceOfIMBBannerlordTableauManager.InitializeCharacterTableauRenderSystemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBBannerlordTableauManager.InitializeCharacterTableauRenderSystemDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBBannerlordTableauManager_request_character_tableau_render:
			ScriptingInterfaceOfIMBBannerlordTableauManager.call_RequestCharacterTableauRenderDelegate = (ScriptingInterfaceOfIMBBannerlordTableauManager.RequestCharacterTableauRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBBannerlordTableauManager.RequestCharacterTableauRenderDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBDebugExtensions_override_native_parameter:
			ScriptingInterfaceOfIMBDebugExtensions.call_OverrideNativeParameterDelegate = (ScriptingInterfaceOfIMBDebugExtensions.OverrideNativeParameterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBDebugExtensions.OverrideNativeParameterDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBDebugExtensions_reload_native_parameters:
			ScriptingInterfaceOfIMBDebugExtensions.call_ReloadNativeParametersDelegate = (ScriptingInterfaceOfIMBDebugExtensions.ReloadNativeParametersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBDebugExtensions.ReloadNativeParametersDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBDebugExtensions_render_debug_arc_on_terrain:
			ScriptingInterfaceOfIMBDebugExtensions.call_RenderDebugArcOnTerrainDelegate = (ScriptingInterfaceOfIMBDebugExtensions.RenderDebugArcOnTerrainDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBDebugExtensions.RenderDebugArcOnTerrainDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBDebugExtensions_render_debug_circle_on_terrain:
			ScriptingInterfaceOfIMBDebugExtensions.call_RenderDebugCircleOnTerrainDelegate = (ScriptingInterfaceOfIMBDebugExtensions.RenderDebugCircleOnTerrainDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBDebugExtensions.RenderDebugCircleOnTerrainDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBDebugExtensions_render_debug_line_on_terrain:
			ScriptingInterfaceOfIMBDebugExtensions.call_RenderDebugLineOnTerrainDelegate = (ScriptingInterfaceOfIMBDebugExtensions.RenderDebugLineOnTerrainDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBDebugExtensions.RenderDebugLineOnTerrainDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_activate_scene_editor_presentation:
			ScriptingInterfaceOfIMBEditor.call_ActivateSceneEditorPresentationDelegate = (ScriptingInterfaceOfIMBEditor.ActivateSceneEditorPresentationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.ActivateSceneEditorPresentationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_add_editor_warning:
			ScriptingInterfaceOfIMBEditor.call_AddEditorWarningDelegate = (ScriptingInterfaceOfIMBEditor.AddEditorWarningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.AddEditorWarningDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_add_entity_warning:
			ScriptingInterfaceOfIMBEditor.call_AddEntityWarningDelegate = (ScriptingInterfaceOfIMBEditor.AddEntityWarningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.AddEntityWarningDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_add_nav_mesh_warning:
			ScriptingInterfaceOfIMBEditor.call_AddNavMeshWarningDelegate = (ScriptingInterfaceOfIMBEditor.AddNavMeshWarningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.AddNavMeshWarningDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_apply_delta_to_editor_camera:
			ScriptingInterfaceOfIMBEditor.call_ApplyDeltaToEditorCameraDelegate = (ScriptingInterfaceOfIMBEditor.ApplyDeltaToEditorCameraDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.ApplyDeltaToEditorCameraDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_border_helpers_enabled:
			ScriptingInterfaceOfIMBEditor.call_BorderHelpersEnabledDelegate = (ScriptingInterfaceOfIMBEditor.BorderHelpersEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.BorderHelpersEnabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_deactivate_scene_editor_presentation:
			ScriptingInterfaceOfIMBEditor.call_DeactivateSceneEditorPresentationDelegate = (ScriptingInterfaceOfIMBEditor.DeactivateSceneEditorPresentationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.DeactivateSceneEditorPresentationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_enter_edit_mission_mode:
			ScriptingInterfaceOfIMBEditor.call_EnterEditMissionModeDelegate = (ScriptingInterfaceOfIMBEditor.EnterEditMissionModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.EnterEditMissionModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_enter_edit_mode:
			ScriptingInterfaceOfIMBEditor.call_EnterEditModeDelegate = (ScriptingInterfaceOfIMBEditor.EnterEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.EnterEditModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_exit_edit_mode:
			ScriptingInterfaceOfIMBEditor.call_ExitEditModeDelegate = (ScriptingInterfaceOfIMBEditor.ExitEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.ExitEditModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_get_all_prefabs_and_child_with_tag:
			ScriptingInterfaceOfIMBEditor.call_GetAllPrefabsAndChildWithTagDelegate = (ScriptingInterfaceOfIMBEditor.GetAllPrefabsAndChildWithTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.GetAllPrefabsAndChildWithTagDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_get_editor_scene_view:
			ScriptingInterfaceOfIMBEditor.call_GetEditorSceneViewDelegate = (ScriptingInterfaceOfIMBEditor.GetEditorSceneViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.GetEditorSceneViewDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_helpers_enabled:
			ScriptingInterfaceOfIMBEditor.call_HelpersEnabledDelegate = (ScriptingInterfaceOfIMBEditor.HelpersEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.HelpersEnabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_is_edit_mode:
			ScriptingInterfaceOfIMBEditor.call_IsEditModeDelegate = (ScriptingInterfaceOfIMBEditor.IsEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.IsEditModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_is_edit_mode_enabled:
			ScriptingInterfaceOfIMBEditor.call_IsEditModeEnabledDelegate = (ScriptingInterfaceOfIMBEditor.IsEditModeEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.IsEditModeEnabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_is_entity_selected:
			ScriptingInterfaceOfIMBEditor.call_IsEntitySelectedDelegate = (ScriptingInterfaceOfIMBEditor.IsEntitySelectedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.IsEntitySelectedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_is_replay_manager_recording:
			ScriptingInterfaceOfIMBEditor.call_IsReplayManagerRecordingDelegate = (ScriptingInterfaceOfIMBEditor.IsReplayManagerRecordingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.IsReplayManagerRecordingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_is_replay_manager_rendering:
			ScriptingInterfaceOfIMBEditor.call_IsReplayManagerRenderingDelegate = (ScriptingInterfaceOfIMBEditor.IsReplayManagerRenderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.IsReplayManagerRenderingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_is_replay_manager_replaying:
			ScriptingInterfaceOfIMBEditor.call_IsReplayManagerReplayingDelegate = (ScriptingInterfaceOfIMBEditor.IsReplayManagerReplayingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.IsReplayManagerReplayingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_leave_edit_mission_mode:
			ScriptingInterfaceOfIMBEditor.call_LeaveEditMissionModeDelegate = (ScriptingInterfaceOfIMBEditor.LeaveEditMissionModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.LeaveEditMissionModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_leave_edit_mode:
			ScriptingInterfaceOfIMBEditor.call_LeaveEditModeDelegate = (ScriptingInterfaceOfIMBEditor.LeaveEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.LeaveEditModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_render_editor_mesh:
			ScriptingInterfaceOfIMBEditor.call_RenderEditorMeshDelegate = (ScriptingInterfaceOfIMBEditor.RenderEditorMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.RenderEditorMeshDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_set_level_visibility:
			ScriptingInterfaceOfIMBEditor.call_SetLevelVisibilityDelegate = (ScriptingInterfaceOfIMBEditor.SetLevelVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.SetLevelVisibilityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_set_upgrade_level_visibility:
			ScriptingInterfaceOfIMBEditor.call_SetUpgradeLevelVisibilityDelegate = (ScriptingInterfaceOfIMBEditor.SetUpgradeLevelVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.SetUpgradeLevelVisibilityDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_tick_edit_mode:
			ScriptingInterfaceOfIMBEditor.call_TickEditModeDelegate = (ScriptingInterfaceOfIMBEditor.TickEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.TickEditModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_tick_scene_editor_presentation:
			ScriptingInterfaceOfIMBEditor.call_TickSceneEditorPresentationDelegate = (ScriptingInterfaceOfIMBEditor.TickSceneEditorPresentationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.TickSceneEditorPresentationDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_toggle_enable_editor_physics:
			ScriptingInterfaceOfIMBEditor.call_ToggleEnableEditorPhysicsDelegate = (ScriptingInterfaceOfIMBEditor.ToggleEnableEditorPhysicsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.ToggleEnableEditorPhysicsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_update_scene_tree:
			ScriptingInterfaceOfIMBEditor.call_UpdateSceneTreeDelegate = (ScriptingInterfaceOfIMBEditor.UpdateSceneTreeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.UpdateSceneTreeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBEditor_zoom_to_position:
			ScriptingInterfaceOfIMBEditor.call_ZoomToPositionDelegate = (ScriptingInterfaceOfIMBEditor.ZoomToPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBEditor.ZoomToPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_enforce_constraints:
			ScriptingInterfaceOfIMBFaceGen.call_EnforceConstraintsDelegate = (ScriptingInterfaceOfIMBFaceGen.EnforceConstraintsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.EnforceConstraintsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_flush_face_cache:
			ScriptingInterfaceOfIMBFaceGen.call_FlushFaceCacheDelegate = (ScriptingInterfaceOfIMBFaceGen.FlushFaceCacheDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.FlushFaceCacheDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_deform_key_data:
			ScriptingInterfaceOfIMBFaceGen.call_GetDeformKeyDataDelegate = (ScriptingInterfaceOfIMBFaceGen.GetDeformKeyDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetDeformKeyDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_face_gen_instances_length:
			ScriptingInterfaceOfIMBFaceGen.call_GetFaceGenInstancesLengthDelegate = (ScriptingInterfaceOfIMBFaceGen.GetFaceGenInstancesLengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetFaceGenInstancesLengthDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_facial_indices_by_tag:
			ScriptingInterfaceOfIMBFaceGen.call_GetFacialIndicesByTagDelegate = (ScriptingInterfaceOfIMBFaceGen.GetFacialIndicesByTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetFacialIndicesByTagDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_hair_color_count:
			ScriptingInterfaceOfIMBFaceGen.call_GetHairColorCountDelegate = (ScriptingInterfaceOfIMBFaceGen.GetHairColorCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetHairColorCountDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_hair_color_gradient_points:
			ScriptingInterfaceOfIMBFaceGen.call_GetHairColorGradientPointsDelegate = (ScriptingInterfaceOfIMBFaceGen.GetHairColorGradientPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetHairColorGradientPointsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_hair_indices_by_tag:
			ScriptingInterfaceOfIMBFaceGen.call_GetHairIndicesByTagDelegate = (ScriptingInterfaceOfIMBFaceGen.GetHairIndicesByTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetHairIndicesByTagDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_maturity_type:
			ScriptingInterfaceOfIMBFaceGen.call_GetMaturityTypeDelegate = (ScriptingInterfaceOfIMBFaceGen.GetMaturityTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetMaturityTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_num_editable_deform_keys:
			ScriptingInterfaceOfIMBFaceGen.call_GetNumEditableDeformKeysDelegate = (ScriptingInterfaceOfIMBFaceGen.GetNumEditableDeformKeysDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetNumEditableDeformKeysDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_params_from_key:
			ScriptingInterfaceOfIMBFaceGen.call_GetParamsFromKeyDelegate = (ScriptingInterfaceOfIMBFaceGen.GetParamsFromKeyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetParamsFromKeyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_params_max:
			ScriptingInterfaceOfIMBFaceGen.call_GetParamsMaxDelegate = (ScriptingInterfaceOfIMBFaceGen.GetParamsMaxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetParamsMaxDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_race_ids:
			ScriptingInterfaceOfIMBFaceGen.call_GetRaceIdsDelegate = (ScriptingInterfaceOfIMBFaceGen.GetRaceIdsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetRaceIdsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_random_body_properties:
			ScriptingInterfaceOfIMBFaceGen.call_GetRandomBodyPropertiesDelegate = (ScriptingInterfaceOfIMBFaceGen.GetRandomBodyPropertiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetRandomBodyPropertiesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_scale:
			ScriptingInterfaceOfIMBFaceGen.call_GetScaleFromKeyDelegate = (ScriptingInterfaceOfIMBFaceGen.GetScaleFromKeyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetScaleFromKeyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_skin_color_count:
			ScriptingInterfaceOfIMBFaceGen.call_GetSkinColorCountDelegate = (ScriptingInterfaceOfIMBFaceGen.GetSkinColorCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetSkinColorCountDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_skin_color_gradient_points:
			ScriptingInterfaceOfIMBFaceGen.call_GetSkinColorGradientPointsDelegate = (ScriptingInterfaceOfIMBFaceGen.GetSkinColorGradientPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetSkinColorGradientPointsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_tatoo_color_count:
			ScriptingInterfaceOfIMBFaceGen.call_GetTatooColorCountDelegate = (ScriptingInterfaceOfIMBFaceGen.GetTatooColorCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetTatooColorCountDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_tatoo_color_gradient_points:
			ScriptingInterfaceOfIMBFaceGen.call_GetTatooColorGradientPointsDelegate = (ScriptingInterfaceOfIMBFaceGen.GetTatooColorGradientPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetTatooColorGradientPointsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_tattoo_indices_by_tag:
			ScriptingInterfaceOfIMBFaceGen.call_GetTattooIndicesByTagDelegate = (ScriptingInterfaceOfIMBFaceGen.GetTattooIndicesByTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetTattooIndicesByTagDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_voice_records_count:
			ScriptingInterfaceOfIMBFaceGen.call_GetVoiceRecordsCountDelegate = (ScriptingInterfaceOfIMBFaceGen.GetVoiceRecordsCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetVoiceRecordsCountDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_voice_type_usable_for_player_data:
			ScriptingInterfaceOfIMBFaceGen.call_GetVoiceTypeUsableForPlayerDataDelegate = (ScriptingInterfaceOfIMBFaceGen.GetVoiceTypeUsableForPlayerDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetVoiceTypeUsableForPlayerDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_get_zero_probabilities:
			ScriptingInterfaceOfIMBFaceGen.call_GetZeroProbabilitiesDelegate = (ScriptingInterfaceOfIMBFaceGen.GetZeroProbabilitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.GetZeroProbabilitiesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_produce_numeric_key_with_default_values:
			ScriptingInterfaceOfIMBFaceGen.call_ProduceNumericKeyWithDefaultValuesDelegate = (ScriptingInterfaceOfIMBFaceGen.ProduceNumericKeyWithDefaultValuesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.ProduceNumericKeyWithDefaultValuesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_produce_numeric_key_with_params:
			ScriptingInterfaceOfIMBFaceGen.call_ProduceNumericKeyWithParamsDelegate = (ScriptingInterfaceOfIMBFaceGen.ProduceNumericKeyWithParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.ProduceNumericKeyWithParamsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBFaceGen_transform_face_keys_to_default_face:
			ScriptingInterfaceOfIMBFaceGen.call_TransformFaceKeysToDefaultFaceDelegate = (ScriptingInterfaceOfIMBFaceGen.TransformFaceKeysToDefaultFaceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBFaceGen.TransformFaceKeysToDefaultFaceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBGame_load_module_data:
			ScriptingInterfaceOfIMBGame.call_LoadModuleDataDelegate = (ScriptingInterfaceOfIMBGame.LoadModuleDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBGame.LoadModuleDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBGame_start_new:
			ScriptingInterfaceOfIMBGame.call_StartNewDelegate = (ScriptingInterfaceOfIMBGame.StartNewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBGame.StartNewDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBGameEntityExtensions_create_from_weapon:
			ScriptingInterfaceOfIMBGameEntityExtensions.call_CreateFromWeaponDelegate = (ScriptingInterfaceOfIMBGameEntityExtensions.CreateFromWeaponDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBGameEntityExtensions.CreateFromWeaponDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBGameEntityExtensions_fade_in:
			ScriptingInterfaceOfIMBGameEntityExtensions.call_FadeInDelegate = (ScriptingInterfaceOfIMBGameEntityExtensions.FadeInDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBGameEntityExtensions.FadeInDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBGameEntityExtensions_fade_out:
			ScriptingInterfaceOfIMBGameEntityExtensions.call_FadeOutDelegate = (ScriptingInterfaceOfIMBGameEntityExtensions.FadeOutDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBGameEntityExtensions.FadeOutDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBGameEntityExtensions_hide_if_not_fading_out:
			ScriptingInterfaceOfIMBGameEntityExtensions.call_HideIfNotFadingOutDelegate = (ScriptingInterfaceOfIMBGameEntityExtensions.HideIfNotFadingOutDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBGameEntityExtensions.HideIfNotFadingOutDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_holster_frame_by_index:
			ScriptingInterfaceOfIMBItem.call_GetHolsterFrameByIndexDelegate = (ScriptingInterfaceOfIMBItem.GetHolsterFrameByIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetHolsterFrameByIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_item_holster_index:
			ScriptingInterfaceOfIMBItem.call_GetItemHolsterIndexDelegate = (ScriptingInterfaceOfIMBItem.GetItemHolsterIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetItemHolsterIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_item_is_passive_usage:
			ScriptingInterfaceOfIMBItem.call_GetItemIsPassiveUsageDelegate = (ScriptingInterfaceOfIMBItem.GetItemIsPassiveUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetItemIsPassiveUsageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_item_usage_index:
			ScriptingInterfaceOfIMBItem.call_GetItemUsageIndexDelegate = (ScriptingInterfaceOfIMBItem.GetItemUsageIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetItemUsageIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_item_usage_reload_action_code:
			ScriptingInterfaceOfIMBItem.call_GetItemUsageReloadActionCodeDelegate = (ScriptingInterfaceOfIMBItem.GetItemUsageReloadActionCodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetItemUsageReloadActionCodeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_item_usage_set_flags:
			ScriptingInterfaceOfIMBItem.call_GetItemUsageSetFlagsDelegate = (ScriptingInterfaceOfIMBItem.GetItemUsageSetFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetItemUsageSetFlagsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_item_usage_strike_type:
			ScriptingInterfaceOfIMBItem.call_GetItemUsageStrikeTypeDelegate = (ScriptingInterfaceOfIMBItem.GetItemUsageStrikeTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetItemUsageStrikeTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBItem_get_missile_range:
			ScriptingInterfaceOfIMBItem.call_GetMissileRangeDelegate = (ScriptingInterfaceOfIMBItem.GetMissileRangeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBItem.GetMissileRangeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_accessible_point_near_position:
			ScriptingInterfaceOfIMBMapScene.call_GetAccessiblePointNearPositionDelegate = (ScriptingInterfaceOfIMBMapScene.GetAccessiblePointNearPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetAccessiblePointNearPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_battle_scene_index_map:
			ScriptingInterfaceOfIMBMapScene.call_GetBattleSceneIndexMapDelegate = (ScriptingInterfaceOfIMBMapScene.GetBattleSceneIndexMapDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetBattleSceneIndexMapDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_battle_scene_index_map_resolution:
			ScriptingInterfaceOfIMBMapScene.call_GetBattleSceneIndexMapResolutionDelegate = (ScriptingInterfaceOfIMBMapScene.GetBattleSceneIndexMapResolutionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetBattleSceneIndexMapResolutionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_color_grade_grid_data:
			ScriptingInterfaceOfIMBMapScene.call_GetColorGradeGridDataDelegate = (ScriptingInterfaceOfIMBMapScene.GetColorGradeGridDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetColorGradeGridDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_mouse_visible:
			ScriptingInterfaceOfIMBMapScene.call_GetMouseVisibleDelegate = (ScriptingInterfaceOfIMBMapScene.GetMouseVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetMouseVisibleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_nearest_nav_mesh_face_center_position_between_regions_using_path:
			ScriptingInterfaceOfIMBMapScene.call_GetNearestFaceCenterForPositionWithPathDelegate = (ScriptingInterfaceOfIMBMapScene.GetNearestFaceCenterForPositionWithPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetNearestFaceCenterForPositionWithPathDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_nearest_nav_mesh_face_center_position_for_position:
			ScriptingInterfaceOfIMBMapScene.call_GetNearestFaceCenterPositionForPositionDelegate = (ScriptingInterfaceOfIMBMapScene.GetNearestFaceCenterPositionForPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetNearestFaceCenterPositionForPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_get_season_time_factor:
			ScriptingInterfaceOfIMBMapScene.call_GetSeasonTimeFactorDelegate = (ScriptingInterfaceOfIMBMapScene.GetSeasonTimeFactorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.GetSeasonTimeFactorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_load_atmosphere_data:
			ScriptingInterfaceOfIMBMapScene.call_LoadAtmosphereDataDelegate = (ScriptingInterfaceOfIMBMapScene.LoadAtmosphereDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.LoadAtmosphereDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_remove_zero_corner_bodies:
			ScriptingInterfaceOfIMBMapScene.call_RemoveZeroCornerBodiesDelegate = (ScriptingInterfaceOfIMBMapScene.RemoveZeroCornerBodiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.RemoveZeroCornerBodiesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_send_mouse_key_down_event:
			ScriptingInterfaceOfIMBMapScene.call_SendMouseKeyEventDelegate = (ScriptingInterfaceOfIMBMapScene.SendMouseKeyEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.SendMouseKeyEventDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_set_frame_for_atmosphere:
			ScriptingInterfaceOfIMBMapScene.call_SetFrameForAtmosphereDelegate = (ScriptingInterfaceOfIMBMapScene.SetFrameForAtmosphereDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.SetFrameForAtmosphereDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_set_mouse_pos:
			ScriptingInterfaceOfIMBMapScene.call_SetMousePosDelegate = (ScriptingInterfaceOfIMBMapScene.SetMousePosDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.SetMousePosDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_set_mouse_visible:
			ScriptingInterfaceOfIMBMapScene.call_SetMouseVisibleDelegate = (ScriptingInterfaceOfIMBMapScene.SetMouseVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.SetMouseVisibleDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_set_political_color:
			ScriptingInterfaceOfIMBMapScene.call_SetPoliticalColorDelegate = (ScriptingInterfaceOfIMBMapScene.SetPoliticalColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.SetPoliticalColorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_set_season_time_factor:
			ScriptingInterfaceOfIMBMapScene.call_SetSeasonTimeFactorDelegate = (ScriptingInterfaceOfIMBMapScene.SetSeasonTimeFactorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.SetSeasonTimeFactorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_set_terrain_dynamic_params:
			ScriptingInterfaceOfIMBMapScene.call_SetTerrainDynamicParamsDelegate = (ScriptingInterfaceOfIMBMapScene.SetTerrainDynamicParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.SetTerrainDynamicParamsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_tick_ambient_sounds:
			ScriptingInterfaceOfIMBMapScene.call_TickAmbientSoundsDelegate = (ScriptingInterfaceOfIMBMapScene.TickAmbientSoundsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.TickAmbientSoundsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_tick_step_sound:
			ScriptingInterfaceOfIMBMapScene.call_TickStepSoundDelegate = (ScriptingInterfaceOfIMBMapScene.TickStepSoundDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.TickStepSoundDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_tick_visuals:
			ScriptingInterfaceOfIMBMapScene.call_TickVisualsDelegate = (ScriptingInterfaceOfIMBMapScene.TickVisualsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.TickVisualsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMapScene_validate_terrain_sound_ids:
			ScriptingInterfaceOfIMBMapScene.call_ValidateTerrainSoundIdsDelegate = (ScriptingInterfaceOfIMBMapScene.ValidateTerrainSoundIdsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMapScene.ValidateTerrainSoundIdsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMessageManager_display_message:
			ScriptingInterfaceOfIMBMessageManager.call_DisplayMessageDelegate = (ScriptingInterfaceOfIMBMessageManager.DisplayMessageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMessageManager.DisplayMessageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMessageManager_display_message_with_color:
			ScriptingInterfaceOfIMBMessageManager.call_DisplayMessageWithColorDelegate = (ScriptingInterfaceOfIMBMessageManager.DisplayMessageWithColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMessageManager.DisplayMessageWithColorDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMessageManager_set_message_manager:
			ScriptingInterfaceOfIMBMessageManager.call_SetMessageManagerDelegate = (ScriptingInterfaceOfIMBMessageManager.SetMessageManagerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMessageManager.SetMessageManagerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_add_ai_debug_text:
			ScriptingInterfaceOfIMBMission.call_AddAiDebugTextDelegate = (ScriptingInterfaceOfIMBMission.AddAiDebugTextDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.AddAiDebugTextDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_add_boundary:
			ScriptingInterfaceOfIMBMission.call_AddBoundaryDelegate = (ScriptingInterfaceOfIMBMission.AddBoundaryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.AddBoundaryDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_add_missile:
			ScriptingInterfaceOfIMBMission.call_AddMissileDelegate = (ScriptingInterfaceOfIMBMission.AddMissileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.AddMissileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_add_missile_single_usage:
			ScriptingInterfaceOfIMBMission.call_AddMissileSingleUsageDelegate = (ScriptingInterfaceOfIMBMission.AddMissileSingleUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.AddMissileSingleUsageDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_add_particle_system_burst_by_name:
			ScriptingInterfaceOfIMBMission.call_AddParticleSystemBurstByNameDelegate = (ScriptingInterfaceOfIMBMission.AddParticleSystemBurstByNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.AddParticleSystemBurstByNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_add_team:
			ScriptingInterfaceOfIMBMission.call_AddTeamDelegate = (ScriptingInterfaceOfIMBMission.AddTeamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.AddTeamDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_backup_record_to_file:
			ScriptingInterfaceOfIMBMission.call_BackupRecordToFileDelegate = (ScriptingInterfaceOfIMBMission.BackupRecordToFileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.BackupRecordToFileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_batch_formation_unit_positions:
			ScriptingInterfaceOfIMBMission.call_BatchFormationUnitPositionsDelegate = (ScriptingInterfaceOfIMBMission.BatchFormationUnitPositionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.BatchFormationUnitPositionsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_clear_agent_actions:
			ScriptingInterfaceOfIMBMission.call_ClearAgentActionsDelegate = (ScriptingInterfaceOfIMBMission.ClearAgentActionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ClearAgentActionsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_clear_corpses:
			ScriptingInterfaceOfIMBMission.call_ClearCorpsesDelegate = (ScriptingInterfaceOfIMBMission.ClearCorpsesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ClearCorpsesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_clear_missiles:
			ScriptingInterfaceOfIMBMission.call_ClearMissilesDelegate = (ScriptingInterfaceOfIMBMission.ClearMissilesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ClearMissilesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_clear_record_buffers:
			ScriptingInterfaceOfIMBMission.call_ClearRecordBuffersDelegate = (ScriptingInterfaceOfIMBMission.ClearRecordBuffersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ClearRecordBuffersDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_clear_resources:
			ScriptingInterfaceOfIMBMission.call_ClearResourcesDelegate = (ScriptingInterfaceOfIMBMission.ClearResourcesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ClearResourcesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_clear_scene:
			ScriptingInterfaceOfIMBMission.call_ClearSceneDelegate = (ScriptingInterfaceOfIMBMission.ClearSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ClearSceneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_compute_exact_missile_range_at_height_difference:
			ScriptingInterfaceOfIMBMission.call_ComputeExactMissileRangeAtHeightDifferenceDelegate = (ScriptingInterfaceOfIMBMission.ComputeExactMissileRangeAtHeightDifferenceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ComputeExactMissileRangeAtHeightDifferenceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_create_agent:
			ScriptingInterfaceOfIMBMission.call_CreateAgentDelegate = (ScriptingInterfaceOfIMBMission.CreateAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.CreateAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_create_mission:
			ScriptingInterfaceOfIMBMission.call_CreateMissionDelegate = (ScriptingInterfaceOfIMBMission.CreateMissionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.CreateMissionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_end_of_record:
			ScriptingInterfaceOfIMBMission.call_EndOfRecordDelegate = (ScriptingInterfaceOfIMBMission.EndOfRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.EndOfRecordDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_finalize_mission:
			ScriptingInterfaceOfIMBMission.call_FinalizeMissionDelegate = (ScriptingInterfaceOfIMBMission.FinalizeMissionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.FinalizeMissionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_find_agent_with_index:
			ScriptingInterfaceOfIMBMission.call_FindAgentWithIndexDelegate = (ScriptingInterfaceOfIMBMission.FindAgentWithIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.FindAgentWithIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_find_convex_hull:
			ScriptingInterfaceOfIMBMission.call_FindConvexHullDelegate = (ScriptingInterfaceOfIMBMission.FindConvexHullDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.FindConvexHullDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_force_disable_occlusion:
			ScriptingInterfaceOfIMBMission.call_ForceDisableOcclusionDelegate = (ScriptingInterfaceOfIMBMission.ForceDisableOcclusionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ForceDisableOcclusionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_agent_count_around_position:
			ScriptingInterfaceOfIMBMission.call_GetAgentCountAroundPositionDelegate = (ScriptingInterfaceOfIMBMission.GetAgentCountAroundPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetAgentCountAroundPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_alternate_position_for_navmeshless_or_out_of_bounds_position:
			ScriptingInterfaceOfIMBMission.call_GetAlternatePositionForNavmeshlessOrOutOfBoundsPositionDelegate = (ScriptingInterfaceOfIMBMission.GetAlternatePositionForNavmeshlessOrOutOfBoundsPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetAlternatePositionForNavmeshlessOrOutOfBoundsPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_atmosphere_name_for_replay:
			ScriptingInterfaceOfIMBMission.call_GetAtmosphereNameForReplayDelegate = (ScriptingInterfaceOfIMBMission.GetAtmosphereNameForReplayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetAtmosphereNameForReplayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_atmosphere_season_for_replay:
			ScriptingInterfaceOfIMBMission.call_GetAtmosphereSeasonForReplayDelegate = (ScriptingInterfaceOfIMBMission.GetAtmosphereSeasonForReplayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetAtmosphereSeasonForReplayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_average_fps:
			ScriptingInterfaceOfIMBMission.call_GetAverageFpsDelegate = (ScriptingInterfaceOfIMBMission.GetAverageFpsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetAverageFpsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_average_morale_of_agents:
			ScriptingInterfaceOfIMBMission.call_GetAverageMoraleOfAgentsDelegate = (ScriptingInterfaceOfIMBMission.GetAverageMoraleOfAgentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetAverageMoraleOfAgentsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_best_slope_angle_height_pos_for_defending:
			ScriptingInterfaceOfIMBMission.call_GetBestSlopeAngleHeightPosForDefendingDelegate = (ScriptingInterfaceOfIMBMission.GetBestSlopeAngleHeightPosForDefendingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetBestSlopeAngleHeightPosForDefendingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_best_slope_towards_direction:
			ScriptingInterfaceOfIMBMission.call_GetBestSlopeTowardsDirectionDelegate = (ScriptingInterfaceOfIMBMission.GetBestSlopeTowardsDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetBestSlopeTowardsDirectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_biggest_agent_collision_padding:
			ScriptingInterfaceOfIMBMission.call_GetBiggestAgentCollisionPaddingDelegate = (ScriptingInterfaceOfIMBMission.GetBiggestAgentCollisionPaddingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetBiggestAgentCollisionPaddingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_boundary_count:
			ScriptingInterfaceOfIMBMission.call_GetBoundaryCountDelegate = (ScriptingInterfaceOfIMBMission.GetBoundaryCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetBoundaryCountDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_boundary_name:
			ScriptingInterfaceOfIMBMission.call_GetBoundaryNameDelegate = (ScriptingInterfaceOfIMBMission.GetBoundaryNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetBoundaryNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_boundary_points:
			ScriptingInterfaceOfIMBMission.call_GetBoundaryPointsDelegate = (ScriptingInterfaceOfIMBMission.GetBoundaryPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetBoundaryPointsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_boundary_radius:
			ScriptingInterfaceOfIMBMission.call_GetBoundaryRadiusDelegate = (ScriptingInterfaceOfIMBMission.GetBoundaryRadiusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetBoundaryRadiusDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_camera_frame:
			ScriptingInterfaceOfIMBMission.call_GetCameraFrameDelegate = (ScriptingInterfaceOfIMBMission.GetCameraFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetCameraFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_clear_scene_timer_elapsed_time:
			ScriptingInterfaceOfIMBMission.call_GetClearSceneTimerElapsedTimeDelegate = (ScriptingInterfaceOfIMBMission.GetClearSceneTimerElapsedTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetClearSceneTimerElapsedTimeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_closest_ally:
			ScriptingInterfaceOfIMBMission.call_GetClosestAllyDelegate = (ScriptingInterfaceOfIMBMission.GetClosestAllyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetClosestAllyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_closest_boundary_position:
			ScriptingInterfaceOfIMBMission.call_GetClosestBoundaryPositionDelegate = (ScriptingInterfaceOfIMBMission.GetClosestBoundaryPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetClosestBoundaryPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_closest_enemy:
			ScriptingInterfaceOfIMBMission.call_GetClosestEnemyDelegate = (ScriptingInterfaceOfIMBMission.GetClosestEnemyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetClosestEnemyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_combat_type:
			ScriptingInterfaceOfIMBMission.call_GetCombatTypeDelegate = (ScriptingInterfaceOfIMBMission.GetCombatTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetCombatTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_current_volume_generator_version:
			ScriptingInterfaceOfIMBMission.call_GetCurrentVolumeGeneratorVersionDelegate = (ScriptingInterfaceOfIMBMission.GetCurrentVolumeGeneratorVersionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetCurrentVolumeGeneratorVersionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_debug_agent:
			ScriptingInterfaceOfIMBMission.call_GetDebugAgentDelegate = (ScriptingInterfaceOfIMBMission.GetDebugAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetDebugAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_fall_avoid_system_active:
			ScriptingInterfaceOfIMBMission.call_GetFallAvoidSystemActiveDelegate = (ScriptingInterfaceOfIMBMission.GetFallAvoidSystemActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetFallAvoidSystemActiveDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_game_type_for_replay:
			ScriptingInterfaceOfIMBMission.call_GetGameTypeForReplayDelegate = (ScriptingInterfaceOfIMBMission.GetGameTypeForReplayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetGameTypeForReplayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_is_loading_finished:
			ScriptingInterfaceOfIMBMission.call_GetIsLoadingFinishedDelegate = (ScriptingInterfaceOfIMBMission.GetIsLoadingFinishedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetIsLoadingFinishedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_missile_collision_point:
			ScriptingInterfaceOfIMBMission.call_GetMissileCollisionPointDelegate = (ScriptingInterfaceOfIMBMission.GetMissileCollisionPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetMissileCollisionPointDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_missile_has_rigid_body:
			ScriptingInterfaceOfIMBMission.call_GetMissileHasRigidBodyDelegate = (ScriptingInterfaceOfIMBMission.GetMissileHasRigidBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetMissileHasRigidBodyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_missile_range:
			ScriptingInterfaceOfIMBMission.call_GetMissileRangeDelegate = (ScriptingInterfaceOfIMBMission.GetMissileRangeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetMissileRangeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_missile_vertical_aim_correction:
			ScriptingInterfaceOfIMBMission.call_GetMissileVerticalAimCorrectionDelegate = (ScriptingInterfaceOfIMBMission.GetMissileVerticalAimCorrectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetMissileVerticalAimCorrectionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_navigation_points:
			ScriptingInterfaceOfIMBMission.call_GetNavigationPointsDelegate = (ScriptingInterfaceOfIMBMission.GetNavigationPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetNavigationPointsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_nearby_agents_aux:
			ScriptingInterfaceOfIMBMission.call_GetNearbyAgentsAuxDelegate = (ScriptingInterfaceOfIMBMission.GetNearbyAgentsAuxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetNearbyAgentsAuxDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_number_of_teams:
			ScriptingInterfaceOfIMBMission.call_GetNumberOfTeamsDelegate = (ScriptingInterfaceOfIMBMission.GetNumberOfTeamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetNumberOfTeamsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_old_position_of_missile:
			ScriptingInterfaceOfIMBMission.call_GetOldPositionOfMissileDelegate = (ScriptingInterfaceOfIMBMission.GetOldPositionOfMissileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetOldPositionOfMissileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_pause_ai_tick:
			ScriptingInterfaceOfIMBMission.call_GetPauseAITickDelegate = (ScriptingInterfaceOfIMBMission.GetPauseAITickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetPauseAITickDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_position_of_missile:
			ScriptingInterfaceOfIMBMission.call_GetPositionOfMissileDelegate = (ScriptingInterfaceOfIMBMission.GetPositionOfMissileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetPositionOfMissileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_scene_levels_for_replay:
			ScriptingInterfaceOfIMBMission.call_GetSceneLevelsForReplayDelegate = (ScriptingInterfaceOfIMBMission.GetSceneLevelsForReplayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetSceneLevelsForReplayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_scene_name_for_replay:
			ScriptingInterfaceOfIMBMission.call_GetSceneNameForReplayDelegate = (ScriptingInterfaceOfIMBMission.GetSceneNameForReplayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetSceneNameForReplayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_straight_path_to_target:
			ScriptingInterfaceOfIMBMission.call_GetStraightPathToTargetDelegate = (ScriptingInterfaceOfIMBMission.GetStraightPathToTargetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetStraightPathToTargetDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_tick_debug_paused:
			ScriptingInterfaceOfIMBMission.call_GetTickDebugPausedDelegate = (ScriptingInterfaceOfIMBMission.GetTickDebugPausedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetTickDebugPausedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_time:
			ScriptingInterfaceOfIMBMission.call_GetTimeDelegate = (ScriptingInterfaceOfIMBMission.GetTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetTimeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_velocity_of_missile:
			ScriptingInterfaceOfIMBMission.call_GetVelocityOfMissileDelegate = (ScriptingInterfaceOfIMBMission.GetVelocityOfMissileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetVelocityOfMissileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_water_level_at_position:
			ScriptingInterfaceOfIMBMission.call_GetWaterLevelAtPositionDelegate = (ScriptingInterfaceOfIMBMission.GetWaterLevelAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetWaterLevelAtPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_get_weighted_point_of_enemies:
			ScriptingInterfaceOfIMBMission.call_GetWeightedPointOfEnemiesDelegate = (ScriptingInterfaceOfIMBMission.GetWeightedPointOfEnemiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.GetWeightedPointOfEnemiesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_has_any_agents_of_team_around:
			ScriptingInterfaceOfIMBMission.call_HasAnyAgentsOfTeamAroundDelegate = (ScriptingInterfaceOfIMBMission.HasAnyAgentsOfTeamAroundDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.HasAnyAgentsOfTeamAroundDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_idle_tick:
			ScriptingInterfaceOfIMBMission.call_IdleTickDelegate = (ScriptingInterfaceOfIMBMission.IdleTickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.IdleTickDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_initialize_mission:
			ScriptingInterfaceOfIMBMission.call_InitializeMissionDelegate = (ScriptingInterfaceOfIMBMission.InitializeMissionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.InitializeMissionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_is_agent_in_proximity_map:
			ScriptingInterfaceOfIMBMission.call_IsAgentInProximityMapDelegate = (ScriptingInterfaceOfIMBMission.IsAgentInProximityMapDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.IsAgentInProximityMapDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_is_formation_unit_position_available:
			ScriptingInterfaceOfIMBMission.call_IsFormationUnitPositionAvailableDelegate = (ScriptingInterfaceOfIMBMission.IsFormationUnitPositionAvailableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.IsFormationUnitPositionAvailableDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_is_position_inside_any_blocker_nav_mesh_face_2d:
			ScriptingInterfaceOfIMBMission.call_IsPositionInsideAnyBlockerNavMeshFace2DDelegate = (ScriptingInterfaceOfIMBMission.IsPositionInsideAnyBlockerNavMeshFace2DDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.IsPositionInsideAnyBlockerNavMeshFace2DDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_is_position_inside_boundaries:
			ScriptingInterfaceOfIMBMission.call_IsPositionInsideBoundariesDelegate = (ScriptingInterfaceOfIMBMission.IsPositionInsideBoundariesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.IsPositionInsideBoundariesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_is_position_inside_hard_boundaries:
			ScriptingInterfaceOfIMBMission.call_IsPositionInsideHardBoundariesDelegate = (ScriptingInterfaceOfIMBMission.IsPositionInsideHardBoundariesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.IsPositionInsideHardBoundariesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_is_position_on_any_blocker_nav_mesh_face:
			ScriptingInterfaceOfIMBMission.call_IsPositionOnAnyBlockerNavMeshFaceDelegate = (ScriptingInterfaceOfIMBMission.IsPositionOnAnyBlockerNavMeshFaceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.IsPositionOnAnyBlockerNavMeshFaceDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_make_sound:
			ScriptingInterfaceOfIMBMission.call_MakeSoundDelegate = (ScriptingInterfaceOfIMBMission.MakeSoundDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.MakeSoundDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_make_sound_only_on_related_peer:
			ScriptingInterfaceOfIMBMission.call_MakeSoundOnlyOnRelatedPeerDelegate = (ScriptingInterfaceOfIMBMission.MakeSoundOnlyOnRelatedPeerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.MakeSoundOnlyOnRelatedPeerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_make_sound_with_parameter:
			ScriptingInterfaceOfIMBMission.call_MakeSoundWithParameterDelegate = (ScriptingInterfaceOfIMBMission.MakeSoundWithParameterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.MakeSoundWithParameterDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_on_fast_forward_state_changed:
			ScriptingInterfaceOfIMBMission.call_OnFastForwardStateChangedDelegate = (ScriptingInterfaceOfIMBMission.OnFastForwardStateChangedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.OnFastForwardStateChangedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_pause_mission_scene_sounds:
			ScriptingInterfaceOfIMBMission.call_PauseMissionSceneSoundsDelegate = (ScriptingInterfaceOfIMBMission.PauseMissionSceneSoundsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.PauseMissionSceneSoundsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_prepare_missile_weapon_for_drop:
			ScriptingInterfaceOfIMBMission.call_PrepareMissileWeaponForDropDelegate = (ScriptingInterfaceOfIMBMission.PrepareMissileWeaponForDropDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.PrepareMissileWeaponForDropDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_process_record_until_time:
			ScriptingInterfaceOfIMBMission.call_ProcessRecordUntilTimeDelegate = (ScriptingInterfaceOfIMBMission.ProcessRecordUntilTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ProcessRecordUntilTimeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_agent_proximity_map_begin_search:
			ScriptingInterfaceOfIMBMission.call_ProximityMapBeginSearchDelegate = (ScriptingInterfaceOfIMBMission.ProximityMapBeginSearchDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ProximityMapBeginSearchDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_agent_proximity_map_find_next:
			ScriptingInterfaceOfIMBMission.call_ProximityMapFindNextDelegate = (ScriptingInterfaceOfIMBMission.ProximityMapFindNextDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ProximityMapFindNextDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_agent_proximity_map_get_max_search_radius:
			ScriptingInterfaceOfIMBMission.call_ProximityMapMaxSearchRadiusDelegate = (ScriptingInterfaceOfIMBMission.ProximityMapMaxSearchRadiusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ProximityMapMaxSearchRadiusDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_ray_cast_for_closest_agent:
			ScriptingInterfaceOfIMBMission.call_RayCastForClosestAgentDelegate = (ScriptingInterfaceOfIMBMission.RayCastForClosestAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RayCastForClosestAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_ray_cast_for_closest_agents_limbs:
			ScriptingInterfaceOfIMBMission.call_RayCastForClosestAgentsLimbsDelegate = (ScriptingInterfaceOfIMBMission.RayCastForClosestAgentsLimbsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RayCastForClosestAgentsLimbsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_ray_cast_for_given_agents_limbs:
			ScriptingInterfaceOfIMBMission.call_RayCastForGivenAgentsLimbsDelegate = (ScriptingInterfaceOfIMBMission.RayCastForGivenAgentsLimbsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RayCastForGivenAgentsLimbsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_record_current_state:
			ScriptingInterfaceOfIMBMission.call_RecordCurrentStateDelegate = (ScriptingInterfaceOfIMBMission.RecordCurrentStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RecordCurrentStateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_remove_boundary:
			ScriptingInterfaceOfIMBMission.call_RemoveBoundaryDelegate = (ScriptingInterfaceOfIMBMission.RemoveBoundaryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RemoveBoundaryDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_remove_missile:
			ScriptingInterfaceOfIMBMission.call_RemoveMissileDelegate = (ScriptingInterfaceOfIMBMission.RemoveMissileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RemoveMissileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_reset_first_third_person_view:
			ScriptingInterfaceOfIMBMission.call_ResetFirstThirdPersonViewDelegate = (ScriptingInterfaceOfIMBMission.ResetFirstThirdPersonViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ResetFirstThirdPersonViewDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_reset_teams:
			ScriptingInterfaceOfIMBMission.call_ResetTeamsDelegate = (ScriptingInterfaceOfIMBMission.ResetTeamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ResetTeamsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_restart_record:
			ScriptingInterfaceOfIMBMission.call_RestartRecordDelegate = (ScriptingInterfaceOfIMBMission.RestartRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RestartRecordDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_restore_record_from_file:
			ScriptingInterfaceOfIMBMission.call_RestoreRecordFromFileDelegate = (ScriptingInterfaceOfIMBMission.RestoreRecordFromFileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.RestoreRecordFromFileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_resume_mission_scene_sounds:
			ScriptingInterfaceOfIMBMission.call_ResumeMissionSceneSoundsDelegate = (ScriptingInterfaceOfIMBMission.ResumeMissionSceneSoundsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.ResumeMissionSceneSoundsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_bow_missile_speed_modifier:
			ScriptingInterfaceOfIMBMission.call_SetBowMissileSpeedModifierDelegate = (ScriptingInterfaceOfIMBMission.SetBowMissileSpeedModifierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetBowMissileSpeedModifierDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_camera_frame:
			ScriptingInterfaceOfIMBMission.call_SetCameraFrameDelegate = (ScriptingInterfaceOfIMBMission.SetCameraFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetCameraFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_camera_is_first_person:
			ScriptingInterfaceOfIMBMission.call_SetCameraIsFirstPersonDelegate = (ScriptingInterfaceOfIMBMission.SetCameraIsFirstPersonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetCameraIsFirstPersonDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_close_proximity_wave_sounds_enabled:
			ScriptingInterfaceOfIMBMission.call_SetCloseProximityWaveSoundsEnabledDelegate = (ScriptingInterfaceOfIMBMission.SetCloseProximityWaveSoundsEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetCloseProximityWaveSoundsEnabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_combat_type:
			ScriptingInterfaceOfIMBMission.call_SetCombatTypeDelegate = (ScriptingInterfaceOfIMBMission.SetCombatTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetCombatTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_crossbow_missile_speed_modifier:
			ScriptingInterfaceOfIMBMission.call_SetCrossbowMissileSpeedModifierDelegate = (ScriptingInterfaceOfIMBMission.SetCrossbowMissileSpeedModifierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetCrossbowMissileSpeedModifierDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_debug_agent:
			ScriptingInterfaceOfIMBMission.call_SetDebugAgentDelegate = (ScriptingInterfaceOfIMBMission.SetDebugAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetDebugAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_fall_avoid_system_active:
			ScriptingInterfaceOfIMBMission.call_SetFallAvoidSystemActiveDelegate = (ScriptingInterfaceOfIMBMission.SetFallAvoidSystemActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetFallAvoidSystemActiveDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_last_movement_key_pressed:
			ScriptingInterfaceOfIMBMission.call_SetLastMovementKeyPressedDelegate = (ScriptingInterfaceOfIMBMission.SetLastMovementKeyPressedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetLastMovementKeyPressedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_missile_range_modifier:
			ScriptingInterfaceOfIMBMission.call_SetMissileRangeModifierDelegate = (ScriptingInterfaceOfIMBMission.SetMissileRangeModifierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetMissileRangeModifierDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_mission_corpse_fade_out_time_in_seconds:
			ScriptingInterfaceOfIMBMission.call_SetMissionCorpseFadeOutTimeInSecondsDelegate = (ScriptingInterfaceOfIMBMission.SetMissionCorpseFadeOutTimeInSecondsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetMissionCorpseFadeOutTimeInSecondsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_navigation_face_cost_with_id_around_position:
			ScriptingInterfaceOfIMBMission.call_SetNavigationFaceCostWithIdAroundPositionDelegate = (ScriptingInterfaceOfIMBMission.SetNavigationFaceCostWithIdAroundPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetNavigationFaceCostWithIdAroundPositionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_override_corpse_count:
			ScriptingInterfaceOfIMBMission.call_SetOverrideCorpseCountDelegate = (ScriptingInterfaceOfIMBMission.SetOverrideCorpseCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetOverrideCorpseCountDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_pause_ai_tick:
			ScriptingInterfaceOfIMBMission.call_SetPauseAITickDelegate = (ScriptingInterfaceOfIMBMission.SetPauseAITickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetPauseAITickDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_random_decide_time_of_agents:
			ScriptingInterfaceOfIMBMission.call_SetRandomDecideTimeOfAgentsDelegate = (ScriptingInterfaceOfIMBMission.SetRandomDecideTimeOfAgentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetRandomDecideTimeOfAgentsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_render_parallel_logic_in_progress:
			ScriptingInterfaceOfIMBMission.call_SetRenderParallelLogicInProgressDelegate = (ScriptingInterfaceOfIMBMission.SetRenderParallelLogicInProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetRenderParallelLogicInProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_report_stuck_agents_mode:
			ScriptingInterfaceOfIMBMission.call_SetReportStuckAgentsModeDelegate = (ScriptingInterfaceOfIMBMission.SetReportStuckAgentsModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetReportStuckAgentsModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_throwing_missile_speed_modifier:
			ScriptingInterfaceOfIMBMission.call_SetThrowingMissileSpeedModifierDelegate = (ScriptingInterfaceOfIMBMission.SetThrowingMissileSpeedModifierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetThrowingMissileSpeedModifierDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_set_velocity_of_missile:
			ScriptingInterfaceOfIMBMission.call_SetVelocityOfMissileDelegate = (ScriptingInterfaceOfIMBMission.SetVelocityOfMissileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SetVelocityOfMissileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_skip_forward_mission_replay:
			ScriptingInterfaceOfIMBMission.call_SkipForwardMissionReplayDelegate = (ScriptingInterfaceOfIMBMission.SkipForwardMissionReplayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.SkipForwardMissionReplayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_start_recording:
			ScriptingInterfaceOfIMBMission.call_StartRecordingDelegate = (ScriptingInterfaceOfIMBMission.StartRecordingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.StartRecordingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_tick:
			ScriptingInterfaceOfIMBMission.call_TickDelegate = (ScriptingInterfaceOfIMBMission.TickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.TickDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBMission_tick_agents_and_teams_async:
			ScriptingInterfaceOfIMBMission.call_TickAgentsAndTeamsAsyncDelegate = (ScriptingInterfaceOfIMBMission.TickAgentsAndTeamsAsyncDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBMission.TickAgentsAndTeamsAsyncDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_add_new_bot_on_server:
			ScriptingInterfaceOfIMBNetwork.call_AddNewBotOnServerDelegate = (ScriptingInterfaceOfIMBNetwork.AddNewBotOnServerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.AddNewBotOnServerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_add_new_player_on_server:
			ScriptingInterfaceOfIMBNetwork.call_AddNewPlayerOnServerDelegate = (ScriptingInterfaceOfIMBNetwork.AddNewPlayerOnServerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.AddNewPlayerOnServerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_add_peer_to_disconnect:
			ScriptingInterfaceOfIMBNetwork.call_AddPeerToDisconnectDelegate = (ScriptingInterfaceOfIMBNetwork.AddPeerToDisconnectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.AddPeerToDisconnectDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_begin_broadcast_module_event:
			ScriptingInterfaceOfIMBNetwork.call_BeginBroadcastModuleEventDelegate = (ScriptingInterfaceOfIMBNetwork.BeginBroadcastModuleEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.BeginBroadcastModuleEventDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_begin_module_event_as_client:
			ScriptingInterfaceOfIMBNetwork.call_BeginModuleEventAsClientDelegate = (ScriptingInterfaceOfIMBNetwork.BeginModuleEventAsClientDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.BeginModuleEventAsClientDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_can_add_new_players_on_server:
			ScriptingInterfaceOfIMBNetwork.call_CanAddNewPlayersOnServerDelegate = (ScriptingInterfaceOfIMBNetwork.CanAddNewPlayersOnServerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.CanAddNewPlayersOnServerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_clear_replication_table_statistics:
			ScriptingInterfaceOfIMBNetwork.call_ClearReplicationTableStatisticsDelegate = (ScriptingInterfaceOfIMBNetwork.ClearReplicationTableStatisticsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ClearReplicationTableStatisticsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_elapsed_time_since_last_udp_packet_arrived:
			ScriptingInterfaceOfIMBNetwork.call_ElapsedTimeSinceLastUdpPacketArrivedDelegate = (ScriptingInterfaceOfIMBNetwork.ElapsedTimeSinceLastUdpPacketArrivedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ElapsedTimeSinceLastUdpPacketArrivedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_end_broadcast_module_event:
			ScriptingInterfaceOfIMBNetwork.call_EndBroadcastModuleEventDelegate = (ScriptingInterfaceOfIMBNetwork.EndBroadcastModuleEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.EndBroadcastModuleEventDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_end_module_event_as_client:
			ScriptingInterfaceOfIMBNetwork.call_EndModuleEventAsClientDelegate = (ScriptingInterfaceOfIMBNetwork.EndModuleEventAsClientDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.EndModuleEventAsClientDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_get_active_udp_sessions_ip_address:
			ScriptingInterfaceOfIMBNetwork.call_GetActiveUdpSessionsIpAddressDelegate = (ScriptingInterfaceOfIMBNetwork.GetActiveUdpSessionsIpAddressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.GetActiveUdpSessionsIpAddressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_get_average_packet_loss_ratio:
			ScriptingInterfaceOfIMBNetwork.call_GetAveragePacketLossRatioDelegate = (ScriptingInterfaceOfIMBNetwork.GetAveragePacketLossRatioDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.GetAveragePacketLossRatioDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_get_debug_uploads_in_bits:
			ScriptingInterfaceOfIMBNetwork.call_GetDebugUploadsInBitsDelegate = (ScriptingInterfaceOfIMBNetwork.GetDebugUploadsInBitsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.GetDebugUploadsInBitsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_get_multiplayer_disabled:
			ScriptingInterfaceOfIMBNetwork.call_GetMultiplayerDisabledDelegate = (ScriptingInterfaceOfIMBNetwork.GetMultiplayerDisabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.GetMultiplayerDisabledDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_initialize_client_side:
			ScriptingInterfaceOfIMBNetwork.call_InitializeClientSideDelegate = (ScriptingInterfaceOfIMBNetwork.InitializeClientSideDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.InitializeClientSideDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_initialize_server_side:
			ScriptingInterfaceOfIMBNetwork.call_InitializeServerSideDelegate = (ScriptingInterfaceOfIMBNetwork.InitializeServerSideDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.InitializeServerSideDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_is_dedicated_server:
			ScriptingInterfaceOfIMBNetwork.call_IsDedicatedServerDelegate = (ScriptingInterfaceOfIMBNetwork.IsDedicatedServerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.IsDedicatedServerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_prepare_new_udp_session:
			ScriptingInterfaceOfIMBNetwork.call_PrepareNewUdpSessionDelegate = (ScriptingInterfaceOfIMBNetwork.PrepareNewUdpSessionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.PrepareNewUdpSessionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_print_debug_stats:
			ScriptingInterfaceOfIMBNetwork.call_PrintDebugStatsDelegate = (ScriptingInterfaceOfIMBNetwork.PrintDebugStatsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.PrintDebugStatsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_print_replication_table_statistics:
			ScriptingInterfaceOfIMBNetwork.call_PrintReplicationTableStatisticsDelegate = (ScriptingInterfaceOfIMBNetwork.PrintReplicationTableStatisticsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.PrintReplicationTableStatisticsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_read_byte_array_from_packet:
			ScriptingInterfaceOfIMBNetwork.call_ReadByteArrayFromPacketDelegate = (ScriptingInterfaceOfIMBNetwork.ReadByteArrayFromPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ReadByteArrayFromPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_read_float_from_packet:
			ScriptingInterfaceOfIMBNetwork.call_ReadFloatFromPacketDelegate = (ScriptingInterfaceOfIMBNetwork.ReadFloatFromPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ReadFloatFromPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_read_int_from_packet:
			ScriptingInterfaceOfIMBNetwork.call_ReadIntFromPacketDelegate = (ScriptingInterfaceOfIMBNetwork.ReadIntFromPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ReadIntFromPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_read_long_from_packet:
			ScriptingInterfaceOfIMBNetwork.call_ReadLongFromPacketDelegate = (ScriptingInterfaceOfIMBNetwork.ReadLongFromPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ReadLongFromPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_read_string_from_packet:
			ScriptingInterfaceOfIMBNetwork.call_ReadStringFromPacketDelegate = (ScriptingInterfaceOfIMBNetwork.ReadStringFromPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ReadStringFromPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_read_uint_from_packet:
			ScriptingInterfaceOfIMBNetwork.call_ReadUintFromPacketDelegate = (ScriptingInterfaceOfIMBNetwork.ReadUintFromPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ReadUintFromPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_read_ulong_from_packet:
			ScriptingInterfaceOfIMBNetwork.call_ReadUlongFromPacketDelegate = (ScriptingInterfaceOfIMBNetwork.ReadUlongFromPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ReadUlongFromPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_remove_bot_on_server:
			ScriptingInterfaceOfIMBNetwork.call_RemoveBotOnServerDelegate = (ScriptingInterfaceOfIMBNetwork.RemoveBotOnServerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.RemoveBotOnServerDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_reset_debug_uploads:
			ScriptingInterfaceOfIMBNetwork.call_ResetDebugUploadsDelegate = (ScriptingInterfaceOfIMBNetwork.ResetDebugUploadsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ResetDebugUploadsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_reset_debug_variables:
			ScriptingInterfaceOfIMBNetwork.call_ResetDebugVariablesDelegate = (ScriptingInterfaceOfIMBNetwork.ResetDebugVariablesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ResetDebugVariablesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_reset_mission_data:
			ScriptingInterfaceOfIMBNetwork.call_ResetMissionDataDelegate = (ScriptingInterfaceOfIMBNetwork.ResetMissionDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ResetMissionDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_server_ping:
			ScriptingInterfaceOfIMBNetwork.call_ServerPingDelegate = (ScriptingInterfaceOfIMBNetwork.ServerPingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.ServerPingDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_set_server_bandwidth_limit_in_mbps:
			ScriptingInterfaceOfIMBNetwork.call_SetServerBandwidthLimitInMbpsDelegate = (ScriptingInterfaceOfIMBNetwork.SetServerBandwidthLimitInMbpsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.SetServerBandwidthLimitInMbpsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_set_server_frame_rate:
			ScriptingInterfaceOfIMBNetwork.call_SetServerFrameRateDelegate = (ScriptingInterfaceOfIMBNetwork.SetServerFrameRateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.SetServerFrameRateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_set_server_tick_rate:
			ScriptingInterfaceOfIMBNetwork.call_SetServerTickRateDelegate = (ScriptingInterfaceOfIMBNetwork.SetServerTickRateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.SetServerTickRateDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_terminate_client_side:
			ScriptingInterfaceOfIMBNetwork.call_TerminateClientSideDelegate = (ScriptingInterfaceOfIMBNetwork.TerminateClientSideDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.TerminateClientSideDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_terminate_server_side:
			ScriptingInterfaceOfIMBNetwork.call_TerminateServerSideDelegate = (ScriptingInterfaceOfIMBNetwork.TerminateServerSideDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.TerminateServerSideDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_write_byte_array_to_packet:
			ScriptingInterfaceOfIMBNetwork.call_WriteByteArrayToPacketDelegate = (ScriptingInterfaceOfIMBNetwork.WriteByteArrayToPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.WriteByteArrayToPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_write_float_to_packet:
			ScriptingInterfaceOfIMBNetwork.call_WriteFloatToPacketDelegate = (ScriptingInterfaceOfIMBNetwork.WriteFloatToPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.WriteFloatToPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_write_int_to_packet:
			ScriptingInterfaceOfIMBNetwork.call_WriteIntToPacketDelegate = (ScriptingInterfaceOfIMBNetwork.WriteIntToPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.WriteIntToPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_write_long_to_packet:
			ScriptingInterfaceOfIMBNetwork.call_WriteLongToPacketDelegate = (ScriptingInterfaceOfIMBNetwork.WriteLongToPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.WriteLongToPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_write_string_to_packet:
			ScriptingInterfaceOfIMBNetwork.call_WriteStringToPacketDelegate = (ScriptingInterfaceOfIMBNetwork.WriteStringToPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.WriteStringToPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_write_uint_to_packet:
			ScriptingInterfaceOfIMBNetwork.call_WriteUintToPacketDelegate = (ScriptingInterfaceOfIMBNetwork.WriteUintToPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.WriteUintToPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBNetwork_write_ulong_to_packet:
			ScriptingInterfaceOfIMBNetwork.call_WriteUlongToPacketDelegate = (ScriptingInterfaceOfIMBNetwork.WriteUlongToPacketDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBNetwork.WriteUlongToPacketDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_begin_module_event:
			ScriptingInterfaceOfIMBPeer.call_BeginModuleEventDelegate = (ScriptingInterfaceOfIMBPeer.BeginModuleEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.BeginModuleEventDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_end_module_event:
			ScriptingInterfaceOfIMBPeer.call_EndModuleEventDelegate = (ScriptingInterfaceOfIMBPeer.EndModuleEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.EndModuleEventDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_get_average_loss_percent:
			ScriptingInterfaceOfIMBPeer.call_GetAverageLossPercentDelegate = (ScriptingInterfaceOfIMBPeer.GetAverageLossPercentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.GetAverageLossPercentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_get_average_ping_in_milliseconds:
			ScriptingInterfaceOfIMBPeer.call_GetAveragePingInMillisecondsDelegate = (ScriptingInterfaceOfIMBPeer.GetAveragePingInMillisecondsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.GetAveragePingInMillisecondsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_get_host:
			ScriptingInterfaceOfIMBPeer.call_GetHostDelegate = (ScriptingInterfaceOfIMBPeer.GetHostDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.GetHostDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_get_is_synchronized:
			ScriptingInterfaceOfIMBPeer.call_GetIsSynchronizedDelegate = (ScriptingInterfaceOfIMBPeer.GetIsSynchronizedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.GetIsSynchronizedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_get_port:
			ScriptingInterfaceOfIMBPeer.call_GetPortDelegate = (ScriptingInterfaceOfIMBPeer.GetPortDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.GetPortDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_get_reversed_host:
			ScriptingInterfaceOfIMBPeer.call_GetReversedHostDelegate = (ScriptingInterfaceOfIMBPeer.GetReversedHostDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.GetReversedHostDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_is_active:
			ScriptingInterfaceOfIMBPeer.call_IsActiveDelegate = (ScriptingInterfaceOfIMBPeer.IsActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.IsActiveDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_send_existing_objects:
			ScriptingInterfaceOfIMBPeer.call_SendExistingObjectsDelegate = (ScriptingInterfaceOfIMBPeer.SendExistingObjectsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.SendExistingObjectsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_set_controlled_agent:
			ScriptingInterfaceOfIMBPeer.call_SetControlledAgentDelegate = (ScriptingInterfaceOfIMBPeer.SetControlledAgentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.SetControlledAgentDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_set_is_synchronized:
			ScriptingInterfaceOfIMBPeer.call_SetIsSynchronizedDelegate = (ScriptingInterfaceOfIMBPeer.SetIsSynchronizedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.SetIsSynchronizedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_set_relevant_game_options:
			ScriptingInterfaceOfIMBPeer.call_SetRelevantGameOptionsDelegate = (ScriptingInterfaceOfIMBPeer.SetRelevantGameOptionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.SetRelevantGameOptionsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_set_team:
			ScriptingInterfaceOfIMBPeer.call_SetTeamDelegate = (ScriptingInterfaceOfIMBPeer.SetTeamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.SetTeamDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBPeer_set_user_data:
			ScriptingInterfaceOfIMBPeer.call_SetUserDataDelegate = (ScriptingInterfaceOfIMBPeer.SetUserDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBPeer.SetUserDataDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBScreen_on_edit_mode_enter_press:
			ScriptingInterfaceOfIMBScreen.call_OnEditModeEnterPressDelegate = (ScriptingInterfaceOfIMBScreen.OnEditModeEnterPressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBScreen.OnEditModeEnterPressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBScreen_on_edit_mode_enter_release:
			ScriptingInterfaceOfIMBScreen.call_OnEditModeEnterReleaseDelegate = (ScriptingInterfaceOfIMBScreen.OnEditModeEnterReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBScreen.OnEditModeEnterReleaseDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBScreen_on_exit_button_click:
			ScriptingInterfaceOfIMBScreen.call_OnExitButtonClickDelegate = (ScriptingInterfaceOfIMBScreen.OnExitButtonClickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBScreen.OnExitButtonClickDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_create_agent_skeleton:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_CreateAgentSkeletonDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.CreateAgentSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.CreateAgentSkeletonDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_create_simple_skeleton:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_CreateSimpleSkeletonDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.CreateSimpleSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.CreateSimpleSkeletonDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_create_with_action_set:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_CreateWithActionSetDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.CreateWithActionSetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.CreateWithActionSetDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_does_action_continue_with_current_action_at_channel:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_DoesActionContinueWithCurrentActionAtChannelDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.DoesActionContinueWithCurrentActionAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.DoesActionContinueWithCurrentActionAtChannelDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_get_action_at_channel:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_GetActionAtChannelDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.GetActionAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.GetActionAtChannelDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_get_bone_entitial_frame:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_GetBoneEntitialFrameDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.GetBoneEntitialFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.GetBoneEntitialFrameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_get_bone_entitial_frame_at_animation_progress:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_GetBoneEntitialFrameAtAnimationProgressDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.GetBoneEntitialFrameAtAnimationProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.GetBoneEntitialFrameAtAnimationProgressDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_get_skeleton_face_animation_name:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_GetSkeletonFaceAnimationNameDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.GetSkeletonFaceAnimationNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.GetSkeletonFaceAnimationNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_get_skeleton_face_animation_time:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_GetSkeletonFaceAnimationTimeDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.GetSkeletonFaceAnimationTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.GetSkeletonFaceAnimationTimeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_set_agent_action_channel:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_SetAgentActionChannelDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.SetAgentActionChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.SetAgentActionChannelDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_set_animation_at_channel:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_SetAnimationAtChannelDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.SetAnimationAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.SetAnimationAtChannelDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_set_facial_animation_of_channel:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_SetFacialAnimationOfChannelDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.SetFacialAnimationOfChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.SetFacialAnimationOfChannelDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_set_skeleton_face_animation_time:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_SetSkeletonFaceAnimationTimeDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.SetSkeletonFaceAnimationTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.SetSkeletonFaceAnimationTimeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSkeletonExtensions_tick_action_channels:
			ScriptingInterfaceOfIMBSkeletonExtensions.call_TickActionChannelsDelegate = (ScriptingInterfaceOfIMBSkeletonExtensions.TickActionChannelsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSkeletonExtensions.TickActionChannelsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSoundEvent_create_event_from_external_file:
			ScriptingInterfaceOfIMBSoundEvent.call_CreateEventFromExternalFileDelegate = (ScriptingInterfaceOfIMBSoundEvent.CreateEventFromExternalFileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSoundEvent.CreateEventFromExternalFileDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSoundEvent_create_event_from_sound_buffer:
			ScriptingInterfaceOfIMBSoundEvent.call_CreateEventFromSoundBufferDelegate = (ScriptingInterfaceOfIMBSoundEvent.CreateEventFromSoundBufferDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSoundEvent.CreateEventFromSoundBufferDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSoundEvent_play_sound:
			ScriptingInterfaceOfIMBSoundEvent.call_PlaySoundDelegate = (ScriptingInterfaceOfIMBSoundEvent.PlaySoundDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSoundEvent.PlaySoundDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSoundEvent_play_sound_with_int_param:
			ScriptingInterfaceOfIMBSoundEvent.call_PlaySoundWithIntParamDelegate = (ScriptingInterfaceOfIMBSoundEvent.PlaySoundWithIntParamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSoundEvent.PlaySoundWithIntParamDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSoundEvent_play_sound_with_param:
			ScriptingInterfaceOfIMBSoundEvent.call_PlaySoundWithParamDelegate = (ScriptingInterfaceOfIMBSoundEvent.PlaySoundWithParamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSoundEvent.PlaySoundWithParamDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBSoundEvent_play_sound_with_str_param:
			ScriptingInterfaceOfIMBSoundEvent.call_PlaySoundWithStrParamDelegate = (ScriptingInterfaceOfIMBSoundEvent.PlaySoundWithStrParamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBSoundEvent.PlaySoundWithStrParamDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTeam_is_enemy:
			ScriptingInterfaceOfIMBTeam.call_IsEnemyDelegate = (ScriptingInterfaceOfIMBTeam.IsEnemyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTeam.IsEnemyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTeam_set_is_enemy:
			ScriptingInterfaceOfIMBTeam.call_SetIsEnemyDelegate = (ScriptingInterfaceOfIMBTeam.SetIsEnemyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTeam.SetIsEnemyDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_auto_continue:
			ScriptingInterfaceOfIMBTestRun.call_AutoContinueDelegate = (ScriptingInterfaceOfIMBTestRun.AutoContinueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.AutoContinueDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_close_scene:
			ScriptingInterfaceOfIMBTestRun.call_CloseSceneDelegate = (ScriptingInterfaceOfIMBTestRun.CloseSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.CloseSceneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_enter_edit_mode:
			ScriptingInterfaceOfIMBTestRun.call_EnterEditModeDelegate = (ScriptingInterfaceOfIMBTestRun.EnterEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.EnterEditModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_get_fps:
			ScriptingInterfaceOfIMBTestRun.call_GetFPSDelegate = (ScriptingInterfaceOfIMBTestRun.GetFPSDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.GetFPSDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_leave_edit_mode:
			ScriptingInterfaceOfIMBTestRun.call_LeaveEditModeDelegate = (ScriptingInterfaceOfIMBTestRun.LeaveEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.LeaveEditModeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_new_scene:
			ScriptingInterfaceOfIMBTestRun.call_NewSceneDelegate = (ScriptingInterfaceOfIMBTestRun.NewSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.NewSceneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_open_default_scene:
			ScriptingInterfaceOfIMBTestRun.call_OpenDefaultSceneDelegate = (ScriptingInterfaceOfIMBTestRun.OpenDefaultSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.OpenDefaultSceneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_open_scene:
			ScriptingInterfaceOfIMBTestRun.call_OpenSceneDelegate = (ScriptingInterfaceOfIMBTestRun.OpenSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.OpenSceneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_save_scene:
			ScriptingInterfaceOfIMBTestRun.call_SaveSceneDelegate = (ScriptingInterfaceOfIMBTestRun.SaveSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.SaveSceneDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBTestRun_start_mission:
			ScriptingInterfaceOfIMBTestRun.call_StartMissionDelegate = (ScriptingInterfaceOfIMBTestRun.StartMissionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBTestRun.StartMissionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBVoiceManager_get_voice_definition_count_with_monster_sound_and_collision_info_class_name:
			ScriptingInterfaceOfIMBVoiceManager.call_GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassNameDelegate = (ScriptingInterfaceOfIMBVoiceManager.GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBVoiceManager.GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBVoiceManager_get_voice_definitions_with_monster_sound_and_collision_info_class_name:
			ScriptingInterfaceOfIMBVoiceManager.call_GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassNameDelegate = (ScriptingInterfaceOfIMBVoiceManager.GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBVoiceManager.GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassNameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBVoiceManager_get_voice_type_index:
			ScriptingInterfaceOfIMBVoiceManager.call_GetVoiceTypeIndexDelegate = (ScriptingInterfaceOfIMBVoiceManager.GetVoiceTypeIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBVoiceManager.GetVoiceTypeIndexDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWindowManager_dont_change_cursor_pos:
			ScriptingInterfaceOfIMBWindowManager.call_DontChangeCursorPosDelegate = (ScriptingInterfaceOfIMBWindowManager.DontChangeCursorPosDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWindowManager.DontChangeCursorPosDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWindowManager_erase_message_lines:
			ScriptingInterfaceOfIMBWindowManager.call_EraseMessageLinesDelegate = (ScriptingInterfaceOfIMBWindowManager.EraseMessageLinesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWindowManager.EraseMessageLinesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWindowManager_get_screen_resolution:
			ScriptingInterfaceOfIMBWindowManager.call_GetScreenResolutionDelegate = (ScriptingInterfaceOfIMBWindowManager.GetScreenResolutionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWindowManager.GetScreenResolutionDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWindowManager_pre_display:
			ScriptingInterfaceOfIMBWindowManager.call_PreDisplayDelegate = (ScriptingInterfaceOfIMBWindowManager.PreDisplayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWindowManager.PreDisplayDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWindowManager_screen_to_world:
			ScriptingInterfaceOfIMBWindowManager.call_ScreenToWorldDelegate = (ScriptingInterfaceOfIMBWindowManager.ScreenToWorldDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWindowManager.ScreenToWorldDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWindowManager_world_to_screen:
			ScriptingInterfaceOfIMBWindowManager.call_WorldToScreenDelegate = (ScriptingInterfaceOfIMBWindowManager.WorldToScreenDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWindowManager.WorldToScreenDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWindowManager_world_to_screen_with_fixed_z:
			ScriptingInterfaceOfIMBWindowManager.call_WorldToScreenWithFixedZDelegate = (ScriptingInterfaceOfIMBWindowManager.WorldToScreenWithFixedZDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWindowManager.WorldToScreenWithFixedZDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_check_resource_modifications:
			ScriptingInterfaceOfIMBWorld.call_CheckResourceModificationsDelegate = (ScriptingInterfaceOfIMBWorld.CheckResourceModificationsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.CheckResourceModificationsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_fix_skeletons:
			ScriptingInterfaceOfIMBWorld.call_FixSkeletonsDelegate = (ScriptingInterfaceOfIMBWorld.FixSkeletonsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.FixSkeletonsDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_get_game_type:
			ScriptingInterfaceOfIMBWorld.call_GetGameTypeDelegate = (ScriptingInterfaceOfIMBWorld.GetGameTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.GetGameTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_get_global_time:
			ScriptingInterfaceOfIMBWorld.call_GetGlobalTimeDelegate = (ScriptingInterfaceOfIMBWorld.GetGlobalTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.GetGlobalTimeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_get_last_messages:
			ScriptingInterfaceOfIMBWorld.call_GetLastMessagesDelegate = (ScriptingInterfaceOfIMBWorld.GetLastMessagesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.GetLastMessagesDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_pause_game:
			ScriptingInterfaceOfIMBWorld.call_PauseGameDelegate = (ScriptingInterfaceOfIMBWorld.PauseGameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.PauseGameDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_set_body_used:
			ScriptingInterfaceOfIMBWorld.call_SetBodyUsedDelegate = (ScriptingInterfaceOfIMBWorld.SetBodyUsedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.SetBodyUsedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_set_game_type:
			ScriptingInterfaceOfIMBWorld.call_SetGameTypeDelegate = (ScriptingInterfaceOfIMBWorld.SetGameTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.SetGameTypeDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_set_material_used:
			ScriptingInterfaceOfIMBWorld.call_SetMaterialUsedDelegate = (ScriptingInterfaceOfIMBWorld.SetMaterialUsedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.SetMaterialUsedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_set_mesh_used:
			ScriptingInterfaceOfIMBWorld.call_SetMeshUsedDelegate = (ScriptingInterfaceOfIMBWorld.SetMeshUsedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.SetMeshUsedDelegate));
			break;
		case CoreInterfaceGeneratedEnum.enm_IMono_MBWorld_unpause_game:
			ScriptingInterfaceOfIMBWorld.call_UnpauseGameDelegate = (ScriptingInterfaceOfIMBWorld.UnpauseGameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMBWorld.UnpauseGameDelegate));
			break;
		case (CoreInterfaceGeneratedEnum)27:
			break;
		}
	}
}
