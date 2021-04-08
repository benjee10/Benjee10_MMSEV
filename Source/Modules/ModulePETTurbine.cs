using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

using PlanetsideExplorationTechnologies.DifficultySettings;
using PlanetsideExplorationTechnologies.Extensions;

namespace PlanetsideExplorationTechnologies.Modules
{
    public class ModulePETTurbine : ModulePETAnimation
    {
        private const string GROUPNAME = "WindTurbine";
        private const string GROUPNAMEDISPLAY = "#LOC_B10_MMSEV_Turbine_GroupDisplayName";
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

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "#LOC_B10_MMSEV_Field_ToggleTurbine", guiActiveUnfocused = false, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY),
        UI_Toggle(controlEnabled = true, disabledText = "#LOC_B10_MMSEV_Status_Off", enabledText = "#LOC_B10_MMSEV_Status_On", scene = UI_Scene.Flight, requireFullControl = false)]
        public bool isActive;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_B10_MMSEV_Field_ECOutput", guiFormat = "N1", guiUnits = "#LOC_B10_MMSEV_Field_ECOutput_units", guiActiveUnfocused = true, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string flowRateDisplay;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_B10_MMSEV_Field_Efficiency", guiFormat = "P1", guiActiveUnfocused = true, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string turbineEfficiencyDisplay;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_B10_MMSEV_Field_CurrentWindSpeed", guiFormat = "P1", guiActiveUnfocused = true, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string windSpeedDisplay;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_B10_MMSEV_Field_CurrentWindDirection", guiFormat = "N1", guiActiveUnfocused = true,  groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY)]
        public string windDirectionDisplay;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "#LOC_B10_MMSEV_Field_ShowWindDirection", guiActiveUnfocused = false, groupName = GROUPNAME, groupDisplayName = GROUPNAMEDISPLAY),
        UI_Toggle(controlEnabled = true, disabledText = "#LOC_B10_MMSEV_Status_Off", enabledText = "#LOC_B10_MMSEV_Status_On", scene = UI_Scene.Flight, requireFullControl = false)]
        public bool toggleLines;

        [KSPEvent(externalToEVAOnly = true, guiName = "#LOC_B10_MMSEV_Field_ToggleTurbine")]
        public void ToggleEVATurbine() => ToggleTurbine(null);

        // PlanetsideExplorationTechnologies.cfg -> debug = true
        [KSPEvent(guiActiveEditor = false, guiActive = true, guiName = "#LOC_B10_MMSEV_Event_DebugForceUpdate", guiActiveUnfocused = true)]
        public void ForceWindUpdate()
        {
            PlanetsideExplorationTechnologies.Instance.GenerateWindSpeed();
        }

        [KSPAction(guiName = "#LOC_B10_MMSEV_Action_ToggleTurbine")]
        public void ToggleTurbine(KSPActionParam param)
        {
            isActive = !isActive;
        }

        [KSPAction(guiName = "#LOC_B10_MMSEV_Action_EnableTurbine")]
        public void EnableTurbine(KSPActionParam param)
        {
            isActive = true;
        }

        [KSPAction(guiName = "#LOC_B10_MMSEV_Action_DisableTurbine")]
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
        private float trueEfficiencyAngle;

        private Vector3Renderer currentOrientationLine;
        private Vector3Renderer windDirectionLine;

        public override string GetInfo()
        {
            string str = string.Empty;

            str += Localizer.Format("#LOC_B10_MMSEV_Info_MinimumWindSpeed", minWindSpeed * 100) + "\n";
            str += Localizer.Format("#LOC_B10_MMSEV_Info_RatedForMaxWindTolerance", maxWindTolerance * 100) + "\n";
            if (!string.IsNullOrEmpty(turbineType))
                str += Localizer.Format("#LOC_B10_MMSEV_Info_TurbineType", turbineType) + "\n";
            else if (!string.IsNullOrEmpty(rotationPivotName))
                str += Localizer.Format("#LOC_B10_MMSEV_Info_TurbineTypeTracking") + "\n";
            else
                str += Localizer.Format("#LOC_B10_MMSEV_Info_TurbineTypeStatic") + "\n";

            str += $"{resHandler.PrintModuleResources()} \n\n";

            if (isBreakable)
                str += "<color=orange>" + Localizer.Format("#LOC_B10_MMSEV_Info_CanBeRepairedWith", requiredRepairKits);

            return str;
        }

        public override string GetModuleDisplayName()
        {
            if (!string.IsNullOrEmpty(animationName))
                return Localizer.Format("#LOC_B10_MMSEV_Info_DeployableWindTurbine");
            else
                return Localizer.Format("#LOC_B10_MMSEV_Info_WindTurbine");
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
            turbineToggle.requireFullControl = DifficultyGeneral.Instance.requireFullControll;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (showWindDirection)
                {
                    windDirectionLine = new Vector3Renderer(part, "windDirection", Localizer.Format("#LOC_B10_MMSEV_Vector_CurrentWindDirection"), ConfigSettings.Instance.windColor);
                    currentOrientationLine = new Vector3Renderer(part, "currentOrientation", Localizer.Format("#LOC_B10_MMSEV_Vector_CurrentOrientation"), ConfigSettings.Instance.orientationColor);
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
         
            efficiencyAngle = Vector3.Angle(windDirection, hasRotatingPivot ? rotationPivot.forward : turbinePivot.forward);
            trueEfficiencyAngle = 1 - (Math.Abs(efficiencyAngle) / 180);

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
            float localWindSpeed = (float)(vessel.atmDensity * windSpeedMult) * trueEfficiencyAngle;

            switch (deployState)
            {
                case DeployState.EXTENDED:
                    if (isActive)
                    {
                        if (localWindSpeed < minWindSpeed)
                        {
                            statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_TooLittleWind");
                            efficiencyCurve = efficiencyAngle = 0.0f;
                            break;
                        }

                        turbineSpeed = Math.Abs(Mathf.Lerp(turbineSpeed, localWindSpeed, TimeWarp.fixedDeltaTime * spoolUpTime));
                        efficiencyCurve = turbineSpeed / (float)vessel.atmDensity * atmEfficiencyCurve.Evaluate((float)vessel.atmDensity);
                        turbinePivot.Rotate(turbinePivotAxis * (TimeWarp.fixedDeltaTime * SPEEDCONST) * turbineSpeed * turbineSpeedMult, Space.Self);

                        statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_GeneratingPower");

                        if (turbineSpeed > maxWindTolerance)
                        {
                            Destroy();
                            ScreenMessage("orange", Localizer.Format("#LOC_B10_MMSEV_Msg_TurbineDestroyedByWind"));
                        }
                    }
                    else
                    {
                        if (localWindSpeed < minWindSpeed)
                        {
                            statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_TooLittleWind");
                            efficiencyCurve = efficiencyAngle = 0.0f;
                            break;
                        }

                        turbineSpeed = Mathf.Lerp(turbineSpeed, 0, TimeWarp.fixedDeltaTime * spoolUpTime);
                        efficiencyCurve = Mathf.Lerp(efficiencyCurve, 0, TimeWarp.fixedDeltaTime);
                        turbinePivot.Rotate(turbinePivotAxis * (TimeWarp.fixedDeltaTime * SPEEDCONST) * turbineSpeed * turbineSpeedMult, Space.Self);
                    }
                    break;
                case DeployState.RETRACTING:
                    turbineSpeed = Mathf.Lerp(turbineSpeed, 0, (1f - this.anim[this.animationName].normalizedTime) * TimeWarp.fixedDeltaTime);
                    efficiencyCurve = Mathf.Lerp(efficiencyCurve, 0, (1f - this.anim[this.animationName].normalizedTime) * TimeWarp.fixedDeltaTime);
                    turbinePivot.Rotate(turbinePivotAxis * (TimeWarp.fixedDeltaTime * SPEEDCONST) * turbineSpeed * turbineSpeedMult, Space.Self);
                    break;
            }
        }

        private void UpdateResourceHandler()
        {           
            resHandler.UpdateModuleResourceOutputs(deployState == DeployState.EXTENDED ? efficiencyCurve * trueEfficiencyAngle : 0);
        }

        public override void Update()
        {
            base.Update();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            windDirectionDisplay = Utilities.GetCardinalDirection(windHeading);
            windSpeedDisplay = $"{(float)vessel.atmDensity * windSpeedMult:P1}";


            turbineEfficiencyDisplay = $"{(deployState == DeployState.EXTENDED ? efficiencyCurve * trueEfficiencyAngle : 0):P1}";
            flowRateDisplay = (deployState == DeployState.EXTENDED ? $"{(efficiencyCurve * trueEfficiencyAngle * chargeRate):N2} " : "0 ");
//            flowRateDisplay = $"{(deployState == DeployState.EXTENDED ? (efficiencyCurve * trueEfficiencyAngle) * chargeRate : 0):N2}";

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
