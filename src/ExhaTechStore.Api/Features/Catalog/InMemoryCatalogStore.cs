namespace ExhaTechStore.Api.Features.Catalog;

// Yurguen: Catálogo MVP en memoria; lista pre-ordenada y mapa por id para respuestas ágiles.
public sealed class InMemoryCatalogStore
{
    private static readonly CatalogProduct[] ProductsSeed =
    [
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000001"), "MK-MAR-001", "Martillo Uña 16oz", "Martillo de acero forjado para uso general.", 5500m, true, 28, "https://picsum.photos/seed/mk-tools-01/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000002"), "MK-MAR-002", "Martillo Bola 24oz", "Ideal para trabajo metálico y taller.", 7900m, true, 18, "https://picsum.photos/seed/mk-tools-02/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000003"), "MK-ALI-001", "Alicate Universal 8in", "Mango ergonómico y corte reforzado.", 4800m, true, 34, "https://picsum.photos/seed/mk-tools-03/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000004"), "MK-ALI-002", "Alicate Punta Larga 6in", "Precisión para cableado y electrónica básica.", 4300m, true, 26, "https://picsum.photos/seed/mk-tools-04/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000005"), "MK-ALI-003", "Alicate de Corte Diagonal", "Corte limpio para alambre y nylon.", 4600m, true, 30, "https://picsum.photos/seed/mk-tools-05/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000006"), "MK-DES-001", "Juego Destornilladores 6pz", "Plano y estrella en tamaños estándar.", 6200m, true, 22, "https://picsum.photos/seed/mk-tools-06/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000007"), "MK-DES-002", "Destornillador Phillips #2", "Punta imantada para atornillado rápido.", 2100m, true, 50, "https://picsum.photos/seed/mk-tools-07/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000008"), "MK-DES-003", "Destornillador Plano 1/4", "Mango antideslizante para uso rudo.", 1950m, true, 46, "https://picsum.photos/seed/mk-tools-08/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000009"), "MK-LLA-001", "Llave Ajustable 10in", "Apertura amplia para tuercas comunes.", 6800m, true, 21, "https://picsum.photos/seed/mk-tools-09/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000000a"), "MK-LLA-002", "Juego Llaves Mixtas 8pz", "Medidas métricas 8 a 19 mm.", 13200m, true, 14, "https://picsum.photos/seed/mk-tools-10/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000000b"), "MK-LLA-003", "Llave Stilson 14in", "Para plomería y ajustes pesados.", 9800m, true, 13, "https://picsum.photos/seed/mk-tools-11/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000000c"), "MK-CIN-001", "Cinta Métrica 5m", "Carcasa resistente con traba firme.", 2400m, true, 65, "https://picsum.photos/seed/mk-tools-12/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000000d"), "MK-NIV-001", "Nivel de Burbuja 24in", "Cuerpo de aluminio para obra liviana.", 7100m, true, 16, "https://picsum.photos/seed/mk-tools-13/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000000e"), "MK-CUT-001", "Cúter Profesional 18mm", "Guía metálica y seguro reforzado.", 1850m, true, 70, "https://picsum.photos/seed/mk-tools-14/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000000f"), "MK-CUT-002", "Repuesto Cúter 10 Hojas", "Hojas segmentadas de alta duración.", 1300m, true, 90, "https://picsum.photos/seed/mk-tools-15/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000010"), "MK-TAL-001", "Taladro Percutor 1/2 710W", "Para concreto, metal y madera.", 38900m, true, 9, "https://picsum.photos/seed/mk-tools-16/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000011"), "MK-TAL-002", "Taladro Inalámbrico 20V", "Incluye batería y cargador.", 47900m, true, 7, "https://picsum.photos/seed/mk-tools-17/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000012"), "MK-BRO-001", "Juego Brocas Concreto 5pz", "Brocas SDS para perforación de obra.", 6200m, true, 24, "https://picsum.photos/seed/mk-tools-18/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000013"), "MK-BRO-002", "Juego Brocas Metal 13pz", "Acero rápido para taller mecánico.", 7400m, true, 19, "https://picsum.photos/seed/mk-tools-19/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000014"), "MK-SIE-001", "Sierra Circular 7 1/4", "Motor potente para cortes rectos.", 52500m, true, 6, "https://picsum.photos/seed/mk-tools-20/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000015"), "MK-SIE-002", "Sierra Caladora 650W", "Corte curvo y detalle en madera.", 29800m, true, 8, "https://picsum.photos/seed/mk-tools-21/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000016"), "MK-ESM-001", "Esmeril Angular 4 1/2", "Uso profesional para corte y desbaste.", 31500m, true, 11, "https://picsum.photos/seed/mk-tools-22/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000017"), "MK-DIS-001", "Disco Corte Metal 4 1/2", "Disco delgado de alto rendimiento.", 1200m, true, 140, "https://picsum.photos/seed/mk-tools-23/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000018"), "MK-DIS-002", "Disco Desbaste 4 1/2", "Abrasivo resistente para metal.", 1650m, true, 120, "https://picsum.photos/seed/mk-tools-24/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000019"), "MK-LIJ-001", "Lijadora Orbital 1/4", "Acabado fino para carpintería.", 25600m, true, 10, "https://picsum.photos/seed/mk-tools-25/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000001a"), "MK-CEP-001", "Cepillo Eléctrico 3 1/4", "Desbaste preciso en madera.", 34200m, true, 5, "https://picsum.photos/seed/mk-tools-26/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000001b"), "MK-PIST-001", "Pistola Silicón Profesional", "Aplicación uniforme en acabados.", 3900m, true, 32, "https://picsum.photos/seed/mk-tools-27/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000001c"), "MK-SIL-001", "Silicón Transparente 280ml", "Sellador multiuso para interiores.", 2350m, true, 68, "https://picsum.photos/seed/mk-tools-28/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000001d"), "MK-PIA-001", "Pistola de Pintura HVLP", "Acabado uniforme para metal y madera.", 18400m, true, 9, "https://picsum.photos/seed/mk-tools-29/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000001e"), "MK-ROD-001", "Rodillo Felpa 9in", "Cobertura rápida en paredes.", 2800m, true, 55, "https://picsum.photos/seed/mk-tools-30/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000001f"), "MK-BRO-003", "Brocha 3in Profesional", "Cerdas mixtas de buena retención.", 2200m, true, 64, "https://picsum.photos/seed/mk-tools-31/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000020"), "MK-ESC-001", "Escalera Aluminio 6 Peldaños", "Ligera y estable para mantenimiento.", 32900m, true, 7, "https://picsum.photos/seed/mk-tools-32/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000021"), "MK-CAR-001", "Carretillo Obra Pesada", "Bandeja reforzada para construcción.", 46500m, true, 4, "https://picsum.photos/seed/mk-tools-33/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000022"), "MK-PAL-001", "Pala Cuadrada Mango Fibra", "Pala de alto desempeño para mezcla.", 11900m, true, 15, "https://picsum.photos/seed/mk-tools-34/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000023"), "MK-PIC-001", "Pico Azadón 5lb", "Cabeza templada para terreno duro.", 13200m, true, 12, "https://picsum.photos/seed/mk-tools-35/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000024"), "MK-BAR-001", "Barra Cuadrante 1.5m", "Para palanca en obra y demolición.", 14600m, true, 10, "https://picsum.photos/seed/mk-tools-36/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000025"), "MK-LLA-004", "Juego Llaves Allen 9pz", "Llaves métricas de acero endurecido.", 3600m, true, 43, "https://picsum.photos/seed/mk-tools-37/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000026"), "MK-SOC-001", "Juego Dados 1/2 24pz", "Incluye ratchet y extensiones.", 23900m, true, 12, "https://picsum.photos/seed/mk-tools-38/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000027"), "MK-RAT-001", "Ratchet 1/2 Reversible", "Mecanismo de 72 dientes.", 9200m, true, 18, "https://picsum.photos/seed/mk-tools-39/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000028"), "MK-TOR-001", "Torquímetro 1/2 210Nm", "Ajuste preciso para mecánica.", 28400m, true, 6, "https://picsum.photos/seed/mk-tools-40/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000029"), "MK-CAB-001", "Cable Extensión 20m", "Calibre 12 para herramientas eléctricas.", 18900m, true, 14, "https://picsum.photos/seed/mk-tools-41/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000002a"), "MK-MUL-001", "Multímetro Digital", "Medición de voltaje, resistencia y continuidad.", 14500m, true, 17, "https://picsum.photos/seed/mk-tools-42/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000002b"), "MK-PRO-001", "Probador Voltaje Tipo Lápiz", "Verificación rápida en instalaciones.", 3500m, true, 40, "https://picsum.photos/seed/mk-tools-43/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000002c"), "MK-SOL-001", "Soldadora Inverter 200A", "Compacta para trabajo de herrería.", 68900m, true, 4, "https://picsum.photos/seed/mk-tools-44/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000002d"), "MK-ELE-001", "Electrodo 6013 1/8 Caja", "Uso general para soldadura estructural.", 12400m, true, 20, "https://picsum.photos/seed/mk-tools-45/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000002e"), "MK-CAS-001", "Casco Seguridad Ajustable", "Protección industrial clase E.", 5200m, true, 38, "https://picsum.photos/seed/mk-tools-46/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-00000000002f"), "MK-GAF-001", "Gafas Seguridad Transparentes", "Lentes antiimpacto con protección UV.", 2900m, true, 75, "https://picsum.photos/seed/mk-tools-47/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000030"), "MK-GUA-001", "Guantes Cuero Reforzados", "Protección para construcción y taller.", 4100m, true, 60, "https://picsum.photos/seed/mk-tools-48/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000031"), "MK-BOT-001", "Botas Seguridad Punta Acero", "Suela antideslizante, uso industrial.", 28900m, true, 16, "https://picsum.photos/seed/mk-tools-49/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000032"), "MK-CAJ-001", "Caja Herramientas 19in", "Organizador portátil con bandeja.", 8400m, true, 23, "https://picsum.photos/seed/mk-tools-50/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000033"), "MK-ORG-001", "Organizador Tornillería 15in", "Compartimentos removibles para ferretería.", 7900m, true, 20, "https://picsum.photos/seed/mk-tools-51/384/288"),
        new CatalogProduct(Guid.Parse("10000000-0000-0000-0000-000000000034"), "MK-TOR-002", "Tornillos Gypsum 1in 100pz", "Rosca fina para perfilería liviana.", 1750m, true, 130, "https://picsum.photos/seed/mk-tools-52/384/288")
    ];

    private static readonly IReadOnlyList<CatalogProduct> ProductsOrdered =
        ProductsSeed.OrderBy(x => x.Name).ToList();

    private static readonly Dictionary<Guid, CatalogProduct> ProductsById =
        ProductsSeed.ToDictionary(x => x.Id);

    public IReadOnlyList<CatalogProduct> GetProducts()
    {
        return ProductsOrdered;
    }

    public CatalogProduct? GetById(Guid id)
    {
        return ProductsById.TryGetValue(id, out var p) ? p : null;
    }
}

// Yurguen: ImageUrl opcional URL pública del proveedor/CDN (no blob en BD típico).
public sealed record CatalogProduct(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    bool IsPublished,
    int SimulatedStock,
    string? ImageUrl);
