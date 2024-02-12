using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoinfo_lambda.Models
{
    public class PlayerDeath
    {
        public PlayerDeath(Player attacker, Player deadPlayer, Player? assister, string weapon, bool isHeadshot)
        {
            Attacker = attacker;
            DeadPlayer = deadPlayer;
            Assister = assister;
            Weapon = weapon;
            IsHeadshot = isHeadshot;
        }

        public Player Attacker { get; set; }

        public Player DeadPlayer { get; set; }

        public Player? Assister { get; set; }

        public string Weapon { get; set; }

        public bool IsHeadshot { get; set; }
    }
}
