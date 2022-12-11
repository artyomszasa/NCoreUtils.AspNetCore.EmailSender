using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NCoreUtils.AspNetCore.EmailSender
{
    public interface IMqttClientService : IHostedService
    {

        Task<int?> PublishAsync<T>(T payload, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken);
    }
}