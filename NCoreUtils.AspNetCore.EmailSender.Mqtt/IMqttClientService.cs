using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public interface IMqttClientService : IHostedService, IMqttClientConnectedHandler, IMqttClientDisconnectedHandler
    {
        Task<int?> PublishAsync<T>(T payload, CancellationToken cancellationToken);
    }
}