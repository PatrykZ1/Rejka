namespace backend.Application.Authentication;

public enum PasswordVerificationStatus
{
    Failed,
    Success,
    SuccessRehashNeeded
}