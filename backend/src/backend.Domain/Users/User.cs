namespace backend.Domain.Users;

public sealed class User
{
    public int Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string PasswordHash { get; private set; }
    public UserState State { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public User(string username, string email, string passwordHash, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        Username = username.Trim();
        Email = email.Trim();
        NormalizedEmail = Email.ToUpperInvariant();
        PasswordHash = passwordHash;
        State = UserState.Active;
        CreatedAt = createdAt;
    }

    public bool ChangeState(UserState newState, DateTimeOffset changedAt)
    {
        if (!CanTransitionTo(newState))
        {
            return false;
        }

        State = newState;
        if (newState == UserState.Deleted)
        {
            DeletedAt = changedAt;
        }

        return true;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }

    private bool CanTransitionTo(UserState newState) => State switch
    {
        UserState.Active => newState is UserState.Admin or UserState.Blocked or UserState.Deleted,
        UserState.Admin => newState is UserState.Active or UserState.Deleted,
        UserState.Blocked => newState is UserState.Active or UserState.Deleted,
        UserState.Deleted => false,
        _ => false
    };
}