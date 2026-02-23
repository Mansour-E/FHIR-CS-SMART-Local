using System;
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

            CreateHostBuilder().Build().Run();

           return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder() =>
        
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(WebHostBuilder =>
                {
                    WebHostBuilder.UseStartup<Startup>();
                });
    }
}