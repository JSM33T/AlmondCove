﻿using System.ComponentModel.DataAnnotations;

namespace almondcove.Models.Domain
{
    public class Avatar
    {
       
        public int Id { get; set; }

        public string Title { get; set; }

        public string Image { get; set; }

        public string Description { get; set; }
       
    }
}
