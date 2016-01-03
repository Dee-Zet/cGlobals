using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class Example
{
    const string lexeme = @"\s*\b[_a-zA-Z][_a-zA-Z0-9]*\b";

    public static string fileChoseDialog()
    {
        Console.WriteLine ("Пожалуйста, введите путь к файлу:");
        string fileName = Console.ReadLine();
        if (System.IO.File.Exists(fileName))
            return fileName;
        else
            return null;
    }

    public static string lastWord(string matchString)
    {
        var splitedString = matchString.Split(' ');
        return splitedString[splitedString.Length - 1];
    }

    public static string addType(string types, string newType)
    {
        types = types.Substring(0, types.Length - 4);
        return types + @"|" + newType + @")\s+";
    }

    public static void addDeclaration(string matchedLexeme, List<string> names, List<int> counters)
    {
        if (names.IndexOf(matchedLexeme) == -1)
        {
            names.Add(matchedLexeme);
            counters.Add(-1);
        }
        else
            --counters[names.IndexOf(matchedLexeme)];
    }

    public static void countInitialization(string codeLine, string matchedLexeme, List<string> names, List<int> counters)
    {
        string initPattern = matchedLexeme + @"\s*=";
        foreach (Match match in Regex.Matches(codeLine, initPattern, RegexOptions.IgnoreCase))
            ++counters[names.IndexOf(matchedLexeme)];
    }

    public static void findCommaDeclarations(string codeLine, string firstVariable, string types, List<string> names, List<int> counters)
    {
        string commaDeclarationPattern = firstVariable + @"[\S\s^,]*?,(?!" + types + @")" + lexeme;
        Match commaMatch = Regex.Match(codeLine, commaDeclarationPattern, RegexOptions.None);
        while (commaMatch.Success)
        {
            addDeclaration(lastWord(commaMatch.Value), names, counters);
            countInitialization(codeLine, lastWord(commaMatch.Value), names, counters);
            commaDeclarationPattern = @"\b" + lastWord(commaMatch.Value) + @"\b[\S\s^,]*?,(?!" + types + @")" + lexeme;
            commaMatch = Regex.Match(codeLine, commaDeclarationPattern, RegexOptions.None);
        }
    }

    public static void countSpan(string codeLine, List<string> globalNames, List<int> globalCounters, List<string> localNames, List<int> localCounters, int bracketsCounter)
    {
        List<string> tempGlobalNames = new List<string>();
        tempGlobalNames = globalNames.ToList();
        foreach (string localName in localNames) 
            if (tempGlobalNames.IndexOf(localName) >= 0)
                tempGlobalNames.Remove(localName);

        foreach (string localName in localNames)
        {
            string searchPattern = @"\b" + localName + @"\b";
            foreach (Match match in Regex.Matches(codeLine, searchPattern, RegexOptions.None))
                ++localCounters[localNames.IndexOf(localName)];
        }

        foreach (string globalName in tempGlobalNames)
        {
            string searchPattern = @"\b" + globalName + @"\b";
            foreach (Match match in Regex.Matches(codeLine, searchPattern, RegexOptions.None))
                ++globalCounters[globalNames.IndexOf(globalName)];
        }
    }

    public static float countAup(float Aup, List<int> globalCounters, List<int> tempGlobalCounters)
    {
        for (int i = 0; i < globalCounters.Count; i++)
            if (globalCounters[i] > tempGlobalCounters[i])
                Aup++;
        return Aup;
    }

    public static void output(List<string> names, List<int> counters)
    {
        Console.WriteLine();
        foreach (string name in names)
            Console.WriteLine("Спен переменной " + name + "\t= " + counters[names.IndexOf(name)] + ".");
    }

    public static void prepareFile(string fileName)
    {
        System.IO.StreamReader file = new System.IO.StreamReader(fileName);
        string stringSearchPattern = "\"[\\s\\S]*?\"";
        string commentSearchPattern = @"/\*[\s\S]*?\*/";
        string codeFile = file.ReadToEnd();
        file.Close();
        
        string replacement = "\"\"";
        Regex rgx = new Regex(stringSearchPattern);
        codeFile = rgx.Replace(codeFile, replacement);

        replacement = @" ";
        rgx = new Regex(commentSearchPattern);
        codeFile = rgx.Replace(codeFile, replacement);

        commentSearchPattern = "//[^\n]*";
        replacement = "\r";
        rgx = new Regex(commentSearchPattern);
        codeFile = rgx.Replace(codeFile, replacement);

        System.IO.File.WriteAllText(fileName + @"~", codeFile);
    }

    public static void Main()
    {
        int bracketCounter = 0;
        float Aup = 0, Pup = 0;
        string codeLine;
        string patternTypedef = @"typedef\s*\b[_a-zA-Z0-9]+\b\s*\b[_a-zA-Z0-9]+\b";
        List<string> globalNames = new List<string>();
        List<int> globalCounters = new List<int>();
        List<int> tempGlobalCounters = new List<int>();
        List<string> localNames = new List<string>();
        List<int> localCounters = new List<int>();
        string types = @"\s*(void|int|signed|unsigned|short|long|char|float|double)\s+";
        string typeDeclaration = types + @"(?!" + types + @")" + lexeme + @"(?!(\s*\())";
        string procedureDeclaration = types + @"(?!" + types + @")" + lexeme + @"\s*\(";
        string fileName = fileChoseDialog();
        bool procedureDeclarationFlag = false;
        if (fileName != null)
        {
            prepareFile(fileName);
            System.IO.StreamReader file = new System.IO.StreamReader(fileName + @"~");
            while ((codeLine = file.ReadLine()) != null)
            {
                if (codeLine.Contains("{"))
                    bracketCounter++;
                Match typeMatch = Regex.Match(codeLine, patternTypedef, RegexOptions.None);
                if (typeMatch.Success)
                {
                    types = addType(types, lastWord(typeMatch.Value));
                    typeDeclaration = types + @"(?!" + types + @")" + lexeme + @"(?!(\s*\())";
                }
                Match procedureMatch = Regex.Match(codeLine, procedureDeclaration, RegexOptions.None);
                if (procedureMatch.Success)
                {
                    Console.WriteLine(procedureMatch.Value.Substring(0, procedureMatch.Value.Length - 1));
                    foreach (Match match in Regex.Matches(codeLine, typeDeclaration, RegexOptions.None))
                        if (bracketCounter == 0)
                        {
                            addDeclaration(lastWord(match.Value), localNames, localCounters);
                            countInitialization(codeLine, lastWord(match.Value), localNames, localCounters);
                            findCommaDeclarations(codeLine, lastWord(match.Value), types, localNames, localCounters);        
                        }
                    Pup = Pup + globalNames.Count;
                    tempGlobalCounters = globalCounters.ToList();
                    procedureDeclarationFlag = true;
                }
                else
                    foreach (Match match in Regex.Matches(codeLine, typeDeclaration, RegexOptions.None))
                        if (bracketCounter == 0)
                        {
                            addDeclaration(lastWord(match.Value), globalNames, globalCounters);
                            countInitialization(codeLine, lastWord(match.Value), globalNames, globalCounters);
                            findCommaDeclarations(codeLine, lastWord(match.Value), types, globalNames, globalCounters);
                        }
                        else
                        {
                            addDeclaration(lastWord(match.Value), localNames, localCounters);
                            countInitialization(codeLine, lastWord(match.Value), localNames, localCounters);
                            findCommaDeclarations(codeLine, lastWord(match.Value), types, localNames, localCounters);
                        }
                countSpan(codeLine, globalNames, globalCounters, localNames, localCounters, bracketCounter);
                if (codeLine.Contains("}"))
                    bracketCounter--;
                if (bracketCounter == 0)
                {
                    if (!procedureDeclarationFlag)
                    {
                        localNames.Clear();
                        localCounters.Clear();
                        if (tempGlobalCounters.Count > 0)
                        {
                            Aup = countAup(Aup, globalCounters, tempGlobalCounters);
                            tempGlobalCounters.Clear();
                        }
                    }
                    else
                        procedureDeclarationFlag = false;
                }
            }
            Console.WriteLine("\r\nГлобальные переменные");
            Console.WriteLine(Aup/Pup);
            file.Close();
        }
        else
            Console.WriteLine("Файл по данному пути отсутствует.");
        Console.ReadLine();
    }
}