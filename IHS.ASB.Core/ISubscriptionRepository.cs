using System.Threading.Tasks;

namespace IHS.ASB.Core
{
    public interface ISubscriptionRepository
    {
        Task Subscribe();
    }
}