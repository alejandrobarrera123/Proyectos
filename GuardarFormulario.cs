#r "Newtonsoft.Json"
#r "Microsoft.Azure.Cosmos"

using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public static class GuardarFormulario
{
    private static readonly string endpoint = System.Environment.GetEnvironmentVariable("COSMOSDB_ENDPOINT");
    private static readonly string key = System.Environment.GetEnvironmentVariable("COSMOSDB_KEY");
    private static readonly string databaseId = "bot21";
    private static readonly string containerId = "mensajes";

    private static readonly CosmosClient client = new CosmosClient(endpoint, key);

    [FunctionName("GuardarFormulario")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<Formulario>(requestBody);

        if (string.IsNullOrEmpty(data.Nombre) || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Mensaje))
        {
            return new BadRequestObjectResult(new { error = "Todos los campos son obligatorios" });
        }

        try
        {
            var container = client.GetContainer(databaseId, containerId);
            data.Id = System.Guid.NewGuid().ToString();
            data.Fecha = System.DateTime.UtcNow;

            await container.CreateItemAsync(data, new PartitionKey(data.Email));

            return new OkObjectResult(new { mensaje = "Formulario guardado con éxito" });
        }
        catch (CosmosException ex)
        {
            System.Console.WriteLine($"Error al guardar: {ex.Message}");
            return new StatusCodeResult(500);
        }
    }
}

public class Formulario
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("nombre")]
    public string Nombre { get; set; }
    [JsonProperty("email")]
    public string Email { get; set; }
    [JsonProperty("mensaje")]
    public string Mensaje { get; set; }
    [JsonProperty("fecha")]
    public System.DateTime Fecha { get; set; }
}
