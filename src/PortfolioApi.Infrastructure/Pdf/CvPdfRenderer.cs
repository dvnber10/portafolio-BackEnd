using PortfolioApi.Application.Models;
using PortfolioApi.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortfolioApi.Infrastructure.Pdf;

/// <summary>
/// Renderizador inspirado en moderncv "classic": nombre + contacto en la
/// primera página (sin repetición), secciones con regla lateral y layout de
/// dos columnas (período/etiqueta a la izquierda, contenido a la derecha).
/// Texto plano, sin imágenes: apto para ATS.
/// </summary>
public class CvPdfRenderer : ICvPdfRenderer
{
    private static readonly Color BandColor = Color.FromHex("#1F3A5F");
    private static readonly Color AccentColor = Color.FromHex("#2E6DA4");
    private static readonly Color TextColor = Color.FromHex("#2B2B2B");
    private static readonly Color MutedColor = Color.FromHex("#6B7280");

    // Columnas de moderncv classic (medidas del PDF de referencia):
    //  - etiqueta/período de x=30 a x=92.5 (ancho 62.7, texto alineado a la derecha)
    //  - contenido de x=101.5 hacia la derecha
    private const float LabelWidth = 62.7f;
    private const float LabelGap = 8.8f;
    private const float LeftSlot = LabelWidth + LabelGap;

    private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> ContactIcons = new(LoadContactIcons);

    private static IReadOnlyDictionary<string, byte[]> LoadContactIcons()
    {
        var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var asm = typeof(CvPdfRenderer).Assembly;
        const string marker = ".Icons.";
        foreach (var name in asm.GetManifestResourceNames())
        {
            var idx = name.LastIndexOf(marker, StringComparison.Ordinal);
            if (idx < 0 || !name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null) continue;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            map[name.Substring(idx + marker.Length, name.Length - idx - marker.Length - 4)] = ms.ToArray();
        }
        return map;
    }

    public CvPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(CvData data, string profileSlug)
    {
        var profile = data.Profiles.FirstOrDefault(p =>
                          string.Equals(p.Slug, profileSlug, StringComparison.OrdinalIgnoreCase))
                      ?? data.Profiles.FirstOrDefault()
                      ?? new ProfileItem { Slug = profileSlug, Title = data.Personal.Title };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(10).FontColor(TextColor).LineHeight(1.3f));

                page.Content().Column(section =>
                {
                    // ---- Cabecera (solo primera página) ----
                    TitleBlock(section, data.Personal, profile);

                    // ---- PERFIL PROFESIONAL ----
                    ModernSection(section, "Perfil Profesional", col =>
                    {
                        col.Item().PaddingTop(2).PaddingLeft(LeftSlot).Text(profile.Summary).FontSize(10);
                    });

                    // ---- EDUCACIÓN ----
                    if (data.Education.Count > 0)
                    {
                        ModernSection(section, "Educación", col =>
                        {
                            foreach (var edu in data.Education)
                                RenderEducation(col, edu);
                        });
                    }

                    // ---- EXPERIENCIA PROFESIONAL ----
                    var professionalExp = data.Experience
                        .Where(e => !string.Equals(e.Type, "project", StringComparison.OrdinalIgnoreCase))
                        .Where(e => AppliesTo(e, profile.Slug))
                        .ToList();
                    if (professionalExp.Count > 0)
                    {
                        ModernSection(section, "Experiencia Profesional", col =>
                        {
                            foreach (var exp in professionalExp)
                                RenderExperience(col, exp);
                        });
                    }

                    // ---- EXPERIENCIA Y PROYECTOS ----
                    var personalExp = data.Experience
                        .Where(e => string.Equals(e.Type, "project", StringComparison.OrdinalIgnoreCase))
                        .Where(e => AppliesTo(e, profile.Slug))
                        .ToList();
                    if (personalExp.Count > 0)
                    {
                        ModernSection(section, "Experiencia y Proyectos", col =>
                        {
                            foreach (var exp in personalExp)
                                RenderExperience(col, exp);
                        });
                    }

                    // ---- HABILIDADES TÉCNICAS ----
                    if (profile.Skills.Count > 0)
                    {
                        ModernSection(section, "Habilidades Técnicas", col =>
                        {
                            col.Item().PaddingTop(2).PaddingLeft(LeftSlot).Text(string.Join(", ", profile.Skills)).FontSize(10);
                        });
                    }

                    // ---- IDIOMAS ----
                    ModernSection(section, "Idiomas", col =>
                    {
                        foreach (var lang in data.Languages)
                        {
                            col.Item().PaddingVertical(1.5f).Row(row =>
                            {
                                row.ConstantItem(LeftSlot).Column(label =>
                                {
                                    label.Item().AlignRight().PaddingRight(LabelGap)
                                        .Text(lang.Name).FontSize(10.9f).FontColor(TextColor);
                                });
                                row.RelativeItem().Text(lang.Level).FontSize(10);
                            });
                        }
                    });

                    // ---- INTERESES ----
                    var interests = !string.IsNullOrWhiteSpace(profile.Interests)
                        ? profile.Interests
                        : FallbackInterests(data);
                    if (!string.IsNullOrWhiteSpace(interests))
                    {
                        ModernSection(section, "Intereses", col =>
                        {
                            col.Item().PaddingTop(2).PaddingLeft(LeftSlot).Text(interests).FontSize(10);
                        });
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber().FontSize(10).FontColor(MutedColor);
                    text.Span("/").FontSize(10).FontColor(MutedColor);
                    text.TotalPages().FontSize(10).FontColor(MutedColor);
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Nombre grande a la izquierda; contacto (con indicadores) a la derecha, misma banda superior.</summary>
    private static void TitleBlock(ColumnDescriptor section, PersonalInfo p, ProfileItem profile)
    {
        section.Item().Table(titleTable =>
        {
            titleTable.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(338);
                cd.ConstantColumn(201);
            });

            titleTable.Cell().AlignTop().Column(name =>
            {
                name.Item().Text(p.FullName).FontSize(29).FontColor(BandColor);
                name.Item().PaddingTop(2).Text(profile.Title).FontSize(17).FontColor(AccentColor);
            });

titleTable.Cell().AlignTop().AlignRight().Column(contact =>
                {
                    foreach (var item in ContactItems(p))
                    {
                        contact.Item().PaddingTop(3f).AlignRight().Row(iconRow =>
                        {
                            iconRow.ConstantItem(15).AlignMiddle().Column(icon =>
                            {
                                if (ContactIcons.Value.TryGetValue(item.Icon, out var bytes))
                                    icon.Item().Width(11).Height(11).Image(bytes);
                                else
                                    icon.Item().Width(8).Height(8).Background(AccentColor);
                            });
                            iconRow.ConstantItem(5);
                            iconRow.AutoItem().AlignMiddle().Column(txt =>
                            {
                                var cell = txt.Item();
                                if (item.Url != null) cell = cell.Hyperlink(item.Url!);
                                cell.Text(text =>
                                {
                                    var span = text.Span(item.Text).FontSize(10).FontColor(TextColor);
                                    if (item.Url != null) span.FontColor(AccentColor).Underline();
                                });
                            });
                        });
                    }
                });
        });
    }

    private static IEnumerable<(string Text, string? Url, string Icon)> ContactItems(PersonalInfo p)
    {
        if (!string.IsNullOrWhiteSpace(p.Location)) yield return (p.Location, null, "location");
        if (!string.IsNullOrWhiteSpace(p.Phone)) yield return (p.Phone, $"tel:{p.Phone.Replace(" ", "")}", "phone");
        if (!string.IsNullOrWhiteSpace(p.Email)) yield return (p.Email, $"mailto:{p.Email}", "gmail");
        if (!string.IsNullOrWhiteSpace(p.Github)) yield return (p.Github, $"https://{p.Github}", "github");
        if (!string.IsNullOrWhiteSpace(p.Linkedin)) yield return (p.Linkedin, $"https://linkedin.com/in/{p.Linkedin}", "linkedin");
        if (!string.IsNullOrWhiteSpace(p.Portfolio)) yield return ("Portafolio: Click Aqui", p.Portfolio, "portfolio");
    }

    /// <summary>Título de sección estilo moderncv: regla corta a la izquierda + texto 14 pt.</summary>
    private static void ModernSection(ColumnDescriptor section, string title, Action<ColumnDescriptor> body)
    {
        section.Item().PaddingTop(18).Row(header =>
        {
            header.ConstantItem(LeftSlot).AlignMiddle().Column(rule =>
            {
                rule.Item().Width(LabelWidth).Height(2f).Background(AccentColor);
            });
            header.RelativeItem().Text(title).Bold().FontSize(14).FontColor(BandColor);
        });
        section.Item().PaddingTop(4).Column(x => body(x));
    }

    private static void RenderEducation(ColumnDescriptor col, EducationItem edu)
    {
        col.Item().PaddingTop(7).Row(row =>
        {
            row.ConstantItem(LeftSlot).Column(left =>
            {
                var parts = SplitPeriod(edu.Period);
                foreach (var part in parts)
                {
                    left.Item().AlignRight().PaddingRight(LabelGap)
                        .Text(part).FontSize(10.9f).FontColor(TextColor);
                }
            });
            row.RelativeItem().Column(right =>
            {
                var title = string.Join(", ", new[] { edu.Degree, edu.Institution, edu.Place }.Where(s => !string.IsNullOrWhiteSpace(s)));
                right.Item().Text(title).FontSize(10.9f).FontColor(TextColor);

                foreach (var bullet in SplitDetail(edu.Detail))
                {
                    right.Item().PaddingLeft(3).PaddingTop(3).Row(bulletRow =>
                    {
                        bulletRow.ConstantItem(13).Text("•").Bold().FontColor(AccentColor);
                        bulletRow.RelativeItem().Text(bullet).FontSize(10);
                    });
                }
            });
        });
    }

    private static void RenderExperience(ColumnDescriptor col, ExperienceItem exp)
    {
        col.Item().PaddingTop(7).Row(row =>
        {
            row.ConstantItem(LeftSlot).AlignTop().Column(left =>
            {
                left.Item().AlignRight().PaddingRight(LabelGap)
                    .Text(exp.Period).FontSize(10.9f).FontColor(TextColor);
            });

            row.RelativeItem().Column(right =>
            {
                right.Item().Text($"{exp.Role}, {exp.Organization}").Bold().FontSize(10.9f).FontColor(TextColor);

                if (!string.IsNullOrWhiteSpace(exp.Link))
                {
                    var url = exp.Link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? exp.Link : $"https://{exp.Link}";
                    right.Item().PaddingTop(1).Hyperlink(url)
                        .Text(text => text.Span(url).FontSize(9).FontColor(AccentColor).Underline());
                }

                foreach (var resp in exp.Responsibilities)
                {
                    right.Item().PaddingLeft(3).PaddingTop(2).Row(bulletRow =>
                    {
                        bulletRow.ConstantItem(13).Text("•").Bold().FontColor(AccentColor);
                        bulletRow.RelativeItem().Text(resp).FontSize(10);
                    });
                }
            });
        });
    }

    private static bool AppliesTo(ExperienceItem exp, string slug)
        => exp.Profiles.Count == 0
           || exp.Profiles.Contains(slug, StringComparer.OrdinalIgnoreCase);

    /// <summary>Divide "2021 – 2026 (En curso)" en ["2021 – 2026", "(En curso)"].</summary>
    private static string[] SplitPeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return Array.Empty<string>();
        var idx = period.IndexOf('(');
        if (idx <= 0) return new[] { period };
        return new[] { period[..idx].Trim(), period[idx..] };
    }

    /// <summary>Separa el "detail" en viñetas usando ". " y " · ".</summary>
    private static List<string> SplitDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return new List<string>();
        var pieces = detail
            .Replace("·", ".")
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return pieces.Where(p => p.Length > 0).ToList();
    }

    private static string FallbackInterests(CvData data)
    {
        var interests = data.Profiles
            .Where(p => !string.IsNullOrWhiteSpace(p.Interests))
            .Select(p => p.Interests)
            .Distinct();
        return string.Join(" ", interests);
    }
}