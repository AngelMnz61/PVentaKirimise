using Microsoft.UI.Xaml.Controls;
namespace Kirimise;

public sealed class Limitador
{
    readonly int _max;
    readonly TimeSpan _ventana;
    readonly Queue<DateTime> _marcas = new();

    public Limitador(int max, TimeSpan ventana)
    {
        _max = max;
        _ventana = ventana;
    }

    public int Espera { get; private set; }

    public bool Permitir()
    {
        var ahora = DateTime.Now;
        while (_marcas.Count > 0 && ahora - _marcas.Peek() > _ventana) _marcas.Dequeue();
        if (_marcas.Count >= _max)
        {
            Espera = Math.Max(1, (int)Math.Ceiling((_ventana - (ahora - _marcas.Peek())).TotalSeconds));
            return false;
        }
        _marcas.Enqueue(ahora);
        Espera = 0;
        return true;
    }
}

public static class Validaciones
{
    public static void SinEspacios(TextBox tb, int max)
    {
        tb.MaxLength = max;
        tb.BeforeTextChanging += (s, a) =>
        {
            if (a.NewText.Any(char.IsWhiteSpace)) a.Cancel = true;
        };
    }

    public static void SinEspacios(PasswordBox pb, int max)
    {
        pb.MaxLength = max;
        pb.PasswordChanged += (s, a) =>
        {
            var limpio = new string(pb.Password.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (limpio != pb.Password) pb.Password = limpio;
        };
    }

    public static int Simbolos(string t) => t.Count(c => !char.IsLetterOrDigit(c) && c != ' ');

    public static void Texto(TextBox tb, int max, Func<char, bool> permitido, int maxSimbolos)
    {
        tb.MaxLength = max;
        tb.BeforeTextChanging += (s, a) =>
        {
            var t = a.NewText;
            if (t.Length == 0) return;
            if (!char.IsLetterOrDigit(t[0]) || t.Contains("  ") || t.Any(c => c != ' ' && !permitido(c)) || Repite(t, 3, 4) || Simbolos(t) > maxSimbolos) a.Cancel = true;
        };
    }

    public static void Busqueda(TextBox tb, int max, int maxEspacios)
    {
        tb.MaxLength = max;
        tb.BeforeTextChanging += (s, a) =>
        {
            var t = a.NewText;
            if (t.Length == 0) return;
            if (t[0] == ' ' || t.Contains("  ") || t.Count(c => c == ' ') > maxEspacios || t.Any(c => c != ' ' && !CaracterProducto(c)) || Repite(t, 3, 4)) a.Cancel = true;
        };
    }

    public static void Chat(TextBox tb, int max)
    {
        tb.MaxLength = max;
        tb.BeforeTextChanging += (s, a) =>
        {
            var t = a.NewText;
            if (t.Length == 0) return;
            if (t[0] == ' ' || t.Contains("  ") || t.Any(c => c == '\t' || c == '\r' || c == '\n')) { a.Cancel = true; return; }
            if (t.Count(char.IsDigit) > 6 || Repite(t, 3, 3)) a.Cancel = true;
        };
    }

    public static bool Repite(string t, int maxLetras, int maxDigitos)
    {
        int racha = 1;
        for (int i = 1; i < t.Length; i++)
        {
            racha = t[i] == t[i - 1] ? racha + 1 : 1;
            int tope = char.IsDigit(t[i]) ? maxDigitos : maxLetras;
            if (racha > tope) return true;
        }
        return false;
    }

    public static void Correo(TextBox tb, int max)
    {
        tb.MaxLength = max;
        tb.BeforeTextChanging += (s, a) =>
        {
            var t = a.NewText;
            if (t.Length == 0) return;
            int arroba = t.IndexOf('@');
            bool invalido =
                t.Length > max ||
                t.Any(c => c >= 128 || !(char.IsLetterOrDigit(c) || "._%+-@".Contains(c))) ||
                t.Count(c => c == '@') > 1 ||
                t[0] == '@' || t[0] == '.' ||
                t.Contains("..") || t.Contains(".@") || t.Contains("@.") ||
                (arroba >= 0 ? arroba > 64 : t.Length > 64) ||
                Repite(t, 3, 5);
            if (invalido) a.Cancel = true;
        };
    }

    public static void Decimal(TextBox tb, int enteros, int decimales, double max)
    {
        tb.MaxLength = enteros + decimales + 1;
        tb.InputScope = new Microsoft.UI.Xaml.Input.InputScope
        {
            Names = { new Microsoft.UI.Xaml.Input.InputScopeName { NameValue = Microsoft.UI.Xaml.Input.InputScopeNameValue.Number } }
        };
        tb.BeforeTextChanging += (s, a) =>
        {
            var t = a.NewText;
            if (t.Length == 0) return;
            if (!t.All(c => (c >= '0' && c <= '9') || c == '.') || t.Count(c => c == '.') > 1 || t[0] == '.') { a.Cancel = true; return; }
            var partes = t.Split('.');
            if (partes[0].Length > enteros || (partes[0].Length > 1 && partes[0][0] == '0') || (partes.Length > 1 && partes[1].Length > decimales)) { a.Cancel = true; return; }
            if (double.TryParse(t.TrimEnd('.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) && n > max) a.Cancel = true;
        };
    }

    public static void Clave(PasswordBox pb, int max)
    {
        pb.MaxLength = max;
        string ultimo = "";
        pb.PasswordChanged += (s, a) =>
        {
            var limpio = new string(pb.Password.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (limpio != pb.Password || Repite(limpio, 3, 4))
            {
                pb.Password = ultimo;
                return;
            }
            ultimo = limpio;
        };
    }

    public static void Numero(TextBox tb, int digitos, int min, int max)
    {
        tb.MaxLength = digitos;
        tb.InputScope = new Microsoft.UI.Xaml.Input.InputScope
        {
            Names = { new Microsoft.UI.Xaml.Input.InputScopeName { NameValue = Microsoft.UI.Xaml.Input.InputScopeNameValue.Number } }
        };
        tb.BeforeTextChanging += (s, a) =>
        {
            var t = a.NewText;
            if (t.Length == 0) return;
            if (t.Length > digitos || !t.All(c => c >= '0' && c <= '9') || (t[0] == '0' && (t.Length > 1 || min > 0)) || int.Parse(t) > max) a.Cancel = true;
        };
        tb.LostFocus += (s, a) =>
        {
            if (!int.TryParse(tb.Text, out var n) || n < min) tb.Text = min.ToString();
        };
    }

    public static bool CaracterProducto(char c) => char.IsLetterOrDigit(c) || ".,-'&/%+".Contains(c);
    public static bool CaracterDescripcion(char c) => char.IsLetterOrDigit(c) || ".,;:-'!?¡¿&/%+".Contains(c);

    public static bool Entero(double v, double min, double max) =>
        !double.IsNaN(v) && v == Math.Floor(v) && v >= min && v <= max;

    public static bool Dinero(double v, double max) =>
        !double.IsNaN(v) && v > 0 && v <= max && Math.Round(v, 2) == v;
}
