using System.Threading.Tasks;

namespace Slush.Application.Interfaces
{
    public interface ISnapshotService
    {
        Task TakeActivitySnapshotAsync();
        Task SyncGameCatalogAsync();
    }
}