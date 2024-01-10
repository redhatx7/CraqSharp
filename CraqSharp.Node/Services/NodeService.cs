using CraqSharp.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CraqSharp.Node.Services;

public class NodeService : CraqSharp.Grpc.Services.NodeService.NodeServiceBase
{
    public override Task<PingResponse> Ping(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new PingResponse()
        {
            NodeId = Guid.NewGuid().ToString()
        });
    }

    public override Task<ConnectResponse> Connect(Empty request, ServerCallContext context)
    {

        return Task.FromResult(new ConnectResponse()
        {
            IsHealthy = true
        });
    }

    public override async Task<Empty> Update(NodeData request, ServerCallContext context)
    {
        throw new NotImplementedException();
    }
}