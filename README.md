# Quick start guide – Authentication with .NET-Core
This example shows you how to set up authentication with Signicat using .NET Core 3.1 and OpenID Connect (OIDC). In the end of the example you will have a connection to our demo service and you can authenticate using demo credentials for various methods.

## Initialize a new project
```
mkdir SignicatQuickstart
cd SignicatQuickstart
```

```dotnet
dotnet new sln
dotnet new web
dotnet sln SignicatQuickstart.sln add .
```

## Install dependencies

```dotnet
dotnet add . package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

## Edit Startup.cs

Near the top of the file, add the following using statements:

```dotnet
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
```

Add new method in the Startup class:
```dotnet
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
```

Add the following inside the ConfigureServices method:
```dotnet
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
```

Inside Configure add the following after `app.UseRouting();`:
```dotnet
app.UseAuthentication();
app.UseAuthorization();
```

Change the content of the block after app.UseEndpoints to the following:
```dotnet
endpoints.MapGet("/", async context =>
{
    await context.Response.WriteAsync("Hello " + context.User.FindFirst("name").Value);
}).RequireAuthorization();
```

#### Run the code
```dotnet
dotnet run
```
Open http://localhost:5000/ in a browser
