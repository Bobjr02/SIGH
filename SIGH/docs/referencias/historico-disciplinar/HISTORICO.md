# Importação do histórico disciplinar — arquivo fictício e resultados esperados

`base_disciplinar_ficticia.xlsx` reproduz o formato da planilha de acompanhamento de penalidades usada hoje pelo RH (ver LEIAME.md, seção 3): aba `Planilha1`, 14 colunas, `MATRÍCULA` como número e `DATA` como data do Excel. Todos os dados são fictícios e usam as matrículas de `funcionarios_carga_inicial.xlsx`.

**Pré-requisito do teste:** importar antes `funcionarios_carga_inicial.xlsx`. O histórico só se liga a funcionários que já existem no SIGH.

**A planilha real é maior e virá completa na importação real.** Este arquivo só reproduz os formatos e as inconsistências encontradas na amostra.

## Mapeamentos

- `INFRAÇÃO` = "ADVERTÊNCIA" → advertência escrita (WrittenWarning). Se `TIPO DA INFRAÇÃO` contiver "VERBAL", advertência verbal.
- `INFRAÇÃO` = "SUSPENSÃO" → suspensão. O "1" de "SUSPENSÃO 1" é só rótulo.
- `INFRAÇÃO` ou `TIPO DA INFRAÇÃO` contendo "JUSTA CAUSA" → demissão por justa causa (JustCauseTermination).
- `GERÊNCIA`: nome comparado (sem acento e sem diferença de maiúsculas) com os nomes das 9 gerências do SIGH. Vira a gerência da época do registro.
- `MOTIVO DA INFRAÇÃO` → tipo de infração, pelo nome normalizado. Motivo inexistente no SIGH é listado na pré-visualização e criado na confirmação.
- Período da suspensão, em ordem de prioridade:
  1. `PERÍODO DA SUSPENSÃO`, aceitando "À", "à", "A", "a" ou "até" entre as datas;
  2. `DATA` até `DATA FIM DA SUSPENSÃO`;
  3. sem período.
- `Nº ADVERTÊNCIA` e `Nº SUSPENSÃO` **não são importados**. O sistema recalcula a contagem e avisa quando diverge.

## Resultado esperado

| Linha do Excel | Matrícula | Situação | Resultado esperado |
|---|---|---|---|
| 2 | 50090007 | Advertência normal | Importada |
| 3 | 50090007 | Suspensão de 1 dia | Importada; período 15/04/2025 |
| 4 | 50090007 | Suspensão de 3 dias | Importada; período 02/06/2025 a 04/06/2025 |
| 5 | 50090006 | Advertência por EPI | Importada |
| 6 | 50090006 | Advertência com `Nº ADVERTÊNCIA` = 1, mas é a 2ª | Importada; **aviso** de contador divergente (planilha 1, recalculado 2) |
| 7 | 50090000 | Suspensão sem dias e sem período | Importada; **aviso** "suspensão sem período"; dias desconhecidos (não zero) |
| 8 | 50090003 | Advertência com `DIAS DE SUSPENSÃO` = 1 | Importada como advertência; **aviso**; os dias são ignorados |
| 9 | 50090008 | Suspensão sem período, com DATA FIM | Importada; período 12/01/2026 a 14/01/2026, vindo de DATA e DATA FIM |
| 10 | 50090009 | Motivo "USO DE CELULAR EM SERVIÇO" (não existe) | Pré-visualização lista o motivo novo; na confirmação ele é criado e a linha importada |
| 11 | 50090012 | `GERÊNCIA` = MANUTENÇÃO, mas o funcionário está hoje na USGA131 (Industrial) | Importada; **aviso**; gerência da época = USGA132 |
| 12 | 59990001 | Matrícula inexistente no SIGH | **Rejeitada**: "funcionário não encontrado — importe a base completa do SAP antes" |
| 13 | 50090007 | Cópia exata da linha 2 | **Rejeitada** como duplicada dentro do arquivo |
| 14 | 50090011 | Demissão por justa causa | Importada como justa causa |
| 15 | 50090004 | `DATA` gravada como texto "15/02/2026" | Importada; data lida corretamente |
| 16 | 50090005 | Período com "a" minúsculo | Importada; período 03/03/2026 a 05/03/2026 |
| 17 | 50090002 | `DIAS` = 3, mas o período tem 5 dias | Importada com o período (5 dias); **aviso** de divergência |
| 18 | — | Linha totalmente vazia | Ignorada, sem erro |

Totais esperados: 16 linhas com dados, 14 importadas, 2 rejeitadas e 1 motivo novo.

Avisos esperados: linhas 6, 7, 8, 11 e 17, além do aviso de motivo novo da linha 10.

Depois da importação, o apoio à decisão da matrícula 50090007 mostra 1 advertência, 2 suspensões e 4 dias de suspensão, todos identificados como histórico importado.

Reimportar o mesmo arquivo não cria nada novo.
