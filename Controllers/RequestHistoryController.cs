using Microsoft.AspNetCore.Mvc;
using Npgsql;
using project.Models;

namespace project.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestHistoryController : ControllerBase
{
    private readonly string _connectionString;

    public RequestHistoryController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        string checkSql = "SELECT status FROM requests WHERE id = @id";
        await using var checkCmd = new NpgsqlCommand(checkSql, conn);
        checkCmd.Parameters.AddWithValue("id", id);

        string? currentStatus = (string?)await checkCmd.ExecuteScalarAsync();

        if (currentStatus == null)
            return NotFound(new { message = "Заявка не найдена" });

        if (currentStatus == "closed" || currentStatus == "resolved")
            return BadRequest(new { message = "Нельзя изменить статус завершенной заявки" });

        string updateSql = "UPDATE requests SET status = @newStatus, updated_at = NOW() WHERE id = @id";
        await using var updateCmd = new NpgsqlCommand(updateSql, conn);
        updateCmd.Parameters.AddWithValue("newStatus", dto.NewStatus);
        updateCmd.Parameters.AddWithValue("id", id);
        await updateCmd.ExecuteNonQueryAsync();

        string historySql = @"INSERT INTO status_history (request_id, changed_by, old_status, new_status, changed_at, comment) 
                              VALUES (@requestId, @changedBy, @oldStatus, @newStatus, NOW(), @comment)";
        await using var historyCmd = new NpgsqlCommand(historySql, conn);
        historyCmd.Parameters.AddWithValue("requestId", id);
        historyCmd.Parameters.AddWithValue("changedBy", dto.ChangedBy);
        historyCmd.Parameters.AddWithValue("oldStatus", currentStatus);
        historyCmd.Parameters.AddWithValue("newStatus", dto.NewStatus);
        historyCmd.Parameters.AddWithValue("comment", (object?)dto.Comment ?? DBNull.Value);
        await historyCmd.ExecuteNonQueryAsync();

        return Ok(new { message = "Статус успешно изменен" });
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignUser(int id, [FromBody] AssignUserDto dto)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        string checkSql = "SELECT status FROM requests WHERE id = @id";
        await using var checkCmd = new NpgsqlCommand(checkSql, conn);
        checkCmd.Parameters.AddWithValue("id", id);
        string? currentStatus = (string?)await checkCmd.ExecuteScalarAsync();

        if (currentStatus == null)
            return NotFound(new { message = "Заявка не найдена" });

        string assignSql = @"INSERT INTO assignments (request_id, user_id, assigned_at, status) 
                             VALUES (@requestId, @userId, NOW(), 'assigned')";
        await using var assignCmd = new NpgsqlCommand(assignSql, conn);
        assignCmd.Parameters.AddWithValue("requestId", id);
        assignCmd.Parameters.AddWithValue("userId", dto.UserId);
        await assignCmd.ExecuteNonQueryAsync();

        string historySql = @"INSERT INTO status_history (request_id, changed_by, old_status, new_status, changed_at, comment) 
                              VALUES (@requestId, @changedBy, @oldStatus, @newStatus, NOW(), @comment)";
        await using var historyCmd = new NpgsqlCommand(historySql, conn);
        historyCmd.Parameters.AddWithValue("requestId", id);
        historyCmd.Parameters.AddWithValue("changedBy", dto.UserId);
        historyCmd.Parameters.AddWithValue("oldStatus", currentStatus);
        historyCmd.Parameters.AddWithValue("newStatus", currentStatus);
        historyCmd.Parameters.AddWithValue("comment", $"Назначен исполнитель ID {dto.UserId}. {dto.Comment}");
        await historyCmd.ExecuteNonQueryAsync();

        return Ok(new { message = "Исполнитель назначен" });
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var history = new List<RequestHistory>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "SELECT id, request_id, old_status, new_status, changed_by, changed_at, comment FROM status_history WHERE request_id = @id ORDER BY changed_at DESC";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            history.Add(new RequestHistory
            {
                Id = reader.GetInt32(0),
                RequestId = reader.GetInt32(1),
                OldStatus = reader.IsDBNull(2) ? null : reader.GetString(2),
                NewStatus = reader.IsDBNull(3) ? null : reader.GetString(3),
                ChangedBy = reader.GetInt32(4),
                ChangedAt = reader.GetDateTime(5),
                Comment = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return Ok(history);
    }
}
