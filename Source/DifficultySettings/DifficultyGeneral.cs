using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanetsideExplorationTechnologies.DifficultySettings
{
    public class DifficultyGeneral : GameParameters.CustomParameterNode
    {
        private const string DISPLAYNAME = "General Settings";
        private const string SECTIONNAME = "Planetside Exploration Technologies";

        public override string Title
        {
            get { return DISPLAYNAME; }
        }

        public override string DisplaySection
        {
            get { return SECTIONNAME; }
        }

        public override string Section
        {
            get { return SECTIONNAME; }
        }

        public override int SectionOrder
        {
            get { return 0; }
        }

        public override GameParameters.GameMode GameMode
        {
            get { return GameParameters.GameMode.ANY; }
        }

        public override bool HasPresets
        {
            get { return true; }
        }

        public static DifficultyGeneral Instance
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<DifficultyGeneral>(); }
        }

        [GameParameters.CustomParameterUI("Enable Breakable Turbines", toolTip = "Set to enable breakable turbines through wind")]
        public bool isBreakable = true;

        [GameParameters.CustomParameterUI("Enable Mininimum Wind Speed Requirements", toolTip = "Set to enable minimum wind speed requirements in order for turbines to spin")]
        public bool requireMinSpeed = true;

        [GameParameters.CustomParameterUI("Require Full Control", toolTip = "Set to require full probe core control to start and shutdown turbine")]
        public bool requireFullControll = false;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    isBreakable = false;
                    requireMinSpeed = false;
                    break;
                case GameParameters.Preset.Normal:
                    isBreakable = true;
                    requireMinSpeed = true;
                    break;
                case GameParameters.Preset.Moderate:
                    isBreakable = true;
                    requireMinSpeed = true;
                    break;
                case GameParameters.Preset.Hard:
                    isBreakable = true;
                    requireMinSpeed = true;
                    break;
            }
        }
    }
}
