using System.Diagnostics;
using Final;
using System.Drawing;
using TdLib;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
//using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


BotMenu botMenu = new BotMenu();

Users users = new Users();

//TODO ну это что такое? Ну сколько можно? (с) Т.
//var bot = new TelegramBotClient("7367447150:AAFHBjaG_Hwak_CM2v6FhHY6EL36J--Mq44"); //IsItTimeNow

var bot = new TelegramBotClient("7518668541:AAHarcea1zKavuWM3Ub4J-PRYr4-o_ueoFM"); //b00terbr0d_bot

using var cts = new CancellationTokenSource();

//Message lastMessageRecieved = null;

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
bot.StartReceiving(
    updateHandler: HandleUpdate,
    cancellationToken: cts.Token,
    pollingErrorHandler: HandleError
);

// Tell the user the bot is online
Console.WriteLine("Start listening for updates. Press enter to stop");

Console.ReadLine();

// Send cancellation request to stop the bot
cts.Cancel();

async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
{

    User? currentUser = users.GetUserByUpdate(update);

    if (currentUser != null)
    {
        if (!users.UserPresented(currentUser))
        {
            users.AddUserData(new UserData(currentUser));
        }
    }
    else
    {
        Console.WriteLine($"Не смог распознать пользователя {currentUser} по UpdateType = {update.Type}");
        return;
    }

    switch (update.Type)
    {
        case UpdateType.Message:

            Console.WriteLine($"Handling Update for {currentUser}. UpdateType is {update.Type}");
            await HandleMessage(update.Message!);
            break;

        case UpdateType.CallbackQuery:

            Console.WriteLine($"Handling Update for {currentUser}. UpdateType is {update.Type}");
            await HandleButton(update.CallbackQuery!, cts.Token);
            break;

        case UpdateType.MyChatMember:
            {
                Console.WriteLine($"Handling Update for {currentUser}. UpdateType is {update.Type}");
                UserData currentUserData = users.GetUserDataByUser(currentUser);

                if (update.MyChatMember != null)
                {
                    Console.WriteLine($"ChatMemberUpdate : " +
                                      $"\nChatType: {update.MyChatMember.Chat.Type},\n" +
                                      $"ChatId: {update.MyChatMember.Chat.Id},\n" +
                                      $"ChatMember: {(update.ChatMember == null ? "null" : "not null")}\n" +
                                      $"MyChatMember old {(update.MyChatMember.OldChatMember)} new {(update.MyChatMember.NewChatMember)}"); //old Telegram.Bot.Types.ChatMemberAdministrator new Telegram.Bot.Types.ChatMemberLeft

                    currentUserData.ChatId = update.MyChatMember.Chat.Id;

                    if (update.MyChatMember.NewChatMember is ChatMemberAdministrator)
                        if (currentUserData.NoAdminMessage != null)
                        {
                            await bot.EditMessageTextAsync(
                                currentUserData.NoAdminMessage!.Chat.Id,
                                currentUserData.NoAdminMessage.MessageId,
                                $"Я теперь админ в {update.MyChatMember.Chat.Type} '{update.MyChatMember.Chat.Title}'",
                                ParseMode.Html
                            );
                        }

                    if (update.MyChatMember.NewChatMember is ChatMemberLeft)
                    {
                        if (currentUserData.NoAdminMessage != null)
                        {
                            await bot.EditMessageTextAsync(
                                currentUserData.NoAdminMessage!.Chat.Id,
                                currentUserData.NoAdminMessage.MessageId,
                                $"Я удалился из {update.MyChatMember.Chat.Type} '{update.MyChatMember.Chat.Title}'",
                                ParseMode.Html
                            );
                        }
                    }
                }
                break;
            }
        default:
            Console.WriteLine(update.Type);
            break;
    }
}
async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
{
    await Console.Error.WriteLineAsync(exception.ToString());
}

async Task HandleMessage(Message msg)
{
    User currentUser = msg.From;
    var text = msg.Text ?? string.Empty;
    if (currentUser is null) return;
    Console.WriteLine($"Handling Message from {currentUser}...");

    if (!users.UserPresented(currentUser))
    {
        users.AddUserData(new UserData(currentUser));
        Console.WriteLine($"UserData for {currentUser} not presented. Added. Users: {(string.Join(", ", users.GetUsersList()))}");
    }
    UserData currentUserData = users.GetUserDataByUser(currentUser);
    if (currentUserData is null) return;

    Console.WriteLine($"isWaitingForInput = {currentUserData.IsWaitingForInput}; isNewQuiz = {currentUserData.IsNewQuiz};" +
                      $" isNewQuestion = {currentUserData.IsNewQuestion}; isNewQuestionNewTimeSpan = {currentUserData.IsNewQuestionNewTimeSpan}");

    
    if (currentUserData.IsWaitingForInput)
    {
        if (currentUserData.IsNewQuiz)
        {
            if (msg.Text != null)
            {
                var newQuiz = CreateNewQuiz(msg);
                currentUserData.Quizzes.Add(newQuiz);
                currentUserData.EditingQuizGuid = newQuiz.Guid;
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Название должно быть текстом");
                return;
            }
            currentUserData.IsNewQuiz = false;
        }

        TimeSpan ts = TimeSpan.Zero;

        if (currentUserData.IsNewQuestionNewTimeSpan && msg.Type == MessageType.Text)
        {
            Console.WriteLine(currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid).Name);
            var seconds = 0.0;

            if (!Double.TryParse(msg.Text, out seconds))
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Формат не распознан. Введите количество секунд на ответ");
                return;
                ////TODO: default value from config 
                seconds = 120.0;
            }

            ts = TimeSpan.FromSeconds(seconds);

            var curreentQuiz = currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid);

            curreentQuiz.Questions.Last().SetTimeSpan(ts);

            Console.WriteLine("\nTOTAL : " + curreentQuiz.GetTotalTimeSpan() + "\n");

            Console.WriteLine($"question data: {currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid).Questions.Last().GetQuestionData()}");

            //isNewQuestionNewTimeSpan = false;
            currentUserData.IsWaitingForInput = false;
        }

        if (currentUserData.IsNewQuestion)
        {
            CreateNewQuestion(msg);
            currentUserData.IsNewQuestion = false;
            currentUserData.IsNewQuestionNewTimeSpan = true;
            await bot.SendTextMessageAsync(msg.Chat, "Время на ответ:");
        }

        if (!currentUserData.IsWaitingForInput && currentUserData.IsNewQuestionNewTimeSpan)
        {
            currentUserData.IsNewQuestionNewTimeSpan = false;
            Task.Run(async () =>
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Вопрос добавлен. " +
                                                            $"{currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Last().GetQuestionData()}." +
                                                            $" Всего вопросов = {currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Count()}",
                    replyMarkup: botMenu.NewQuizMenuMarkup);
            });
        }
    }

    Console.WriteLine($"{currentUser.FirstName} [Id: {currentUser.Id}] sent {msg.Type} {text}");

    if (text.StartsWith("/"))
    {
        await HandleCommand(currentUser.Id,
            ParseCommandAndAttribute(text).command,
            ParseCommandAndAttribute(text).attribute ?? "null");
    }
}

(string? command, string? attribute) ParseCommandAndAttribute(string? inputLine)
{
	Console.WriteLine($"Parsing command");
    if (inputLine != null)
    {
        var commandLastIndex = inputLine.IndexOf(' ') > 0 ? inputLine.IndexOf(' ') : inputLine.Length;

        var command = inputLine[..commandLastIndex];

        var attribute = commandLastIndex + 1 < inputLine.Length ?
            inputLine.Substring(commandLastIndex + 1, inputLine.Length - commandLastIndex - 1)
            : null;
        return (command, attribute);
    }
    else
        return (null, null);
}

async Task SendQuestion(long chatId, Question question) //отправляем в чятик
{
    var messageType = question.Type;

    Task.Run(async () =>
    {
        if (messageType is MessageType.Text)
        {
            await bot.SendTextMessageAsync(chatId, question.GetQuestionData());
            //Todo вынести в config отправлять ли в чят время на ответ
            //await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
        }
        if (messageType is MessageType.Photo)
        {
	        await bot.SendPhotoAsync(chatId, new InputFileId(question.GetQuestionData()));
	        //await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
        }
        if (messageType is MessageType.Video)
        {
	        await bot.SendVideoAsync(chatId, new InputFileId(question.GetQuestionData()));
	        //await bot.SendTextMessageAsync(chatId, "Время на ответ: " + question.GetTimeSpan().ToString());
        }
    });
}
async Task HandleCommand(long userId, string command, string? message)
{
    switch (command)
    {
        case "/start":
            await SendMenu(userId);
            break;
    }

    await Task.CompletedTask;
}

Quiz CreateNewQuiz(Message quizMessage)
{

    User currentUser = quizMessage.From;
    UserData currentUserData = users.GetUserDataByUser(currentUser);

    Quiz newQuiz = new Quiz(quizMessage.Text, string.Empty, new List<Question>());
    Task.Run(async () =>
    {
        await bot.SendTextMessageAsync(quizMessage.Chat.Id, "пусть будет '" + quizMessage.Text + $"'\nGuid = {newQuiz.Guid}",
            replyMarkup: botMenu.NewQuizMenuMarkup);
    });

    currentUserData.IsWaitingForInput = false;

    return newQuiz;
}

Question CreateNewQuestion(Message quizMessage)
{

    User currentUser = quizMessage.From;
    UserData currentUserData = users.GetUserDataByUser(currentUser);

    string questionData = string.Empty;
    Question question;


    if (quizMessage.Type == MessageType.Photo)
    {
        var photo = quizMessage.Photo;
        questionData = photo[photo.Count() - 1].FileId;
    }
    if (quizMessage.Type == MessageType.Video)
    {
	    var video = quizMessage.Video;
	    questionData = video.FileId;
    }
    if (quizMessage.Type == MessageType.Text)
        questionData = quizMessage.Text;

    question = new Question(quizMessage.Type, questionData, TimeSpan.Zero);

    currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Questions.Add(question);

    Console.WriteLine($"Вопрос {question.Guid} добавлен в квиз '{currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Name}'," +
                      $" всего времени на ответы в квизе = {currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).GetTotalTimeSpan()}");
    return question;
}


async Task SendMenu(long userId)
{
    var text = "Меню:";
    var markup = botMenu.MenuMarkup;

    await bot.SendTextMessageAsync(
        userId,
        text,
        replyMarkup: markup
    );
}
async Task FireQuiz(Guid? quizGuid, UserData userData)
{

    if (quizGuid is null)
    {
        Console.WriteLine($"Nothing armed to fire for user = {userData.User}");
        return;
    }
    Quiz quiz = userData.Quizzes.First(a => a.Guid == quizGuid);
    if (userData.ChatId != 0)
        await SendQuestionsSequentially(userData.ChatId, quiz.Questions);
    else
    {
        Console.WriteLine("No Chats found with Administrative role/permissions to send messages");
    }

}
async Task SendQuestionsSequentially(long chatId, IEnumerable<Question> questions)
{
    foreach (var question in questions)
    {
        await SendQuestion(chatId, question);
        await Task.Delay(question.GetTimeSpan());
    }
}

async Task HandleButton(CallbackQuery query, CancellationToken cancellationToken)
{

    User currentUser = query.From;
    UserData currentUserData = users.GetUserDataByUser(currentUser);
    if (currentUserData is null) { return;}

    Console.WriteLine($"users Count = {users.GetUsersList().Count()};\n" +
                      $"currentUser = {currentUser};\n" +
                      $"currentUserData.User = {users.GetUserDataByUser(currentUser)}");

    Console.WriteLine(query.Data);

    string text = "";

    InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());

    var quizGuid = Guid.Empty;

    if (Guid.TryParse(query.Data, out quizGuid) && currentUserData.Quizzes.Any())
    {
        try
        {
            Chat chat = await bot.GetChatAsync(currentUserData.ChatId, cancellationToken);
            string chatTitle = chat.Title;

            text = $"Quiz data: {currentUserData.Quizzes.First(a => a.Guid == quizGuid).GetQuizData()}";

            markup = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Назад", "QuizGeneratedMarkupBack"),
                InlineKeyboardButton.WithCallbackData($"🔥fire! Целимся в\n{chatTitle}", $"fireQuiz_{quizGuid}"),
            });
        }
        catch (ApiRequestException e)
        {
            Console.WriteLine("ChatId = 0?\n" + e);
            currentUserData.NoAdminMessage = await bot.SendTextMessageAsync(query.Message.Chat,
                "Я пока нигде не админ, удоли и добавь в нужную группу для старта квизов");
        }
    }

    Console.WriteLine($"query data = {query.Data}; " +
                      $"quizGuid = {quizGuid};"
                      + $" armedToFire = {(currentUserData.ArmedToFire == null ? "none" : currentUserData.ArmedToFire.ToString())}");

    if (string.Equals(query.Data, quizGuid.ToString(), StringComparison.InvariantCulture))
    {
        currentUserData.ArmedToFire = quizGuid;
        Console.WriteLine($"Заряжен квиз с GUID = {currentUserData.ArmedToFire}!");
    }

    if (query.Data == $"fireQuiz_{currentUserData.ArmedToFire}")
    {
        Chat chat = await bot.GetChatAsync(currentUserData.ChatId, cancellationToken);
        string chatTitle = chat.Title;
        Console.WriteLine($"FIRED");

        if (currentUserData.ArmedToFire != null)
        {
            Task.Run(async () => await FireQuiz(currentUserData.ArmedToFire, currentUserData));
            text = $"Выстрелили квизом с GUID = {currentUserData.ArmedToFire} в чятик {chatTitle}!";
        }

    }
    switch (query.Data)
    {
        case "DelayDataCallback":
            text = "Delay Message:";
            Console.WriteLine($"delay button pressed");
            await bot.SendTextMessageAsync(query.Message.Chat.Id, "Enter message to deliever:");
            //markup = botMenu.GenerateMarkupFromQuizzes(quizzes);
            currentUserData.IsWaitingForInput = true;
            return;

        case "NewQuizDataCallback":
            text = "Создаём новый квиз.\nИмя квиза:";
            markup = new(Array.Empty<InlineKeyboardButton>());
            currentUserData.IsNewQuiz = true;
            currentUserData.IsWaitingForInput = true;
            break;

        case "NewQuizAddQuestionDataCallBack":
            {
                text = "Новый вопрос (text or photo or video):";
                markup = new(Array.Empty<InlineKeyboardButton>());
                currentUserData.IsNewQuestion = true;
                currentUserData.IsWaitingForInput = true;
                break;
            }

        case "HelpDataCallback":
            text = "Меню:";
            markup = botMenu.MenuMarkup;
            break;

        case "NewQuizStopEditingDataCallBack":
            if (!currentUserData.Quizzes.Any())
                break;
            //quizzes.First(a => a.Guid == editingQuizGuid).ReCalculateQuestionTs();


            markup = botMenu.MenuMarkup;
            currentUserData.IsNewQuestion = false;
            currentUserData.IsWaitingForInput = false;
            Quiz quiz = currentUserData.Quizzes.First(a => a.Guid == currentUserData.EditingQuizGuid);

            FileHelper.SaveQuizToFile(quiz, Path.Combine(currentUser.Username, $"{quiz.Guid}_{quiz.Name}_{DateTime.Now.TimeOfDay.ToString().Split('.')[0].Replace(':', '-')}.json"));

            text = "Quiz saved!";
            break;

        case "MyQuizzesDataCallback":
            text = "Мои квизы:";
            currentUserData.Quizzes.Clear();

            var path = currentUser.Username;

            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path);

                foreach (var file in files)
                {
                    currentUserData.Quizzes.Add(FileHelper.LoadQuizFromFile(file));
                    Console.WriteLine($"{file} added to list Of Quizzes");

                }
            }
            else
            {
                text = "Нет квизов.\nМеню:";
                Console.WriteLine($"Directory {path} does not exist.");
            }


            markup = botMenu.GenerateMarkupFromQuizzes(currentUserData.Quizzes);

            break;
        case "QuizGeneratedMarkupBack":
            text = "Меню:";
            markup = botMenu.MenuMarkup;
            break;
    }


    // Close the query to end the client-side loading animation
    try
    {
        await bot.AnswerCallbackQueryAsync(query.Id);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        // throw;
    }

    try
    {
        // Replace menu text and keyboard
        await bot.EditMessageTextAsync(
            query.Message!.Chat.Id,
            query.Message.MessageId,
            text,
            ParseMode.Html,
            replyMarkup: markup
        );
    }
    catch (ApiRequestException apiRequestException)
    {
        //await bot.SendTextMessageAsync(query.Message.Chat.Id, "Чё-то пошло не так..." + apiRequestException.Message);
        Console.WriteLine(apiRequestException.Message);
    }
}