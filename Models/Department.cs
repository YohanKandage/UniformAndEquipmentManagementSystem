using System.ComponentModel.DataAnnotations;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Department
    {
        public Department()
        {
            Employees = new HashSet<Employee>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Department Name")]
        public string Name { get; set; }

        public virtual ICollection<Employee> Employees { get; set; }
    }
}
