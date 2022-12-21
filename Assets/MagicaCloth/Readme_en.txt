//------------------------------------------------------------------------------
// Magica Cloth
// Copyright (c) Magica Soft, 2020-2021
// https://magicasoft.jp
//------------------------------------------------------------------------------

### About
Magica Cloth is a high-speed cloth simulation operated by Unity Job System + Burst compiler.


### Support Unity versions
Unity2019.4.31(LTS) or higher


### Feature

* Fast cloth simulation with Unity Job System + Burst compiler
* Works on all platforms except WebGL
* Implement BoneCloth driven by Bone (Transform) and MeshCloth driven by mesh
* MeshCloth can also work with skinning mesh
* Easy setup with an intuitive interface
* Time operation such as slow is possible
* With full source code


### Documentation
Since it is an online manual, please refer to the following URL for details.
https://magicasoft.jp/en/magica-cloth-install-2/

First, import the package according to the [Installation Guide].
Then read the [System Overview] and proceed with the [Setup Guide] for a better understanding.


### Release Notes
[v1.12.11]
Fixed: Fixed an issue that caused an error with the Collections 2.x package.

[v1.12.10]
Fixed: Fixed an issue that could cause malfunctions in calculating vertex normals when using many MeshCloths.

[v1.12.9]
Fixed: Fixed an issue in RestoreRotation in Algorithm 2 that caused an error when two particles overlapped at exactly the same coordinates.
Fixed: Fixed an issue where external forces such as wind would become weaker at lower framerates.

[v1.12.8]
Fixed: Fixed an issue that caused an error when removing a cloth component after adding a collider. This is a bug introduced in v1.12.7.

[v1.12.7]
Fixed: Fixed an error caused by turning off / on the cloth component active after removing the collider.

[v1.12.6]
Fixed: Fixed an issue that caused an error in Collections Package 1.3.0 and later.

[v1.12.5]
Note: MagicaCloth is now available in the 2D Animation Package.
Added: Added an option to the MagicaPhysicsManager component to change the location of simulation updates.
Normally, the simulation is run after the Late Update, but you can select Before Late Update to run it before the Late Update.
This is required when using 2D Animation Package etc.
Added: Added API to change Spring Power.
Added: Added API to get a list of all registered MagicaCloth components.
Improvement: In the Spring parameter inspector, numbers that cannot be changed during execution are grayed out and cannot be manipulated.

[v1.12.4]
Note: The fast mesh write option [Faster Write] is turned off by default.
This is due to problems such as the mesh breaking depending on the environment.
We are currently investigating this matter.
Fixed: Fixed an issue where the mesh would break at run time if there was a difference between the SkinnedMeshRenderer scale and the rootBone scale when creating the RenderDeformer.
However, to solve this problem, you need to reconstruct the data by pressing the [Create] button of the corresponding Render Deformer.

[v1.12.3]
Fixed: Fixed an issue where the mesh Cloth vertices would collapse when the [Faster Write] option was enabled in Unity 2021.2 or later + Meta Quest 2 environment.

[v1.12.2]
Added: Added support for writing mesh vertex data using Unity's new Mesh API.
This is available in the MagicaPhysicsManager's Faster Write option (default is ON)
By using this function, the speed of writing data to the mesh will be significantly increased.
However, this feature is only available on Unity 2021.2 and above.
Added: Added an API to get all the Transforms used by the component.
Added: Added an API that allows component Transform substitutions to be specified in the <Name, Transform> dictionary.
Fixed: Fixed an issue where the clone mesh was not properly freed memory when destroying MagicaRenderDeformer.
Improvement: Changed the coordinate base of the skinning mesh from the renderer Transform to the SkinnedMeshRenderer RootBone.

[v1.12.1]
Fixed: Fixed an issue where collision detection became unstable when the global collider was moved from its initial position. This is a problem that only occurred in v1.12.0.

[v1.12.0]
Note: Unity 2018 is no longer supported. From this time, it will be supported after Unity 2019.4.31.
Added: A Magica Area Wind component has been added that allows you to specify the wind zone.
Added: Added Frequency parameter to the wind component to adjust the rate of change of turbulence.
Added: Added Wind Synchronization parameter to the External Force panel to adjust the wind synchronization rate.
Added: Added the Depth Influence parameter to the External Force panel to set the influence of external forces according to the depth of the particles.
Instead, the Mass Influence parameter is obsolete.
Added: Wind calculation has been changed to calculate per particle instead of per component.
Added: The cloth monitor now shows the active count of winds.
Added: Added [Frequency] to the Wind sample scene.
Added: Added a Blast Wave button for blast testing to the Wind sample scene.
Added: ExternalForce_MassInfluence () has been removed from the API and replaced with ExternalForce_DepthInfluence ().
Fixed: Fixed an issue where the mesh would disappear if two particles overlap at the same position while using the Triangle Bend.
Fixed: Fixed an issue that caused an error in some situations when enabling / turning Enable in Physics Manager.
Fixed: Fixed PlayerLoopSystem's subSystemList to work even if it becomes null when updating PhysicsManager.

[v1.11.2]
Added: A menu has been added to create components externally.
It can be started from Tools / Magica Cloth / Build Menu.
From this menu you can batch process component creation and algorithm upgrades.
Added: Removed the UseAnimatedPose flag and added SkinningMode instead.
By setting Skinning Mode to "User Animation", the distance and angle will be calculated according to the posture changed after the animation.
If the animation changes the shape of the cloth, you can get good results by enabling this option.
This "User Animation" is ON by default.
Added: Added support for Unity 2021.2 and later.
Fix: Fixed an issue that caused an error while displaying gizmo.
Improvement: The component creation process can now be called externally.
See BuildManager.cs in the Scripts / Editor / Build folder.
Improvement: The operation of Clamp Rotation of Algorithm 2 is more stable.
Improvement: Changed the display of "Base Pose" in Cloth Monitor from sphere to mesh shape.
Improvement: Some presets have been readjusted.
Improvement: Colorized some logs.

[v1.11.1]
Added: An error message is displayed for meshes that are set to "Keep Quads" in the model importer.
The "Keep Quads" mesh does not work properly.
Added: Added API to set "Clamp Position".
Added: RenderDeformer now has an option to extend the mesh's bounding box.
This option can be used to solve the problem that the mesh is present but not drawn to the camera.
Fix: Fixed vibration issue when update mode is "Unity Physics".

[v1.11.0]
Added: A new computational algorithm (Algorithm 2) has been added.
In Algorithm2, the processing of Clamp Rotation / Restore Rotation / Triangle Bend has been redesigned to achieve more stable operation with significantly reduced vibration problems.
When using Algorithm2, it can be set from the new [Algorithm] panel.
It is also possible to convert parameters from the old algorithm to the new algorithm.
The parameters hold both old and new data separately, so you can easily revert to the old algorithm.
The old and new algorithms can coexist, but the old algorithm will be abolished in the future, so please move to Algorithm 2 as much as possible.
Added: Added "Twist Correction" check to Triangle Bend to correct twist problems.
If you turn this check on and create data, the problem of twisting will be greatly improved.
Added: The data format has changed. A warning will be displayed for older data formats.
Older data formats can be used for some time, but may be deprecated in the future.
Fix: Fixed an issue where particle rotation values ​​would not be restored in MeshCloth where lines and triangles are compounded.
Fix: Fixed an issue where Virtual Deformer's vertex reduction process caused incorrect vertices that were not concatenated anywhere.
Improvement: All preset data has been updated with the implementation of Algorithm 2.
Improvement: All sample scenes have been updated with the implementation of Algorithm 2.

[v1.10.3]
Added: Added "Mesh Sequential Loop" and "Mesh Sequential No Loop" to BoneCloth's Mesh connection method. Generate meshes according to the order of Transforms registered in RootList.
This allows for more flexible support than traditional Mesh connections.
Added: Added "Use Animated Distance" option to determine the restore distance from the current animated pose instead of the initial pose when performing Restore Distance and Clamp Distance.
This can improve the problem that the cloth shrinks unintentionally when the cloth expands significantly from the initial posture due to animation.
Fix: Addressed an error with the Collections 1.0.0 package.
Improvement: Changed the initial value of the number of vertex weights of VirtualDeformer from 3 to 4.

[v1.10.2]
Added: Added a menu to remove unwanted sub-assets left in the prefab by right-clicking in ProjectView and selecting "Magica Cloth/Clean up sub-assets".
Fix: Fixed an issue where turning the cloth component on and off using DistanceDisable would break the position of the bones in some situations.
Fix: Fixed the problem that Blend rate is always 0 when FadeDistance of DistanceDisable is 0.
Fix: Fixed an issue where the manager component would disappear if the manager was accessed before the PhysicsManager's Awake() was called.
Improvement: Adjusted the skirt of the UnityChan sample.
Improvement: Adjusted the initial value of VelocityInfluence of ClampDistance to about 0.2.

[v1.10.1]
Improvement: Cloth gizmos are now only shown when Calculation is ON
Fix: When using the culling system together with DistanceDisable, the component was not enabled again.

[v1.10.0]
Added: Camera culling system added. It improves performance by stopping the simulation of characters that are not on screen.

[v1.9.5]
Added: API for changing collider parameters.
Improvement: Changed the maximum radius of particles from 0.1 to 0.3.
Improvement: Cleaned up useless internal processing. Slightly improved processing speed.
Fix: Fixed an issue where subassets in the prefab would multiply depending on the situation.

[v1.9.4]
Improvement: The problem of vibration caused by static friction has been improved.
Fix: Fixed various issues that were occurring in Unity Physics mode.
Fix: From v1.9.2, fixed the problem that the collision particles were vibrating strongly depending on the situation.

[v1.9.3]
Improvement: The initial value of static friction was too strong, so it was reduced to 0.03.
Fix: Fixed an issue that caused an error in the Mathematics 1.1.0 package.
Fix: Fixed an issue where deferred execution future prediction was not working properly.
Fix: Fixed an issue that caused an error when registering the same collider twice with AddCollider ().
Fix: BlendWeight is internally clamped to 0-1.

[v1.9.2]
Added: Added static friction. You can improve the problem of particles slipping on the collider.
Improvement: Improved friction calculation.
Improvement: Adjusted the initial value of each component
Improvement: Adjusted each preset
Fix: Fixed an issue where fixed particles were misaligned with the previous frame. Vibration during movement is improved.
Fix: Fixed an issue where MeshCloth polygons would disappear in rare situations.

[v1.9.1]
Added: Added OnPreUpdate / OnPostUpdate events to PhysicsManager.
Added: Added [Velocity Limit] parameter to Clamp Rotation.
Clamp Rotation is stabilized by limiting the speed according to the moving speed.
This can be set back to v1.9.0 or earlier by setting it to 0.0.
Improvement: Fixed the problem that the angle limitation by Clamp Rotation is invalidated when a strong force is applied. [Velocity Limit] parameter.
Fix: Fixed an issue where the Surface Penetration debug line was not displayed during execution.

[v1.9.0]
Added: Added support for Assembly Definition (asmdef). As a result, the folder structure has changed significantly.
Note: If you are already using MagicaCloth, please delete the old MagicaCloth folder and do a clean install.

[v1.8.8]
Added: You can now change the direction of gravity.
Improvement: The Collision [Keep Shape] option has been removed due to its many disadvantages.
Fix: Fixed an issue where RenderDeformer would not display an error if there was a difference between the internal data and the current mesh data.

[v1.8.7]
Added: Added a mode to sync with Unity Physics for cloth-components.
Added: Added API to change cloth-component update mode.
Improvement: Changed the maximum connection distance of Virtual Deformer from 0.1 to 0.3.
Fix: Fixed an issue where data might not be saved correctly when saving a prefab.

[v1.8.6]
Added: Added API to switch Influence Target.
Improvement: Turned off the Breast preset limit axis
Improvement: Changed to perform mesh shaping process per vertex instead of per mesh when the number of meshes is small.
Improvement: Improved the reduction algorithm of Virtual Deformer.
Fix: Fixed a bug where the global collider would cause an error in some situations and the mesh would not be drawn.
Fix: Fixed an issue where setting TimeScale = 0 would cause an error in some situations
Fix: Fixed an issue where data would be saved in the wrong prefab if the prefab had a parent-child relationship
Fix: Fixed an issue where particles would sway after teleporting when Keep Teleporting during rotation
Fix: Fixed an issue where the simulation would not run on the second boot when ReloadDomain in Enter Play Mode Settings was turned off.

[v1.8.5]
Added: Added a mode to teleport while preserving the simulation.
Added: Added Keep Teleport API.
Improvement: Reduced vibration caused by collision detection.
Improvement: Improved future prediction algorithms to reduce simulation vibrations.
Fix: Fixed an issue where the mesh disappeared due to an error when using Global Collider.
Fix: Fixed an issue that caused an error at runtime when copying / pasting component data in the editor.

[v1.8.4]
Added: Added DynamicBone style presets.
Added: Added a button in the Parameter Inspector that allows you to set presets with a single click.
Improvement: MeshCloth performance has been improved in Unity 2019.4 and above.

[v1.8.3]
Improvement: Fixed several issues caused by collision detection. This has greatly improved collision detection accuracy, especially for moving characters.
Fix: Fixed an issue that caused a data access error at the end of execution.
Fix: Fixed an issue that caused vibrations in the collision detection of moving colliders due to a miscalculation.
Fix: Fixed an issue where collision detection was not accurate when the drawing frame rate was higher than in physics simulation.
Fix: Fixed an issue that caused a malfunction due to a calculation error in collision detection during delayed execution.

[v1.8.2]
Added: Added API for Penetration's [MovingRadius] parameter.
Fix: Fixed an issue that could cause an error when adding a collider with AddCollider() during execution.
Fix: Fixed an issue where colliders added during the run were not removed correctly.
Fix: Fixed an issue that caused code warnings in Unity 2020.
Fix: Fixed an issue that caused an error when using MAGICACLOTH_ECS Define in Unity 2018.

[v1.8.1]
Improvement: BoneCloth's Mesh connection has been changed to create Rotation Line from the Transform hierarchy.
Improvement: Tweaked collision detection.
Fix: Fixed an issue that caused an error when the timescale was 0.
Fix: Fixed an issue where initialization would fail if multiple components were attached to a GameObject.
Fix: Fixed an issue that caused an error when disabled while running Penetration's connection collider.

[v1.8.0]
Note: The data format has changed. Since the new function cannot be used with the past data, it is necessary to press the [Create] button again to recreate the data.
Added: Added BoneCloth mesh connection method. This will automatically create a mesh from the bone connections. The movement of the skirt by the bone is improved.
Added: It is now possible to turn on/off the entire cloth simulation by operating the enable of MagicaPhysicsManager.
Improvement: Adjusted some preset parameters.
Improvement: Improved collision detection.
Improvement: The operation of Penetration has been improved.
Fix: Fixed the problem at the time of negative scale (flip) of Penetration.

[v1.7.6]
Added: Added support for character minus scale (flip). However, only one xyz axis can be reversed.
Added: Added negative scale sample scene.
Improvement: The [Once per Frame] update option is now left forever.
Improvement: Changed the minimum value of particle radius from 0.01 to 0.001.
Fix: Fixed the problem that the normal/tangent recalculation of MeshSpring was not done correctly. Affects lighting.
Fix: Fixed the problem that an error occurs when the execution is stopped in the editor.

[v1.7.5]
Added: Added maximum speed setting to [World Influence]. The simulation is stable when moving at high speed.
Added: Added a parameter [Stabilization Time After Reset] that improves the problem of particles bouncing on reset.
Improvement: Reduced particle vibration due to collision detection.
Improvement: The accuracy of collision detection has been slightly improved.
Fix: Fixed issue where global colliders would bounce away particles when moved/rotated at high speed.
Fix: Fixed the problem that a bone grows when moving at high speed.
Fix: It was fixed because there was a mistake in the interpolation processing of fixed particles.
Fix: Fixed that the bar did not turn yellow even if some parameters of spring setting were changed.
Fix: Fixed the issue that an error occurs in RenderDeformer when there is no tangent data in the mesh.
Fix: Fixed the problem that it doesn't work when used together with Cubism SDK (Live2D). (However, Unity 2019.3 and above)

[v1.7.4]
Added: Added [Radius][Drag][Gravity][Mass] parameter API.
Improvement: An error is displayed when the number of vertices of the mesh exceeds 65535. This is an existing limitation.
Fix: Fixed the problem that an error occurs in Unity2019.1-2019.2.13 due to scaling processing.

[v1.7.3]
Added: You can now add colliders to the cloth component at runtime.
Added: Added option to take over Avatar collider when connecting Avatar Parts to Avatar.
Added: You can now set the offset position on the Collider component.
Added: Added OnAttachParts and OnDetachParts events to Magica Avatar.
Improvement: Animation control of BlendWeight is now possible.
Improvement: [Adjust Rotation] of Spring component is always enabled and changed to the method to set the behavior according to the mode.
Improvement: Fixed the problem that gizmo of MeshSpring and Collider was displayed even in unnecessary situations.
Improvement: The control of PlayerLoop is exposed as an external function.
Fix: Fixed an issue where mesh vertices would occasionally collapse when swapping multiple cloth components.
Fix: Fixed the problem that a GUI error occurs when loading a preset in Unity 2019.4.
Fix: Fixed the problem that an error occurs when a mesh with vertex weights is put into MeshRenderer and MeshCloth is set.

[v1.7.2]
Added: Added support for scaling at runtime.
Improvement: The color of the parameter bar of the inspector will change to yellow when the data needs to be reconstructed due to the parameter change.
Fix: Fixed an issue that caused an error due to the initialization order of Cloth components.
Fix: Fixed an issue where the Influence Target would not switch when attaching AvatarParts.
Fix: Fixed an issue that sometimes caused an error when deleting a Prefab in Project.

[v1.7.1]
Added: Added access function to [World Influence] [Collider Collision] [Penetration] parameter to API.
Improvement: When data has not been created yet, instead of an error in the information, it now shows the state that there is no data.
FIX: Fixed the problem that an error occurs when the collection package 0.9.0 or more is included.
FIX: Fixed the error that occurs when the [ReadOnly] attribute is custom defined.

[v1.7.0]
Note: The data format has changed and the old data no longer starts. You need to press the [Create] button again to recreate the data.
Added: Added Surface Penetration function.
Added: Added Collider Penetration function.
Added: It can be used together with Entity Component System.In Unity2018.4 / Unity2019.2, you need to set the MAGICACLOTH_ECS define in your project.
Added: It is now possible to display vertex / particle axes (XYZ) from the cloth monitor.
Improvement: The collision detection has been improved.
Improvement: The project setting of Unsafe Code is no longer required.
Improvement: Depth display of cloth monitor now shows the current value.
Improvement: Fixed the problem that the simulation becomes unstable when the weight of the movable bone is already included in the mesh.
Fix: Fixed the spelling mistake of [Create] button of MeshCloth.
Fix: Fixed the problem which falls into an infinite loop at the time of data creation of Virtual Deformer.

[v1.6.1]
Improvement: Improved the friction processing algorithm. The problem of particles vibrating has been reduced.
Fix: Fixed a problem that vertex painting could not be performed properly when there are two or more inspector windows in the editor.
Fix: Fixed an issue that caused an error at the end of execution in the editor.

[v1.6.0]
Improvement: Improved the behavior of ClampPosition / ClampRotation. Collision detection has priority over movement restriction.
Improvement: Improved collision determination processing.
Improvement: Improved the rotation line generation algorithm.
Improvement: Collider Gizmo is basically hidden when not selected.
Improvement: Added a reset simulation button to the running inspector.
Improvement: Improved the friction processing algorithm.
Improvement: Enabled to specify the maximum number of connections in [Near Point] of Restore Distance.
Improvement: Changed the Virtual Deformer weight calculation method to the average weight value of the referenced skinning mesh vertices.
This greatly reduces the problem of unintended vertex deformation during animation.
Improvement: Fixed vertices in VirtualDeformer were set to be completely excluded from the calculation in some situations.
This greatly reduces the problem of unintended vertex deformation during animation.
Improvement: Renamed Adjust Line to Rotation Interpolation.
Improvement: Added FixedNonRotation flag to Rotation Interpolation.
If this flag is set to ON, fixed particles will not rotate at all.
Fix: Fixed an issue where Global Collider was not working properly.

[v1.5.1]
Added: Added API for accessing [Distance Disable] parameter.
Added: Added API for accessing [External Force] parameter.
Improvement: Connection control between components has been strengthened.
Fix: Fixed an issue where an error would occur if [Distance Disable] was turned On / Off during execution.
Fix: Fixed an issue that caused an error when creating a cloth component with LateUpdate during delayed execution.
Fix: Changed the delayed execution to be executed at PostLateUpdate instead of at the end of LateUpdate.
Fix: Fixed an issue where inactive render deformers were being calculated.
Fix: Fixed an issue where inactive render deformers were causing a memory leak.
Fix: Fixed [RenderMeshVertexUsed] [VirtualMeshVertexUsed] values on cloth monitor to be correct.

[v1.5.0]
Added: Added delayed execution mode.
Improvement: Improved performance.
Improvement: Displaying write time to mesh in profiler.
Improvement: Render deformer normal / tangent recalculation can now select normal only or normal + tangent.
Improvement: Scene view can be rotated by Alt + mouse drag while vertex painting.
Fix: Fixed incorrect scale calculation when writing to bones.
Fix: Fixed an issue where references to parent bones could be lost when referring to one bone multiple times.
Fix: Fixed an issue where teleport might not work properly.
Fix: Fixed an issue when selecting the wind component when the cloth monitor was hidden.
Fix: Fixed an issue where data was not written correctly when editing in prefab mode.
Fix: Modified to redraw the scene view when editing MeshSpring axes in the inspector.
Note: The update mode [Once Per Frame] will be deprecated in the future.

[v1.4.2]
Improvement: When creating a collider, it has been changed to adjust the collider scale from the parent scale.
Fix: Fixed a problem where mesh was broken when SkinnedMeshRenderer and MeshRenderer were mixed using Unity2018.
Fix: Fixed Capsule Collider gizmo not displaying correctly.
Fix: Fixed an issue where cloth simulation was not running on frames with cloth components attached.

[v1.4.0]
Added: Added dress-up system (Avatar, AvatarParts).
Added: Teleport is turned off by default.
Improvement: Reduced vibration caused by movement.
Improvement: When creating a cloth component object, it is set to inherit the parent name.
Fix: Fixed issue where MeshOptimizeMissmatch error would occur when loading from asset bundle.
Fix: Fixed an issue where the scene view was not redrawn when painting vertices.
Fix: Fixed an issue where writing transforms was not correct when adding / removing cloth components repeatedly.
Fix: Fixed collider to correctly reflect transform scale.
Fix: Fixed an issue where an error would occur if the main camera did not exist.
Fix: Fixed an issue where data was not created when attaching a RenderDeformer with multiple renderers selected.

[v1.3.0]
Added: Added wind function (Wind).
Added: Added wind sample scene (WindSample).
Improvement: Changed cloth team preprocessing from C # to JobSystem.

[v1.2.0]
Added: Added blending function with original posture (Blend Weight).
Added: Added the function to disable simulation by distance (Distance Disable).
Added: Added a sample scene for distance disable function (DistanceDisableSample).
Improvement: Added scrollbar to cloth monitor.
Improvement: Data can be created even if the mesh has no UV value.
Improvement: Enhanced error handling.
Fix: Fixed slow playback bug. Time.timeScale works correctly.
Fix: Fixed an issue where an error occurred when duplicating a prefab with [Ctrl+D].
Fix: Fixed an issue where trying to create data without vertex painting would result in an error.

[v1.1.0]
Added: Added support for Unity2018.4 (LTS).
Improvement: Error details are now displayed along with error codes.
Improvement: Vertex paint now records by vertex hash instead of vertex index.
Fix: If two or more MagicaPhysicsManagers are found, delete those found later.

[v1.0.3]
Fix: Fixed the problem that reference to data is lost while editing in Unity2019.3.0.

[v1.0.2]
Fix: Fixed an issue where an error occurred when running in the Mac editor environment.

[v1.0.1]
Fix: Fixed an error when writing a prefab in Unity2019.3.

[v1.0.0]
Note: first release.




