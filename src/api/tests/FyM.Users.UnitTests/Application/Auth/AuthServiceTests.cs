using FluentAssertions;
using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Auth;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;
using FyM.Users.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace FyM.Users.UnitTests.Application.Auth;

public sealed class AuthServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly AuthOptions _authOptions = new() { AllowSelfRegistration = true };

    private readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private AuthService CreateSut() => new(
        _userRepository, _roleRepository, _refreshTokenRepository, _unitOfWork,
        _passwordHasher, _tokenService, _dateTimeProvider, _authOptions);

    public AuthServiceTests()
    {
        _dateTimeProvider.UtcNow.Returns(_now);
        _tokenService.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<int>())
            .Returns(new AccessTokenResult("access-token", 900));
        _tokenService.GenerateRefreshToken().Returns("raw-refresh-token");
        _tokenService.HashRefreshToken(Arg.Any<string>()).Returns("hashed-refresh-token");
    }

    private static User CreateActiveUser(string passwordHash = "hash") => new()
    {
        Id = Guid.NewGuid(),
        UserName = "jdoe",
        Email = "jdoe@fym.com",
        NormalizedEmail = "JDOE@FYM.COM",
        PasswordHash = passwordHash,
        IsActive = true,
        Profile = new UserProfile { FirstName = "John", LastName = "Doe" },
    };

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndResetsFailedCount()
    {
        var user = CreateActiveUser();
        user.AccessFailedCount = 3;
        _userRepository.GetByEmailAsync("JDOE@FYM.COM", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct-password", user.PasswordHash).Returns(true);

        var sut = CreateSut();
        var result = await sut.LoginAsync(new LoginRequest("jdoe@fym.com", "correct-password"), "127.0.0.1", "test-agent", CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        user.AccessFailedCount.Should().Be(0);
        user.LastLoginUtc.Should().Be(_now);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsAndIncrementsFailedCount()
    {
        var user = CreateActiveUser();
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest("jdoe@fym.com", "wrong"), null, null, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        user.AccessFailedCount.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_OnFifthFailedAttempt_LocksAccount()
    {
        var user = CreateActiveUser();
        user.AccessFailedCount = 4;
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest("jdoe@fym.com", "wrong"), null, null, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEndUtc.Should().Be(_now.AddMinutes(15));
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsLockedOut_ThrowsForbidden()
    {
        var user = CreateActiveUser();
        user.LockoutEndUtc = _now.AddMinutes(5);
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest("jdoe@fym.com", "whatever"), null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsInactive_ThrowsForbidden()
    {
        var user = CreateActiveUser();
        user.IsActive = false;
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest("jdoe@fym.com", "whatever"), null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ThrowsBusinessRuleException()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest("nobody@fym.com", "whatever"), null, null, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedToken_RevokesAllActiveTokensAndThrows()
    {
        var userId = Guid.NewGuid();
        var stored = new RefreshToken { UserId = userId, TokenHash = "hashed-refresh-token", RevokedAtUtc = _now.AddMinutes(-1), ExpiresAtUtc = _now.AddDays(1) };
        _refreshTokenRepository.GetByTokenHashAsync("hashed-refresh-token", Arg.Any<CancellationToken>()).Returns(stored);

        var sut = CreateSut();
        var act = () => sut.RefreshAsync("raw-refresh-token", "10.0.0.1", null, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>().WithMessage("*cerraron*");
        await _refreshTokenRepository.Received(1).RevokeAllActiveForUserAsync(userId, "10.0.0.1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_Throws()
    {
        var stored = new RefreshToken { UserId = Guid.NewGuid(), TokenHash = "hashed-refresh-token", ExpiresAtUtc = _now.AddMinutes(-1) };
        _refreshTokenRepository.GetByTokenHashAsync("hashed-refresh-token", Arg.Any<CancellationToken>()).Returns(stored);

        var sut = CreateSut();
        var act = () => sut.RefreshAsync("raw-refresh-token", null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownToken_Throws()
    {
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var sut = CreateSut();
        var act = () => sut.RefreshAsync("raw-refresh-token", null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task RegisterAsync_WhenSelfRegistrationDisabled_Throws()
    {
        _authOptions.AllowSelfRegistration = false;
        var sut = CreateSut();

        var act = () => sut.RegisterAsync(
            new RegisterRequest("newuser", "new@fym.com", "Passw0rd!", "New", "User", null, null, null),
            null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenOperationException>();
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsConflict()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(true);
        var sut = CreateSut();

        var act = () => sut.RegisterAsync(
            new RegisterRequest("newuser", "existing@fym.com", "Passw0rd!", "New", "User", null, null, null),
            null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_Throws()
    {
        var user = CreateActiveUser();
        _userRepository.GetByIdAsync(user.Id, false, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("wrong", "NewPassw0rd!"), null, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectPassword_RotatesSecurityStampAndRevokesTokens()
    {
        var user = CreateActiveUser();
        var originalStamp = user.SecurityStamp;
        _userRepository.GetByIdAsync(user.Id, false, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("new-hash");

        var sut = CreateSut();
        await sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("correct", "NewPassw0rd!"), "127.0.0.1", CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        user.SecurityStamp.Should().NotBe(originalStamp);
        await _refreshTokenRepository.Received(1).RevokeAllActiveForUserAsync(user.Id, "127.0.0.1", Arg.Any<CancellationToken>());
    }
}
