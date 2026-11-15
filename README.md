# Pluvia: monitoramento e prevenção inteligente de alagamentos

Global Solution 2026.2 · CitizenTech · Engenharia de Software · FIAP
Disciplina: **C# Software Development**

## Integrantes

| Nome | RM |
|------|----|
| Eduardo Dallabella Lima | RM556803 |
| Guilherme Abe | RM554743 |
| Gustavo Ruiz Vieira Paulino | RM554779 |
| Heloísa Real | RM554535 |
| Victor Pacifico Dias | RM558017 |

## Ficha da CitizenTech

| Campo | Conteúdo |
|-------|----------|
| **Nome da CitizenTech** | Pluvia |
| **Cidade e bairro** | São Paulo (região de demonstração; coordenadas fictícias) |
| **Cidadão beneficiado** | Motoristas, motociclistas, entregadores, quem estaciona na rua, passageiros e moradores de áreas vulneráveis |
| **Problema, em uma frase** | Durante chuvas fortes, pessoas entram em vias inundadas sem aviso, deixam o veículo em local vulnerável ou não sabem se um ponto de alagamento continua ativo, porque a informação é dispersa e pouco contextualizada. |
| **Como funciona** | A API reúne ocorrências de fontes identificadas (oficial e sensores), relatos da comunidade com triagem de IA e validação, e estimativa de risco por histórico. Avisa quem monitora um local ou um veículo estacionado, sem tratar "sem ponto no mapa" como "seguro". |
| **O que cada parte faz** | Ver "Como as partes se encaixam" |
| **Link do repositório** | https://github.com/GuilhermeAbej/Pluvia-Global-Solution
|
| **Indicador de impacto** | Confiabilidade da informação (percentual de eventos com fonte e horário identificados e status corretamente atualizado) e rapidez dos avisos (tempo entre registrar um evento de teste e o alerta ficar disponível) |
| **ODS conectados** | 11 (Cidades e comunidades sustentáveis), 13 (Ação contra a mudança global do clima), 9 (Indústria, inovação e infraestrutura) |

### O que a solução não faz

O Pluvia não promete prever cada enchente e não substitui comunicados oficiais. Ele transforma informação dispersa em alertas
localizados e transparentes. Não afirma que uma rua está liberada e não recomenda atravessar água.

### Como as partes se encaixam

| Disciplina | Parte do Pluvia |
|------------|----------------|
| C# Software Development | Esta API (backend) |
| SOA e Web Services | Contratos REST, domínio, persistência, migrações, Swagger e testes de integração |
| Mobile Development & IoT | App React Native + Expo: mapa, relato com foto, local do carro, avisos |
| Physical Computing: IoT & IoB | ESP32 no Wokwi enviando `POST /api/sensores/leituras` |
| IA e Machine Learning | Modelo de risco com dados públicos (`IEstimadorDeRisco`) e análise de imagem (`IAnalisadorDeImagem`) |
| Cybersecurity | Mapeamento de dados pessoais em fotos e localização, taint analysis e dashboard de Risco Cidadão |
| Operating Systems | Linux com provisionamento, serviço da aplicação, scripts de monitoramento, logs e simulação de falhas |
| Testing, Compliance & QA | Features com critérios de aceite e casos de teste no Azure Boards |

## Estados que a API diferencia 

| Estado | Como aparece na API |
|--------|---------------------|
| Confirmado • ativo | `Ocorrencia` com `status = Ativa`, registrada por uma **fonte identificada** (oficial ou sensor) |
| Encerrado | `status = Encerrada`, só pela mesma fonte que registrou, com o rótulo "a condição física precisa ser reavaliada" |
| Relato em análise | `Relato` com `status = EmAnalise`: não aparece no mapa e nunca é registro oficial |
| Relato publicado | `status = Publicado`, exibido separado como "possível alagamento • relato comunitário (não é registro oficial)" |
| Risco estimado | `GET /api/riscos` e `/api/locais-monitorados/{id}/risco`, separado das ocorrências, nunca confirma água na pista |
| Sem dados recentes | `statusDosDados = SemDadosRecentes` na lista de ocorrências, e ocorrência ativa sem nova informação da fonte fica marcada. Nunca sinal de segurança |

## Decisões de produto e onde estão no código

| Decisão do documento | Como o código garante |
|----------------------|-----------------------|
| "Sem ponto no mapa" não é "seguro" | `ListaDeOcorrenciasResponse.Aviso` e `RiscoEstimado.Indeterminado` dizem isso por escrito |
| Encerramento não afirma que a via está segura | `MensagemDeAlerta.Encerramento` traz fonte e horário e avisa que não garante |
| Só a mesma fonte encerra | `Ocorrencia.Encerrar(fonteId)` recusa outra fonte; a chave oficial só atua em fontes do tipo Oficial |
| A IA só ajuda na triagem | `PoliticaDeTriagem` só define prioridade. Publicar depende de `PoliticaDeValidacao`, que não usa a IA |
| Relato comunitário separado do oficial | Entidades e endpoints diferentes (`/api/ocorrencias` x `/api/relatos/comunitarios`) |
| Critérios de validação do relato | Fonte identificada com ocorrência ativa perto, ou 3 pessoas diferentes no mesmo ponto (configurável), ou moderação |
| Fotos protegidas | Metadados (EXIF, GPS) removidos antes de salvar; nome aleatório; só o autor ou a fonte oficial veem; autor pode excluir o relato e a foto some |
| Privacidade do veículo | `VeiculoService` devolve 403 para quem não é o dono |
| Moderação e fraude | 5 relatos por hora por usuário (429), duplicidade do mesmo usuário no mesmo ponto (422), relatos antigos recusados (máx. 12 h) |
| Notificações sem repetição e silenciáveis | Índice único (usuário, ocorrência, tipo), raio por local, silêncio de até 24 h, avisos do veículo desligáveis |
| Nunca exigir entrar na água para tirar o carro | `MensagemDeAlerta.NovoEventoVeiculo` |
| Aviso de mudança de risco | `GeradorDeAlertas` avisa quando a estimativa de um local sobe para Moderado ou Alto |

## Arquitetura

```
src/
  Pluvia.Domain/   Entidades, regras e interfaces de estratégia (sem dependência de framework)
  Pluvia.Api/      Controllers, serviços, EF Core, DTOs, infraestrutura
tests/
  Pluvia.Tests/    Testes unitários e de serviço (xUnit, SQLite em memória)
```

```
Sensor ─┐
Oficial ┴─> GestorDeOcorrencias ──> Ocorrencia (Ativa/Encerrada) + HistoricoStatus
                     │
                     └─> GeradorDeAlertas ──> locais e veículos perto, sem silêncio, sem repetir ──> INotificador (log)

Relato (+ foto sanitizada + triagem IA) ──> EmAnalise ──(PoliticaDeValidacao)──> Publicado
```

Entidades do documento: Ocorrência, Fonte, HistóricoStatus, Relato, ÁreaMonitorada (local monitorado), LeituraSensor e Alerta
têm tabela própria. **Foto** e **AvaliaçãoIA** ficam dentro de `Relato` (nome do arquivo e objeto de valor `AvaliacaoImagem`),
decisão de simplificação do MVP.


## Como executar

Pré-requisito: [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/GuilhermeAbej/pluvia.git
cd pluvia
dotnet run --project src/Pluvia.Api
```

A API sobe em `http://localhost:5080` e o Swagger em `http://localhost:5080/swagger`.
O banco SQLite (`pluvia.db`) é criado ao lado do projeto `Pluvia.Api` no primeiro uso, com duas fontes
(uma oficial simulada e o sensor) e o sensor `ESP32-001`. Para recomeçar do zero, pare a API e apague `src/Pluvia.Api/pluvia.db`
(e a pasta `fotos`, se existir). Se você rodou uma versão anterior do projeto, apague esse arquivo antes.

Testes: `dotnet test`. O arquivo `src/Pluvia.Api/pluvia.http` tem a sequência de demonstração (extensão REST Client do VS Code).

### Identificação no Swagger

Não há login real. O usuário é identificado pelo cabeçalho `X-Usuario-Id` (id devolvido em `POST /api/usuarios`).
A fonte oficial e a moderação usam `X-Chave-Oficial: chave-oficial-demo`.

No Swagger, clique em **Authorize** (cadeado no topo), preencha `X-Usuario-Id` com o id do usuário e, para ações oficiais, `X-Chave-Oficial`. Os cabeçalhos passam a ser enviados em todas as chamadas.

## Endpoints

| Método | Rota | Quem | Descrição |
|--------|------|------|-----------|
| POST | `/api/usuarios` | qualquer | Cadastro com consentimento LGPD |
| GET | `/api/fontes` | qualquer | Fontes que registram ocorrências |
| POST | `/api/sensores/leituras` | sensor | Nível da água em cm; abre, atualiza ou encerra ocorrência da fonte do sensor |
| GET | `/api/ocorrencias?lat&lng&raioMetros&status&fonteId&desde&ate&todos` | qualquer | Ocorrências por perto, com `statusDosDados`. Padrão: só ativas |
| GET | `/api/ocorrencias/{id}` | qualquer | Detalhe |
| GET | `/api/ocorrencias/{id}/historico` | qualquer | Mudanças de status e fonte de cada uma |
| POST | `/api/ocorrencias` | oficial | Fonte oficial registra ocorrência ativa |
| POST | `/api/ocorrencias/{id}/encerramento` | oficial | A mesma fonte oficial encerra |
| POST | `/api/relatos` | usuário | Relato com foto opcional (multipart). Nasce em análise |
| GET | `/api/relatos/meus` · `/api/relatos/{id}` | autor | Acompanha o status |
| DELETE | `/api/relatos/{id}` | autor | Exclui o relato e a foto |
| GET | `/api/relatos/comunitarios?lat&lng&raioMetros` | qualquer | Relatos publicados, separados dos oficiais |
| GET | `/api/relatos/{id}/foto` | autor ou oficial | Foto protegida |
| POST | `/api/relatos/{id}/publicacao` · `/rejeicao` | oficial | Moderação |
| POST | `/api/fotos/analise` | usuário | Resultado da triagem de IA, sem criar relato |
| POST / GET | `/api/locais-monitorados` | usuário | Cadastra e lista locais (raio de 100 a 2000 m) |
| GET | `/api/locais-monitorados/{id}/risco` | dono | Risco estimado do local |
| POST / DELETE | `/api/locais-monitorados/{id}/silencio` | dono | Silencia (até 24 h) ou reativa |
| GET | `/api/riscos?lat&lng&raioMetros` | qualquer | Exposição estimada de um ponto ("onde estacionar") |
| GET | `/api/alertas` | usuário | Avisos, com fonte, horário e link do evento |
| POST | `/api/veiculos` | usuário | Veículo e raio de monitoramento |
| PUT / GET | `/api/veiculos/{id}/posicao` | dono | Salva e consulta onde o carro está |
| PUT | `/api/veiculos/{id}/avisos` | dono | Liga ou desliga os avisos do veículo |

## Premissas e limitações (o que é simulado ou ficou de fora)

| Item | Situação |
|------|----------|
| Análise de foto | **Simulada.** `AnalisadorDeImagemSimulado` escolhe uma das 3 categorias do documento a partir do hash do arquivo, sem olhar a imagem. Um modelo real implementa `IAnalisadorDeImagem` |
| Qualidade da foto | Valida formato, tamanho mínimo e máximo e estrutura. **Não verifica nitidez** |
| Privacidade da foto | Remove EXIF e metadados. **Não desfoca placas nem rostos**; por isso a foto não é pública, só autor e fonte oficial |
| Fonte oficial (CGE-SP) | **Simulada** por `POST /api/ocorrencias` com chave oficial. Não há integração real |
| Notificação | **Só log no console**, com a mensagem completa para a demonstração. Push entra em `INotificador` |
| Estimativa de risco | Regra por histórico do próprio Pluvia (0 = baixo, 1 a 2 = moderado, 3 ou mais = alto, mínimo de 30 dias). **Não é o modelo de ML** do documento e não usa chuva (INMET) |
| Verificação de status | Acontece no momento de cada evento. **Não há job periódico** |
| Busca por bairro ou endereço | Fica no app. A API recebe latitude e longitude |
| Autenticação e HTTPS | Simuladas por cabeçalhos e HTTP local. Produção exige JWT, HTTPS e política de retenção |
| Números (3 pessoas, 200 m, 2 h, 5 relatos por hora, 12 h, 60 min, 2 leituras, limite de 80 cm) | **Premissas de projeto** em `appsettings.json`. Não foram medidos em campo |
| Relato em análise | Só é reavaliado quando chega outro relato do ponto ou por moderação. Relato enviado antes de um evento oficial não é publicado sozinho depois |

## Dados

Todos os dados são **simulados**. Nenhum dado pessoal real é utilizado.
A posição do sensor de exemplo (`DbInicializador`) é fictícia, escolhida só para a demonstração.

## Evidências de funcionamento

Os prints estão em `docs/evidencias/`. O fluxo abaixo foi executado de ponta a ponta pelo Swagger, com dados fictícios, e cada passo tem a resposta real da API. Ele mostra também por que o alerta chega ao usuário: a API gera o aviso, guarda no banco e o expõe em `GET /api/alertas`; o envio ativo (push) entra em `INotificador`.

| # | Passo | Resultado esperado | Print |
|---|-------|--------------------|-------|
| 1 | Swagger aberto em `/swagger` | 10 grupos e 23 caminhos | `01-swagger.png` |
| 2 | `POST /api/usuarios` sem consentimento | 422 "É necessário aceitar o tratamento de dados (LGPD)" | `02-usuario-422.png` |
| 3 | `POST /api/usuarios` com `consentimentoLgpd: true` | 201 com `id` | `03-usuario-201.png` |
| 4 | `POST /api/locais-monitorados` perto do sensor | 201, `riscoAtual: "Indeterminado"` | `04-local.png` |
| 5 | `GET /api/riscos` sem histórico | "Indeterminado" e aviso de que isso NÃO significa local seguro | `05-risco-indeterminado.png` |
| 6 | `GET /api/ocorrencias` numa região sem dados | `statusDosDados: "SemDadosRecentes"` | `06-sem-dados.png` |
| 7 | `POST /api/sensores/leituras` com 120 cm | 201, "Leitura registrada.", sem ocorrência (pico isolado não dispara) | `07-leitura-1.png` |
| 8 | `POST /api/sensores/leituras` com 130 cm | 201, "Alagamento ativo registrado pelo sensor.", `ocorrenciaId` | `08-leitura-2-abre.png` |
| 9 | `GET /api/ocorrencias` perto do sensor | ocorrência "Ativa", rótulo "Confirmado • ativo", fonte e horário | `09-ocorrencia.png` |
| 10 | `GET /api/alertas` com `X-Usuario-Id` | alerta `NovoEvento` com fonte, horário e link | `10-alerta-novo.png` |
| 11 | `GET /api/ocorrencias/{id}/historico` | mudança de status com a fonte | `11-historico.png` |
| 12 | Duas leituras de 20 e 25 cm | "Ocorrência encerrada pela fonte." | `12-encerramento.png` |
| 13 | `GET /api/alertas` de novo | alerta `Encerramento`: "Isso não garante que a via esteja livre ou segura" | `13-alerta-encerramento.png` |
| 14 | `POST /api/ocorrencias` sem `X-Chave-Oficial` | 403 | `14-sem-chave-403.png` |
| 15 | `GET /api/alertas` sem `X-Usuario-Id` | 401 | `15-sem-usuario-401.png` |
| 16 | `POST /api/relatos` (multipart, com foto) | status "EmAnalise", prioridade e metadados removidos | `16-relato.png` |
| 17 | `dotnet test` | 84 testes passando | `17-testes.png` |

Regras que o fluxo comprova: só uma fonte confirma e encerra uma ocorrência; é preciso duas leituras seguidas do sensor para abrir ou encerrar; a ausência de dados nunca é tratada como segurança; o alerta de encerramento não promete que a via está livre.
