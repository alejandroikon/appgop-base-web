// T-INFRA-13: Key Vault integration via Managed Identity (DefaultAzureCredential)
// Loads secrets into IConfiguration at startup — no credentials hardcoded

using Azure.Identity;
using Serilog;

namespace GOP.API.Extensions;

public static class KeyVaultExtension
{
    public static WebApplicationBuilder AddKeyVaultIfConfigured(this WebApplicationBuilder builder)
    {
        var kvUri = builder.Configuration["KeyVaultUri"];
        if (string.IsNullOrWhiteSpace(kvUri))
        {
            // Not configured — skip (dev/local environments)
            return builder;
        }

        builder.Configuration.AddAzureKeyVault(
            new Uri(kvUri),
            // Uses AZURE_CLIENT_ID env var (set by App Service to the managed identity)
            // Falls back to system-assigned identity, then developer credentials (VS, az CLI)
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"]
            }));

        Log.Information("Key Vault configurado: {KvUri}", kvUri);
        return builder;
    }
}
