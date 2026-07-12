using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementAPI.DTOs;
using StudentManagementAPI.Services;

namespace StudentManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            IStudentService studentService,
            ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents()
        {
            try
            {
                var students = await _studentService.GetAllStudentsAsync();
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStudents failed");
                return StatusCode(500, "An error occurred while retrieving students");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<StudentDto>> GetStudent(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    return NotFound();
                }
                return Ok(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStudent failed for id: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the student");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StudentDto>> CreateStudent([FromBody] CreateStudentDto studentDto)
        {
            try
            {
                var createdStudent = await _studentService.CreateStudentAsync(studentDto);
                return CreatedAtAction(nameof(GetStudent), new { id = createdStudent.Id }, createdStudent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateStudent failed");
                return StatusCode(500, "An error occurred while creating the student");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto studentDto)
        {
            try
            {
                if (id != studentDto.Id)
                {
                    return BadRequest("ID mismatch");
                }

                var updatedStudent = await _studentService.UpdateStudentAsync(id, studentDto);
                if (updatedStudent == null)
                {
                    return NotFound();
                }

                return Ok(updatedStudent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateStudent failed for id: {Id}", id);
                return StatusCode(500, "An error occurred while updating the student");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var result = await _studentService.DeleteStudentAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteStudent failed for id: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the student");
            }
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> SearchStudents(
            [FromQuery] string? searchTerm,
            [FromQuery] string? course,
            [FromQuery] double? minGPA,
            [FromQuery] double? maxGPA)
        {
            try
            {
                var students = await _studentService.SearchStudentsAsync(searchTerm, course, minGPA, maxGPA);
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchStudents failed");
                return StatusCode(500, "An error occurred while searching for students");
            }
        }

        [HttpGet("courses")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<string>>> GetCourses()
        {
            try
            {
                var courses = await _studentService.GetDistinctCoursesAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCourses failed");
                return StatusCode(500, "An error occurred while retrieving courses");
            }
        }
    }
}