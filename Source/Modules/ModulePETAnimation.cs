using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

using PlanetsideExplorationTechnologies.Extensions;

namespace PlanetsideExplorationTechnologies.Modules
{
    public class ModulePETAnimation : PartModule, IMultipleDragCube
    {
        private const string MODULENAME = "ModulePETAnimation";
      
        [KSPField]
        public string animationName;

        [KSPField]
        public int animationLayer = 1;

        [KSPField]
        public string breakName;

        [KSPField]
        public bool isBreakable = false;

        [KSPField]
        public int requiredRepairKits = 3;

        [KSPField]
        public float impactResistance = 2f;

        [KSPField(isPersistant = true)]
        public float animationSpeed = 1.0f;
      
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_B10_MMSEV_Field_Status")]
        public string statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_Retracted");

        [KSPField(isPersistant = true)]
        public DeployState deployState = DeployState.RETRACTED;

        [KSPField(isPersistant = true)]
        public float savedAnimationTime = 0f;

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_B10_MMSEV_Event_Extend")]
        public void ExtendEvent() => Extend();

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_B10_MMSEV_Event_Retract")]
        public void RetractEvent() => Retract();

        [KSPEvent(externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, guiName = "#LOC_B10_MMSEV_Event_Repair", unfocusedRange = 15f)]
        public void RepairEvent() => Repair();

        [KSPAction("Toggle Deploy", guiName = "#LOC_B10_MMSEV_Action_ToggleDeploy")]
        public void ToggleAction(KSPActionParam param)
        {
            if (deployState == DeployState.RETRACTED) Extend();
            else if (deployState == DeployState.EXTENDED) Retract();
        }

        [KSPAction("Retract", guiName = "#LOC_B10_MMSEV_Action_Retract")]
        public void RetractAction(KSPActionParam param) => Retract();

        [KSPAction("Extend", guiName = "#LOC_B10_MMSEV_Action_Extend")]
        public void ExtendAction(KSPActionParam param) => Extend();

        public enum DeployState
        {
            RETRACTED,
            RETRACTING,
            EXTENDING,
            EXTENDED,
            BROKEN
        }

        public Animation anim;
        private Transform breakTransform;

        private bool hasAnimation = true;
        private bool hasBreakingTransform = false;

        private int physicalObjectTime = 20;

        // Current Deploy Angle - Target Deploy Angle
        public EventData<float, float> onAnimationStart;

        // Current Deploy Angle
        public EventData<float> onAnimationStop;         
        
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            onAnimationStart = new EventData<float, float>(animationName);
            onAnimationStop = new EventData<float>(animationName);

            if (!string.IsNullOrEmpty(breakName))
            {
                breakTransform = part.FindModelTransform(breakName);

                if (breakTransform == null)
                {
                    Debug.LogError($"[{MODULENAME}] Could not find breakTransform '{breakName}' on part '{part.name}' ");
                }
                else
                {
                    hasBreakingTransform = true;
                }
            }

            if (!string.IsNullOrEmpty(animationName))
            {
                anim = part.FindModelAnimator(animationName);
                if (anim == null)
                {
                    Debug.LogError($"[{MODULENAME}] Could not find animation '{animationName}' on part '{part.name}' ");
                    deployState = DeployState.EXTENDED;
                    hasAnimation = false;
                    statusDisplay = "-";
                }
            }
            else
            {
                deployState = DeployState.EXTENDED;
                hasAnimation = false;
                statusDisplay = "-";
            }

            StartFSM();
        }

        private void StartFSM()
        {
            switch (deployState)
            {
                case DeployState.RETRACTING:
                    Events["RetractEvent"].active = false;
                    Events["ExtendEvent"].active = false;
                    Events["RepairEvent"].active = false;
                    break;
                case DeployState.EXTENDING:
                    Events["RetractEvent"].active = false;
                    Events["ExtendEvent"].active = false;
                    Events["RepairEvent"].active = false;
                    break;
                case DeployState.RETRACTED:
                    anim[animationName].wrapMode = WrapMode.ClampForever;
                    anim[animationName].normalizedTime = 0.0f;
                    anim[animationName].enabled = true;
                    anim[animationName].weight = 1.0f;
                    anim.Stop(animationName);
                    Events["RetractEvent"].active = false;
                    Events["ExtendEvent"].active = true;
                    Events["RepairEvent"].active = false;
                    savedAnimationTime = 0.0f;
                    break;
                case DeployState.EXTENDED:
                    if (hasAnimation)
                    {
                        anim[animationName].wrapMode = WrapMode.ClampForever;
                        anim[animationName].normalizedTime = 1.0f;
                        anim[animationName].enabled = true;
                        anim[animationName].weight = 1.0f;
                        Events["ExtendEvent"].active = false;
                        Events["RetractEvent"].active = true;
                    }                 
                    else
                    {
                        Events["ExtendEvent"].active = false;
                        Events["RetractEvent"].active = false;
                        Actions["ToggleAction"].active = false;
                        Actions["RetractAction"].active = false;
                        Actions["ExtendAction"].active = false;
                    }
                    Events["RepairEvent"].active = false;
                    savedAnimationTime = 1.0f;
                    break;
                case DeployState.BROKEN:
                    if (hasAnimation)
                    {
                        anim[animationName].wrapMode = WrapMode.ClampForever;
                        anim[animationName].normalizedTime = savedAnimationTime;
                        anim[animationName].enabled = true;
                        anim[animationName].weight = 1.0f;
                    }

                    Events["ExtendEvent"].active = false;
                    Events["RetractEvent"].active = false;
                    Events["RepairEvent"].active = true;

                    List<Transform> childs = breakTransform.GetComponentsInChildren<Transform>(false).Where(t => t.name != breakName).ToList();
                    foreach (Transform child in childs)
                    {
                        child.gameObject.SetActive(false);
                    }
                    break;
            }
        }

        public void Retract()
        {
            if (deployState == DeployState.BROKEN) 
                return;

            if (anim != null)
            {
                anim[animationName].speed = HighLogic.LoadedSceneIsEditor ? -animationSpeed * 10 : -animationSpeed;
                anim[animationName].normalizedTime = 1.0f;
                anim[animationName].enabled = true;
                anim.Play(animationName);

                deployState = DeployState.RETRACTING;
                Events["RetractEvent"].active = false;
                onAnimationStart.Fire(1f, 0f);
            }
        }

        public void Extend()
        {
            if (deployState == DeployState.BROKEN) return;

            if (anim != null)
            {
                anim[animationName].speed = HighLogic.LoadedSceneIsEditor ? animationSpeed * 10 : animationSpeed;
                anim[animationName].normalizedTime = 0.0f;
                anim[animationName].enabled = true;
                anim.Play(animationName);

                deployState = DeployState.EXTENDING;
                Events["ExtendEvent"].active = false;
                onAnimationStart.Fire(0f, 1f);
            }
        }   

        public virtual void Update()
        {
            if (hasAnimation)
            {
                UpdateFSM();
            }
        }

        private void UpdateFSM()
        {
            switch (deployState)
            {
                case DeployState.EXTENDED:
                    SetDragCubeWeight(1f);
                    break;
                case DeployState.RETRACTED:
                    SetDragCubeWeight(0f);
                    break;
                case DeployState.EXTENDING:
                    if (anim[animationName].normalizedTime >= 1.0)
                    {
                        anim.Stop(animationName);
                        deployState = DeployState.EXTENDED;
                        part.ScheduleSetCollisionIgnores();
                        onAnimationStop.Fire(1f);
                        savedAnimationTime = 1.0f;
                        Events["RetractEvent"].active = true;
                        statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_Extended");
                    }
                    else
                    {
                        statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_Extending");
                    }
                    SetDragCubeWeight(1f - anim[animationName].normalizedTime);
                    break;

                case DeployState.RETRACTING:
                    if (anim[animationName].normalizedTime <= 0.0)
                    {
                        anim.Stop(animationName);
                        deployState = DeployState.RETRACTED;
                        part.ScheduleSetCollisionIgnores();
                        Events["ExtendEvent"].active = true;
                        onAnimationStop.Fire(0f);
                        savedAnimationTime = 0.0f;
                        statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_Retracted");
                    }
                    else
                    {
                        statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_Retracting");
                    }
                    SetDragCubeWeight(1f - anim[animationName].normalizedTime);
                    break;
                case DeployState.BROKEN:
                    statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_Broken");
                    break;
            }
        }

        public virtual void Repair()
        {
            if (!isBreakable && deployState != DeployState.BROKEN)
                return;

            if (!(FlightGlobals.ActiveVessel.evaController.ModuleInventoryPartReference.TotalAmountOfPartStored("evaRepairKit") >= requiredRepairKits)) 
                return;

            List<Transform> childs = breakTransform.GetComponentsInChildren<Transform>(true).Where(t => t.name != breakName).ToList();

            foreach (Transform child in childs)
            {
                child.gameObject.SetActive(true);
            }

            breakTransform.gameObject.SetActive(true);

            if (hasAnimation)
            {
                deployState = savedAnimationTime >= 1.0f ? DeployState.EXTENDED : DeployState.RETRACTED;
                StartFSM();
            }
            else
            {
                deployState = DeployState.EXTENDED;
                StartFSM();
            }

            ScreenMessage("yellow", Localizer.Format("#LOC_B10_MMSEV_Msg_TurbineRepaired", requiredRepairKits));

            FlightGlobals.ActiveVessel.evaController.ModuleInventoryPartReference.RemoveNPartsFromInventory("evaRepairKit", requiredRepairKits);
            Events["RepairEvent"].active = false;

            statusDisplay = $"-";
        }

        public virtual void OnCollisionEnter(Collision collision)
        {
            if (CheatOptions.NoCrashDamage || !isBreakable || deployState == DeployState.BROKEN)
                return;

            if (collision.relativeVelocity.magnitude > impactResistance)
            {
                Destroy();
                ScreenMessage("orange", Localizer.Format("#LOC_B10_MMSEV_Msg_TurbineDestroyed"));
            }           
        }

        public virtual void Destroy()
        {
            if (!isBreakable)
                return;

            if (!hasBreakingTransform || deployState == DeployState.BROKEN || CheatOptions.NoCrashDamage)
                return;

            if (hasAnimation && anim.isPlaying) 
                anim.Stop(animationName);

            GameObject breakGameObject = Instantiate(breakTransform.gameObject, breakTransform.position, breakTransform.rotation, null);
            List<Transform> childs = breakGameObject.GetComponentsInChildren<Transform>(false).Where(t => t.name != breakName).ToList();
            breakTransform.gameObject.SetActive(false);

            foreach (Transform child in childs)
            {
                physicalObject physicBreakObject = physicalObject.ConvertToPhysicalObject(part, child.gameObject);

                Vector3 randomAngularVelocity = new Vector3(UnityEngine.Random.Range(0.0f, 3.5f), UnityEngine.Random.Range(0.0f, 3.5f), UnityEngine.Random.Range(0.0f, 3.5f));
                Vector3 randomVelocity = new Vector3(UnityEngine.Random.Range(0.0f, 3.0f), UnityEngine.Random.Range(0.0f, 3.0f), UnityEngine.Random.Range(0.0f, 3.0f));

                physicBreakObject.rb.mass = 0.02f;
                physicBreakObject.rb.useGravity = false;
                physicBreakObject.rb.velocity = part.Rigidbody.velocity + randomVelocity;

                physicBreakObject.rb.angularVelocity = part.Rigidbody.angularVelocity + randomAngularVelocity;
                physicBreakObject.rb.maxAngularVelocity = PhysicsGlobals.MaxAngularVelocity;
                physicBreakObject.origDrag = 0.02f;

                StartCoroutine(RemovePhysicalObject(child.gameObject));
            }

            deployState = DeployState.BROKEN;
            statusDisplay = Localizer.Format("#LOC_B10_MMSEV_Status_Broken");

            part.ResetCollisions();

            Events["RetractEvent"].active = false;
            Events["ExtendEvent"].active = false;
            Events["RepairEvent"].active = true;
        }

        public void ScreenMessage(string color, string message)
        {
            ScreenMessages.PostScreenMessage($"<color={color}>[{part.partInfo.title}] {message}", 6f, ScreenMessageStyle.UPPER_CENTER);
        }

        private IEnumerator RemovePhysicalObject(GameObject gameObject)
        {
            yield return new WaitForTimeWarpSeconds(physicalObjectTime);
            DestroyImmediate(gameObject);
        }

        // Drag Cube Stuff
        public void SetDragCubeWeight(float time)
        {
            part.DragCubes.SetCubeWeight("EXTENDED", time);
            part.DragCubes.SetCubeWeight("RETRACTED", 1f - time);
        }

        public bool IsMultipleCubesActive { get { return true; } }
        public bool UsesProceduralDragCubes() => false;

        public string[] GetDragCubeNames()
        {
            return new string[]
            {
                "EXTENDED",
                "RETRACTED"
            };
        }

        public void AssumeDragCubePosition(string name)
        {
            Animation anim = GetComponentInChildren<Animation>();

            if (anim != null)
            {
                anim[animationName].speed = 0f;
                anim[animationName].enabled = true;
                anim[animationName].weight = 1f;

                switch (name)
                {
                    case "EXTENDED":
                        anim[animationName].normalizedTime = 1f;
                        break;
                    case "RETRACTED":
                        anim[animationName].normalizedTime = 0f;
                        break;
                }

                Debug.Log($"[{MODULENAME}] Generating Drag Cubes...");
            }
            else
            {
                Debug.Log($"[{MODULENAME}] Drag Cube Generation failed, could not find animator. Continuing...");
            }   
        }
    }
}
