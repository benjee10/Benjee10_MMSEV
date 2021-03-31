using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanetsideExplorationTechnologies.Extensions
{
    public class WaitForTimeWarpSeconds : CustomYieldInstruction
    {
        private double waitTime;
        private double waitUntilTime = -1;

        public WaitForTimeWarpSeconds(double time)
        {
            waitTime = time;
        }

        public override bool keepWaiting
        {
            get
            {
                if (waitUntilTime < 0)
                {
                    waitUntilTime = Planetarium.GetUniversalTime() + waitTime;
                }

                bool wait = Planetarium.GetUniversalTime() < waitUntilTime;
                if (!wait)
                {
                    waitUntilTime = -1;
                    Reset();
                }

                return wait;
            }
        }
    }
}
