using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using PlanetsideExplorationTechnologies.DifficultySettings;
using PlanetsideExplorationTechnologies.Extensions;

namespace PlanetsideExplorationTechnologies.Modules
{
    public class ModulePETTurbine : ModulePETAnimation
    {
        private const string GROUPNAME = "WindTurbine";
        private const string GROUPNAMEDISPLAY = "Wind Turbine";
        private const string MODULENAME = "ModuleDeployableWindTurbine";
        private const int SPEEDCONST = 200;

        [KSPField]
        public string turbineType = string.Empty;

        [KSPField]
        public string turbinePivotName = "pivot";

        [KSPField]
        public string rotationPivotName = string.Empty;

        [KSPField]
        public float turbineSpeedMult = 1.0f;

        [KSPField]
        public float rotationSpeedMult = 1.0f;

        [KSPField]
        public FloatCurve atmEfficiencyCurve = new FloatCurve();

        [KSPField]
        public string resourceName = "ElectricCharge";

        [KSPField]
        public float chargeRate = 1f;

        [KSPField]
        public float minWindSpeed = 0.25f;

        [KSPField]
        public float maxWindTolerance = 3.0f;

        [KSPField]
        public bool showWindDirection = true;

        [KSPField]
        public float spoolUpTime = 1.0f;

        [KSPField(isPersistant = true)]
        private Quaternion defaultRotation;

        [KSPField(isPersistant = true)]
        private Quaternion savedRotation = Quaternion.identity;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Toggle Turbine", guiActiveUnfocused = false, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY),
        UI_Toggle(controlEnabled = true, disabledText = "Off", enabledText = "On", scene = UI_Scene.Flight, requireFullControl = false)]
        public bool isActive;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "EC Output", guiFormat = "N1", guiUnits = " EC/s", guiActiveUnfocused = true, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string flowRateDisplay;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Efficiency", guiFormat = "P1", guiActiveUnfocused = true, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string turbineEfficiencyDisplay;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Current Wind Speed", guiFormat = "P1", guiActiveUnfocused = true, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string windSpeedDisplay;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Current Wind Direction", guiFormat = "N1", guiActiveUnfocused = true,  groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string windDirectionDisplay;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Show Wind Direction", guiActiveUnfocused = false, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY),
        UI_Toggle(controlEnabled = true, disabledText = "Off", enabledText = "On", scene = UI_Scene.Flight, requireFullControl = false)]
        public bool toggleLines;

        [KSPEvent(externalToEVAOnly = true, guiName = "Toggle Turbine")]
        public void ToggleEVATurbine() => ToggleTurbine(null);

        // PlanetsideExplorationTechnologies.cfg -> debug = true
        [KSPEvent(guiActiveEditor = false, guiActive = true, guiName = "Debug: Force Update", guiActiveUnfocused = true)]
        public void ForceWindUpdate()
        {
            PlanetsideExplorationTechnologies.Instance.GenerateWindSpeed();
        }

        [KSPAction(guiName = "Toggle Turbine")]
        public void ToggleTurbine(KSPActionParam param)
        {
            isActive = !isActive;
        }

        [KSPAction(guiName = "Enable Turbine")]
        public void EnableTurbine(KSPActionParam param)
        {
            isActive = true;
        }

        [KSPAction(guiName = "Disable Turbine")]
        public void DisableTurbine(KSPActionParam param)
        {
            isActive = false;
        }

        private Transform turbinePivot;  // Z = rotating axis
        private Transform rotationPivot; // Y = up, Z = front facing

        private Vector3 turbinePivotAxis = new Vector3(0, 0, 1);
        private Vector3 windDirection = Vector3.zero;

        private bool hasRotatingPivot;

        private float windSpeedMult = 0.0f;
        private float windHeading = 0.0f;
        private float turbineSpeed;
        private float efficiencyCurve;
        private float efficiencyAngle;

        private Vector3Renderer currentOrientationLine;
        private Vector3Renderer windDirectionLine;

        public override string GetInfo()
        {
            string str = string.Empty;

            str += $"Minimum Wind Speed: {minWindSpeed * 100} % \n";
            str += $"Rated for: max. {maxWindTolerance * 100} % \n";

            if (!string.IsNullOrEmpty(turbineType))
                str += $"Turbine Type: {turbineType} \n";
            else if (!string.IsNullOrEmpty(rotationPivotName))
                str += $"Turbine Type: Tracking \n";
            else
                str += $"Turbine Type: Static \n";

            str += $"{resHandler.PrintModuleResources()} \n\n";

            if (isBreakable)
                str += $"<color=orange>Can be repaired with {requiredRepairKits} repair kits";

            return str;
        }

        public override string GetModuleDisplayName()
        {
            if (!string.IsNullOrEmpty(animationName))
                return "Deployable Wind Turbine";
            else
                return "Wind Turbine";
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            resHandler.outputResources.Add(new ModuleResource()
            {
                name = resourceName,
                title = KSPUtil.PrintModuleName(resourceName),
                rate = chargeRate,
                id = resourceName.GetHashCode()
            });
        }

        public override void OnAwake()
        {            
            base.OnAwake();

            turbinePivot = part.FindModelTransform(turbinePivotName);
            if (turbinePivot == null)
            {
                Debug.LogError($"[{MODULENAME}] Could not find turbine pivot '{turbinePivotName}' on part '{part.name}' ");
            }

            if (!string.IsNullOrEmpty(rotationPivotName))
            {
                rotationPivot = part.FindModelTransform(rotationPivotName);
                if (rotationPivot == null)
                {
                    Debug.Log($"[{MODULENAME}] Could not find rotation pivot '{rotationPivotName}' on part '{part.name}' ");
                    hasRotatingPivot = false;
                }
                else
                {
                    hasRotatingPivot = true;
                    defaultRotation = rotationPivot.localRotation;
                }
            }
        }

        public void Start()
        {
            if (hasRotatingPivot && savedRotation != Quaternion.identity)
                rotationPivot.localRotation = savedRotation;

            if (deployState == DeployState.RETRACTED)
                Fields["isActive"].guiActive = false;

            if (!showWindDirection)
                Fields["toggleLines"].guiActive = false;

            if (!ConfigSettings.Instance.debug)
                Events["ForceWindUpdate"].guiActive = false;

            UI_Toggle turbineToggle = (UI_Toggle)Fields["isActive"].uiControlFlight;
            turbineToggle.requireFullControl = DifficultyGeneral.Instance.requireFullControl;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (showWindDirection)
                {
                    windDirectionLine = new Vector3Renderer(part, "windDirection", "Current Wind Direction", ConfigSettings.Instance.windColor);
                    currentOrientationLine = new Vector3Renderer(part, "currentOrientation", "Current Orientation", ConfigSettings.Instance.orientationColor);
                }

                onAnimationStart.Add(AnimationStart);
                onAnimationStop.Add(AnimationStop);
            }
        }

        private void AnimationStop(float currentDeploy)
        {
            if (currentDeploy == 1f)
            {
                Fields["isActive"].guiActive = true;
            }
            else if (currentDeploy == 0f)
            {
                turbineSpeed = efficiencyCurve = efficiencyAngle = 0;
                isActive = false;

                if (hasRotatingPivot) 
                    rotationPivot.localRotation = defaultRotation;
            }
        }

        private void AnimationStart(float currentDeploy, float targetDeploy)
        {
            if (targetDeploy == 0f)
            {
                Fields["isActive"].guiActive = false;
            }  
            else if (targetDeploy == 1f)
            {
                // Enable on default
                isActive = true;
            }              
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) 
                return;

            UpdateWind();
            CalculateTracking();
            UpdateTurbine();
            UpdateResourceHandler();
        }

        private void UpdateWind()
        {
            windHeading = !part.WaterContact ? PlanetsideExplorationTechnologies.Instance.WindHeading : 0.0f;
            windSpeedMult = !part.WaterContact ? PlanetsideExplorationTechnologies.Instance.WindSpeedMultiplier : 0.0f;

            windDirection = Quaternion.AngleAxis(windHeading, vessel.upAxis) * vessel.north;
        }

        private void CalculateTracking()
        {
            if (part.WaterContact)
                return;
         
            float angle = Vector3.Angle(windDirection, hasRotatingPivot ? rotationPivot.forward : turbinePivot.forward);
            efficiencyAngle = 1 - (Math.Abs(angle) / 180);

            if (hasRotatingPivot)
            {
                Vector3 windDirectionLocal = rotationPivot.InverseTransformDirection(windDirection);
                Quaternion rotDelta = Quaternion.Euler(0.0f, Mathf.Atan2(windDirectionLocal.x, windDirectionLocal.z), 0.0f);

                switch (deployState)
                {
                    case DeployState.EXTENDED:
                        if (isActive)
                        {
                            rotationPivot.rotation = Quaternion.Lerp(rotationPivot.rotation, rotationPivot.rotation * rotDelta, TimeWarp.fixedDeltaTime * rotationSpeedMult);
                            savedRotation = rotationPivot.localRotation;
                        }
                        break;
                    case DeployState.RETRACTING:
                        rotationPivot.localRotation = Quaternion.Lerp(rotationPivot.localRotation, defaultRotation, (1f - anim[animationName].normalizedTime) * TimeWarp.fixedDeltaTime);
                        break;
                }
            }
        }

        private void UpdateTurbine()
        {
            float localWindSpeed = (float)(vessel.atmDensity * windSpeedMult) * efficiencyAngle;
            
            switch (deployState)
            {
                case DeployState.EXTENDED:
                    if (isActive)
                    {
                        if (localWindSpeed < minWindSpeed)
                        {
                            statusDisplay = $"Too little wind to generate power!";
                            efficiencyCurve = efficiencyAngle = turbineSpeed = 0.0f;
                            break;
                        }

                        turbineSpeed = Math.Abs(Mathf.Lerp(turbineSpeed, localWindSpeed, TimeWarp.fixedDeltaTime * spoolUpTime));
                        efficiencyCurve = turbineSpeed / (float)vessel.atmDensity * atmEfficiencyCurve.Evaluate((float)vessel.atmDensity);
                        turbinePivot.Rotate(turbinePivotAxis * (TimeWarp.fixedDeltaTime * SPEEDCONST) * turbineSpeed * turbineSpeedMult, Space.Self);

                        statusDisplay = $"Generating Power...";

                        Debug.Log($"{turbineSpeed}");

                        if (turbineSpeed > maxWindTolerance)
                        {
                            Destroy();
                            ScreenMessage("orange", "Turbine destroyed by excessive wind speed");
                        }
                    }
                    else
                    {

                        turbineSpeed = Mathf.Lerp(turbineSpeed, 0, TimeWarp.fixedDeltaTime * spoolUpTime);
                        efficiencyCurve = turbineSpeed / (float)vessel.atmDensity * atmEfficiencyCurve.Evaluate((float)vessel.atmDensity);
                        turbinePivot.Rotate(turbinePivotAxis * (TimeWarp.fixedDeltaTime * SPEEDCONST) * turbineSpeed * turbineSpeedMult, Space.Self);

                        if (turbineSpeed < 0.01)
                            statusDisplay = $"Idling - Not generating power";
                    }
                    break;
                case DeployState.RETRACTING:
                    turbineSpeed = Mathf.Lerp(turbineSpeed, 0, (1f - anim[animationName].normalizedTime) * TimeWarp.fixedDeltaTime);
                    efficiencyCurve = turbineSpeed / (float)vessel.atmDensity * atmEfficiencyCurve.Evaluate((float)vessel.atmDensity);
                    turbinePivot.Rotate(turbinePivotAxis * (TimeWarp.fixedDeltaTime * SPEEDCONST) * turbineSpeed * turbineSpeedMult, Space.Self);
                    break;
            }    
        }

        private void UpdateResourceHandler()
        {           
            resHandler.UpdateModuleResourceOutputs(deployState == DeployState.EXTENDED ? efficiencyCurve * efficiencyAngle : 0);
        }

        public override void Update()
        {
            base.Update();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            windDirectionDisplay = Utilities.GetCardinalDirection(windHeading);
            windSpeedDisplay = $"{(float)vessel.atmDensity * windSpeedMult:P1}";

            turbineEfficiencyDisplay = $"{(deployState == DeployState.EXTENDED ? efficiencyCurve * efficiencyAngle : 0):P1}";
            flowRateDisplay = $"{(deployState == DeployState.EXTENDED ? (efficiencyCurve * efficiencyAngle) * chargeRate : 0):N2}";

            if (showWindDirection)
            {
                currentOrientationLine.DrawLine(turbinePivot, turbinePivot.forward, 5, toggleLines);
                windDirectionLine.DrawLine(turbinePivot, windDirection, windSpeedMult * 5, windSpeedMult != 0 ? toggleLines : false);
            }
        }

        public override void Destroy()
        {
            if (deployState == DeployState.BROKEN || !isBreakable || !DifficultyGeneral.Instance.isBreakable)
                return;

            base.Destroy();

            turbineSpeed = efficiencyCurve = efficiencyAngle = 0.0f;

            Fields["isActive"].guiActive = false;
            Fields["toggleLines"].guiActive = false;

            isActive = false;
            toggleLines = false;
        }

        public override void Repair()
        {
            if (!isBreakable && deployState != DeployState.BROKEN)
                return;

            if (!(FlightGlobals.ActiveVessel.evaController.ModuleInventoryPartReference.TotalAmountOfPartStored("evaRepairKit") >= requiredRepairKits))
                return;

            base.Repair();

            Fields["isActive"].guiActive = true;

            if (showWindDirection)
                Fields["toggleLines"].guiActive = true;
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                onAnimationStart.Remove(AnimationStart);
                onAnimationStop.Remove(AnimationStop);
            }
        }
    }
}
