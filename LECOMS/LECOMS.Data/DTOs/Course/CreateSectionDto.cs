﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class CreateSectionDto
    {
        public string CourseId { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
    }
}
