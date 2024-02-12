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
        // 2 Winner = Terrorists
        // 3 Winner = Counter-Terrorists
        public static Dictionary<ulong, float> CalculateWinMatchEloChange(Dictionary<ulong, float> initialElo, List<ulong> ctTeam, List<ulong> tTeam, int winner)
        {
            Dictionary<ulong, float> updatedElo = new Dictionary<ulong, float>(initialElo);

            List<ulong> winningTeam;
            List<ulong> losingTeam;

            // Check all players in winning or losing team, and ensure that they are in the initial elo list.
            // If a player is not in the list, they have no recorded kills or deaths

            if (winner == 2)
            {
                winningTeam = new List<ulong>(tTeam.Where(t => initialElo.ContainsKey(t)));
                losingTeam = new List<ulong>(ctTeam.Where(ct => initialElo.ContainsKey(ct)));
            }
            else if (winner == 3)
            {
                winningTeam = new List<ulong>(ctTeam.Where(ct => initialElo.ContainsKey(ct)));
                losingTeam = new List<ulong>(tTeam.Where(t => initialElo.ContainsKey(t)));
            }
            else
            {
                throw new Exception("Last round not won by either team");
            }


            float winningTeamAvgElo = winningTeam.Average(w => initialElo[w]);
            float losingTeamAvgElo = losingTeam.Average(l => initialElo[l]);

            foreach (var winPlayer in winningTeam)
            {
                var (winRating, _) = EloUtils.GetNewRating(initialElo[winPlayer], losingTeamAvgElo, Constants.WinningWeight, false);
                updatedElo[winPlayer] = winRating;
            }

            foreach (var losePlayer in losingTeam)
            {
                var (_, lossRating) = EloUtils.GetNewRating(winningTeamAvgElo, initialElo[losePlayer], Constants.WinningWeight, false);
                updatedElo[losePlayer] = lossRating;
            }

            Dictionary<ulong, float> eloChange = new Dictionary<ulong, float>();
            foreach (var update in updatedElo)
            {
                eloChange.Add(update.Key, update.Value - initialElo[update.Key]);
            }

            return eloChange;
        }
    }
}
