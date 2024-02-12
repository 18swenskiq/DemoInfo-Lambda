using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoinfo_lambda
{
    public static class EloUtils
    {
        // winner false = Player A wins
        // winner true = Player B wins
        public static (float, float) GetNewRating(float player1Rating, float player2Rating, float constant, bool winner)
        {
            var (probability1Win, probability2Win) = Probability(player1Rating, player2Rating);

            // Player 1 wins
            if (winner == false)
            {
                float newRatingPlayer1 = NewRating(player1Rating, constant, probability1Win, true);
                float newRatingPlayer2 = NewRating(player2Rating, constant, probability2Win, false);
                return (newRatingPlayer1, newRatingPlayer2);
            }
            else
            {
                float newRatingPlayer1 = NewRating(player1Rating, constant, probability1Win, false);
                float newRatingPlayer2 = NewRating(player2Rating, constant, probability2Win, true);
                return (newRatingPlayer1, newRatingPlayer2);
            }
        }

        private static (float, float) Probability(float player1Rating, float player2Rating)
        {
            float probability2Win = (float)(1.0f * 1.0f / (1 + 1.0f * Math.Pow(10, 1.0f * (player1Rating - player2Rating) / 400)));
            float probability1Win = (float)(1.0f * 1.0f / (1 + 1.0f * Math.Pow(10, 1.0f * (player2Rating - player1Rating) / 400)));
            return (probability1Win, probability2Win);
        }

        private static float NewRating(float rating, float constant, float probabilityOfWin, bool won)
        {
            float wonNum = won ? 1.0f : 0.0f;
            return (rating + constant * (wonNum - probabilityOfWin));
        }
    }
}
