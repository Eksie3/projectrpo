using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using project.Models;

namespace project.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly string _connectionString;
    public RequestsController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        string? status,
        string? priority,
        string sortBy = "id",
        string sortOrder = "asc",
        int page = 1,
        int pageSize = 10)
    {
        if (pageSize > 20) pageSize = 50;
        if (page < 1) page = 1;
        if (status != null && status != "new" && status != "in_progress" && status != "resolved" && status != "closed" && status != "rejected")
        return BadRequest("неверный статус");
        if (priority != null && priority != "low" && priority != "medium" && priority != "high" && priority != "critical")
        return BadRequest("неверный приоритет");
        string sql = "SELECT * FROM requests WHERE 1=1";
        if (!string.IsNullOrEmpty(status))
        {
            sql += $" AND status = '{status}'";
        }
        if (!string.IsNullOrEmpty(priority))
        {
            sql += $" AND priority = '{priority}'";
        }
        sql += $" ORDER BY {sortBy} {sortOrder}";
        sql += $" LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";


        var requests = new List<Request>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            requests.Add(new Request
            {
                Id = reader.GetInt32(0),
                AuthorId = reader.GetInt32(1),
                Title = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                Status = reader.GetString(4),
                Priority = reader.GetString(5),
                CreatedAt = reader.GetDateTime(6),
                Deadline = reader.GetDateTime(7),
            });
        }
        return Ok(requests);
    }
}


