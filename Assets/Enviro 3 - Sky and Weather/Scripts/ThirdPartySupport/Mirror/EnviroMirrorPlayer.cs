/// <summary>
/// This component can be used to synchronize time and weather in games where server is a player too.
/// </summary>

using UnityEngine;
using System.Collections;
#if ENVIRO_MIRROR_SUPPORT
using Mirror;
#endif

namespace Enviro
{
#if ENVIRO_MIRROR_SUPPORT
	[AddComponentMenu("Enviro 3/Integrations/Mirror Player")]
	[RequireComponent(typeof (NetworkIdentity))]
	public class EnviroMirrorPlayer : NetworkBehaviour 
	{
#else
	public class EnviroMirrorPlayer : MonoBehaviour 
	{
#endif
	#if ENVIRO_MIRROR_SUPPORT
		public bool assignOnStart = true;
		public bool findSceneCamera = true;

		public Camera Camera;
		
		public void Start()
		{
			// Deactivate if it isn't ours!
			if (!isLocalPlayer && !isServer) {
				this.enabled = false;
				return;
			}
	
			if (Camera == null && findSceneCamera)
				Camera = Camera.main;

			if (isLocalPlayer) 
			{
				if (assignOnStart && Camera != null)
					EnviroManager.instance.Camera = Camera;

				Cmd_RequestSeason ();
				Cmd_RequestCurrentWeather ();
			}
		}
			
		[Command]
		void Cmd_RequestSeason ()
		{
			if(EnviroManager.instance.Environment != null)
			    RpcRequestSeason((int)EnviroManager.instance.Environment.Settings.season);
		}

		[ClientRpc]
		void RpcRequestSeason (int season)
		{
			if(EnviroManager.instance.Environment != null)
			   EnviroManager.instance.Environment.ChangeSeason((EnviroEnvironment.Seasons)season);
		}

		[Command]
		void Cmd_RequestCurrentWeather ()
		{
			if(EnviroManager.instance.Weather != null)
			{
				//for (int i = 0; i < EnviroSkyMgr.instance.Weather.zones.Count; i++) 
				//{
					for (int w = 0; w < EnviroManager.instance.Weather.Settings.weatherTypes.Count; w++)
					{
						if (EnviroManager.instance.Weather.Settings.weatherTypes[w] == EnviroManager.instance.Weather.targetWeatherType)
							RpcRequestCurrentWeather(w);
					}
				//}
			}
		}

		[ClientRpc]
		void RpcRequestCurrentWeather (int weather)
		{
			if(EnviroManager.instance.Weather != null)
				EnviroManager.instance.Weather.ChangeWeatherInstant(EnviroManager.instance.Weather.Settings.weatherTypes[weather]);
		}

	#endif
	}
}
