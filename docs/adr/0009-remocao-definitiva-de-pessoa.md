# Remoção definitiva de pessoa

Remover uma pessoa apaga a pessoa, o cartão e as vacinações (chave estrangeira com `ON DELETE CASCADE`), sem soft delete. O enunciado pede a exclusão de todos os registros associados, e vacinação é dado de saúde, um dado pessoal sensível na LGPD: guardar esse dado depois do pedido de remoção não teria finalidade.
