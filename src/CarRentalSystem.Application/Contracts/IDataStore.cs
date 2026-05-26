using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarRentalSystem.Application.Contracts
{
    public interface IDataStore<T>
    {
        Task<IReadOnlyCollection<T>> LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(IReadOnlyCollection<T> items, CancellationToken cancellationToken = default);
    }
}