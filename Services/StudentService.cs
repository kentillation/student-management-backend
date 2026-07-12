using Microsoft.EntityFrameworkCore;
using StudentManagementAPI.Data;
using StudentManagementAPI.DTOs;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _context.Students.ToListAsync();
            return students.Select(MapToDto);
        }

        public async Task<StudentDto?> GetStudentByIdAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            return student == null ? null : MapToDto(student);
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto studentDto)
        {
            var student = new Student
            {
                FirstName = studentDto.FirstName,
                LastName = studentDto.LastName,
                Email = studentDto.Email,
                Phone = studentDto.Phone,
                Course = studentDto.Course,
                EnrollmentDate = studentDto.EnrollmentDate,
                GPA = studentDto.GPA
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return MapToDto(student);
        }

        public async Task<StudentDto?> UpdateStudentAsync(int id, UpdateStudentDto studentDto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return null;

            student.FirstName = studentDto.FirstName;
            student.LastName = studentDto.LastName;
            student.Email = studentDto.Email;
            student.Phone = studentDto.Phone;
            student.Course = studentDto.Course;
            student.EnrollmentDate = studentDto.EnrollmentDate;
            student.GPA = studentDto.GPA;

            await _context.SaveChangesAsync();
            return MapToDto(student);
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StudentExistsAsync(int id)
        {
            return await _context.Students.AnyAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<StudentDto>> SearchStudentsAsync(string? searchTerm, string? course, double? minGPA, double? maxGPA)
        {
            var query = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s => s.FirstName.ToLower().Contains(term) || s.LastName.ToLower().Contains(term) || s.Email.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(course))
            {
                var c = course.Trim().ToLower();
                query = query.Where(s => s.Course.ToLower() == c);
            }

            if (minGPA.HasValue)
            {
                query = query.Where(s => s.GPA >= minGPA.Value);
            }

            if (maxGPA.HasValue)
            {
                query = query.Where(s => s.GPA <= maxGPA.Value);
            }

            var students = await query.ToListAsync();
            return students.Select(MapToDto);
        }

        public async Task<IEnumerable<string>> GetDistinctCoursesAsync()
        {
            return await _context.Students
                .Select(s => s.Course)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToListAsync();
        }

        private static StudentDto MapToDto(Student student)
        {
            return new StudentDto
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                Phone = student.Phone,
                Course = student.Course,
                EnrollmentDate = student.EnrollmentDate,
                GPA = student.GPA
            };
        }
    }
}