using StudentManagementAPI.DTOs;

namespace StudentManagementAPI.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<StudentDto?> GetStudentByIdAsync(int id);
        Task<StudentDto> CreateStudentAsync(CreateStudentDto studentDto);
        Task<StudentDto?> UpdateStudentAsync(int id, UpdateStudentDto studentDto);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> StudentExistsAsync(int id);
        Task<IEnumerable<StudentDto>> SearchStudentsAsync(string? searchTerm, string? course, double? minGPA, double? maxGPA);
        Task<IEnumerable<string>> GetDistinctCoursesAsync();
    }
}