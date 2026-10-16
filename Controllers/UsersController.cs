using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace project.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase {
    private readonly string? _connectionString;

    public UsersController(IConfiguration configuration) {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetUsersByRole(int roleId) {
        var users = new List<object>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT id, login, full_name, email FROM users WHERE role_id = @roleId", conn);

        cmd.Parameters.AddWithValue("roleId",roleId);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync()) {
            users.Add(new {Id = reader.GetInt32(0),Login = reader.GetString(1),FullName = reader.GetString(2),Email = reader.GetString(3)});
        }
        return Ok(users);
    }
}
