using Microsoft.AspNetCore.Mvc;
using Npgsql;
using project.Models;

namespace project.Controllers;

[ApiController]
[Route("api/requests/{requestId}/comments")]
public class RequestCommentsController : ControllerBase
{
    private readonly string _connectionString;
    private readonly string _uploadFolder;

    public RequestCommentsController(IConfiguration configuration, IWebHostEnvironment env)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _uploadFolder = Path.Combine(env.ContentRootPath, "Uploads");
        
        if (!Directory.Exists(_uploadFolder))
            Directory.CreateDirectory(_uploadFolder);
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(int requestId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var checkReq = new NpgsqlCommand("SELECT id FROM requests WHERE id = @id", conn);
        checkReq.Parameters.AddWithValue("id", requestId);
        if (await checkReq.ExecuteScalarAsync() == null)
            return NotFound("Заявка не найдена");

        var comments = new List<CommentDto>();

        string sql = @"SELECT c.id, c.request_id, c.user_id, u.full_name, c.text, c.created_at 
                       FROM requestcomments c 
                       JOIN users u ON u.id = c.user_id 
                       WHERE c.request_id = @reqId 
                       ORDER BY c.created_at ASC";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("reqId", requestId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            comments.Add(new CommentDto
            {
                Id = reader.GetInt32(0),
                RequestId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                AuthorName = reader.GetString(3),
                Text = reader.GetString(4),
                CreatedAt = reader.GetDateTime(5)
            });
        }

        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment(int requestId, [FromBody] CreateCommentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest("Текст комментария не может быть пустым");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var checkReq = new NpgsqlCommand("SELECT id FROM requests WHERE id = @id", conn);
        checkReq.Parameters.AddWithValue("id", requestId);
        if (await checkReq.ExecuteScalarAsync() == null)
            return NotFound("Заявка не найдена");

        int userId = 1;

        string sql = "INSERT INTO requestcomments (request_id, user_id, text) VALUES (@reqId, @userId, @text) RETURNING id, created_at";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("reqId", requestId);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("text", dto.Text);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        
        return Ok(new { id = reader.GetInt32(0), createdAt = reader.GetDateTime(1) });
    }

    [HttpPut("{commentId}")]
    public async Task<IActionResult> UpdateComment(int requestId, int commentId, [FromBody] UpdateCommentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest("Текст комментария не может быть пустым");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var checkReq = new NpgsqlCommand("SELECT id FROM requests WHERE id = @id", conn);
        checkReq.Parameters.AddWithValue("id", requestId);
        if (await checkReq.ExecuteScalarAsync() == null)
            return NotFound("Заявка не найдена");

        await using var checkComment = new NpgsqlCommand("SELECT request_id FROM requestcomments WHERE id = @id", conn);
        checkComment.Parameters.AddWithValue("id", commentId);
        var actualRequestId = await checkComment.ExecuteScalarAsync();

        if (actualRequestId == null)
            return NotFound("Комментарий не найден");

        if ((int)actualRequestId != requestId)
            return BadRequest("Комментарий принадлежит другой заявке");

        await using var updateCmd = new NpgsqlCommand("UPDATE requestcomments SET text = @text WHERE id = @id", conn);
        updateCmd.Parameters.AddWithValue("text", dto.Text);
        updateCmd.Parameters.AddWithValue("id", commentId);
        await updateCmd.ExecuteNonQueryAsync();

        return NoContent();
    }

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(int requestId, int commentId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var checkReq = new NpgsqlCommand("SELECT id FROM requests WHERE id = @id", conn);
        checkReq.Parameters.AddWithValue("id", requestId);
        if (await checkReq.ExecuteScalarAsync() == null)
            return NotFound("Заявка не найдена");

        await using var checkComment = new NpgsqlCommand("SELECT request_id FROM requestcomments WHERE id = @id", conn);
        checkComment.Parameters.AddWithValue("id", commentId);
        var actualRequestId = await checkComment.ExecuteScalarAsync();

        if (actualRequestId == null)
            return NotFound("Комментарий не найден");

        if ((int)actualRequestId != requestId)
            return BadRequest("Комментарий принадлежит другой заявке");

        await using var deleteCmd = new NpgsqlCommand("DELETE FROM requestcomments WHERE id = @id", conn);
        deleteCmd.Parameters.AddWithValue("id", commentId);
        await deleteCmd.ExecuteNonQueryAsync();

        return NoContent();
    }

    [HttpPost("attachments")]
    public async Task<IActionResult> UploadAttachment(int requestId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("Размер файла превышает 5 МБ");

        var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Разрешены только PDF, PNG и JPEG");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var checkReq = new NpgsqlCommand("SELECT id FROM requests WHERE id = @id", conn);
        checkReq.Parameters.AddWithValue("id", requestId);
        if (await checkReq.ExecuteScalarAsync() == null)
            return NotFound("Заявка не найдена");

        string uniqueFileName = $"{Guid.NewGuid()}{extension}";
        string fullPath = Path.Combine(_uploadFolder, uniqueFileName);

        try
        {
            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string sql = @"INSERT INTO requestattachments (request_id, filename, storedpath, contenttype, sizebytes) 
                           VALUES (@reqId, @fileName, @storedPath, @contentType, @sizeBytes) RETURNING id";
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("reqId", requestId);
            cmd.Parameters.AddWithValue("fileName", file.FileName);
            cmd.Parameters.AddWithValue("storedPath", uniqueFileName);
            cmd.Parameters.AddWithValue("contentType", file.ContentType);
            cmd.Parameters.AddWithValue("sizeBytes", (int)file.Length);

            int attachmentId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new { id = attachmentId, message = "Файл успешно загружен" });
        }
        catch
        {
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
            throw;
        }
    }
}