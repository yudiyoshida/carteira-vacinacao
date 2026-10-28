# Cartão de Vacinação

Registro e consulta das vacinas aplicadas em cada pessoa, com validação de cada dose contra o esquema da vacina. Os termos abaixo são a linguagem do domínio; entre parênteses, o nome usado no código.

## Linguagem

### Pessoas

**Pessoa** (`Person`):
Indivíduo titular de um cartão de vacinação, identificado por CPF.
_Evitar_: paciente, usuário, cidadão

**CPF** (`Cpf`):
Número de identificação único de uma pessoa no sistema.
_Evitar_: documento, número de identificação, RG

### Vacinas

**Vacina** (`Vaccine`):
Item do catálogo, identificado por um nome único e com um esquema de doses.
_Evitar_: imunizante, imunobiológico

**Esquema de doses** (`DoseScheme`):
Quantidade de doses primárias e de doses de reforço que uma vacina admite.
_Evitar_: protocolo, calendário

**Dose** (`Dose`):
Posição no esquema de uma vacina, definida pelo tipo (primária ou reforço) e pelo número dentro do tipo.
_Evitar_: aplicação

**Dose primária** (`Primary`):
Dose da série inicial do esquema.
_Evitar_: dose inicial

**Dose de reforço** (`Booster`):
Dose prevista para depois da série primária completa.
_Evitar_: dose adicional

**Série primária**:
Conjunto das doses primárias de uma vacina. Está completa para uma pessoa quando todas as doses primárias têm vacinação registrada.

**Dose única**:
Rótulo da dose de uma vacina cujo esquema tem uma dose primária e nenhuma dose de reforço.

### Cartão

**Vacinação** (`Vaccination`):
Registro de que uma dose de uma vacina foi aplicada em uma pessoa, numa data de aplicação.
_Evitar_: registro de vacinação, aplicação, vacina aplicada

**Data de aplicação** (`AppliedOn`):
Data civil, sem hora, em que a dose foi aplicada.
_Evitar_: data da vacina, data de registro

**Cartão de vacinação** (`VaccinationCard`):
Conjunto das vacinações de uma pessoa, agrupado por vacina.
_Evitar_: carteira de vacinação, caderneta, histórico

**Sequência de doses**:
Ordem obrigatória das doses de uma vacina: doses primárias em ordem crescente, depois doses de reforço em ordem crescente.

**Próxima dose esperada**:
Primeira dose da sequência que ainda não tem vacinação registrada para a pessoa.

**Última dose**:
Dose mais avançada na sequência entre as que já têm vacinação registrada para a pessoa, numa vacina.

### Acesso

**Operador**:
Quem usa o sistema, por meio de uma conta.
_Evitar_: usuário

**Conta** (`Account`):
Credencial de acesso de um operador. Não se confunde com pessoa.
_Evitar_: usuário, login
