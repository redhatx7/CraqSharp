namespace CraqSharp.Storage;

public interface IStorage<in TKey, in TValue, TItem> where TItem: IItem<TKey,TValue> 
{
    public Task Commit(TKey key, ulong version);

    public Task<TItem?> Read(TKey key);

    public Task<TItem?> ReadByVersion(TKey key, ulong version);
    
    public Task Write(TKey key, TValue value, ulong version);

    public Task<IEnumerable<TItem>> GetAllDirty();

    public Task<IEnumerable<TItem>> GetAllCommitted();

    public Task<IEnumerable<TItem>> GetAllNewerCommitted(Dictionary<string, ulong> keyVersions);
    
    public Task<IEnumerable<TItem>> GetAllNewerDirty(Dictionary<string, ulong> keyVersions);

}