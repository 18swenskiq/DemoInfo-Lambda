using demoinfo_lambda.Models;
using SharpYaml.Serialization;
using System.ComponentModel;

namespace demoinfo_lambda
{
    public static class PlayerDeathEventsSerialization
    {
        public static string Serialize(List<PlayerDeath> events, string guildId, DemoContext demoContext, PlaytestType playtestType)
        {
            var obj = new ParsedDemo(guildId, events, demoContext, playtestType);
            Console.WriteLine("Serializing object");
            Console.WriteLine(guildId);
            Console.WriteLine(events);
            Console.WriteLine(demoContext);
            Console.WriteLine(playtestType);


            foreach (var coolEvent in obj.DeathEvents)
            {
                //Console.WriteLine("Printing event...");
                foreach (PropertyDescriptor desc in TypeDescriptor.GetProperties(coolEvent))
                {
                    string name = desc.Name;
                    object value = desc.GetValue(coolEvent);

                    if (name == "Attacker" || name == "DeadPlayer" || name == "Weapon" || name == "IsHeadshot")
                    {
                        Console.WriteLine("{0} = {1}", name, value);
                        //if (value == null)
                        //{
                        //    Console.WriteLine("Found unexpected null value");
                        //    Console.WriteLine("{0} = {1}", name, value);
                        //}
                    }
                    // Console.WriteLine("{0} = {1}", name, value);
                }
            }

            var serializer = new Serializer();
            var text = serializer.Serialize(obj);
            Console.WriteLine("Serialization Result:");
            Console.WriteLine(text);
            return text;
        }

        public static ParsedDemo? Deserialize(string ymlString)
        {
            var serializer = new Serializer();
            List<PlayerDeath> retList = new List<PlayerDeath>();
            ParsedDemo? parsedDemo = default;
            serializer.Deserialize(ymlString, parsedDemo);
            return parsedDemo;
        }
    }
}
