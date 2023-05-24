using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
 

namespace Enviro
{
	[Serializable] 
	public class EnviroWeatherTypeCloudsOverride
	{
		//Layer 1
		public bool showLayer1;
		public float coverageLayer1 = 0f;
		public float dilateCoverageLayer1 = 0.5f;
		public float dilateTypeLayer1 = 0.5f; 
		public float typeModifierLayer1 = 0.5f;
		public float anvilBiasLayer1 = 0.0f;
		public float scatteringIntensityLayer1 = 1.5f;		
		public float multiScatteringALayer1 = 0.5f;
		public float multiScatteringBLayer1 = 0.5f;
		public float multiScatteringCLayer1 = 0.5f;
		public float powderIntensityLayer1 = 0.3f;
		public float silverLiningSpreadLayer1 = 0.8f;
		public float ligthAbsorbtionLayer1 = 1.0f;
		public float densityLayer1 = 0.3f;
		public float baseErosionIntensityLayer1 = 0.0f;
		public float detailErosionIntensityLayer1 = 0.3f;
		public float curlIntensityLayer1 = 0.05f;

		//Layer 2
		public bool showLayer2;
		public float coverageLayer2 = 0f;
		public float dilateCoverageLayer2 = 0.5f;
		public float dilateTypeLayer2 = 0.5f;
		public float typeModifierLayer2 = 0.5f;
		public float anvilBiasLayer2 = 0.0f;
		public float scatteringIntensityLayer2 = 1.5f;
		public float multiScatteringALayer2 = 0.5f;  
		public float multiScatteringBLayer2 = 0.5f;
		public float multiScatteringCLayer2 = 0.5f;
		public float powderIntensityLayer2 = 0.3f;
		public float silverLiningSpreadLayer2 = 0.8f;
		public float ligthAbsorbtionLayer2 = 1.0f;
		public float densityLayer2 = 0.3f;
		public float baseErosionIntensityLayer2 = 0.0f;
		public float detailErosionIntensityLayer2 = 0.3f;
		public float curlIntensityLayer2 = 0.05f;
	}

	[Serializable]  
	public class EnviroWeatherTypeFlatCloudsOverride
	{
		public float cirrusCloudsAlpha = 0.5f;
		public float cirrusCloudsCoverage = 0.5f;
		public float cirrusCloudsColorPower = 1.0f;
		public float flatCloudsCoverage = 1.0f;
		public float flatCloudsDensity = 1.0f;
		public float flatCloudsLightIntensity = 1.0f;
		public float flatCloudsAmbientIntensity = 1.0f;
		public float flatCloudsAbsorbtion = 0.6f;
	} 

	[Serializable] 
	public class EnviroWeatherTypeLightingOverride
	{
		public float directLightIntensityModifier = 1.0f;
		public float ambientIntensityModifier = 1.0f;
	} 
 
 	[Serializable]  
	public class EnviroAudioOverrideType
	{
		public bool showEditor;
		public string name;
		public float volume;
		public bool spring;
		public bool summer;
		public bool autumn;
		public bool winter;
	}

	[Serializable]  
	public class EnviroWeatherTypeAudioOverride
	{
		public List<EnviroAudioOverrideType> ambientOverride = new List<EnviroAudioOverrideType>();
		public List<EnviroAudioOverrideType> weatherOverride = new List<EnviroAudioOverrideType>();
	}
	
	[Serializable] 
	public class EnviroWeatherTypeFogOverride
	{
		public float fogDensity = 0.02f; 
		public float fogHeightFalloff = 0.2f;
		public float fogHeight = 0.0f;
		public float fogDensity2 = 0.02f;
		public float fogHeightFalloff2 = 0.2f;
		public float fogHeight2;   
		public float fogColorBlend = 0.5f;
		public float scattering = 0.015f;
		public float extinction = 0.01f;
		public float anistropy = 0.6f; 

		#if ENVIRO_HDRP 
		public float fogAttenuationDistance = 400f;	
		public float maxHeight = 250f;
		public float baseHeight = 0f;
		public float ambientDimmer = 1f;
		public float directLightMultiplier = 1f;
		public float directLightShadowdimmer = 1f;
		#endif
	}

	[Serializable]  
	public class EnviroWeatherTypeEffectsOverride
	{
		public float rain1Emission, rain2Emission, snow1Emission, snow2Emission, custom1Emission, custom2Emission = 0f;
	} 

	[Serializable]  
	public class EnviroWeatherTypeAuroraOverride
	{
		public float auroraIntensity = 1f;
	} 

	[Serializable]  
	public class EnviroWeatherTypeEnvironmentOverride
	{
		public float temperatureWeatherMod = 0f;
		public float wetnessTarget = 0f;
		public float snowTarget = 0f;

		public float windDirectionX = 1f;
		public float windDirectionY = -1f;
		public float windSpeed = 0.25f;
		public float windTurbulence = 0.25f;

	} 

	[Serializable]  
	public class EnviroWeatherTypeLightningOverride
	{
		public bool lightningStorm = false;
		public float randomLightningDelay = 1f;
	} 
 
	[Serializable]  
	public class EnviroWeatherType : ScriptableObject 
	{
		//Inspector
		public bool showEditor, showEffectControls, showCloudControls, showFlatCloudControls, showFogControls, showLightingControls, showAuroraControls,showEnvironmentControls, showAudioControls, showAmbientAudioControls, showWeatherAudioControls,showLightningControls;
		
		public EnviroWeatherTypeCloudsOverride cloudsOverride;
		public EnviroWeatherTypeFlatCloudsOverride flatCloudsOverride;
		public EnviroWeatherTypeLightingOverride lightingOverride;
		public EnviroWeatherTypeFogOverride fogOverride;
		public EnviroWeatherTypeAuroraOverride auroraOverride;
		public EnviroWeatherTypeEffectsOverride effectsOverride;
		public EnviroWeatherTypeAudioOverride audioOverride;
		public EnviroWeatherTypeLightningOverride lightningOverride;
		public EnviroWeatherTypeEnvironmentOverride environmentOverride;
	}


	public class EnviroWeatherTypeCreation {
		#if UNITY_EDITOR
		[UnityEditor.MenuItem("Assets/Create/Enviro3/Weather")]
		#endif
		public static EnviroWeatherType CreateMyAsset()
		{
			EnviroWeatherType wpreset = ScriptableObject.CreateInstance<EnviroWeatherType>();
			#if UNITY_EDITOR
			// Create and save the new profile with unique name
			string path = UnityEditor.AssetDatabase.GetAssetPath (UnityEditor.Selection.activeObject);
			if (path == "") 
			{
				path = EnviroHelper.assetPath + "/Profiles/Weather Types";
			} 
			string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath (path + "/New " + "Weather Type" + ".asset");
			UnityEditor.AssetDatabase.CreateAsset (wpreset, assetPathAndName);
			UnityEditor.AssetDatabase.SaveAssets ();
			UnityEditor.AssetDatabase.Refresh();
			#endif
			return wpreset;
		}


		public static GameObject GetAssetPrefab(string name)
		{
			#if UNITY_EDITOR
			string[] assets = UnityEditor.AssetDatabase.FindAssets(name, null);
			for (int idx = 0; idx < assets.Length; idx++)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[idx]);
				if (path.Contains(".prefab"))
				{
					return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
				}
			}
			#endif
			return null;
		}

		public static Cubemap GetAssetCubemap(string name)
		{
			#if UNITY_EDITOR
			string[] assets = UnityEditor.AssetDatabase.FindAssets(name, null);
			for (int idx = 0; idx < assets.Length; idx++)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[idx]);
				if (path.Contains(".png"))
				{
					return UnityEditor.AssetDatabase.LoadAssetAtPath<Cubemap>(path);
				}
			}
			#endif
			return null;
		}

		public static Texture GetAssetTexture(string name)
		{
			#if UNITY_EDITOR
			string[] assets = UnityEditor.AssetDatabase.FindAssets(name, null);
			for (int idx = 0; idx < assets.Length; idx++)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[idx]);
				if (path.Length > 0)
				{
					return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(path);
				}
			}
			#endif
			return null;
		}
			
		public static Gradient CreateGradient()
		{
			Gradient nG = new Gradient ();
			GradientColorKey[] gClr = new GradientColorKey[2];
			GradientAlphaKey[] gAlpha = new GradientAlphaKey[2];
 
			gClr [0].color = Color.white;
			gClr [0].time = 0f;
			gClr [1].color = Color.white;
			gClr [1].time = 0f;

			gAlpha [0].alpha = 0f;
			gAlpha [0].time = 0f;
			gAlpha [1].alpha = 0f;
			gAlpha [1].time = 1f;

			nG.SetKeys (gClr, gAlpha);

			return nG;
		}
			
		public static Color GetColor (string hex)
		{
			Color clr = new Color ();	
			ColorUtility.TryParseHtmlString (hex, out clr);
			return clr;
		}
		
		public static Keyframe CreateKey (float value, float time)
		{
			Keyframe k = new Keyframe();
			k.value = value;
			k.time = time;
			return k;
		}

		public static Keyframe CreateKey (float value, float time, float inTangent, float outTangent)
		{
			Keyframe k = new Keyframe();
			k.value = value;
			k.time = time;
			k.inTangent = inTangent;
			k.outTangent = outTangent;
			return k;
		}		
	}
}
