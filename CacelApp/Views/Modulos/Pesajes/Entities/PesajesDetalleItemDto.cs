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
    [ObservableProperty] private string? pde_nbza; 
    [ObservableProperty] private string? pde_pb; 
    [ObservableProperty] private string? pde_pt; 
    [ObservableProperty] private string? pde_pn; 
    [ObservableProperty] private string? pde_obs;
    [ObservableProperty] private string? pde_gus_des;
    [ObservableProperty] private DateTime created;
    [ObservableProperty] private DateTime updated;
    [ObservableProperty] private string? pde_path; 
    [ObservableProperty] private string? pde_media; 
    [ObservableProperty] private int? pde_t6m_id;
    [ObservableProperty] private string? pde_bie_cod;

    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isNew;
    [ObservableProperty] private bool canEdit = true;
    [ObservableProperty] private bool canDelete = true;
    [ObservableProperty] private bool isPesoBrutoReadOnly; 


    public List<(string nombre, byte[] contenido)>? FotosCapturas { get; set; }

    public System.Collections.ObjectModel.ObservableCollection<Core.Shared.Entities.SelectOption>? MaterialOptionsReference { get; set; }
    public Func<object?, string, int?>? GetValueFromExtFunc { get; set; }
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
        Pde_pb = (string?)_originalValues[nameof(Pde_pb)];
        Pde_pt = (string?)_originalValues[nameof(Pde_pt)];
        Pde_pn = (string?)_originalValues[nameof(Pde_pn)];
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
               Pde_pb != (string?)_originalValues[nameof(Pde_pb)] ||
               Pde_pt != (string?)_originalValues[nameof(Pde_pt)] ||
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
        IsPesoBrutoReadOnly = value != "B5-O";
        if (!IsPesoBrutoReadOnly)
        {
            Pde_pt = null;
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

    partial void OnPde_pbChanged(string? value)
    {
        CalculateNetWeight();
    }

    partial void OnPde_ptChanged(string? value)
    {
        CalculateNetWeight();
    }

    private void CalculateNetWeight()
    {
        decimal pb = 0;
        decimal pt = 0;

        decimal.TryParse(Pde_pb, out pb);
        decimal.TryParse(Pde_pt, out pt);

        // Validar que la tara no supere el peso bruto
        if (pt > pb && pb > 0)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Peso Tara ({pt}) no puede superar Peso Bruto ({pb}).");
            // Resetear tara si supera al bruto (Validación estricta solicitada)
            // Usamos un flag interno o simplemente seteamos el campo backing para evitar ciclo infinito si fuera necesario,
            // pero como Pde_pt es string y OnPde_ptChanged llama a esto, debemos tener cuidado.
            // Al ser un DTO, lo más seguro es dejarlo inválido visualmente o resetearlo.
            // El usuario dijo "NO PUEDE SER MAYOR", así que lo impedimos.
            
            // Nota: Si estamos escribiendo "10" y bruto es "5", al escribir "1" es válido, al escribir "0" (10) ya no.
            // Si reseteamos a 0, borramos lo que escribió.
            // Si reseteamos al valor anterior, necesitamos tracking.
            // Por simplicidad y robustez, si supera, lo igualamos al bruto o lo dejamos en 0.
            // Vamos a dejarlo en 0 para que el usuario sepa que está mal.
            Pde_pt = "0";
            pt = 0; 
        }

        Pde_pn = (pb - pt).ToString("0.00");
    }
}
