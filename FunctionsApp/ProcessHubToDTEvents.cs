using Azure;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleFunctionsApp
{
    // This class processes telemetry events from IoT Hub, reads temperature of a device
    // and sets the "Temperature" property of the device with the value of the telemetry.
    public class ProcessHubToDTEvents
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string adtServiceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");

        [FunctionName("ProcessHubToDTEvents")]
        public async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {  
            //Authenticate with Digital Twins
            var credentials = new DefaultAzureCredential();
            DigitalTwinsClient client = new DigitalTwinsClient(
                new Uri(adtServiceUrl), credentials, new DigitalTwinsClientOptions
                { Transport = new HttpClientTransport(httpClient) });
            log.LogInformation($"ADT service client connection created.");

            if (eventGridEvent != null && eventGridEvent.Data != null)
            {
                log.LogInformation(eventGridEvent.Data.ToString());

                // Reading deviceId and temperature for IoT Hub JSON
                JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                string deviceId = (string)deviceMessage["systemProperties"]["iothub-connection-device-id"];
                var temperature = deviceMessage["body"]["Temperature"];

                log.LogInformation($"Device:{deviceId} Temperature is:{temperature}");

                //Update twin using device temperature
                // var updateTwinData = new JsonPatchDocument();
                // updateTwinData.AppendReplace("/Temperature", temperature.Value<double>());
                // await client.UpdateDigitalTwinAsync(deviceId, updateTwinData);

                try
                    {
                        var updateTwinData = new JsonPatchDocument();
                        updateTwinData.AppendReplace("/Temperature", temperature.Value<double>());

                        log.LogInformation($"Updating twin '{deviceId}' with Temperature = {temperature.Value<double>()}");

                        await client.UpdateDigitalTwinAsync(deviceId, updateTwinData);

                        log.LogInformation($"Twin '{deviceId}' updated successfully.");
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Failed to update twin '{deviceId}': {ex.Message}");
                    }

            }
        }
    }
}