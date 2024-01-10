using CraqSharp.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CraqSharp.Coordinator.Services;

public class CoordinatorService(
    ILogger<CoordinatorService> logger,
    Coordinator coordinator)
    : CraqSharp.Grpc.Services.CoordinatorService.CoordinatorServiceBase
{
    private readonly ILogger<CoordinatorService> _logger = logger;

    public override async Task<NodeData> AddNode(AddNodeRequest request, ServerCallContext context)
    {
        var node = new Node(request.NodeAddress)
        {
            CoordinatorAddress = context.Host,
            LastUsed = DateTime.UtcNow,
            UniqueId = Guid.NewGuid().ToString(),
            NodeAddress = request.NodeAddress,
        };

        var result = await coordinator.AddNode(node);
        return result;
    }

    public override Task<Empty> RemoveNode(RemoveNodeRequest request, ServerCallContext context)
    {
        return Task.FromResult(new Empty());
    }
    
    public override Task<WriteResponse> Write(WriteRequest request, ServerCallContext context)
    {
        return Task.FromResult(new WriteResponse()
        {
            VersionNumber = (ulong)(Random.Shared.Next(0,1000))
        });
    }
}