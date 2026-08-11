using ClinicHub.Domain.Patients;

namespace ClinicHub.Application.Patients.Dtos;

public sealed record PatientListItemDto(
    Guid Id,
    string Name,
    string EmailMasked,
    string PhoneMasked,
    bool IsActive)
{
    public static PatientListItemDto FromDomain(Patient patient) => new(
        patient.Id,
        patient.Name.Value,
        MaskEmail(patient.Email.Value),
        MaskPhone(patient.Phone.Value),
        patient.IsActive);

    private static string MaskEmail(string email)
    {
        var separatorIndex = email.IndexOf('@');
        if (separatorIndex <= 0)
        {
            return "***";
        }

        return $"{email[0]}***{email[separatorIndex..]}";
    }

    private static string MaskPhone(string phone) =>
        phone.Length <= 4 ? "****" : $"{new string('*', phone.Length - 4)}{phone[^4..]}";
}
