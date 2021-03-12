namespace GitTest
{
	internal struct CommitInfo
	{
		internal string _hash;
		internal string _message;

		internal CommitInfo(string hash, string message)
		{
			_hash = hash;
			_message = message;
		}
	}
}