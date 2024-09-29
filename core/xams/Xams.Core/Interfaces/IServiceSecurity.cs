using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

/// <summary>
/// Defines a setup of security rules for which permissions can be granted.
/// </summary>
public interface IServiceSecurity
{
    public Task<Response<object?>> Execute(SecurityContext context);
}