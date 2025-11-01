using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // 👈 thêm namespace này

namespace LECOMS.Data.Entities
{
    [Index(nameof(Slug), IsUnique = true)]
    public class CourseCategory
    {
        [Key]
        public string Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(180)]
        public string Slug { get; set; } = null!;

        public byte Active { get; set; } = 1;

        [JsonIgnore] // 👈 thêm dòng này để chặn vòng lặp JSON
        public ICollection<Course> Courses { get; set; } = new List<Course>();

        public string? Description { get; set; }
    }
}
