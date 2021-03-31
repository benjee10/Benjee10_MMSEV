using System;
using System.Collections;
using System.Linq;
using UnityEngine;

using PlanetsideExplorationTechnologies.DifficultySettings;
using PlanetsideExplorationTechnologies.Extensions;

namespace PlanetsideExplorationTechnologies
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PlanetsideExplorationTechnologies : MonoBehaviour
    {
        private const string DISPAYNAME = "PlanetsideExplorationTechnologies";

        private float totalProbability;
        private float probabilityHighWinds;
        private float probabilityMidWinds;
        private float probabilityLowWinds;
        private float probabilityNoWinds;

        private TimeSpan windInterval;

        private float windSpeed;
        private float windHeading;

        // For Future Integration with Kerbal Wind and Kerbal Weather Project
        public static bool useKerbalWind = false;
        public static bool useKerbalWeatherProject = false;
        public static bool useKerbalWeatherProjectLite = false;

        public static PlanetsideExplorationTechnologies Instance { get; private set; }

        public float WindSpeedMultiplier
        {
            get { return windSpeed; }
        }

        public float WindHeading
        {
            get { return windHeading; }
        }

        public void Start()
        {
            Instance = this;

            CacheSettings();

            StartCoroutine(DelayedUpdate());
            StartCoroutine(LateStart());

            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
        }

        private void CacheSettings()
        {
            windInterval = TimeSpan.FromHours(DifficultyGeneralWind.Instance.windInterval);

            probabilityHighWinds = DifficultyWindProbability.Instance.probabilityHighWinds;
            probabilityMidWinds = DifficultyWindProbability.Instance.probabilityMidWinds;
            probabilityLowWinds = DifficultyWindProbability.Instance.probabilityLowWinds;
            probabilityNoWinds = DifficultyWindProbability.Instance.probabilityNoWinds;

            totalProbability = probabilityHighWinds + probabilityMidWinds + probabilityLowWinds + probabilityNoWinds;

            if (ConfigSettings.Instance.debug)
            {
                Debug.Log($"[{DISPAYNAME}] Cached Settings: Wind Interval: {windInterval}," +
                                          $" Probability (High-, Mid-, Low-, No Wind) {probabilityHighWinds}," +
                                          $" {probabilityMidWinds} {probabilityLowWinds}, {probabilityNoWinds}");
            }             
        }

        private void OnGameSettingsApplied() => CacheSettings();

        private IEnumerator DelayedUpdate()
        {
            while (true)
            {
                yield return new WaitForTimeWarpSeconds(windInterval.TotalSeconds);
                GenerateWindSpeed();
            }
        }

        private IEnumerator LateStart()
        {
            yield return null;
            GenerateWindSpeed();
        }

        public void GenerateWindSpeed()
        {
            float probabilityWinds = UnityEngine.Random.Range(0.0f, totalProbability);
            windHeading = UnityEngine.Random.Range(0f, 360f);

            if ((probabilityWinds -= probabilityHighWinds) < 0)
            {
                windSpeed = UnityEngine.Random.Range(1.1f, 1.8f);
            }
            else if ((probabilityWinds -= probabilityMidWinds) < 0)
            {
                windSpeed = UnityEngine.Random.Range(0.9f, 1.05f);
            }
            else if ((probabilityWinds -= probabilityLowWinds) < 0)
            {
                windSpeed = UnityEngine.Random.Range(0.5f, 0.8f);
            }
            else
            {
                windSpeed = 0;
            }

            if (ConfigSettings.Instance.debug) 
                Debug.Log($"[{DISPAYNAME}] Wind Update; Speed: {windSpeed}, Heading: {windHeading}");
        }

        /*// Kerbal Wind by Butcher
        public bool VerifyKerbalWindAPI()
        {
            AssemblyLoader.LoadedAssembly KerbalWeatherProject = AssemblyLoader.loadedAssemblies.SingleOrDefault(a => a.dllName == "KerbalWind");

            if (KerbalWeatherProject != null)
            {
                useKerbalWeatherProjectLite = true;
                return KerbalWindWrapper.InitKWWrapper();
            }
            else return false;
        }*/
    }
}
