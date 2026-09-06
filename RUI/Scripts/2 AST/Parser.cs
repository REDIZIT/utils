using System;
using System.Collections.Generic;
using System.Globalization;

namespace REDIZIT.RUI
{
    public static class Parser
    {
        public static int current;
        public static List<Token> tokens;

        public static Node_Root Parse(List<Token> sourceTokens)
        {
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

            return root;
        }

        // Парсинг CanvasElement:
        // { ... }
        public static Node_Element ParseElement()
		{
		    SkipTerminators();
		    
		    // ЗАЩИТА ОТ БЕСКОНЕЧНОГО ЦИКЛА: запоминаем стартовую позицию
		    int startTokenIndex = current;

		    bool isTemplate = false;
		    if (Check<Token_Identifier>() && Peek<Token_Identifier>(0).name == "template")
		    {
		        Consume<Token_Identifier>();
		        isTemplate = true;
		        SkipTerminators();
		    }

		    string composerType = "Fill";
		    if (Check<Token_Identifier>())
		    {
		        composerType = Consume<Token_Identifier>().name;
		    }

		    string key = null;
		    if (Check<Token_Hash>())
		    {
		        Consume<Token_Hash>();
		        key = Consume<Token_Identifier>("Ожидался ID").name;
		    }

		    Node_Element element = new Node_Element { 
		        key = key, 
		        composerType = composerType, 
		        isTemplate = isTemplate 
		    };

		    if (Check<Token_BracketOpen>())
		    {
		        Consume<Token_BracketOpen>();
		        SkipTerminators();
		        while (!IsAtEnd() && !Check<Token_BracketClose>())
		        {
		            Token_Identifier propName = Consume<Token_Identifier>();
		            Consume<Token_Assign>();
		            element.properties.Add(new Node_Property { name = propName.name, value = ParseExpression() });
		            if (Check<Token_Comma>()) Consume<Token_Comma>();
		            else break;
		            SkipTerminators();
		        }
		        Consume<Token_BracketClose>();
		    }

		    SkipTerminators();

		    if (Check<Token_BlockOpen>())
		    {
		        Consume<Token_BlockOpen>();
		        SkipTerminators();
		        while (!IsAtEnd() && !Check<Token_BlockClose>())
		        {
		            if (IsComponentStart()) element.components.Add(ParseComponent());
		            else if (Check<Token_Identifier>() && Check<Token_Assign>(1)) 
		            {
		                Token_Identifier propName = Consume<Token_Identifier>();
		                Consume<Token_Assign>();
		                element.properties.Add(new Node_Property { name = propName.name, value = ParseExpression() });
		            }
		            else element.children.Add(ParseElement());
		            
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

        private static bool IsComponentStart()
        {
            if (!Check<Token_Identifier>()) return false;

            // Type: ...
            if (Check<Token_Colon>(1)) return true;

            // Type#id: ...
            if (Check<Token_Hash>(1) && Check<Token_Identifier>(2) && Check<Token_Colon>(3)) return true;

            // Пустой компонент в конце строки (Type \n)
            if (Check<Token_Terminator>(1) || Check<Token_BlockClose>(1)) return true;

            return false;
        }

        // Парсинг компонента:
        // Button: { ... }
        // Button: normalColor = #FF0000, hoverColor = #00FF00
        // Button#myBtn: normalColor = #FF0000
        // Button:
        // Button
        public static Node_Component ParseComponent()
        {
            Token_Identifier typeIdent = Consume<Token_Identifier>();
            string id = null;

            if (Check<Token_Hash>())
            {
                Consume<Token_Hash>();
                id = Consume<Token_Identifier>("Ожидался идентификатор после #").name;
            }

            Node_Component comp = new Node_Component
            {
                typeName = typeIdent.name,
                id = id
            };

            // Двоеточие опционально, если компонент пустой
            bool hasColon = Check<Token_Colon>();
            if (hasColon) Consume<Token_Colon>();

            SkipSpacesOnly();

            // Вариант А: Многострочный блок в фигурных скобках { ... }
            if (Check<Token_BlockOpen>())
            {
                Consume<Token_BlockOpen>();
                SkipTerminators();

                while (!IsAtEnd() && !Check<Token_BlockClose>())
                {
                    comp.properties.Add(ParseSingleProperty());
                    SkipTerminators();
                }

                Consume<Token_BlockClose>("Ожидалась '}' блока компонента.");
            }
            // Вариант Б: Инлайн-свойства через запятую: prop = val, prop2 = val
            else if (hasColon && Check<Token_Identifier>() && Check<Token_Assign>(1))
            {
                while (!IsAtEnd() && Check<Token_Identifier>() && Check<Token_Assign>(1))
                {
                    comp.properties.Add(ParseSingleProperty());

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
            }
            // Вариант В: Пустой компонент (ничего не делаем)

            return comp;
        }

        private static Node_Property ParseSingleProperty()
        {
            Token_Identifier propName = Consume<Token_Identifier>("Ожидалось имя свойства.");
            Consume<Token_Assign>("Ожидался знак '=' после имени свойства.");
            Node_Expression expr = ParseExpression();
            return new Node_Property { name = propName.name, value = expr };
        }

        public static Node_Expression ParseExpression()
        {
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
            throw new Exception($"Неожиданный токен для значения: '{got.GetType().Name}' на строке {got.line}");
        }

        public static bool Check<T>(int offset = 0) where T : Token
        {
            int index = current + offset;
            if (index >= tokens.Count) return false;
            return tokens[index] is T;
        }

        public static Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        public static bool IsAtEnd() => current >= tokens.Count;

        public static Token Peek(int offset = 0) => tokens[current + offset];
        
        public static T Peek<T>(int offset = 0) where T : Token
        {
	        return (T)Peek(offset);
        }

        public static Token Previous() => tokens[current - 1];

        public static T Consume<T>(string errorMessage = "Неожиданная ошибка") where T : Token
        {
            if (Check<T>()) return (T)Advance();
            Token got = IsAtEnd() ? new Token_EOF() : Peek();
            throw new Exception($"Ошибка парсинга (строка {got.line}): {errorMessage} Получено: {got.GetType().Name}");
        }

        public static bool SkipTerminators()
        {
            if (IsAtEnd()) return false;
            while (Peek() is Token_Terminator)
            {
                Advance();
                if (IsAtEnd()) return true;
            }
            return false;
        }

        private static void SkipSpacesOnly()
        {
            // Токены пробелов уже отфильтрованы Tokenizer'ом
        }
    }
}
