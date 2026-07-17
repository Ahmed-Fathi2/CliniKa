namespace CliniKa.Application.DTOs.Sessions
{
    public class SessionDto
    {
    }

    public sealed record CreateClinicalSessionDto(
        int PatientId
    );
}
