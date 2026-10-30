# Vacinações no agregado da pessoa, sem módulo próprio

As vacinações ficam no módulo `People`, como filhas do agregado `Person`. As regras de dose dependem das outras vacinações da mesma pessoa: sequência, duplicidade, ordem das datas, correção entre as doses vizinhas e exclusão só da última dose. O agregado é a fronteira de consistência: cada operação lê o cartão da pessoa, aplica as regras e grava numa transação. Por isso, toda mudança de vacinação passa por um método da raiz (`RegisterVaccination`, `CorrectVaccinationDate` e `RemoveVaccination`). A raiz é a pessoa porque o cartão pertence a ela e o controle de concorrência vale para o cartão inteiro.

## Opções consideradas

- **Módulo próprio de vacinações**, com um agregado do cartão que referencia a pessoa pelo id: `People` viraria um cadastro de nome e CPF. Como commands não cruzam módulos, a remoção da pessoa dependeria de uma cascata no banco de dados sobre a tabela de outro módulo ou de eventos de domínio, que a stack não tem. A versão do cartão pediria uma tabela própria, e o registro conferiria a pessoa por query, fora da transação que grava a vacinação. A leitura do cartão passaria a depender de dois módulos externos.
- **Um agregado por pessoa e vacina:** é a menor fronteira que as regras de dose exigem, mas o cartão é lido e versionado inteiro.

## Consequências

- Remover a pessoa apaga as vacinações por uma cascata dentro do próprio módulo.
- A versão da pessoa protege o cartão inteiro contra alterações concorrentes.
- Cada gravação carrega todas as vacinações da pessoa, um volume pequeno: dezenas de registros.
- Uma futura edição dos dados da pessoa disputaria a mesma versão com o registro de uma dose. Hoje a edição de pessoa está fora do escopo.
- Um módulo próprio passa a valer a pena se a vacinação ganhar dados e ciclo de vida próprios, como lote, fabricante, local de aplicação ou agendamento, ou se a pessoa ganhar regras que disputem a versão do cartão.
