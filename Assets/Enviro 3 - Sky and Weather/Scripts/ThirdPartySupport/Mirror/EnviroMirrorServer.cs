/// <summary>
/// This component can be used to synchronize time and weather.
/// </summary>

using UnityEngine;
#if ENVIRO_MIRROR_SUPPORT
using Mirror;
#endif
using System.Collections;
namespace Enviro
{
	#if ENVIRO_MIRROR_SUPPORT
	[AddComponentMenu("Enviro 3/Integrations/Mirror Server")]
	[RequireComponent(typeof (NetworkIdentity))]
	public class EnviroMirrorServer : NetworkBehaviour {
	#else
	public class EnviroMirrorServer : MonoBehaviour {
	#endif
	#if ENVIRO_MIRROR_SUPPORT
		public float updateSmoothing = 15f;

		[SyncVar] private float networkHours;
		[SyncVar] private int networkDays;
		[SyncVar] private int networkMonths;
		[SyncVar] private int networkYears;

		public bool isHeadless = true;

		public override void OnStartServer()
		{
			if (isHeadless) 
			{
				//EnviroManager.instance.serverMode = true;
			}
				
			EnviroManager.instance.OnSeasonChanged += (EnviroEnvironment.Seasons season) => {
				SendSeasonToClient (season);
			};
			EnviroManager.instance.OnWeatherChanged += (EnviroWeatherType type) => {
				SendWeatherToClient (type);
			};
		}

		public void Start ()
		{
			if (!isServer) 
			{
				if(EnviroManager.instance.Time != null)
				   EnviroManager.instance.Time.Settings.simulate = false;
			}
		}

		void SendWeatherToClient (EnviroWeatherType w)
		{
			if(EnviroManager.instance.Weather != null)
			{
				for (int i = 0; i < EnviroManager.instance.Weather.Settings.weatherTypes.Count; i++)
				{
					if(EnviroManager.instance.Weather.Settings.weatherTypes[i] == w)
					   RpcWeatherUpdate (i);
				}
			}
		}

		void SendSeasonToClient (EnviroEnvironment.Seasons s)
		{
			RpcSeasonUpdate((int)s);
		}

		[ClientRpc]
		void RpcSeasonUpdate (int season)
		{
			if(EnviroManager.instance.Environment != null)
			   EnviroManager.instance.Environment.ChangeSeason((EnviroEnvironment.Seasons)season);
		}

		[ClientRpc]
		void RpcWeatherUpdate (int weather)
		{
			if(EnviroManager.instance.Weather != null)
			   EnviroManager.instance.Weather.ChangeWeather(EnviroManager.instance.Weather.Settings.weatherTypes[weather]);
		}


		void Update ()
		{
			if (EnviroManager.instance == null || EnviroManager.instance.Time == null)
				return;

			if (!isServer) 
			{
				if (networkHours < 1f && EnviroManager.instance.Time.GetTimeOfDay() > 23f)
					EnviroManager.instance.Time.SetTimeOfDay(networkHours);

				EnviroManager.instance.Time.SetTimeOfDay(Mathf.Lerp(EnviroManager.instance.Time.GetTimeOfDay(), (float)networkHours, Time.deltaTime * updateSmoothing));
				EnviroManager.instance.Time.years = networkYears;
				EnviroManager.instance.Time.months = networkMonths;
				EnviroManager.instance.Time.days = networkDays;

			} else {
				networkHours = EnviroManager.instance.Time.GetTimeOfDay();
				networkDays = EnviroManager.instance.Time.days;
				networkMonths = EnviroManager.instance.Time.months;
				networkYears = EnviroManager.instance.Time.years;
			}

		}
	#endif
	}
}

