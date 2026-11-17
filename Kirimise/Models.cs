using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
namespace Kirimise;

public class Usuario
{
    public string Nombre { get; set; } = "";
    public int Edad { get; set; }
    public string Username { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Rol { get; set; } = "Normal";
    public string Hash { get; set; } = "";
    public string Salt { get; set; } = "";
    public int Visitas { get; set; }
    public DateTime UltimaVisita { get; set; }
    public Dictionary<string, int> Compras { get; set; } = new();
    public List<string> Notas { get; set; } = new();
    [JsonIgnore] public bool EsAdmin => Rol == "Admin";
}

public class Producto : INotifyPropertyChanged
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Precio { get; set; }
    public string Categoria { get; set; } = "";
    public string ImagenPath { get; set; } = "";
    public bool SoloMayores { get; set; }

    int _stock;
    public int Stock
    {
        get => _stock;
        set { _stock = value; Raise(nameof(Stock)); Raise(nameof(StockText)); Raise(nameof(Disponible)); Raise(nameof(StockBrush)); }
    }

    [JsonIgnore] public string PrecioText => Precio.ToString("C", MenuData.MX);
    [JsonIgnore] public string StockText => Stock > 0 ? $"Stock: {Stock}" : "Agotado";
    [JsonIgnore] public bool Disponible => Stock > 0;
    static readonly Brush Rojo = new SolidColorBrush(ColorHelper.FromArgb(255, 200, 40, 60));
    static readonly Brush Oro = new SolidColorBrush(ColorHelper.FromArgb(255, 244, 185, 66));
    static readonly Brush Verde = new SolidColorBrush(ColorHelper.FromArgb(255, 63, 182, 139));
    [JsonIgnore] public Brush StockBrush => Stock == 0 ? Rojo : Stock <= 5 ? Oro : Verde;
    [JsonIgnore] public string AdminLinea => $"{PrecioText} · {StockText} · {Categoria}";

    ImageSource? _img;
    [JsonIgnore] public ImageSource? Imagen => _img ??= CargarImagen();
    public void RefreshImage() { _img = null; Raise(nameof(Imagen)); }

    ImageSource? CargarImagen()
    {
        var ruta = Store.ResolveImage(ImagenPath);
        if (ruta.Length == 0 || !File.Exists(ruta)) return null;
        return new BitmapImage(new Uri(ruta)) { DecodePixelWidth = 400 };
    }

    static Brush Solido(byte r, byte g, byte b) => new SolidColorBrush(ColorHelper.FromArgb(255, r, g, b));
    static readonly Dictionary<string, Brush> Fondos = new()
    {
        ["brasas"] = Solido(200, 40, 60),
        ["fritos"] = Solido(214, 150, 40),
        ["fideos"] = Solido(214, 98, 46),
        ["dulces"] = Solido(214, 90, 140),
        ["bebidas"] = Solido(52, 140, 190),
        ["otro"] = Solido(98, 70, 170)
    };

    [JsonIgnore]
    public Brush FondoBrush
    {
        get
        {
            var c = Categoria.ToLowerInvariant();
            var k = c.Contains("brasa") ? "brasas" : c.Contains("frito") ? "fritos" : c.Contains("fideo") ? "fideos"
                  : c.Contains("dulce") ? "dulces" : c.Contains("bebida") ? "bebidas" : "otro";
            return Fondos[k];
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class CartLine : INotifyPropertyChanged
{
    public Producto Item { get; }
    int _qty;
    public CartLine(Producto item, int qty) { Item = item; _qty = qty; }
    public int Qty
    {
        get => _qty;
        set { _qty = value; Raise(nameof(Qty)); Raise(nameof(Title)); Raise(nameof(TotalText)); }
    }
    public decimal Total => Item.Precio * Qty;
    public string Title => $"{Qty} × {Item.Nombre}";
    public string TotalText => Total.ToString("C", MenuData.MX);
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class ChatMsg : INotifyPropertyChanged
{
    static readonly Brush UserBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 200, 40, 60));
    static readonly Brush BotBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 58, 40, 96));
    string _text = "";
    public string Text
    {
        get => _text;
        set { _text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); }
    }
    public bool IsUser { get; init; }
    public HorizontalAlignment Align => IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    public Brush Bubble => IsUser ? UserBrush : BotBrush;
    public static ChatMsg Bot(string t) => new() { Text = t, IsUser = false };
    public static ChatMsg User(string t) => new() { Text = t, IsUser = true };
    public event PropertyChangedEventHandler? PropertyChanged;
}

public class Venta
{
    public string Folio { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "Invitado";
    public decimal Total { get; set; }
    public string Resumen { get; set; } = "";
    public List<VentaLinea> Lineas { get; set; } = new();
    [JsonIgnore] public string Titulo => $"{Folio}  ·  {Cliente}";
    [JsonIgnore] public string Detalle => $"{Fecha:dd/MM  HH:mm}  ·  {Resumen}";
    [JsonIgnore] public string TotalText => Total.ToString("C", MenuData.MX);
}

public class VentaLinea
{
    public string Nombre { get; set; } = "";
    public int Cant { get; set; }
    public decimal Importe { get; set; }
}
