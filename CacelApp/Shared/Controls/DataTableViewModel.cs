using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CacelApp.Shared.Controls;

/// <summary>
/// Wrapper para agregar índice y funcionalidad de expansión a cada elemento
/// </summary>
public partial class IndexedItem<T> : ObservableObject
{
    [ObservableProperty]
    private int rowNumber;
    
    [ObservableProperty]
    private T item = default!;
    
    [ObservableProperty]
    private bool isExpanded;
}

/// <summary>
/// ViewModel base para manejar paginación, filtrado y ordenamiento de datos tabulares
/// Usa generics para trabajar con cualquier tipo de entidad
/// </summary>
/// <typeparam name="T">Tipo de entidad a mostrar en la tabla</typeparam>
public partial class DataTableViewModel<T> : ObservableObject where T : class
{
    // Colección completa de datos (sin filtrar ni paginar)
    private List<T> _allData = new();

    // Colección filtrada pero sin paginar
    private List<T> _filteredData = new();

    /// <summary>
    /// Colección observable de datos paginados con índice (visible en la UI)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<IndexedItem<T>> _paginatedData = new();

    /// <summary>
    /// Término de búsqueda global
    /// </summary>
    [ObservableProperty]
    private string? _searchTerm;

    /// <summary>
    /// Página actual (base 1)
    /// </summary>
    [ObservableProperty]
    private int _currentPage = 1;

    /// <summary>
    /// Tamaño de página
    /// </summary>
    [ObservableProperty]
    private int _pageSize = 10;

    /// <summary>
    /// Total de páginas
    /// </summary>
    [ObservableProperty]
    private int _totalPages = 1;

    /// <summary>
    /// Total de registros (después de filtrar)
    /// </summary>
    [ObservableProperty]
    private int _totalRecords;

    /// <summary>
    /// Total de registros sin filtrar
    /// </summary>
    [ObservableProperty]
    private int _totalAllRecords;

    /// <summary>
    /// Índice de inicio para la página actual (para numeración)
    /// </summary>
    [ObservableProperty]
    private int _pageStartIndex;

    /// <summary>
    /// Elemento seleccionado (wrapper con índice)
    /// </summary>
    [ObservableProperty]
    private IndexedItem<T>? _selectedItem;

    /// <summary>
    /// Diccionario de totales por nombre de propiedad
    /// </summary>
    [ObservableProperty]
    private Dictionary<string, decimal> _columnTotals = new();

    /// <summary>
    /// Si se deben mostrar totales
    /// </summary>
    [ObservableProperty]
    private bool _showTotals = false;

    /// <summary>
    /// Opciones de tamaños de página
    /// </summary>
    public List<int> PageSizeOptions { get; } = new() { 10, 25, 50, 100 };

    /// <summary>
    /// Predicado personalizado para filtrar datos
    /// </summary>
    public Func<T, string?, bool>? CustomFilter { get; set; }

    /// <summary>
    /// Comandos de navegación
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage() => CurrentPage--;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage() => CurrentPage++;

    [RelayCommand]
    private void FirstPage() => CurrentPage = 1;

    [RelayCommand]
    private void LastPage() => CurrentPage = TotalPages;

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages && TotalPages > 0;

    /// <summary>
    /// Actualiza los datos completos y recalcula paginación
    /// </summary>
    public void SetData(IEnumerable<T> data)
    {
        _allData = data?.ToList() ?? new List<T>();
        TotalAllRecords = _allData.Count;
        CurrentPage = 1;
        ApplyFilteringAndPaging();
    }

    /// <summary>
    /// Configura las columnas que deben mostrar totales
    /// </summary>
    public void ConfigureTotals(IEnumerable<string> propertyNames)
    {
        ShowTotals = propertyNames?.Any() ?? false;
        if (ShowTotals)
        {
            CalculateTotals(propertyNames);
        }
    }

    /// <summary>
    /// Calcula los totales de las columnas especificadas
    /// </summary>
    private void CalculateTotals(IEnumerable<string> propertyNames)
    {
        var newTotals = new Dictionary<string, decimal>();

        foreach (var propName in propertyNames)
        {
            var property = typeof(T).GetProperty(propName);
            if (property != null)
            {
                decimal total = 0;
                foreach (var item in _filteredData)
                {
                    var value = property.GetValue(item);
                    if (value != null)
                    {
                        if (decimal.TryParse(value.ToString(), out var numValue))
                        {
                            total += numValue;
                        }
                    }
                }
                newTotals[propName] = total;
            }
        }

        ColumnTotals = newTotals;
    }

    /// <summary>
    /// Aplica filtrado y paginación
    /// </summary>
    private void ApplyFilteringAndPaging()
    {
        // Aplicar filtro
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            _filteredData = _allData.ToList();
        }
        else
        {
            if (CustomFilter != null)
            {
                _filteredData = _allData.Where(item => CustomFilter(item, SearchTerm)).ToList();
            }
            else
            {
                // Filtro por defecto: buscar en todas las propiedades string
                _filteredData = _allData.Where(item =>
                {
                    var properties = typeof(T).GetProperties()
                        .Where(p => p.PropertyType == typeof(string) || 
                                    p.PropertyType == typeof(string));
                    
                    return properties.Any(p =>
                    {
                        var value = p.GetValue(item)?.ToString();
                        return value?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false;
                    });
                }).ToList();
            }
        }

        var totalFiltered = _filteredData.Count;
        TotalPages = (int)Math.Ceiling((double)totalFiltered / PageSize);

        // Ajustar página actual si es necesario
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }

        // Aplicar paginación
        var skip = (CurrentPage - 1) * PageSize;
        var pagedData = _filteredData.Skip(skip).Take(PageSize).ToList();
        
        // Actualizar TotalRecords con los registros de la página actual
        TotalRecords = pagedData.Count;
        
        // Calcular el índice de inicio para esta página
        PageStartIndex = skip;

        PaginatedData.Clear();
        for (int i = 0; i < pagedData.Count; i++)
        {
            PaginatedData.Add(new IndexedItem<T>
            {
                RowNumber = skip + i + 1,
                Item = pagedData[i]
            });
        }

        // Recalcular totales si están habilitados
        if (ShowTotals && ColumnTotals.Any())
        {
            var propertyNames = ColumnTotals.Keys.ToList();
            CalculateTotals(propertyNames);
        }

        // Notificar cambios en comandos de navegación
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTermChanged(string? value)
    {
        CurrentPage = 1;
        ApplyFilteringAndPaging();
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        ApplyFilteringAndPaging();
    }

    partial void OnCurrentPageChanged(int value)
    {
        ApplyFilteringAndPaging();
    }

    /// <summary>
    /// Refresca los datos aplicando nuevamente filtros y paginación
    /// </summary>
    public void Refresh()
    {
        ApplyFilteringAndPaging();
    }

    /// <summary>
    /// Limpia el filtro de búsqueda
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchTerm = string.Empty;
    }
}
