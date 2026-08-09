using ClinicHub.Domain.Common;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Patients;

public sealed class Patient : AggregateRoot
{
    private Patient() : base()
    {
        Name = null!;
        Email = null!;
        Phone = null!;
    }

    private Patient(Guid id, PersonName name, DateOnly birthDate, EmailAddress email, PhoneNumber phone) : base(id)
    {
        Name = name;
        BirthDate = birthDate;
        Email = email;
        Phone = phone;
        IsActive = true;
    }

    public PersonName Name { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public EmailAddress Email { get; private set; }
    public PhoneNumber Phone { get; private set; }
    public bool IsActive { get; private set; }

    public static DomainResult<Patient> Create(Guid id, PersonName name, DateOnly birthDate, EmailAddress email, PhoneNumber phone, DateOnly today)
    {
        if (id == Guid.Empty)
        {
            return DomainResult<Patient>.Failure(new("patient.id.required", "O identificador do paciente é obrigatório."));
        }

        if (birthDate >= today)
        {
            return DomainResult<Patient>.Failure(new("patient.birth_date.invalid", "A data de nascimento deve ser anterior à data atual."));
        }

        return DomainResult<Patient>.Success(new Patient(id, name, birthDate, email, phone));
    }

    public DomainResult UpdateContact(EmailAddress email, PhoneNumber phone)
    {
        Email = email;
        Phone = phone;
        return DomainResult.Success();
    }

    public DomainResult UpdateProfile(PersonName name, DateOnly birthDate, EmailAddress email, PhoneNumber phone, DateOnly today)
    {
        if (!IsActive)
        {
            return DomainResult.Failure(new("patient.inactive", "Não é permitido alterar um paciente inativo."));
        }

        if (birthDate >= today)
        {
            return DomainResult.Failure(new("patient.birth_date.invalid", "A data de nascimento deve ser anterior à data atual."));
        }

        Name = name;
        BirthDate = birthDate;
        Email = email;
        Phone = phone;
        return DomainResult.Success();
    }

    public DomainResult Deactivate()
    {
        if (!IsActive)
        {
            return DomainResult.Failure(new("patient.inactive", "O paciente já está inativo."));
        }

        IsActive = false;
        return DomainResult.Success();
    }
}
