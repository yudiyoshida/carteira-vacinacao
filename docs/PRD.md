# PRD — Cartão de Vacinação

| | |
|---|---|
| Versão | 1.0 |
| Data | 28/10/2026 |
| Status | Final |
| Origem | Enunciado do desafio (`PRODUCT.MD`) |
| Vocabulário | [Glossário](../GLOSSARY.md) |

## 1. Contexto e problema

Cada vacina tem um esquema próprio de doses. Um cartão de vacinação mantido sem regras acumula doses fora de ordem, duplicadas ou com datas incoerentes, e deixa de ser uma fonte confiável.

O sistema gerencia o cartão de vacinação de cada pessoa: cadastro de vacinas e de pessoas, registro de vacinações com validação de dose, consulta do cartão, correção e exclusão de vacinações. As funcionalidades ficam disponíveis por uma API REST com JSON e por uma interface web.

## 2. Objetivos

- **O1.** Registrar vacinações sempre coerentes com o esquema de doses da vacina.
- **O2.** Mostrar o cartão de vacinação agrupado por vacina, com o progresso de cada esquema.
- **O3.** Impedir estados inconsistentes: dose fora do esquema, fora de sequência, duplicada ou com data incoerente.
- **O4.** Restringir o acesso a operadores autenticados e proteger os dados pessoais.

### Não objetivos

- Reproduzir o calendário nacional de vacinação completo.
- Controlar estoque, lote, fabricante, local de aplicação ou profissional que aplicou a dose.
- Agendar doses ou enviar lembretes.
- Oferecer perfis de acesso diferentes.

## 3. Atores

- **Operador:** usa o sistema por meio de uma conta (ex: profissional de saúde).
- **Pessoa:** titular do cartão de vacinação. Não acessa o sistema.

## 4. Escopo

### 4.1 Funcionalidades

| ID | Funcionalidade |
|---|---|
| F01 | Autenticação: entrar, renovar a sessão automaticamente e sair |
| F02 | Cadastrar vacina |
| F03 | Listar vacinas e consultar uma vacina |
| F04 | Cadastrar pessoa |
| F05 | Listar pessoas com busca e paginação; consultar uma pessoa |
| F06 | Remover pessoa |
| F07 | Registrar vacinação |
| F08 | Consultar cartão de vacinação |
| F09 | Corrigir a data de aplicação de uma vacinação |
| F10 | Excluir vacinação |

### 4.2 Fora de escopo

- Editar pessoa; editar ou remover vacina.
- Cadastro de contas, recuperação de senha, perfis de acesso e mais de uma sessão ativa por conta.
- Vacinas recorrentes (ex: influenza anual) e intervalo mínimo entre doses.
- Registro histórico com lacuna (ex: só a 2ª dose comprovada).
- Data de nascimento ou outros dados da pessoa além de nome e CPF; pessoa sem CPF.
- Busca de pessoa por CPF.

## 5. Histórias e critérios de aceite

### F01 — Autenticação

Como operador, quero entrar com e-mail e senha e continuar conectado enquanto uso o sistema, para não repetir o login a cada poucos minutos.

- Credenciais válidas abrem uma sessão.
- Credenciais inválidas geram uma mensagem genérica, que não revela se o e-mail existe (RN22).
- A sessão se renova sem ação do operador por até 7 dias (RN23).
- A 6ª tentativa de login em 1 minuto, a partir do mesmo IP, é recusada (RN24).
- Sair encerra a sessão, e a renovação deixa de funcionar (RN23).
- Sem sessão válida, nenhuma funcionalidade fica acessível (RN21).

### F02 — Cadastrar vacina

Como operador, quero cadastrar uma vacina com o seu esquema de doses, para registrar vacinações dela.

- O nome é obrigatório, tem de 2 a 100 caracteres e não pode repetir o de outra vacina (RN05).
- O esquema tem de 1 a 10 doses primárias e de 0 a 5 doses de reforço (RN06).
- Depois do cadastro, a vacina fica disponível para registrar vacinações.

### F03 — Listar e consultar vacinas

Como operador, quero ver as vacinas cadastradas, para escolher a vacina ao registrar uma vacinação.

- A lista vem ordenada por nome e aceita busca por parte do nome (RN20).
- Sem parâmetros de paginação, a lista traz todas as vacinas (RN20).
- A consulta de uma vacina inexistente informa que ela não foi encontrada.

### F04 — Cadastrar pessoa

Como operador, quero cadastrar uma pessoa, para manter o cartão de vacinação dela.

- O nome é obrigatório e tem de 3 a 150 caracteres (RN01).
- O CPF tem de ser válido e pode vir com ou sem máscara (RN02).
- Um CPF já cadastrado é recusado (RN03).

### F05 — Listar e consultar pessoas

Como operador, quero encontrar uma pessoa pelo nome, para abrir o cartão dela.

- A busca usa parte do nome e ignora maiúsculas e acentos (RN19).
- A lista vem ordenada por nome e paginada: 20 pessoas por página por padrão, no máximo 100 (RN19).
- Na lista, o CPF aparece mascarado; na consulta de uma pessoa e no cartão, aparece completo e formatado (RN26).
- A consulta de uma pessoa inexistente informa que ela não foi encontrada.

### F06 — Remover pessoa

Como operador, quero remover uma pessoa, para apagar os dados dela do sistema.

- A interface pede confirmação antes de remover.
- A remoção apaga definitivamente a pessoa, o cartão e todas as vacinações (RN04).

### F07 — Registrar vacinação

Como operador, quero registrar a dose aplicada numa pessoa, para manter o cartão de vacinação em dia.

- O operador informa a vacina, a dose (tipo e número) e a data de aplicação.
- A interface sugere a próxima dose esperada da vacina escolhida.
- O registro é recusado quando:
  - a vacina ou a pessoa não existe;
  - a dose está fora do esquema da vacina (RN09);
  - a dose já tem vacinação registrada (RN11);
  - a dose não é a próxima dose esperada (RN10);
  - a data de aplicação está ausente, é futura ou é anterior ao limite mínimo (RN12);
  - a data de aplicação não é posterior à da dose anterior (RN13);
  - o cartão foi alterado por outra operação no meio do caminho (RN16).

### F08 — Consultar cartão de vacinação

Como operador, quero ver o cartão de vacinação de uma pessoa, para saber quais doses ela já recebeu.

- O cartão mostra os dados da pessoa e as vacinações agrupadas por vacina (RN17).
- Cada vacina mostra o esquema, as doses aplicadas com a data e o progresso (RN17).
- As doses aparecem com rótulos em português (RN18).

### F09 — Corrigir data de aplicação

Como operador, quero corrigir a data de aplicação de uma vacinação, para consertar um erro de digitação.

- Só a data de aplicação muda. Para trocar a vacina ou a dose, o operador exclui a vacinação e registra outra.
- A nova data segue RN12 e RN14.

### F10 — Excluir vacinação

Como operador, quero excluir uma vacinação, para desfazer um registro errado.

- Só a última dose de uma vacina pode ser excluída (RN15).
- A interface pede confirmação antes de excluir.
- Quando a vacinação excluída era a única da vacina, a vacina sai do cartão.

## 6. Regras de negócio

### Pessoa

- **RN01 — Nome da pessoa:** obrigatório, de 3 a 150 caracteres depois de remover os espaços das pontas.
- **RN02 — CPF válido:** 11 dígitos com dígitos verificadores corretos. Sequências de um único dígito repetido (ex: `111.111.111-11`) são inválidas. O CPF é aceito com ou sem máscara e guardado só com os dígitos.
- **RN03 — CPF único:** duas pessoas não podem ter o mesmo CPF.
- **RN04 — Remoção definitiva:** remover uma pessoa apaga a pessoa, o cartão de vacinação e todas as vacinações, sem retenção.

### Vacina

- **RN05 — Nome da vacina:** obrigatório, de 2 a 100 caracteres depois de remover os espaços das pontas, e único sem diferenciar maiúsculas de minúsculas.
- **RN06 — Esquema de doses:** de 1 a 10 doses primárias e de 0 a 5 doses de reforço.
- **RN07 — Esquema imutável:** como a vacina não é editável, o esquema não muda depois do cadastro.

### Vacinação e dose

- **RN08 — Identidade da dose:** uma dose é definida pelo tipo (primária ou reforço) e pelo número dentro do tipo, a partir de 1.
- **RN09 — Dose dentro do esquema:** dose primária de 1 até o número de doses primárias; dose de reforço de 1 até o número de doses de reforço. Uma vacina sem reforço não aceita dose de reforço.
- **RN10 — Sequência:** cada vacinação registra a próxima dose esperada. As doses primárias seguem em ordem; o primeiro reforço só vem depois da série primária completa; os reforços seguem em ordem.
- **RN11 — Duplicidade:** a mesma dose da mesma vacina não se repete para a mesma pessoa. A recusa informa que a dose já foi registrada, e não que está fora de sequência.
- **RN12 — Data de aplicação:** obrigatória, é uma data civil sem hora. Não pode ser futura nem anterior a 01/01/1900. "Hoje" é a data atual no fuso de Brasília (`America/Sao_Paulo`).
- **RN13 — Ordem cronológica:** a data de aplicação de cada dose é posterior à da dose anterior na sequência da mesma vacina. A mesma data não vale.
- **RN14 — Correção de data:** a correção altera só a data de aplicação. A nova data segue RN12 e fica entre a data da dose anterior e a da dose seguinte, quando elas existirem, sem coincidir com nenhuma das duas.
- **RN15 — Exclusão:** só a última dose de uma vacina pode ser excluída.
- **RN16 — Concorrência:** uma alteração no cartão baseada numa versão desatualizada é recusada, com orientação para recarregar o cartão.

Quando um registro viola mais de uma regra, a recusa informa a primeira nesta ordem: RN09, RN11, RN10, RN12, RN13.

### Cartão de vacinação

- **RN17 — Composição:** o cartão traz os dados da pessoa e uma entrada por vacina com pelo menos uma vacinação. As vacinas vêm ordenadas por nome e as doses na ordem da sequência. Cada entrada mostra o esquema, as doses aplicadas com a data de aplicação e o progresso: doses primárias aplicadas sobre o total, série primária completa (sim ou não) e reforços aplicados sobre o total.
- **RN18 — Rótulos de dose:** "Dose única" quando o esquema tem 1 dose primária e nenhum reforço. Nos demais casos, as doses primárias aparecem como "1ª dose", "2ª dose" e assim por diante, e os reforços como "1º reforço", "2º reforço" e assim por diante.

### Listagens

- **RN19 — Lista de pessoas:** busca por parte do nome, ignorando maiúsculas e acentos; ordem por nome; 20 pessoas por página por padrão e no máximo 100.
- **RN20 — Lista de vacinas:** busca por parte do nome; ordem por nome. Sem parâmetros de paginação, traz todas as vacinas.

### Acesso

- **RN21 — Acesso protegido:** toda funcionalidade exige sessão válida, exceto entrar, renovar a sessão e verificar a saúde do sistema.
- **RN22 — Login:** e-mail e senha. Credenciais inválidas recebem uma mensagem genérica.
- **RN23 — Sessão:** o acesso vale por 15 minutos e se renova automaticamente por até 7 dias. Cada renovação invalida a credencial de renovação anterior. Cada conta tem uma sessão ativa por vez: um novo login encerra a sessão anterior. Sair encerra a sessão.
- **RN24 — Limite de tentativas:** no máximo 5 tentativas de login por minuto a partir do mesmo IP.

### Privacidade

- **RN25 — CPF fora da URL e dos logs:** o CPF nunca aparece em URL. Os logs não registram CPF, nome nem senha.
- **RN26 — Exibição do CPF:** completo e formatado (`123.456.789-09`) só na consulta de uma pessoa e no cartão; mascarado (`***.456.789-**`) nas listas.

## 7. Interface web

- Em português, com datas no formato `dd/MM/aaaa`, componentes Angular Material e layout responsivo.
- Temas claro e escuro: o tema inicial segue a preferência do sistema, e o operador pode trocar a qualquer momento; a escolha fica salva no navegador.
- Acessibilidade no nível AA das WCAG.
- Telas:
  - **Login.**
  - **Pessoas:** lista com busca e paginação, cadastro e remoção com confirmação.
  - **Cartão da pessoa:** grade com uma linha por vacina e, em cada linha, um bloco por dose do esquema da vacina, com o rótulo da dose e a data de aplicação. Permite registrar uma dose (com a próxima dose esperada sugerida), corrigir a data de aplicação e excluir a última dose, com confirmação.
  - **Vacinas:** lista e cadastro com esquema de doses.
- Navegação lateral no desktop e barra inferior no celular, com Pessoas, Vacinas, a troca de tema e a opção "Sair".
- Mensagens: a confirmação enviada pela API aparece num aviso temporário; um erro de campo aparece junto do campo; os demais erros aparecem num aviso temporário.
- A sessão se renova em segundo plano. Quando a sessão expira, a interface volta para o login.

## 8. Requisitos não funcionais

- **RNF01 — API:** REST com JSON, versionada (`/api/v1`). Erros no formato ProblemDetails (RFC 9457), com código estável e mensagem em português. Listas com envelope de paginação.
- **RNF02 — Segurança:** token de acesso de curta duração, credencial de renovação em cookie inacessível ao JavaScript, limite de tentativas de login e rotas protegidas por padrão.
- **RNF03 — Privacidade (LGPD):** o CPF é dado pessoal e a vacinação é dado de saúde, portanto dado pessoal sensível. Exposição mínima (RN25, RN26) e remoção definitiva (RN04).
- **RNF04 — Consistência:** os dados continuam íntegros com operações simultâneas (RN11, RN16).
- **RNF05 — Execução:** um único comando (`docker compose up`) sobe banco de dados, API e interface, já com dados de demonstração.
- **RNF06 — Qualidade:** testes unitários e de integração cobrindo todas as regras de negócio, testes de arquitetura e integração contínua a cada push.
- **RNF07 — Documentação:** README com setup, rotas, exemplos de chamadas e decisões; documentação interativa da API (OpenAPI); ADRs.
- **RNF08 — Observabilidade:** verificação de saúde que inclui o banco de dados; logs estruturados.

## 9. Dados de demonstração

- Carga automática na inicialização, idempotente (só com tabelas vazias) e desligável por configuração.
- Vacinas, com esquemas simplificados do calendário do PNI. O README as identifica como dados de demonstração; o banco de dados e a interface não têm marcação própria:

  | Vacina | Doses primárias | Doses de reforço |
  |---|---|---|
  | BCG | 1 | 0 |
  | Hepatite B | 3 | 0 |
  | DTP | 3 | 2 |
  | Poliomielite inativada (VIP) | 3 | 1 |
  | Pneumocócica 10-valente | 2 | 1 |
  | Meningocócica C | 2 | 1 |
  | Tríplice viral | 2 | 0 |
  | Febre amarela | 1 | 0 |

- Uma pessoa de demonstração, com CPF fictício válido e cartão parcial.
- Uma conta de demonstração, com as credenciais no README.

## 10. Limitações conhecidas

- Os esquemas são simplificados e não substituem o calendário oficial.
- Não há vacinas recorrentes, intervalo mínimo entre doses nem registro histórico com lacuna.
- Cada conta tem uma sessão ativa por vez.

## 11. Critérios de entrega

- F01 a F10 disponíveis na API e na interface.
- Cada regra de RN01 a RN26 coberta por pelo menos um teste automatizado.
- `docker compose up` funcionando numa máquina limpa com Docker.
- CI verde e README completo.
- Entrega até o fim do dia 03/11/2026.
