# ClickBusRouterAPI

Web API em .NET 10 que recebe a posição do usuário no Terminal Tietê e devolve a rota a pé mais rápida até o destino.

## Camadas

```
src/
  ClickBusRouterAPI.Domain          Entidades (MapPoint, Edge, Segment), Coordinate e o contrato IMapGraph. Sem dependências.
  ClickBusRouterAPI.Application     Casos de uso: RouteService (A*), PointService, projeção no corredor, instruções, exceções.
  ClickBusRouterAPI.Infrastructure  Lê o JSON, valida e monta o grafo em FrozenDictionary (singleton).
  ClickBusRouterAPI.Api             Controllers, ProblemDetails, Swagger, configuração.
tests/
  ClickBusRouterAPI.UnitTests        Loader, qualidade do JSON, normalização, curvas, projeção, A* (comparado
                                     com Dijkstra em todos os pares do mapa), RouteService e PointService.
  ClickBusRouterAPI.IntegrationTests API inteira em memória (WebApplicationFactory): status HTTP, ProblemDetails, contratos.
```

Dependências: Api → Application + Infrastructure → Domain. A Application só conhece `IMapGraph`, então trocar o JSON por banco/Redis é mexer só na Infrastructure.

## Como funciona

1. **Carga em memória**: no startup o JSON vira `FrozenDictionary<string, MapPoint>` (pontos) e `FrozenDictionary<string, ImmutableArray<Edge>>` (adjacência). Ids duplicados ou conexões para pontos inexistentes derrubam a aplicação na subida; pontos sem conexão geram warning no log.
2. **Origem**: a coordenada do usuário é projetada no trecho de corredor mais próximo (não no nó mais próximo), e o custo parcial até cada ponta do trecho entra no cálculo.
3. **Rota**: A* com heurística euclidiana. Aceita vários destinos ao mesmo tempo, então `destinationQuery: "bilheteria"` retorna a bilheteria mais próxima pelo caminho, não em linha reta.
4. **Resposta**: passos com instrução ("Vire à direita em Corredor 89 e siga até …"), distância por trecho e acumulada, distância em metros, tempo estimado e polyline para desenhar no mapa.

## Rodando

```bash
dotnet run --project src/ClickBusRouterAPI.Api      # Swagger em http://localhost:5080/swagger
dotnet test                                          # unitários + integração
dotnet test --collect:"XPlat Code Coverage"          # com cobertura (coverlet)
```

A documentação completa (parâmetros, tipos e exemplos) fica no Swagger em `/swagger`, gerada a partir dos comentários XML das três camadas.

## Referência da API

### Sistema de coordenadas

`x` e `y` vão de **0 a 100** sobre a planta do terminal. `x` cresce para a direita e `y` cresce **para baixo** (como em coordenadas de tela). Distâncias são devolvidas em unidades do mapa e em metros (`Routing:MetersPerMapUnit`).

### `POST /api/routes` — calcular rota

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `x` | number | sim | Coordenada horizontal do usuário (0 = esquerda, 100 = direita). |
| `y` | number | sim | Coordenada vertical do usuário (0 = topo, 100 = base). |
| `destinationId` | string | um dos dois | Id exato do destino, ex.: `TIETE-TER-SERVICO-031`. Sem diferenciar maiúsculas. |
| `destinationQuery` | string | um dos dois | Início do nome, sem acento/maiúsculas. A rota vai para o candidato **mais próximo pelo caminho**. |
| `floor` | string | não | Andar do usuário. Padrão `terreo` (único andar mapeado). |

Valores úteis para `destinationQuery`: `bilheteria`, `informacao`, `alimentacao`, `praca de alimentacao`, `lojas`, `sanitarios`, `telefones`, `farmacia`, `taxi`, `acesso metro`, `cruzeiro do sul`, `estacionamento`. O nome completo também funciona (`bilheteria 6`).

```http
POST /api/routes
{ "x": 51.81, "y": 75.81, "destinationQuery": "bilheteria" }
```

### `GET /api/routes` — mesma operação via query string

| Parâmetro | Equivale a |
|---|---|
| `x`, `y` (obrigatórios) | `x`, `y` |
| `to` | `destinationId` |
| `q` | `destinationQuery` |
| `floor` | `floor` |

```http
GET /api/routes?x=51.81&y=75.81&q=bilheteria
```

### `GET /api/points` — listar pontos

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `type` | `PointType` | Filtra pela categoria. Valor fora da lista retorna 400. |
| `search` | string | Trecho do nome em qualquer posição, sem acento/maiúsculas. |

`GET /api/points/{id}` devolve um ponto (404 se não existir).

### Tipos

**`PointType`** (trafega como texto):

| Valor | Significado | Exemplos |
|---|---|---|
| `entrada` | Acesso ao terminal | Acesso metro, Cruzeiro do sul, estacionamento |
| `corredor` | Nó de passagem usado para montar o caminho | Corredor 82 |
| `servico` | Serviço ou comércio | bilheteria, sanitários, alimentação, lojas, farmácia, táxi |
| `desconhecido` | Tipo não reconhecido no JSON (não ocorre no mapa atual) | — |

**`RouteResult`**: `origin`, `entryPoint` (onde a origem foi encaixada no corredor), `destination`, `totalDistance`, `totalDistanceMeters`, `estimatedSeconds`, `steps[]` (`order`, `point`, `distanceFromPrevious`, `cumulativeDistance`, `instruction`) e `polyline[]` (origem, ponto de entrada e cada passo).

Instruções possíveis em `instruction`: "Siga em frente", "Mantenha-se à direita/esquerda", "Vire à direita/esquerda", "Retorne"; o último passo termina com "Você chegou ao destino.".

### Erros (ProblemDetails, `application/problem+json`)

| Status | Quando |
|---|---|
| 400 | Coordenada ausente/inválida, nenhum ou os dois destinos informados, `type` inválido |
| 404 | `destinationId` inexistente ou nenhum nome começando com `destinationQuery` |
| 422 | Origem longe demais dos corredores, andar sem mapa ou destino sem caminho |

## Configuração (`appsettings.json`)

| Chave | Padrão | Uso |
|---|---|---|
| `MapData:FilePath` | `Data/tiete-map.json` | Arquivo do mapa |
| `Routing:MaxSnapDistance` | `10` | Distância máxima até um corredor para aceitar a origem |
| `Routing:MetersPerMapUnit` | `2.0` | **Estimativa** — calibre com a planta real |
| `Routing:WalkingSpeedMetersPerSecond` | `1.3` | Velocidade média para o tempo estimado |

## Correções aplicadas no JSON

| Id | Antes | Depois | Motivo |
|---|---|---|---|
| `TIETE-TER-SERVICO-008` | bilheria 6 | bilheteria 6 | erro de digitação |
| `TIETE-TER-SERVICO-010` | bilheria 8 | bilheteria 8 | erro de digitação |
| `TIETE-TER-SERVICO-032` | alimentacoa 7 | alimentacao 7 | erro de digitação |
| `TIETE-TER-SERVICO-030` | telefone 3 | telefones 3 | mesmo padrão de "telefones" / "telefones 2" |
| `TIETE-TER-SERVICO-027` | lojas 4 | lojas 6 | nome repetido com `SERVICO-024` |
| `TIETE-TER-CORREDOR-046` | Corredor 71 | Corredor 72 | nome repetido com `CORREDOR-041` |

O teste `MapDataQualityTests` trava isso: falha se aparecer nome fora do vocabulário conhecido ou nome repetido.

Pendência conhecida: `TIETE-TER-SERVICO-018` ("banho") continua sem nenhuma conexão no mapa, por isso a API responde 422 para ele. Basta incluir uma conexão no JSON (e ajustar o teste `Unico_ponto_isolado_e_o_banho`).
