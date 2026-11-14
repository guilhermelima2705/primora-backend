# Primora — Ambiente de backend

Ambiente local para ASP.NET Core Web API (.NET 10), C#, EF Core e PostgreSQL. Decisão inicial: cada unidade representa um tenant. O frontend permanece em uma pasta e repositório separados.

## Ferramentas

- .NET SDK 10.0.403, fixado em global.json com atualização de patch permitida.
- Runtime ASP.NET Core 10.0.14.
- Ferramenta local dotnet-ef 10.0.14, registrada em dotnet-tools.json.
- PostgreSQL 18.6 no Docker Desktop, com imagem fixada pelo digest no compose.yaml.
- Visual Studio Community 2026 18.11.3 com carga ASP.NET e desenvolvimento Web: D:\Desenvolvimento\VisualStudio\2026\Community.
- Certificado HTTPS de desenvolvimento de localhost configurado.

## Banco local

| Campo | Valor |
| --- | --- |
| Servidor | 127.0.0.1 |
| Porta | 5433 |
| Banco | primora |
| Usuário da aplicação | primora_app |
| Contêiner | primora-postgres |
| Volume persistente | primora_postgres_data |

As senhas aleatórias ficam em .secrets/, com acesso restrito ao usuário do Windows e SYSTEM. Não publique essa pasta. A senha de primora_app está em .secrets/primora_app_password; a de administração está em .secrets/postgres_admin_password. Não há senha em compose.yaml.

O usuário primora_app não é superusuário, não cria bancos nem usuários e é proprietário apenas do banco Primora para permitir migrations locais. Ao preparar produção, separar as credenciais de migrations e de execução da API.

## Comandos de uso

Abra um novo terminal PowerShell nesta pasta:

```powershell
docker compose up -d --wait
docker compose ps
dotnet --version
dotnet tool restore
dotnet ef --version
```

Para carregar a conexão e o ambiente Development no terminal atual, sem exibir a senha:

```powershell
. .\scripts\Set-PrimoraEnvironment.ps1
```

O comando define ConnectionStrings__Primora, que o ASP.NET Core pode ler como ConnectionStrings:Primora. Execute-o no terminal em que iniciar a futura API. Para iniciar pela interface do Visual Studio, será necessário configurar a mesma conexão em User Secrets do futuro projeto Web, sem gravá-la em appsettings versionado.

Para abrir um console SQL administrativo dentro do contêiner:

```powershell
docker compose exec postgres psql -U primora_admin -d primora
```

Para interromper o banco mantendo os dados:

```powershell
docker compose stop
```

Para reiniciar:

```powershell
docker compose start
```

O volume preserva os dados após parar ou recriar o contêiner. Não remova o volume para uma parada normal. Os scripts de infra/postgres/init rodam somente na primeira inicialização de um volume vazio; alterar o arquivo de senha não altera automaticamente a senha já gravada no banco.

## Validação do ambiente

A pasta .checks/ contém somente uma aplicação temporária de diagnóstico, ignorada pelo Git: não é a implementação da API do Primora. Ela usa EF Core 10.0.14, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 e o banco local para executar SELECT 1 e verificar HTTP/HTTPS. Não cria tabelas nem importa dados.

```powershell
. .\scripts\Set-PrimoraEnvironment.ps1
dotnet run --project .checks/EnvironmentCheck/EnvironmentCheck.csproj --no-launch-profile --urls 'https://localhost:7246;http://127.0.0.1:5246'
```

Confira /health no endereço informado. Essa aplicação não precisa permanecer em execução após a validação.

## Planejamento registrado na preparação do ambiente

Criar a solução e os projetos da API, domínio, aplicação, infraestrutura e importador; implementar migrations e contratos aprovados. Nenhum repositório Git ou remoto foi criado nesta preparação. Não há tabelas de negócio, autenticação, importação do JSON nem alteração no frontend.

Validações concluídas: build pelo SDK e pelo MSBuild do Visual Studio; ferramenta EF; conexão e SELECT 1 pelo EF/Npgsql; HTTP e HTTPS com certificado confiável; PostgreSQL saudável sem tabelas de negócio. Instaladores concluídos com sucesso e sem solicitação de reinicialização.


## Etapa 1 — Estrutura HTTP e instruções de execução

A solução `Primora.slnx` está implementada em .NET 10. Os arquivos e comandos anteriores documentam o ambiente preparado; esta seção descreve a API definitiva. Tenant significa unidade nesta fase. A modelagem e a migration da Etapa 2 estão descritas ao final deste documento. A importação oficial foi implementada na Etapa 3, descrita ao final; autenticação ainda não está implementada.

### Projetos e referências

```text
Primora.slnx
src/
  Primora.Api/               Controllers, contratos HTTP, OpenAPI e erros
  Primora.Application/       Casos de uso, contratos e interfaces
  Primora.Domain/            Entidades e regras de integridade
  Primora.Infrastructure/    DbContext, PostgreSQL e implementações das interfaces
  Primora.Importador/        Executável reservado para importação futura
tests/
  Primora.UnitTests/         Testes do serviço de disponibilidade
  Primora.IntegrationTests/  Inicialização, HTTP, OpenAPI, erros e PostgreSQL real
```

Referências: Application → Domain; Infrastructure → Application e Domain; Api e Importador → Application e Infrastructure. UnitTests → Application; IntegrationTests → Api. Não há dependência circular. Domain não depende de HTTP ou de EF Core.

A injeção de dependência é organizada por `AddApplication()` e `AddInfrastructure()`. `PrimoraDbContext` configura as nove entidades da Etapa 2. A API nunca chama `EnsureCreated`, `Migrate` ou qualquer comando para criar/recriar o banco. O Importador executa a validação e a carga transacional descritas na Etapa 3.

### Configuração e credenciais

A API lê `ConnectionStrings:Primora` por configuração padrão do ASP.NET Core. Não há conexão nem senha em `appsettings.json`, `launchSettings.json` ou código.

Os User Secrets da API já foram configurados para o usuário atual, permitindo iniciar diretamente no Visual Studio. Para configurar novamente a partir da senha protegida existente:

```powershell
.\scripts\Configure-PrimoraUserSecrets.ps1
```

O comando envia a conexão ao armazenamento local de User Secrets pela entrada padrão, sem incluir a senha na linha de comando. Preserve `.secrets/`; ela não deve ser versionada. User Secrets são um recurso local de desenvolvimento, fora do repositório, não um cofre de produção.

Como alternativa para o terminal, o script existente continua válido:

```powershell
. .\scripts\Set-PrimoraEnvironment.ps1
```

Variáveis de ambiente têm precedência sobre User Secrets. Nenhum desses scripts modifica a senha no PostgreSQL. Configuração de conexão ausente impede a inicialização com uma mensagem genérica, sem exibir credenciais.

### Executar pelo terminal

Abra o PowerShell na raiz `C:\Users\Guilherme\Projetos\primora-backend`:

```powershell
docker compose up -d --wait
dotnet restore Primora.slnx
dotnet build Primora.slnx --no-restore
dotnet run --project src/Primora.Api --launch-profile https
```

O perfil `https` disponibiliza `https://localhost:7246` e `http://localhost:5246`. Para executar somente HTTP:

```powershell
dotnet run --project src/Primora.Api --launch-profile http
```

Encerre com `Ctrl+C`. Não execute simultaneamente os dois perfis ou a antiga aplicação de `.checks/` nas mesmas portas. O diagnóstico antigo permanece preservado e não faz parte da solução.

### Executar no Visual Studio

1. Abra `Primora.slnx` no Visual Studio 2026.
2. Defina `Primora.Api` como projeto de inicialização.
3. Selecione o perfil `https`.
4. Confirme que `primora-postgres` está ativo no Docker Desktop.
5. Execute com F5 ou Ctrl+F5. O navegador abrirá `/health`.

Não é necessário copiar a senha para as propriedades do projeto: o perfil Development carrega os User Secrets já preparados. O Importador não deve ser selecionado para iniciar a API.

### Endpoints da etapa 1

| Método e rota | Resultado |
| --- | --- |
| `GET /health` | Consulta real `SELECT 1` via EF Core/Npgsql. Retorna 200 se a aplicação e o banco estiverem disponíveis; banco indisponível retorna 503. |
| `GET /openapi/v1.json` | Documento OpenAPI gerado a partir dos controllers, disponível apenas em Development. Não há dependência adicional de Swagger UI. |

Resposta saudável:

```json
{"status":"healthy","application":"available","database":"available"}
```

Falhas de banco retornam `application/problem+json` com status 503. Erros HTTP e exceções usam ProblemDetails com `status`, `title`, `detail`, `type` e `traceId`. Não retornam mensagens internas, stack traces, conexão ou credenciais. A consulta ao PostgreSQL tem timeout de conexão e comando de cinco segundos. HTTP é permitido no desenvolvimento; fora dele há redirecionamento para HTTPS e HSTS.

Exemplos com a API em execução:

```powershell
Invoke-RestMethod https://localhost:7246/health
Invoke-RestMethod http://localhost:5246/health
Invoke-RestMethod https://localhost:7246/openapi/v1.json
```

### Testes e validações

```powershell
docker compose up -d --wait
dotnet restore Primora.slnx
dotnet build Primora.slnx --no-restore
dotnet test Primora.slnx --no-build --no-restore
```

A base da Etapa 1 possui 3 casos unitários e 9 de integração, preservados na Etapa 2. Cobrem disponibilidade/cancelamento, inicialização e validação de configuração, respostas 200/503, ProblemDetails para 400/404/500, ausência de dados internos nas respostas e OpenAPI em Development/Production. Um teste marcado `Category=PostgreSQL` utiliza o banco real existente via configuração Development, sem criar ou remover tabelas. Os outros utilizam uma substituição da interface do banco no host de testes, sem parar o contêiner.

Para executar somente os testes que dispensam PostgreSQL:

```powershell
dotnet test Primora.slnx --filter 'Category!=PostgreSQL'
```

Os endpoints `__tests` existem apenas no host de testes, pois seus controllers pertencem ao projeto de integração, não à API.

A validação da etapa 1 inclui restore, build sem avisos, todos os testes, HTTP e HTTPS reais com o certificado confiável, leitura do OpenAPI e conferência de que o banco continua sem tabelas de negócio. O frontend React não foi alterado. Nenhum repositório remoto, commit ou push foi criado.

### Próxima etapa do backend

A modelagem e a migration inicial foram concluídas na Etapa 2; a importação idempotente foi concluída na Etapa 3. As próximas entregas serão os endpoints de negócio e a integração com o React. Agregados e cadastros detalhados devem permanecer separados: não criar clientes fictícios para completar a carteira informada no JSON. O isolamento e a autorização por unidade serão implementados nas etapas correspondentes; a estrutura atual não representa autenticação já pronta.


## Etapa 2 — Modelagem PostgreSQL e migration inicial

O banco foi inspecionado antes da aplicação e não continha tabelas, views ou sequências de usuário. O SQL da migration foi revisado: apenas criação de estruturas, índices e registro de controle do EF, sem remoção ou alteração de estruturas existentes. Uma cópia do esquema anterior e o SQL revisado estão em `.checks/antes-modelo-inicial.sql` e `.checks/ModeloInicial.sql`, fora dos arquivos versionáveis.

Migration aplicada: `20261108012712_ModeloInicial`, em `src/Primora.Infrastructure/Persistence/Migrations/`, acompanhada do Designer e do snapshot do modelo. O comando de verificação confirmou que não há mudanças de modelo pendentes.

### Entidades e tabelas

| Entidade de domínio | Tabela | Chave primária |
| --- | --- | --- |
| VersaoDataset | versoes_dataset | id (UUID) |
| Unidade | unidades | versao_id + id textual |
| IndicadoresUnidade | indicadores_unidade | versao_id + unidade_id |
| SerieEngajamentoUnidade | serie_engajamento_unidade | versao_id + unidade_id + posicao |
| SerieFunilUnidade | serie_funil_unidade | versao_id + unidade_id + posicao |
| FatorRiscoUnidade | fatores_risco_unidade | versao_id + unidade_id + ordem_origem |
| Cliente | clientes | versao_id + unidade_id + id textual |
| HistoricoSaudeCliente | historico_saude_cliente | versao_id + unidade_id + cliente_id + data |
| EventoCliente | eventos_cliente | versao_id + unidade_id + cliente_id + ordem_origem |

Existe também `__EFMigrationsHistory`, criada pelo EF Core para controlar migrations: são nove tabelas de domínio e uma tabela técnica. Ao concluir a Etapa 2, as tabelas estavam vazias. A carga posterior da Etapa 3 está documentada abaixo.

### Integridade e representação dos dados

- As oito FKs usam exclusão `RESTRICT`. Eventos e históricos referenciam a chave completa do cliente: versão, unidade e ID. Um registro de outra unidade ou de outra versão não pode ser associado por engano.
- Os IDs de unidade e cliente permanecem `text`, sem conversão para números ou UUIDs. O mesmo ID textual pode existir em unidades/versões diferentes com identidades compostas distintas.
- A ordem de unidades e clientes é única dentro da versão. A ordem de fatores é local à unidade; a de eventos, ao cliente. Todas são posições iniciadas em 1. Eventos não são deduplicados por horário ou tipo, pois o JSON não fornece um ID de evento.
- Carteira, engajamento, usuários ativos e contagens do funil são inteiros não negativos. A soma da distribuição de saúde deve corresponder à carteira, e o risco absoluto informado deve corresponder a risco + crítico. Essas condições foram conferidas no JSON oficial antes da modelagem. Não se somam etapas do funil como categorias exclusivas.
- A carteira agregada do JSON representa 677 clientes, mas só há três cadastros detalhados. O modelo não exige que a quantidade de linhas em clientes corresponda à carteira: indicadores agregados são preservados separadamente.
- Valores monetários usam `decimal`/`numeric(18,2)`. A série proporcional do funil usa `decimal`/`numeric` sem escala fixa, preservando a precisão recebida e valores entre 0 e 1. Fórmulas do Comparativo não foram modificadas nem adicionadas nesta etapa.
- Séries de unidade guardam posições de 1 a 30, únicas por unidade e versão. Não foram atribuídas datas a essas posições.
- Scores usam datas (`DateOnly`/`date`) e valores de 0 a 100, únicos por cliente/data.
- Eventos usam `DateTimeKind.Unspecified` e `timestamp without time zone`. Nenhum fuso foi deduzido e nenhum `Z` foi acrescentado. O instante de importação da versão é metadado separado em UTC (`DateTimeOffset`/`timestamptz`).
- Fatores de risco permanecem descrições textuais da origem, sem pesos, scores ou causas estruturadas inventadas.
- O hash SHA-256 da versão é obrigatório, hexadecimal, normalizado em minúsculas e único. A futura importação utilizará esse controle para idempotência.
- Configurações explícitas ficam em `Persistence/Configurations/`; o DbContext as carrega pelo assembly. Não há propriedades sombra, seeds ou migração automática no início da API.

As chaves compostas protegem a integridade referencial por unidade; autenticação, autorização e filtros de acesso multi-tenant serão implementados nas etapas correspondentes.

### Comandos EF Core

Na raiz do backend, utilizando a configuração local já preparada:

```powershell
. .\scripts\Set-PrimoraEnvironment.ps1
dotnet ef migrations list --project src/Primora.Infrastructure --startup-project src/Primora.Api
dotnet ef migrations has-pending-model-changes --project src/Primora.Infrastructure --startup-project src/Primora.Api
```

Para aplicar migrations pendentes em um ambiente previamente inspecionado:

```powershell
dotnet ef database update --project src/Primora.Infrastructure --startup-project src/Primora.Api
```

No ambiente local atual a migration inicial já está aplicada. Não é necessário recriar banco, volume ou contêiner. O método `Down` gerado é apenas o caminho de reversão do EF e não foi executado.

### Testes da Etapa 2

```powershell
dotnet build Primora.slnx --no-restore
dotnet test Primora.slnx --no-build --no-restore
```

Total aprovado: 46 casos (11 unitários e 35 de integração), incluindo os 12 anteriores. As novas verificações cobrem as nove entidades, tipos e FKs do modelo; posições e scores inválidos; consistência dos agregados; IDs textuais; precisão decimal; associação incorreta de unidade/versão; unicidade de hash/posição; exclusões restritas; descrições vazias e leitura dos horários sem conversão de fuso.

`PostgresIntegrityTests` usa o banco real existente com dados exclusivamente sintéticos, em uma transação por teste. Todas as transações são revertidas ao finalizar, inclusive nos cenários de violação de restrição. Não executa importação, recriação de banco, truncamento ou migration reversa. A migration deve estar aplicada antes de executar essa classe. Os testes marcados `Category=PostgreSQL` podem ser excluídos com o filtro já documentado.

O `/health` permanece com o contrato da Etapa 1. Frontend, Docker, credenciais e User Secrets foram preservados. Não foram criados endpoints de negócio, commit ou push.


## Etapa 3 — Importação oficial do JSON

O executável `Primora.Importador` lê o arquivo original do frontend sem modificá-lo. A camada Application valida e prepara todas as entidades em memória; a Infrastructure grava pelo EF Core em uma transação PostgreSQL. Não há endpoints de importação, alteração de schema ou migration automática.

### Executar novamente

Abra o PowerShell na raiz do backend:

```powershell
cd "C:\Users\Guilherme\Projetos\primora-backend"
docker compose start
. .\scripts\Set-PrimoraEnvironment.ps1
dotnet run --project src/Primora.Importador -- "C:\Users\Guilherme\Projetos\desafio-primora-painel-inteligencia\src\data\dataset_painel_multitenant.json"
```

Se o Docker Desktop estiver fechado, inicie-o antes do comando. O script de ambiente existente carrega a conexão protegida no terminal. Não é necessário copiar credenciais para o projeto do importador.

O importador exige um caminho explícito para o arquivo. `--help` mostra o uso. Retorna código 0 para uma importação bem-sucedida ou conteúdo já importado, 2 para uso/dataset inválido, 1 para falha operacional e 130 para cancelamento.

### Idempotência e transação

- O SHA-256 é calculado sobre os bytes originais do arquivo, incluindo um possível BOM. Renomear/copiar o mesmo arquivo não cria versão nova. Alterações de bytes, inclusive formatação, têm outro hash e, quando válidas, geram outra versão.
- O nome de origem e o instante da primeira importação permanecem preservados ao repetir o mesmo hash. `geradoEm` é a data de referência; conteúdos válidos diferentes podem ter a mesma data e versões distintas.
- A consulta do hash e a gravação são protegidas por um lock transacional PostgreSQL derivado do hash. O índice único existente em `hash_sha256` permanece como restrição adicional.
- Todos os campos e registros são validados antes de qualquer escrita. A versão e os dependentes são gravados em duas chamadas de SaveChanges dentro da mesma transação, confirmada somente após concluir todas as inserções.
- Se houver uma falha, a operação reverte suas gravações. Em uma transação externa, utiliza savepoint e preserva o trabalho anterior do chamador. O importador exige um DbContext sem alterações pendentes e remove do rastreamento suas entidades após rollback.
- Não atualiza, apaga ou limpa versões já importadas. A CLI não imprime configuração, SQL ou credenciais.

### Validação e preservação dos dados

Campos obrigatórios, tipos, arrays, propriedades desconhecidas/duplicadas, IDs duplicados e referências de unidade são verificados. O `tenantNome` do cliente deve corresponder ao nome da unidade. Para compatibilidade com o dataset e os Selects atuais, IDs de clientes são únicos dentro do arquivo, embora o modelo físico suporte IDs iguais em escopos diferentes.

São mantidas as regras de integridade do domínio, sem calcular a carteira contando cadastros detalhados e sem somar etapas do funil como categorias exclusivas. Valores monetários que exigiriam arredondamento ou excederiam numeric(18,2) são rejeitados; decimais não representáveis com exatidão também são rejeitados.

As séries de unidade mantêm 30 posições, de 1 a 30, sem datas inventadas. Scores devem ter datas únicas em ordem crescente, representáveis pelo histórico atual. Eventos mantêm a ordem original com `ordem_origem`, inclusive quando ocorrem no mesmo horário. Seus horários devem estar sem fuso e caber na precisão de microssegundos do PostgreSQL; valores incompatíveis são rejeitados, sem conversão silenciosa. Não são criados scores, eventos, clientes, fatores de risco ou métricas adicionais.

### Carga oficial verificada

Versão importada: `b534eb00-6b66-4f06-98e4-70fba8b80926`.

SHA-256: `57d1bccadc5e2a708f29c5aa67219a5702d40271d4219a8131add469fb112d2e`.

| Tabela | Registros |
| --- | ---: |
| versoes_dataset | 1 |
| unidades | 4 |
| indicadores_unidade | 4 |
| clientes | 3 |
| historico_saude_cliente | 16 |
| eventos_cliente | 30 |
| serie_engajamento_unidade | 120 |
| serie_funil_unidade | 120 |
| fatores_risco_unidade | 7 |

Os indicadores representam uma carteira agregada de 677 clientes; os três cadastros detalhados permanecem apenas três. A segunda execução retornou `novaVersao: false`, com o mesmo ID e as contagens inalteradas. As contagens exibidas pela CLI descrevem a versão, não a quantidade de novas linhas inseridas em uma repetição.

Uma consulta independente reconstruiu o JSON a partir das nove tabelas, respeitando posições e ordens de origem, e comparou todos os campos e valores ao arquivo original com precisão decimal. Nenhuma divergência foi encontrada. O hash do arquivo original foi preservado.

### Testes da Etapa 3

```powershell
dotnet restore Primora.slnx
dotnet build Primora.slnx --no-restore
. .\scripts\Set-PrimoraEnvironment.ps1
dotnet test Primora.slnx --no-build --no-restore
```

A suíte contém 90 casos: 43 unitários e 47 de integração, preservando os 46 testes anteriores. Os novos testes cobrem validação completa, conteúdo inválido, IDs duplicados, tenant inexistente, campos incompatíveis, hashes/BOM, primeira importação, reimportação, novo conteúdo, relacionamentos e leitura exata dos valores. Simulam falha após gravar a versão tanto em transação própria quanto em savepoint, confirmando rollback integral.

Os testes PostgreSQL de importação utilizam fixtures sintéticas e revertem suas transações. Não limpam o banco e podem ser executados com o dataset oficial já importado. Os arquivos de conferência em `.checks/` são diagnósticos locais, ignorados pelo Git, e não são dependências do importador.

Arquivos principais: `Application/Importacao/` (contratos e leitor), `Infrastructure/Importacao/EfDatasetImportador.cs` (persistência), `Importador/Program.cs` (CLI), `tests/Fixtures/DatasetFixture.cs` e os testes `DatasetReaderTests`/`DatasetImportacaoTests`.

A migration `20261108012712_ModeloInicial`, o Docker, as credenciais e o frontend foram preservados. Nenhum endpoint de negócio, commit ou push foi criado nesta etapa.


## Etapa 4 — Versionamento e colaboração acadêmica

O backend é um repositório independente do frontend. O commit inicial local tem a mensagem `Estrutura inicial do backend Primora`. A branch `develop` parte da mesma versão da `main` e será a base dos próximos trabalhos.

### Publicação preparada, aguardando aprovação

- Conta proprietária: `guilhermelima2705`.
- Nome: `primora-backend`.
- Visibilidade: **Private**.
- Descrição proposta: API REST do painel Primora com ASP.NET Core 10, EF Core e PostgreSQL.
- Branch inicial proposta: `main`, com `develop` também publicada para desenvolvimento.
- Criar o repositório remoto vazio, pois README, gitignore, solução e migrations já fazem parte do histórico local.

A criação no GitHub, a associação do remoto e o primeiro push dependem de aprovação. Os comandos abaixo são apenas o plano para depois da criação autorizada:

```powershell
git remote add origin https://github.com/guilhermelima2705/primora-backend.git
git push --set-upstream origin main
git push --set-upstream origin develop
```

Depois de publicar, o proprietário poderá convidar os integrantes pelas contas GitHub ou e-mails em Settings > Collaborators. Cada convite deve ser autorizado e o integrante precisa aceitá-lo. Documentação: [convites de colaboradores no GitHub](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/repository-access-and-collaboration/inviting-collaborators-to-a-personal-repository).

Fluxo combinado para o projeto: criar branches `feature/...` a partir de `develop`, revisar por Pull Request para `develop` e promover versões estáveis para `main` por outra PR.

### Arquivos versionados e ambiente individual

O repositório inclui solução, projetos, código-fonte, configurações sem segredos, migration e snapshot, testes, fixture sintética, Docker Compose e scripts que leem segredos locais. O identificador `UserSecretsId` presente no projeto é metadado de configuração, não contém as credenciais.

`.gitignore` exclui `.secrets/`, `.env` e variantes, arquivos de User Secrets/credenciais, chaves e certificados privados, binários, `bin/`, `obj/`, caches, preferências locais de IDE, resultados de testes e `.checks/`. Os arquivos locais permanecem no computador; não são enviados ao Git. `.gitattributes` mantém os scripts `.sh` com LF para execução no contêiner Linux.

Cada integrante deve configurar seu próprio PostgreSQL local e suas próprias senhas. Não copiar as credenciais ou o banco de outro integrante. Em um clone novo, antes da primeira inicialização do seu volume local, crie os dois arquivos secretos exigidos pelo Compose:

```powershell
# Executar somente no ambiente novo do integrante, antes de inicializar o banco.
$secretFolder = Join-Path (Get-Location) '.secrets'
if (Test-Path -LiteralPath $secretFolder) { throw 'A pasta já existe: verificar a configuração local antes de continuar.' }
New-Item -ItemType Directory -Path $secretFolder | Out-Null
foreach ($secretName in @('postgres_admin_password', 'primora_app_password')) {
    $secretValue = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    [IO.File]::WriteAllText((Join-Path $secretFolder $secretName), $secretValue)
}
```

Restrinja o acesso dessa pasta ao usuário local. Depois siga os comandos já documentados para iniciar seu banco, configurar os User Secrets, restaurar a solução e aplicar a migration. Os scripts de inicialização do PostgreSQL só rodam em um volume novo: para um volume já existente, preserve as senhas atuais. Esta etapa de Git não alterou o banco nem gerou novas credenciais.

O dataset oficial continua no frontend separado; cada integrante deve obter o arquivo fornecido para a atividade e informar seu caminho ao importador. Os dados já importados no computador do autor não são distribuídos pelo Git.


## Sprint 6 — Endpoints do painel

- `GET /unidades` retorna o array de unidades com indicadores agregados, distribuições, séries e fatores de risco.
- `GET /clientes` retorna o array de clientes detalhados com histórico de saúde e eventos.
- Ambos consultam o PostgreSQL em modo de leitura e servem apenas a versão cujo SHA-256 está em `DatasetPublicacao:HashSha256`. O padrão é o hash do dataset acadêmico importado na Etapa 3. Em outro ambiente, use `DatasetPublicacao__HashSha256` para publicar outra versão já importada. Se o hash não existir no banco, ambas as rotas retornam `[]`.
- Quando a versão existe, `X-Dataset-Gerado-Em` informa a data original `YYYY-MM-DD`. Datas e horários de eventos são enviados sem deslocamento de fuso. Eventos sem valor omitem a propriedade `valor`; zero é preservado.
- Em Development, o CORS permite `http://localhost:5173` e `http://127.0.0.1:5173`. Em produção, configure `Cors__AllowedOrigins__0` com a origem HTTPS exata do frontend, sem barra final. Adicione outras origens autorizadas por índice, quando necessário. A política expõe `X-Dataset-Gerado-Em` e permite GET sem credenciais CORS.

Com o Docker Desktop ativo, inicie o contêiner já configurado e execute na raiz do repositório:

```powershell
docker compose start
. .\scripts\Set-PrimoraEnvironment.ps1
dotnet run --project src/Primora.Api --launch-profile https
```

Em outro PowerShell 7.5 ou posterior, na raiz do repositório:

```powershell
Invoke-RestMethod https://localhost:7246/health
.\scripts\Test-PrimoraDatasetApi.ps1 -DatasetPath '..\desafio-primora-painel-inteligencia\src\data\dataset_painel_multitenant.json'
```

O script compara as duas respostas com o JSON acadêmico, incluindo ordem dos arrays, campos aninhados, datas e valores numéricos. Para os testes automatizados da solução, carregue o ambiente com `Set-PrimoraEnvironment.ps1` e execute `dotnet test Primora.slnx`.
