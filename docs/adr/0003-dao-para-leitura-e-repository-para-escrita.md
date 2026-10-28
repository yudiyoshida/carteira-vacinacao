# Leitura por DAO, escrita por Repository

Cada módulo declara os ports de persistência na Application. O DAO lê dados e devolve DTOs por projeção direta. Nos módulos com agregado, o Repository carrega e grava o agregado inteiro. Assim, as consultas não reidratam entidades, e toda escrita passa pelas regras do agregado. Nos módulos simples, o DAO também grava.
