namespace Movement_Home_Task.Models.DTOs
{
    public record LoginByIdResponse
    (
        string Id,
        string Role,
        DateTime CreatedAt,
        string JwtToken
    );
}
