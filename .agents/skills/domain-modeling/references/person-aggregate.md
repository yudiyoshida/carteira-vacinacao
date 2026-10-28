# Agregado Person

O desenho (métodos, ordem de checagem, próxima dose esperada, correção e exclusão) está na TechSpec §5.2. Este arquivo lista os casos que os testes unitários do agregado cobrem. Cada caso vira um teste com o `[Trait]` da RN.

## RegisterVaccination

Ordem de checagem: RN09, RN11, RN10, RN12, RN13. Só o primeiro erro aparece.

| RN | Caso | Resultado |
|---|---|---|
| RN09 | dose primária acima do esquema | `vaccination.dose_outside_scheme` |
| RN09 | reforço numa vacina sem reforço | `vaccination.dose_outside_scheme` |
| RN11 | dose já registrada, mesmo que também fora de sequência | `vaccination.dose_already_registered` |
| RN10 | 2ª dose sem a 1ª | `vaccination.dose_out_of_sequence` |
| RN10 | 1º reforço antes da série primária completa | `vaccination.dose_out_of_sequence` |
| RN10 | 2º reforço sem o 1º | `vaccination.dose_out_of_sequence` |
| RN12 | data de amanhã | `vaccination.applied_on_in_future` |
| RN12 | data de hoje | aceita |
| RN12 | 31/12/1899 | `vaccination.applied_on_before_minimum` |
| RN12 | 01/01/1900 | aceita |
| RN13 | mesma data da dose anterior | `vaccination.applied_on_not_after_previous` |
| RN13 | dia seguinte ao da dose anterior | aceita |
| ordem | fora do esquema e com data futura | `vaccination.dose_outside_scheme` |
| ordem | fora de sequência e com data futura | `vaccination.dose_out_of_sequence` |
| ordem | data futura e não posterior à da dose anterior | `vaccination.applied_on_in_future` |

Cada vacina tem a própria sequência: as doses de uma não contam na outra.

## NextExpectedDose

| Situação | Próxima dose |
|---|---|
| nenhuma vacinação da vacina | 1ª dose primária |
| série primária completa, com reforço no esquema | 1º reforço |
| esquema completo | `null` |

## CorrectVaccinationDate (RN12, RN14)

| Caso | Resultado |
|---|---|
| vacinação inexistente | `vaccination.not_found` |
| nova data entre a da dose anterior e a da seguinte | aceita |
| nova data igual à da dose anterior ou à da seguinte | `vaccination.applied_on_out_of_order` |
| primeira dose, com a nova data antes da seguinte | aceita |
| última dose, com a nova data depois da anterior e até hoje | aceita |
| data futura ou anterior a 01/01/1900 | o código de RN12 |
| data futura e fora da ordem | o código de RN12, checado antes |

## RemoveVaccination (RN15)

| Caso | Resultado |
|---|---|
| última dose da vacina | removida; essa dose volta a ser a próxima esperada |
| dose com outra registrada depois dela | `vaccination.not_last_dose` |
| vacinação inexistente | `vaccination.not_found` |
