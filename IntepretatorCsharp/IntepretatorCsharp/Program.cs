using System;
using System.IO;

namespace IntepretatorCsharp
{
    public class Intepreter
    {
        const int BSIZE = 15;   // размер буфера
        const int NONE = -1;    // другой символ обычно ;
        const char EOS = '\0';  // конец строки
        const int NUM = 256;    // число
        const int ID = 257;     // переменная
        const int ARRAY = 258;  // массив
        const int IF = 259;
        const int THEN = 260;
        const int ELSE = 261;
        const int WHILE = 262;
        const int DO = 263;
        const int READ = 264;
        const int WRITE = 265;
        const int INT = 266;    // целочисленный тип переменной используется при объявлении
        const int INTM = 267;   // целочисленный тип массива используется при объявлении
        const int INDEX = 268;  // метка массива для ОПС
        const int METKAJF = 269; // метка перехода по "ложь" для ОПС
        const int METKAJ = 270; // метка безусловного перехода для ОПС
        const int DONE = 271;   // конец программы
        const int STRMAX = 999; // Размер массива лексем
        const int SYMMAX = 10000000; // Размер таблицы символов

        const int STACKSIZE = 500000;

        // Таблица лексического анализатора, номера семантических программ
        int[,] LexTable = {
            { 0,  2, 18,  4,  5,  6,  7,  8,  9, 20, 17, 10, 11, 12, 13, 26, 27, 28, 19, 24, 14, 29},
            { 1, 21, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 23, 19, 25 ,15, 15},
            { 21, 3, 16, 23, 16, 16, 16, 16, 16, 23, 16, 23, 16, 16, 16, 16, 23, 23, 19, 22 ,16, 23}
        };

        // Таблица переходов, по ASCII-коду символа получаем номер столбца в LexTable
        int[] TransitionTable = {
            18, 18, 18, 18, 18, 18, 18, 18, 18, 2, 19,
            18, 18, 19, 18, 18, 18, 18, 18, 18, 18,
            18, 18, 18, 18, 18, 18, 18, 18, 18, 18,
            18,  2, 18, 18,  3, 18, 18, 20, 18, 11,
            12,  6,  7, 21,  8, 18,  5,  1,  1,  1,
            1,  1,  1,  1,  1,  1,  1, 18, 15, 13,
            4, 14, 18, 18,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            9, 18, 10, 4, 18, 18,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0, 16, 18, 17, 18, 18
        };


        // Таблица ключевых слов
        TableEntry[] Keywords = {
            new TableEntry { Lexeme = "if", Token = IF },
            new TableEntry { Lexeme = "then", Token = THEN },
            new TableEntry { Lexeme = "else", Token = ELSE },
            new TableEntry { Lexeme = "while", Token = WHILE },
            new TableEntry { Lexeme = "in", Token = READ },
            new TableEntry { Lexeme = "out", Token = WRITE },
            new TableEntry { Lexeme = "integer", Token = INT },
            new TableEntry { Lexeme = "mass", Token = INTM },
            new TableEntry { Lexeme = "do", Token = DO },
            new TableEntry { Lexeme = "0", Token = 0 },
        };

        public TableEntry[] SymbolsTable = new TableEntry[SYMMAX]; // Таблица символов
        public TableEntry[] Variables = new TableEntry[SYMMAX];    // Переменные и их значения

        // Массивы и их содержимое
        public MassTableEntry[] MassesTable = new MassTableEntry[SYMMAX]; // хранит массивы во время выполнения OPSStack
        public MassElement[] Passport = new MassElement[SYMMAX]; // помогает обращаться к массивам

        public OPSElement[] OPSMass = new OPSElement[9999]; // Генерируемая ОПС

        public int OPSCounter = 0; // Счетчик элементов в ОПС
        public int LastChar = -1;  // Последняя использованная позиция в systemtablt Lexemes нужно для проверок переполнения массива лексемм
        public int LastEntry = 0;  // Последняя использованная позиция в таблице символов нужно для проверок переполнения символьной таблицы и адресации массивов:переменных и массивов 
        public int LookAhead;      // Тип лексемы очередная лексемма

        public char[] ProgramText; // Текст программы

        int k = -1;     // Номер обозреваемого символа
        int ss = -1;    // Номер символа в строке программы
        int es = -1;    // Ошибочные
        int semanticProgram;         //номер семантической программы было перед лексическим анализатором


        char[] LexBuffer = new char[BSIZE]; // Буфер Лексемы в лексическом анализаторе в нем хранится распознаваемая лаксемма
        int LineNumber = 1; // Номер строки
        int TokenValue = NONE;
        int x = 10; // Символьная длина элемента в ОПС

        // Типы элементов в ОПС
        public enum OPSType
        {
            IDE, MAS,   // переменная или массив
            NUMBER,     // число
            SIGN,       // операция
            POINT,      // точка перехода
            IND,        // индексатор
            METJF,      // метка для "ложь"
            METJ,       // метка для безусловного перехода
            RE,         // метка чтения
            WR          // метка записи
        };

        public struct OPSElement
        {
            public char[] Element { get; set; }
            public OPSType Type { get; set; }
        }

        // Запись в таблице символов и таблице переменных
        public struct TableEntry
        {
            public string Lexeme;
            public int Token;
        }

        public struct MassTableEntry
        {
            public char[] Mass;
            public int[] Element;
        }

        public struct MassElement
        {
            public int Mass;
            public int Element;
        }

        public struct StackElement
        {
            public int Value;
            public OPSType Type;
        }

        public class Stack
        {
            public Stack()
            {
                StackElements = new StackElement[STACKSIZE];
                ElementCounter = 0;
                Console.WriteLine("Stack Initialized");
            }

            public StackElement[] StackElements { get; set; }
            private int ElementCounter { get; set; }

            public void Push(StackElement element)
            {
                if (ElementCounter == STACKSIZE)
                {
                    Console.WriteLine("Stack is full");
                    return;
                }

                StackElements[ElementCounter] = element;
                ElementCounter++;
            }

            public StackElement Pop()
            {
                if (ElementCounter == 0)
                {
                    Console.WriteLine("Stack is empty");
                }

                ElementCounter--;
               return StackElements[ElementCounter];
            }
        }

        // Загрузка ключевых слов в таблицу символов
        public void Initialize()
        {
            TableEntry[] entries = Keywords;
            foreach (var entry in entries)
            {
                Insert(entry.Lexeme, entry.Token);
            }
        }

        // Возвращает положение в таблице символов для s
        public int Lookup(string s)
        {
            for (int i = LastEntry; i > 0; i--)
            {
                if (s.CompareTo(SymbolsTable[i].Lexeme) == 0)
                {
                    return i;
                }
            }
            return 0;
        }

        // Добавляет новую лексему и возвращает положение в таблице символов для s
        public int Insert(string s, int token)
        {
            int len = s.Length;

            bool massOverflow = LastEntry + 1 >= SYMMAX;
            if (massOverflow)
            {
                Console.WriteLine("Symbol Table is full");
                Environment.Exit(1);
            }

            bool lexMassOverflow = LastChar + len + 1 >= STRMAX;
            if (lexMassOverflow)
            {
                Console.WriteLine("Lexemes Array is full");
                Environment.Exit(1); ;
            }

            LastEntry++;                                                        // Переходим к следующей строке в таблице символов
            SymbolsTable[LastEntry].Token = token;                              // Устанавливаем хранимый токен
  
            SymbolsTable[LastEntry].Lexeme = s;  // Адрес начала лексемы в массиве дексем

            if (token == ID)
            {
                Variables[LastEntry].Lexeme = s;
            }

            if (token == ARRAY)
            {
                MassesTable[LastEntry].Mass = s.ToCharArray();
            }

            LastChar = LastChar + len + 1;         // Обновляем последнюю позицию в массиве лексем
            SymbolsTable[LastEntry].Lexeme = s;    // Заполняем массив лексем
        
            return LastEntry;
        }

        // Взятие очередного символа текста
        public char GetSymbol()
        {
            ss++; k++;
            return ProgramText[k];
        }

        // Лексический анализатор
        public int LexicalAnalyzer()
        {
            //state
            // 0 - начальное
            // 1, 2 - распознаем имя, распознаем константу
            // 3 - конечное
            // 4 - ошибочное конечное	 

            int b = 0, symbolFromLookup;
            char readSymbol; // очередной символ
            int state = 0;
            bool error = false;

            while ((!error) || (state < 3))
            {
                readSymbol = GetSymbol();
                semanticProgram = LexTable[state, TransitionTable[readSymbol]];

                switch (semanticProgram)
                {
                    //начало лексемы
                    case 0:
                        state = 1;
                        LexBuffer[b] = readSymbol;
                        b++;
                        break;

                    //накопление лексемы
                    case 1:
                        state = 1;
                        LexBuffer[b] = readSymbol;
                        b++;
                        break;

                    //Начало числа
                    case 2:
                        state = 2;
                        TokenValue = (int)Char.GetNumericValue(readSymbol);
                        break;

                    //Накопление числа
                    case 3:
                        state = 2;
                        TokenValue = TokenValue * 10 + (int)Char.GetNumericValue(readSymbol);
                        break;

                    //  #
                    case 4:
                    //  =
                    case 5:
                    //  /
                    case 6:
                    //  *
                    case 7:
                    //  +
                    case 8:
                    //  -
                    case 9:
                    //  (
                    case 10:
                    //  )
                    case 11:
                    //  <
                    case 12:
                    //  >
                    case 13:
                    //  ]
                    case 17:
                    // [
                    case 20:
                    //  ;
                    case 26:
                    //  {
                    case 27:
                    //  }
                    case 28:
                    // ~
                    // ,
                    case 29:
                        state = 3;
                        TokenValue = NONE;
                        return readSymbol;

                    //Конец файла
                    case 14:
                        state = 3;
                        return DONE;

                    // Имя
                    case 15:
                        state = 3;
                        LexBuffer[b] = EOS;
                        if (readSymbol != '@')
                        {
                            k--;
                            ss--;
                        }

                        //поиск слова в таблице символов
                        symbolFromLookup = Lookup(new string(LexBuffer));

                        if (symbolFromLookup == 0)
                        {
                            if (readSymbol == '[')
                                symbolFromLookup = Insert(new string(LexBuffer), ARRAY);
                            else
                                symbolFromLookup = Insert(new string(LexBuffer), ID);




                        }

                        TokenValue = symbolFromLookup;
                        Array.Clear(LexBuffer, 0, LexBuffer.Length);//добавил
                        return SymbolsTable[symbolFromLookup].Token;

                    // константа
                    case 16:
                        state = 3;
                        k--;
                        ss--;
                        return NUM;

                    //Пропуск символа
                    case 18:
                        state = 0;
                        break;

                    //не символ языка
                    case 19:
                        state = 4;
                        error = true;
                        TokenValue = NONE;
                        return readSymbol;

                    //Неверная лексема
                    case 21:
                        state = 4;
                        Console.WriteLine("Wrong lexem");
                        error = true;
                        break;

                    //константа, переход на новую строку
                    case 22:
                        state = 3;
                        return NUM;

                    //Другая ошибка
                    case 23:
                        state = 4;
                        Console.WriteLine("Wrong expr");
                        error = true;
                        break;

                    //Переход на новую строку
                    case 24:
                        state = 0;
                        LineNumber++;
                        ss = -1;
                        break;

                    //  имя, переход на новую строку
                    case 25:
                        state = 3;
                        LineNumber++;
                        ss = -1;
                        LexBuffer[b] = EOS;

                        if (readSymbol != '@')
                        {
                            k--;
                            ss--;
                        }

                        //поиск слова в таблице символов
                        symbolFromLookup = Lookup(new string(LexBuffer));

                        if (symbolFromLookup == 0)
                        {
                            if (readSymbol == '[')
                                symbolFromLookup = Insert(new string(LexBuffer), ARRAY);
                            else
                                symbolFromLookup = Insert(new string(LexBuffer), ID);
                        }

                        TokenValue = symbolFromLookup;
                        Array.Clear(LexBuffer, 0, LexBuffer.Length); // очистка буфера
                        return SymbolsTable[symbolFromLookup].Token;
                }

            }

            return 0;
        }

        // Сохранение ОПС
        public void Emit(int t, int tval)
        {
            OPSMass[OPSCounter].Element = new char[x];
            switch (t)
            {
                case '+':
                    OPSMass[OPSCounter].Element = "+".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    //Console.WriteLine((char)t);
                    break;
                case '-':
                    OPSMass[OPSCounter].Element = "-".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    // Console.WriteLine((char)t);
                    break;
                case '*':
                    OPSMass[OPSCounter].Element = "*".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    //Console.WriteLine((char)t);
                    break;
                case '/':
                    OPSMass[OPSCounter].Element = "/".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    //Console.WriteLine((char)t);
                    break;
                case '=':
                    OPSMass[OPSCounter].Element = "=".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    //Console.WriteLine((char)t);
                    break;
                case '>':
                    OPSMass[OPSCounter].Element = ">".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    Console.WriteLine((char)t);
                    break;
                case '<':
                    OPSMass[OPSCounter].Element = "<".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    //Console.WriteLine((char)t);
                    break;
                case '#':
                    OPSMass[OPSCounter].Element = "#".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    //Console.WriteLine((char)t);
                    break;
                case '~':
                    OPSMass[OPSCounter].Element = "~".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.SIGN;
                    OPSCounter++;
                    //Console.WriteLine((char)t);
                    break;
                case NUM:
                    OPSMass[OPSCounter].Element = tval.ToString().ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.NUMBER;
                    OPSCounter++;
                    //Console.WriteLine(tval);
                    break;
                case ARRAY:
                    OPSMass[OPSCounter].Element = SymbolsTable[tval].Lexeme.ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.MAS;
                    OPSCounter++;
                    //Console.WriteLine(SymbolsTable[tval].Lexeme);
                    break;
                case ID:
                    OPSMass[OPSCounter].Element = SymbolsTable[tval].Lexeme.ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.IDE;
                    OPSCounter++;
                    //Console.WriteLine(SymbolsTable[tval].Lexeme);
                    break;
                case INDEX:
                    OPSMass[OPSCounter].Element = "<i>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.IND;
                    OPSCounter++;
                    //Console.WriteLine("<i>");
                    break;
                case METKAJF:
                    OPSMass[OPSCounter].Element = "<jf>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.METJF;
                    OPSCounter++;
                    //Console.WriteLine("<jf>");
                    break;
                case METKAJ:
                    OPSMass[OPSCounter].Element = "<j>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.METJ;
                    OPSCounter++;
                    //Console.WriteLine("<j>");
                    break;
                case READ:
                    OPSMass[OPSCounter].Element = "<r>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.RE;
                    OPSCounter++;
                    //Console.WriteLine("<r>");
                    break;
                case WRITE:
                    OPSMass[OPSCounter].Element = "<w>".ToCharArray();
                    OPSMass[OPSCounter].Type = OPSType.WR;
                    OPSCounter++;
                    //Console.WriteLine("<w>");
                    break;
                default:
                    Console.WriteLine();
                    break;

            }
        }

        // Переход к следующей лексеме 
        public void Match(int t)
        {
            if (LookAhead == t)
                LookAhead = LexicalAnalyzer();
            else
            {
                Console.WriteLine("ERROR bad symbol" + "line: " + LineNumber + "  symbol: " + ss/2+"  Expected: "+LookAhead+"  Now: "+t);
            }
        }
        
        // Выделение памяти для хранения массивов
        public void SetSize()
        {
            if (LookAhead == NUM)
                MassesTable[LastEntry].Element = new int[TokenValue];
        }
        
        // Анализ операторов сравнения
        public void Comparison()
        {
            int t;
            switch (LookAhead)
            {

                case '#':
                case '>':
                case '<':
                    t = LookAhead;
                    Match(t);
                    Expression();
                    Emit(t, NONE);
                    break;
                default:
                    Console.WriteLine("error comp");
                    break;
            }
        }
        
        // Анализ условий
        public void Condition()
        {
            switch (LookAhead)
            {
                case NUM:
                    Emit(NUM, TokenValue);
                    Match(NUM);
                    MultiplyOrDivide();
                    PlusOrMinus();
                    Comparison();
                    break;
                case ID:
                    Emit(ID, TokenValue);
                    Match(ID);
                    MultiplyOrDivide();
                    PlusOrMinus();
                    Comparison();
                    break;
                case ARRAY:
                    Emit(ARRAY, TokenValue);
                    Match(ARRAY);
                    Match('[');
                    Expression();
                    Match(']');
                    Emit(INDEX, 0);
                    MultiplyOrDivide();
                    PlusOrMinus();
                    Comparison();
                    break;
                case '(':
                    Match('(');
                    Expression();
                    Match(')');
                    MultiplyOrDivide();
                    PlusOrMinus();
                    Comparison();
                    break;
                default:
                    Console.WriteLine("syntax ERROR cond");
                    Console.WriteLine("line =" + LineNumber + "symbol =" + es);
                    break;
            }

        }

        public void MultiplyOrDevideContent()
        {
            switch (LookAhead)
            {
                case NUM:
                    Emit(NUM, TokenValue);
                    Match(NUM);
                    break;

                case ID:
                    Emit(ID, TokenValue);
                    Match(ID);
                    break;

                case ARRAY:
                    Emit(ARRAY, TokenValue);
                    Match(ARRAY);
                    Match('[');
                    Expression();
                    Match(']');
                    Emit(INDEX, 0);
                    break;

                case '(':
                    Match('(');
                    Expression();
                    Match(')');
                    break;

                default:
                    Console.WriteLine("syntax ERROR Multuply or devide content");
                    Console.WriteLine("line = " + LineNumber + "  symbol = " + es);

                    int w = Convert.ToInt32(Console.ReadLine());
                    break;
            }
        }
        
        // Анализ умножения и деления
        public void MultiplyOrDivide()
        {
            int t;
            switch (LookAhead)
            {

                case '*':
                case '/':
                    t = LookAhead;
                    Match(LookAhead);
                    MultiplyOrDevideContent();
                    Emit(t, NONE);
                    MultiplyOrDivide();
                    break;

                default:
                    break;
            }
        }

        public void PlusOrMinusContent()
        {
            switch (LookAhead)
            {
                case NUM:
                    Emit(NUM, TokenValue);
                    Match(NUM);
                    MultiplyOrDivide();
                    break;

                case ID:
                    Emit(ID, TokenValue);
                    Match(ID);
                    MultiplyOrDivide();
                    break;

                case ARRAY:
                    Emit(ARRAY, TokenValue);
                    Match(ARRAY);
                    Match('[');
                    Expression();
                    Match(']');
                    Emit(INDEX, 0);
                    MultiplyOrDivide();
                    break;

                case '(':
                    Match('(');
                    Expression();
                    Match(')');
                    MultiplyOrDivide();
                    break;

                default:
                    Console.WriteLine("syntax ERROR Plus or minus content");
                    Console.WriteLine("line = " + LineNumber + "symbol = " + es);
                    Console.ReadLine();
                    break;
            }

        }
        
        // Анализ сложения и вычитания
        public void PlusOrMinus()
        {
            int t;

            switch (LookAhead)
            {
                case '+':
                case '-':
                    t = LookAhead;
                    Match(t);
                    PlusOrMinusContent();
                    Emit(t, NONE);
                    PlusOrMinus();
                    break;
                default:
                    break;
            }
        }
        
        // Анализ выражений и унарного минуса
        public void Expression()
        {
            switch (LookAhead)
            {
                case NUM:
                    Emit(NUM, TokenValue);
                    Match(NUM);
                    MultiplyOrDivide();
                    PlusOrMinus();
                    break;

                case ID:
                    Emit(ID, TokenValue);
                    Match(ID);
                    MultiplyOrDivide();
                    PlusOrMinus();
                    break;

                case ARRAY:
                    Emit(ARRAY, TokenValue);
                    Match(ARRAY);
                    Match('[');
                    Expression();
                    Match(']');
                    Emit(INDEX, 0);
                    MultiplyOrDivide();
                    PlusOrMinus();
                    break;

                case '(':
                    Match('(');
                    Expression();
                    Match(')');
                    MultiplyOrDivide();
                    PlusOrMinus();
                    break;

                case '~':
                    Match('~');
                    Expression();
                    Emit('~', NONE);
                    break;

                default:
                    Console.WriteLine("syntax ERROR");
                    Console.WriteLine("line = " + LineNumber + "symbol =" + es);
                    Console.ReadLine();
                    break;
            }

        }
        
        // Анализ Ввода и вывода а также расстановка меток для циклов и условий
        public void ExpressionWithBlocks()
        {
            int pointJF, pointJ;
            switch (LookAhead)
            {

                case ID:
                    Emit(ID, TokenValue);
                    Match(ID);
                    Match('=');
                    Expression();
                    Emit('=', NONE);
                    break;

                case ARRAY:
                    Emit(ARRAY, TokenValue);
                    Match(ARRAY);
                    Match('[');
                    Expression();
                    Match(']');
                    Emit(INDEX, 0);
                    Match('=');
                    Expression();
                    Emit('=', NONE);
                    break;

                case IF:
                    Match(IF);
                    Match('(');
                    Condition();
                    Match(')');
                    //место для будущей мектки в ОПС
                    pointJF = OPSCounter;
                    //к следующему элементу в ОПС
                    OPSCounter++;
                    Match(THEN);
                    Emit(METKAJF, 0);
                    ExpressionWithBlocks();

                    switch (LookAhead)
                    {

                        case ELSE:
                            Match(ELSE);
                            //место для будущей мектки в ОПС
                            pointJ = OPSCounter;
                            //к следующему элементу в ОПС
                            OPSCounter++;
                            //заносим <j> в ОПС
                            Emit(METKAJ, 0);
                            OPSMass[pointJF].Element = new char[x];
                            //адрес перехода для <jf>
                            OPSMass[pointJF].Element = OPSCounter.ToString().ToCharArray();

                            OPSMass[pointJ].Type = OPSMass[pointJF].Type = OPSType.POINT;
                            ExpressionWithBlocks();
                            OPSMass[pointJ].Element = new char[x];
                            OPSMass[pointJ].Element = OPSCounter.ToString().ToCharArray();
                            break;

                        default:
                            OPSMass[pointJF].Element = new char[x];
                            OPSMass[pointJF].Element = OPSCounter.ToString().ToCharArray();
                            OPSMass[pointJF].Type = OPSType.POINT;
                            break;
                    }
                    break;

                case WHILE:
                    Match(WHILE);
                    Match('(');
                    //адрес перехода для <j>
                    pointJ = OPSCounter;
                    //условие
                    Condition();
                    Match(')');
                    //место для будущей мектки в ОПС
                    pointJF = OPSCounter;
                    //к следующему элементу в ОПС
                    OPSCounter++;
                    Match(DO);
                    //заносим <jf> в ОПС 
                    Emit(METKAJF, 0);
                    ExpressionWithBlocks();
                    OPSMass[OPSCounter].Element = new char[x];
                    OPSMass[OPSCounter].Element = pointJ.ToString().ToCharArray();
                    OPSMass[OPSCounter].Type = OPSMass[pointJF].Type = OPSType.POINT;
                    OPSCounter++;
                    //заносим <j> в ОПС
                    Emit(METKAJ, 0);//заносим <j> в ОПС 
                    OPSMass[pointJF].Element = new char[x];
                    OPSMass[pointJF].Element = OPSCounter.ToString().ToCharArray();
                    break;

                case '{':
                    Match('{');
                    MegaExpression();
                    Match('}');
                    break;

                case READ:
                    Match(READ);
                    Match('(');
                    switch (LookAhead)
                    {
                        case ID:
                            Emit(ID, TokenValue);
                            Match(ID);
                            break;
                        case ARRAY:
                            Emit(ARRAY, TokenValue);
                            Match(ARRAY);
                            Match('[');
                            Expression();
                            Match(']');
                            Emit(INDEX, 0);
                            break;
                        default:
                            Console.WriteLine("error read(?)");
                            break;
                    }
                    Match(')');
                    Emit(READ, 0);
                    break;

                case WRITE:
                    Match(WRITE);
                    Match('(');
                    Expression();
                    Match(')');
                    Emit(WRITE, 0);
                    break;

                default:
                    Console.WriteLine("syntax ERROR Expresiion With Blocks");
                    Console.WriteLine("line = " + LineNumber + "  symbol = " + es);
                    Console.ReadLine();
                    break;
            }
        }
       
        // Анализ переменных и массивов в выражениях
        public void Name()
        {
            switch (LookAhead)
            {

                case ID:
                    Match(ID);
                    while (LookAhead != ';')
                    {
                        Match(',');
                        Match(ID);
                    }
                    break;

                case ARRAY:
                    Match(ARRAY);
                    Match('[');
                    SetSize();
                    Match(NUM);
                    Match(']');
                    while (LookAhead != ';')
                    {
                        Match(',');
                        Match(ARRAY);
                        Match('[');
                        SetSize();
                        Match(NUM);
                        Match(']');
                    }
                    break;

                default:
                    Console.WriteLine("syntax ERROR. Mistake in description variables");
                    break;
            }

        }
        
        // Анализ Ввода и вывода а также расстановка меток для циклов и условий
        public void MegaExpression()
        {
            int pointJF, pointJ;
            switch (LookAhead)
            {

                case ID:
                    Emit(ID, TokenValue);
                    Match(ID);
                    Match('=');
                    Expression();
                    Emit('=', NONE);
                    Match(';');
                    MegaExpression();
                    break;

                case ARRAY:
                    Emit(ARRAY, TokenValue);
                    Match(ARRAY);
                    Match('[');
                    Expression();
                    Match(']');
                    Emit(INDEX, 0);
                    Match('=');
                    Expression();
                    Emit('=', NONE);
                    Match(';');
                    MegaExpression();
                    break;

                case IF:
                    Match(IF);
                    Match('(');
                    Condition();
                    Match(')');
                    //место для будущей мектки в ОПС
                    pointJF = OPSCounter;
                    OPSCounter++;
                    Match(THEN);
                    Emit(METKAJF, 0);
                    ExpressionWithBlocks();

                    switch (LookAhead)
                    {

                        case ELSE:
                            Match(ELSE);
                            //место для будущей мектки в ОПС
                            pointJ = OPSCounter;
                            //к следующему элементу в ОПС
                            OPSCounter++;
                            //заносим <j> в ОПС
                            Emit(METKAJ, 0);
                            OPSMass[pointJF].Element = new char[x];
                            //адрес перехода для <jf>
                            OPSMass[pointJF].Element = OPSCounter.ToString().ToCharArray();
                            OPSMass[pointJ].Type = OPSMass[pointJF].Type = OPSType.POINT;
                            ExpressionWithBlocks();
                            OPSMass[pointJ].Element = new char[x];
                            OPSMass[pointJ].Element = OPSCounter.ToString().ToCharArray();
                            break;

                        default:
                            OPSMass[pointJF].Element = new char[x];
                            OPSMass[pointJF].Element = OPSCounter.ToString().ToCharArray();
                            OPSMass[pointJF].Type = OPSType.POINT;
                            break;
                    }

                    Match(';');
                    MegaExpression();
                    break;

                case WHILE:
                    Match(WHILE);
                    Match('(');
                    //адрес перехода для <j>
                    pointJ = OPSCounter;
                    Condition();//условие
                    Match(')');
                    //место для будущей мектки в ОПС
                    pointJF = OPSCounter;
                    //к следующему элементу в ОПС
                    OPSCounter++;
                    Match(DO);
                    //заносим <jf> в ОПС 
                    Emit(METKAJF, 0);
                    ExpressionWithBlocks();
                    OPSMass[OPSCounter].Element = new char[x];
                    OPSMass[OPSCounter].Element = pointJ.ToString().ToCharArray();
                    OPSMass[OPSCounter].Type = OPSMass[pointJF].Type = OPSType.POINT;
                    OPSCounter++;
                    //заносим <j> в ОПС 
                    Emit(METKAJ, 0);
                    OPSMass[pointJF].Element = new char[x];
                    OPSMass[pointJF].Element = OPSCounter.ToString().ToCharArray();
                    Match(';');
                    MegaExpression();
                    break;

                case READ:
                    Match(READ);
                    Match('(');
                    switch (LookAhead)
                    {

                        case ID:
                            Emit(ID, TokenValue);
                            Match(ID);
                            break;

                        case ARRAY:
                            Emit(ARRAY, TokenValue);
                            Match(ARRAY);
                            Match('[');
                            Expression();
                            Match(']');
                            Emit(INDEX, 0);
                            break;

                        default:
                            Console.WriteLine("error read(?)");
                            break;
                    }

                    Match(')');
                    Emit(READ, 0);
                    Match(';');
                    MegaExpression();
                    break;

                case WRITE:
                    Match(WRITE);
                    Match('(');
                    Expression();
                    Match(')');
                    Emit(WRITE, 0);
                    Match(';');
                    MegaExpression();
                    break;

                default:
                    break;

            }
        }

        // Начало синтаксического анализатора анализ объявления переменных
        public void InitializeNames()
        {
            if (LookAhead != DONE)
            {
                switch (LookAhead)
                {
                    case INT:
                        Match(INT);
                        Name();
                        Match(';');
                        InitializeNames();
                        break;
                    case INTM:
                        Match(INTM);
                        Name();
                        Match(';');
                        InitializeNames();
                        break;
                    default:
                        MegaExpression();
                        break;
                }
            }
            return;
        }

        // Выполнение программы
        public void OPSStack()
        {
            int length = OPSCounter;
            int z = 0, d = 0, res = 0, ps = 0;
            char t;

            Stack stack = new Stack();
            StackElement firstElement;
            StackElement secondElement;

            while (z < length)
            {
                switch (OPSMass[z].Type)
                {
                    case OPSType.IDE:
                        d = Lookup(new string (OPSMass[z].Element)); // Позиция в таблице переменных 
                        firstElement.Value = d;         // Позиция
                        firstElement.Type = OPSType.IDE; // Тип
                        stack.Push(firstElement);       // Помещаем в стек
                        z++;                            // Переходим к следующему элементу
                        break;

                    case OPSType.NUMBER:
                        firstElement.Value = Convert.ToInt32(new string (OPSMass[z].Element));
                        firstElement.Type = OPSType.NUMBER;
                        stack.Push(firstElement); // Помещаем в стек
                        z++;
                        break;

                    case OPSType.MAS:
                        d = Lookup(new string(OPSMass[z].Element)); // Позиция в таблице массивов
                        firstElement.Value = d; // Позиция
                        firstElement.Type = OPSType.MAS;
                        stack.Push(firstElement);
                        z++; // Переходим к следующему элементу
                        break;

                    case OPSType.IND:
                        firstElement = stack.Pop(); // Индекс элемента
                        secondElement = stack.Pop(); // Массив

                        switch (firstElement.Type)
                        {
                            case OPSType.IDE:
                                Passport[ps].Element = Variables[firstElement.Value].Token;
                                break;
                            case OPSType.NUMBER:
                                Passport[ps].Element = firstElement.Value;
                                break;
                            default:
                                break;
                        }

                        Passport[ps].Mass = secondElement.Value;
                        firstElement.Value = ps; ps++;
                        firstElement.Type = OPSType.MAS;
                        stack.Push(firstElement); z++;
                        break;

                    case OPSType.SIGN:
                        t = OPSMass[z].Element[0];
                        switch (t)
                        {
                            case '=':
                                firstElement = stack.Pop(); // Правое значение
                                secondElement = stack.Pop(); // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                Variables[secondElement.Value].Token = Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                Variables[secondElement.Value].Token = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                Variables[secondElement.Value].Token = firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] = Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                 MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] = firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value - firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] - secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;


                            case '*':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token * Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token * MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token * firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] * Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    * MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] * firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value * Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value * firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] * secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '+':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token + Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token + MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token + firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] + Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    + MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] + firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value + Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value + firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] + secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '-':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token - MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token - firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    - MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] - firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value - Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value - firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] - secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;


                            case '/':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = Variables[secondElement.Value].Token / Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = Variables[secondElement.Value].Token / MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = Variables[secondElement.Value].Token / firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] / Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    / MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element];
                                                break;

                                            case OPSType.NUMBER:
                                                res = MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] / firstElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                res = secondElement.Value / Variables[firstElement.Value].Token;
                                                break;

                                            case OPSType.NUMBER:
                                                res = secondElement.Value / firstElement.Value;
                                                break;

                                            case OPSType.MAS:
                                                res = MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] / secondElement.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '>':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение
                                res = 0;

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (Variables[secondElement.Value].Token > Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (Variables[secondElement.Value].Token > MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element])
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (Variables[secondElement.Value].Token > firstElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] > Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    > MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element])
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] > firstElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (secondElement.Value > Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (secondElement.Value > firstElement.Value)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] < secondElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '<':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение
                                res = 0;

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (Variables[secondElement.Value].Token < Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (Variables[secondElement.Value].Token < MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element])
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (Variables[secondElement.Value].Token < firstElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] < Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    < MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element])
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] < firstElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (secondElement.Value < Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (secondElement.Value < firstElement.Value)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] > secondElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '#':
                                firstElement = stack.Pop();     // Правое значение
                                secondElement = stack.Pop();    // Левое значение
                                res = 0;

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (Variables[secondElement.Value].Token == Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (Variables[secondElement.Value].Token == MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element])
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (Variables[secondElement.Value].Token == firstElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.MAS:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] == Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element]
                                                    == MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element])
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element] == firstElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    case OPSType.NUMBER:
                                        switch (firstElement.Type)
                                        {
                                            case OPSType.IDE:
                                                if (secondElement.Value == Variables[firstElement.Value].Token)
                                                    res = 1;
                                                break;

                                            case OPSType.NUMBER:
                                                if (secondElement.Value == firstElement.Value)
                                                    res = 1;
                                                break;

                                            case OPSType.MAS:
                                                if (MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] == secondElement.Value)
                                                    res = 1;
                                                break;

                                            default:
                                                break;
                                        }
                                        break;
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            case '~':
                                //firstElement = stack.Pop();     // Левый операнд 0
                                secondElement = stack.Pop();    // Правый операнд 

                                switch (secondElement.Type)
                                {
                                    case OPSType.IDE:
                                       res = - Variables[secondElement.Value].Token;
                                       break;

                                    case OPSType.MAS:
                                        res = - MassesTable[Passport[secondElement.Value].Mass].Element[Passport[secondElement.Value].Element];
                                        break;

                                    case OPSType.NUMBER:
                                       res = - secondElement.Value;
                                       break;
                                        
                                }

                                firstElement.Value = res;
                                firstElement.Type = OPSType.NUMBER;
                                stack.Push(firstElement);
                                z++;
                                break;

                            default:
                                break;
                        }
                        break;

                    case OPSType.POINT:
                        d = Convert.ToInt32(new string (OPSMass[z].Element));
                        z++;

                        if (OPSMass[z].Type == OPSType.METJF)
                        {
                            firstElement = stack.Pop();
                            if (firstElement.Value == 1)
                            {
                                z++;
                            }
                            else
                            {
                                z = d;
                            }
                        };

                        if (OPSMass[z].Type == OPSType.METJ)
                        {
                            z = d;
                        }
                        break;

                    case OPSType.WR:
                        firstElement = stack.Pop();
                        switch (firstElement.Type)
                        {
                            case OPSType.IDE:
                                z--;
                                d = Lookup(new string(OPSMass[z].Element));
                                Console.WriteLine(Variables[d].Token);
                                z = z + 2;
                                break;

                            case OPSType.NUMBER:
                                Console.WriteLine(firstElement.Value);
                                z++;
                                break;

                            case OPSType.MAS:
                                Console.WriteLine(MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element]);
                                z++;
                                break;

                            default:
                                break;
                        }
                        break;

                    case OPSType.RE:
                        firstElement = stack.Pop();
                        switch (firstElement.Type)
                        {
                            case OPSType.IDE:
                                z--;
                                d = Lookup(new string(OPSMass[z].Element));
                                Console.Write("?? ");
                                Variables[d].Token = Convert.ToInt32(Console.ReadLine());
                                z = z + 2;
                                break;

                            case OPSType.MAS:
                                Console.Write("??");
                                MassesTable[Passport[firstElement.Value].Mass].Element[Passport[firstElement.Value].Element] = Convert.ToInt32(Console.ReadLine());
                                z++;
                                break;

                            default:
                                break;
                        }
                        break;

                    default:
                        break;
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Intepreter Intepreter = new Intepreter();

            string fileName = "MathOperations.txt"; // имя исходного файла

            long fileSize;          
            fileSize = File.ReadAllText(fileName).Length;

            Intepreter.ProgramText = new char[fileSize+1];    //создание массива для текста программы

            char[] text = File.ReadAllText(fileName).ToCharArray();
            // переписываем в новый массив т.к. нужно ещё 1 ячейка памяти для @
            for (int i = 0; i < fileSize; i++)
            {
                Intepreter.ProgramText[i] = text[i];
            }
        
            Intepreter.ProgramText[Intepreter.ProgramText.Length-1] = '@';    // конечный символ

            Intepreter.Initialize();    // заполнение  таблицы ключевыми словами
            Intepreter.LookAhead = Intepreter.LexicalAnalyzer(); // 1 лексемма
            Intepreter.InitializeNames(); 

            Intepreter.OPSMass[Intepreter.OPSCounter].Element = new char[2];
            Intepreter.OPSMass[Intepreter.OPSCounter].Element = "@".ToCharArray();
            Intepreter.OPSMass[Intepreter.OPSCounter].Type = Intepreter.OPSType.SIGN;

            // Вывод ОПС в консоль
            for (int f = 0; f < Intepreter.OPSCounter + 1; f++) 
            {
                for (int i = 0; i < Intepreter.OPSMass[f].Element.Length; i++)
                {
                    if (Intepreter.OPSMass[f].Element[i]!='\0')
                    Console.Write(Intepreter.OPSMass[f].Element[i]);
                }
                Console.Write(" ");
            }
    
            Intepreter.OPSStack(); // Исполнение программы

            Console.ReadLine();

        }
    }
}



