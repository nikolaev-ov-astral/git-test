using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitTest
{
	internal class SourceRepository
	{
		private readonly string _path;
		private readonly Dictionary<string, Commit> _commitByHash;
		private readonly Dictionary<string, Commit> _commitByBranchName;
		private readonly Commit _rootCommit;

		internal SourceRepository(string path)
		{
			_path = path;
			Directory.SetCurrentDirectory(path);
			string message = Git.Execute(path, $"fetch --all", out string error);
			message = Git.Execute(path, $"remote update origin --prune", out error);
			string[] remoteArray = Git.Execute(path, "branch -r --format \"%(refname)\"", out error).Split(Environment.NewLine);
			_commitByHash = new Dictionary<string, Commit>();
			_commitByBranchName = new Dictionary<string, Commit>();
			for (int branchIndex = 0x0; branchIndex != remoteArray.Length; branchIndex++)
			{
				string remote = remoteArray[branchIndex];
				if (remote.Length == 0x0)
					continue;
				var remoteBranchName = remote.Remove(0, "refs/remotes/".Length);
				if (remoteBranchName == "origin/HEAD")
					continue;
				string commitHash = Git.GetCommitHash(path, remoteBranchName);
				CommitInfo commitInfo = Git.GetCommitInfo(path, commitHash);
				if (!_commitByHash.TryGetValue(commitInfo._hash, out Commit commit))
					_commitByHash.Add(commitInfo._hash, commit = new Commit(commitInfo));
				var localBranchName = remoteBranchName.Remove(0, "origin/".Length);
				_commitByBranchName.Add(localBranchName, commit);
			}
			HashSet<Commit> rootCommits = new HashSet<Commit>(_commitByHash.Values);
			List<Commit> parentCommits = new List<Commit>(_commitByHash.Values);
			List<Commit> resolvingCommits = new List<Commit>();
			HashSet<Commit> resolvedCommits = new HashSet<Commit>();
			while (parentCommits.Count != 0x0)
			{
				resolvingCommits.Clear();
				resolvingCommits.AddRange(parentCommits);
				parentCommits.Clear();
				foreach (Commit resolvingCommit in resolvingCommits)
				{
					if (!resolvedCommits.Add(resolvingCommit))
						continue;
					List<string> parentCommitHashList = Git.GetParentCommitHashList(path, resolvingCommit.Hash);
					if (parentCommitHashList.Count != 0x0)
						rootCommits.Remove(resolvingCommit);
					foreach (string parentCommitHash in parentCommitHashList)
					{
						if (!_commitByHash.TryGetValue(parentCommitHash, out Commit parentCommit))
						{
							parentCommit = new Commit(Git.GetCommitInfo(path, parentCommitHash));
							_commitByHash.Add(parentCommitHash, parentCommit);
							rootCommits.Add(parentCommit);
							parentCommits.Add(parentCommit);
						}
						resolvingCommit.AddPreviousCommit(parentCommit);
						parentCommit.AddNextCommit(resolvingCommit);
					}
					resolvedCommits.Add(resolvingCommit);
				}
			}
			_rootCommit = rootCommits.Single();
		}

		internal string Path => _path;
		internal Commit RootCommit => _rootCommit;
		internal IEnumerable<string> BrancheNameCollection => _commitByBranchName.Keys;

		internal Commit GetCommitByBranchName(string branchName) => _commitByBranchName[branchName];
		internal Commit GetCommitByHash(string hash) => _commitByHash[hash];
	}
}