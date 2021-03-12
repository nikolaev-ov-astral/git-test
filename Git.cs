using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace GitTest
{
	static internal class Git
	{
		static internal void ClearRepository(string repositoryPath)
		{
			foreach (string filePath in Directory.GetFiles(repositoryPath))
				File.Delete(filePath);
			foreach (string subDirectoryPath in Directory.GetDirectories(repositoryPath))
			{
				if (Path.GetFileName(subDirectoryPath) == ".git")
					continue;
				Directory.Delete(subDirectoryPath, true);
			}
		}
		static internal void CopyRepository(string sourceRepositoryPath, string destRepositoryPath, long fileSizeLimit)
		{
			if (Path.GetFileName(sourceRepositoryPath) == ".git")
				return;
			if (!Directory.Exists(destRepositoryPath))
				Directory.CreateDirectory(destRepositoryPath);
			foreach (string sourceFilePath in Directory.GetFiles(sourceRepositoryPath))
			{
				string filePath = Path.Combine(destRepositoryPath, Path.GetFileName(sourceFilePath));
				if (fileSizeLimit != long.MaxValue && new FileInfo(sourceFilePath).Length > fileSizeLimit)
				{
					Console.Write("Skipping file: ");
					Console.WriteLine(sourceFilePath);
					continue;
				}
				File.Copy(sourceFilePath, filePath);
			}
			foreach (string sourceSubDirectoryPath in Directory.GetDirectories(sourceRepositoryPath))
				CopyRepository(sourceSubDirectoryPath, Path.Combine(destRepositoryPath, Path.GetFileName(sourceSubDirectoryPath)), fileSizeLimit);
		}
		static internal CommitInfo GetCommitInfo(string repositoryPath, string commitHash) => new CommitInfo(commitHash, Git.Execute(repositoryPath, $"log {commitHash} --pretty=format:\"%s\" -1", out string error));
		static internal string GetCommitHash(string repositoryPath, string target) => Git.Execute(repositoryPath, $"rev-parse {target}", out string error).TrimEnd();
		static internal List<string> GetParentCommitHashList(string repositoryPath, string commitHash)
		{
			string[] commitArray = Git.Execute(repositoryPath, $"rev-parse {commitHash}^@", out string error).Split(Environment.NewLine);
			List<string> commitHashList = new List<string>(commitArray.Length);
			foreach (string parentCommitHash in commitArray)
			{
				if (parentCommitHash.Length == 0x0)
					continue;
				commitHashList.Add(parentCommitHash);
			}
			return commitHashList;
		}
		static internal string Execute(string repositoryPath, string args, out string error)
		{
			ProcessStartInfo info = new ProcessStartInfo("git", args);
			info.WorkingDirectory = repositoryPath;
			info.RedirectStandardOutput = true;
			info.RedirectStandardError = true;
			Process process = Process.Start(info);
			StringBuilder outputStringBuilder = new StringBuilder();
			StringBuilder errorStringBuilder = new StringBuilder();
			SpinWait spinWait = default;
			while (!process.HasExited)
			{
				outputStringBuilder.Append(process.StandardOutput.ReadToEnd());
				errorStringBuilder.Append(process.StandardError.ReadToEnd());
				spinWait.SpinOnce();
			}
			outputStringBuilder.Append(process.StandardOutput.ReadToEnd());
			errorStringBuilder.Append(process.StandardError.ReadToEnd());
			string output = outputStringBuilder.ToString();
			error = errorStringBuilder.ToString();
			Console.Write(error);
			if (error.IsError())
			{

			}
			return output;
		}
	}
}
