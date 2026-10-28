# Erros por categoria convertidos em ProblemDetails

Domain e Application sinalizam falhas com quatro exceções por categoria (`NotFoundException`, `ConflictException`, `BusinessRuleException` e `UnauthorizedException`), cada uma com um `code` estável e uma mensagem em português. Um `IExceptionHandler` na camada HTTP converte a categoria no status (`404`, `409`, `422` e `401`) e responde em ProblemDetails (RFC 9457), com a extensão `code`. A validação de entrada responde `400`. O domínio não conhece HTTP, e o frontend e os testes comparam o `code`, não o texto.

## Opções consideradas

- **Uma exceção por razão, herdando a categoria:** mais tipos sem ganho prático, porque o `code` já distingue a razão.
- **Corpo `{ message }` com o status dentro da exceção:** acopla o domínio ao HTTP e foge do padrão RFC 9457.
