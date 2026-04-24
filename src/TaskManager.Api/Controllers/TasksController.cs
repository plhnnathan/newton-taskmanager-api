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

    /// <summary>
    /// Extrai o UserId do token JWT (claim "sub" / NameIdentifier).
    /// </summary>
    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("UserId não encontrado no token.");

    // ╔══════════════════════════════════════════════════════════════╗
    // ║  ETAPA 2 — Protejam os endpoints com [Authorize]             ║
    // ╚══════════════════════════════════════════════════════════════╝

    /// <summary>
    /// GET /api/tasks
    /// Retorna as tarefas do usuário autenticado.
    /// </summary>
    [Authorize] // TODO 2.1: Implementado
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        // TODO 2.2: Implementado
        var userId = GetUserId();
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
            
        return Ok(tasks);
    }

    /// <summary>
    /// POST /api/tasks
    /// Cria uma nova tarefa vinculada ao usuário autenticado.
    /// </summary>
    [Authorize] // TODO 2.3: Implementado
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        // TODO 2.4: Implementado
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

    // ╔══════════════════════════════════════════════════════════════╗
    // ║  ETAPA 3 — Restrinjam o DELETE com a Policy "CanDeleteTask"  ║
    // ╚══════════════════════════════════════════════════════════════╝

    /// <summary>
    /// DELETE /api/tasks/{id}
    /// Remove uma tarefa. Apenas Admin pode executar (Policy: CanDeleteTask).
    /// </summary>
    [Authorize(Policy = "CanDeleteTask")] // TODO 3.2: Implementado
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
