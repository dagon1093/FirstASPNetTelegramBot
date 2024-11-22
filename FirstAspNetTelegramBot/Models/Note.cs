﻿namespace FirstAspNetTelegramBot.Models
{
    public class Note
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }

        public long ChatId { get; set; }
        public Note() { }
        public Note(int id, string title, string description, string author, long chatId)
        {
            Id = id;
            Title = title;
            Description = description;
            Author = author;
            ChatId = chatId;
        }

    }
}
