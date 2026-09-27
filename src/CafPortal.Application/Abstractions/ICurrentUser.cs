namespace CafPortal.Application.Abstractions;

/// <summary>The authenticated user for the current request (used to stamp updated-by on governance writes).</summary>
public interface ICurrentUser
{
    int? Id { get; }
    string? Name { get; }
    string? Role { get; }
}
