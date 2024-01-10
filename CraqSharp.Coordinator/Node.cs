using System.Diagnostics.CodeAnalysis;
using CraqSharp.Core;
using CraqSharp.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace CraqSharp.Coordinator;

public class Node : INode<NodeData>
{
   
    public required string NodeAddress { get; init; }
    public bool IsConnected { get; set; }
    public DateTime LastUsed { get; set; }
    public string CoordinatorAddress { get; init; }
    public string LocalAddress { get; init; }
    public string UniqueId { get; set; }


    private readonly GrpcChannel _channel;
    private readonly NodeService.NodeServiceClient _client;
    
    [SetsRequiredMembers]
    public Node(string nodeAddress)
    {
        NodeAddress = nodeAddress;
       // _channel = GrpcChannel.ForAddress(NodeAddress);
        //_client = new NodeService.NodeServiceClient(_channel);
    }

   
    public async Task Connect()
    {
        var reply = await _client.PingAsync(new Empty());
        if (reply is not null)
        {
            IsConnected = true;
        }
        else
        {
            IsConnected = false;
        }
    }

    public Task Update(NodeData data)
    {
        throw new NotImplementedException();
    }
    
    public Task Write(string key, ReadOnlyMemory<byte> values)
    {
        throw new NotImplementedException();
    }
}