using System;
using System.Linq;
using System.Reflection.PortableExecutable;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace smart_local
{

    /// <summary>
    /// Main Program
    /// </summary>
    public static class Program
    {
        private const string _defaultFhirServerUrl = "https://launch.smarthealthit.org/v/r4/sim/WzIsIiIsIjFjYjUxMTU3LTgwODMtNDEwZi04N2QxLTA3YTk0NjI5MjIyYSIsIkFVVE8iLDAsMCwwLCIiLCIiLCIiLCIiLCIiLCIiLCIiLDAsMSwiIl0/fhir";


        /// <summary>
        /// Program to access a SMART FHIR Server with a local webserver for redirection
        /// </summary>
        /// <param name="fhirServerUrl"></param>
        /// <returns></returns>
        static int Main(string fhirServerUrl)
        {
            if (string.IsNullOrEmpty(fhirServerUrl))
            {
                fhirServerUrl = _defaultFhirServerUrl;
            }

            System.Console.WriteLine($"FHIR Server: {fhirServerUrl}");

            FhirClient fhirClient = new FhirClient(fhirServerUrl);

            if(!FhirUtils.TryGetSmartUrls(fhirClient, out string authorizeUrl , out string tokenUrl))
            {
                System.Console.WriteLine($"Failed to discover SMART configuration");
                return -1;
            }

            

            System.Console.WriteLine($"Authorize URL: {authorizeUrl}");
            System.Console.WriteLine($"    Token URL: {tokenUrl}");

            
            Task.Run(() => CreateHostBuilder().Build().Run());
            int listenPort = GetListenPort().Result;

            System.Console.WriteLine($" Listening on : {listenPort}");

            for ( int i = 0; i < 30 ; i++)
            {
                System.Threading.Thread.Sleep(1000);
            }

           return 0;
        }

        /// <summary>
        /// start the webserver in the background and wait for it to initialize
        /// </summary>
        public static async void StartWebServerInBackground()
        {
            
            await Task.Delay(500);
        }

        /// <summary>
        /// Determin the listening port of the web server
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<int> GetListenPort()
        {
            await Task.Delay(500);

           for ( int loops = 0; loops < 100; loops++)
            {
                await Task.Delay(500);
                if (Startup.Addresseses == null)
                {
                    continue;
                }
                string address = Startup.Addresseses.Addresses.FirstOrDefault();

                if (string.IsNullOrEmpty(address))
                {
                    continue;
                }

                if (address.Length < 18)
                {
                    continue;
                }

                if ((int.TryParse(address.Substring(17), out int port)) && (port != 0))
                {
                    return port;
                }

            }

            throw new Exception($"Failed to get listen port!");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder() =>
        
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.UseKestrel();
                    webBuilder.UseStartup<Startup>();
                });
    }
}