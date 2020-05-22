using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SignicatQuickstart
{
    public class Startup
    {
        protected virtual async Task RedeemAuthorizationCodeAsync(AuthorizationCodeReceivedContext context)
        {
            var configuration = await context.Options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint);
            string authInfo = context.TokenEndpointRequest.ClientId + ":" + context.TokenEndpointRequest.ClientSecret;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authInfo);
            var msg = context.TokenEndpointRequest.Clone();
            msg.ClientSecret = null;
            requestMessage.Content = new FormUrlEncodedContent(msg.Parameters);
            

            var responseMessage = await context.Backchannel.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine(await responseMessage.Content.ReadAsStringAsync());
                return;
            }

            try
            {
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                var message = new OpenIdConnectMessage(responseContent);
                context.HandleCodeRedemption(message);
            }
            catch (Exception)
            {
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies")
                .AddOpenIdConnect("oidc", options =>
                    {
                        options.Events.OnAuthorizationCodeReceived = RedeemAuthorizationCodeAsync;
                        options.Authority = "https://preprod.signicat.com/oidc";
                        options.CallbackPath = "/redirect";

                        options.ClientId = "demo-preprod";
                        options.ClientSecret = "mqZ-_75-f2wNsiQTONb7On4aAZ7zc218mrRVk1oufa8";
                        options.ResponseType = "code";
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.GetClaimsFromUserInfoEndpoint = true;

                        options.SaveTokens = true;
                    });
            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello " + context.User.FindFirst("name").Value);
                }).RequireAuthorization();
            });
        }
    }
}
