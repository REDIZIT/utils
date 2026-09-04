using System;
using System.Collections.Generic;

namespace InGame.UI
{
    public class Tokenizer
    {
        public string source;

        public int currentPos;
        public int startRead;
        public int endRead;

        public int beginLine;
        public int currentLine;
        public int linedCurrentPos;

        public string CurrentWord => source.Substring(startRead, currentPos - startRead);

        private static Dictionary<string, Type> singleCharTokens = new Dictionary<string, Type>()
        {
            { "{", typeof(Token_BlockOpen) },
            { "}", typeof(Token_BlockClose) },
            { "(", typeof(Token_BracketOpen) },
            { ")", typeof(Token_BracketClose) },
            { ":", typeof(Token_Colon) },
            { "=", typeof(Token_Assign) },
            { ",", typeof(Token_Comma) },
        };

        public List<Token> Tokenize(string sourceText, bool includeSpacesAndEOF = false)
        {
            Reset(sourceText, 0, sourceText.Length);

            List<Token> tokens = new List<Token>();

            while (true)
            {
                Token token = Advance();

                if (!includeSpacesAndEOF && (token is Token_Space || token is Token_EOF || token is Token_Comment))
                {
                    // Пропускаем
                }
                else
                {
                    tokens.Add(token);
                }

                if (token is Token_EOF) break;
            }

            return tokens;
        }

        public void Reset(string text, int start, int end)
        {
            source = text;
            currentPos = startRead = start;
            endRead = end;
            currentLine = linedCurrentPos = 0;
        }

        public Token Advance()
        {
            if (currentPos >= endRead)
            {
                return new Token_EOF();
            }

            startRead = currentPos;
            beginLine = currentLine;

            Token token = AdvanceInternal();
            FillToken(token);

            if (token is Token_Terminator == false)
            {
                linedCurrentPos += currentPos - startRead;
            }
            else
            {
                linedCurrentPos = 0;
            }

            return token;
        }

        private void FillToken(Token token)
        {
            token.begin = startRead;
            token.end = currentPos;
            token.line = beginLine;
            token.endLine = currentLine;
            token.linedBegin = linedCurrentPos;

            int len = currentPos - startRead;
            token.chars = new char[len];
            source.CopyTo(startRead, token.chars, 0, len);
        }

        private Token AdvanceInternal()
        {
            char startChar = source[currentPos++];

            // Пробелы
            if (startChar == ' ' || startChar == '\t')
            {
                while (currentPos < endRead && (source[currentPos] == ' ' || source[currentPos] == '\t'))
                {
                    currentPos++;
                }
                return new Token_Space();
            }

            // Переносы строк
            if (startChar == '\r' || startChar == '\n' || startChar == ';')
            {
                while (currentPos < endRead && (source[currentPos] == '\r' || source[currentPos] == '\n' || source[currentPos] == ';'))
                {
                    if (source[currentPos] == '\n') currentLine++;
                    currentPos++;
                }
                return new Token_Terminator();
            }

            // Одиночные символы
            if (singleCharTokens.TryGetValue(startChar.ToString(), out Type tokenType))
            {
                return (Token)Activator.CreateInstance(tokenType);
            }

            // Комментарии (// и /* */)
            if (startChar == '/' && currentPos < endRead)
            {
                if (source[currentPos] == '/')
                {
                    while (currentPos < endRead && source[currentPos] != '\n' && source[currentPos] != '\r')
                        currentPos++;
                    return new Token_Comment();
                }
                if (source[currentPos] == '*')
                {
                    currentPos++;
                    while (currentPos < endRead - 1)
                    {
                        if (source[currentPos] == '\n') currentLine++;
                        if (source[currentPos] == '*' && source[currentPos + 1] == '/')
                        {
                            currentPos += 2;
                            break;
                        }
                        currentPos++;
                    }
                    return new Token_Comment();
                }
            }

            // Символ решетки (#)
            if (startChar == '#')
            {
                // Если следом идет буква идентификатора, но НЕ 3, 6 или 8 hex-цифр подряд в значении цвета,
                // либо если это явный ID элемента (например, #myBtn)
                int hexCount = 0;
                int tempPos = currentPos;
                while (tempPos < endRead && IsHexCharacter(source[tempPos]))
                {
                    hexCount++;
                    tempPos++;
                }

                // Цвет имеет длину 3, 4, 6 или 8 hex-цифр и после них нет других букв
                bool isColor = (hexCount == 3 || hexCount == 4 || hexCount == 6 || hexCount == 8) &&
                               (tempPos >= endRead || !char.IsLetterOrDigit(source[tempPos]));

                if (isColor)
                {
                    currentPos = tempPos;
                    string hexString = "#" + source.Substring(startRead + 1, currentPos - (startRead + 1));
                    return new Token_ColorHex(hexString);
                }

                // Иначе это просто токен # для ID
                return new Token_Hash();
            }

            // Строки "..."
            if (startChar == '"')
            {
                int strStart = currentPos;
                while (currentPos < endRead)
                {
                    char c = source[currentPos++];
                    if (c == '"')
                    {
                        return new Token_String(source.Substring(strStart, currentPos - strStart - 1));
                    }
                    if (c == '\n' || c == '\r') return new Token_Bad();
                }
                return new Token_Bad();
            }

            // Числа
            if (char.IsDigit(startChar) || (startChar == '-' && currentPos < endRead && char.IsDigit(source[currentPos])))
            {
                bool hasDot = false;

                while (currentPos < endRead)
                {
                    char c = source[currentPos];
                    if (char.IsDigit(c))
                    {
                        currentPos++;
                    }
                    else if (c == '.' && !hasDot && currentPos + 1 < endRead && char.IsDigit(source[currentPos + 1]))
                    {
                        hasDot = true;
                        currentPos++;
                    }
                    else
                    {
                        break;
                    }
                }

                return new Token_Number(source.Substring(startRead, currentPos - startRead));
            }

            // Идентификаторы и булевы значения
            if (char.IsLetter(startChar) || startChar == '_')
            {
                while (currentPos < endRead && (char.IsLetterOrDigit(source[currentPos]) || source[currentPos] == '_'))
                {
                    currentPos++;
                }

                string word = CurrentWord;
                if (word == "true") return new Token_Boolean(true);
                if (word == "false") return new Token_Boolean(false);

                return new Token_Identifier { name = word };
            }

            return new Token_Bad();
        }

        private static bool IsHexCharacter(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }
    }
}