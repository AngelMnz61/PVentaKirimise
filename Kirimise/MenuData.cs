using System.Globalization;
namespace Kirimise;

public static class MenuData
{
    public static readonly CultureInfo MX = new("es-MX");

    static Producto P(string id, string nombre, string cat, decimal precio, string desc, bool mayores = false) =>
        new() { Id = id, Nombre = nombre, Categoria = cat, Precio = precio, Descripcion = desc, Stock = 20, SoloMayores = mayores };

    public static List<Producto> Semilla() => new()
    {
        P("yakitori", "Yakitori de pollo", "Brasas", 45m, "Brocheta a la brasa con salsa tare."),
        P("negima", "Negima de cebollín", "Brasas", 52m, "Pollo y cebollín, ahumado y jugoso."),
        P("takoyaki", "Takoyaki 6 piezas", "Fritos", 75m, "Bolitas de pulpo con salsa, mayo y bonito."),
        P("tempura", "Tempura mixta", "Fritos", 95m, "Camarón y verduras, crujiente y ligera."),
        P("karaage", "Karaage", "Fritos", 85m, "Pollo frito estilo japonés con limón."),
        P("okonomiyaki", "Okonomiyaki", "Fritos", 110m, "Panqueque salado de col, a la plancha."),
        P("ramen", "Ramen shoyu", "Fideos y arroz", 130m, "Caldo de soya, huevo, cerdo y alga nori."),
        P("udon", "Udon caliente", "Fideos y arroz", 120m, "Fideo grueso en caldo; reconforta antes de una noche larga."),
        P("yakisoba", "Yakisoba picante", "Fideos y arroz", 115m, "Fideos salteados con col y salsa con picante."),
        P("onigiri", "Onigiri 2 piezas", "Fideos y arroz", 50m, "Bolas de arroz rellenas de atún o ciruela."),
        P("combo", "Combo Antes de la Misión", "Fideos y arroz", 199m, "Ramen + 2 yakitori + ramune. Para ir con todo."),
        P("dango", "Dango 3 bolitas", "Dulces", 40m, "Bolitas de arroz dulce en brocheta."),
        P("taiyaki", "Taiyaki de frijol dulce", "Dulces", 55m, "Pastelito con forma de pez, relleno de anko."),
        P("ramune", "Ramune", "Bebidas", 35m, "Refresco japonés con canica."),
        P("te", "Té verde caliente", "Bebidas", 30m, "Sin azúcar, limpia el paladar."),
        P("sake", "Sake tibio", "Bebidas", 90m, "Solo mayores de edad. Va bien con brasas.", mayores: true),
    };
}
