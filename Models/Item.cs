using System.ComponentModel.DataAnnotations;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Size { get; set; }
        public string Image { get; set; }
    }
}
