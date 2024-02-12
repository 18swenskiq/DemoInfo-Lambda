using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoinfo_lambda
{
    public class DemoParseDescription
    {
        /// <summary>
        /// The name of the demo
        /// </summary>
        public string DemoName { get; set; } = string.Empty;

        /// <summary>
        /// The guild that this demo is from
        /// </summary>
        public string GuildId { get; set; } = string.Empty;

        /// <summary>
        /// The context that the demo is from
        /// </summary>
        public DemoContext DemoContext { get; set; }

        /// <summary>
        /// Optional. The type of the playtest
        /// </summary>
        public PlaytestType PlaytestType { get; set; }
    }

    public enum DemoContext
    {
        Playtest,
        PUG
    }

    public enum PlaytestType
    {
        None,
        TwoVersusTwo,
        FiveVersusFive,
        TenVersusTen
    }
}
