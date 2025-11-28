using CommunityToolkit.Mvvm.ComponentModel;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de detalle de pesajes (pde)
/// Optimizado para binding en DataGrid con propiedades observables
/// </summary>
public partial class PesajesDetalleItemDto : ObservableObject
{
    [ObservableProperty] private int pde_id;
    [ObservableProperty] private int pde_pes_id;
    [ObservableProperty] private int? pde_mde_id;
    [ObservableProperty] private string? pde_mde_des;
    [ObservableProperty] private int pde_bie_id;
    [ObservableProperty] private string? pde_bie_des;
    [ObservableProperty] private string? pde_nbza; // Nombre de balanza
    [ObservableProperty] private decimal pde_pb; // Peso bruto
    [ObservableProperty] private decimal pde_pt; // Peso tara
    [ObservableProperty] private decimal pde_pn; // Peso neto
    [ObservableProperty] private string? pde_obs;
    [ObservableProperty] private string? pde_gus_des;
    [ObservableProperty] private DateTime created;
    [ObservableProperty] private DateTime updated;
    [ObservableProperty] private string? pde_path; // Ruta de imágenes
    [ObservableProperty] private string? pde_media; // Nombres de archivos de imágenes
    [ObservableProperty] private int? pde_t6m_id;
    [ObservableProperty] private string? pde_bie_cod;

    // Propiedades calculadas para UI
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isNew;
    [ObservableProperty] private bool canEdit = true;
    [ObservableProperty] private bool canDelete = true;
    [ObservableProperty] private bool isPesoBrutoReadOnly; // Se bloquea cuando es balanza B1-A, B2-A, B3-B, B4-B

    // Lista de fotos capturadas (no se persiste, solo para UI)
    public List<(string nombre, byte[] contenido)>? FotosCapturas { get; set; }

    // Referencia a MaterialOptions para extraer Ext cuando cambia Pde_bie_id
    public System.Collections.ObjectModel.ObservableCollection<Core.Shared.Entities.SelectOption>? MaterialOptionsReference { get; set; }

    // Función helper para extraer valores del Ext (JsonElement) - Retorna int? específicamente para t6m_id
    public Func<object?, string, int?>? GetValueFromExtFunc { get; set; }

    // Copia de valores originales para cancelar edición
    private Dictionary<string, object?>? _originalValues;

    /// <summary>
    /// Guarda los valores actuales antes de editar
    /// </summary>
    public void SaveOriginalValues()
    {
        _originalValues = new Dictionary<string, object?>
        {
            [nameof(Pde_mde_id)] = Pde_mde_id,
            [nameof(Pde_mde_des)] = Pde_mde_des,
            [nameof(Pde_bie_id)] = Pde_bie_id,
            [nameof(Pde_bie_des)] = Pde_bie_des,
            [nameof(Pde_nbza)] = Pde_nbza,
            [nameof(Pde_pb)] = Pde_pb,
            [nameof(Pde_pt)] = Pde_pt,
            [nameof(Pde_pn)] = Pde_pn,
            [nameof(Pde_obs)] = Pde_obs
        };
    }

    /// <summary>
    /// Restaura los valores originales
    /// </summary>
    public void RestoreOriginalValues()
    {
        if (_originalValues == null) return;

        Pde_mde_id = (int?)_originalValues[nameof(Pde_mde_id)];
        Pde_mde_des = (string?)_originalValues[nameof(Pde_mde_des)];
        Pde_bie_id = (int)_originalValues[nameof(Pde_bie_id)]!;
        Pde_bie_des = (string?)_originalValues[nameof(Pde_bie_des)];
        Pde_nbza = (string?)_originalValues[nameof(Pde_nbza)];
        Pde_pb = (decimal)_originalValues[nameof(Pde_pb)]!;
        Pde_pt = (decimal)_originalValues[nameof(Pde_pt)]!;
        Pde_pn = (decimal)_originalValues[nameof(Pde_pn)]!;
        Pde_obs = (string?)_originalValues[nameof(Pde_obs)];

        _originalValues = null;
    }

    /// <summary>
    /// Verifica si hay cambios respecto a los valores originales
    /// </summary>
    public bool HasChanges()
    {
        if (_originalValues == null) return false;

        return Pde_mde_id != (int?)_originalValues[nameof(Pde_mde_id)] ||
               Pde_bie_id != (int)_originalValues[nameof(Pde_bie_id)]! ||
               Pde_nbza != (string?)_originalValues[nameof(Pde_nbza)] ||
               Pde_pb != (decimal)_originalValues[nameof(Pde_pb)]! ||
               Pde_pt != (decimal)_originalValues[nameof(Pde_pt)]! ||
               Pde_obs != (string?)_originalValues[nameof(Pde_obs)];
    }

    /// <summary>
    /// Determina si tiene imágenes capturadas
    /// </summary>
    public bool HasImages => !string.IsNullOrEmpty(Pde_media);

    /// <summary>
    /// Obtiene los nombres de archivos de imágenes como lista
    /// </summary>
    public List<string> GetImageNames()
    {
        if (string.IsNullOrEmpty(Pde_media))
            return new List<string>();

        return new List<string>(Pde_media.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    partial void OnPde_nbzaChanged(string? value)
    {
        // Bloquear peso bruto si es balanza principal (B1-A, B2-A, B3-B, B4-B)
        // Estas balanzas capturan el peso automáticamente desde la balanza
        IsPesoBrutoReadOnly = value == "B1-A" || value == "B2-A" || value == "B3-B" || value == "B4-B";

        // Resetear tara si no es balanza principal
        if (!IsPesoBrutoReadOnly)
        {
            Pde_pt = 0;
        }
    }

    partial void OnPde_bie_idChanged(int value)
    {
        // Cuando cambia el material seleccionado, extraer bie_t6m_id del Ext
        if (value > 0 && MaterialOptionsReference != null && GetValueFromExtFunc != null)
        {
            var materialOption = MaterialOptionsReference.FirstOrDefault(m => 
                m.Value != null && Convert.ToInt32(m.Value) == value);

            if (materialOption?.Ext != null)
            {
                // Extraer bie_t6m_id usando la función helper del ViewModel
                var t6mId = GetValueFromExtFunc.Invoke(materialOption.Ext, "bie_t6m_id");
                
                if (t6mId.HasValue)
                {
                    Pde_t6m_id = t6mId.Value;
                }
            }
        }
    }

    partial void OnPde_pbChanged(decimal value)
    {
        // Recalcular peso neto
        Pde_pn = value - Pde_pt;
    }

    partial void OnPde_ptChanged(decimal value)
    {
        // Validar que la tara no supere el peso bruto
        if (value > Pde_pb && Pde_pb > 0)
        {
            // Solo resetear silenciosamente - no mostrar diálogos desde un DTO
            System.Diagnostics.Debug.WriteLine($"⚠️ Peso Tara ({value}) no puede superar Peso Bruto ({Pde_pb}). Reseteando a 0.");
            Pde_pt = 0;
            return;
        }

        // Recalcular peso neto
        Pde_pn = Pde_pb - value;
    }
}
