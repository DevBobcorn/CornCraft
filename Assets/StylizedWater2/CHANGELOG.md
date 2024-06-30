1.6.7

Fixed:
- Enviro fog shading not having any effect since version 3.1.3 due to a code change

1.6.6

Fixed:
- Workaround for shader error on Mac in Unity 2022.3.15+, regarding "_FOVEATED_RENDERING_NON_UNIFORM_RASTER" (known bug: UUM-67560)

1.6.5

Added:
- "Sample Water Normal" sub-graph, allows other shaders to read out the water surface normal

Fixed:
- Distance fading for waves not behaving correctly if an origin shifting system was in use (WaterObject.PositionOffset)
- Displacement Pre-pass unnecessarily also calculating information per-pixel

1.6.4

Changed:
- Implemented proper error handling for Unity 6.
- "Murky" water material

Fixed:
- Planar Reflections, minute changes in render scale not having an effect on resolution.
- Buoyancy API not respecting custom time value if it was exactly 0.
- Indirect lighting and reflections being black in demo scenes in Unity 2023.2+.
- Translucency for point lights being visible on backfaces (underwater scenario).

1.6.3

Added:
- Checkbox on material to enable Dynamic Effects (enabled by default).

Changed:
- Simulation space of particle effects is now World-Space by default
- Water Grid 'columns' parameter now allows for a 0 value. This covers the use case of using the component with custom geometry.
- Water Mesh creation is no longer limited to 65536 vertices.

Fixed:
- Water Grid, wireframe display not accurately representing the geometry if the Transform was scaled.
- Caustics effect not appearing correctly when the "Disable Depth Texture" option was enabled.
- Shader compile error on MacOS when using 2022.3.15+ (workaround for known URP bug)

1.6.2

Added:
- Intersection Foam distortion parameter, offsets the texture sample by the normals
- Caustics chromance parameter, allows blending between grayscale- and RGB caustics

Fixed:
- Translucency rendering not being applied with the correct strength for a 2nd+ point light.

1.6.1

Fixed:
- DWP2 integration, water level being fixed to a value of 0 in some cases.
- Atmospheric Height Fog integration, shader error when using latest version (v3.2.0+)

1.6.0
Verified compatibility with Unity 2023.2.0

Note: Unity packages cannot track moved/renamed/deleted files. It is recommended to delete the StylizedWater2 folder before updating!

Added:
- Generalized water render feature
  * Screen Space Reflections (in preview)
  * Water displacement prepass (advanced custom uses for the time being)
  * Directional caustics option

- Public static C# parameter: WaterObject.PositionOffset. Water shading and buoyancy will be offset by this value, for use with floating-origin systems.
- Fog integration for Buto 2022 (v7.7.3+)
- Render queue -/+ buttons in material UI
- Lava flow river material

Fixed:
- Planar Reflections not composting correctly on Android when HDR was enabled
- HDR information from reflection probes not correctly being used in the water material, making emissive surfaces appear dim.
- Shader compile error when building for Android when using VR and OpenGLES (workaround for returning Unity bug).

Changed:
- Tiling parameters for Normals and Surface Foam now have an X & Y component. Allowing for width/length stretching.
- Animation speed for texture-based effects now remain consistent when altering a tiling parameter.
- "Floating Transform" component was renamed to "Align Transform To Waves", to clarify its functionality.
- Surface Foam, normal map now also influences the Distortion parameter
- Negative speed values are now allowed for Rivers
- Buoyancy.SetCustomTime function deprecated. The static WaterObject.CustomTime parameter may now be used instead.
- Dynamic effects (v1.1.0)
  * Created curvature is no longer influenced by reflection distortion parameters. Effects always create visual distortions.
  * Projected normals are now visible even when the Normals feature is disabled on the material.
  * Vertex displacement now also applies to water materials with Tessellation disabled.

Removed:
- Obsolete buoyancy code functions
- Support for code without the Mathematics package installed (it is required with URP anyway).

1.5.5

Fixed:
- Shader error when using Curved World 2022, due to incorrect order of shader library compilation.
- Water appearing completely black when Buto 2022 is installed (temporary fix until integration is available)

1.5.4

Fixed:
- Material UI, the Dynamic Effects surface foam texture slot being linked to the regular Foam Mask slot.

1.5.3

Fixed:
- Material UI, dropdown texture selection not working if the asset was installed as a package
- Vertex color-based intersection foam not appearing if the "Disable Depth Texture" option was enabled.
- Caustics not being visible if the "Disable Depth Texture" option was enabled.
- Surface Foam added through vertex colors not contributing to river slopes

1.5.2

Added:
- Material UI, dropdown menu next to foam/normal map texture slots, to quickly try out different textures.
- River mode, parameters to control the minimum slope angle and falloff.
- Translucency, parameter to control effect from direct light, simulates glacial or morene lakes.
- Option to enable support for light cookies in the shader inspector

Changed:
- Improved calculation of light intensity for sun reflection and translucency, now more accurately responds to color.

Fixed:
- Box projected Reflection probes not appearing correctly on the water surface for orthographic cameras.
- Caustics appearing in shadows underwater when using the Advanced shading mode (regression since v1.4.0)
- Shader error when Light Probe Volumes was enabled in Unity 2023.1+

1.5.1
Minimum supported version is now Unity 2021.3.16f1 (URP 12.1.6).

Added:
- Planar Reflections UI, warning when using the default renderer. Added a button to automatically create and assign a new empty renderer.
- Planar Reflections, option to enabled default fog for the render.
- Frozen lake prefab material.
- Extended support for the Rendering Debugger. Reflections, translucency and caustics can now also be inspected.

Changed:
- Revised some demo scene content.
- Fog density is now calculated per pixel, as opposed to per vertex (standard as of Unity 2021.2).

Fixed:
- Water not rendering on Android VR if URP's "SH Evaluation" setting was set to 'Per vertex'.
- Surface Foam tiling parameter not having any effect for foam drawn on slopes (regression since v1.5.0).
- (Unity 2022.2+) Planar Reflections causing an error in URP's rendering loop when the scene hierarchy search functionality was used.
- Planar Reflections, error thrown when using the Profiler and exiting Play mode.

1.5.0
Verified compatibility with Unity 2023.1f1. This is the last update supporting Unity 2020.

Added:
- Swamp water material
- Improved refraction (Advanced shading mode only), taking the surface curvature more accurately into account + Exposed parameter to control chromatic aberration
- Color absorption feature, darkens the underwater color based on its depth (Advanced shading mode only).
- Surface Foam now dissipates based on a base amount of foam, and that added through waves
  * Base Amount parameter, previously controlled by the foam color alpha channel
  * Foam Distortion parameter, distorts the foam by the amount of vertical surface displacement (such as with waves).
- Parameters to control the Tiling/Speed of the Normal Map and Surface Foam sub-layers.
- Support for Light Layers in Unity 2022.2+
- Global Illumination support when using Tessellation

Changed:
- Material UI, corrected unwanted indentation in Unity 2022.2+ due to parameter locking functionality.
- Translucency shading, Curvature Mask parameter now acts as a mask for surface slopes
- Increased maximum allowed amount of tessellation subdivisions from 16 to 32 (internally still limited to 15 on Xbox One and PS5).
- Water Mesh asset utility now has a "Bounds padding" value exposed. Previously used a fixed value of 4, to add artificial height to the mesh for improved culling.
- Improved consistency of Distance Depth (fog) appearance between perspective- and orthographic cameras.
- Caustics no longer appear on the skybox behind water geometry
- Refraction is no longer forced to be disabled if the Normals feature is.
- Distance Normals tiling parameter is now decoupled from the base normal tiling
- Updated fog integration for Atmospheric Height Fog (3.0.0).
- Shader error message is now thrown should a fog asset cause a Built-in RP shader library to be compiled into it.
- Fog shading now also applies to backfaces of the geometry used

Fixed:
- Inconsistent animation direction between Mesh UV and World Projected coordinate modes. 
- Static lightmapping not having any effect in Unity 2022.3+ (GI now no longer supported in 2020.3)
- Normals being incorrect if World XZ Projected UV was used, yet the water plane was rotated on the Y-axis.
- Caustics no longer disappear when lightmaps are applied (no light was available to control the effect's intensity)
- Default Unity fog integration not taking effect, when automatic integration detection was enabled.
- Scaling a Water Grid component caused it to not create geometry correctly, nor display the grid gizmo accurately.
- Auto-setup function for DWP2 in help window not working if no Flat Water Data Provider component was present anywhere.
- Preventing Enviro (1 & 3) and Atmospheric Height Fog from shading the water if their settings are un-initialized.
- Incorrect wave normals when geometry had a rotation, causing reflections to look incorrect.
- Pre-emptive shader error fixes in Unity 2023.2+, due to API changes.

Removed:
- Surface Foam, Wave Mask Exponent parameter

1.4.0
Converted water shader to a new 'scriptable shader' framework. Integrations, extensions and version-specific hooks are now automatically incorporated.

Added:
- Material UI, added a "Toggle tooltips" button, which shows every parameter's tooltip below it.
- Surface Foam slope parameter for River Mode. Controls how much foam is automatically drawn on slopes.
- Waterfall foam texture.
- Option to disable z-clipping of the water surface (Rendering tab).
- Improved performance of Buoyancy API (roughly 20% faster).
- Water Mesh + Water Grid inspector, option to preview the mesh as a wireframe in the scene view.
- Options under "GameObject/3D Object/Water" to create a water object or grid.
- Planar Reflection Renderer inspector, preview image of rendered reflection in the UI.
- Planar Reflection Renderer, option to take the component's transform rotation into account. Making an upside-down/sideways reflection possible.

Changed:
- Material UI, "Advanced" section renamed to "Rendering".
- River slope normal map now uses the same import settings as the base normal map (reduces the amount of samplers used).
- Re-authored foam/caustics textures to double their scaling size, reducing visible tiling artefacts.
- Caustics are no longer disabled if the "Disable Depth Texture" is enabled. Instead, they draw on the water surface itself.
- Integration for Dynamic Water Physics 2 is now based on the "NWH_DWP2" scripting define symbol. No longer required to unlock the component.
- WaterMesh class, improvements to make parameter changes non-destructive:
  * Vertex noise is now consistent at any mesh scale
  * UV scale is no longer influenced by scale
  * No longer uses a fixed number of subdivisions, but rather a "vertex distance" value.

Fixed:
- Warnings about obsolete code in Unity 2023.1+ regarding OpenGLES 2.0 checks (now completely unsupported).
- Reflection distortion for Planar Reflections not being equally strong as for reflection probes.
- Refraction artifacts visible for geometry in front of the water in orthographic projections.
- Refraction no longer being visible when the Disable Depth Texture option was enabled.

Removed:
- Public functions in Buoyancy class that were marked as obsolete since v1.1.4

1.3.1

Added:
- Fog integration for Buto Volumetric Fog and Lighting

Changed:
- Reduced the amount of shader variants by 22%, decreasing build times.
- Updated fog integration for SC Post Effects (v2.3.2+)
- Reduced the file size of the StreamWaves normal map file by half (no quality loss).

Fixed:
- Wave tint parameter creating negative color values (it now merely brighten wave crest as intended).
- Preemptive shader error fix for Unity 2023.1.0a26+
- Materials not receiving shadows, or showing banding artefacts, when a single shadow cascade is used. (In Unity 2021.3.15+, or versions released after December 15th 2022)

1.3.0
- If in use, this version must be updated alongside of the Underwater Rendering extension v1.0.7.
- Retired support for Unity 2019

Materials must be converted to an updated format, a pop up will appear when selecting a water material to notify you. This process is automated.
* Custom water materials may look incorrect (eg. certain features suddenly disabled) until the conversion is performed.

Added:
- Material UI now supports multi-selection and Material Variants (in Unity 2022.1+).
- Support for Forward+ rendering (in 2022.2+).
- Distortion & Strength Multiplier parameters for point/spot light reflections.
- A separate normal map texture can now be used for river slopes.

Changed:
- Translucency shading now only takes effect within the water's fog.
- Sun/Light specular reflection no longer assume the surface is flat, and will better work on curved surfaces.
- Usage of a custom time value no longer requires modifying the shader code. Instead using the Buoyancy.SetCustomTime function automatically enables this.
- URP version specific shader code is now solely based on the Unity editor version, rather than the URP package version (new standard as of 2022.1).
- Help window now shows the compatibility state of the chosen graphics API.

Fixed:
- Horizon color and Reflection Curvature Mask now behave the same for back faces as they do for front faces.
- Caustics for point/spot lights not taking the water's fog density into account.

1.2.0

Added:
- Fog integration for Enviro 3
- Planar Reflection Renderer, option to move the rendering bounds with the Transform

Fixed:
- Refraction not affecting the skybox on sloped water surfaces
- 'Assets/Create/Water mesh' action not directly creating the asset, unless the asset database was refreshed.
- Planar Reflections, realtime changes to the Field of View or Orthographic camera properties not being applied.
- Pre-emptive fixes for Unity 2022.2 beta

1.1.9

Added:
- Planar Reflection Renderer, API to add/remove specific water objects

Changed:
- Updated fog integration for COZY Weather
- The distance normals texture now uses the same import settings as the regular normal map (eg. filtering settings)

Fixed:
- Horizon color appearing as a sharp circle on Mobile at large distances
- Underwater reflection not appearing entirely correct (regression since v1.1.8)
- Edge fading canceling out translucency shading when viewed from underwater

1.1.8b

Hotfixes:
- Underwater reflections not showing objects above the water surface
- Build error caused by keyword stripping under certain conditions

1.1.8
Minimum supported version is now Unity 2020.3.1 and URP 10.3.2

Added:
- Fog integration for COZY Weather.
- Sharp and Stream waves normal map.
- River mode, slope threshold parameter.

Changed:
- Updated smooth waves normal map to show less repetition.
- Alpha channels of deep/shallow colors no longer control opacity, but water depth instead. Clear water is now made possible.
- Simple shading now also masks out objects above the water for refraction.
- Advanced shading now also refracts the fog gradient (requires more calculations, hence limited to this mode).
- Reflections are now laid over translucency shading. Added a parameter to control this behaviour.
- Optimized wave shader calculations for vertex animations.
- Double-clicking a water mesh in an inspector now properly selects it, rather than the OS trying to open the file.

Fixed:
- Planar Reflection with Underwater Rendering causing a black/white image when used together.
- Blue vertex color painting not affecting horizontal wave movements.
- Horizontal displacement created by waves not affecting UV's. Normal map now also moves back and forth.

1.1.7

Added:
- Support for spot/point light shadows in Unity 2021.1+/URP v12+ (requires the "Receive shadows" option to be enabled).

Changed:
- Caustics effect is now influenced by the sun light's intensity. Spot/point lights will create a localized caustics effect.
- Improved translucency shading for point/spot lights, now visually behaves the same as it does for a directional light.
- Improved light reflection for rivers, now takes the mesh's curvature into account.
- Lighting no longer applies to the reflected image, since it is already lit. Added parameter to control this behavior.
- Warning is now displayed in the material UI if the render queue set is illegal.

Fixed:
- Specular reflection for point lights still being calculated when Light Reflection feature was disabled.
- Shadow mask used for the sun reflection/translucency did not take shadow fading into account.
  * The "Receive shadows" must be now be enabled to mask these effect. Shadow strength can still be 0.

1.1.6

Added:
- Planar Reflection Renderer, option to enable shadows in the reflection render.

Fixed:
- StylizedWaterDataProvider script breaking since DWP2 v2.5.1, due to API changes (now the minimum required version)

1.1.5
Minimum supported version is now 2019.4.15 LTS and URP 7.5.3. Verified compatibility with 2021.2.0

If in use, this version must be updated alongside of the Underwater Rendering extension v1.0.4.

Added:
- Support for lightmaps and realtime GI

Changed:
- Floating Transform component now has a "Dynamic material" checkbox. If set, wave settings are fetched each frame.
- Sample positions on Floating Transform component now correctly work with prefab overrides.
- Water Grid component will now regenerate the water tile mesh if it is missing. Allowing it to be used in prefabs.
- Intersection Foam and Edge Fade effects now fade out when the camera sits extremely close on the water surface (excluded for rivers).
  * This avoids them being visible through the waves, causing a horizontal line.
  * Can cause slight visual warping, but this isn't noticeable for animated water
- Green vertex color channel now controls the depth color gradient, rather than Opacity. This allows all depth-based effects to be controlled through vertex colors
* This behaviour still controls opacity when river mode is used.

Fixed:
- Terrain appearing black/red in planar reflections when using more than 4 layers.
- "Screen position out of view frustum" errors being thrown when using Planar Reflections with an orthographic camera aimed completely straight.
- Sun reflection blowing out when using Gamma color space and HDR
- Unused shader variants not being stripped when Tessellation was being used, causing longer build times on older Unity versions.

1.1.4

Added:
- WaterObject.Find function. Attempts to find the WaterObject above or below the given position.
  * Floating Transform component now has an "Auto-find" option
- Buoyancy.Raycast function. Given a position and direction, finds the intersection point with the water
- Planar Reflections, maximum LOD quality setting. Can be used to limit reflected objects to LOD1 or LOD2
- Caustics distortion parameter. Offsets the caustics based on the normal map.

Changed:
- Planar Reflections, standard fog no longer renders into to the reflection. This can cause artifacts where it "bleeds" over partially culled geometry

Fixed:
- Azure fog library file not being found (GUID was changed)

1.1.3

Added:
- Vertical depth parameter, controls the density of the water based on viewing angle
- Realistic water material (ocean-esque)

Changed:
- Water meshes created through the utility or Water Grid component now have some height. Avoids premature culling when using high waves.
- Water Mesh utility can now automatically apply changes when parameters are modified.
- Setup actions under the Window menu now support Undo/redo actions
- Improved blending of normal maps for the Advanced shading mode
- Wave normals now distort caustics, creating a more believable effect
- Translucency shading now takes the surface curvature into account (controllable through new parameter)
- UI: translucency moved to the Lighting/Shading tab

Fixed:
- Water depth not rendering correctly when using an orthographic camera and OpenGL
- WaterMesh.Create function not taking the chosen shape into account, always creating a rectangle
- Integration for Dynamic Water Physics 2 no longer working since v2.4.2 (now the minimum required version)
- Refraction and reflection distortion appearing much stronger for orthographic camera's

1.1.2
This version includes some changes required for the Underwater Rendering extension

Added:
- Tessellation support (preview feature). Can be enabled under the "Advanced" tab.
- Added menu item to auto-setup Planar Reflections or create a Water Grid
- Hard-coded file paths for third-party fog libraries are now automatically rewritten
- Fog can now be globally disabled for the water through script (see "Third party integrations" section in docs)
- Support for reflection probe blending and box projection. Requires 2021.2.0b1+, will break in an alpha version
- (Pre-emptive) Support for rendering debug window in 2021.2.0b1

Changed:
- Menu item "Help/Stylized Water 2" moved to "Window/Stylized Water 2/Hub"
- Improved refraction, this now takes the surface curvature into account. Maximum allowed strength increased to x3

Fixed:
- Fog integration for SC Post Effects and Atmospheric Height Fog (v2.0.0+)

1.1.1

Hotfix for build-blocking error in some cases. Shader variants for features not installed are now stripped during the build process

1.1.0

Added:
- Diorama example scene
- Exposed parameter to control the size of point/spot light specular reflections

Changed:
- Translucency shading will now also work if lighting is disabled
- Intersection- and surface foam and now appear correct when using the Gamma color-space
- Surface foam can now be painted using the Alpha channel of the painted color. Previously the Blue channel was used only if River Mode was enabled.
- Planar Reflection Renderer component now shows a warning if reflection were globally disabled by an external script

Fixed:
- Scripting build error on tvOS due to VR code being stripped for the platform
- When river mode is enabled, a second layer of foam on slopes wasn't visible.
- Planar Reflection Renderer component needing to be toggled when using multiple instances
- Buoyancy not being in sync when animation speed was higher than x1

1.0.9

Added:
- Support for SC Post Effects Pack fog rendering (activated through the Help window)

Changed:
- Material UI now has a help toggle button, with quick links to documentation
- Buoyancy.SampleWaves function now has a boolean argument, to indicate if the material's wave parameters are being changed at runtime. Version without this has been marked as obsolete.
- Floating Transform component now supports multi-selection

Fixed:
- Planar reflections failing to render if the water mesh was positioned further away than its size
- Warning about obsolete API in Unity 2021.2+ due to URP changes, package will now import without any interuptions
- Shader error when Enviro fog was enabled, due to a conflicting function name

1.0.8

Added:
- Distance normals feature, blends in a second normal map, based on a start/end distance.
- Distance fade parameter for waves. Waves can smoothly fade out between a start/end distance, to avoid tiling artifacts when viewed from afar.
- Planar Reflections, option to specify the renderer to be used for reflection. This allows a set up where certain render features aren't being applied to the reflection

Changed:
- Planar Reflections inspector now shows a warning and quick-fix button if the bounds need to be recalculated.
- Material section headers now also have tooltips
- Water Grid improvements, now only recreates tiles if the rows/colums value was changed

Fixed:
- Issues with DWP2 integration since its latest update

1.0.7

Added:
- Planar Reflections, option to include skybox.

Changed:
- Greatly improved caustics shading. No longer depends on the normal map for distortion.
- Normal map now has a speed multiplier, rather than being bound to the wave animation speed. If any custom materials were made, these likely have to be adjusted
- Updated default filepath for Boxophobic's Atmospheric Height Fog. This requires to reconfigure the fog integration through the Help window.
- Removed "MainColor" attribute from shader's deep color, to avoid Unity giving it a white color value for new materials
- Floating Transform, if roll strength is 0, the component will no longer modify the transform's orientation

Fixed:
- Objects above/in front of the water are no longer being refracted. Requires some additional legwork, so is limited to the Advanced shading mode.
- (Preliminary) Error about experimental API in Unity 2021.2+

1.0.6

Added:
- River mode, forces animations to flow in the vertical UV direction and draws surface foam on slopes (with configurable stretching and speed).
- Waterfall splash particle effect
- Data provider script for Dynamic Water Physics 2 is now part of the package (can be unlocked through the Help window if installed)
- Curvature mask parameter for environment reflections, allows for more true-to-nature reflections

Changed:
- Water hole shader can now also be used with Curved World 2020

Fixed:
- Planar reflections, reflected objects being sorting in reverse in some cases
- Mobile, animations appearing stepped on some devices when using Vulkan rendering
- (Preliminary) Error in URP 11, due to changes in shader code

1.0.5

Added:
- Particle effects, composed out of flipbooks with normal maps:
  * Big splash
  * Stationary ripples
  * Trail
  * Collision effect (eg. boat bows)
  * Splash ring (eg. footsteps)
  * Waterfall mist
  * Splash upwards
- Water Grid component, can create a grid of water objects and follow a specific transform.

Fixed:
- Material turning black if normal map was enabled, yet no normal map was assigned.
- Intersection texture was using mesh UV, even when UV source was set to "World XZ Projected".

Changed:
- Waves no longer displace along the mesh's normal when "World XZ projected" UV is used, which was incorrect behaviour
- Sparkles are no longer based on sun direction, this way they stay a consistent size in dynamically lit scenes. Instead they fade out when the sun approaches the horizon.
- When using Flat Shading, refraction and caustics still use the normal map, instead of the flat face normals.
- Planar Reflections render scale now takes render scale configured in URP settings into account
- Improved Rough Waves and Sea Foam textures

1.0.4

Added:
- CanTouchWater function to Buoyancy class: Checks if a position is below the maximum possible wave height. Can be used as a fast broad-phase check, before actually using the more expensive SampleWaves function
- Context menu option to Transform component to add Floating Transform component

Fixed:
- Error in material UI when no pipeline asset was assigned in Graphics Settings
- Floating Transform, sample points not being saved if object was a prefab

Changed:
- Revised demo content
- Floating Transform no longer animates when editing a prefab using the component
- Minor UI improvements

1.0.3

Added:
- Water Object script, as a general means of identifying and finding water meshes between systems, this is now attached to all prefabs
- Planar reflections renderer, enables mirror-like reflections from specific layers

Changed:
- Buoyancy sample positions of Floating Transform can now be manipulated in the scene view

Fixed:
- Error when assigning material to Floating Transform component, or not being able to

1.0.2
Verified compatibility with Wavemaker

Added:
- Support for Boxophobic's Atmospheric Height Fog (can be enabled through the Help window)

Changed:
- Shader now correctly takes the mesh's vertex normal into account, making it suitable for use with spheres and rivers

1.0.1
Verified compatibility with Oculus Quest (see compatibility section of documentation, some caveats apply due to an OpenGLES bug)

Fixed:
- Hard visible transition in translucency effect when exponent was set to 1
- Back faces not visible when culling was set to "Double-sided" and depth texture was enabled

1.0.0
Initial release (Nov 3 2020)