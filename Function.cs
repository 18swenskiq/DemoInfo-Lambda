using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.S3.Model;
using DemoFile;
using demoinfo_lambda.Models;
using System.Text.Json.Serialization;

namespace demoinfo_lambda;

public class Function
{
    /// <summary>
    /// The main entry point for the Lambda function. The main function is called once during the Lambda init phase. It
    /// initializes the .NET Lambda runtime client passing in the function handler to invoke for each Lambda event and
    /// the JSON serializer to use for converting Lambda JSON format to the .NET types. 
    /// </summary>
    private static async Task Main()
    {
        Func<DemoParseDescription, ILambdaContext, Task<string>> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    public static List<Player> players = new List<Player>();
    public static List<PlayerDeath> playerDeathEvents = new List<PlayerDeath>();
    public static List<ulong> lastCTTeam = new List<ulong>();
    public static List<ulong> lastTTeam = new List<ulong>();
    public static int lastRoundWinner = -1;

    public static Dictionary<ulong, float> playerElo = new Dictionary<ulong, float>();

    public static AmazonS3Client S3Client;

    /// <summary>
    /// A simple function that takes a string and does a ToUpper.
    ///
    /// To use this handler to respond to an AWS event, reference the appropriate package from 
    /// https://github.com/aws/aws-lambda-dotnet#events
    /// and change the string input parameter to the desired event type. When the event type
    /// is changed, the handler type registered in the main method needs to be updated and the LambdaFunctionJsonSerializerContext 
    /// defined below will need the JsonSerializable updated. If the return type and event type are different then the 
    /// LambdaFunctionJsonSerializerContext must have two JsonSerializable attributes, one for each type.
    ///
    // When using Native AOT extra testing with the deployed Lambda functions is required to ensure
    // the libraries used in the Lambda function work correctly with Native AOT. If a runtime 
    // error occurs about missing types or methods the most likely solution will be to remove references to trim-unsafe 
    // code or configure trimming options. This sample defaults to partial TrimMode because currently the AWS 
    // SDK for .NET does not support trimming. This will result in a larger executable size, and still does not 
    // guarantee runtime trimming errors won't be hit. 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<string> FunctionHandler(DemoParseDescription input, ILambdaContext context)
    {
        Console.WriteLine("Building Request");
        RegionEndpoint bucketRegion = RegionEndpoint.USEast2;
        S3Client = new AmazonS3Client(bucketRegion);

        string keyName = $"Demos/{input.DemoName}.dem";

        Console.WriteLine($"Getting key: {keyName}");

        var objectRequest = new GetObjectRequest
        {
            BucketName = "squidbot",
            Key = keyName,
        };

        var response = await S3Client.GetObjectAsync(objectRequest);

        var demo = new DemoParser();
        AttachDemoEvents(demo);

        await demo.Start(response.ResponseStream);

        Random rand = new Random();

        await PopulatePlayerElo(players);

        // TODO: Fix exploit that can be caused by a player leaving the match before the end of the last round
        // IDEA: Keep a running total of the number of rounds won/lost by each player

        var perfEloChanges = EloCalculators.EloCalculators.CalculateRawPerformanceEloChange(playerElo, playerDeathEvents);
        var resultEloChanges = EloCalculators.EloCalculators.CalculateWinMatchEloChange(playerElo, lastCTTeam, lastTTeam, lastRoundWinner);

        foreach (var player in playerElo)
        {
            var perfEloChange = perfEloChanges[player.Key];
            var resultEloChange = resultEloChanges[player.Key];

            // TODO: If a player went positive, make them unable to lose elo on performance

            var kills = playerDeathEvents.Count(e => e.Attacker.SteamId == player.Key);
            var deaths = playerDeathEvents.Count(e => e.DeadPlayer.SteamId == player.Key);
            var assists = playerDeathEvents.Count(e => e.Assister?.SteamId == player.Key);

            bool winGame = (lastRoundWinner == 2 && lastTTeam.Contains(player.Key)) || (lastRoundWinner == 3 && lastCTTeam.Contains(player.Key));
            var resultText = winGame ? "Win" : "Loss";

            /*
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine($"Player: {player.Key} - Initial Elo: {player.Value}");
            Console.WriteLine($"\tK/D/A: {kills}/{deaths}/{assists} ({perfEloChange} elo)");
            Console.WriteLine($"\tResult: {resultText} ({resultEloChange} elo)");
            Console.WriteLine($"\tElo Adjustment: {player.Value} -> {perfEloChange + resultEloChange + player.Value}");
            */

            Console.WriteLine($"\tElo Adjustment: {player.Value} -> {perfEloChange + resultEloChange + player.Value}");
            var updateRequest = new PutObjectRequest
            {
                BucketName = "squidbot",
                Key = $"EloDatabase/{player.Key}.txt",
                ContentType = "text/plain",
                ContentBody = $"{perfEloChange + resultEloChange + player.Value}"
            };
            await S3Client.PutObjectAsync(updateRequest);
        }

        // Add all playerdeath events to a json file so we can reparse later if necessary
        string ymlString = PlayerDeathEventsSerialization.Serialize(playerDeathEvents, input.GuildId, input.DemoContext, input.PlaytestType);
        var putParsedEventRequest = new PutObjectRequest
        {
            BucketName = "squidbot",
            Key = $"ParsedDemoEvents/{input.DemoName}.yml",
            ContentType = "text/plain",
            ContentBody = ymlString
        };
        await S3Client.PutObjectAsync(putParsedEventRequest);

        Console.WriteLine("\nFinished!");

        return $"cool";
    }

    public static void AttachDemoEvents(DemoParser demo)
    {
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            if (e.Attacker == null)
            {
                Console.WriteLine("Null attacker detected, skipping");
                return;
            }

            if (e.Player == null)
            {
                Console.WriteLine("Null dead player detected, skipping");
                return;
            }

            Player attacker = players.SingleOrDefault(x => x.SteamId == e.Attacker.SteamID) ?? new Player(e.Attacker.PlayerName, e.Attacker.SteamID);
            Player deadPlayer = players.SingleOrDefault(x => x.SteamId == e.Player.SteamID) ?? new Player(e.Player.PlayerName, e.Player.SteamID);
            Player? assister = null;

            if (e.Assister != default)
            {
                assister = players.SingleOrDefault(x => x.SteamId == e.Assister.SteamID) ?? new Player(e.Assister.PlayerName, e.Assister.SteamID);
            }

            // add players, ensure no duplicates
            if (!players.Select(x => x.SteamId).Contains(attacker.SteamId))
            {
                players.Add(attacker);
            }

            if (!players.Select(x => x.SteamId).Contains(deadPlayer.SteamId))
            {
                players.Add(deadPlayer);
            }

            if (e.Assister != default && !players.Select(x => x.SteamId).Contains(assister!.SteamId))
            {
                players.Add(assister);
            }

            playerDeathEvents.Add(new PlayerDeath(attacker, deadPlayer, assister, e.Weapon, e.Headshot));
        };

        demo.Source1GameEvents.RoundEnd += f =>
        {
            Source1RoundEndEvent test = f;

            lastRoundWinner = test.Winner;

            lastCTTeam = new List<ulong>();
            foreach (var item in demo.TeamCounterTerrorist.CSPlayers)
            {
                lastCTTeam.Add(item.OriginalController!.SteamID);
            }

            lastTTeam = new List<ulong>();
            foreach (var item in demo.TeamTerrorist.CSPlayers)
            {
                lastTTeam.Add(item.OriginalController!.SteamID);
            }
        };
    }

    public static async Task PopulatePlayerElo(List<Player> players)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = "squidbot",
            Prefix = "EloDatabase/"
        };

        var response = await S3Client.ListObjectsV2Async(request);
        
        foreach (var player in players)
        {
            var existingS3Obj = response.S3Objects.Find(s => s.Key.Contains(player.SteamId.ToString()));

            // Case: This elo doesn't exist. Create it and populate with the default elo
            if (existingS3Obj == null)
            {
                // Add to S3
                var putRequest = new PutObjectRequest
                {
                    BucketName = "squidbot",
                    Key = $"EloDatabase/{player.SteamId}.txt",
                    ContentType = "text/plain",
                    ContentBody = Constants.DefaultElo.ToString()
                };
                await S3Client.PutObjectAsync(putRequest);

                // Add to list
                Console.WriteLine($"Creating new elo entry for {player.SteamId}: {Constants.DefaultElo}");
                playerElo.Add(player.SteamId, Constants.DefaultElo);
            }
            // Case: Elo does exist. Pull it from S3
            else
            {
                // Get from S3
                var getRequest = new GetObjectRequest
                {
                    BucketName = existingS3Obj.BucketName,
                    Key = existingS3Obj.Key,
                };
                var eloObj = await S3Client.GetObjectAsync(getRequest);

                // Parse and add to list
                var buffer = new byte[128];
                await eloObj.ResponseStream.ReadAsync(buffer);
                var bufferResult = System.Text.Encoding.Default.GetString(buffer);

                Console.WriteLine($"Retrieved buffer: {bufferResult}, adding to existing elo list for {player.SteamId}");

                playerElo.Add(player.SteamId, Single.Parse(bufferResult));
            }
        }
    }
}

/// <summary>
/// This class is used to register the input event and return type for the FunctionHandler method with the System.Text.Json source generator.
/// There must be a JsonSerializable attribute for each type used as the input and return type or a runtime error will occur 
/// from the JSON serializer unable to find the serialization information for unknown types.
/// </summary>
[JsonSerializable(typeof(DemoParseDescription))]
public partial class LambdaFunctionJsonSerializerContext : JsonSerializerContext
{
    // By using this partial class derived from JsonSerializerContext, we can generate reflection free JSON Serializer code at compile time
    // which can deserialize our class and properties. However, we must attribute this class to tell it what types to generate serialization code for.
    // See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation
}