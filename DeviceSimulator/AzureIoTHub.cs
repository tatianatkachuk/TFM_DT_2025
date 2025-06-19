using Microsoft.Azure.Devices.Client;
using System.Collections.Generic;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dotenv.net;

class Program
{
    static async Task Main()
    {
        DotEnv.Load(); // Carga el .env

        var cts = new CancellationTokenSource();
        Console.WriteLine($"Directorio de ejecución: {Environment.CurrentDirectory}");

        var tempTask = SensorSimulator.RunAsync("TEMP_CONNECTION_STRING", "Temperature", 900.0, -50, 50, "°C", cts.Token);
        var pressureTask = SensorSimulator.RunAsync("PRESSURE_CONNECTION_STRING", "Pressure", 1500.0, -200, 200, "kPa", cts.Token);
        var vibrationTask = SensorSimulator.RunAsync("VIBRATION_CONNECTION_STRING", "Vibration", 6.0, -4.0, 6.0, "mm/s RMS", cts.Token);

        Console.WriteLine("Simulando sensores... pulsa una tecla para detener."); 
        Console.ReadKey();
        cts.Cancel();

        await Task.WhenAll(tempTask, pressureTask, vibrationTask);
    }
}

public static class SensorSimulator
{
    public static async Task RunAsync(string envVariable, string propertyName, double avg, double minDelta, double maxDelta, string unit, CancellationToken token)
    {
        string connectionString = Environment.GetEnvironmentVariable(envVariable);

        var client = DeviceClient.CreateFromConnectionString(connectionString);
        var rand = new Random();

        while (!token.IsCancellationRequested)
        {
            double value = avg + rand.NextDouble() * (maxDelta - minDelta) + minDelta;
            
            object data;
            switch (propertyName)
            {
                case "Temperature":
                    data = new { Temperature = value };
                    break;
                case "Pressure":
                    data = new { Pressure = value };
                    break;
                case "Vibration":
                    data = new { Vibration = value };
                    break;
                default:
                    Console.WriteLine($"Propiedad desconocida: {propertyName}");
                    return;
            }
             var json = JsonSerializer.Serialize(data);
            var message = new Message(Encoding.UTF8.GetBytes(json))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };
           
            await client.SendEventAsync(message);
            Console.WriteLine($"[{propertyName}] {DateTime.Now} > {value:F2} {unit}");

            await Task.Delay(5000);
        }
    }
}