﻿namespace almondcove.Models.DTO.BlogDTOs
{
    public class BlogLikeDTO
    {

        public int UserId { get; set; }
        public int BlogId { get; set; }
        public bool IsLiked { get; set; }
        public string Slug { get; set; }
    }
}
