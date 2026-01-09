using System.Threading.Tasks;
using ADOApi.Models;

namespace ADOApi.Interfaces
{
    public interface IAuditLogger
    {
        Task AuditAsync(AuditEvent evt);
    }
}
