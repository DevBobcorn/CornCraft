This project uses a few assets from Asset Store which is **not** included in the project repository. You can either purchase and import them to get the full project as is, or make changes to the project by replacing the references/usages of these assets.

Here's a list of the assets:

- [**Stylized Water 2**](https://assetstore.unity.com/packages/vfx/shaders/stylized-water-2-170386): Water material used in the project. The water materials can be replaced with transparent block material at `Resources/Block/Materials/Block Transparent.mat`

- [**Enviro 3**](https://assetstore.unity.com/packages/tools/particles-effects/enviro-3-sky-and-weather-236601): Provides volumetric clouds and a nice-looking skybox as the environment. There is a `Simple Skybox` GameObject in every scene which can be used as a substitute for this, just activate it and remove the Enviro 3 one, and then set the `Environment Manager` reference in `CornClient` to this object. Also, you'll need to update `Atlas Transparent` reference in `Chunk Material Manager` to get transparent materials working.

- [**Magica Cloth 2**](https://assetstore.unity.com/packages/tools/physics/magica-cloth-2-242307): Cloth/Hair physics simulation for rigged characters. You can remove the `Physics` GameObject from player render prefab if not using this asset, or you can instead use the blocky-lookng vanilla player render.

Also, there are some free assets included in the repo just for convenience. It is recommended that you claim these assets from Asset Store so as to get updates and avoid potential licensing issues. Below is a list of them:

- [**Unity-Chan! Model**](https://assetstore.unity.com/packages/3d/characters/unity-chan-model-18705): Default player render model. Original BiRP materials are updated for rendering with URP. This asset is licensed under [UCL](https://unity-chan.com/contents/license_en/).

- [**Kinematic Character Controller**](https://assetstore.unity.com/packages/tools/physics/kinematic-character-controller-99131): Code base for creating custom player controllers used in the project.