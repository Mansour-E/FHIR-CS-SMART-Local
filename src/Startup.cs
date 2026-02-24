using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Primitives;
using Hl7.Fhir.Model;

namespace smart_local
{
    public class Startup
    {

        private static IServerAddressesFeature _addresses = null;
        /// <summary>
        /// List of the webServer addresses in use
        /// </summary>
        public static IServerAddressesFeature Addresseses => _addresses;
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            _addresses = app.ServerFeatures.Get<IServerAddressesFeature>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/" , async context =>
                {
                    string code = string.Empty;
                    string state = string.Empty;

                    IQueryCollection query = context.Request.Query;

                    foreach (KeyValuePair<string, StringValues> kvp in query)
                    {
                        switch (kvp.Key)
                        {
                            case "code":
                                code = kvp.Value.FirstOrDefault();
                                break;

                            case "state":
                                state = kvp.Value.FirstOrDefault();
                                break;
                        }
                    }

                    System.Threading.Tasks.Task.Run(() => Program.SetAuthCode(code, state));

                    await context.Response.WriteAsync("Code received, you may close this window.");

                });
            });
        }
    }
}