using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Web;


namespace smart_local
{

    /// <summary>
    /// Main Program
    /// </summary>
    public static class Program
    {
        private const string _clientId = "fhir_demo_id";

        private const string _defaultFhirServerUrl = "https://launch.smarthealthit.org/v/r4/sim/WzIsIiIsIjFjYjUxMTU3LTgwODMtNDEwZi04N2QxLTA3YTk0NjI5MjIyYSIsIkFVVE8iLDAsMCwwLCIiLCIiLCIiLCIiLCIiLCIiLCIiLDAsMSwiIl0/fhir";

        private static string _authCode = string.Empty;
        private static string _clientState = string.Empty;

        private static string _redirectUrl = string.Empty;

        private static string _tokenUrl = string.Empty;

        private static string _fhirServerUrl = string.Empty;




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
            _fhirServerUrl = fhirServerUrl;


            FhirClient fhirClient = new FhirClient(fhirServerUrl);

            if(!FhirUtils.TryGetSmartUrls(fhirClient, out string authorizeUrl , out string tokenUrl))
            {
                System.Console.WriteLine($"Failed to discover SMART configuration");
                return -1;
            }

            

            System.Console.WriteLine($"Authorize URL: {authorizeUrl}");
            System.Console.WriteLine($"    Token URL: {tokenUrl}");
            _tokenUrl = tokenUrl;
            
            Task.Run(() => CreateHostBuilder().Build().Run());

            int listenPort = GetListenPort().Result;

            System.Console.WriteLine($" Listening on : {listenPort}");
            _redirectUrl = $"http://127.0.0.1:{listenPort}";

            //
            // Location: https://ehr/authorize?
            // response_type=code&
            // client_id=app-client-id&
            // redirect_uri=https%3A%2F%2Fapp%2Fafter-auth&
            // launch=xyz123&
            // scope=launch+patient%2FObservation.rs+patient%2FPatient.rs+openid+fhirUser&
            // state=98wrghuwuogerg97&
            // aud=https://ehr/fhir
            //

            string url =
                $"{authorizeUrl}" +
                $"?response_type=code" +
                $"&client_id={_clientId}" +
                $"&redirect_uri={HttpUtility.UrlEncode(_redirectUrl)}" +
                $"&scope={HttpUtility.UrlEncode("openid fhirUser profile launch/patient patient/*.read")}" + 
                $"&state=local_state" + 
                $"&aud={fhirServerUrl}";

            LaunchUrl(url);

            //http://127.0.0.1:53357/?code=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJjb250ZXh0Ijp7Im5lZWRfcGF0aWVudF9iYW5uZXIiOnRydWUsInNtYXJ0X3N0eWxlX3VybCI6Imh0dHBzOi8vbGF1bmNoLnNtYXJ0aGVhbHRoaXQub3JnL3NtYXJ0LXN0eWxlLmpzb24iLCJwYXRpZW50IjoiNzc1YWU0NTUtNzk2Ni00MTZlLThkMDQtZmQwYjhmZGFjMjhmIn0sImNsaWVudF9pZCI6ImZoaXJfZGVtb19pZCIsInJlZGlyZWN0X3VyaSI6Imh0dHA6Ly8xMjcuMC4wLjE6NTMzNTciLCJzY29wZSI6Im9wZW5pZCBmaGlyVXNlciBwcm9maWxlIGxhdW5jaC9wYXRpZW50IHBhdGllbnQvKi5yZWFkIiwicGtjZSI6ImF1dG8iLCJjbGllbnRfdHlwZSI6InB1YmxpYyIsInVzZXIiOiJQcmFjdGl0aW9uZXIvMWNiNTExNTctODA4My00MTBmLTg3ZDEtMDdhOTQ2MjkyMjJhIiwiaWF0IjoxNzcxOTQ3MDIxLCJleHAiOjE3NzE5NDczMjF9.ZabxiRHPpMoEbNASWuvr0GyDvq_NrID9hznNnfDKdZ4&state=local_state

            for ( int i = 0; i < 5 ; i++)
            {
                System.Threading.Thread.Sleep(1000);
            }

           return 0;
        }

        /// <summary>
        /// Set the authorizeation code and state
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        public static async void SetAuthCode(string code, string state)
        {
            _authCode = code;
            _clientState = state;

            System.Console.WriteLine($"Code received: {code}");

            Dictionary<string, string> requestValues = new Dictionary<string, string>()
            {
                {"grant_type", "authorization_code"},
                {"code", code},
                {"redirect_uri", _redirectUrl},
                {"client_id" , _clientId},
            };

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_tokenUrl),
                Content = new FormUrlEncodedContent(requestValues),

            };

            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine($"Failed to exchange code for token!");
                throw new Exception($"Unauthorized: {response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync();

            System.Console.WriteLine($"----- Authorization Response -----");
            System.Console.WriteLine(json);
            System.Console.WriteLine($"----- Authorization Response -----");

            SmartResponse smartResponse = JsonSerializer.Deserialize<SmartResponse>(json);
        }

        /// <summary>
        /// use SMART token with the fhir Net API
        /// </summary>
        /// <param name="smartResponse"></param>
        public static void DoSomethingWithToken(SmartResponse smartResponse)
        {
            if (smartResponse == null)
            {
                throw new ArgumentNullException(nameof(smartResponse));
            }

            if (string.IsNullOrEmpty(smartResponse.AccessToken))
            {
                throw new ArgumentNullException("SMART Access Token is requiered!");
            }

            Hl7.Fhir.Rest.FhirClient fhirClient = new Hl7.Fhir.Rest.FhirClient(_fhirServerUrl);
        }

        /// <summary>
        /// Launch a URL in the user's default web browser
        /// </summary>
        /// <param name="url"></param>
        /// <returns>true if seccessful , false otherweise </returns>
        public static bool LaunchUrl(string url)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = url,
                    UseShellExecute = true,
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception)
            {
                //ignore
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd" , $"/c start {url}"){CreateNoWindow = true});
                    return true;
                }
                catch (System.Exception)
                {
                    //ignore
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string[] allowedProgramsToRun = {"xdg-open","gnome-open","kfmclient"};

                foreach ( string helper in allowedProgramsToRun)
                {
                    try
                    {
                        Process.Start(helper, url);
                        return true;
                    }
                    catch (Exception)
                    {
                        //egal
                    }
                }

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    Process.Start("open",url);
                    return true;
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            System.Console.WriteLine($"Fained to launch URL");
            return false;
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

                if (int.TryParse(address.Substring(17), out int port) && (port != 0))
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