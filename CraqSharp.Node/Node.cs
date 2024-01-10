using System.Collections.Concurrent;
using System.Threading.Channels;
using CraqSharp.Core;
using CraqSharp.Grpc.Services;
using CraqSharp.Storage;
using Grpc.Net.Client;

namespace CraqSharp.Node;

public class Node : INode<NodeData>
{
    private readonly InMemoryKv _storage;
    public string NodeAddress { get; init; }
    public bool IsConnected { get; set; }
    public DateTime LastUsed { get; set; }
    public required string CoordinatorAddress { get; init; }
    public string LocalAddress { get; init; }
    public string UniqueId { get; set; }
    
    public bool IsHead { get; set; }
    public bool IsTail { get; set; }

    public string NextAddress { get; set; }
    public string PrevAddress { get; set; }
    

    private readonly GrpcChannel _channel;
    private readonly CoordinatorService.CoordinatorServiceClient _client;

    private readonly Dictionary<string, ulong> _latestVersions = new();
    

    public Node(InMemoryKv storage)
    {
        _storage = storage;
        _channel = GrpcChannel.ForAddress(CoordinatorAddress!);
        _client = new CoordinatorService.CoordinatorServiceClient(_channel);
    }
    
    
    public async Task ConnectToCoordinator()
    {
        var reply = await _client.AddNodeAsync(new AddNodeRequest()
        {
            NodeAddress = this.NodeAddress
        });
        if (reply is not null)
        {
            this.UniqueId = reply.UniqueId;
            this.IsTail = reply.IsTail;
            this.IsHead = reply.IsHead;
            this.PrevAddress = reply.PrevNode;
            this.NextAddress = reply.NextNode;
        }
        else
        {
            throw new Exception("Unable to connect to coordinator");
        }
    }

    public Task Update(NodeData data)
    {
        throw new NotImplementedException();
    }

    public async Task Write(string key, byte[] value)
    {
        
    }

    private async Task FillLatestVersions()
    {
        foreach (var item in await _storage.GetAllCommitted())
        {
            _latestVersions[item.Key] = item.Version;
        }
    }
}