using System.Threading;
using System.Threading.Tasks;

namespace RTSC.Devices.Helpers
{
    interface IModel
    {
        Task StartAsync(CancellationToken token);
    }
}
