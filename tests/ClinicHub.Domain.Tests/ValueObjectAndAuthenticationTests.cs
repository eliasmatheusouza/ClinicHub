using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Payments;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Tests;

public sealed class ValueObjectAndAuthenticationTests
{
    private static readonly DateTime Now = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("", "person_name.required")]
    [InlineData("A1", "person_name.invalid")]
    public void PersonName_CreateInvalid_ReturnsNotification(string value, string code)
    {
        var result = PersonName.Create(value);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == code);
    }

    [Fact]
    public void ContactAndMoney_ValueObjectsNormalizeAndValidate()
    {
        var email = EmailAddress.Create(" ANA@CLINICHUB.LOCAL ");
        var phone = PhoneNumber.Create("(11) 99999-9999");
        var money = Money.Create(125.50m, "brl");

        Assert.Equal("ana@clinichub.local", email.Value!.Value);
        Assert.Equal("11999999999", phone.Value!.Value);
        Assert.Equal("BRL", money.Value!.Currency);
        Assert.Equal(125.50m, money.Value.Amount);
    }

    [Fact]
    public void AppointmentSlot_WhenNonUtc_ReturnsNotification()
    {
        var result = AppointmentSlot.Create(DateTime.Now, TimeSpan.FromMinutes(30));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == "appointment_slot.timezone");
    }

    [Fact]
    public void RefreshToken_Revoke_ChangesActiveState()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(1), Now).Value!;

        var result = token.Revoke(Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.False(token.IsActive(Now.AddMinutes(2)));
        Assert.NotNull(token.RevokedAtUtc);
    }

    [Fact]
    public void User_AndPayment_CreateValidAggregates()
    {
        var user = User.Create(Guid.NewGuid(), EmailAddress.Create("doctor@clinichub.local").Value!, "hash", UserRole.Doctor);
        var payment = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Create(200m).Value!,
            PaymentMethod.Pix,
            Now,
            Now);

        Assert.True(user.IsSuccess);
        Assert.Equal(UserRole.Doctor, user.Value!.Role);
        Assert.True(payment.IsSuccess);
        Assert.Equal(PaymentMethod.Pix, payment.Value!.Method);
    }

    [Fact]
    public void ConfirmEmail_WhenPendingTokenIsValid_ActivatesUserAndConsumesToken()
    {
        var user = User.CreatePending(
            Guid.NewGuid(),
            EmailAddress.Create("patient@clinichub.local").Value!,
            "hash",
            UserRole.Patient,
            "confirmation-hash",
            Now.AddHours(24),
            Now).Value!;

        var result = user.ConfirmEmail("confirmation-hash", Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.Equal(Now.AddMinutes(1), user.EmailConfirmedAtUtc);
        Assert.Null(user.EmailConfirmationTokenHash);
        Assert.Null(user.EmailConfirmationExpiresAtUtc);
    }

    [Fact]
    public void ConfirmEmail_WhenTokenIsExpired_ReturnsExpiryNotification()
    {
        var user = User.CreatePending(
            Guid.NewGuid(),
            EmailAddress.Create("patient@clinichub.local").Value!,
            "hash",
            UserRole.Patient,
            "confirmation-hash",
            Now.AddMinutes(1),
            Now).Value!;

        var result = user.ConfirmEmail("confirmation-hash", Now.AddHours(1));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == "user.email_confirmation.expired");
        Assert.False(user.IsActive);
    }

    [Fact]
    public void ConfirmEmail_WhenTokenWasAlreadyConsumed_ReturnsInvalidNotification()
    {
        var user = User.CreatePending(
            Guid.NewGuid(),
            EmailAddress.Create("patient@clinichub.local").Value!,
            "hash",
            UserRole.Patient,
            "confirmation-hash",
            Now.AddHours(24),
            Now).Value!;
        user.ConfirmEmail("confirmation-hash", Now.AddMinutes(1));

        var result = user.ConfirmEmail("confirmation-hash", Now.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == "user.email_confirmation.invalid");
    }

    [Fact]
    public void Money_AndPayment_WhenInvalid_ReturnNotifications()
    {
        var invalidMoney = Money.Create(1.999m, "BRL");
        var invalidPayment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), Money.Create(10m).Value!, PaymentMethod.Cash, Now.AddMinutes(1), Now);

        Assert.False(invalidMoney.IsSuccess);
        Assert.Contains(invalidMoney.Notifications, notification => notification.Code == "money.amount.invalid");
        Assert.False(invalidPayment.IsSuccess);
        Assert.Contains(invalidPayment.Notifications, notification => notification.Code == "payment.date.invalid");
    }

    [Fact]
    public void RefreshToken_WhenExpired_CannotBeRevoked()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddMinutes(1), Now).Value!;

        var result = token.Revoke(Now.AddHours(1));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == "refresh_token.inactive");
    }
}
