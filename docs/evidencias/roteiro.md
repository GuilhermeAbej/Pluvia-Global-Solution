# Roteiro dos prints

Suba a API (`dotnet run --project src/Alaga.Api`), abra `http://localhost:5080/swagger` e use dados fictícios.
Salve cada print nesta pasta com o nome da tabela "Evidências de funcionamento" do README.

Cabeçalhos: clique em **Authorize** (cadeado no topo do Swagger) e preencha `X-Usuario-Id` com o id devolvido no passo 3.
Para ações oficiais, preencha também `X-Chave-Oficial` = `chave-oficial-demo`. Para provar o 401/403, clique em Logout e repita a chamada.

Corpos usados:

- Usuário: `{ "nome": "Demo Alaga", "consentimentoLgpd": true }`
- Local: `{ "nome": "Casa (demo)", "lat": -23.5652, "lng": -46.6502, "raioMetros": 500 }`
- Sensor: `{ "codigo": "ESP32-001", "nivelCm": 120 }` e depois `130`; para encerrar, `20` e `25`
- Consultas: `lat=-23.565&lng=-46.65&raioMetros=1000`
- Região sem dados: `lat=-22.9&lng=-43.2&raioMetros=1000`

A ordem importa: o sensor só abre a ocorrência na segunda leitura seguida acima de 80 cm
e só encerra na segunda leitura seguida abaixo.
