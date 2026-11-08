using backend.Domain.Users;

namespace backend.UnitTests
{
    public class UserTests
    {
        [Theory]
        [InlineData(UserState.Active, UserState.Admin)]
        [InlineData(UserState.Active, UserState.Blocked)]
        [InlineData(UserState.Active, UserState.Deleted)]
        [InlineData(UserState.Admin, UserState.Active)]
        [InlineData(UserState.Admin, UserState.Deleted)]
        [InlineData(UserState.Blocked, UserState.Active)]
        [InlineData(UserState.Blocked, UserState.Deleted)]
        public void ChangeState_AllowsDocumentedTransitions(UserState initialState, UserState newState)
        {
            var user = CreateUserInState(initialState);

            var changed = user.ChangeState(newState, DateTimeOffset.UtcNow);

            Assert.True(changed);
            Assert.Equal(newState, user.State);
            Assert.Equal(newState == UserState.Deleted, user.DeletedAt.HasValue);
        }

        [Theory]
        [InlineData(UserState.Active, UserState.Active)]
        [InlineData(UserState.Admin, UserState.Blocked)]
        [InlineData(UserState.Blocked, UserState.Admin)]
        [InlineData(UserState.Deleted, UserState.Active)]
        public void ChangeState_RejectsUndocumentedTransitions(UserState initialState, UserState newState)
        {
            var user = CreateUserInState(initialState);

            var changed = user.ChangeState(newState, DateTimeOffset.UtcNow);

            Assert.False(changed);
            Assert.Equal(initialState, user.State);
        }

        [Fact]
        public void Constructor_InitializesActiveUserWithoutStoringPlaintextPassword()
        {
            var createdAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
            var user = new User("driver", "driver@example.com", "hashed-password", createdAt);

            Assert.Equal(UserState.Active, user.State);
            Assert.Equal("hashed-password", user.PasswordHash);
            Assert.Equal("DRIVER@EXAMPLE.COM", user.NormalizedEmail);
            Assert.Null(user.DeletedAt);
            Assert.Equal(createdAt, user.CreatedAt);
        }

        private static User CreateUserInState(UserState state)
        {
            var user = new User("driver", "driver@example.com", "hashed-password", DateTimeOffset.UtcNow);
            if (state != UserState.Active)
            {
                user.ChangeState(state, DateTimeOffset.UtcNow);
            }

            return user;
        }
    }
}