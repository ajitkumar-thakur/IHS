using System.Threading.Tasks;

namespace IHS.ASB.Core
{
    public interface IMessageRepository
    {
        Task Publish();
    }
}
