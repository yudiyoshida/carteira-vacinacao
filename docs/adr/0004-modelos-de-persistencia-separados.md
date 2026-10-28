# Modelos de persistência separados das entidades de domínio

O EF Core mapeia classes de persistência próprias (`PersonRecord`, `VaccinationRecord`), que ficam na Infra. O Repository converte esses registros em entidades com `PersonFactory.Load` e, ao gravar, aplica nos registros as mudanças do agregado. Domain e Application não referenciam o EF Core, e as entidades não fazem nenhuma concessão ao ORM: nada de construtor sem parâmetros, setters públicos ou campos só para mapeamento.

## Opções consideradas

- **EF Core mapeando as entidades de domínio direto:** menos código e change tracking automático, mas o domínio passa a ter detalhes exigidos pelo ORM.

## Consequências

- Mais código de mapeamento. O `EditAsync` do Repository compara as vacinações dos registros com as do agregado.
- A versão usada no controle de concorrência do cartão fica só no registro de persistência, fora do domínio.
