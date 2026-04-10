using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TasksController> _logger;

    public TasksController(AppDbContext context, ILogger<TasksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("UserId não encontrado no token.");

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var userId = GetUserId();
        
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
            
        return Ok(tasks);
    }

    [Authorize] 
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetUserId();
        
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            UserId = userId
        };
        
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tarefa {TaskId} criada por {UserId}", task.Id, userId);
        
        return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
    }

    [Authorize(Policy = "CanDeleteTask")] 
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task is null)
            return NotFound(new { message = $"Tarefa {id} não encontrada." });

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tarefa {TaskId} deletada por {UserId}", id, GetUserId());
        return NoContent();
    }
}