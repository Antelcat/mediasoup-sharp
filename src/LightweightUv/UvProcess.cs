namespace LightweightUv;

public partial class UvProcess
{
    public int Id { get; }
    
    private UvProcess(int id)
    {
        Id = id;
    }
}