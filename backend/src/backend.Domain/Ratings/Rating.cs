namespace backend.Domain.Ratings;

public sealed class Rating
{
    public string RegistrationNumber { get; private set; }
    public int Value { get; private set; }

    public Rating(string registrationNumber, int value)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            throw new ArgumentException("Registration number cannot be empty.", nameof(registrationNumber));
        }

        RegistrationNumber = registrationNumber.Trim().ToUpperInvariant();
        Value = value;
    }
}
