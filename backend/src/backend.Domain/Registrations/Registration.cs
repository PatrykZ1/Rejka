namespace backend.Domain.Registrations;

public sealed class Registration
{
    public int Id { get; private set; }
    public string Number { get; private set; }

    public Registration(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Registration number cannot be empty.", nameof(number));
        }

        Number = number.Trim().ToUpperInvariant();
    }

    public Registration(int id, string number)
    {
        Id = id;
        Number = number;
    }

    public void SetId(int id)
    {
        Id = id;
    }
}
