using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;


namespace Final
{
	class FileHelper
	{
		private readonly string _filePath;

		public FileHelper(string filePath)
		{
			_filePath = filePath;
		}

		public async Task SaveQuizzesAsync(List<Quiz> quizzes)
		{
			var json = JsonSerializer.Serialize(quizzes, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(_filePath, json);
		}

		public async Task<List<Quiz>> LoadQuizzesAsync()
		{
			if (!File.Exists(_filePath))
				return new List<Quiz>();

			var json = await File.ReadAllTextAsync(_filePath);
			return JsonSerializer.Deserialize<List<Quiz>>(json);
		}
	}
}
