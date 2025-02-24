using System.Text;
using FirstAspNetTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace Webhook.Controllers.Services;

public class UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger) : IUpdateHandler
{
    private static readonly InputPollOption[] PollOptions = ["Hello", "World!"];

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogInformation("HandleError: {Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message }                        => OnMessage(message),
            { EditedMessage: { } message }                  => OnMessage(message),
            { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll }                              => OnPoll(poll),
            { PollAnswer: { } pollAnswer }                  => OnPollAnswer(pollAnswer),
            // ChannelPost:
            // EditedChannelPost:
            // ShippingQuery:
            // PreCheckoutQuery:
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        if (msg.Text is not { } messageText)
            return;

        Message sentMessage = await (messageText.Split(' ')[0] switch
        {
            "/last_five_notes" => SendLastFiveNotes(msg),
            "/delete" => Delete(msg),
            "/photo" => SendPhoto(msg),
            "/inline_buttons" => SendInlineKeyboard(msg),
            "/keyboard" => SendReplyKeyboard(msg),
            "/remove" => RemoveKeyboard(msg),
            "/request" => RequestContactAndLocation(msg),
            "/inline_mode" => StartInlineQuery(msg),
            "/poll" => SendPoll(msg),
            "/poll_anonymous" => SendAnonymousPoll(msg),
            "/throw" => FailingHandler(msg),
            _ => Usage(msg)
        });

        
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
    }

    async Task<Message> Usage(Message msg)
    {
        const string usage = """
                <b><u>Bot menu</u></b>:
                /photo          - send a photo
                /inline_buttons - send inline buttons
                /keyboard       - send keyboard buttons
                /remove         - remove keyboard buttons
                /request        - request location or contact
                /inline_mode    - send inline-mode results list
                /poll           - send a poll
                /poll_anonymous - send an anonymous poll
                /throw          - what happens if handler fails

                your message saved
            """;

        var inlineMarkup = new InlineKeyboardMarkup()
            .AddNewRow()
                .AddButton("���������� ���� ������");

        using (ApplicationContext db = new ApplicationContext())
        {
            if (db.Users.FirstOrDefault(u => u.Id == msg.Chat.Id) == null)
                db.Users.Add(new FirstAspNetTelegramBot.Models.User { Id = msg.Chat.Id, FirstName = msg.Chat.FirstName, LastName = msg.Chat.LastName });
            // ������� ��� ������� User
            Note note = new Note(msg.Id, "", msg.Text, /*$"{msg.Chat.FirstName} {msg.Chat.LastName}",*/ msg.Chat.Id , DateTime.Now.ToUniversalTime());

            // ��������� �� � ��
            db.Notes.Add(note);
            db.SaveChanges();
        }

        return await bot.SendMessage(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: inlineMarkup);
    }

    async Task<Message> SendPhoto(Message msg)
    {
        await bot.SendChatAction(msg.Chat, ChatAction.UploadPhoto);
        await Task.Delay(2000); // simulate a long task
        await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
        return await bot.SendPhoto(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
    }

    // Send inline keyboard. You can process responses in OnCallbackQuery handler
    async Task<Message> SendInlineKeyboard(Message msg)
    {
        var inlineMarkup = new InlineKeyboardMarkup()
            .AddButton("�������� ��������� 5 �������", lastFiveUserNotes(msg.Chat.Id));
        //var inlineMarkup = new InlineKeyboardMarkup()
        //    .AddNewRow("1.1", "1.2", "1.3")
        //    .AddNewRow()
        //        .AddButton("WithCallbackData", "CallbackData")
        //        .AddButton(InlineKeyboardButton.WithUrl("WithUrl", "https://github.com/TelegramBots/Telegram.Bot"));
        return await bot.SendMessage(msg.Chat, "Inline buttons:", replyMarkup: inlineMarkup);
    }

    async Task<Message> SendReplyKeyboard(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddNewRow().AddButton("/last_five_notes")
            .AddNewRow().AddButton("/delete");
        return await bot.SendMessage(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
    }

    async Task<Message> RemoveKeyboard(Message msg)
    {
        return await bot.SendMessage(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
    }

    async Task<Message> RequestContactAndLocation(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddButton(KeyboardButton.WithRequestLocation("Location"))
            .AddButton(KeyboardButton.WithRequestContact("Contact"));
        return await bot.SendMessage(msg.Chat, "Who or Where are you?", replyMarkup: replyMarkup);
    }

    async Task<Message> StartInlineQuery(Message msg)
    {
        var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
        return await bot.SendMessage(msg.Chat, "Press the button to start Inline Query\n\n" +
            "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
    }

    async Task<Message> SendPoll(Message msg)
    {
        return await bot.SendPoll(msg.Chat, "Question", PollOptions, isAnonymous: false);
    }

    async Task<Message> SendAnonymousPoll(Message msg)
    {
        return await bot.SendPoll(chatId: msg.Chat, "Question", PollOptions);
    }

    static Task<Message> FailingHandler(Message msg)
    {
        throw new NotImplementedException("FailingHandler");
    }

    // Process Inline Keyboard callback data
    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        if (callbackQuery.Data.Contains("���������� ���� ������"))
        {
            await bot.AnswerCallbackQuery(callbackQuery.Id, $"{getNotesByFirstNameAndLastName(callbackQuery.Message.Chat.Id)}");
            await bot.SendMessage(callbackQuery.Message!.Chat, $"{getNotesByFirstNameAndLastName(callbackQuery.Message.Chat.Id)}");
        }
        if (callbackQuery.Data.Contains("�������� ��������� 5 �������"))
        {
            await bot.AnswerCallbackQuery(callbackQuery.Id, $"{lastFiveUserNotes(callbackQuery.Message.Chat.Id)}");
            await bot.SendMessage(callbackQuery.Message!.Chat, $"{lastFiveUserNotes(callbackQuery.Message.Chat.Id)}");
        }

            //await bot.AnswerCallbackQuery(callbackQuery.Id, $"Received {callbackQuery.Data}");
        await bot.SendMessage(callbackQuery.Message!.Chat, $"{callbackQuery.Data}");
    }

    #region Inline Mode

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = [ // displayed result
            new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
            new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
        ];
        await bot.AnswerInlineQuery(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        await bot.SendMessage(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
    }

    #endregion

    private Task OnPoll(Poll poll)
    {
        logger.LogInformation("Received Poll info: {Question}", poll.Question);
        return Task.CompletedTask;
    }

    private async Task OnPollAnswer(PollAnswer pollAnswer)
    {
        var answer = pollAnswer.OptionIds.FirstOrDefault();
        var selectedOption = PollOptions[answer];
        if (pollAnswer.User != null)
            await bot.SendMessage(pollAnswer.User.Id, $"You've chosen: {selectedOption.Text} in poll");
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private string getNotesByFirstNameAndLastName(long id)
    {
        StringBuilder stringBuilder = new StringBuilder();
        using (ApplicationContext db = new ApplicationContext())
        {
            var notes = db.Notes.Where(p => p.User.Id == id);
            foreach (Note note in notes) { 
                stringBuilder.Append(note.Description + "\n");
                Console.WriteLine($"{note.Description}");
            }
        }
        return stringBuilder.ToString();
    }

    private string lastFiveUserNotes(long id) {

        StringBuilder stringBuilder = new StringBuilder();
        using (ApplicationContext db = new ApplicationContext())
        {
            var notes = db.Notes.OrderByDescending(p => p.Id).Take(5);
            foreach (Note note in notes)
            {
                stringBuilder.Append(note.Description + "\n");
                Console.WriteLine($"{note.Description}");
            }
        }

        return stringBuilder.ToString(); 
    }

    private async Task<Message> SendLastFiveNotes(Message msg)
    {
        return await bot.SendMessage(msg.Chat, lastFiveUserNotes(msg.Chat.Id), parseMode: ParseMode.Html);
    }

    private async Task<Message> Delete(Message msg)
    {
        DeleteNotesByChatId(msg.Chat.Id);
        return await bot.SendMessage(msg.Chat, "������� �������", parseMode: ParseMode.Html);
    }

    private void DeleteNotesByChatId(long chatId)
    {
        StringBuilder stringBuilder = new StringBuilder();
        using (ApplicationContext db = new ApplicationContext())
        {
            var toRemoveNotes = db.Notes.Where(p => p.UserId == chatId).ToList();
            foreach(Note note in toRemoveNotes)
            {
                db.Notes.Remove(note);
            }
            db.SaveChanges();
        }
    }
}
