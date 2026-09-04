namespace InGame.UI
{
	public abstract class Token
	{
		public int line;
		public int endLine;
		public int begin;
		public int end;
		public int linedBegin;
		public char[] chars;
	}

	public class Token_Identifier : Token
	{
		public string name;
	}

	public class Token_Colon : Token { }
	public class Token_Hash : Token { } // # (для id: Button#myBtn)
	public class Token_Assign : Token { }
	public class Token_Comma : Token { }

	public class Token_BlockOpen : Token { }          // {
	public class Token_BlockClose : Token { }         // }
	public class Token_BracketOpen : Token { }        // (
	public class Token_BracketClose : Token { }       // )

	public class Token_Terminator : Token { }
	public class Token_Space : Token { }
	public class Token_EOF : Token { }
	public class Token_Bad : Token { }
	public class Token_Comment : Token { }

	public class Token_String : Token
	{
		public string str;

		public Token_String(string str)
		{
			this.str = str;
		}
	}

	public class Token_Number : Token
	{
		public string word;

		public Token_Number(string word)
		{
			this.word = word;
		}
	}

	public class Token_ColorHex : Token
	{
		public string hex;

		public Token_ColorHex(string hex)
		{
			this.hex = hex;
		}
	}

	public class Token_Boolean : Token
	{
		public bool value;

		public Token_Boolean(bool value)
		{
			this.value = value;
		}
	}
}