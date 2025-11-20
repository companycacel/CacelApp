using CommunityToolkit.Mvvm.ComponentModel;
using Core.Repositories.Balanza.Entities;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de lista de balanza
/// Usa directamente la entidad Baz de la base de datos sin adaptadores
/// </summary>
public partial class BalanzaItemDto : ObservableObject
{
    // Entidad completa de la base de datos - acceso directo a todas las propiedades
    public Baz Baz { get; set; } = new();

    // Índice para numeración en la tabla
    [ObservableProperty] 
    private int? index;
}

