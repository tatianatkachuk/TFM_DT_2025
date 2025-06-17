using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.DigitalTwins.Parser;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace SampleClientApp
{
    public class Program
    {
        private static DigitalTwinsClient client;

        static async Task Main()
        {
            Uri adtInstanceUrl;
            try
            {
                // Read configuration data from the appsettings.json file
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();
                adtInstanceUrl = new Uri(config["instanceUrl"]);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is UriFormatException)
            {
                Log.Error($"Could not read configuration. Have you configured your ADT instance URL in appsettings.json?\n\nException message: {ex.Message}");
                return;
            }

            Log.Ok("Authenticating...");
            var credential = new DefaultAzureCredential();
            client = new DigitalTwinsClient(adtInstanceUrl, credential);

            Log.Ok($"Service client created – ready to go");
            Log.Ok("Loading models...");
            
            await LoadModels();

            await BuildGraph();
            
        }

        public static async Task LoadModels()
        {
            string directory = "Models";

            string extension = "json";
            DirectoryInfo dinfo;
            dinfo = new DirectoryInfo(directory);
            Log.Alert($"Loading *.{extension} files in folder '{dinfo.FullName}'");
            if (dinfo.Exists == false)
            {
                Log.Error($"Specified directory '{directory}' does not exist: Exiting...");
                return;
            }
            else
            {
                var files = dinfo.EnumerateFiles($"*.{extension}");
                if (files.Count() == 0)
                {
                    Log.Alert("No model files found.");
                    return;
                }
                Dictionary<FileInfo, string> modelDict = new Dictionary<FileInfo, string>();
                int count = 0;
                string lastFile = "<none>";
                try
                {
                    foreach (FileInfo fi in files)
                    {
                        string dtdl = File.ReadAllText(fi.FullName);
                        modelDict.Add(fi, dtdl);
                        lastFile = fi.FullName;
                        count++;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Could not read files. \nLast file read: {lastFile}\nError: \n{e.Message}");
                    return;
                }
                Log.Ok($"Read {count} files from specified directory");
                int errJson = 0;
                foreach (FileInfo fi in modelDict.Keys)
                {
                    modelDict.TryGetValue(fi, out string dtdl);
                    try
                    {
                        JsonDocument.Parse(dtdl);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Invalid json found in file {fi.FullName}.\nJson parser error \n{e.Message}");
                        errJson++;
                    }
                }
                if (errJson > 0)
                {
                    Log.Error($"\nFound  {errJson} Json parsing errors");
                    return;
                }
                Log.Ok($"Validated JSON for all files - now validating DTDL");
                var modelList = modelDict.Values.ToList<string>();
                var parser = new ModelParser();
                try
                {
                    IReadOnlyDictionary<Dtmi, DTEntityInfo> om = await parser.ParseAsync(modelList);
                    Log.Out("");
                    Log.Ok($"**********************************************");
                    Log.Ok($"** Validated all files - Your DTDL is valid **");
                    Log.Ok($"**********************************************");
                    Log.Out($"Found a total of {om.Keys.Count()} entities in the DTDL");

                    try
                    {
                        await client.CreateModelsAsync(modelList);
                        Log.Ok($"**********************************************");
                        Log.Ok($"** Models uploaded successfully **************");
                        Log.Ok($"**********************************************");
                    }
                    catch (RequestFailedException ex)
                    {
                        Log.Error($"*** Error uploading models: {ex.Status}/{ex.ErrorCode}");
                        return;
                    }
                }
                catch (ParsingException pe)
                {
                    Log.Error($"*** Error parsing models");
                    int derrcount = 1;
                    foreach (ParsingError err in pe.Errors)
                    {
                        Log.Error($"Error {derrcount}:");
                        Log.Error($"{err.Message}");
                        Log.Error($"Primary ID: {err.PrimaryID}");
                        Log.Error($"Secondary ID: {err.SecondaryID}");
                        Log.Error($"Property: {err.Property}\n");
                        derrcount++;
                    }
                    return;
                }
            }
        }

        private static async Task BuildGraph()
        {
            Log.Out($"Creating SpaceModel and Thermostat...");
            await CreateDigitalTwin(new string[15]
                {
                    "CreateTwin", "dtmi:contosocom:DigitalTwins:Space;1", "floor1",
                    "DisplayName", "string", "Floor 1",
                    "Location", "string", "Puget Sound",
                    "Temperature", "double", "0",
                    "ComfortIndex", "double", "0"
                });
            await CreateDigitalTwin(new string[15]
                {
                    "CreateTwin", "dtmi:contosocom:DigitalTwins:Space;1", "room21",
                    "DisplayName", "string", "Room 21",
                    "Location", "string", "Puget Sound",
                    "Temperature", "double", "0",
                    "ComfortIndex", "double", "0"
                });
            await CreateDigitalTwin(new string[18]
                {
                    "CreateTwin", "dtmi:contosocom:DigitalTwins:Thermostat;1", "thermostat67",
                    "DisplayName", "string", "Thermostat 67",
                    "Location", "string", "Puget Sound",
                    "FirmwareVersion", "string", "1.3.9",
                    "Temperature", "double", "0",
                    "ComfortIndex", "double", "0"
                });

            Log.Out($"Creating edges between the Floor, Room and Thermostat");
            await CreateRelationship(new string[11]
                {
                    "CreateEdge", "floor1", "contains", "room21", "floor_to_room_edge",
                    "ownershipUser", "string", "Contoso",
                    "ownershipDepartment", "string", "Comms Division"
                });
            await CreateRelationship(new string[11]
                {
                    "CreateEdge", "room21", "contains", "thermostat67", "room_to_therm_edge",
                    "ownershipUser", "string", "Contoso",
                    "ownershipDepartment", "string", "Comms Division"
                });
        }

        private static async Task CreateDigitalTwin(string[] cmd)
        {
            Log.Alert($"Preparing...");
            if (cmd.Length < 2)
            {
                Log.Error("Please specify a model id as the first argument");
                return;
            }
            string modelId = cmd[1];
            string twinId = Guid.NewGuid().ToString();
            if (cmd.Length > 2)
                twinId = cmd[2];
            string[] args = cmd.Skip(3).ToArray();

            var twinData = new BasicDigitalTwin
            {
                Id = twinId,
                Metadata =
                {
                    ModelId = modelId,
                },
            };

            for (int i = 0; i < args.Length; i += 3)
            {
                twinData.Contents.Add(args[i], ConvertStringToType(args[i + 1], args[i + 2]));
            }
            Log.Alert($"Submitting...");

            try
            {
                await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(twinData.Id, twinData);
                Log.Ok($"Twin '{twinId}' created successfully!");
            }
            catch (RequestFailedException e)
            {
                Log.Error($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex}");
            }
        }

        private static async Task CreateRelationship(string[] cmd)
        {
            if (cmd.Length < 5)
            {
                Log.Error("To create an Relationship you must specify at least source twin, target twin, relationship name and relationship id");
                return;
            }
            string sourceTwinId = cmd[1];
            string relationshipName = cmd[2];
            string targetTwinId = cmd[3];
            string relationshipId = cmd[4];

            string[] args = null;
            if (cmd.Length > 5)
            {
                args = cmd.Skip(5).ToArray();
                if (args.Length % 3 != 0)
                {
                    Log.Error("To add properties to relationships specify triples of propName schema value");
                    return;
                }
            }

            var relationship = new BasicRelationship
            {
                Id = relationshipId,
                SourceId = sourceTwinId,
                TargetId = targetTwinId,
                Name = relationshipName,
            };

            if (args != null)
            {
                for (int i = 0; i < args.Length; i += 3)
                {
                    relationship.Properties.Add(args[i], ConvertStringToType(args[i + 1], args[i + 2]));
                }
            }

            Log.Out($"Submitting...");
            try
            {
                await client.CreateOrReplaceRelationshipAsync(sourceTwinId, relationshipId, relationship);
                Log.Ok($"Relationship {relationshipId} of type {relationshipName} created successfully from {sourceTwinId} to {targetTwinId}!");
            }
            catch (RequestFailedException e)
            {
                Log.Error($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex}");
            }
        }

        private static object ConvertStringToType(string schema, string val)
        {
            switch (schema)
            {
                case "boolean":
                    return bool.Parse(val);
                case "double":
                    return double.Parse(val);
                case "float":
                    return float.Parse(val);
                case "integer":
                case "int":
                    return int.Parse(val);
                case "datetime":
                    return DateTime.Parse(val);
                case "duration":
                    return int.Parse(val);
                case "string":
                default:
                    return val;
            }
        }
    }
}
