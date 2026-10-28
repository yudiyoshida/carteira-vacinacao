# Contexto do agente — Cartão de Vacinação

Sistema de cartão de vacinação: API REST em ASP.NET Core 10 (MediatR e EF Core), PostgreSQL 18 e interface em Angular 21.

Você executa as tasks de `docs/TASKS.md`, uma por vez. O autor revisa cada entrega e faz o commit.

## Documentação

Os caminhos partem da raiz do repositório.

- `docs/TASKS.md`: plano, ordem e progresso das tasks. Leia a entrada da task no início de cada uma.
- `docs/PRD.md`: o que o sistema faz, com as funcionalidades (F01–F10), as regras de negócio (RN01–RN26) e os critérios de aceite. Leia as RN que a task cita.
- `docs/TECHSPEC.md`: como o sistema é construído. Leia as seções da linha "Referência" da task.
- `docs/adr/`: o porquê das decisões de arquitetura. Leia o ADR antes de mudar ou contestar a decisão dele.
- `GLOSSARY.md`: termos do domínio e o nome de cada um no código. Use esses termos em código, documentação e mensagens.
- `.agents/rules/architecture.md`: camadas, dependências e onde cada coisa fica. Leia antes de criar, mover ou importar código.
- `.agents/skills/`: uma skill por tipo de mudança, com passos e modelos de código. Leia o `SKILL.md` da área antes de mexer nela; `new-feature` ordena os passos de uma task.
- `PRODUCT.MD`: enunciado original. O PRD resolve as ambiguidades dele e prevalece.

## Como executar uma task

1. **Ponto de partida:** rode `git status`. Se houver trabalho pendente de outra task, pare e pergunte ao autor.
2. **Leitura:** leia a entrada da task, as RN citadas e as seções da "Referência". A leitura termina quando você sabe qual arquivo ou teste atende cada item de "Entrega" e de "Verificação".
3. **Implementação:** entregue o que a task pede; uma ideia fora dela vira sugestão na entrega. Cada regra de domínio nasce de um teste vermelho: escreva o teste, veja falhar e só então implemente.
4. **Pronto:** cumpra a "Verificação" da task e, em task de código, cada item da definição de pronto (`docs/TASKS.md`, seção 2). Revise o diff com a skill `task-review` e corrija os achados bloqueantes. Depois, marque a task com `[x]` em `docs/TASKS.md`.
5. **Entrega:** pare e responda com:
   - os arquivos alterados;
   - os comandos executados e o resultado de cada um;
   - as RN e os critérios cobertos;
   - as decisões tomadas, os desvios e as pendências;
   - uma mensagem de commit sugerida, em Conventional Commits, em inglês e sem trailer de coautoria.

   A task seguinte começa só depois da aprovação do autor.

Uma lacuna ou contradição entre a task, os documentos e o código interrompe a task: pare e pergunte. A resposta entra na TechSpec ou num ADR antes do código.

## Regras fixas

- **Git:** você só lê (`status`, `diff`, `log`, `show`). Qualquer comando que mude o índice, o histórico ou a árvore de trabalho é do autor.
- **Idioma:** código, comentários, identificadores, rotas e commits em inglês; documentação, skills, mensagens de erro e interface em português.
- **Pacotes:** use as versões da TechSpec §2. O MediatR fica no 12.5.0, a última versão Apache-2.0 (`docs/adr/0005-mediatr-12-5.md`). Pacote fora da TechSpec ou troca de versão principal é decisão do autor: pergunte antes e informe a licença.

## Comandos

```bash
docker compose up db                                         # banco de dados para a API local
dotnet build backend/VaccinationCard.slnx                    # warnings são erros
dotnet test backend/VaccinationCard.slnx                     # exige Docker (Testcontainers)
dotnet test backend/VaccinationCard.slnx --filter "RN=RN10"  # testes de uma regra
dotnet run --project backend/src/VaccinationCard.Api
npm --prefix frontend start                                  # também: test, run lint, run build
docker compose up --build                                    # sistema inteiro
```
