namespace FyM.Users.Application.Common;

/// <summary>Parámetros de consulta compartidos por los listados paginados de usuarios y roles.</summary>
public class QueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > MaxPageSize ? 10 : value;
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

/// <summary>Filtros adicionales propios del listado de usuarios.</summary>
public sealed class UserQueryParameters : QueryParameters
{
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
}
