# DDD-light só onde há regra de negócio

Só o módulo `People` tem modelo de domínio rico: o agregado `Person` controla as vacinações e aplica as regras de dose. `Vaccines` e `Accounts` são CRUD simples, sem entidade: a persistência passa por um DAO, e as invariantes do esquema de doses ficam no value object `DoseScheme`. O DDD aparece onde existe complexidade, sem cerimônia nos cadastros simples.

## Consequências

- `Dose`, `DoseType` e `DoseScheme` ficam no domínio de `Vaccines`, porque uma dose é uma posição no esquema de uma vacina. O domínio de `People` usa esses tipos.
