using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MookTest.Enums;
using MookTest.Model;

namespace MookTest.Services;

public static class ExcelQuestionImport
{
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxQuestions = 500;
    private const int MaxAnswers = 20;
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static List<Question> Parse(Stream stream)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > 200 || archive.Entries.Sum(e => e.Length) > 25 * 1024 * 1024)
                throw new ArgumentException("File Excel quá lớn hoặc có quá nhiều thành phần.");

            var strings = new List<string>();
            var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
            if (sharedEntry is not null)
            {
                var shared = ReadXml(sharedEntry);
                strings = shared.Descendants(Main + "si")
                    .Select(item => string.Concat(item.Descendants(Main + "t").Select(t => t.Value))).ToList();
            }

            var workbook = ReadXml(RequiredEntry(archive, "xl/workbook.xml"));
            var firstSheet = workbook.Descendants(Main + "sheet").FirstOrDefault()
                ?? throw new ArgumentException("File Excel không có trang tính.");
            var relationshipId = (string?)firstSheet.Attribute(OfficeRelationships + "id");
            var relationships = ReadXml(RequiredEntry(archive, "xl/_rels/workbook.xml.rels"));
            var target = relationships.Descendants(PackageRelationships + "Relationship")
                .FirstOrDefault(r => (string?)r.Attribute("Id") == relationshipId)?.Attribute("Target")?.Value;
            if (string.IsNullOrWhiteSpace(target) || target.Contains("://", StringComparison.Ordinal) ||
                target.Contains("..", StringComparison.Ordinal))
                throw new ArgumentException("Không tìm thấy trang tính đầu tiên trong file Excel.");
            var worksheetPath = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
            var sheet = ReadXml(RequiredEntry(archive, worksheetPath.Replace('\\', '/')));
            var rows = sheet.Descendants(Main + "sheetData").Elements(Main + "row").ToList();
            if (rows.Count == 0) throw new ArgumentException("File Excel không có dòng tiêu đề.");

            var header = Cells(rows[0], strings);
            if (Cell(header, 0) != "Question" || Cell(header, 1) != "Type")
                throw new ArgumentException("Dòng 1 phải bắt đầu bằng hai cột Question và Type. Hãy tải file mẫu.");
            for (var i = 2; i < header.Count; i++)
            {
                var expected = i % 2 == 0 ? $"Answer{i / 2}" : $"Correct{i / 2}";
                if (Cell(header, i) != expected)
                    throw new ArgumentException($"Cột {i + 1} phải có tên {expected}.");
            }
            if (header.Count < 6 || header.Count > 2 + MaxAnswers * 2 || header.Count % 2 != 0)
                throw new ArgumentException("File cần ít nhất hai cặp Answer/Correct và tối đa 20 cặp.");

            var questions = new List<Question>();
            foreach (var row in rows.Skip(1))
            {
                var line = (int?)row.Attribute("r") ?? questions.Count + 2;
                var cells = Cells(row, strings);
                if (cells.All(string.IsNullOrWhiteSpace)) continue;
                if (questions.Count >= MaxQuestions) throw new ArgumentException($"File chỉ được chứa tối đa {MaxQuestions} câu hỏi.");
                if (cells.Count > header.Count && cells.Skip(header.Count).Any(value => !string.IsNullOrWhiteSpace(value)))
                    throw new ArgumentException($"Dòng {line}: có dữ liệu ngoài các cột trong mẫu.");

                var content = Cell(cells, 0);
                if (string.IsNullOrWhiteSpace(content) || content.Length > 10000)
                    throw new ArgumentException($"Dòng {line}: Question phải có nội dung (tối đa 10000 ký tự).");
                if (!TryType(Cell(cells, 1), out var type))
                    throw new ArgumentException($"Dòng {line}: Type phải là số từ 1 đến 6.");

                var answers = new List<Answer>();
                for (var i = 2; i < header.Count; i += 2)
                {
                    var answer = Cell(cells, i);
                    var flag = Cell(cells, i + 1);
                    if (string.IsNullOrWhiteSpace(answer))
                    {
                        if (!string.IsNullOrWhiteSpace(flag))
                            throw new ArgumentException($"Dòng {line}: Correct{i / 2} có giá trị nhưng Answer{i / 2} trống.");
                        continue;
                    }
                    if (answer.Length > 1000 || !TryFlag(flag, out var correct))
                        throw new ArgumentException($"Dòng {line}: Answer{i / 2} quá dài hoặc Correct{i / 2} phải là 1/0 (TRUE/FALSE).");
                    answers.Add(new Answer { Content = answer, IsCorrect = correct });
                }

                var isChoice = type is QuestionType.MultipleChoice or QuestionType.SingleChoice or QuestionType.TrueFalse;
                if (isChoice)
                {
                    var correctCount = answers.Count(a => a.IsCorrect);
                    if (answers.Count < 2 || (type == QuestionType.TrueFalse && answers.Count != 2) ||
                        correctCount == 0 || (type != QuestionType.MultipleChoice && correctCount != 1))
                        throw new ArgumentException($"Dòng {line}: số đáp án hoặc số đáp án đúng không phù hợp với Type.");
                }
                else if (answers.Count != 0)
                    throw new ArgumentException($"Dòng {line}: câu trả lời văn bản không dùng các cột Answer/Correct.");

                questions.Add(new Question { Content = content, QuestionType = type, Answers = answers });
            }
            if (questions.Count == 0) throw new ArgumentException("File Excel không có câu hỏi để nhập.");
            return questions;
        }
        catch (InvalidDataException)
        {
            throw new ArgumentException("File không phải định dạng .xlsx hợp lệ.");
        }
        catch (XmlException)
        {
            throw new ArgumentException("Nội dung XML của file Excel không hợp lệ.");
        }
    }

    public static byte[] CreateTemplate()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            Add(zip, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Add(zip, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Questions" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            Add(zip, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            var header = new[] { "Question", "Type" }.Concat(Enumerable.Range(1, 4)
                .SelectMany(i => new[] { $"Answer{i}", $"Correct{i}" }));
            var cells = string.Concat(header.Select((value, index) =>
                $"<c r=\"{Column(index)}1\" t=\"inlineStr\"><is><t>{value}</t></is></c>"));
            Add(zip, "xl/worksheets/sheet1.xml", $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1">{cells}</row></sheetData></worksheet>
                """);
        }
        return output.ToArray();
    }

    private static void Add(ZipArchive zip, string path, string xml)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(xml);
    }

    private static ZipArchiveEntry RequiredEntry(ZipArchive zip, string path) =>
        zip.GetEntry(path) ?? throw new ArgumentException("File Excel thiếu thành phần bắt buộc.");

    private static XDocument ReadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 25 * 1024 * 1024
        });
        return XDocument.Load(reader);
    }

    private static List<string> Cells(XElement row, List<string> strings)
    {
        var values = new List<string>();
        foreach (var cell in row.Elements(Main + "c"))
        {
            if (cell.Element(Main + "f") is not null)
                throw new ArgumentException($"Dòng {(string?)row.Attribute("r")}: công thức không được hỗ trợ. Hãy dán giá trị vào file mẫu.");
            var reference = (string?)cell.Attribute("r") ?? "";
            var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
            var index = 0;
            foreach (var letter in letters.ToUpperInvariant()) index = checked(index * 26 + letter - 'A' + 1);
            index--;
            if (index < 0 || index > 100)
                throw new ArgumentException("File Excel có ô ngoài phạm vi cột được hỗ trợ.");
            while (values.Count <= index) values.Add("");
            var type = (string?)cell.Attribute("t");
            var raw = type == "inlineStr"
                ? string.Concat(cell.Descendants(Main + "t").Select(t => t.Value))
                : (string?)cell.Element(Main + "v") ?? "";
            if (type == "s")
            {
                if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var stringIndex) ||
                    stringIndex < 0 || stringIndex >= strings.Count)
                    throw new ArgumentException("File Excel có tham chiếu chuỗi không hợp lệ.");
                raw = strings[stringIndex];
            }
            values[index] = raw.Trim();
        }
        return values;
    }

    private static string Cell(List<string> cells, int index) => index < cells.Count ? cells[index] : "";

    private static bool TryType(string value, out QuestionType type)
    {
        type = default;
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
            Enum.IsDefined(type = (QuestionType)number);
    }

    private static bool TryFlag(string value, out bool correct)
    {
        correct = value is "1" or "TRUE" or "true";
        return value is "1" or "0" or "TRUE" or "FALSE" or "true" or "false";
    }

    private static string Column(int index)
    {
        var column = "";
        for (index++; index > 0; index = (index - 1) / 26)
            column = (char)('A' + (index - 1) % 26) + column;
        return column;
    }
}
