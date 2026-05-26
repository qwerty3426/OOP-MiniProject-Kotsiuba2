namespace CarRentalSystem.Domain;

public class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }

    public Customer(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Name cannot be empty.");
        if (!email.Contains("@")) throw new ArgumentException("Invalid email format.");

        Id = Guid.NewGuid();
        FullName = fullName;
        Email = email;
    }
}