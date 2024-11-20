using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Final.Interfaces;
using Final.Repository;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Final.Repository.Quiz;

namespace Final.Services
{
    internal class QuizService : IQuizService
    {
	    private readonly ITelegramBotClient _bot;
	    private readonly IUserService _userService;
	    
	    private readonly BotMenuService _botMenuService;
	    private readonly ILogger<BotService> _logger;
	    private CancellationTokenSource? _cts;
		public QuizService(ITelegramBotClient botClient, IUserService userService, ILogger<BotService> logger)
	    {
		    _bot = botClient;
		    _userService = userService;
		    _botMenuService = new BotMenuService(_bot);
		    _logger = logger;
	    }
		public Quiz CreateNewQuiz(Message quizMessage)
	    {

			{

				User currentUser = quizMessage.From;

				UserData currentUserData = _userService.GetUserDataByUser(currentUser);

				Quiz newQuiz = new Quiz(quizMessage.Text, string.Empty, new List<Quiz.Question>());

				Task.Run(async () =>
				{
					await _bot.SendTextMessageAsync(quizMessage.Chat.Id,
						"пусть будет '" + quizMessage.Text + $"'\nGuid = {newQuiz.Guid}",
						replyMarkup: _botMenuService.NewQuizMenuMarkup);
				});

				currentUserData.IsWaitingForInput = false;

				return newQuiz;

			}

		}

public Question CreateNewQuestion(Message quizMessage)
		{

			User currentUser = quizMessage.From;
			UserData currentUserData = _userService.GetUserDataByUser(currentUser);

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

			Console.WriteLine(
				$"Вопрос {question.Guid} добавлен в квиз '{currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).Name}'," +
				$" всего времени на ответы в квизе = {currentUserData.Quizzes.First(quiz => quiz.Guid == currentUserData.EditingQuizGuid).GetTotalTimeSpan()}");
			return question;

		}

		async public Task FireQuiz(Guid? quizGuid, UserData userData)
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

		async Task SendQuestion(long chatId, Question question) 
		{

			Task.Run(async () =>
			{
				if (question.Type is MessageType.Text)
				{
					await _bot.SendTextMessageAsync(chatId, question.GetQuestionData());
					//Todo вынести в config отправлять ли в чят время на ответ
					
				}

				if (question.Type is MessageType.Photo)
				{
					await _bot.SendPhotoAsync(chatId, new InputFileId(question.GetQuestionData()));
					
				}

				if (question.Type is MessageType.Video)
				{
					await _bot.SendVideoAsync(chatId, new InputFileId(question.GetQuestionData()));
					
				}
			});
		}
	}
}
