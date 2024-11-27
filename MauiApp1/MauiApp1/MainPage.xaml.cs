using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using IdentityModel.OidcClient;
using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;
using Microsoft.Maui.Controls;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
    private string? _currentAccessToken;
    public MainPage()
    {
        InitializeComponent();
    }
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            editor.Text = "Login Clicked";

            _currentAccessToken = await GetTokenByWebAuthenticator();
            editor.Text = "Loggin successful";
        }
        catch (Exception ex)
        {
            editor.Text = "Error: " + ex.Message;
            editor.Text = "Loggin successful";
        }
    }
    private async Task<string> GetTokenByWebAuthenticator()
    {
        // Configure the discovery policy
        var authorityUrl = "https://cognito-idp.ca-central-1.amazonaws.com/ca-central-1_3uLXoqZ8N";
        var domainUrl = "https://ca-central-13ulxoqz8n.auth.ca-central-1.amazoncognito.com";
        var discoveryPolicy = new DiscoveryPolicy
        {
            RequireHttps = true,                // Require HTTPS for security
            ValidateIssuerName = false,          // Validate the issuer name
            Authority = authorityUrl
        };
        // Add fallback or additional endpoints
        discoveryPolicy.AdditionalEndpointBaseAddresses.Add(domainUrl);

        // Step 1: Generate an auth url
        var oidcClient = new OidcClient(new()
        {
            Authority = authorityUrl,
            ClientId = "4uunqqtdcqnv02o81nie8fmnec",
            Scope = "email openid phone",
            RedirectUri = "myapp://callback",
            //ProviderInformation = 
            Policy = new() { Discovery = discoveryPolicy },
            Browser = new WebAuthenticatorBrowser()
        });

        var result = await oidcClient.LoginAsync();
        if (result.IsError)
        {
            throw new Exception("Error: " + result.Error);
        }
        return result.AccessToken;

    }

    private async void OnApiClicked(object sender, EventArgs e)
    {
        try
        {
            editor.Text = "API Clicked";
            if (_currentAccessToken != null)
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _currentAccessToken);
                    var response = await client.GetAsync("https://192.168.86.243:7274/WeatherForecast");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(content).RootElement;
                        editor.Text = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                    }
                    else
                    {
                        editor.Text = response.ReasonPhrase;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            editor.Text = ex.Message;
        }
    }
    public class WebAuthenticatorBrowser : IdentityModel.OidcClient.Browser.IBrowser
    {
        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(options.StartUrl),
                    new Uri(options.EndUrl));

                var url = new RequestUrl("myapp://callback")
                    .Create(new Parameters(result.Properties));

                return new BrowserResult
                {
                    Response = url,
                    ResultType = BrowserResultType.Success,
                };
            }
            catch (TaskCanceledException)
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UserCancel
                };
            }
        }
    }
}

