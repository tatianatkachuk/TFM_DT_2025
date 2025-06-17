using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceSimulator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await SimulateDeviceToSendD2cAndReceiveD2c();
        }        

        private static async Task SimulateDeviceToSendD2cAndReceiveD2c()
        {
            var tokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                tokenSource.Cancel();
                Console.WriteLine("Exiting...");
            };
            Console.WriteLine("Press CTRL+C to exit");

            await Task.WhenAll(
                AzureIoTHub.SendDeviceToCloudMessageAsync(tokenSource.Token));

            tokenSource.Dispose();
        }
    }
}
