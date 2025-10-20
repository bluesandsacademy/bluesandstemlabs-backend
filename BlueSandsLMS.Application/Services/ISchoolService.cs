using BlueSandsLMS.Common.DTOs;

public interface ISchoolService
{
    Task<SchoolDto> CreateAsync(CreateSchoolDto dto);
    Task<IEnumerable<SchoolDto>> GetAllAsync();
    Task<SchoolDto> UpdateAsync(Guid id, UpdateSchoolDto dto);
    Task DeleteAsync(Guid id);   
}
