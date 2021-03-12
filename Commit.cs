using System.Collections.Generic;
using System.Linq;

namespace GitTest
{
	internal class Commit
	{
		private readonly CommitInfo _info;
		private readonly HashSet<Commit> _previousCommitSet;
		private readonly HashSet<Commit> _nextCommitSet;

		internal Commit(CommitInfo info)
		{
			_info = info;
			_previousCommitSet = new HashSet<Commit>();
			_nextCommitSet = new HashSet<Commit>();
		}

		internal string Hash => _info._hash;
		internal string Message => _info._message;
		internal int PreviousCommitCount => _previousCommitSet.Count;
		internal int NextCommitCount => _nextCommitSet.Count;
		internal IEnumerable<Commit> PreviousCommitCollection => _previousCommitSet.AsEnumerable();
		internal IEnumerable<Commit> NextCommitCollection => _nextCommitSet.AsEnumerable();

		public void AddPreviousCommit(Commit commit) => _previousCommitSet.Add(commit);
		public void AddNextCommit(Commit commit) => _nextCommitSet.Add(commit);
		public void RemovePreviousCommit(Commit commit) => _previousCommitSet.Remove(commit);
		public void RemoveNextCommit(Commit commit) => _nextCommitSet.Remove(commit);
	}
}