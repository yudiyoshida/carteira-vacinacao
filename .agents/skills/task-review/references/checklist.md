# Checklist de revisão

Aplique cada item ao diff. Um item que o diff não toca fica fora do relatório.

## Especificação

- Cada item de "Entrega" e de "Verificação" da task está no diff.
- O comportamento bate com as RN citadas e com os contratos da TechSpec §7.
- O diff fica dentro da task; o que passou disso vira sugestão na entrega.

## Arquitetura

- Cada tipo novo está na pasta da tabela "Onde cada coisa fica", com o namespace igual à pasta.
- As dependências respeitam A1–A7, e os testes de arquitetura passam.
- Outro módulo entra só por query.

## Domínio

- Cada RN da task tem a regra no dono certo e um teste com `[Trait]`.
- O registro de vacinação checa na ordem RN09, RN11, RN10, RN12, RN13.
- "Hoje" chega ao domínio como parâmetro.

## Casos de uso

- Os tipos levam os sufixos de A7, e o handler devolve output, DTO ou `SuccessMessage`.
- Nomes com `Trim` e `CancellationToken` repassado.
- Validator com mensagens em português, por campo.

## Persistência

- DAO com `AsNoTracking` e projeção; Repository com a diferença das vacinações.
- `ToTable`, `ValueGeneratedNever` na chave e índice único com nome explícito.
- Migration lida e coerente com a TechSpec §6.1.
- Ports sem tipos do EF Core.

## API

- Rota, status e corpo iguais à linha da TechSpec §7.2.
- `[ProducesResponseType]` para cada status; `[EndpointSummary]` e `[Tags]` em português.
- Exemplo da rota em `backend/VaccinationCard.http`.
- `[AllowAnonymous]` só nas rotas anônimas da TechSpec §7.2.

## Erros

- Código novo na TechSpec §8.3 e no catálogo do módulo, com teste.
- Mensagens em português, sem CPF, nome ou e-mail.

## Segurança e privacidade

- Nenhum segredo real; valores de desenvolvimento só onde a TechSpec §12.2 indica.
- CPF fora de rota, query string e logs; token, senha e hash fora dos logs.
- As invariantes da skill `authentication` continuam valendo.

## Testes

- Par unit + integração para cada caso de uso e teste HTTP para cada rota.
- Nomes `Should_<Resultado>_When_<Condição>`, `_sut` e comentários Arrange, Act e Assert.
- Tempo por `FakeTimeProvider` e asserções pelo código do erro.

## Frontend

- Cada arquivo na pasta da tabela "Frontend" de `.agents/rules/architecture.md`.
- Componentes com OnPush, signals, `inject()`, `input()` e `output()`; HTML com `@if` e `@for` e bindings de `class` e `style`.
- Chamadas da API só pelos services de `core/api`, com `environment.apiUrl`.
- Access token só em memória, erros pelo `core/utils/api-error.ts` e avisos pelo `ToastService`.
- Cores e espaçamentos pelas variáveis de `_tokens.scss`; a tela funciona nos temas claro e escuro.
- Rótulo e ARIA em todo campo e botão de ícone; `npm run lint` sem erros.
- Textos em português e rótulo de dose pelo `DoseLabelPipe`.

## Documentação

- TechSpec, ADRs, glossário e skills refletem as decisões e as convenções novas.
- Task marcada em `docs/TASKS.md`.
- Mensagem de commit sugerida em inglês, em Conventional Commits e sem trailer de coautoria.
