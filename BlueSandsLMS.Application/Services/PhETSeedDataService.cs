using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Application.Services
{
    public class PhETSeedDataService : IPhETSeedDataService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly ILogger<PhETSeedDataService> _logger;
        private readonly Random _random = new();
        
        public PhETSeedDataService(BlueSandsLMSDbContext db, ILogger<PhETSeedDataService> logger)
        {
            _db = db;
            _logger = logger;
        }
        
        public async Task<SeedResult> GenerateSeedDataAsync(int studentCount, int experimentsPerStudent, CancellationToken ct = default)
        {
            try
            {
                var students = await _db.Users
                    .Where(u => u.Role!.Name == "Student" && u.IsActive)
                    .Take(studentCount)
                    .Select(u => new { u.Id, u.SchoolId })
                    .ToListAsync(ct);
                
                if (!students.Any())
                {
                    return new SeedResult(0, "No students found in database");
                }
                
                var simulations = await _db.PhETSimulations
                    .Where(s => s.IsActive)
                    .Select(s => new { s.Id, s.Title, s.Topic })
                    .ToListAsync(ct);
                
                if (!simulations.Any())
                {
                    return new SeedResult(0, "No PhET simulations found. Import simulations first.");
                }
                
                int created = 0;
                var now = DateTime.UtcNow;
                
                foreach (var student in students)
                {
                    for (int i = 0; i < experimentsPerStudent; i++)
                    {
                        var sim = simulations[_random.Next(simulations.Count)];
                        var startedAt = now.AddDays(-_random.Next(1, 90));
                        var completed = _random.NextDouble() > 0.3;
                        
                        var launch = new ExperimentLaunch
                        {
                            Id = Guid.NewGuid(),
                            UserId = student.Id,
                            PhETSimulationId = sim.Id,
                            Subject = sim.Topic,
                            ExperimentName = sim.Title,
                            Mode = _random.Next(2) == 0 ? "guided" : "free",
                            LastStep = completed ? 10 : _random.Next(1, 10),
                            StartedAt = startedAt,
                            EndedAt = completed ? startedAt.AddMinutes(_random.Next(10, 60)) : null,
                            Completed = completed,
                            DurationSec = completed ? _random.Next(600, 3600) : 0,
                            DateCreated = startedAt
                        };
                        
                        _db.ExperimentLaunches.Add(launch);
                        created++;
                        
                        if (created % 100 == 0)
                        {
                            await _db.SaveChangesAsync(ct);
                            _logger.LogInformation("Generated {Count} launches...", created);
                        }
                    }
                }
                
                await _db.SaveChangesAsync(ct);
                
                _logger.LogInformation("Seed complete: {Count} experiments for {Students} students", 
                    created, students.Count);
                
                return new SeedResult(created, $"Successfully generated {created} experiments for {students.Count} students");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating seed data");
                return new SeedResult(0, $"Error: {ex.Message}");
            }
        }
    }
}