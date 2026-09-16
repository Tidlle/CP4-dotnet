# Documentação técnica — GameCatalog API

Documento explicativo completo do projeto: o que cada arquivo faz, como as camadas
conversam entre si, quais dependências são necessárias e como instalá-las.

> Para o resumo do checkpoint (requisitos atendidos, endpoints e roteiro de
> apresentação), veja o [README.md](README.md). Aqui o foco é **explicar o código**.

---

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Dependências e como instalá-las](#2-dependências-e-como-instalá-las)
3. [Estrutura de pastas](#3-estrutura-de-pastas)
4. [Camada Domain](#4-camada-domain)
5. [Camada Application](#5-camada-application)
6. [Camada Infrastructure](#6-camada-infrastructure)
7. [Camada Presentation](#7-camada-presentation)
8. [Program.cs — o pipeline HTTP](#8-programcs--o-pipeline-http)
9. [Documentação Swagger (Doc/Samples)](#9-documentação-swagger-docsamples)
10. [Arquivos de configuração](#10-arquivos-de-configuração)
11. [Projeto de testes](#11-projeto-de-testes)
12. [Passo a passo para rodar](#12-passo-a-passo-para-rodar)
13. [Solução de problemas](#13-solução-de-problemas)

---

## 1. Visão geral

**GameCatalog API** é uma API REST em **.NET 8 / ASP.NET Core** que gerencia um
catálogo de jogos independentes e seus estúdios desenvolvedores. Os dados ficam em
um banco **Oracle**, acessado via **Entity Framework Core**.

A solução (`GameCatalog.sln`) tem dois projetos:

| Projeto | Tipo | Papel |
|---------|------|-------|
| `GameCatalog.API` | Web API (`Microsoft.NET.Sdk.Web`) | A aplicação em si |
| `GameCatalog.Tests` | Biblioteca de testes (`Microsoft.NET.Sdk`) | 55 testes automatizados |

### Arquitetura em camadas

O projeto segue uma **arquitetura em camadas com inversão de dependência**
(inspirada em Clean Architecture). As dependências sempre apontam "para dentro":

```
┌─────────────────────────────────────────────────────┐
│ Presentation  (Controllers)                         │
│   ↓ depende de IEstudioUseCase / IJogoUseCase       │
├─────────────────────────────────────────────────────┤
│ Application   (UseCases, DTOs, Mappers)             │
│   ↓ depende de IEstudioRepository / IJogoRepository │
├─────────────────────────────────────────────────────┤
│ Domain        (Entities, Enums, Interfaces)         │
│   ← núcleo: não depende de ninguém                  │
├─────────────────────────────────────────────────────┤
│ Infrastructure (EF Core, Repositories, IoC)         │
│   → implementa as interfaces do Domain              │
└─────────────────────────────────────────────────────┘
```

O ponto-chave: o **Domain declara as interfaces** dos repositórios, e a
**Infrastructure as implementa**. Assim a camada de negócio nunca sabe que existe
EF Core ou Oracle — ela só conhece contratos. A "amarração" entre interface e
implementação acontece em um único lugar: `Bootstrap.AddIoC`.

### Fluxo de uma requisição

```
HTTP  →  Controller  →  UseCase  →  Repository  →  EF Core  →  Oracle
        (Presentation) (Application) (Infrastructure)
        valida HTTP    regra de      SQL/paginação
        e status code  negócio
```

Exemplo concreto — `PATCH /api/jogo/1/desconto`:

1. `JogoController.PatchDesconto` recebe o JSON e o transforma em `PostDescontoDto`.
2. Chama `IJogoUseCase.AplicarDesconto(1, 25)`.
3. `JogoUseCase` valida a regra (0 < percentual ≤ 90), busca o jogo, calcula o novo preço.
4. `JogoRepository.AtualizarPreco` grava no banco via EF Core.
5. O controller devolve `200 OK` — ou `400` se o use case lançou `ArgumentException`.

---

## 2. Dependências e como instalá-las

### 2.1 Pré-requisitos de máquina

| Ferramenta | Versão | Obrigatório? | Link |
|------------|--------|--------------|------|
| **.NET SDK** | 8.0 | ✅ Sim | https://dotnet.microsoft.com/download/dotnet/8.0 |
| **dotnet-ef** | 8.0.26 | ⚠️ Só para criar/alterar o banco | Instalado via `dotnet tool` (abaixo) |
| Visual Studio 2022 / VS Code | 17.8+ | ❌ Opcional | https://visualstudio.microsoft.com/ |
| Acesso ao Oracle da FIAP | — | ⚠️ Só para rodar a API (os testes não precisam) | — |

**Verificar se o SDK está instalado:**

```bash
dotnet --version
```

Se aparecer `8.x.x` (ou superior — o projeto usa `RollForward=LatestMajor`, então
runtimes 9/10 também funcionam), está pronto.

**Instalar a ferramenta de migrations do EF Core:**

```bash
dotnet tool install --global dotnet-ef --version 8.0.26
```

Se já estiver instalada em uma versão antiga:

```bash
dotnet tool update --global dotnet-ef --version 8.0.26
```

### 2.2 Pacotes NuGet — você **não** precisa baixá-los manualmente

Todas as bibliotecas estão declaradas nos arquivos `.csproj`. O comando abaixo
baixa tudo de uma vez:

```bash
dotnet restore
```

Na prática, `dotnet build`, `dotnet run` e `dotnet test` já executam o restore
automaticamente — então normalmente basta rodar o projeto.

### 2.3 Pacotes do projeto `GameCatalog.API`

| Pacote | Versão | Para que serve |
|--------|--------|----------------|
| `Microsoft.EntityFrameworkCore` | 8.0.26 | ORM: mapeia classes C# em tabelas do banco |
| `Microsoft.EntityFrameworkCore.Relational` | 8.0.26 | Base comum dos provedores relacionais (SQL, migrations) |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.26 | Suporte aos comandos `dotnet ef` / Package Manager Console |
| `Oracle.EntityFrameworkCore` | 8.23.26200 | Provedor Oracle: traduz LINQ em SQL do Oracle (`UseOracle`) |
| `AspNetCore.HealthChecks.Oracle` | 8.0.1 | Health check que abre uma conexão de teste no Oracle (`.AddOracle`) |
| `Serilog.AspNetCore` | 8.0.3 | Logging estruturado em console e arquivo com rotação diária |
| `Azure.Monitor.OpenTelemetry.AspNetCore` | 1.6.0 | Exporta traces e métricas para o Application Insights |
| `Microsoft.ApplicationInsights.AspNetCore` | 3.1.2 | Telemetria de requisições no Application Insights |
| `Swashbuckle.AspNetCore` | 6.6.2 | Gera o documento OpenAPI e a UI do Swagger |
| `Swashbuckle.AspNetCore.Annotations` | 6.6.2 | Habilita `[SwaggerOperation]`, `[SwaggerResponse]`, `[SwaggerParameter]` |
| `Swashbuckle.AspNetCore.Filters` | 6.1.0 | Habilita exemplos de payload (`IExamplesProvider<T>`) |

> `Microsoft.AspNetCore.*` (controllers, rate limiting, compressão, health checks)
> **não aparece na lista** porque já faz parte do framework compartilhado do ASP.NET Core 8.

### 2.4 Pacotes do projeto `GameCatalog.Tests`

| Pacote | Versão | Para que serve |
|--------|--------|----------------|
| `xunit` | 2.5.3 | Framework de testes (`[Fact]`, `[Theory]`, `Assert`) |
| `xunit.runner.visualstudio` | 2.5.3 | Permite que o VS / `dotnet test` descubra e execute os testes |
| `Microsoft.NET.Test.Sdk` | 17.8.0 | Infraestrutura de execução de testes do .NET |
| `Moq` | 4.20.72 | Cria dublês (mocks) das interfaces para isolar o código testado |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.26 | Banco em memória — testa os repositórios sem Oracle |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0.30 | Sobe a API inteira em memória (`WebApplicationFactory`) |
| `coverlet.collector` | 6.0.0 | Coleta cobertura de código |

---

## 3. Estrutura de pastas

```
CP4-dotnet/
├── GameCatalog.sln                 → arquivo da solução (agrupa os 2 projetos)
├── README.md                       → visão do checkpoint
├── DOCUMENTACAO.md                 → este arquivo
│
├── GameCatalog.API/
│   ├── GameCatalog.API.csproj      → dependências e configuração de build
│   ├── Program.cs                  → ponto de entrada e pipeline HTTP
│   ├── appsettings.json            → configuração base
│   ├── appsettings.Development.json→ configuração local (connection string)
│   ├── GameCatalog.API.http        → requisições prontas para testar a API
│   ├── Properties/launchSettings.json → portas e perfis de execução
│   │
│   ├── Domain/
│   │   ├── Entities/               → EstudioEntity, JogoEntity, PlataformaEntity
│   │   ├── Enums/                  → GeneroJogo
│   │   └── Interfaces/             → IEstudioRepository, IJogoRepository
│   │
│   ├── Application/
│   │   ├── Dtos/                   → PostEstudioDto, PostJogoDto, PostPlataformaDto,
│   │   │                             PostDescontoDto, PaginacaoDto<T>
│   │   ├── Interfaces/             → IEstudioUseCase, IJogoUseCase
│   │   ├── Mappers/                → EstudioMapper, JogoMapper
│   │   └── UseCases/               → EstudioUseCase, JogoUseCase
│   │
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   ├── ApplicationContext.cs   → DbContext do EF Core
│   │   │   ├── Migrations/             → scripts de criação do banco
│   │   │   └── Repositories/           → EstudioRepository, JogoRepository
│   │   └── IoC/Bootstrap.cs            → injeção de dependência, log e telemetria
│   │
│   ├── Presentation/Controllers/   → EstudioController, JogoController, HealthController
│   └── Doc/Samples/                → exemplos de request/response do Swagger
│
└── GameCatalog.Tests/
    ├── GameCatalog.Tests.csproj
    └── App/                        → os 7 arquivos de teste + a factory
```

---

## 4. Camada Domain

O núcleo do sistema. Contém **só o que é essencial ao negócio**: as entidades, o
enum de gênero e os contratos de persistência. **Não referencia nenhuma outra camada.**

### 4.1 `Domain/Entities/EstudioEntity.cs`

Representa um estúdio desenvolvedor e, ao mesmo tempo, a tabela `tb_estudio`.

```csharp
[Table("tb_estudio")]
[Index(nameof(Nome), IsUnique = true, Name = "IDX_ESTUDIO_NOME")]
[Index(nameof(Pais), nameof(AnoFundacao), Name = "IDX_ESTUDIO_PAIS_ANO")]
public class EstudioEntity { ... }
```

| Elemento | Explicação |
|----------|------------|
| `[Table("tb_estudio")]` | Define o nome da tabela no Oracle |
| `[Index(..., IsUnique = true)]` | Cria o índice único `IDX_ESTUDIO_NOME` — acelera a busca por nome e impede estúdios duplicados |
| `[Index(Pais, AnoFundacao)]` | Índice **composto**, criado para o filtro por país + ano |
| `[Key] int Id` | Chave primária, gerada pelo Oracle (`IDENTITY`) |
| `[Column("s_nome")]` | Nome da coluna no banco. O prefixo indica o tipo: `s_` string, `i_` inteiro, `n_` numérico, `d_` data, `b_` booleano |
| `[StringLength(150, MinimumLength = 3)]` | Vira `NVARCHAR2(150)` no banco **e** validação automática de entrada |
| `ICollection<JogoEntity>? Jogos` | Propriedade de navegação **1:N** — um estúdio tem vários jogos |

> **Por que os índices ficam na entidade?** Porque o EF Core lê esses atributos ao
> gerar a migration e emite os `CreateIndex` correspondentes. O índice fica versionado
> junto do código, não em um script SQL solto.

### 4.2 `Domain/Entities/JogoEntity.cs`

Tabela `tb_jogo`. Tem três índices e dois relacionamentos:

| Índice | Colunas | Consulta que acelera |
|--------|---------|----------------------|
| `IDX_JOGO_TITULO` | `Titulo` | Ordenação padrão da listagem (`OrderBy(x => x.Titulo)`) |
| `IDX_JOGO_ESTUDIO_DATA` | `EstudioId`, `DataLancamento` | Jogos de um estúdio por lançamento |
| `IDX_JOGO_GENERO_PRECO` | `Genero`, `Preco` | Filtro por gênero com faixa de preço |

Pontos importantes:

- `[ForeignKey(nameof(EstudioEntity))] int EstudioId` — chave estrangeira **N:1** para o estúdio.
- `[JsonIgnore] EstudioEntity? Estudio` — a navegação de volta existe para o EF Core,
  mas é **ignorada na serialização JSON**. Sem isso, `Estudio → Jogos → Estudio → …`
  causaria um ciclo infinito ao gerar a resposta.
- `ICollection<PlataformaEntity>? Plataformas` — o lado "muitos" do relacionamento **N:N**.

### 4.3 `Domain/Entities/PlataformaEntity.cs`

Tabela `tb_plataforma`. Guarda o nome da plataforma (PC, PS5, Switch…) e a coleção
inversa `Jogos`, também marcada com `[JsonIgnore]` para evitar ciclo.

### 4.4 `Domain/Enums/GeneroJogo.cs`

Enum com os 8 gêneros aceitos (`Acao = 1` … `Terror = 8`). É gravado como número
(`NUMBER(10)`) no banco, mas o `JsonStringEnumConverter` registrado no `Program.cs`
faz a API **receber e devolver texto** (`"Rpg"`), o que é muito mais legível na API
e no Swagger.

### 4.5 `Domain/Interfaces/IEstudioRepository.cs` e `IJogoRepository.cs`

Os **contratos de persistência**. Declaram *o que* pode ser feito com os dados,
sem dizer *como*:

```csharp
public interface IEstudioRepository
{
    IEnumerable<EstudioEntity> ObterTodos(int PageNumber, int PageSize, string? Busca);
    int ObterTotal(string? Busca);
    EstudioEntity? ObterUm(int Id);
    EstudioEntity? Adicionar(EstudioEntity entity);
    EstudioEntity? Editar(int Id, EstudioEntity entity);
    EstudioEntity? Deletar(int Id);
    bool Existe(int Id);
}
```

`IJogoRepository` acrescenta dois métodos específicos do domínio de jogos:
`AtualizarPreco` (usado pelo desconto) e `AdicionarPlataforma` (o vínculo N:N).

> **Este é o coração da inversão de dependência.** A interface mora no Domain
> (camada de dentro) e a implementação na Infrastructure (camada de fora). O
> `EstudioUseCase` depende da interface, então ele pode ser testado com um mock
> — sem banco nenhum.

---

## 5. Camada Application

Onde vivem as **regras de negócio** e a tradução entre o mundo HTTP e o domínio.

### 5.1 `Application/Dtos/` — os objetos de entrada e saída

**DTO** = *Data Transfer Object*. A API **nunca recebe uma entidade diretamente**.
Isso evita dois problemas clássicos: *over-posting* (o cliente enviar `Id` ou
`Jogos` e sobrescrever dados que não deveria) e o acoplamento do contrato público
ao modelo do banco.

| DTO | Usado em | Validações (`DataAnnotations`) |
|-----|----------|--------------------------------|
| `PostEstudioDto` | POST/PUT de estúdio | `Nome` 3–150 chars, `Pais` até 60, `AnoFundacao` entre 1958 e 2100 |
| `PostJogoDto` | POST/PUT de jogo | `Titulo` 2–150, `Preco` 0–1000, `Nota` 0–10 |
| `PostPlataformaDto` | POST de plataforma | `Nome` obrigatório, até 100 chars |
| `PostDescontoDto` | PATCH de desconto | `Percentual` entre 0,01 e 90 |
| `PaginacaoDto<T>` | **Saída** de todas as listagens | — |

As `DataAnnotations` são validadas **automaticamente** pelo `[ApiController]`:
se o payload violar alguma, o ASP.NET devolve `400 Bad Request` antes mesmo de
o método do controller ser chamado.

#### `PaginacaoDto<T>` em detalhe

É o envelope de toda listagem. Recebe os dados e três números, e **calcula o resto**:

```csharp
public int  TotalPaginas      => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)PageSize);
public bool TemPaginaAnterior => PageNumber > 1;
public bool TemProximaPagina  => PageNumber < TotalPaginas;
```

São propriedades *computadas* (`=>`), então não ocupam espaço em memória e nunca
ficam dessincronizadas dos valores de origem. O cliente da API recebe tudo o que
precisa para montar uma paginação sem fazer conta.

### 5.2 `Application/Mappers/` — DTO → Entidade

`EstudioMapper` e `JogoMapper` são classes **estáticas com extension methods**:

```csharp
public static EstudioEntity ToEstudioEntity(this PostEstudioDto obj) { ... }
```

O `this` no primeiro parâmetro permite escrever `dto.ToEstudioEntity()` como se o
método pertencesse ao DTO. O mapeamento é manual e explícito (sem AutoMapper):
mais verboso, porém rastreável — dá para ver exatamente qual campo vai para onde,
e o compilador acusa se uma propriedade mudar de nome.

Note que os mappers **também mapeiam as coleções aninhadas**: `EstudioMapper`
chama `ToJogoEntity()` para cada jogo enviado junto, e `JogoMapper` converte a
lista de plataformas. O `?? Enumerable.Empty<…>().ToList()` garante uma lista
vazia em vez de `null` quando o cliente não envia nada.

### 5.3 `Application/Interfaces/` — contratos dos use cases

`IEstudioUseCase` e `IJogoUseCase` são o que os controllers enxergam. Graças a
eles, os testes de controller substituem toda a regra de negócio por um mock e
exercitam apenas o comportamento HTTP.

### 5.4 `Application/UseCases/EstudioUseCase.cs`

Recebe `IEstudioRepository` e `ILogger` por **injeção via construtor**.

**Regras implementadas:**

1. **Normalização da paginação** (`ObterTodosEstudios`):
   ```csharp
   if (PageNumber < 1) PageNumber = 1;
   if (PageSize <= 0) PageSize = 10;
   if (PageSize > PageSizeMaximo) PageSize = PageSizeMaximo;  // 100
   ```
   Protege o banco: sem esse teto, `PageSize=999999` traria a tabela inteira.

2. **Ano de fundação válido** (`ValidarAnoFundacao`): precisa estar entre **1958**
   (primeiro videogame comercial) e o **ano atual**. Se violar, lança
   `ArgumentException` — que o controller converte em `400`.

3. **Estúdio com jogos não pode ser excluído** (`DeletarEstudio`): evita deixar
   jogos órfãos. Registra um `LogWarning` e lança `ArgumentException`.

Repare que `ObterTodosEstudios` faz **duas chamadas** ao repositório: `ObterTodos`
(a página) e `ObterTotal` (a contagem completa). São necessárias porque o total de
registros não pode ser deduzido de uma página.

### 5.5 `Application/UseCases/JogoUseCase.cs`

Depende de **dois** repositórios: `IJogoRepository` e `IEstudioRepository` — este
último só para validar a existência do estúdio.

**Regras implementadas:**

1. **Jogo exige estúdio existente** (`AdicionarJogo` / `EditarJogo`):
   ```csharp
   if (!_estudioRepository.Existe(entity.EstudioId)) return null;
   ```
   Retornar `null` é o sinal que o controller traduz para `404 Not Found`.

2. **Desconto entre 0% (exclusivo) e 90%** (`AplicarDesconto`):
   ```csharp
   if (Percentual <= 0)   throw new ArgumentException("...deve ser maior que zero.");
   if (Percentual > 90)   throw new ArgumentException("...nao pode ultrapassar 90%.");
   var novoPreco = Math.Round(jogo.Preco - (jogo.Preco * Percentual / 100), 2);
   ```
   O `Math.Round(..., 2)` evita preços com dízima (ex.: `133.26666…`).

3. **Normalização da paginação**, idêntica à do estúdio.

**Duas convenções de erro** convivem aqui, e é importante entender a diferença:

| Situação | Sinal | Status HTTP |
|----------|-------|-------------|
| Recurso não encontrado | `return null` | `404 Not Found` |
| Regra de negócio violada | `throw new ArgumentException` | `400 Bad Request` |

---

## 6. Camada Infrastructure

Os detalhes técnicos: banco, EF Core, log, telemetria e o registro das dependências.

### 6.1 `Infrastructure/Data/ApplicationContext.cs`

O `DbContext` — a sessão do EF Core com o banco.

```csharp
public DbSet<EstudioEntity>    Estudio    { get; set; }
public DbSet<JogoEntity>       Jogo       { get; set; }
public DbSet<PlataformaEntity> Plataforma { get; set; }
```

Cada `DbSet<T>` é a porta de entrada para consultar e gravar aquela tabela.

O `OnModelCreating` faz uma única configuração via **Fluent API**: dar um nome
curto à tabela de ligação do N:N.

```csharp
modelBuilder.Entity<JogoEntity>()
    .HasMany(x => x.Plataformas)
    .WithMany(x => x.Jogos)
    .UsingEntity(j => j.ToTable("tb_jogo_plataforma"));
```

Sem isso o EF Core criaria `JogoEntityPlataformaEntity`. Todo o resto do
mapeamento vem dos atributos nas entidades.

### 6.2 `Infrastructure/Data/Repositories/EstudioRepository.cs`

Implementa `IEstudioRepository` usando o `ApplicationContext` injetado.

**`ObterTodos` — paginação no banco, o ponto mais importante:**

```csharp
var consulta = _context.Estudio.Include(x => x.Jogos).AsQueryable();

if (!string.IsNullOrWhiteSpace(Busca))
    consulta = consulta.Where(x => x.Nome.Contains(Busca) || x.Pais.Contains(Busca));

var resultado = consulta
    .OrderBy(x => x.Nome)
    .Skip((PageNumber - 1) * PageSize)
    .Take(PageSize)
    .ToList();
```

Por que isso importa:

- `AsQueryable()` mantém a consulta **como expressão**, ainda não executada.
- Os `Where`/`OrderBy`/`Skip`/`Take` vão sendo **acumulados** na expressão.
- Só o `.ToList()` dispara o SQL. O EF Core traduz `Skip`/`Take` em
  **`OFFSET n ROWS FETCH NEXT m ROWS ONLY`** — ou seja, o Oracle devolve apenas
  os registros da página. **Nada é paginado em memória.**
- `Include(x => x.Jogos)` gera o `JOIN` que traz os jogos junto (*eager loading*).
  Sem ele, `Jogos` viria `null`.

**Os demais métodos:**

| Método | O que faz |
|--------|-----------|
| `ObterTotal` | Repete os mesmos filtros e executa `Count()` — um `SELECT COUNT(*)`, sem trazer linhas |
| `ObterUm` | `FirstOrDefault` com `Include(Jogos)`; devolve `null` se não achar |
| `Adicionar` | `Add` + `SaveChanges`; após salvar, a entidade já volta com o `Id` gerado |
| `Editar` | Busca a entidade **rastreada**, copia campo a campo e salva — assim o `Id` e os jogos existentes são preservados |
| `Deletar` | Busca, `Remove`, `SaveChanges`; devolve o registro removido |
| `Existe` | `Any(x => x.Id == Id)` — `SELECT 1`, mais barato que carregar a entidade |

### 6.3 `Infrastructure/Data/Repositories/JogoRepository.cs`

Mesma estrutura, com dois detalhes próprios:

**`MontarConsulta` — filtros compostos sem duplicação:**

```csharp
private IQueryable<JogoEntity> MontarConsulta(GeneroJogo? Genero, int? EstudioId)
{
    var consulta = _context.Jogo.AsQueryable();
    if (Genero.HasValue)    consulta = consulta.Where(x => x.Genero == Genero.Value);
    if (EstudioId.HasValue) consulta = consulta.Where(x => x.EstudioId == EstudioId.Value);
    return consulta;
}
```

Método privado reaproveitado por `ObterTodos` e `ObterTotal`, garantindo que a
contagem use **exatamente os mesmos filtros** da listagem. Se divergissem,
`TotalPaginas` ficaria errado.

**`AdicionarPlataforma` — o vínculo N:N:**

```csharp
var jogo = _context.Jogo.FirstOrDefault(x => x.Id == jogoId);
if (jogo is null) return null;

entity.Jogos = [jogo];        // associa a plataforma ao jogo
_context.Plataforma.Add(entity);
_context.SaveChanges();       // o EF grava em tb_plataforma E em tb_jogo_plataforma
```

Ao atribuir `entity.Jogos = [jogo]`, o EF Core detecta a relação e insere
automaticamente a linha na tabela de ligação — não é preciso manipulá-la à mão.

**`AtualizarPreco`** existe separado do `Editar` porque o desconto altera **só** o
preço; usar o `Editar` exigiria enviar o objeto completo.

### 6.4 `Infrastructure/Data/Migrations/`

| Arquivo | Conteúdo |
|---------|----------|
| `20260916011040_InitDb.cs` | O script: `Up()` cria as 4 tabelas e os 5 índices; `Down()` desfaz |
| `..._InitDb.Designer.cs` | Snapshot do modelo **no momento** desta migration |
| `ApplicationContextModelSnapshot.cs` | Snapshot do modelo **atual** — o EF compara com as entidades para gerar a próxima migration |

O `Up()` cria, em ordem: `tb_estudio`, `tb_plataforma`, `tb_jogo` (com a FK para
estúdio, `onDelete: Cascade`) e `tb_jogo_plataforma` (chave primária composta
`JogosId + PlataformasId`). Depois vêm os `CreateIndex` dos cinco índices.

> Se você alterar uma entidade, gere uma nova migration em vez de editar esta:
> `dotnet ef migrations add NomeDaMudanca --project GameCatalog.API`

### 6.5 `Infrastructure/IoC/Bootstrap.cs`

Classe estática que centraliza **todo** o registro de serviços. Três métodos:

**`AddTelemetria(services, configuration)`**

```csharp
var connectionString = configuration["ApplicationInsights:ConnectionString"];
if (string.IsNullOrWhiteSpace(connectionString)) return;   // ← saída antecipada
services.AddOpenTelemetry().UseAzureMonitor(o => o.ConnectionString = connectionString);
```

A saída antecipada é deliberada: **sem a chave do Azure a API sobe normalmente**,
apenas sem telemetria. Isso mantém o projeto rodando em máquina local e nos testes.

**`AddLogApi(services)`** — configura o Serilog:

- Nível mínimo `Information`, com `Microsoft.AspNetCore` rebaixado para `Warning`
  (corta o ruído de log de cada requisição do framework).
- `Enrich.FromLogContext()` adiciona propriedades contextuais a cada evento.
- Dois destinos: **console** e **arquivo** em `logs/api-<data>.log`, com rotação
  diária e retenção de 7 arquivos.

**`AddIoC(services, configuration)`** — a amarração das camadas:

```csharp
services.AddDbContext<ApplicationContext>(o => o.UseOracle(configuration.GetConnectionString("Oracle")));

services.AddTransient<IEstudioRepository, EstudioRepository>();
services.AddTransient<IJogoRepository,    JogoRepository>();
services.AddTransient<IEstudioUseCase,    EstudioUseCase>();
services.AddTransient<IJogoUseCase,       JogoUseCase>();
```

Cada linha diz ao contêiner de DI: *"quando alguém pedir esta interface, entregue
aquela implementação"*. `AddTransient` cria uma instância nova a cada injeção
(repositórios e use cases são leves e sem estado). O `DbContext` usa
`AddDbContext`, que registra com tempo de vida **scoped** — uma instância por
requisição HTTP, o correto para uma unidade de trabalho.

**É aqui que a arquitetura se fecha:** trocar Oracle por SQL Server exigiria mudar
apenas esta classe. Nenhum controller ou use case seria tocado.

---

## 7. Camada Presentation

### 7.1 `Presentation/Controllers/EstudioController.cs`

```csharp
[Route("api/estudio")]
[ApiController]
[EnableRateLimiting("politica_rate_limit")]
public class EstudioController : ControllerBase
```

| Atributo | Efeito |
|----------|--------|
| `[Route("api/estudio")]` | Prefixo das rotas |
| `[ApiController]` | Valida os DTOs automaticamente (400 em payload inválido), infere `[FromBody]` e habilita respostas de erro padronizadas |
| `[EnableRateLimiting("politica_rate_limit")]` | Aplica a política de 5 req/20s definida no `Program.cs` a **todos** os endpoints da classe |

O controller recebe `IEstudioUseCase` e `ILogger<EstudioController>` por construtor —
ele **não conhece EF Core nem o repositório**.

**Responsabilidades (e só elas):** traduzir HTTP ↔ use case e escolher o status code.

| Endpoint | Regra de status |
|----------|-----------------|
| `GET /api/estudio` | `200` com dados; `204 No Content` se a página vier vazia; `400` em exceção |
| `GET /api/estudio/{id}` | `200`; `404` se o use case devolver `null` |
| `POST /api/estudio` | `201 Created` via `CreatedAtAction`; `400` se a regra do ano falhar |
| `PUT /api/estudio/{id}` | `200`; `404` se não existir |
| `DELETE /api/estudio/{id}` | `200`; `400` se tiver jogos vinculados; `404` se não existir |

`CreatedAtAction(nameof(Get), new { id = ... }, estudio)` devolve `201` **e** o
cabeçalho `Location` apontando para o recurso recém-criado — o comportamento REST correto.

Todo método é envolvido em `try/catch` que registra `LogError` e devolve `400` com
a mensagem, evitando que uma exceção vaze como `500` sem contexto.

### 7.2 `Presentation/Controllers/JogoController.cs`

Mesma estrutura, com dois endpoints a mais:

**`PATCH /api/jogo/{id}/desconto`** — o único com **dois `catch` distintos**:

```csharp
catch (ArgumentException ex)   // regra de negócio violada
{
    _logger.LogWarning("[REGRA DE NEGOCIO] - {0}", ex.Message);
    return BadRequest(ex.Message);
}
catch (Exception ex)           // falha inesperada
{
    _logger.LogError("[ERROR] - {0}", ex.Message);
    return BadRequest(ex.Message);
}
```

A distinção importa no log: uma regra violada é comportamento **esperado**
(`Warning`), uma exceção genérica indica **defeito** (`Error`). Sem essa separação,
todo desconto de 95% poluiria o log de erros.

**`POST /api/jogo/{jogoId}/plataforma`** — cria a plataforma e o vínculo N:N,
devolvendo `201` ou `404` se o jogo não existir.

O uso de `PATCH` (e não `PUT`) no desconto é semanticamente correto: é uma
**alteração parcial** de um único campo.

### 7.3 `Presentation/Controllers/HealthController.cs`

Expõe os health checks como endpoints REST com JSON estruturado. Recebe o
`HealthCheckService` do framework por injeção.

| Endpoint | Filtro | Pergunta que responde |
|----------|--------|------------------------|
| `GET /api/health/live` | tag `live` | *A API está de pé?* (liveness) |
| `GET /api/health/db` | tag `db` | *O Oracle responde?* (readiness) |

```csharp
var report = await _healthCheckService.CheckHealthAsync(r => r.Tags.Contains("live"), ct);
return report.Status == HealthStatus.Healthy
    ? Ok(result)
    : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
```

O `503 Service Unavailable` é o status que orquestradores (Kubernetes, Azure App
Service) esperam para tirar a instância do balanceador.

**Por que existem dois caminhos para a mesma coisa?** O `Program.cs` mapeia
`/health/live` e `/health/db` no formato nativo (texto puro, para *probes*
automatizados); o `HealthController` oferece `/api/health/*` com JSON detalhado
(nome do check, descrição e mensagem de erro), útil para diagnóstico humano.
Este controller **não tem rate limiting** — monitoramento não pode ser bloqueado.

---

## 8. `Program.cs` — o pipeline HTTP

O arquivo de entrada, em duas fases: **registro de serviços** (antes de
`builder.Build()`) e **montagem do pipeline** (depois).

### Fase 1 — registro de serviços

**1. Telemetria, log e injeção de dependência**
```csharp
builder.Services.AddTelemetria(builder.Configuration);
builder.Services.AddLogApi();
Bootstrap.AddIoC(builder.Services, builder.Configuration);
```

**2. Controllers com enum em texto**
```csharp
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```
Faz `GeneroJogo` viajar como `"Rpg"` em vez de `3`, na entrada e na saída.

**3. Swagger com annotations e exemplos**
```csharp
builder.Services.AddSwaggerGen(c => { c.EnableAnnotations(); c.ExampleFilters(); });
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
```
`EnableAnnotations()` ativa os `[Swagger*]`; `ExampleFilters()` + a última linha
varrem o assembly atrás das classes `IExamplesProvider<T>` em `Doc/Samples`.

**4. Compressão de resposta**
```csharp
options.Providers.Add<BrotliCompressionProvider>();
options.Providers.Add<GzipCompressionProvider>();
options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
```
Brotli vem primeiro (comprime melhor); Gzip é o fallback para clientes antigos.
O `Concat(["application/json"])` é essencial — **JSON não está na lista padrão**,
e sem isso as respostas da API não seriam comprimidas. Ambos usam
`CompressionLevel.SmallestSize` (menor payload, mais CPU).

O cliente escolhe via cabeçalho `Accept-Encoding: br` ou `gzip`.

**5. Rate limiting**
```csharp
options.AddPolicy("politica_rate_limit", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ObterIpDoCliente(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(20),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));
```

| Parâmetro | Valor | Significado |
|-----------|-------|-------------|
| `partitionKey` | IP do cliente | Cada cliente tem **seu próprio contador** — um usuário abusivo não bloqueia os demais |
| `PermitLimit` | 5 | Requisições permitidas por janela |
| `Window` | 20s | Duração da janela fixa |
| `QueueLimit` | 0 | Excedeu, rejeita na hora — não enfileira |

E a resposta da rejeição:
```csharp
options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
options.OnRejected = async (context, ct) => {
    context.HttpContext.Response.Headers.RetryAfter = "20";
    await context.HttpContext.Response.WriteAsync("Limite de 5 requisicoes...", ct);
};
```
O cabeçalho `Retry-After: 20` diz ao cliente **quando** tentar de novo.

A função auxiliar `ObterIpDoCliente` lê primeiro o `X-Forwarded-For` (para quando
a API está atrás de proxy ou load balancer, onde o IP direto seria sempre o do
proxy) e só então cai para `Connection.RemoteIpAddress`.

**6. Health checks**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execucao"), tags: ["live"])
    .AddOracle(connectionString: ..., name: "oracle", failureStatus: HealthStatus.Unhealthy, tags: ["db"]);
```
As **tags** são o que permite consultar os grupos separadamente — liveness não
depende do banco, readiness sim.

### Fase 2 — montagem do pipeline

```csharp
app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health/live", ...);   // Predicate = tags.Contains("live")
app.MapHealthChecks("/health/db",   ...);   // Predicate = tags.Contains("db")
app.UseAuthorization();
app.UseRateLimiter();          // ← barra o excesso antes de gastar CPU
app.UseResponseCompression();  // ← comprime o que passou
app.MapControllers();
app.Run();
```

**A ordem é significativa.** Cada `Use*` é um middleware que embrulha o próximo. O
rate limiter vem **antes** da compressão porque não faz sentido comprimir uma
resposta que será rejeitada. Os health checks vêm antes do rate limiter, e por
isso ficam fora do limite.

### `public partial class Program { }`

A última linha do arquivo. Em um projeto com *top-level statements*, a classe
`Program` gerada é `internal` — invisível para o projeto de testes. Declará-la
como `partial` (e pública) permite que `WebApplicationFactory<Program>` a
referencie nos testes funcionais. Sem essa linha, os testes de controller não compilam.

---

## 9. Documentação Swagger (`Doc/Samples`)

Seis classes que implementam `IExamplesProvider<T>` e alimentam o Swagger com
payloads realistas em vez dos valores genéricos (`"string"`, `0`) gerados por padrão.

| Classe | Alimenta |
|--------|----------|
| `EstudioRequestSample` | Corpo do `POST /api/estudio` |
| `EstudioResponseSample` | Resposta `200`/`201` de estúdio |
| `EstudioResponseListSample` | Resposta `200` da listagem paginada |
| `JogoRequestSample` | Corpo do `POST /api/jogo` |
| `JogoResponseSample` | Resposta `200`/`201` de jogo |
| `JogoResponseListSample` | Resposta `200` da listagem paginada |

```csharp
public class JogoRequestSample : IExamplesProvider<PostJogoDto>
{
    public PostJogoDto GetExamples() => new PostJogoDto
    {
        Titulo = "Neon Drift", Genero = GeneroJogo.Acao, Preco = 149.90, ...
    };
}
```

A ligação com o endpoint é feita pelos atributos no controller:
`[SwaggerRequestExample(typeof(PostJogoDto), typeof(JogoRequestSample))]` e
`[SwaggerResponseExample(200, typeof(JogoResponseListSample))]`.

Resultado prático: no Swagger UI, o botão *Try it out* já vem com um JSON válido
preenchido — dá para testar a API sem digitar nada.

---

## 10. Arquivos de configuração

### `GameCatalog.API.csproj`

```xml
<TargetFramework>net8.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<RootNamespace>GameCatalog.API</RootNamespace>
<RollForward>LatestMajor</RollForward>
```

| Propriedade | Efeito |
|-------------|--------|
| `Nullable` | Ativa a análise de nulos — daí os `?` em `EstudioEntity?` e os avisos do compilador |
| `ImplicitUsings` | Importa automaticamente `System`, `System.Linq`, `Microsoft.AspNetCore.*` etc. |
| `RollForward=LatestMajor` | Permite rodar em máquinas que só têm runtime .NET 9/10 instalado |

### `appsettings.json` e `appsettings.Development.json`

O ASP.NET carrega `appsettings.json` e **sobrepõe** com o arquivo do ambiente
atual (`ASPNETCORE_ENVIRONMENT`, que o `launchSettings.json` define como
`Development`). Ou seja: o arquivo `.Development.json` **vence** para as chaves
que declara.

Chaves relevantes:

| Chave | Usada em |
|-------|----------|
| `ConnectionStrings:Oracle` | `Bootstrap.AddIoC` (`UseOracle`) e o health check do Oracle |
| `ApplicationInsights:ConnectionString` | `Bootstrap.AddTelemetria` |
| `Logging:LogLevel` | Configuração base de log do framework |

> ⚠️ **Nunca versione sua senha do Oracle.** Em vez de escrevê-la no
> `appsettings.Development.json`, prefira *user secrets*:
> ```bash
> dotnet user-secrets init --project GameCatalog.API
> dotnet user-secrets set "ConnectionStrings:Oracle" "..." --project GameCatalog.API
> ```

### `Properties/launchSettings.json`

Perfis de execução local (só afeta a máquina do desenvolvedor):

| Perfil | URLs | Observação |
|--------|------|------------|
| `http` | `http://localhost:5121` | Perfil padrão de `dotnet run` |
| `https` | `https://localhost:7121` + `http://localhost:5121` | Exige certificado de dev |

Ambos abrem o navegador direto em `/swagger` e definem `ASPNETCORE_ENVIRONMENT=Development`.

### `GameCatalog.API.http`

Coleção de requisições prontas (POSTs de estúdios, jogos, plataformas e cenários
de erro). Executável direto do Visual Studio ou do VS Code com a extensão
*REST Client* — serve para popular a base rapidamente.

---

## 11. Projeto de testes

**55 testes** (48 `[Fact]` + 2 `[Theory]` que geram 7 casos), em três níveis.
Nenhum deles precisa de Oracle.

```bash
dotnet test
```

### 11.1 `App/CustomWebApplicationFactory.cs`

A peça central dos testes funcionais. Herda de `WebApplicationFactory<Program>`,
que sobe **a API inteira em memória** (via `TestServer`, sem abrir porta de rede)
e permite substituir serviços:

```csharp
services.RemoveAll(typeof(IEstudioUseCase));
services.AddSingleton(EstudioUseCaseMock.Object);
```

Os use cases reais são trocados por mocks do Moq. Resultado: o teste exercita
**todo o pipeline real** — roteamento, `[ApiController]`, rate limiting,
compressão, serialização JSON — mas sem tocar no banco.

**`CompatibilidadeTestServerFilter`** (classe aninhada privada) resolve um
problema de compatibilidade: o `PipeWriter` do `Microsoft.AspNetCore.TestHost`
8.x não implementa `PipeWriter.UnflushedBytes`, membro que o `System.Text.Json`
passou a exigir a partir do .NET 9. Em uma máquina que só tenha runtime 9/10,
toda resposta JSON dos testes falharia com `InvalidOperationException`. O filtro
troca a feature de corpo da resposta por uma implementação baseada em `Stream`,
compatível com as duas versões.

> É código **exclusivo dos testes**. A API em execução usa Kestrel, não o
> TestServer, e não é afetada.

### 11.2 Testes de repositório — EF Core InMemory

`EstudioRepositoryTest` (9 testes) e `JogoRepositoryTest` (8 testes) usam
`Microsoft.EntityFrameworkCore.InMemory`: um provedor que guarda os dados em
memória, com um nome de banco único por teste (isolamento total).

Cobrem: paginação (`PageSize` respeitado, segunda página traz registros
diferentes), filtros de busca/gênero/estúdio, `ObterTotal` coerente com os
filtros, CRUD completo, `AtualizarPreco` e o vínculo N:N de plataforma.

### 11.3 Testes de use case — Moq

`EstudioUseCaseTest` (10 casos) e `JogoUseCaseTest` (11 casos) injetam mocks dos
repositórios e verificam **apenas a regra de negócio**:

- Normalização da paginação (`PageNumber = 0` vira 1, `PageSize = 150` vira 100).
- Metadados de `PaginacaoDto` calculados corretamente.
- Ano de fundação inválido lança exceção (`[Theory]` com 1900, 1957, 2500).
- Estúdio com jogos vinculados não pode ser deletado.
- Jogo com estúdio inexistente não persiste.
- Desconto: cálculo do novo preço, limite de 90% permitido, percentuais
  inválidos lançam exceção (`[Theory]` com 0, -10, 90.01, 150).
- Mapeamento DTO → entidade.

### 11.4 Testes funcionais — HTTP de ponta a ponta

| Suíte | Testes | Destaques |
|-------|--------|-----------|
| `EstudioControllerTest` | 5 | 200 com dados, 204 sem dados, 404, 400 de regra de negócio, 400 de payload inválido |
| `JogoControllerTest` | 9 | Filtro repassado ao use case, desconto 200/400, **429 do rate limit**, **rate limit independente por cliente**, **compressão Brotli** |
| `HealthControllerTest` | 3 | `/api/health/live`, `/health/live` e o documento OpenAPI acessível |

Os testes de rate limit fazem 6 requisições seguidas e conferem que a sexta
retorna `429`; o de independência usa `X-Forwarded-For` diferentes e confirma que
um cliente não afeta o outro. O de compressão envia `Accept-Encoding: br` e
valida o cabeçalho `Content-Encoding` da resposta.

---

## 12. Passo a passo para rodar

### 12.1 Instalar o .NET SDK 8

Baixe em https://dotnet.microsoft.com/download/dotnet/8.0 e confirme:

```bash
dotnet --version
```

### 12.2 Restaurar os pacotes

Na pasta raiz do projeto (`CP4-dotnet`):

```bash
dotnet restore
```

### 12.3 Configurar a conexão com o Oracle

Edite `GameCatalog.API/appsettings.Development.json` e preencha `User Id` e `Password`:

```json
"ConnectionStrings": {
  "Oracle": "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))) (CONNECT_DATA=(SERVER=DEDICATED)(SID=ORCL)));User Id=RM123456;Password=SUA_SENHA;"
}
```

### 12.4 Instalar o dotnet-ef e criar as tabelas

```bash
dotnet tool install --global dotnet-ef --version 8.0.26
```

```bash
dotnet ef database update --project GameCatalog.API
```

Isso executa a migration `InitDb`, criando as 4 tabelas e os 5 índices.

### 12.5 Executar a API

```bash
dotnet run --project GameCatalog.API
```

A API sobe em `http://localhost:5121` e o Swagger abre em `/swagger`.

### 12.6 Executar os testes

```bash
dotnet test
```

Esperado: **55 testes aprovados**, sem necessidade de banco.

### Comandos úteis

| Comando | O que faz |
|---------|-----------|
| `dotnet build` | Compila sem executar |
| `dotnet test --logger "console;verbosity=detailed"` | Testes com o nome de cada caso |
| `dotnet ef migrations add NomeDaMudanca --project GameCatalog.API` | Cria uma nova migration |
| `dotnet ef migrations list --project GameCatalog.API` | Lista as migrations e quais já foram aplicadas |
| `dotnet ef database update 0 --project GameCatalog.API` | Reverte **todas** as migrations (apaga as tabelas) |
| `dotnet list package --outdated` | Mostra pacotes com versão mais nova disponível |

---

## 13. Solução de problemas

| Sintoma | Causa provável | Solução |
|---------|----------------|---------|
| `ORA-01017: invalid username/password` | Credenciais erradas na connection string | Revise `User Id` e `Password` no `appsettings.Development.json` |
| `ORA-12541: TNS:no listener` | Sem acesso ao servidor da FIAP | Verifique a rede/VPN e o host `oracle.fiap.com.br:1521` |
| `ORA-00955: name is already used` no `database update` | As tabelas já existem no schema | Apague as tabelas ou use `dotnet ef database update 0` antes |
| `dotnet ef` não é reconhecido | Ferramenta não instalada ou fora do PATH | Rode o `dotnet tool install` da seção 12.4 e abra um terminal novo |
| `/api/health/db` devolve `503` | O Oracle não responde | Normal sem acesso ao banco — `/api/health/live` continua `200` |
| `429 Too Many Requests` durante a demonstração | Rate limit de 5 req/20s | Aguarde 20 segundos, ou ajuste `PermitLimit` no `Program.cs` |
| Resposta JSON falha nos testes | Runtime mais novo que .NET 8 | Já tratado pelo `CompatibilidadeTestServerFilter` — confirme que está usando o `CustomWebApplicationFactory` |
| A API sobe mas não registra telemetria | `ApplicationInsights:ConnectionString` vazia | Comportamento esperado; preencha a chave para ativar |
