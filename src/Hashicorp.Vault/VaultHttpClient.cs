using System.Net.Http.Headers;

namespace Hashicorp.Vault;

public class VaultHttpClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<string> GetSecretAsync(string vaultToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/v1/kv-v2/data/demo-app%2Fconfig");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        request.Headers.Add("X-Vault-Token", vaultToken);

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
