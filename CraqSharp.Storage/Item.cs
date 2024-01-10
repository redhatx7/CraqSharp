namespace CraqSharp.Storage;

public class Item : IItem<string, byte[]>
{
    public required string Key { get; set; }
    public required byte[] Value { get; set; }
    public required ulong Version { get; set; }
    public required bool Committed { get; set; }
}