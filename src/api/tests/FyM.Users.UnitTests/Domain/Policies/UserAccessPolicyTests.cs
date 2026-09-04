using FluentAssertions;
using FyM.Users.Domain.Exceptions;
using FyM.Users.Domain.Policies;
using Xunit;

namespace FyM.Users.UnitTests.Domain.Policies;

public sealed class UserAccessPolicyTests
{
    private readonly UserAccessPolicy _policy = new();

    private static readonly ActorContext SuperAdmin = new(Guid.NewGuid(), 100, IsSuperAdmin: true);
    private static readonly ActorContext Admin = new(Guid.NewGuid(), 50, IsSuperAdmin: false);
    private static readonly ActorContext RegularUser = new(Guid.NewGuid(), 10, IsSuperAdmin: false);

    // --- EnsureCanModify (regla 1) ---

    [Fact]
    public void EnsureCanModify_WhenTargetLevelIsLowerThanActor_DoesNotThrow()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 10, IsSystem: false);

        var act = () => _policy.EnsureCanModify(Admin, target);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanModify_WhenTargetLevelEqualsActor_Throws()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 50, IsSystem: false);

        var act = () => _policy.EnsureCanModify(Admin, target);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanModify_WhenTargetLevelIsHigherThanActor_Throws()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 100, IsSystem: false);

        var act = () => _policy.EnsureCanModify(Admin, target);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanModify_WhenTargetIsSelf_DoesNotThrowRegardlessOfLevel()
    {
        var target = new TargetUserContext(RegularUser.UserId, MaxRoleLevel: 10, IsSystem: false);

        var act = () => _policy.EnsureCanModify(RegularUser, target);

        act.Should().NotThrow();
    }

    // --- EnsureCanDeactivateOrDelete (reglas 1, 2, 3, 4) ---

    [Fact]
    public void EnsureCanDeactivateOrDelete_WhenTargetIsSelf_Throws()
    {
        var target = new TargetUserContext(Admin.UserId, MaxRoleLevel: 50, IsSystem: false);

        var act = () => _policy.EnsureCanDeactivateOrDelete(Admin, target, isLastActiveSuperAdmin: false);

        act.Should().Throw<ForbiddenOperationException>().WithMessage("*propia cuenta*");
    }

    [Fact]
    public void EnsureCanDeactivateOrDelete_WhenTargetIsSystem_Throws()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 10, IsSystem: true);

        var act = () => _policy.EnsureCanDeactivateOrDelete(SuperAdmin, target, isLastActiveSuperAdmin: false);

        act.Should().Throw<ForbiddenOperationException>().WithMessage("*sistema*");
    }

    [Fact]
    public void EnsureCanDeactivateOrDelete_WhenTargetIsLastActiveSuperAdmin_Throws()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 100, IsSystem: false);

        var act = () => _policy.EnsureCanDeactivateOrDelete(SuperAdmin, target, isLastActiveSuperAdmin: true);

        act.Should().Throw<ForbiddenOperationException>().WithMessage("*único super administrador*");
    }

    [Fact]
    public void EnsureCanDeactivateOrDelete_WhenAdminTargetsSuperAdmin_Throws()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 100, IsSystem: false);

        var act = () => _policy.EnsureCanDeactivateOrDelete(Admin, target, isLastActiveSuperAdmin: false);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanDeactivateOrDelete_WhenValid_DoesNotThrow()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 10, IsSystem: false);

        var act = () => _policy.EnsureCanDeactivateOrDelete(Admin, target, isLastActiveSuperAdmin: false);

        act.Should().NotThrow();
    }

    // --- EnsureCanGrantRoleLevel (regla 5 generalizada) ---

    [Fact]
    public void EnsureCanGrantRoleLevel_WhenNonSuperAdminRequestsLevelAboveOwn_Throws()
    {
        var act = () => _policy.EnsureCanGrantRoleLevel(Admin, maxRequestedRoleLevel: 100);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanGrantRoleLevel_WhenNonSuperAdminRequestsOwnLevel_Throws()
    {
        var act = () => _policy.EnsureCanGrantRoleLevel(Admin, maxRequestedRoleLevel: 50);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanGrantRoleLevel_WhenSuperAdminRequestsSuperAdminLevel_DoesNotThrow()
    {
        var act = () => _policy.EnsureCanGrantRoleLevel(SuperAdmin, maxRequestedRoleLevel: 100);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanGrantRoleLevel_WhenNonSuperAdminRequestsLowerLevel_DoesNotThrow()
    {
        var act = () => _policy.EnsureCanGrantRoleLevel(Admin, maxRequestedRoleLevel: 10);

        act.Should().NotThrow();
    }

    // --- EnsureCanAssignRoles (reglas 1 y 5 combinadas) ---

    [Fact]
    public void EnsureCanAssignRoles_WhenAdminTriesToGrantSuperAdminRole_Throws()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 10, IsSystem: false);

        var act = () => _policy.EnsureCanAssignRoles(Admin, target, maxRequestedRoleLevel: 100);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanAssignRoles_WhenSuperAdminGrantsSuperAdminRole_DoesNotThrow()
    {
        var target = new TargetUserContext(Guid.NewGuid(), MaxRoleLevel: 10, IsSystem: false);

        var act = () => _policy.EnsureCanAssignRoles(SuperAdmin, target, maxRequestedRoleLevel: 100);

        act.Should().NotThrow();
    }

    // --- EnsureRoleIsMutable (regla 4) ---

    [Fact]
    public void EnsureRoleIsMutable_WhenRoleIsSystem_Throws()
    {
        var act = () => _policy.EnsureRoleIsMutable(roleIsSystem: true, action: "eliminarse");

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureRoleIsMutable_WhenRoleIsNotSystem_DoesNotThrow()
    {
        var act = () => _policy.EnsureRoleIsMutable(roleIsSystem: false, action: "eliminarse");

        act.Should().NotThrow();
    }

    // --- EnsureCanSetRoleLevel ---

    [Theory]
    [InlineData(50)]
    [InlineData(60)]
    public void EnsureCanSetRoleLevel_WhenRequestedLevelIsEqualOrAboveActor_Throws(int requestedLevel)
    {
        var act = () => _policy.EnsureCanSetRoleLevel(Admin, requestedLevel);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanSetRoleLevel_WhenRequestedLevelIsBelowActor_DoesNotThrow()
    {
        var act = () => _policy.EnsureCanSetRoleLevel(Admin, 49);

        act.Should().NotThrow();
    }
}
