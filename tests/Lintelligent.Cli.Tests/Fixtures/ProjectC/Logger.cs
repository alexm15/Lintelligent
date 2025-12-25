namespace ProjectC;

public class Logger
{
    private readonly List<string> _logs = new();
    
    public void Log(string message)
    {
        _logs.Add($"{DateTime.UtcNow:O} - {message}");
    }
    
    public IReadOnlyList<string> GetLogs()
    {
        return _logs.AsReadOnly();
    }
}
