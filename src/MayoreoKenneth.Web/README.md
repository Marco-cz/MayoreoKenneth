# MayoreoKenneth.Web

Frontend web responsivo que consume la API de `MayoreoKenneth`.

## Modo MVP local (sin BD / sin pasarela)

1. Corre el API:
   - `dotnet run --project ../MayoreoKenneth.Api`
2. Abre esta carpeta con Live Server (o servidor estatico) y levanta `index.html`.
3. El frontend consume `https://localhost:7253/api/products`.
4. El checkout usa `POST /api/checkout/simulate` (temporal MVP).

## Notas

- Deploy recomendado: Static Web App, Vercel o Netlify.
- Este proyecto se publica por separado del backend en Azure.
- Puntos claramente temporales para produccion:
  - catalogo en memoria (`Catalog:UseInMemory`)
  - checkout simulado (`MvpMode:EnableSimulatedCheckout`)
