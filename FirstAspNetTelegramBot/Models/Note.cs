﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstAspNetTelegramBot.Models
{
    public class Note
    {
        private int id1;
        private string v;
        private string? text;
        private long id2;

        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        public DateTime CreatedAt { get; set; } 

        public Note() { }
        public Note(int id, string title, string description, long userId, DateTime createdAt)
        {
            Id = id;
            Title = title;
            Description = description;
            UserId = userId;
            CreatedAt = createdAt;
        }

        public Note(int id, string title, string description, long userId)
        {
            Id = id;
            Title = title;
            Description = description;
            UserId = userId;
        }
    }
}
