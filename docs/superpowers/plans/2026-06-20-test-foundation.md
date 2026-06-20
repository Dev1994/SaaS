# Test Foundation (#21) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an automated test suite covering `PhraseService` (unit) and the HTTP endpoints (integration), giving a regression net before the M3/M4 security changes.

**Architecture:** A new xUnit test project `SaffaApi.Tests` referencing `SaffaApi`. Unit tests drive `PhraseService` directly against a small deterministic JSON fixture. Integration tests use `WebApplicationFactory<Program>` to boot the real app in-memory and hit endpoints over HTTP, asserting status codes and payload shape (not exact phrase content, which changes as `phrases.json` grows).

**Tech Stack:** .NET 10, xUnit, `Microsoft.AspNetCore.Mvc.Testing`, plain xUnit `Assert` (no FluentAssertions — v8+ is commercially licensed).

## Global Constraints

- Target framework: `net10.0` (matches `SaffaApi`).
- `Nullable` enabled, `ImplicitUsings` enabled (match `SaffaApi.csproj`).
- No new runtime dependencies added to `SaffaApi` — test-only packages live in the test project.
- Integration tests must NOT assert on specific phrase text or counts from the real `data/phrases.json`; assert on shape, status, and structural invariants only.
- Test runner command for every task: `dotnet test` from repo root `C:\dev\SaaS`.

---

### Task 1: Test project scaffold + boot smoke test

Creates the test project, wires it into the solution, exposes `Program` for `WebApplicationFactory`, and proves the app boots in-memory via `/health`. Everything later builds on this harness.

**Files:**
- Modify: `SaffaApi/Program.cs` (append `public partial class Program { }`)
- Create: `SaffaApi.Tests/SaffaApi.Tests.csproj`
- Create: `SaffaApi.Tests/SmokeTests.cs`
- Modify: `SaffaApi/SaffaApi.slnx` (add test project entry)

**Interfaces:**
- Consumes: nothing.
- Produces: `public partial class Program` (entry point made referenceable by `WebApplicationFactory<Program>`); test project `SaffaApi.Tests` that other tasks add files to.

- [ ] **Step 1: Make `Program` referenceable**

Append to the end of `SaffaApi/Program.cs` (after `app.Run();`):

```csharp

public partial class Program { }
```

- [ ] **Step 2: Create the test project file**

Create `SaffaApi.Tests/SaffaApi.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SaffaApi/SaffaApi.csproj" />
  </ItemGroup>

</Project>
```

Note: if `dotnet restore` fails on a pinned version, run `dotnet add SaffaApi.Tests package <name>` to take the latest compatible and record the resolved version.

- [ ] **Step 3: Write the failing smoke test**

Create `SaffaApi.Tests/SmokeTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SaffaApi.Tests;

public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 4: Run the test to verify it fails to build/run**

Run: `dotnet test`
Expected: FAIL — project not yet in solution / first build. Confirm the failure is a compile or discovery issue, not an assertion failure, before proceeding.

- [ ] **Step 5: Add the test project to the solution**

Edit `SaffaApi/SaffaApi.slnx`, add inside `<Solution>` next to the existing project entries:

```xml
  <Project Path="../SaffaApi.Tests/SaffaApi.Tests.csproj" />
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test`
Expected: PASS — 1 test passing.

- [ ] **Step 7: Commit**

```bash
git add SaffaApi.Tests/ SaffaApi/Program.cs SaffaApi/SaffaApi.slnx
git commit -m "test: scaffold SaffaApi.Tests with boot smoke test"
```

---

### Task 2: PhraseService query-method unit tests

Tests the core read methods against a small deterministic JSON fixture so exact counts and lookups are assertable.

**Files:**
- Create: `SaffaApi.Tests/TestData/test-phrases.json`
- Create: `SaffaApi.Tests/PhraseServiceTests.cs`
- Modify: `SaffaApi.Tests/SaffaApi.Tests.csproj` (copy fixture to output)

**Interfaces:**
- Consumes: `SaffaApi.Services.PhraseService(string jsonFilePath)`, methods `GetRandom()`, `GetByCategory(string)`, `GetByTerm(string)`, `GetForDutch()`, `GetRandomForDutch()` from `SaffaApi`.
- Produces: a reusable fixture file `TestData/test-phrases.json` (4 phrases: 2 `slang`, 1 `cultural`, 1 `expression`; 3 with a Dutch explanation, 1 without) used by later unit tasks.

- [ ] **Step 1: Create the deterministic fixture**

Create `SaffaApi.Tests/TestData/test-phrases.json`:

```json
[
  { "text": "Braai", "category": "cultural", "actualMeaning": "Barbecue gathering", "afrikaansInfluence": true, "explainLikeImDutch": "Not just a barbecue.", "misunderstandingProbability": 0.9, "confidence": "High" },
  { "text": "Voetsek", "category": "slang", "actualMeaning": "Go away", "afrikaansInfluence": true, "explainLikeImDutch": "Not polite.", "misunderstandingProbability": 0.8, "confidence": "High" },
  { "text": "Lekker", "category": "expression", "actualMeaning": "Nice, good", "afrikaansInfluence": true, "explainLikeImDutch": "Broader than Dutch lekker.", "misunderstandingProbability": 0.5, "confidence": "High" },
  { "text": "Eish", "category": "slang", "actualMeaning": "Expression of dismay", "afrikaansInfluence": false, "explainLikeImDutch": "", "misunderstandingProbability": 0.3, "confidence": "High" }
]
```

- [ ] **Step 2: Ensure the fixture copies to output**

Add to `SaffaApi.Tests/SaffaApi.Tests.csproj` inside a new `<ItemGroup>`:

```xml
  <ItemGroup>
    <Content Include="TestData\test-phrases.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
```

- [ ] **Step 3: Write the failing unit tests**

Create `SaffaApi.Tests/PhraseServiceTests.cs`:

```csharp
using SaffaApi.Services;

namespace SaffaApi.Tests;

public class PhraseServiceTests
{
    private static PhraseService CreateService()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "test-phrases.json");
        return new PhraseService(path);
    }

    [Fact]
    public void GetRandom_returns_a_phrase_from_the_set()
    {
        var service = CreateService();

        var phrase = service.GetRandom();

        Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
    }

    [Theory]
    [InlineData("slang", 2)]
    [InlineData("cultural", 1)]
    [InlineData("expression", 1)]
    public void GetByCategory_returns_expected_count(string category, int expected)
    {
        var service = CreateService();

        var phrases = service.GetByCategory(category);

        Assert.Equal(expected, phrases.Count);
    }

    [Fact]
    public void GetByCategory_is_case_insensitive()
    {
        var service = CreateService();

        Assert.Equal(2, service.GetByCategory("SLANG").Count);
    }

    [Fact]
    public void GetByCategory_unknown_returns_empty_list()
    {
        var service = CreateService();

        Assert.Empty(service.GetByCategory("nonexistent"));
    }

    [Fact]
    public void GetByCategory_returns_a_copy_not_internal_list()
    {
        var service = CreateService();

        var first = service.GetByCategory("slang");
        first.Clear();

        Assert.Equal(2, service.GetByCategory("slang").Count);
    }

    [Theory]
    [InlineData("Braai")]
    [InlineData("braai")]
    [InlineData("BRAAI")]
    public void GetByTerm_is_case_insensitive_and_found(string term)
    {
        var service = CreateService();

        var phrase = service.GetByTerm(term);

        Assert.NotNull(phrase);
        Assert.Equal("Braai", phrase!.Text);
    }

    [Fact]
    public void GetByTerm_unknown_returns_null()
    {
        var service = CreateService();

        Assert.Null(service.GetByTerm("notarealterm"));
    }

    [Fact]
    public void GetForDutch_returns_only_phrases_with_dutch_explanation()
    {
        var service = CreateService();

        var dutch = service.GetForDutch();

        Assert.Equal(3, dutch.Count);
        Assert.All(dutch, p => Assert.False(string.IsNullOrWhiteSpace(p.ExplainLikeImDutch)));
    }

    [Fact]
    public void GetRandomForDutch_returns_phrase_with_dutch_explanation()
    {
        var service = CreateService();

        var phrase = service.GetRandomForDutch();

        Assert.False(string.IsNullOrWhiteSpace(phrase.ExplainLikeImDutch));
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test`
Expected: FAIL — fixture not found OR assertions fail until the `CopyToOutputDirectory` build picks it up. If failure is "file not found", confirm Step 2 was saved and rebuild.

- [ ] **Step 5: Make the tests pass**

No production code change needed — these test existing behaviour. Re-run after the fixture is copying:

Run: `dotnet test`
Expected: PASS — all `PhraseServiceTests` green.

- [ ] **Step 6: Commit**

```bash
git add SaffaApi.Tests/
git commit -m "test: add PhraseService query-method unit tests"
```

---

### Task 3: PhraseService edge & error-path unit tests

Covers the empty-dataset and missing-file branches, which the fixture-based tests cannot reach.

**Files:**
- Modify: `SaffaApi.Tests/PhraseServiceTests.cs` (add edge-case tests)

**Interfaces:**
- Consumes: `PhraseService` constructor (throws `FileNotFoundException` on missing file), `GetRandom()` and `GetRandomForDutch()` (return `new Phrase()` when the relevant set is empty).
- Produces: nothing new.

- [ ] **Step 1: Write the failing edge-case tests**

Append to the `PhraseServiceTests` class in `SaffaApi.Tests/PhraseServiceTests.cs`:

```csharp
    [Fact]
    public void Constructor_throws_when_file_missing()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "does-not-exist.json");

        Assert.Throws<FileNotFoundException>(() => new PhraseService(path));
    }

    [Fact]
    public void GetRandom_on_empty_dataset_returns_blank_phrase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "[]");
        try
        {
            var service = new PhraseService(path);

            var phrase = service.GetRandom();

            Assert.Equal(string.Empty, phrase.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetRandomForDutch_on_empty_dataset_returns_blank_phrase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "[]");
        try
        {
            var service = new PhraseService(path);

            var phrase = service.GetRandomForDutch();

            Assert.Equal(string.Empty, phrase.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }
```

- [ ] **Step 2: Run the tests to verify**

Run: `dotnet test`
Expected: PASS — these assert existing behaviour (`PhraseService` already throws on missing file and returns `new Phrase()` on empty sets). If any fail, that is a real bug in the SUT — STOP and report rather than editing the test to match.

- [ ] **Step 3: Commit**

```bash
git add SaffaApi.Tests/PhraseServiceTests.cs
git commit -m "test: add PhraseService edge and error-path tests"
```

---

### Task 4: Endpoint integration tests

Boots the real app via `WebApplicationFactory<Program>` and exercises every route, asserting status codes and payload shape against the shipped `data/phrases.json`.

**Files:**
- Create: `SaffaApi.Tests/EndpointTests.cs`

**Interfaces:**
- Consumes: `WebApplicationFactory<Program>` (from Task 1), the live routes `/`, `/phrase`, `/phrase/dutch`, `/phrase/{term}`, `/phrase/category/{category}`, `/health`.
- Produces: nothing new.

- [ ] **Step 1: Write the failing integration tests**

Create `SaffaApi.Tests/EndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SaffaApi.Models;

namespace SaffaApi.Tests;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Root_returns_welcome_payload()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Saffa as a Service", body);
    }

    [Fact]
    public async Task Phrase_returns_a_non_empty_phrase()
    {
        var client = _factory.CreateClient();

        var phrase = await client.GetFromJsonAsync<Phrase>("/phrase", JsonOptions);

        Assert.NotNull(phrase);
        Assert.False(string.IsNullOrWhiteSpace(phrase!.Text));
    }

    [Fact]
    public async Task Phrase_dutch_returns_phrase_with_dutch_explanation()
    {
        var client = _factory.CreateClient();

        var phrase = await client.GetFromJsonAsync<Phrase>("/phrase/dutch", JsonOptions);

        Assert.NotNull(phrase);
        Assert.False(string.IsNullOrWhiteSpace(phrase!.ExplainLikeImDutch));
    }

    [Fact]
    public async Task Phrase_by_term_returns_200_for_known_term()
    {
        var client = _factory.CreateClient();

        // Fetch a real phrase first so the test does not hard-code data.
        var seed = await client.GetFromJsonAsync<Phrase>("/phrase", JsonOptions);
        Assert.NotNull(seed);

        var response = await client.GetAsync($"/phrase/{Uri.EscapeDataString(seed!.Text)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<Phrase>(JsonOptions);
        Assert.Equal(seed.Text, found!.Text);
    }

    [Fact]
    public async Task Phrase_by_term_returns_404_for_unknown_term()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/phrase/definitely-not-a-real-term-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Phrase_by_category_returns_list()
    {
        var client = _factory.CreateClient();

        var phrases = await client.GetFromJsonAsync<List<Phrase>>("/phrase/category/slang", JsonOptions);

        Assert.NotNull(phrases);
        Assert.NotEmpty(phrases!);
        Assert.All(phrases!, p => Assert.Equal("slang", p.Category, ignoreCase: true));
    }

    [Fact]
    public async Task Phrase_by_unknown_category_returns_empty_list()
    {
        var client = _factory.CreateClient();

        var phrases = await client.GetFromJsonAsync<List<Phrase>>("/phrase/category/nope", JsonOptions);

        Assert.NotNull(phrases);
        Assert.Empty(phrases!);
    }

    [Fact]
    public async Task Health_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all `EndpointTests` green. The factory boots the app with the real `data/phrases.json`; `slang` is a known-populated category. If `/phrase/category/slang` is empty, the data changed — report rather than weaken the assertion.

- [ ] **Step 3: Commit**

```bash
git add SaffaApi.Tests/EndpointTests.cs
git commit -m "test: add endpoint integration tests"
```

---

## Self-Review

**Spec coverage:** The roadmap spec describes #21 as "unit coverage for `PhraseService`, integration coverage for endpoints (status codes, payload shape, rate limiting, category/term lookup, not-found paths)." Covered: PhraseService units (Tasks 2–3), endpoint integration with status + shape (Task 4), category lookup (Tasks 2, 4), term lookup found/not-found (Tasks 2, 4), not-found paths (Tasks 2, 4). **Gap:** rate-limiting was named in the spec but is not directly tested here — asserting 429s requires firing 100+ requests and is flaky in-memory. Deferred deliberately; noted so the reviewer knows it was a choice, not an omission.

**Placeholder scan:** No TBD/TODO; every code step contains complete code.

**Type consistency:** `Phrase` properties (`Text`, `Category`, `ExplainLikeImDutch`) match `Models/Phrase.cs`. Service method names match `IPhraseService`. `WebApplicationFactory<Program>` consistent across Tasks 1 and 4. Task 4 uses `System.Net.Http.Json` for `GetFromJsonAsync`/`ReadFromJsonAsync`.
