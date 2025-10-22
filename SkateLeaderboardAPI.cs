// Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add DbContext with SQL Server
builder.Services.AddDbContext<LeaderboardContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LeaderboardContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

// Models
public class LeaderboardEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string PlayerName { get; set; }
    
    [Required]
    public int Score { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Level { get; set; }
    
    public DateTime DateCompleted { get; set; } = DateTime.UtcNow;
}

public class LeaderboardEntryDto
{
    [Required]
    public string PlayerName { get; set; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int Score { get; set; }
    
    [Required]
    public string Level { get; set; }
}

// DbContext
public class LeaderboardContext : DbContext
{
    public LeaderboardContext(DbContextOptions<LeaderboardContext> options)
        : base(options)
    {
    }

    public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaderboardEntry>()
            .HasIndex(e => e.Score)
            .IsDescending();
            
        modelBuilder.Entity<LeaderboardEntry>()
            .HasIndex(e => e.DateCompleted);
    }
}

// Controllers
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly LeaderboardContext _context;

    public LeaderboardController(LeaderboardContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaderboardEntry>>> GetLeaderboard(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 100)
    {
        var entries = await Task.Run(() => 
            _context.LeaderboardEntries
                .OrderByDescending(e => e.Score)
                .ThenBy(e => e.DateCompleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList());
        
        return Ok(entries);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LeaderboardEntry>> GetEntry(int id)
    {
        var entry = await _context.LeaderboardEntries.FindAsync(id);

        if (entry == null)
        {
            return NotFound();
        }

        return Ok(entry);
    }

    [HttpPost]
    public async Task<ActionResult<LeaderboardEntry>> PostEntry(LeaderboardEntryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var entry = new LeaderboardEntry
        {
            PlayerName = dto.PlayerName,
            Score = dto.Score,
            Level = dto.Level,
            DateCompleted = DateTime.UtcNow
        };

        _context.LeaderboardEntries.Add(entry);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, entry);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        var entry = await _context.LeaderboardEntries.FindAsync(id);
        if (entry == null)
        {
            return NotFound();
        }

        _context.LeaderboardEntries.Remove(entry);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("top/{count}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntry>>> GetTopScores(int count = 10)
    {
        var entries = await Task.Run(() =>
            _context.LeaderboardEntries
                .OrderByDescending(e => e.Score)
                .ThenBy(e => e.DateCompleted)
                .Take(count)
                .ToList());

        return Ok(entries);
    }

    [HttpGet("player/{playerName}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntry>>> GetPlayerScores(string playerName)
    {
        var entries = await Task.Run(() =>
            _context.LeaderboardEntries
                .Where(e => e.PlayerName.ToLower() == playerName.ToLower())
                .OrderByDescending(e => e.Score)
                .ToList());

        return Ok(entries);
    }
}