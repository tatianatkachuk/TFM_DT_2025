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
    public class ProcessHubToDTEvents
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string adtServiceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");

        [FunctionName("ProcessHubToDTEvents")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            var credentials = new DefaultAzureCredential();
            var client = new DigitalTwinsClient(
                new Uri(adtServiceUrl),
                credentials,
                new DigitalTwinsClientOptions
                {
                    Transport = new HttpClientTransport(httpClient)
                });

            log.LogInformation("ADT service client connection created.");

            if (eventGridEvent?.Data != null)
            {
                try
                {
                    JObject message = JObject.Parse(eventGridEvent.Data.ToString());
                    string deviceId = (string)message["systemProperties"]?["iothub-connection-device-id"];
                    JObject body = (JObject)message["body"];

                    if (deviceId == null || body == null)
                    {
                        log.LogWarning("Datos incompletos en el mensaje.");
                        return;
                    }

                    // Detecta qué propiedad incluye el mensaje (Temperature, Pressure o Vibration)
                    string propertyName = null;
                    double propertyValue = 0;

                    foreach (var prop in body)
                    {
                        propertyName = prop.Key;
                        propertyValue = prop.Value.Value<double>();
                        break; // Solo se espera una propiedad por mensaje
                    }

                    if (propertyName == null)
                    {
                        log.LogWarning("No se encontró ninguna propiedad válida en el cuerpo del mensaje.");
                        return;
                    }

                    log.LogInformation($"Device: {deviceId} | {propertyName} = {propertyValue}");

                    var patch = new JsonPatchDocument();
                    patch.AppendReplace($"/{propertyName}", propertyValue);

                    await client.UpdateDigitalTwinAsync(deviceId, patch);
                    log.LogInformation($"Twin '{deviceId}' updated with {propertyName} = {propertyValue}");
                }
                catch (Exception ex)
                {
                    log.LogError($"Error procesando el evento: {ex.Message}");
                }
            }
        }
    }
}