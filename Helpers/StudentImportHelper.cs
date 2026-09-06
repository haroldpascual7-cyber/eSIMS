using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SmartSchoolIncidentManagementSystem.Helpers
{
    public class StudentImportRow
    {
        public int RowIndex { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male";
        public string GradeLevel { get; set; } = "Grade 10";
        public string Section { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;
        public string GuardianContact { get; set; } = string.Empty;

        public bool IsValid { get; set; } = true;
        public string ValidationMessage { get; set; } = "Ready to Import";
        public string StatusColor => IsValid ? "#10B981" : "#EF4444";

        public Student ToStudent()
        {
            return new Student
            {
                StudentNumber = StudentNumber.Trim(),
                FirstName = FirstName.Trim(),
                MiddleName = MiddleName.Trim(),
                LastName = LastName.Trim(),
                Gender = string.IsNullOrWhiteSpace(Gender) ? "Male" : Gender.Trim(),
                GradeLevel = string.IsNullOrWhiteSpace(GradeLevel) ? "Grade 10" : GradeLevel.Trim(),
                Section = Section.Trim(),
                GuardianName = GuardianName.Trim(),
                GuardianEmail = GuardianEmail.Trim(),
                GuardianContact = GuardianContact.Trim(),
                GuardianRelation = "Parent",
                TrackStrand = "Academic"
            };
        }
    }

    public static class StudentImportHelper
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<StudentImportRow> ParseFile(string filePath, HashSet<string> existingStudentNumbers)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The selected import file could not be found.", filePath);

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            List<List<string>> rawRows;

            if (ext == ".csv")
            {
                rawRows = ParseCsv(filePath);
            }
            else if (ext == ".xlsx")
            {
                rawRows = ParseXlsx(filePath);
            }
            else
            {
                throw new NotSupportedException($"File extension '{ext}' is not supported. Please select a .csv or .xlsx file.");
            }

            return ValidateAndMapRows(rawRows, existingStudentNumbers);
        }

        private static List<List<string>> ParseCsv(string filePath)
        {
            var rows = new List<List<string>>();
            using var reader = new StreamReader(filePath);
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                rows.Add(ParseCsvLine(line));
            }

            return rows;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(CleanCsvValue(line.Substring(start, i - start)));
                    start = i + 1;
                }
            }

            values.Add(CleanCsvValue(line.Substring(start)));
            return values;
        }

        private static string CleanCsvValue(string raw)
        {
            string trimmed = raw.Trim();
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2)
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2).Replace("\"\"", "\"");
            }
            return trimmed.Trim();
        }

        private static List<List<string>> ParseXlsx(string filePath)
        {
            var rows = new List<List<string>>();

            using var archive = ZipFile.OpenRead(filePath);
            var sharedStrings = new List<string>();

            // Load shared strings table if present
            var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
            if (sharedEntry != null)
            {
                using var stream = sharedEntry.Open();
                var doc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                foreach (var si in doc.Descendants(ns + "si"))
                {
                    var tElem = si.Element(ns + "t");
                    if (tElem != null)
                    {
                        sharedStrings.Add(tElem.Value);
                    }
                    else
                    {
                        // Rich text run
                        string text = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
                        sharedStrings.Add(text);
                    }
                }
            }

            // Load sheet1.xml
            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet") && e.FullName.EndsWith(".xml"));
            if (sheetEntry == null)
                throw new InvalidDataException("The Excel workbook does not contain any valid worksheet data.");

            using (var stream = sheetEntry.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                foreach (var rowElem in doc.Descendants(ns + "row"))
                {
                    var cellDict = new SortedDictionary<int, string>();

                    foreach (var cElem in rowElem.Elements(ns + "c"))
                    {
                        string cellRef = cElem.Attribute("r")?.Value ?? string.Empty;
                        int colIndex = GetColumnIndexFromCellRef(cellRef);
                        string cellType = cElem.Attribute("t")?.Value ?? string.Empty;
                        string val = cElem.Element(ns + "v")?.Value ?? string.Empty;

                        if (cellType == "s" && int.TryParse(val, out int sIndex) && sIndex >= 0 && sIndex < sharedStrings.Count)
                        {
                            val = sharedStrings[sIndex];
                        }

                        cellDict[colIndex] = val.Trim();
                    }

                    if (cellDict.Count > 0)
                    {
                        int maxCol = cellDict.Keys.Max();
                        var rowList = new List<string>();
                        for (int c = 0; c <= maxCol; c++)
                        {
                            rowList.Add(cellDict.TryGetValue(c, out var val) ? val : string.Empty);
                        }
                        rows.Add(rowList);
                    }
                }
            }

            return rows;
        }

        private static int GetColumnIndexFromCellRef(string cellRef)
        {
            int index = 0;
            foreach (char c in cellRef)
            {
                if (char.IsLetter(c))
                {
                    index = index * 26 + (char.ToUpper(c) - 'A' + 1);
                }
                else break;
            }
            return Math.Max(0, index - 1);
        }

        private static List<StudentImportRow> ValidateAndMapRows(List<List<string>> rawRows, HashSet<string> existingStudentNumbers)
        {
            var result = new List<StudentImportRow>();
            if (rawRows.Count == 0) return result;

            int startIndex = 0;
            var header = rawRows[0].Select(h => h.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "")).ToList();

            int idCol = header.FindIndex(h => h.Contains("id") || h.Contains("number") || h.Contains("lrn"));
            int fnCol = header.FindIndex(h => h.Contains("first"));
            int mnCol = header.FindIndex(h => h.Contains("middle"));
            int lnCol = header.FindIndex(h => h.Contains("last"));
            int genderCol = header.FindIndex(h => h.Contains("gender") || h.Contains("sex"));
            int gradeCol = header.FindIndex(h => h.Contains("grade") || h.Contains("level"));
            int secCol = header.FindIndex(h => h.Contains("section"));
            int gNameCol = header.FindIndex(h => h.Contains("guardian") || h.Contains("parent"));
            int gEmailCol = header.FindIndex(h => h.Contains("email") || h.Contains("mail"));
            int gContactCol = header.FindIndex(h => h.Contains("contact") || h.Contains("phone") || h.Contains("mobile"));

            bool hasHeader = idCol >= 0 || fnCol >= 0 || lnCol >= 0;
            if (hasHeader)
            {
                startIndex = 1;
            }
            else
            {
                // Default positional columns: ID, FirstName, LastName, Grade, Section, GuardianName, GuardianEmail, GuardianContact
                idCol = 0; fnCol = 1; lnCol = 2; gradeCol = 3; secCol = 4; gNameCol = 5; gEmailCol = 6; gContactCol = 7;
            }

            var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = startIndex; i < rawRows.Count; i++)
            {
                var r = rawRows[i];
                if (r.All(string.IsNullOrWhiteSpace)) continue;

                string GetVal(int col) => (col >= 0 && col < r.Count) ? r[col].Trim() : string.Empty;

                var row = new StudentImportRow
                {
                    RowIndex = i + 1,
                    StudentNumber = GetVal(idCol),
                    FirstName = GetVal(fnCol),
                    MiddleName = GetVal(mnCol),
                    LastName = GetVal(lnCol),
                    Gender = string.IsNullOrWhiteSpace(GetVal(genderCol)) ? "Male" : GetVal(genderCol),
                    GradeLevel = string.IsNullOrWhiteSpace(GetVal(gradeCol)) ? "Grade 10" : GetVal(gradeCol),
                    Section = GetVal(secCol),
                    GuardianName = GetVal(gNameCol),
                    GuardianEmail = GetVal(gEmailCol),
                    GuardianContact = GetVal(gContactCol)
                };

                // Validate
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(row.StudentNumber))
                {
                    errors.Add("Missing Student ID");
                }
                else if (seenInFile.Contains(row.StudentNumber))
                {
                    errors.Add("Duplicate ID in file");
                }
                else if (existingStudentNumbers.Contains(row.StudentNumber.ToLowerInvariant()))
                {
                    errors.Add("ID already registered in system");
                }
                else
                {
                    seenInFile.Add(row.StudentNumber);
                }

                if (string.IsNullOrWhiteSpace(row.FirstName))
                    errors.Add("Missing First Name");

                if (string.IsNullOrWhiteSpace(row.LastName))
                    errors.Add("Missing Last Name");

                if (!string.IsNullOrWhiteSpace(row.GuardianEmail) && !EmailRegex.IsMatch(row.GuardianEmail))
                    errors.Add("Invalid Parent Email");

                if (errors.Count > 0)
                {
                    row.IsValid = false;
                    row.ValidationMessage = string.Join("; ", errors);
                }
                else
                {
                    row.IsValid = true;
                    row.ValidationMessage = "Valid";
                }

                result.Add(row);
            }

            return result;
        }
    }
}
