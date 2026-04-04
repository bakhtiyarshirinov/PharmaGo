using System.Net.Http.Json;
using System.Text.Json;

namespace PharmaGo.IntegrationTests.Infrastructure;

public static class JsonExtensions
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task<T?> ReadAsAsync<T>(this HttpContent content)
    {
        return content.ReadFromJsonAsync<T>(SerializerOptions);
    }
}
