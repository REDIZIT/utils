using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace REDIZIT.RUI
{
    public class Parser
    {
        private int current;
        private List<Token> tokens;
        private ILogger<Parser> logger;

        public Parser(ILogger<Parser> logger)
        {
	        this.logger = logger;
        }

        public Node_Root Parse(List<Token> sourceTokens)
        {
	        logger.LogDebug($"Parse {sourceTokens.Count} source tokens");
	        
            current = 0;
            tokens = sourceTokens;
            tokens.RemoveAll(t => t is Token_Comment);

            Node_Root root = new Node_Root();

            SkipTerminators();

            while (!IsAtEnd())
            {
                root.elements.Add(ParseElement());
                SkipTerminators();
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
	            StringBuilder b = new();
	            root.PrintTree(b, 0);
	            logger.LogDebug($"Parsed tree:\n{b}");
            }

            return root;
        }

        private Node_Element ParseElement()
		{
			logger.LogDebug("Parse Element");
			
		    SkipTerminators();
		    
		    // ЗАЩИТА ОТ БЕСКОНЕЧНОГО ЦИКЛА: запоминаем стартовую позицию
		    int startTokenIndex = current;

		    bool isTemplate = false;
		    bool isStyle = false;
		    
		    if (Check<Token_Identifier>() && Peek<Token_Identifier>(0).name == "template")
		    {
		        Consume<Token_Identifier>();
		        isTemplate = true;
		        SkipTerminators();
		    }
		    else if (Check<Token_Identifier>() && Peek<Token_Identifier>(0).name == "style") // <-- Добавлено
		    {
			    Consume<Token_Identifier>();
			    isStyle = true;
			    SkipTerminators();
		    }

		    string key = null;
		    if (Check<Token_Identifier>())
		    {
			    key = Consume<Token_Identifier>().name;
		    }

		    Node_Element element = new() { 
		        key = key,
		        isTemplate = isTemplate,
		        isStyle = isStyle
		    };

		    SkipTerminators();

		    if (Check<Token_BlockOpen>())
		    {
		        Consume<Token_BlockOpen>();
		        SkipTerminators();
		        while (!IsAtEnd() && !Check<Token_BlockClose>())
		        {
			        if (IsComponentStart(element.isStyle))
			        {
				        element.components.Add(ParseComponent());
			        }
		            else if (Check<Token_Identifier>() && Check<Token_Assign>(1)) 
		            {
		                Token_Identifier propName = Consume<Token_Identifier>();
		                Consume<Token_Assign>();
		                
		                logger.LogDebug($"Append element property '{propName.name}'");
		                element.properties.Add(new()
		                {
			                name = propName.name,
			                value = ParseExpression()
		                });
		            }
		            else
		            {
			            element.children.Add(ParseElement());
		            }
		            
		            SkipTerminators();
		        }
		        Consume<Token_BlockClose>();
		    }

		    // ЕСЛИ ПАРСЕР НИЧЕГО НЕ ПРОЧИТАЛ — ЭТО СИНТАКСИЧЕСКАЯ ОШИБКА И ЗАВИСАНИЕ!
		    if (current == startTokenIndex)
		    {
		        Token errorToken = IsAtEnd() ? new Token_EOF() : Peek();
		        throw new Exception($"[RUI Parser] Синтаксическая ошибка! Неизвестный токен '{errorToken.GetType().Name}' на строке {errorToken.line}.");
		    }

		    return element;
		}

		private bool IsComponentStart(bool isInsideStyle = false)
		{
			if (!Check<Token_Identifier>()) return false;

			// Type: ...
			if (Check<Token_Colon>(1)) return true;

			// Type#id: ...
			if (Check<Token_Hash>(1) && Check<Token_Identifier>(2) && Check<Token_Colon>(3)) return true;

			// Внутри style разрешен блочный синтаксис: Type { ... } и Type#id { ... }
			if (isInsideStyle)
			{
				if (Check<Token_BlockOpen>(1)) return true;
				if (Check<Token_Hash>(1) && Check<Token_Identifier>(2) && Check<Token_BlockOpen>(3)) return true;
			}

			return false;
		}

		private Node_Component ParseComponent()
		{
			Token_Identifier typeIdent = Consume<Token_Identifier>();
			string id = null;

			if (Check<Token_Hash>())
			{
				Consume<Token_Hash>();
				id = Consume<Token_Identifier>("Ожидался идентификатор после #").name;
			}

			bool isBlock = false;
			if (Check<Token_BlockOpen>())
			{
				Consume<Token_BlockOpen>();
				isBlock = true;
			}
			else
			{
				Consume<Token_Colon>();
			}

			Node_Component comp = new Node_Component
			{
				typeName = typeIdent.name,
				id = id
			};

			SkipSpacesOnly();

			if (isBlock)
			{
				SkipTerminators();
				while (!IsAtEnd() && !Check<Token_BlockClose>())
				{
					if (Check<Token_Identifier>() && Check<Token_Assign>(1))
					{
						comp.properties.Add(ParseSingleProperty());
					}
					SkipTerminators();
				}
				Consume<Token_BlockClose>();
			}
			else
			{
				if (Check<Token_Identifier>() && Check<Token_Assign>(1))
				{
					while (!IsAtEnd() && Check<Token_Identifier>() && Check<Token_Assign>(1))
					{
						comp.properties.Add(ParseSingleProperty());
						SkipSpacesOnly();
					}
				}
			}

			return comp;
		}

        private Node_Property ParseSingleProperty()
        {
            Token_Identifier propName = Consume<Token_Identifier>("Ожидалось имя свойства.");
            Consume<Token_Assign>("Ожидался знак '=' после имени свойства.");
            Node_Expression expr = ParseExpression();
            return new Node_Property { name = propName.name, value = expr };
        }

        private Node_Expression ParseExpression()
        {
	        logger.LogDebug("Parse Expression");
	        
            SkipSpacesOnly();

            // Векторы/кортежи: (300, 200) или (16, 16, 16, 16)
            if (Check<Token_BracketOpen>())
            {
	            Consume<Token_BracketOpen>();
	            Node_TupleLiteral tuple = new Node_TupleLiteral();

	            while (!Check<Token_BracketClose>())
	            {
		            // Рекурсивно парсим выражение (число или идентификатор)
		            tuple.elements.Add(ParseExpression());

		            if (Check<Token_Comma>())
		            {
			            Consume<Token_Comma>();
			            SkipTerminators();
		            }
		            else
		            {
			            break;
		            }
	            }

	            Consume<Token_BracketClose>("Ожидалась ')' закрывающая кортеж.");
	            return tuple;
            }

            if (Check<Token_Number>())
            {
                Token_Number t = Consume<Token_Number>();
                return new Node_NumberLiteral { value = float.Parse(t.word, CultureInfo.InvariantCulture) };
            }

            if (Check<Token_String>())
            {
                Token_String t = Consume<Token_String>();
                return new Node_StringLiteral { value = t.str };
            }

            if (Check<Token_ColorHex>())
            {
                Token_ColorHex t = Consume<Token_ColorHex>();
                return new Node_ColorLiteral { hex = t.hex };
            }

            if (Check<Token_Boolean>())
            {
                Token_Boolean t = Consume<Token_Boolean>();
                return new Node_BooleanLiteral { value = t.value };
            }

            if (Check<Token_Identifier>())
            {
                Token_Identifier t = Consume<Token_Identifier>();
                return new Node_IdentifierReference { name = t.name };
            }

            Token got = Peek();
            throw new($"Неожиданный токен для значения: '{got.GetType().Name}' на строке {got.line}");
        }

        private bool Check<T>(int offset = 0) where T : Token
        {
            int index = current + offset;
            if (index >= tokens.Count) return false;
            return tokens[index] is T;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private bool IsAtEnd() => current >= tokens.Count;

        private Token Peek(int offset = 0) => tokens[current + offset];
        
        private T Peek<T>(int offset = 0) where T : Token
        {
	        return (T)Peek(offset);
        }

        private Token Previous() => tokens[current - 1];

        private T Consume<T>(string errorMessage = "Неожиданная ошибка") where T : Token
        {
            if (Check<T>()) return (T)Advance();
            Token got = IsAtEnd() ? new Token_EOF() : Peek();
            throw new Exception($"Ошибка парсинга (строка {got.line}): {errorMessage} Получено: {got.GetType().Name}");
        }

        private bool SkipTerminators()
        {
            if (IsAtEnd()) return false;
            while (Peek() is Token_Terminator)
            {
                Advance();
                if (IsAtEnd()) return true;
            }
            return false;
        }

        private void SkipSpacesOnly()
        {
            // Токены пробелов уже отфильтрованы Tokenizer'ом
        }
    }
}