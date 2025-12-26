namespace ProjectB;

public static class Validator
{
    public static bool IsValid(string? input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }
}