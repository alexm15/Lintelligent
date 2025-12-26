public class UserService
{
    public void CreateUser(string email, string name)
    {
        var user = new User { Name = name };
        
        // DUPLICATED validation block
        if (string.IsNullOrEmpty(email))
            throw new ArgumentException("Email required");
        if (!email.Contains("@"))
            throw new ArgumentException("Invalid email");
        if (email.Length > 255)
            throw new ArgumentException("Email too long");
        
        user.Email = email;
        SaveUser(user);
    }
    
    public void UpdateEmail(int userId, string email)
    {
        var user = GetUser(userId);
        
        // SAME validation block (will be detected as duplication!)
        if (string.IsNullOrEmpty(email))
            throw new ArgumentException("Email required");
        if (!email.Contains("@"))
            throw new ArgumentException("Invalid email");
        if (email.Length > 255)
            throw new ArgumentException("Email too long");
        
        user.Email = email;
        UpdateUser(user);
    }
}
