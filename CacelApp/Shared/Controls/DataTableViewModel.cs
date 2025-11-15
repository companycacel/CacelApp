using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CacelApp.Shared.Controls;

/// <summary>
/// Wrapper para agregar índice a cada elemento
/// </summary>
public class IndexedItem<T>
{
    public int RowNumber { get; set; }
    public T Item { get; set; } = default!;
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

        // Actualizar estado de comandos
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
