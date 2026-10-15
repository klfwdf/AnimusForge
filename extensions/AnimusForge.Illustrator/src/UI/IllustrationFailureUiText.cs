using System;

namespace AnimusForge.Illustrator.UI
{
    // Display-only projection. Keep worker errors, diagnostic records and provider details intact.
    internal static class IllustrationFailureUiText
    {
        private static readonly System.Text.RegularExpressions.Regex ImageBudget = new System.Text.RegularExpressions.Regex(
            IllustratorText.Raw("Error_ImagePromptBudgetRegex"), System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        internal static string Project(string raw)
            => Project(raw, unwrapCommunication: true);

        private static string Project(string raw, bool unwrapCommunication)
        {
            if (raw == IllustratorText.Raw("Error_PanoramaRootsBudget") || raw == "The current scene has more than 32768 root entities to traverse. The environment snapshot stopped; a truncated copy must not be marked as a complete panorama.")
                return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_ROOTS_BUDGET", "The current scene has more than 32768 root entities to traverse. The environment snapshot stopped; a truncated copy must not be marked as a complete panorama.");
            if (raw == IllustratorText.Raw("Error_PanoramaNodesBudget") || raw == "The current environment hierarchy has more than 32768 nodes. The snapshot stopped; an incomplete copy must not be marked as a complete panorama.")
                return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_NODES_BUDGET", "The current environment hierarchy has more than 32768 nodes. The snapshot stopped; an incomplete copy must not be marked as a complete panorama.");
            var imageBudget = ImageBudget.Match(raw ?? string.Empty);
            if (imageBudget.Success)
                return IllustratorUiText.Format("AF_ILL_ERROR_IMAGE_PROMPT_BUDGET", "The image prompt exceeds the {LIMIT}-character budget and cannot be shortened while preserving facts, reference-image constraints and complete paragraphs. No request was sent. Shorten custom styles or requirements, or reduce the material for this request.", "LIMIT", imageBudget.Groups[1].Value);
            switch (IllustratorText.NormalizeLegacyCase(raw))
            {
                case "AFICASE765de577eda396dd":
                case "The images/edits endpoint does not support this reference image request. Generation stopped to avoid silently losing character or scene references. Enable the preferred conversation multimodal image channel or configure a usable /images/edits endpoint.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_EDITS_UNSUPPORTED", "The images/edits endpoint does not support this reference image request. Generation stopped to avoid silently losing character or scene references. Enable the preferred conversation multimodal image channel or configure a usable /images/edits endpoint.");
                case "AFICASE31492fedefc8eb58":
                case "The interface explicitly does not support image input; one text-only conception attempt was made":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_TEXT_ONLY_ATTEMPT", "The interface explicitly does not support image input; one text-only conception attempt was made");
                case "AFICASE19ce89a05a432700":
                case "The native panorama render name exceeds the safe length.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_NAME", "The native panorama render name exceeds the safe length.");
                case "AFICASE5ebe388db56d9106":
                case "An environment snapshot batch is already running.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_BATCH_ACTIVE", "An environment snapshot batch is already running.");
                case "AFICASE1c0e9b57843701b7":
                case "Observation lighting requires a valid isolated scene copy.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_SCENE", "Observation lighting requires a valid isolated scene copy.");
                case "AFICASE57332399775473fe":
                case "Observation lighting has no off-screen camera position.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_POSITION_MISSING", "Observation lighting has no off-screen camera position.");
                case "AFICASE43fe8d2ff2069a1a":
                case "The off-screen observation lighting camera position is invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_POSITION_INVALID", "The off-screen observation lighting camera position is invalid.");
                case "AFICASE9d533584d8193eb7":
                case "The observation lighting coverage is invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_COVERAGE", "The observation lighting coverage is invalid.");
                case "AFICASE0d74292082e12424":
                case "Observation lights could not be created in the isolated scene.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_CREATE", "Observation lights could not be created in the isolated scene.");
                case "AFICASE81a976c54cf9c68d":
                case "The native observation light name exceeds the safe length.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_NAME", "The native observation light name exceeds the safe length.");
                case "AFICASE4ce414b787fe6a77":
                case "The engine did not create a valid observation light.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_NATIVE", "The engine did not create a valid observation light.");
                case "AFICASE612a69631eebd8c1":
                case "The observation light could not be attached to the isolated scene.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_ATTACH", "The observation light could not be attached to the isolated scene.");
                case "AFICASEe6ecb865f88e4fe6":
                case "The observation light does not belong to an isolated scene entity.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_LIGHTING_OWNER", "The observation light does not belong to an isolated scene entity.");
                case "AFICASE2138c4705e7120d7":
                case "Resource supplementation was cancelled.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SUPPLEMENT_CANCELLED", "Resource supplementation was cancelled.");
                case "AFICASE4fee994917391cc7":
                case "The resource template scene name is too long.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_TEMPLATE_NAME", "The resource template scene name is too long.");
                case "AFICASE8c3466a000fc33f1":
                case "Resource supplementation exceeded its entity traversal budget. Capture stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SUPPLEMENT_ENTITY_BUDGET", "Resource supplementation exceeded its entity traversal budget. Capture stopped.");
                case "AFICASEef4a13b5ed18eeaf":
                case "The resource template hierarchy exceeds the traversal budget.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_TEMPLATE_DEPTH", "The resource template hierarchy exceeds the traversal budget.");
                case "AFICASE25341083f09cab04":
                case "The resource template does not belong to the owned scene. Its transform was not changed.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_TEMPLATE_OWNER", "The resource template does not belong to the owned scene. Its transform was not changed.");
                case "AFICASEc43af8a48fcbf232":
                case "A static resource template could not be created.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_TEMPLATE_CREATE", "A static resource template could not be created.");
                case "AFICASE687f51115d86f4e3":
                case "Resource geometry could not be loaded into the isolated template scene.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_TEMPLATE_LOAD", "Resource geometry could not be loaded into the isolated template scene.");
                case "AFICASE92af66dc9f00dc24":
                case "An independent resource mesh copy could not be obtained.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_COPY", "An independent resource mesh copy could not be obtained.");
                case "AFICASE4f7bb69cc9161914":
                case "Resource geometry did not correctly enter the isolated render scene.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_SCENE", "Resource geometry did not correctly enter the isolated render scene.");
                case "AFICASE0501fd1e67bb74cc":
                case "The resource copy mesh bounds are invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_BOUNDS", "The resource copy mesh bounds are invalid.");
                case "AFICASE70709b676992b1e3":
                case "The panorama snapshot has no isolated scene. The current mission scene was not modified.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_ISOLATION", "The panorama snapshot has no isolated scene. The current mission scene was not modified.");
                case "AFICASE94eba315b78347ac":
                case "The current scene changed. Its resources cannot be read.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCENE_CHANGED", "The current scene changed. Its resources cannot be read.");
                case "AFICASE056066715081e0ba":
                case "The current scene's actual resource file could not be resolved. Environment reconstruction stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCENE_RESOURCE_RESOLVE", "The current scene's actual resource file could not be resolved. Environment reconstruction stopped.");
                case "AFICASEd77d276bcea67350":
                case "Scene resource read scheduling stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCENE_READ_SCHEDULER", "Scene resource read scheduling stopped.");
                case "AFICASEbeadce8c85bb169f":
                case "The scene or visible level changed while reading resources. Capture again.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCENE_READ_CHANGED", "The scene or visible level changed while reading resources. Capture again.");
                case "AFICASEbec3bbf325dca0ac":
                case "The environment snapshot could not be created on the game thread.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_CREATE", "The environment snapshot could not be created on the game thread.");
                case "AFICASE2ea7db48b671b715":
                case "Environment snapshot frame scheduling stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_SCHEDULER", "Environment snapshot frame scheduling stopped.");
                case "AFICASEf7591a2b5e3c6d8c":
                case "Scene resource supplementation scheduling stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SUPPLEMENT_SCHEDULER", "Scene resource supplementation scheduling stopped.");
                case "AFICASE5149b45e08cc9bfa":
                case "Scene resource supplementation frame scheduling stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SUPPLEMENT_FRAME_SCHEDULER", "Scene resource supplementation frame scheduling stopped.");
                case "AFICASEad29686613d1e39f":
                case "Scene template cleanup scheduling stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_TEMPLATE_CLEANUP_SCHEDULER", "Scene template cleanup scheduling stopped.");
                case "AFICASE12a8f325a60959c8":
                case "Neither the scene nor its resources contains usable static geometry. A blank environment reference will not be generated.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_GEOMETRY_EMPTY", "Neither the scene nor its resources contains usable static geometry. A blank environment reference will not be generated.");
                case "AFICASEbdbdf94f8e0baab0":
                case "The scene's visible level changed during capture. Capture again.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCENE_VISIBLE_LEVEL", "The scene's visible level changed during capture. Capture again.");
                case "AFICASE90a5f647c95e96f8":
                case "The native panorama scene name exceeds the safe length.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_NAME", "The native panorama scene name exceeds the safe length.");
                case "AFICASEb482cbd99342eadf":
                case "The capture center for the surroundings near the player could not be determined.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_CENTER_MISSING", "The capture center for the surroundings near the player could not be determined.");
                case "AFICASE38423e6cc29fedb7":
                case "The environment capture center is invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_CENTER_INVALID", "The environment capture center is invalid.");
                case "AFICASE94debb6b2ddc49d6":
                case "The scene for the environment snapshot ended or changed.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_SCENE_ENDED", "The scene for the environment snapshot ended or changed.");
                case "AFICASEfda42ba646e06c94":
                case "The environment snapshot was cancelled.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SNAPSHOT_CANCELLED", "The environment snapshot was cancelled.");
                case "AFICASE5be584f3d9b63a5f":
                case "An independent copy of the scene's static mesh could not be obtained.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_STATIC_COPY", "An independent copy of the scene's static mesh could not be obtained.");
                case "AFICASE2700dd579ad4f599":
                case "The static mesh did not correctly enter the isolated scene.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_STATIC_SCENE", "The static mesh did not correctly enter the isolated scene.");
                case "AFICASEfb98400f52e3287e":
                case "The static copy mesh bounds are invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_STATIC_BOUNDS", "The static copy mesh bounds are invalid.");
                case "AFICASEb3aff0c14c5ccca4":
                case "The actual path of the current scene resource file was not obtained.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_PATH_MISSING", "The actual path of the current scene resource file was not obtained.");
                case "AFICASEa34bb829a4df7490":
                case "The current scene resource file does not exist.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_FILE_MISSING", "The current scene resource file does not exist.");
                case "AFICASEac3a901827164126":
                case "The scene resource is empty or exceeds the 8 MiB read budget.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_READ_BUDGET", "The scene resource is empty or exceeds the 8 MiB read budget.");
                case "AFICASE4f55b71adc2e9cef":
                case "The scene resource changed before reading. Capture again.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_CHANGED_BEFORE", "The scene resource changed before reading. Capture again.");
                case "AFICASEf1fb7acc80a0b85d":
                case "The scene resource changed during reading. Capture again.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_CHANGED_DURING", "The scene resource changed during reading. Capture again.");
                case "AFICASE8266acd7d0919d0d":
                case "Access to the current scene resource file was denied.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_ACCESS", "Access to the current scene resource file was denied.");
                case "AFICASE9ddde1e6fa0f0ac4":
                case "The current resource is not valid scene XML.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_SCENE_XML", "The current resource is not valid scene XML.");
                case "AFICASE4f01dcd5a01528db":
                case "The scene resource contains duplicate entities nodes.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_ENTITIES_DUPLICATE", "The scene resource contains duplicate entities nodes.");
                case "AFICASEed007c0ab3089dc8":
                case "The scene resource contains duplicate levels nodes.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_LEVELS_DUPLICATE", "The scene resource contains duplicate levels nodes.");
                case "AFICASE368844dbc2fb7b02":
                case "The current scene resource lacks a scene or entities node.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_NODES_MISSING", "The current scene resource lacks a scene or entities node.");
                case "AFICASEda8123a1d875aa73":
                case "The scene XML hierarchy exceeds the 64-level budget.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_XML_DEPTH", "The scene XML hierarchy exceeds the 64-level budget.");
                case "AFICASEa7ed57581c9b3aa1":
                case "Scene XML streaming exceeded the 524288-element budget.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_XML_ELEMENTS", "Scene XML streaming exceeded the 524288-element budget.");
                case "AFICASE2d59e27a67c9f873":
                case "The scene XML uses an unsupported namespace.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_XML_NAMESPACE", "The scene XML uses an unsupported namespace.");
                case "AFICASE6807c364800bc2ae":
                case "A scene entity subtree exceeds the 32768-element memory budget.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_SUBTREE_ELEMENTS", "A scene entity subtree exceeds the 32768-element memory budget.");
                case "AFICASEb1de378f18256092":
                case "The scene entity subtree ended unexpectedly.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_SUBTREE_END", "The scene entity subtree ended unexpectedly.");
                case "AFICASE6278c7a5a21c1516":
                case "The scene upgrade level definition is invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_LEVEL_DEFINITION", "The scene upgrade level definition is invalid.");
                case "AFICASE738ca0d7d9fa875f":
                case "The scene upgrade level name or mask definition is invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_LEVEL_MASK", "The scene upgrade level name or mask definition is invalid.");
                case "AFICASE89237dcf38f85267":
                case "The static scene geometry plan exceeds the 32768-item budget.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESOURCE_GEOMETRY_BUDGET", "The static scene geometry plan exceeds the 32768-item budget.");
                case "AFICASE6683777e115f24cd":
                case "The current mission scene has not finished rendering. Nearby surroundings cannot be captured yet. Try again later.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_NOT_RENDERED", "The current mission scene has not finished rendering. Nearby surroundings cannot be captured yet. Try again later.");
                case "AFICASE5a3d96ea40de08f8":
                case "Panorama capture could not be scheduled.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_SCHEDULE", "Panorama capture could not be scheduled.");
                case "AFICASE3c96f6b6297b0e97":
                case "The panorama renderer was not created.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_RENDERER_MISSING", "The panorama renderer was not created.");
                case "AFICASE000cfa23b3b0805d":
                case "An unfinished export prevented switching the panorama camera.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_EXPORT_ACTIVE", "An unfinished export prevented switching the panorama camera.");
                case "AFICASEd4dff0c6bb32ef53":
                case "The panorama render frame could not be awaited.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_WAIT_FRAME", "The panorama render frame could not be awaited.");
                case "AFICASE6c12ed1795622ddb":
                case "The panorama camera did not finish rendering. No incomplete reference image was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_RENDER_INCOMPLETE", "The panorama camera did not finish rendering. No incomplete reference image was sent.");
                case "AFICASE9537283131352e60":
                case "The panorama export is not ready.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_EXPORT_NOT_READY", "The panorama export is not ready.");
                case "AFICASE11ec79e731ce368f":
                case "Panorama export did not produce a complete image. Repeated viewpoints were not substituted.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_EXPORT_INCOMPLETE", "Panorama export did not produce a complete image. Repeated viewpoints were not substituted.");
                case "AFICASE9bd69992dcc0d932":
                case "Scene state validation scheduling stopped. This illustration cannot continue.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCENE_VALIDATION_SCHEDULER", "Scene state validation scheduling stopped. This illustration cannot continue.");
                case "AFICASE046a2f857098dc27":
                case "Environment panorama capture exceeded its time budget and stopped. No incomplete environment image was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_TIMEOUT", "Environment panorama capture exceeded its time budget and stopped. No incomplete environment image was sent.");
                case "AFICASE9129d4acfad5416f":
                case "The image is empty or exceeds the 24 MiB limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PAYLOAD_SIZE", "The image is empty or exceeds the 24 MiB limit.");
                case "AFICASEa9399e2f0a46fe8d":
                case "Only decodable PNG or JPEG images are accepted.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PAYLOAD_FORMAT", "Only decodable PNG or JPEG images are accepted.");
                case "AFICASEabbc64da09994bae":
                case "The normalized image exceeds the 24 MiB limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_NORMALIZED_SIZE", "The normalized image exceeds the 24 MiB limit.");
                case "AFICASEe8186ab9e492e981":
                case "The two-view reference image exceeds the upload size limit. No reference images were discarded.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PAIR_SIZE", "The two-view reference image exceeds the upload size limit. No reference images were discarded.");
                case "AFICASEf17c8cf08b2c7ee4":
                case "The cached image exceeds the size limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CACHE_SIZE", "The cached image exceeds the size limit.");
                case "AFICASE24df76e570957027":
                case "The image exceeds the 8192-pixel edge or 16-million-pixel limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_IMAGE_DIMENSIONS", "The image exceeds the 8192-pixel edge or 16-million-pixel limit.");
                case "AFICASEdd46ec78f458f1fd":
                case "The server response exceeds the size limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_SIZE", "The server response exceeds the size limit.");
                case "AFICASEbc7a82e77f79206f":
                case "The native capture exceeds the 96 MiB capture limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_SIZE", "The native capture exceeds the 96 MiB capture limit.");
                case "AFICASEfaecb22b638c013b":
                case "The native capture is not a complete, valid BMP.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_BMP_INVALID", "The native capture is not a complete, valid BMP.");
                case "AFICASE47868f8849902e56":
                case "The capture height is invalid.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_HEIGHT", "The capture height is invalid.");
                case "AFICASE08549a13859da2ec":
                case "The native BMP is incomplete or its pixel format is unsupported.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_BMP_INCOMPLETE", "The native BMP is incomplete or its pixel format is unsupported.");
                case "AFICASE28017cb5f03c5e32":
                case "The capture dimensions are invalid or exceed the 24-million-pixel limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_DIMENSIONS", "The capture dimensions are invalid or exceed the 24-million-pixel limit.");
                case "AFICASE16fb0e39da0ac7f9":
                case "The scene capture is almost entirely black; characters and surroundings cannot be identified. Generation stopped. No director or image request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_BLACK", "The scene capture is almost entirely black; characters and surroundings cannot be identified. Generation stopped. No director or image request was sent.");
                case "AFICASE030188035ef5f96a":
                case "The current mission has no single-player frame available for capture.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_MISSION", "The current mission has no single-player frame available for capture.");
                case "AFICASEda1c04128fde447d":
                case "Exit the native photo mode before starting capture from the illustration button.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_PHOTO_MODE", "Exit the native photo mode before starting capture from the illustration button.");
                case "AFICASE3b66687d42ff8dab":
                case "A native image capture is in progress. Wait before clicking again.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_ACTIVE", "A native image capture is in progress. Wait before clicking again.");
                case "AFICASE195cbdc4af5263b4":
                case "The capture pause request is already in use.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_PAUSE_OWNER", "The capture pause request is already in use.");
                case "AFICASEd14a4d68df5d946e":
                case "The scene was left before capture finished. Capture was terminated.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_LEFT_SCENE", "The scene was left before capture finished. Capture was terminated.");
                case "AFICASEa2968d9300e9f708":
                case "Another feature took control of the camera during capture. Capture was terminated.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_CAMERA_OWNER", "Another feature took control of the camera during capture. Capture was terminated.");
                case "AFICASE8f3bdd88a3193eda":
                case "Capture export timed out. The original view was restored; no image request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_TIMEOUT", "Capture export timed out. The original view was restored; no image request was sent.");
                case "AFICASEc7311b67df5333f6":
                case "The conversation or scene for this illustration ended or changed. Reopen the scene illustration.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CONTEXT_CHANGED", "The conversation or scene for this illustration ended or changed. Reopen the scene illustration.");
                case "AFICASEcbcec16045cb873a":
                case "Scene context validation stopped. Reopen the scene illustration.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CONTEXT_STOPPED", "Scene context validation stopped. Reopen the scene illustration.");
                case "AFICASE0a8aba7dd54d70e4":
                case "No mission scene or map conversation is currently displayed. Reopen the scene illustration during a conversation.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CONTEXT_MISSING", "No mission scene or map conversation is currently displayed. Reopen the scene illustration during a conversation.");
                case "AFICASE66121e8376f2ecc5":
                case "The scene panorama exceeds its projection budget. Image generation stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_PROJECTION_BUDGET", "The scene panorama exceeds its projection budget. Image generation stopped.");
                case "AFICASEc65b9fd83e41f029":
                case "The front and rear environment images are nearly identical. Different directions were not confirmed. Generation stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PANORAMA_DUPLICATE", "The front and rear environment images are nearly identical. Different directions were not confirmed. Generation stopped.");
                case "AFICASE86b249622eb8b6a2":
                case "unknown purpose":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REFERENCE_UNKNOWN_KIND", "unknown purpose");
                case "AFICASE77d088ff8786fe8c":
                case "Native Gemini image generation requires the non-streaming :generateContent endpoint. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_NON_STREAMING", "Native Gemini image generation requires the non-streaming :generateContent endpoint. No request was sent.");
                case "AFICASE8f8115f1c6c5d007":
                case "The complete URL option requires /models/MODEL:generateContent. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_EXACT_URL", "The complete URL option requires /models/MODEL:generateContent. No request was sent.");
                case "AFICASE0f13bf8f410e2f04":
                case "Native Gemini image generation requires a model name. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_MODEL_MISSING", "Native Gemini image generation requires a model name. No request was sent.");
                case "AFICASE3bceb09264b09be5":
                case "The native Gemini model name is invalid. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_MODEL_INVALID", "The native Gemini model name is invalid. No request was sent.");
                case "AFICASE4ce6d9401066e872":
                case "The native Gemini image request is missing complete user content. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_USER_CONTENT", "The native Gemini image request is missing complete user content. No request was sent.");
                case "AFICASE20a10394bb25e364":
                case "Native Gemini reference images must be validated PNG data. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_PNG_REFERENCE", "Native Gemini reference images must be validated PNG data. No request was sent.");
                case "AFICASEbcb2153ce82f08e2":
                case "The native Gemini image request contains an unsupported content block. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_CONTENT_BLOCK", "The native Gemini image request contains an unsupported content block. No request was sent.");
                case "AFICASE60459c35f13e804e":
                case "The native Gemini image request is empty. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GEMINI_EMPTY_CONTENT", "The native Gemini image request is empty. No request was sent.");
                case "AFICASE182c62faa16ab067":
                case "The image endpoint must be a complete HTTP/HTTPS URL.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_HTTP_ENDPOINT", "The image endpoint must be a complete HTTP/HTTPS URL.");
                case "AFICASE601d9c6011f90ec6":
                case "The standard image editing endpoint is /images/edits (plural), not /image/edits. Use complete URL mode for special service paths.":
                case "The standard image editing endpoint is /images/edits (plural), not /image/edits. Configure the standard /images/edits endpoint.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_EDITS_ENDPOINT", "The standard image editing endpoint is /images/edits (plural), not /image/edits. Configure the standard /images/edits endpoint.");
                case "AFICASEb2e8aa393cfae238":
                case "Image decoding or format conversion failed":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REFERENCE_DECODE", "Image decoding or format conversion failed");
                case "AFICASEcbc4a27f1d3d5d50":
                case "Image data is empty":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REFERENCE_EMPTY", "Image data is empty");
                case "AFICASE4a72b6d89ffad816":
                case "The data URI format is invalid":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REFERENCE_DATA_URI", "The data URI format is invalid");
                case "AFICASEb04e90bd8966c02e":
                case "The image exceeds the 24 MiB per-image limit":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REFERENCE_SINGLE_LIMIT", "The image exceeds the 24 MiB per-image limit");
                case "AFICASE06cc9b12d34011ff":
                case "Reference images exceed the combined 48 MiB limit":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REFERENCE_TOTAL_LIMIT", "Reference images exceed the combined 48 MiB limit");
                case "AFICASEff334deb0f4bc9ab":
                case "The Player2 interface is unconfirmed. Check the application status later. No image request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PLAYER2_UNCONFIRMED", "The Player2 interface is unconfirmed. Check the application status later. No image request was sent.");
                case "AFICASE320de3434d428aa9":
                case "The Player2 image interface could not be confirmed. Check that the application is running and signed in and that the port is correct. No image request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PLAYER2_UNAVAILABLE", "The Player2 image interface could not be confirmed. Check that the application is running and signed in and that the port is correct. No image request was sent.");
                case "AFICASEa8a0c7c2d4a1840e":
                case "The Player2 payload still exceeds its upload budget. All reference images were retained without further loss of detail. No request was sent. Shorten the prompt or custom rules.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PLAYER2_UPLOAD_BUDGET", "The Player2 payload still exceeds its upload budget. All reference images were retained without further loss of detail. No request was sent. Shorten the prompt or custom rules.");
                case "AFICASEc710e36ee5c20737":
                case "Grok image editing supports at most 5 reference images. No references were discarded and no request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GROK_REFERENCE_LIMIT", "Grok image editing supports at most 5 reference images. No references were discarded and no request was sent.");
                case "AFICASE0afb057e12b2b05a":
                case "No usable reference image is available. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REFERENCE_UNAVAILABLE", "No usable reference image is available. No request was sent.");
                case "AFICASE071fbe5819267d46":
                case "Free-camera generation requires one or two scene captures. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCREENSHOT_COUNT", "Free-camera generation requires one or two scene captures. No request was sent.");
                case "AFICASE4b5743ce4e1b6004":
                case "Scene capture references are missing or have the wrong type. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCREENSHOT_KIND", "Scene capture references are missing or have the wrong type. No request was sent.");
                case "AFICASE089c10d6bd73985c":
                case "The two captures are identical. Capture the scene again. Generation stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_SCREENSHOT_DUPLICATE", "The two captures are identical. Capture the scene again. Generation stopped.");
                case "AFICASEdab6b96ba2bc2420":
                case "Image generation did not return an image.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_IMAGE_NOT_RETURNED", "Image generation did not return an image.");
                case "AFICASE7e0355adf78bfb9e":
                case "The generated image could not be saved to the gallery.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GALLERY_SAVE", "The generated image could not be saved to the gallery.");
                case "AFICASEb9f5a52fdddaa20f":
                case "The generated image could not be saved.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_IMAGE_SAVE", "The generated image could not be saved.");
                case "AFICASEa4805ab773ef9b1e":
                case "The server returned an empty response body":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_EMPTY_BODY", "The server returned an empty response body");
                case "AFICASE77ab8283f295c564":
                case "The response contains no recognizable image data":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_UNRECOGNIZED", "The response contains no recognizable image data");
                case "AFICASE01f18bcf9fa5ed1b":
                case "The server refused image generation or applied content filtering":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_FILTERED", "The server refused image generation or applied content filtering");
                case "AFICASE8d4f171a95c824cd":
                case "The server output reached its length limit; no image was obtained":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_LENGTH_LIMIT", "The server output reached its length limit; no image was obtained");
                case "AFICASEf681e28dc6d3c240":
                case "The server returned an empty reply without text or images":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_EMPTY_REPLY", "The server returned an empty reply without text or images");
                case "AFICASE13f8c940a28375e0":
                case "The server returned content, but no image could be extracted. Check the response format or model output":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_EXTRACTION", "The server returned content, but no image could be extracted. Check the response format or model output");
                case "AFICASE2630c97bdb59fc38":
                case "The encoded image exceeds the limit.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_IMAGE_ENCODING_LIMIT", "The encoded image exceeds the limit.");
                case "AFICASE24e617455b9fad44":
                case "The director is disabled":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_DISABLED", "The director is disabled");
                case "AFICASE20f4234a9700a407":
                case "The director interface is not configured":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_UNCONFIGURED", "The director interface is not configured");
                case "AFICASE4e470f8e02062a49":
                case "The director is disabled or unconfigured. Redrawing with a prompt did not start.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_REDRAW_UNAVAILABLE", "The director is disabled or unconfigured. Redrawing with a prompt did not start.");
                case "AFICASE7f9e5f019afd8af7":
                case "Scene capture generation requires an enabled and configured vision director. Capture did not start.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_CAPTURE_UNAVAILABLE", "Scene capture generation requires an enabled and configured vision director. Capture did not start.");
                case "AFICASE406e133d8493b4e8":
                case "The director did not return a complete visual plan":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_INCOMPLETE", "The director did not return a complete visual plan");
                case "AFICASEa35df81b044a6e8d":
                case "The director output contains unsupported shields, flags, or an unsuitable portrait action":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_UNSUPPORTED_FACTS", "The director output contains unsupported shields, flags, or an unsuitable portrait action");
                case "AFICASEb3f9754519dd6940":
                case "The director repeated the narrative instead of translating it into a visual plan":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_NARRATIVE", "The director repeated the narrative instead of translating it into a visual plan");
                case "AFICASEdfcff5821b1cfde3":
                case "The director returned an empty body":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_EMPTY", "The director returned an empty body");
                case "AFICASEc1913753081d45ba":
                case "The director body lacks complete scene, lighting, or spatial details, or the composition does not meet the requirements":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_SCENE_INCOMPLETE", "The director body lacks complete scene, lighting, or spatial details, or the composition does not meet the requirements");
                case "AFICASEc11cd77a6f725244":
                case "The director output was truncated at the token limit. The incomplete body was not used":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_TRUNCATED", "The director output was truncated at the token limit. The incomplete body was not used");
                case "AFICASE98fdfef079923148":
                case "The director refused or filtered this output":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_FILTERED", "The director refused or filtered this output");
                case "AFICASE6a0ceb2c1689c39c":
                case "The director did not finish the body normally":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_UNFINISHED", "The director did not finish the body normally");
                case "AFICASEaf317e0b68d09d29":
                case "The director request timed out":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_TIMEOUT", "The director request timed out");
                case "AFICASE5244fc23802dd5f6":
                case "The director interface was rate-limited or has insufficient quota (HTTP 429)":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_QUOTA", "The director interface was rate-limited or has insufficient quota (HTTP 429)");
                case "AFICASE5dbd9db503bf3f17":
                case "The encyclopedia's complete equipped portrait could not be captured off-screen. Generation stopped; check failureCode in this request's portrait_stage_result.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PORTRAIT_REFERENCE", "The encyclopedia's complete equipped portrait could not be captured off-screen. Generation stopped; check failureCode in this request's portrait_stage_result.");
                case "AFICASEb85aa6999a9f2750":
                case "The player's complete equipped portrait could not be captured off-screen. Generation stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PLAYER_REFERENCE", "The player's complete equipped portrait could not be captured off-screen. Generation stopped.");
                case "AFICASEe6a5add8fa93bc78":
                case "The other character's complete equipped portrait could not be captured off-screen. Generation stopped.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PARTNER_REFERENCE", "The other character's complete equipped portrait could not be captured off-screen. Generation stopped.");
                case "AFICASE18b670f4b65a7fa0":
                case "The isolated scene capture did not return a complete panorama reference.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PROBE_MISSING", "The isolated scene capture did not return a complete panorama reference.");
                case "AFICASE50f5a67bd7a60fab":
                case "The isolated scene capture returned an empty reference composite.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PROBE_EMPTY", "The isolated scene capture returned an empty reference composite.");
                case "AFICASE440ceae2c62cdf87":
                case "The game-thread task queue is full. Illustration capture could not run.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_QUEUE", "The game-thread task queue is full. Illustration capture could not run.");
                case "AFICASE115753d3609f1b0b":
                case "AI image generation is disabled.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DISABLED", "AI image generation is disabled.");
                case "AFICASE09f096a542bee8f8":
                case "No image API endpoint (Base URL) is configured.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_ENDPOINT_MISSING", "No image API endpoint (Base URL) is configured.");
                case "AFICASE222185ef18d806ef":
                case "No player scene is available for screenshot illustration, or image generation is disabled.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_MISSION_UNAVAILABLE", "No player scene is available for screenshot illustration, or image generation is disabled.");
                case "AFICASE57dc41db19e0f065":
                case "Configure the image API first.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CONFIGURE_API", "Configure the image API first.");
                case "AFICASE7c13f5e61eb17341":
                case "Redrawing from this image requires a valid current image. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REDRAW_SOURCE", "Redrawing from this image requires a valid current image. No request was sent.");
                case "AFICASEc71b263fefa57520":
                case "The images/edits endpoint requires a usable reference image. Enable references and capture a character or scene reference before generating. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_EDITS_REFERENCE", "The images/edits endpoint requires a usable reference image. Enable references and capture a character or scene reference before generating. No request was sent.");
                case "AFICASEcc9ccfb372d02f70":
                case "The exact images/generations endpoint cannot accept reference images. Use /images/edits or enable the conversation multimodal image channel. No request was sent.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GENERATIONS_REFERENCE", "The exact images/generations endpoint cannot accept reference images. Use /images/edits or enable the conversation multimodal image channel. No request was sent.");
                case "AFICASEabec1504a532f44d":
                case "No valid image data could be extracted from the server response.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_IMAGE", "No valid image data could be extracted from the server response.");
                case "AFICASE3c00eaf66c61a225":
                case "The image generation request was cancelled.":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CANCELLED", "The image generation request was cancelled.");
                case "AFICASEcdf7c2522d1c86b7":
                case "AFICASE51e1db2786b4a269":
                case "AFICASE8b24a74c6c0c7319":
                    return IllustratorUiText.WeeklyNotSaved;
                case "AFICASE9bd8c12490b0fd1f":
                    return IllustratorUiText.CardNotSavedOrDisplayed;
            }

            string stoppedSuffix = raw != null && raw.EndsWith(". Image generation stopped.", StringComparison.Ordinal)
                ? ". Image generation stopped." : IllustratorText.Raw("Residual_IllustrationFailureUiText_10fb0edab25d");
            if (raw != null && raw.EndsWith(stoppedSuffix, StringComparison.Ordinal))
                return IllustratorUiText.Format("AF_ILL_ERROR_GENERATION_STOPPED", "{REASON} Image generation stopped.", "REASON",
                    Project(raw.Substring(0, raw.Length - stoppedSuffix.Length), unwrapCommunication: false));

            string noRetry = raw != null && raw.IndexOf("; no automatic retry was made. You may redraw manually later.", StringComparison.Ordinal) >= 0
                ? "; no automatic retry was made. You may redraw manually later." : IllustratorText.Raw("Residual_IllustrationFailureUiText_62852e91bb02");
            string diagnosticPrefix = noRetry.StartsWith(";", StringComparison.Ordinal) ? " Diagnostic: " : IllustratorText.Raw("Residual_IllustrationFailureUiText_a5aa76bc64f3");
            int retryAt = raw?.IndexOf(noRetry, StringComparison.Ordinal) ?? -1;
            if (retryAt >= 0)
            {
                string remaining = raw.Substring(retryAt + noRetry.Length);
                if (remaining.Length == 0 || remaining.StartsWith(diagnosticPrefix, StringComparison.Ordinal))
                {
                    string diagnostic = remaining.Length == 0 ? string.Empty
                        : IllustratorUiText.Format("AF_ILL_ERROR_DIAGNOSTIC", " Diagnostic: {ID}", "ID", remaining.Substring(diagnosticPrefix.Length));
                    return Project(raw.Substring(0, retryAt), unwrapCommunication: false)
                        + IllustratorUiText.Resolve("AF_ILL_ERROR_NO_AUTOMATIC_RETRY", "; no automatic retry was made. You may redraw manually later.") + diagnostic;
                }
            }

            bool englishReference = raw != null && raw.StartsWith("Reference image ", StringComparison.Ordinal);
            string referencePrefix = englishReference ? "Reference image " : IllustratorText.Raw("Residual_IllustrationFailureUiText_e93aea8dae50");
            string referenceSuffix = englishReference ? ". Generation stopped; no request was sent." : IllustratorText.Raw("Residual_IllustrationFailureUiText_8b18ac30f682");
            string kindMarker = englishReference ? " (" : "（";
            string detailMarker = englishReference ? ") is invalid: " : IllustratorText.Raw("Residual_IllustrationFailureUiText_14d94d0e7864");
            if (raw != null && raw.StartsWith(referencePrefix, StringComparison.Ordinal) && raw.EndsWith(referenceSuffix, StringComparison.Ordinal))
            {
                int kindAt = raw.IndexOf(kindMarker, referencePrefix.Length, StringComparison.Ordinal);
                int detailAt = kindAt < 0 ? -1 : raw.IndexOf(detailMarker, kindAt + kindMarker.Length, StringComparison.Ordinal);
                if (detailAt >= 0)
                {
                    string index = raw.Substring(referencePrefix.Length, kindAt - referencePrefix.Length);
                    if (IsBudget(index))
                        return IllustratorUiText.Format3("AF_ILL_ERROR_REFERENCE_INVALID", "Reference image {INDEX} ({KIND}) is invalid: {DETAIL}. Generation stopped; no request was sent.",
                            "INDEX", index, "KIND", Project(raw.Substring(kindAt + kindMarker.Length, detailAt - kindAt - kindMarker.Length), unwrapCommunication: false),
                            "DETAIL", Project(raw.Substring(detailAt + detailMarker.Length, raw.Length - referenceSuffix.Length - detailAt - detailMarker.Length), unwrapCommunication: false));
                }
            }

            string freeDirectorPrefix = raw != null && raw.StartsWith("Free-camera generation requires the vision director to plan the scene: ", StringComparison.Ordinal)
                ? "Free-camera generation requires the vision director to plan the scene: " : IllustratorText.Raw("Residual_IllustrationFailureUiText_8a4cca3198db");
            if (raw != null && raw.StartsWith(freeDirectorPrefix, StringComparison.Ordinal))
                return IllustratorUiText.Format("AF_ILL_ERROR_FREE_CAMERA_DIRECTOR", "Free-camera generation requires the vision director to plan the scene: {DETAIL}", "DETAIL", Project(raw.Substring(freeDirectorPrefix.Length), unwrapCommunication: false));
            string redrawDirectorPrefix = raw != null && raw.StartsWith("Redrawing with a prompt cannot use a local composition: ", StringComparison.Ordinal)
                ? "Redrawing with a prompt cannot use a local composition: " : IllustratorText.Raw("Residual_IllustrationFailureUiText_c26cedbef949");
            if (raw != null && raw.StartsWith(redrawDirectorPrefix, StringComparison.Ordinal))
                return IllustratorUiText.Format("AF_ILL_ERROR_REDRAW_DIRECTOR", "Redrawing with a prompt cannot use a local composition: {DETAIL}", "DETAIL", Project(raw.Substring(redrawDirectorPrefix.Length), unwrapCommunication: false));

            string closed = ProjectClosed(raw, "Observation lighting requires a valid isolated scene copy.", "", "AF_ILL_ERROR_LIGHTING_SCENE_DETAIL", "Observation lighting requires a valid isolated scene copy.{DETAIL}", numeric: false)
                ?? ProjectClosed(raw, "Observation lighting has no off-screen camera position.", "", "AF_ILL_ERROR_LIGHTING_POSITION_DETAIL", "Observation lighting has no off-screen camera position.{DETAIL}", numeric: false)
                ?? ProjectClosed(raw, "The current scene XML resource could not be parsed: ", "", "AF_ILL_ERROR_RESOURCE_XML_PARSE", "The current scene XML resource could not be parsed: {DETAIL}", numeric: false)
                ?? ProjectClosed(raw, "Reading the current scene resource failed: ", "", "AF_ILL_ERROR_RESOURCE_READ_FAILED", "Reading the current scene resource failed: {DETAIL}", numeric: false)
                ?? ProjectClosed(raw, "The scene resource contains duplicate ", " nodes.", "AF_ILL_ERROR_RESOURCE_NODE_DUPLICATE", "The scene resource contains duplicate {DETAIL} nodes.", numeric: false)
                ?? ProjectClosed(raw, "The director request failed (", ")", "AF_ILL_ERROR_DIRECTOR_REQUEST_FAILED", "The director request failed ({DETAIL})", numeric: false)
                ?? ProjectClosed(raw, "The director request or response parsing failed (", ")", "AF_ILL_ERROR_DIRECTOR_PARSE_FAILED", "The director request or response parsing failed ({DETAIL})", numeric: false)
                ?? ProjectClosed(raw, "The director service denied access from the current region (HTTP ", ")", "AF_ILL_ERROR_DIRECTOR_REGION", "The director service denied access from the current region (HTTP {DETAIL})", numeric: true)
                ?? ProjectClosed(raw, "Director authentication or access failed (HTTP ", ")", "AF_ILL_ERROR_DIRECTOR_ACCESS", "Director authentication or access failed (HTTP {DETAIL})", numeric: true)
                ?? ProjectClosed(raw, "The upstream director service is temporarily unavailable (HTTP ", ")", "AF_ILL_ERROR_DIRECTOR_UPSTREAM", "The upstream director service is temporarily unavailable (HTTP {DETAIL})", numeric: true)
                ?? ProjectClosed(raw, "The director interface returned HTTP ", "", "AF_ILL_ERROR_DIRECTOR_HTTP", "The director interface returned HTTP {DETAIL}", numeric: true)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_1c0e9b578437"), "", "AF_ILL_ERROR_LIGHTING_SCENE_DETAIL", "Observation lighting requires a valid isolated scene copy.{DETAIL}", numeric: false)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_573323997754"), "", "AF_ILL_ERROR_LIGHTING_POSITION_DETAIL", "Observation lighting has no off-screen camera position.{DETAIL}", numeric: false)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_8e2b0b98d7d9"), "", "AF_ILL_ERROR_RESOURCE_XML_PARSE", "The current scene XML resource could not be parsed: {DETAIL}", numeric: false)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_ace9d5aabf3b"), "", "AF_ILL_ERROR_RESOURCE_READ_FAILED", "Reading the current scene resource failed: {DETAIL}", numeric: false)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_d5c89906c943"), IllustratorText.Raw("Residual_IllustrationFailureUiText_d3ff0ff3e45a"), "AF_ILL_ERROR_RESOURCE_NODE_DUPLICATE", "The scene resource contains duplicate {DETAIL} nodes.", numeric: false)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_1644f2a79384"), "）", "AF_ILL_ERROR_DIRECTOR_REQUEST_FAILED", "The director request failed ({DETAIL})", numeric: false)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_67c485fb3daa"), "）", "AF_ILL_ERROR_DIRECTOR_PARSE_FAILED", "The director request or response parsing failed ({DETAIL})", numeric: false)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_3ad913de5b81"), "）", "AF_ILL_ERROR_DIRECTOR_REGION", "The director service denied access from the current region (HTTP {DETAIL})", numeric: true)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_8157b0d4d53f"), "）", "AF_ILL_ERROR_DIRECTOR_ACCESS", "Director authentication or access failed (HTTP {DETAIL})", numeric: true)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_cf649febb7a1"), "）", "AF_ILL_ERROR_DIRECTOR_UPSTREAM", "The upstream director service is temporarily unavailable (HTTP {DETAIL})", numeric: true)
                ?? ProjectClosed(raw, IllustratorText.Raw("Residual_IllustrationFailureUiText_03733f6d6083"), "", "AF_ILL_ERROR_DIRECTOR_HTTP", "The director interface returned HTTP {DETAIL}", numeric: true);
            if (closed != null) return closed;

            string textOnlyPrefix = raw != null && raw.StartsWith("The interface explicitly does not support image input; one text-only conception attempt was made; ", StringComparison.Ordinal)
                ? "The interface explicitly does not support image input; one text-only conception attempt was made; " : IllustratorText.Raw("Residual_IllustrationFailureUiText_70d8d8233b83");
            if (raw != null && raw.StartsWith(textOnlyPrefix, StringComparison.Ordinal))
                return IllustratorUiText.Format2("AF_ILL_ERROR_DIRECTOR_TEXT_ONLY_DETAIL", "{SUMMARY}; {DETAIL}",
                    "SUMMARY", IllustratorUiText.Resolve("AF_ILL_ERROR_DIRECTOR_TEXT_ONLY_ATTEMPT", "The interface explicitly does not support image input; one text-only conception attempt was made"),
                    "DETAIL", Project(raw.Substring(textOnlyPrefix.Length), unwrapCommunication: false));

            bool englishTimeout = raw != null && raw.StartsWith("The image generation request exceeded ", StringComparison.Ordinal);
            string timeoutPrefix = englishTimeout ? "The image generation request exceeded " : IllustratorText.Raw("Residual_IllustrationFailureUiText_fb5df795345a");
            string timeoutSuffix = englishTimeout ? " seconds total budget" : IllustratorText.Raw("Residual_IllustrationFailureUiText_5088eaab3391");
            if (raw != null && raw.StartsWith(timeoutPrefix, StringComparison.Ordinal)
                && raw.EndsWith(timeoutSuffix, StringComparison.Ordinal))
            {
                string seconds = raw.Substring(timeoutPrefix.Length, raw.Length - timeoutPrefix.Length - timeoutSuffix.Length);
                if (IsBudget(seconds))
                    return IllustratorUiText.Format("AF_ILL_ERROR_TIMEOUT", "The image generation request exceeded its total budget of {SECONDS} seconds.", "SECONDS", seconds);
            }
            string communicationPrefix = raw != null && raw.StartsWith("Image generation communication error: ", StringComparison.Ordinal)
                ? "Image generation communication error: " : IllustratorText.Raw("Residual_IllustrationFailureUiText_a2749247797c");
            if (unwrapCommunication && raw != null && raw.StartsWith(communicationPrefix, StringComparison.Ordinal))
                return IllustratorUiText.Format("AF_ILL_ERROR_COMMUNICATION", "Image generation communication error: {DETAIL}", "DETAIL", Project(raw.Substring(communicationPrefix.Length), unwrapCommunication: false));
            return raw;
        }

        private static string ProjectClosed(string raw, string prefix, string suffix, string id, string fallback, bool numeric)
        {
            if (raw == null || !raw.StartsWith(prefix, StringComparison.Ordinal) || !raw.EndsWith(suffix, StringComparison.Ordinal)
                || raw.Length < prefix.Length + suffix.Length) return null;
            string detail = raw.Substring(prefix.Length, raw.Length - prefix.Length - suffix.Length);
            if (numeric && !IsBudget(detail)) return null;
            return IllustratorUiText.Format(id, fallback, "DETAIL", detail);
        }

        private static bool IsBudget(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < value.Length; i++)
                if (value[i] < '0' || value[i] > '9') return false;
            return true;
        }
    }
}
