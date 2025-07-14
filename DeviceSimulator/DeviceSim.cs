using Microsoft.Azure.Devices.Client;
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

        // Temperatura (°C)
        // Valor medio: 900 °C (crucero) | Rango aceptable: 850–1700 °C
        // Genera valores entre (800 y 850) y entre (1700 y 1800) para simular alertas
        var tempTask = SensorSimulator.RunAsync("TEMP_CONNECTION_STRING", "Temperature", 900.0, -100, 900, "°C", cts.Token);

        // Presión (kPa)
        // Valor medio: 500 kPa | Rango aceptable: 300–600 kPa
        // Genera valores entre (200 y 300) y entre (600 y 700) para provocar desviaciones
        var pressureTask = SensorSimulator.RunAsync("PRESSURE_CONNECTION_STRING", "Pressure", 500.0, -300, 200, "kPa", cts.Token);

        // Vibración (mm/s RMS)
        // Valor medio: 4.5 mm/s
        // Simula valores entre 0 y 11 mm/s para abarcar zonas A–D
        var vibrationTask = SensorSimulator.RunAsync("VIBRATION_CONNECTION_STRING", "Vibration", 4.5, -4.5, 6.5, "mm/s RMS", cts.Token);

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