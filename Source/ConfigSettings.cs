using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanetsideExplorationTechnologies
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ConfigSettings : MonoBehaviour
    {
        private const string DISPLAYNAME = "PlanetsideExplorationTechnologies";
        private const string CONFIGNODE = "PlanetsideExplorationTechnologies";

        public Color windColor = Color.white;
        private Vector3 windColorVector;

        public Color orientationColor = Color.white;
        private Vector3 orientationColorVector;

        public bool debug = false;

        public static ConfigSettings Instance { get; private set; }

        public void Awake()
        {
            Instance = this;

            if (GameDatabase.Instance.GetConfigNodes(CONFIGNODE) == null)
            {
                Debug.LogWarning($"[{DISPLAYNAME}] No settings file found");
                return;
            }

            LoadConfigNode();
            Debug.Log($"[{DISPLAYNAME}] Settings loaded");

            DontDestroyOnLoad(this);
        }

        public void LoadConfigNode()
        {
            ConfigNode configNode = GameDatabase.Instance.GetConfigNodes(CONFIGNODE).FirstOrDefault();

            configNode.TryGetValue("windColor", ref windColorVector);
            configNode.TryGetValue("orientationColor", ref orientationColorVector);
            configNode.TryGetValue("debug", ref debug);

            if (windColorVector != null)
                windColor = new Color(windColorVector.x, windColorVector.y, windColorVector.z);

            if (orientationColorVector != null)
                orientationColor = new Color(orientationColorVector.x, orientationColorVector.y, orientationColorVector.z);
        }
    }
}
