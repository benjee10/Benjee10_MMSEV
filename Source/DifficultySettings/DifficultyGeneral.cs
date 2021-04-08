using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanetsideExplorationTechnologies.DifficultySettings
{
    public class DifficultyGeneral : GameParameters.CustomParameterNode
    {
        private const string DISPLAYNAME = "#LOC_B10_MMSEV_GeneralSettings_title";
        private const string SECTIONNAME = "#LOC_B10_MMSEV_SettingsSection_title";

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

        [GameParameters.CustomParameterUI("#LOC_B10_MMSEV_Setting_BreakableTurbines_title", toolTip = "#LOC_B10_MMSEV_Setting_BreakableTurbines_tooltip")]
        public bool isBreakable = true;

        [GameParameters.CustomParameterUI("#LOC_B10_MMSEV_Setting_MinWindSpeedRequirements_title", toolTip = "#LOC_B10_MMSEV_Setting_MinWindSpeedRequirements_tooltip")]
        public bool requireMinSpeed = true;

        [GameParameters.CustomParameterUI("#LOC_B10_MMSEV_Setting_RequireFullControl_title", toolTip = "#LOC_B10_MMSEV_Setting_RequireFullControl_tooltip")]
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
