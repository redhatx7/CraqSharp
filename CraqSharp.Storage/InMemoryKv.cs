using System.Collections.Concurrent;
using System.Collections.Immutable;
using CraqSharp.Core;

namespace CraqSharp.Storage;

public class InMemoryKv : IStorage<string, byte[], Item>
{

    private readonly Dictionary<string, List<Item>> _store = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Commit(string key, ulong version)
    {
        await _semaphore.WaitAsync();
        try
        {
            bool ok = _store.TryGetValue(key, out var list);
            if (!ok)
                throw new KeyNotFoundException("Given key is not present in dictionary");

            int index = list!.FindIndex(t => t.Version == version);
            if (index > -1)
                list[index].Committed = true;

            _store[key] = list[index..];
        }
        finally
        {
            _semaphore.Release();
        }
        
    }

    public async Task<Item?> Read(string key)
    {
        await _semaphore.WaitAsync();
        try
        {
            bool ok = _store.TryGetValue(key, out var list);
            if (!ok)
            {
                return null;
            }

            var last = list!.Last();
            if (last.Committed == false)
            {
                return null;
            }

            return last;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Item?> ReadByVersion(string key, ulong version)
    {
        await _semaphore.WaitAsync();
        bool ok = _store.TryGetValue(key, out var list);
        if (!ok)
            return null;

        var item = list!.FirstOrDefault(t => t.Version == version);
        _semaphore.Release();
        return item;
    }

    public async Task Write(string key, byte[] value, ulong version)
    {
        await _semaphore.WaitAsync();
        if(_store.ContainsKey(key) == false)
            _store.Add(key, new List<Item>());

        var list = _store[key];
        var item = new Item()
        {
            Committed = false,
            Key = key,
            Value = value,
            Version = version
        };
        list.Add(item);
        _semaphore.Release();

    }

    public async Task<IEnumerable<Item>> GetAllDirty()
    {
        await _semaphore.WaitAsync();
        var dirties = new List<Item>(capacity: _store.Values.Sum(t => t.Count));
        foreach (var key in _store.Keys)
        {
            var uncommitted = _store[key].Where(t => t.Committed == false).ToImmutableList();
            dirties.AddRange(uncommitted);
        }

        _semaphore.Release();
        return dirties;
    }

    public async Task<IEnumerable<Item>> GetAllCommitted()
    {
        await _semaphore.WaitAsync();
        var allCommitted = new List<Item>(capacity: _store.Values.Sum(t => t.Count));
        foreach (var key in _store.Keys)
        {
            var committed = _store[key].Where(t => t.Committed).ToImmutableList();
            allCommitted.AddRange(committed);
        }

        _semaphore.Release();
        return allCommitted;
    }

    public async Task<IEnumerable<Item>> GetAllNewerCommitted(Dictionary<string, ulong> keyVersions)
    {
        await _semaphore.WaitAsync();
        var newer = new List<Item>();
        foreach (var (key, value ) in _store)
        {
            var last = value.Last();
            bool exists = keyVersions.TryGetValue(key, out var version);
            if (last.Committed && (!exists || last.Version > version))
            {
                newer.Add(last);
            }
        }

        _semaphore.Release();
        return newer;
    }

    public async Task<IEnumerable<Item>> GetAllNewerDirty(Dictionary<string, ulong> keyVersions)
    {
        await _semaphore.WaitAsync();
        var newerDirties = new List<Item>();
        foreach (var (key, value) in _store)
        {
            var last = value.Last();
            bool exists = keyVersions.TryGetValue(key, out var version);
            if(!last.Committed && (!exists || last.Version > version))
                newerDirties.Add(last);
        }

        _semaphore.Release();
        return newerDirties;
    }
}