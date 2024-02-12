using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoinfo_lambda.Models
{
    public class Player
    {
        public Player(string nickname, ulong steamId)
        {
            Nickname = nickname;
            SteamId = steamId;
        }

        public string Nickname { get; set; }

        public ulong SteamId { get; set; }
    }
}
