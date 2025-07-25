﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Item Type")]
        [RegularExpression("^(Uniform|Equipment)$", ErrorMessage = "Item Type must be either 'Uniform' or 'Equipment'")]
        public string ItemType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Item ID")]
        public string ItemId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [ForeignKey("Department")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        [Required]
        public string Material { get; set; } = string.Empty;

        [Required]
        [ForeignKey("Supplier")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Threshold Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Threshold quantity must be a non-negative number")]
        public int ThresholdQuantity { get; set; }

        [Display(Name = "Image")]
        public string? ImagePath { get; set; }

        [NotMapped]
        [Display(Name = "Upload Image")]
        public IFormFile? ImageFile { get; set; }

        // [ForeignKey("AssignedTo")]
        // [Display(Name = "Assigned To")]
        // public int? AssignedToId { get; set; }
        // public virtual Employee? AssignedTo { get; set; }

        // [Display(Name = "Assignment Date")]
        // public DateTime? AssignmentDate { get; set; }

        // [Display(Name = "Assigned Date")]
        // public DateTime? AssignedDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Available"; // Available, Assigned, Returned, Maintenance
    }
}
