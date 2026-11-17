using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
namespace Kirimise;

public static class Store
{
    public const string AdminKey = "1929";

    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kirimise");
    static string UsersFile => Path.Combine(Dir, "usuarios.json");
    static string ProductsFile => Path.Combine(Dir, "productos.json");
    static readonly JsonSerializerOptions Opt = new() { WriteIndented = true };

    public static List<Usuario> LoadUsers()
    {
        try { if (File.Exists(UsersFile)) return JsonSerializer.Deserialize<List<Usuario>>(File.ReadAllText(UsersFile)) ?? new(); }
        catch { }
        return new();
    }
    public static void SaveUsers(IEnumerable<Usuario> users)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(UsersFile, JsonSerializer.Serialize(users, Opt));
    }

    public static List<Producto> LoadProducts()
    {
        try
        {
            if (File.Exists(ProductsFile))
            {
                var list = JsonSerializer.Deserialize<List<Producto>>(File.ReadAllText(ProductsFile));
                if (list != null)
                {
                    if (Normalizar(list)) SaveProducts(list);
                    return list;
                }
            }
        }
        catch { }
        var seed = MenuData.Semilla();
        SaveProducts(seed);
        return seed;
    }
    public static void SaveProducts(IEnumerable<Producto> products)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(ProductsFile, JsonSerializer.Serialize(products, Opt));
    }

    static string SalesFile => Path.Combine(Dir, "ventas.json");
    public static List<Venta> LoadSales()
    {
        try { if (File.Exists(SalesFile)) return JsonSerializer.Deserialize<List<Venta>>(File.ReadAllText(SalesFile)) ?? new(); }
        catch { }
        return new();
    }
    public static void SaveSales(IEnumerable<Venta> sales)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(SalesFile, JsonSerializer.Serialize(sales, Opt));
    }

    static string? _proyecto;

    public static string ProjectDir
    {
        get
        {
            if (_proyecto != null) return _proyecto;
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; d != null && i < 8; i++, d = d.Parent)
                if (d.GetFiles("*.csproj").Length > 0) return _proyecto = d.FullName;
            return _proyecto = AppContext.BaseDirectory;
        }
    }

    public static string ImagesDir => Path.Combine(ProjectDir, "Imagenes", "Productos");

    public static string ResolveImage(string path) =>
        string.IsNullOrEmpty(path) ? "" : Path.IsPathRooted(path) ? path : Path.Combine(ProjectDir, path);

    static string Slug(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in s.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c < 128 && char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }
        var r = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        if (r.Length > 30) r = r[..30].Trim('-');
        return r.Length == 0 ? "producto" : r;
    }

    public static string CopyImage(string source, string nombre)
    {
        Directory.CreateDirectory(ImagesDir);
        var archivo = $"{Slug(nombre)}-{Guid.NewGuid().ToString("N")[..6]}{Path.GetExtension(source).ToLowerInvariant()}";
        File.Copy(source, Path.Combine(ImagesDir, archivo), true);
        return Path.Combine("Imagenes", "Productos", archivo);
    }

    public static void DeleteImage(string path)
    {
        try
        {
            var full = Path.GetFullPath(ResolveImage(path));
            if (full.StartsWith(Path.GetFullPath(ImagesDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
                File.Delete(full);
        }
        catch { }
    }

    static readonly Dictionary<string, string> NombresNuevos = new()
    {
        ["Negima (cebollín)"] = "Negima de cebollín",
        ["Takoyaki (6 pzas)"] = "Takoyaki 6 piezas",
        ["Onigiri (2 pzas)"] = "Onigiri 2 piezas",
        ["Dango (3 bolitas)"] = "Dango 3 bolitas"
    };

    static bool Normalizar(List<Producto> lista)
    {
        bool cambio = false;
        foreach (var p in lista)
        {
            var n = NombresNuevos.TryGetValue(p.Nombre, out var nuevo)
                ? nuevo
                : Regex.Replace(p.Nombre.Replace("(", "").Replace(")", ""), @"\s{2,}", " ").Trim();
            if (n != p.Nombre) { p.Nombre = n; cambio = true; }
            if (!string.IsNullOrEmpty(p.ImagenPath) && Path.IsPathRooted(p.ImagenPath) && File.Exists(p.ImagenPath))
            {
                p.ImagenPath = CopyImage(p.ImagenPath, p.Nombre);
                cambio = true;
            }
        }
        return cambio;
    }

    public static (string hash, string salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var h = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(h), Convert.ToBase64String(salt));
    }
    public static bool Verify(string password, string hash, string salt)
    {
        var h = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), Convert.FromBase64String(salt), 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(h, Convert.FromBase64String(hash));
    }
}
