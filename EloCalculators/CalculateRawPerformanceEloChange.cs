using demoinfo_lambda.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoinfo_lambda.EloCalculators
{
    public partial class EloCalculators
    {
        public static Dictionary<ulong, float> CalculateRawPerformanceEloChange(Dictionary<ulong, float> initialElo, List<PlayerDeath> playerDeathEvents)
        {
            Dictionary<ulong, float> updatedElo = new Dictionary<ulong, float>(initialElo);

            Console.WriteLine("Calculating performance elo:");
            foreach (var coolEvent in playerDeathEvents)
            {
                ulong attackerId = coolEvent.Attacker.SteamId;
                ulong victimId = coolEvent.DeadPlayer.SteamId;

                /*
                Console.WriteLine("----------------------");
                Console.WriteLine($"Attacker: {attackerId} ({updatedElo[attackerId]} elo)");
                Console.WriteLine($"Victim: {victimId} ({updatedElo[victimId]} elo)");
                */

                var constant = DetermineEloConstant(updatedElo[attackerId], updatedElo[victimId]);

                var (newRatingAttacker, newRatingDeadPlayer) = EloUtils.GetNewRating(updatedElo[coolEvent.Attacker.SteamId], updatedElo[coolEvent.DeadPlayer.SteamId], constant, false);
                float multiplier = coolEvent.IsHeadshot ? Constants.HeadshotMulitplier : 1.0f;
                var headshotAdjustedAttackerRating = (multiplier * (newRatingAttacker - updatedElo[coolEvent.Attacker.SteamId])) + updatedElo[coolEvent.Attacker.SteamId];
                /*
                Console.WriteLine($"\t\tRating pre-adjustment: {newRatingAttacker}");
                Console.WriteLine($"\t\tRating post-adjustment: {headshotAdjustedAttackerRating}");
                */

                // Calc elo for assists
                // This is done by doing a regular attacking calc but then just dividing the result by our assist constant
                if (coolEvent.Assister != default)
                {
                    var assistConstant = DetermineEloConstant(updatedElo[attackerId], updatedElo[victimId]);
                    var (newAssisterRating, _) = EloUtils.GetNewRating(updatedElo[coolEvent.Assister.SteamId], updatedElo[coolEvent.DeadPlayer.SteamId], (assistConstant * Constants.AssistEloReduction), false);
                    updatedElo[coolEvent.Assister.SteamId] = newAssisterRating;
                }

                /*
                Console.WriteLine($"\tAttacker elo gain: {headshotAdjustedAttackerRating - updatedElo[coolEvent.Attacker.SteamId]}");
                Console.WriteLine($"\tVictim elo loss: {newRatingDeadPlayer - updatedElo[coolEvent.DeadPlayer.SteamId]}");
                */
                var victimEloLoss = Constants.DeathReduction * (newRatingDeadPlayer - updatedElo[coolEvent.DeadPlayer.SteamId]);
                //Console.WriteLine($"\tVictim elo loss: {victimEloLoss}");

                updatedElo[coolEvent.Attacker.SteamId] = headshotAdjustedAttackerRating;
                updatedElo[coolEvent.DeadPlayer.SteamId] = updatedElo[coolEvent.DeadPlayer.SteamId] + victimEloLoss;
            }

            Dictionary<ulong, float> eloChange = new Dictionary<ulong, float>();
            foreach (var update in updatedElo)
            {
                eloChange.Add(update.Key, update.Value - initialElo[update.Key]);
            }

            return eloChange;
        }

        private static float DetermineEloConstant(float attackerRating, float victimRating)
        {
            float diff = -(attackerRating - victimRating);
            // float root = (float)Math.Pow(diff, 1.0 / 20); 

            var from = (0 - Constants.MaximumElo, Constants.MaximumElo);
            var to = (0.5, 5);

            var ret = diff.Remap(from.Item1, from.MaximumElo, (float)to.Item1, to.Item2);

            ret *= Constants.PerformanceStabilization;

            return ret;
        }
    }
}
