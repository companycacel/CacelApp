using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;

namespace Infrastructure.WebApi.Repositories.Balanza;

/// <summary>
/// Implementación del repositorio de escritura de balanza usando API HTTP
/// Implementa el patrón Repository para operaciones CRUD
/// </summary>
public class BalanzaWriteRepository : IBalanzaWriteRepository
{
    private readonly HttpClient _httpClient;
    private const string BaseEndpoint = "api/balanza";

    public BalanzaWriteRepository(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Baz> CrearAsync(Baz registro, CancellationToken cancellationToken = default)
    {
        try
        {
            // Serializar el registro a JSON
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registro),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(BaseEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Aquí se deserializaría el JSON a BalanzaRegistro
            // Por ahora retornamos el mismo registro
            return registro;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al crear el registro de balanza", ex);
        }
    }

    public async Task<Baz> ActualizarAsync(Baz registro, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registro),
                System.Text.Encoding.UTF8,
                "application/json");

            var url = $"{BaseEndpoint}/{registro.baz_id}";
            var response = await _httpClient.PutAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Aquí se deserializaría el JSON a BalanzaRegistro
            return registro;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al actualizar el registro de balanza con ID {registro.baz_id}", ex);
        }
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseEndpoint}/{id}";
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al eliminar el registro de balanza con ID {id}", ex);
        }
    }

    public async Task<bool> CambiarEstadoAsync(int id, int nuevoEstado, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { estado = nuevoEstado }),
                System.Text.Encoding.UTF8,
                "application/json");

            var url = $"{BaseEndpoint}/{id}/estado";
            var response = await _httpClient.PatchAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al cambiar el estado del registro con ID {id}", ex);
        }
    }
}
