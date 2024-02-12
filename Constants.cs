using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoinfo_lambda
{
    public static class Constants
    {
        public const float AssistEloReduction = 0.15f;

        public const float DeathReduction = 0.65f;

        public const float DefaultElo = 1200.0f;

        public const float HeadshotMulitplier = 1.005f;

        public const float MaximumElo = 3000.0f;

        // This is used for "normalizing" the elo gain in performance
        public const float PerformanceStabilization = 0.20f;

        public const float WinningWeight = 27.0f;
    }
}
