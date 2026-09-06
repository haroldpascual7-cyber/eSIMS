using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public partial class PdfExportService : IPdfExportService
    {
        public void GenerateAuditLedgerPdf(string filePath, List<AuditLogModel> logs)
        {
            if (logs == null) throw new ArgumentNullException(nameof(logs));

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("SMART SCHOOL ACADEMY - SYSTEM SECURITY DIVISION").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("SYSTEM AUDIT LEDGER & SECURITY ACTIVITY LOG").FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
                        col.Item().PaddingTop(4).Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} • Total Log Entries: {logs.Count}").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(110); // Timestamp
                                columns.ConstantColumn(80);  // User ID
                                columns.ConstantColumn(70);  // Action Type
                                columns.ConstantColumn(100); // Module
                                columns.RelativeColumn(3);  // Description
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Timestamp").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("User ID").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Action").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Module").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Description").Bold().FontColor(Colors.White).FontSize(8);
                            });

                            for (int i = 0; i < logs.Count; i++)
                            {
                                var item = logs[i];
                                string bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bg).Padding(4).Text(item.FormattedTimestamp).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.UserId).FontSize(8).Bold();
                                table.Cell().Background(bg).Padding(4).Text(item.ActionType).FontSize(8).Bold().FontColor(item.ActionType == "DELETE" ? Colors.Red.Darken2 : item.ActionType == "CREATE" || item.ActionType == "LOGIN" ? Colors.Green.Darken2 : Colors.Blue.Darken2);
                                table.Cell().Background(bg).Padding(4).Text(item.ModuleName).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.Description).FontSize(8);
                            }
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("Official Security Audit Record • Smart School Incident Management System").FontSize(8).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Page ");
                                txt.CurrentPageNumber();
                                txt.Span(" of ");
                                txt.TotalPages();
                            });
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        public void GenerateComplianceCertificatePdf(string filePath, List<DiagnosticResultModel> auditResults)
        {
            if (auditResults == null) throw new ArgumentNullException(nameof(auditResults));

            bool isAllPassed = auditResults.All(r => r.Status == "PASSED");
            string certificateStatus = isAllPassed ? "SYSTEM INTEGRITY VERIFIED" : "SYSTEM INTEGRITY COMPROMISED / WARNING";
            string titleColor = isAllPassed ? Colors.Green.Darken2 : Colors.Red.Darken2;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("SMART SCHOOL ACADEMY - SYSTEM SECURITY & COMPLIANCE").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("DATA INTEGRITY & CRYPTOGRAPHIC COMPLIANCE CERTIFICATE").FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
                        col.Item().PaddingTop(4).Text($"Audit Executed On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().PaddingBottom(15).Border(1.5f).BorderColor(titleColor).Background(isAllPassed ? Colors.Green.Lighten5 : Colors.Red.Lighten5).Padding(14).Column(c =>
                        {
                            c.Item().AlignCenter().Text(certificateStatus).FontSize(14).Bold().FontColor(titleColor);
                            c.Item().PaddingTop(4).AlignCenter().Text(isAllPassed ? "All system B-Tree data structures, relational foreign keys, and SHA-256 cryptographic hashes passed verification." : "One or more integrity checks reported warnings or data discrepancies requiring administrator attention.").FontSize(9).FontColor(Colors.Grey.Darken3);
                        });

                        col.Item().PaddingBottom(6).Text("Diagnostic Audit Results Summary").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(130);
                                columns.ConstantColumn(70);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Diagnostic Test").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Target Component").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Status").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Details").Bold().FontColor(Colors.White).FontSize(8);
                            });

                            for (int i = 0; i < auditResults.Count; i++)
                            {
                                var item = auditResults[i];
                                string bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bg).Padding(4).Text(item.TestName).FontSize(8).Bold();
                                table.Cell().Background(bg).Padding(4).Text(item.TargetComponent).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.Status).FontSize(8).Bold().FontColor(item.Status == "PASSED" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                table.Cell().Background(bg).Padding(4).Text(item.Details).FontSize(8);
                            }
                        });

                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Verified By:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text("Automated Security Engine").Bold().FontSize(10);
                                c.Item().Text("Smart Academy Security Division").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("____________________________").FontColor(Colors.Grey.Darken1);
                                c.Item().Text("Chief Information Officer").Bold().FontSize(10);
                                c.Item().Text("Digital Seal: SHA-256 Verified").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("Official Security Integrity Certificate • Smart School Incident Management System").FontSize(8).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Page ");
                                txt.CurrentPageNumber();
                                txt.Span(" of ");
                                txt.TotalPages();
                            });
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        public void GenerateSecurityAccessReportPdf(string filePath, List<AccessSecurityLogModel> logs)
        {
            if (logs == null) throw new ArgumentNullException(nameof(logs));

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("SMART SCHOOL ACADEMY - SECURITY OPERATIONS CENTER").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("SECURITY ACCESS LOG & AUTHENTICATION REPORT").FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
                        col.Item().PaddingTop(4).Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} • Total Records: {logs.Count}").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(105); // Timestamp
                                columns.ConstantColumn(80);  // Username
                                columns.ConstantColumn(80);  // Machine Name
                                columns.ConstantColumn(85);  // Event Type
                                columns.ConstantColumn(60);  // Status
                                columns.RelativeColumn(3);  // Details
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Timestamp").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Username").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Machine").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Event").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Status").Bold().FontColor(Colors.White).FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Details").Bold().FontColor(Colors.White).FontSize(8);
                            });

                            for (int i = 0; i < logs.Count; i++)
                            {
                                var item = logs[i];
                                string bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bg).Padding(4).Text(item.FormattedTimestamp).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.AttemptedUsername).FontSize(8).Bold();
                                table.Cell().Background(bg).Padding(4).Text(item.MachineName).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.EventType).FontSize(8);
                                table.Cell().Background(bg).Padding(4).Text(item.Status).FontSize(8).Bold().FontColor(item.Status == "Success" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                table.Cell().Background(bg).Padding(4).Text(item.Details).FontSize(8);
                            }
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("Official Security Access Report • Smart School Incident Management System").FontSize(8).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Page ");
                                txt.CurrentPageNumber();
                                txt.Span(" of ");
                                txt.TotalPages();
                            });
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }
    }
}