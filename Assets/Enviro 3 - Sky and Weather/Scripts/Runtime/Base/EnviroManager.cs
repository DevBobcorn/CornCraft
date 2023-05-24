using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Enviro
{
    

    [ExecuteInEditMode]
    public class EnviroManager : EnviroManagerBase
    {     
        private static EnviroManager _instance; // Creat a static instance for easy access!

        public static EnviroManager instance
        { 
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<EnviroManager>();

                return _instance;
            }
        }
  
        //General
        public GeneralObjects Objects = new GeneralObjects();
        
        //Setup
        public bool dontDestroyOnLoad;
        public Camera Camera;
        public string CameraTag = "MainCamera";
        public List<EnviroCameras> Cameras = new List<EnviroCameras>();
  
        //Inspector
        public bool showSetup;
        public bool showModules;
        public bool showEvents;
        public bool showThirdParty;

        // Publics 
        [Range(0.2f,0.7f)]
        public float dayNightSwitch = 0.45f;
        public bool isNight;
        public float solarTime;
        public float lunarTime;
        public bool notFirstFrame = false;

        //Effect Removal Zones
        public List<EnviroEffectRemovalZone> removalZones = new List<EnviroEffectRemovalZone>();
   
        struct ZoneParams
        {
            public Vector3 pos;
            public float radius;
            public Vector3 axis;
            public float stretch;
            public float density;
            public float feather;
            public float padding1;
            public float padding2;
        }

        public ComputeBuffer clearZoneCB;
        public ComputeBuffer removeZoneParamsCB;
        public ComputeBuffer clearCB;

        ZoneParams[] removalZoneParams;

        //Non time module controls
        [Range(0f,360f)]
        public float sunRotationX;
        [Range(0f,360f)]
        public float sunRotationY;
        [Range(0f,360f)]
        public float moonRotationX;
        [Range(0f,360f)]
        public float moonRotationY;
        public bool showNonTimeControls;
        /////// 
        //Events
        public Enviro.EnviroEvents Events;
        public delegate void HourPassed();
        public delegate void DayPassed();
        public delegate void YearPassed();
        public delegate void WeatherChanged(EnviroWeatherType weatherType);
        public delegate void ZoneWeatherChanged(EnviroWeatherType weatherType, Enviro.EnviroZone zone);
        public delegate void SeasonChanged(EnviroEnvironment.Seasons season);
        public delegate void isNightEvent();
        public delegate void isDayEvent();

        public event HourPassed OnHourPassed;
        public event DayPassed OnDayPassed;
        public event YearPassed OnYearPassed;
        public event WeatherChanged OnWeatherChanged;
        public event ZoneWeatherChanged OnZoneWeatherChanged;
        public event SeasonChanged OnSeasonChanged;
        public event isNightEvent OnNightTime;
        public event isDayEvent OnDayTime;

        public virtual void NotifyHourPassed()
        {
            if (OnHourPassed != null)
                OnHourPassed();
        }
        public virtual void NotifyDayPassed()
        {
            if (OnDayPassed != null)
                OnDayPassed();
        }
        public virtual void NotifyYearPassed()
        {
            if (OnYearPassed != null)
                OnYearPassed();
        }
        public virtual void NotifyWeatherChanged(EnviroWeatherType type)
        {
            if (OnWeatherChanged != null)
                OnWeatherChanged(type);
        }
        public virtual void NotifyZoneWeatherChanged(EnviroWeatherType type, EnviroZone zone)
        {
            if (OnZoneWeatherChanged != null)
                OnZoneWeatherChanged(type, zone);
        }
        public virtual void NotifySeasonChanged(EnviroEnvironment.Seasons season)
        {
            if (OnSeasonChanged != null)
                OnSeasonChanged(season);
        }
        public virtual void NotifyIsNight()
        {
            if (OnNightTime != null)
                OnNightTime();
        }
        public virtual void NotifyIsDay()
        {
            if (OnDayTime != null)
                OnDayTime();
        }

        //Event Invoke
        private void HourPassedInvoke()
        {
            Events.onHourPassedActions.Invoke();
        }
        private void DayPassedInvoke()
        {
            Events.onDayPassedActions.Invoke();
        }
        private void YearPassedInvoke()
        {
            Events.onYearPassedActions.Invoke();
        }
        private void WeatherChangedInvoke()
        {
            Events.onWeatherChangedActions.Invoke();
        }
        private void SeasonsChangedInvoke()
        {
            Events.onSeasonChangedActions.Invoke();
        }
        private void NightTimeInvoke()
        {
            Events.onNightActions.Invoke ();
        }
        private void DayTimeInvoke()
        {
            Events.onDayActions.Invoke ();
        }
        private void ZoneChangedInvoke()
        {
            Events.onZoneChangedActions.Invoke ();
        }

        //Lighting updates
        public bool updateSkyAndLighting = false;
        public bool updateSkyAndLightingHDRP = false;
 
        // HDRP
#if ENVIRO_HDRP 
        public UnityEngine.Rendering.Volume volumeHDRP;
#endif
        //////



        void OnEnable()
        {
    #if UNITY_EDITOR
            if(UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject))
               UnityEditor.PrefabUtility.UnpackPrefabInstance(gameObject,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.UserAction);
    #endif

            CreateGeneralObjects ();
    #if ENVIRO_HDRP
            CreateHDRPVolume ();
    #endif
            UpdateManager();
            EnableModules();

    #if !ENVIRO_HDRP && !ENVIRO_URP
            //Add Enviro Render Component to main camera in builtin rp
           AddCameraComponents();
    #endif

            EventInit();
        }  

        void OnDisable()
        {  
            if(Fog != null)
               Fog.Disable();

            ReleaseZoneBuffers();
        }
 
        private void AddCameraComponents()
        {
            if(Camera != null)
            {
                if(Camera.gameObject.GetComponent<EnviroRenderer>() == null)
                   Camera.gameObject.AddComponent<EnviroRenderer>();
            }

            for(int i = 0; i < Cameras.Count; i++)
            {
                 if(Cameras[i].camera.gameObject.GetComponent<EnviroRenderer>() == null)
                    Cameras[i].camera.gameObject.AddComponent<EnviroRenderer>();
            }
        }

        // Change the camera to a new one.
        public void ChangeCamera (Camera cam)
        {
            Camera = cam;

    #if !ENVIRO_HDRP && !ENVIRO_URP
            AddCameraComponents();
    #endif
        }

        void Start ()
        {   
            // Set dont destroy on load on start
            if(dontDestroyOnLoad && Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            //Set a first frame bool that will be used for events.
            notFirstFrame = false;
            StartCoroutine(FirstFrame());

            StartModules ();
        }
 
        //Update modules
        void Update()
        { 
            if(!Application.isPlaying)
                LoadConfiguration();

            UpdateManager ();

            //Update all modules
            UpdateModules ();

            //Update non time case
            if(Time == null)
                UpdateNonTime();
        }

        void LateUpdate() 
        { 
            if(Camera != null)
            {
                transform.position = Camera.transform.position;
            }    
        }

        void CreateGeneralObjects ()
        {
            if(Objects.sun == null)
            {
                Objects.sun = new GameObject();
                Objects.sun.name = "Sun";
                Objects.sun.transform.SetParent(transform);
                Objects.sun.transform.localPosition = Vector3.zero;
            }

            if(Objects.moon == null)
            {
                Objects.moon = new GameObject();
                Objects.moon.name = "Moon";
                Objects.moon.transform.SetParent(transform);
                Objects.moon.transform.localPosition = Vector3.zero;
            }
  
            if(Objects.stars == null)
            { 
                Objects.stars = new GameObject();
                Objects.stars.name = "Stars";
                Objects.stars.transform.SetParent(transform);
                Objects.stars.transform.localPosition = Vector3.zero;
            }
        }

        // Set the solar and lunar time based on sun rotation.
        private void UpdateNonTime()
        {
            if(Objects.sun != null)
            {
                Objects.sun.transform.eulerAngles = new Vector3(sunRotationX,sunRotationY,0f);

                if(sunRotationX > 0f && sunRotationX <= 90f)
                   solarTime = EnviroHelper.Remap(sunRotationX, 0f, 90f, 0.5f, 1f);
                else if (sunRotationX > 90f && sunRotationX <= 180f)
                   solarTime = EnviroHelper.Remap(sunRotationX, 90f, 180f, 1f, 0.5f);
                else if (sunRotationX > 180f && sunRotationX <= 270f)
                   solarTime = EnviroHelper.Remap(sunRotationX, 180f, 270f, 0.5f, 0.0f);
                else if (sunRotationX > 270f && sunRotationX <= 360f)
                   solarTime = EnviroHelper.Remap(sunRotationX, 270f, 360f, 0.0f, 0.5f);
                else solarTime = 0.5f;
            }
            if(Objects.moon != null)
            {
                Objects.moon.transform.eulerAngles = new Vector3(moonRotationX,moonRotationY,0f);
                 
                if(moonRotationX > 0f && moonRotationX <= 90f)
                   lunarTime = EnviroHelper.Remap(moonRotationX, 0f, 90f, 0.5f, 1f);
                else if (moonRotationX > 90f && moonRotationX <= 180f)
                   lunarTime = EnviroHelper.Remap(moonRotationX, 90f, 180f, 1f, 0.5f);
                else if (moonRotationX > 180f && moonRotationX <= 270f)
                   lunarTime = EnviroHelper.Remap(moonRotationX, 180f, 270f, 0.5f, 0.0f);
                else if (moonRotationX > 270f && moonRotationX <= 360f)
                   lunarTime = EnviroHelper.Remap(moonRotationX, 270f, 360f, 0.0f, 0.5f);
                else lunarTime = 0.5f; 
            }
        }

        //Effect Removal Zones
        public bool AddRemovalZone (EnviroEffectRemovalZone zone)
        {
            removalZones.Add(zone);
            return true;
        }
        public void RemoveRemovaleZone (EnviroEffectRemovalZone zone)
        {

         if(removalZones.Contains(zone))
            removalZones.Remove(zone);

        }
 
        private void SetupZoneBuffers() 
        { 
            int count = 0;

            for (int z = 0; z < removalZones.Count; z++)
            {
                if (removalZones[z] != null && removalZones[z].enabled && removalZones[z].gameObject.activeSelf)
                count++;
            }

            Shader.SetGlobalFloat("_EllipsZonesCount", count);

            if (count == 0)
            {
                // Can't not set the buffer
                Shader.SetGlobalBuffer("_EllipsZones", clearZoneCB);
                return;
            }

            if (removalZoneParams == null || removalZoneParams.Length != count)
                removalZoneParams = new ZoneParams[count];

            int zoneID = 0;
            for (int i = 0; i < removalZones.Count; i++)
            {
                Enviro.EnviroEffectRemovalZone fz = removalZones[i];
                if (fz == null || !fz.enabled || !fz.gameObject.activeSelf)
                    continue;

                Transform t = fz.transform;

                removalZoneParams[zoneID].pos = t.position;
                removalZoneParams[zoneID].radius = fz.radius * fz.radius;
                removalZoneParams[zoneID].axis = -t.up;
                removalZoneParams[zoneID].stretch = 1.0f/fz.stretch - 1.0f;
                removalZoneParams[zoneID].density = fz.density;
                removalZoneParams[zoneID].feather = 1.0f - fz.feather;
                zoneID++;
            }  
            removeZoneParamsCB.SetData(removalZoneParams);
            Shader.SetGlobalBuffer("_EllipsZones", removeZoneParamsCB);
        }

        private void CreateZoneBuffers()
        {
            EnviroHelper.CreateBuffer(ref removeZoneParamsCB, removalZones.Count, Marshal.SizeOf(typeof(ZoneParams)));
            EnviroHelper.CreateBuffer(ref clearZoneCB, 1, 4);
        }

        private void ReleaseZoneBuffers()
        {
            if(removeZoneParamsCB != null)
            EnviroHelper.ReleaseComputeBuffer(ref removeZoneParamsCB);
            if(clearZoneCB != null)
            EnviroHelper.ReleaseComputeBuffer(ref clearZoneCB);
        }

        IEnumerator FirstFrame ()
        {
            yield return 0;
            notFirstFrame = true;
        }

        ///HDRP Section
        public void CreateHDRPVolume ()
        {
#if ENVIRO_HDRP
            if(volumeHDRP == null)
            {
                GameObject volume = new GameObject();
                volume.name = "Enviro Sky and Fog Volume";
                volume.transform.SetParent(transform);
                volume.transform.localPosition = Vector3.zero;
                volumeHDRP = volume.AddComponent<UnityEngine.Rendering.Volume>();
                volumeHDRP.sharedProfile = EnviroHelper.GetDefaultSkyAndFogProfile("Enviro HDRP Sky and Fog Volume");  
                volumeHDRP.priority = 1;
            }
#endif
        }

        private void CheckCameraSetup ()
        {
            //Auto assign camera with the camera tag when camera not set.
            if(Camera == null)
            {
                for (int i = 0; i < Camera.allCameras.Length; i++)
                {
                    if (Camera.allCameras[i].tag == CameraTag)
                    {
                        Camera = Camera.allCameras[i];
                #if !ENVIRO_HDRP || !ENVIRO_URP
                        AddCameraComponents();
                #endif
                    }
                }
            }
        }

        //Events
        private void EventInit()
        {
            if(Time != null)
            {
                OnHourPassed += () => HourPassedInvoke ();
                OnDayPassed += () => DayPassedInvoke ();
                OnYearPassed += () => YearPassedInvoke ();

                OnNightTime += () => NightTimeInvoke ();
                OnDayTime += () => DayTimeInvoke ();
            }
 
            if(Weather != null)
            { 
               OnWeatherChanged += (EnviroWeatherType type) =>  WeatherChangedInvoke ();
            }

            if(Environment != null)
            {
               OnSeasonChanged += (EnviroEnvironment.Seasons season) => SeasonsChangedInvoke ();
            } 
        }

        //Updates manager variables.
        private void UpdateManager ()
        {
            if(Application.isPlaying)
               CheckCameraSetup ();

            if(solarTime > dayNightSwitch)
            {
                if(isNight == true)
                    NotifyIsDay();

                isNight = false;
            }
            else
            {
                if(isNight == false)
                    NotifyIsNight();

                isNight = true;
            }

            //Effect Removal Zones:
            if(Fog != null || Effects != null)
            { 
                CreateZoneBuffers();
                SetupZoneBuffers();
            } 
        }
    }
}
