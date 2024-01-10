using CraqSharp.Coordinator.Exceptions;
using CraqSharp.Core;
using CraqSharp.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace CraqSharp.Coordinator;

public class Coordinator
{
    private readonly ILogger<Coordinator> _logger;

    private List<Node> _replicas;

    private Dictionary<string, NodeData> _nodesMetaData;

    private Node? _tail;
    private Node? _head;
    private SemaphoreSlim _semaphore;

    public int PingInterval { get; set; }
    
    
    public Coordinator(ILogger<Coordinator> logger)
    {
        _logger = logger;
        _replicas = new List<Node>();
        _nodesMetaData = new Dictionary<string, NodeData>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<NodeData> AddNode(Node node)
    {

        await _semaphore.WaitAsync();
        try
        {
            if (_replicas.Any(t => t.NodeAddress == node.NodeAddress))
                return _nodesMetaData[node.NodeAddress];

            //await node.Connect();
            

            _replicas.Add(node);
            _tail = node;

            var nodeData = new NodeData()
            {
                IsHead = _replicas.Count == 1,
                IsTail = true,
                Tail = node.NodeAddress,
                PrevNode = _replicas.Count > 1 ? _replicas[^2].NodeAddress : "",
                UniqueId = node.UniqueId
            };

            _nodesMetaData[node.NodeAddress] = nodeData;
            //return nodeData;
            var tasks = new List<Task>();
            for (int i = 0; i < _replicas.Count; i++)
            {
                var task = UpdateNodeAsync(i);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            _semaphore.Release();
            return nodeData;
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occured while Adding node {@Node}, Message: {@Ex}", node, ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }

    }

    public async Task RemoveNodeAsync(string address)
    {
        await _semaphore.WaitAsync(TimeSpan.FromSeconds(1));

        try
        {
            var node = _replicas.FirstOrDefault(t => t.NodeAddress == address);
            if (node is null)
                throw new NodeNotFoundException($"Node is null with address {address}");

            int nodeIndex = _replicas.IndexOf(node);
            int replicaLengthBeforeRemove = _replicas.Count;
            _replicas.Remove(node);
            bool isTail = nodeIndex == replicaLengthBeforeRemove - 1;
            if (isTail)
            {
                if (replicaLengthBeforeRemove == 1)
                {
                    _tail = null;
                }
                else
                {
                    _tail = _replicas.Last();
                }
            }

            //Node remains after removal and sits at position nodeIndex, update it
            if (_replicas.Count > nodeIndex)
            {
                await UpdateNodeAsync(nodeIndex);
            }

            // If it is not head, so update pred
            if (nodeIndex > 0)
            {
                await UpdateNodeAsync(nodeIndex - 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occured {@Ex}", ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task UpdateNodeAsync(int index)
    {
        if (index >= _replicas.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        await _semaphore.WaitAsync();
        var node = _replicas[index];
        var nodeMetaData = new NodeData()
        {
            IsHead = index == 0,
            IsTail = index == _replicas.Count - 1,
            UniqueId = node.UniqueId,
            Tail = _replicas.Last().NodeAddress,
            PrevNode = index > 0 ? _replicas[index - 1].NodeAddress : "",
            NextNode = index + 1 < _replicas.Count ? _replicas[index + 1].NodeAddress : ""
        };
        try
        {
            await node.Update(nodeMetaData);
        }
        catch (Exception ex)
        {
            _logger.LogError("An Exception occured while updating node: {@Node}, Message: {@Ex}", node, ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    //private async Task UpdateNodeAsync(Node node) => await UpdateNodeAsync(_replicas.Find(t => t.NodeAddress == node.NodeAddress).NodeAddress);
    
    

    private async Task PingReplicas(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Dictionary<Node, Task<bool>> dictionary = new Dictionary<Node, Task<bool>>();
            foreach (var node in _replicas)
            {
                dictionary[node] = PingNodeAsync(node);
            }

            await Task.WhenAll(dictionary.Values);

            await _semaphore.WaitAsync(ct);
            foreach (var pair in dictionary)
            {
                if (!pair.Value.Result)
                {
                    _logger.LogInformation("Node {@Node} is faulty or takes too long too respond, Removing from replicas", pair.Key.NodeAddress);
                    _replicas.Remove(pair.Key);
                }
            }

            _semaphore.Release();
            
            await Task.Delay(TimeSpan.FromSeconds(PingInterval),ct);
        }
    }


    public async Task RemoveNodeAsync(Node node) => await RemoveNodeAsync(_replicas.FirstOrDefault(t => t.NodeAddress == node.NodeAddress)?.NodeAddress ?? "");
    private async Task<bool> PingNodeAsync(Node node)
    {
        _logger.LogInformation("Pinging Node {@Node}", node.NodeAddress);
        try
        {
            var pingTask = node.Connect();
            var timeOutTask = Task.Delay(1000);
            var completedTask = await Task.WhenAny(pingTask, timeOutTask);
            if (completedTask == pingTask)
            {
                _logger.LogInformation("Node {@Node} responded successfully", node.NodeAddress);
                return true;
            }
            
        }
        finally
        {
            _logger.LogInformation("Node {@Node} ping timeout", node.NodeAddress);
        }
        return false;
    }

    public async Task WriteDataAsync(string key, ReadOnlyMemory<byte> value)
    {
        if (_replicas.Count == 0)
            throw new EmptyChainException("Chain is empty");
        var head = _replicas.First();
        try
        {
            await head.Write(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occured while Writing data to the chain, Message: {@Ex}", ex);
            throw;
        }
    }
    
    
    
}