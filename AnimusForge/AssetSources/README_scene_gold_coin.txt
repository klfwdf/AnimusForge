Scene taunt gold drops now try the custom item id first:

  animusforge_denar_coin_item

The live ModuleData version currently uses the native mesh ui_intentory_coin_a with the native
bo_sling_ammo collision body, so it does not require an external asset package.

If that item exists, the mod uses MissionWeapon + SpawnWeaponWithNewEntity(WithPhysics) so the coins
scatter using Bannerlord's native dropped-item physics.

If the item does not exist yet, the mod falls back to the custom prefab name:

  animusforge_denar_coin

Do not place animusforge_denar_coin.prefab.xml into AnimusForge/Prefabs until the mesh
animusforge_denar_coin and collision body bo_animusforge_denar_coin have been imported into
the Bannerlord Resource Browser and packed into the module asset package.

The OBJ/MTL files in this folder are source assets only. Bannerlord will not load raw OBJ files
as runtime meshes; they must be imported and packed first.

When the asset package is ready:

1. Put animusforge_scene_gold_items.xml into AnimusForge/ModuleData/.
2. Add the XmlNode from SubModule_items_patch_example.xml into AnimusForge/SubModule.xml.
3. Put animusforge_denar_coin.prefab.xml into AnimusForge/Prefabs/ if you still want the prefab fallback.
