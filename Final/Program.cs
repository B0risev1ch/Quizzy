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


long chatId = 0; //TODO: THIS IS BOGUS! Add to list of chats where bot is Admin / has sending messages permissions

BotMenu botMenu = new BotMenu();

//user:
UserData userData = new UserData();
//:
bool isWaitingForInput = false;
bool isNewQuestion = false;
bool isNewQuiz = false;
bool isNewQuestionNewTimeSpan = false;
Guid editingQuizGuid = Guid.NewGuid();
Guid armedToFire = Guid.NewGuid();
List<Quiz> quizzes = new();
Message noAdminMessage = null;
User user = new User();

var fileHelper = new FileHelper();

//List<UserData> userDataList = new();

Dictionary<User, Message> lastMessagesRecievedFromUsers = new Dictionary<User, Message>();

var bot = new TelegramBotClient("7367447150:AAFHBjaG_Hwak_CM2v6FhHY6EL36J--Mq44");
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

// Each time a user interacts with the bot, this method is called
async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
{
    switch (update.Type)
    {
        // A message was received
        case UpdateType.Message:

            await HandleMessage(update.Message!);

            break;

        case UpdateType.CallbackQuery:
            await HandleButton(update.CallbackQuery!, cts.Token);
            break;

        case UpdateType.MyChatMember:
            {
                if (update.MyChatMember != null)
                {
                    Console.WriteLine($"ChatMemberUpdate : " +
                                      $"\nChatType: {update.MyChatMember.Chat.Type},\n" +
                                      $"ChatId: {update.MyChatMember.Chat.Id},\n" +
                                      $"ChatMember: {(update.ChatMember == null ? "null" : "not null")}\n" +
                                      $"MyChatMember old {(update.MyChatMember.OldChatMember)} new {(update.MyChatMember.NewChatMember)}"); //old Telegram.Bot.Types.ChatMemberAdministrator new Telegram.Bot.Types.ChatMemberLeft

                    chatId = update.MyChatMember.Chat.Id;

                    if (update.MyChatMember.NewChatMember is ChatMemberAdministrator)
                        if (noAdminMessage != null)
                        {
                            await bot.EditMessageTextAsync(
                                noAdminMessage!.Chat.Id,
                                noAdminMessage.MessageId,
                                $"Я теперь админ в {update.MyChatMember.Chat.Type} '{update.MyChatMember.Chat.Title}'",
                                ParseMode.Html
                            );
                        }

                    if (update.MyChatMember.NewChatMember is ChatMemberLeft)
                    {
                        if (noAdminMessage != null)
                        {
                            await bot.EditMessageTextAsync(
                                noAdminMessage!.Chat.Id,
                                noAdminMessage.MessageId,
                                $"Я удалился из {update.MyChatMember.Chat.Type} '{update.MyChatMember.Chat.Title}'",
                                ParseMode.Html
                            );
                        }
                        //noAdminMessage ==;
                    }
                    else

                    {

                        //await bot.SendTextMessageAsync($"Я теперь админ в {update.MyChatMember.Chat.Type} '{update.MyChatMember.Chat.Title}'");
                    }
                    //chatId = 0;

                }


                break;
            }
        default:
            Console.WriteLine(update.Type);
            //cts.Cancel();
            break;
    }
}
async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
{
    await Console.Error.WriteLineAsync(exception.ToString());
}

async Task HandleMessage(Message msg)
{

    Message newQuizQuestionMessage;

    user = msg.From;
    var text = msg.Text ?? string.Empty;

    if (user is null)
        return;

    /*
	if (!userDataList.Where(a => a.).Contains(user))
	{
		userDataTuples.Add(user, (msg, msg.Chat));
		Console.WriteLine($"user = {user} was not presented. Added. Total users = {userDataTuples.Count()}");
	}

	*/
    // if(isWaitingForInputFrom(user))
    /*  lastMessagesRecievedFromUsers.Where(a => a.UserId).First()
	 *
	 */

    Console.WriteLine($"isWaitingForInput = {isWaitingForInput}; isNewQuiz = {isNewQuiz}; isNewQuestion = {isNewQuestion}; isNewQuestionNewTimeSpan = {isNewQuestionNewTimeSpan}");

    if (isWaitingForInput)
    {
        if (isNewQuiz)
        {
            if (msg.Text != null)
            {
                var newQuiz = CreateNewQuiz(msg);
                quizzes.Add(newQuiz);
                editingQuizGuid = newQuiz.Guid;
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Название должно быть текстом");
                return;
            }
            isNewQuiz = false;
        }

        TimeSpan ts = TimeSpan.Zero;

        if (isNewQuestionNewTimeSpan && msg.Type == MessageType.Text)
        {
            //updateTimeSpan

            Console.WriteLine(quizzes.First(a => a.Guid == editingQuizGuid).Name);
            var seconds = 0.0;

            if (!Double.TryParse(msg.Text, out seconds))

            {

                await bot.SendTextMessageAsync(msg.Chat.Id, $"Формат не распознан. Введите количество секунд на ответ");
                return;
                ////TODO: default value from config 
                seconds = 120.0;
            }

            ts = TimeSpan.FromSeconds(seconds);

            var curreentQuiz = quizzes.First(a => a.Guid == editingQuizGuid);

            curreentQuiz.Questions.Last().SetTimeSpan(ts);

            Console.WriteLine("\nTOTAL : " + curreentQuiz.GetTotalTimeSpan() + "\n");

            Console.WriteLine($"question data: {quizzes.First(a => a.Guid == editingQuizGuid).Questions.Last().GetQuestionData()}");

            //isNewQuestionNewTimeSpan = false;
            isWaitingForInput = false;
        }

        if (isNewQuestion)
        {
            CreateNewQuestion(msg);
            isNewQuestion = false;
            isNewQuestionNewTimeSpan = true;
            await bot.SendTextMessageAsync(msg.Chat, "Время на ответ:");
        }

        if (!isWaitingForInput && isNewQuestionNewTimeSpan)
        {
            isNewQuestionNewTimeSpan = false;
            Task.Run(async () =>
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Вопрос добавлен. {quizzes.First(quiz => quiz.Guid == editingQuizGuid).Questions.Last().GetQuestionData()}. Всего вопросов = {quizzes.First(quiz => quiz.Guid == editingQuizGuid).Questions.Count()}", replyMarkup: botMenu.NewQuizMenuMarkup);
            });
        }
    }

    Console.WriteLine($"{user.FirstName} [Id: {user.Id}] sent {msg.Type} {text}");

    if (text.StartsWith("/"))
    {
        (string? command, string? attribute) userInput;
        userInput = ParseCommandAndAttribute(text);
        var command = userInput.command;
        var message = userInput.attribute ?? "null";

        await HandleCommand(user.Id, command, message);
    }
}

(string? command, string? attribute) ParseCommandAndAttribute(string? inputLine)
{
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
    Quiz newQuiz = new Quiz(quizMessage.Text, string.Empty, new List<Question>());
    Task.Run(async () =>
    {
        await bot.SendTextMessageAsync(quizMessage.Chat.Id, "пусть будет '" + quizMessage.Text + $"'\nGuid = {newQuiz.Guid}",
            replyMarkup: botMenu.NewQuizMenuMarkup);
    });

    isWaitingForInput = false;

    return newQuiz;
}

Question CreateNewQuestion(Message quizMessage)
{
    string questionData = string.Empty;
    Question question;


    if (quizMessage.Type == MessageType.Photo)
    {
        var photo = quizMessage.Photo;
        questionData = photo[photo.Count() - 1].FileId;
    }
    if (quizMessage.Type == MessageType.Text)
        questionData = quizMessage.Text;

    question = new Question(quizMessage.Type, questionData, TimeSpan.Zero);

    quizzes.First(quiz => quiz.Guid == editingQuizGuid).Questions.Add(question);

    Console.WriteLine($"Вопрос {question.Guid} добавлен в квиз '{quizzes.First(quiz => quiz.Guid == editingQuizGuid).Name}', всего времени на ответы в квизе = {quizzes.First(quiz => quiz.Guid == editingQuizGuid).GetTotalTimeSpan()}");
    
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
async Task FireQuiz(Guid quizGuid)
{
    Quiz quiz = quizzes.First(a => a.Guid == quizGuid);
    if (chatId != 0)
        await SendQuestionsSequentially(chatId, quiz.Questions);
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
    Console.WriteLine(query.Data);

    string text = "";

    InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());

    var quizGuid = Guid.Empty;

    if (Guid.TryParse(query.Data, out quizGuid) && quizzes.Any())
    {
        try
        {
            Chat chat = await bot.GetChatAsync(chatId, cancellationToken);
            string chatTitle = chat.Title;

            text = $"Quiz data: {quizzes.First(a => a.Guid == quizGuid).GetQuizData()}";

            markup = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Назад", "QuizGeneratedMarkupBack"),
                InlineKeyboardButton.WithCallbackData($"🔥fire! Целимся в\n{chatTitle}", $"fireQuiz_{quizGuid}"),
            });
        }
        catch (ApiRequestException e)
        {
            Console.WriteLine("ChatId = 0?\n" + e);
            noAdminMessage = await bot.SendTextMessageAsync(query.Message.Chat,
                "Я пока нигде не админ, удоли и добавь в нужную группу для старта квизов");
        }
    }

    Console.WriteLine($"query data = {query.Data}; quizGuid = {quizGuid}; armedToFire = {armedToFire}");

    if (string.Equals(query.Data, quizGuid.ToString(), StringComparison.InvariantCulture))
    {
        armedToFire = quizGuid;
        Console.WriteLine($"Заряжен квиз с GUID = {armedToFire}!");
    }

    if (query.Data == $"fireQuiz_{armedToFire}")
    {
        Chat chat = await bot.GetChatAsync(chatId, cancellationToken);
        string chatTitle = chat.Title;
        Console.WriteLine($"FIRED");

        Task.Run(async () => await FireQuiz(armedToFire));

        text = $"Выстрелили квизом с GUID = {armedToFire} в чятик {chatTitle}!";

    }
    switch (query.Data)
    {
        case "DelayDataCallback":
            text = "Delay Message:";
            Console.WriteLine($"delay button pressed");
            await bot.SendTextMessageAsync(query.Message.Chat.Id, "Enter message to deliever:");
            //markup = botMenu.GenerateMarkupFromQuizzes(quizzes);
            isWaitingForInput = true;
            return;

        case "NewQuizDataCallback":
            text = "Создаём новый квиз.\nИмя квиза:";
            markup = new(Array.Empty<InlineKeyboardButton>());
            isNewQuiz = true;
            isWaitingForInput = true;
            break;

        case "NewQuizAddQuestionDataCallBack":
            {
                text = "Новый вопрос (text or photo):";
                markup = new(Array.Empty<InlineKeyboardButton>());
                isNewQuestion = true;
                isWaitingForInput = true;
                break;
            }

        case "HelpDataCallback":
            text = "Меню:";
            markup = botMenu.MenuMarkup;
            break;

        case "NewQuizStopEditingDataCallBack":
            if (!quizzes.Any())
                break;
            //quizzes.First(a => a.Guid == editingQuizGuid).ReCalculateQuestionTs();


            markup = botMenu.MenuMarkup;
            isNewQuestion = false;
            isWaitingForInput = false;
            Quiz quiz = quizzes.First(a => a.Guid == editingQuizGuid);

            FileHelper.SaveQuizToFile(quiz, Path.Combine(user.Username, $"{quiz.Guid}_{quiz.Name}_{DateTime.Now.TimeOfDay.ToString().Split('.')[0].Replace(':', '-')}.json"));

            text = "Quiz saved!";
            break;

        case "MyQuizzesDataCallback":
            text = "Мои квизы:";
            quizzes.Clear();

            var path = user.Username;

            if (Directory.Exists(path))
            {
	           var files = Directory.EnumerateFiles(path);

	            foreach (var file in files)
	            {
		            quizzes.Add(FileHelper.LoadQuizFromFile(file));
		            Console.WriteLine($"{file} added to list Of Quizzes");
                    
	            }
            }
            else
            {
	            text = "Нет квизов.\nМеню:";
                Console.WriteLine($"Directory {path} does not exist.");
	           
            }
	        //


            /*
			fileHelper = new FileHelper("quizzes.json");
            var loadedQuizzes = await fileHelper.LoadQuizzesAsync();
			
			foreach (var quiz in loadedQuizzes)
			{
				Console.WriteLine($"Loaded Quiz: {quiz.Name}, totalTimeSpan: {quiz.GetTotalTimeSpan()}");
			}
			*/
            //quizzes = loadedQuizzes;

            markup = botMenu.GenerateMarkupFromQuizzes(quizzes);
            //isNewQuestion = false;
            //isWaitingForInput = false;
            break;
        case "QuizGeneratedMarkupBack":
            text = "Меню:";
            markup = botMenu.MenuMarkup;
            //isNewQuestion = false;
            //isWaitingForInput = false;
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