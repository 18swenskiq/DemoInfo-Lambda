using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoinfo_lambda.Models
{
    public class ParsedDemo
    {
        public string GuildId { get; set; }
        public List<PlayerDeath> DeathEvents { get; set; }

        public string DemoContext { get; set; }

        public string PlaytestType { get; set; }

        public ParsedDemo(string guildId, List<PlayerDeath> deathEvents, DemoContext demoContext, PlaytestType playtestType)
        {
            GuildId = guildId;
            DeathEvents = deathEvents;
            DemoContext = demoContext.ToString();
            PlaytestType = playtestType.ToString();

        }
    }
}
