# Referências de negócio do SIGH

Este pacote não contém dados pessoais reais. Os modelos de carta foram limpos (sem nomes, CPFs, matrículas, metadados de autoria ou vínculo de mala direta) e as planilhas de funcionários são fictícias.

Nunca versione no repositório a base real de funcionários nem cartas preenchidas com dados reais.

## 1. Cartas de medida disciplinar

As cartas têm duas partes: um **modelo estrutural** (`modelos-medidas/`) e um **texto de motivo** (`catalogo-motivos/`).

### 1.1 Modelos estruturais

Um modelo por tipo de medida, com cabeçalho (logotipos), destinatário, saudação, assinaturas e rodapé (CNPJ e endereços). O parágrafo que justifica a medida é substituído por `{{CORPO_MOTIVO}}`.

| Arquivo | Medida |
|---|---|
| `ADVERTENCIA.docx` | Carta de advertência |
| `SUSPENSAO.docx` | Carta de suspensão (qualquer número de dias) |
| `JUSTA_CAUSA.docx` | Carta de demissão por justa causa |

Placeholders do modelo estrutural:

| Placeholder | Origem | Formato |
|---|---|---|
| `{{NOME}}` | Funcionário (Nome) | como no cadastro |
| `{{MATRICULA}}` | Funcionário (Matrícula) | 8 dígitos |
| `{{ADMISSAO}}` | Funcionário (Data admissão) | dd/MM/yyyy |
| `{{CPF}}` | Funcionário (CPF) | 000.000.000-00 |
| `{{SETOR}}` | Funcionário (Descr.Unid.Org.) | como no cadastro |
| `{{DATA_EMISSAO}}` | Data de emissão | dd/MM/yyyy |
| `{{DATA_RECEBIMENTO}}` | "Recebi em" (padrão: data de emissão) | dd/MM/yyyy |
| `{{CORPO_MOTIVO}}` | Texto do motivo, já preenchido (ver 1.2) | ocupa um parágrafo próprio; vira um ou mais parágrafos com a mesma formatação |

Cada placeholder ocupa um único run no XML. Mesmo assim, o preenchimento deve tolerar placeholders quebrados em vários runs, porque o RH vai editar os modelos no Word.

### 1.2 Catálogo de motivos

Cada motivo de penalidade tem: código, nome, **fundamento legal** e **um texto por tipo de medida**. Um texto pode ser específico para uma quantidade de medidas anteriores (`medidasAnteriores`) ou genérico (`null`). Na geração vale o mais específico: primeiro o texto com o mesmo número de medidas anteriores e, se não houver, o genérico daquela medida.

`motivos-exemplo.json` traz **só o motivo "Faltas injustificadas"**, para demonstrar a estrutura. Ele reproduz os textos das cartas atuais. **O catálogo definitivo (todos os motivos, textos e fundamentos jurídicos) será enviado depois pelo RH**, e deve ser mantido no sistema por uma tela, sem depender de programador.

Formatação do texto: uma linha em branco (`\n\n`) separa parágrafos com um parágrafo vazio entre eles; uma quebra simples (`\n`) começa um novo parágrafo, sem espaço.

Placeholders que podem aparecer no texto do motivo:

| Placeholder | Valor | Exemplo |
|---|---|---|
| `{{PERIODO_FALTAS}}` | Datas das ocorrências do processo; dias consecutivos agrupados | "06/11/2026 e 07/11/2026"; "03/11/2026 a 05/11/2026" |
| `{{PERIODO_SUSPENSAO}}` | Período da suspensão | "10/11/2026"; "10/11/2026 a 12/11/2026" |
| `{{DIAS_SUSPENSAO_EXTENSO}}` | Dias de suspensão por extenso, **masculino** | "um (1) dia", "três (3) dias" |
| `{{QTD_MEDIDAS_ANTERIORES_EXTENSO}}` | Medidas anteriores (advertências + suspensões) por extenso, **feminino** | "uma (1) vez", "duas (2) vezes" |
| `{{FUNDAMENTO_LEGAL}}` | Fundamento legal do motivo | artigo 482 da C.L.T., letra "E" (...) |

Também podem aparecer no texto do motivo os placeholders do modelo estrutural (por exemplo `{{NOME}}`).

### 1.3 Resultado esperado

Modelo estrutural + texto do catálogo reproduzem as cartas atuais. A pasta `referencia-visual/` mostra as 5 combinações, todas em 1 página, com dados fictícios:

| Imagem | Medida | Medidas anteriores | Dias |
|---|---|---|---|
| `ADVERTENCIA.png` | Advertência | 0 | — |
| `SUSPENSAO_1_DIA.png` | Suspensão | 1 (usa o texto específico) | 1 |
| `SUSPENSAO_3_DIAS.png` | Suspensão | 2 (texto genérico) | 3 |
| `SUSPENSAO_5_DIAS.png` | Suspensão | 3 (texto genérico) | 5 |
| `JUSTA_CAUSA.png` | Justa causa | 4 | — |

Comparadas com as cartas originais, a única diferença de texto é na suspensão de 5 dias: a original dizia "continuou a faltar" e a genérica diz "tenha continuado a faltar". Os textos finais virão do RH.

### 1.4 Correções feitas nos textos originais

- "Prezador (a) Senhor (a)" → "Prezado (a) Senhor (a)".
- "infringindo desta forma a disposto" → "o disposto".

Os textos devem ser validados pelo jurídico antes do uso em produção.

### 1.5 Fontes

Os modelos usam Arial, Times New Roman, Calibri e Tahoma. Na conversão por LibreOffice em Linux, as fontes métricas-compatíveis (Liberation Sans/Serif, Carlito) mantêm o layout em 1 página.

## 2. Base de funcionários (`importacao-funcionarios/`)

A base real é uma exportação do SAP: 1 aba (`Data`), 72 colunas, todas as células como texto. Perfil da exportação real (sem dados pessoais):

- Cerca de 4.700 linhas na amostra recebida, que tinha apenas os Status `Ativo` e `Afastado`. **A exportação completa, usada na carga real, também trará os desligados**, com `Data demissão` preenchida e um Status próprio (o valor exato ainda será confirmado).
- `Matrícula`: sempre 8 dígitos, única.
- `CPF`: sempre 11 dígitos, sem máscara, pode começar com zero. Na base real, 1 CPF tem dígito verificador inválido.
- `Grupo_Emp`: `Campo` ou `Industria`.
- `Chave Organização`: `USGA131` a `USGA141`.
- Datas no formato dd/MM/yyyy.
- O cabeçalho `Estado` aparece **duas vezes** (UF do RG e UF do endereço). O mapeamento por nome não pode quebrar com isso.

**Os arquivos reais desta pasta são só amostras de formato.** A carga real usará a exportação completa, que pode ter valores de Status além de `Ativo` e `Afastado`. O importador deve: mapear `Ativo`, `Afastado` e qualquer valor que indique desligamento (por exemplo `Desligado`, `Demitido`) e rejeitar a linha com mensagem clara se aparecer um status desconhecido, nunca adivinhar.

### Colunas que o SIGH importa (lista fechada)

| Coluna SAP | Uso no SIGH |
|---|---|
| `Matrícula` | Chave do funcionário |
| `Nome` | Nome |
| `CPF` | CPF (validar dígitos) |
| `Status` | Ativo / Afastado |
| `Grupo_Emp` | Campo / Indústria |
| `Cargo`, `Descr.Cargo` | Cargo (código e descrição) |
| `Uni.Org`, `Descr.Unid.Org.` | Setor (código e descrição) — é o "Setor" das cartas |
| `Data admissão` | Admissão |
| `Data demissão` | Demissão, se vier preenchida |
| `Descrição Turma` | Turma (só Campo) |
| `Chave Organização` | Unidade organizacional |

**Todas as outras colunas são ignoradas e nunca são gravadas nem logadas.** Entre elas há dados que o SIGH não precisa e que a LGPD manda não coletar: raça/cor, conta bancária, filiação, endereço, RG, CNH, PIS, título de eleitor etc.

### Arquivos fictícios para teste

- `funcionarios_carga_inicial.xlsx`: 40 funcionários, mesmo cabeçalho (72 colunas, `Estado` duplicado). As colunas sensíveis estão preenchidas com valores obviamente falsos, para o teste provar que o importador não as lê.
- `funcionarios_atualizacao.xlsx`: a mesma base com alterações. Resultado esperado ao importar depois da carga inicial:

| Matrícula | Situação | Resultado esperado |
|---|---|---|
| 50090010 | Ausente do arquivo | Marcado como desligado (nunca apagado) |
| 50090000 | Setor mudou | Atualizado, com auditoria do campo |
| 50090001 | Status Ativo → Afastado | Atualizado |
| 50090002 | Nome corrigido | Atualizado |
| 59999998 | Novo | Criado |
| 59999999 | CPF inválido | Linha rejeitada com erro |
| 59999997 | Nome vazio | Linha rejeitada com erro |
| 59999996 | CPF igual ao da 50090007 | Linha rejeitada com erro |
| 50090005 | CPF começa com zero | Zero preservado |
| demais | Sem mudança | Nenhuma alteração |

## 3. Base disciplinar atual (para o sprint de importação do histórico)

O acompanhamento das penalidades hoje é feito numa planilha com 1 aba e 14 colunas. A amostra recebida é incompleta; a base completa virá na importação real.

| Coluna | Tipo observado | Observação |
|---|---|---|
| `MATRÍCULA` | número | 8 dígitos; liga ao funcionário |
| `DATA` | data | data da medida |
| `TIPO DA INFRAÇÃO` | texto | ex.: "ADVERTÊNCIA ESCRITA", "SUSPENSÃO 1" (o "1" é rótulo, não o número de dias) |
| `MOTIVO DA INFRAÇÃO` | texto | ex.: faltas injustificadas, falta de uso de EPI, negligência ou imprudência, outros |
| `DIAS DE SUSPENSÃO` | número ou vazio | dias reais da suspensão |
| `PERÍODO DA SUSPENSÃO` | texto | "dd/MM/yyyy À dd/MM/yyyy" (com acento) ou vazio |
| `NOME DO FUNCIONÁRIO`, `CARGO`, `SETOR` | texto | cópia do cadastro na época |
| `DATA FIM DA SUSPENSÃO` | data ou vazio | |
| `GERÊNCIA` | texto | nome da gerência; corresponde 1:1 à Chave Organização (ex.: "AGRÍCOLA URBANO" = USGA137) |
| `INFRAÇÃO` | texto | "ADVERTÊNCIA" ou "SUSPENSÃO" |
| `Nº ADVERTÊNCIA`, `Nº SUSPENSÃO` | número | contadores manuais; **não devem ser importados**, o sistema recalcula |

Inconsistências encontradas na amostra (a importação deve detectá-las e mostrá-las na pré-visualização): contadores que não batem com a contagem real; suspensões sem dias nem período; advertência com dias de suspensão preenchidos.
