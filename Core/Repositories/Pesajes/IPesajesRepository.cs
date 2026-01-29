using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Pesajes;

public interface IPesajesRepository
{
    Task<ApiResponse<Pes>> SavePesajeAsync(Pes request);
    Task<ApiResponse<Pde>> SavePesajeDetalleAsync(Pde request);
}
