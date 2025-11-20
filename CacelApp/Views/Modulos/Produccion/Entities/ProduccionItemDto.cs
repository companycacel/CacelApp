using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.Entities.Generic;
using System;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de producción (pde) en el listado
/// </summary>
[ObservableObject]
public partial class ProduccionItemDto : Pde
{
    // Propiedades calculadas
    // Índice para numeración en la tabla
    [ObservableProperty] private int? index;
    [ObservableProperty] private bool canEdit;
    [ObservableProperty] private bool canDelete;
    [ObservableProperty] private bool hasMedia;
}
