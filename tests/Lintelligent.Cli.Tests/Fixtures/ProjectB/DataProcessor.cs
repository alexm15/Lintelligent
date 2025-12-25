namespace ProjectB;

public class DataProcessor
{
    public List<string> ProcessData(string[] items)
    {
        var result = new List<string>();
        
        foreach (var item in items)
        {
            result.Add(item.ToUpper());
        }
        
        return result;
    }
}
