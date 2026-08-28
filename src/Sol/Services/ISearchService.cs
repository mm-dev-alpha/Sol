using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

public interface ISearchService
{
    Task<IEnumerable<AdUser>> SearchUsersAsync(string query, CancellationToken cancellationToken = default);
}
