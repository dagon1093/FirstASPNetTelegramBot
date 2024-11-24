namespace FirstAspNetTelegramBot.Models
{
    public class User
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<Note> Notes { get; set; }
        public User() { }
        public User(long chatId, string fistName, string lastName, List<Note> notes)
        {
            Id = chatId;
            FirstName = fistName;
            LastName = lastName;
            Notes = notes;
        }

        public User(long chatId, string fistName, string lastName)
        {
            Id = chatId;
            FirstName = fistName;
            LastName = lastName;
        }
    }
}
