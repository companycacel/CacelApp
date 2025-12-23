using CacelApp.Services.Dialog;
using Core.Services.Configuration;
using Infrastructure.Services.Shared;
using System.IO;

namespace CacelApp.Services.ImageAudit;

public class ImageAuditService : IImageAuditService
{
    private readonly ICameraService _cameraService;
    private readonly IConfigurationService _configService;
    private readonly IDialogService _dialogService;

    public ImageAuditService(
        ICameraService cameraService,
        IConfigurationService configService,
        IDialogService dialogService)
    {
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    public async Task<List<System.IO.MemoryStream>> CapturarImagenesAsync(string nombreBalanza)
    {
        var imagenes = new List<System.IO.MemoryStream>();

        try
        {
            // 1. Obtener configuración de la sede activa
            var sede = await _configService.GetSedeActivaAsync();
            if (sede == null || !sede.RequiereCamaras())
                return imagenes;

            // 2. Buscar configuración de la balanza
            var balanzaConfig = sede.Balanzas.FirstOrDefault(b => b.Nombre == nombreBalanza);
            if (balanzaConfig == null || !balanzaConfig.CanalesCamaras.Any())
                return imagenes;

            // 3. Verificar estado de cámaras e inicializar si es necesario
            var estadoCamaras = _cameraService.ObtenerEstadoCamaras();
            if (!estadoCamaras.Any())
            {
                if (!await _cameraService.InicializarAsync(sede.Dvr, sede.Camaras.ToList()))
                    return imagenes;
            }

            // 4. Iniciar streaming para canales que no estén activos
            foreach (var canal in balanzaConfig.CanalesCamaras)
            {
                if (!estadoCamaras.ContainsKey(canal) || !estadoCamaras[canal])
                {
                    _cameraService.IniciarStreaming(canal, IntPtr.Zero);
                }
            }

            foreach (var canal in balanzaConfig.CanalesCamaras)
            {
                try
                {
                    var imagenStream = await _cameraService.CapturarImagenAsync(canal);
                    if (imagenStream != null)
                    {
                        imagenes.Add(imagenStream);
                    }
                    else
                    {
                        await _dialogService.ShowWarning("No se pudo capturar la imagen", "Advertencia");
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowError($"Error al capturar imagen: {ex.Message}", "Error");
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError($"Error al capturar imágenes: {ex.Message}", "Error");
        }

        return imagenes;
    }

    public List<Microsoft.AspNetCore.Http.IFormFile> ConvertirAFormFiles(
        List<System.IO.MemoryStream> imagenes,
        string prefijo = "files")
    {
        if (imagenes == null || !imagenes.Any())
            return new List<Microsoft.AspNetCore.Http.IFormFile>();

        return imagenes.Select((ms, index) =>
        {
            var bytes = ms.ToArray();
            return (Microsoft.AspNetCore.Http.IFormFile)new SimpleFormFile(
                bytes,
                prefijo,
                $"{index + 1}.jpg");
        }).ToList();
    }

    public async Task GuardarImagenesLocalmenteAsync(
        List<System.IO.MemoryStream> imagenes,
        string rutaRelativa,
        string mediaPrefix)
    {
        if (imagenes == null || !imagenes.Any())
            return;

        try
        {
            var config = await _configService.LoadAsync();
            var rutaBase = config.Global.Ftp.CarpetaLocal;

            if (string.IsNullOrEmpty(rutaBase))
            {
                return;
            }

            var carpeta = Path.Combine(rutaBase,
                rutaRelativa.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            int index = 1;
            foreach (var contenido in imagenes)
            {
                var nombreArchivo = $"{mediaPrefix}-{index}.jpg";
                var ruta = Path.Combine(carpeta, nombreArchivo);

                contenido.Position = 0;
                using var fs = new FileStream(ruta, FileMode.Create);
                await contenido.CopyToAsync(fs);
                index++;
            }
        }
        catch (Exception ex)
        {
        }
    }
}
