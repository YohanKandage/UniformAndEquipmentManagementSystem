using System.ComponentModel.DataAnnotations;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Department
    {
        public Department()
        {
            Employees = new HashSet<Employee>();
            Items = new HashSet<Item>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Department Name")]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<Employee>? Employees { get; set; }
        public virtual ICollection<Item>? Items { get; set; }
    }
}
