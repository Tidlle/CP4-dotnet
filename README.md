# GameCatalog API — CP4 Advanced Business Development with .NET (2026)

API REST em **.NET 8 (ASP.NET Core com Controllers)** para gestão de um **catálogo de jogos independentes e seus estúdios desenvolvedores**.

O projeto aplica os requisitos do checkpoint: arquitetura em camadas, Repository Pattern, DTOs com mapeamento, paginação com índices de banco, compressão de dados, rate limiting, Swagger Annotations, testes automatizados (unidade e funcionais), health checks, logging estruturado e telemetria com Application Insights.

## Integrantes do grupo

| Nome completo | RM |
|---------------|----|
| _preencher_   | _preencher_ |
| _preencher_   | _preencher_ |

---

## 1. Descrição do projeto

| Entidade | Tabela | Descrição |
|----------|--------|-----------|
| **EstudioEntity** | `tb_estudio` | Estúdio desenvolvedor (nome, país, ano de fundação, ativo) |
| **JogoEntity** | `tb_jogo` | Jogo publicado (título, gênero, preço, lançamento, nota) |
| **PlataformaEntity** | `tb_plataforma` | Plataforma em que o jogo está disponível |

Relacionamentos:

- **1:N** — um estúdio possui vários jogos (`tb_jogo.i_estudio_id`)
- **N:N** — um jogo está em várias plataformas e uma plataforma tem vários jogos (`tb_jogo_plataforma`)

Regras de negócio implementadas na camada de Application (UseCases):

- O ano de fundação do estúdio precisa estar entre **1958** e o ano atual
- Um estúdio **com jogos vinculados não pode ser excluído**
- Um jogo só pode ser cadastrado para um **estúdio existente**
- O desconto aplicado em um jogo precisa ser **maior que 0% e no máximo 90%**
- `PageSize` é limitado a **100** registros por página

---

## 2. Arquitetura e componentes

```
GameCatalog.sln
├── GameCatalog.API
│   ├── Application
│   │   ├── Dtos          → PostEstudioDto, PostJogoDto, PostPlataformaDto, PostDescontoDto, PaginacaoDto<T>
│   │   ├── Interfaces    → IEstudioUseCase, IJogoUseCase
│   │   ├── Mappers       → EstudioMapper, JogoMapper (extension methods)
│   │   └── UseCases      → EstudioUseCase, JogoUseCase (regras de negócio)
│   ├── Doc
│   │   └── Samples       → exemplos de request/response exibidos no Swagger
│   ├── Domain
│   │   ├── Entities      → EstudioEntity, JogoEntity, PlataformaEntity (+ índices)
│   │   ├── Enums         → GeneroJogo
│   │   └── Interfaces    → IEstudioRepository, IJogoRepository
│   ├── Infrastructure
│   │   ├── Data          → ApplicationContext, Migrations, Repositories
│   │   └── IoC           → Bootstrap (injeção de dependência, telemetria e log)
│   ├── Presentation
│   │   └── Controllers   → EstudioController, JogoController, HealthController
│   └── Program.cs        → pipeline HTTP, Swagger, compressão, rate limit e health checks
└── GameCatalog.Tests
    └── App               → testes de Repository, UseCase e Controller
```

**Fluxo de uma requisição:**

```
Controller (Presentation) → UseCase (Application) → Repository (Domain/Interfaces → Infrastructure) → EF Core → Oracle
```

- **Presentation** não conhece EF Core: depende apenas de `IEstudioUseCase` / `IJogoUseCase`
- **Application** não conhece o banco: depende apenas de `IEstudioRepository` / `IJogoRepository`, declarados no **Domain** e implementados na **Infrastructure**
- A troca acontece no `Bootstrap.AddIoC` (inversão de dependência)

### Stack

| Recurso | Implementação |
|---------|---------------|
| Framework | .NET 8 / ASP.NET Core 8 (Controllers) |
| ORM | Entity Framework Core 8 + `Oracle.EntityFrameworkCore` |
| Banco de dados | Oracle (FIAP) |
| Documentação | Swashbuckle + **Annotations** + **Filters** (exemplos de payload) |
| Logging | Serilog (console + arquivo com rotação diária) |
| Observabilidade | Azure Monitor / Application Insights (OpenTelemetry) + Health Checks |
| Testes | xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory |

---

## 3. Requisitos do checkpoint e onde estão implementados

### 3.1 Organização em camadas
`Domain`, `Application`, `Infrastructure` e `Presentation` como pastas do projeto, com as dependências sempre apontando para dentro. O registro de tudo fica centralizado em [Bootstrap.cs](GameCatalog.API/Infrastructure/IoC/Bootstrap.cs).

### 3.2 Repository Pattern, DTOs e mapeamentos
- Interfaces no Domain ([IEstudioRepository](GameCatalog.API/Domain/Interfaces/IEstudioRepository.cs), [IJogoRepository](GameCatalog.API/Domain/Interfaces/IJogoRepository.cs)) e implementações na Infrastructure.
- A API **nunca recebe entidades**: a entrada é sempre um DTO (`PostEstudioDto`, `PostJogoDto`, `PostDescontoDto`).
- O mapeamento DTO → entidade fica em *extension methods* ([EstudioMapper](GameCatalog.API/Application/Mappers/EstudioMapper.cs), [JogoMapper](GameCatalog.API/Application/Mappers/JogoMapper.cs)), chamados pelos UseCases.

### 3.3 Paginação e índices
- Todos os endpoints de listagem recebem **`PageNumber`** e **`PageSize`**, normalizados no UseCase (mínimo 1, padrão 10, máximo 100).
- A paginação é feita **no banco** (`Skip`/`Take` → `OFFSET/FETCH`), nunca em memória.
- A resposta é um `PaginacaoDto<T>` com `TotalRegistros`, `TotalPaginas`, `TemPaginaAnterior` e `TemProximaPagina`.
- Índices declarados **nas entidades** com o atributo `[Index]` e criados pela migration `InitDb`:

| Índice | Tabela | Consulta que ele acelera |
|--------|--------|--------------------------|
| `IDX_ESTUDIO_NOME` (único) | `tb_estudio` | Busca/ordenação por nome e unicidade |
| `IDX_ESTUDIO_PAIS_ANO` | `tb_estudio` | Filtro por país e ano de fundação |
| `IDX_JOGO_TITULO` | `tb_jogo` | Ordenação padrão da listagem |
| `IDX_JOGO_ESTUDIO_DATA` | `tb_jogo` | Jogos de um estúdio por lançamento |
| `IDX_JOGO_GENERO_PRECO` | `tb_jogo` | Filtro por gênero com faixa de preço |

### 3.4 Compressão e Rate Limiting
- **Response Compression** com **Brotli** e **Gzip** (`CompressionLevel.SmallestSize`), incluindo `application/json`.
- **Rate Limiting** com janela fixa **particionada por cliente (IP de origem)**: **5 requisições a cada 20 segundos**, aplicado nos controllers com `[EnableRateLimiting("politica_rate_limit")]`.
- Ao exceder o limite a API responde **429 Too Many Requests** com o cabeçalho `Retry-After`.
- Os health checks ficam fora do rate limit para não bloquear o monitoramento.

### 3.5 Documentação Swagger avançada
`[SwaggerOperation]` (com descrição em markdown), `[SwaggerResponse]`, `[SwaggerParameter]`, `[SwaggerRequestExample]` e `[SwaggerResponseExample]`. Os exemplos de payload ficam em [Doc/Samples](GameCatalog.API/Doc/Samples) implementando `IExamplesProvider<T>`.

### 3.6 Testes automatizados — 55 testes
| Suíte | Tipo | O que valida |
|-------|------|--------------|
| `EstudioRepositoryTest`, `JogoRepositoryTest` | Unidade (EF InMemory) | Paginação, filtros, busca, CRUD e o relacionamento N:N |
| `EstudioUseCaseTest`, `JogoUseCaseTest` | Unidade (Moq) | Regras de negócio, mapeamentos e normalização da paginação |
| `EstudioControllerTest`, `JogoControllerTest` | Funcional (`WebApplicationFactory`) | Ciclo HTTP completo: 200/204/400/404, **429 do rate limit** e **compressão Brotli** |
| `HealthControllerTest` | Funcional | `/api/health/live`, `/health/live` e o documento OpenAPI |

### 3.7 Observabilidade
- **Logging estruturado** com Serilog em console e em `logs/api-<data>.log` (rotação diária, 7 arquivos).
- **Health Checks**: `self` (liveness) e `oracle` (readiness), expostos em `/health/live`, `/health/db` e também pelo `HealthController` (`/api/health/live`, `/api/health/db`), que devolve **503** quando a dependência está fora.
- **Tracing e métricas** enviados ao **Application Insights** via Azure Monitor OpenTelemetry (ativado quando existe connection string configurada).

---

## 4. Como rodar o projeto

### Pré-requisitos
- **.NET SDK 8.0** — https://dotnet.microsoft.com/download/dotnet/8.0
- Acesso ao **Oracle da FIAP** (usuário `RMxxxxxx` e senha)

### 4.1 Configurar a conexão com o banco

Em [appsettings.Development.json](GameCatalog.API/appsettings.Development.json), preencha o usuário e a senha:

```json
"ConnectionStrings": {
  "Oracle": "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))) (CONNECT_DATA=(SERVER=DEDICATED)(SID=ORCL)));User Id=RM123456;Password=SUA_SENHA;"
}
```

### 4.2 Criar as tabelas e os índices

```bash
dotnet tool install --global dotnet-ef --version 8.0.26
```

```bash
dotnet ef database update --project GameCatalog.API
```

### 4.3 Executar a API

```bash
dotnet run --project GameCatalog.API
```

A aplicação sobe em `http://localhost:5121` e abre o **Swagger** em `/swagger`.

> Para popular a base rapidamente durante a demonstração, use o arquivo [GameCatalog.API.http](GameCatalog.API/GameCatalog.API.http) (Visual Studio / VS Code REST Client): ele já contém os POSTs de estúdios, jogos, plataformas e os cenários de erro.

### 4.4 Executar os testes

```bash
dotnet test
```

Resultado esperado: **55 testes aprovados**. Os testes **não dependem do Oracle** — os repositórios são testados com EF Core InMemory e os controllers com use cases mockados.

> **Nota sobre runtimes mais novos:** o projeto é `net8.0`. Em máquinas que possuem apenas runtime .NET 9/10 instalado, o `TestHost` 8.x expõe um `PipeWriter` sem `UnflushedBytes` (membro exigido pelo `System.Text.Json` a partir do .NET 9) e as respostas JSON dos testes funcionais falhariam. Por isso o `CustomWebApplicationFactory` registra o `CompatibilidadeTestServerFilter`, que troca a feature de corpo da resposta por uma implementação baseada em `Stream`. É código exclusivo dos testes — a API em execução não é afetada.

---

## 5. Configurações

### Application Insights

Informe a connection string do recurso do Azure em `appsettings.json` (ou na variável de ambiente `APPLICATIONINSIGHTS_CONNECTION_STRING`):

```json
"ApplicationInsights": {
  "ConnectionString": "InstrumentationKey=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx;IngestionEndpoint=https://brazilsouth-1.in.applicationinsights.azure.com/"
}
```

Com a chave preenchida, requests, dependências (inclusive as consultas ao Oracle) e traces passam a ser exportados. **Sem a chave a API funciona normalmente** — a telemetria simplesmente não é registrada.

### Rate limiting

Configurado em [Program.cs](GameCatalog.API/Program.cs):

```csharp
PermitLimit = 5,
Window = TimeSpan.FromSeconds(20)
```

---

## 6. Endpoints disponíveis

### Estúdios — `/api/estudio`

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/estudio` | Lista paginada (`PageNumber`, `PageSize`, `Busca`) | 200, 204, 400, 429 |
| GET | `/api/estudio/{id}` | Estúdio com os jogos | 200, 404 |
| POST | `/api/estudio` | Cadastra estúdio | 201, 400 |
| PUT | `/api/estudio/{id}` | Atualiza estúdio | 200, 400, 404 |
| DELETE | `/api/estudio/{id}` | Remove estúdio sem jogos | 200, 400, 404 |

### Jogos — `/api/jogo`

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/jogo` | Lista paginada (`PageNumber`, `PageSize`, `Genero`, `EstudioId`) | 200, 204, 400, 429 |
| GET | `/api/jogo/{id}` | Consulta jogo | 200, 404 |
| POST | `/api/jogo` | Cadastra jogo | 201, 404 |
| PUT | `/api/jogo/{id}` | Atualiza jogo | 200, 404 |
| PATCH | `/api/jogo/{id}/desconto` | Aplica desconto (regra de negócio) | 200, 400, 404 |
| DELETE | `/api/jogo/{id}` | Remove jogo | 200, 404 |
| POST | `/api/jogo/{jogoId}/plataforma` | Vincula plataforma (N:N) | 201, 404 |

Gêneros aceitos: `Acao`, `Aventura`, `Rpg`, `Estrategia`, `Simulacao`, `Puzzle`, `Plataforma`, `Terror`.

### Monitoramento

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health/live` / `/api/health/live` | Liveness (a API está de pé) |
| GET | `/health/db` / `/api/health/db` | Readiness (o Oracle responde) |
| GET | `/swagger` | Documentação interativa |

### Exemplos de requisição

**Cadastrar estúdio**

```bash
curl -X POST http://localhost:5121/api/estudio -H "Content-Type: application/json" -d "{\"nome\":\"Pixel Forge Studios\",\"pais\":\"Brasil\",\"anoFundacao\":2011,\"ativo\":true}"
```

**Listagem paginada de jogos**

```bash
curl "http://localhost:5121/api/jogo?PageNumber=1&PageSize=3&Genero=Rpg"
```

```json
{
  "pageNumber": 1,
  "pageSize": 3,
  "totalRegistros": 47,
  "totalPaginas": 16,
  "temPaginaAnterior": false,
  "temProximaPagina": true,
  "dados": [
    {
      "id": 2,
      "titulo": "Chronicles of Aether",
      "genero": "Rpg",
      "preco": 199.9,
      "dataLancamento": "2020-02-15T00:00:00",
      "nota": 9.1,
      "estudioId": 1,
      "plataformas": []
    }
  ]
}
```

**Aplicar desconto (regra de negócio)**

```bash
curl -X PATCH http://localhost:5121/api/jogo/1/desconto -H "Content-Type: application/json" -d "{\"percentual\":25}"
```

Com `"percentual": 95` a API responde **400 Bad Request**:

```
O percentual de desconto nao pode ultrapassar 90%.
```

**Rate limiting** — a partir da 6ª requisição em 20 segundos:

```
HTTP/1.1 429 Too Many Requests
Retry-After: 20

Limite de 5 requisicoes a cada 20 segundos excedido. Tente novamente em instantes.
```

**Health check**

```bash
curl http://localhost:5121/api/health/live
```

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "self", "status": "Healthy", "description": "API em execucao", "error": null }
  ]
}
```

---

## 7. Roteiro sugerido para a apresentação (5 min)

1. **Arquitetura** — mostrar as pastas `Domain / Application / Infrastructure / Presentation` e o `Bootstrap.AddIoC` ligando interface e implementação.
2. **Swagger** — `/swagger`, com as descrições e os exemplos vindos das annotations.
3. **Paginação** — `GET /api/jogo?PageNumber=2&PageSize=5` mostrando `totalRegistros` e `totalPaginas`; abrir a migration `InitDb` e apontar os `CreateIndex`.
4. **Regra de negócio** — desconto de 25% (200) e de 95% (400).
5. **Rate limiting** — repetir a listagem 6 vezes até o **429**.
6. **Observabilidade** — `/api/health/live`, `/api/health/db`, os logs no console e o painel do Application Insights.
7. **Testes** — `dotnet test` com os 55 testes aprovados.
