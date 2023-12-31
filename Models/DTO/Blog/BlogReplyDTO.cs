﻿using System.ComponentModel.DataAnnotations;

namespace almondcove.Models.DTO.BlogDTOs
{
    public class BlogReplyDTO
    {
        public string Slug { get; set; }
        public string CommentId { get; set; }

        [MaxLength(200)]
        [MinLength(2)]
        public string Reply { get; set; }
        public int ReplyId { get; set; }
        public int UserId { get; set; }
        public string ReplyText { get; set; }
    }
}
