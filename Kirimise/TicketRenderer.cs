using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
namespace Kirimise;

public record TicketLinea(int Cant, string Nombre, decimal Total);
public record TicketData(string Folio, DateTime Fecha, int Mesa, string Cliente, List<TicketLinea> Lineas,
                         decimal Subtotal, decimal Iva, decimal Propina, decimal Total, string Last4);

public static class TicketRenderer
{
    const int W = 760, M = 28, PAD = 46, TOOTH = 12, TEETH = 40;
    static readonly Color Dark = Color.FromArgb(0x15, 0x0C, 0x26);
    static readonly Color Cream = Color.FromArgb(0xFF, 0xF7, 0xE8);
    static readonly Color Ink = Color.FromArgb(0x2A, 0x1B, 0x2E);
    static readonly Color Muted = Color.FromArgb(0x7A, 0x6A, 0x70);
    static readonly Color Red = Color.FromArgb(0xC8, 0x28, 0x3C);

    public static void Render(TicketData t, string logoPath, string outPath)
    {
        using var logo = File.Exists(logoPath) ? Image.FromFile(logoPath) : null;
        float h;
        using (var m = new Bitmap(1, 1))
        using (var g = Graphics.FromImage(m)) h = Draw(g, t, logo, false, 0);
        using var bmp = new Bitmap(W, (int)Math.Ceiling(h));
        using (var g2 = Graphics.FromImage(bmp)) Draw(g2, t, logo, true, h);
        bmp.Save(outPath, ImageFormat.Png);
    }

    static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath();
        p.AddArc(x, y, r * 2, r * 2, 180, 90);
        p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        p.CloseFigure();
        return p;
    }

    static float Draw(Graphics g, TicketData t, Image? logo, bool draw, float totalH)
    {
        var mx = MenuData.MX;
        if (draw)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Dark);

            var pts = new List<PointF>();
            float step = (W - 2f * M) / TEETH, top = M, bot = totalH - M;
            for (int i = 0; i <= TEETH; i++) pts.Add(new PointF(M + i * step, top + (i % 2 == 0 ? 0 : TOOTH)));
            for (int i = TEETH; i >= 0; i--) pts.Add(new PointF(M + i * step, bot - (i % 2 == 0 ? 0 : TOOTH)));
            using var paper = new SolidBrush(Cream);
            g.FillPolygon(paper, pts.ToArray());
        }

        using var fLabel = new Font("Segoe UI", 12, FontStyle.Bold, GraphicsUnit.Pixel);
        using var fVal = new Font("Segoe UI", 19, FontStyle.Bold, GraphicsUnit.Pixel);
        using var fItem = new Font("Segoe UI", 20, FontStyle.Regular, GraphicsUnit.Pixel);
        using var fItemB = new Font("Segoe UI", 20, FontStyle.Bold, GraphicsUnit.Pixel);
        using var fSmall = new Font("Segoe UI", 17, FontStyle.Regular, GraphicsUnit.Pixel);
        using var fTotal = new Font("Segoe UI", 30, FontStyle.Bold, GraphicsUnit.Pixel);
        using var fSerif = new Font("Georgia", 24, FontStyle.Italic, GraphicsUnit.Pixel);
        using var left = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        using var right = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.NoWrap };
        using var center = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

        void Txt(string s, Font f, Color c, float x, float yy, float w, StringFormat sf)
        {
            if (!draw) return;
            using var b = new SolidBrush(c);
            g.DrawString(s, f, b, new RectangleF(x, yy, w, f.Height + 4), sf);
        }
        void Dash(float yy)
        {
            if (!draw) return;
            using var p = new Pen(Color.FromArgb(150, Muted), 1.5f) { DashPattern = new float[] { 5, 5 } };
            g.DrawLine(p, PAD, yy, W - PAD, yy);
        }

        float cw = W - 2 * PAD, half = cw / 2, y = M + TOOTH + 26;

        const float ls = 230;
        if (logo != null) { if (draw) g.DrawImage(logo, (W - ls) / 2, y, ls, ls); y += ls + 4; }
        Txt("TICKET DE COMPRA", fLabel, Red, PAD, y, cw, center); y += 30;
        Dash(y); y += 20;

        var cliente = string.IsNullOrWhiteSpace(t.Cliente) ? "Invitado" : t.Cliente;
        Txt("FOLIO", fLabel, Muted, PAD, y, half, left); Txt("FECHA", fLabel, Muted, PAD + half, y, half, left);
        Txt(t.Folio, fVal, Ink, PAD, y + 16, half, left); Txt(t.Fecha.ToString("dd/MM/yyyy  HH:mm"), fVal, Ink, PAD + half, y + 16, half, left);
        y += 54;
        Txt("MESA", fLabel, Muted, PAD, y, half, left); Txt("CLIENTE", fLabel, Muted, PAD + half, y, half, left);
        Txt(t.Mesa.ToString(), fVal, Ink, PAD, y + 16, half, left); Txt(cliente, fVal, Ink, PAD + half, y + 16, half, left);
        y += 54;
        Dash(y); y += 18;

        Txt("CANT.", fLabel, Muted, PAD, y, 70, left);
        Txt("PRODUCTO", fLabel, Muted, PAD + 80, y, cw - 200, left);
        Txt("IMPORTE", fLabel, Muted, PAD, y, cw, right);
        y += 26;
        foreach (var l in t.Lineas)
        {
            Txt($"{l.Cant}×", fItemB, Red, PAD, y, 70, left);
            Txt(l.Nombre, fItem, Ink, PAD + 80, y, cw - 80 - 130, left);
            Txt(l.Total.ToString("C", mx), fItem, Ink, PAD, y, cw, right);
            y += 36;
        }
        y += 4; Dash(y); y += 16;

        void Row(string a, decimal v)
        {
            Txt(a, fSmall, Muted, PAD, y, cw, left);
            Txt(v.ToString("C", mx), fSmall, Ink, PAD, y, cw, right);
            y += 28;
        }
        Row("Subtotal", t.Subtotal);
        Row("IVA 16% incluido", t.Iva);
        Row("Propina", t.Propina);

        y += 8;
        if (draw)
        {
            using var path = RoundRect(PAD, y, cw, 60, 14);
            using var b = new SolidBrush(Red);
            g.FillPath(b, path);
        }
        Txt("TOTAL", fVal, Color.White, PAD + 22, y + 19, cw, left);
        Txt(t.Total.ToString("C", mx), fTotal, Color.White, PAD, y + 12, cw - 22, right);
        y += 60 + 24;

        Txt($"Pago con tarjeta terminación {t.Last4}  ·  Aprobado", fSmall, Muted, PAD, y, cw, center);
        y += 36;

        Txt("Gracias por visitarnos", fSerif, Ink, PAD, y, cw, center); y += 38;
        Txt("Que la noche te sea ligera.", fSmall, Muted, PAD, y, cw, center); y += 34;

        return y + TOOTH + M + 8;
    }
}
