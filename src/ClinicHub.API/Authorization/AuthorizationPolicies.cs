namespace ClinicHub.API.Authorization;

public static class AuthorizationPolicies
{
    public const string PatientsRead = "patients.read";
    public const string PatientsWrite = "patients.write";
    public const string PatientsDeactivate = "patients.deactivate";
    public const string AppointmentsManage = "appointments.manage";
    public const string FinancialRead = "financial.read";
    public const string PaymentsManage = "payments.manage";
    public const string DoctorsRead = "doctors.read";
    public const string PatientPortalAccess = "patient-portal.access";
}
