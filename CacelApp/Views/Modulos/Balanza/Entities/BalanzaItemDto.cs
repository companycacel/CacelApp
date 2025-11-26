using CommunityToolkit.Mvvm.ComponentModel;
using Core.Repositories.Balanza.Entities;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de lista de balanza
/// Usa directamente la entidad Baz de la base de datos sin adaptadores
/// </summary>
/// 
[ObservableObject]
public partial class BalanzaItemDto : Baz
{
    // Índice para numeración en la tabla
    [ObservableProperty]
    private int? index;
    public string baz_tipo_des => baz_tipo switch
    {
        0 => "Cliente Externo",
        1 => "Interno Despacho",
        2 => "Interno Recepción",
        _ => "Desconocido"
    };
}

