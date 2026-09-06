using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface IPdfExportService
    {
        void GenerateGoodMoralCertificate(string filePath, StudentTranscriptResult student);
        void GenerateConductTranscript(string filePath, StudentTranscriptResult student);
        void GenerateParentNoticePdf(string filePath, string finalizedLetterBody, string officerName, string templateTitle);
        void GenerateOverdueComplianceReport(string filePath, List<AcknowledgmentLog> overdueRecords);
        void GenerateCampusSafetyHeatmapReport(string filePath, List<HeatmapNodeModel> nodes);
        void GenerateRecidivismReport(string filePath, RecidivismSummaryModel summary, List<ChronicOffenderWatchItem> chronicList);
        void GenerateExecutiveSummaryPdf(string filePath, ExecutiveReportData data, ExecutiveReportFlags flags);
        void GenerateAuditLedgerPdf(string filePath, List<AuditLogModel> logs);
        void GenerateComplianceCertificatePdf(string filePath, List<DiagnosticResultModel> auditResults);
        void GenerateSecurityAccessReportPdf(string filePath, List<AccessSecurityLogModel> logs);
    }

    public partial class PdfExportService : IPdfExportService
    {
        public PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void GenerateGoodMoralCertificate(string filePath, StudentTranscriptResult student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("REPUBLIC OF THE PHILIPPINES").FontSize(10).FontColor(Colors.Grey.Darken2);
                        col.Item().AlignCenter().Text("DEPARTMENT OF EDUCATION").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                        col.Item().AlignCenter().Text("SMART SCHOOL ACADEMY").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().AlignCenter().Text("Office of the Student Affairs & Discipline").FontSize(10).Italic().FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().PaddingBottom(20).AlignCenter().Text("CERTIFICATE OF GOOD MORAL CHARACTER").FontSize(20).Bold().FontColor(Colors.Blue.Darken4);

                        col.Item().Text("TO WHOM IT MAY CONCERN:").FontSize(12).Bold();

                        col.Item().PaddingTop(15).Text(text =>
                        {
                            text.Span("This is to certify that ");
                            text.Span(student.StudentName).Bold().Underline();
                            text.Span($" (Student ID: {student.StudentId}), enrolled in ");
                            text.Span($"{student.GradeLevel} - {student.Section}").Bold();
                            text.Span(", has maintained a satisfactory behavioral conduct standing with a Conduct Score of ");
                            text.Span($"{student.ConductScore:F1}/100").Bold();
                            text.Span(" during their stay in this institution.");
                        });

                        col.Item().PaddingTop(15).Text(text =>
                        {
                            text.Span("Based on official school records, the student has ");
                            text.Span("NO ACTIVE SUSPENSIONS").Bold();
                            text.Span(" or major disciplinary disqualifications on file as of ");
                            text.Span($"{DateTime.Now:MMMM dd, yyyy}").Bold();
                            text.Span(".");
                        });

                        col.Item().PaddingTop(15).Text("This certification is issued upon request for whatever legal or academic purpose it may serve.");

                        col.Item().PaddingTop(50).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Issued on:").FontSize(10).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{DateTime.Now:MMMM dd, yyyy}").FontSize(11).Bold();
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("____________________________").FontColor(Colors.Grey.Darken1);
                                c.Item().Text("Prefect of Discipline").Bold().FontSize(11);
                                c.Item().Text("Smart School Academy").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(4).AlignCenter().Text("Official Document • Smart Academy Incident Management System • Verification QR / Serial: SSA-GMC-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()).FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(filePath);
        }

        public void GenerateConductTranscript(string filePath, StudentTranscriptResult student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("SMART SCHOOL ACADEMY").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                                c.Item().Text("STUDENT BEHAVIORAL CONDUCT TRANSCRIPT").FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
                            });

                            row.ConstantItem(150).Column(c =>
                            {
                                c.Item().AlignRight().Text($"Date: {DateTime.Now:yyyy-MM-dd}").FontSize(10);
                                c.Item().AlignRight().Text($"Status: {student.StandingLabel}").FontSize(10).Bold();
                            });
                        });
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Student Name: {student.StudentName}").Bold();
                                c.Item().Text($"Student ID: {student.StudentId}");
                                c.Item().Text($"Grade & Section: {student.GradeLevel} - {student.Section}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Standing: {student.StandingLabel}").Bold();
                                c.Item().Text($"Conduct Score: {student.ConductScore:F1} / 100").Bold().FontColor(student.ConductScore >= 75 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                c.Item().Text($"Total Entries: {student.Entries.Count}");
                            });
                        });

                        col.Item().PaddingTop(15).Text("Incident & Disciplinary History Log").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(90);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Date").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Title").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Module").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Status").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Impact").Bold().FontColor(Colors.White).FontSize(9);
                            });

                            foreach (var inc in student.Entries)
                            {
                                table.Cell().Padding(5).Text(inc.FormattedDate).FontSize(9);
                                table.Cell().Padding(5).Text(inc.Title).FontSize(9).Bold();
                                table.Cell().Padding(5).Text(inc.SourceModule).FontSize(9);
                                table.Cell().Padding(5).Text(inc.ComplianceStatus).FontSize(9);
                                table.Cell().Padding(5).Text(inc.ScoreImpactText).FontSize(9).FontColor(inc.ScoreImpact < 0 ? Colors.Red.Medium : Colors.Green.Medium);
                            }
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(4).AlignCenter().Text("Official Record • Smart School Academy Behavioral Tracking Division").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(filePath);
        }

        public void GenerateParentNoticePdf(string filePath, string finalizedLetterBody, string officerName, string templateTitle)
        {
            if (string.IsNullOrWhiteSpace(finalizedLetterBody)) throw new ArgumentNullException(nameof(finalizedLetterBody));

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("SMART SCHOOL ACADEMY").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().AlignCenter().Text("OFFICE OF STUDENT DISCIPLINE & PARENT RELATIONS").FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                        col.Item().AlignCenter().Text("123 Academy Boulevard, Metro Manila • Tel: (02) 8800-1234").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().PaddingBottom(15).AlignCenter().Text(templateTitle.ToUpper()).FontSize(14).Bold().FontColor(Colors.Red.Darken2);

                        col.Item().Text(finalizedLetterBody).FontSize(11).LineHeight(1.5f);

                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Issued By:").FontSize(10).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(officerName).Bold().FontSize(11);
                                c.Item().Text("Discipline Officer").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("____________________________").FontColor(Colors.Grey.Darken1);
                                c.Item().Text("Parent / Guardian Signature").Bold().FontSize(11);
                                c.Item().Text("Date Signed: ________________").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(4).AlignCenter().Text("Official Document • Smart Academy Incident Management System • Parent Notice Registry").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(filePath);
        }
    }
}