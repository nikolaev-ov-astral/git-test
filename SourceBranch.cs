using System.Collections.Generic;

namespace GitTest
{
	internal class SourceBranch
	{
		private readonly string _name;
		private readonly List<Commit> _commitList;
		private readonly HashSet<Commit> _commitSet;

		internal SourceBranch(string name, Commit commit)
		{
			_name = name;
			_commitList = new List<Commit>();
			_commitSet = new HashSet<Commit>();
			List<Commit> parentCommits = new List<Commit>();
			parentCommits.Add(commit);
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
					_commitList.Add(resolvingCommit);
					_commitSet.Add(resolvingCommit);
					IEnumerable<Commit> parentCommitList = resolvingCommit.PreviousCommitCollection;
					foreach (Commit parentCommit in parentCommitList)
						parentCommits.Add(parentCommit);
					resolvedCommits.Add(resolvingCommit);
				}
			}
		}

		public string Name => _name;
		public int CommitCount => _commitList.Count;

		public bool Contains(Commit commit) => _commitSet.Contains(commit);
		public Commit GetCommitByNumber(int number) => _commitList[_commitList.Count - 0x1 - number];
	}
}