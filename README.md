# Bíblia Reader API

API ASP.NET Core 8 em **projeto único** (mesmo estilo do `MinhaRotina.Api.Vps`): `Controllers`, `Models`, `Data`, `Services`, `ViewModels`, `Validators`.

## Requisitos

- .NET 8 SDK
- SQL Server (connection string em `appsettings.json`)

## Configuração

Igual ao **`MinhaRotina.Api.Vps`**: só connection string nomeada **`Default`** e JWT só com **`Key`** (sem `Issuer` / `Audience` no código — o `Program.cs` do Minha Rotina usa `ValidateIssuer = false` e `ValidateAudience = false`).

1. **`ConnectionStrings:Default`** — SQL Server (no `appsettings.json` ou variável de ambiente).
2. **`Jwt:Key`** — chave secreta do token.
3. **`SendGrid:ApiKey`** — opcional em desenvolvimento; se vazio, e-mails não são enviados (serviço nulo com log).
4. **`Support:NotifyEmail`** — e-mail para tickets de suporte (opcional).
5. **`AppSettings:BaseUrl`** — URL pública da API (links de e-mail).

### VPS (systemd / Linux) — mesmo padrão do Minha Rotina

No ASP.NET Core, `:` vira `__` nas variáveis de ambiente. Use **`Default`**, não `DefaultConnection`:

```ini
[Service]
Environment="ConnectionStrings__Default=Server=localhost,1433;Database=BibliaReaderDb;User Id=...;Password=...;Trusted_Connection=False;MultipleActiveResultSets=True;TrustServerCertificate=True"
Environment="Jwt__Key=sua-chave-longa-secreta"
```

Se antes estava `ConnectionStrings__DefaultConnection`, troque para **`ConnectionStrings__Default`** para bater com `GetConnectionString("Default")` — como no `appsettings.json` do Minha Rotina (`"ConnectionStrings": { "Default": "..." }`).

Pode remover `Jwt__Issuer` e `Jwt__Audience` da unidade: o Bíblia Reader segue o mesmo JWT do Minha Rotina (só validação da assinatura com a chave).

## Banco de dados

**Antes do primeiro `dotnet run`**, crie a migração inicial (a pasta `Data/Migrations` não vem no repositório até você gerar):

```bash
cd C:\dev\BibleReader.Api.Vps
dotnet tool install -g dotnet-ef
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
dotnet ef database update
```

Opcional: defina `BIBLIA_EF_CONNECTION` ao gerar migrações, ou ajuste a connection string em `Data/AppDbContextFactory.cs`.

Na subida da API, o `Program.cs` aplica migrações (`MigrateAsync`) e executa o **seed** da Bíblia (66 livros, 1189 capítulos, versículos de exemplo em Gênesis 1).

## Regras principais — planos de leitura

| Plano            | Capítulos por dia |
|------------------|-------------------|
| Bíblia em 1 ano  | 4                 |
| Bíblia em 6 meses| 7                 |
| Bíblia em 90 dias | 13               |

- O backend gera **todos os dias** e **capítulos por dia** ao escolher o plano (`POST /v1/reading-plans/select`).
- Um utilizador tem **no máximo um plano ativo**; ao escolher outro, o anterior fica **Superseded**.
- **Dia concluído** quando todos os capítulos daquele dia estão lidos.
- **Progresso** e **calendário** vêm sempre do banco.

## Endpoints (prefixo `v1`)

- **Auth:** `POST /v1/auth/register`, `login`, `verify-email`, `forgot-password`, `reset-password`
- **Bíblia:** `GET /v1/bible/versions`, `books`, `chapters`, `books/{bookId}/chapters/{chapter}`, etc.
- **Plano:** `GET /v1/reading-plans/current`, `POST .../select`, `.../calendar`, `.../today`, `.../days/{yyyy-MM-dd}`, `PATCH .../chapters/{id}/read|unread`, `.../progress`
- **Comunidade:** `GET /v1/community/feed`, `POST .../posts`, `GET .../posts/{id}`, like/unlike, comentários, salvar post
- **Suporte:** `POST /v1/support/messages`

Swagger: `/swagger` (em desenvolvimento).

## Pipeline

1. `UseRouting()` → `UseAuthentication()` → `UseAuthorization()` → `MapControllers()`  
   Evita rotas com `[Authorize]` a responderem 404 por ordem incorreta de middleware.

## Texto bíblico completo

O seed cria estrutura e texto de exemplo; importe o texto integral da sua tradução conforme necessário (tabela `BibleVerses`).

## GitHub Actions

- **`ci.yml`** — `dotnet build` + `publish` em **pull requests** para `main`/`master` (e manual). Não corre em push, para não duplicar o deploy.
- **`deploy.yml`** — publica na VPS em **push** para `main` ou `master` (e manual).

### Secrets no repositório (Settings → Secrets and variables → Actions)

| Secret | Descrição |
|--------|-----------|
| `VPS_SSH_HOST` | IP ou hostname da VPS |
| `VPS_SSH_USER` | Utilizador SSH (ex.: `root` ou deploy) |
| `VPS_SSH_KEY` | Chave privada SSH (conteúdo completo, incluindo `BEGIN`/`END`) |

Ajuste no topo de `deploy.yml` se precisar: `SERVICE_NAME`, `REMOTE_DIR` (ex.: `/var/www/biblereader-api`), `API_PORT` (predefinido `5001`).

Na VPS, configure **connection string**, **JWT** e restantes variáveis no ficheiro do **systemd** (como no Minha Rotina) ou em `appsettings.Production.json` fora do Git.
