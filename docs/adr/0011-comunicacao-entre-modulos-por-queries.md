# Comunicação entre módulos só por queries do MediatR

Um módulo usa outro só enviando queries do MediatR (`ISender.Send`) e recebendo DTOs. Nunca acessa DAO, Repository, registros de persistência ou tabelas de outro módulo, e commands não cruzam módulos. O cartão de vacinação, por exemplo, junta os dados de `People` com uma query de `Vaccines` para os nomes e esquemas das vacinas, em vez de um join entre tabelas dos dois módulos. As fronteiras ficam explícitas e verificáveis por teste de arquitetura, ao custo de uma consulta a mais em algumas leituras.
