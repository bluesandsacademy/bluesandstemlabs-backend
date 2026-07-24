
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InquiriesController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;

        public InquiriesController(BlueSandsLMSDbContext db)
        {
            _db = db;
        }


        [HttpPost("school")]
        public async Task<IActionResult> SchoolInquiry([FromBody] SchoolInquiryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var inquiry = new SchoolInquiry
                {
                    Id = Guid.NewGuid(),
                    SchoolName = dto.SchoolName.Trim(),
                    Type = dto.Type.Trim(),
                    Email = dto.Email.Trim().ToLower(),
                    Phone = dto.Phone.Trim(),
                    ContactPerson = dto.ContactPerson.Trim(),
                    Designation = dto.Designation.Trim(),
                    StudentCount = dto.StudentCount,
                    TeacherCount = dto.TeacherCount,
                    DateCreated = DateTime.UtcNow,
                    IsContacted = false
                };

                _db.SchoolInquiries.Add(inquiry);
                await _db.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true,
                    message = "Thank you! We've received your inquiry and will contact you soon.",
                    inquiryId = inquiry.Id 
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An error occurred. Please try again later." });
            }
        }


        [HttpPost("individual")]
        public async Task<IActionResult> IndividualInquiry([FromBody] IndividualInquiryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var inquiry = new IndividualInquiry
                {
                    Id = Guid.NewGuid(),
                    FullName = dto.FullName.Trim(),
                    Gender = dto.Gender.Trim(),
                    Role = dto.Role.Trim(),
                    School = dto.School.Trim(),
                    Email = dto.Email.Trim().ToLower(),
                    Phone = dto.Phone.Trim(),
                    Location = dto.Location.Trim(),
                    Notes = dto.Notes?.Trim(),
                    DateCreated = DateTime.UtcNow,
                    IsContacted = false
                };

                _db.IndividualInquiries.Add(inquiry);
                await _db.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true,
                    message = "Thank you! We've received your inquiry and will contact you soon.",
                    inquiryId = inquiry.Id 
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An error occurred. Please try again later." });
            }
        }
    }
}
