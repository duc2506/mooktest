using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using MookTest.Enums;
using MookTest.Services;

var template = ExcelQuestionImport.CreateTemplate();
Check(template.Length > 0, "template is generated");
try
{
    ExcelQuestionImport.Parse(new MemoryStream(template));
    throw new Exception("An empty template should not import questions.");
}
catch (ArgumentException error) when (error.Message.Contains("không có câu hỏi"))
{
    Console.WriteLine("PASS empty template is rejected");
}

XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
var source = new MemoryStream(template);
var filled = new MemoryStream();
using (var original = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true))
using (var output = new ZipArchive(filled, ZipArchiveMode.Create, leaveOpen: true))
{
    foreach (var entry in original.Entries)
    {
        var target = output.CreateEntry(entry.FullName);
        using var targetStream = target.Open();
        if (entry.FullName != "xl/worksheets/sheet1.xml")
        {
            using var input = entry.Open();
            input.CopyTo(targetStream);
            continue;
        }
        using var sheetStream = entry.Open();
        var sheet = XDocument.Load(sheetStream);
        var data = sheet.Descendants(ns + "sheetData").Single();
        data.Add(Row(2, "Capital of France?", "2", "Paris", "1", "London", "0"));
        data.Add(Row(3, "Explain your choice", "6"));
        sheet.Save(targetStream);
    }
}
filled.Position = 0;
var questions = ExcelQuestionImport.Parse(filled);
Check(questions.Count == 2, "two questions imported");
Check(questions[0].QuestionType == QuestionType.SingleChoice &&
      questions[0].Answers.Count == 2 && questions[0].Answers.Count(a => a.IsCorrect) == 1,
      "single-choice answers and correct flag parsed");
Check(questions[1].QuestionType == QuestionType.LongAnswer && questions[1].Answers.Count == 0,
      "text question has no choices");

static XElement Row(int number, params string[] values)
{
    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    return new XElement(ns + "row", new XAttribute("r", number), values.Select((value, index) =>
        new XElement(ns + "c", new XAttribute("r", $"{(char)('A' + index)}{number}"),
            new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", value)))));
}

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}
