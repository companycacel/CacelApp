using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CacelApp.Config;

namespace CacelApp.Services.Image;

/// <summary>
/// Servicio para cargar imágenes desde el servidor FTP/HTTP
/// Basado en la implementación del proyecto CacelTracking
/// </summary>
public interface IImageLoaderService
{
    /// <summary>
    /// Carga las imágenes de un registro de balanza desde el servidor
    /// </summary>
    /// <param name="rutaBase">Ruta base donde están las imágenes (ej: 2024/11/17)</param>
    /// <param name="nombresArchivos">Nombres de archivos separados por coma</param>
    /// <returns>Lista de BitmapImage cargadas</returns>
    Task<List<BitmapImage>> CargarImagenesAsync(string rutaBase, string nombresArchivos);
}

public class ImageLoaderService : IImageLoaderService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ImageLoaderService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        // Usar servidor FTP específico para imágenes
        _baseUrl = AppConfiguration.Ftp.ServerUrl;
    }

    public async Task<List<BitmapImage>> CargarImagenesAsync(string rutaBase, string nombresArchivos)
    {
        var imagenes = new List<BitmapImage>();

        if (string.IsNullOrWhiteSpace(nombresArchivos))
        {
            return imagenes;
        }

        var archivos = nombresArchivos.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var nombreArchivo in archivos)
        {
            try
            {
                var imagen = await CargarImagenDesdeUrlAsync(rutaBase, nombreArchivo.Trim());
                if (imagen != null)
                {
                    imagenes.Add(imagen);
                }
            }
            catch (Exception ex)
            {
                // Log error pero continuar con las demás imágenes
                System.Diagnostics.Debug.WriteLine($"Error al cargar imagen {nombreArchivo}: {ex.Message}");
            }
        }

        return imagenes;
    }

    private async Task<BitmapImage?> CargarImagenDesdeUrlAsync(string rutaBase, string nombreArchivo)
    {
        try
        {
            var url = $"{_baseUrl}/{rutaBase}/{nombreArchivo}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var imageData = await response.Content.ReadAsByteArrayAsync();
            
            return ConvertirBytesABitmapImage(imageData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar imagen desde URL: {ex.Message}");
            return null;
        }
    }

    private BitmapImage? ConvertirBytesABitmapImage(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(imageData))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Importante para usar en diferentes hilos
            }
            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al convertir bytes a imagen: {ex.Message}");
            return null;
        }
    }
}
