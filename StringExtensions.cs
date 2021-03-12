namespace GitTest
{
	static internal class StringExtensions
	{
		static private readonly char[] _specialCharacters;

		static StringExtensions()
		{
			_specialCharacters = new char[] { '\"' };
		}

		static internal bool IsError(this string text)
		{
			string lowerText = text.ToLower();
			return lowerText.Contains("error") || lowerText.Contains("fail") || lowerText.Contains("fall") || lowerText.Contains("fatal");
		}
		static internal string ToGitString(this string text)
		{
			foreach (char specialCharacter in _specialCharacters)
				text = text.Replace(specialCharacter.ToString(), "\\" + specialCharacter);
			return text;
		}
	}
}
