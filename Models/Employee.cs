using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "Username cannot contain numbers. Only letters are allowed.")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must be exactly 10 digits and contain only numbers.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        [StringLength(12, MinimumLength = 10, ErrorMessage = "NIC must be between 10 and 12 characters.")]
        [RegularExpression(@"^[A-Za-z0-9]{10,12}$", ErrorMessage = "NIC must be 10 or 12 characters and contain only letters and numbers.")]
        [Display(Name = "NIC")]
        public string NIC { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Position { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        [ValidateNever]
        public Department Department { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public string? ImagePath { get; set; }

        [NotMapped]
        public string? Password { get; set; }
    }
}
