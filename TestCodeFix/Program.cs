namespace TestCodeFix;

// Test code to trigger LNT200 diagnostic and code fix
public class UserService
{
    // This should show LNT200 diagnostic with code fix option
    public string? FindUser(int id)
    {
        if (id == 0) return null;
        if (id < 0) return null;
        if (id > 1000) return null;
        return $"User{id}";
    }

    // Another test case
    public int? Calculate(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;
        if (input.Length < 5) return null;
        return input.Length;
    }
}

class Program
{
    static void Main()
    {
        var service = new UserService();
        var user = service.FindUser(42);
        Console.WriteLine(user ?? "Not found");
    }
}
