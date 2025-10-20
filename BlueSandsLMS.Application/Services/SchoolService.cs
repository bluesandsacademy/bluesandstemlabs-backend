using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services
{
    public class SchoolService : ISchoolService
    {
        private readonly BlueSandsLMSDbContext _db;
        public SchoolService(BlueSandsLMSDbContext db) => _db = db;

        public async Task<SchoolDto> CreateAsync(CreateSchoolDto dto)
        {
            var school = new School
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Subdomain = dto.Subdomain,
                IsActive = true,
                DateCreated = DateTime.UtcNow
            };
            _db.Schools.Add(school);
            await _db.SaveChangesAsync();

            return new SchoolDto
            {
                Id = school.Id,
                Name = school.Name,
                Subdomain = school.Subdomain
            };
        }

        public async Task<SchoolDto> UpdateAsync(Guid id, UpdateSchoolDto dto)
{
    var school = await _db.Schools.FindAsync(id);
    if (school == null) throw new Exception("School not found.");

    school.Name = dto.Name;
    school.Subdomain = dto.Subdomain;
    // Optionally handle IsActive, Address, etc.

    await _db.SaveChangesAsync();

    return new SchoolDto
    {
        Id = school.Id,
        Name = school.Name,
        Subdomain = school.Subdomain
    };
}
public async Task DeleteAsync(Guid id)
{
    var school = await _db.Schools.FindAsync(id);
    if (school == null) throw new Exception("School not found.");

    _db.Schools.Remove(school);
    await _db.SaveChangesAsync();
}


        public async Task<IEnumerable<SchoolDto>> GetAllAsync()
        {
            return await _db.Schools
                .Select(s => new SchoolDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Subdomain = s.Subdomain
                })
                .ToListAsync();
        }
    }
}
