namespace CacelApp.Services.ImageAudit;

public interface IImageAuditService
{
    /// <summary>
    /// Captura imágenes desde los canales de cámaras configurados para una balanza
    /// </summary>
    /// <param name="nombreBalanza">Nombre de la balanza (ej: "B1-A")</param>
    /// <returns>Lista de MemoryStreams con las imágenes capturadas</returns>
    Task<List<System.IO.MemoryStream>> CapturarImagenesAsync(string nombreBalanza);

    /// <summary>
    /// Convierte MemoryStreams a IFormFile para envío al servidor
    /// </summary>
    /// <param name="imagenes">Lista de imágenes en MemoryStream</param>
    /// <param name="prefijo">Prefijo para el nombre del campo (por defecto "files")</param>
    /// <returns>Lista de IFormFile listos para enviar</returns>
    List<Microsoft.AspNetCore.Http.IFormFile> ConvertirAFormFiles(
        List<System.IO.MemoryStream> imagenes, 
        string prefijo = "files");

    /// <summary>
    /// Guarda imágenes localmente como auditoría (solo para ActionType.Create)
    /// </summary>
    /// <param name="imagenes">Lista de imágenes a guardar</param>
    /// <param name="rutaRelativa">Ruta relativa desde la carpeta base (ej: pde_path)</param>
    /// <param name="mediaPrefix">Prefijo para el nombre de archivo (ej: pde_media)</param>
    Task GuardarImagenesLocalmenteAsync(
        List<System.IO.MemoryStream> imagenes,
        string rutaRelativa,
        string mediaPrefix);
}
