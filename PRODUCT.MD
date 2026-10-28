# Desafio: Cartão de Vacinação

## Descrição do problema:

Desenvolver um sistema para gerenciar o cartão de vacinação de uma pessoa, permitindo o cadastro, consulta, atualização e exclusão das vacinas registradas.

## Fluxo do sistema:

O sistema deve permitir o cadastro de vacinas recebidas por uma pessoa, registrando informações como nome da vacina, data da aplicação e doses aplicadas.

As funcionalidades devem estar acessíveis via APIs REST, possibilitando o cadastro, consulta e exclusão das vacinas no cartão de vacinação.

## Funcionalidades:

- Cadastrar uma vacina: Uma vacina consiste em um nome e um identificador único.
- Cadastrar uma pessoa: Uma pessoa consiste em um nome e um número de identificação único.
- Remover uma pessoa: Uma pessoa pode ser removida do sistema, o que também implica na exclusão de seu cartão de vacinação e todos os registros associados.
- Cadastrar uma vacinação: Para uma pessoa cadastrada, é possível registrar uma vacinação, fornecendo informações como o identificador da vacina e a dose aplicada (A dose deve ser validada pelo sistema).
- Consultar o cartão de vacinação de uma pessoa: Permite visualizar todas as vacinas registradas no cartão de vacinação de uma pessoa, incluindo detalhes como o nome da vacina, data de aplicação e doses recebidas.
- Excluir registro de vacinação: Permite excluir um registro de vacinação específico do cartão de vacinação de uma pessoa.

## Orientações técnicas:

- A comunicação deve ser realizada via JSON, seguindo as melhores práticas para APIs REST.
- Recomenda-se utilizar boas práticas de programação para a linguagem escolhida.
- A implementação de testes unitários é encorajada para garantir a robustez do sistema.
- A implementação de autenticação na API é um bonus.

## Dicas:

- Documente todas as partes do sistema, desde o setup até as rotas da API, exemplos de chamadas e decisões arquiteturais.
- Utilize o Git para versionar o código, com commits descritivos e bem estruturados.

## Requisitos técnicos:

### Backend
- .NET 10
- MediatR
- EntityFramework
- DDD + CleanArch

### Frontend
- Angular
