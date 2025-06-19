using Microsoft.Azure.Devices.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks; 

namespace DeviceSimulator
{
    public static class AzureIoTHub
    { 
        private const string deviceConnectionString = "..."; 
        
        public static async Task SendDeviceToCloudMessageAsync(CancellationToken cancelToken)
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);

            double avgTemperature = 70.0D;
            var rand = new Random();

            while (!cancelToken.IsCancellationRequested)
            {
                double currentTemperature = avgTemperature + rand.NextDouble() * 4 - 3;

                var telemetryDataPoint = new
                {
                    Temperature = currentTemperature
                };
                var messageString = JsonSerializer.Serialize(telemetryDataPoint);
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(messageString))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };
                await deviceClient.SendEventAsync(message);
                Console.WriteLine($"{DateTime.Now} > Sending message: {messageString}");
                
                await Task.Delay(5000);
            }
        }             
    }
}
