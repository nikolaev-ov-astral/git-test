using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitTest
{
	static internal class Program
	{
		private const string _sourceRepositoryPath = "/home/nikolaev_ov/Documents/temp/repos/Celsus ЛК";
		private const string _destRepositoryPath = "/home/nikolaev_ov/Documents/temp/repos/legacy";
		private const long _fileSizeLimit = 0x100000;

		static private HashSet<Commit> GetCommits(Commit startCommit)
		{
			HashSet<Commit> commits = new HashSet<Commit>();
			List<Commit> childCommits = new List<Commit>();
			childCommits.Add(startCommit);
			List<Commit> resolvingCommits = new List<Commit>();
			HashSet<Commit> resolvedCommits = new HashSet<Commit>();
			while (childCommits.Count != 0x0)
			{
				resolvingCommits.Clear();
				resolvingCommits.AddRange(childCommits);
				childCommits.Clear();
				foreach (Commit resolvingCommit in resolvingCommits)
				{
					if (!resolvedCommits.Add(resolvingCommit))
						continue;
					commits.Add(resolvingCommit);
					IEnumerable<Commit> childCommitList = resolvingCommit.NextCommitCollection;
					foreach (Commit childCommit in childCommitList)
						childCommits.Add(childCommit);
					resolvedCommits.Add(resolvingCommit);
				}
			}
			return commits;
		}
		static internal void Main(string[] args)
		{
			if (!Git.Execute(_sourceRepositoryPath, $"status", out string error).Contains("nothing to commit"))
			{
				Console.WriteLine("There are not committed changes in the source repository.");
				return;
			}
			if (Directory.Exists(_destRepositoryPath))
			{
				Console.WriteLine("Destination repository is already created.");
				return;
			}
			SourceRepository sourceRepository = new SourceRepository(_sourceRepositoryPath);
			List<SourceBranch> sourceBranches = new List<SourceBranch>();
			foreach (string sourceBranchName in sourceRepository.BrancheNameCollection)
				sourceBranches.Add(new SourceBranch(sourceBranchName, sourceRepository.GetCommitByBranchName(sourceBranchName)));
			if (sourceBranches.Count == 0x0)
			{
				Console.WriteLine("There is no reason to recreate the repository.");
				return;
			}
			SourceBranch someSourceBranch = sourceBranches[0x0];
			Commit startCommit = sourceRepository.RootCommit;
			for (int someSourceBranchCommitNumber = 0x0; someSourceBranchCommitNumber != someSourceBranch.CommitCount; someSourceBranchCommitNumber++)
			{
				Commit someSourceBranchCommit = someSourceBranch.GetCommitByNumber(someSourceBranchCommitNumber);
				bool allHere = true;
				for (int sourceBranchIndex = 0x1; sourceBranchIndex != sourceBranches.Count; sourceBranchIndex++)
					if (!sourceBranches[sourceBranchIndex].Contains(someSourceBranchCommit))
					{
						allHere = false;
						break;
					}
				if (!allHere)
					break;
				startCommit = someSourceBranchCommit;
			}
			HashSet<Commit> commits = GetCommits(startCommit);
			Directory.CreateDirectory(_destRepositoryPath);
			string message = Git.Execute(_destRepositoryPath, $"init", out error);
			Dictionary<string, string> destCommitHashBySourceCommitHash = new Dictionary<string, string>();
			StringBuilder stringBuilder = new StringBuilder();
			List<Commit> childCommits = new List<Commit>();
			childCommits.Add(startCommit);
			List<Commit> resolvingCommits = new List<Commit>();
			HashSet<Commit> resolvedCommits = new HashSet<Commit>();
			HashSet<Commit> unresolvedCommits = new HashSet<Commit>();
			while (childCommits.Count != 0x0)
			{
				resolvingCommits.Clear();
				resolvingCommits.AddRange(childCommits);
				childCommits.Clear();
				foreach (Commit resolvingCommit in resolvingCommits)
				{
					if (!resolvedCommits.Add(resolvingCommit))
						continue;
					message = Git.Execute(_sourceRepositoryPath, $"reset {resolvingCommit.Hash} --hard", out error);
					if (resolvingCommit != startCommit)
					{
						Commit parentCommit = null;
						foreach (Commit checkingParentCommit in resolvingCommit.PreviousCommitCollection)
						{
							if (!commits.Contains(checkingParentCommit))
								continue;
							parentCommit = checkingParentCommit;
							break;
						}
						message = Git.Execute(_destRepositoryPath, $"reset {destCommitHashBySourceCommitHash[parentCommit.Hash]} --hard", out error);
						if (resolvingCommit.PreviousCommitCount > 0x1)
						{
							stringBuilder.Clear();
							foreach (Commit otherParentCommit in resolvingCommit.PreviousCommitCollection)
							{
								if (otherParentCommit == parentCommit || !commits.Contains(otherParentCommit))
									continue;
								stringBuilder.Append(destCommitHashBySourceCommitHash[otherParentCommit.Hash]);
								stringBuilder.Append(' ');
							}
							if (stringBuilder.Length != 0x0)
								message = Git.Execute(_destRepositoryPath, $"merge --no-ff --no-commit {stringBuilder.ToString()}", out error);
						}
					}
					Git.ClearRepository(_destRepositoryPath);
					Git.CopyRepository(_sourceRepositoryPath, _destRepositoryPath, _fileSizeLimit);
					message = Git.Execute(_destRepositoryPath, $"add .", out error);
					message = Git.Execute(_destRepositoryPath, $"commit -m \"{resolvingCommit.Message.ToGitString()}\"", out error);
					string resolvingCommitDestHash = Git.GetCommitHash(_destRepositoryPath, "HEAD");
					if (resolvingCommit == startCommit)
					{
						message = Git.Execute(_destRepositoryPath, $"checkout {resolvingCommitDestHash}", out error);
						message = Git.Execute(_destRepositoryPath, $"branch master -D", out error);
					}
					destCommitHashBySourceCommitHash.Add(resolvingCommit.Hash, resolvingCommitDestHash);
					IEnumerable<Commit> childCommitList = resolvingCommit.NextCommitCollection;
					foreach (Commit childCommit in childCommitList)
					{
						bool allHere = true;
						foreach (Commit parentCommit in childCommit.PreviousCommitCollection)
							if (commits.Contains(parentCommit) && !destCommitHashBySourceCommitHash.ContainsKey(parentCommit.Hash))
							{
								unresolvedCommits.Add(childCommit);
								allHere = false;
								break;
							}
						if (!allHere)
							continue;
						childCommits.Add(childCommit);
					}
					unresolvedCommits.Remove(resolvingCommit);
					resolvedCommits.Add(resolvingCommit);
				}
			}
			foreach (string sourceBranchName in sourceRepository.BrancheNameCollection)
				message = Git.Execute(_destRepositoryPath, $"branch {sourceBranchName} {destCommitHashBySourceCommitHash[sourceRepository.GetCommitByBranchName(sourceBranchName).Hash]}", out error);
		}
	}
}
