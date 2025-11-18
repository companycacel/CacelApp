using Core.Repositories.Pesajes;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Validators;

namespace Infrastructure.Services.Services.Pesajes;

/// <summary>
/// Servicio de aplicación para operaciones de Pesajes
/// Implementa la lógica de negocio y orquesta operaciones entre repositorios
/// </summary>
public class PesajesService : IPesajesService
{
    private readonly IPesajesRepository _repository;

    public PesajesService(IPesajesRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Obtiene el listado de pesajes por tipo
    /// </summary>
    /// <param name="pes_tipo">Tipo de pesaje: PE (Entrada), PS (Salida), DS (Devolución)</param>
    /// <returns>Lista de pesajes</returns>
    public async Task<ApiResponse<IEnumerable<Pes>>> GetPesajes(string pes_tipo)
    {
        if (string.IsNullOrWhiteSpace(pes_tipo))
            throw new ArgumentException("El tipo de pesaje no puede estar vacío", nameof(pes_tipo));

        return await _repository.GetPesajes(pes_tipo);
    }

    /// <summary>
    /// Obtiene un pesaje por su ID con todos sus detalles
    /// </summary>
    public async Task<ApiResponse<Pes>> GetPesajesById(int code)
    {
        ValidationHelper.ValidarId(code, nameof(code));
        return await _repository.GetPesajesById(code);
    }

    /// <summary>
    /// Obtiene el reporte PDF de un pesaje
    /// </summary>
    public async Task<byte[]> GetReportAsync(int code)
    {
        ValidationHelper.ValidarId(code, nameof(code));
        return await _repository.GetReportAsync(code);
    }

    /// <summary>
    /// Guarda o actualiza un pesaje
    /// </summary>
    public async Task<ApiResponse<Pes>> Pesajes(Pes request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _repository.Pesajes(request);
    }

    /// <summary>
    /// Obtiene los detalles de un pesaje
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalle(int code)
    {
        ValidationHelper.ValidarId(code, nameof(code));
        return await _repository.GetPesajesDetalle(code);
    }

    /// <summary>
    /// Guarda o actualiza un detalle de pesaje
    /// </summary>
    public async Task<ApiResponse<Pde>> PesajesDetalle(Pde request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _repository.PesajesDetalle(request);
    }

    /// <summary>
    /// Obtiene la descripción del estado del pesaje
    /// </summary>
    public string GetStatusDescription(int status)
    {
        return status switch
        {
            1 => "PROCESADO",
            2 => "REGISTRANDO",
            _ => "DESCONOCIDO"
        };
    }

    /// <summary>
    /// Valida si un pesaje puede ser editado según su tipo
    /// </summary>
    public bool CanEdit(string pes_tipo)
    {
        return new[] { "PE", "PS", "DS" }.Contains(pes_tipo);
    }

    /// <summary>
    /// Valida si un pesaje puede ser anulado según su estado
    /// </summary>
    public bool CanDelete(int pes_status)
    {
        return pes_status == 2; // Solo se pueden anular pesajes en estado REGISTRANDO
    }
}
