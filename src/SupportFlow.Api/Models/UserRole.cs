namespace SupportFlow.Api.Models;

public static class UserRole
{
    public const string Customer = "Customer";
    public const string Agent = "Agent";
    public const string Admin = "Admin";

    public static readonly string[] SupportTeam = [Agent, Admin];
}
