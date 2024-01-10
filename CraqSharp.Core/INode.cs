namespace CraqSharp.Core;

public interface INode<T> 
{
    public string NodeAddress { get; init; }
    public bool IsConnected { get;  set; }
    public DateTime LastUsed { get; set; }

    public string CoordinatorAddress { get; init; }

    public string LocalAddress { get; init; }
    
    public string UniqueId { get; set; }


  
    

}